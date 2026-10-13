"""Bake the approved limestone onto the existing optimized rock and save the same scene.
Blender --background --disable-autoexec BooterGroundSetup.blend --python THIS -- ROOT
Completed maps can resume a failed attempt only while the scene/source hashes match.
"""
import hashlib, json, shutil, sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
scene_path=root/'SourceArt/Blender/Studies/BooterGroundSetup/BooterGroundSetup.blend'
base=scene_path.parent/'LimestoneRock01/ApprovedWhiteV2'
source_dir=base/'SourceTextures'; exports=base/'Exports'; proof=root/'Logs/LimestoneRock01/WhiteV2'
proof.mkdir(parents=True,exist_ok=True)
assert Path(bpy.data.filepath).resolve()==scene_path.resolve()
provenance=json.loads((base/'source_provenance.json').read_text())
assert hashlib.sha256((source_dir/'WhiteLimestone_ApprovedAlbedo.png').read_bytes()).hexdigest()==provenance['copied_albedo_sha256']
def export_meshes(meshes,path):
    if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
    locations=[o.location.copy() for o in meshes]
    visibility=[(o.hide_get(),o.hide_select) for o in meshes]
    for obj in bpy.context.scene.objects: obj.select_set(False)
    for obj in meshes:
        obj.hide_set(False); obj.hide_select=False; obj.location=(0,0,0); obj.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0]
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},global_scale=1,
                            apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
                            use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
    for obj,location,(hidden,locked) in zip(meshes,locations,visibility):
        obj.location=location; obj.hide_set(hidden); obj.hide_select=locked
if len(sys.argv[sys.argv.index('--')+1:])>1 and sys.argv[-1]=='refresh-export':
    report=json.loads((base/'application.json').read_text())
    assert hashlib.sha256(scene_path.read_bytes()).hexdigest()==report['after_scene_sha256']
    export_meshes([bpy.data.objects[n] for n in report['meshes']],exports/'LimestoneRock01_WhiteV2.fbx')
    report['exported_files']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in exports.iterdir()}
    (base/'application.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
    print('Refreshed the four-mesh FBX. Original scene not saved or changed.'); sys.exit(0)
assert not (base/'application.json').exists(), 'This material version is already applied'
input_hash=hashlib.sha256(scene_path.read_bytes()).hexdigest()
manifest=dict(scene_sha256=input_hash,albedo_sha256=provenance['copied_albedo_sha256'])
attempt=base/'bake_input.json'
if attempt.exists(): assert json.loads(attempt.read_text())==manifest, 'Scene/source changed during a partial bake'
else: attempt.write_text(json.dumps(manifest,indent=2)+'\n',newline='\n')
backup=scene_path.parent/'BooterGroundSetup_BeforeWhiteLimestone_2026-10-12.blend'
if backup.exists(): assert hashlib.sha256(backup.read_bytes()).hexdigest()==input_hash
else: shutil.copy2(scene_path,backup)
exports.mkdir(parents=True,exist_ok=True)
scene=bpy.context.scene
names=['LimestoneRock01_LOD0','LimestoneRock01_LOD1','LimestoneRock01_LOD2','LimestoneRock01_Collision']
meshes=[bpy.data.objects[n] for n in names]; low=meshes[0]; artist=bpy.data.objects['Icosphere']
def signature(obj,material=True):
    h=hashlib.sha256(); h.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        v=np.empty(len(obj.data.vertices)*3,dtype=np.float32); obj.data.vertices.foreach_get('co',v); h.update(v.tobytes())
        for p in obj.data.polygons: h.update(str(tuple(p.vertices)).encode())
        for uv in obj.data.uv_layers:
            data=np.empty(len(uv.data)*2,dtype=np.float32); uv.data.foreach_get('uv',data); h.update(data.tobytes())
        if material: h.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return h.hexdigest()
before={o.name:signature(o,o.name not in names[:3]) for o in scene.objects}
scene_names=[s.name for s in bpy.data.scenes]
visibility={o:(o.hide_get(),o.hide_render,o.hide_select) for o in scene.objects}
render_saved={k:getattr(scene.render,k) for k in ['engine','resolution_x','resolution_y','resolution_percentage','filepath']}
cycle_saved={k:getattr(scene.cycles,k) for k in ['device','samples']}
bake_saved={k:getattr(scene.render.bake,k) for k in ['margin','margin_type','use_clear','use_selected_to_active','cage_extrusion','max_ray_distance']}
camera_saved=(scene.camera.matrix_world.copy(),scene.camera.data.type,scene.camera.data.ortho_scale)
light_saved={o:o.matrix_world.copy() for o in scene.objects if o.type=='LIGHT'}
def active(obj):
    if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT'); obj.hide_set(False); obj.hide_select=False
    obj.select_set(True); bpy.context.view_layer.objects.active=obj
high=artist.copy(); high.data=artist.data.copy(); scene.collection.objects.link(high); high.name='White limestone temporary bake source'
transform=low.matrix_world.inverted()@artist.matrix_world
high.data.transform(transform); high.matrix_world=low.matrix_world.copy()
for p in high.data.polygons: p.use_smooth=True
active(low)
source_mat=bpy.data.materials.new('Approved white limestone bake source'); source_mat.use_nodes=True
n,l=source_mat.node_tree.nodes,source_mat.node_tree.links; n.clear()
out=n.new('ShaderNodeOutputMaterial'); shader=n.new('ShaderNodeBsdfPrincipled'); l.new(shader.outputs['BSDF'],out.inputs['Surface'])
coord=n.new('ShaderNodeTexCoord'); scale=n.new('ShaderNodeVectorMath'); scale.operation='SCALE'; scale.inputs[3].default_value=1/1.1
l.new(coord.outputs['Object'],scale.inputs[0])
nodes={}
for kind,file in [('BaseColor','WhiteLimestone_ApprovedAlbedo.png'),('Height','WhiteLimestone_Height16.png'),('Roughness','WhiteLimestone_Roughness.png'),('Occlusion','WhiteLimestone_Occlusion.png')]:
    node=n.new('ShaderNodeTexImage'); node.image=bpy.data.images.load(str(source_dir/file),check_existing=True)
    node.image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
    node.projection='BOX'; node.projection_blend=.25; l.new(scale.outputs['Vector'],node.inputs['Vector']); nodes[kind]=node
l.new(nodes['BaseColor'].outputs['Color'],shader.inputs['Base Color']); l.new(nodes['Roughness'].outputs['Color'],shader.inputs['Roughness'])
bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=1; bump.inputs['Distance'].default_value=.0006
l.new(nodes['Height'].outputs['Color'],bump.inputs['Height']); l.new(bump.outputs['Normal'],shader.inputs['Normal'])
high.data.materials.clear(); high.data.materials.append(source_mat)
target_mat=bpy.data.materials.new('White limestone temporary bake target'); target_mat.use_nodes=True
low.data.materials.clear(); low.data.materials.append(target_mat)
target=target_mat.node_tree.nodes.new('ShaderNodeTexImage'); target_mat.node_tree.nodes.active=target
scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=16
scene.render.bake.margin=24; scene.render.bake.margin_type='EXTEND'; scene.render.bake.use_clear=True
emission=n.new('ShaderNodeEmission'); maps={}; size=2048
def bake(kind,socket=None):
    path=exports/f'LimestoneRock01_WhiteV2_{kind}.png'
    if path.exists():
        image=bpy.data.images.load(str(path),check_existing=True)
        image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'; maps[kind]=image
        assert tuple(image.size)==(size,size)
        if kind=='Normal': l.new(shader.outputs['BSDF'],out.inputs['Surface'])
        print('Reused completed '+kind,flush=True); return
    image=bpy.data.images.new('LimestoneRock01_WhiteV2_'+kind,width=size,height=size,alpha=False)
    image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
    target.image=image; target_mat.node_tree.nodes.active=target
    if socket:
        l.new(socket,emission.inputs['Color']); l.new(emission.outputs['Emission'],out.inputs['Surface'])
    else: l.new(shader.outputs['BSDF'],out.inputs['Surface'])
    active(low); high.hide_render=False; high.hide_set(False); high.select_set(True)
    bpy.ops.object.bake(type='NORMAL' if kind=='Normal' else 'EMIT',use_selected_to_active=True,normal_space='TANGENT',cage_extrusion=.035,max_ray_distance=.08)
    image.filepath_raw=str(path); image.file_format='PNG'; image.save(); maps[kind]=image; print('Baked '+kind,flush=True)
bake('BaseColor',nodes['BaseColor'].outputs['Color']); bake('Roughness',nodes['Roughness'].outputs['Color'])
ao=n.new('ShaderNodeAmbientOcclusion'); ao.inputs['Distance'].default_value=.15; ao.samples=16
multiply=n.new('ShaderNodeMath'); multiply.operation='MULTIPLY'
l.new(nodes['Occlusion'].outputs['Color'],multiply.inputs[0]); l.new(ao.outputs['AO'],multiply.inputs[1])
bake('Occlusion',multiply.outputs[0]); bake('Height',nodes['Height'].outputs['Color']); bake('Normal')
rough=np.empty(size*size*4,dtype=np.float32); maps['Roughness'].pixels.foreach_get(rough)
packed=np.zeros((size*size,4),dtype=np.float32); packed[:,3]=1-rough.reshape(-1,4)[:,0]
image=bpy.data.images.new('LimestoneRock01_WhiteV2_MetallicSmoothness',width=size,height=size,alpha=True)
image.colorspace_settings.name='Non-Color'; image.pixels.foreach_set(packed.ravel())
image.filepath_raw=str(exports/'LimestoneRock01_WhiteV2_MetallicSmoothness.png'); image.file_format='PNG'; image.save(); maps['MetallicSmoothness']=image
game=bpy.data.materials.new('LimestoneRock01 - approved white limestone'); game.use_nodes=True
gn,gl=game.node_tree.nodes,game.node_tree.links; bsdf=gn.get('Principled BSDF')
for kind,socket in [('BaseColor','Base Color'),('Roughness','Roughness')]:
    node=gn.new('ShaderNodeTexImage'); node.image=maps[kind]; gl.new(node.outputs['Color'],bsdf.inputs[socket])
node=gn.new('ShaderNodeTexImage'); node.image=maps['Normal']; normal=gn.new('ShaderNodeNormalMap')
gl.new(node.outputs['Color'],normal.inputs['Color']); gl.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
for obj in meshes[:3]: obj.data.materials.clear(); obj.data.materials.append(game)
export_meshes(meshes,exports/'LimestoneRock01_WhiteV2.fbx')
high_mesh=high.data; bpy.data.objects.remove(high,do_unlink=True); bpy.data.meshes.remove(high_mesh)
bpy.data.materials.remove(source_mat); bpy.data.materials.remove(target_mat)
for o,(hidden,render_hidden,locked) in visibility.items(): o.hide_set(hidden); o.hide_render=render_hidden; o.hide_select=locked
for k,v in render_saved.items(): setattr(scene.render,k,v)
for k,v in cycle_saved.items(): setattr(scene.cycles,k,v)
for k,v in bake_saved.items(): setattr(scene.render.bake,k,v)
active(low)
assert before=={o.name:signature(o,o.name not in names[:3]) for o in scene.objects}, 'Existing geometry, UVs, transforms or unrelated materials changed'
assert scene_names==[s.name for s in bpy.data.scenes]
for kind,image in maps.items(): image.name='LimestoneRock01_WhiteV2_'+kind; image.use_fake_user=True; image.pack()
note=bpy.data.texts.get('Approved white limestone') or bpy.data.texts.new('Approved white limestone')
note.write('Approved white limestone with fine grit and a smooth worn surface.\nThe scene, 712-triangle rock, player and ground are retained.\nThe material uses the exact approved generated albedo; roughness/relief are artistic interpretations.\nSix 2K maps and the matching FBX are in LimestoneRock01/ApprovedWhiteV2/Exports.\n')
bpy.ops.wm.save_as_mainfile(filepath=str(scene_path),check_existing=False)
report=dict(scene=str(scene_path),backup=str(backup),before_scene_sha256=input_hash,after_scene_sha256=hashlib.sha256(scene_path.read_bytes()).hexdigest(),
            unchanged_object_signatures=before,approved_albedo_sha256=provenance['copied_albedo_sha256'],material=game.name,
            scene_count=len(bpy.data.scenes),texture_size=size,meshes=names,source='Approved generated preview; prior workbench-derived surface replaced',
            geometry_changed=False,projection_tile_metres=1.1,microrelief_amplitude_metres=.0006,
            exported_files={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in exports.iterdir()})
(base/'application.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print('Saved approved white limestone in the original scene. Geometry and other scene contents unchanged.')
# Render transient views after saving; do not serialize camera/render overrides.
scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=1100; scene.render.resolution_y=800; scene.render.resolution_percentage=100
scene.render.filepath=str(proof/'same-scene.png'); bpy.ops.render.render(write_still=True)
for o in scene.objects:
    if o.type=='MESH': o.hide_render=o!=low
target=low.location+Vector((0,0,low.dimensions.z/2)); scene.camera.data.type='ORTHO'; scene.camera.data.ortho_scale=max(low.dimensions)*1.5
scene.camera.location=target+Vector((3,-4,2.5)).normalized()*8
scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
for o in light_saved:
    o.location=target+Vector((2,-4,6) if o.name=='Key' else (-4,2,4))
    o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(proof/'rock-closeup.png'); bpy.ops.render.render(write_still=True)
print('Transient renders completed; the original camera and render settings are retained in the saved scene.')
