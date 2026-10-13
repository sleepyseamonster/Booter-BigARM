"""Fresh, read-only verification of the approved material in the original scene.
Blender --background --disable-autoexec BooterGroundSetup.blend --python THIS -- ROOT
"""
import hashlib, json, math, sys
from pathlib import Path
import bpy, bmesh
import numpy as np
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
scene_path=root/'SourceArt/Blender/Studies/BooterGroundSetup/BooterGroundSetup.blend'
base=scene_path.parent/'LimestoneRock01/ApprovedWhiteV2'
applied=json.loads((base/'application.json').read_text())
assert Path(bpy.data.filepath).resolve()==scene_path.resolve()
assert hashlib.sha256(scene_path.read_bytes()).hexdigest()==applied['after_scene_sha256']
assert hashlib.sha256(Path(applied['backup']).read_bytes()).hexdigest()==applied['before_scene_sha256']
assert hashlib.sha256((base/'SourceTextures/WhiteLimestone_ApprovedAlbedo.png').read_bytes()).hexdigest()==applied['approved_albedo_sha256']
def signature(obj,material=True):
    h=hashlib.sha256(); h.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        v=np.empty(len(obj.data.vertices)*3,dtype=np.float32); obj.data.vertices.foreach_get('co',v); h.update(v.tobytes())
        for p in obj.data.polygons: h.update(str(tuple(p.vertices)).encode())
        for uv in obj.data.uv_layers:
            data=np.empty(len(uv.data)*2,dtype=np.float32); uv.data.foreach_get('uv',data); h.update(data.tobytes())
        if material: h.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return h.hexdigest()
names=applied['meshes']
assert applied['unchanged_object_signatures']=={o.name:signature(o,o.name not in names[:3]) for o in bpy.context.scene.objects}
assert len(bpy.data.scenes)==1
low=bpy.data.objects[names[0]]; low.data.calc_loop_triangles()
assert len(low.data.vertices)==358 and len(low.data.loop_triangles)==712
assert bpy.context.view_layer.objects.active==low and not low.hide_get() and not low.hide_render
report=dict(original_scene_reopened=True,scene_count=1,all_geometry_uvs_transforms_and_unrelated_materials_unchanged=True,
            approved_source_hash_matches=True,meshes={},maps={})
for name in names:
    obj=bpy.data.objects[name]; obj.data.calc_loop_triangles()
    bm=bmesh.new(); bm.from_mesh(obj.data)
    assert all(e.is_manifold for e in bm.edges) and all(v.link_edges for v in bm.verts)
    assert all(f.calc_area()>1e-12 for f in bm.faces) and bm.calc_volume()>0; bm.free()
    if name in names[:3]: assert obj.data.materials[0].name==applied['material']
    report['meshes'][name]=dict(triangles=len(obj.data.loop_triangles),vertices=len(obj.data.vertices),dimensions=list(obj.dimensions))
size=applied['texture_size']; uv=low.data.uv_layers.active
assert all(math.isfinite(x) and 0<=x<=1 for loop in uv.data for x in loop.uv)
for kind in ('BaseColor','Roughness','Occlusion','Height','Normal','MetallicSmoothness'):
    image=bpy.data.images['LimestoneRock01_WhiteV2_'+kind]
    assert tuple(image.size)==(size,size) and image.packed_file and image.use_fake_user
    assert image.colorspace_settings.name==('sRGB' if kind=='BaseColor' else 'Non-Color')
    pixels=np.empty(size*size*4,dtype=np.float32); image.pixels.foreach_get(pixels)
    pixels=pixels.reshape(size,size,4); assert np.isfinite(pixels).all()
    values=[]
    for triangle in low.data.loop_triangles:
        coords=[uv.data[i].uv for i in triangle.loops]
        for weights in ((1/3,1/3,1/3),(.6,.2,.2),(.2,.6,.2),(.2,.2,.6)):
            point=sum((p*w for p,w in zip(coords,weights)),Vector((0,0)))
            values.append(pixels[min(size-1,int(point.y*size)),min(size-1,int(point.x*size))])
    values=np.array(values)
    if kind=='Normal':
        assert np.all(values[:,2]>.5) and np.all(np.abs(np.linalg.norm(values[:,:3]*2-1,axis=1)-1)<.08)
    elif kind=='BaseColor': assert np.all(np.max(values[:,:3],axis=1)>.15)
    elif kind=='Roughness': assert np.all(values[:,0]>.75) and np.all(values[:,0]<.86)
    elif kind=='MetallicSmoothness': assert np.max(values[:,:3])==0 and np.all(values[:,3]>.14) and np.all(values[:,3]<.25)
    report['maps'][kind]=dict(size=[size,size],packed=True,valid_samples=len(values))
for file,digest in applied['exported_files'].items(): assert hashlib.sha256((base/'Exports'/file).read_bytes()).hexdigest()==digest
expected=report['meshes']
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(base/'Exports/LimestoneRock01_WhiteV2.fbx'))
objects=[o for o in bpy.data.objects if o.type=='MESH']; assert set(o.name for o in objects)==set(names)
for obj in objects:
    obj.data.calc_loop_triangles()
    assert len(obj.data.loop_triangles)==expected[obj.name]['triangles']
    assert all(abs(a-b)<1e-4 for a,b in zip(obj.dimensions,expected[obj.name]['dimensions']))
report['fbx_roundtrip_passed']=True
(base/'verification.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print(json.dumps(report,indent=2))
