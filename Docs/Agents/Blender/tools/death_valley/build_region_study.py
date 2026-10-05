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
    parser.add_argument("--detail-manifest", type=Path)
    parser.add_argument("--scan-manifest", type=Path)
    parser.add_argument("--overview-color", type=Path,
                        help="Optional geographic color image on the same overview grid")
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def collection(name: str) -> bpy.types.Collection:
    result = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(result)
    return result


def material(name: str, image_path: str, scan_manifest: Path | None = None,
             regional_image: str | None = None, regional_bounds: list[float] | None = None,
             origin: tuple[float, float] | None = None) -> bpy.types.Material:
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    result.diffuse_color = (0.48, 0.42, 0.35, 1)
    nodes = result.node_tree.nodes
    principled = nodes.get("Principled BSDF")
    principled.inputs["Roughness"].default_value = 0.9
    image = nodes.new("ShaderNodeTexImage")
    image.label = "Geographic color reference, not a close-range game texture"
    image.image = bpy.data.images.load(image_path, check_existing=True)
    image.interpolation = "Linear"
    image.extension = "EXTEND"
    links = result.node_tree.links
    macro_color = image.outputs["Color"]
    if regional_image and regional_bounds and origin:
        geometry = nodes.new("ShaderNodeNewGeometry")
        relative = nodes.new("ShaderNodeVectorMath")
        relative.operation = "SUBTRACT"
        relative.inputs[1].default_value = (regional_bounds[0] - origin[0], regional_bounds[1] - origin[1], 0)
        links.new(geometry.outputs["Position"], relative.inputs[0])
        normalized = nodes.new("ShaderNodeVectorMath")
        normalized.operation = "DIVIDE"
        normalized.inputs[1].default_value = (regional_bounds[2] - regional_bounds[0],
                                              regional_bounds[3] - regional_bounds[1], 1)
        links.new(relative.outputs["Vector"], normalized.inputs[0])
        regional = nodes.new("ShaderNodeTexImage")
        regional.image = bpy.data.images.load(regional_image, check_existing=True)
        regional.extension = "EXTEND"
        links.new(normalized.outputs["Vector"], regional.inputs["Vector"])
        weight = nodes.new("ShaderNodeVertexColor")
        weight.layer_name = "FineTerrainWeight"
        blend = nodes.new("ShaderNodeMixRGB")
        blend.blend_type = "MIX"
        links.new(weight.outputs["Color"], blend.inputs[0])
        links.new(regional.outputs["Color"], blend.inputs[1])
        links.new(image.outputs["Color"], blend.inputs[2])
        macro_color = blend.outputs["Color"]
    links.new(macro_color, principled.inputs["Base Color"])
    if scan_manifest:
        scan = json.loads(scan_manifest.read_text())
        maps = scan["maps"]
        position = nodes.new("ShaderNodeNewGeometry")
        scale = nodes.new("ShaderNodeVectorMath")
        scale.operation = "SCALE"
        scale.inputs["Scale"].default_value = 0.44
        links.new(position.outputs["Position"], scale.inputs["Vector"])

        def scan_image(key: str, non_color: bool = False) -> bpy.types.ShaderNodeTexImage:
            node = nodes.new("ShaderNodeTexImage")
            node.label = f"Poly Haven {scan['asset_id']} {key}, CC0"
            path = Path(maps[key]["path"])
            if not path.is_absolute():
                path = scan_manifest.parent / path
            node.image = bpy.data.images.load(str(path), check_existing=True)
            if non_color:
                node.image.colorspace_settings.name = "Non-Color"
            node.extension = "REPEAT"
            links.new(scale.outputs["Vector"], node.inputs["Vector"])
            return node

        scanned_color = scan_image("Diffuse")
        color_mix = nodes.new("ShaderNodeMixRGB")
        color_mix.blend_type = "MULTIPLY"
        color_mix.inputs[0].default_value = 0.16
        links.new(macro_color, color_mix.inputs[1])
        links.new(scanned_color.outputs["Color"], color_mix.inputs[2])
        links.new(color_mix.outputs["Color"], principled.inputs["Base Color"])
        rough = scan_image("Rough", True)
        links.new(rough.outputs["Color"], principled.inputs["Roughness"])
        normal_tex = scan_image("nor_gl", True)
        normal = nodes.new("ShaderNodeNormalMap")
        normal.inputs["Strength"].default_value = 0.55
        links.new(normal_tex.outputs["Color"], normal.inputs["Color"])
        links.new(normal.outputs["Normal"], principled.inputs["Normal"])
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


def geology_material(name: str, scan_manifest: str | None = None) -> bpy.types.Material:
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
    if scan_manifest:
        scan = json.loads(Path(scan_manifest).read_text())
        maps = scan["maps"]
        scale = nodes.new("ShaderNodeVectorMath")
        scale.operation = "SCALE"
        scale.inputs["Scale"].default_value = 0.44  # about one 2.3 m scan tile
        links.new(geometry.outputs["Position"], scale.inputs["Vector"])

        def scanned_image(key: str, non_color: bool = False) -> bpy.types.ShaderNodeTexImage:
            node = nodes.new("ShaderNodeTexImage")
            node.label = f"Poly Haven {scan['asset_id']} {key} CC0"
            map_path = Path(maps[key]["path"])
            if not map_path.is_absolute():
                map_path = Path(scan_manifest).parent / map_path
            node.image = bpy.data.images.load(str(map_path), check_existing=True)
            if non_color:
                node.image.colorspace_settings.name = "Non-Color"
            node.extension = "REPEAT"
            links.new(scale.outputs["Vector"], node.inputs["Vector"])
            return node

        diffuse = scanned_image("Diffuse")
        blend = nodes.new("ShaderNodeMixRGB")
        blend.blend_type = "MIX"
        blend.inputs[0].default_value = 0.62
        links.new(mix.outputs["Color"], blend.inputs[1])
        links.new(diffuse.outputs["Color"], blend.inputs[2])
        links.new(blend.outputs["Color"], principled.inputs["Base Color"])
        rough = scanned_image("Rough", True)
        links.new(rough.outputs["Color"], principled.inputs["Roughness"])
        normal_texture = scanned_image("nor_gl", True)
        normal = nodes.new("ShaderNodeNormalMap")
        normal.inputs["Strength"].default_value = 0.65
        links.new(normal_texture.outputs["Color"], normal.inputs["Color"])
        links.new(normal.outputs["Normal"], bump.inputs["Normal"])
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
    texture_bounds: list[float] | None = None,
    exclude_bounds: list[float] | None = None,
    blend_weights: np.ndarray | None = None,
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
            if exclude_bounds:
                cell_x = xmin + (start_col + c + 0.5) * spacing
                cell_y = ymax - (start_row + r + 0.5) * spacing
                if exclude_bounds[0] <= cell_x <= exclude_bounds[2] and exclude_bounds[1] <= cell_y <= exclude_bounds[3]:
                    continue
            a = r * cols + c
            faces.append((a, a + cols, a + cols + 1, a + 1))
    used = sorted({index for face in faces for index in face})
    remap = {old: new for new, old in enumerate(used)}
    vertices = [vertices[index] for index in used]
    faces = [tuple(remap[index] for index in face) for face in faces]
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="SourceGridUV")
    colors = mesh.vertex_colors.new(name="GeologyPalette") if geology is not None else None
    blend_colors = mesh.vertex_colors.new(name="FineTerrainWeight") if blend_weights is not None else None
    tex_xmin, tex_ymin, tex_xmax, tex_ymax = texture_bounds or bounds
    width = tex_xmax - tex_xmin
    height = tex_ymax - tex_ymin
    for poly in mesh.polygons:
        for loop_index in poly.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            absolute_x = vertex.x + origin[0]
            absolute_y = vertex.y + origin[1]
            uv.data[loop_index].uv = ((absolute_x - tex_xmin) / width, (absolute_y - tex_ymin) / height)
            if colors is not None:
                local_index = used[mesh.loops[loop_index].vertex_index]
                source_row = start_row + local_index // cols
                source_col = start_col + local_index % cols
                colors.data[loop_index].color = GEOLOGY_COLORS.get(int(geology[source_row, source_col]), GEOLOGY_COLORS[0])
            if blend_colors is not None:
                local_index = used[mesh.loops[loop_index].vertex_index]
                source_row = start_row + local_index // cols
                source_col = start_col + local_index % cols
                weight = float(blend_weights[source_row, source_col])
                blend_colors.data[loop_index].color = (weight, weight, weight, 1)
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
    scan_manifest: Path | None = None,
    exclude_bounds: list[float] | None = None,
    elevation_override: np.ndarray | None = None,
    geology_override: np.ndarray | None = None,
    bounds_override: list[float] | None = None,
    texture_bounds: list[float] | None = None,
    blend_weights: np.ndarray | None = None,
    regional_image: str | None = None,
    regional_bounds: list[float] | None = None,
) -> tuple[int, int]:
    if elevation_override is None:
        with np.load(record["prepared_npz"]) as arrays:
            elevation = arrays["elevation"]
    else:
        elevation = elevation_override
    if geology_override is None:
        geology_path = Path(record["prepared_npz"]).parent / "geology.npz"
        with np.load(geology_path) as arrays:
            geology = arrays["unit_code"]
    else:
        geology = geology_override
    bounds = bounds_override or record["bounds_m"]
    spacing = float(record["resolution_m"])
    expected = (round((bounds[3] - bounds[1]) / spacing) + 1, round((bounds[2] - bounds[0]) / spacing) + 1)
    if elevation.shape != expected or not np.isfinite(elevation).all():
        raise ValueError(f"Invalid grid for {name}: got {elevation.shape}, expected {expected}")
    image_material = material(f"{name}_GeographicSurface", record["color_reference_png"], scan_manifest,
                              regional_image, regional_bounds, origin)
    authored_material = geology_material(f"{name}_GeologyPalette_Authored")
    objects = 0
    triangles = 0
    for row in range(0, elevation.shape[0] - 1, step):
        end_row = min(row + step, elevation.shape[0] - 1)
        for col in range(0, elevation.shape[1] - 1, step):
            end_col = min(col + step, elevation.shape[1] - 1)
            tile = mesh_tile(
                f"{name}_r{row:04d}_c{col:04d}", elevation,
                row, end_row, col, end_col, bounds, spacing, origin,
                destination, image_material, geology, z_offset,
                texture_bounds=texture_bounds, exclude_bounds=exclude_bounds,
                blend_weights=blend_weights,
            )
            tile.data.materials.append(authored_material)
            tile["material_slot_0"] = "geographic color; close surface from CC0 scan when present"
            tile["material_slot_1"] = "authored geology palette alternate"
            objects += 1
            triangles += 2 * len(tile.data.polygons)
    return objects, triangles


def sample_coarse_grid(coarse: np.ndarray, coarse_bounds: list[float], spacing: float,
                       xx: np.ndarray, yy: np.ndarray) -> np.ndarray:
    col = np.clip((xx - coarse_bounds[0]) / spacing, 0, coarse.shape[1] - 1)
    row = np.clip((coarse_bounds[3] - yy) / spacing, 0, coarse.shape[0] - 1)
    c0 = np.minimum(np.floor(col).astype(int), coarse.shape[1] - 2)
    r0 = np.minimum(np.floor(row).astype(int), coarse.shape[0] - 2)
    dx = col - c0
    dy = row - r0
    return ((1 - dy) * ((1 - dx) * coarse[r0, c0] + dx * coarse[r0, c0 + 1])
            + dy * ((1 - dx) * coarse[r0 + 1, c0] + dx * coarse[r0 + 1, c0 + 1]))


def stitched_hero(overview: dict, hero: dict) -> tuple[np.ndarray, np.ndarray, list[float], np.ndarray]:
    coarse_bounds = overview["bounds_m"]
    hero_bounds = hero["bounds_m"]
    coarse_step = float(overview["resolution_m"])
    fine_step = float(hero["resolution_m"])
    outer = [
        coarse_bounds[0] + math.floor((hero_bounds[0] - coarse_bounds[0]) / coarse_step) * coarse_step,
        coarse_bounds[1] + math.floor((hero_bounds[1] - coarse_bounds[1]) / coarse_step) * coarse_step,
        coarse_bounds[0] + math.ceil((hero_bounds[2] - coarse_bounds[0]) / coarse_step) * coarse_step,
        coarse_bounds[1] + math.ceil((hero_bounds[3] - coarse_bounds[1]) / coarse_step) * coarse_step,
    ]
    with np.load(overview["prepared_npz"]) as arrays:
        coarse = arrays["elevation"]
    with np.load(hero["prepared_npz"]) as arrays:
        fine = arrays["elevation"]
    with np.load(Path(hero["prepared_npz"]).parent / "geology.npz") as arrays:
        units = arrays["unit_code"]
    xs = np.arange(round((outer[2] - outer[0]) / fine_step) + 1) * fine_step + outer[0]
    ys = outer[3] - np.arange(round((outer[3] - outer[1]) / fine_step) + 1) * fine_step
    xx, yy = np.meshgrid(xs, ys)
    fine_col = np.clip(np.rint((xx - hero_bounds[0]) / fine_step).astype(int), 0, fine.shape[1] - 1)
    fine_row = np.clip(np.rint((hero_bounds[3] - yy) / fine_step).astype(int), 0, fine.shape[0] - 1)
    coarse_height = sample_coarse_grid(coarse, coarse_bounds, coarse_step, xx, yy)
    edge_distance = np.minimum.reduce((xx - outer[0], outer[2] - xx, yy - outer[1], outer[3] - yy))
    fine_weight = np.clip(edge_distance / 100.0, 0, 1)
    stitched = coarse_height * (1 - fine_weight) + fine[fine_row, fine_col] * fine_weight
    return stitched.astype("float32"), units[fine_row, fine_col], outer, fine_weight.astype("float32")


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
    for marker in scene.timeline_markers:
        if marker.camera == camera_object:
            scene.frame_set(marker.frame)
            break
    scene.camera = camera_object
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)


def configure_review_viewport() -> None:
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                space = area.spaces.active
                space.region_3d.view_perspective = "CAMERA"
                space.shading.type = "MATERIAL"
                space.overlay.show_overlays = False


def main() -> None:
    args = arguments()
    manifest = json.loads(args.manifest.read_text())
    overview = manifest["regions"]["overview"]
    if args.overview_color:
        overview = dict(overview)
        overview["color_reference_png"] = str(args.overview_color)
    hero = manifest["regions"]["hero"]
    bounds = overview["bounds_m"]
    detail = json.loads(args.detail_manifest.read_text()) if args.detail_manifest else None
    detail_bounds = detail["bounds_m"] if detail else None
    origin = ((bounds[0] + bounds[2]) / 2, (bounds[1] + bounds[3]) / 2)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1
    source_collection = collection("SourceTerrain_Overview_200m")
    hero_collection = collection("HeroTerrain_Mosaic_10m")
    detail_collection = collection("Mosaic_1m_TerrainOnly")
    collection("Landmarks")
    cameras = collection("ReviewCameras")
    hero_elevation, hero_geology, hero_outer_bounds, hero_weight = stitched_hero(overview, hero)
    overview_objects, overview_tris = build_terrain("Overview", overview, origin, source_collection, 160,
                                                    exclude_bounds=hero_outer_bounds)
    detail_hole = detail_bounds
    hero_objects, hero_tris = build_terrain("MosaicHero", hero, origin, hero_collection, 200, z_offset=0.35,
                                            scan_manifest=args.scan_manifest, exclude_bounds=detail_hole,
                                            elevation_override=hero_elevation, geology_override=hero_geology,
                                            bounds_override=hero_outer_bounds, texture_bounds=hero["bounds_m"],
                                            blend_weights=hero_weight,
                                            regional_image=overview["color_reference_png"], regional_bounds=bounds)
    detail_tris = 0
    if detail:
        with np.load(detail["prepared_npz"]) as arrays:
            elevation = arrays["elevation"]
        if elevation.shape != (501, 501) or not np.isfinite(elevation).all():
            raise ValueError("The Mosaic 1 m patch must be a finite 501 x 501 grid")
        detail_material = material("Mosaic_1m_GeographicSurface", hero["color_reference_png"], args.scan_manifest)
        for row in range(0, 500, 250):
            for col in range(0, 500, 250):
                mesh_tile(f"Mosaic1m_r{row}_c{col}", elevation, row, row + 250, col, col + 250,
                          detail_bounds, 1, origin, detail_collection, detail_material,
                          z_offset=0.8, texture_bounds=hero["bounds_m"])
        detail_tris = 500000
    setup_light()
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 8
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1400
    scene.render.resolution_percentage = 100
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "AgX"
    scene.render.film_transparent = False
    scene["source_manifest"] = str(args.manifest)
    scene["source_name"] = manifest["name"]
    scene["source_crs"] = manifest["working_crs"]
    scene["source_origin_m"] = json.dumps(origin)
    scene["overview_triangles"] = overview_tris
    scene["hero_triangles"] = hero_tris
    scene["detail_triangles"] = detail_tris
    scene["surface_note"] = "Natural-color Landsat regional reference; NAIP Mosaic color; CC0 scanned normal/roughness nearby. No rock scatter."
    overview_cam = camera("Overview_Map", (0, -2000, 240000), (0, 0, 700), 230000, cameras)
    hero_cx = (hero["bounds_m"][0] + hero["bounds_m"][2]) / 2 - origin[0]
    hero_cy = (hero["bounds_m"][1] + hero["bounds_m"][3]) / 2 - origin[1]
    hero_cam = camera("Mosaic_Elevated", (hero_cx - 1100, hero_cy - 1600, 1800), (hero_cx, hero_cy, 450), 3200, cameras)
    scene.camera = overview_cam
    scene.timeline_markers.new("01 Regional map", frame=1).camera = overview_cam
    scene.timeline_markers.new("02 Mosaic elevated", frame=2).camera = hero_cam
    if detail_bounds:
        detail_cx = (detail_bounds[0] + detail_bounds[2]) / 2 - origin[0]
        detail_cy = (detail_bounds[1] + detail_bounds[3]) / 2 - origin[1]
        close_cam = camera("Mosaic_1m_Elevated", (detail_cx - 150, detail_cy - 250, 850),
                           (detail_cx, detail_cy, 350), 760, cameras)
        scene.timeline_markers.new("03 Mosaic 1m", frame=3).camera = close_cam
    configure_review_viewport()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(args.output))
    if args.render_dir:
        args.render_dir.mkdir(parents=True, exist_ok=True)
        render(overview_cam, args.render_dir / "overview_map.png", (1200, 1400))
        render(hero_cam, args.render_dir / "mosaic_elevated.png", (1200, 900))
        if detail_bounds:
            render(close_cam, args.render_dir / "mosaic_1m_elevated.png", (1200, 900))
    print(json.dumps({"overview_objects": overview_objects, "overview_triangles": overview_tris, "hero_objects": hero_objects, "hero_triangles": hero_tris, "detail_triangles": detail_tris, "source_origin_m": origin}))


if __name__ == "__main__":
    main()
