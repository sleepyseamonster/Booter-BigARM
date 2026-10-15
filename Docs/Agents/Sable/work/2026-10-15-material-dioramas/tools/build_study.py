"""Background-only authored dioramas. No production asset writes.
Blender --background --factory-startup --python build_study.py -- ROOT [--preview] [--scene yard]
"""
import bpy, math, random, hashlib, json, sys, time
from pathlib import Path
from mathutils import Vector

ARGS=sys.argv[sys.argv.index('--')+1:]
ROOT=Path(ARGS[0]).resolve()
HERE=Path(__file__).resolve().parent.parent
LOCAL=ROOT/'SourceArt/Blender/Studies/Sable/MaterialDioramas20261015'
OUT=ROOT/'outputs/Sable/MaterialDioramas20261015'
PREVIEW='--preview' in ARGS
ONLY=ARGS[ARGS.index('--scene')+1] if '--scene' in ARGS else None
OLD=HERE.parent/'2026-10-15-stylized-realism'
ATLAS=OLD/'textures/B_subtle_atlas.png'
for p in [LOCAL, OUT, OUT/'renders', OUT/'previews']: p.mkdir(parents=True,exist_ok=True)
STYLES={'A':'Grounded realism','B':'Painted realism','C':'Expressive painterly','D':'Graphic simplification'}
LABELS={'M01':'Clean processed metal','M02':'Brushed metal','M03':'Bare forged iron','M04':'Blackened cast iron','M05':'Partial rust','M06':'Heavy rust','S01':'Dense basalt','S02':'Porous volcanic','S03':'Granite','S04':'Limestone','G01':'Compacted dirt','G02':'Dry fines','G03':'Gravelly dirt','O01':'Iron-rich rock','C01':'Ceramic'}
SOURCES={str(ATLAS.relative_to(ROOT)):hashlib.sha256(ATLAS.read_bytes()).hexdigest()}
ROCKDIR=ROOT/'Assets/_Project/Art/Environment/Rocks/SculptedBasalt01'
for p in [ROCKDIR/'SculptedBasalt01.fbx',ROCKDIR/'SculptedBasalt01_BaseColor.png',OLD/'textures/C_painterly_atlas.png']:
    SOURCES[str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
camera_source=ROOT/'Assets/_Project/Scenes/Production/GreaterWasteland.unity'
SOURCES[str(camera_source.relative_to(ROOT))]=hashlib.sha256(camera_source.read_bytes()).hexdigest()
SOURCES[str(Path(__file__).resolve().relative_to(ROOT))]=hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
random.seed(151026)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
SC=bpy.context.scene
SC.render.engine='CYCLES'
SC.cycles.device='GPU'
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX'; prefs.get_devices()
for device in prefs.devices: device.use=device.type=='OPTIX'
SC.cycles.samples=12 if PREVIEW else 40
SC.cycles.use_denoising=True
SC.cycles.max_bounces=6; SC.cycles.diffuse_bounces=3; SC.cycles.glossy_bounces=3
SC.render.resolution_x=960 if PREVIEW else 2560
SC.render.resolution_y=540 if PREVIEW else 1440
SC.render.resolution_percentage=100
SC.render.image_settings.file_format='PNG'; SC.render.image_settings.color_mode='RGB'
SC.render.image_settings.color_depth='8'
SC.view_settings.view_transform='AgX'; SC.view_settings.look='AgX - Medium High Contrast'; SC.view_settings.exposure=0
SC.world.use_nodes=True
SC.unit_settings.system='METRIC'

def node(m,t,name=None):
    n=m.node_tree.nodes.new(t)
    if name:n.name=n.label=name
    return n
def wire(m,a,b):m.node_tree.links.new(a,b)
def setin(m,s,v):
    if isinstance(v,(int,float)):s.default_value=(v,v,v,1) if s.type=='RGBA' else v
    elif isinstance(v,tuple):s.default_value=v
    else:wire(m,v,s)
def mathn(m,op,a,b=None):
    n=node(m,'ShaderNodeMath'); n.operation=op
    setin(m,n.inputs[0],a)
    if b is not None:setin(m,n.inputs[1],b)
    return n.outputs[0]
def vec(m,a,scale):
    n=node(m,'ShaderNodeVectorMath'); n.operation='MULTIPLY';wire(m,a,n.inputs[0]);n.inputs[1].default_value=scale;return n.outputs[0]
def noise(m,coord,scale,detail=2):
    n=node(m,'ShaderNodeTexNoise');wire(m,coord,n.inputs['Vector']);n.inputs['Scale'].default_value=scale;n.inputs['Detail'].default_value=detail;return n.outputs['Fac']
def ramp(m,fac,colors,constant=False):
    n=node(m,'ShaderNodeValToRGB');r=n.color_ramp;r.interpolation='CONSTANT' if constant else 'LINEAR'
    while len(r.elements)>2:r.elements.remove(r.elements[-1])
    for i,(pos,col) in enumerate(colors):
        e=r.elements[i] if i<2 else r.elements.new(pos);e.position=pos;e.color=(*col[:3],1)
    wire(m,fac,n.inputs[0]);return n.outputs[0]
def mix(m,a,b,f,mode='MIX'):
    n=node(m,'ShaderNodeMixRGB');n.blend_type=mode;setin(m,n.inputs[0],f);setin(m,n.inputs[1],a);setin(m,n.inputs[2],b);return n.outputs[0]
def rgba(c):return (*c[:3],1)
def painted_stone_patch(m,coord):
    # Sample only an interior painted island; blend projections in world space.
    # This avoids treating the old generated UV-island layout as a production unwrap.
    sep=node(m,'ShaderNodeSeparateXYZ');wire(m,coord,sep.inputs[0])
    geo=node(m,'ShaderNodeNewGeometry');absolute=node(m,'ShaderNodeVectorMath');absolute.operation='ABSOLUTE';wire(m,geo.outputs['Normal'],absolute.inputs[0])
    ns=node(m,'ShaderNodeSeparateXYZ');wire(m,absolute.outputs[0],ns.inputs[0])
    total=mathn(m,'ADD',mathn(m,'ADD',ns.outputs[0],ns.outputs[1]),ns.outputs[2])
    result=(0,0,0,1)
    for axis,pair in enumerate([(1,2),(0,2),(0,1)]):
        uv=node(m,'ShaderNodeCombineXYZ')
        for k in range(2):
            f=mathn(m,'FRACT',mathn(m,'MULTIPLY',sep.outputs[pair[k]],.7))
            v=mathn(m,'ADD',mathn(m,'MULTIPLY',f,(90/2172 if k==0 else 100/724)),(300/2172 if k==0 else 200/724))
            wire(m,v,uv.inputs[k])
        im=node(m,'ShaderNodeTexImage','Interior painted stone pigment patch');im.image=bpy.data.images.load(str(OLD/'textures/C_painterly_atlas.png'),check_existing=True);wire(m,uv.outputs[0],im.inputs[0])
        weight=mathn(m,'DIVIDE',ns.outputs[axis],total)
        weighted=mix(m,(0,0,0,1),im.outputs['Color'],weight)
        result=mix(m,result,weighted,1,'ADD')
    return result
def atlas_ground(m,coord,legacy=False):
    # Exact original B panel bounds/color grading; derivative blends phase-shifted tiles.
    mapped=vec(m,coord,(.19,.19,.19))
    def sample(uv):
        f=node(m,'ShaderNodeVectorMath');f.operation='FRACTION';wire(m,uv,f.inputs[0])
        v=vec(m,f.outputs[0],(795/2172,720/724,1))
        add=node(m,'ShaderNodeVectorMath');add.operation='ADD';wire(m,v,add.inputs[0]);add.inputs[1].default_value=(649/2172,2/724,0)
        im=node(m,'ShaderNodeTexImage','Preserved accepted B ground atlas');im.image=bpy.data.images.load(str(ATLAS),check_existing=True);im.extension='REPEAT';wire(m,add.outputs[0],im.inputs[0]);return im.outputs['Color']
    c=sample(mapped)
    if not legacy:
        off=node(m,'ShaderNodeVectorMath');off.operation='ADD';wire(m,mapped,off.inputs[0]);off.inputs[1].default_value=(.37,.63,0)
        c=mix(m,c,sample(off.outputs[0]),noise(m,coord,.32))
    gray=node(m,'ShaderNodeRGBToBW');wire(m,c,gray.inputs[0])
    return mix(m,mix(m,c,gray.outputs[0],.6),(.55,.48,.40,1),1,'MULTIPLY')

BASE={
 'M01':((.16,.19,.22),(.34,.38,.41),.23,1),
 'M02':((.16,.19,.22),(.29,.33,.36),.39,1),
 'M03':((.10,.115,.13),(.24,.255,.27),.55,1),
 'M04':((.022,.027,.032),(.075,.085,.095),.65,1),
 'M05':((.18,.21,.23),(.34,.36,.37),.41,1),
 'M06':((.14,.055,.022),(.38,.17,.055),.83,0),
 'S01':((.038,.046,.059),(.14,.16,.18),.86,0),
 'S02':((.075,.035,.026),(.22,.12,.075),.93,0),
 'S03':((.20,.18,.16),(.47,.43,.36),.77,0),
 'S04':((.19,.16,.11),(.40,.35,.26),.87,0),
 'G01':((.085,.049,.028),(.26,.17,.095),.92,0),
 'G02':((.12,.075,.040),(.27,.18,.105),.98,0),
 'G03':((.095,.065,.042),(.28,.21,.14),.91,0),
 'O01':((.06,.043,.037),(.24,.11,.055),.85,0),
 'C01':((.055,.14,.15),(.15,.32,.31),.34,0)}

def material(mid,style,legacy=False):
    m=bpy.data.materials.new(f'{style}_{mid}'+('_legacy' if legacy else ''));m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');tc=node(m,'ShaderNodeTexCoord')
    # A single global reference gives metre-based texture scale independent of object size.
    tc.object=REFERENCE;co=tc.outputs['Object']
    idx='ABCD'.index(style)
    macro=noise(m,co,1.7,2)
    fine=noise(m,co,[90,40,20,8][idx],2)
    stretched=vec(m,co,(.5,2.4,.8) if mid.startswith('G') else (1.5,7,2.3))
    strokes=noise(m,stretched,[2.4,1.9,1.3,.8][idx],1)
    lo,hi,rough,metal=BASE[mid]
    fac=mix(m,macro,strokes,[.05,.24,.65,.1][idx])
    # MixRGB fac is grayscale here; constant ramps make D's color grouping explicit.
    if style=='D':
        c=ramp(m,macro,[(.25,lo),(.50,tuple((a+b)/2 for a,b in zip(lo,hi))),(.70,hi)],True)
    else:c=ramp(m,fac,[(.18,lo),(.78,hi)])
    c=mix(m,c,fine,[.19,.07,.025,0][idx],'MULTIPLY')
    height=fine
    if mid=='S03':
        vo=node(m,'ShaderNodeTexVoronoi','Granite mineral aggregate');vo.feature='F1';vo.inputs['Scale'].default_value=65 if style=='A' else 38
        wire(m,co,vo.inputs[0]);bw=node(m,'ShaderNodeRGBToBW');wire(m,vo.outputs['Color'],bw.inputs[0])
        mineral=ramp(m,bw.outputs[0],[(.24,(.06,.065,.07)),(.43,(.34,.29,.23)),(.62,(.57,.54,.46)),(.79,(.25,.21,.19))],style!='A')
        c=mix(m,c,mineral,[.65,.47,.33,.18][idx]);height=vo.outputs['Distance']
    if mid=='S04':
        wave=node(m,'ShaderNodeTexWave');wave.wave_type='BANDS';wave.bands_direction='Z';wave.inputs['Scale'].default_value=4;wave.inputs['Distortion'].default_value=2
        wire(m,co,wave.inputs[0]);c=mix(m,c,ramp(m,wave.outputs[0],[(.1,lo),(.9,hi)]),.16)
    if mid=='S02':
        vo=node(m,'ShaderNodeTexVoronoi','Vesicles');vo.inputs['Scale'].default_value=27;wire(m,co,vo.inputs[0])
        holes=mathn(m,'LESS_THAN',vo.outputs['Distance'],.21)
        c=mix(m,c,(.012,.008,.006,1),holes);height=mathn(m,'SUBTRACT',fine,holes)
    if mid in ['M05','M06']:
        # Identical meter-space mask, independent of style; oxide is dielectric.
        coverage=noise(m,vec(m,co,(2,2,3)),2.9,3)
        edge=.55 if mid=='M05' else .34
        oxide=mathn(m,'MULTIPLY',mathn(m,'SUBTRACT',coverage,edge),18)
        cl=node(m,'ShaderNodeClamp');wire(m,oxide,cl.inputs[0]);oxide=cl.outputs[0]
        rust=ramp(m,noise(m,co,16),[(.2,(.05,.016,.007)),(.50,(.15,.05,.014)),(.8,(.32,.145,.045))],style=='D')
        c=mix(m,c,rust,oxide)
        setin(m,bs.inputs['Metallic'],mathn(m,'SUBTRACT',1,oxide))
        setin(m,bs.inputs['Roughness'],mix(m,.38,.86,oxide))
        height=mix(m,fine,noise(m,co,48),oxide)
    else:
        bs.inputs['Metallic'].default_value=metal
        roughvar=mathn(m,'ADD',rough,mathn(m,'MULTIPLY',mathn(m,'SUBTRACT',fine,.5),.12))
        setin(m,bs.inputs['Roughness'],roughvar)
    if mid in ['M01','M02']:
        bs.inputs['Anisotropic'].default_value=.2 if mid=='M01' else .62
        scratches=noise(m,vec(m,co,(1,160,160)),3,1);height=mix(m,fine,scratches,.55)
    if mid=='G01' and (style=='B' or legacy):c=atlas_ground(m,co,legacy)
    if mid=='O01':
        vein=ramp(m,macro,[(.35,(.035,.025,.023)),(.6,(.32,.12,.035))]);c=mix(m,c,vein,.6)
    if style=='C':
        accent=ramp(m,strokes,[(.22,lo),(.52,hi),(.76,tuple(min(1,x*1.2) for x in hi))],True)
        c=mix(m,c,accent,.14 if mid.startswith('G') else .26)
    if mid=='G01' and style=='C':
        broad=ramp(m,noise(m,co,.28,1),[(.18,(.095,.055,.030)),(.82,(.24,.16,.09))])
        c=mix(m,atlas_ground(m,co),broad,.65)
    if mid=='G01' and style=='D':
        c=ramp(m,noise(m,co,.24,1),[(.28,(.12,.075,.045)),(.50,(.16,.105,.06)),(.72,(.21,.145,.085))],True)
    if mid=='S01' and style=='A':
        im=node(m,'ShaderNodeTexImage','Basalt authored albedo')
        if style=='A':
            im.image=bpy.data.images.load(str(ROCKDIR/'SculptedBasalt01_BaseColor.png'),check_existing=True)
            texcolor=im.outputs['Color']
        else:
            atlaspath=ATLAS if style=='B' else OLD/'textures/C_painterly_atlas.png'
            im.image=bpy.data.images.load(str(atlaspath),check_existing=True)
            frac=node(m,'ShaderNodeVectorMath');frac.operation='FRACTION';wire(m,tc.outputs['UV'],frac.inputs[0])
            left,right=(2,642) if style=='B' else (2,721)
            uv=vec(m,frac.outputs[0],((right-left)/2172,720/724,1))
            add=node(m,'ShaderNodeVectorMath');add.operation='ADD';wire(m,uv,add.inputs[0]);add.inputs[1].default_value=(left/2172,2/724,0);wire(m,add.outputs[0],im.inputs[0])
            texcolor=im.outputs['Color']
        c=mix(m,texcolor,(.09,.105,.125,1),.18)
    if mid=='S01' and style in ['B','C']:
        c=mix(m,c,painted_stone_patch(m,co),.45 if style=='B' else .75)
    wire(m,c,bs.inputs['Base Color'])
    bump=node(m,'ShaderNodeBump');bump.inputs['Strength'].default_value=[.33,.17,.09,.025][idx]
    bump.inputs['Distance'].default_value=.028 if mid.startswith(('S','G','O')) else .008
    setin(m,bump.inputs['Height'],height);wire(m,bump.outputs[0],bs.inputs['Normal'])
    if mid=='C01':bs.inputs['Coat Weight'].default_value=.22
    m['surface_id']=mid;m['style']=style;m['provenance']='Original procedural material; B ground references retained generated atlas'
    return m

refmesh=bpy.data.meshes.new('Texture origin');REFERENCE=bpy.data.objects.new('GLOBAL_METRE_TEXTURE_ORIGIN',refmesh);SC.collection.objects.link(REFERENCE)
MATS={s:{mid:material(mid,s) for mid in LABELS} for s in STYLES}
LEGACY=material('G01','B',True)
DEPOSITS={}
for style in STYLES:
    DEPOSITS[style]={}
    for mid in ['G02','G03']:
        mat=MATS[style][mid].copy();mat.name+=' faded deposits'
        attr=node(mat,'ShaderNodeAttribute','Soft dust boundary');attr.attribute_name='deposit_alpha'
        wire(mat,attr.outputs['Fac'],mat.node_tree.nodes.get('Principled BSDF').inputs['Alpha'])
        DEPOSITS[style][mid]=mat
# Reuse the sealed existing sculpt as one immutable mesh prototype.
bpy.ops.import_scene.fbx(filepath=str(ROCKDIR/'SculptedBasalt01.fbx'))
imported=list(bpy.context.selected_objects)
PROTO=next(o for o in imported if o.type=='MESH' and 'LOD0' in o.name)
PROTOMESH=PROTO.data.copy()
for ob in imported:bpy.data.objects.remove(ob,do_unlink=True)
COLS={};MEMBERS={};NAMED={};COUNTER=0
def collection(name):
    c=bpy.data.collections.new(name);SC.collection.children.link(c);COLS[name]=c;MEMBERS[name]=[];return c
def register(o,name,mid,scene):
    global COUNTER
    COUNTER+=1;o.name=f'{scene}_{COUNTER:04}_{name}'
    for c in list(o.users_collection):c.objects.unlink(o)
    COLS[scene].objects.link(o);o.data.materials.clear();o.data.materials.append(MATS['A'][mid])
    o['surface_id']=mid;o['study_id']=o.name;o['role']=name
    MEMBERS[scene].append(o);NAMED[o.name]=o
    return o
def cube(name,loc,dims,mid,scene,bevel=.06,rot=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.rotation_euler[2]=rot
    if bevel:
        m=o.modifiers.new('Machined or chipped edge','BEVEL');m.width=bevel;m.segments=2
        o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
    return register(o,name,mid,scene)
def cyl(name,loc,radius,depth,mid,scene,axis='Z',verts=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=depth,location=loc);o=bpy.context.object
    if axis=='X':o.rotation_euler[1]=math.pi/2
    elif axis=='Y':o.rotation_euler[0]=math.pi/2
    be=o.modifiers.new('Rounded metal edges','BEVEL');be.width=min(.045,radius*.12);be.segments=2
    o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
    return register(o,name,mid,scene)
def beam(name,a,b,width,mid,scene):
    a,b=Vector(a),Vector(b);o=cube(name,(a+b)/2,(width,width,(b-a).length),mid,scene,width*.12)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def rock(name,loc,scale,mid,scene,seed=1,sub=2):
    rng=random.Random(seed)
    if mid=='S01' and sub==2:
        me=PROTOMESH.copy();o=bpy.data.objects.new(name,me);SC.collection.objects.link(o)
        mn=Vector(tuple(min(v.co[k] for v in me.vertices) for k in range(3)))
        mx=Vector(tuple(max(v.co[k] for v in me.vertices) for k in range(3)))
        for v in me.vertices:
            v.co.x=((v.co.x-mn.x)/(mx.x-mn.x)-.5)*scale[0]*2
            v.co.y=((v.co.y-mn.y)/(mx.y-mn.y)-.5)*scale[1]*2
            v.co.z=(v.co.z-mn.z)/(mx.z-mn.z)*scale[2]*1.65
        o.location=loc;o.rotation_euler[2]=rng.uniform(-.5,.5)
        return register(o,name,mid,scene)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc);o=bpy.context.object
    for v in o.data.vertices:
        factor=rng.uniform(.84,1.12);v.co.x*=scale[0]*factor;v.co.y*=scale[1]*factor;v.co.z*=scale[2]*factor
        # A flattened foot keeps fragments grounded; the artist-edited broad mesh is intentional.
        v.co.z=max(v.co.z,-scale[2]*.65)
    o.location.z=loc[2]+scale[2]*.65
    return register(o,name,mid,scene)
def torus(name,loc,major,minor,mid,scene,axis='Z'):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=32,minor_segments=8,location=loc);o=bpy.context.object
    if axis=='X':o.rotation_euler[1]=math.pi/2
    elif axis=='Y':o.rotation_euler[0]=math.pi/2
    return register(o,name,mid,scene)
def cable(points,scene):
    cu=bpy.data.curves.new('Cable','CURVE');cu.dimensions='3D';cu.bevel_depth=.035;cu.bevel_resolution=2
    sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
    for p,co in zip(sp.points,points):p.co=(*co,1)
    o=bpy.data.objects.new('Cable',cu);COLS[scene].objects.link(o);o.data.materials.append(MATS['A']['M04']);o['surface_id']='M04';o['study_id']=f'{scene}_cable';o['role']='Cable';MEMBERS[scene].append(o)
def canister(loc,scene):
    x,y,z=loc
    cube('Ceramic canister',(x,y,z+.5),(.65,.48,1),'C01',scene,.09)
    for zz in [.13,.86]:cube('Canister strap',(x,y,z+zz),(.70,.52,.075),'M03',scene,.025)
    cyl('Canister cap',(x,y,z+1.04),.13,.09,'M01',scene)
def hardware(loc,scene):
    x,y,z=loc
    for dx,dy in [(-.34,-.31),(.34,-.31),(-.34,.31),(.34,.31)]:
        cyl('Bolted fastener',(x+dx,y+dy,z),.065,.075,'M01',scene,verts=6)
def sample_station(scene,loc=(3,-2,0)):
    x,y,z=loc
    cube('Sample table',(x,y,z+.65),(3.0,.85,.12),'M03',scene,.03)
    for dx in [-1.2,1.2]:
        for dy in [-.3,.3]:beam('Sample stand leg',(x+dx,y+dy,.03),(x+dx,y+dy,z+.62),.08,'M04',scene)
    for i,mid in enumerate(['S01','S02','S03','S04','O01']):
        rock('Sorted sample '+mid,(x-1.15+i*.56,y,z+.72),(.22,.22,.22),mid,scene,20+i)
    for i,mid in enumerate(['M01','M02','M03','M04','M05','M06']):
        cube('Finish sample '+mid,(x-1.2+i*.45,y-.1,z+.78),(.3,.1,.05),mid,scene,.01)
def standin(scene,loc=(-2,-3,0)):
    x,y,z=loc
    # Original 1.72 m articulated scale mannequin, not Booter.
    for dx in [-.12,.12]:
        beam('Scale mannequin leg',(x+dx,y,z+.05),(x+dx,y,z+.85),.12,'M02',scene)
    cube('Scale mannequin torso',(x,y,z+1.12),(.43,.23,.52),'C01',scene,.10)
    cyl('Scale mannequin neck',(x,y,z+1.43),.07,.12,'M03',scene)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.16,location=(x,y,z+1.56));register(bpy.context.object,'Scale mannequin head','M02',scene)
    for sign in [-1,1]:beam('Scale mannequin arm',(x+sign*.28,y,z+1.28),(x+sign*.37,y-.05,z+.9),.09,'M03',scene)
def ground(scene):
    # Continuous apron far beyond all cameras; small physical relief without stage boundaries.
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=65,y_subdivisions=65,size=100,location=(0,0,-.06));o=bpy.context.object
    for v in o.data.vertices:
        x,y=v.co.x,v.co.y;v.co.z=.012*math.sin(x*.8)*math.cos(y*.65)
    register(o,'Continuous ground','G01',scene)
    for i,(x,y,sx,sy,mid) in enumerate([(-4,1,3,2,'G02'),(4,2,2,3,'G03'),(-1,-3,2,1,'G02')]):
        # Thin irregular deposits with sloped edges, intentionally open ground surfaces.
        rng=random.Random(50+i);verts=[(x,y,-.004)];faces=[]
        for k in range(20):
            a=k*math.tau/20;r=rng.uniform(.8,1.1);verts.append((x+math.cos(a)*sx*r,y+math.sin(a)*sy*r,-.025))
        for k in range(20):faces.append((0,k+1,(k+1)%20+1))
        me=bpy.data.meshes.new('Fines deposit');me.from_pydata(verts,[],faces);ob=bpy.data.objects.new('Fines deposit',me);SC.collection.objects.link(ob);register(ob,'Ground deposit',mid,scene)
        attr=me.attributes.new('deposit_alpha','FLOAT','POINT')
        for j,d in enumerate(attr.data):d.value=.80 if j==0 else 0
def dress(scene,seed):
    rng=random.Random(seed)
    for i in range(40):
        x,y=rng.uniform(-7,7),rng.uniform(-5,5)
        # Keep the central travel strip calm; clustered fragments at perimeter and structures.
        if abs(x)<1.3 and y<1:continue
        s=rng.uniform(.06,.28);mid=rng.choice(['S01','S01','S02','S03','S04'])
        rock('Ground fragment',(x,y,0),(s,s*rng.uniform(.65,1.2),s*.6),mid,scene,seed+i,1)
    for i in range(7):
        x=-3+i*.27;y=-1.5+math.sin(i)*.2
        cube('Scrap offcut',(x,y,.05),(.22,.12,.05),'M05' if i%2 else 'M06',scene,.008,rot=i*.7)
    for i in range(5):cyl('Loose fastener',(-2.8+i*.12,-1.1,.06),.045,.08,'M01',scene,verts=6)
def yard():
    s='yard';collection(s);ground(s)
    for x in [-1.3,1.3]:cube('Skid rail',(x,1,.18),(.30,4.6,.36),'M06',s,.04)
    cube('Service deck',(0,1,.44),(3.2,4.0,.16),'M02',s,.04)
    cyl('Main cast housing',(0,1,1.35),.92,2.7,'M04',s,'Y',32)
    for y in [-.37,2.37]:
        cyl('Machined flange',(0,y,1.35),1.04,.16,'M01',s,'Y',32)
        cyl('Dark shaft opening',(0,y-.10,1.35),.43,.075,'M04',s,'Y')
        for k in range(10):
            a=k*math.tau/10;cyl('Flange bolt',(math.cos(a)*.86,y-.11,1.35+math.sin(a)*.86),.065,.09,'M03',s,'Y',6)
    cube('Top cover',(0,1,2.31),(1.0,1.6,.2),'M05',s,.10)
    hardware((0,1,2.45),s)
    beam('Exit pipe',(1,1.9,1.3),(2.2,1.9,1.3),.40,'M05',s)
    torus('Valve wheel',(2.3,1.9,1.3),.38,.055,'M03',s,'X')
    for i in range(3):cube('Processed sheet stack',(-3.1,1.4,.12+i*.12),(1.8,1.25,.10),'M01' if i==2 else 'M05',s,.02,rot=.12*i)
    cube('Equipment case',(-3.5,3,.5),(1.3,.9,1),'M05',s,.07)
    cube('Case inset',(-3.5,2.53,.6),(.9,.04,.46),'M02',s,.02)
    for i in range(3):cyl('Pipe stock',(-4.2+i*.4,.2,.22),.18,2.1,'M03',s,'Y')
    rock('Basalt backdrop',(-4,5,0),(2,1.5,2.6),'S01',s,17)
    rock('Porous block',(4,4,0),(1.2,1.1,1.0),'S02',s,18)
    cube('Granite support',(3.5,1,.42),(1.1,.9,.84),'S03',s,.10)
    for i in range(3):cube('Limestone stock',(5,1+i*.7,.23),(1.3,.6,.46),'S04',s,.06)
    cable([(1,-.2,.14),(2,-1,.1),(2.7,-1.6,.08),(3.2,-1.3,.1)],s)
    sample_station(s,(3.6,-2,0));canister((-3,-2.7,0),s);standin(s);dress(s,100)
    # Surrounding geology and useful structures occupy the actual gameplay frame.
    for i,(x,y,sz) in enumerate([(-8,6,2.8),(-5,8,3.5),(-2,9,2.7),(1,9,3.1),(4,8,2.5),(7,6,2.8)]):rock('Workyard canyon rim',(x,y,0),(2.5,1.9,sz),'S01',s,530+i)
    for i in range(5):rock('Workyard talus',(-8+i*3.5,5.4,0),(1.4,1.0,.65),'S01',s,630+i)
    for x in [-6.1,-3.5]:
        for y in [-3.8,-1.8]:beam('Repair stand post',(x,y,0),(x,y,1.0),.10,'M03',s)
    cube('Repair bench',(-4.8,-2.8,1.05),(3.2,2.2,.16),'M02',s,.04)
    cyl('Spare cast wheel',(-4.8,-2.8,1.46),.75,.62,'M04',s,'X',32)
    torus('Machined wheel lip',(-4.45,-2.8,1.46),.62,.075,'M01',s,'X')
    for x in [-6.7,6.7]:
        beam('Service rack upright',(x,4.2,0),(x,4.2,2.7),.14,'M05',s)
    beam('Service rack crossbar',(-6.7,4.2,2.7),(6.7,4.2,2.7),.14,'M03',s)
    for i in range(4):
        cube('Ribbed housing fin',(-.92+i*.6,1,1.35),(.10,2.0,1.6),'M04',s,.02)
    for i in range(3):
        x=5.4+i*.5
        cyl('Storage spool',(x,-4.0,.43),.38,.8,'M05',s)
        torus('Coiled cable',(x,-4.0,.86),.30,.035,'M03',s)
    for i in range(4):cube('Service paving',(-.3,-4+i*1.1,.01),(1.3,.95,.10),'S04',s,.10,rot=.03*i)
    for x,y in [(-6,0),(6,2),(-5.5,-4.8)]:
        cube('Slatted salvage container',(x,y,.48),(1.25,1.0,.96),'M05',s,.05)
        for zz in [.16,.79]:cube('Container band',(x,y-.52,zz),(1.30,.06,.09),'M03',s,.015)
        for dx in [-.43,.43]:cube('Container edge',(x+dx,y-.52,.48),(.075,.06,.9),'M02',s,.015)
def gate():
    s='gate';collection(s);ground(s)
    for side in [-1,1]:
        for row in range(5):
            for col in range(3):
                x=side*(2+col*1.12+(row%2)*.16);y=2.8
                cube('Limestone wall',(x,y,.38+row*.72),(1.08,1.35,.68),'S04',s,.07,rot=(row%2-.5)*.025)
        for col in range(3):cube('Granite footing',(side*(2+col*1.12),2.8,.18),(1.15,1.55,.36),'S03',s,.05)
    # Segmented arch spans the visible gap; break the right outer section deliberately.
    for k in range(9):
        a=math.pi*(.10+k*.095);x=math.cos(a)*2.05;z=2.4+math.sin(a)*2.05
        o=cube('Arch voussoir',(x,2.8,z),(.65,1.4,.70),'S04',s,.07);o.rotation_euler[1]=a-math.pi/2
    beam('Iron cross brace',(-4,2,2.6),(-1.9,2,1.0),.18,'M06',s)
    cube('Rusted gate leaf',(-1.5,2.8,1.4),(.16,1.5,2.8),'M05',s,.04,rot=-.60)
    for i in range(5):beam('Forged gate bar',(-1.4,2.3+i*.25,.4),(-1.4,2.3+i*.25,2.4),.06,'M03',s)
    for i in range(7):
        o=cube('Fallen masonry',(-4+i*.65,1.3-(i%3)*.55,.3),(.8,.65,.6),'S04' if i%2 else 'S03',s,.10,rot=i*.6)
    rock('Basalt obstruction',(5,4,0),(2,1.3,2),'S01',s,21)
    rock('Volcanic rubble',(3.5,2,0),(.7,.8,.6),'S02',s,22)
    cyl('Cast wheel wreck',(-3,-1,.5),.65,.4,'M04',s,'Y')
    cube('Processed panel',(-3.5,-1.5,.14),(1.4,1.0,.15),'M02',s,.03)
    sample_station(s,(3.7,-2,0));canister((-4,-2.5,0),s);standin(s,(-.8,-2.5,0));dress(s,200)
    for side in [-1,1]:
        for i in range(3):rock('Gate canyon shoulder',(side*(6+i*.8),4+i*.6,0),(1.7,1.4,1.2+i*.5),'S01',s,700+i+10*(side+1))
    for i in range(4):cube('Buried approach slab',(0,-4+i*1.05,-.005),(1.4,.9,.08),'S03',s,.08,rot=.06*(i-2))
def quarry():
    s='quarry';collection(s);ground(s)
    for i in range(5):rock('Basalt quarry face',(-5+i*2.4,5,0),(1.6,1.25,2.0+(i%3)*.65),'S01',s,300+i)
    cube('Relay plinth',(0,1,.3),(3,2.7,.6),'S03',s,.08)
    cube('Relay enclosure',(0,1.1,1.25),(1.9,1.4,1.3),'M04',s,.14)
    cube('Relay access panel',(0,.36,1.25),(1.45,.08,.85),'M02',s,.03)
    hardware((0,1.1,1.98),s)
    for x in [-1.1,1.1]:beam('Relay mast',(x,1.1,.6),(x,1.1,4.6),.16,'M03',s)
    beam('Relay crossmember',(-1.2,1.1,4.6),(1.2,1.1,4.6),.18,'M05',s)
    for z in [2.5,3.2,3.9]:beam('Mast diagonal',(-1.1,1.1,z),(1.1,1.1,z+.65),.09,'M06',s)
    for i in range(4):
        cube('Granite stock',(4+(i%2)*1.0,1.6+(i//2)*1.1,.4),(.92,1.0,.8),'S03',s,.08)
    for i in range(4):cube('Limestone cut slab',(-3.7,1,.13+i*.19),(1.6,2,.17),'S04',s,.035,rot=i*.025)
    cube('Dragged sled',(-3,-1.7,.17),(2.2,1.2,.25),'M06',s,.06)
    for i in range(3):rock('Ore load',(-3.7+i*.6,-1.7,.30),(.32,.34,.28),'O01',s,400+i)
    for i in range(2):rock('Porous volcanic block',(4.5+i*.5,-.5,0),(.6,.65,.7),'S02',s,440+i)
    cyl('Spare machined coupling',(2,-1.5,.32),.4,.6,'M01',s,'Y')
    sample_station(s,(3.7,-3,0));canister((-4.7,-3,0),s);standin(s,(-.8,-2.5,0));dress(s,300)
    for i in range(4):rock('Quarry lower talus',(-6+i*4,3.7,0),(1.4,1.1,.65),'S01',s,900+i)
    for x in [-5.7,5.7]:
        for y in [-1,2]:beam('Quarry work rail post',(x,y,0),(x,y,1),.10,'M06',s)
        beam('Quarry side rail',(x,-1,1),(x,2,1),.10,'M03',s)

yard();gate();quarry()
collection('plates')
GROUPS={'metal':['M01','M02','M03','M04','M05','M06'],'stone':['S01','S02','S03','S04','O01'],'ground':['G01','G02','G03','C01']}
PLATE_MEMBERS={}
for group,ids in GROUPS.items():
    obs=[]
    for i,mid in enumerate(ids):
        x=(i%3-1)*2.7;y=(i//3-.5)*2.8
        obs.append(cube('Coupon '+mid,(x,y,.10),(2.25,2.15,.20),mid,'plates',.12))
        obs.append(cyl('Curved coupon '+mid,(x,y+.20,.45),.38,1.45,mid,'plates','X',32))
    PLATE_MEMBERS[group]=obs
collection('control')
CONTROL=[]
for x,mat in [(-1.6,LEGACY),(1.6,MATS['B']['G01'])]:
    ob=cube('Accepted ground control',(x,0,.1),(2.8,3,.2),'G01','control',.04);ob.data.materials[0]=mat;CONTROL.append(ob)
    # Center both controls on identical coordinates, changing only the derivative's tile blend.
    unique=mat.copy();ob.data.materials[0]=unique
    origin=bpy.data.objects.new('Ground coupon texture origin',None);SC.collection.objects.link(origin);origin.location=(x,0,0)
    for n in unique.node_tree.nodes:
        if n.type=='TEX_COORD':n.object=origin

def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('Study camera');CAM=bpy.data.objects.new('Study camera',camdata);SC.collection.objects.link(CAM);SC.camera=CAM
camdata.sensor_fit='VERTICAL';camdata.sensor_height=24;camdata.lens=24/(2*math.tan(math.radians(48)/2));camdata.clip_end=250
def area(name,loc,power,size,color,target=(0,1,0)):
    d=bpy.data.lights.new(name,'AREA');o=bpy.data.objects.new(name,d);SC.collection.objects.link(o);o.location=loc;d.energy=power;d.shape='DISK';d.size=size;d.color=color;aim(o,target);return o
KEY=area('Broad reflection key',(-6,-4,10),1900,7,(1,.86,.70))
FILL=area('Broad sky fill',(5,-2,7),1400,8,(.68,.78,1))
RIM=area('Rim bounce',(-1,7,8),900,6,(1,.72,.42))
sd=bpy.data.lights.new('Low sunset','SUN');SUN=bpy.data.objects.new('Low sunset',sd);SC.collection.objects.link(SUN);sd.angle=math.radians(6);SUN.rotation_euler=(math.radians(68),math.radians(-22),math.radians(-40))

def lights(mode):
    bg=SC.world.node_tree.nodes['Background']
    if mode=='sunset':
        sd.energy=2.4;sd.color=(1,.77,.53);KEY.data.energy=1700;FILL.data.energy=1300;RIM.data.energy=1000
        KEY.data.color=(1,.86,.7);FILL.data.color=(.72,.82,1);bg.inputs['Color'].default_value=(.34,.24,.17,1);bg.inputs['Strength'].default_value=.40
    elif mode=='twilight':
        sd.energy=.65;sd.color=(1,.60,.33);KEY.data.energy=700;FILL.data.energy=1100;RIM.data.energy=500
        KEY.data.color=(1,.76,.56);FILL.data.color=(.65,.76,1);bg.inputs['Color'].default_value=(.26,.22,.22,1);bg.inputs['Strength'].default_value=.32
    else:
        sd.energy=0;KEY.data.energy=2200;FILL.data.energy=1500;RIM.data.energy=1000
        KEY.data.color=FILL.data.color=RIM.data.color=(1,1,1);bg.inputs['Color'].default_value=(.32,.32,.32,1);bg.inputs['Strength'].default_value=.55
def visible(name,group=None):
    for sn,obs in MEMBERS.items():
        for ob in obs:ob.hide_render=sn!=name
    if name=='plates':
        for ob in MEMBERS['plates']:ob.hide_render=ob not in PLATE_MEMBERS[group]
def style_assign(style):
    for sn,obs in MEMBERS.items():
        if sn=='control':continue
        for ob in obs:ob.data.materials[0]=(DEPOSITS[style][ob['surface_id']] if ob['role']=='Ground deposit' else MATS[style][ob['surface_id']])
def camera(view):
    camdata.type='PERSP';camdata.sensor_fit='VERTICAL';camdata.lens=24/(2*math.tan(math.radians(48)/2))
    if view=='gameplay':dist,pitch,yaw,target=22,50,40,Vector((0,0,1.15))
    elif view=='close':dist,pitch,yaw,target=10.5,34,15,Vector((1.0,.25,1.0))
    elif view=='orbit':dist,pitch,yaw,target=24,26,115,Vector((0,1,1.4))
    elif view=='plate':dist,pitch,yaw,target=11.8,65,0,Vector((0,0,.1))
    elif view=='control':dist,pitch,yaw,target=8.5,68,0,Vector((0,0,0))
    a,b=math.radians(pitch),math.radians(yaw)
    CAM.location=target+Vector((math.sin(b)*math.cos(a)*dist,-math.cos(b)*math.cos(a)*dist,math.sin(a)*dist));aim(CAM,target)
    bpy.context.view_layer.update()
    return {'view':view,'distance':dist,'pitch':pitch,'yaw':yaw,'target':list(target),'matrix':[list(r) for r in CAM.matrix_world],'vertical_fov':48,'aspect':16/9}
def fingerprint(sn):
    payload=[]
    for ob in MEMBERS[sn]:
        payload.append((ob['study_id'],list(ob.location),list(ob.scale),list(ob.rotation_euler),ob['surface_id'],[(list(v.co)) for v in ob.data.vertices] if ob.type=='MESH' else []))
    return hashlib.sha256(json.dumps(payload,sort_keys=True).encode()).hexdigest()
GEOMETRY={sn:fingerprint(sn) for sn in ['yard','gate','quarry']}
FORM_BASE={ob.name:([v.co.copy() for v in ob.data.vertices],ob.scale.copy()) for ob in MEMBERS['yard'] if ob.type=='MESH'}
def forms(style):
    for ob in MEMBERS['yard']:
        if ob.type!='MESH':continue
        verts,scale=FORM_BASE[ob.name];ob.scale=scale
        for v,base in zip(ob.data.vertices,verts):v.co=base
        for mod in list(ob.modifiers):
            if mod.name=='Whole-style facets':ob.modifiers.remove(mod)
        if ob['role'] in ['Continuous ground','Ground deposit'] or 'mannequin' in ob['role']:continue
        if ob['surface_id'].startswith(('S','O')):
            if style in ['C','D']:
                q=.10 if style=='C' else .22
                for v in ob.data.vertices:
                    v.co.x=round(v.co.x/q)*q;v.co.y=round(v.co.y/q)*q
                if style=='D':
                    de=ob.modifiers.new('Whole-style facets','DECIMATE');de.ratio=.42
        elif style in ['C','D'] and ob['role'] in ['Main cast housing','Equipment case','Top cover']:
            ob.scale.x*=1.08 if style=='C' else 1.16
            ob.scale.y*=.98 if style=='C' else .94
        # Realism and the preferred hybrid retain the common geometry intentionally.

def graph_digest(m):
    nodes=[]
    for n in m.node_tree.nodes:
        inputs=[]
        for socket in n.inputs:
            if not hasattr(socket,'default_value'):continue
            v=socket.default_value
            if not isinstance(v,(str,int,float,bool)):
                try:v=list(v)
                except TypeError:v=str(v)
            inputs.append((socket.name,v))
        nodes.append((n.name,n.bl_idname,getattr(n,'operation',''),getattr(n,'attribute_name',''),inputs))
    links=[(l.from_node.name,l.from_socket.name,l.to_node.name,l.to_socket.name) for l in m.node_tree.links]
    return hashlib.sha256(json.dumps([nodes,links],sort_keys=True).encode()).hexdigest()
receipt={'version':bpy.app.version_string,'device':[d.name for d in prefs.devices if d.use],'sources':SOURCES,'styles':STYLES,'materials':LABELS,'material_graphs':{m.name:graph_digest(m) for m in bpy.data.materials if m.use_nodes},'geometry':GEOMETRY,'objects':{sn:[{'id':o['study_id'],'role':o['role'],'material':o['surface_id']} for o in obs] for sn,obs in MEMBERS.items()},'renders':[],'color_management':{'transform':'AgX','look':SC.view_settings.look,'exposure':0},'source_method':'Original authored geometry and procedural material graphs; accepted ground references prior generated image atlas. No manually painted claim. No production changes.'}
def save_receipt():
    (OUT/('preview_receipt.json' if PREVIEW else 'render_receipt.json')).write_text(json.dumps(receipt,indent=2),encoding='utf-8')
def render(sn,style,light,view,kind='controlled',group=None):
    import shutil
    if shutil.disk_usage(ROOT).free<700_000_000:raise RuntimeError('Less than 700 MB free; preserve all existing outputs and stop new renders')
    visible(sn,group);style_assign(style);lights(light);cp=camera(view)
    if kind=='controlled':assert fingerprint(sn)==GEOMETRY[sn]
    tag=f'{sn}_{style}_{light}_{view}_{kind}'+('_'+group if group else '')
    path=OUT/('previews' if PREVIEW else 'renders')/(tag+'.png')
    SC.render.filepath=str(path);t=time.time();bpy.ops.render.render(write_still=True)
    row={'file':path.name,'scene':sn,'style':style,'light':light,'view':view,'kind':kind,'group':group,'camera':cp,'seconds':time.time()-t,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'geometry':fingerprint(sn) if sn in GEOMETRY else None,'resolution':[SC.render.resolution_x,SC.render.resolution_y]}
    receipt['renders'].append(row);save_receipt();print('SABLE_RENDER',tag,round(row['seconds'],2),flush=True)

# Save the complete authored source with all materials and packed image dependencies.
visible('yard');style_assign('B');lights('sunset');camera('gameplay')
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(LOCAL/'MaterialDioramas.blend'))
receipt['native_source']=str((LOCAL/'MaterialDioramas.blend').relative_to(ROOT));receipt['native_sha256']=hashlib.sha256((LOCAL/'MaterialDioramas.blend').read_bytes()).hexdigest();save_receipt()
for sn in ['yard','gate','quarry']:
    if ONLY and sn!=ONLY:continue
    for light in (['sunset'] if PREVIEW else ['sunset','twilight']):
        for view in (['gameplay'] if PREVIEW else ['gameplay','close','orbit']):
            for style in STYLES:render(sn,style,light,view)
if not PREVIEW and not ONLY:
    for group in GROUPS:
        for style in STYLES:render('plates',style,'neutral','plate','material',group)
    for style in STYLES:render('yard',style,'neutral','gameplay','neutral_diagnostic')
    visible('control');lights('neutral');camera('control')
    # render() preserves the two explicitly assigned legacy/derivative materials.
    render('control','B','neutral','control','accepted_ground')
    for style in STYLES:
        forms(style)
        for view in ['gameplay','close']:render('yard',style,'sunset',view,'whole_style')
        if style!='A':
            visible('yard');style_assign(style);lights('sunset');camera('gameplay')
            bpy.ops.wm.save_as_mainfile(filepath=str(LOCAL/f'WholeStyle_{style}.blend'))
    forms('A')
receipt['whole_style_scope']='A/B preserve base form. C adds broader rock planes and slightly expressive housings; D further reduces rock facets and broadens selected machinery. Composition, physical height/contacts and lighting remain controlled. Original interpretations, not exact game reproductions.'
receipt['complete']=not PREVIEW and not ONLY;save_receipt()
for rel,h in SOURCES.items():
    assert hashlib.sha256((ROOT/rel).read_bytes()).hexdigest()==h,'Source changed during study: '+rel
print('SABLE_COMPLETE',len(receipt['renders']),flush=True)
