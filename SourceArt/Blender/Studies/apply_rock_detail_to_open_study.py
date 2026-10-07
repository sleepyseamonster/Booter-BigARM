"""Apply the refined rock assets to the currently open badlands .blend without replacing the scene.

Run from Blender's Python Console with:
exec(compile(open('/absolute/path/to/this/file.py').read(), '/absolute/path/to/this/file.py', 'exec'))
"""

from pathlib import Path
import tempfile

import bpy
from mathutils import Vector


STUDY_DIR = Path(__file__).resolve().parent
SOURCE = Path(tempfile.gettempdir()) / "BooterBadlandsRockDetailSource.blend"
ROCK_PREFIXES = ("Formation_", "Stone_")


def main():
    scene = bpy.context.scene
    if "Badlands terrain - study only" not in bpy.data.objects:
        raise RuntimeError("Open the badlands study before applying rock detail")
    rocks = {obj.name: obj for obj in scene.objects
             if obj.type == "MESH" and obj.name.startswith(ROCK_PREFIXES)}
    if len(rocks) != 145:
        raise RuntimeError(f"Expected 145 study rocks; found {len(rocks)}. No changes made.")
    if not SOURCE.exists():
        raise RuntimeError("Build the temporary rock-detail source scene before applying this script")
    desired = {name + "Mesh" for name in rocks}
    with bpy.data.libraries.load(str(SOURCE), link=False) as (available, loaded):
        missing = desired - set(available.meshes)
        if missing:
            raise RuntimeError(f"Refined meshes missing: {sorted(missing)[:5]}")
        loaded.meshes = sorted(desired)
    replacements = {requested.removesuffix("Mesh"): mesh
                    for requested, mesh in zip(sorted(desired), loaded.meshes)}
    if set(replacements) != set(rocks):
        raise RuntimeError("Imported rock names do not match the open scene. No changes made.")

    previous = []
    for name, obj in rocks.items():
        previous.append(obj.data)
        obj.data = replacements[name]
        obj["rock_detail_pass"] = "2026-09-30"
    for mesh in previous:
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)

    if "Soft sky bounce" not in bpy.data.objects:
        light_data = bpy.data.lights.new("Soft sky bounce", "AREA")
        fill = bpy.data.objects.new("Soft sky bounce", light_data)
        scene.collection.objects.link(fill)
        fill.location = (28, 0, 25)
        direction = Vector((18, 15, 0)) - fill.location
        fill.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
        light_data.energy = 2100
        light_data.shape = "DISK"
        light_data.size = 25

    bpy.ops.file.pack_all()
    print(f"Refined {len(rocks)} rocks in the currently open {Path(bpy.data.filepath).name}; save this file to keep the edit.")


if __name__ == "__main__":
    main()
