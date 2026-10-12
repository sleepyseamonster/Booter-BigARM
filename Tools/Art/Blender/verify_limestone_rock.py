"""Read-only verification of the saved limestone asset and its FBX round trip.
Blender --background --disable-autoexec DELIVERY --python THIS -- ROOT
"""
import hashlib, json, math, sys
from pathlib import Path
import bpy, bmesh
import numpy as np
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
base=root/'SourceArt/Blender/Studies/BooterGroundSetup/LimestoneRock01'
proof=root/'Logs/LimestoneRock01'
delivery=json.loads((base/'delivery.json').read_text())
assert Path(bpy.data.filepath).resolve()==Path(delivery['output']).resolve()
assert hashlib.sha256((base.parent/'BooterGroundSetup.blend').read_bytes()).hexdigest()==delivery['source_sha256']
for name,digest in delivery['texture_sources'].items():
    assert hashlib.sha256((root/'Assets/_Project/Art/Environment/Rocks/Workbench/Layered'/name).read_bytes()).hexdigest()==digest
report={'blender':bpy.app.version_string,'source_and_workbench_textures_unchanged':True,'meshes':{},'maps':{}}
expected={}
for name,stats in delivery['meshes'].items():
    obj=bpy.data.objects[name]; mesh=obj.data; mesh.calc_loop_triangles()
    assert len(mesh.vertices)==stats['vertices'] and len(mesh.loop_triangles)==stats['triangles']
    bm=bmesh.new(); bm.from_mesh(mesh)
    assert all(e.is_manifold for e in bm.edges)
    assert all(v.link_edges for v in bm.verts)
    assert all(f.calc_area()>1e-12 for f in bm.faces)
    assert bm.calc_volume()>0
    bm.free()
    assert obj.scale==Vector((1,1,1))
    if 'Collision' not in name:
        uv=mesh.uv_layers.active
        assert uv and all(math.isfinite(x) and 0<=x<=1 for loop in uv.data for x in loop.uv)
    expected[name]={'triangles':len(mesh.loop_triangles),'dimensions':list(obj.dimensions),'vertices':len(mesh.vertices)}
    report['meshes'][name]=expected[name]

low=bpy.data.objects['LimestoneRock01_LOD0']
size=delivery['texture_size']
for name in ('BaseColor','Roughness','Occlusion','Height','Normal','MetallicSmoothness'):
    image=bpy.data.images.get('LimestoneRock01_'+name)
    if image is None: image=bpy.data.images.load(str(base/'Exports'/f'LimestoneRock01_{name}.png'))
    assert tuple(image.size)==(size,size)
    pixels=np.empty(size*size*4,dtype=np.float32); image.pixels.foreach_get(pixels)
    pixels=pixels.reshape(size,size,4)
    assert np.isfinite(pixels).all()
    uv=low.data.uv_layers.active
    values=[]
    for triangle in low.data.loop_triangles:
        coords=[uv.data[i].uv for i in triangle.loops]
        for weights in ((1/3,1/3,1/3),(.6,.2,.2),(.2,.6,.2),(.2,.2,.6)):
            point=sum((p*w for p,w in zip(coords,weights)),Vector((0,0)))
            values.append(pixels[min(size-1,int(point.y*size)),min(size-1,int(point.x*size)),:])
    values=np.array(values)
    if name=='Normal':
        vectors=values[:,:3]*2-1
        assert np.all(values[:,2]>.5), 'Normal bake missed a UV sample or points backwards'
        assert np.all(np.abs(np.linalg.norm(vectors,axis=1)-1)<.08), 'Invalid tangent normals'
    elif name=='BaseColor': assert np.all(np.max(values[:,:3],axis=1)>.01), 'Unbaked color sample'
    elif name=='Roughness': assert np.all(values[:,0]>=.64) and np.all(values[:,0]<=.96)
    elif name=='MetallicSmoothness': assert np.max(values[:,:3])==0 and np.all(values[:,3]>=.04) and np.all(values[:,3]<=.36)
    report['maps'][name]={'dimensions':[size,size],'samples_checked':len(values),'finite':True,'packed':image.packed_file is not None}
assert bpy.context.view_layer.objects.active==low

# Reimport in a disposable, unsaved factory scene. Excludes the character and ground.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(base/'Exports/LimestoneRock01.fbx'))
found={}
for obj in bpy.data.objects:
    if obj.type!='MESH': continue
    obj.data.calc_loop_triangles(); found[obj.name]={'triangles':len(obj.data.loop_triangles),'dimensions':list(obj.dimensions)}
assert set(found)==set(expected)
for name,actual in found.items():
    assert actual['triangles']==expected[name]['triangles']
    assert all(abs(a-b)<1e-4 for a,b in zip(actual['dimensions'],expected[name]['dimensions'])), name
report['fbx_roundtrip']=found
(base/'verification.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print(json.dumps(report,indent=2))
