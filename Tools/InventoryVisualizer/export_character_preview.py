"""Read existing FBXs into a temporary Blender scene; export a browser-only standing preview.

Run Blender hidden/background with --factory-startup --disable-autoexec --python THIS -- ROOT.
No source FBX, .meta, .blend, Unity asset or scene is saved or modified.
"""
import bpy
import hashlib
import json
import struct
import sys
from pathlib import Path
from mathutils import Vector, Matrix

root = Path(sys.argv[sys.argv.index('--') + 1])
out = root / 'Tools/InventoryVisualizer/dist/preview'
out.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
sources = [root / 'Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/defaultmale_rig.fbx',
           root / 'Assets/_Project/Art/Characters/Booter/Cloak/Cloak_Worn.fbx']
parts = []
data = bytearray()
arm_transforms = {}

def standing_vertex(obj, index, point, normal=None):
    transforms = arm_transforms.get(obj.name)
    if not transforms:
        return point if normal is None else normal
    original = point if normal is None else normal
    result = original.copy()
    for groups, matrix in transforms:
        weight = sum(g.weight for g in obj.data.vertices[index].groups if g.group in groups)
        if weight:
            moved = matrix @ point if normal is None else matrix.to_3x3() @ normal
            result += (moved-original)*weight
    return result if normal is None else result.normalized()

def import_meshes(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=True)
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    return [obj for obj in bpy.data.objects if obj not in before and obj.type == 'MESH']

def evaluated_points(objects):
    deps = bpy.context.evaluated_depsgraph_get()
    result = []
    for obj in objects:
        evaluated = obj.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        result.extend(standing_vertex(obj, vertex.index, obj.matrix_world @ vertex.co) for vertex in mesh.vertices)
        evaluated.to_mesh_clear()
    return result

def emit(objects, part, color, transform):
    start = len(data) // 24
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        evaluated = obj.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
        for triangle in mesh.loop_triangles:
            for index in triangle.vertices:
                vertex = mesh.vertices[index]
                world_point = obj.matrix_world @ vertex.co
                point = transform(standing_vertex(obj, index, world_point))
                normal = (normal_matrix @ vertex.normal).normalized()
                normal = standing_vertex(obj, index, world_point, normal)
                data.extend(struct.pack('<6f', point.x, point.z, -point.y, normal.x, normal.z, -normal.y))
        evaluated.to_mesh_clear()
    parts.append({'part':part, 'first':start, 'count':len(data)//24-start, 'color':color})

body = import_meshes(sources[0])
# Bake a neutral standing pose from the original arm-branch skin weights. This
# avoids FBX inheritance quirks while leaving the source rig and mesh unchanged.
for obj in body:
    modifier = next(m for m in obj.modifiers if m.type == 'ARMATURE')
    rig = modifier.object
    modifier.show_viewport = False
    arm_transforms[obj.name] = []
    for name in ['LeftUpperArm', 'RightUpperArm']:
        bone = rig.data.bones[name]
        head = rig.matrix_world @ bone.head_local
        elbow = rig.data.bones[name.replace('UpperArm','LowerArm')].head_local
        direction = (rig.matrix_world.to_3x3() @ (elbow-bone.head_local)).normalized()
        target = Vector((.19 if head.x > 0 else -.19, 0, -1)).normalized()
        turn = direction.rotation_difference(target).to_matrix().to_4x4()
        names = {b.name for b in [bone]+list(bone.children_recursive)}
        groups = {g.index for g in obj.vertex_groups if g.name in names}
        arm_transforms[obj.name].append((groups, Matrix.Translation(head) @ turn @ Matrix.Translation(-head)))
bpy.context.view_layer.update()
points = evaluated_points(body)
low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
if high.x-low.x > 1:
    raise RuntimeError('Standing pose export still contains an extended arm.')
scale = 1.8 / (high.z-low.z)
center = Vector((0, (low.y+high.y)/2, low.z))
emit(body, 'body', [.47,.49,.43], lambda p:(p-center)*scale)

cloak = import_meshes(sources[1])
points = evaluated_points(cloak)
clow = Vector(tuple(min(p[i] for p in points) for i in range(3)))
chigh = Vector(tuple(max(p[i] for p in points) for i in range(3)))
ccenter = (clow+chigh)*.5
cscale = .91 / max(chigh.z-clow.z, .001)
emit(cloak, 'equipment.cloak', [.29,.24,.18], lambda p:(p-ccenter)*cscale+Vector((0,.12,1.08)))

(out / 'character.bin').write_bytes(data)
metadata = {'version':1, 'stride':24, 'parts':parts, 'pose':'Existing humanoid rig, arms lowered to a neutral standing inspection pose in a temporary scene; normalized to 1.8m for this viewer only.',
            'sourceFiles':[{'path':str(p.relative_to(root)), 'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sources],
            'limitations':'Prototype character, neutral preview materials; equipment proxies are presentation only. No approved final Booter appearance is claimed.',
            'blender':bpy.app.version_string, 'vertices':len(data)//24}
(out / 'character.json').write_bytes((json.dumps(metadata,indent=2)+'\n').encode('utf-8'))
print(json.dumps({'parts':parts,'bodyBounds':[list(low),list(high)],'cloakBounds':[list(clow),list(chigh)],'bytes':len(data),'blender':bpy.app.version_string}))
