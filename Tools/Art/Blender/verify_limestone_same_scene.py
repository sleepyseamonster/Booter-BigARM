"""Verify the original scene contains the finished rock without altering its other contents."""
import hashlib, json, sys
from pathlib import Path
import bpy
import numpy as np
root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
base=root/'SourceArt/Blender/Studies/BooterGroundSetup'
asset=base/'LimestoneRock01'
delivery=json.loads((asset/'delivery.json').read_text())
backup=base/'BooterGroundSetup_ArtistSource_2026-10-12.blend'
original=base/'BooterGroundSetup.blend'
assert hashlib.sha256(backup.read_bytes()).hexdigest()==delivery['source_sha256']
def signature(obj):
    digest=hashlib.sha256(); digest.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        vertices=np.empty(len(obj.data.vertices)*3,dtype=np.float32)
        obj.data.vertices.foreach_get('co',vertices); digest.update(vertices.tobytes())
        for p in obj.data.polygons: digest.update(str(tuple(p.vertices)).encode())
        digest.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return digest.hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(backup))
before={o.name:signature(o) for o in bpy.context.scene.objects}
scenes_before=[s.name for s in bpy.data.scenes]
bpy.ops.wm.open_mainfile(filepath=str(original))
assert scenes_before==[s.name for s in bpy.data.scenes]
assert before=={n:signature(bpy.data.objects[n]) for n in before}
names=list(delivery['meshes'])
assert set(o.name for o in bpy.context.scene.objects)==set(before)|set(names)
low=bpy.data.objects['LimestoneRock01_LOD0']
low.data.calc_loop_triangles()
assert len(low.data.vertices)==358 and len(low.data.loop_triangles)==712
assert low.data.uv_layers.active and low.data.materials[0].name.startswith('LimestoneRock01')
assert not low.hide_get() and not low.hide_render
assert bpy.data.objects['Icosphere'].hide_get() and bpy.data.objects['Icosphere'].hide_render
assert bpy.context.view_layer.objects.active==low
for n in names[1:]: assert bpy.data.objects[n].hide_get() and bpy.data.objects[n].hide_render
report=dict(original_file=str(original),artist_backup=str(backup),artist_source_sha256=delivery['source_sha256'],
            integrated_file_sha256=hashlib.sha256(original.read_bytes()).hexdigest(),appended_objects=names,
            existing_objects_unchanged=before,scene_count=len(bpy.data.scenes),selected=low.name,
            original_sculpt_hidden=True,new_project_created=False,reopen_verification_passed=True)
(asset/'same_scene_integration.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
delivery['artist_source_path']=str(backup)
(asset/'delivery.json').write_text(json.dumps(delivery,indent=2)+'\n',newline='\n')
print(json.dumps({k:v for k,v in report.items() if k!='existing_objects_unchanged'},indent=2))
