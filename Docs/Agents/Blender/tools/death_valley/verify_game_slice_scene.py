"""Check saved Blender chunk vertices against the native DEM and all neighboring edges."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bpy
import numpy as np


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def main() -> None:
    args = arguments()
    record = json.loads(args.manifest.read_text())
    bounds = record["bounds_m"]
    origin = json.loads(bpy.context.scene["local_origin_epsg26911"])
    with np.load(record["terrain_npz"]) as arrays:
        native = arrays["native_1m"]
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    if len(meshes) != 64:
        raise RuntimeError(f"Expected 64 chunk meshes, got {len(meshes)}")
    cameras = [obj for obj in bpy.data.objects if obj.type == "CAMERA"]
    if len(cameras) != 1 or cameras[0].name != "BasinEyeView_1p7m":
        raise RuntimeError("Expected exactly one basin-eye review camera")
    camera = cameras[0]
    if bpy.context.scene.camera != camera:
        raise RuntimeError("Basin-eye review camera is not the active scene camera")
    viewports = [area.spaces.active for screen in bpy.data.screens for area in screen.areas
                 if area.type == "VIEW_3D"]
    if not viewports or not any(space.region_3d.view_perspective == "CAMERA" and
                                space.lock_camera for space in viewports):
        raise RuntimeError("Saved scene has no camera-locked viewport")
    absolute_xy = json.loads(camera["absolute_xy_epsg26911"])
    x,y = absolute_xy
    ix,iy = round(x-bounds[0]),round(bounds[3]-y)
    ground = float(native[iy,ix])
    eye_height = float(camera.location.z)-ground
    if (abs(camera.location.x-(x-origin[0]))>1e-4 or
        abs(camera.location.y-(y-origin[1]))>1e-4 or
        abs(eye_height-1.7)>1e-4 or
        abs(float(camera["source_ground_elevation_m"])-ground)>1e-4):
        raise RuntimeError("Basin-eye camera is not 1.7 m above its source DEM position")
    tiles = {(int(obj["chunk_id"][1:3]), int(obj["chunk_id"][5:7])): obj for obj in meshes}
    if len(tiles) != 64:
        raise RuntimeError("Chunk IDs are missing or duplicated")
    max_source_error = 0.0
    edge_vertices = {}
    triangles = 0
    for key,obj in tiles.items():
        row,col = key
        tile = next(t for t in record["tiles"] if t["id"] == obj["chunk_id"])
        x0,y0,x1,y1 = tile["bounds_m"]
        edge = {name:{} for name in ("north","south","west","east")}
        for vertex in obj.data.vertices:
            x = vertex.co.x + origin[0]
            y = vertex.co.y + origin[1]
            ix = round(x-bounds[0])
            iy = round(bounds[3]-y)
            if abs(x-(bounds[0]+ix))>1e-4 or abs(y-(bounds[3]-iy))>1e-4:
                raise RuntimeError(f"Mesh vertex is off native 1 m grid: {obj.name}")
            error = abs(vertex.co.z-float(native[iy,ix]))
            max_source_error=max(max_source_error,error)
            if error>1e-4:
                raise RuntimeError(f"Mesh vertex height differs from source: {obj.name}, {error}")
            point=(ix,iy)
            if y==y1:edge["north"][point]=vertex.co.z
            if y==y0:edge["south"][point]=vertex.co.z
            if x==x0:edge["west"][point]=vertex.co.z
            if x==x1:edge["east"][point]=vertex.co.z
        edge_vertices[key]=edge
        triangles+=sum(len(poly.vertices)-2 for poly in obj.data.polygons)
    for row in range(8):
        for col in range(8):
            current=edge_vertices[(row,col)]
            if row<7 and current["south"]!=edge_vertices[(row+1,col)]["north"]:
                raise RuntimeError(f"Mesh north/south seam at {row},{col}")
            if col<7 and current["east"]!=edge_vertices[(row,col+1)]["west"]:
                raise RuntimeError(f"Mesh east/west seam at {row},{col}")
    result={"mesh_chunks":len(meshes),"triangles":triangles,"packed_images":sum(bool(i.packed_file) for i in bpy.data.images),
            "cameras":1,"basin_eye_height_m":eye_height,"all_mesh_heights_from_1m_source":True,"maximum_mesh_source_error_m":max_source_error,
            "all_mesh_edges_identical":True,"note":"Art material bump is shading only; it does not change the DEM mesh."}
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(result,indent=2)+"\n")
    print("SCENE_AUDIT",json.dumps(result))


if __name__=="__main__":
    main()
