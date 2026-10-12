"""Author the rigid framed pack in Booter's cloak coordinate space. Local source only."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--') + 1])
source = root / 'SourceArt/Blender/Studies/BooterBackpack'
export = root / 'Assets/_Project/Art/Characters/Booter/Backpack'
proof = root / 'Docs/Evidence/Art/BooterBackpack'
for folder in (source, export, proof): folder.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name, color, metallic=0):
    m = bpy.data.materials.new(name); m.diffuse_color = (*color, 1); m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metallic; p.inputs['Roughness'].default_value = .78
    return m
canvas = mat('PackCanvas', (.32,.225,.13))
webbing = mat('PackWebbing', (.095,.075,.052))
iron = mat('PackFrame', (.24,.23,.20), .7)
parts = []
def box(name, loc, dims, material, bevel=.02):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o=bpy.context.object; o.name=name; o.scale=dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(material)
    b=o.modifiers.new('Soft constructed edge','BEVEL'); b.width=bevel; b.segments=3
    o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL'); parts.append(o)
    return o
def tube(name, points, radius, material):
    curve=bpy.data.curves.new(name,'CURVE'); curve.dimensions='3D'
    curve.bevel_depth=radius; curve.bevel_resolution=2; curve.resolution_u=4
    s=curve.splines.new('POLY'); s.points.add(len(points)-1)
    for p,co in zip(s.points,points): p.co=(*co,1)
    o=bpy.data.objects.new(name,curve); bpy.context.collection.objects.link(o)
    o.data.materials.append(material); parts.append(o); return o
def ribbon(name, points, width):
    vs=[]; fs=[]
    for x,y,z in points: vs.extend([(x-width/2,y,z),(x+width/2,y,z)])
    for i in range(len(points)-1): a=2*i; fs.append((a,a+1,a+3,a+2))
    m=bpy.data.meshes.new(name); m.from_pydata(vs,[],fs); m.update()
    o=bpy.data.objects.new(name,m); bpy.context.collection.objects.link(o)
    o.data.materials.append(webbing); o.modifiers.new('Woven strap thickness','SOLIDIFY').thickness=.007
    b=o.modifiers.new('Strap edge','BEVEL'); b.width=.003; b.segments=2; parts.append(o)

# Cloak's rear surface is Blender +Y .18 at the shoulders, .25 at the hips.
# Pack contact face begins at .29, leaving cloth clearance. Unity maps rear to -Z.
box('Main canvas sack',(0,.425,1.19),(.43,.27,.61),canvas,.055)
box('Overhanging storm lid',(0,.424,1.51),(.47,.30,.105),canvas,.035)
box('Lower reinforced base',(0,.43,.902),(.43,.27,.07),webbing,.022)
box('Rear bellows pocket',(0,.583,1.18),(.30,.09,.29),canvas,.03)
box('Rear pocket flap',(0,.60,1.335),(.33,.10,.045),canvas,.018)
for side in (-1,1):
    x=side*.257
    box('Side pouch',(x,.43,1.09),(.105,.20,.28),canvas,.027)
    box('Side pocket rim',(x,.43,1.238),(.115,.20,.035),webbing,.012)
    tube('External frame rail',[(side*.237,.28,.83),(side*.237,.274,1.49),(side*.21,.29,1.56)],.013,iron)
    ribbon('Shoulder harness',[(side*.19,.31,1.39),(side*.19,.16,1.49),(side*.19,-.02,1.47),(side*.19,-.14,1.32),(side*.16,-.13,1.10),(side*.20,.02,.99),(side*.22,.28,1.01)],.042)
    ribbon('Lid tie down',[(side*.13,.55,1.55),(side*.13,.587,1.44),(side*.13,.587,1.36)],.024)
    box('Lid buckle',(side*.13,.599,1.402),(.039,.014,.033),iron,.004)
    for z in (1.02,1.32):
        box('Side compression tape',(side*.22,.43,z),(.013,.29,.022),webbing,.004)
        box('Compression buckle',(side*.237,.49,z),(.018,.032,.032),iron,.004)
    box('Lumbar pad',(side*.12,.274,.975),(.17,.035,.16),webbing,.025)
tube('Upper frame bridge',[(-.21,.29,1.56),(0,.29,1.575),(.21,.29,1.56)],.013,iron)
tube('Lower frame bridge',[(-.237,.28,.84),(.237,.28,.84)],.013,iron)
tube('Carry loop',[(-.055,.40,1.56),(-.05,.40,1.62),(.05,.40,1.62),(.055,.40,1.56)],.009,webbing)
# Waist wings and buckle wrap the outside of the worn garment.
tube('Hip harness',[(-.22,.28,.96),(-.29,.1,.96),(-.24,-.14,.96),(0,-.18,.96),(.24,-.14,.96),(.29,.1,.96),(.22,.28,.96)],.024,webbing)
box('Waist buckle',(0,-.20,.96),(.065,.022,.044),iron,.005)
box('Sternum tape',(0,-.15,1.24),(.38,.009,.022),webbing,.003)

# One rigid game mesh, three material slots; named binding contract in the animation driver.
for o in parts:
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH')
bpy.ops.object.select_all(action='DESELECT')
for o in parts: o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join()
pack=bpy.context.object; pack.name='Backpack_RigidPack'; pack.data.name='Backpack_RigidPack'
bpy.context.scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
pack.data.calc_loop_triangles()
assert all(math.isfinite(c) for v in pack.data.vertices for c in v.co)
bpy.ops.export_scene.fbx(filepath=str(export/'BooterFramedBackpack.fbx'),use_selection=True,
    object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,
    use_mesh_modifiers=True,add_leaf_bones=False,path_mode='AUTO')
report={'blender':bpy.app.version_string,'triangles':len(pack.data.loop_triangles),
        'dimensions_author_space':list(pack.dimensions),'character_visual_scale':1.4,
        'materials':sorted({m.name for m in pack.data.materials}),
        'unity_rear_axis':'-Z','source':str(source/'BooterFramedBackpack.blend')}
(proof/'mesh-report.json').write_text(json.dumps(report,indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(source/'BooterFramedBackpack.blend'))
print(json.dumps(report))
