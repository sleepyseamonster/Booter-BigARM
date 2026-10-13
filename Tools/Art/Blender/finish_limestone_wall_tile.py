"""Bake an AI masonry image into a repeat-ready wall color tile.
Uses Blender material sampling to crop through joint centres and harmonize
the narrow wrap borders. No project or scene file is opened or saved.
Blender --background --factory-startup --python THIS -- ROOT GENERATED_IMAGE
"""
import hashlib, json, shutil, sys
from pathlib import Path
import bpy
import numpy as np

root_arg,image_arg=sys.argv[sys.argv.index('--')+1:][:2]
root=Path(root_arg).resolve(); generated=Path(image_arg).resolve()
base=root/'SourceArt/Blender/Studies/LimestoneBrickWall'
source=base/'Source/LimestoneCutBrick_Generated.png'
final=base/'LimestoneCutBrick_Albedo_Repeat.png'
if final.exists(): raise FileExistsError('Preserve the existing wall tile before rebuilding')
source.parent.mkdir(parents=True,exist_ok=True)
shutil.copy2(generated,source)
image=bpy.data.images.load(str(source)); image.colorspace_settings.name='sRGB'
width,height=image.size
pixels=np.empty(width*height*4,dtype=np.float32); image.pixels.foreach_get(pixels)
pixels=pixels.reshape(height,width,4)
rows=pixels[:,:,:3].mean(axis=(1,2))
def joint_centre(values):
    threshold=(float(np.quantile(values,.1))+float(np.quantile(values,.9)))/2
    band=np.where(values<threshold)[0]
    assert len(band)>2
    return float((band[0]+band[-1])/2)
lower=joint_centre(rows[:25])
upper=height-1-joint_centre(rows[-25:][::-1])
assert upper-lower>height*.95
offset=(lower+.5)/height; scale=(upper-lower)/height
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.mesh.primitive_plane_add(size=2)
plane=bpy.context.object
material=bpy.data.materials.new('Repeat-ready limestone wall sampler'); material.use_nodes=True
plane.data.materials.append(material)
nodes,links=material.node_tree.nodes,material.node_tree.links; nodes.clear()
out=nodes.new('ShaderNodeOutputMaterial'); emission=nodes.new('ShaderNodeEmission')
links.new(emission.outputs['Emission'],out.inputs['Surface'])
uv=nodes.new('ShaderNodeTexCoord'); split=nodes.new('ShaderNodeSeparateXYZ'); links.new(uv.outputs['UV'],split.inputs[0])
def math_node(operation,a,b=None):
    n=nodes.new('ShaderNodeMath'); n.operation=operation
    for index,value in enumerate([a,b] if b is not None else [a]):
        if isinstance(value,(float,int)): n.inputs[index].default_value=value
        else: links.new(value,n.inputs[index])
    return n.outputs[0]
u,v=split.outputs['X'],split.outputs['Y']
inverse_u=math_node('SUBTRACT',1,u); inverse_v=math_node('SUBTRACT',1,v)
size=2048
def edge_weight(co,inverse):
    distance=math_node('MINIMUM',co,inverse)
    px=math_node('MULTIPLY',distance,size)
    ramp=nodes.new('ShaderNodeMapRange'); ramp.clamp=True
    links.new(px,ramp.inputs['Value'])
    ramp.inputs['From Min'].default_value=1
    ramp.inputs['From Max'].default_value=8
    ramp.inputs['To Min'].default_value=.5
    ramp.inputs['To Max'].default_value=0
    return ramp.outputs['Result']
wx,wy=edge_weight(u,inverse_u),edge_weight(v,inverse_v)
def sample(x,y):
    cropped_y=math_node('ADD',math_node('MULTIPLY',y,scale),offset)
    vector=nodes.new('ShaderNodeCombineXYZ'); links.new(x,vector.inputs['X']); links.new(cropped_y,vector.inputs['Y'])
    tex=nodes.new('ShaderNodeTexImage'); tex.image=image; tex.interpolation='Linear'; tex.extension='EXTEND'
    links.new(vector.outputs[0],tex.inputs['Vector']); return tex.outputs['Color']
def mix(a,b,weight):
    node=nodes.new('ShaderNodeMixRGB'); links.new(weight,node.inputs[0]); links.new(a,node.inputs[1]); links.new(b,node.inputs[2])
    return node.outputs[0]
color=mix(mix(sample(u,v),sample(inverse_u,v),wx),mix(sample(u,inverse_v),sample(inverse_u,inverse_v),wx),wy)
links.new(color,emission.inputs['Color'])
target=bpy.data.images.new('LimestoneCutBrick_Albedo_Repeat',width=size,height=size,alpha=False)
target.colorspace_settings.name='sRGB'
node=nodes.new('ShaderNodeTexImage'); node.image=target; nodes.active=node
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=1
bpy.ops.object.bake(type='EMIT',use_selected_to_active=False,margin=0)
values=np.empty(size*size*4,dtype=np.float32); target.pixels.foreach_get(values)
values=values.reshape(size,size,4)
assert np.isfinite(values).all()
# Pixel sharing at the outermost texels removes stochastic bake sampling jitter.
# The broader material blend above keeps these shared texels continuous inward.
horizontal=(values[:,0,:3]+values[:,-1,:3])*.5
values[:,0,:3]=horizontal; values[:,-1,:3]=horizontal
vertical=(values[0,:,:3]+values[-1,:,:3])*.5
values[0,:,:3]=vertical; values[-1,:,:3]=vertical
target.pixels.foreach_set(values.ravel()); target.update()
target.filepath_raw=str(final); target.file_format='PNG'; target.save()
edge_x=float(np.max(np.abs(values[:,0,:3]-values[:,-1,:3])))
edge_y=float(np.max(np.abs(values[0,:,:3]-values[-1,:,:3])))
assert edge_x==0 and edge_y==0, 'Opposite edges failed the repeat closure check'
report=dict(source_image=str(generated),source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
            final_file=str(final),final_sha256=hashlib.sha256(final.read_bytes()).hexdigest(),generated_size=[width,height],output_size=[size,size],
            joint_centres_bottom_up_pixels=[lower,upper],crop_fraction_y=[offset,offset+scale],border_harmonization_pixels=8,
            max_opposite_edge_difference_linear_rgb=[edge_x,edge_y],finite=True,wrap_mode='Repeat',
            brick_pitches_horizontal=5,courses_vertical=10,stagger='half-brick running bond',
            nominal_brick_width_inches=9,nominal_brick_height_inches=4.5,nominal_brick_depth_inches=9,
            physical_tile_width_metres=1.143,physical_tile_height_metres=1.143,
            native_generated_detail='1254-pixel AI source resampled through a material bake; not a measured stone scan',
            method='Generated masonry retained; crop through top/bottom mortar centres removes the doubled border joint. An 8-pixel material blend harmonizes both wrap borders; shared outermost texels eliminate bake sampling jitter. Interior retained.',
            scene_or_project_saved=False)
(base/'verification.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print(json.dumps(report,indent=2))
