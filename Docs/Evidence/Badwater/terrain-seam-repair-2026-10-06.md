# Death Valley terrain seam repair — 2026-10-06

The user's Editor Play screenshot showed open, illuminated cracks in `BadwaterFourSlices.unity`. The previous source-height audit was insufficient: similar full-resolution border heights did not prove that Unity could stitch the rendered LODs.

## Cause and repair

- All 256 saved Terrain objects disabled automatic connectivity. The original isolated builder called `SetNeighbors` in memory, but those references were not serialized in the scene.
- Four tiles had 257 × 257 heightmaps and 252 had 129 × 129 heightmaps. Enabling connectivity alone reproduced Unity's warning: “Top neighbor of the terrain has a different heightmap resolution. Stop neighboring.” Native LOD stitching requires compatible grids.
- All TerrainData now use 257 × 257 render grids over their original 256 m bounds. The 252 context tiles retain their original 2 m source measurements at every other vertex; additional vertices interpolate between those measurements. This does not introduce new surveyed detail. The four focus tiles retain their original 1 m interiors and the already-recorded focus-perimeter interpolation.
- Shared border vertices, including four-way corners, use one deterministic geographic owner before Unity height quantization. Saved neighboring borders now match exactly, rather than merely fitting the earlier 4 cm readback tolerance.
- Every tile has Auto Connect enabled in group 26911. A scene-local `BadwaterTerrainConnectivity` component explicitly restores native LOD links on enable, scene loading, and startup, scoped to the existing terrain parent. Pixel error remains 8 and instanced drawing remains enabled.
- The TerrainCollider owners also receive the existing `TopDown3DGroundSurface` marker so Booter and Legger recognize this terrain as traversable ground. Their gameplay code is unchanged.

TerrainData assets are updated in place. Existing scene objects, tile names, UTM bounds, GUIDs, RAW elevation sources, color images, terrain layers, materials, the production world, packages, and Build Settings retain their identities. The scene remains a separate, always-loaded geographic study. No production generator, streaming manager, or persistence format was introduced; runtime deltas remain outside this terrain.

The initial GUI checks passed with Auto Connect alone. A fresh background project exposed missing native links after scene load: six of nine checks passed and the immediate-validator, reload, and Play-mode connectivity checks failed. This prompted the scene-local connection component. It performs lifecycle-only scans of its own children, without a per-frame scan or a dependency on an active Scene/Game view. The focused tests continue to inspect native neighbors without manually connecting them.

## Verification

The final candidate ran all six seam tests and three explicitly selected scene-playability tests in an isolated copy of the project, using Unity 6000.4.0f1 batchmode with the window hidden. The existing GUI was untouched; no competing batchmode editor used the live project.

**9 passed, 0 failed, 0 skipped** in 17.92 seconds:

- The canonical playable-scene and terrain readback validators accept an immediate scene load and all source-derived vertices.
- Context-grid promotion preserves every source sample and interpolates added vertices.
- Shared-edge reconciliation closes every side and the four-way corner while preserving interiors, regardless of dictionary insertion order.
- Saved-scene inspection verifies all 256 physical tile bounds, collider/data pairings, render resolutions, and original DEM sample values. Maximum measured-source readback error is **0.02746582 m**, within the original quantization tolerance.
- Scene reload and Play-mode checks verify **960 directed neighbor links and 480 shared edges** without manually re-establishing connections in the tests. Every paired saved edge sample is identical.
- The original Death Valley scene passes grounding, simulated keyboard/gamepad movement, camera orbit, inventory, and physical companion motion. The retained diagnostic case and generated-world regression check also pass.

Final test results are retained at ignored `Logs/badwater-isolated-editor-tests.xml` and `Logs/badwater-isolated-editor-tests.log`. The earlier eight-test GUI result remains at `Logs/badwater-seam-editor-tests.xml`. The source-hash audit, isolated terrain-builder verification, and visual review are recorded separately below. These checks do not claim full-suite success, physical-controller acceptance, full-route traversal, or a Player performance result.

The read-only source audit checked all 256 tiles against the retained source hashes and passed (`Logs/badwater-seam-source-audit.json`). A fresh isolated Unity project rebuilt the terrain from those sources, applied the URP material, and ran the retained builder validator. Unity exited successfully with 256 tiles, four focus tiles, a maximum height readback error of **0.0274926424 m**, and a maximum shared border gap of **0 m**. Its logs and report are retained at ignored `Logs/badwater-isolated-builder-verification.log` and `Logs/badwater-isolated-builder-readback.json`.

The repaired scene was also reviewed in Editor Play near the reported focus/context boundary. The illuminated open line was absent in the [captured Play view](./terrain-seam-repair-play-view-2026-10-06.png). This is local visual evidence; the exhaustive edge and neighbor checks establish coverage of the full grid.

## Reproduction and cost

[BadwaterTerrainSeamRepair](../../../Assets/_Project/Scripts/Editor/TopDown3D/BadwaterTerrainSeamRepair.cs) exposes guarded repair and validation commands. Repair refuses Play mode, another scene, or unsaved scene changes; it backs up originals under ignored `Logs/BadwaterSeamRepairBackup` and preserves TerrainData GUIDs.

[BadwaterHeightmapStitching](../../../Assets/_Project/Scripts/Editor/TopDown3D/BadwaterHeightmapStitching.cs) is the shared source-grid/interpolation/border utility. Copy it beside the retained isolated terrain builder when rebuilding. The builder emits compatible grids and Auto Connect flags; the playable assembler requires those settings and installs ground markers and the lifecycle connection component. Existing height readback validation now requires identical shared borders.

[BadwaterTerrainConnectivity](../../../Assets/_Project/Scripts/Runtime/TopDown3D/BadwaterTerrainConnectivity.cs) belongs only to the bounded study scene. The playable assembler and repair command install it on the existing terrain parent; validation requires it and verifies the resulting links. It does not participate in procedural chunk identity, streaming, or saved runtime deltas.

The uniform grids increase serialized TerrainData storage from roughly 34 MB to 60 MB. Unity still adapts rendered LODs; frame rate and runtime memory need separate measurements before a performance claim. The source resolution distinction remains four 1 m DEM tiles and 252 2 m DEM tiles.

The work is limited to the reported seam and terrain-ground wiring defects. Wider Badwater material, streaming, and performance work remains deferred. The continuing target is Editor Play; no executable package is required.
