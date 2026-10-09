# Next northern row: complete repository handoff

This handoff is for a new agent continuing the Death Valley GIS → Blender → native Unity Terrain pipeline. It records the accepted saved-world checkpoint and the next row's proposed geometry. It does not start acquisition, authoring, Unity integration, cleanup or publication.

## 1. Resolve the requested number of sections before implementation

The user described the next work as both “next five” and a full next row to the north. Those scopes differ. The current terrain spans six 4,096 m columns, so a full row requires **six new sections**, not five. A clarification is pending. Six sections is the recommended interpretation of “full row”; five would leave one 4,096 × 4,096 m northern section unbuilt. Do not silently select either scope or start the new build before this is resolved.

The accepted world is a fully occupied **96 × 48 chunk grid**, with 4,608 chunks and eighteen sections. Its EPSG:26911 bounds are `[499920,4006200,524496,4018488]`, measuring 24.576 × 12.288 km. The next northern strip is `[499920,4018488,524496,4022584]`, measuring 24.576 × 4.096 km.

If six sections are confirmed, build west to east, finishing and committing each section before starting the next. Stop at easting 524496 and northing 4022584. If five are confirmed, obtain the omitted section or exact partial footprint; a west-to-east five-section interpretation would omit the easternmost section and would not constitute a full row.

Candidate IDs below are **proposals**, not accepted manifests. Keep the new row separate from the sealed five-step `north_row` implementation. Use fresh execution dates and unique versioned batch owners after the scope is confirmed.

| Order | Proposed section ID | EPSG:26911 bounds `[xmin,ymin,xmax,ymax]` | South anchor | Retained chunks | Total chunks | Exact joins | Exterior slots | Collider samples |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1 | `north_row2_e499920_n4018488` | `[499920,4018488,504016,4022584]` | `north_ridge` | 4,608 | 4,864 | 9,568 | 320 | 121,600 |
| 2 | `north_row2_e504016_n4018488` | `[504016,4018488,508112,4022584]` | `north_row_e504016` | 4,864 | 5,120 | 10,080 | 320 | 128,000 |
| 3 | `north_row2_e508112_n4018488` | `[508112,4018488,512208,4022584]` | `north_row_e508112` | 5,120 | 5,376 | 10,592 | 320 | 134,400 |
| 4 | `north_row2_e512208_n4018488` | `[512208,4018488,516304,4022584]` | `north_row_e512208` | 5,376 | 5,632 | 11,104 | 320 | 140,800 |
| 5 | `north_row2_e516304_n4018488` | `[516304,4018488,520400,4022584]` | `north_row_e516304` | 5,632 | 5,888 | 11,616 | 320 | 147,200 |
| 6 | `north_row2_e520400_n4018488` | `[520400,4018488,524496,4022584]` | `north_row_e520400` | 5,888 | 6,144 | 12,128 | 320 | 153,600 |

Order 1 has a south anchor only; there is no terrain west of the new section. Orders 2–6 also have the preceding new section as their west anchor. Every section has 16 × 16 chunks, each 256 × 256 m, and adds 256 chunks.

The first section adds 480 internal joins and 16 south joins: **496**, not 512. Later sections add another 16 west joins, giving 512 each. Exterior null-neighbor slots increase from 288 to 320 at the first section and remain 320 thereafter. Collider sampling remains 25 direct samples per terrain.

At accepted step k, occupied cells are the original 96 × 48 rectangle plus rows 48–63 in columns 0–`16*k-1`. The AABB becomes `[499920,4006200,524496,4022584]` at the first addition but is not fully occupied until step 6. Five west-to-east sections leave 256 upper-east cells unbuilt. All six give 96 × 64 chunks, 6,144 terrains, twenty-four sections and a 24.576 × 16.384 km rectangle.

## 2. Accepted repository checkpoint

- Checkout: `D:/Arc & Dust/Project`; all continuing project writes belong on D:.
- Branch: `main`, tracking `origin/main`; no branch/worktree operation is authorized by this handoff.
- HEAD before this handoff: `a1b6dd0dd754ce6a975ba7981b093079dc6b0dcc`.
- The branch was 43 commits ahead of upstream before this handoff. This is a local checkpoint, not permission to push.
- At the completed-build checkpoint the index was empty and only the material/QualitySettings edits were dirty. A later handoff-time audit found additional work in progress; preserve every path listed below and confirm fresh status.
- Preserve earlier untracked diagnostic files. Their presence does not authorize overwriting, deleting or committing them.
- These facts are snapshots: inspect current status, HEAD and process ownership before acting.

The previous five northern-row section commits are `5ad6639e`, `9f5a603a3`, `083e92f4c`, `65223c554` and `076117a95`. Completed Blender cleanup is recorded by `60900b01f`; the cleanup evidence digest/checkpoint is `a1b6dd0dd`. Retain these historical accepted batches and their proofs.

| Production contract | Accepted value |
| --- | --- |
| Scene | `Assets/_Project/Scenes/Production/GreaterWasteland.unity` |
| Scene GUID | `4de5dd018ee194314a00fd369e2d3eeb` |
| Shared terrain parent file ID | `1259644692` |
| Fixed projected Unity origin | `[522448,4008248]` |
| Scene SHA-256 | `52ffe7c65416f2e9d027c61481f0d841cc767382be28c18f8ca95f9e6751ca1b` |
| Latest runtime manifest SHA-256 | `f23132a0fd1724fa0f806252960af3b0630c77af8e73104a015b7808601a20eb` |
| Latest native owner | `Assets/_Project/Art/Terrain/NorthRowE5204002026-10-09/` |
| Saved native proof | 4,608 terrains, 9,072 exact joins, 288 exterior slots, 115,200 collider samples |
| Candidate/final focused Unity checks | 29 passed / 57 passed |
| Maximum native height error | approximately 0.0274926424 m |
| Maximum collider error | 0.00006103515625 m |

At the committed checkpoint, saved production bytes and transferred assets matched the fully validated isolated scene. The working scene has since changed; the checkpoint proof does not validate those newer bytes. Do not upgrade these facts into Player performance or hands-on traversal acceptance. No gameplay smoke test or FPS benchmark was run.

## 3. Live Editor state requires a fresh read

The known root Unity process is PID **13084**, with the CLI service on port **7800**. A fresh read during handoff preparation now reports the production scene open with **4,608 terrains**, clean and outside Play Mode. The earlier pending Reload was resolved after the build closeout. This live count/clean-state read does not provide a new full native source/collider audit.

The working scene SHA-256 observed during handoff is `ffe0254b8279c64a65356650b250088bd0bbb0caba3d2cd8eece3b4499e7bcc2`, differing from the accepted checkpoint above. The diff includes terrain reparenting, and three retained TerrainData assets plus seam-repair tooling are modified. These newer changes are outside this handoff task and may be active work by the user or another agent. Do not restore the checkpoint over them, commit them with this document, or assert that the older protected-file seal still matches.

Tracked modifications observed:

- `Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07/TerrainData/north/r10_c01.asset`
- `Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07/TerrainData/north/r11_c02.asset`
- `Assets/_Project/Art/Terrain/WestPair2026-10-08/TerrainData/west_outer/r01_c10.asset`
- `Assets/_Project/Materials/TopDown3D/Greybox_BigARM.mat`
- `Assets/_Project/Scenes/Production/GreaterWasteland.unity`
- `Assets/_Project/Scripts/Editor/TopDown3D/BadwaterTerrainSeamRepair.cs`
- `Docs/Engineering/UNITY_AUTOMATION.md`
- `ProjectSettings/QualitySettings.asset`

**Baseline gate:** wait for or coordinate acceptance of this work, then establish a fresh native saved-scene/protected-file baseline and current hierarchy contract before northern-row integration. In particular, old parent ID `1259644692` and the exact byte-preservation recipe must be checked against the accepted new hierarchy; do not force the old layout or treat all reparenting as an authorized next-row change. Preserving the old sealed manifests is still required. If accepted repairs change their protected payloads, explicitly version the new baseline/protection contract rather than bypassing old hash guards or rewriting historical evidence.

The user previously authorized bringing Unity forward for Reload; automation had failed at that time. Ordinary work remains background-only. Use current live reads and applicable user authority for any future UI action.

Do not ask the user to focus Unity merely to continue repository work. Prefer CLI reads and isolated saved-scene validation. If interactive Reload remains necessary, provide the precise handoff and distinguish it from the validated saved scene. Do not save or discard user Editor changes to manufacture a clean baseline.

Never launch a second Unity Editor against the locked production checkout. Use a fresh isolated validation copy and its own logs/cache. Reuse the root connection only after explicitly identifying the D: project and its current scene.

## 4. Load these governing documents

Read root [AGENTS.md](../../AGENTS.md) and [LOCAL_WORKSPACE.md](../LOCAL_WORKSPACE.md) first. The protected C: rollback checkout remains intact; the D: checkout is the production workspace. Old Mac/C: paths in receipts are historical evidence, not current destinations.

Read [REPOSITORY_ORGANIZATION.md](../REPOSITORY_ORGANIZATION.md) for placement and duplicate handling, and [UNITY_AUTOMATION.md](../Engineering/UNITY_AUTOMATION.md) for CLI/batch control. Read [AGENT_AND_UNITY_PRACTICES.md](../Operations/AGENT_AND_UNITY_PRACTICES.md) and [GIT_BATCHING_STANDARD.md](../Operations/GIT_BATCHING_STANDARD.md) before integration and closeout.

Role ownership remains unchanged:

- [Gottspan](../Agents/Gottspan/README.md): scope, integration order, delegated boundaries, proof and documentation.
- [Babineaux](../Agents/Babineaux/README.md): Unity bridge, imports, baseline capture, scene validation and live-state claims.
- [Blender](../Agents/Blender/README.md): native authoring/reopening and source handoff; follow [MODEL_TO_UNITY.md](../Agents/Blender/sops/MODEL_TO_UNITY.md).
- [Gear Ball](../Agents/GearBall/README.md): task-owned staging and verified local commits; external writes require current authority.
- Lorekeeper remains responsible for setting-dependent work; this geographic task introduces no narrative canon.

Historical execution and accepted evidence:

- [Completed northern-row plan](./NORTH_ROW_EASTWARD_PLAN_2026-10-09.md): the previous five eastward additions, not the next row's execution contract.
- [Last section receipt](./NORTH_ROW_E520400_IMPLEMENTATION_2026-10-09.md) and [batch evidence](../Evidence/DeathValley/NorthRowE5204002026-10-09/).
- [Northern-cap receipt](./NORTH_RIDGE_IMPLEMENTATION_2026-10-09.md) and [terrain slice workflow](./TERRAIN_SLICE_WORKFLOW.md).
- [Blender cleanup summary](./BLENDER_TERRAIN_CLEANUP_2026-10-09.md), [cleanup receipt](../Evidence/DeathValley/BLENDER_TERRAIN_CLEANUP_2026-10-09.json) and [reviewed pre-cleanup inventory](../Evidence/DeathValley/BLENDER_TERRAIN_PRE_CLEANUP_2026-10-09.json).
- [Current catalog](./coverage_catalog.json), [atlas](./coverage_atlas.html) and [tile CSV](./unity_tiles.csv).

## 5. Retired native Blender files must remain retired

The user authorized deleting all completed Death Valley terrain `.blend` files after native Unity proof. Cleanup removed **46 files totaling 5,571,537,330 bytes**, while preserving fourteen unintegrated studies/recovered references. Thirty-nine removed files remain in Git/LFS history; seven untracked versions were retired without that history. Do not imply all 46 can be recovered from Git.

Measured GIS windows and metadata, prepared grids and imagery, JSON/NPZ Blender proof exports, runtime source exports, TerrainData, TerrainLayers and scene bytes are retained. Cleanup did not modify those production inputs or the accepted gameplay terrain. Historical native authoring proofs remain immutable even though their source files are absent.

[blender_retirement.py](./blender_retirement.py) requires a completed cleanup receipt, a reviewed pre-cleanup identity snapshot and sealed historical native production proof. It distinguishes `historical_sha256` from `current_sha256=None`. The catalog's `retired_blender_files` are intentionally absent sources, not current-byte hash successes. Unlisted missing files, changed evidence and reappearing retired files fail validation.

**Do not restore or author at a retired path.** Reappearing `BadwaterGameSlice.blend` or any retired completed-batch source would conflict with that ledger. New authoring belongs in a fresh versioned next-row folder. Historical exporters/reopen verifiers still require their exact native files; they must not receive missing-file bypasses or alternate accepted hashes.

Rebuild the new section from retained measured/prepared inputs where appropriate and a fresh native Unity baseline. Create new `.blend` sources and new reopen proofs. Do not modify an old proof to point at a new file or silently promote reconstructed sources to accepted old bytes.

The current retirement ledger describes one exact 46-file cleanup. If the user later authorizes cleanup of new completed batches, create versioned evidence and extend ledger support deliberately. Do not overwrite the existing receipt or snapshot, and do not automatically delete a new `.blend` merely because Unity import passed.

## 6. Existing tools are references, not executable next-row commands

Tracked Python terrain tooling under `Tools/Art/Blender/death_valley/`:

- `north_row_contract.py`: five sealed steps, accepted south/west anchors, source ownership and color policy.
- `acquire_north_row.py`: official DEM/imagery acquisition for those old footprints.
- `prepare_north_row.py`: old-row two-border preparation and precision checks.
- `build_north_row_scene.py`: pilot, section, combined review and native reopen verification.
- `export_north_row_unity.py`: guarded Blender proof export to native terrain payloads.
- `run_expansion_blender.py`: hidden Blender launcher; its current `--step` choices cover only the old five-step contract.
- `tests/test_north_row_contract.py`: existing geography, anchor and RGB corner regression fixtures.

Tracked inventory tooling includes [audit_coverage.py](./audit_coverage.py), [north_row_coverage.py](./north_row_coverage.py), [terrain_tools.py](./terrain_tools.py), [test_north_row_coverage.py](./test_north_row_coverage.py) and [test_blender_retirement.py](./test_blender_retirement.py). Preserve historical dispatch and its fresh-proof rejection.

Tracked Unity integration is under `Assets/_Project/Scripts/Editor/TopDown3D/`: `BadwaterNorthRowSource.cs`, `BadwaterNorthRowBuilder.cs`, `BadwaterPlayableSceneBuilder.cs`, `DeathValleyMapData.cs` and `DeathValleyMapWindow.cs`. Relevant tests include `Assets/_Project/Tests/Editor/BadwaterNorthRowTests.cs` and `DeathValleyExpandedMapTests.cs`.

The source/builder classes and dispatch enforce old-row coordinates and counts. A new CLI argument or repointed directory does not make them valid for the next row. Build a distinct approved-row contract or bounded new adapters; do not extend the old sealed five-step constants in place.

Local ignored helpers exist under `Logs/DeathValleyNorthRow/`: `seed.py`, `seed_step1.py`, `run-unity.ps1`, `run-blender.ps1`, `seal_production_proof.py`, `transfer.py`, `finish_receipt.py`, `stage.py`, `verify_commit.py`, `metadata_audit.py`, `audit_sources.py`, `update_docs.py`, `final_plan.py`, `cleanup_blends.py` and `patch_retirement_inventory.py`.

Those helpers are local, untracked execution records with old-row paths, counts and assumptions. They are not portable new-row automation. Inspect and create new scoped helpers before reuse. In particular, never rerun the old cleanup helper, staging list, transfer script or final documentation writer against a new row.

## 7. Runtime and placement preflight

Verify the pinned Unity version **6000.4.0f1**, normally under `%LOCALAPPDATA%/Unity/Editors/6000.4.0f1/Editor/Unity.exe`. Unity CLI is installed under `%LOCALAPPDATA%/Unity/bin/unity.exe`. Derive actual paths from current installation rather than assuming an old retained path runs.

Known working Blender: `D:/BooterBigArmTools/Blender522/blender-5.2.2-windows-x64/blender.exe`, version **5.2.2 LTS**. GIS Python is the repository `.venv/Scripts/python.exe`. Preserve dependency/package pins; setup does not authorize upgrades.

Set subprocess `TEMP` and `TMP` to `D:/BooterBigArmTools/Temp`. Keep isolated validation projects under `D:/BooterBigArmValidation/`, transfer receipts under `D:/Arc & Dust/Transfers/`, and new ignored diagnostic logs under a fresh D: task directory.

Use these read-only structural preflight commands from the checkout root; they do not execute next-row implementation:

```powershell
git status --short --branch
git rev-parse HEAD
git diff --cached --name-only
git rev-list --count origin/main..HEAD
Get-PSDrive -Name C,D
& .\Tools\Repository\Test-LocalWorkspace.ps1
& "$env:LOCALAPPDATA\Unity\bin\unity.exe" editors running --json
```

Before each section, estimate peak D: usage for source windows, grids, imagery, new native sources, combined review, runtime assets, full baseline readback, an isolated import cache, evidence and local Git/LFS storage. Keep a working margin. Do not infer capacity from the retired `.blend` size alone; retained LFS objects and copied validation caches still occupy space.

Create a fresh isolated seed from the accepted checkpoint, including all required native payloads and metadata. Avoid copying unrelated dirty files into the accepted baseline. Record precisely which bytes came from HEAD and which task-owned files are included. This is an isolated project copy, not an authorized branch/worktree operation.

## 8. Capture a complete native baseline for each accepted predecessor

For section 1, baseline all 4,608 retained native terrains and all 9,072 joins. For each later section, capture the immediately preceding accepted scene; never reuse the original 4,608-terrain baseline after new integration.

Seal the complete twelve accepted terrain asset roots before section 1: `BadwaterFourSlices`, `WestNorthNorthwest2026-10-07`, `WestPair2026-10-08`, `NextWestPair2026-10-08`, `FarWestPair2026-10-08`, `WestRidgePair2026-10-08`, `NorthRidge2026-10-09`, and the five `NorthRowE{504016,508112,512208,516304,520400}2026-10-09` roots under `Assets/_Project/Art/Terrain/`.

For each later step, add every accepted next-row owner to this exact inventory. Seal TerrainData, metadata, layers, textures, source bytes, GUIDs, scene documents, package pins and enabled build routing. Reject incomplete protection receipts instead of treating absent owners as optional.

Capture full native 257 × 257 normalized height readback and exact retained geographic keys/positions. Verify all existing joins and native collision before using them as preparation anchors. Keep identity `[EPSG:26911, lower-left easting/northing, size256, source version]` distinct from section-local `rNN_cNN` addresses.

## 9. Acquire and audit both source axes

After the scope is confirmed, perform **early source reconnaissance across every approved next-row footprint before expensive authoring**. Check complete DEM coverage, height extrema, metadata and ownership for the whole strip, especially the eastern mountains, which may exceed 1,700 m. Bounded source acquisition/audit is distinct from terrain integration; it does not skip fresh per-section baselines or the ordered Blender/Unity gates. If any approved footprint exceeds the encoding, stop for the user's range/design decision before authoring rather than silently clamping or globally rescaling retained terrain. No next-row acquisition was performed for this handoff, and no new source-coverage claim is made here.

Query official USGS products for each exact new section; catalog intersection alone does not prove full coverage. Preserve query responses, bounded 1 m DEM windows, XML metadata, EPSG:26911 CRS, bare-earth surface classification, NAVD88 metres, sample orientation and every source hash.

The next row crosses **northing 4020000** within every section and may cross product easting ownership boundaries. Audit both axes, not just the previous row's easting split. Record deterministic multidimensional ownership where north/south and east/west products overlap. Measure wide overlaps and actual same-coordinate ownership boundary differences before choosing owners.

Prove complete finite **4,097 × 4,097 native 1 m vertex samples** for each section, then strict decimation to **2,049 × 2,049** controls. Do not replace strict decimation with coarse filtered reprojection. Do not interpolate across NoData or hide incomplete ownership with arbitrary averaging.

Reuse retained source windows only after proving exact coverage, metadata, hashes and sample correspondence. Preserve failed acquisition directories as diagnostics; accepted input belongs in a fresh versioned source owner. New source products and height ranges require fresh checks.

Acquire aligned **2,048 × 2,048 RGB imagery at 2 m spacing** from the established USGS NAIP service. Preserve service metadata, intersecting image catalog, request parameters, response bytes, alignment and hashes. Reject NoData or missing image coverage. Aerial imagery remains geographic reference, not final close-range material approval.

Fresh heights may exceed the accepted **−100 to 1,700 m** encoding. Check the complete measured range before preparation/export. Never clamp, change normalization silently or broaden a validator to conceal overflow. Stop for an explicit supported range/design decision if needed.

## 10. Prepare the correct anchors and new-side changes

Section 1 pins its south edge only. It must not invent a west terrain anchor or import the old row's two-anchor requirement. Later sections pin the south edge and the predecessor's east edge; their southwest geometric sample must agree exactly.

Use accepted runtime image crops as authoritative retained color anchors. Check exact **256 × 256 runtime crops** against the prepared full-section images before use. Historical source selection matters: the original expansion uses accepted `Prepared03`; the earlier `WestPair` uses accepted `Prepared02`, not arbitrary earlier folders.

The previous row's explicit southwest color policy was south-row priority at one raster-cell-center pixel, preserving the west column everywhere else and the south row everywhere. Re-audit the new row's measured corner values. Do **not** copy the old final `[16,16,16]` allowance into a new row or make it a general seam tolerance.

For section 1 no south/west color conflict exists; use only the available south anchor. For later sections bound any sole southwest exception by the freshly measured retained RGB disagreement. Preserve every old pixel and feather only inside the new section using the established 128-pixel/256 m strips. Reject a predecessor whose southeast corner no longer matches its accepted south anchor.

Maintain full native 1 m borders around the 2 m interior control mesh. Each runtime render grid is 257 × 257; each 2 m source grid is 129 × 129. Full-precision measured input remains separate from new-side authored joins and encoded exports.

Maximum new-side height adjustment remains **0.0420576669 m**, from `1.5*1800/65535 + 4*float32_epsilon*1800`. Native height readback tolerance remains `1800/65532 + 0.00025` m. Collider tolerance remains `8*float32_epsilon*5000` m. Report measured errors separately from these unchanged allowed bounds.

## 11. Blender build, reopen and visual export gate

Build a junction pilot before the complete section. The previous eight-mesh pilot tested two borders; design a bounded **one-border pilot for the first new section**, then two-border/corner pilots for the later sections. Do not pretend the old eight-mesh arrangement validates a nonexistent west join.

Use Blender 5.2.2 LTS in hidden/background mode with auto-execution disabled. Configure metre units, fixed origin, correct source-to-Blender axis mapping, stable mesh names and packed geographic imagery. The editable section has 256 meshes, 2 m interiors and full 1 m borders.

Save and reopen the pilot and section sources. Verify complete mesh/source correspondence, exact border samples, upward normals, transforms, units, positions, geographic identity and packed resources. Advisory mesh inspection alone does not accept visible quality.

Build and reopen a combined review of the **entire exact current occupied footprint** after each section, using 4 m review interiors and full borders. It must include every retained owner and no premature upper-east cells. Inspect five views: pilot junction, section overhead/oblique, combined overhead/oblique.

Keep the Blender-side hash/contract path compatible with its standard-library environment. Do not import `rasterio` through the GIS contract inside Blender; a prior attempted build failed that way. Perform GIS preparation/export in the pinned repository `.venv`, with explicit handoff manifests to native Blender scripts.

Export only after native reopening and view inspection pass. Record the new source file hashes, Blender version, mesh counts, spacing, source correspondence, inspected image paths and export dependencies. All sources and proofs belong to fresh next-row owners.

## 12. Guarded native export and candidate validation

Export north-first little-endian float32 **normalized 257-square** render payloads and north-first little-endian uint16 **129-square** 2 m source grids. Normalization uses minimum −100 m and span 1,800 m. Keep the Unity reader's row reversal into north-positive Z unchanged.

After metre-space Blender proof passes, pin the new normalized anchor borders directly to retained native normalized samples. This avoids conversion drift and must not modify retained payloads. Verify every expected new/retained seam before generating native assets.

Create new TerrainData, layers, textures, source resources, materials and one reusable section prefab under the fresh next-row runtime owner. Keep its candidate scene outside enabled Build Settings. Unity must create/import `.meta` files natively; never invent asset GUIDs or manufacture production proof metadata.

Extend newest-batch dispatch together across Python coverage, native source/validator, playable-scene checks and the Unity developer map. Keep older accepted manifests/proofs immutable. The latest fresh scene/manifest proof validates the current world; stale historical scene hashes are source history, not current validation.

Add focused negative cases for holes, duplicates, shifted cells, absent predecessors, wrong south/west anchors, unexpected western terrain at first step, premature upper-east fill, missing owners, changed GUID/hash/native source and invalid border/corner policy. Check the first step's **320 exterior slots and 496 added joins** explicitly.

Validate full retained grids/GUIDs/hashes, reciprocal neighbors, one connectivity owner, exact occupied union, all shared edges, direct collider samples and unchanged precision gates. Save, reopen and validate the candidate; then integrate in isolation and validate the saved production scene independently.

Integration must preserve every prior scene document except the common terrain parent's one appended section child. Expect **1,282 new scene documents per section**. Preserve scene GUID, existing actors, input, camera, UI, atmosphere, colliders and sole enabled build routing.

## 13. Transfer, close out and advance once

Transfer only verified task-owned assets, metadata, proof resources and the preserved scene. Guard the root scene's saved hash, clean state and Play Mode before transfer. Preserve all separately owned dirty bytes. Do not resolve a live reload dialog by silently saving or discarding Editor changes.

Reuse the existing root Editor for import/reload only with current authority and explicit project targeting. Saved-scene proof and live loaded-state proof are separate. If Reload stalls, report the exact state rather than repeating activation attempts indefinitely or claiming the final count is loaded.

Refresh the catalog, tile CSV, atlas and developer map only after current native production proof passes. Verify unique geographic selection keys and exact occupied cells; framing bounds must not fill an unbuilt AABB. Render map views through hidden GL/editor automation without foreground activation.

Check native asset/metadata pairing, global GUID uniqueness, retained bytes, coverage lookup/atlas, source ownership and terrain doctor. Inspect actual map previews. Seal section source ranges, boundary audits, adjustment/errors, colors/corner exception, Blender evidence, scene/manifest hashes, preservation, transfer, tests and live-state limits in the receipt.

Stage only that section's approved task-owned files. Review the staged diff and whitespace, then create a verified local commit on `main`. A commit does not authorize push, PR, release or a branch/worktree change. Close the section before starting the next baseline.

## 14. Keep validation relevant and bounded

The user was frustrated by prolonged testing. Run the directly relevant structural/source and focused EditMode checks once per meaningful gate. Repeat only for changed bytes, failures or a genuinely unresolved issue; do not rerun broad successful suites for reassurance.

Existing row candidate test timeout is **1,800,000 ms**; the canonical full-audit timeout is **900,000 ms**, with assertions unchanged. Earlier final checks initially produced 56 passes and one default 180-second timeout; correcting the timeout yielded 57 passes. A timeout does not justify weakening preservation or source assertions.

Short CLI response timeouts can expire while Unity's operation continues. Before rerunning, inspect process state, fresh logs and the operation's output/report timestamps. Do not kill the root Editor or start duplicate operations solely because the bridge stopped waiting.

Do not create or run gameplay smoke tests. Hands-on traversal and Player FPS/memory acceptance remain user-owned. Authoring-memory readings, native terrain checks and map renders do not prove Player performance of the larger always-loaded world.

## 15. Stop gates and questions for the receiving agent

- Resolve **five partial sections versus six full-row sections** before new implementation. If partial, seal the exact omitted footprint.
- Recheck branch, HEAD, user-owned dirty files, current D: capacity, Unity lock/process ownership and live Reload state.
- Stop on incomplete DEM/NAIP coverage, ambiguous metadata/ownership, NoData, out-of-range heights, incompatible retained anchors or an RGB conflict outside the freshly audited corner policy.
- Stop on missing protected owners, source/GUID/hash changes, holes, duplicates, shifted coverage, failed seams/collision, incorrect exterior slots or unsafe live scene state.
- Preserve successful prior sections and their source history when a later step fails; do not integrate around a failed predecessor.
- Keep all writes on D: and the C: rollback intact. Do not alter `Library`, `Temp`, `Logs` or `UserSettings` as production content.
- Add no generation, streaming, procedural save integration, gameplay redesign, package upgrade, project-setting change or external publication.
- Perform no next-row cleanup without explicit authorization and versioned retirement ledger support.

The next agent's first useful output is a scope-confirmed next-row contract and source/baseline plan derived from this checkpoint. Until scope is resolved, read-only orientation and capacity/source availability inspection may proceed; acquisition, authoring and production integration remain pending.

## 16. Concrete file and manifest handoff for the new implementation

After confirmation, choose a fresh `<batch>` for each section, for example `NorthRow2E499920N4018488YYYY-MM-DD`. The execution date is not automatically October 9. Put immutable acquisition versions, baseline versions and preparation versions under that owner rather than reusing an accepted earlier directory.

| Artifact | New repository-relative destination |
| --- | --- |
| DEM windows, XML, catalogs, imagery responses | `SourceData/Terrain/DeathValley/<batch>/Acquisition01/` |
| Full native readback and protected inventory | `SourceData/Terrain/DeathValley/<batch>/Baseline01/` |
| Prepared grids/images and Blender JSON/NPZ proof | `SourceData/Terrain/DeathValley/<batch>/Prepared01/` |
| Editable native sources and source README | `SourceArt/Blender/Studies/DeathValley/<batch>/` |
| Unity payloads, textures, TerrainData, layers, prefab | `Assets/_Project/Art/Terrain/<batch>/` |
| Disabled candidate | `Assets/_Project/Scenes/Reference/<new-candidate>.unity` |
| Sealed accepted evidence | `Docs/Evidence/DeathValley/<batch>/` |
| Scope, section receipt and integration explanation | `Docs/DeathValley/` |

Use acquisition/preparation suffixes `02`, `03`, etc. when a failed or replaced attempt is retained. Record why the accepted version differs. Never reuse an existing output directory merely because it contains partial data or a prior script failed.

The new bounded row contract should explicitly seal these facts before authoring:

1. Row family/version, approved number of steps, step index, fresh batch owner and section ID.
2. Exact section bounds, fixed origin, grid size, tile size and local address orientation.
3. Exact occupied geographic-cell set before and after the step, including the initially sparse upper-east area.
4. South owner and south anchor border; west owner/border explicitly absent for step 1 and required thereafter.
5. Immediately preceding accepted scene hash, manifest hash and complete protected owner inventory.
6. Expected retained/new/total terrain counts, seams, exterior slots, collider sample count and connectivity owner count.
7. DEM products, metadata/datum/surface, source spacing, both-axis ownership and accepted acquisition identity.
8. Measured height range, unchanged encoding and the derived precision/adjustment limits.
9. Runtime color-crop identities, freshly measured corner values and any sole permitted raster-pixel exception.
10. Saved native source/reopen proof/export hashes and source, runtime, candidate and evidence destinations.

All explicit “none” values need a reason. A missing west anchor on step 1 is correct geometry; a missing west anchor on step 2 is a failed contract. A broad AABB with fewer occupied cells is expected mid-row; a silently missing occupied cell is not.

Adapt the new acquisition, preparation, native authoring and export tools as one contract. Keep Blender's runtime imports narrow and keep GIS-only dependencies in the `.venv` process. The exporter must reject stale source/reopen hashes before it creates accepted runtime files.

For Unity, keep Editor-only automation under the existing isolated Editor assembly. Preserve runtime gameplay in `BooterBigArm.TopDown3D.Runtime`; this row needs no gameplay assembly or runtime world manager. Adding a new bounded Editor source/builder does not authorize package or project-wide configuration changes.

Before each local commit, the section receipt must answer:

- What exact geographic section and predecessor were accepted, and which source versions were used?
- Which native files were saved/reopened, and which five visual views were inspected?
- Which scene/manifest/native asset bytes were validated and transferred?
- How many original scene documents were preserved, which parent changed, and how many documents were added?
- What were measured height/collider/adjustment/color errors, compared with unchanged tolerances?
- Which focused checks passed, and which saved-versus-live proof limits remain?

Keep the section's staging list explicit and include native `.meta` companions where applicable. Do not stage a directory wholesale while unrelated dirty files exist. The new row's source/proof identities and future retirement records must remain traceable after any later authorized cleanup.

## Handoff-time reconciliation note

The table of accepted counts and hashes above describes the completed, committed terrain build. The newer working edits listed in section 3 have not been validated or accepted by this handoff author. The next agent must reconcile them before treating any historical scene, parent layout, or full protected-file hash inventory as its current starting baseline. This handoff creates no authority to discard that work.

## Capacity snapshot and practical execution

The handoff-time free-space read was approximately **42.06 GiB on D:**. This is not a six-section capacity pass. The old orchestration retained a fresh full Unity project/cache for every section; repeating that layout for six larger sections would need substantially more space than this snapshot. Inventory current payloads and peak source/prepared/export/LFS/cache growth before launching the row. Plan a bounded number of isolated copies, or verified disposal of new task-owned temporary validation copies only after their required assets, logs and proofs are preserved. Do not delete earlier validation projects or the protected C: rollback merely to force the capacity gate to pass. Native source retirement frees working-file space, but ordinary local Git/LFS history still retains tracked payloads; do not assume the full original payload size disappears from all storage.
