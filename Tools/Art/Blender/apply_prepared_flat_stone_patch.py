"""Apply the reviewed autosave-derived patch to the author's current saved scene.
Blender --background --disable-autoexec BooterGroundSetup.blend --python THIS -- ROOT
Refuses changed selections/geometry, stale writes and pre-existing runtime exports.
"""
import datetime,hashlib,json,shutil,sys
from pathlib import Path
import bpy
import numpy as np

root=Path(sys.argv[sys.argv.index('--')+1]).resolve(); asset='FlatStoneGroundPatch01'
scene_path=root/'SourceArt/Blender/Studies/BooterGroundSetup/BooterGroundSetup.blend'
proof=root/'Logs'/asset; docs=scene_path.parent/asset
exports=root/'Assets/_Project/Art/Environment/Ground'/asset
assert Path(bpy.data.filepath).resolve()==scene_path.resolve()
report=json.loads((proof/'preparation.json').read_text())
resume_after_save='--resume-after-artist-save' in sys.argv
assert asset not in bpy.data.objects, 'The patch is already in this scene'
assert resume_after_save or not (docs/'application.json').exists(), 'Use the reviewed late-save restoration path explicitly'
# The live autosave identifies the author's selection. A saved scene may differ
# only in selection; exact geometry/UV/material/transform signatures below guard it.
assert all(n in bpy.data.objects for n in report['source_selection'])
selected=[bpy.data.objects[n] for n in report['source_selection']]
def signature(obj):
    digest=hashlib.sha256(); digest.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        coords=np.empty(len(obj.data.vertices)*3,dtype=np.float32); obj.data.vertices.foreach_get('co',coords); digest.update(coords.tobytes())
        for p in obj.data.polygons: digest.update(str(tuple(p.vertices)).encode())
        for uv in obj.data.uv_layers:
            data=np.empty(len(uv.data)*2,dtype=np.float32); uv.data.foreach_get('uv',data); digest.update(data.tobytes())
        digest.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return digest.hexdigest()
assert {o.name:signature(o) for o in selected}==report['original_selected_signatures'], 'Artist assembly changed since preparation; rerun reduction/bake on the new save'
scene=bpy.context.scene; before={o.name:signature(o) for o in scene.objects}
scene_names=[s.name for s in bpy.data.scenes]
input_hash=hashlib.sha256(scene_path.read_bytes()).hexdigest()
backup=scene_path.parent/'BooterGroundSetup_BeforeFlatStonePatch_2026-10-13.blend'
if backup.exists() and hashlib.sha256(backup.read_bytes()).hexdigest()!=input_hash:
    assert resume_after_save, 'A different artist save must be retained explicitly'
    stamp=datetime.datetime.fromtimestamp(scene_path.stat().st_mtime).strftime('%H%M%S')
    backup=scene_path.parent/f'BooterGroundSetup_BeforeFlatStonePatch_2026-10-13_AfterArtistSave_{stamp}.blend'
if backup.exists(): assert hashlib.sha256(backup.read_bytes()).hexdigest()==input_hash
else: shutil.copy2(scene_path,backup)
exports.mkdir(parents=True,exist_ok=True)
for name,digest in report['exported_files'].items():
    src=proof/'PreparedExports'/name; dst=exports/name
    assert hashlib.sha256(src.read_bytes()).hexdigest()==digest
    if dst.exists(): assert hashlib.sha256(dst.read_bytes()).hexdigest()==digest
    else: shutil.copy2(src,dst)
with bpy.data.libraries.load(str(proof/'prepared_patch.blend'),link=False) as (source,destination):
    assert asset in source.objects; destination.objects=[asset]
patch=destination.objects[0]; scene.collection.objects.link(patch)
bpy.context.view_layer.update()
patch.asset_mark()
patch.asset_data.description='Reusable 23-stone limestone ground patch with a fused 25 mm base and ground-surface pivot'
for tag in ['Environment','Ground','Limestone','Static']: patch.asset_data.tags.new(tag)
for node in patch.data.materials[0].node_tree.nodes:
    if node.type=='TEX_IMAGE' and node.image:
        image=node.image
        kind=next(k for k in ['BaseColor','Roughness','Normal'] if k in image.name)
        image.name=asset+'_'+kind
        target=docs/(asset+'_Roughness.png') if 'Roughness' in image.name else exports/(image.name+'.png')
        assert target.exists(); image.filepath=str(target); image.pack()
assert before=={o.name:signature(o) for o in scene.objects if o.name in before}
assert len(scene.objects)==len(before)+1 and scene_names==[s.name for s in bpy.data.scenes]
for o in scene.objects: o.select_set(False)
for o in selected: o.hide_set(True); o.hide_render=True
patch.hide_set(False); patch.hide_render=False; patch.hide_select=False; patch.select_set(True)
bpy.context.view_layer.objects.active=patch
note=bpy.data.texts.new(asset+' - editing and export')
note.write('Selected: one closed, fused reusable ground-patch mesh. Original stones and plane remain hidden and editable.\nPivot is the original plane centre at its top surface; solid base extends 25 mm downward. Move/rotate/duplicate this single object.\nRuntime FBX, URP atlas and static Unity prefab are under Assets/_Project.\nReload this existing scene in Blender with File > Revert after background authoring; save new edits before any reload.\n')
assert hashlib.sha256(scene_path.read_bytes()).hexdigest()==input_hash, 'Artist saved during application; do not overwrite their new file'
bpy.ops.wm.save_as_mainfile(filepath=str(scene_path),check_existing=False)
report.update(scene=str(scene_path),backup=str(backup),before_scene_sha256=input_hash,
              after_scene_sha256=hashlib.sha256(scene_path.read_bytes()).hexdigest(),unchanged_object_signatures=before,
              scene_count=len(bpy.data.scenes),selected_object=patch.name)
report['resumed_after_artist_save']=resume_after_save
(docs/'application.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print('APPLIED TO ORIGINAL SCENE; ALL ORIGINAL OBJECT SIGNATURES UNCHANGED',flush=True)
