import bpy,json,sys,hashlib
from pathlib import Path
ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve();HERE=Path(__file__).resolve().parent;r=json.loads((HERE/'receipt.json').read_text())
path=ROOT/r['native'];assert hashlib.sha256(path.read_bytes()).hexdigest()==r['native_sha256']
bpy.ops.wm.open_mainfile(filepath=str(path))
images=[i for i in bpy.data.images if i.source=='FILE'];assert len(images)==25 and all(i.packed_file for i in images)
for f in r['families']:
    assert bpy.data.materials.get(f+'_Authoring')
    for s in r['styles']:
        m=bpy.data.materials[f+'_'+s+'_Baked'];assert len([n for n in m.node_tree.nodes if n.type=='TEX_IMAGE'])==3
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
SC=bpy.context.scene;SC.cycles.device='GPU';out=ROOT/'outputs/Sable/MaterialDioramas20261015/OrganicLibrary/reopened.png';SC.render.filepath=str(out);bpy.ops.render.render(write_still=True)
(HERE/'native_verification.json').write_text(json.dumps({'native_hash_match':True,'packed_images':25,'authoring_materials':5,'baked_materials':15,'reopened_capture':str(out.relative_to(ROOT)),'reference':'WornLeather_E.png','local_source':r['native']},indent=2),encoding='utf-8',newline='\n')
print('LIBRARY_NATIVE_VERIFIED',flush=True)
