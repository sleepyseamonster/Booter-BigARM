"""Put the finished rock into the existing BooterGroundSetup.blend.
Run in background on the original file. Keeps a local artist-source backup.
Only appends the four asset objects, hides the sculpt, and selects the finished rock.
"""
import hashlib, json, shutil, sys
from pathlib import Path
import bpy
import numpy as np

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
base=root/'SourceArt/Blender/Studies/BooterGroundSetup'
original=base/'BooterGroundSetup.blend'
asset=base/'LimestoneRock01'
delivery=json.loads((asset/'delivery.json').read_text())
assert Path(bpy.data.filepath).resolve()==original.resolve()
assert hashlib.sha256(original.read_bytes()).hexdigest()==delivery['source_sha256'], 'Original changed since the saved sculpt; inspect before integration'
backup=base/'BooterGroundSetup_ArtistSource_2026-10-12.blend'
if backup.exists():
    assert hashlib.sha256(backup.read_bytes()).hexdigest()==delivery['source_sha256']
else: shutil.copy2(original,backup)

def signature(obj):
    digest=hashlib.sha256()
    digest.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        vertices=np.empty(len(obj.data.vertices)*3,dtype=np.float32)
        obj.data.vertices.foreach_get('co',vertices); digest.update(vertices.tobytes())
        for p in obj.data.polygons: digest.update(str(tuple(p.vertices)).encode())
        digest.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return digest.hexdigest()

before={o.name:signature(o) for o in bpy.context.scene.objects}
scenes_before=list(bpy.data.scenes)
if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
names=list(delivery['meshes'])
assert not any(n in bpy.data.objects for n in names), 'Rock is already integrated'
with bpy.data.libraries.load(str(asset/'LimestoneRock01_GameReady.blend'),link=False) as (available,loaded):
    assert all(n in available.objects for n in names)
    loaded.objects=names.copy()
collection=bpy.data.collections.new('Limestone Rock - optimized asset')
bpy.context.scene.collection.children.link(collection)
for obj in loaded.objects:
    collection.objects.link(obj)
    obj.hide_set(obj.name!='LimestoneRock01_LOD0')
    obj.hide_render=obj.name!='LimestoneRock01_LOD0'
    obj.hide_select=False
sculpt=bpy.data.objects['Icosphere']
sculpt.hide_set(True); sculpt.hide_render=True
low=bpy.data.objects['LimestoneRock01_LOD0']
bpy.ops.object.select_all(action='DESELECT')
low.select_set(True); low.hide_set(False)
bpy.context.view_layer.objects.active=low
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D': area.spaces.active.shading.type='MATERIAL'
if bpy.context.window: bpy.context.window.workspace=bpy.data.workspaces.get('Layout',bpy.context.window.workspace)
assert scenes_before==list(bpy.data.scenes), 'Integration created an extra scene'
assert before=={n:signature(bpy.data.objects[n]) for n in before}, 'Existing geometry, transforms or material bindings changed'
bpy.ops.wm.save_as_mainfile(filepath=str(original),check_existing=False)
report=dict(original_file=str(original),artist_backup=str(backup),artist_source_sha256=delivery['source_sha256'],
            integrated_file_sha256=hashlib.sha256(original.read_bytes()).hexdigest(),appended_objects=names,
            existing_objects_unchanged=before,scene_count=len(bpy.data.scenes),selected=low.name,
            original_sculpt_hidden=True,new_project_created=False)
(asset/'same_scene_integration.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
delivery['artist_source_path']=str(backup)
(asset/'delivery.json').write_text(json.dumps(delivery,indent=2)+'\n',newline='\n')
print(json.dumps({k:v for k,v in report.items() if k!='existing_objects_unchanged'},indent=2))
