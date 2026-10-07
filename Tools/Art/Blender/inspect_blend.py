"""Read-only Blender mesh inventory. Run with blender --background file.blend --python this_file -- [--output path]."""

import argparse
import json
import sys

import bmesh
import bpy


def arguments():
    trailing = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", help="Write JSON to this path; otherwise print to stdout")
    return parser.parse_args(trailing)


def inspect_object(obj, depsgraph):
    mesh = obj.data
    bm = bmesh.new()
    try:
        bm.from_mesh(mesh)
        boundary_edges = sum(1 for edge in bm.edges if edge.is_boundary)
        non_manifold_edges = sum(1 for edge in bm.edges if not edge.is_manifold)
        loose_vertices = sum(1 for vert in bm.verts if not vert.link_edges)
    finally:
        bm.free()

    evaluated_obj = obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_obj.to_mesh()
    try:
        evaluated_mesh.calc_loop_triangles()
        evaluated_triangles = len(evaluated_mesh.loop_triangles)
    finally:
        evaluated_obj.to_mesh_clear()

    mesh.calc_loop_triangles()
    return {
        "name": obj.name,
        "source_triangles": len(mesh.loop_triangles),
        "evaluated_triangles": evaluated_triangles,
        "dimensions": [round(float(value), 6) for value in obj.dimensions],
        "scale": [round(float(value), 6) for value in obj.scale],
        "uv_layers": [layer.name for layer in mesh.uv_layers],
        "material_slots": len(obj.material_slots),
        "modifiers": [modifier.type for modifier in obj.modifiers],
        "boundary_edges": boundary_edges,
        "non_manifold_edges": non_manifold_edges,
        "loose_vertices": loose_vertices,
    }


def main():
    args = arguments()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    report = {
        "blend_file": bpy.data.filepath,
        "blender_version": bpy.app.version_string,
        "mesh_objects": [
            inspect_object(obj, depsgraph)
            for obj in sorted(bpy.data.objects, key=lambda item: item.name)
            if obj.type == "MESH"
        ],
    }
    payload = json.dumps(report, indent=2) + "\n"
    if args.output:
        with open(args.output, "w", encoding="utf-8") as handle:
            handle.write(payload)
    else:
        print(payload)


if __name__ == "__main__":
    main()
