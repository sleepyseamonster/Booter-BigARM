"""Measured reduction and PBR baking for the saved Booter-ground sculpt.
Blender --background --disable-autoexec SOURCE --python THIS -- inspect|build ROOT
Never overwrites the artist source or an existing delivery.
"""
import json, math, random, sys, hashlib
from pathlib import Path
import bpy, bmesh
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

mode, root_arg = sys.argv[sys.argv.index('--') + 1:][:2]
root = Path(root_arg).resolve()
base = root / 'SourceArt/Blender/Studies/BooterGroundSetup/LimestoneRock01'
export = base / 'Exports'
proof = root / 'Logs/LimestoneRock01'
proof.mkdir(parents=True, exist_ok=True)
textures = root / 'Assets/_Project/Art/Environment/Rocks/Workbench/Layered'
source_path = Path(bpy.data.filepath)
source_hash = hashlib.sha256(source_path.read_bytes()).hexdigest()

def active(obj):
    if bpy.context.object and bpy.context.object.mode != 'OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')
    obj.hide_set(False); obj.hide_select = False
    obj.select_set(True); bpy.context.view_layer.objects.active = obj

source = bpy.data.objects['Icosphere']
active(source)
source = source.copy(); source.data = source.data.copy()
bpy.context.scene.collection.objects.link(source)
source.name = 'LimestoneRock01_HighSource'
active(source)
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
source.data.calc_loop_triangles()
original_location = source.location.copy()

def stats(obj):
    obj.data.calc_loop_triangles()
    bm = bmesh.new(); bm.from_mesh(obj.data)
    result = dict(vertices=len(bm.verts), triangles=len(obj.data.loop_triangles), faces=len(bm.faces),
                  nonmanifold=sum(not e.is_manifold for e in bm.edges), loose=sum(not v.link_edges for v in bm.verts),
                  degenerate=sum(f.calc_area() < 1e-12 for f in bm.faces), volume=bm.calc_volume())
    bm.free(); return result

def reduce(obj, target, name):
    low = obj.copy(); low.data = obj.data.copy(); bpy.context.scene.collection.objects.link(low)
    low.name = name; active(low)
    mod = low.modifiers.new('Silhouette measured collapse', 'DECIMATE')
    mod.ratio = min(1.0, target / stats(obj)['triangles']); mod.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return low

def tree(obj):
    return BVHTree.FromPolygons([v.co for v in obj.data.vertices], [p.vertices[:] for p in obj.data.polygons])

def samples(obj):
    mesh = obj.data; mesh.calc_loop_triangles(); rng = random.Random(20261012)
    result = [v.co.copy() for v in mesh.vertices]
    areas = [t.area for t in mesh.loop_triangles]
    for tri in rng.choices(list(mesh.loop_triangles), weights=areas, k=10000):
        a,b,c = [mesh.vertices[i].co for i in tri.vertices]
        u,v = rng.random(),rng.random()
        if u+v > 1: u,v = 1-u,1-v
        result.append(a+(b-a)*u+(c-a)*v)
    return result

points = [v.co for v in source.data.vertices]
center = sum(points, Vector()) / len(points)
radius = max((p-center).length for p in points) * 1.08
directions = [(1,0,0), (0,1,0), (0,0,1), (1,1,.65), (-1,1,.65), (1,-1,.65), (-1,-1,.65), (1,.3,-.5)]

def mask(bvh, direction, resolution=192):
    forward = Vector(direction).normalized()
    side = forward.cross(Vector((0,0,1)) if abs(forward.z)<.9 else Vector((0,1,0))).normalized()
    up = side.cross(forward).normalized()
    result = np.zeros((resolution,resolution), dtype=bool)
    for y in range(resolution):
        row = center + up*((y+.5)/resolution*2-1)*radius - forward*radius*3
        for x in range(resolution):
            origin = row + side*((x+.5)/resolution*2-1)*radius
            result[y,x] = bvh.ray_cast(origin,forward,radius*6)[0] is not None
    return result

def look(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def acceptable(row):
    return row['nonmanifold']==0 and row['p95_m']<=.008 and row['max_sampled_m']<=.02 and row['silhouette_iou_min']>=.994

def render(obj, label, direction=(3,-4,2.5), plain=False):
    scene = bpy.context.scene
    old = [(o,o.hide_render) for o in scene.objects]
    for o in scene.objects:
        if o.type == 'MESH': o.hide_render = o != obj
    obj.hide_render=False
    camera = scene.camera
    camera.data.type='ORTHO'; camera.data.ortho_scale=max(obj.dimensions)*1.55
    target=obj.location+center
    camera.location=target+Vector(direction).normalized()*8
    look(camera,target)
    for o in scene.objects:
        if o.type=='LIGHT':
            o.hide_render=False; o.location=target+Vector((2,-4,6) if o.name=='Key' else (-4,2,4)); look(o,target)
    scene.render.engine='BLENDER_EEVEE'
    scene.render.resolution_x=900; scene.render.resolution_y=900; scene.render.resolution_percentage=100
    scene.render.filepath=str(proof/(label+'.png'))
    bpy.ops.render.render(write_still=True)
    for o,hidden in old: o.hide_render=hidden

if mode == 'inspect':
    source_bvh=tree(source); source_points=samples(source)
    masks=[mask(source_bvh,d) for d in directions]
    report=dict(source=stats(source), dimensions=list(source.dimensions), source_sha256=source_hash, candidates=[])
    def measure(target):
        low=reduce(source,target,'Candidate'); low_bvh=tree(low)
        distances=[low_bvh.find_nearest(p)[3] for p in source_points]+[source_bvh.find_nearest(p)[3] for p in samples(low)]
        ious=[]
        for d,reference in zip(directions,masks):
            candidate=mask(low_bvh,d)
            ious.append(float(np.count_nonzero(reference & candidate)/np.count_nonzero(reference | candidate)))
        row=dict(target=target,**stats(low), p95_m=float(np.percentile(distances,95)), max_sampled_m=max(distances),
                 silhouette_iou_min=min(ious), silhouette_iou_mean=float(np.mean(ious)))
        report['candidates'].append(row); print(json.dumps(row),flush=True)
        bpy.data.objects.remove(low,do_unlink=True)
        return row
    for target in [64,96,128,192,256,384,512,768,1024,1536,2048,3072]:
        measure(target)
    passing=min((r for r in report['candidates'] if acceptable(r)),key=lambda r:r['target'])
    lower=max(r['target'] for r in report['candidates'] if r['target']<passing['target'])
    upper=passing['target']
    while upper-lower>8:
        middle=((lower+upper)//4)*2
        if acceptable(measure(middle)): upper=middle
        else: lower=middle
    report['selection_gates']=dict(p95_metres=.008,max_sampled_metres=.02,min_silhouette_iou=.994,
                                  tested_boundary_lower=lower,tested_boundary_upper=upper,resolution=192,views=len(directions))
    (proof/'candidates.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
    render(source,'source-shape')
    sys.exit(0)

if mode not in {'build','resume'}: raise ValueError('Expected inspect, build or resume')
output=base/'LimestoneRock01_GameReady.blend'
if output.exists() or (export.exists() and mode!='resume'): raise FileExistsError('Preserve the existing delivery before rebuilding')
report=json.loads((proof/'candidates.json').read_text())
assert report['source_sha256']==source_hash, 'Sculpt changed since reduction inspection'
chosen=min((r for r in report['candidates'] if acceptable(r)),key=lambda r:r['triangles'])
low=reduce(source,chosen['target'],'LimestoneRock01_LOD0')
# Bottom-centre asset pivot, retaining the user's scene position.
minimum=Vector(tuple(min(v.co[i] for v in source.data.vertices) for i in range(3)))
maximum=Vector(tuple(max(v.co[i] for v in source.data.vertices) for i in range(3)))
pivot=Vector(((minimum.x+maximum.x)/2,(minimum.y+maximum.y)/2,minimum.z))
for obj in (source,low):
    for v in obj.data.vertices: v.co-=pivot
    obj.location=original_location+pivot
    for p in obj.data.polygons: p.use_smooth=True
center-=pivot
active(low)
for uv in list(low.data.uv_layers): low.data.uv_layers.remove(uv)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.02, scale_to_bounds=True)
bpy.ops.object.mode_set(mode='OBJECT')
export.mkdir(parents=True,exist_ok=mode=='resume')
material=bpy.data.materials.new('Limestone - workbench derivative bake source'); material.use_nodes=True
n,l=material.node_tree.nodes,material.node_tree.links; n.clear()
out=n.new('ShaderNodeOutputMaterial'); bsdf=n.new('ShaderNodeBsdfPrincipled'); l.new(bsdf.outputs['BSDF'],out.inputs['Surface'])
coord=n.new('ShaderNodeTexCoord')
scale=n.new('ShaderNodeVectorMath'); scale.operation='SCALE'; scale.inputs[3].default_value=1/1.1
l.new(coord.outputs['Object'],scale.inputs[0])
channels={}
for kind in ('Albedo','Surface'):
    node=n.new('ShaderNodeTexImage'); node.image=bpy.data.images.load(str(textures/f'RockWorkbenchSide_{kind}.png'),check_existing=True)
    node.image.colorspace_settings.name='sRGB' if kind=='Albedo' else 'Non-Color'
    node.projection='BOX'; node.projection_blend=.2; l.new(scale.outputs['Vector'],node.inputs['Vector']); channels[kind]=node
# Exact source pattern retained: palette transformation occurs in the material,
# then is baked to the asset UVs rather than generating unrelated surface detail.
bw=n.new('ShaderNodeRGBToBW'); l.new(channels['Albedo'].outputs['Color'],bw.inputs['Color'])
remap=n.new('ShaderNodeMapRange'); remap.inputs['From Min'].default_value=.005; remap.inputs['From Max'].default_value=.30
l.new(bw.outputs['Val'],remap.inputs['Value'])
ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].color=(.20,.185,.15,1); ramp.color_ramp.elements[1].color=(.76,.72,.61,1)
l.new(remap.outputs['Result'],ramp.inputs['Fac']); l.new(ramp.outputs['Color'],bsdf.inputs['Base Color'])
separate=n.new('ShaderNodeSeparateColor'); l.new(channels['Surface'].outputs['Color'],separate.inputs['Color'])
rough=n.new('ShaderNodeMapRange'); rough.inputs['To Min'].default_value=.65; rough.inputs['To Max'].default_value=.95
l.new(separate.outputs['Green'],rough.inputs['Value']); l.new(rough.outputs['Result'],bsdf.inputs['Roughness'])
bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.35; bump.inputs['Distance'].default_value=.018
l.new(separate.outputs['Blue'],bump.inputs['Height']); l.new(bump.outputs['Normal'],bsdf.inputs['Normal'])
source.data.materials.clear(); source.data.materials.append(material)
# Independent target material avoids target texture feedback during baking.
target_material=bpy.data.materials.new('Limestone bake target'); target_material.use_nodes=True
low.data.materials.clear(); low.data.materials.append(target_material)
target=target_material.node_tree.nodes.new('ShaderNodeTexImage'); target_material.node_tree.nodes.active=target
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=16
scene.render.bake.margin=24; scene.render.bake.use_clear=True; scene.render.bake.margin_type='EXTEND'
emission=n.new('ShaderNodeEmission'); maps={}; size=2048
def bake(kind, socket=None):
    if mode=='resume':
        path=export/f'LimestoneRock01_{kind}.png'
        if not path.exists(): raise FileNotFoundError(path)
        image=bpy.data.images.load(str(path),check_existing=True); image.name='LimestoneRock01_'+kind
        image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
        assert image.size[:]==(size,size)
        maps[kind]=image
        if kind=='Normal': l.new(bsdf.outputs['BSDF'],out.inputs['Surface'])
        print('Reused completed bake '+kind,flush=True)
        return
    image=bpy.data.images.new('LimestoneRock01_'+kind,width=size,height=size,alpha=False)
    image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
    target.image=image; target_material.node_tree.nodes.active=target
    if socket:
        l.new(socket,emission.inputs['Color']); l.new(emission.outputs['Emission'],out.inputs['Surface'])
    else: l.new(bsdf.outputs['BSDF'],out.inputs['Surface'])
    active(low); source.hide_render=False; source.select_set(True)
    bpy.ops.object.bake(type='NORMAL' if kind=='Normal' else 'EMIT',use_selected_to_active=True,
                        normal_space='TANGENT',cage_extrusion=.035,max_ray_distance=.08)
    image.filepath_raw=str(export/f'LimestoneRock01_{kind}.png'); image.file_format='PNG'; image.save()
    maps[kind]=image
    print('Baked '+kind,flush=True)
bake('BaseColor',ramp.outputs['Color'])
bake('Roughness',rough.outputs['Result'])
ao=n.new('ShaderNodeAmbientOcclusion'); ao.inputs['Distance'].default_value=.15; ao.samples=16
ao_mix=n.new('ShaderNodeMath'); ao_mix.operation='MULTIPLY'
l.new(ao.outputs['AO'],ao_mix.inputs[0]); l.new(separate.outputs['Red'],ao_mix.inputs[1])
bake('Occlusion',ao_mix.outputs[0])
bake('Height',separate.outputs['Blue'])
bake('Normal')
pixels=np.empty(size*size*4,dtype=np.float32); maps['Roughness'].pixels.foreach_get(pixels)
packed=np.zeros((size*size,4),dtype=np.float32); packed[:,3]=1-pixels.reshape(-1,4)[:,0]
image=bpy.data.images.new('LimestoneRock01_MetallicSmoothness',width=size,height=size,alpha=True)
image.colorspace_settings.name='Non-Color'; image.pixels.foreach_set(packed.ravel())
image.filepath_raw=str(export/'LimestoneRock01_MetallicSmoothness.png'); image.file_format='PNG'; image.save(); maps['MetallicSmoothness']=image
game=bpy.data.materials.new('LimestoneRock01 - light limestone'); game.use_nodes=True
gn,gl=game.node_tree.nodes,game.node_tree.links; game_bsdf=gn.get('Principled BSDF')
for kind,socket in (('BaseColor','Base Color'),('Roughness','Roughness')):
    node=gn.new('ShaderNodeTexImage'); node.image=maps[kind]; gl.new(node.outputs['Color'],game_bsdf.inputs[socket])
node=gn.new('ShaderNodeTexImage'); node.image=maps['Normal']; normal=gn.new('ShaderNodeNormalMap')
gl.new(node.outputs['Color'],normal.inputs['Color']); gl.new(normal.outputs['Normal'],game_bsdf.inputs['Normal'])
low.data.materials.clear(); low.data.materials.append(game)
lod1=reduce(low,max(64,chosen['triangles']//2),'LimestoneRock01_LOD1')
lod2=reduce(low,max(32,chosen['triangles']//4),'LimestoneRock01_LOD2')
hull=low.copy(); hull.data=low.data.copy(); scene.collection.objects.link(hull); hull.name='LimestoneRock01_Collision'
def convex(obj):
    bm=bmesh.new()
    for v in obj.data.vertices: bm.verts.new(v.co)
    result=bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
    unused=[v for v in bm.verts if not v.link_faces]
    if unused: bmesh.ops.delete(bm,geom=unused,context='VERTS')
    bmesh.ops.triangulate(bm,faces=list(bm.faces)); bm.to_mesh(obj.data); bm.free()
convex(hull)
if stats(hull)['triangles']>240:
    smaller=reduce(hull,220,'Collision reduction')
    bpy.data.objects.remove(hull,do_unlink=True); hull=smaller; hull.name='LimestoneRock01_Collision'; convex(hull)
assert stats(hull)['triangles']<=255
meshes=[low,lod1,lod2,hull]
saved_locations=[o.location.copy() for o in meshes]
for o in meshes: o.location=(0,0,0)
active(low)
for o in meshes: o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(export/'LimestoneRock01.fbx'),use_selection=True,object_types={'MESH'},
                        global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
                        use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
for o,location in zip(meshes,saved_locations): o.location=location
original=bpy.data.objects['Icosphere']; original.hide_set(True); original.hide_render=True
source.hide_set(True); source.hide_render=True
for o in (lod1,lod2,hull): o.hide_set(True); o.hide_render=True
active(low)
for direction,label in [((3,-4,2.5),'limestone-front'),((-3,4,2.5),'limestone-back'),((0,-.01,5),'limestone-top'),((3,-4,-2.5),'limestone-underside')]:
    render(low,label,direction)
render(source,'limestone-high-source')
for image in maps.values(): image.use_fake_user=True; image.pack()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.region_3d.view_location=low.location+center
            area.spaces.active.region_3d.view_distance=max(low.dimensions)*2.5
bpy.context.window.workspace=bpy.data.workspaces.get('Layout',bpy.context.window.workspace)
scene.render.engine='BLENDER_EEVEE'
bpy.ops.wm.save_as_mainfile(filepath=str(output),check_existing=False)
report.update(chosen=chosen,output=str(output),export=str(export),meshes={o.name:stats(o) for o in meshes},
              pivot_shift=list(pivot),texture_size=size,source_hash_unchanged=hashlib.sha256(source_path.read_bytes()).hexdigest()==source_hash,
              texture_sources={f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in [textures/'RockWorkbenchSide_Albedo.png',textures/'RockWorkbenchSide_Surface.png']})
(base/'delivery.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print(json.dumps(report,indent=2),flush=True)
