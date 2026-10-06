"""Build a bounded, game-scale Badwater terrain study with 256 m mesh chunks.

This is a Blender visual/source scene and a candidate height-tile handoff. It
does not contain a Unity streaming implementation, collision, or game camera.
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Euler

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_region_study import collection, mesh_tile


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--config", type=Path, required=True)
    parser.add_argument("--color", type=Path, required=True)
    parser.add_argument("--scan-manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def image_node(nodes, links, name: str, path: Path, vector=None, *, non_color=False):
    node = nodes.new("ShaderNodeTexImage")
    node.label = name
    node.image = bpy.data.images.load(str(path), check_existing=True)
    node.extension = "REPEAT" if vector is not None else "EXTEND"
    if non_color:
        node.image.colorspace_settings.name = "Non-Color"
    if vector is not None:
        links.new(vector, node.inputs["Vector"])
    return node


def mix_color(nodes, links, name: str, factor, color_a, color_b):
    node = nodes.new("ShaderNodeMixRGB")
    node.label = name
    node.blend_type = "MIX"
    if isinstance(factor, (int, float)):
        node.inputs[0].default_value = factor
    else:
        links.new(factor, node.inputs[0])
    if isinstance(color_a, tuple):
        node.inputs[1].default_value = color_a
    else:
        links.new(color_a, node.inputs[1])
    if isinstance(color_b, tuple):
        node.inputs[2].default_value = color_b
    else:
        links.new(color_b, node.inputs[2])
    return node.outputs["Color"]


def materials(color_path: Path, mask_path: Path, scan_manifest: Path):
    scan = json.loads(scan_manifest.read_text())
    def scan_path(key: str) -> Path:
        return (scan_manifest.parent / scan["maps"][key]["path"]).resolve()

    geographic = bpy.data.materials.new("Reference_NAIP_Landsat_NotGameMaterial")
    geographic.use_nodes = True
    n, l = geographic.node_tree.nodes, geographic.node_tree.links
    geo = image_node(n, l, "Recorded geographic color; visual reference", color_path)
    p = n.get("Principled BSDF")
    l.new(geo.outputs["Color"], p.inputs["Base Color"])
    p.inputs["Roughness"].default_value = 0.9

    art = bpy.data.materials.new("Provisional_BarrenTerrain_ArtSurface")
    art.use_nodes = True
    n, l = art.node_tree.nodes, art.node_tree.links
    p = n.get("Principled BSDF")
    p.inputs["Roughness"].default_value = 0.9
    macro = image_node(n, l, "Subtle geographic macro tint", color_path)
    masks = image_node(n, l, "R bedrock G talus B playa; provisional art masks", mask_path, non_color=True)
    separate = n.new("ShaderNodeSeparateColor")
    l.new(masks.outputs["Color"], separate.inputs["Color"])
    bedrock, talus, playa = (separate.outputs[key] for key in ("Red", "Green", "Blue"))
    base = mix_color(n, l, "Alluvium to talus", talus,
                     (0.46, 0.37, 0.29, 1), (0.34, 0.28, 0.23, 1))
    base = mix_color(n, l, "Talus to exposed bedrock", bedrock,
                     base, (0.27, 0.22, 0.20, 1))
    base = mix_color(n, l, "Pale basin floor", playa,
                     base, (0.69, 0.65, 0.57, 1))
    base = mix_color(n, l, "Geographic tint at macro scale", 0.80,
                     base, macro.outputs["Color"])

    geometry = n.new("ShaderNodeNewGeometry")
    scale = n.new("ShaderNodeVectorMath")
    scale.operation = "SCALE"
    scale.inputs["Scale"].default_value = 0.45
    l.new(geometry.outputs["Position"], scale.inputs["Vector"])
    diffuse = image_node(n, l, "CC0 rocky ground micro color", scan_path("Diffuse"), scale.outputs["Vector"])
    rock_weight = n.new("ShaderNodeMath")
    rock_weight.operation = "MAXIMUM"
    l.new(bedrock, rock_weight.inputs[0])
    l.new(talus, rock_weight.inputs[1])
    rock_strength = n.new("ShaderNodeMath")
    rock_strength.operation = "MULTIPLY"
    rock_strength.inputs[1].default_value = 0.23
    l.new(rock_weight.outputs[0], rock_strength.inputs[0])
    strength = n.new("ShaderNodeMath")
    strength.operation = "ADD"
    strength.inputs[1].default_value = 0.17
    l.new(rock_strength.outputs[0], strength.inputs[0])
    rough_color = mix_color(n, l, "Subtle scanned grain on alluvium, stronger on rock", strength.outputs[0],
                           base, diffuse.outputs["Color"])
    l.new(rough_color, p.inputs["Base Color"])

    normal_image = image_node(n, l, "CC0 rocky ground OpenGL normal", scan_path("nor_gl"),
                              scale.outputs["Vector"], non_color=True)
    normal = n.new("ShaderNodeNormalMap")
    l.new(normal_image.outputs["Color"], normal.inputs["Color"])
    l.new(strength.outputs[0], normal.inputs["Strength"])
    noise = n.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 0.9
    noise.inputs["Detail"].default_value = 2
    l.new(geometry.outputs["Position"], noise.inputs["Vector"])
    bump = n.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.16
    bump.inputs["Distance"].default_value = 0.07
    l.new(noise.outputs["Fac"], bump.inputs["Height"])
    l.new(normal.outputs["Normal"], bump.inputs["Normal"])
    fine_noise = n.new("ShaderNodeTexNoise")
    fine_noise.inputs["Scale"].default_value = 8.0
    fine_noise.inputs["Detail"].default_value = 3
    l.new(geometry.outputs["Position"], fine_noise.inputs["Vector"])
    fine_bump = n.new("ShaderNodeBump")
    fine_bump.inputs["Strength"].default_value = 0.42
    fine_bump.inputs["Distance"].default_value = 0.035
    l.new(fine_noise.outputs["Fac"], fine_bump.inputs["Height"])
    l.new(bump.outputs["Normal"], fine_bump.inputs["Normal"])
    l.new(fine_bump.outputs["Normal"], p.inputs["Normal"])
    rough = image_node(n, l, "CC0 rocky ground roughness", scan_path("Rough"),
                       scale.outputs["Vector"], non_color=True)
    l.new(rough.outputs["Color"], p.inputs["Roughness"])
    return art, geographic


def coarse_tile_with_native_edge(name: str, regular_tile: np.ndarray, native: np.ndarray,
                                 row: int, col: int, bounds: list[float],
                                 full_bounds: list[float], origin: tuple[float, float],
                                 high_rows: set[int], high_cols: set[int],
                                 destination, art, geographic):
    """Add source-height midpoint vertices only where a 2 m tile meets a 1 m tile."""
    def high(r: int, c: int) -> bool:
        return r in high_rows and c in high_cols

    neighbors = [side for side, active in (
        ("north", high(row - 1, col)), ("south", high(row + 1, col)),
        ("west", high(row, col - 1)), ("east", high(row, col + 1))) if active]
    if len(neighbors) > 1:
        raise ValueError("The current edge triangulation supports one high neighbor per coarse tile")
    side = neighbors[0] if neighbors else None
    x0, y0, x1, y1 = bounds
    vertices = [(x0 + c*2 - origin[0], y1 - r*2 - origin[1], float(regular_tile[r, c]))
                for r in range(129) for c in range(129)]
    faces = []
    for r in range(128):
        for c in range(128):
            a = r*129+c
            b = a+1
            d = a+129
            e = d+1
            edge = ((side == "north" and r == 0) or
                    (side == "south" and r == 127) or
                    (side == "west" and c == 0) or
                    (side == "east" and c == 127))
            if not edge:
                faces.append((a, d, e, b))
                continue
            if side in ("north", "south"):
                native_row = row*256 + (0 if side == "north" else 256)
                native_col = col*256 + c*2 + 1
            else:
                native_row = row*256 + r*2 + 1
                native_col = col*256 + (0 if side == "west" else 256)
            x = full_bounds[0] + native_col
            y = full_bounds[3] - native_row
            midpoint = len(vertices)
            vertices.append((x - origin[0], y - origin[1], float(native[native_row, native_col])))
            if side == "north":
                faces.extend(((a, d, midpoint), (d, e, midpoint), (e, b, midpoint)))
            elif side == "south":
                faces.extend(((a, d, midpoint), (a, midpoint, e), (a, e, b)))
            elif side == "west":
                faces.extend(((b, a, midpoint), (b, midpoint, d), (b, d, e)))
            else:
                faces.extend(((a, d, e), (a, e, midpoint), (a, midpoint, b)))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="SourceGridUV")
    for polygon in mesh.polygons:
        polygon.use_smooth = True
        for loop_index in polygon.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            uv.data[loop_index].uv = ((vertex.x + origin[0] - full_bounds[0]) / (full_bounds[2]-full_bounds[0]),
                                      (vertex.y + origin[1] - full_bounds[1]) / (full_bounds[3]-full_bounds[1]))
    obj = bpy.data.objects.new(name, mesh)
    destination.objects.link(obj)
    mesh.materials.append(art)
    mesh.materials.append(geographic)
    obj["source_resolution_m"] = 2
    obj["native_edge_transition"] = side or "none"
    return obj


def main() -> None:
    args = arguments()
    manifest = json.loads(args.manifest.read_text())
    config = json.loads(args.config.read_text())
    bounds = manifest["bounds_m"]
    if bounds != config["bounds_m"] or manifest["working_crs"] != config["working_crs"]:
        raise ValueError("Manifest/config geography mismatch")
    for path in (args.color, args.scan_manifest, Path(manifest["surface_masks_rgb"])):
        if not path.is_file():
            raise FileNotFoundError(path)
    with np.load(manifest["terrain_npz"]) as arrays:
        native = arrays["native_1m"]
        regular = arrays["regular_2m"]
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1
    origin = ((bounds[0] + bounds[2]) / 2, (bounds[1] + bounds[3]) / 2)
    art, geographic = materials(args.color, Path(manifest["surface_masks_rgb"]), args.scan_manifest)
    outer = collection("Badwater_2m_256mChunks")
    inner = collection("Badwater_1m_Focus_256mChunks")
    core_rows = set(config["high_detail_rows"])
    core_cols = set(config["high_detail_columns"])
    count = int((bounds[2] - bounds[0]) / config["chunk_size_m"])
    triangles = 0
    for tile in manifest["tiles"]:
        row, col = tile["row_north_to_south"], tile["column_west_to_east"]
        local = tile["bounds_m"]
        high = row in core_rows and col in core_cols
        if high:
            fine = native[row*256:row*256+257, col*256:col*256+257]
            data = fine
            spacing, destination, layer = 1, inner, "1m"
            obj = mesh_tile(f"Badwater_{layer}_{tile['id']}", data, 0, data.shape[0]-1,
                            0, data.shape[1]-1, local, spacing, origin, destination, art,
                            texture_bounds=bounds)
            obj.data.materials.append(geographic)
        else:
            data = regular[row*128:row*128+129, col*128:col*128+129]
            spacing, destination, layer = 2, outer, "2m"
            obj = coarse_tile_with_native_edge(f"Badwater_{layer}_{tile['id']}", data, native,
                                               row, col, local, bounds, origin, core_rows,
                                               core_cols, destination, art, geographic)
        obj["chunk_id"] = tile["id"]
        obj["chunk_size_m"] = 256
        obj["lod_resolution_m"] = spacing
        obj["height_source"] = config["source_product"]
        obj["height_raw_candidate"] = tile["lod1m" if high else "lod2m"]["path"]
        obj["absolute_bounds_epsg26911"] = json.dumps(local)
        triangles += sum(len(poly.vertices)-2 for poly in obj.data.polygons)
    if len(outer.objects) != count*count-4 or len(inner.objects) != 4:
        raise RuntimeError("Unexpected terrain chunk count")
    scene["source_manifest"] = str(args.manifest)
    scene["source_product"] = config["source_product"]
    scene["absolute_bounds_epsg26911"] = json.dumps(bounds)
    scene["local_origin_epsg26911"] = json.dumps(origin)
    scene["chunk_size_m"] = 256
    scene["visible_terrain_triangles"] = triangles
    scene["material_status"] = "Provisional art surface; geographic slot 1 is comparison only"
    scene["runtime_status"] = "Unity import, streaming, collision and gameplay unverified"
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.world.color = (0.16, 0.14, 0.13)
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type != "VIEW_3D":
                continue
            space = area.spaces.active
            space.clip_start = 0.1
            space.clip_end = 15000
            space.shading.type = "MATERIAL"
            space.region_3d.view_perspective = "PERSP"
            space.region_3d.view_location = (-370, -260, 160)
            space.region_3d.view_distance = 1850
            space.region_3d.view_rotation = Euler((math.radians(57), 0,
                                                   math.radians(-35)), "XYZ").to_quaternion()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(args.output))
    print(json.dumps({"chunks_2m": len(outer.objects), "chunks_1m": len(inner.objects),
                      "visible_triangles": triangles, "output": str(args.output)}))


if __name__ == "__main__":
    main()
