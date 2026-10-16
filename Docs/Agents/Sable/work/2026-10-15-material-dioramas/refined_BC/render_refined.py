"""Refine the sealed workyard without altering its ground, geometry or lighting.
Blender --background MaterialDioramas.blend --python-exit-code 1 --python THIS -- ROOT
"""
import ast,bpy,json,hashlib,sys,time
from pathlib import Path
from mathutils import Matrix
ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
HERE=Path(__file__).resolve().parent
PARENT=HERE.parent
RAW=ROOT/'outputs/Sable/MaterialDioramas20261015'
OUT=RAW/'refined_BC';OUT.mkdir(exist_ok=True)
LOCAL=ROOT/'SourceArt/Blender/Studies/Sable/MaterialDioramas20261015/Refined_BC.blend'
SOURCE=Path(bpy.data.filepath)
SOURCE_HASH=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
old=json.loads((PARENT/'render_receipt.json').read_text())
SC=bpy.context.scene;CAM=SC.camera
KEY=bpy.data.objects['Broad reflection key'];FILL=bpy.data.objects['Broad sky fill'];RIM=bpy.data.objects['Rim bounce'];SUN=bpy.data.objects['Low sunset'];sd=SUN.data
lightfunc=next(n for n in ast.parse((PARENT/'tools/build_study.py').read_text()).body if isinstance(n,ast.FunctionDef) and n.name=='lights')
exec(compile(ast.Module(body=[lightfunc],type_ignores=[]),'retained lighting recipe','exec'),globals())

def color_group(mat):
    """Clone the saved C color ancestors exactly; expose color, not its BSDF."""
    tree=mat.node_tree;bs=tree.nodes.get('Principled BSDF')
    source=bs.inputs['Base Color'].links[0].from_socket
    group=bpy.data.node_groups.new('Retained C pigment '+mat.name,'ShaderNodeTree')
    group.interface.new_socket(name='Color',in_out='OUTPUT',socket_type='NodeSocketColor')
    output=group.nodes.new('NodeGroupOutput');copies={}
    def clone(n):
        if n.name in copies:return copies[n.name]
        dest=group.nodes.new(n.bl_idname);copies[n.name]=dest;dest.name=n.name;dest.label=n.label
        for prop in ['operation','blend_type','noise_dimensions','noise_type','normalize','feature','distance','voronoi_dimensions','wave_type','bands_direction','rings_direction','wave_profile','extension','projection','projection_blend','interpolation','attribute_name']:
            if hasattr(n,prop) and hasattr(dest,prop):setattr(dest,prop,getattr(n,prop))
        if n.type=='TEX_IMAGE':dest.image=n.image
        if n.type=='TEX_COORD':dest.object=n.object
        if hasattr(n,'color_ramp'):
            a=n.color_ramp;b=dest.color_ramp;b.interpolation=a.interpolation;b.color_mode=a.color_mode;b.hue_interpolation=a.hue_interpolation
            for i,e in enumerate(a.elements):
                de=b.elements[i] if i<2 else b.elements.new(e.position);de.position=e.position;de.color=e.color
        for i,s in enumerate(n.inputs):
            if hasattr(s,'default_value'):
                try:dest.inputs[i].default_value=s.default_value
                except (TypeError,ValueError):pass
            for link in s.links:
                upstream=clone(link.from_node)
                idx=list(link.from_node.outputs).index(link.from_socket)
                group.links.new(upstream.outputs[idx],dest.inputs[i])
        return dest
    root=clone(source.node);idx=list(source.node.outputs).index(source)
    group.links.new(root.outputs[idx],output.inputs[0]);return group

materials={};records=[]
for mid in old['materials']:
    b=bpy.data.materials['B_'+mid]
    if mid.startswith('G'):
        materials[mid]=b;records.append({'id':mid,'method':'Exact existing B material; no changes'});continue
    mat=b.copy();mat.name='E_'+mid;mat.use_fake_user=True
    nt=mat.node_tree;bs=nt.nodes.get('Principled BSDF');base=bs.inputs['Base Color'].links[0].from_socket
    cnode=nt.nodes.new('ShaderNodeGroup');cnode.node_tree=color_group(bpy.data.materials['C_'+mid]);cnode.label='Retained C broad pigment'
    geom=nt.nodes.new('ShaderNodeNewGeometry');sep=nt.nodes.new('ShaderNodeSeparateXYZ');nt.links.new(geom.outputs['Normal'],sep.inputs[0])
    weight=nt.nodes.new('ShaderNodeMapRange');weight.clamp=True;nt.links.new(sep.outputs['Z'],weight.inputs['Value'])
    weight.inputs['From Min'].default_value=-.4;weight.inputs['From Max'].default_value=.8
    # These are shader controls, not a claim that an art style has numerical percentages.
    rock=mid.startswith(('S','O'));weight.inputs['To Min'].default_value=.28 if rock else .18;weight.inputs['To Max'].default_value=.58 if rock else .42
    blend=nt.nodes.new('ShaderNodeMixRGB');blend.label='Broader pigment on exposed planes'
    nt.links.new(weight.outputs[0],blend.inputs[0]);nt.links.new(base,blend.inputs[1]);nt.links.new(cnode.outputs[0],blend.inputs[2]);nt.links.new(blend.outputs[0],bs.inputs['Base Color'])
    mat['style']='E / refined B-C';mat['provenance']='B material response; retained C color graph blended by exposed-plane normal. Exact B ground.'
    materials[mid]=mat;records.append({'id':mid,'method':'B roughness, metallic, relief, wear coverage and coatings; C pigment on exposed planes','color_weight_range':[weight.inputs['To Min'].default_value,weight.inputs['To Max'].default_value]})

def assign(scene,group=None):
    for cn in ['yard','gate','quarry','plates','control']:
        for ob in bpy.data.collections[cn].objects:
            mid=ob.get('surface_id');ob.hide_render=cn!=scene
            if scene=='plates' and cn=='plates':ob.hide_render=not mid or mid not in group
            if mid and cn!='control':
                ob.data.materials[0]=bpy.data.materials['B_'+mid+' faded deposits'] if ob.get('role')=='Ground deposit' else materials[mid]

before={o.name:([list(v.co) for v in o.data.vertices],[list(r) for r in o.matrix_world]) for o in bpy.data.collections['yard'].objects if o.type=='MESH'}
assign('yard');lights('sunset');RIM.data.color=(1,.72,.42)
SC.cycles.samples=64;SC.cycles.use_denoising=True
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
SC.cycles.device='GPU'
SC.render.resolution_x=2560;SC.render.resolution_y=1440
SC.render.image_settings.file_format='PNG';SC.render.image_settings.color_mode='RGB'
SC.render.filepath=str(OUT/'yard_E_sunset_close.png')
cp=next(r['camera'] for r in old['renders'] if r['kind']=='controlled' and r['scene']=='yard' and r['view']=='close');CAM.matrix_world=Matrix(cp['matrix'])
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(LOCAL))
receipt={'source':str(SOURCE.relative_to(ROOT)),'source_sha256':SOURCE_HASH,'script_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'native':str(LOCAL.relative_to(ROOT)),'native_sha256':hashlib.sha256(LOCAL.read_bytes()).hexdigest(),'materials':records,'ground':'Exact B G01/G02/G03 and faded deposits, unchanged','geometry_unchanged':True,'samples':64,'renders':[],'unity_proof':False}
captures=[('yard','sunset','close',None),('yard','sunset','gameplay',None),('yard','twilight','close',None),('yard','neutral','gameplay',None),('plates','neutral','plate','metal'),('plates','neutral','plate','stone')]
groups={'metal':['M01','M02','M03','M04','M05','M06'],'stone':['S01','S02','S03','S04','O01']}
for scene,light,view,group in captures:
    assign(scene,groups.get(group));RIM.data.color=(1,.72,.42);lights(light)
    row=next(r for r in old['renders'] if r['scene']==scene and r['light']==light and r['view']==view and r.get('group')==group and r['kind'] in ['controlled','neutral_diagnostic','material'])
    CAM.matrix_world=Matrix(row['camera']['matrix']);bpy.context.view_layer.update()
    path=OUT/f'{scene}_E_{light}_{view}{"_"+group if group else ""}.png'
    SC.render.filepath=str(path);start=time.time();bpy.ops.render.render(write_still=True)
    receipt['renders'].append({'file':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'camera':row['camera'],'scene':scene,'light':light,'view':view,'group':group,'seconds':time.time()-start})
    (HERE/'receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8',newline='\n')
    print('REFINED_CAPTURE',path.name,flush=True)
after={o.name:([list(v.co) for v in o.data.vertices],[list(r) for r in o.matrix_world]) for o in bpy.data.collections['yard'].objects if o.type=='MESH'}
assert before==after,'Geometry or transforms changed'
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==SOURCE_HASH,'Original source changed'
receipt['complete']=True
(HERE/'receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8',newline='\n')
print('REFINED_COMPLETE',flush=True)
