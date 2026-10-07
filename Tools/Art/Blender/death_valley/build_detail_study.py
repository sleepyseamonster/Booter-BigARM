"""Build a 1 m terrain patch, fractured rock kit, and Geometry Nodes placement study."""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_region_study import camera, collection, configure_review_viewport, geology_material, mesh_tile, render, setup_light


ROCKS = (
    ("StratifiedSlab", 8, 4.0, 2.6, 0.7, 8, 140, (0.55, 0.49, 0.42, 1)),
    ("FracturedCliffBlock", 7, 3.2, 2.5, 2.7, 10, 100, (0.43, 0.37, 0.34, 1)),
    ("AngularDarkBoulder", 7, 1.7, 1.5, 1.4, 7, 240, (0.26, 0.23, 0.24, 1)),
    ("TalusShard", 5, 0.9, 0.6, 0.45, 5, 450, (0.51, 0.42, 0.35, 1)),
    ("Pebble", 5, 0.22, 0.16, 0.12, 4, 700, (0.44, 0.38, 0.34, 1)),
)


def args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--detail-manifest", type=Path, required=True)
    parser.add_argument("--regional-manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--render-dir", type=Path)
    parser.add_argument("--export-dir", type=Path)
    parser.add_argument("--scan-manifest", type=Path)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def rock_material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = color
    principled.inputs["Roughness"].default_value = 0.92
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    geometry = nodes.new("ShaderNodeNewGeometry")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 3.0
    noise.inputs["Detail"].default_value = 4
    links.new(geometry.outputs["Position"], noise.inputs["Vector"])
    wave = nodes.new("ShaderNodeTexWave")
    wave.wave_type = "BANDS"
    wave.bands_direction = "Z"
    wave.inputs["Scale"].default_value = 11
    wave.inputs["Distortion"].default_value = 1.5
    links.new(geometry.outputs["Position"], wave.inputs["Vector"])
    mix = nodes.new("ShaderNodeMixRGB")
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 0.22
    mix.inputs[1].default_value = color
    links.new(wave.outputs["Color"], mix.inputs[2])
    links.new(mix.outputs["Color"], principled.inputs["Base Color"])
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.38
    bump.inputs["Distance"].default_value = 0.07
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])
    return material


def rock_mesh(name: str, sides: int, sx: float, sy: float, height: float, seed: int, destination: bpy.types.Collection, material: bpy.types.Material) -> bpy.types.Object:
    rng = np.random.default_rng(seed)
    angles = np.linspace(0, math.tau, sides, endpoint=False) + rng.uniform(-0.07, 0.07, sides)
    radius = rng.uniform(0.82, 1.08, sides)
    slab = name == "StratifiedSlab"
    # Slabs keep broad strata; other families break their crown into angular facets.
    rings = ((0.0, 1.0), (0.28, 0.96), (0.56, 0.91), (0.76, 0.88), (1.0, 0.76)) if slab else (
        (0.0, 0.88), (0.22, 1.04), (0.58, 0.92), (0.82, 0.62))
    vertices = []
    for ring_index, (ring_z, taper) in enumerate(rings):
        for index, angle in enumerate(angles):
            skew = (ring_z - 0.45) * 0.13
            breakup = 1 if slab else rng.uniform(0.77, 1.19)
            height_jitter = 0 if slab or ring_index == 0 else rng.uniform(-0.10, 0.10) * height
            vertices.append((math.cos(angle) * radius[index] * sx * 0.5 * taper * breakup + skew,
                             math.sin(angle) * radius[index] * sy * 0.5 * taper * breakup,
                             ring_z * height - 0.12 * height + height_jitter))
    faces = [tuple(reversed(range(sides)))]
    for ring in range(len(rings) - 1):
        for index in range(sides):
            a = ring * sides + index
            b = ring * sides + (index + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    if slab:
        faces.append(tuple((len(rings) - 1) * sides + index for index in range(sides)))
    else:
        apex = len(vertices)
        vertices.append((rng.uniform(-0.14, 0.14) * sx, rng.uniform(-0.14, 0.14) * sy, height * 0.92))
        for index in range(sides):
            a = (len(rings) - 1) * sides + index
            b = (len(rings) - 1) * sides + (index + 1) % sides
            faces.append((a, b, apex))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="RockTriplanarApproxUV")
    for polygon in mesh.polygons:
        cap = abs(polygon.normal.z) > 0.65
        for loop_id in polygon.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_id].vertex_index].co
            if cap:
                uv.data[loop_id].uv = (vertex.x / max(sx, 0.01) + 0.5, vertex.y / max(sy, 0.01) + 0.5)
            else:
                u = math.atan2(vertex.y / max(sy, 0.01), vertex.x / max(sx, 0.01)) / math.tau + 0.5
                uv.data[loop_id].uv = (u, vertex.z / max(height, 0.01))
    obj = bpy.data.objects.new(name, mesh)
    destination.objects.link(obj)
    obj.data.materials.append(material)
    obj.location = (10000 + seed * 12, 0, 0)
    obj["asset_role"] = "procedural rock prototype; Geometry Nodes source"
    obj["dimension_m"] = json.dumps([sx, sy, height])
    obj["evaluated_triangles"] = sum(max(len(face.vertices) - 2, 0) for face in mesh.polygons)
    return obj


def instance_group(name: str, prototype: bpy.types.Object, seed: int) -> bpy.types.NodeTree:
    group = bpy.data.node_groups.new(name, "GeometryNodeTree")
    group.interface.new_socket(name="Geometry", in_out="INPUT", socket_type="NodeSocketGeometry")
    group.interface.new_socket(name="Geometry", in_out="OUTPUT", socket_type="NodeSocketGeometry")
    input_node = group.nodes.new("NodeGroupInput")
    output_node = group.nodes.new("NodeGroupOutput")
    instance = group.nodes.new("GeometryNodeInstanceOnPoints")
    source = group.nodes.new("GeometryNodeObjectInfo")
    source.inputs["Object"].default_value = prototype
    source.inputs["As Instance"].default_value = True
    source.transform_space = "ORIGINAL"
    size = group.nodes.new("FunctionNodeRandomValue")
    size.data_type = "FLOAT"
    size.inputs["Min"].default_value = 0.58
    size.inputs["Max"].default_value = 1.48
    size.inputs["Seed"].default_value = seed
    turn = group.nodes.new("FunctionNodeRandomValue")
    turn.data_type = "FLOAT_VECTOR"
    turn.inputs["Min"].default_value = (0, 0, 0)
    turn.inputs["Max"].default_value = (0, 0, math.tau)
    turn.inputs["Seed"].default_value = seed + 19
    group.links.new(input_node.outputs["Geometry"], instance.inputs["Points"])
    group.links.new(source.outputs["Geometry"], instance.inputs["Instance"])
    group.links.new(size.outputs["Value"], instance.inputs["Scale"])
    group.links.new(turn.outputs["Value"], instance.inputs["Rotation"])
    group.links.new(instance.outputs["Instances"], output_node.inputs["Geometry"])
    return group


def scatter(name: str, prototype: bpy.types.Object, count: int, seed: int, elevation: np.ndarray, slope: np.ndarray, geology: np.ndarray, wash: np.ndarray, talus: np.ndarray, bounds: list[float], destination: bpy.types.Collection) -> bpy.types.Object:
    rng = np.random.default_rng(seed)
    xmin, ymin, xmax, ymax = bounds
    points = []
    attempts = 0
    while len(points) < count and attempts < count * 35:
        attempts += 1
        x = rng.uniform(xmin + 3, xmax - 3)
        y = rng.uniform(ymin + 3, ymax - 3)
        row = int(round(ymax - y))
        col = int(round(x - xmin))
        gradient = float(slope[row, col])
        if gradient > (32 if "Cliff" in name else 24) or ("Cliff" in name and gradient < 9):
            continue
        unit = int(geology[row, col])
        if "DarkBoulder" in name and unit != 10:
            continue
        if "Stratified" in name and unit not in (8, 10):
            continue
        if "Stratified" in name and unit == 10 and rng.random() > 0.35:
            continue
        if "Pebble" in name and unit == 8 and rng.random() > 0.5:
            continue
        if "Cliff" in name and float(talus[row, col]) < 0.12:
            continue
        if "Talus" in name and float(talus[row, col]) < 0.08:
            continue
        if "Stratified" in name and float(wash[row, col]) > 0.55:
            continue
        points.append((x - xmin, y - ymin, float(elevation[row, col]) - 0.1))
    mesh = bpy.data.meshes.new(name + "_Points")
    mesh.from_pydata(points, [], [])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    destination.objects.link(obj)
    modifier = obj.modifiers.new("DeterministicInstances", "NODES")
    modifier.node_group = instance_group(name + "_GN", prototype, seed)
    obj["seed"] = seed
    obj["slope_rule"] = "cliff 9-32 degrees; other families 0-24 degrees"
    obj["geology_rule"] = "dark boulder on basement; slab on sedimentary or sparse basement; pebbles reduced on sedimentary"
    obj["source_grid"] = "Mosaic 1 m detail patch"
    obj["point_count"] = len(points)
    return obj


def main() -> None:
    arguments = args()
    detail = json.loads(arguments.detail_manifest.read_text())
    regional = json.loads(arguments.regional_manifest.read_text())
    bounds = detail["bounds_m"]
    with np.load(detail["prepared_npz"]) as arrays:
        elevation = arrays["elevation"]
        slope = arrays["slope"]
        wash = arrays["wash"]
        talus = arrays["talus"]
    hero = regional["regions"]["hero"]
    with np.load(Path(hero["prepared_npz"]).parent / "geology.npz") as arrays:
        hero_geology = arrays["unit_code"]
    rows = np.clip(np.rint((hero["bounds_m"][3] - np.arange(bounds[3], bounds[1] - 1, -1)) / 10).astype(int), 0, hero_geology.shape[0] - 1)
    cols = np.clip(np.rint((np.arange(bounds[0], bounds[2] + 1) - hero["bounds_m"][0]) / 10).astype(int), 0, hero_geology.shape[1] - 1)
    geology = hero_geology[np.ix_(rows, cols)]
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = "METRIC"
    terrain_collection = collection("SourceTerrain_1m_Immutable")
    library = collection("RockLibrary_Procedural")
    scatter_collection = collection("ScatterPreview_GeometryNodes")
    cameras = collection("ReviewCameras")
    terrain_material = geology_material("GeologyPalette_MicroStudy", str(arguments.scan_manifest) if arguments.scan_manifest else None)
    terrain_tiles = []
    for row in range(0, 500, 250):
        for col in range(0, 500, 250):
            terrain_tiles.append(mesh_tile(f"Detail_r{row}_c{col}", elevation, row, row + 250, col, col + 250,
                                           bounds, 1, (bounds[0], bounds[1]), terrain_collection,
                                           terrain_material, geology))
    rocks = []
    scatters = []
    for index, (name, sides, sx, sy, height, seed, count, color) in enumerate(ROCKS):
        prototype = rock_mesh(name, sides, sx, sy, height, seed, library, rock_material(name + "_Mat", color))
        rocks.append(prototype)
        scatters.append(scatter(name + "_Scatter", prototype, count, 4000 + index, elevation, slope, geology, wash, talus, bounds, scatter_collection))
    setup_light()
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 16
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "AgX"
    scene["source_manifest"] = str(arguments.detail_manifest)
    scene["working_crs"] = "EPSG:26911"
    scene["absolute_origin_m"] = json.dumps([bounds[0], bounds[1]])
    scene["terrain_triangles"] = 500000
    scene["rock_instance_count"] = sum(int(obj["point_count"]) for obj in scatters)
    if arguments.scan_manifest:
        scene["scanned_ground_manifest"] = str(arguments.scan_manifest)
    wide = camera("Detail_Elevated", (180, -260, 880), (245, 245, 350), 620, cameras)
    ground_height = float(elevation[500 - 90, 250])
    ground = camera("Detail_Ground", (250, 90, ground_height + 3), (250, 340, float(elevation[500 - 340, 250]) + 8), 310, cameras)
    ground.data.type = "PERSP"
    ground.data.lens = 32
    scene.camera = wide
    scene.timeline_markers.new("01 Detail elevated", frame=1).camera = wide
    scene.timeline_markers.new("02 Detail ground", frame=2).camera = ground
    configure_review_viewport()
    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    if arguments.scan_manifest:
        bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(arguments.output))
    if arguments.export_dir:
        arguments.export_dir.mkdir(parents=True, exist_ok=True)
        for rock in rocks:
            bpy.ops.object.select_all(action="DESELECT")
            rock.select_set(True)
            bpy.context.view_layer.objects.active = rock
            previous_location = rock.location.copy()
            rock.location = (0, 0, 0)
            bpy.ops.export_scene.fbx(filepath=str(arguments.export_dir / (rock.name + ".fbx")), use_selection=True,
                                     object_types={"MESH"}, apply_unit_scale=True, global_scale=1,
                                     add_leaf_bones=False, path_mode="AUTO")
            rock.location = previous_location
    if arguments.render_dir:
        arguments.render_dir.mkdir(parents=True, exist_ok=True)
        render(wide, arguments.render_dir / "mosaic_1m_detail_elevated.png", (1200, 900))
        render(ground, arguments.render_dir / "mosaic_1m_detail_ground.png", (1200, 900))
    print(json.dumps({"terrain_tiles": len(terrain_tiles), "terrain_triangles": 500000,
                      "rock_meshes": {obj.name: int(obj["evaluated_triangles"]) for obj in rocks},
                      "scatter_instances": {obj.name: int(obj["point_count"]) for obj in scatters}}))


if __name__ == "__main__":
    main()
