"""Preserve a saved artist rock; measure reduction candidates, bake and export a separate game asset.

Blender --background artist.blend --python this.py -- inspect|build ROOT
Source .blend files remain local. Never saves over the input file.
"""
import json
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(sys.argv[-1]).resolve()
MODE = sys.argv[-2]
LOCAL = ROOT / "SourceArt/Blender/Studies/RockSculpting"
PROOF = ROOT / "Logs/RockSculpt"
EXPORT = ROOT / "Assets/_Project/Art/Environment/Rocks/SculptedBasalt01"
TEXTURES = ROOT / "SourceArt/Blender/Studies/textures"
PROOF.mkdir(parents=True, exist_ok=True)


def active(obj):
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    obj.hide_set(False)
    obj.hide_select = False
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def stats(obj):
    obj.data.calc_loop_triangles()
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    result = dict(vertices=len(bm.verts), faces=len(bm.faces), triangles=len(obj.data.loop_triangles),
                  nonmanifold=sum(not e.is_manifold for e in bm.edges),
                  loose=sum(not v.link_edges for v in bm.verts),
                  degenerate=sum(f.calc_area() < 1e-12 for f in bm.faces), volume=bm.calc_volume())
    bm.free()
    return result


def reduce(source, ratio, name):
    obj = source.copy()
    obj.data = source.data.copy()
    bpy.context.scene.collection.objects.link(obj)
    obj.name = name
    active(obj)
    mod = obj.modifiers.new("Measured collapse", "DECIMATE")
    mod.ratio = ratio
    mod.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def samples(mesh, count=6000):
    mesh.calc_loop_triangles()
    rng = random.Random(20261010)
    triangles = mesh.loop_triangles
    result = [mesh.vertices[i].co.copy() for i in range(0,len(mesh.vertices),max(1,len(mesh.vertices)//count))]
    for _ in range(count):
        tri = triangles[rng.randrange(len(triangles))]
        a,b,c = [mesh.vertices[i].co for i in tri.vertices]
        u,v = rng.random(),rng.random()
        if u+v > 1: u,v=1-u,1-v
        result.append(a+(b-a)*u+(c-a)*v)
    return result


def bvh(mesh):
    return BVHTree.FromPolygons([v.co for v in mesh.vertices], [p.vertices[:] for p in mesh.polygons])


def make_hull(obj):
    bm=bmesh.new()
    for vert in obj.data.vertices: bm.verts.new(vert.co)
    bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
    unused=[v for v in bm.verts if not v.link_faces]
    if unused: bmesh.ops.delete(bm,geom=unused,context="VERTS")
    bmesh.ops.triangulate(bm,faces=list(bm.faces)); bm.to_mesh(obj.data); bm.free()


def error(source, candidate, source_points, source_bvh):
    target_bvh = bvh(candidate.data)
    distances = [target_bvh.find_nearest(p)[3] for p in source_points]
    distances += [source_bvh.find_nearest(p)[3] for p in samples(candidate.data)]
    distances.sort()
    return dict(p95_m=distances[int(.95*(len(distances)-1))], max_sampled_m=max(distances), samples=len(distances))


def look(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat("-Z","Y").to_euler()


def render(obj, label, direction=(8,-11,7)):
    scene=bpy.context.scene
    for other in scene.objects:
        if other.type in {"MESH","FONT"}: other.hide_render=other != obj
    obj.hide_render=False
    camera=scene.camera
    center=obj.location+Vector((0,0,2.35))
    camera.location=center+Vector(direction)
    look(camera,center)
    camera.data.ortho_scale=7.4
    scene.render.engine="BLENDER_EEVEE"
    scene.render.resolution_x=900
    scene.render.resolution_y=900
    scene.render.resolution_percentage=100
    for light in [o for o in scene.objects if o.type=="LIGHT"]:
        light.hide_set(False)
        light.location=obj.location+Vector((2 if "Key" in light.name else -4,-4,8))
        look(light,center)
    scene.render.filepath=str(PROOF/(label+".png"))
    bpy.ops.render.render(write_still=True)


source=bpy.data.objects.get("Rock_Sculpt_01") or bpy.data.objects.get("Artist sculpt - preserved high detail")
if source is None: raise RuntimeError("Expected saved artist Rock_Sculpt_01")
active(source)
if MODE == "verify":
    import numpy as np
    low=bpy.data.objects["SculptedBasalt01_LOD0"]
    meshes=[bpy.data.objects["SculptedBasalt01_LOD"+str(i)] for i in range(3)]+[bpy.data.objects["SculptedBasalt01_Collision"]]
    for obj in meshes:
        checked=stats(obj)
        assert checked["nonmanifold"] == 0 and checked["loose"] == 0 and checked["degenerate"] == 0 and checked["volume"] > 0
    for obj in meshes[:3]:
        uv=obj.data.uv_layers.active
        assert uv and all(math.isfinite(x) and 0 <= x <= 1 for loop in uv.data for x in loop.uv)
    image=bpy.data.images.get("SculptedBasalt01_Normal")
    pixels=np.array(image.pixels[:],dtype=np.float32).reshape(-1,4)
    assert np.isfinite(pixels).all()
    normal_report=dict(finite=True,blue_above_half_fraction=float(np.mean(pixels[:,2]>.5)))
    # Check bake coverage at triangle centres, where shader texture lookup matters.
    low.data.calc_loop_triangles(); uv=low.data.uv_layers.active
    sample_uv=[sum((uv.data[i].uv for i in tri.loops),Vector((0,0)))/3 for tri in low.data.loop_triangles]
    sampled=[pixels[min(1023,int(p.y*1024))*1024+min(1023,int(p.x*1024)),:3] for p in sample_uv]
    assert all(np.linalg.norm(p*2-1)>.5 and p[2]>.5 for p in sampled), "Missed normal bake ray or invalid UV-centre normal"
    normal_report["triangle_centres_checked"]=len(sampled)
    (PROOF/"blender-reopen-verification.json").write_text(json.dumps(dict(meshes={o.name:stats(o) for o in meshes},normal_bake=normal_report),indent=2))
    for label,direction in (("basalt-back",(-8,11,7)),("basalt-top",(0,-.1,15)),("basalt-underside",(6,-9,-8))):
        render(low,label,direction)
elif MODE == "repair-collision":
    hull=bpy.data.objects["SculptedBasalt01_Collision"]
    make_hull(hull)
    if stats(hull)["triangles"] > 240:
        simpler=reduce(hull,240/stats(hull)["triangles"],"Replacement collision")
        bpy.data.objects.remove(hull,do_unlink=True); hull=simpler; hull.name="SculptedBasalt01_Collision"
        make_hull(hull)
    assert stats(hull)["nonmanifold"] == 0 and stats(hull)["triangles"] <= 255
    meshes=[bpy.data.objects["SculptedBasalt01_LOD"+str(i)] for i in range(3)]+[hull]
    for obj in meshes: active(obj); obj.location=(0,0,0)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes: obj.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0]
    bpy.ops.export_scene.fbx(filepath=str(EXPORT/"SculptedBasalt01.fbx"),use_selection=True,object_types={"MESH"},global_scale=1,
        apply_unit_scale=True,apply_scale_options="FBX_SCALE_UNITS",axis_forward="-Z",axis_up="Y",use_mesh_modifiers=True,
        mesh_smooth_type="FACE",use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode="STRIP")
    for obj in meshes: obj.location=(5,0,0)
    for obj in meshes[1:]: obj.hide_render=True; obj.hide_set(True)
    active(meshes[0])
    source.hide_render=True; source.hide_set(True); source.hide_select=True
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath,check_existing=False)
    report=json.loads((PROOF/"delivery.json").read_text())
    report["game_meshes"]={o.name:stats(o) for o in meshes}
    (PROOF/"delivery.json").write_text(json.dumps(report,indent=2))
elif MODE == "inspect":
    report={"blender":bpy.app.version_string,"source":stats(source),"dimensions":list(source.dimensions),"candidates":[]}
    points=samples(source.data)
    tree=bvh(source.data)
    for ratio in [.005,.01,.02,.04,.08,.12,.2]:
        candidate=reduce(source,ratio,"Candidate")
        row=dict(ratio=ratio,**stats(candidate),**error(source,candidate,points,tree))
        report["candidates"].append(row)
        print(json.dumps(row),flush=True)
        bpy.data.objects.remove(candidate,do_unlink=True)
    (PROOF/"reduction-candidates.json").write_text(json.dumps(report,indent=2))
    render(source,"artist-source")
else:
    output=LOCAL/"RockSculpt_Basalt_GameReady_2026-10-10.blend"
    if output.exists() or EXPORT.exists(): raise FileExistsError("Preserve existing delivery before rebuilding")
    report=json.loads((PROOF/"reduction-candidates.json").read_text())
    acceptable=[r for r in report["candidates"] if r["p95_m"] <= .01 and r["max_sampled_m"] <= .03 and r["nonmanifold"] == 0]
    chosen=min(acceptable,key=lambda r:r["triangles"])
    low=reduce(source,chosen["ratio"],"SculptedBasalt01_LOD0")
    # Shared local coordinates and bottom-centre pivot. Preserve the original artist file separately.
    coords=[v.co for v in source.data.vertices]
    pivot=Vector(((min(v.x for v in coords)+max(v.x for v in coords))/2,
                  (min(v.y for v in coords)+max(v.y for v in coords))/2,min(v.z for v in coords)))
    for obj in (source,low):
        for vert in obj.data.vertices: vert.co -= pivot
        obj.location=(5,0,0)
        for face in obj.data.polygons: face.use_smooth=True
    active(low)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.025, area_weight=0.0, correct_aspect=True, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    EXPORT.mkdir(parents=True)
    material=bpy.data.materials.new("Sculpted Basalt - projected bake source")
    material.use_nodes=True
    nodes,links=material.node_tree.nodes,material.node_tree.links
    nodes.clear()
    out=nodes.new("ShaderNodeOutputMaterial")
    shader=nodes.new("ShaderNodeBsdfPrincipled")
    links.new(shader.outputs["BSDF"],out.inputs["Surface"])
    shader.inputs["Metallic"].default_value=0
    coord=nodes.new("ShaderNodeTexCoord")
    scale=nodes.new("ShaderNodeVectorMath"); scale.operation="SCALE"; scale.inputs[3].default_value=.8
    links.new(coord.outputs["Object"],scale.inputs[0])
    channels={}
    for channel,suffix in (("BaseColor","Albedo.png"),("Roughness","Roughness.png"),("Occlusion","AO.png"),("Height","Height16.png")):
        node=nodes.new("ShaderNodeTexImage")
        node.image=bpy.data.images.load(str(TEXTURES/("VolcanicBlackBasalt_"+suffix)),check_existing=True)
        node.image.colorspace_settings.name="sRGB" if channel=="BaseColor" else "Non-Color"
        node.projection="BOX"; node.projection_blend=.25
        links.new(scale.outputs["Vector"],node.inputs["Vector"])
        channels[channel]=node
    links.new(channels["BaseColor"].outputs["Color"],shader.inputs["Base Color"])
    links.new(channels["Roughness"].outputs["Color"],shader.inputs["Roughness"])
    bump=nodes.new("ShaderNodeBump"); bump.inputs["Strength"].default_value=.35; bump.inputs["Distance"].default_value=.018
    links.new(channels["Height"].outputs["Color"],bump.inputs["Height"])
    links.new(bump.outputs["Normal"],shader.inputs["Normal"])
    source.data.materials.clear(); source.data.materials.append(material)
    low.data.materials.clear(); low.data.materials.append(material)
    scene=bpy.context.scene
    scene.render.engine="CYCLES"; scene.cycles.device="CPU"; scene.cycles.samples=8
    scene.render.bake.margin=16
    scene.render.bake.use_clear=True
    emission=nodes.new("ShaderNodeEmission")
    target=nodes.new("ShaderNodeTexImage"); nodes.active=target
    maps={}
    for channel in ("BaseColor","Roughness","Occlusion"):
        active(low)
        image=bpy.data.images.new("SculptedBasalt01_"+channel,width=1024,height=1024,alpha=False)
        image.colorspace_settings.name="sRGB" if channel=="BaseColor" else "Non-Color"
        target.image=image; nodes.active=target
        links.new(channels[channel].outputs["Color"],emission.inputs["Color"])
        links.new(emission.outputs["Emission"],out.inputs["Surface"])
        bpy.ops.object.bake(type="EMIT",use_selected_to_active=False)
        image.filepath_raw=str(EXPORT/("SculptedBasalt01_"+channel+".png")); image.file_format="PNG"; image.save()
        maps[channel]=image
    links.new(shader.outputs["BSDF"],out.inputs["Surface"])
    normal=bpy.data.images.new("SculptedBasalt01_Normal",width=1024,height=1024,alpha=False)
    normal.colorspace_settings.name="Non-Color"; target.image=normal; nodes.active=target
    active(low); source.select_set(True); source.hide_render=False
    bpy.ops.object.bake(type="NORMAL",use_selected_to_active=True,normal_space="TANGENT",cage_extrusion=.06,max_ray_distance=.12)
    normal.filepath_raw=str(EXPORT/"SculptedBasalt01_Normal.png"); normal.file_format="PNG"; normal.save(); maps["Normal"]=normal
    import numpy as np
    rough=np.empty(1024*1024*4,dtype=np.float32); maps["Roughness"].pixels.foreach_get(rough)
    packed=np.zeros_like(rough).reshape(-1,4); packed[:,3]=1-rough.reshape(-1,4)[:,0]
    surface=bpy.data.images.new("SculptedBasalt01_MetallicSmoothness",width=1024,height=1024,alpha=True)
    surface.colorspace_settings.name="Non-Color"; surface.pixels.foreach_set(packed.ravel())
    surface.filepath_raw=str(EXPORT/"SculptedBasalt01_MetallicSmoothness.png"); surface.file_format="PNG"; surface.save()
    game=bpy.data.materials.new("SculptedBasalt01 - dark volcanic"); game.use_nodes=True
    gn,gl=game.node_tree.nodes,game.node_tree.links
    bsdf=gn.get("Principled BSDF"); bsdf.inputs["Metallic"].default_value=0
    for channel,socket in (("BaseColor","Base Color"),("Roughness","Roughness")):
        node=gn.new("ShaderNodeTexImage"); node.image=maps[channel]; gl.new(node.outputs["Color"],bsdf.inputs[socket])
    normtex=gn.new("ShaderNodeTexImage"); normtex.image=normal
    norm=gn.new("ShaderNodeNormalMap"); gl.new(normtex.outputs["Color"],norm.inputs["Color"]); gl.new(norm.outputs["Normal"],bsdf.inputs["Normal"])
    low.data.materials.clear(); low.data.materials.append(game)
    lod1=reduce(low,.5,"SculptedBasalt01_LOD1")
    lod2=reduce(low,.25,"SculptedBasalt01_LOD2")
    hull=low.copy(); hull.data=low.data.copy(); scene.collection.objects.link(hull); hull.name="SculptedBasalt01_Collision"
    make_hull(hull)
    if stats(hull)["triangles"] > 240:
        simpler=reduce(hull,240/stats(hull)["triangles"],"SculptedBasalt01_Collision")
        bpy.data.objects.remove(hull,do_unlink=True); hull=simpler; hull.name="SculptedBasalt01_Collision"
        make_hull(hull)
    assert stats(hull)["nonmanifold"] == 0 and stats(hull)["triangles"] <= 255
    meshes=(low,lod1,lod2,hull)
    for obj in meshes:
        active(obj); obj.location=(0,0,0)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes: obj.select_set(True)
    bpy.context.view_layer.objects.active=low
    bpy.ops.export_scene.fbx(filepath=str(EXPORT/"SculptedBasalt01.fbx"),use_selection=True,object_types={"MESH"},
        global_scale=1,apply_unit_scale=True,apply_scale_options="FBX_SCALE_UNITS",axis_forward="-Z",axis_up="Y",
        use_mesh_modifiers=True,mesh_smooth_type="FACE",use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode="STRIP")
    for obj in meshes: obj.location=(5,0,0)
    source.name="Artist sculpt - preserved high detail"
    source.hide_render=True; source.hide_set(True); source.hide_select=True
    for obj in (lod1,lod2,hull): obj.hide_render=True; obj.hide_set(True)
    active(low)
    render(low,"basalt-game-ready")
    # Restore starter reference visibility and present textured LOD0 in Object Mode.
    for obj in scene.objects:
        if obj.name.startswith("Reference") or obj.name.startswith("Flat Ground") or obj.type=="FONT": obj.hide_render=False
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=="VIEW_3D":
                area.spaces.active.shading.type="MATERIAL"
                area.spaces.active.region_3d.view_location=Vector((5,0,2.4))
                area.spaces.active.region_3d.view_distance=9
    if bpy.context.window: bpy.context.window.workspace=bpy.data.workspaces.get("Layout",bpy.context.window.workspace)
    for image in maps.values(): image.pack()
    bpy.ops.wm.save_as_mainfile(filepath=str(output),check_existing=False)
    report.update(chosen=chosen,pivot_shift=list(pivot),game_meshes={o.name:stats(o) for o in meshes},
        output_blend=str(output),export=str(EXPORT/"SculptedBasalt01.fbx"),texture_resolution=1024,
        maps="UV-baked original basalt base/roughness/AO; high-to-low tangent normals including restrained height bump; metallic zero, alpha smoothness",
        high_source_preserved=True)
    (PROOF/"delivery.json").write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2),flush=True)
