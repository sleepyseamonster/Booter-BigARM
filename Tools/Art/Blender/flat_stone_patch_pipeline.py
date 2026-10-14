"""Read-only reduction audit, then a guarded same-scene paving-patch build.
Blender --background --disable-autoexec SOURCE --python THIS -- inspect|build ROOT
The build requires the saved artist selection and preserves a local source backup.
"""
import hashlib, json, math, random, shutil, sys
from pathlib import Path
import bpy, bmesh
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

mode,root_arg=sys.argv[sys.argv.index('--')+1:][:2]
root=Path(root_arg).resolve(); asset='FlatStoneGroundPatch01'
proof=root/'Logs'/asset; proof.mkdir(parents=True,exist_ok=True)
docs=root/'SourceArt/Blender/Studies/BooterGroundSetup'/asset
exports=root/'Assets/_Project/Art/Environment/Ground'/asset
if mode=='prepare': exports=proof/'PreparedExports'
original_path=root/'SourceArt/Blender/Studies/BooterGroundSetup/BooterGroundSetup.blend'
input_path=Path(bpy.data.filepath).resolve()
selected=[o for o in bpy.context.view_layer.objects if o.select_get() and o.type=='MESH']
planes=[o for o in selected if len(o.data.vertices)==4 and len(o.data.polygons)==1]
assert len(planes)==1 and len(selected)>2, 'Save the selected stones and exactly one four-vertex plane first'
plane=planes[0]; stones=[o for o in selected if o!=plane]
assert all(len(o.modifiers)==0 for o in selected)
pivot=sum((plane.matrix_world@v.co for v in plane.data.vertices),Vector())/4
assert max(abs((plane.matrix_world@v.co).z-pivot.z) for v in plane.data.vertices)<1e-5
frame=Matrix.Translation(-pivot)
def active(obj):
    if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
    for o in bpy.context.scene.objects: o.select_set(False)
    obj.hide_set(False); obj.hide_select=False; obj.select_set(True); bpy.context.view_layer.objects.active=obj
def geometry_stats(obj):
    obj.data.calc_loop_triangles(); bm=bmesh.new(); bm.from_mesh(obj.data)
    remaining=set(bm.verts); components=0
    while remaining:
        stack=[remaining.pop()]; components+=1
        while stack:
            v=stack.pop()
            for e in v.link_edges:
                other=e.other_vert(v)
                if other in remaining: remaining.remove(other); stack.append(other)
    out=dict(vertices=len(bm.verts),triangles=len(obj.data.loop_triangles),faces=len(bm.faces),
             nonmanifold=sum(not e.is_manifold for e in bm.edges),loose=sum(not v.link_edges for v in bm.verts),
             degenerate=sum(f.calc_area()<1e-12 for f in bm.faces),components=components,volume=bm.calc_volume())
    bm.free(); return out
def copy_world(obj,name):
    copy=obj.copy(); copy.data=obj.data.copy(); bpy.context.scene.collection.objects.link(copy)
    copy.name=name; copy.data.transform(frame@obj.matrix_world); copy.matrix_world=Matrix.Translation(pivot)
    copy.hide_set(False); copy.hide_render=False; copy.hide_select=False
    return copy
def clip_buried(obj):
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,
                          plane_co=(0,0,-.010),plane_no=(0,0,1),clear_inner=True)
    edges=[e for e in bm.edges if e.is_boundary]
    if edges: bmesh.ops.holes_fill(bm,edges=edges,sides=0)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(obj.data); bm.free(); obj.data.update()
def reduce(obj,count):
    low=obj.copy(); low.data=obj.data.copy(); bpy.context.scene.collection.objects.link(low); active(low)
    mod=low.modifiers.new('Measured stone reduction','DECIMATE'); mod.ratio=min(1,count/geometry_stats(obj)['triangles']); mod.use_collapse_triangulate=True
    bpy.ops.object.modifier_apply(modifier=mod.name); return low
def tree(obj): return BVHTree.FromPolygons([v.co for v in obj.data.vertices],[p.vertices[:] for p in obj.data.polygons])
def samples(obj,count=1800):
    mesh=obj.data; mesh.calc_loop_triangles(); rng=random.Random(20261013)
    out=[v.co.copy() for v in mesh.vertices]
    for t in rng.choices(list(mesh.loop_triangles),weights=[t.area for t in mesh.loop_triangles],k=count):
        a,b,c=[mesh.vertices[i].co for i in t.vertices]; u,v=rng.random(),rng.random()
        if u+v>1: u,v=1-u,1-v
        out.append(a+(b-a)*u+(c-a)*v)
    return out
def top_mask(bvh,obj,resolution=128):
    coords=[v.co for v in obj.data.vertices]
    lo=Vector(tuple(min(p[i] for p in coords) for i in range(3))); hi=Vector(tuple(max(p[i] for p in coords) for i in range(3)))
    margin=.02; out=np.zeros((resolution,resolution),dtype=bool)
    for y in range(resolution):
        for x in range(resolution):
            p=Vector((lo.x-margin+(x+.5)/resolution*(hi.x-lo.x+2*margin),lo.y-margin+(y+.5)/resolution*(hi.y-lo.y+2*margin),hi.z+.5))
            out[y,x]=bvh.ray_cast(p,Vector((0,0,-1)),2)[0] is not None
    return out
def render_group(objects,label):
    scene=bpy.context.scene
    bpy.context.view_layer.update()
    for o in scene.objects:
        if o.type=='MESH': o.hide_render=o not in objects
    corners=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
    lo=Vector(tuple(min(p[i] for p in corners) for i in range(3))); hi=Vector(tuple(max(p[i] for p in corners) for i in range(3)))
    center=(lo+hi)/2; extent=max(hi-lo)
    scene.camera.data.type='ORTHO'; scene.camera.data.ortho_scale=extent*1.25
    scene.camera.location=center+Vector((6,-8,10)).normalized()*extent*2
    scene.camera.rotation_euler=(center-scene.camera.location).to_track_quat('-Z','Y').to_euler()
    for o in scene.objects:
        if o.type=='LIGHT':
            o.location=center+Vector((2,-4,8) if o.name=='Key' else (-4,4,6)); o.hide_render=False
            o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
    scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=1400; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
    scene.render.filepath=str(proof/(label+'.png')); bpy.ops.render.render(write_still=True)

if mode=='inspect':
    report=dict(input_file=str(input_path),selection=[o.name for o in selected],plane=plane.name,stone_count=len(stones),pivot=list(pivot),
                original_stone_triangles=sum(geometry_stats(o)['triangles'] for o in stones),rows=[],slab_thickness_metres=.025)
    for stone in stones:
        source=copy_world(stone,'Audit source'); bounds=[v.co.z for v in source.data.vertices]
        assert min(bounds)<0 and max(bounds)>0, stone.name+' does not intersect the plane'
        clip_buried(source); before=geometry_stats(source); assert before['nonmanifold']==0 and before['volume']>0
        source_tree=tree(source); source_points=samples(source); reference=top_mask(source_tree,source)
        row=dict(name=stone.name,source=geometry_stats(stone),clipped=before,candidates=[])
        for count in [64,96,128,192,256,384,before['triangles']]:
            if count>before['triangles']: continue
            candidate=reduce(source,count); stats=geometry_stats(candidate); target=tree(candidate)
            errors=[target.find_nearest(p)[3] for p in source_points]+[source_tree.find_nearest(p)[3] for p in samples(candidate)]
            mask=top_mask(target,source)
            iou=float(np.count_nonzero(mask&reference)/np.count_nonzero(mask|reference))
            result=dict(target=count,**stats,p95_metres=float(np.percentile(errors,95)),max_sampled_metres=max(errors),top_silhouette_iou=iou)
            row['candidates'].append(result); bpy.data.objects.remove(candidate,do_unlink=True)
        passing=[r for r in row['candidates'] if r['nonmanifold']==0 and r['components']==1 and r['degenerate']==0 and r['p95_metres']<=.003 and r['max_sampled_metres']<=.008 and r['top_silhouette_iou']>=.993]
        row['chosen']=min(passing,key=lambda r:r['triangles']); report['rows'].append(row)
        bpy.data.objects.remove(source,do_unlink=True)
        print(json.dumps({'name':stone.name,'source_triangles':row['source']['triangles'],'chosen':row['chosen']}),flush=True)
    report['chosen_stone_triangles']=sum(r['chosen']['triangles'] for r in report['rows'])
    (proof/'reduction_audit.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
    render_group(selected,'artist-assembly')
    print('AUDIT COMPLETE',flush=True); sys.exit(0)

audit=json.loads((proof/'reduction_audit.json').read_text())
assert sorted(audit['selection'])==sorted(o.name for o in selected), 'Artist selection differs from the audited assembly'
assert asset not in bpy.data.objects, 'Asset already exists; review its receipt instead of rebuilding'
def signature(obj):
    digest=hashlib.sha256(); digest.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        v=np.empty(len(obj.data.vertices)*3,dtype=np.float32); obj.data.vertices.foreach_get('co',v); digest.update(v.tobytes())
        for p in obj.data.polygons: digest.update(str(tuple(p.vertices)).encode())
        for uv in obj.data.uv_layers:
            data=np.empty(len(uv.data)*2,dtype=np.float32); uv.data.foreach_get('uv',data); digest.update(data.tobytes())
        digest.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return digest.hexdigest()
scene=bpy.context.scene; before={o.name:signature(o) for o in scene.objects}
saved_visibility={o:(o.hide_get(),o.hide_render,o.hide_select) for o in scene.objects}
saved_render={k:getattr(scene.render,k) for k in ['engine','resolution_x','resolution_y','resolution_percentage','filepath']}
saved_cycles={k:getattr(scene.cycles,k) for k in ['device','samples']}
scene_names=[s.name for s in bpy.data.scenes]
if mode in ['build','prepare']:
    if mode=='build':
        assert input_path==original_path.resolve(), 'Build only the saved original authoring scene'
    else: assert input_path!=original_path.resolve(), 'Preparation reads the autosave without modifying the original'
    assert not (docs/'application.json').exists(), 'An applied patch already exists'
    docs.mkdir(parents=True,exist_ok=True); exports.mkdir(parents=True,exist_ok=True)
    input_hash=hashlib.sha256(input_path.read_bytes()).hexdigest()
    backup=input_path.parent/'BooterGroundSetup_BeforeFlatStonePatch_2026-10-13.blend'
    if mode=='build':
        if backup.exists(): assert hashlib.sha256(backup.read_bytes()).hexdigest()==input_hash, 'Backup belongs to a different source save'
        else: shutil.copy2(input_path,backup)
low_stones=[]
union_collection=bpy.data.collections.new('Temporary stone union')
bpy.context.scene.collection.children.link(union_collection)
for stone in stones:
    clipped=copy_world(stone,'Temporary clipped stone'); clip_buried(clipped)
    row=next(r for r in audit['rows'] if r['name']==stone.name)
    low=reduce(clipped,row['chosen']['target']); low_stones.append(low)
    for collection in list(low.users_collection): collection.objects.unlink(low)
    union_collection.objects.link(low)
    bpy.data.objects.remove(clipped,do_unlink=True)
coords=[frame@plane.matrix_world@v.co for v in plane.data.vertices]
xs=[v.x for v in coords]; ys=[v.y for v in coords]; x0,x1=min(xs),max(xs); y0,y1=min(ys),max(ys)
mesh=bpy.data.meshes.new(asset+' mesh')
mesh.from_pydata([(x,y,z) for z in [-.025,0] for y in [y0,y1] for x in [x0,x1]],[],
                 [(0,2,3,1),(4,5,7,6),(0,1,5,4),(1,3,7,5),(3,2,6,7),(2,0,4,6)])
mesh.update(); patch=bpy.data.objects.new(asset,mesh); bpy.context.scene.collection.objects.link(patch)
patch.matrix_world=Matrix.Translation(pivot)
for material in plane.data.materials: patch.data.materials.append(material)
slab_source=patch.copy(); slab_source.data=patch.data.copy(); scene.collection.objects.link(slab_source); slab_source.name='Temporary slab bake source'; slab_source.hide_render=True
active(patch)
mod=patch.modifiers.new('Fuse stones into solid base','BOOLEAN'); mod.operation='UNION'; mod.solver='EXACT'
mod.operand_type='COLLECTION'; mod.collection=union_collection
bpy.ops.object.modifier_apply(modifier=mod.name)
bm=bmesh.new(); bm.from_mesh(patch.data)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
bmesh.ops.dissolve_limit(bm,angle_limit=.0001,verts=list(bm.verts),edges=list(bm.edges),delimit={'MATERIAL','UV'})
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='BEAUTY',ngon_method='BEAUTY')
bm.to_mesh(patch.data); bm.free(); patch.data.update()
stats=geometry_stats(patch); print('UNION '+json.dumps(stats),flush=True)
assert stats['components']==1 and stats['nonmanifold']==0 and stats['loose']==0 and stats['degenerate']==0 and stats['volume']>0
union_tree=tree(patch); union_points=samples(patch,24000); fusion_candidates=[]; chosen=None
for count in [2500,3500,4500,4750,5000,5250,5500,6500,stats['triangles']]:
    if count>stats['triangles']: continue
    candidate=reduce(patch,count); candidate_tree=tree(candidate); result=geometry_stats(candidate)
    errors=[candidate_tree.find_nearest(p)[3] for p in union_points]+[union_tree.find_nearest(p)[3] for p in samples(candidate,24000)]
    result.update(target=count,p95_metres=float(np.percentile(errors,95)),max_sampled_metres=max(errors))
    fusion_candidates.append(result); print('FUSED REDUCTION '+json.dumps(result),flush=True)
    if result['nonmanifold']==0 and result['components']==1 and result['degenerate']==0 and result['p95_metres']<=.003 and result['max_sampled_metres']<=.008:
        chosen=candidate; break
    bpy.data.objects.remove(candidate,do_unlink=True)
assert chosen is not None
bpy.data.objects.remove(patch,do_unlink=True); patch=chosen; patch.name=asset
for low in low_stones: bpy.data.objects.remove(low,do_unlink=True)
bpy.data.collections.remove(union_collection)
if mode=='prototype':
    (proof/'fusion_prototype.json').write_text(json.dumps(dict(union=stats,candidates=fusion_candidates,final=geometry_stats(patch)),indent=2)+'\n',newline='\n')
    render_group([patch],'fusion-prototype'); print('PROTOTYPE COMPLETE',flush=True); sys.exit(0)
assert mode in ['build','prepare']
highs=[slab_source]
for stone in stones:
    high=copy_world(stone,'Temporary original stone bake source'); highs.append(high)
source_materials=[]
for high in highs:
    for index,slot in enumerate(high.material_slots):
        material=slot.material.copy(); high.data.materials[index]=material
        assert material.use_nodes
        shader=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        output=next(n for n in material.node_tree.nodes if n.type=='OUTPUT_MATERIAL' and n.is_active_output)
        source_materials.append((material,shader,output))
# Preserve the stone tops' smooth shading; keep slab and side boundaries sharp.
for p in patch.data.polygons: p.use_smooth=p.center.z>.005 and p.normal.z>-.5
active(patch)
while patch.data.uv_layers: patch.data.uv_layers.remove(patch.data.uv_layers[0])
patch.data.uv_layers.new(name='Paving atlas')
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(60),island_margin=.006,area_weight=1,correct_aspect=True,scale_to_bounds=True)
bpy.ops.object.mode_set(mode='OBJECT')
target_material=bpy.data.materials.new(asset+' atlas bake target'); target_material.use_nodes=True
patch.data.materials.clear(); patch.data.materials.append(target_material)
for p in patch.data.polygons: p.material_index=0
target_node=target_material.node_tree.nodes.new('ShaderNodeTexImage'); target_material.node_tree.nodes.active=target_node
scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=1
scene.render.bake.margin=16; scene.render.bake.margin_type='EXTEND'; scene.render.bake.use_clear=True
size=4096; maps={}
bake_manifest=dict(source_scene_sha256=input_hash,selected_signatures={o.name:before[o.name] for o in selected},
                   mesh=geometry_stats(patch),texture_size=size)
manifest_path=proof/'bake_input.json'
if manifest_path.exists(): assert json.loads(manifest_path.read_text())==bake_manifest, 'Source changed during partial atlas preparation'
else:
    if any(exports.glob('*.png')):
        assert '--resume-reviewed' in sys.argv, 'Review existing maps before adopting a partial bake'
        assert json.loads((proof/'original-selected-signatures.json').read_text())==bake_manifest['selected_signatures']
        assert json.loads((proof/'autosave-selected-signatures.json').read_text())==bake_manifest['selected_signatures']
    manifest_path.write_text(json.dumps(bake_manifest,indent=2)+'\n',newline='\n')
for obj in scene.objects:
    if obj.type=='MESH': obj.hide_render=obj not in highs and obj!=patch
def configure_source(kind):
    for material,shader,output in source_materials:
        nodes,links=material.node_tree.nodes,material.node_tree.links
        if kind=='Normal': links.new(shader.outputs['BSDF'],output.inputs['Surface']); continue
        emission=nodes.new('ShaderNodeEmission')
        if kind=='Occlusion':
            node=nodes.new('ShaderNodeAmbientOcclusion'); node.inputs['Distance'].default_value=.20; node.samples=16
            links.new(node.outputs['AO'],emission.inputs['Color'])
        else:
            socket=shader.inputs['Base Color' if kind=='BaseColor' else 'Roughness']
            if socket.is_linked: links.new(socket.links[0].from_socket,emission.inputs['Color'])
            elif kind=='BaseColor': emission.inputs['Color'].default_value=socket.default_value
            else: emission.inputs['Color'].default_value=(socket.default_value,)*3+(1,)
        links.new(emission.outputs['Emission'],output.inputs['Surface'])
for kind in ['BaseColor','Roughness','Occlusion','Normal']:
    path=(exports if kind!='Roughness' else docs)/(asset+'_'+kind+'.png')
    if path.exists():
        image=bpy.data.images.load(str(path),check_existing=True); assert tuple(image.size)==(size,size)
        image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'; maps[kind]=image
        print('REUSED '+kind,flush=True); continue
    configure_source(kind)
    image=bpy.data.images.new(asset+'_'+kind,width=size,height=size,alpha=False)
    image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'; target_node.image=image
    active(patch)
    for high in highs: high.hide_set(False); high.hide_select=False; high.select_set(True)
    bpy.ops.object.bake(type='NORMAL' if kind=='Normal' else 'EMIT',use_selected_to_active=True,
                        normal_space='TANGENT',cage_extrusion=.014,max_ray_distance=.06)
    image.filepath_raw=str(path); image.file_format='PNG'; image.save()
    maps[kind]=image; print('BAKED '+kind,flush=True)
rough=np.empty(size*size*4,dtype=np.float32); maps['Roughness'].pixels.foreach_get(rough)
packed=np.zeros((size*size,4),dtype=np.float32); packed[:,3]=1-rough.reshape(-1,4)[:,0]
image=bpy.data.images.new(asset+'_MetallicSmoothness',width=size,height=size,alpha=True)
image.colorspace_settings.name='Non-Color'; image.pixels.foreach_set(packed.ravel()); image.filepath_raw=str(exports/(asset+'_MetallicSmoothness.png'))
image.file_format='PNG'; image.save(); maps['MetallicSmoothness']=image
game=bpy.data.materials.new(asset+' - limestone paving atlas'); game.use_nodes=True
nodes,links=game.node_tree.nodes,game.node_tree.links; shader=nodes.get('Principled BSDF')
for kind,socket in [('BaseColor','Base Color'),('Roughness','Roughness')]:
    node=nodes.new('ShaderNodeTexImage'); node.image=maps[kind]; links.new(node.outputs['Color'],shader.inputs[socket])
normal=nodes.new('ShaderNodeNormalMap'); node=nodes.new('ShaderNodeTexImage'); node.image=maps['Normal']
links.new(node.outputs['Color'],normal.inputs['Color']); links.new(normal.outputs['Normal'],shader.inputs['Normal'])
patch.data.materials.clear(); patch.data.materials.append(game)
for high in highs:
    data=high.data; bpy.data.objects.remove(high,do_unlink=True); bpy.data.meshes.remove(data)
for material,_,_ in source_materials: bpy.data.materials.remove(material)
bpy.data.materials.remove(target_material)
active(patch); saved_position=patch.location.copy(); patch.location=(0,0,0)
bpy.ops.export_scene.fbx(filepath=str(exports/(asset+'.fbx')),use_selection=True,object_types={'MESH'},global_scale=1,
                        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
                        use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
patch.location=saved_position
bpy.context.view_layer.update()
assert before=={o.name:signature(o) for o in scene.objects if o.name in before}, 'Original object geometry, UVs, materials or transforms changed'
assert len(scene.objects)==len(before)+1 and scene_names==[s.name for s in bpy.data.scenes]
for obj,(hidden,render_hidden,locked) in saved_visibility.items(): obj.hide_set(hidden); obj.hide_render=render_hidden; obj.hide_select=locked
for obj in selected: obj.hide_set(True); obj.hide_render=True
for k,value in saved_render.items(): setattr(scene.render,k,value)
for k,value in saved_cycles.items(): setattr(scene.cycles,k,value)
active(patch)
patch['Asset role']='Reusable static ground patch: move as one object; Z=0 pivot is the original plane surface'
patch['Source assembly']='23 original stones and plane remain hidden in the same scene'
patch['Base thickness metres']=.025
for kind,image in maps.items(): image.name=asset+'_'+kind; image.use_fake_user=True; image.pack()
note=bpy.data.texts.new(asset+' - editing and export')
note.write('The selected '+asset+' is one closed, fused ground-patch mesh.\nOriginal stones and plane remain hidden and editable; unhide them only when replacing/rebuilding this patch.\nPivot is the base surface at the original plane centre. Base extends 25 mm downward. One metre equals one Unity unit.\nRuntime FBX and 4K atlas maps: Assets/_Project/Art/Environment/Ground/'+asset+'/.\nKeep the same scene. Reload the existing Blender window with File > Revert after background authoring.\n')
# Refuse a stale write if the author saved again while baking.
if mode=='prepare':
    bpy.data.libraries.write(str(proof/'prepared_patch.blend'),{patch},fake_user=True)
    report=dict(source_file=str(input_path),source_sha256=input_hash,source_selection=audit['selection'],
                original_selected_signatures={o.name:before[o.name] for o in selected},pivot=list(pivot),
                original_stone_triangles=audit['original_stone_triangles'],fusion_candidates=fusion_candidates,
                mesh=geometry_stats(patch),dimensions_metres=list(patch.dimensions),material=game.name,texture_size=size,
                exported_files={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in exports.iterdir() if p.suffix in ['.fbx','.png']})
    (proof/'preparation.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
    render_group([patch],'prepared-patch'); print('PREPARATION COMPLETE; ORIGINAL SCENE NOT MODIFIED',flush=True); sys.exit(0)
assert hashlib.sha256(input_path.read_bytes()).hexdigest()==input_hash, 'Artist saved during baking: leave the new save intact and review the export'
bpy.ops.wm.save_as_mainfile(filepath=str(input_path),check_existing=False)
report=dict(scene=str(input_path),backup=str(backup),before_scene_sha256=input_hash,after_scene_sha256=hashlib.sha256(input_path.read_bytes()).hexdigest(),
            unchanged_object_signatures=before,source_selection=audit['selection'],pivot=list(pivot),original_stone_triangles=audit['original_stone_triangles'],
            fusion_candidates=fusion_candidates,mesh=geometry_stats(patch),dimensions_metres=list(patch.dimensions),material=game.name,texture_size=size,
            exported_files={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in exports.iterdir() if p.suffix in ['.fbx','.png']})
(docs/'application.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
render_group([patch],'final-patch'); print('BUILD COMPLETE',flush=True)
