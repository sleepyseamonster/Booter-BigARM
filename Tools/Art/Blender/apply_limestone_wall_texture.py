"""Map the repeating limestone tile onto the saved Cube wall, at physical scale.
Run in background on the existing BooterGroundSetup.blend -- ROOT.
Preserves geometry, object transforms and other objects; keeps a local backup.
"""
import hashlib, json, math, shutil, sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
scene_path=root/'SourceArt/Blender/Studies/BooterGroundSetup/BooterGroundSetup.blend'
asset=root/'SourceArt/Blender/Studies/LimestoneBrickWall'
texture=asset/'LimestoneCutBrick_Albedo_Repeat.png'
proof=root/'Logs/LimestoneBrickWall/WallApplication'; proof.mkdir(parents=True,exist_ok=True)
receipt=asset/'wall_application.json'
assert Path(bpy.data.filepath).resolve()==scene_path.resolve()
def render_reviews(scene,wall):
    dimensions=wall.dimensions
    for obj in scene.objects:
        if obj.type=='MESH': obj.hide_render=obj!=wall
    scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=1400; scene.render.resolution_y=900; scene.render.resolution_percentage=100
    camera=scene.camera
    target=sum((wall.matrix_world@Vector(c) for c in wall.bound_box),Vector())/8
    camera.data.type='ORTHO'; camera.data.ortho_scale=dimensions.x*1.18
    camera.location=target+Vector((dimensions.x*.6,-dimensions.x*1.25,dimensions.z*2.5))
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    for obj in scene.objects:
        if obj.type=='LIGHT':
            obj.hide_render=False; obj.location=target+Vector((2,-4,6) if obj.name=='Key' else (-4,-1,4))
            obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(proof/'wall-overview.png'); bpy.ops.render.render(write_still=True)
    camera.data.ortho_scale=2.6
    target=wall.matrix_world@Vector((.75,-1,.3))
    camera.location=target+Vector((3,-5,2.5)); camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(proof/'wall-corner.png'); bpy.ops.render.render(write_still=True)
if len(sys.argv[sys.argv.index('--')+1:])>1 and sys.argv[-1]=='render-only':
    report=json.loads(receipt.read_text())
    assert hashlib.sha256(scene_path.read_bytes()).hexdigest()==report['after_scene_sha256']
    render_reviews(bpy.context.scene,bpy.data.objects[report['wall_object']]); print('Transient renders only; scene not saved.'); sys.exit(0)
assert not receipt.exists(), 'The recorded wall application already exists; inspect before replacing it'
before_hash=hashlib.sha256(scene_path.read_bytes()).hexdigest()
backup=scene_path.parent/'BooterGroundSetup_BeforeWallTexture_2026-10-12.blend'
if backup.exists(): assert hashlib.sha256(backup.read_bytes()).hexdigest()==before_hash
else: shutil.copy2(scene_path,backup)
scene=bpy.context.scene; wall=bpy.data.objects['Cube']
assert wall in list(scene.objects) and wall.type=='MESH'
assert len(wall.data.vertices)==8 and len(wall.data.polygons)==6
assert wall.parent is None and len(wall.modifiers)==0
assert all(abs(n)>1-1e-6 for p in wall.data.polygons for n in [max(abs(x) for x in p.normal)])
assert wall.matrix_world.to_3x3().col[2].normalized().dot(Vector((0,0,1)))>.9999
saved_active=bpy.context.view_layer.objects.active.name if bpy.context.view_layer.objects.active else None
def signature(obj,wall_geometry=False):
    h=hashlib.sha256(); h.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        coords=np.empty(len(obj.data.vertices)*3,dtype=np.float32); obj.data.vertices.foreach_get('co',coords); h.update(coords.tobytes())
        for p in obj.data.polygons: h.update(str(tuple(p.vertices)).encode())
        if not wall_geometry:
            for uv in obj.data.uv_layers:
                values=np.empty(len(uv.data)*2,dtype=np.float32); uv.data.foreach_get('uv',values); h.update(values.tobytes())
            h.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return h.hexdigest()
before={o.name:signature(o,o==wall) for o in scene.objects}
scene_names=[s.name for s in bpy.data.scenes]
if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
if wall.data.users>1: wall.data=wall.data.copy()
scales=Vector(tuple(wall.matrix_world.to_3x3().col[i].length for i in range(3)))
coords=[Vector(tuple(v.co[i]*scales[i] for i in range(3))) for v in wall.data.vertices]
minimum=Vector(tuple(min(p[i] for p in coords) for i in range(3)))
maximum=Vector(tuple(max(p[i] for p in coords) for i in range(3)))
dimensions=maximum-minimum
perimeter=2*(dimensions.x+dimensions.y)
nominal_tile=1.143
perimeter_tiles=max(1,round(perimeter/nominal_tile))
tile_u=perimeter/perimeter_tiles
tile_v=nominal_tile
brick_width_inches=(tile_u/5)/.0254
assert 8<=brick_width_inches<=10, 'Whole-perimeter repeat would exceed the requested brick width'
uv=wall.data.uv_layers.get('Limestone Wall - metre mapping') or wall.data.uv_layers.new(name='Limestone Wall - metre mapping')
wall.data.uv_layers.active=uv; uv.active_render=True
face_mapping=[]
for polygon in wall.data.polygons:
    axis=max(range(3),key=lambda i:abs(polygon.normal[i])); positive=polygon.normal[axis]>0
    for index in polygon.loop_indices:
        p=coords[wall.data.loops[index].vertex_index]
        if axis==2:
            # Square brick footprints on the caps: ten rows span twice the side tile width.
            u=(p.x-minimum.x)/tile_u; v=(p.y-minimum.y)/(2*tile_u)
        elif axis==1 and not positive:
            u=(p.x-minimum.x)/tile_u; v=(p.z-minimum.z)/tile_v
        elif axis==0 and positive:
            u=(dimensions.x+p.y-minimum.y)/tile_u; v=(p.z-minimum.z)/tile_v
        elif axis==1 and positive:
            u=(dimensions.x+dimensions.y+maximum.x-p.x)/tile_u; v=(p.z-minimum.z)/tile_v
        else:
            u=(2*dimensions.x+dimensions.y+maximum.y-p.y)/tile_u; v=(p.z-minimum.z)/tile_v
        uv.data[index].uv=(u,v)
    polygon.material_index=0
    face_mapping.append(dict(face=polygon.index,normal=list(polygon.normal),cap=axis==2,
                             uv=[list(uv.data[i].uv) for i in polygon.loop_indices]))
image=bpy.data.images.load(str(texture),check_existing=True)
image.name='LimestoneCutBrick_Albedo_Repeat'; image.colorspace_settings.name='sRGB'; image.use_fake_user=True; image.pack()
material=bpy.data.materials.new('LimestoneCutBrick - repeating wall'); material.use_nodes=True
nodes,links=material.node_tree.nodes,material.node_tree.links
bsdf=nodes.get('Principled BSDF'); bsdf.inputs['Roughness'].default_value=.8; bsdf.inputs['Metallic'].default_value=0
uv_node=nodes.new('ShaderNodeUVMap'); uv_node.uv_map=uv.name
tex=nodes.new('ShaderNodeTexImage'); tex.image=image; tex.extension='REPEAT'; tex.interpolation='Linear'
links.new(uv_node.outputs['UV'],tex.inputs['Vector']); links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
wall.data.materials.clear(); wall.data.materials.append(material)
for obj in scene.objects: obj.select_set(False)
wall.hide_set(False); wall.hide_select=False; wall.select_set(True); bpy.context.view_layer.objects.active=wall
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D': area.spaces.active.shading.type='MATERIAL'
assert scene_names==[s.name for s in bpy.data.scenes]
assert before=={o.name:signature(o,o==wall) for o in scene.objects}, 'Other scene contents or wall geometry changed'
bpy.ops.wm.save_as_mainfile(filepath=str(scene_path),check_existing=False)
report=dict(scene=str(scene_path),backup=str(backup),before_scene_sha256=before_hash,after_scene_sha256=hashlib.sha256(scene_path.read_bytes()).hexdigest(),
            previous_saved_active=saved_active,wall_object=wall.name,identification='Only eight-vertex, six-face cuboid in the saved scene; proportions match the owner-described wall.',
            material=material.name,image=image.name,texture_sha256=hashlib.sha256(texture.read_bytes()).hexdigest(),uv_layer=uv.name,
            wall_dimensions_metres=list(dimensions),wall_transform=[list(row) for row in wall.matrix_world],
            vertical_tile_metres=tile_v,horizontal_tile_metres=tile_u,whole_perimeter_tile_count=perimeter_tiles,
            brick_width_inches=brick_width_inches,brick_height_inches=4.5,cap_brick_footprint_inches=[brick_width_inches,brick_width_inches],
            geometry_unchanged=True,object_signatures=before,faces=face_mapping,scene_count=len(bpy.data.scenes))
receipt.write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print(json.dumps({k:v for k,v in report.items() if k not in {'object_signatures','faces'}},indent=2))
# Transient renders for visual QA; saved camera/lights remain unchanged.
render_reviews(scene,wall)
print('Same scene saved with only Cube UV/material changes; transient wall renders completed.')
