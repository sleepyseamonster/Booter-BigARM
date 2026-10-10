"""Editor-only exact solid union. JSON meshes in/out; preserves UV and per-corner source normals.

blender --background --factory-startup --python this.py -- input.json output.json
No .blend is modified. Disjoint, open or invalid geometry is rejected.
"""
import json
import math
import sys
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def vector(value, size=3):
    return tuple(value[k] for k in "xyz"[:size]) if isinstance(value,dict) else tuple(value)


def topology(mesh):
    bm=bmesh.new(); bm.from_mesh(mesh)
    remaining=set(bm.verts); components=0
    while remaining:
        components+=1; stack=[remaining.pop()]
        while stack:
            for edge in stack.pop().link_edges:
                for vertex in edge.verts:
                    if vertex in remaining: remaining.remove(vertex); stack.append(vertex)
    report=dict(nonmanifold=sum(not e.is_manifold for e in bm.edges),
                degenerate=sum(f.calc_area()<1e-12 for f in bm.faces),
                loose=sum(not v.link_faces for v in bm.verts),components=components,volume=bm.calc_volume())
    bm.free(); return report


def import_mesh(data,index):
    vertices=[vector(v) for v in data["vertices"]]
    if not all(math.isfinite(x) for v in vertices for x in v): raise ValueError("Non-finite input")
    triangles=data["triangles"]
    mesh=bpy.data.meshes.new("Source "+str(index))
    mesh.from_pydata(vertices,[],[triangles[i:i+3] for i in range(0,len(triangles),3)])
    mesh.update()
    uv=mesh.uv_layers.new(name="UVMap")
    normal=mesh.attributes.new("fusion_normal", "FLOAT_VECTOR", "CORNER")
    mesh.attributes.new("fusion_tangent", "FLOAT_VECTOR", "CORNER")
    mesh.attributes.new("fusion_sign", "FLOAT", "CORNER")
    provenance=mesh.attributes.new("fusion_source", "INT", "FACE")
    materials=mesh.attributes.new("fusion_material", "INT", "FACE")
    for face in mesh.polygons: provenance.data[face.index].value=index
    for face in mesh.polygons: materials.data[face.index].value=data["materialIndices"][face.index]
    uv=mesh.uv_layers.active; normal=mesh.attributes["fusion_normal"]
    for loop in mesh.loops:
        uv.data[loop.index].uv=vector(data["uv"][loop.vertex_index],2)
        normal.data[loop.index].vector=vector(data["normals"][loop.vertex_index])
        mesh.attributes["fusion_tangent"].data[loop.index].vector=vector(data["tangents"][loop.vertex_index])
        mesh.attributes["fusion_sign"].data[loop.index].value=data["tangents"][loop.vertex_index]["w"]
    bm=bmesh.new(); bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    if bm.calc_volume()<0: bmesh.ops.reverse_faces(bm,faces=list(bm.faces))
    bm.to_mesh(mesh); bm.free(); mesh.update()
    report=topology(mesh)
    if report["nonmanifold"] or report["degenerate"] or report["loose"] or report["volume"]<=0:
        raise ValueError("Source is not a closed, positive-volume solid: "+str(report))
    obj=bpy.data.objects.new(mesh.name,mesh); bpy.context.scene.collection.objects.link(obj)
    return obj,report


def strictly_inside(tree, point):
    _,_,_,distance=tree.find_nearest(point)
    if distance<=1e-5: return False
    votes=0
    for direction in (Vector((1,.347,.193)),Vector((-.238,1,.631)),Vector((.113,-.457,1))):
        direction.normalize(); cursor=point.copy(); crossings=0
        while crossings<512:
            hit,_,_,_=tree.ray_cast(cursor,direction)
            if hit is None: break
            crossings+=1; cursor=hit+direction*1e-6
        if crossings>=512: raise ValueError("Ray classification did not terminate")
        votes+=crossings%2
    return votes>=2


def fuse(payload):
    if payload["version"]!=2 or not 2<=len(payload["sources"])<=16: raise ValueError("Expected 2–16 source rocks")
    sources=[]; reports=[]
    for i,data in enumerate(payload["sources"]):
        obj,report=import_mesh(data,i); sources.append(obj); reports.append(report)
    source_trees=[BVHTree.FromPolygons([v.co for v in o.data.vertices],[p.vertices[:] for p in o.data.polygons]) for o in sources]
    result=sources[0].copy(); result.data=sources[0].data.copy(); bpy.context.scene.collection.objects.link(result)
    bpy.context.view_layer.objects.active=result; result.select_set(True)
    for obj in sources[1:]:
        modifier=result.modifiers.new("Remove buried surfaces","BOOLEAN")
        modifier.operation="UNION"; modifier.solver="EXACT"; modifier.object=obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    report=topology(result.data)
    if report["nonmanifold"] or report["degenerate"] or report["loose"] or report["components"]!=1 or report["volume"]<=0:
        raise ValueError("Union must be one closed solid. Overlap the rocks; touching/disjoint rocks are rejected: "+str(report))
    if report["volume"]>=sum(r["volume"] for r in reports)-1e-7:
        raise ValueError("No positive-volume overlap; move the rocks into one another")
    # Original face provenance identifies the new rock-to-rock seams, independent of triangulation.
    bm=bmesh.new(); bm.from_mesh(result.data); label=bm.faces.layers.int.get("fusion_source")
    if label is None: raise ValueError("Boolean lost source provenance")
    seams=sum(len(e.link_faces)==2 and e.link_faces[0][label]!=e.link_faces[1][label] for e in bm.edges)
    for edge in bm.edges:
        if len(edge.link_faces)==2 and edge.link_faces[0][label]!=edge.link_faces[1][label]: edge.smooth=False
    bm.to_mesh(result.data); bm.free(); result.data.update()
    if not result.data.attributes.get("fusion_normal"): raise ValueError("Boolean lost corner normals")
    result.data.calc_loop_triangles()
    interior=0
    # Polygon vertex averages can lie outside concave faces. Test actual tessellated surface points.
    for face in result.data.loop_triangles:
        center=sum((result.data.vertices[i].co for i in face.vertices),Vector())/3
        for tree in source_trees:
            if strictly_inside(tree,center):
                interior+=1; break
    if interior: raise ValueError("Union retained buried faces: "+str(interior))
    positions=[]; normals=[]; tangents=[]; uvs=[]; triangles=[]; material_indices=[]
    attrs=result.data.attributes["fusion_normal"].data
    for triangle in result.data.loop_triangles:
        material_indices.append(result.data.attributes["fusion_material"].data[triangle.polygon_index].value)
        for loop_index in triangle.loops:
            loop=result.data.loops[loop_index]
            point=result.data.vertices[loop.vertex_index].co
            normal=attrs[loop_index].vector.normalized()
            tangent=result.data.attributes["fusion_tangent"].data[loop_index].vector
            tangent=(tangent-normal*normal.dot(tangent)).normalized()
            sign=result.data.attributes["fusion_sign"].data[loop_index].value
            uv=result.data.uv_layers.active.data[loop_index].uv
            if normal.length<.9 or not all(math.isfinite(x) for x in (*point,*normal,*uv)):
                raise ValueError("Invalid interpolated corner data")
            if tangent.length<.9 or abs(sign)<.5: raise ValueError("Invalid interpolated tangent")
            positions.extend(point); normals.extend(normal); tangents.extend((*tangent,1 if sign>0 else -1)); uvs.extend(uv); triangles.append(len(triangles))
    report.update(seam_edges=seams,interior_faces=interior,triangles=len(triangles)//3,
                  input_triangles=sum(len(d["triangles"])//3 for d in payload["sources"]),
                  input_volume=sum(r["volume"] for r in reports))
    return dict(version=2,blender=bpy.app.version_string,vertices=positions,normals=normals,tangents=tangents,uv=uvs,triangles=triangles,
                materialIndices=material_indices,materialCount=len(payload["materialIds"]),report=report)


if __name__=="__main__":
    args=sys.argv[sys.argv.index("--")+1:]
    try:
        result=fuse(json.loads(Path(args[0]).read_text(encoding="utf-8-sig")))
        Path(args[1]).write_text(json.dumps(result,separators=(",",":")))
        print(json.dumps(result["report"]))
    except Exception as error:
        Path(args[1]).write_text(json.dumps(dict(error=str(error))))
        raise
