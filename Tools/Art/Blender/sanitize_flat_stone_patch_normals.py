"""Use geometry normals where edge projection produced inward-facing tangent normals.
Only the task's normal data/cache changes. Does not save the original scene.
"""
import hashlib,json,sys
from pathlib import Path
import bpy,numpy as np
root=Path(sys.argv[sys.argv.index('--')+1]).resolve(); proof=root/'Logs/FlatStoneGroundPatch01'; asset='FlatStoneGroundPatch01'
report=json.loads((proof/'preparation.json').read_text())
with bpy.data.libraries.load(str(proof/'prepared_patch.blend'),link=False) as (src,dst): dst.objects=[asset]
patch=dst.objects[0]; bpy.context.scene.collection.objects.link(patch); bpy.context.view_layer.update()
image=next(n.image for n in patch.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE' and 'Normal' in n.image.name)
pixels=np.empty(4096*4096*4,dtype=np.float32); image.pixels.foreach_get(pixels); pixels=pixels.reshape(-1,4)
normals=pixels[:,:3]*2-1; bad=normals[:,2]<.25
factor=np.clip(normals[bad,2]/.25,0,1); factor=factor*factor*(3-2*factor)
corrected=normals[bad]*factor[:,None]; corrected[:,2]+=1-factor
corrected/=np.maximum(np.linalg.norm(corrected,axis=1,keepdims=True),1e-8)
pixels[bad,:3]=(corrected+1)/2
image.pixels.foreach_set(pixels.ravel()); image.filepath_raw=str(proof/'PreparedExports'/f'{asset}_Normal.png'); image.file_format='PNG'; image.save(); image.pack()
bpy.data.libraries.write(str(proof/'prepared_patch.blend'),{patch},fake_user=True)
report['normal_edge_cleanup']=dict(affected_atlas_pixels=int(np.count_nonzero(bad)),atlas_fraction=float(np.mean(bad)),
    method='Inward tangent normals fall back to geometry normals; smooth transition to the untouched bake over tangent Z=0..0.25')
report['exported_files'][asset+'_Normal.png']=hashlib.sha256(Path(image.filepath).read_bytes()).hexdigest()
(proof/'preparation.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print(json.dumps(report['normal_edge_cleanup']),flush=True)
