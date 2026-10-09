# Next western ridge pair

The user authorized two more westward sections after the accepted 2,560-terrain batch. All task artifacts and subprocess temporary files stay on D:. Production Editor PID 13084 is open on the saved, clean Greater Wasteland scene outside Play Mode; preserve its foreground state and unsaved-work boundaries.

| Section | EPSG:26911 bounds in metres | New chunks |
| --- | --- | --- |
| `west_ridge` | `[499920,4006200,504016,4010296]` | 256 |
| `northwest_ridge` | `[499920,4010296,504016,4014392]` | 256 |
| Full target | `[499920,4006200,524496,4014392]` | 3,072 total |

The 96 × 32 grid has 6,016 internal joins and 256 outer neighbor slots. Preserve projected origin `[522448,4008248]`, geographic identities, all 2,560 retained full grids/GUIDs, authored gameplay objects and scene/build routing. Terrain remains fixed and always loaded; generation, streaming/unload/reload, generated-object identities and persisted runtime deltas are not introduced.

1. Acquire four official USGS 1 m products across easting 500000 (`x49y401`, `x49y402`, `x50y401`, `x50y402`), with fresh windows, XML metadata, product catalog and explicit source ownership. Validate bare-earth NAVD88 metre elevations, complete finite grids and strict 2 m decimation. Actual acquisition range is 311.438171–1,634.202637 m, within existing encoding; no clipping is allowed. Record overlap discrepancies rather than silently averaging source products.
2. Acquire aligned NAIP imagery and provenance; preserve raw data, match retained macro-color and constrain only new-side geometry/color at easting 504016.
3. Capture the complete saved 2,560-terrain baseline in isolated `D:/BooterBigArmValidation/TerrainWestRidgePair20261008-01`, validating the accepted FarWest source first. Seal all five terrain roots and pinned package/build files.
4. Build/save/reopen a Blender junction pilot, both complete sections and the 3,072-mesh combined review using Blender 5.2.2 LTS. Verify metre units/origin/transforms, complete borders, upward normals, imagery resources and inspected renders.
5. Export only verified reopened Blender geometry, with exact retained native anchors and 6,016 normalized joins. Build native TerrainData/layers/textures and reusable prefabs in an isolated candidate outside enabled Build Settings.
6. Validate retained hashes/readback, native dimensions, neighbors, all joins and 76,800 direct collider samples. Run focused non-smoke tests, integrate in isolation and preserve all old scene documents except the terrain parent's two appended children.
7. Transfer verified assets and metadata only after checking current production scene, protected bytes and live dirty/Play state. Import/reload and validate through the existing Editor without activation; refresh coverage/map evidence, commit task-owned files on main and preserve unrelated BigARM material/QualitySettings edits. No push or package change is authorized.

All implementation and verification gates completed October 9. See [the implementation receipt](./WEST_RIDGE_PAIR_IMPLEMENTATION_2026-10-08.md) for the 3,072-terrain production result, exact preservation evidence, source/Blender proofs, 96 passing Unity tests, 61 passing offline tests, 6,016 exact joins and 76,800 direct collider samples. The established October 8 batch directory names remain stable.
