"""Trim invisible buried bases of the active badlands rocks.

The cut for each rock is below the lowest terrain vertex in its footprint plus
a 3 m guard band. This deliberately leaves a burial margin instead of trying
to fit a jagged rock exactly to the terrain surface. Archived source meshes,
terrain, shale plates, scree batches, transforms, and materials are untouched.

Blender --background FILE --python THIS -- --dry-run
Run without --dry-run in the already open study for the inspected application.
"""

import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Vector


STUDY = "BrokenWorldBadlandsStudy.blend"
TERRAIN = "Badlands terrain - study only"
MARKER = "buried_base_trim_2026_09_30"
FOOTPRINT_PAD = 3.0  # more than the terrain grid's widest cell
BURIAL_MARGIN = 0.16
MIN_REMOVABLE_DEPTH = 0.08
ROCK_PREFIXES = ("Formation_", "Stone_", "Talus_")


def terrain_samples():
    terrain = bpy.data.objects[TERRAIN]
    return np.array([tuple(terrain.matrix_world @ vertex.co)
                     for vertex in terrain.data.vertices], dtype=np.float64)


def cutoff_for(rock, samples):
    corners = [rock.matrix_world @ Vector(corner) for corner in rock.bound_box]
    lo_x, hi_x = min(v.x for v in corners), max(v.x for v in corners)
    lo_y, hi_y = min(v.y for v in corners), max(v.y for v in corners)
    footprint = ((samples[:, 0] >= lo_x - FOOTPRINT_PAD)
                 & (samples[:, 0] <= hi_x + FOOTPRINT_PAD)
                 & (samples[:, 1] >= lo_y - FOOTPRINT_PAD)
                 & (samples[:, 1] <= hi_y + FOOTPRINT_PAD))
    if not np.any(footprint):
        return None
    return float(np.min(samples[footprint, 2]) - BURIAL_MARGIN)


def plan():
    samples = terrain_samples()
    result = []
    for rock in bpy.data.objects:
        if rock.type != "MESH" or not rock.name.startswith(ROCK_PREFIXES):
            continue
        if rock.get(MARKER):
            continue
        cutoff = cutoff_for(rock, samples)
        if cutoff is None:
            continue
        world_z = np.array([(rock.matrix_world @ vertex.co).z
                            for vertex in rock.data.vertices], dtype=np.float64)
        if not len(world_z) or world_z.min() >= cutoff - MIN_REMOVABLE_DEPTH:
            continue
        if world_z.max() <= cutoff + BURIAL_MARGIN:
            continue
        # Count only triangles entirely below the safe cut. Crossing faces are
        # retained in shortened form, so this is a conservative saving estimate.
        buried = sum(all(world_z[index] < cutoff for index in polygon.vertices)
                     for polygon in rock.data.polygons)
        if buried:
            result.append((rock, cutoff, buried))
    return result


def trim(rock, cutoff):
    mesh = rock.data
    before = len(mesh.polygons)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    world = rock.matrix_world.copy()
    inverse = world.inverted()
    for vertex in bm.verts:
        vertex.co = world @ vertex.co
    bmesh.ops.bisect_plane(
        bm, geom=list(bm.verts) + list(bm.edges) + list(bm.faces),
        dist=0.00001, plane_co=Vector((0, 0, cutoff)),
        plane_no=Vector((0, 0, 1)), clear_inner=True, clear_outer=False)
    # Only cap fresh plane boundaries; do not fill unrelated pre-existing gaps.
    cut_edges = [edge for edge in bm.edges
                 if edge.is_boundary and all(abs(v.co.z - cutoff) < 0.0001
                                             for v in edge.verts)]
    if cut_edges:
        bmesh.ops.holes_fill(bm, edges=cut_edges)
    for vertex in bm.verts:
        vertex.co = inverse @ vertex.co
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    rock[MARKER] = round(cutoff, 5)
    return before, len(mesh.polygons), len(cut_edges)


def main():
    if Path(bpy.data.filepath).name != STUDY:
        raise RuntimeError(f"Open the existing {STUDY}")
    dry_run = "--dry-run" in sys.argv
    old_mode = bpy.context.mode
    if old_mode not in {"OBJECT", "EDIT_MESH"}:
        raise RuntimeError(f"Unsupported current mode: {old_mode}")
    if old_mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    try:
        changes = plan()
        print("BURIED BASE PLAN", len(changes), "rocks,",
              sum(row[2] for row in changes), "fully buried faces")
        print("BURIED BASE EXAMPLES", [(rock.name, round(z, 3), count)
                                      for rock, z, count in changes[:12]])
        if dry_run:
            return
        old_faces = new_faces = caps = 0
        for rock, cutoff, _ in changes:
            before, after, edge_count = trim(rock, cutoff)
            old_faces += before
            new_faces += after
            caps += edge_count
        print("BURIED BASE RESULT", len(changes), "rocks,",
              old_faces, "->", new_faces, "faces;",
              caps, "cap boundary edges")
    finally:
        if old_mode == "EDIT_MESH":
            bpy.ops.object.mode_set(mode="EDIT")


if __name__ == "__main__":
    main()
