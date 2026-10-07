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
