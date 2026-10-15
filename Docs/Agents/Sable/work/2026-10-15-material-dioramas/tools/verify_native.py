"""Run against a clean scratch copy of the saved study via background Blender.
Blender -b SCRATCH.blend --python-exit-code 1 --python verify_native.py -- ROOT
"""
import bpy,bmesh,ast,sys,json,hashlib,os
from pathlib import Path
ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
HERE=Path(__file__).resolve().parent.parent
OUT=ROOT/'outputs/Sable/MaterialDioramas20261015'
receipt=json.loads((OUT/'render_receipt.json').read_text(encoding='utf-8'))
SC=bpy.context.scene
images=[]
for im in bpy.data.images:
    if im.source in ['GENERATED','VIEWER']:continue
    assert im.packed_file is not None,('Unpacked image dependency',im.name,im.filepath)
    first_pixel=im.pixels[0]  # Packed images load lazily after a background reopen.
    assert im.has_data and im.size[0]>0,('Missing pixels',im.name)
    images.append({'name':im.name,'packed':True,'dimensions':list(im.size),'packed_sha256':hashlib.sha256(bytes(im.packed_file.data)).hexdigest()})
assert len(images)>=3
materials={m.name for m in bpy.data.materials}
assert all(f'{s}_{mid}' in materials for s in 'ABCD' for mid in receipt['materials'])
inventory={sn:len([o for o in bpy.data.objects if o.name.startswith(sn+'_') and o.type in ['MESH','CURVE']]) for sn in ['yard','gate','quarry']}
assert all(v>0 for v in inventory.values())
topology=[]
coverage={sn:{mid:[] for mid in receipt['materials']} for sn in inventory}
for ob in bpy.data.objects:
    if ob.get('surface_id') and ob.name.split('_')[0] in coverage:
        coverage[ob.name.split('_')[0]][ob['surface_id']].append(ob.name)
    if ob.type!='MESH' or not ob.get('study_id'):continue
    bm=bmesh.new();bm.from_mesh(ob.data)
    loose=sum(not v.link_edges for v in bm.verts);boundary=sum(e.is_boundary for e in bm.edges);other=sum(not e.is_manifold and not e.is_boundary for e in bm.edges)
    expected_open=ob.get('role') in ['Continuous ground','Ground deposit']
    assert loose==0,(ob.name,'loose vertices')
    assert other==0,(ob.name,'nonmanifold junction')
    assert expected_open or boundary==0,(ob.name,'unexpected open boundary')
    topology.append({'object':ob.name,'vertices':len(bm.verts),'faces':len(bm.faces),'boundary_edges':boundary,'intentional_open_ground':expected_open});bm.free()
for sn,byid in coverage.items():assert all(byid.values()),('Missing surface coverage',sn)
def value(v):
    if isinstance(v,(str,float,int,bool)) or v is None:return v
    try:return list(v)
    except TypeError:return str(v)
graphs={}
for m in bpy.data.materials:
    if not m.use_nodes:continue
    entries=[]
    for n in m.node_tree.nodes:
        e={'name':n.name,'type':n.bl_idname,'operation':getattr(n,'operation',None),'inputs':[(s.name,value(s.default_value)) for s in n.inputs if hasattr(s,'default_value')]}
        if hasattr(n,'color_ramp'):e['ramp']=[(p.position,list(p.color)) for p in n.color_ramp.elements];e['interpolation']=n.color_ramp.interpolation
        if n.type=='TEX_IMAGE':e['image']=n.image.name;e['projection']=n.projection;e['interpolation']=n.interpolation
        if n.type=='TEX_COORD':e['object']=n.object.name if n.object else None
        entries.append(e)
    graphs[m.name]={'nodes':entries,'links':[(l.from_node.name,l.from_socket.name,l.to_node.name,l.to_socket.name) for l in m.node_tree.links]}
(HERE/'material_manifest.json').write_text(json.dumps({'graphs':graphs,'coverage':coverage,'topology':topology,'source':'Saved packed native source, inspected on reopen'},indent=2),encoding='utf-8')
if '--inspect-only' in sys.argv:
    (HERE/'native_inventory.json').write_text(json.dumps({'packed_images':images,'inventory':inventory,'materials':len(materials),'mesh_topology_checks':len(topology),'all_surfaces_covered':True},indent=2),encoding='utf-8')
    print('SABLE_NATIVE_INVENTORY_OK',inventory,len(topology),flush=True)
    sys.exit(0)
# Resolve the exact shared lighting recipes against the saved rig and retain numeric settings.
tree=ast.parse((HERE/'tools/build_study.py').read_text(encoding='utf-8'))
lightfunc=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='lights')
KEY=bpy.data.objects['Broad reflection key'];FILL=bpy.data.objects['Broad sky fill'];RIM=bpy.data.objects['Rim bounce'];SUN=bpy.data.objects['Low sunset'];sd=SUN.data
original_lights={o.name:(o.data.energy,tuple(o.data.color)) for o in [KEY,FILL,RIM,SUN]}
original_world=(tuple(SC.world.node_tree.nodes['Background'].inputs['Color'].default_value),SC.world.node_tree.nodes['Background'].inputs['Strength'].default_value)
exec(compile(ast.Module(body=[lightfunc],type_ignores=[]),'shared lighting recipe','exec'),globals())
rigs={}
for mode in ['sunset','twilight','neutral']:
    lights(mode)
    rigs[mode]={'lights':{o.name:{'type':o.data.type,'energy':o.data.energy,'color':list(o.data.color),'size':getattr(o.data,'size',None),'angle':getattr(o.data,'angle',None),'matrix':[list(r) for r in o.matrix_world]} for o in [KEY,FILL,RIM,SUN]},'world_color':list(SC.world.node_tree.nodes['Background'].inputs['Color'].default_value),'world_strength':SC.world.node_tree.nodes['Background'].inputs['Strength'].default_value}
rigs['whole_style_sunset']=json.loads(json.dumps(rigs['sunset']))
rigs['whole_style_sunset']['lights']['Rim bounce']['color']=[1,1,1]
(HERE/'lighting_manifest.json').write_text(json.dumps({'rigs':rigs,'exposure':SC.view_settings.exposure,'camera_aspect':SC.render.resolution_x/SC.render.resolution_y,'same_recipe_within_each_comparison':True,'mode_difference':'Whole-style pilot retains the neutral rim color from the preceding diagnostic. All four pilot candidates share it; do not compare modes as a geometry-only experiment.'},indent=2),encoding='utf-8')
for o in [KEY,FILL,RIM,SUN]:o.data.energy,o.data.color=original_lights[o.name]
SC.world.node_tree.nodes['Background'].inputs['Color'].default_value=original_world[0]
SC.world.node_tree.nodes['Background'].inputs['Strength'].default_value=original_world[1]
# The sealed main source is saved in B / yard / sunset / gameplay before captures.
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
SC.cycles.device='GPU'
path=OUT/'native_reproduction.png';SC.render.filepath=str(path)
bpy.ops.render.render(write_still=True)
reference=next(r for r in receipt['renders'] if (r['scene'],r['style'],r['light'],r['view'],r['kind'])==('yard','B','sunset','gameplay','controlled'))
report={'opened_source':bpy.data.filepath,'packed_images':images,'inventory':inventory,'materials':len(materials),'mesh_topology_checks':len(topology),'material_manifest_sha256':hashlib.sha256((HERE/'material_manifest.json').read_bytes()).hexdigest(),'reference':reference['file'],'reproduction':str(path.relative_to(ROOT)),'reproduction_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'exact_hash_match':hashlib.sha256(path.read_bytes()).hexdigest()==reference['sha256'],'render_settings':{'samples':SC.cycles.samples,'resolution':[SC.render.resolution_x,SC.render.resolution_y],'exposure':SC.view_settings.exposure},'unity_proof':False}
(HERE/'native_verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('SABLE_NATIVE_VERIFIED',json.dumps(report),flush=True)
