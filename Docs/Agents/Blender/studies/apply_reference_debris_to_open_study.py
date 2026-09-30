"""Add reference-inspired scree around the existing low badlands formations.

Run this once in the Python Console of the open BrokenWorldBadlandsStudy.blend.
It adds a separate, hideable collection and a close review camera, and leaves
the terrain, existing rocks, user lights, and earlier cameras intact.
"""

from math import cos, floor, pi, sin
from pathlib import Path
import random

import bpy
from mathutils import Vector


PASS_COLLECTION = "Reference study - rubble aprons"


def ground_z(terrain, x, y):
    """Bilinear height sample within the preserved 0.75 m central terrain grid."""
    ix = min(239, max(0, floor((x + 90.0) / 0.75)))
    iy = min(239, max(0, floor((y + 90.0) / 0.75)))
    tx = min(1.0, max(0.0, (x + 90.0) / 0.75 - ix))
    ty = min(1.0, max(0.0, (y + 90.0) / 0.75 - iy))
    side = 313
    def z(dx, dy):
        return terrain.data.vertices[(iy + 36 + dy) * side + ix + 36 + dx].co.z
    return ((1 - tx) * (1 - ty) * z(0, 0) + tx * (1 - ty) * z(1, 0)
            + (1 - tx) * ty * z(0, 1) + tx * ty * z(1, 1))


def debris_materials():
    dark = bpy.data.materials.get("StudyRock_DarkFractured.001")
    rust = bpy.data.materials.get("StudyRock_RustIron.001")
    if dark is None or rust is None:
        raise RuntimeError("Expected the existing textured study rock materials")
    shale = bpy.data.materials.get("Scree - dusty shale")
    if shale is None:
        shale = dark.copy()
        shale.name = "Scree - dusty shale"
        hue = next((node for node in shale.node_tree.nodes if node.type == "HUE_SAT"), None)
        if hue:
            hue.inputs["Saturation"].default_value = 0.60
            hue.inputs["Value"].default_value = 1.38
    return (dark, rust, shale)


def append_chip(vertices, faces, face_materials, rng, x, y, z,
                size, height, angle, material_index):
    """Append a closed, irregular 6-sided broken fragment to a batch mesh."""
    start = len(vertices)
    sides = 6
    stretch = rng.uniform(0.72, 1.45)
    for ring in range(2):
        for index in range(sides):
            direction = angle + 2 * pi * index / sides + rng.uniform(-0.11, 0.11)
            radius = size * rng.uniform(0.78, 1.16) * (0.68 if ring else 1.0)
            vx = x + radius * cos(direction) * stretch
            vy = y + radius * sin(direction) / stretch
            vz = z + (-0.055 * size if ring == 0 else height * rng.uniform(0.68, 1.1))
            vertices.append((vx, vy, vz))
    for index in range(sides):
        nxt = (index + 1) % sides
        faces.append((start + index, start + nxt, start + sides + nxt,
                      start + sides + index))
        face_materials.append(material_index)
    faces.append(tuple(start + sides + index for index in range(sides)))
    face_materials.append(material_index)
    faces.append(tuple(start + index for index in reversed(range(sides))))
    face_materials.append(material_index)


def add_low_shelf(scene, terrain, collection):
    name = "Reference low eroded shelf"
    existing = scene.objects.get(name)
    if existing:
        return existing
    cx, cy = 64.0, -9.0
    base_z = ground_z(terrain, cx, cy)
    rng = random.Random(88019)
    sides = 22
    radii = [rng.uniform(0.84, 1.12) *
             (1.0 + 0.19 * sin(2 * pi * index * 3 / sides + 0.4)
              + 0.08 * sin(2 * pi * index * 7 / sides))
             for index in range(sides)]
    tiers = ((1.08, -0.10), (0.97, 0.50), (0.88, 1.02),
             (1.05, 1.17), (0.83, 2.10), (0.80, 2.27))
    vertices, faces, material_indices = [], [], []
    for ring, (width, height) in enumerate(tiers):
        for index in range(sides):
            angle = 2 * pi * index / sides
            radius = radii[index] * width
            x = 5.8 * radius * cos(angle) + 0.42 * sin(2 * angle)
            y = 3.8 * radius * sin(angle) + 0.29 * cos(5 * angle)
            z = ((ground_z(terrain, cx + x, cy + y) - base_z - 0.10)
                 if ring == 0 else height + 0.09 * sin(index * 2.9 + ring))
            vertices.append((x, y, z))
    for ring in range(len(tiers) - 1):
        for index in range(sides):
            next_index = (index + 1) % sides
            faces.append((ring * sides + index,
                          ring * sides + next_index,
                          (ring + 1) * sides + next_index,
                          (ring + 1) * sides + index))
            material_indices.append(1 if ring in (0, 2, 4) else 0)
    faces.append(tuple(reversed(range(sides))))
    material_indices.append(0)
    top_start = len(vertices)
    for index in range(sides):
        angle = 2 * pi * index / sides
        radius = radii[index] * 0.36
        vertices.append((5.8 * radius * cos(angle),
                         3.8 * radius * sin(angle),
                         2.26 + 0.17 * sin(index * 2.2)))
        faces.append(((len(tiers) - 1) * sides + index,
                      (len(tiers) - 1) * sides + (index + 1) % sides,
                      top_start + (index + 1) % sides,
                      top_start + index))
        material_indices.append(1)
    vertices.append((0.15, -0.18, 2.43))
    for index in range(sides):
        faces.append((top_start + index, top_start + (index + 1) % sides,
                      len(vertices) - 1))
        material_indices.append(1)
    mesh = bpy.data.meshes.new("ReferenceLowErodedShelfMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    dark, rust, _ = debris_materials()
    mesh.materials.append(dark)
    mesh.materials.append(rust)
    for face, material_index in zip(mesh.polygons, material_indices):
        face.material_index = material_index
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.location = (cx, cy, base_z)
    obj["reference_note"] = "Short layered eroded shelf, about 2.3 m high; provisional visual study"
    return obj


def add_rubble(scene, terrain, collection):
    colors = debris_materials()
    fan_directions = (4.6, 5.45, 3.8, 4.35, 5.2, 4.75)
    total = 0
    for group in range(6):
        name = f"Reference scree apron {group:02d}"
        if scene.objects.get(name) is not None:
            continue
        core = scene.objects.get(f"Formation_{group:02d}_Core" if group < 5
                                 else "Reference low eroded shelf")
        if core is None:
            raise RuntimeError(f"Missing existing formation {group}")
        rng = random.Random(94017 + group * 717)
        vertices, faces, indices = [], [], []
        base_radius = 0.44 * max(core.dimensions.x, core.dimensions.y) + 0.6
        for index in range(320):
            # Uneven downslope fans are dense at the foot and taper outward.
            fan = rng.random() < 0.78
            angle = (rng.gauss(fan_directions[group], 0.77) if fan
                     else rng.uniform(0, 2 * pi))
            reach = base_radius + 0.35 + 11.0 * rng.random() ** 1.85
            x = core.location.x + reach * cos(angle)
            y = core.location.y + reach * sin(angle) * rng.uniform(0.72, 1.12)
            if index < 22:
                size = rng.uniform(0.30, 0.72)
            elif index < 145:
                size = rng.uniform(0.15, 0.39)
            else:
                size = rng.uniform(0.045, 0.17)
            height = size * rng.uniform(0.18, 0.72)
            material_index = rng.choices((0, 1, 2), weights=(0.51, 0.32, 0.17))[0]
            append_chip(vertices, faces, indices, rng, x, y,
                        ground_z(terrain, x, y) + 0.014, size, height,
                        rng.uniform(0, 2 * pi), material_index)
            total += 1
        mesh = bpy.data.meshes.new(f"ReferenceScreeApron{group:02d}Mesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        for material in colors:
            mesh.materials.append(material)
        for polygon, material_index in zip(mesh.polygons, indices):
            polygon.material_index = material_index
        obj = bpy.data.objects.new(name, mesh)
        collection.objects.link(obj)
        obj["fragments"] = 320
        obj["reference_note"] = "Dense dark and rust rubble near existing low formation; visual study"
    return total


def add_review_camera(scene, collection):
    name = "Rubble and rock close review camera"
    camera = scene.objects.get(name)
    if camera is None:
        camera = bpy.data.objects.new(name, bpy.data.cameras.new(name))
        collection.objects.link(camera)
    camera.location = (78, -43, 22)
    target = Vector((64, -9, 0.65))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 57
    camera.data.clip_end = 500
    scene.camera = camera


def add_warm_local_bounce(scene, collection):
    name = "Reference warm rock bounce"
    if scene.objects.get(name):
        return
    light_data = bpy.data.lights.new(name, "AREA")
    light_data.energy = 1150
    light_data.color = (1.0, 0.67, 0.47)
    light_data.shape = "DISK"
    light_data.size = 14
    light = bpy.data.objects.new(name, light_data)
    collection.objects.link(light)
    light.location = (56, -21, 17)
    light.rotation_euler = (Vector((64, -9, 0)) - light.location).to_track_quat("-Z", "Y").to_euler()


def main():
    if Path(bpy.data.filepath).name != "BrokenWorldBadlandsStudy.blend":
        raise RuntimeError("Open the current BrokenWorldBadlandsStudy.blend first")
    scene = bpy.context.scene
    terrain = scene.objects.get("Badlands terrain - study only")
    if terrain is None or terrain.get("expanded_authoring_area_m") != 360:
        raise RuntimeError("Expected the existing 360 m badlands study")
    collection = bpy.data.collections.get(PASS_COLLECTION)
    if collection is None:
        collection = bpy.data.collections.new(PASS_COLLECTION)
        scene.collection.children.link(collection)
    add_low_shelf(scene, terrain, collection)
    count = add_rubble(scene, terrain, collection)
    add_review_camera(scene, collection)
    add_warm_local_bounce(scene, collection)
    print(f"Reference debris pass: {count} chips added around existing low formations")
    print("Save the currently open BrokenWorldBadlandsStudy.blend")


if __name__ == "__main__":
    main()
