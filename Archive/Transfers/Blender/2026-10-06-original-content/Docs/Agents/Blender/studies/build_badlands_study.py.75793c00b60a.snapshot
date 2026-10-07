"""Build a provisional Broken World badlands scene; run with Blender --background --python."""

from math import cos, pi, sin
from pathlib import Path
import random

import bpy
from mathutils import Vector


STUDY_DIR = Path(__file__).resolve().parent
REPO = STUDY_DIR.parents[3]
GROUND = REPO / "Assets/_Project/Art/Environment/Ground/SandDirt"
ROCKS = REPO / "Assets/_Project/Art/Environment/Rocks"
RNG = random.Random(230930)


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def image_node(nodes, links, path, vector, scale):
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = bpy.data.images.load(str(path), check_existing=True)
    texture.extension = "REPEAT"
    mapping = nodes.new("ShaderNodeVectorMath")
    mapping.operation = "SCALE"
    mapping.inputs[3].default_value = scale
    links.new(vector, mapping.inputs[0])
    links.new(mapping.outputs[0], texture.inputs[0])
    return texture.outputs[0]


def material(name, albedo_path, scale, roughness=0.92):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value = roughness
    geometry = nodes.new("ShaderNodeNewGeometry")
    color = image_node(nodes, links, albedo_path, geometry.outputs["Position"], scale)
    links.new(color, bsdf.inputs["Base Color"])
    return mat


def ground_material():
    mat = bpy.data.materials.new("StudyGround_SandGravelRock")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value = 0.95
    geometry = nodes.new("ShaderNodeNewGeometry")
    position = geometry.outputs["Position"]
    sand = image_node(nodes, links, GROUND / "BrokenWorldSandDirtAlbedo.png", position, 0.18)
    gravel = image_node(nodes, links, GROUND / "BrokenWorldGravelAlbedo.png", position, 0.22)
    rocky = image_node(nodes, links, GROUND / "BrokenWorldMixedRockyAlbedo.png", position, 0.16)
    swept = image_node(nodes, links, GROUND / "BrokenWorldSweptSandAlbedo.png", position, 0.19)

    attribute = nodes.new("ShaderNodeAttribute")
    attribute.attribute_name = "GroundMask"
    split = nodes.new("ShaderNodeSeparateColor")
    links.new(attribute.outputs["Color"], split.inputs[0])
    mix_gravel = nodes.new("ShaderNodeMixRGB")
    mix_gravel.blend_type = "MIX"
    links.new(sand, mix_gravel.inputs[1])
    links.new(gravel, mix_gravel.inputs[2])
    links.new(split.outputs["Red"], mix_gravel.inputs[0])
    mix_rock = nodes.new("ShaderNodeMixRGB")
    mix_rock.blend_type = "MIX"
    links.new(mix_gravel.outputs[0], mix_rock.inputs[1])
    links.new(rocky, mix_rock.inputs[2])
    links.new(split.outputs["Green"], mix_rock.inputs[0])
    mix_swept = nodes.new("ShaderNodeMixRGB")
    mix_swept.blend_type = "MIX"
    links.new(mix_rock.outputs[0], mix_swept.inputs[1])
    links.new(swept, mix_swept.inputs[2])
    links.new(split.outputs["Blue"], mix_swept.inputs[0])
    links.new(mix_swept.outputs[0], bsdf.inputs["Base Color"])
    return mat


def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)


def wash_center(y):
    return 3.8 * sin(y / 16.0) + 1.2 * sin(y / 5.5)


def terrain_height(x, y):
    distance = abs(x - wash_center(y))
    low_rise = 0.48 * smoothstep(9.0, 18.0, distance)
    broad = 0.42 * sin(x * 0.18 + y * 0.11) * sin(y * 0.16)
    broken = 0.20 * sin(x * 0.68 + y * 0.4) * sin(y * 0.52)
    fine = 0.08 * sin(x * 1.43 + y * 0.71)
    wash = -0.42 * (1.0 - smoothstep(1.8, 8.0, distance))
    return low_rise + broad + broken + fine + wash


def terrain():
    count = 120
    extent = 45.0
    vertices, faces = [], []
    for iy in range(count + 1):
        y = -extent + 2 * extent * iy / count
        for ix in range(count + 1):
            x = -extent + 2 * extent * ix / count
            vertices.append((x, y, terrain_height(x, y)))
    for iy in range(count):
        for ix in range(count):
            index = iy * (count + 1) + ix
            faces.append((index, index + 1, index + count + 2, index + count + 1))
    mesh = bpy.data.meshes.new("BadlandsTerrainMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    mask = mesh.vertex_colors.new(name="GroundMask")
    for poly in mesh.polygons:
        for loop_index in poly.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            d = abs(vertex.x - wash_center(vertex.y))
            irregular = 0.85 * sin(vertex.x * 0.7 + vertex.y * 0.4)
            patches = sin(vertex.x * 0.31 + vertex.y * 0.12) * sin(vertex.y * 0.25 - vertex.x * 0.11)
            gravel = 0.15 + 0.54 * smoothstep(-0.18, 0.62, patches + 0.13 * d / 20)
            rock = 0.38 * smoothstep(11.0, 29.0, d + irregular) * smoothstep(-0.3, 0.6, -patches)
            swept = 0.44 * (1.0 - smoothstep(2.0, 8.0, d + irregular))
            mask.data[loop_index].color = (gravel, rock, swept, 1.0)
    obj = bpy.data.objects.new("Badlands terrain - study only", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(ground_material())
    return obj


def rock_mesh(name, radius, height, rings, seed):
    local = random.Random(seed)
    sides = 7 + seed % 3
    angles = [2 * pi * i / sides for i in range(sides)]
    radials = [local.uniform(0.78, 1.17) for _ in range(sides)]
    vertices = []
    for level, (z, size) in enumerate(rings):
        for i, angle in enumerate(angles):
            wobble = 1.0 + 0.08 * sin(angle * 3 + level * 1.7)
            vertices.append((radius * size * radials[i] * cos(angle) * wobble,
                             radius * size * radials[i] * sin(angle) * wobble,
                             height * z + 0.06 * radius * sin(angle * 2 + seed)))
    faces = []
    for level in range(len(rings) - 1):
        for i in range(sides):
            next_i = (i + 1) % sides
            faces.append((level * sides + i, level * sides + next_i,
                          (level + 1) * sides + next_i, (level + 1) * sides + i))
    faces.append(tuple(reversed(range(sides))))
    faces.append(tuple((len(rings) - 1) * sides + i for i in range(sides)))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def place_rock(name, x, y, radius, height, seed, mat, spire=False):
    if spire:
        rings = [(0.0, 0.86), (0.16, 1.0), (0.5, 0.76), (0.83, 0.48), (1.0, 0.25)]
    else:
        rings = [(0.0, 0.85), (0.22, 1.0), (0.65, 0.88), (0.93, 0.56), (1.0, 0.3)]
    obj = rock_mesh(name, radius, height, rings, seed)
    obj.location = (x, y, terrain_height(x, y) - 0.16 * height)
    obj.rotation_euler.z = RNG.uniform(0, 2 * pi)
    obj.data.materials.append(mat)
    return obj


def formations(rock_mat, dark_mat):
    clusters = [(-19, 16, 3.5, 5.2), (22, 18, 3.4, 5.8), (-25, -7, 2.8, 3.8),
                (28, -15, 3.0, 4.2), (-15, 32, 2.3, 3.4)]
    for group, (cx, cy, radius, height) in enumerate(clusters):
        place_rock(f"Formation_{group:02d}_Core", cx, cy, radius, height,
                   100 + group, dark_mat if group % 2 else rock_mat, True)
        for piece in range(3):
            angle = 2 * pi * piece / 3 + 0.4 * group
            reach = radius * (0.85 + 0.22 * piece)
            place_rock(f"Formation_{group:02d}_Buttress_{piece}",
                       cx + reach * cos(angle), cy + reach * sin(angle),
                       radius * 0.62, height * (0.45 + 0.1 * piece),
                       200 + 11 * group + piece, rock_mat)


def scatter(rock_mat, dark_mat):
    made = 0
    attempts = 0
    while made < 125 and attempts < 800:
        attempts += 1
        x, y = RNG.uniform(-39, 39), RNG.uniform(-37, 38)
        d = abs(x - wash_center(y))
        if d < 5.5 and RNG.random() < 0.88:
            continue
        if d > 35 and RNG.random() < 0.55:
            continue
        radius = RNG.uniform(0.22, 0.8) * (1.25 if d > 10 else 1.0)
        height = radius * RNG.uniform(0.55, 1.5)
        place_rock(f"Stone_{made:03d}", x, y, radius, height,
                   1000 + made, dark_mat if made % 4 == 0 else rock_mat)
        made += 1


def look_at(obj, point):
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def lighting_and_camera():
    world = bpy.context.scene.world or bpy.data.worlds.new("Dust Twilight")
    bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.34, 0.19, 0.11, 1)
    background.inputs["Strength"].default_value = 0.62

    sun_data = bpy.data.lights.new("Low twilight sun", "SUN")
    sun = bpy.data.objects.new("Low twilight sun", sun_data)
    bpy.context.collection.objects.link(sun)
    sun.location = (-30, 26, 28)
    look_at(sun, (0, 0, 0))
    sun_data.energy = 2.0
    sun_data.color = (1.0, 0.57, 0.31)
    sun_data.angle = 0.07

    camera_data = bpy.data.cameras.new("Elevated review camera")
    camera = bpy.data.objects.new("Elevated review camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (42, -62, 60)
    look_at(camera, (0, 5, 1.8))
    camera_data.type = "PERSP"
    camera_data.lens = 36
    camera_data.clip_end = 300
    bpy.context.scene.camera = camera


def configure_render():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1440
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(STUDY_DIR / "BrokenWorldBadlandsStudy.png")
    scene.view_settings.view_transform = "AgX"
    scene.render.film_transparent = False


def main():
    reset()
    bpy.context.preferences.filepaths.save_version = 0
    terrain()
    rock_mat = material("StudyRock_RustIron", ROCKS / "BrokenWorldRockSurfaceAlbedo.png", 0.2)
    dark_mat = material("StudyRock_DarkFractured", ROCKS / "BrokenWorldRockSurfaceDarkAlbedo.png", 0.18)
    formations(rock_mat, dark_mat)
    scatter(rock_mat, dark_mat)
    lighting_and_camera()
    configure_render()
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(STUDY_DIR / "BrokenWorldBadlandsStudy.blend"))
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
