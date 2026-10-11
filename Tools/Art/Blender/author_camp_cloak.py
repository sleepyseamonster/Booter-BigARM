"""Bounded local camp study. Run Blender --background --python this-file -- ROOT."""
import bpy, math, sys, json
from pathlib import Path
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--') + 1])
source = root / 'SourceArt/Blender/Studies/CampAndCloak'
evidence = root / 'Docs/Evidence/Art/CampSleep'
camp_export = root / 'Assets/_Project/Art/Environment/Camp'
cloak_export = root / 'Assets/_Project/Art/Characters/Booter/Cloak'
for folder in (source, evidence, camp_export, cloak_export): folder.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def material(name, color, metallic=0):
    mat = bpy.data.materials.new(name); mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = .8
    return mat
fabric = material('Dusty Ochre Canvas', (.34, .25, .16))
metal = material('Worn Iron Frame', (.16, .12, .095), .65)
cloak = material('Warm Grey Cloak', (.23, .245, .23))
objects = {}

def group(name):
    obj = bpy.data.objects.new(name, None); bpy.context.collection.objects.link(obj)
    objects[name] = obj; return obj
def mesh(name, vertices, faces, mat, parent):
    data = bpy.data.meshes.new(name); data.from_pydata(vertices, [], faces); data.update()
    obj = bpy.data.objects.new(name, data); bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat); obj.parent = parent
    return obj
def box(name, location, scale, mat, parent):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object; obj.name = name; obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat); obj.parent = parent
    bevel = obj.modifiers.new('Readable edge bevel', 'BEVEL'); bevel.width = .012; bevel.segments = 2
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj
def rod(name, a, b, radius, parent):
    delta = Vector(b) - Vector(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=delta.length,
        location=(Vector(a)+Vector(b))*.5)
    obj = bpy.context.object; obj.name = name
    obj.rotation_euler = delta.to_track_quat('Z','Y').to_euler()
    obj.data.materials.append(metal); obj.parent = parent
    return obj
def panel(name, width, origin, angle, parent):
    nx,nz = 14,12; verts=[]; faces=[]
    for iz in range(nz+1):
        z=.04+1.20*iz/nz
        for ix in range(nx+1):
            x=width*(ix/nx-.5)
            y=.028*math.sin(ix/nx*math.pi*6)*math.sin(iz/nz*math.pi)
            verts.append((x,y,z))
    for iz in range(nz):
        for ix in range(nx):
            a=iz*(nx+1)+ix; faces.append((a,a+1,a+nx+2,a+nx+1))
    obj=mesh(name,verts,faces,fabric,parent); obj.location=origin; obj.rotation_euler.z=angle
    solid=obj.modifiers.new('Fabric thickness','SOLIDIFY'); solid.thickness=.008
    return obj

deployed=group('PopupShade_Deployed')
panel('CentreCanvas',1.6,(0,0,0),0,deployed)
panel('LeftReturnCanvas',.33,(-.95,.10,0),-.60,deployed)
panel('RightReturnCanvas',.33,(.95,.10,0),.60,deployed)
for x in (-.8,.8):
    rod('TelescopingUpright', (x,0,.02),(x,0,1.31),.024,deployed)
    box('WeightedFoot',(x,0,.035),(.28,.38,.07),metal,deployed)
    rod('ShortFrameBrace',(x,-.15,.09),(x,0,.48),.012,deployed)
rod('TopSpreader',(-1.09,.20,1.29),(1.09,.20,1.29),.02,deployed)
rod('BottomHem',(-1.09,.20,.055),(1.09,.20,.055),.014,deployed)

packed=group('PopupShade_Packed')
box('FoldedCanvas',(0,0,.45),(.22,.14,.9),fabric,packed)
for x in (-.14,.14): rod('BundledPole',(x,0,0),(x,0,.94),.022,packed)
for z in (.18,.7): box('PackingStrap',(0,0,z),(.32,.17,.045),metal,packed)

worn=group('Cloak_Worn'); verts=[]; faces=[]; nx,nz=20,18
for iz in range(nz+1):
    t=iz/nz; width=.58+.34*t; z=1.46-1.15*t
    for ix in range(nx+1):
        u=ix/nx; x=(u-.5)*width
        y=.18+.13*t+.04*math.sin(u*math.pi*8)*t
        hem=.045*math.cos(u*math.pi*5)*t**8
        verts.append((x,y,z+hem))
for iz in range(nz):
    for ix in range(nx):
        if iz>nz-4 and ix==nx//2: continue
        a=iz*(nx+1)+ix; faces.append((a,a+1,a+nx+2,a+nx+1))
obj=mesh('SplitHemCloak',verts,faces,cloak,worn)
obj.modifiers.new('Cloth thickness','SOLIDIFY').thickness=.008
box('ShoulderYoke',(0,.08,1.45),(.59,.26,.065),cloak,worn)

blanket=group('Cloak_Blanket'); verts=[]; faces=[]; nx,ny=12,18
for iy in range(ny+1):
    t=iy/ny; y=-.72+1.55*t; halfwidth=.35+.10*math.sin(t*math.pi)
    for ix in range(nx+1):
        u=ix/nx; x=(u*2-1)*halfwidth
        z=.09+.25*math.sin(u*math.pi)+.025*math.sin(t*math.pi*10)*math.sin(u*math.pi)
        verts.append((x,y,z))
for iy in range(ny):
    for ix in range(nx):
        a=iy*(nx+1)+ix; faces.append((a,a+1,a+nx+2,a+nx+1))
obj=mesh('BodyDrapedBlanket',verts,faces,cloak,blanket)
obj.modifiers.new('Cloth thickness','SOLIDIFY').thickness=.009

def export(parent, path):
    bpy.ops.object.select_all(action='DESELECT')
    parent.select_set(True)
    for obj in parent.children_recursive: obj.select_set(True)
    bpy.context.view_layer.objects.active=parent
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','EMPTY'},
        apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,
        use_mesh_modifiers=True,add_leaf_bones=False,path_mode='AUTO')
for name,path in [('PopupShade_Deployed',camp_export/'PopupShade_Deployed.fbx'),
                  ('PopupShade_Packed',camp_export/'PopupShade_Packed.fbx'),
                  ('Cloak_Worn',cloak_export/'Cloak_Worn.fbx'),
                  ('Cloak_Blanket',cloak_export/'Cloak_Blanket.fbx')]: export(objects[name],path)

# Inspection staging is retained in the local source; it is not exported.
packed.location=(2.4,0,0); worn.location=(-2.4,0,0); blanket.location=(0,1.25,0)
bpy.ops.mesh.primitive_plane_add(size=200)
ground=bpy.context.object; ground.name='ReviewGround'; ground.data.materials.append(material('Review earth',(.16,.105,.07)))
bpy.ops.object.light_add(type='AREA',location=(0,-4,7)); bpy.context.object.data.energy=1300; bpy.context.object.data.shape='DISK'; bpy.context.object.data.size=6
bpy.ops.object.light_add(type='SUN'); bpy.context.object.rotation_euler=(.9,-.5,-.7); bpy.context.object.data.energy=2
bpy.ops.object.camera_add(location=(5,-7,5))
camera=bpy.context.object; camera.rotation_euler=(Vector((0,.5,.65))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=7; bpy.context.scene.camera=camera
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=1200; scene.render.resolution_y=800; scene.render.resolution_percentage=100
bpy.ops.wm.save_as_mainfile(filepath=str(source/'CampAndCloak.blend'))
report={}
deps=bpy.context.evaluated_depsgraph_get()
for name,parent in objects.items():
    tris=0
    for obj in parent.children_recursive:
        if obj.type!='MESH': continue
        evaluated=obj.evaluated_get(deps); data=evaluated.to_mesh(); data.calc_loop_triangles()
        assert all(math.isfinite(c) for v in data.vertices for c in v.co)
        tris+=len(data.loop_triangles); evaluated.to_mesh_clear()
    report[name]={'evaluated_triangles':tris}
(evidence/'mesh-report.json').write_text(json.dumps(report,indent=2))
for name,loc in [('overview',(5,-7,5)),('rear',(-5,7,4)),('top',(0,.5,9))]:
    camera.location=loc; camera.rotation_euler=(Vector((0,.5,.65))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(evidence/(name+'.png')); bpy.ops.render.render(write_still=True)
print(json.dumps(report))
