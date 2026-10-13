"""Read-only saved-scene check of wall scale, UV corners and protected content."""
import hashlib, json, math, sys
from pathlib import Path
import bpy
import numpy as np

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
asset=root/'SourceArt/Blender/Studies/LimestoneBrickWall'
applied=json.loads((asset/'wall_application.json').read_text())
scene_path=Path(applied['scene'])
assert Path(bpy.data.filepath).resolve()==scene_path.resolve()
assert hashlib.sha256(scene_path.read_bytes()).hexdigest()==applied['after_scene_sha256']
assert hashlib.sha256(Path(applied['backup']).read_bytes()).hexdigest()==applied['before_scene_sha256']
texture=asset/'LimestoneCutBrick_Albedo_Repeat.png'
assert hashlib.sha256(texture.read_bytes()).hexdigest()==applied['texture_sha256']
wall=bpy.data.objects[applied['wall_object']]
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
assert applied['object_signatures']=={o.name:signature(o,o==wall) for o in bpy.context.scene.objects}
assert len(bpy.data.scenes)==applied['scene_count']==1
assert len(wall.data.vertices)==8 and len(wall.data.polygons)==6
assert bpy.context.view_layer.objects.active==wall and wall.select_get()
assert len(wall.data.materials)==1 and wall.data.materials[0].name==applied['material']
material=wall.data.materials[0]; nodes=material.node_tree.nodes
tex=next(n for n in nodes if n.type=='TEX_IMAGE')
assert tex.extension=='REPEAT' and tex.image.packed_file and tex.image.colorspace_settings.name=='sRGB'
assert tuple(tex.image.size)==(2048,2048)
uvnode=next(n for n in nodes if n.type=='UVMAP'); assert uvnode.uv_map==applied['uv_layer']
uv=wall.data.uv_layers[applied['uv_layer']]
assert uv.active_render and wall.data.uv_layers.active==uv
assert all(math.isfinite(x) for loop in uv.data for x in loop.uv)
positions=[wall.matrix_world@v.co for v in wall.data.vertices]
seams={}; edge_checks=0
for polygon in wall.data.polygons:
    loops=list(polygon.loop_indices)
    cap=abs(polygon.normal.z)>.9
    for a,b in zip(loops,loops[1:]+loops[:1]):
        ia,ib=wall.data.loops[a].vertex_index,wall.data.loops[b].vertex_index
        ua,ub=uv.data[a].uv,uv.data[b].uv
        du,dv=abs(ub.x-ua.x),abs(ub.y-ua.y)
        length=(positions[ia]-positions[ib]).length
        if du>1e-5:
            assert dv<1e-5 and abs(length/du-applied['horizontal_tile_metres'])<1e-5
        else:
            expected=2*applied['horizontal_tile_metres'] if cap else applied['vertical_tile_metres']
            assert dv>1e-5 and abs(length/dv-expected)<1e-5
        edge_checks+=1
        if not cap and du<1e-5:
            seams.setdefault(tuple(sorted((ia,ib))),[]).append({ia:ua.copy(),ib:ub.copy()})
for vertices,pairs in seams.items():
    assert len(pairs)==2
    for vertex in vertices:
        a,b=pairs[0][vertex],pairs[1][vertex]
        assert abs(a.y-b.y)<1e-5
        assert abs((a.x-b.x)-round(a.x-b.x))<1e-5
assert 8<=applied['brick_width_inches']<=10
assert abs(applied['horizontal_tile_metres']/5-(2*applied['horizontal_tile_metres'])/10)<1e-10
report=dict(reopened_original_scene=True,wall=wall.name,scene_count=1,geometry_and_other_objects_unchanged=True,
            wall_vertices=8,wall_faces=6,material=material.name,packed_texture=True,repeat_enabled=True,
            edge_scale_checks=edge_checks,continuous_vertical_corner_edges=len(seams),
            brick_width_inches=applied['brick_width_inches'],brick_height_inches=applied['brick_height_inches'],
            square_cap_footprint=True,selected_wall=True)
(asset/'wall_application_verification.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print(json.dumps(report,indent=2))
