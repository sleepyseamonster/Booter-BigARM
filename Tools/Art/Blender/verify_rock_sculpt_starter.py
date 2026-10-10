"""Read-only reopen, dimension/topology checks and a disposable render of the starter."""
import json
import sys
from pathlib import Path

import bmesh
import bpy

report_path, image_path = (Path(p).resolve() for p in sys.argv[sys.argv.index("--") + 1:][:2])
scene = bpy.context.scene
rock = bpy.data.objects["Rock_Sculpt_01"]
assert scene.unit_settings.system == "METRIC" and scene.unit_settings.scale_length == 1
assert all(abs(float(d) - 1) < 0.00001 for d in bpy.data.objects["Reference - 1 Unity Unit - 1m Cube"].dimensions)
assert all(abs(float(d) - 2.48) < 0.00001 for d in bpy.data.objects["Reference - Booter Height - 2.48m Cube"].dimensions)
assert all(abs(float(d) - 2) < 0.00001 for d in rock.dimensions)
assert tuple(round(float(v), 6) for v in rock.scale) == (1, 1, 1)
assert min(float(v.co.z) for v in rock.data.vertices) == 0
assert rock.mode == "SCULPT"
assert scene.tool_settings.sculpt.brush is not None
assert not any((scene.tool_settings.sculpt.use_symmetry_x, scene.tool_settings.sculpt.use_symmetry_y, scene.tool_settings.sculpt.use_symmetry_z))
bm = bmesh.new()
bm.from_mesh(rock.data)
try:
    non_manifold = sum(not e.is_manifold for e in bm.edges)
    loose = sum(not v.link_edges for v in bm.verts)
finally:
    bm.free()
assert non_manifold == 0 and loose == 0
report = {
    "file": bpy.data.filepath, "blender": bpy.app.version_string,
    "sculpt_mode": rock.mode, "brush": scene.tool_settings.sculpt.brush.name,
    "reference_metres": [1, 2.48], "sculpt_dimensions": list(rock.dimensions),
    "vertices": len(rock.data.vertices), "faces": len(rock.data.polygons),
    "non_manifold_edges": non_manifold, "loose_vertices": loose,
    "reference_selection_protected": all(o.hide_select for o in bpy.data.collections["Scale References - protected"].objects),
    "source_modified_by_verifier": False, "unity_export_or_import": False,
}
report_path.parent.mkdir(parents=True, exist_ok=True)
report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
# Discardable presentation changes only; do not save the source.
for obj in bpy.data.collections["Preview Camera and Lights - not for export"].objects:
    obj.hide_set(False)
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(image_path)
image_path.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.render.render(write_still=True)
print(json.dumps(report, indent=2))
