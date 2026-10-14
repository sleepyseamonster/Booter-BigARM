"""Fresh native and FBX readback; no save. ROOT prepared|applied.
Run on the original scene for applied mode, factory startup for prepared mode.
"""
import hashlib,json,sys
from pathlib import Path
import bpy,bmesh
import numpy as np
from mathutils import Matrix,Vector,geometry
from mathutils.bvhtree import BVHTree

root=Path(sys.argv[sys.argv.index('--')+1]).resolve(); mode=sys.argv[-1]
asset='FlatStoneGroundPatch01'; proof=root/'Logs'/asset
docs=root/'SourceArt/Blender/Studies/BooterGroundSetup'/asset
report=json.loads(((docs/'application.json') if mode=='applied' else (proof/'preparation.json')).read_text())
exports=root/'Assets/_Project/Art/Environment/Ground'/asset if mode=='applied' else proof/'PreparedExports'
def signature(obj):
    digest=hashlib.sha256(); digest.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        coords=np.empty(len(obj.data.vertices)*3,dtype=np.float32); obj.data.vertices.foreach_get('co',coords); digest.update(coords.tobytes())
        for p in obj.data.polygons: digest.update(str(tuple(p.vertices)).encode())
        for uv in obj.data.uv_layers:
            data=np.empty(len(uv.data)*2,dtype=np.float32); uv.data.foreach_get('uv',data); digest.update(data.tobytes())
        digest.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return digest.hexdigest()
if mode=='applied':
    assert bpy.data.objects[asset].asset_data is not None
    assert {name:signature(bpy.data.objects[name]) for name in report['unchanged_object_signatures']}==report['unchanged_object_signatures']
    assert [o.name for o in bpy.context.view_layer.objects if o.select_get()]==[asset]
    assert all(bpy.data.objects[n].hide_get() and bpy.data.objects[n].hide_render for n in report['source_selection'])
    assert len(bpy.data.scenes)==report['scene_count']
else:
    with bpy.data.libraries.load(str(proof/'prepared_patch.blend'),link=False) as (src,dst): dst.objects=[asset]
    bpy.context.scene.collection.objects.link(dst.objects[0])
bpy.context.view_layer.update()
patch=bpy.data.objects[asset]; mesh=patch.data; mesh.calc_loop_triangles()
assert len(mesh.loop_triangles)==report['mesh']['triangles'] and len(mesh.vertices)==report['mesh']['vertices']
bm=bmesh.new(); bm.from_mesh(mesh)
assert all(e.is_manifold for e in bm.edges) and all(v.link_edges for v in bm.verts)
assert all(f.calc_area()>1e-12 for f in bm.faces) and bm.calc_volume()>0
assert len(bm.verts)-len(bm.edges)+len(bm.faces)==2
bm.free()
assert len(mesh.materials)==1 and all(p.material_index==0 for p in mesh.polygons)
assert np.allclose(patch.matrix_world,Matrix.Translation(Vector(report['pivot'])),atol=1e-6)
assert abs(min(v.co.z for v in mesh.vertices)+.025)<1e-6
uvs=np.array([uv.uv[:] for uv in mesh.uv_layers.active.data]); assert np.isfinite(uvs).all() and uvs.min()>=-1e-6 and uvs.max()<=1.000001
zero_uv=0
for t in mesh.loop_triangles:
    a,b,c=uvs[list(t.loops)]
    if abs(np.linalg.det(np.stack([b-a,c-a])))<1e-14: zero_uv+=1
assert zero_uv==0
# Test non-adjacent triangle pairs for interior segment crossings.
positions=[v.co.copy() for v in mesh.vertices]; tris=[tuple(t.vertices) for t in mesh.loop_triangles]
tree=BVHTree.FromPolygons(positions,tris,all_triangles=True)
crossings=[]
for i,j in tree.overlap(tree):
    if i>=j or set(tris[i])&set(tris[j]): continue
    hit=False
    for first,second in [(tris[i],tris[j]),(tris[j],tris[i])]:
        triangle=[positions[v] for v in second]
        for k in range(3):
            a=positions[first[k]]; b=positions[first[(k+1)%3]]; delta=b-a
            p=geometry.intersect_ray_tri(*triangle,delta,a,True)
            if p is not None:
                t=(p-a).dot(delta)/delta.length_squared
                if 1e-5<t<1-1e-5: hit=True; break
        if hit: break
    if hit: crossings.append([i,j])
assert not crossings, 'Non-adjacent triangle crossings: '+str(crossings[:10])
for name,digest in report['exported_files'].items(): assert hashlib.sha256((exports/name).read_bytes()).hexdigest()==digest
maps={}
for node in mesh.materials[0].node_tree.nodes:
    if node.type=='TEX_IMAGE':
        image=node.image; assert tuple(image.size)==(4096,4096) and image.packed_file is not None
        maps[image.name]=dict(size=list(image.size),colorspace=image.colorspace_settings.name,packed=True)
native_bbox=np.array([[min(v.co[i] for v in mesh.vertices),max(v.co[i] for v in mesh.vertices)] for i in range(3)])
before=set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=str(exports/(asset+'.fbx')),use_anim=False)
imported=[o for o in bpy.data.objects if o not in before and o.type=='MESH']; assert len(imported)==1
obj=imported[0]; obj.data.calc_loop_triangles(); assert len(obj.data.loop_triangles)==len(mesh.loop_triangles)
points=[obj.matrix_world@v.co for v in obj.data.vertices]
bounds=np.array([[min(v[i] for v in points),max(v[i] for v in points)] for i in range(3)])
assert np.allclose(bounds,native_bbox,atol=1e-5), 'FBX scale, pivot or orientation drift'
receipt=dict(mode=mode,blender=bpy.app.version_string,vertices=len(mesh.vertices),triangles=len(mesh.loop_triangles),
             closed_connected_genus_zero=True,triangle_crossings=0,nonzero_uv_triangles=True,one_material=True,
             fbx_vertices=len(obj.data.vertices),fbx_triangles=len(obj.data.loop_triangles),fbx_bounds_match=True,maps=maps,
             unchanged_originals=(mode=='applied'),original_file_not_saved=True)
(proof/(mode+'-verification.json')).write_text(json.dumps(receipt,indent=2)+'\n',newline='\n')
print(json.dumps(receipt),flush=True)
