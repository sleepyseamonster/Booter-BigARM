"""Replace only the paving mesh in the existing scene, preserving latest verified object pose."""
import hashlib,json,shutil,sys
from pathlib import Path
import bpy,numpy as np
from mathutils import Matrix
root=Path(sys.argv[sys.argv.index('--')+1]).resolve();asset='FlatStoneGroundPatch01'
proof=root/'Logs'/asset/'Repair01';docs=root/'SourceArt/Blender/Studies/BooterGroundSetup'/asset
scene_path=root/'SourceArt/Blender/Studies/BooterGroundSetup/BooterGroundSetup.blend'
exports=root/'Assets/_Project/Art/Environment/Ground'/asset
assert Path(bpy.data.filepath).resolve()==scene_path.resolve()
report=json.loads((proof/'preparation.json').read_text())
saved=json.loads((proof/'latest-saved-inspection.json').read_text());auto=json.loads((proof/'latest-autosave-inspection.json').read_text())
assert saved['patch_geometry_signature']==auto['patch_geometry_signature']
assert saved['other_signatures']==auto['other_signatures'], 'Unrelated unsaved changes require a fresh author save'
def signature(obj):
    h=hashlib.sha256();h.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        a=np.empty(len(obj.data.vertices)*3,dtype=np.float32);obj.data.vertices.foreach_get('co',a);h.update(a.tobytes())
        for p in obj.data.polygons:h.update(str(tuple(p.vertices)).encode())
        for uv in obj.data.uv_layers:
            a=np.empty(len(uv.data)*2,dtype=np.float32);uv.data.foreach_get('uv',a);h.update(a.tobytes())
        h.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return h.hexdigest()
old=bpy.data.objects[asset];before={o.name:signature(o) for o in bpy.context.scene.objects if o!=old}
assert before==report['original_object_signatures']
input_hash=hashlib.sha256(scene_path.read_bytes()).hexdigest();assert input_hash==report['input_scene_sha256']
backup=scene_path.parent/'BooterGroundSetup_BeforeFlatStoneRepair01_2026-10-13.blend'
if backup.exists():assert hashlib.sha256(backup.read_bytes()).hexdigest()==input_hash
else:shutil.copy2(scene_path,backup)
auto_path=Path(auto['file']);auto_backup=scene_path.parent/'BooterGroundSetup_ArtistAutosaveBeforeRepair01_2026-10-13.blend'
if not auto_backup.exists():shutil.copy2(auto_path,auto_backup)
with bpy.data.libraries.load(str(proof/'supported_patch.blend'),link=False) as (src,dst):dst.objects=[n for n in src.objects if n.startswith('Supported tile repair')]
assert len(dst.objects)==1;new=dst.objects[0]
if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
old.data=new.data
for group in new.vertex_groups:
    if group.name not in old.vertex_groups:old.vertex_groups.new(name=group.name)
old.matrix_world=Matrix(auto['matrix'])
bpy.data.objects.remove(new,do_unlink=True)
old.data.name=asset+' - supported repair 01';old['Repair version']=1
old['Repair method']='Solid heightfield tops and vertical support to the base; contact vertices protected during simplification'
old.asset_data.description='Reusable supported limestone paving patch; repaired stone/base joins'
for image in [n.image for m in old.data.materials for n in m.node_tree.nodes if n.type=='TEX_IMAGE']:image.pack()
for obj in bpy.context.scene.objects:obj.select_set(False)
old.hide_set(False);old.hide_render=False;old.hide_select=False;old.select_set(True);bpy.context.view_layer.objects.active=old
bpy.context.view_layer.update()
assert before=={n:signature(bpy.data.objects[n]) for n in before}
note=bpy.data.texts.new(asset+' - repair 01')
note.write('Repair 01 fills the stone-to-base undercuts with solid limestone support. 8,000 triangles; contact boundaries are protected.\\nTwo material slots retain the gray base and approved white limestone. No cage-bake UV holes.\\nOriginal source stones are unchanged and hidden. The latest autosaved patch pose is preserved.\\nLoad background changes with File > Revert before saving again.\\n')
pose=old.matrix_world.copy();old.matrix_world=Matrix.Identity(4);bpy.context.view_layer.update()
bpy.ops.export_scene.fbx(filepath=str(exports/(asset+'.fbx')),use_selection=True,object_types={'MESH'},global_scale=1,
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,
    mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
old.matrix_world=pose;bpy.context.view_layer.update()
ground=old.data.materials[0].node_tree.nodes.get('Principled BSDF')
report['ground_color_linear']=list(ground.inputs['Base Color'].default_value);report['ground_roughness']=ground.inputs['Roughness'].default_value
assert hashlib.sha256(scene_path.read_bytes()).hexdigest()==input_hash
bpy.ops.wm.save_as_mainfile(filepath=str(scene_path),check_existing=False)
report.update(scene=str(scene_path),backup=str(backup),artist_autosave_backup=str(auto_backup),preserved_pose=auto['matrix'],
    after_scene_sha256=hashlib.sha256(scene_path.read_bytes()).hexdigest(),fbx_sha256=hashlib.sha256((exports/(asset+'.fbx')).read_bytes()).hexdigest())
(docs/'repair01.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print('REPAIR APPLIED TO SAME SCENE; ORIGINAL SOURCES AND LATEST OBJECT POSE PRESERVED',flush=True)
