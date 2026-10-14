"""Rebuild supported stone tiles from retained sources; never collapse the fused contact mesh.
Blender background ORIGINAL --python THIS -- ROOT prepare
Apply the inspected result separately with apply_flat_stone_repair.py.
"""
import hashlib,json,shutil,sys
from pathlib import Path
import bpy,bmesh,numpy as np
from mathutils import Matrix,Vector,geometry
from mathutils.bvhtree import BVHTree
root=Path(sys.argv[sys.argv.index('--')+1]).resolve(); mode=sys.argv[-1]; asset='FlatStoneGroundPatch01'
assert mode=='prepare', 'This tool prepares only; use apply_flat_stone_repair.py to apply.'
scene=bpy.context.scene; old=bpy.data.objects[asset]; proof=root/'Logs'/asset/'Repair01'; proof.mkdir(parents=True,exist_ok=True)
docs=root/'SourceArt/Blender/Studies/BooterGroundSetup'/asset; exports=root/'Assets/_Project/Art/Environment/Ground'/asset
application=json.loads((docs/'application.json').read_text()); audit=json.loads((root/'Logs'/asset/'reduction_audit.json').read_text())
source_path=Path(bpy.data.filepath).resolve(); input_hash=hashlib.sha256(source_path.read_bytes()).hexdigest()
def signature(obj):
    digest=hashlib.sha256();digest.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        coords=np.empty(len(obj.data.vertices)*3,dtype=np.float32);obj.data.vertices.foreach_get('co',coords);digest.update(coords.tobytes())
        for p in obj.data.polygons: digest.update(str(tuple(p.vertices)).encode())
        for uv in obj.data.uv_layers:
            data=np.empty(len(uv.data)*2,dtype=np.float32);uv.data.foreach_get('uv',data);digest.update(data.tobytes())
        digest.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return digest.hexdigest()
def stats(obj):
    obj.data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(obj.data);remaining=set(bm.verts);components=0
    while remaining:
        stack=[remaining.pop()];components+=1
        while stack:
            v=stack.pop()
            for e in v.link_edges:
                other=e.other_vert(v)
                if other in remaining:remaining.remove(other);stack.append(other)
    result=dict(vertices=len(bm.verts),triangles=len(obj.data.loop_triangles),boundary=sum(e.is_boundary for e in bm.edges),
        nonmanifold=sum(not e.is_manifold for e in bm.edges),components=components,degenerate=sum(f.calc_area()<1e-12 for f in bm.faces),volume=bm.calc_volume())
    bm.free();return result
def active(obj):
    if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
    for o in scene.objects:o.select_set(False)
    obj.hide_set(False);obj.hide_select=False;obj.select_set(True);bpy.context.view_layer.objects.active=obj
before={o.name:signature(o) for o in scene.objects if o!=old}
source_names=application['source_selection']; sources=[bpy.data.objects[n] for n in source_names]
plane=next(o for o in sources if len(o.data.vertices)==4);stones=[o for o in sources if o!=plane]
pivot=Vector(application['pivot']);frame=Matrix.Translation(-pivot)
ground=plane.data.materials[0]
top=stones[0].data.materials[0].copy();top.name='FlatStoneGroundPatch01 - retained limestone tops'
side=bpy.data.materials.new('FlatStoneGroundPatch01 - supported limestone sides');side.use_nodes=True
white=root/'SourceArt/Blender/Studies/BooterGroundSetup/LimestoneRock01/ApprovedWhiteV2/SourceTextures'
shader=side.node_tree.nodes.get('Principled BSDF')
for kind,socket,file in [('BaseColor','Base Color','WhiteLimestone_ApprovedAlbedo.png'),('Roughness','Roughness','WhiteLimestone_Roughness.png')]:
    n=side.node_tree.nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(white/file),check_existing=True)
    n.image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color';side.node_tree.links.new(n.outputs['Color'],shader.inputs[socket])
height=side.node_tree.nodes.new('ShaderNodeTexImage');height.image=bpy.data.images.load(str(white/'WhiteLimestone_Height16.png'),check_existing=True);height.image.colorspace_settings.name='Non-Color'
bump=side.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Distance'].default_value=.0006
side.node_tree.links.new(height.outputs['Color'],bump.inputs['Height']);side.node_tree.links.new(bump.outputs['Normal'],shader.inputs['Normal'])
material_set=[ground,side];uv_name=stones[0].data.uv_layers.active.name
collection=bpy.data.collections.new('Repair supported columns');scene.collection.children.link(collection);columns=[];rows=[]
for source in stones:
    obj=source.copy();obj.data=source.data.copy();scene.collection.objects.link(obj);obj.name='Repair column '+source.name
    obj.data.transform(frame@source.matrix_world);obj.matrix_world=Matrix.Translation(pivot);active(obj)
    # Reuse the measured per-stone reduction, then discard the buried and undercut underside.
    bm=bmesh.new();bm.from_mesh(obj.data)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(0,0,-.010),plane_no=(0,0,1),clear_inner=True)
    bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data);bm.free();obj.data.update();base=stats(obj)
    target=next(r['chosen']['target'] for r in audit['rows'] if r['name']==source.name)
    mod=obj.modifiers.new('Retain measured upper stone shape','DECIMATE');mod.ratio=min(1,target/base['triangles']);mod.use_collapse_triangulate=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.clear()
    for material in material_set:obj.data.materials.append(material)
    bm=bmesh.new();bm.from_mesh(obj.data);uv=bm.loops.layers.uv.get(uv_name)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(0,0,0),plane_no=(0,0,1),clear_inner=True)
    bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.normal_update();bm.verts.ensure_lookup_table();bm.verts.index_update()
    # Intersect projected upper-face constraints, resolving partially occluded
    # facets rather than dropping whole triangles and leaving pinched boundaries.
    upper=[f for f in bm.faces if f.normal.z>1e-6]
    source_faces=[([v.co.copy() for v in f.verts],[l[uv].uv.copy() for l in f.loops]) for f in upper]
    cdt=geometry.delaunay_2d_cdt([v.co.xy for v in bm.verts],[],[[v.index for v in f.verts] for f in upper],1,1e-6)
    xy,_,triangles,_,_,orig_faces=cdt
    heights={};chosen=[]
    for triangle,origins in zip(triangles,orig_faces):
        assert origins
        midpoint=sum((xy[i] for i in triangle),Vector((0,0)))/3
        def point_on_face(index,p):
            coords,_=source_faces[index]
            return geometry.barycentric_transform(Vector((p.x,p.y,0)),*[Vector((c.x,c.y,0)) for c in coords],*coords)
        best=max(origins,key=lambda index:point_on_face(index,midpoint).z);chosen.append(best)
        for i in triangle:heights[i]=max(heights.get(i,-1e20),point_on_face(best,xy[i]).z)
    used=sorted(heights);remap={i:k for k,i in enumerate(used)}
    new_mesh=bpy.data.meshes.new('Supported upper envelope')
    new_mesh.from_pydata([(xy[i].x,xy[i].y,max(0,heights[i])) for i in used],[],[[remap[i] for i in t] for t in triangles]);new_mesh.update()
    uv_out=new_mesh.uv_layers.new(name=uv_name)
    for polygon,triangle,best in zip(new_mesh.polygons,triangles,chosen):
        for loop,i in zip(polygon.loop_indices,triangle):
            uv_out.data[loop].uv=xy[i]/1.1
    bm.free();obj.data=new_mesh
    for material in material_set:new_mesh.materials.append(material)
    bm=bmesh.new();bm.from_mesh(new_mesh);uv=bm.loops.layers.uv.get(uv_name)
    for f in bm.faces:f.material_index=1;f.smooth=True
    # A single upward-facing disk is required, with no internal projected holes.
    boundary=[e for e in bm.edges if e.is_boundary];assert boundary,source.name
    adjacency={}
    for e in boundary:
        for v in e.verts:adjacency.setdefault(v,[]).append(e.other_vert(v))
    assert all(len(n)==2 for n in adjacency.values()),source.name+' has an ambiguous upper contour'
    first=next(iter(adjacency));ordered=[first];previous=None;current=first
    while True:
        following=next(v for v in adjacency[current] if v!=previous)
        if following==first:break
        assert following not in ordered
        ordered.append(following);previous,current=current,following
    assert len(ordered)==len(adjacency),source.name+' has multiple upper contour loops'
    bottom={v:bm.verts.new((v.co.x,v.co.y,-.010)) for v in ordered}
    # Boundary face orientation is opposite the existing top's boundary edge.
    distance=0
    perimeter=sum((ordered[(i+1)%len(ordered)].co.xy-v.co.xy).length for i,v in enumerate(ordered))
    repeats=max(1,round(perimeter/1.1))
    for i,start in enumerate(ordered):
        end=ordered[(i+1)%len(ordered)];e=next(edge for edge in start.link_edges if end in edge.verts)
        loop=e.link_loops[0];a=loop.vert;b=loop.link_loop_next.vert
        face=bm.faces.new((b,a,bottom[a],bottom[b]));face.material_index=1;face.smooth=False
        length=(b.co.xy-a.co.xy).length
        values={start:distance/perimeter*repeats,end:(distance+length)/perimeter*repeats}
        for l in face.loops:
            v=l.vert;original=start if v==bottom[start] else end if v==bottom[end] else v
            l[uv].uv=(values[original],v.co.z/1.1)
        distance+=length
    cap=bm.faces.new([bottom[v] for v in reversed(ordered)]);cap.material_index=1;cap.smooth=False
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data);bm.free();obj.data.update();s=stats(obj)
    assert s['nonmanifold']==0 and s['components']==1 and s['degenerate']==0 and s['volume']>0,(source.name,s)
    rows.append(dict(source=source.name,**s));columns.append(obj)
    for c in list(obj.users_collection):c.objects.unlink(obj)
    collection.objects.link(obj)
coords=[frame@plane.matrix_world@v.co for v in plane.data.vertices];x0=min(v.x for v in coords);x1=max(v.x for v in coords);y0=min(v.y for v in coords);y1=max(v.y for v in coords)
mesh=bpy.data.meshes.new('Supported tile repair mesh');mesh.from_pydata([(x,y,z) for z in [-.025,0] for y in [y0,y1] for x in [x0,x1]],[],[(0,2,3,1),(4,5,7,6),(0,1,5,4),(1,3,7,5),(3,2,6,7),(2,0,4,6)]);mesh.update()
patch=bpy.data.objects.new('Supported tile repair',mesh);scene.collection.objects.link(patch);patch.matrix_world=old.matrix_world.copy()
for material in material_set:mesh.materials.append(material)
uv=mesh.uv_layers.new(name=uv_name)
for p in mesh.polygons:
    for i in p.loop_indices:uv.data[i].uv=mesh.vertices[mesh.loops[i].vertex_index].co.xy/1.1
# Boolean operates at the source construction pose; retain the user's object pose afterwards.
user_pose=patch.matrix_world.copy();patch.matrix_world=Matrix.Translation(pivot);active(patch)
mod=patch.modifiers.new('Watertight supported stone union','BOOLEAN');mod.solver='EXACT';mod.operation='UNION';mod.operand_type='COLLECTION';mod.collection=collection
bpy.ops.object.modifier_apply(modifier=mod.name)
mesh=patch.data
bm=bmesh.new();bm.from_mesh(mesh)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges))
bmesh.ops.dissolve_limit(bm,angle_limit=.0001,verts=list(bm.verts),edges=list(bm.edges),delimit={'UV','MATERIAL'})
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();mesh.update()
patch.matrix_world=user_pose;bpy.context.view_layer.update()
# Zero-weight contact, UV-seam and base vertices are ineligible for collapse.
# Only redundant interior roof vertices may move; vertical support remains intact.
bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table();bm.verts.index_update();uv=bm.loops.layers.uv.active
movable=[]
for v in bm.verts:
    values={tuple(round(float(c),6) for c in l[uv].uv) for l in v.link_loops}
    if v.co.z>.001 and len(values)==1 and all(f.material_index==1 for f in v.link_faces):movable.append(v.index)
bm.free();group=patch.vertex_groups.new(name='Repair - interior roofs only');group.add(movable,1,'REPLACE')
reference=BVHTree.FromPolygons([v.co for v in mesh.vertices],[p.vertices[:] for p in mesh.polygons])
reference_points=[v.co.copy() for v in mesh.vertices]+[p.center.copy() for p in mesh.polygons]
reduction=[];accepted=None
for budget in [6500,8000,9500,11000,13000]:
    candidate=patch.copy();candidate.data=patch.data.copy();scene.collection.objects.link(candidate);active(candidate)
    modifier=candidate.modifiers.new('Protect repaired contacts','DECIMATE');modifier.ratio=budget/stats(patch)['triangles'];modifier.use_collapse_triangulate=True
    modifier.vertex_group=group.name;modifier.vertex_group_factor=1
    bpy.ops.object.modifier_apply(modifier=modifier.name);s=stats(candidate)
    target_tree=BVHTree.FromPolygons([v.co for v in candidate.data.vertices],[p.vertices[:] for p in candidate.data.polygons])
    errors=[target_tree.find_nearest(p)[3] for p in reference_points]+[reference.find_nearest(v.co)[3] for v in candidate.data.vertices]
    downward=sum(p.normal.z<-.01 and p.center.z>.005 for p in candidate.data.polygons)
    s.update(budget=budget,max_surface_error=max(errors),p95_surface_error=float(np.percentile(errors,95)),unsupported_faces=downward)
    reduction.append(s);print('PROTECTED REDUCTION '+json.dumps(s),flush=True)
    if s['nonmanifold']==0 and s['components']==1 and s['degenerate']==0 and max(errors)<.004 and downward==0:
        accepted=candidate;break
    bpy.data.objects.remove(candidate,do_unlink=True)
if accepted:
    bpy.data.objects.remove(patch,do_unlink=True);patch=accepted;mesh=patch.data
final=stats(patch)
assert final['boundary']==0 and final['nonmanifold']==0 and final['components']==1 and final['degenerate']==0 and final['volume']>0,final
for obj in columns:bpy.data.objects.remove(obj,do_unlink=True)
bpy.data.collections.remove(collection)
assert before=={n:signature(bpy.data.objects[n]) for n in before}
report=dict(input_scene_sha256=input_hash,old=stats(old),repaired=final,columns=rows,materials=[m.name for m in material_set],
    original_object_signatures=before,pose=[list(v) for v in old.matrix_world],source_selection=source_names,protected_reduction=reduction)
bpy.data.libraries.write(str(proof/'supported_patch.blend'),{patch},fake_user=True)
(proof/'preparation.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n');print(json.dumps(final),flush=True)
# Transient diagnostic renders: no scene save.
for o in scene.objects:
    if o.type=='MESH':o.hide_render=o!=patch
camera=scene.camera;target=patch.matrix_world@Vector((-1,-2,0));camera.data.type='ORTHO';camera.data.ortho_scale=4.5
camera.location=target+Vector((0,-4,3));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
for o in scene.objects:
    if o.type=='LIGHT':
        o.location=patch.location+Vector((2,-4,8) if o.name=='Key' else (-4,4,6));o.rotation_euler=(patch.location-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.filepath=str(proof/'supported-closeup.png');bpy.ops.render.render(write_still=True)
print('Supported tile preparation complete; original scene not saved',flush=True)
