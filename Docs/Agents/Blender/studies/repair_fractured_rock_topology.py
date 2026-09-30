"""Remesh folded badlands rock sources at bounded, game-conscious density.

Run in the existing Blender scene, then inspect and save the same .blend.
Object transforms, material assignments, and shared instancing are preserved.
"""
from pathlib import Path
import bpy


STUDY = 'BrokenWorldBadlandsStudy.blend'
MARKER = 'clean_fracture_topology_2026_09_30'


def apply():
    if Path(bpy.data.filepath).name != STUDY:
        raise RuntimeError(f'Open the existing {STUDY}')
    terrain = bpy.data.objects.get('Badlands terrain - study only')
    if terrain is None or not terrain.get('geology_pass_six'):
        raise RuntimeError('Expected the inspected six-pass badlands study')
    if terrain.get(MARKER):
        print('Controlled fracture topology already applied')
        return
    targets = [o for o in bpy.context.scene.objects if o.type == 'MESH' and
               (o.get('geological_fracture_pass') or
                o.name.startswith('Exposed shale plate') or
                o.name == 'Reference low eroded shelf')]
    old_to_new = {}
    selected = tuple(bpy.context.selected_objects)
    active = bpy.context.view_layer.objects.active
    try:
        for obj in targets:
            old = obj.data
            if old in old_to_new:
                obj.data = old_to_new[old]
            else:
                points = [v.co for v in old.vertices]
                span = min(max(p[axis] for p in points)-min(p[axis] for p in points)
                           for axis in range(3))
                materials = tuple(old.materials)
                obj.data = old.copy()
                bpy.ops.object.select_all(action='DESELECT')
                obj.select_set(True)
                bpy.context.view_layer.objects.active = obj
                obj.data.remesh_voxel_size = max(.015, span / 85)
                bpy.ops.object.voxel_remesh()
                smooth = obj.modifiers.new('Remove folded micro edges', 'SMOOTH')
                smooth.factor = .8
                smooth.iterations = 3
                bpy.ops.object.modifier_apply(modifier=smooth.name)
                # Blender's decimate ratio settles close to these vertex
                # budgets. Larger silhouettes receive the most detail.
                budget = (2500 if obj.name.startswith('Formation_') else
                          850 if obj.name.startswith('Exposed shale plate') else
                          550)
                if len(obj.data.polygons) > budget:
                    reduce = obj.modifiers.new('Bounded rock density', 'DECIMATE')
                    reduce.ratio = budget / len(obj.data.polygons)
                    bpy.ops.object.modifier_apply(modifier=reduce.name)
                for polygon in obj.data.polygons:
                    polygon.use_smooth = True
                for material in materials:
                    if material.name not in obj.data.materials:
                        obj.data.materials.append(material)
                old_to_new[old] = obj.data
            obj[MARKER] = True
    finally:
        bpy.ops.object.select_all(action='DESELECT')
        for obj in selected:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = active
    terrain[MARKER] = True
    print(f'CONTROLLED TOPOLOGY: {len(targets)} rock objects, '
          f'{len(old_to_new)} shared sources; folded bevel topology remeshed')


if __name__ == '__main__':
    apply()
