"""Reopen original and refined native sources and check the declared invariants."""
import bpy,sys,json,hashlib
from pathlib import Path
ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve();HERE=Path(__file__).resolve().parent
receipt=json.loads((HERE/'receipt.json').read_text())
def graph(tree,socket=None):
    entries={}
    def visit(n):
        if n.name in entries:return
        e={'type':n.bl_idname,'settings':{},'inputs':[]};entries[n.name]=e
        for k in ['operation','blend_type','noise_dimensions','noise_type','normalize','feature','distance','voronoi_dimensions','wave_type','bands_direction','projection','extension','interpolation','attribute_name']:
            if hasattr(n,k):e['settings'][k]=getattr(n,k)
        if hasattr(n,'color_ramp'):e['ramp']=[(x.position,list(x.color)) for x in n.color_ramp.elements]
        if n.type=='TEX_IMAGE':e['image']=n.image.name
        if n.type=='TEX_COORD':e['object']=n.object.name if n.object else None
        for s in n.inputs:
            val=getattr(s,'default_value',None)
            if val is not None and not isinstance(val,(str,int,float,bool)):val=list(val)
            links=[(l.from_node.name,l.from_socket.name) for l in s.links]
            e['inputs'].append((s.name,val,links))
            for l in s.links:visit(l.from_node)
    if socket:
        for l in socket.links:visit(l.from_node)
        payload=[getattr(socket,'default_value',None),entries]
    else:
        for n in tree.nodes:visit(n)
        payload=entries
    return hashlib.sha256(json.dumps(payload,sort_keys=True,default=lambda x:list(x)).encode()).hexdigest()
def geometry():return {o.name:([list(v.co) for v in o.data.vertices],[list(r) for r in o.matrix_world]) for o in bpy.data.collections['yard'].objects if o.type=='MESH'}
response=['Metallic','Roughness','Normal','Coat Weight','Anisotropic']
bpy.ops.wm.open_mainfile(filepath=str(ROOT/receipt['source']))
original_geometry=geometry()
ground_names=['B_'+m+s for m in ['G01','G02','G03'] for s in (['',' faded deposits'] if m!='G01' else [''])]
grounds={n:graph(bpy.data.materials[n].node_tree) for n in ground_names}
baseline={mid:{s:graph(bpy.data.materials['B_'+mid].node_tree,bpy.data.materials['B_'+mid].node_tree.nodes['Principled BSDF'].inputs[s]) for s in response} for mid in [x['id'] for x in receipt['materials'] if not x['id'].startswith('G')]}
bpy.ops.wm.open_mainfile(filepath=str(ROOT/receipt['native']))
assert geometry()==original_geometry
assert all(graph(bpy.data.materials[n].node_tree)==d for n,d in grounds.items())
for mid,checks in baseline.items():
    mat=bpy.data.materials['E_'+mid]
    assert all(graph(mat.node_tree,mat.node_tree.nodes['Principled BSDF'].inputs[s])==d for s,d in checks.items()),mid
    assert any(n.type=='GROUP' and n.node_tree.name.startswith('Retained C pigment') for n in mat.node_tree.nodes)
images=[{'name':i.name,'packed':bool(i.packed_file)} for i in bpy.data.images if i.source=='FILE'];assert all(i['packed'] for i in images)
assert all(o.active_material.name==('B_'+o['surface_id']+(' faded deposits' if o.get('role')=='Ground deposit' else '') if o['surface_id'].startswith('G') else 'E_'+o['surface_id']) for o in bpy.data.collections['yard'].objects if o.get('surface_id'))
for f,k in [(receipt['source'],'source_sha256'),(receipt['native'],'native_sha256')]:assert hashlib.sha256((ROOT/f).read_bytes()).hexdigest()==receipt[k]
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
bpy.context.scene.cycles.device='GPU'
path=ROOT/'outputs/Sable/MaterialDioramas20261015/refined_BC/reopened_close.png';bpy.context.scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
report={'geometry_and_transforms_match':True,'exact_B_ground_graphs':grounds,'B_response_preserved_on_all_12_other_surfaces':True,'response_inputs':response,'C_pigment_groups':12,'packed_images':images,'yard_material_assignments_verified':True,'source_and_native_hashes_match':True,'reopened_render':str(path.relative_to(ROOT)),'unity_proof':False}
(HERE/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n')
print('REFINED_REOPEN_OK',flush=True)
