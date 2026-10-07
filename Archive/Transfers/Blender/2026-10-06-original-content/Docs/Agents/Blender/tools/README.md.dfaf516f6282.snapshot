# Blender agent tools

`inspect_blend.py` is a read-only report for mesh objects in a saved Blender file. It reports source and evaluated triangle counts, dimensions, UV presence, transform scale, boundary/non-manifold edges, loose vertices, and modifiers. Boundary edges are often valid for open surfaces; interpret the report against the asset brief.

Run in Blender's background process, from the repo root:

```sh
blender --background /absolute/path/to/Asset.blend --python Docs/Agents/Blender/tools/inspect_blend.py -- --output /tmp/asset-mesh-report.json
```

The script does not save or edit the `.blend`. Review its output together with a visual inspection. Blender is not currently available on this host, so the script needs a live Blender execution before it can be treated as validated for a particular installed version.
