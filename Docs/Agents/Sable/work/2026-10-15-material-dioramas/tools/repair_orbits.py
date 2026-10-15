"""Extend only the distant background apron, then recapture matching rear-orbit groups.
Foreground meshes, materials, camera pose and lighting remain unchanged.
Uses the sealed common source; updates raw receipt and retains the repair script hash.
"""
import bpy,ast,sys,json,hashlib,time
from pathlib import Path
from mathutils import Matrix
ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve();HERE=Path(__file__).resolve().parent.parent
OUT=ROOT/'outputs/Sable/MaterialDioramas20261015';LOCAL=ROOT/'SourceArt/Blender/Studies/Sable/MaterialDioramas20261015'
receipt=json.loads((OUT/'render_receipt.json').read_text(encoding='utf-8'))
SC=bpy.context.scene;CAM=SC.camera
KEY=bpy.data.objects['Broad reflection key'];FILL=bpy.data.objects['Broad sky fill'];RIM=bpy.data.objects['Rim bounce'];SUN=bpy.data.objects['Low sunset'];sd=SUN.data
lightfunc=next(n for n in ast.parse((HERE/'tools/build_study.py').read_text(encoding='utf-8')).body if isinstance(n,ast.FunctionDef) and n.name=='lights')
exec(compile(ast.Module(body=[lightfunc],type_ignores=[]),'capture lighting recipe','exec'),globals())
bpy.ops.mesh.primitive_plane_add(size=4000,location=(0,0,-.10));apron=bpy.context.object;apron.name='Distant ground apron';apron.data.materials.append(bpy.data.materials['B_G01'])
CAM.data.clip_end=5000
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
SC.cycles.device='GPU'
for row in receipt['renders']:
    if row['kind']!='controlled' or row['view']!='orbit':continue
    for collection in ['yard','gate','quarry','plates','control']:
        for ob in bpy.data.collections[collection].objects:
            ob.hide_render=collection!=row['scene']
            if ob.get('surface_id'):
                mid=ob['surface_id'];name=f"{row['style']}_{mid}"
                if ob.get('role')=='Ground deposit':name+=' faded deposits'
                ob.data.materials[0]=bpy.data.materials[name]
    apron.data.materials[0]=bpy.data.materials[f"{row['style']}_G01"]
    RIM.data.color=(1,.72,.42);lights(row['light']);CAM.matrix_world=Matrix(row['camera']['matrix']);bpy.context.view_layer.update()
    path=OUT/'renders'/row['file'];SC.render.filepath=str(path);start=time.time();bpy.ops.render.render(write_still=True)
    row['replaced_stage_edge_sha256']=row['sha256'];row['sha256']=hashlib.sha256(path.read_bytes()).hexdigest();row['repair_seconds']=time.time()-start;row['background_apron_m']=4000;row['far_clip_m']=5000
    print('SABLE_ORBIT_REPAIRED',row['file'],flush=True)
receipt['background_repair']={'frames':24,'method':'4000 m background-only apron below the original ground, matching G01; far clip extended to 5000 m. Foreground geometry fingerprints and camera poses unchanged. No terrain integration. Other views retain their original capture background.','script_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}
(OUT/'render_receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
