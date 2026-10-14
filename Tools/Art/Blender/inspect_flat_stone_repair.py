"""Read-only geometric and material diagnosis of the selected paving asset."""
import sys,json,hashlib
from pathlib import Path
import bpy,bmesh
import numpy as np
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
proof=root/'Logs/FlatStoneGroundPatch01/Repair01'; proof.mkdir(parents=True,exist_ok=True)
patch=bpy.data.objects.get('FlatStoneGroundPatch01'); assert patch
mesh=patch.data; mesh.calc_loop_triangles(); bm=bmesh.new(); bm.from_mesh(mesh)
report=dict(file=bpy.data.filepath,vertices=len(bm.verts),triangles=len(mesh.loop_triangles),
    boundaries=sum(e.is_boundary for e in bm.edges),nonmanifold=sum(not e.is_manifold for e in bm.edges),
    negative_up_faces=sum(f.normal.z<-.1 and f.calc_center_median().z>.005 for f in bm.faces),
    materials=[m.name for m in mesh.materials],uvs=[u.name for u in mesh.uv_layers],
    selected=[o.name for o in bpy.context.view_layer.objects if o.select_get()],matrix=[list(v) for v in patch.matrix_world])
bm.free(); print(json.dumps(report),flush=True)
def signature(obj,transform=True):
    h=hashlib.sha256()
    if transform:h.update(str(list(obj.matrix_world)).encode())
    if obj.type=='MESH':
        a=np.empty(len(obj.data.vertices)*3,dtype=np.float32);obj.data.vertices.foreach_get('co',a);h.update(a.tobytes())
        for p in obj.data.polygons:h.update(str(tuple(p.vertices)).encode())
        for uv in obj.data.uv_layers:
            a=np.empty(len(uv.data)*2,dtype=np.float32);uv.data.foreach_get('uv',a);h.update(a.tobytes())
        h.update(str([m.name if m else None for m in obj.data.materials]).encode())
    return h.hexdigest()
report['patch_geometry_signature']=signature(patch,False)
report['other_signatures']={o.name:signature(o) for o in bpy.context.scene.objects if o!=patch}
(proof/(sys.argv[-1]+'-inspection.json')).write_text(json.dumps(report,indent=2)+'\n',newline='\n')
if sys.argv[-1]=='saved':
    scene=bpy.context.scene
    for o in scene.objects:
        if o.type=='MESH': o.hide_render=o!=patch
    coords=[patch.matrix_world@Vector(c) for c in patch.bound_box]; center=sum(coords,Vector())/8
    camera=scene.camera; camera.data.type='ORTHO'; camera.data.ortho_scale=4.5
    # Frontmost small stones and an adjacent large stone show the reported seam/cut artifacts.
    target=patch.matrix_world@Vector((-1,-2,0))
    camera.location=target+Vector((0,-4,3)); camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    for o in scene.objects:
        if o.type=='LIGHT':
            o.location=center+Vector((2,-4,8) if o.name=='Key' else (-4,4,6))
            o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
    scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    scene.render.filepath=str(proof/'before-textured.png'); bpy.ops.render.render(write_still=True)
    gray=bpy.data.materials.new('Repair diagnostic neutral');gray.use_nodes=True;gray.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.6,.6,.6,1)
    mesh.materials.clear();mesh.materials.append(gray)
    scene.render.filepath=str(proof/'before-untextured.png');bpy.ops.render.render(write_still=True)
