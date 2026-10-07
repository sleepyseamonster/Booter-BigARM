"""Build the reference slate rock as a small static mesh and bake portable maps.

Run in background Blender, without opening the user's study. Outputs only here.
"""
from pathlib import Path
import json
import math

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
NAME = 'ReferenceRock_FracturedSlate_01'
RESOLUTION = 2048


def mesh_asset():
    # Three depth contours of the same broad broken slab profile. The notches
    # are major silhouette changes; small chips and fissures exist only in maps.
    profile=[(-1.05,0),(.70,0),(1.20,.40),(1.36,1.20),(1.14,1.36),
             (1.40,2.12),(.60,2.40),(.35,2.26),(-.25,2.92),
             (-.90,3.32),(-1.20,3.00),(-1.34,1.05),(-1.25,.35)]
    count=len(profile)
    vertices=[]
    for ring in range(3):
        for i,(x,z) in enumerate(profile):
            if ring==0:
                px=x;py=-.77; pz=z
                if i in (0,1,2,12):py+=.13
            elif ring==1:
                px=x*1.04-.025;py=.0;pz=z*(.96 if x>0 else 1.0)
                if i in (3,4):px-=.07
            else:
                px=x*.86-.08;py=.83;pz=z*.92
                if i in (0,1,2,12):py-=.12
            vertices.append((px,py,pz))
    faces=[tuple(range(count))]
    for ring in range(2):
        for i in range(count):
            j=(i+1)%count
            faces.append((ring*count+i,(ring+1)*count+i,(ring+1)*count+j,ring*count+j))
    faces.append(tuple(reversed(range(count*2,count*3))))
    mesh=bpy.data.meshes.new(NAME+'_Mesh')
    mesh.from_pydata(vertices,[],faces);mesh.update()
    bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='BEAUTY',ngon_method='BEAUTY')
    bm.to_mesh(mesh);bm.free();mesh.update()
    obj=bpy.data.objects.new(NAME,mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(50),island_margin=.025)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj['asset_brief']='October 1 user reference; broad silhouette geometry, baked fracture detail'
    obj['intended_height_metres']=max(v[2] for v in vertices)
    obj['source_triangle_budget']=100
    return obj


def source_material(obj):
    mat=bpy.data.materials.new('ReferenceSlate_BakeSource')
    mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();links=mat.node_tree.links
    out=nodes.new('ShaderNodeOutputMaterial')
    bsdf=nodes.new('ShaderNodeBsdfPrincipled');links.new(bsdf.outputs['BSDF'],out.inputs['Surface'])
    texcoord=nodes.new('ShaderNodeTexCoord')
    mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='SCALE'
    mapping.inputs[3].default_value=.37
    links.new(texcoord.outputs['Object'],mapping.inputs[0])
    image=nodes.new('ShaderNodeTexImage')
    image.image=bpy.data.images.load(str(ROOT/'textures/FracturedRock_SurfaceSource.png'))
    image.projection='BOX';image.projection_blend=.15
    links.new(mapping.outputs['Vector'],image.inputs['Vector'])
    links.new(image.outputs['Color'],bsdf.inputs['Base Color'])
    gray=nodes.new('ShaderNodeRGBToBW');links.new(image.outputs['Color'],gray.inputs[0])
    rough=nodes.new('ShaderNodeMapRange')
    rough.inputs['From Min'].default_value=.04;rough.inputs['From Max'].default_value=.55
    rough.inputs['To Min'].default_value=.88;rough.inputs['To Max'].default_value=.64
    links.new(gray.outputs[0],rough.inputs['Value']);links.new(rough.outputs[0],bsdf.inputs['Roughness'])
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.75
    bump.inputs['Distance'].default_value=.055
    links.new(gray.outputs[0],bump.inputs['Height']);links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])
    obj.data.materials.append(mat)
    return mat,out,bsdf,image.outputs['Color'],rough.outputs[0]


def bake_maps(obj,mat,out,bsdf,color,roughness):
    scene=bpy.context.scene
    scene.render.engine='CYCLES';scene.cycles.samples=16
    scene.render.bake.margin=24
    scene.render.bake.use_clear=True
    scene.render.bake.normal_space='TANGENT'
    nodes=mat.node_tree.nodes;links=mat.node_tree.links
    emission=nodes.new('ShaderNodeEmission')
    maps={}
    for name,kind,socket in [('BaseColor','EMIT',color),('Roughness','EMIT',roughness),('NormalGL','NORMAL',None)]:
        img=bpy.data.images.new('ReferenceSlate_'+name,RESOLUTION,RESOLUTION,alpha=False)
        img.colorspace_settings.name='sRGB' if name=='BaseColor' else 'Non-Color'
        target=nodes.new('ShaderNodeTexImage');target.image=img
        for node in nodes:node.select=False
        target.select=True;nodes.active=target
        if kind=='EMIT':
            links.new(socket,emission.inputs['Color']);links.new(emission.outputs[0],out.inputs['Surface'])
        else:
            links.new(bsdf.outputs[0],out.inputs['Surface'])
        bpy.ops.object.bake(type=kind)
        img.filepath_raw=str(ROOT/'textures'/f'ReferenceSlate_{name}.png')
        img.file_format='PNG';img.save();img.pack()
        maps[name]=img
        nodes.remove(target)
    return maps


def runtime_material(obj,maps):
    mat=bpy.data.materials.new('ReferenceSlate_GameMaterial')
    mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links
    bsdf=nodes.get('Principled BSDF');bsdf.inputs['Metallic'].default_value=0
    for i,(name,img) in enumerate(maps.items()):
        node=nodes.new('ShaderNodeTexImage');node.image=img;node.label=name
        node.location=(-600,300-i*300)
        if name=='NormalGL':
            normal=nodes.new('ShaderNodeNormalMap');normal.space='TANGENT';normal.uv_map=obj.data.uv_layers.active.name
            normal.location=(-260,-220)
            links.new(node.outputs['Color'],normal.inputs['Color'])
            links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
        else:
            links.new(node.outputs['Color'],bsdf.inputs['Base Color' if name=='BaseColor' else 'Roughness'])
    obj.data.materials.clear();obj.data.materials.append(mat)
    return mat


def render_view(obj,name,direction,solid=False):
    scene=bpy.context.scene
    center=Vector((0,0,1.55))
    camera=scene.camera
    camera.location=center+Vector(direction)*7
    camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO';camera.data.ortho_scale=4.3
    scene.render.engine='BLENDER_WORKBENCH' if solid else 'CYCLES'
    if solid:
        scene.display.shading.light='STUDIO';scene.display.shading.color_type='SINGLE'
        scene.display.shading.single_color=(.45,.46,.48)
        scene.display.shading.show_cavity=True
    scene.render.filepath=str(ROOT/f'ReferenceSlate_{name}.png')
    bpy.ops.render.render(write_still=True)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    obj=mesh_asset()
    source=source_material(obj)
    maps=bake_maps(obj,*source)
    runtime_material(obj,maps)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'
    scene.unit_settings.scale_length=1
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ReferenceSlate.blend'))
    bpy.ops.export_scene.gltf(filepath=str(ROOT/'ReferenceSlate.glb'),export_format='GLB',use_selection=True,export_apply=True)
    if hasattr(bpy.ops.export_scene,'fbx'):
        bpy.ops.export_scene.fbx(filepath=str(ROOT/'ReferenceSlate.fbx'),use_selection=True,
            object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
            use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=False,path_mode='RELATIVE')
    bm=bmesh.new();bm.from_mesh(obj.data)
    report={'name':obj.name,'vertices':len(obj.data.vertices),'triangles':len(obj.data.polygons),
        'material_slots':len(obj.data.materials),'uv_layers':len(obj.data.uv_layers),
        'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),
        'degenerate_faces':sum(f.calc_area()<1e-9 for f in bm.faces),
        'dimensions_metres':list(obj.dimensions),'modifiers':len(obj.modifiers),
        'texture_resolution':RESOLUTION,'blender':bpy.app.version_string}
    bm.free()
    (ROOT/'mesh_report.json').write_text(json.dumps(report,indent=2)+'\n')
    print('REFERENCE_ROCK_REPORT',json.dumps(report))
    world=bpy.data.worlds.new('Temporary neutral review world');world.use_nodes=True
    world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.17,.19,1)
    world.node_tree.nodes['Background'].inputs[1].default_value=.25;scene.world=world
    for name,location,energy,size in [('Key',(-3,-4,7),1000,4),('Fill',(5,-1,4),350,3),('Rim',(1,4,6),800,3)]:
        data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=size
        light=bpy.data.objects.new(name,data);scene.collection.objects.link(light);light.location=location
        light.rotation_euler=(Vector((0,0,1.5))-light.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Temporary review camera');camera=bpy.data.objects.new('Temporary review camera',data)
    scene.collection.objects.link(camera);scene.camera=camera
    scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.cycles.samples=32;scene.cycles.use_denoising=True
    render_view(obj,'Beauty',(1,-1.7,.65))
    render_view(obj,'Back',(-1,1,.6))
    render_view(obj,'Elevated',(1,-1.4,1.65))
    render_view(obj,'Geometry',(1,-1.7,.65),solid=True)


if __name__=='__main__':
    main()
