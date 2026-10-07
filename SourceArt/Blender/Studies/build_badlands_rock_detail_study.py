"""Build a refined-rock badlands study; run with Blender --background --python."""

from math import cos, exp, pi, sin
from pathlib import Path
import random
import tempfile

import bpy
from mathutils import Vector


STUDY_DIR = Path(__file__).resolve().parent
REPO = next(p for p in Path(__file__).resolve().parents if (p / "Packages/manifest.json").is_file() and (p / "ProjectSettings/ProjectVersion.txt").is_file())
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


def image_node(nodes, links, path, vector, scale, box=False):
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = bpy.data.images.load(str(path), check_existing=True)
    texture.extension = "REPEAT"
    if box:
        texture.projection = "BOX"
        texture.projection_blend = 0.18
    mapping = nodes.new("ShaderNodeVectorMath")
    mapping.operation = "SCALE"
    mapping.inputs[3].default_value = scale
    links.new(vector, mapping.inputs[0])
    links.new(mapping.outputs[0], texture.inputs[0])
    return texture.outputs[0]


def material(name, albedo_path, scale, roughness=0.92, value=1.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value = roughness
    geometry = nodes.new("ShaderNodeNewGeometry")
    color = image_node(nodes, links, albedo_path, geometry.outputs["Position"], scale, box=True)
    tint = nodes.new("ShaderNodeHueSaturation")
    tint.inputs["Value"].default_value = value
    links.new(color, tint.inputs["Color"])
    links.new(tint.outputs["Color"], bsdf.inputs["Base Color"])
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 7.0
    noise.inputs["Detail"].default_value = 4.0
    noise.inputs["Roughness"].default_value = 0.58
    links.new(geometry.outputs["Position"], noise.inputs["Vector"])
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.22
    bump.inputs["Distance"].default_value = 0.025
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
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


def profile_size(rings, t):
    for index in range(len(rings) - 1):
        start_z, start_size = rings[index]
        end_z, end_size = rings[index + 1]
        if t <= end_z:
            blend = (t - start_z) / (end_z - start_z)
            return start_size * (1.0 - blend) + end_size * blend
    return rings[-1][1]


def rock_mesh(name, radius, height, rings, seed):
    local = random.Random(seed)
    sides = 26 if radius > 1.0 else 18
    levels = 17 if radius > 1.0 else 12
    angles = [2 * pi * i / sides for i in range(sides)]
    phase = local.uniform(0.0, 2 * pi)
    fracture_angle = local.uniform(0.0, 2 * pi)
    length_ratio = local.uniform(0.78, 1.22)
    vertices = []
    for level in range(levels):
        z = level / (levels - 1)
        size = profile_size(rings, z)
        for i, angle in enumerate(angles):
            broad = 1.0 + 0.11 * cos(2 * angle + phase) + 0.075 * sin(3 * angle - phase * 0.7)
            chips = 0.035 * sin(7 * angle + 9 * z + phase) + 0.023 * sin(11 * angle - 13 * z)
            angular_distance = abs((angle - fracture_angle + pi) % (2 * pi) - pi)
            fissure = 0.15 * exp(-((angular_distance / 0.17) ** 2)) * sin(pi * z) ** 2
            r = radius * size * max(0.55, broad + chips - fissure)
            z_detail = 0.035 * sin(3 * angle + phase) * sin(pi * z)
            vertices.append((r * cos(angle), length_ratio * r * sin(angle),
                             height * (z + z_detail)))
    faces = []
    for level in range(levels - 1):
        for i in range(sides):
            next_i = (i + 1) % sides
            faces.append((level * sides + i, level * sides + next_i,
                          (level + 1) * sides + next_i, (level + 1) * sides + i))
    faces.append(tuple(reversed(range(sides))))
    vertices.append((0.0, 0.0, height * 1.045))
    peak = len(vertices) - 1
    for i in range(sides):
        faces.append(((levels - 1) * sides + i,
                      (levels - 1) * sides + (i + 1) % sides, peak))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    for polygon in list(mesh.polygons)[1:]:
        polygon.use_smooth = True
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def place_rock(name, x, y, radius, height, seed, mat, spire=False):
    if spire:
        rings = [(0.0, 0.81), (0.12, 1.0), (0.35, 0.91), (0.64, 0.73),
                 (0.86, 0.45), (1.0, 0.14)]
    else:
        rings = [(0.0, 0.71), (0.13, 0.91), (0.28, 1.0), (0.5, 0.93),
                 (0.72, 0.75), (0.9, 0.43), (1.0, 0.15)]
    obj = rock_mesh(name, radius, height, rings, seed)
    obj.location = (x, y, terrain_height(x, y) - 0.23 * height)
    obj.rotation_euler.z = RNG.uniform(0, 2 * pi)
    tilt_rng = random.Random(seed + 99173)
    obj.rotation_euler.x = tilt_rng.uniform(-0.07, 0.07)
    obj.rotation_euler.y = tilt_rng.uniform(-0.07, 0.07)
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

    fill_data = bpy.data.lights.new("Soft sky bounce", "AREA")
    fill = bpy.data.objects.new("Soft sky bounce", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = (28, 0, 25)
    look_at(fill, (18, 15, 0))
    fill_data.energy = 2100
    fill_data.shape = "DISK"
    fill_data.size = 25

    camera_data = bpy.data.cameras.new("Elevated review camera")
    camera = bpy.data.objects.new("Elevated review camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (42, -62, 60)
    look_at(camera, (0, 5, 1.8))
    camera_data.type = "PERSP"
    camera_data.lens = 36
    camera_data.clip_end = 300
    bpy.context.scene.camera = camera

    detail_data = bpy.data.cameras.new("Rock detail camera")
    detail = bpy.data.objects.new("Rock detail camera", detail_data)
    bpy.context.collection.objects.link(detail)
    detail.location = (34, 0, 17)
    look_at(detail, (22, 18, 2.5))
    detail_data.lens = 40
    detail_data.clip_end = 100
    return detail


def configure_render():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1440
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(STUDY_DIR / "BrokenWorldBadlandsRockDetailStudy.png")
    scene.view_settings.view_transform = "AgX"
    scene.render.film_transparent = False


def main():
    reset()
    bpy.context.preferences.filepaths.save_version = 0
    terrain()
    rock_mat = material("StudyRock_RustIron", ROCKS / "BrokenWorldRockSurfaceAlbedo.png", 0.2)
    dark_mat = material("StudyRock_DarkFractured", ROCKS / "BrokenWorldRockSurfaceDarkAlbedo.png", 0.18, value=1.45)
    formations(rock_mat, dark_mat)
    scatter(rock_mat, dark_mat)
    detail = lighting_and_camera()
    configure_render()
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(Path(tempfile.gettempdir()) / "BooterBadlandsRockDetailSource.blend"))
    bpy.ops.render.render(write_still=True)
    scene = bpy.context.scene
    main_camera = scene.camera
    scene.camera = detail
    scene.render.filepath = str(STUDY_DIR / "BrokenWorldBadlandsRockDetailCloseup.png")
    bpy.ops.render.render(write_still=True)
    scene.camera = main_camera


if __name__ == "__main__":
    main()
