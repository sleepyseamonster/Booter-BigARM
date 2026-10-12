"""Read a saved Blender library without saving, exporting, packing, or changing artist state.

blender --background --disable-autoexec library.blend --python inspect_asset_library.py -- --output report.json
The report describes saved disk state; it cannot inspect unsaved edits in another Blender process.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy


def rounded(values):
    return [round(float(value), 6) for value in values]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    source = Path(bpy.data.filepath)
    if not source.is_file():
        raise RuntimeError("Open a saved library before inspection.")
    destination = Path(args.output)
    if destination.suffix.lower() != ".json" or destination.resolve() == source.resolve():
        raise RuntimeError("Inspection output must be a separate JSON report.")
    source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
    dependency_graph = bpy.context.evaluated_depsgraph_get()
    objects = []
    for obj in sorted(bpy.data.objects, key=lambda value: value.name):
        entry = {
            "name": obj.name, "type": obj.type,
            "collections": sorted(collection.name for collection in obj.users_collection),
            "parent": obj.parent.name if obj.parent else None,
            "location": rounded(obj.location), "rotation": rounded(obj.rotation_euler),
            "scale": rounded(obj.scale), "dimensions": rounded(obj.dimensions),
            "hidden_render": obj.hide_render, "hidden_viewport": obj.hide_viewport,
            "asset_marked": obj.asset_data is not None,
            "custom_properties": {key: repr(obj[key]) for key in obj.keys()},
        }
        if obj.type == "MESH":
            mesh = obj.data
            mesh.calc_loop_triangles()
            evaluated = obj.evaluated_get(dependency_graph)
            result = evaluated.to_mesh()
            try:
                result.calc_loop_triangles()
                entry.update(vertices=len(mesh.vertices), triangles=len(mesh.loop_triangles),
                             evaluated_triangles=len(result.loop_triangles))
            finally:
                evaluated.to_mesh_clear()
            entry.update(
                uv_layers=[{"name": layer.name, "loops": len(layer.data)} for layer in mesh.uv_layers],
                materials=[slot.material.name if slot.material else None for slot in obj.material_slots],
                modifiers=[{"name": modifier.name, "type": modifier.type,
                            "viewport": modifier.show_viewport, "render": modifier.show_render}
                           for modifier in obj.modifiers],
                local_bounds=[rounded(corner) for corner in obj.bound_box],
            )
        objects.append(entry)
    images = []
    for image in sorted(bpy.data.images, key=lambda value: value.name):
        resolved = Path(bpy.path.abspath(image.filepath)) if image.filepath else None
        packed = bool(image.packed_file) or bool(image.packed_files)
        images.append({"name": image.name, "source": image.source, "size": list(image.size),
                       "color_space": image.colorspace_settings.name if image.source != "VIEWER" else None, "packed": packed,
                       "filepath": image.filepath, "resolved_path": str(resolved) if resolved else None,
                       "external_exists": resolved.is_file() if resolved else False,
                       "missing_dependency": image.source == "FILE" and not packed
                           and (resolved is None or not resolved.is_file())})
    materials = []
    for material in sorted(bpy.data.materials, key=lambda value: value.name):
        materials.append({"name": material.name, "uses_nodes": material.use_nodes,
                          "image_nodes": [{"name": node.name, "image": node.image.name if node.image else None}
                                          for node in material.node_tree.nodes if node.type == "TEX_IMAGE"]
                          if material.node_tree else []})
    report = {
        "report_version": 1, "scope": "saved disk state only; no Unity acceptance implied",
        "source": str(source), "source_sha256": source_hash,
        "source_stable_during_inspection": source_hash == hashlib.sha256(source.read_bytes()).hexdigest(),
        "blender_version": bpy.app.version_string,
        "units": {"system": bpy.context.scene.unit_settings.system,
                  "scale_length": bpy.context.scene.unit_settings.scale_length},
        "collections": [{"name": collection.name, "objects": sorted(obj.name for obj in collection.objects)}
                        for collection in sorted(bpy.data.collections, key=lambda value: value.name)],
        "objects": objects, "images": images, "materials": materials,
    }
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"ASSET_LIBRARY_INSPECTED: {len(objects)} objects, {len(images)} images; source not saved")


if __name__ == "__main__":
    main()
