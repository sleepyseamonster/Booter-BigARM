"""Author periodic PBR graphs, bake Unity maps, and render matched material studies.
Background Blender --factory-startup --python-exit-code 1 --python THIS -- ROOT
"""
import bpy,math,sys,json,hashlib,time,shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve();HERE=Path(__file__).resolve().parent
OUT=ROOT/'outputs/Sable/MaterialDioramas20261015/OrganicLibrary';OUT.mkdir(parents=True,exist_ok=True)
MAPS=OUT/'Textures';MAPS.mkdir(exist_ok=True)
LOCAL=ROOT/'SourceArt/Blender/Studies/Sable/MaterialDioramas20261015/OrganicLibrary.blend'
SIZE=2048
ONLY=sys.argv[sys.argv.index('--family')+1] if '--family' in sys.argv else None
FAMILIES={'WornLeather':('Worn leather',(.055,.018,.009),(.23,.095,.035),.62),'Cloth':('Cloth — visual reference',(.09,.095,.075),(.29,.31,.25),.88),'Bone':('Weathered bone',(.24,.19,.12),(.64,.56,.39),.6),'Chitin':('Layered chitin',(.025,.045,.04),(.13,.22,.17),.38),'AgedWood':('Aged wood — visual reference',(.055,.033,.02),(.26,.17,.085),.83)}
STYLES={'R':('Restrained realism',.12),'E':('Combined direction',.38),'P':('Broader painted accents',.68)}
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
SC=bpy.context.scene;SC.render.engine='CYCLES';SC.cycles.samples=24;SC.cycles.use_denoising=True;SC.cycles.max_bounces=6
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
SC.cycles.device='GPU';SC.unit_settings.system='METRIC'
SC.view_settings.view_transform='AgX';SC.view_settings.look='AgX - Medium High Contrast';SC.view_settings.exposure=0
SC.render.resolution_x=1600;SC.render.resolution_y=1200;SC.render.resolution_percentage=100
SC.render.image_settings.file_format='PNG';SC.render.image_settings.color_mode='RGB'
SC.world.use_nodes=True;SC.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.23,.23,.23,1);SC.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5

def n(m,t,name=None):
    x=m.node_tree.nodes.new(t)
    if name:x.name=x.label=name
    return x
def link(m,a,b):m.node_tree.links.new(a,b)
def put(m,s,v):
    if hasattr(v,'node'):link(m,v,s)
    elif isinstance(v,tuple):s.default_value=v
    else:s.default_value=(v,v,v,1) if s.type=='RGBA' else v
def op(m,operation,a,b=None):
    x=n(m,'ShaderNodeMath');x.operation=operation;put(m,x.inputs[0],a)
    if b is not None:put(m,x.inputs[1],b)
    return x.outputs[0]
def mix(m,a,b,f,mode='MIX'):
    x=n(m,'ShaderNodeMixRGB');x.blend_type=mode;put(m,x.inputs[0],f);put(m,x.inputs[1],a);put(m,x.inputs[2],b);return x.outputs[0]
def ramp(m,f,entries,constant=False):
    x=n(m,'ShaderNodeValToRGB');x.color_ramp.interpolation='CONSTANT' if constant else 'EASE'
    for i,(pos,c) in enumerate(entries):
        e=x.color_ramp.elements[i] if i<2 else x.color_ramp.elements.new(pos);e.position=pos;e.color=(*c,1)
    link(m,f,x.inputs[0]);return x.outputs[0]
def periodic(m,u,v,scale,ru=1,rv=1):
    """4D torus embedding: analytic periodicity in both UV directions."""
    a=op(m,'MULTIPLY',u,2*math.pi*ru);b=op(m,'MULTIPLY',v,2*math.pi*rv)
    vec=n(m,'ShaderNodeCombineXYZ')
    for socket,val in zip(vec.inputs,[op(m,'COSINE',a),op(m,'SINE',a),op(m,'COSINE',b)]):link(m,val,socket)
    x=n(m,'ShaderNodeTexNoise');x.noise_dimensions='4D';x.inputs['Scale'].default_value=scale;x.inputs['Detail'].default_value=3;x.inputs['Roughness'].default_value=.68
    link(m,vec.outputs[0],x.inputs['Vector']);link(m,op(m,'SINE',b),x.inputs['W']);return x.outputs['Fac']
def wave(m,coord,count):return op(m,'ADD',op(m,'MULTIPLY',op(m,'SINE',op(m,'MULTIPLY',coord,2*math.pi*count)),.5),.5)

materials={};channels={}
for family,(label,lo,hi,roughness) in FAMILIES.items():
    m=bpy.data.materials.new(family+'_Authoring');m.use_nodes=True;m.use_fake_user=True
    bs=m.node_tree.nodes['Principled BSDF'];bs.inputs['Metallic'].default_value=0
    uv=n(m,'ShaderNodeTexCoord');sep=n(m,'ShaderNodeSeparateXYZ');link(m,uv.outputs['UV'],sep.inputs[0]);u,v=sep.outputs['X'],sep.outputs['Y']
    macro=periodic(m,u,v,.8);meso=periodic(m,u,v,5);fine=periodic(m,u,v,90)
    strokes=periodic(m,u,v,1.7,1,3);height=meso;rough=op(m,'ADD',roughness,op(m,'MULTIPLY',op(m,'SUBTRACT',fine,.5),.16))
    base=ramp(m,macro,[(.15,lo),(.85,hi)])
    if family=='WornLeather':
        wrinkle=wave(m,periodic(m,u,v,4,1,3),4)
        crease=op(m,'POWER',wrinkle,16)
        pores=op(m,'POWER',fine,4)
        height=op(m,'SUBTRACT',op(m,'MULTIPLY',pores,.35),op(m,'MULTIPLY',crease,.12))
        base=mix(m,base,(*lo,1),op(m,'MULTIPLY',crease,.18))
        base=mix(m,base,ramp(m,fine,[(.15,lo),(.85,hi)]),.18)
        burnish=op(m,'MULTIPLY',op(m,'MAXIMUM',op(m,'SUBTRACT',meso,.48),0),1.4)
        base=mix(m,base,(*hi,1),burnish);rough=op(m,'SUBTRACT',rough,op(m,'MULTIPLY',burnish,.2))
        bs.inputs['Sheen Weight'].default_value=.08
    elif family=='Cloth':
        warp=wave(m,u,170);weft=wave(m,v,170)
        twill=wave(m,op(m,'ADD',u,v),85)
        height=op(m,'ADD',op(m,'MULTIPLY',op(m,'MULTIPLY',warp,weft),.6),op(m,'MULTIPLY',twill,.14))
        height=op(m,'ADD',height,op(m,'MULTIPLY',fine,.08))
        base=mix(m,base,ramp(m,twill,[(.1,lo),(.9,hi)]),.18)
        base=mix(m,base,ramp(m,height,[(.1,lo),(.7,hi)]),.13);bs.inputs['Sheen Weight'].default_value=.4;bs.inputs['Sheen Roughness'].default_value=.7
    elif family=='Bone':
        striation=periodic(m,u,v,7,1,6);pits=op(m,'POWER',op(m,'SUBTRACT',1,fine),7)
        height=op(m,'SUBTRACT',op(m,'MULTIPLY',striation,.3),op(m,'MULTIPLY',pits,4))
        base=mix(m,base,(*lo,1),op(m,'MULTIPLY',pits,2.8))
        base=mix(m,base,ramp(m,striation,[(.1,lo),(.85,hi)]),.14);bs.inputs['Subsurface Weight'].default_value=.035
    elif family=='Chitin':
        growth=wave(m,op(m,'ADD',v,op(m,'MULTIPLY',periodic(m,u,v,1.2),.07)),18)
        height=op(m,'ADD',op(m,'MULTIPLY',growth,.4),op(m,'MULTIPLY',fine,.18))
        base=mix(m,base,ramp(m,growth,[(.2,lo),(.8,hi)]),.2)
        bs.inputs['Coat Weight'].default_value=.2;bs.inputs['Coat Roughness'].default_value=.3
    else:
        warp=op(m,'ADD',u,op(m,'MULTIPLY',periodic(m,u,v,1,1,2),.035))
        grain=op(m,'ADD',op(m,'MULTIPLY',wave(m,warp,39),.46),op(m,'ADD',op(m,'MULTIPLY',wave(m,warp,83),.34),op(m,'MULTIPLY',wave(m,warp,145),.20)))
        for cx,cy in [(.28,.42),(.73,.82)]:
            dx=op(m,'DIVIDE',op(m,'SINE',op(m,'MULTIPLY',op(m,'SUBTRACT',u,cx),math.pi)),math.pi)
            dy=op(m,'MULTIPLY',op(m,'DIVIDE',op(m,'SINE',op(m,'MULTIPLY',op(m,'SUBTRACT',v,cy),math.pi)),math.pi),.28)
            radius=op(m,'SQRT',op(m,'ADD',op(m,'MULTIPLY',dx,dx),op(m,'MULTIPLY',dy,dy)))
            mask=op(m,'MINIMUM',op(m,'MAXIMUM',op(m,'MULTIPLY',op(m,'SUBTRACT',.105,radius),18),0),1)
            rings=wave(m,op(m,'ADD',radius,op(m,'MULTIPLY',periodic(m,u,v,3),.006)),68)
            grain=mix(m,grain,rings,mask)
        splits=op(m,'POWER',op(m,'SUBTRACT',1,periodic(m,u,v,12,10,1)),8)
        height=op(m,'SUBTRACT',op(m,'ADD',op(m,'MULTIPLY',grain,.35),op(m,'MULTIPLY',fine,.14)),op(m,'MULTIPLY',splits,6))
        base=mix(m,base,ramp(m,grain,[(.12,lo),(.9,hi)]),.48);base=mix(m,base,(*lo,1),op(m,'MULTIPLY',splits,3))
        gray=ramp(m,macro,[(.2,(.07,.066,.058)),(.8,(.25,.23,.19))]);base=mix(m,base,gray,.28)
    # Intrinsic micro-cavity occlusion only; no directional scene lighting in the textures.
    ao=op(m,'MAXIMUM',op(m,'SUBTRACT',1,op(m,'MULTIPLY',op(m,'MAXIMUM',op(m,'SUBTRACT',.3,height),0),.16)),.65)
    roughcl=n(m,'ShaderNodeClamp');link(m,rough,roughcl.inputs[0]);rough=roughcl.outputs[0]
    bump=n(m,'ShaderNodeBump');bump.inputs['Strength'].default_value=.30;bump.inputs['Distance'].default_value=.004 if family=='Cloth' else .012
    link(m,height,bump.inputs['Height']);link(m,bump.outputs[0],bs.inputs['Normal']);link(m,rough,bs.inputs['Roughness'])
    color_outputs={}
    # Soft transitions retain broad pigment masses without camouflage-like cutouts.
    accent=ramp(m,strokes,[(.28,tuple(a*.80+b*.20 for a,b in zip(lo,hi))),(.53,tuple(a*.40+b*.60 for a,b in zip(lo,hi))),(.72,hi)])
    for code,(style,amount) in STYLES.items():
        color_outputs[code]=mix(m,base,accent,amount)
    link(m,color_outputs['E'],bs.inputs['Base Color'])
    packed=n(m,'ShaderNodeCombineColor');packed.mode='RGB';packed.inputs['Red'].default_value=0;link(m,ao,packed.inputs['Green']);packed.inputs['Blue'].default_value=1
    channels[family]={'colors':color_outputs,'rough':rough,'height':height,'maskrgb':packed.outputs[0],'smooth':op(m,'SUBTRACT',1,rough)}
    materials[family]=m

# The bake surface uses a complete, undistorted UV tile and a +Z tangent frame.
bpy.ops.mesh.primitive_plane_add(size=1);bakeob=bpy.context.object;bakeob.name='UV bake tile';SC.render.bake.use_clear=True;SC.render.bake.margin=0
em=bpy.data.materials.new('Bake emission temporary');em.use_nodes=True
receipt={'resolution':SIZE,'tile_span_metres':1,'normal_convention':'Tangent-space +Y/OpenGL; Unity normal import, flipGreenChannel=false','mask':'R metallic=0, G intrinsic cavity occlusion, B reserved=1, A smoothness','styles':STYLES,'families':{k:v[0] for k,v in FAMILIES.items()},'maps':[],'renders':[],'script_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'unity_proof':False}
if ONLY:
    previous=json.loads((HERE/'receipt.json').read_text())
    receipt['maps']=[x for x in previous['maps'] if x['family']!=ONLY];receipt['renders']=[x for x in previous['renders'] if x['family']!=ONLY]
    receipt['refinement']='Aged wood: irregular multi-frequency grain, two elongated knot fields and desaturated weathering. Other families retain their prior maps/captures.'
    receipt['previous_script_sha256']=previous['script_sha256']
def seal(): (HERE/'receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8',newline='\n')
def bake(family,kind,socket=None,alpha=None):
    if shutil.disk_usage(ROOT).free<900_000_000:raise RuntimeError('Insufficient free space; no unrelated cleanup authorized')
    mat=materials[family];tree=mat.node_tree;bs=tree.nodes['Principled BSDF'];output=tree.nodes['Material Output']
    image=bpy.data.images.new(family+'_'+kind,width=SIZE,height=SIZE,alpha=True,float_buffer=False)
    image.colorspace_settings.name='sRGB' if kind.startswith('BaseColor') else 'Non-Color'
    target=n(mat,'ShaderNodeTexImage');target.image=image;tree.nodes.active=target
    bakeob.data.materials.clear();bakeob.data.materials.append(mat)
    bpy.context.view_layer.objects.active=bakeob;bakeob.select_set(True)
    if kind=='Normal':
        link(mat,bs.outputs[0],output.inputs['Surface']);SC.cycles.samples=16;bpy.ops.object.bake(type='NORMAL',normal_space='TANGENT')
    else:
        emission=n(mat,'ShaderNodeEmission');link(mat,socket,emission.inputs['Color']);link(mat,emission.outputs[0],output.inputs['Surface']);SC.cycles.samples=1;bpy.ops.object.bake(type='EMIT');tree.nodes.remove(emission)
    if alpha:
        # Bake scalar alpha to a separate temporary image, then copy channel values verbatim.
        temp=bpy.data.images.new('Smoothness temporary',width=SIZE,height=SIZE,alpha=True);temp.colorspace_settings.name='Non-Color';target.image=temp
        emission=n(mat,'ShaderNodeEmission');link(mat,alpha,emission.inputs['Color']);link(mat,emission.outputs[0],output.inputs['Surface']);bpy.ops.object.bake(type='EMIT')
        import numpy as np
        a=np.empty(SIZE*SIZE*4,dtype=np.float32);b=np.empty_like(a);image.pixels.foreach_get(a);temp.pixels.foreach_get(b);a[3::4]=b[::4];image.pixels.foreach_set(a);image.update();tree.nodes.remove(emission);bpy.data.images.remove(temp)
    image.filepath_raw=str(MAPS/(family+'_'+kind+'.png'));image.file_format='PNG';image.save()
    receipt['maps'].append({'family':family,'kind':kind,'file':str(Path(image.filepath_raw).relative_to(ROOT)),'sha256':hashlib.sha256(Path(image.filepath_raw).read_bytes()).hexdigest()});seal()
    tree.nodes.remove(target);link(mat,bs.outputs[0],output.inputs['Surface']);bpy.data.images.remove(image)
    print('LIBRARY_BAKED',family,kind,flush=True)
for family in FAMILIES:
    if ONLY and family!=ONLY:continue
    for code in STYLES:bake(family,'BaseColor_'+code,channels[family]['colors'][code])
    bake(family,'Normal');bake(family,'Mask',channels[family]['maskrgb'],channels[family]['smooth'])
bpy.data.objects.remove(bakeob,do_unlink=True)

# Review only objects: the same flat, curved and context forms per family across variants.
def mat_for(family,code):
    mat=bpy.data.materials.new(family+'_'+code+'_Baked');mat.use_nodes=True;mat.use_fake_user=True;bs=mat.node_tree.nodes['Principled BSDF']
    for kind,inp in [('BaseColor_'+code,'Base Color'),('Normal','Normal'),('Mask','Roughness')]:
        tex=n(mat,'ShaderNodeTexImage');tex.image=bpy.data.images.load(str(MAPS/(family+'_'+kind+'.png')),check_existing=True);tex.extension='REPEAT';tex.image.colorspace_settings.name='sRGB' if kind.startswith('BaseColor') else 'Non-Color'
        if kind=='Normal':
            normal=n(mat,'ShaderNodeNormalMap');link(mat,tex.outputs['Color'],normal.inputs['Color']);link(mat,normal.outputs[0],bs.inputs[inp])
        elif kind=='Mask':link(mat,op(mat,'SUBTRACT',1,tex.outputs['Alpha']),bs.inputs[inp])
        else:link(mat,tex.outputs['Color'],bs.inputs[inp])
    # Match stock URP/Lit: no unsupported authoring sheen, coat or subsurface extras.
    return mat
BAKED={f:{s:mat_for(f,s) for s in STYLES} for f in FAMILIES}
def assign(ob,f):ob.data.materials.clear();ob.data.materials.append(BAKED[f]['E']);ob['family']=f
def cube(name,loc,scale,f):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=o.modifiers.new('Soft physical edges','BEVEL');mod.width=.04;mod.segments=3
    assign(o,f);return o
objects={}
for i,f in enumerate(FAMILIES):
    start=set(bpy.data.objects)
    cube(f+' flat sample',(-1.05,0,.07),(1.45,1.65,.14),f)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=64,ring_count=32,radius=.7,location=(.85,.15,.73));o=bpy.context.object;o.name=f+' curved sample';assign(o,f)
    for p in o.data.polygons:p.use_smooth=True
    if f=='WornLeather':
        o=cube('Folded hide / pouch',(.7,-1.3,.18),(1.25,.45,.34),f)
    elif f=='Cloth':
        verts=[];faces=[];N=48
        for y in range(N+1):
            for x in range(N+1):
                u=x/N;v=y/N;verts.append((u*1.6-.8,v*.7-1.8,.10+.12*math.sin(u*math.pi*7)**2+.06*v))
        for y in range(N):
            for x in range(N):
                a=y*(N+1)+x;faces.append((a,a+1,a+N+2,a+N+1))
        mesh=bpy.data.meshes.new('Draped reference mesh');mesh.from_pydata(verts,[],faces);mesh.uv_layers.new()
        for p in mesh.polygons:
            p.use_smooth=True
            for li in p.loop_indices:
                vi=mesh.loops[li].vertex_index;mesh.uv_layers.active.data[li].uv=((vi%(N+1))/N,(vi//(N+1))/N)
        o=bpy.data.objects.new('Draped reference',mesh);SC.collection.objects.link(o);assign(o,f)
    elif f=='Bone':
        bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=24,location=(0,-1.35,.22));o=bpy.context.object;o.scale=(.8,.17,.19);assign(o,f)
        for x in [-.68,.68]:
            bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,radius=.24,location=(x,-1.35,.24));assign(bpy.context.object,f)
    elif f=='Chitin':
        for x in [-.5,0,.5]:
            bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=24,radius=.5,location=(x,-1.3,.12));o=bpy.context.object;o.scale=(.65,.85,.32);assign(o,f)
    else:
        for x in [-.55,0,.55]:cube('Salvaged plank',(x,-1.35,.12),(.45,1,.24),f)
    objects[f]=list(set(bpy.data.objects)-start)

floor=bpy.data.materials.new('Neutral stage');floor.diffuse_color=(.10,.10,.10,1)
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.location.z=-.05;bpy.context.object.data.materials.append(floor)
def aim(ob,target):ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-5,5));CAM=bpy.context.object;SC.camera=CAM;CAM.data.type='ORTHO';CAM.data.ortho_scale=5.5;aim(CAM,(0,-.35,.2))
for name,loc,energy,size in [('Key',(-3,-4,6),750,4),('Fill',(4,0,4),500,4),('Rim',(-1,4,4),600,3)]:
    data=bpy.data.lights.new(name,'AREA');ob=bpy.data.objects.new(name,data);SC.collection.objects.link(ob);ob.location=loc;data.energy=energy;data.shape='DISK';data.size=size;aim(ob,(0,0,0))
SC.cycles.samples=48
for f in FAMILIES:
    if ONLY and f!=ONLY:continue
    for sn,obs in objects.items():
        for o in obs:o.hide_render=sn!=f
    for code in STYLES:
        for o in objects[f]:o.data.materials[0]=BAKED[f][code]
        path=OUT/(f+'_'+code+'.png');SC.render.filepath=str(path);bpy.ops.render.render(write_still=True)
        receipt['renders'].append({'family':f,'style':code,'file':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()});seal();print('LIBRARY_RENDER',f,code,flush=True)
for sn,obs in objects.items():
    for o in obs:o.hide_render=sn!='WornLeather';o.data.materials[0]=BAKED[sn]['E']
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(LOCAL))
receipt['native']=str(LOCAL.relative_to(ROOT));receipt['native_sha256']=hashlib.sha256(LOCAL.read_bytes()).hexdigest();receipt['complete']=True;seal();print('LIBRARY_COMPLETE',flush=True)
