"""Expand the open badlands study while preserving every existing center vertex.

Run from Blender's Python Console with exec(compile(open(path).read(), path,
'exec'), {'__name__': '__main__', '__file__': path}), then save the open file.
The 180 m center keeps its 0.75 m vertex spacing; the new outer 90 m band
uses 2.5 m spacing. This is a provisional Blender authoring canvas only.
"""

from math import sin
from pathlib import Path

import bpy
from mathutils import Vector


def smoothstep(a, b, value):
    t = max(0.0, min(1.0, (value - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)


def outer_height(x, y):
    wash_center = 3.8 * sin(y / 16.0) + 1.2 * sin(y / 5.5)
    distance = abs(x - wash_center)
    near_height = (
        0.48 * smoothstep(9.0, 18.0, distance)
        + 0.42 * sin(x * 0.18 + y * 0.11) * sin(y * 0.16)
        + 0.20 * sin(x * 0.68 + y * 0.4) * sin(y * 0.52)
        + 0.08 * sin(x * 1.43 + y * 0.71)
        - 0.42 * (1.0 - smoothstep(1.8, 8.0, distance))
    )
    fade = smoothstep(22.0, 75.0, max(abs(x), abs(y)))
    broad = 0.16 * sin(x * 0.045 + y * 0.021) * sin(y * 0.041 - x * 0.019)
    return near_height * (1.0 - 0.95 * fade) + broad * smoothstep(90.0, 120.0, max(abs(x), abs(y)))


def outer_mask(x, y):
    # Wide frequencies avoid aliasing on the more widely spaced outer vertices.
    broad = sin(x * 0.027 + y * 0.015) * sin(y * 0.031 - x * 0.012)
    return (0.34 + 0.10 * broad, 0.07 + 0.045 * broad, 0.08, 1.0)


def expand_terrain(terrain):
    if terrain.get("expanded_authoring_area_m") == 360:
        return False
    old = terrain.data
    if len(old.vertices) != 241 * 241 or len(old.polygons) != 240 * 240:
        raise RuntimeError("Expected the existing 180 m terrain grid; scene was not changed")
    if not old.vertex_colors.get("GroundMask"):
        raise RuntimeError("GroundMask is missing; scene was not changed")
    if terrain.active_material is None:
        raise RuntimeError("Ground material is missing; scene was not changed")

    old_colors = [None] * len(old.vertices)
    mask_data = old.vertex_colors["GroundMask"].data
    for loop in old.loops:
        if old_colors[loop.vertex_index] is None:
            old_colors[loop.vertex_index] = tuple(mask_data[loop.index].color)

    # The first and last 36 samples in each dimension are the new low-density band.
    # The 241 middle samples map one-to-one to the source mesh, including its border.
    coordinates = (
        [-180.0 + 2.5 * i for i in range(36)]
        + [-90.0 + 0.75 * i for i in range(241)]
        + [92.5 + 2.5 * i for i in range(36)]
    )
    side = len(coordinates)
    vertices, colors, faces = [], [], []
    for iy, y in enumerate(coordinates):
        for ix, x in enumerate(coordinates):
            if 36 <= ix <= 276 and 36 <= iy <= 276:
                source_index = (iy - 36) * 241 + (ix - 36)
                vertices.append(tuple(old.vertices[source_index].co))
                colors.append(old_colors[source_index])
            else:
                vertices.append((x, y, outer_height(x, y)))
                border_ix = max(0, min(240, round((x + 90.0) / 0.75)))
                border_iy = max(0, min(240, round((y + 90.0) / 0.75)))
                border_color = old_colors[border_iy * 241 + border_ix]
                broad_color = outer_mask(x, y)
                blend = smoothstep(90.0, 95.0, max(abs(x), abs(y)))
                colors.append(tuple(a * (1 - blend) + b * blend
                                    for a, b in zip(border_color, broad_color)))
    for iy in range(side - 1):
        for ix in range(side - 1):
            index = iy * side + ix
            faces.append((index, index + 1, index + side + 1, index + side))

    mesh = bpy.data.meshes.new("BadlandsTerrain360mAuthoringMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    new_mask = mesh.vertex_colors.new(name="GroundMask")
    for loop in mesh.loops:
        new_mask.data[loop.index].color = colors[loop.vertex_index]
    mesh.materials.append(terrain.active_material)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    terrain.data = mesh
    terrain["expanded_authoring_area_m"] = 360
    terrain["preserved_center_m"] = 180
    if old.users == 0:
        bpy.data.meshes.remove(old)
    return True


def add_overview_camera(scene):
    name = "Expanded authoring overview camera"
    camera = scene.objects.get(name)
    if camera is None:
        camera = bpy.data.objects.new(name, bpy.data.cameras.new(name))
        scene.collection.objects.link(camera)
    camera.location = (0, -310, 240)
    camera.rotation_euler = (Vector((0, 0, 0)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 32
    camera.data.clip_end = 750
    scene.camera = camera


def main():
    if Path(bpy.data.filepath).name != "BrokenWorldBadlandsStudy.blend":
        raise RuntimeError("Open BrokenWorldBadlandsStudy.blend before expanding")
    scene = bpy.context.scene
    terrain = scene.objects.get("Badlands terrain - study only")
    if terrain is None:
        raise RuntimeError("Expected badlands terrain in the open scene")
    changed = expand_terrain(terrain)
    add_overview_camera(scene)
    print("Expanded to 360 m square; original 180 m center preserved" if changed else "360 m terrain already present")
    print("Save the currently open BrokenWorldBadlandsStudy.blend")


if __name__ == "__main__":
    main()
