"""Inspected realism passes for the existing open Blender study.

Import this file and call pass_one() or pass_two() from Blender's console.
The original rock objects are retained in a hidden archive collection.
"""
from pathlib import Path
from math import cos, sin, pi, exp
from bisect import bisect_right
import random
import bpy
import bmesh
from mathutils import Vector, noise

ROOT = Path(__file__).resolve().parents[4]
TEXTURES = ROOT / 'Assets/_Project/Art/Environment/Rocks/Workbench/Layered'
ARCHIVE = 'Archive - rocks before geological realism'
DETAIL = 'Realism - exposed strata and debris'
GRID = [-180+2.5*i for i in range(36)]+[-90+.75*i for i in range(241)]+[92.5+2.5*i for i in range(36)]


def collection(name):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c


def terrain():
    if Path(bpy.data.filepath).name != 'BrokenWorldBadlandsStudy.blend':
        raise RuntimeError('Open the existing BrokenWorldBadlandsStudy.blend')
    t = bpy.data.objects.get('Badlands terrain - study only')
    if t is None or len(t.data.vertices) != 97969:
        raise RuntimeError('Expected the authored 360 m terrain')
    return t


def ground(x, y):
    t = bpy.data.objects['Badlands terrain - study only']
    ix=max(0,min(311,bisect_right(GRID,x)-1))
    iy=max(0,min(311,bisect_right(GRID,y)-1))
    a=max(0,min(1,(x-GRID[ix])/(GRID[ix+1]-GRID[ix])))
    b=max(0,min(1,(y-GRID[iy])/(GRID[iy+1]-GRID[iy])))
    def h(dx, dy):
        return t.data.vertices[(iy + dy) * 313 + ix + dx].co.z
    return (1-a)*(1-b)*h(0,0)+a*(1-b)*h(1,0)+(1-a)*b*h(0,1)+a*b*h(1,1)


def archive(obj):
    c = collection(ARCHIVE)
    backup = obj.copy()
    backup.name = 'Before realism - ' + obj.name
    c.objects.link(backup)
    c.hide_render = c.hide_viewport = True
    obj['realism_original_object'] = backup.name


def tex(nodes, links, name, vector, data=False):
    n = nodes.new('ShaderNodeTexImage')
    n.image = bpy.data.images.load(str(TEXTURES / name), check_existing=True)
    if data:
        n.image.colorspace_settings.name = 'Non-Color'
    n.image.pack()
    n.projection = 'BOX'
    n.projection_blend = .22
    n.extension = 'REPEAT'
    links.new(vector, n.inputs['Vector'])
    return n


def rock_material():
    name = 'Geology - fractured shale with dust'
    if bpy.data.materials.get(name):
        return bpy.data.materials[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    n, l = m.node_tree.nodes, m.node_tree.links
    bs = n.get('Principled BSDF')
    bs.inputs['Specular IOR Level'].default_value = .28
    geo = n.new('ShaderNodeNewGeometry')
    mapping = n.new('ShaderNodeVectorMath'); mapping.operation = 'SCALE'
    mapping.inputs['Scale'].default_value = .58
    l.new(geo.outputs['Position'], mapping.inputs[0])
    vector = mapping.outputs['Vector']
    side = tex(n,l,'RockWorkbenchSide_Albedo.png',vector)
    top = tex(n,l,'RockWorkbenchTop_Albedo.png',vector)
    sideh = tex(n,l,'RockWorkbenchSide_Surface.png',vector,True)
    toph = tex(n,l,'RockWorkbenchTop_Surface.png',vector,True)
    normal = n.new('ShaderNodeSeparateXYZ'); l.new(geo.outputs['Normal'],normal.inputs[0])
    slope = n.new('ShaderNodeMapRange')
    slope.inputs['From Min'].default_value = .35
    slope.inputs['From Max'].default_value = .82
    l.new(normal.outputs['Z'],slope.inputs['Value'])
    mix = n.new('ShaderNodeMixRGB'); l.new(slope.outputs['Result'],mix.inputs[0])
    l.new(side.outputs['Color'],mix.inputs[1]); l.new(top.outputs['Color'],mix.inputs[2])
    mineral = n.new('ShaderNodeMixRGB'); mineral.blend_type = 'MULTIPLY'
    mineral.inputs[0].default_value = .55
    mineral.inputs[2].default_value = (.59,.51,.43,1)
    l.new(mix.outputs[0],mineral.inputs[1])
    variation = n.new('ShaderNodeTexNoise'); variation.inputs['Scale'].default_value = 1.7
    variation.inputs['Detail'].default_value = 3
    l.new(geo.outputs['Position'],variation.inputs['Vector'])
    dust = n.new('ShaderNodeMath'); dust.operation = 'MULTIPLY'
    l.new(slope.outputs['Result'],dust.inputs[0]); l.new(variation.outputs['Fac'],dust.inputs[1])
    amount = n.new('ShaderNodeMath'); amount.operation='MULTIPLY'; amount.inputs[1].default_value=.32
    l.new(dust.outputs[0],amount.inputs[0])
    dusty = n.new('ShaderNodeMixRGB'); l.new(amount.outputs[0],dusty.inputs[0])
    l.new(mineral.outputs[0],dusty.inputs[1]); dusty.inputs[2].default_value=(.23,.115,.054,1)
    l.new(dusty.outputs[0],bs.inputs['Base Color'])
    surface = n.new('ShaderNodeMixRGB'); l.new(slope.outputs['Result'],surface.inputs[0])
    l.new(sideh.outputs['Color'],surface.inputs[1]); l.new(toph.outputs['Color'],surface.inputs[2])
    channels=n.new('ShaderNodeSeparateColor'); l.new(surface.outputs[0],channels.inputs[0])
    l.new(channels.outputs['Green'],bs.inputs['Roughness'])
    bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.65; bump.inputs['Distance'].default_value=.09
    l.new(channels.outputs['Blue'],bump.inputs['Height'])
    micro=n.new('ShaderNodeTexNoise'); micro.inputs['Scale'].default_value=95; micro.inputs['Detail'].default_value=2
    l.new(geo.outputs['Position'],micro.inputs['Vector'])
    grain=n.new('ShaderNodeBump'); grain.inputs['Strength'].default_value=.26; grain.inputs['Distance'].default_value=.014
    l.new(bump.outputs['Normal'],grain.inputs['Normal']); l.new(micro.outputs['Fac'],grain.inputs['Height'])
    l.new(grain.outputs['Normal'],bs.inputs['Normal'])
    return m


def fractured_mesh(name, width, depth, height, seed, cuts=9, pieces=1):
    """Dense weathered fracture surfaces with broad planes and irregular joints."""
    rng=random.Random(seed)
    final=bmesh.new()
    for piece in range(pieces):
        bm=bmesh.new()
        pw=width if pieces==1 else width * (.58 if piece==0 else .43)
        cx=0 if pieces==1 else width * (-.21 if piece==0 else .30)
        ph=height * (1 if piece==0 else rng.uniform(.72,.93))
        # Unequal fracture planes and tilted wedge tops avoid extruded columns.
        sides=rng.randrange(6,10)
        phase=rng.uniform(0,2*pi)
        outline=[]
        for i in range(sides):
            angle=phase+2*pi*(i+rng.uniform(-.16,.16))/sides
            radius=rng.uniform(.38,.59)
            outline.append((radius*cos(angle),radius*sin(angle)))
        vertices=[]
        tx,ty=rng.uniform(-.23,.23),rng.uniform(-.23,.23)
        taper_x,taper_y=rng.uniform(.65,1.03),rng.uniform(.70,1.04)
        slope_x,slope_y=rng.uniform(-.7,.7),rng.uniform(-.55,.55)
        for ring in range(2):
            for x,y in outline:
                xx=(x*(taper_x if ring else 1)+rng.uniform(-.065,.065)+(tx if ring else 0))*pw+cx
                yy=(y*(taper_y if ring else 1)+rng.uniform(-.07,.07)+(ty if ring else 0))*depth
                zz=-.05*height if ring==0 else ph*(.76+slope_x*x+slope_y*y+rng.uniform(-.13,.13))
                vertices.append(bm.verts.new((xx,yy,zz)))
        bmesh.ops.convex_hull(bm,input=vertices)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bmesh.ops.bevel(bm,geom=list(bm.edges),offset=min(pw,depth,ph)*.023,segments=2,affect='EDGES')
        bmesh.ops.triangulate(bm,faces=list(bm.faces))
        bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=cuts,use_grid_fill=True)
        bm.normal_update()
        joints=[(rng.uniform(.18,.8)*ph,rng.uniform(.018,.035)*ph) for _ in range(3 if ph>1 else 1)]
        size=min(pw,depth,ph)
        for v in bm.verts:
            p=v.co.copy(); normal=v.normal.copy()
            broad=noise.noise(Vector((p.x*2.3+seed*.17,p.y*2.3,p.z*2.3)))
            fine=noise.noise(Vector((p.x*13.7,p.y*13.7+seed*.13,p.z*13.7)))
            recess=0
            for elevation,half_width in joints:
                warped=p.z+.055*size*noise.noise(Vector((p.x*3,p.y*3,seed*.3)))
                recess+=.047*size*exp(-((warped-elevation)/max(.015,half_width))**2)*(1-abs(normal.z))
            v.co+=normal*(.027*size*broad+.009*size*fine-recess)
        bm.normal_update()
        offset=len(final.verts)
        copied={v:final.verts.new(v.co) for v in bm.verts}
        for f in bm.faces:
            final.faces.new([copied[v] for v in f.verts])
        bm.free()
    mesh=bpy.data.meshes.new(name)
    final.to_mesh(mesh); final.free(); mesh.update()
    for f in mesh.polygons: f.use_smooth=True
    mesh.materials.append(rock_material())
    return mesh


def camera(name, location, target, lens):
    obj=bpy.data.objects.get(name)
    if obj is None:
        obj=bpy.data.objects.new(name,bpy.data.cameras.new(name))
        bpy.context.scene.collection.objects.link(obj)
    obj.location=location
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    obj.data.lens=lens; obj.data.clip_end=10000
    return obj


def review_cameras():
    camera('Geology close review',(40,-34,6.5),(28,-15,1.4),43)
    camera('Geology wide review',(8,-54,9),(0,20,.8),35)


def pass_one():
    t=terrain()
    if t.get('geology_pass_one'): return
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(('Formation_','Stone_','Talus_'))]
    for i,obj in enumerate(objects):
        archive(obj)
        coords=[v.co for v in obj.data.vertices]
        lo=Vector(tuple(min(v[a] for v in coords) for a in range(3)))
        hi=Vector(tuple(max(v[a] for v in coords) for a in range(3)))
        dim=hi-lo
        main=obj.name.startswith('Formation_')
        mesh=fractured_mesh(obj.name+' fractured source',dim.x,dim.y,dim.z,230930+i*79,cuts=12 if main else 4,pieces=2 if '_Core' in obj.name else 1)
        for v in mesh.vertices: v.co.z+=lo.z
        obj.modifiers.clear(); obj.data=mesh
        obj['geological_fracture_pass']='2026-09-30'
    review_cameras()
    t['geology_pass_one']=True
    print('PASS ONE: replaced',len(objects),'rock meshes; originals archived')


def improve_ground():
    t=terrain(); archive(t); t.data=t.data.copy()
    centers=[(o.location.x,o.location.y,max(1,o.dimensions.x*.55),max(1,o.dimensions.y*.55))
             for o in bpy.context.scene.objects if o.name.startswith('Formation_')]
    deposit=t.data.attributes.new('RealismSandDeposit','FLOAT','POINT')
    for v in t.data.vertices:
        x,y=v.co.x,v.co.y
        amount=0
        for cx,cy,rx,ry in centers:
            if abs(x-cx)>rx*3 or abs(y-cy)>ry*3: continue
            r=(((x-cx)/rx)**2+((y-cy)/ry)**2)**.5
            lee=max(.1,min(1,(x-cx)/rx*.35+.55))
            amount=max(amount,.22*exp(-((r-.9)/.72)**2)*lee)
        v.co.z+=amount
        deposit.data[v.index].value=amount/.22
    t.data.update()
    old=t.active_material; old.use_fake_user=True
    mat=old.copy(); mat.name='StudyGround - shale dirt and deposited sand'
    t.active_material=mat
    n,l=mat.node_tree.nodes,mat.node_tree.links
    bs=n.get('Dry badlands ground')
    original=bs.inputs['Base Color'].links[0].from_socket
    balance=n.new('ShaderNodeHueSaturation'); balance.name='Restrained rust palette'
    balance.inputs['Saturation'].default_value=.72
    balance.inputs['Value'].default_value=1.05
    l.new(original,balance.inputs['Color']); l.new(balance.outputs[0],bs.inputs['Base Color'])
    # Image grain now contributes relief at the same places as the visible shale.
    height=n.new('ShaderNodeRGBToBW'); l.new(original,height.inputs[0])
    image_bump=n.new('ShaderNodeBump'); image_bump.name='Shale relief matching color'
    image_bump.inputs['Strength'].default_value=.35; image_bump.inputs['Distance'].default_value=.07
    l.new(height.outputs[0],image_bump.inputs['Height'])
    fine=n.get('Fine dirt and chip relief')
    l.new(fine.outputs['Normal'],image_bump.inputs['Normal'])
    l.new(image_bump.outputs['Normal'],bs.inputs['Normal'])
    rough=n.new('ShaderNodeMapRange'); rough.inputs['To Min'].default_value=.73; rough.inputs['To Max'].default_value=.96
    l.new(height.outputs[0],rough.inputs['Value']); l.new(rough.outputs[0],bs.inputs['Roughness'])
    return t


def add_bedrock():
    c=collection(DETAIL); rng=random.Random(674923)
    patches=[(-18,-24),(10,-32),(21,-23),(35,-24),(-12,-4),(10,6),(-29,10),(32,12),
             (-8,27),(45,-6),(57,-15),(70,3),(-40,39),(33,46),(-45,-24),(9,56),(48,36),(1,-42)]
    prototypes=[fractured_mesh('Exposed bedrock source '+str(i),1,1,.13,i*731+8291,cuts=8) for i in range(12)]
    count=0
    for cx,cy in patches:
        for j in range(rng.randrange(7,14)):
            x,y=cx+rng.gauss(0,4.5),cy+rng.gauss(0,3.2)
            # Leave a recognizable open wash amid irregular shelves.
            if abs(x)<3 and -18<y<36: continue
            obj=bpy.data.objects.new(f'Exposed shale plate {count:03d}',rng.choice(prototypes)); c.objects.link(obj)
            sx,sy=rng.uniform(2.0,6.7),rng.uniform(1.4,4.5)
            obj.scale=(sx,sy,rng.uniform(2.0,5.5))
            obj.rotation_euler=(rng.uniform(-.08,.08),rng.uniform(-.10,.10),rng.gauss(.25,.35))
            obj.location=(x,y,0)
            bpy.context.view_layer.update()
            points=[obj.matrix_world @ v.co for v in obj.data.vertices if v.co.z<.01]
            obj.location.z=min(ground(p.x,p.y)-p.z for p in points)-.025
            count+=1
    shelf=bpy.data.objects.get('Reference low eroded shelf')
    archive(shelf)
    shelf.modifiers.clear()
    shelf.data=fractured_mesh('Fractured low shelf',13,8.5,2.7,61572,cuts=15,pieces=2)
    shelf.data.update()
    print('Added',count,'exposed bedrock plates')
    return patches


def chip_mesh(seed):
    """Small fragments need chipped outlines, not hero-rock subdivisions."""
    rng=random.Random(seed); bm=bmesh.new(); rings=[]
    sides=rng.choice((5,6)); radii=[rng.uniform(.35,.57) for _ in range(sides)]
    for level in range(2):
        ring=[]
        for i in range(sides):
            angle=2*pi*i/sides; r=radii[i]*(rng.uniform(.70,.92) if level else 1)
            ring.append(bm.verts.new((r*cos(angle),r*sin(angle)*.75,-.035 if not level else rng.uniform(.18,.38))))
        rings.append(ring)
    bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[1])
    for i in range(sides):
        j=(i+1)%sides; bm.faces.new((rings[0][i],rings[0][j],rings[1][j],rings[1][i]))
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bmesh.ops.bevel(bm,geom=list(bm.edges),offset=.025,segments=1,affect='EDGES')
    mesh=bpy.data.meshes.new('Small shale chip '+str(seed)); bm.to_mesh(mesh); bm.free()
    mesh.update()
    return mesh


def add_grit(patches):
    rng=random.Random(931720)
    prototypes=[chip_mesh(827+i*59) for i in range(12)]
    proto=[([v.co.copy() for v in m.vertices],[tuple(p.vertices) for p in m.polygons]) for m in prototypes]
    verts,faces=[],[]
    for i in range(11500):
        if i<8200:
            cx,cy=rng.choice(patches)
            x,y=cx+rng.gauss(0,6),cy+rng.gauss(0,4.5)
        else:
            x,y=rng.uniform(-65,83),rng.uniform(-49,90)
        if abs(x)<2.7 and -12<y<42 and rng.random()<.85: continue
        size=rng.uniform(.035,.14) if i%5 else rng.uniform(.15,.55)
        if i%53==0: size=rng.uniform(.55,1.05)
        angle=rng.uniform(0,2*pi); ca,sa=cos(angle),sin(angle)
        z=ground(x,y)-size*.045
        points,polys=rng.choice(proto); start=len(verts)
        for p in points:
            verts.append((x+size*(p.x*ca-p.y*sa),y+size*(p.x*sa+p.y*ca),z+size*p.z))
        faces.extend(tuple(start+j for j in poly) for poly in polys)
    mesh=bpy.data.meshes.new('Fine to medium fractured ground debris')
    mesh.from_pydata(verts,[],faces); mesh.materials.append(rock_material()); mesh.update()
    for p in mesh.polygons: p.use_smooth=True
    obj=bpy.data.objects.new('Fractured gravel across exposed strata',mesh); collection(DETAIL).objects.link(obj)
    obj['scatter_seed']=931720
    print('Added clustered fine debris:',len(verts),'vertices')


def distant_ground(t):
    side=313
    perimeter=[i for i in range(side)]
    perimeter += [j*side+side-1 for j in range(1,side)]
    perimeter += [(side-1)*side+i for i in range(side-2,-1,-1)]
    perimeter += [j*side for j in range(side-2,0,-1)]
    base=[t.data.vertices[i].co.copy() for i in perimeter]
    verts=[]; faces=[]; size=len(base)
    for ring,scale in enumerate((1,1.1,1.4,2,3,4.5,7,12)):
        for p in base:
            x,y=p.x*scale,p.y*scale
            height=p.z if ring==0 else (min(7,(scale-1)*1.7)*noise.noise(Vector((x/150,y/150,171.8))))
            verts.append((x,y,height))
        if ring:
            for i in range(size):
                nxt=(i+1)%size
                faces.append(((ring-1)*size+i,ring*size+i,ring*size+nxt,(ring-1)*size+nxt))
    mesh=bpy.data.meshes.new('Low distant landscape continuation'); mesh.from_pydata(verts,[],faces)
    mesh.materials.append(t.active_material)
    for f in mesh.polygons: f.use_smooth=True
    mesh.update()
    obj=bpy.data.objects.new('Low distant landscape continuation',mesh); collection(DETAIL).objects.link(obj)
    obj['purpose']='Visual horizon continuation; 360 m authoring terrain remains separate'


def lighting():
    s=bpy.context.scene
    for o in s.objects:
        if o.type=='LIGHT':
            o['before_realism_energy']=o.data.energy
            o['before_realism_color']=list(o.data.color)
            o['before_realism_rotation']=list(o.rotation_euler)
    sun=bpy.data.objects['Low twilight sun']
    sun.data.energy=3.0; sun.data.color=(1,.84,.67); sun.data.angle=.035
    sun.rotation_euler=Vector((.6,.5,-.65)).to_track_quat('-Z','Y').to_euler()
    for name in ('Soft sky bounce','Reference warm rock bounce'):
        bpy.data.objects[name].data.energy*=.22
    old=s.world; old.use_fake_user=True
    s.world=old.copy(); s.world.name='Badlands sky - camera panorama and balanced fill'
    n,l=s.world.node_tree.nodes,s.world.node_tree.links
    out=next(x for x in n if x.type=='OUTPUT_WORLD')
    original=out.inputs['Surface'].links[0].from_socket
    ambient=n.new('ShaderNodeBackground'); ambient.name='Balanced diffuse environment'
    ambient.inputs['Color'].default_value=(.42,.38,.32,1); ambient.inputs['Strength'].default_value=.5
    rays=n.new('ShaderNodeLightPath')
    mix=n.new('ShaderNodeMixShader'); l.new(rays.outputs['Is Camera Ray'],mix.inputs[0])
    l.new(ambient.outputs[0],mix.inputs[1]); l.new(original,mix.inputs[2]); l.new(mix.outputs[0],out.inputs['Surface'])
    # A shallow atmosphere fades distant ground naturally into the reference sky.
    bm=bmesh.new(); bmesh.ops.create_cube(bm,size=2)
    for v in bm.verts: v.co=(v.co.x*2600,v.co.y*2600,v.co.z*80+72)
    mesh=bpy.data.meshes.new('Atmosphere bounds'); bm.to_mesh(mesh); bm.free()
    volume=bpy.data.materials.new('Thin suspended dust'); volume.use_nodes=True
    vn,vl=volume.node_tree.nodes,volume.node_tree.links; vn.clear()
    scatter=vn.new('ShaderNodeVolumeScatter'); scatter.inputs['Color'].default_value=(.70,.59,.46,1)
    scatter.inputs['Density'].default_value=.0011; scatter.inputs['Anisotropy'].default_value=.25
    output=vn.new('ShaderNodeOutputMaterial'); vl.new(scatter.outputs[0],output.inputs['Volume'])
    mesh.materials.append(volume)
    obj=bpy.data.objects.new('Thin atmospheric dust',mesh); collection(DETAIL).objects.link(obj)
    obj.display_type='WIRE'
    s.view_settings.view_transform='AgX'; s.view_settings.exposure=.35
    s.render.engine='CYCLES'; s.cycles.samples=64; s.cycles.use_denoising=True
    s.cycles.preview_samples=16; s.cycles.use_preview_denoising=True
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='METAL'; prefs.get_devices()
    for d in prefs.devices: d.use=d.type=='METAL'
    s.cycles.device='GPU'


def pass_two():
    t=terrain()
    if not t.get('geology_pass_one'): raise RuntimeError('Inspect and apply pass one first')
    if t.get('geology_pass_two'): return
    t=improve_ground()
    patches=add_bedrock(); add_grit(patches)
    distant_ground(t); lighting()
    bpy.context.scene.camera=bpy.data.objects['Geology wide review']
    t['geology_pass_two']=True
    print('PASS TWO: exposed strata, ground contact, debris, lighting and atmosphere authored')


def pass_three():
    """Corrections identified in the first path-traced material review."""
    t=terrain()
    if not t.get('geology_pass_two'): raise RuntimeError('Apply pass two first')
    if t.get('geology_pass_three'): return
    m=rock_material()
    for node in m.node_tree.nodes:
        if node.type=='TEX_IMAGE' and node.image:
            if node.image.name.startswith('RockWorkbenchTop_Albedo'):
                node.image=bpy.data.images.load(str(TEXTURES/'RockWorkbenchSide_Albedo.png'),check_existing=True)
            elif node.image.name.startswith('RockWorkbenchTop_Surface'):
                node.image=bpy.data.images.load(str(TEXTURES/'RockWorkbenchSide_Surface.png'),check_existing=True)
                node.image.colorspace_settings.name='Non-Color'
            node.image.pack()
    # Preserve the planar fractures while roughening exposed tops with shallow
    # irregular fissures. Shared bedrock sources stay shared for scene efficiency.
    seen=set()
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH' or not (obj.get('geological_fracture_pass') or obj.name.startswith('Exposed shale plate') or obj.name=='Reference low eroded shelf'):
            continue
        mesh=obj.data
        if mesh.name in seen: continue
        seen.add(mesh.name)
        extent=max(v.co.x for v in mesh.vertices)-min(v.co.x for v in mesh.vertices)
        height=max(v.co.z for v in mesh.vertices)-min(v.co.z for v in mesh.vertices)
        amplitude=min(extent*.037,height*.18)
        normals=[v.normal.copy() for v in mesh.vertices]
        for v,normal in zip(mesh.vertices,normals):
            if normal.z<.25: continue
            p=v.co.copy(); scale=5/max(.3,extent)
            distances=noise.voronoi(Vector((p.x*scale,p.y*scale,13.1)))[0]
            crack=exp(-((distances[1]-distances[0])/.055)**2)
            rough=noise.noise(Vector((p.x*scale*4,p.y*scale*4,p.z*scale*4)))
            v.co+=normal*amplitude*(.42*rough-crack)*normal.z
        mesh.update()
    n,l=t.active_material.node_tree.nodes,t.active_material.node_tree.links
    balance=n.get('Restrained rust palette'); balance.inputs['Saturation'].default_value=.88
    balance.inputs['Value'].default_value=.88
    tint=n.new('ShaderNodeMixRGB'); tint.name='Burnt rust mineral balance'; tint.blend_type='MULTIPLY'
    tint.inputs[0].default_value=1; tint.inputs[2].default_value=(.9,.55,.31,1)
    l.new(balance.outputs[0],tint.inputs[1]); l.new(tint.outputs[0],n.get('Dry badlands ground').inputs['Base Color'])
    sun=bpy.data.objects['Low twilight sun']; sun.data.energy=2.5; sun.data.color=(1,.81,.63)
    sun.rotation_euler=Vector((.7,.5,-.42)).to_track_quat('-Z','Y').to_euler()
    bpy.context.scene.world.node_tree.nodes['Balanced diffuse environment'].inputs['Strength'].default_value=.22
    bpy.context.scene.view_settings.exposure=.0
    t['geology_pass_three']=True
    print('PASS THREE: corrected top surface, real fissures, rust palette and directional light')


def pass_four():
    """Break up the empty middle distance with low irregular erosion banks."""
    t=terrain()
    if not t.get('geology_pass_three'): raise RuntimeError('Inspect pass three first')
    if t.get('geology_pass_four'): return
    banks=[(-78,117,4.0,62,16),(65,112,3.7,43,18),(-124,-15,3.4,18,66),
           (131,56,4.0,19,57),(12,151,3.0,70,16),(-12,90,1.7,37,13)]
    for v in t.data.vertices:
        x,y=v.co.x,v.co.y; radius=max(abs(x),abs(y))
        fade=max(0,min(1,(180-radius)/20))
        amount=0
        wx=x+8*noise.noise(Vector((x/37,y/37,29)))
        wy=y+8*noise.noise(Vector((x/43,y/43,97)))
        for cx,cy,h,rx,ry in banks:
            distance=((wx-cx)/rx)**2+((wy-cy)/ry)**2
            if distance<10:
                amount+=h*exp(-distance*1.8)
        amount*=fade*(1+.14*noise.noise(Vector((x/9,y/9,67))))
        v.co.z+=amount
    t.data.update()
    c=collection(DETAIL); rng=random.Random(379151)
    sources=[o.data for o in c.objects if o.name.startswith('Exposed shale plate')][:12]
    for i in range(140):
        cx,cy,h,rx,ry=rng.choice(banks)
        x,y=cx+rng.gauss(0,rx*.5),cy+rng.gauss(0,ry*.55)
        if max(abs(x),abs(y))>164: continue
        obj=bpy.data.objects.new(f'Middle distance strata {i:03d}',rng.choice(sources)); c.objects.link(obj)
        obj.scale=(rng.uniform(2,6),rng.uniform(1.4,4),rng.uniform(2,6))
        obj.rotation_euler.z=rng.gauss(.25,.55)
        obj.location=(x,y,ground(x,y)-.1)
    camera('Geology ground-level review',(9,-40,2.7),(1,22,1.0),30)
    t['geology_pass_four']=True
    print('PASS FOUR: low irregular middle-distance banks and sparse exposed strata')


def pass_five():
    """Remove the clipped atmosphere boundary exposed by the wide review."""
    t=terrain()
    if not t.get('geology_pass_four'): raise RuntimeError('Inspect pass four first')
    if t.get('geology_pass_five'): return
    for obj in bpy.context.scene.objects:
        if obj.type=='CAMERA': obj.data.clip_end=10000
    # The former 1 km camera range clipped the 2.6 km atmosphere bounds.
    # Let dust thin gradually overhead instead of ending at a low flat ceiling.
    obj=bpy.data.objects['Thin atmospheric dust']
    for v in obj.data.vertices:
        if v.co.z>100: v.co.z=1200
    obj.data.update()
    n,l=obj.active_material.node_tree.nodes,obj.active_material.node_tree.links
    scatter=next(node for node in n if node.type=='VOLUME_SCATTER')
    geo=n.new('ShaderNodeNewGeometry')
    xyz=n.new('ShaderNodeSeparateXYZ'); l.new(geo.outputs['Position'],xyz.inputs[0])
    scale=n.new('ShaderNodeMath'); scale.operation='MULTIPLY'; scale.inputs[1].default_value=-1/65
    l.new(xyz.outputs['Z'],scale.inputs[0])
    decay=n.new('ShaderNodeMath'); decay.operation='EXPONENT'; l.new(scale.outputs[0],decay.inputs[0])
    density=n.new('ShaderNodeMath'); density.operation='MULTIPLY'; density.inputs[1].default_value=.0014
    l.new(decay.outputs[0],density.inputs[0]); l.new(density.outputs[0],scatter.inputs['Density'])
    bpy.context.scene.view_settings.exposure=.4
    t['geology_pass_five']=True
    print('PASS FIVE: extended camera range and height-faded atmosphere')


if __name__=='__main__':
    pass_one()
