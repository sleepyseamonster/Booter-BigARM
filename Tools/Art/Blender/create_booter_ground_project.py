"""Create a new, local-only Blender project from the Unity standing snapshot.
Run Blender --background --factory-startup --python THIS -- CHECKOUT_ROOT.
Existing authoring files are never replaced.
"""
import json
import sys
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
folder = root / 'SourceArt/Blender/Studies/BooterGroundSetup'
output = folder / 'BooterGroundSetup.blend'
proof = root / 'Logs/BooterGroundSetup'
snapshot = json.loads((proof / 'booter_standing.json').read_text())
if output.exists():
    raise RuntimeError(f'Preserve the existing project before creating another: {output}')
folder.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene.unit_settings.length_unit = 'METERS'
character = bpy.data.collections.new('Booter - Unity standing reference')
scene.collection.children.link(character)
clothing = bpy.data.collections.new('Unity clothing sources - unposed, hidden')
scene.collection.children.link(clothing)
clothing.hide_viewport = True
clothing.hide_render = True
parent = bpy.data.objects.new('Booter - move character here', None)
character.objects.link(parent)
parent.empty_display_type = 'PLAIN_AXES'
parent.empty_display_size = .25
bottom = min(v[1] for m in snapshot['meshes'] if m['name'] == 'Body' for v in m['vertices'])
materials = {}
for source in snapshot['meshes']:
    vertices = [(v[0], -v[2], v[1] - bottom) for v in source['vertices']]
    faces, indices = [], []
    for index, part in enumerate(source['parts']):
        triangles = part['triangles']
        faces.extend((triangles[i], triangles[i+2], triangles[i+1]) for i in range(0, len(triangles), 3))
        indices.extend([index] * (len(triangles) // 3))
    mesh = bpy.data.meshes.new(source['name'])
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Booter_' + source['name'], mesh)
    (character if source['name'] == 'Body' else clothing).objects.link(obj)
    obj.parent = parent
    for part in source['parts']:
        name = part['material']
        if name not in materials:
            mat = bpy.data.materials.new(name)
            mat.diffuse_color = part['color']
            mat.use_nodes = True
            node = mat.node_tree.nodes.get('Principled BSDF')
            node.inputs['Base Color'].default_value = part['color']
            node.inputs['Roughness'].default_value = .75
            materials[name] = mat
        mesh.materials.append(materials[name])
    for polygon, index in zip(mesh.polygons, indices):
        polygon.material_index = index
        polygon.use_smooth = source['name'] == 'Body'
    if len(source['uv']) == len(vertices):
        uv = mesh.uv_layers.new(name='Unity UV')
        for loop in mesh.loops:
            uv.data[loop.index].uv = source['uv'][loop.vertex_index]
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()

bpy.ops.mesh.primitive_plane_add(size=10, location=(0, 0, 0))
ground = bpy.context.object
ground.name = 'Ground - resize with Dimensions X and Y'
ground['Instructions'] = 'N sidebar > Item > Dimensions: edit X and Y in metres. Keep Z location at 0.'
mat = bpy.data.materials.new('Neutral Ground')
mat.diffuse_color = (.28, .30, .32, 1)
mat.use_nodes = True
mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = mat.diffuse_color
mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = .9
ground.data.materials.append(mat)

presentation = bpy.data.collections.new('Preview camera and lights')
scene.collection.children.link(presentation)
def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()
camera = bpy.data.objects.new('Preview Camera', bpy.data.cameras.new('Preview Camera'))
presentation.objects.link(camera)
camera.location = (8, -12, 7)
aim(camera, (0, 0, 1.3))
camera.data.lens = 48
scene.camera = camera
for name, location, energy, size in [('Key', (1, -5, 8), 1800, 6), ('Fill', (-4, 0, 5), 1000, 5), ('Rim', (3, 4, 6), 1400, 4)]:
    light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'AREA'))
    presentation.objects.link(light)
    light.location = location
    light.data.energy, light.data.shape, light.data.size = energy, 'DISK', size
    aim(light, (0, 0, 1.5))
for obj in presentation.objects:
    obj.hide_set(True)
scene.world.color = (.18, .18, .18)
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x, scene.render.resolution_y = 1100, 800
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(proof / 'preview.png')

bpy.ops.object.select_all(action='DESELECT')
ground.select_set(True)
bpy.context.view_layer.objects.active = ground
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            space = area.spaces.active
            space.show_region_ui = True
            space.shading.color_type = 'MATERIAL'
            space.shading.show_cavity = True
            space.region_3d.view_location = (0, 0, 1.2)
            space.region_3d.view_distance = 12
            space.region_3d.view_rotation = (Vector((0, 0, 1.2)) - Vector((8, -12, 7))).to_track_quat('-Z', 'Y')
            space.region_3d.view_perspective = 'PERSP'
        elif area.type == 'PROPERTIES':
            area.spaces.active.context = 'OBJECT'
note = bpy.data.texts.new('START HERE - Adjustable Ground')
note.write('''Ground is selected. Press N > Item > Dimensions to set X and Y in metres.
Or press S to scale the ground; S X / S Y changes one axis. Z location stays 0.
Booter is imported from the production Unity humanoid, at its production 1.4 scale,
in the current MoCap Central standing idle pose.
The body is an editable, frozen pose mesh rather than an animated Blender rig.
Unposed cloak/backpack source meshes are retained in a hidden collection; fitting
them to this pose is separate authoring work. They are not part of the visible setup.
Move the Booter parent empty to move all character parts together.
1 Blender metre = 1 Unity unit. The lowest body point is exactly on Z=0.
Ctrl+S saves this local-only project. Unity assets and scenes were not changed.
''')
bpy.ops.wm.save_as_mainfile(filepath=str(output), check_existing=False)
report = {'file': str(output), 'blender': bpy.app.version_string, 'source': snapshot['source'],
          'idle': snapshot['idle'], 'scale': snapshot['scale'], 'ground_dimensions': list(ground.dimensions),
          'body_ground_z': min(v.co.z for o in character.objects if o.name == 'Booter_Body' for v in o.data.vertices),
          'character_meshes': [{'name': o.name, 'vertices': len(o.data.vertices), 'faces': len(o.data.polygons)} for o in character.objects if o.type == 'MESH'],
          'hidden_clothing_sources': [o.name for o in clothing.objects]}
(proof / 'creation.json').write_text(json.dumps(report, indent=2) + '\n')
bpy.ops.render.render(write_still=True)
print(json.dumps(report, indent=2))
