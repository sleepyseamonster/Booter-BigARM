"""Reopen source and exports, and compare preserved study objects when supplied."""
import argparse
from array import array
import hashlib
import json
import math
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
NAME='ReferenceRock_FracturedSlate_01'


def inspect(obj):
    mesh=obj.data
    bm=bmesh.new();bm.from_mesh(mesh)
    coords=[obj.matrix_world @ v.co for v in mesh.vertices]
    bounds=[max(p[i] for p in coords)-min(p[i] for p in coords) for i in range(3)]
    result={'triangles':sum(len(p.vertices)-2 for p in mesh.polygons),
        'vertices':len(mesh.vertices),'materials':len(mesh.materials),
        'uv_layers':len(mesh.uv_layers),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),
        'degenerate_faces':sum(f.calc_area()<1e-9 for f in bm.faces),
        'finite':all(math.isfinite(c) for v in mesh.vertices for c in v.co),
        'dimensions_metres':bounds,'modifiers':len(obj.modifiers)}
    bm.free()
    return result


def scene_signature():
    records={}
    for obj in bpy.context.scene.objects:
        h=hashlib.sha256()
        h.update(repr(tuple(tuple(row) for row in obj.matrix_world)).encode())
        h.update(repr((obj.type,obj.hide_render,obj.hide_viewport)).encode())
        if obj.type=='MESH':
            for prop,field,length,kind in [(obj.data.vertices,'co',3,'f'),(obj.data.loops,'vertex_index',1,'i'),(obj.data.polygons,'loop_total',1,'i')]:
                data=array(kind,[0])*(len(prop)*length);prop.foreach_get(field,data);h.update(data.tobytes())
            h.update(repr([m.name if m else None for m in obj.data.materials]).encode())
        elif obj.type=='LIGHT':h.update(repr((obj.data.type,obj.data.energy,tuple(obj.data.color))).encode())
        elif obj.type=='CAMERA':h.update(repr((obj.data.type,obj.data.lens,obj.data.clip_start,obj.data.clip_end)).encode())
        records[obj.name]=h.hexdigest()
    return records


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--baseline');parser.add_argument('--study')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ReferenceSlate.blend'))
    rock=bpy.data.objects[NAME];source=inspect(rock)
    assert source['triangles']==74 and source['nonmanifold_edges']==0 and source['degenerate_faces']==0 and source['finite']
    assert source['materials']==1 and source['uv_layers']==1 and source['modifiers']==0
    images=[n.image for n in rock.active_material.node_tree.nodes if n.type=='TEX_IMAGE']
    assert len(images)==3 and all(i.packed_file and tuple(i.size)==(2048,2048) for i in images)
    assert all(-1e-5<=c<=1.00001 for loop in rock.data.uv_layers.active.data for c in loop.uv)
    report={'source':source,'packed_runtime_images':[i.name for i in images]}
    for fmt in ('glb','fbx'):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        if fmt=='glb':bpy.ops.import_scene.gltf(filepath=str(ROOT/'ReferenceSlate.glb'))
        else:bpy.ops.import_scene.fbx(filepath=str(ROOT/'ReferenceSlate.fbx'))
        meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
        assert len(meshes)==1
        data=inspect(meshes[0]);report[fmt]=data
        assert data['triangles']==74 and data['finite'] and data['materials']==1 and data['uv_layers']==1
        assert max(abs(a-b) for a,b in zip(data['dimensions_metres'],source['dimensions_metres']))<1e-4
    if args.baseline and args.study:
        bpy.ops.wm.open_mainfile(filepath=args.baseline);before=scene_signature()
        bpy.ops.wm.open_mainfile(filepath=args.study);after=scene_signature()
        changed=[n for n in before if before[n]!=after.get(n)]
        added=sorted(set(after)-set(before))
        assert not changed,changed
        assert added==[NAME],added
        rock=bpy.data.objects[NAME];saved=inspect(rock)
        assert saved['triangles']==74 and saved['nonmanifold_edges']==0
        report['study']={'preserved_objects':len(before),'changed_existing_objects':changed,'added_objects':added,'rock':saved}
    (ROOT/'validation_report.json').write_text(json.dumps(report,indent=2)+'\n')
    print('REFERENCE_ROCK_VALIDATION',json.dumps(report))


if __name__=='__main__':main()
