"""Render the prepared or applied patch using transient camera/light overrides; never save."""
import sys
from pathlib import Path
import bpy
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]).resolve(); mode=sys.argv[-1]
proof=root/'Logs/FlatStoneGroundPatch01'; asset='FlatStoneGroundPatch01'
scene=bpy.context.scene
if mode=='prepared':
    with bpy.data.libraries.load(str(proof/'prepared_patch.blend'),link=False) as (src,dst): dst.objects=[asset]
    scene.collection.objects.link(dst.objects[0])
bpy.context.view_layer.update(); patch=bpy.data.objects[asset]; patch.hide_set(False); patch.hide_render=False
for o in scene.objects:
    if o.type=='MESH': o.hide_render=o!=patch
corners=[patch.matrix_world@Vector(c) for c in patch.bound_box]
lo=Vector(tuple(min(p[i] for p in corners) for i in range(3))); hi=Vector(tuple(max(p[i] for p in corners) for i in range(3)))
center=(lo+hi)/2; extent=max(hi-lo)
scene.camera.data.type='ORTHO'; scene.camera.data.ortho_scale=extent*1.25
scene.camera.location=center+Vector((6,-8,10)).normalized()*extent*2
scene.camera.rotation_euler=(center-scene.camera.location).to_track_quat('-Z','Y').to_euler()
for o in scene.objects:
    if o.type=='LIGHT':
        o.location=center+Vector((2,-4,8) if o.name=='Key' else (-4,4,6)); o.hide_render=False
        o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=1400; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
scene.render.filepath=str(proof/(mode+'-patch.png')); bpy.ops.render.render(write_still=True)
scene.camera.location=center+Vector((0,-10,1)).normalized()*extent*2
scene.camera.rotation_euler=(center-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(proof/(mode+'-side.png')); bpy.ops.render.render(write_still=True)
print('Transient renders complete; native source not saved',flush=True)
