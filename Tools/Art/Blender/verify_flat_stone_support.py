"""Read-only vertical-solid and topology checks: reveals undercuts missed by edge counts."""
import hashlib,json,sys
from pathlib import Path
import bpy,bmesh,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(sys.argv[sys.argv.index('--')+1]).resolve(); mode=sys.argv[-1];proof=root/'Logs/FlatStoneGroundPatch01/Repair01'
old=bpy.data.objects['FlatStoneGroundPatch01']
if mode=='prepared':
    with bpy.data.libraries.load(str(proof/'supported_patch.blend'),link=False) as (src,dst):dst.objects=[n for n in src.objects if n.startswith('Supported tile repair')]
    assert len(dst.objects)==1;patch=dst.objects[0];bpy.context.scene.collection.objects.link(patch)
else:patch=old
bpy.context.view_layer.update()
def check(obj):
    mesh=obj.data;mesh.calc_loop_triangles();tree=BVHTree.FromPolygons([v.co for v in mesh.vertices],[p.vertices[:] for p in mesh.polygons])
    bm=bmesh.new();bm.from_mesh(mesh)
    geo=dict(vertices=len(bm.verts),triangles=len(mesh.loop_triangles),boundary=sum(e.is_boundary for e in bm.edges),
        nonmanifold=sum(not e.is_manifold for e in bm.edges),degenerate=sum(f.calc_area()<1e-12 for f in bm.faces),
        downward_surface_faces=sum(f.normal.z<-.01 and f.calc_center_median().z>.005 for f in bm.faces),volume=bm.calc_volume())
    bm.free();cavities=0;missing=0;tested=0;examples=[]
    x0=min(v.co.x for v in mesh.vertices);x1=max(v.co.x for v in mesh.vertices);y0=min(v.co.y for v in mesh.vertices);y1=max(v.co.y for v in mesh.vertices)
    for y in range(256):
        for x in range(320):
            p=Vector((x0+(x+.5)/320*(x1-x0),y0+(y+.5)/256*(y1-y0),1))
            crossings=[]
            for _ in range(10):
                hit=tree.ray_cast(p,Vector((0,0,-1)),2)[0]
                if hit is None:break
                crossings.append(hit.z);p=hit-Vector((0,0,2e-5))
            tested+=1
            if len(crossings)!=2:
                if len(crossings)>2:cavities+=1
                else:missing+=1
                if len(examples)<12:examples.append(dict(x=p.x,y=p.y,crossings=crossings))
    geo.update(vertical_samples=tested,cavity_columns=cavities,missing_columns=missing,examples=examples)
    return geo
result=dict(repaired=check(patch))
if mode=='prepared':result['previous']=check(old)
new=result['repaired']
assert new['boundary']==0 and new['nonmanifold']==0 and new['degenerate']==0 and new['downward_surface_faces']==0
assert new['cavity_columns']==0 and new['missing_columns']==0,new
if mode=='applied':
    receipt=json.loads((root/'SourceArt/Blender/Studies/BooterGroundSetup/FlatStoneGroundPatch01/repair01.json').read_text())
    def signature(obj):
        h=hashlib.sha256();h.update(str(list(obj.matrix_world)).encode())
        if obj.type=='MESH':
            a=np.empty(len(obj.data.vertices)*3,dtype=np.float32);obj.data.vertices.foreach_get('co',a);h.update(a.tobytes())
            for p in obj.data.polygons:h.update(str(tuple(p.vertices)).encode())
            for uv in obj.data.uv_layers:
                a=np.empty(len(uv.data)*2,dtype=np.float32);uv.data.foreach_get('uv',a);h.update(a.tobytes())
            h.update(str([m.name if m else None for m in obj.data.materials]).encode())
        return h.hexdigest()
    assert receipt['original_object_signatures']=={n:signature(bpy.data.objects[n]) for n in receipt['original_object_signatures']}
    assert np.allclose(patch.matrix_world,receipt['preserved_pose'],atol=1e-6)
    assert patch.asset_data is not None and len(patch.data.materials)==2
    for p in patch.data.polygons:
        if p.material_index!=1:continue
        assert p.use_smooth or abs(p.normal.z)<.1
    assert hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest()==receipt['after_scene_sha256']
    result['originals_and_latest_pose_preserved']=True
(proof/(mode+'-support-verification.json')).write_text(json.dumps(result,indent=2)+'\n',newline='\n')
print(json.dumps(result),flush=True)
