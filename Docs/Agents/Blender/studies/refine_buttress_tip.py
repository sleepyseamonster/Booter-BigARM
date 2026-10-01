"""Repair the torn tip on Formation_00_Buttress_0 in the open study.

Run in the existing Blender session with runpy.run_path; then inspect and save.
This preserves the object's transform, material assignment, and scene placement.
"""

from pathlib import Path

import bmesh
import bpy


OBJECT_NAME = "Formation_00_Buttress_0"
MARKER = "coherent_buttress_tip_2026_09_30"
TRIANGLE_BUDGET = 2800


def triangle_count(mesh):
    return sum(len(face.vertices) - 2 for face in mesh.polygons)


def apply():
    if Path(bpy.data.filepath).name != "BrokenWorldBadlandsStudy.blend":
        raise RuntimeError("Open the existing badlands study before applying")
    obj = bpy.data.objects.get(OBJECT_NAME)
    if obj is None or obj.type != "MESH":
        raise RuntimeError(f"Missing mesh {OBJECT_NAME}")
    if obj.get(MARKER):
        print("Buttress tip repair already applied")
        return

    before = triangle_count(obj.data)
    if before < TRIANGLE_BUDGET:
        raise RuntimeError(f"Unexpectedly sparse starting mesh: {before} triangles")
    materials = tuple(obj.data.materials)
    selected = tuple(bpy.context.selected_objects)
    active = bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj

        # Remove the interlocking needle folds at the summit. The voxel size
        # follows the rock's five-metre footprint, not viewport magnification.
        obj.data.remesh_voxel_size = 0.085
        bpy.ops.object.voxel_remesh()
        smooth = obj.modifiers.new("Unify broken tip", "SMOOTH")
        smooth.factor = 0.7
        smooth.iterations = 2
        bpy.ops.object.modifier_apply(modifier=smooth.name)

        current = triangle_count(obj.data)
        if current > TRIANGLE_BUDGET:
            reduce = obj.modifiers.new("Buttress source budget", "DECIMATE")
            reduce.ratio = TRIANGLE_BUDGET / current
            bpy.ops.object.modifier_apply(modifier=reduce.name)
        for face in obj.data.polygons:
            face.use_smooth = True
        for material in materials:
            if material.name not in obj.data.materials:
                obj.data.materials.append(material)

        check = bmesh.new()
        try:
            check.from_mesh(obj.data)
            nonmanifold = sum(not edge.is_manifold for edge in check.edges)
        finally:
            check.free()
        after = triangle_count(obj.data)
        if nonmanifold or after > TRIANGLE_BUDGET:
            raise RuntimeError(f"Invalid result: {nonmanifold} nonmanifold edges, {after} triangles")
        obj[MARKER] = True
        print(f"BUTTRESS TIP: {before} -> {after} triangles, {len(obj.data.vertices)} vertices, closed mesh")
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for selected_obj in selected:
            if selected_obj.name in bpy.data.objects:
                selected_obj.select_set(True)
        bpy.context.view_layer.objects.active = active


if __name__ == "__main__":
    apply()
