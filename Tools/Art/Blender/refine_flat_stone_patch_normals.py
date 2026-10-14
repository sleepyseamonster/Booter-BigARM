"""Preserve original unclipped source shading when baking normals onto the fused patch.
Read original author scene; writes task preparation/cache only, never saves that scene.
"""
import hashlib,json,sys
from pathlib import Path
import bpy
from mathutils import Matrix,Vector
root=Path(sys.argv[sys.argv.index('--')+1]).resolve(); proof=root/'Logs/FlatStoneGroundPatch01'; asset='FlatStoneGroundPatch01'
report=json.loads((proof/'preparation.json').read_text()); pivot=Vector(report['pivot']); scene=bpy.context.scene
assert asset not in bpy.data.objects, 'Refinement reads a retained author-source save before applying the patch'
with bpy.data.libraries.load(str(proof/'prepared_patch.blend'),link=False) as (src,dst): dst.objects=[asset]
patch=dst.objects[0]; scene.collection.objects.link(patch); bpy.context.view_layer.update()
for polygon in patch.data.polygons:
    # Nearly-flat base triangles must not interpolate stone-edge normals across metres of ground.
    polygon.use_smooth=polygon.center.z>.005 and polygon.normal.z>-.5
patch.data.update()
highs=[]
for name in report['source_selection']:
    original=bpy.data.objects[name]
    if len(original.data.vertices)==4: continue
    high=original.copy(); high.data=original.data.copy(); scene.collection.objects.link(high)
    high.data.transform(Matrix.Translation(-pivot)@original.matrix_world); high.matrix_world=Matrix.Translation(pivot)
    high.hide_set(False); high.hide_select=False; high.hide_render=False; highs.append(high)
# The closed untextured base is the source for the base and underside normals.
x=patch.dimensions.x/2; y=patch.dimensions.y/2
mesh=bpy.data.meshes.new('Normal bake slab'); mesh.from_pydata([(a,b,c) for c in [-.025,0] for b in [-y,y] for a in [-x,x]],[],
    [(0,2,3,1),(4,5,7,6),(0,1,5,4),(1,3,7,5),(3,2,6,7),(2,0,4,6)]); mesh.update()
slab=bpy.data.objects.new('Normal bake slab',mesh); scene.collection.objects.link(slab); slab.matrix_world=Matrix.Translation(pivot)
highs.append(slab)
image=bpy.data.images.new(asset+'_Normal_Refined',width=4096,height=4096,alpha=False); image.colorspace_settings.name='Non-Color'
material=patch.data.materials[0]; normal_node=next(n for n in material.node_tree.nodes if n.type=='TEX_IMAGE' and 'Normal' in n.image.name)
previous_image=normal_node.image
normal_node.image=image; material.node_tree.nodes.active=normal_node
bpy.data.images.remove(previous_image)
shader=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
saved_normal_socket=shader.inputs['Normal'].links[0].from_socket if shader.inputs['Normal'].is_linked else None
for link in list(shader.inputs['Normal'].links): material.node_tree.links.remove(link)
for o in scene.objects:
    o.select_set(False)
    if o.type=='MESH': o.hide_render=o not in highs and o!=patch
patch.hide_set(False); patch.hide_render=False; patch.hide_select=False; patch.select_set(True); bpy.context.view_layer.objects.active=patch
for high in highs: high.select_set(True)
scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=1
scene.render.bake.margin=16; scene.render.bake.margin_type='EXTEND'; scene.render.bake.use_clear=True
bpy.ops.object.bake(type='NORMAL',use_selected_to_active=True,normal_space='TANGENT',cage_extrusion=.014,max_ray_distance=.06)
if saved_normal_socket is not None: material.node_tree.links.new(saved_normal_socket,shader.inputs['Normal'])
image.name=asset+'_Normal'; image.filepath_raw=str(proof/'PreparedExports'/f'{asset}_Normal.png'); image.file_format='PNG'; image.save(); image.pack()
for high in highs: bpy.data.objects.remove(high,do_unlink=True)
bpy.context.view_layer.update()
bpy.data.libraries.write(str(proof/'prepared_patch.blend'),{patch},fake_user=True)
report['normal_bake_source']='Unclipped original stones preserve original corner shading; base faces below 5 mm use flat normals'
report['exported_files'][asset+'_Normal.png']=hashlib.sha256(Path(image.filepath).read_bytes()).hexdigest()
# Re-export shading changes with the same geometry, UVs and ground pivot.
for o in scene.objects: o.select_set(False)
patch.select_set(True); location=patch.location.copy(); patch.location=(0,0,0); bpy.context.view_layer.update()
bpy.ops.export_scene.fbx(filepath=str(proof/'PreparedExports'/f'{asset}.fbx'),use_selection=True,object_types={'MESH'},global_scale=1,
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,
    mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
patch.location=location; bpy.context.view_layer.update()
report['exported_files'][asset+'.fbx']=hashlib.sha256((proof/'PreparedExports'/f'{asset}.fbx').read_bytes()).hexdigest()
(proof/'preparation.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print('Refined normal map/cache/FBX; original scene not saved',flush=True)
