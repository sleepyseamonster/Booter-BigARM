"""Replace the study terrain's periodic ripples in the currently open .blend.

Run in Blender's Python Console with exec(compile(open(path).read(), path,
'exec'), {'__name__': '__main__', '__file__': path}), then save the open file.
This is an in-place correction, not a rebuild of the user's scene.
"""

from math import sin
from pathlib import Path

import bpy
from mathutils import Vector, noise


def smoothstep(a, b, value):
    t = max(0.0, min(1.0, (value - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)


def signed_noise(x, y, z):
    return noise.noise(Vector((x, y, z)))


def height_change(x, y):
    """Swap only the known sine terms, retaining the existing wash and edits."""
    radius = max(abs(x), abs(y))
    center_weight = 1.0 - 0.95 * smoothstep(22.0, 75.0, radius)
    old_ripples = (
        0.42 * sin(x * 0.18 + y * 0.11) * sin(y * 0.16)
        + 0.20 * sin(x * 0.68 + y * 0.4) * sin(y * 0.52)
        + 0.08 * sin(x * 1.43 + y * 0.71)
    ) * center_weight
    old_outer = (
        0.16 * sin(x * 0.045 + y * 0.021)
        * sin(y * 0.041 - x * 0.019)
        * smoothstep(90.0, 120.0, radius)
    )

    # Warped coherent noise makes irregular islands. A narrow smooth threshold
    # gives low plateaus with short shoulders instead of continuous sine ridges.
    warp_x = 4.0 * signed_noise(x / 43.0, y / 43.0, 7.3)
    warp_y = 4.0 * signed_noise(x / 43.0, y / 43.0, 19.7)
    wx, wy = x + warp_x, y + warp_y
    landform = (
        signed_noise(wx / 22.0, wy / 22.0, 31.2)
        + 0.34 * signed_noise(wx / 9.0, wy / 9.0, 53.4)
    )
    plateau = 0.52 * (smoothstep(-0.15, 0.15, landform) - 0.5)
    fine = 0.055 * signed_noise(wx / 4.5, wy / 4.5, 71.8)
    new_center = (plateau + fine) * center_weight
    new_outer = 0.11 * signed_noise(wx / 38.0, wy / 38.0, 97.1) * smoothstep(90.0, 120.0, radius)
    return new_center + new_outer - old_ripples - old_outer


def main():
    if Path(bpy.data.filepath).name != "BrokenWorldBadlandsStudy.blend":
        raise RuntimeError("Open the working BrokenWorldBadlandsStudy.blend first")
    scene = bpy.context.scene
    terrain = scene.objects.get("Badlands terrain - study only")
    if terrain is None or terrain.type != "MESH" or terrain.get("expanded_authoring_area_m") != 360:
        raise RuntimeError("Expected the expanded 360 m study terrain")
    if terrain.get("irregular_plateau_relief_pass"):
        print("Irregular plateau relief already applied")
        return
    if len(terrain.data.vertices) != 97969:
        raise RuntimeError("Terrain vertex count changed; review the mesh before applying this pass")

    changes = [height_change(vertex.co.x, vertex.co.y) for vertex in terrain.data.vertices]
    for vertex, change in zip(terrain.data.vertices, changes):
        vertex.co.z += change
    terrain.data.update()

    moved = 0
    for obj in scene.objects:
        if obj.type != "MESH" or obj == terrain:
            continue
        if obj.name.startswith(("Formation_", "Stone_", "Talus_")) or obj.name == "Reference low eroded shelf":
            obj.location.z += height_change(obj.matrix_world.translation.x, obj.matrix_world.translation.y)
            moved += 1
        elif obj.name.startswith("Reference scree apron "):
            # These six combined meshes store fragment positions in their own
            # vertices, so each fragment needs the terrain change at its XY.
            world = obj.matrix_world.copy()
            local_vector = world.inverted().to_3x3()
            for vertex in obj.data.vertices:
                point = world @ vertex.co
                vertex.co += local_vector @ Vector((0.0, 0.0, height_change(point.x, point.y)))
            obj.data.update()
            moved += 1

    terrain["irregular_plateau_relief_pass"] = "2026-09-30"
    print(f"Replaced periodic height terms on {len(changes)} terrain vertices; adjusted {moved} rock/scree objects")
    print("Save the currently open BrokenWorldBadlandsStudy.blend")


if __name__ == "__main__":
    main()
