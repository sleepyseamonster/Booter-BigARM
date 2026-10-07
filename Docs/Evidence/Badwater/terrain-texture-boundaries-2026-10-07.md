# Greater Wasteland texture boundaries — 2026-10-07

The user reported persistent visible chunk seams in Greater Wasteland and suspected the texture. Inspection found that every geographic color image uses Repeat wrapping, although each 128 × 128 image represents one unique 256 m tile. Each TerrainLayer maps that image once with a 256 m tile size and zero offset. Bilinear filtering at the tile edge therefore mixes the image's opposite edges instead of sampling the adjoining geographic tile.

All 256 color texture importers now use Clamp on U, V, and W. Only these three serialized importer values change; GUIDs, source images, terrain layers, elevations, scene objects, compression, mipmaps, and filter modes are preserved. This is a bounded correction to geographic image sampling, not a new terrain material system.

## Scope and world contracts

The authored terrain and `rNN_cNN` identities remain fixed. No procedural identity or generated-object identity changes apply because the repair affects only existing texture sampler settings. Greater Wasteland remains always loaded; no streaming or unload/reload mechanism is added. Native asset reimports retain GUIDs. Authored geographic constraints and persisted runtime deltas are unaffected because no heights, objects, gameplay, or save formats change.

## Verification and limits

- All 256 PNG SHA-256 hashes match the retained `Source/tiles.csv` manifest.
- Every importer diff was compared with HEAD: only Repeat-to-Clamp U/V/W changes are allowed. Existing GUIDs and other importer settings are identical.
- A base-level bilinear sampling diagnostic across all 480 shared boundaries gives a mean absolute RGB discontinuity of 12.356/255 with Repeat and 4.468/255 with Clamp, approximately 64% lower. This compares idealized uncompressed source-image edge samples; it is not a Unity render or a guarantee of visual improvement at every boundary.
- Unity 6000.4.0f1 validation uses the existing isolated temporary terrain project, with candidate importer files and hash-matched images, leaving the live editor and foreground application untouched. The transient check verifies all 256 imported textures, importer and texture Clamp states, dimensions, and the existing TerrainLayer texture references, sizes, and offsets. Its log is `Logs/terrain-texture-clamp-import.log`.

Visual acceptance remains pending. Independently generated mipmaps, compression, differences between neighboring source pixels, and terrain lighting may still reveal boundaries. The color imagery remains 2 m per pixel and does not supply close ground detail. This repair does not establish that every visible seam is texture-caused or that geometry and lighting have been freshly validated.

The retained isolated terrain builder loads existing texture importer settings. If source images are recreated in a fresh project without their `.meta` files, explicitly configure geographic images for Clamp before rebuilding; Unity defaults must not silently restore Repeat.

## Follow-up: continuous geographic color map

The user still observed slight color mismatches and corner seams after clamping. Clamp preserves each tile's own outermost pixels, so adjoining tiles still disagree: a four-way corner samples four separate colors. Independent mipmap chains also cannot filter across adjacent geographic tiles.

The follow-up assembles the unchanged 256 source images into `Assets/_Project/Art/Terrain/BadwaterFourSlices/GeographicColor.png`, a 2048 × 2048 RGB image. All existing TerrainLayer assets reference this single texture with a 4096 m tile size. Their offsets are `(column × 256, (15 − row) × 256)` metres, preserving the original north-to-south source-image ordering in Unity's south-to-north UV space. Layer GUIDs, TerrainData references, source PNGs, elevations, scene settings, and gameplay remain unchanged. Only the diffuse texture reference, tile size, and tile offset change in each layer.

Every shared edge and four-way corner now maps to the identical coordinate in the same color texture and mipmap chain. The atlas has the existing Clamp, sRGB, mipmap, bilinear filtering, and compression settings. This supplies continuity without averaging away geographic features or inventing ground detail. The texture extent matches the original 2 m per pixel resolution.

The reproducible [atlas tool](../../../Tools/Art/Blender/death_valley/build_badwater_color_atlas.py) verifies all source hashes before assembling the image and preserves an existing atlas GUID. Run from the repository root:

```powershell
.\.venv\Scripts\python.exe Tools/Art/Blender/death_valley/build_badwater_color_atlas.py
.\.venv\Scripts\python.exe Tools/Art/Blender/death_valley/build_badwater_color_atlas.py --check
```

The read-only check verifies that every atlas pixel matches its original geographic tile, all 256 layer mappings are correct, all 480 shared edges agree, and all 225 four-way corners have one shared UV. Those UV equalities apply to every level of the shared mipmap chain. After any isolated terrain rebuild, run this tool again to restore the continuous mapping; the retained terrain builder alone still creates individual-image layers.

Fresh Windows proof used the current Assets, Packages, and ProjectSettings copied into the existing isolated Editor verification project, without taking control of the live Editor. Unity 6000.4.0f1 exited 0 and verified all 256 native layer references and offsets, the imported 2048² texture, and its 12 mip levels. Both canonical scene/terrain validators passed: 960 directed native neighbor links, 480 exactly matching height edges, maximum source-height readback error 0.0275 m, and zero border gap. A flat four-terrain diagnostic rendered a continuous synthetic gradient through native URP Terrain Lit, verifying the positive offset sign and north/south orientation. Its maximum RGB step across the shared edges and central four-way corner was 1/255, within the 8-bit render readback's quantization. The diagnostic capture is not a visual acceptance of the actual gameplay terrain.

The log is `Logs/terrain-atlas-validation.log`; the diagnostic image is retained in the temporary verification project's `Logs/terrain-atlas-native-mapping.png`. The reused isolated project emitted a startup assembly-scan exception for a missing Collections package test-support DLL, but compilation, explicit validators, diagnostic render, and process exit succeeded. No full test-suite or package-health claim is made, and no package versions were changed.

The scene retains its 3000 m native basemap transition. Unity's independently baked distant basemaps and terrain lighting remain separate potential causes of visible boundaries; neither a shared color map nor identical heights proves matching surface normals or user-accepted appearance. This change makes no Player performance claim and does not activate streaming or alter persisted runtime deltas.
