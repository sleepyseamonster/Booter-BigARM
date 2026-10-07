"""Repair the user-dissolved face on Formation_00_Buttress_2.

Run in the open badlands study with runpy.run_path. Inspect before saving.
The local patch is rebuilt without remeshing the rest of the rock.
"""

from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


OBJECT_NAME = "Formation_00_Buttress_2"
MARKER = "repaired_dissolved_patch_2026_09_30"
PATCH_CENTER = Vector((0.902, 1.761, 3.862))


def triangle_count(mesh):
    return sum(len(face.vertices) - 2 for face in mesh.polygons)


def apply():
    if Path(bpy.data.filepath).name != "BrokenWorldBadlandsStudy.blend":
        raise RuntimeError("Open the existing badlands study before applying")
    obj = bpy.data.objects.get(OBJECT_NAME)
    if obj is None or obj.type != "MESH":
        raise RuntimeError(f"Missing mesh: {OBJECT_NAME}")
    if obj.get(MARKER):
        print("Dissolved buttress patch already repaired")
        return

    prior_mode = bpy.context.mode
    if prior_mode == "EDIT_MESH":
        bpy.ops.object.mode_set(mode="OBJECT")
    old_mesh = obj.data
    before = triangle_count(old_mesh)
    candidate = old_mesh.copy()
    bm = bmesh.new()
    try:
        bm.from_mesh(candidate)
        patch = [
            face for face in bm.faces
            if len(face.verts) == 16 and (face.calc_center_median() - PATCH_CENTER).length < 0.03
        ]
        if len(patch) != 1:
            raise RuntimeError(f"Expected one dissolved upper patch, found {len(patch)}")
        face = patch[0]
        material_index = face.material_index
        smooth = face.smooth

        # A broad, non-planar n-gon lets the renderer choose unstable internal
        # diagonals. Triangulate along the existing boundary; a center fan
        # creates a visible radial pinch and is intentionally avoided.
        result = bmesh.ops.triangulate(bm, faces=[face], quad_method="BEAUTY", ngon_method="BEAUTY")
        for new_face in result["faces"]:
            new_face.material_index = material_index
            new_face.smooth = smooth

        # Ease isolated pinches around the repaired face, fading to unchanged
        # geometry over 70 cm. Limit total motion so the wedge still reads as
        # a fractured rock instead of a rounded blob.
        affected = [v for v in bm.verts if (v.co - PATCH_CENTER).length < 0.7]
        originals = {v: v.co.copy() for v in affected}
        for _ in range(3):
            proposed = {}
            for vertex in affected:
                distance = (originals[vertex] - PATCH_CENTER).length
                weight = max(0.0, 1.0 - (distance / 0.7) ** 2)
                neighbors = [edge.other_vert(vertex) for edge in vertex.link_edges]
                if len(neighbors) < 3:
                    continue
                average = sum((neighbor.co for neighbor in neighbors), Vector()) / len(neighbors)
                point = vertex.co + (average - vertex.co) * (0.32 * weight)
                movement = point - originals[vertex]
                if movement.length > 0.12:
                    movement.length = 0.12
                    point = originals[vertex] + movement
                proposed[vertex] = point
            for vertex, point in proposed.items():
                vertex.co = point
        bm.normal_update()
        invalid = sum(not edge.is_manifold for edge in bm.edges)
        if invalid:
            raise RuntimeError(f"Repair would leave {invalid} nonmanifold edges")
        bm.to_mesh(candidate)
        candidate.update()
        after = triangle_count(candidate)
        if after > before + 2:
            raise RuntimeError(f"Unexpected triangle growth: {before} -> {after}")
        obj.data = candidate
        obj[MARKER] = True
        print(f"PATCH REPAIRED: {before} -> {after} triangles, {len(obj.data.vertices)} vertices")
    except Exception:
        bpy.data.meshes.remove(candidate)
        raise
    finally:
        bm.free()
        if prior_mode == "EDIT_MESH":
            bpy.ops.object.mode_set(mode="EDIT")


if __name__ == "__main__":
    apply()
