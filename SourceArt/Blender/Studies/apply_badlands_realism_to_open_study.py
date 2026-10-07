"""Improve the current badlands study in place, preserving edited sun and original camera.

Run from Blender's Python Console with an exec command that sets __file__ to this path.
"""

from math import cos, pi, sin
from pathlib import Path
import random

import bpy
from mathutils import Vector


def terrain_height(x, y):
    def smoothstep(a, b, value):
        t = max(0.0, min(1.0, (value - a) / (b - a)))
        return t * t * (3.0 - 2.0 * t)

    wash_center = 3.8 * sin(y / 16.0) + 1.2 * sin(y / 5.5)
    distance = abs(x - wash_center)
    return (0.48 * smoothstep(9.0, 18.0, distance)
            + 0.42 * sin(x * 0.18 + y * 0.11) * sin(y * 0.16)
            + 0.20 * sin(x * 0.68 + y * 0.4) * sin(y * 0.52)
            + 0.08 * sin(x * 1.43 + y * 0.71)
            - 0.42 * (1.0 - smoothstep(1.8, 8.0, distance)))


def extend_terrain(terrain):
    if terrain.get("extended_realism_pass"):
        return
    count, extent = 240, 90.0
    vertices, faces = [], []
    def smoothstep(a, b, value):
        t = max(0.0, min(1.0, (value - a) / (b - a)))
        return t * t * (3.0 - 2.0 * t)

    for iy in range(count + 1):
        y = -extent + 2 * extent * iy / count
        for ix in range(count + 1):
            x = -extent + 2 * extent * ix / count
            fade = smoothstep(22.0, 75.0, max(abs(x), abs(y)))
            vertices.append((x, y, terrain_height(x, y) * (1.0 - 0.95 * fade)))
    for iy in range(count):
        for ix in range(count):
            index = iy * (count + 1) + ix
            faces.append((index, index + 1, index + count + 2, index + count + 1))
    mesh = bpy.data.meshes.new("BadlandsTerrainExpandedMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    mask = mesh.vertex_colors.new(name="GroundMask")

    vertex_colors = []
    for x, y, _ in vertices:
        center = 3.8 * sin(y / 16.0) + 1.2 * sin(y / 5.5)
        distance = abs(x - center)
        irregular = 0.85 * sin(x * 0.7 + y * 0.4)
        patches = sin(x * 0.31 + y * 0.12) * sin(y * 0.25 - x * 0.11)
        gravel = 0.15 + 0.54 * smoothstep(-0.18, 0.62, patches + 0.13 * distance / 20)
        rock = 0.38 * smoothstep(11.0, 29.0, distance + irregular) * smoothstep(-0.3, 0.6, -patches)
        swept = 0.44 * (1.0 - smoothstep(2.0, 8.0, distance + irregular))
        vertex_colors.append((gravel, rock, swept, 1.0))
    for loop_index, loop in enumerate(mesh.loops):
        mask.data[loop_index].color = vertex_colors[loop.vertex_index]
    mesh.materials.append(terrain.active_material)
    old_mesh = terrain.data
    terrain.data = mesh
    terrain["extended_realism_pass"] = "2026-09-30"
    if old_mesh.users == 0:
        bpy.data.meshes.remove(old_mesh)


def improve_ground(terrain):
    for polygon in terrain.data.polygons:
        polygon.use_smooth = True
    material = terrain.active_material
    if material is None or material.node_tree is None:
        raise RuntimeError("Expected the existing node-based terrain material")
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        raise RuntimeError("Expected terrain Principled BSDF")
    if nodes.get("Ground broad variation"):
        return
    original_color = bsdf.inputs["Base Color"].links[0].from_socket
    position = nodes.new("ShaderNodeNewGeometry")
    position.name = "Ground world position"
    broad = nodes.new("ShaderNodeTexNoise")
    broad.name = "Ground broad variation"
    broad.inputs["Scale"].default_value = 0.065
    broad.inputs["Detail"].default_value = 3.0
    broad.inputs["Roughness"].default_value = 0.62
    links.new(position.outputs["Position"], broad.inputs["Vector"])
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.name = "Gravel patch coverage"
    ramp.color_ramp.elements[0].position = 0.28
    ramp.color_ramp.elements[0].color = (0.02, 0.02, 0.02, 1)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = (0.40, 0.40, 0.40, 1)
    links.new(broad.outputs["Fac"], ramp.inputs["Fac"])
    gravel_image = next((node for node in nodes if node.type == "TEX_IMAGE"
                         and node.image and "GravelAlbedo" in node.image.name), None)
    if gravel_image is None:
        raise RuntimeError("Expected packed gravel albedo in ground material")
    blend = nodes.new("ShaderNodeMixRGB")
    blend.name = "Mixed gravel dust"
    blend.blend_type = "MIX"
    links.new(ramp.outputs["Color"], blend.inputs[0])
    links.new(original_color, blend.inputs[1])
    links.new(gravel_image.outputs["Color"], blend.inputs[2])
    hue = nodes.new("ShaderNodeHueSaturation")
    hue.name = "Dry earth color balance"
    hue.inputs["Saturation"].default_value = 0.78
    hue.inputs["Value"].default_value = 0.92
    links.new(blend.outputs[0], hue.inputs["Color"])
    links.new(hue.outputs["Color"], bsdf.inputs["Base Color"])


def settle_and_vary_rocks(scene):
    offsets = {(0, 1): (-0.45, 0.18, 0.88), (1, 2): (0.60, -0.38, 1.10),
               (2, 0): (0.37, 0.52, 0.82), (3, 1): (-0.58, 0.28, 1.16),
               (4, 2): (0.47, -0.41, 0.92)}
    changed = 0
    for obj in scene.objects:
        if obj.type != "MESH" or not obj.name.startswith(("Formation_", "Stone_")):
            continue
        if obj.get("grounded_realism_pass"):
            continue
        obj.location.z -= 0.065 * obj.dimensions.z
        if obj.name.startswith("Formation_") and "_Buttress_" in obj.name:
            group = int(obj.name[10:12])
            piece = int(obj.name[-1])
            if (group, piece) in offsets:
                dx, dy, vertical_scale = offsets[(group, piece)]
                obj.location.x += dx
                obj.location.y += dy
                obj.scale.z *= vertical_scale
        obj["grounded_realism_pass"] = "2026-09-30"
        changed += 1
    return changed


def talus_fragment(name, x, y, size, height, seed, material):
    rng = random.Random(seed)
    direction = rng.uniform(0, 2 * pi)
    vertices = []
    for i in range(5):
        angle = direction + 2 * pi * i / 5
        radius = size * rng.uniform(0.7, 1.15)
        vertices.append((radius * cos(angle), radius * sin(angle), 0.0))
    vertices.append((rng.uniform(-0.2, 0.2) * size,
                     rng.uniform(-0.2, 0.2) * size, height))
    faces = [tuple(reversed(range(5)))]
    faces += [(i, (i + 1) % 5, 5) for i in range(5)]
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = (x, y, terrain_height(x, y) - height * 0.18 + 0.015)
    mesh.materials.append(material)


def add_talus(scene):
    if any(obj.name.startswith("Talus_") for obj in scene.objects):
        return 0
    rng = random.Random(91732)
    made = 0
    fan_centers = (0.45, 2.65, 4.55, 1.65, 5.25)
    for group in range(5):
        core = scene.objects.get(f"Formation_{group:02d}_Core")
        if core is None:
            raise RuntimeError(f"Missing formation {group} core")
        rock_material = core.active_material
        light_material = scene.objects[f"Formation_{group:02d}_Buttress_0"].active_material
        for index in range(20):
            angle = rng.gauss(fan_centers[group], 0.55)
            reach = rng.uniform(3.2, 8.2)
            x = core.location.x + reach * cos(angle)
            y = core.location.y + reach * sin(angle)
            size = rng.uniform(0.12, 0.43)
            height = size * rng.uniform(0.35, 0.95)
            talus_fragment(f"Talus_{group:02d}_{index:02d}", x, y, size,
                           height, 10000 + group * 100 + index,
                           light_material if index % 3 else rock_material)
            made += 1
    return made


def make_review_camera(scene):
    camera = scene.objects.get("Interior realism review camera")
    if camera is None:
        data = bpy.data.cameras.new("Interior realism review camera")
        camera = bpy.data.objects.new("Interior realism review camera", data)
        scene.collection.objects.link(camera)
    camera.location = (42, -62, 60)
    direction = Vector((0.0, 5.0, 1.8)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 43
    camera.data.clip_end = 300
    scene.camera = camera


def main():
    scene = bpy.context.scene
    terrain = scene.objects.get("Badlands terrain - study only")
    if terrain is None or not Path(bpy.data.filepath).name == "BrokenWorldBadlandsStudy.blend":
        raise RuntimeError("Open BrokenWorldBadlandsStudy.blend before applying this pass")
    extend_terrain(terrain)
    improve_ground(terrain)
    changed = settle_and_vary_rocks(scene)
    fragments = add_talus(scene)
    make_review_camera(scene)
    bpy.ops.file.pack_all()
    print(f"Ground shading adjusted, {changed} rocks settled, {fragments} talus fragments added; save the open file.")


if __name__ == "__main__":
    main()
