"""Controlled offline material study; never edits imported production assets.

Blender --background --factory-startup --python render_comparison.py -- ROOT [--preview]
Native source stays in ignored SourceArt; review renders and receipt stay beside this tool.
"""
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

args = sys.argv[sys.argv.index('--') + 1:]
ROOT = Path(args[0]).resolve()
PREVIEW = '--preview' in args
HERE = Path(__file__).resolve().parent.parent
OUT = HERE / 'renders'
LOCAL = ROOT / 'SourceArt/Blender/Studies/Sable/StylizedRealism20261015'
OUT.mkdir(parents=True, exist_ok=True)
LOCAL.mkdir(parents=True, exist_ok=True)
ROCK = ROOT / 'Assets/_Project/Art/Environment/Rocks/SculptedBasalt01'
GROUND = ROOT / 'SourceArt/Blender/Studies/textures/BadlandsCompactedRustSoil.png'
sources = [ROCK / 'SculptedBasalt01.fbx', ROCK / 'SculptedBasalt01_BaseColor.png',
           ROCK / 'SculptedBasalt01_Normal.png', ROCK / 'SculptedBasalt01_Roughness.png', GROUND]
atlases = {1: HERE / 'textures/B_subtle_atlas.png', 2: HERE / 'textures/C_painterly_atlas.png'}
panel_rects = {1:{'rock':(2,642),'ground':(649,1444),'ceramic':(1454,2169)},
               2:{'rock':(2,721),'ground':(727,1445),'ceramic':(1453,2169)}}
for path in atlases.values():
    if not path.exists():
        raise FileNotFoundError(f'Generate the study albedo atlas first: {path}')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


source_hashes = {str(p.relative_to(ROOT)): sha(p) for p in sources}
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
devices_used = ['CPU']
if not PREVIEW:
    try:
        prefs = bpy.context.preferences.addons['cycles'].preferences
        prefs.compute_device_type = 'OPTIX'
        prefs.get_devices()
        devices_used = []
        for device in prefs.devices:
            device.use = device.type == 'OPTIX'
            if device.use:
                devices_used.append(device.name)
        if devices_used:
            scene.cycles.device = 'GPU'
        else:
            devices_used = ['CPU']
    except Exception as exc:
        print('GPU_UNAVAILABLE', str(exc), flush=True)
        devices_used = ['CPU']
scene.cycles.samples = 12 if PREVIEW else 24
scene.cycles.use_denoising = True
scene.render.resolution_x = 1000 if PREVIEW else 1440
scene.render.resolution_y = 800 if PREVIEW else 1080
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.view_settings.view_transform = 'AgX'
scene.view_settings.look = 'AgX - Medium High Contrast'
scene.view_settings.exposure = 0
scene.render.film_transparent = False
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.45, .48, .54, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .45


def node(mat, kind, name=None):
    n = mat.node_tree.nodes.new(kind)
    if name:
        n.name = name
        n.label = name
    return n


def link(mat, a, b):
    mat.node_tree.links.new(a, b)


def mathnode(mat, operation, a, b=None):
    n = node(mat, 'ShaderNodeMath')
    n.operation = operation
    for i, v in enumerate([a, b]):
        if v is not None:
            if isinstance(v, (int, float)):
                n.inputs[i].default_value = v
            else:
                link(mat, v, n.inputs[i])
    return n.outputs[0]


def blend(mat, a, b, factor, mode='MIX'):
    n = node(mat, 'ShaderNodeMixRGB')
    n.blend_type = mode
    n.inputs[0].default_value = factor
    for socket, v in zip(n.inputs[1:3], [a, b]):
        if isinstance(v, tuple):
            socket.default_value = v
        else:
            link(mat, v, socket)
    return n.outputs[0]


def image_node(mat, path, data=False):
    n = node(mat, 'ShaderNodeTexImage')
    # Copy image datablock to avoid a data-map colorspace change affecting albedo.
    n.image = bpy.data.images.load(str(path), check_existing=True)
    if data:
        n.image.colorspace_settings.name = 'Non-Color'
    return n


def material(name, kind, level):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    tex = node(m, 'ShaderNodeTexCoord')
    mapping = node(m, 'ShaderNodeVectorMath')
    mapping.operation = 'MULTIPLY'
    mapping.inputs[1].default_value = ((40, 40, 40) if kind == 'ground' else (1, 1, 1))
    link(m, tex.outputs['Generated'], mapping.inputs[0])
    fine = node(m, 'ShaderNodeTexNoise')
    fine.inputs['Scale'].default_value = 175 if kind != 'ground' else 75
    fine.inputs['Detail'].default_value = 3
    link(m, mapping.outputs[0], fine.inputs['Vector'])
    # Anisotropic noise supplies broken directional dabs in object space.
    stroke_map = node(m, 'ShaderNodeVectorMath')
    stroke_map.operation = 'MULTIPLY'
    stroke_map.inputs[1].default_value = (3, 12, 5) if kind == 'ground' else (18, 14, 8)
    link(m, tex.outputs['Object' if kind == 'ground' else 'Generated'], stroke_map.inputs[0])
    strokes = node(m, 'ShaderNodeTexVoronoi', 'Directional pigment dabs')
    strokes.feature = 'SMOOTH_F1'
    strokes.inputs['Scale'].default_value = 1
    strokes.inputs['Smoothness'].default_value = .5
    link(m, stroke_map.outputs[0], strokes.inputs['Vector'])
    pigment = node(m, 'ShaderNodeRGBToBW')
    link(m, strokes.outputs['Color'], pigment.inputs[0])
    ramp = node(m, 'ShaderNodeValToRGB', 'Form-following painted variation')
    ramp.color_ramp.elements[0].position = .18
    ramp.color_ramp.elements[1].position = .8
    palette = {
        'rock': ((.065, .071, .085, 1), (.23, .21, .185, 1)),
        'ground': ((.10, .065, .037, 1), (.30, .215, .14, 1)),
        'ceramic': ((.065, .11, .115, 1), (.22, .34, .32, 1)),
        'metal': ((.11, .067, .038, 1), (.32, .26, .17, 1)),
    }
    ramp.color_ramp.elements[0].color, ramp.color_ramp.elements[1].color = palette[kind]
    link(m, pigment.outputs[0], ramp.inputs['Fac'])
    normal = None
    if kind == 'rock':
        color = image_node(m, ROCK / 'SculptedBasalt01_BaseColor.png').outputs['Color']
        color = blend(m, color, (.12,.13,.15,1), .16)
        rough = image_node(m, ROCK / 'SculptedBasalt01_Roughness.png', True).outputs['Color']
        normal_map = image_node(m, ROCK / 'SculptedBasalt01_Normal.png', True)
        nm = node(m, 'ShaderNodeNormalMap')
        nm.inputs['Strength'].default_value = [1, .42, .12][level]
        link(m, normal_map.outputs['Color'], nm.inputs['Color'])
        normal = nm.outputs['Normal']
    elif kind == 'ground':
        img = image_node(m, GROUND)
        link(m, mapping.outputs[0], img.inputs['Vector'])
        color = img.outputs['Color']
        gray = node(m, 'ShaderNodeRGBToBW')
        link(m, color, gray.inputs[0])
        color = blend(m, color, gray.outputs[0], .6)
        color = blend(m, color, (.55,.48,.40,1), 1, 'MULTIPLY')
        rough = .88
    else:
        color = palette[kind][1]
        rough = .54 if kind == 'metal' else .63
        color = blend(m, color, fine.outputs['Fac'], .10, 'MULTIPLY')
    if level:
        if kind == 'metal':
            color = blend(m, color, ramp.outputs['Color'], .12 if level == 1 else .25)
        else:
            # Use observed panel boundaries: generated draft B did not obey equal widths.
            atlas = image_node(m, atlases[level])
            uv = tex.outputs['UV' if kind == 'rock' else 'Generated']
            if kind == 'ground':
                repeat = node(m, 'ShaderNodeVectorMath')
                repeat.operation = 'FRACTION'
                link(m, mapping.outputs[0], repeat.inputs[0])
                uv = repeat.outputs[0]
            elif kind == 'ceramic':
                separate = node(m, 'ShaderNodeSeparateXYZ')
                combine = node(m, 'ShaderNodeCombineXYZ')
                link(m, uv, separate.inputs[0])
                link(m, separate.outputs['X'], combine.inputs['X'])
                link(m, separate.outputs['Z'], combine.inputs['Y'])
                uv = combine.outputs[0]
            else:
                repeat = node(m, 'ShaderNodeVectorMath')
                repeat.operation = 'FRACTION'
                link(m, uv, repeat.inputs[0])
                uv = repeat.outputs[0]
            left,right = panel_rects[level][kind]
            uv_scale = node(m, 'ShaderNodeVectorMath')
            uv_scale.operation = 'MULTIPLY'
            uv_scale.inputs[1].default_value = ((right-left)/2172,720/724,1)
            link(m, uv, uv_scale.inputs[0])
            offset = node(m, 'ShaderNodeVectorMath')
            offset.operation = 'ADD'
            offset.inputs[1].default_value = (left/2172,2/724,0)
            link(m, uv_scale.outputs[0], offset.inputs[0])
            link(m, offset.outputs[0], atlas.inputs['Vector'])
            source_color = color
            color = atlas.outputs['Color']
            if kind == 'ground':
                gray = node(m, 'ShaderNodeRGBToBW')
                link(m, color, gray.inputs[0])
                color = blend(m, color, gray.outputs[0], .6)
                color = blend(m, color, (.55,.48,.40,1), 1, 'MULTIPLY')
            elif kind == 'rock':
                # Generated UV padding may drift: retain baseline wherever a draft samples black padding.
                atlas_value = node(m, 'ShaderNodeRGBToBW')
                link(m, color, atlas_value.inputs[0])
                padding = mathnode(m, 'LESS_THAN', atlas_value.outputs[0], .008)
                safe = node(m, 'ShaderNodeMixRGB', 'UV padding safety fallback')
                link(m, padding, safe.inputs[0])
                link(m, color, safe.inputs[1])
                link(m, source_color, safe.inputs[2])
                color = safe.outputs[0]
                color = blend(m, color, (.12,.13,.15,1), .16)
    link(m, color, bs.inputs['Base Color'])
    if isinstance(rough, float):
        bs.inputs['Roughness'].default_value = rough
    else:
        link(m, rough, bs.inputs['Roughness'])
    if kind == 'metal':
        bs.inputs['Metallic'].default_value = .75
    bump = node(m, 'ShaderNodeBump')
    bump.inputs['Strength'].default_value = [.36, .13, .04][level]
    bump.inputs['Distance'].default_value = .014 if kind == 'ground' else .008
    link(m, fine.outputs['Fac'], bump.inputs['Height'])
    if normal:
        link(m, normal, bump.inputs['Normal'])
    link(m, bump.outputs['Normal'], bs.inputs['Normal'])
    return m


mats = {level: {kind: material(f'{label}_{kind}', kind, level) for kind in ['rock', 'ground', 'ceramic', 'metal']}
        for level, label in enumerate(['A_Realistic', 'B_Subtle', 'C_Painterly'])}
members = []


def register(obj, kind):
    obj.data.materials.clear()
    obj.data.materials.append(mats[0][kind])
    members.append((obj, kind))
    return obj


bpy.ops.import_scene.fbx(filepath=str(ROCK / 'SculptedBasalt01.fbx'))
meshes = [o for o in bpy.context.selected_objects if o.type == 'MESH']
print('ROCK_IMPORT', [(o.name, len(o.data.polygons), list(o.dimensions)) for o in meshes], flush=True)
rock = max(meshes, key=lambda o: len(o.data.polygons))
for obj in list(bpy.context.selected_objects):
    if obj != rock:
        bpy.data.objects.remove(obj, do_unlink=True)
rock.parent = None
bpy.context.view_layer.objects.active = rock
rock.select_set(True)
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
# FBX importer converts Unity's Y-up asset into Blender Z-up.
corners = [rock.matrix_world @ Vector(c) for c in rock.bound_box]
rock.location -= Vector(((min(v.x for v in corners) + max(v.x for v in corners))/2,
                         (min(v.y for v in corners) + max(v.y for v in corners))/2,
                         min(v.z for v in corners)))
rock.location += Vector((-.85, .55, -.06))
register(rock, 'rock')

# Ground relief is authored once and reused unchanged for every treatment.
verts, faces = [], []
n = 100
for j in range(n + 1):
    for i in range(n + 1):
        x, y = -30 + 60*i/n, -30 + 60*j/n
        z = -.08 + .018*math.sin(x*1.8+y*.8) + .012*math.cos(y*2.5-x*.5)
        verts.append((x, y, z))
for j in range(n):
    for i in range(n):
        a = j*(n+1)+i
        faces.append((a, a+1, a+n+2, a+n+1))
mesh = bpy.data.meshes.new('Shared_ground_relief')
mesh.from_pydata(verts, [], faces)
ground = bpy.data.objects.new('Ground', mesh)
scene.collection.objects.link(ground)
register(ground, 'ground')
for p in ground.data.polygons:
    p.use_smooth = True


def cube(name, loc, scale, kind, bevel=.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('Manufactured edge radius', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        o.modifiers.new('Corner normals', 'WEIGHTED_NORMAL')
    register(o, kind)
    return o


px, py = 1.65, -.7
cube('Ceramic canister body', (px, py, .51), (.62, .42, .98), 'ceramic', .075)
for h in [.14, .83]:
    cube('Iron retention band', (px, py, h), (.66, .455, .085), 'metal', .016)
cube('Iron base', (px, py, .035), (.67, .47, .09), 'metal', .02)
for dx in [-.205, .205]:
    cube('Handle support', (px+dx, py, 1.075), (.06, .09, .20), 'metal', .02)
cube('Carry handle', (px, py, 1.17), (.46, .09, .07), 'metal', .025)
bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=.10, depth=.07, location=(px+.16, py, 1.025))
register(bpy.context.object, 'metal').name = 'Sealed cap'
# Small fixed stones ground the rock; deterministic placement, no production placement system.
for i, (x,y,s) in enumerate([(-2.3,-.9,.18),(-1.8,-1.3,.12),(.2,-1.4,.15),(.65,.1,.21),(2.5,.4,.13),(-2.9,1.5,.20)]):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=s, location=(x,y,s*.3-.03))
    o = bpy.context.object
    o.scale = (1.2,.7,.6)
    o.name = f'Fixed stone {i}'
    register(o, 'rock')


def point(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()


sun_data = bpy.data.lights.new('Shared low sun', 'SUN')
sun = bpy.data.objects.new('Shared low sun', sun_data)
scene.collection.objects.link(sun)
sun.location = (-7,-9,6)
point(sun, (0,0,0))
sun_data.angle = math.radians(8)
fill_data = bpy.data.lights.new('Shared broad sky fill', 'AREA')
fill = bpy.data.objects.new('Shared broad sky fill', fill_data)
scene.collection.objects.link(fill)
fill.location = (3,-5,8)
point(fill, (0,0,1))
fill_data.energy = 450
fill_data.shape = 'DISK'
fill_data.size = 9
fill_data.color = (.72,.81,1)
bpy.ops.object.camera_add()
camera = bpy.context.object
camera.name = 'Review_camera'
scene.camera = camera
camera.data.type = 'PERSP'
camera.data.sensor_fit = 'VERTICAL'
camera.data.sensor_height = 24
camera.data.lens = 24/(2*math.tan(math.radians(48)/2))
camera.data.clip_end = 150
target = Vector((0,0,1.15))
views = {'close': (11, 38, 40), 'gameplay': (22, 50, 40), 'orbit': (16, 26, 115), 'prop': (3.1, 30, 35)}
lights = {'sunset': (2.4, (1,.82,.64), .6), 'twilight': (.85, (1,.63,.45), .28)}
def geometry_fingerprints():
    result = {}
    for o, kind in members:
        payload = json.dumps({'verts':[list(v.co) for v in o.data.vertices],
                              'polygons':[list(p.vertices) for p in o.data.polygons],
                              'matrix':[list(r) for r in o.matrix_world]}, sort_keys=True).encode()
        result[o.name] = hashlib.sha256(payload).hexdigest()
    return result


geometry = geometry_fingerprints()
receipt = {'blender':bpy.app.version_string, 'source_hashes':source_hashes,
           'study_atlas_hashes':{str(p.relative_to(HERE)):sha(p) for p in atlases.values()},
           'atlas_panel_rects_x_pixels':panel_rects,
           'geometry_hashes':geometry, 'rock_dimensions_m':list(rock.dimensions),
           'camera_vertical_fov':48, 'view_transform':'AgX', 'exposure':0,
           'script_sha256':sha(Path(__file__)), 'resolution':[scene.render.resolution_x,scene.render.resolution_y],
           'fill_size_m':9, 'fill_color':list(fill_data.color),
           'samples':scene.cycles.samples, 'renders':[],
           'render_device':scene.cycles.device, 'devices':devices_used,
           'method':'Original baseline maps and imagegen-authored painted albedo atlases, reduced fine relief; offline look development, not manually painted production textures or Unity proof.'}
for light_name, (energy, color, ambient) in lights.items():
    if PREVIEW and light_name != 'sunset':
        continue
    sun_data.energy = energy
    sun_data.color = color
    fill_data.energy = 450 if light_name == 'sunset' else 160
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = ambient
    for view_name, (distance, pitch, yaw) in views.items():
        if PREVIEW and view_name != 'close':
            continue
        p,y = math.radians(pitch), math.radians(yaw)
        framing_target = Vector((px,py,.60)) if view_name == 'prop' else target
        camera.location = framing_target + Vector((math.sin(y)*math.cos(p), -math.cos(y)*math.cos(p), math.sin(p)))*distance
        point(camera, framing_target)
        for level, label in enumerate(['A','B','C']):
            for obj, kind in members:
                obj.data.materials[0] = mats[level][kind]
            assert geometry_fingerprints() == geometry, 'Geometry or placement changed between variants'
            filename = f'{light_name}_{view_name}_{label}.png'
            scene.render.filepath = str(OUT / filename)
            print('SABLE_RENDER', filename, flush=True)
            bpy.ops.render.render(write_still=True)
            receipt['renders'].append({'file':filename, 'variant':label, 'light':light_name,
                'sun_energy':energy, 'sun_color':color, 'ambient':ambient, 'view':view_name,
                'fill_energy':fill_data.energy, 'geometry_verified':True,
                'camera':list(camera.location), 'pitch':pitch, 'yaw':yaw, 'distance':distance,
                'sha256':sha(OUT / filename)})
for path in sources:
    assert sha(path) == source_hashes[str(path.relative_to(ROOT))], f'Source changed: {path}'
if not PREVIEW:
    bpy.ops.wm.save_as_mainfile(filepath=str(LOCAL / 'Sable_StylizedRealism_Comparison.blend'))
receipt_path = ROOT / 'Logs/SableStylizedRealism20261015/preview_receipt.json' if PREVIEW else HERE / 'render_receipt.json'
receipt_path.write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
print('SABLE_COMPLETE', len(receipt['renders']), flush=True)
