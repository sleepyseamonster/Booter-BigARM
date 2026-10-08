# Next west and northwest terrain pair

The user authorized continuing two slices farther west through the existing GIS → Blender → native Unity Terrain pipeline on October 8, 2026. This batch is beyond the completed `WestPair2026-10-08`, not a rebuild of those additions. Acquisition, native Blender construction/reopen verification, isolated integration and live production validation now pass. All task artifacts and temporary build files are on D:; see [the implementation receipt](./NEXT_WEST_PAIR_IMPLEMENTATION_2026-10-08.md) for completed evidence.

## Verified starting point

Read-only Unity CLI inspection of the production Editor, PID 21012, returned `Assets/_Project/Scenes/Production/GreaterWasteland.unity`, 1,536 terrain chunks, a clean scene and Play Mode off. The completed pair's source/proof is recorded in [its implementation receipt](./WEST_PAIR_IMPLEMENTATION_2026-10-08.md). The accepted combined Blender review image was inspected; no new Blender build or fresh visual Unity capture is claimed by this audit.

| Section | EPSG:26911 bounds, metres | New chunks |
| --- | --- | --- |
| `west_next` | `[508112, 4006200, 512208, 4010296]` | 256 |
| `northwest_next` | `[508112, 4010296, 512208, 4014392]` | 256 |
| Entire footprint after integration | `[508112, 4006200, 524496, 4014392]` | 2,048 total |

Each addition is 16 × 16 chunks of 256 m. Preserve origin `[522448, 4008248]`, EPSG:26911 geographic keys and all 1,536 existing TerrainData assets, metadata and scene objects. The resulting grid is 64 × 32, covering 16.384 × 8.192 km. It has 4,000 shared edges and 192 outer neighbor slots.

World identity uses the existing fixed projected origin and section-qualified geographic keys. Terrain remains authored and always loaded; chunk streaming and unload/reload are deferred. Stable generated-object identity and persisted runtime deltas are not applicable because this batch introduces neither generated objects nor runtime persistence. Preserve authored gameplay constraints and existing actors/input without redesign.

## Source audit

A fresh official TNM product query returned these four `CA_FEMAR9Southeast_D24` products. Remote raster headers were opened successfully and all report EPSG:26911, one-metre resolution and NoData `-999999`:

- `x50y401`: bounds approximately `[499994, 3999994, 510006, 4010006]`.
- `x50y402`: bounds approximately `[499994, 4009994, 510006, 4020006]`.
- `x51y401`: bounds approximately `[509994, 3999994, 520006, 4010006]`.
- `x51y402`: bounds approximately `[509994, 4009994, 520006, 4020006]`.

Catalog and exact header diagnostics are preserved locally in ignored `Logs/DeathValleyNextWest/catalog.json` and `raster_headers.json`. These prove availability and nominal coverage only. Full finite raster coverage, height range, vertical datum, surface metadata and source hashes are still acquisition gates. The prior west-pair acquisition selects only `x51`; running it unchanged would omit the new footprint west of easting 510000.

Acquire immutable bounded windows and official XML metadata into a fresh versioned directory under `SourceData/Terrain/DeathValley/`. Verify bare-earth NAVD88 metres and complete finite ownership. Use explicit ownership at easting 510000 and northing 4010000; record overlap differences. Prepare a native 1 m union and strict 2 m decimation. Verify the entire new range before retaining the existing −100 to 1,700 m encoding; never clip.

Acquire aligned 2 m RGB imagery from the established USGS NAIP ImageServer, saving service metadata, intersecting imagery catalog, request parameters, response and hashes. Match geographic macro-color using the established source, feather inside the additions and anchor to the accepted `west_outer`/`northwest_outer` west edges. No new imagery acquisition or close-range material acceptance is claimed yet.

## Ordered build and integration gates

1. Restore C: free space and recheck current Git state, Editor ownership and D: headroom. Keep unrelated BigARM material, QualitySettings and prior diagnostic files outside this batch. Preserve the protected C: rollback checkout.
2. Capture the complete saved 1,536-terrain baseline in a fresh isolated D: validation copy. Seal native full-grid readback, GUIDs, asset/metadata hashes, all three terrain asset roots, packages/build routing and production YAML. Retained seams must be exact. Do not save or discard user Editor edits to manufacture a clean baseline.
3. Extend batch tooling with an explicit fresh manifest and new section IDs. Existing fixed tools require adaptation: baseline capture/sealing assumes 1,024 chunks; Blender adapter/exporter and Unity source/builder/validator assume six sections and 1,536 chunks. Preserve accepted source/proof paths and strict hash rejection. Do not relax old validators to accept arbitrary counts.
4. Prepare both new sections against retained native readback, pinning the east joins at easting 512208 only inside new geometry. Keep the established derived precision budget and reject incompatible joins. Build a four-chunk junction pilot first.
5. Use verified Blender 5.2.2 LTS through `run_expansion_blender.py`, hidden/background with auto-execution disabled. Save two editable 256-mesh sources at 2 m interior spacing with full 1 m borders. Build a combined 2,048-mesh review at 4 m interior spacing with complete borders. Reopen sources, verify mesh/source correspondence, exact edges, upward normals, units/origin and packed imagery, then render and inspect overhead, oblique and junction views.
6. Export only reopened verified Blender heights and imagery; anchor retained normalized borders to exact Unity values. Verify all 4,000 normalized shared edges before Unity creation. Keep source, intermediate and runtime destinations distinct and fresh.
7. Create 512 new TerrainData assets, TerrainLayers and two reusable section prefabs in an isolated candidate outside Build Settings. Update batch-aware dispatch in the production validator and developer map without breaking older accepted manifests. Validate all retained readback/hashes, 4,000 seams, reciprocal neighbors, 192 null exterior slots, source/readback error and direct TerrainCollider samples. Run only relevant offline and focused EditMode checks; no gameplay smoke tests.
8. Integrate only the verified new native assets and metadata. Preserve prior production YAML documents except the intended terrain-parent child additions; verify hashes and scene GUID. Refresh coverage/developer map, perform clean live readback and review the resulting scene without foreground activation. Commit only verified task-owned files on `main`; no push is authorized. Record memory readings as authoring evidence, leaving Player performance and traversal acceptance to the user.

## Storage preflight and proof limits

Windows `Get-PSDrive` reported C: at zero bytes free, then only 4,096 bytes on recheck after the user expected about 3 GiB. D: had approximately 80 GiB free. A Codex read-only audit delegate failed to start with OS error 112, "There is not enough space on the disk." No delegate ran. The C: rollback repository is protected by [LOCAL_WORKSPACE.md](../LOCAL_WORKSPACE.md); deleting it is not routine cleanup authority.

Subsequent Windows readback reported approximately 4 GiB free on C:, clearing the Codex session-state failure. Terrain work continued with `TEMP`/`TMP` directed to `D:/BooterBigArmTools/Temp`; no C: rollback cleanup was performed by this task. The original failure is retained as preflight history, not a current build blocker.

Acquisition verified four source products, complete finite 1 m grids and strict 2 m decimation, plus two aligned aerial images. The saved native baseline contains 1,536 terrains, 2,992 exact joins and 17,489 protected files. Blender saved and reopened the pilot, both 256-mesh sections and the 2,048-mesh combined source with zero mesh sample/edge error. The isolated and live native Unity scenes pass 4,000 exact joins and 51,200 collider samples. Existing production documents remain exact except for the terrain parent's two appended children.
