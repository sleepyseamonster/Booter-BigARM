# Blender agent tools

`inspect_blend.py` is a read-only report for mesh objects in a saved Blender file. It reports source and evaluated triangle counts, dimensions, UV presence, transform scale, boundary/non-manifold edges, loose vertices, and modifiers. Boundary edges are often valid for open surfaces; interpret the report against the asset brief.

Run in Blender's background process, from the repo root:

```sh
blender --background /absolute/path/to/Asset.blend --python Tools/Art/Blender/inspect_blend.py -- --output /tmp/asset-mesh-report.json
```

The script does not save or edit the `.blend`. Review its output together with a visual inspection. Ten retained source files were reopened read-only with Blender 5.2.2 LTS before and after relocation, with embedded scripts disabled and no missing unpacked resources. This does not establish rendering or authoring acceptance. Set BLENDER_EXECUTABLE or pass --blender to rebuild wrappers for your local installation.

`inspect_asset_library.py` adds saved-library inventory, transforms, evaluated triangle counts, collections, UV/material assignments, packed/external image dependencies, asset marks and a source fingerprint. Run it through the same hidden background Blender path with `--disable-autoexec` and `--output <report.json>`. It never saves, exports or packs the file; its report cannot include unsaved edits in the artist's open session. The [October 11 library audit](../../../Docs/Engineering/BLENDER_ASSET_LIBRARY_AUDIT_2026-10-11.md) records the seven current rock families and the source/delivery revision mismatch that must precede automatic re-export.
