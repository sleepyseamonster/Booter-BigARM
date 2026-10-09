# Northern ridge section implementation

The single northern cap is built through the established GIS Ã¢â€ â€™ Blender Ã¢â€ â€™ native Unity Terrain pipeline. Greater Wasteland contains thirteen sections and 3,328 chunks: the retained 96 Ãƒâ€” 32 rectangle plus a 16 Ãƒâ€” 16 cap immediately north of its westernmost section. The combined bounding box is 24.576 Ãƒâ€” 12.288 km; its 1,280 upper-east cells remain unbuilt. This is a sparse occupied union, not a filled rectangle.

| Section | EPSG:26911 bounds in metres | Chunks |
| --- | --- | --- |
| `north_ridge` | `[499920,4014392,504016,4018488]` | 256 new |
| Retained rectangle | `[499920,4006200,524496,4014392]` | 3,072 retained |
| Combined bounding box | `[499920,4006200,524496,4018488]` | 3,328 occupied total |

Origin `[522448,4008248]` and geographic identities remain fixed. Terrain remains authored and always loaded. Generation, streaming/unload/reload, generated-object identities and persisted runtime deltas are not introduced. Existing gameplay objects, inputs, scene GUID and enabled build routing are preserved by the isolated integration checks.

## Source and Blender evidence

Accepted inputs are under `SourceData/Terrain/DeathValley/NorthRidge2026-10-09/Acquisition02/`. Fresh catalog responses, bounded source windows and XML metadata cover official USGS bare-earth 1 m products `x49y402` and `x50y402`, with EPSG:26911 horizontal coordinates and NAVD88 elevations in metres. Complete finite native grids and strict 2 m decimation pass. Measured heights span 223.246719Ã¢â‚¬â€œ1,527.322754 m, within the retained Ã¢Ë†â€™100 to 1,700 m encoding without clipping.

The recorded ownership split is easting 500000. Wider overlapping coverage differs by up to 1.699951172 m; actual same-coordinate ownership-boundary differences are at most 0.000122070 m. Explicit ownership preserves the measurements without averaging the products. Aligned 2 m NAIP imagery, service/catalog records, request parameters and hashes are retained. Geographic macro-color preparation anchors the cap's southern join to retained terrain. Imagery remains geographic reference rather than final close-range material art.

The baseline seals all 3,072 retained native grids, 6,016 exact existing joins and 45,215 protected file hashes. Maximum new-side height adjustment is 0.027496338 m, below the established 0.042057667 m budget.

Saved sources under `SourceArt/Blender/Studies/DeathValley/NorthRidge2026-10-09/` are `Pilot01.blend`, `north_ridge01.blend` and `combined01.blend`. Blender 5.2.2 LTS built, saved and reopened the pilot, editable 256-mesh northern section and combined 3,328-mesh review. Section interiors use 2 m controls with full 1 m borders; the combined review uses 4 m interiors with full borders. Mesh/source correspondence, exact shared edges, upward normals, metre units, fixed origin, transforms and packed imagery pass. All five pilot, section and combined overhead/oblique renders were inspected before Unity export. Images are retained in [the batch evidence directory](../Evidence/DeathValley/NorthRidge2026-10-09/).

Strict batch entry points are `acquire_north_ridge.py`, `prepare_north_ridge.py`, `build_north_ridge_scene.py` and `export_north_ridge_unity.py` under `Tools/Art/Blender/death_valley/`. The exporter uses reopened Blender proofs and exact retained native border anchors. Source, intermediate, runtime and temporary artifacts stay on D:.

## Unity handoff and preservation

The `BadwaterNorthRidge` source, baseline, builder and validator contracts enforce the exact occupied union and preserve earlier batch contracts. Native TerrainData, TerrainLayers, geographic textures, exports and the reusable section prefab use `Assets/_Project/Art/Terrain/NorthRidge2026-10-09/` within the validated isolated project. The separate candidate remains outside enabled Build Settings.

Candidate validation passes 3,328 terrains, 3,072 retained grids, 6,512 exact joins, reciprocal neighbors, 288 exterior null neighbor slots and 83,200 direct TerrainCollider samples. Maximum native height error is 0.027492642 m; maximum collider error is 0.000030518 m. Focused Unity checks passed 118/118 and offline regressions passed 65/65.

The isolated production integration passed. Scene preservation retains 15,923 unaffected prior documents exactly; only the existing terrain parent receives the intended section child addition. The cap adds 1,282 native scene documents. Geographic-key atlas selection, occupied-tile bounds and developer-map coverage distinguish the built cap from the unbuilt upper-east area.

The preserved production scene was transferred byte-for-byte after a clean live-scene guard and a blocked, editing-disabled reload dialog check. Production scene SHA-256 is `177bd3c7f4445f3523e5c3f1c20e2401bfd56d2bbb97fd5565110dc702ee03a8`; export manifest SHA-256 is `acd72d66e6df180ab142ea9d7635295d47c11908339688e445c2b3fea1a4efba`. Saved-scene native readback, transfer and preservation receipts are in `Docs/Evidence/DeathValley/NorthRidge2026-10-09/`. Final focused Unity tests against the integrated scene and refreshed inventory passed 118/118. Terrain doctor checked 6,671 records with zero issues; refreshed lookup and atlas tests passed 9/9 and 2/2. Metadata pairing and GUID uniqueness pass. Isolated Unity map previews were rendered and inspected in overhead and oblique views, showing the exact northern cap with upper-east space unbuilt. Current results are sealed in `completion_receipt.json`. The live Editor remains open with an external-change Reload dialog; background-only authority prevents activating it, so live reload/readback awaits the user. Saved scene and assets already match the fully verified isolated production bytes.

No Player FPS benchmark or gameplay smoke test was run. Traversal and Player performance remain user-owned review. Package changes, inactive archives and separately owned edits are outside this batch. No push or external publication is authorized.
