"""Build a separate tiled Blender study from prepared Death Valley arrays.

Run with Blender in background mode, for example:
  Blender -b --python build_region_study.py -- --manifest /path/manifest.json --output /path/study.blend

The study is a visual reference. It is not a Unity terrain export or a replacement
for the World Creator's absolute-coordinate and chunked world authority.
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--render-dir", type=Path)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def collection(name: str) -> bpy.types.Collection:
    result = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(result)
    return result


def material(name: str, image_path: str) -> bpy.types.Material:
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    result.diffuse_color = (0.48, 0.42, 0.35, 1)
    nodes = result.node_tree.nodes
    principled = nodes.get("Principled BSDF")
    principled.inputs["Roughness"].default_value = 0.9
    image = nodes.new("ShaderNodeTexImage")
    image.label = "USGS NAIP color reference, not a close-range game texture"
    image.image = bpy.data.images.load(image_path, check_existing=True)
    image.interpolation = "Linear"
    result.node_tree.links.new(image.outputs["Color"], principled.inputs["Base Color"])
    return result


GEOLOGY_COLORS = {
    0: (0.33, 0.30, 0.29, 1),
    1: (0.53, 0.43, 0.31, 1),  # alluvium and dry washes
    2: (0.30, 0.24, 0.23, 1),
    3: (0.45, 0.32, 0.27, 1),
    4: (0.31, 0.24, 0.23, 1),
    5: (0.39, 0.34, 0.31, 1),
    6: (0.49, 0.39, 0.34, 1),
    7: (0.36, 0.30, 0.29, 1),
    8: (0.62, 0.55, 0.45, 1),  # Paleozoic sedimentary
    9: (0.37, 0.31, 0.29, 1),
    10: (0.44, 0.38, 0.38, 1), # Precambrian basement
}


def geology_material(name: str) -> bpy.types.Material:
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    nodes = result.node_tree.nodes
    links = result.node_tree.links
    principled = nodes.get("Principled BSDF")
    principled.inputs["Roughness"].default_value = 0.93
    attribute = nodes.new("ShaderNodeVertexColor")
    attribute.layer_name = "GeologyPalette"
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 0.018
    noise.inputs["Detail"].default_value = 3
    geometry = nodes.new("ShaderNodeNewGeometry")
    links.new(geometry.outputs["Position"], noise.inputs["Vector"])
    mix = nodes.new("ShaderNodeMixRGB")
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 0.28
    links.new(attribute.outputs["Color"], mix.inputs[1])
    links.new(noise.outputs["Color"], mix.inputs[2])
    links.new(mix.outputs["Color"], principled.inputs["Base Color"])
    micro = nodes.new("ShaderNodeTexNoise")
    micro.inputs["Scale"].default_value = 2.5
    micro.inputs["Detail"].default_value = 3
    links.new(geometry.outputs["Position"], micro.inputs["Vector"])
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.17
    bump.inputs["Distance"].default_value = 0.09
    links.new(micro.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])
    return result


def mesh_tile(
    name: str,
    data: np.ndarray,
    start_row: int,
    end_row: int,
    start_col: int,
    end_col: int,
    bounds: list[float],
    spacing: float,
    origin: tuple[float, float],
    destination: bpy.types.Collection,
    tile_material: bpy.types.Material,
    geology: np.ndarray | None = None,
    z_offset: float = 0,
) -> bpy.types.Object:
    xmin, ymin, xmax, ymax = bounds
    rows = end_row - start_row + 1
    cols = end_col - start_col + 1
    vertices = [
        (
            xmin + (start_col + c) * spacing - origin[0],
            ymax - (start_row + r) * spacing - origin[1],
            float(data[start_row + r, start_col + c]) + z_offset,
        )
        for r in range(rows) for c in range(cols)
    ]
    faces = []
    for r in range(rows - 1):
        for c in range(cols - 1):
            a = r * cols + c
            faces.append((a, a + cols, a + cols + 1, a + 1))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="SourceGridUV")
    colors = mesh.vertex_colors.new(name="GeologyPalette") if geology is not None else None
    width = xmax - xmin
    height = ymax - ymin
    for poly in mesh.polygons:
        for loop_index in poly.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            absolute_x = vertex.x + origin[0]
            absolute_y = vertex.y + origin[1]
            uv.data[loop_index].uv = ((absolute_x - xmin) / width, (absolute_y - ymin) / height)
            if colors is not None:
                local_index = mesh.loops[loop_index].vertex_index
                source_row = start_row + local_index // cols
                source_col = start_col + local_index % cols
                colors.data[loop_index].color = GEOLOGY_COLORS.get(int(geology[source_row, source_col]), GEOLOGY_COLORS[0])
        poly.use_smooth = True
    obj = bpy.data.objects.new(name, mesh)
    destination.objects.link(obj)
    obj.data.materials.append(tile_material)
    obj["source_bounds_epsg26911"] = json.dumps(bounds)
    obj["source_resolution_m"] = spacing
    obj["source_row_range"] = json.dumps([start_row, end_row])
    obj["source_column_range"] = json.dumps([start_col, end_col])
    return obj


def build_terrain(
    name: str,
    record: dict,
    origin: tuple[float, float],
    destination: bpy.types.Collection,
    step: int,
    z_offset: float = 0,
) -> tuple[int, int]:
    with np.load(record["prepared_npz"]) as arrays:
        elevation = arrays["elevation"]
    geology_path = Path(record["prepared_npz"]).parent / "geology.npz"
    with np.load(geology_path) as arrays:
        geology = arrays["unit_code"]
    bounds = record["bounds_m"]
    spacing = float(record["resolution_m"])
    expected = (round((bounds[3] - bounds[1]) / spacing) + 1, round((bounds[2] - bounds[0]) / spacing) + 1)
    if elevation.shape != expected or not np.isfinite(elevation).all():
        raise ValueError(f"Invalid grid for {name}: got {elevation.shape}, expected {expected}")
    image_material = material(f"{name}_NAIP_StudyReference", record["color_reference_png"])
    authored_material = geology_material(f"{name}_GeologyPalette_Authored")
    objects = 0
    for row in range(0, elevation.shape[0] - 1, step):
        end_row = min(row + step, elevation.shape[0] - 1)
        for col in range(0, elevation.shape[1] - 1, step):
            end_col = min(col + step, elevation.shape[1] - 1)
            tile = mesh_tile(
                f"{name}_r{row:04d}_c{col:04d}", elevation,
                row, end_row, col, end_col, bounds, spacing, origin,
                destination, authored_material, geology, z_offset,
            )
            tile.data.materials.append(image_material)
            tile["material_slot_0"] = "authored geology palette"
            tile["material_slot_1"] = "NAIP study reference; not for game export"
            objects += 1
    triangles = 2 * (elevation.shape[0] - 1) * (elevation.shape[1] - 1)
    return objects, triangles


def look_at(camera: bpy.types.Object, point: tuple[float, float, float]) -> None:
    direction = Vector(point) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def camera(name: str, location: tuple[float, float, float], target: tuple[float, float, float], scale: float, destination: bpy.types.Collection) -> bpy.types.Object:
    cam_data = bpy.data.cameras.new(name)
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = scale
    cam_data.clip_end = 500000
    cam = bpy.data.objects.new(name, cam_data)
    destination.objects.link(cam)
    cam.location = location
    look_at(cam, target)
    return cam


def setup_light() -> None:
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.32, 0.28, 0.25, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
    sun_data = bpy.data.lights.new("StudyLowSun", type="SUN")
    sun_data.energy = 2
    sun_data.color = (1.0, 0.71, 0.48)
    sun_data.angle = math.radians(4)
    sun = bpy.data.objects.new("StudyLowSun", sun_data)
    bpy.context.scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(65), math.radians(-15), math.radians(-35))


def render(camera_object: bpy.types.Object, output: Path, resolution: tuple[int, int]) -> None:
    scene = bpy.context.scene
    scene.camera = camera_object
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)


def main() -> None:
    args = arguments()
    manifest = json.loads(args.manifest.read_text())
    overview = manifest["regions"]["overview"]
    hero = manifest["regions"]["hero"]
    bounds = overview["bounds_m"]
    origin = ((bounds[0] + bounds[2]) / 2, (bounds[1] + bounds[3]) / 2)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1
    source_collection = collection("SourceTerrain_Overview_200m")
    hero_collection = collection("HeroTerrain_Mosaic_10m")
    collection("RockLibrary")
    collection("ScatterPreview")
    collection("Landmarks")
    cameras = collection("ReviewCameras")
    overview_objects, overview_tris = build_terrain("Overview", overview, origin, source_collection, 160)
    hero_objects, hero_tris = build_terrain("MosaicHero", hero, origin, hero_collection, 200, z_offset=0.35)
    setup_light()
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 8
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "AgX"
    scene.render.film_transparent = False
    scene["source_manifest"] = str(args.manifest)
    scene["source_name"] = manifest["name"]
    scene["source_crs"] = manifest["working_crs"]
    scene["source_origin_m"] = json.dumps(origin)
    scene["overview_triangles"] = overview_tris
    scene["hero_triangles"] = hero_tris
    overview_cam = camera("Overview_Map", (0, -2000, 240000), (0, 0, 700), 230000, cameras)
    hero_cx = (hero["bounds_m"][0] + hero["bounds_m"][2]) / 2 - origin[0]
    hero_cy = (hero["bounds_m"][1] + hero["bounds_m"][3]) / 2 - origin[1]
    hero_cam = camera("Mosaic_Elevated", (hero_cx - 1100, hero_cy - 1600, 1800), (hero_cx, hero_cy, 450), 3200, cameras)
    scene.camera = overview_cam
    args.output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(args.output))
    if args.render_dir:
        args.render_dir.mkdir(parents=True, exist_ok=True)
        hero_collection.hide_render = True
        render(overview_cam, args.render_dir / "overview_map.png", (1200, 1400))
        hero_collection.hide_render = False
        source_collection.hide_render = True
        render(hero_cam, args.render_dir / "mosaic_elevated.png", (1200, 900))
        source_collection.hide_render = False
    print(json.dumps({"overview_objects": overview_objects, "overview_triangles": overview_tris, "hero_objects": hero_objects, "hero_triangles": hero_tris, "source_origin_m": origin}))


if __name__ == "__main__":
    main()
