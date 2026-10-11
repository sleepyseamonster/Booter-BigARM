# Third northern row: process and continuation handoff

Prepared October 11, 2026, for a new agent on the same Windows checkout. This is a continuation document, not authority to acquire another row, delete sources, push, change branches, upgrade packages, or resume a paused implementation goal.

## 1. Start here: the previous goal is complete

The user authorized **six** northern sections. We completed the third northern strip, west to east, with separate authoring and native acceptance checkpoints for every section. All six are integrated into the root production scene and committed. The full completion audit passed, and the goal was marked complete after a final current-state check.

**There is no terrain job to resume and no seventh third-row step.** Do not rerun the last candidate, export, acquisition, transfer, audit writer, or commit as though it were still pending. Many local helpers reject existing outputs intentionally.

| Current fact | Accepted value |
| --- | --- |
| Checkout | `D:\Arc & Dust\Project` |
| Branch / upstream | `main` / `origin/main` |
| Latest completion commit before this handoff | `91282123015330e6441973c1960f0ca873292a12` |
| Final native terrain acceptance commit | `c3786697419fd9b5c6a9637f98306c4e7cd8c6f6` |
| Active production scene | `Assets/_Project/Scenes/Production/GreaterWasteland.unity` |
| Scene GUID | `4de5dd018ee194314a00fd369e2d3eeb` |
| Full world bounds, EPSG:26911 | `[499920,4006200,524496,4026680]` |
| Newly completed strip | `[499920,4022584,524496,4026680]` |
| Occupied grid | Complete `96 × 80`, with no northeastern hole |
| Native terrains / sections / sectors | `7680 / 30 / 480` |
| Exact reciprocal joins / exterior slots | `15184 / 352` |
| Direct collider samples | `192000`, 25 per terrain |
| Unity world origin | `[522448,4008248]` |
| Height encoding | Minimum `-100 m`, range `1800 m`; representable maximum `1700 m` |
| Common terrain parent Transform ID | `1259644692` |
| Saved scene SHA256 | `19c3633ffd884f11b72cd9f6daae13526315ad252f93da49b5191d99ab87c566` |
| Final source manifest SHA256 | `9b0c383e636a3a461fa9d607db5f2f84b64da4485edfa2241197145023c06304` |

The last manifest is `Assets/_Project/Art/Terrain/NorthRow3E520400N4022584_2026-10-10/Source/manifest.json`. Its paired `unity_production_validation.json` binds the accepted saved scene to full native readback and collider proof.

Read these current documents before older handoffs:

1. [Completed execution plan and proof summary](NORTH_ROW3_EXECUTION_PLAN_2026-10-10.md).
2. [Full six-section completion audit](../Evidence/DeathValley/NORTH_ROW3_COMPLETION_AUDIT_2026-10-10.json).
3. [Final capacity receipt](../Evidence/DeathValley/NORTH_ROW3_FINAL_CAPACITY_2026-10-10.json).
4. [Final section closeout](../Evidence/DeathValley/NorthRow3E520400N4022584_2026-10-10/closeout.json).
5. [Committed shared-scene reconciliation](../Evidence/DeathValley/NorthRow3E520400N4022584_2026-10-10/committed_scene_reconciliation.json).

The October 9 [next-row handoff](NEXT_NORTHERN_ROW_HANDOFF_2026-10-09.md) described the **second** northern row, starting from 4608 tiles. That work and the third row are now complete. Its proposed bounds and counts are historical, not the next acquisition contract.

## 2. Operating agreement and authority

Load [root AGENTS.md](../../AGENTS.md), [LOCAL_WORKSPACE.md](../LOCAL_WORKSPACE.md), [repository organization](../REPOSITORY_ORGANIZATION.md), and the relevant role instructions. Gottspan coordinates the repository; Babineaux owns the Unity bridge; Blender owns modeling; Gear Ball owns precise Git work; Lorekeeper owns narrative retrieval. We used these lanes in one chat and did not spawn subagents for this row.

Keep these constraints:

- Work on the existing `main` checkout. A completed local commit does not authorize a push, PR, branch switch, worktree, release, or external publication.
- Preserve the user's foreground application. Do not focus, activate, raise, or send keystrokes to Unity or Blender. Use hidden background processes and read-only bridge calls.
- Preserve loaded unsaved scenes. Do not save, switch, close, or restart the user's Editor to make automation convenient.
- Never casually delete, regenerate, rename, or move Unity `.meta` files. Native data bytes, GUIDs, import settings, references, and serialization all matter.
- Gameplay smoke testing and hands-on traversal are user-owned. We did not run a gameplay smoke test or claim Player performance acceptance.
- Greater Wasteland is fixed authored terrain and always loaded. Generation, terrain streaming, unload/reload services, and procedural save integration remain deferred.
- Keep Archive/Unreal, Archive/ProprietaryEngine, and Archive/Unity/HistoricalProject inactive.
- Stage only verified task-owned paths. Preserve concurrent work, including work committed by another task while you are running.

The [October 9 local-only source policy](../Operations/LOCAL_ONLY_SOURCE_POLICY.md) controls over older text saying Blender files use Git LFS. **All `.blend` files and numbered backups, and `SourceData/Terrain/**`, stay local and must not be staged, uploaded, or newly placed in LFS.** Native Unity runtime terrain assets and their required source resources under `Assets/_Project/` remain normal repository content. Source-art text READMEs are committed.

## 3. Exact resumption preflight

Run from the canonical checkout and inspect current state rather than trusting this snapshot:

```powershell
Set-Location -LiteralPath 'D:/Arc & Dust/Project'
git status --short --branch
git log -6 --oneline
Get-FileHash -LiteralPath 'Assets/_Project/Scenes/Production/GreaterWasteland.unity' -Algorithm SHA256
Get-FileHash -LiteralPath 'Assets/_Project/Art/Terrain/NorthRow3E520400N4022584_2026-10-10/Source/manifest.json' -Algorithm SHA256
Get-PSDrive D | Select-Object Free
```

At handoff inspection, `main` was ahead of `origin/main` by 56 commits **before the documentation commit containing this handoff**. No push occurred. The exact ahead count can change through concurrent work.

The remaining unrelated workspace state was:

```text
 M Assets/_Project/Materials/TopDown3D/Greybox_BigARM.mat
?? Assets/_Project/Art/Environment/Rocks/Fused.meta
?? Assets/_Project/Art/Environment/Rocks/Fused/
```

These are user/concurrent-task owned. Do not stage, restore, delete, reimport destructively, or absorb them into terrain work. Other tasks completed sculpted basalt, live rock fusion, smooth cube rock, scene-specific tilt-shift control, and camp/sleep work during this run. Their commits were preserved and were not broadly copied into the frozen terrain validation project.

The root Editor process was PID `13084` at handoff inspection; two `-adb2` worker processes were also present. These are observations, not durable process identities. No terrain validation Editor was running in the isolated project after the finished jobs.

### Bridge state: do not mistake a live process for a ready bridge

The root bridge was healthy at the final transfer guards. After final asset refresh, the proof `.meta` existed, but a targeted CLI import request returned a network error. A later `unity status` returned `STATUS_NO_INSTANCES`, although the root Unity process remained alive. The same discovery failure was observed while preparing this handoff. We did not restart or focus the Editor.

Use fresh checks:

```powershell
& "$env:LOCALAPPDATA/Unity/bin/unity.exe" status --json --project-path 'D:/Arc & Dust/Project'
& "$env:LOCALAPPDATA/Unity/bin/unity.exe" command console_status --json --project-path 'D:/Arc & Dust/Project'
& "$env:LOCALAPPDATA/Unity/bin/unity.exe" command list_open_scenes --json --project-path 'D:/Arc & Dust/Project'
```

Only use the latter commands when the instance is discoverable. Fresh `groundTruth` timestamps and age matter; historical cumulative error counts are not the current console. Do not follow the discovery error's generic suggestion to reinstall Pipeline automatically: the existing pinned bridge worked throughout most of this task. A disconnected bridge does not authorize a package change or destructive Editor restart.

Earlier, the user clicked Reload and fresh main-thread queries recovered. That was a past event, not a standing instruction to click or focus Unity. The final transfer recorded identical loaded-scene fingerprints before/after. The study was clean at that transfer; earlier transfers preserved it while dirty. Recheck current scene state before any future integration.

## 4. Installed tools and isolation

| Surface | Used on this Windows machine |
| --- | --- |
| Unity Editor | `6000.4.0f1`, `%LOCALAPPDATA%/Unity/Editors/6000.4.0f1/Editor/Unity.exe` |
| Unity CLI | `%LOCALAPPDATA%/Unity/bin/unity.exe`, retained beta12 installation |
| Pipeline bridge | Existing `0.8.0-exp.1` package; no upgrade performed |
| Blender | `D:/BooterBigArmTools/Blender522/blender-5.2.2-windows-x64/blender.exe`, `5.2.2 LTS`, build `d13f752e3b9c` |
| Terrain Python | `.venv/Scripts/python.exe`, existing Python 3.12 environment with numpy, Pillow, rasterio and pyproj |
| Process TEMP/TMP | `D:/BooterBigArmTools/Temp` |
| Physical validation project | `D:/BooterBigArmValidation/TerrainNorthRow3_20261010_01` |

The isolated project is a physical copy, not a Git worktree or hardlinked checkout. It has no `.git`. It began with tracked Assets, Packages and ProjectSettings plus a preserved warm Library copy. Only task code was synchronized deliberately. The final reconciliation additionally copied eight exact camp catalog/definition/icon files and metadata, with backups and hashes; it did not copy all concurrent gameplay code.

The final project/cache size was `23361742315` bytes. Preserve it, prior isolated projects, and the C: rollback copy. Root source binaries and GIS were not cloned into its import boundary. The final capacity receipt recorded `24765276160` bytes free, with a 3 GiB working margin. Handoff inspection still showed approximately 24.8 GB free. Measure again before expensive new work; do not reclaim storage by deleting sources without authority.

Pinned file SHA256 values, unchanged from the starting checkpoint:

```text
Packages/manifest.json
a9b4f332893effbb67f87b48508240992d76968b8b78f870f8f05f12a436f409
Packages/packages-lock.json
d2adab2cb95c215760c616ac81950f6eab23ae18aeed23d69b9c58fe63c8f473
ProjectSettings/ProjectVersion.txt
8468d7add4c1613c42224053cb9c889688ccf141875f44065a40e6d133bc3ea7
```

Do not treat prior Mac paths or builds as installed Windows tools or fresh proof.

## 5. Source ownership, data formats, and local banks

The completed survey owner is:

```text
SourceData/Terrain/DeathValley/NorthRow3Survey2026-10-10/
  Acquisition01/DEM/
  ImageryAcquisition02/
  SourceAudit01/
```

DEM came from four USGS 1 m project products for `CA_FEMAR9Southeast_D24`, owners `x49y403`, `x50y403`, `x51y403`, `x52y403`. Imagery came from the retained acquisition service and catalog recorded in the manifests. Initial imagery retrieval encountered HTTP 504s; the incomplete attempt was retained, and resumable acquisition validated cached files and fetched only missing sections into the separately versioned imagery owner.

The source audit reopened 100691969 unique native samples and six 2048 × 2048 imagery sections. It verified EPSG:26911, finite samples, NoData absence, strict 2 m decimation, input hashes, overlap measurements, and half-open source ownership on both axes. Buffered disagreements were measured rather than averaged or smoothed. The vertical basis is NAVD88 metres and bare-earth DEM. The measured range was `[-83.60609436035156,1158.3819580078125] m`, within the existing encoding. Do not assume a farther row fits that range.

The broad colour calibration reference is:

```text
SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/expanded_v2/corridor_40m/prepared/corridor/naip_landsat_composite.png
```

Its covered north edge is N4024200. Preparation extended only the low-frequency palette by holding the covered northern row, with exact reference/metadata hashes and padding recorded per owner. The recorded padding is 124 rows for this strip; derive dimensions from the actual prepared record, not a remembered nominal resolution. Detailed NAIP and DEM remain measured. This avoids an unfilled dark band without inventing geometry or imagery.

Each section has independent owners:

```text
SourceData/Terrain/DeathValley/NorthRow3E{east}N4022584_2026-10-10/
  Baseline01/
  Prepared01/
  NativeLogs01/
  RuntimeExport01/

SourceArt/Blender/Studies/DeathValley/NorthRow3E{east}N4022584_2026-10-10/
  Pilot01.blend
  north_row3_e{east}_n4022584_01.blend
  combined01.blend
  README.md

Assets/_Project/Art/Terrain/NorthRow3E{east}N4022584_2026-10-10/
  Source/
  TerrainData/
  TerrainLayers/
  ...native prefab and owned metadata...
```

Eastings are `499920, 504016, 508112, 512208, 516304, 520400`. Each section is 4096 m square, each sector 1024 m square, and each tile 256 m square.

The 18 new `.blend` files total `3994214928` bytes and remain local. Keep backups and distinct versions. Earlier retired files are not these new sources: the old retirement ledger has 46 immutable entries whose files remain absent.

Formats and orientation are deliberate:

- Source DEM: measured 1 m vertices; authoring interiors use 2 m controls and all borders retain 1 m samples.
- Combined Blender review: 4 m interior controls, with all 1 m borders. Compact verification samples every mesh/control/border without duplicating a dense 257-square review export per tile.
- Native render source: 257 × 257 normalized little-endian float32, north-first file orientation.
- Auxiliary height resource: 129 × 129 UInt16, north-first, from 2 m controls.
- Tile colour resource: 128 × 128 PNG over 256 m physical extent.
- Native normalized snapshots: actual saved/read-back 257 × 257 float32 heights, south-first array orientation. Use the native-reference adapter rather than guessing flips.
- Native TerrainData: 257 height resolution, 1800 m vertical span, inherited ground/material/draw settings; 128 alpha/colour resources with the retained import policy.

The existing shared snapshot owner `Assets/_Project/Art/Terrain/NorthRow2Baseline2026-10-09` is reused. We did not create a duplicate whole-world shared bank for row three or duplicate retained payloads in every export.

Retained authoring banks consist of the original 18-section row-two `RetainedModel01`, all six row-two `AcceptedModel01` banks, and accepted row-three banks created as each next predecessor was captured. Row-two dates vary: the first three are October 9 and last three October 10. Discover owners from native manifests rather than guessing dates. Capturing step six created the step-five accepted model bank. The final sixth row-three owner may have no `AcceptedModel01` yet; the next authorized family must derive that bank from its actual normalized native references.

## 6. Exact sequence we used for every section

Never author a new west neighbor before its predecessor is accepted and committed. Native quantization determines the real anchor.

1. **Inspect and measure.** Confirm main, ownership, current saved scene/source hashes, source availability, live editor restrictions, and storage.
2. **Capture the predecessor.** Verify the committed saved scene, native proof, all retained TerrainData/GUID/metadata/native-reference hashes, and protected inventory. Build the previously missing actual quantized model bank. Protected-file counts progressed `124305,127396,130487,133578,136669,139760`.
3. **Prepare the section.** Use measured DEM/NAIP and immutable accepted south/west anchors. First section has only a south anchor; later sections use the prior row-three west section. Require exact height and RGB joins. Preserve the 0.042057666927576065 m adjustment budget and explicit corner policy. All southwest RGB differences were `[0,0,0]` where both anchors existed; no exception pixels were needed.
4. **Author and inspect the pilot.** Four meshes for step one; eight thereafter. Save `.blend`, reopen in a separate native invocation, verify samples/borders/normals, render the junction, and actually inspect it.
5. **Author and inspect the section.** Build 256 meshes, save/reopen/verify, render overhead and oblique, and inspect both. Preserve measured ridges, drainage, roads and fans rather than redesigning landforms.
6. **Author and inspect the combined world.** Include all retained sections and only the accepted/current upper-row footprint. Save/reopen/verify and inspect overhead/oblique. Earlier steps have a northeast notch; the sixth must be a full rectangle.
7. **Seal authoring.** After all five real inspections, hash all three sources, proofs, exports and views into the dated authoring checkpoint. Write the source README. Commit only verified text/proof routing, leaving native sources ignored.
8. **Export.** Bind export to the final immutable checkpoint bytes, source hashes, predecessor and protected inputs. Require exact occupied union and normalized joins; duplicate no retained payloads.
9. **Seed isolation.** Root and isolated predecessor scene hashes must match. Copy into a fresh owner; do not overwrite old export versions.
10. **Build native candidate.** Hidden batchmode creates 256 TerrainData assets, layers, native snapshots, sixteen-sector prefab and disabled reference candidate. Wait for terminal process success.
11. **Independent candidate check.** New invocation reads all retained and new native grids, reciprocal neighbors, exact sparse/full occupancy, hierarchy and direct collider samples. Run the focused 34 Row3/Map EditMode cases.
12. **Integrate in isolation.** Append one section to the existing common parent. Restore every original scene document byte-exact except its appended child list. Assert exactly 1314 new documents, including 256 Terrain and 256 TerrainCollider documents.
13. **Validate the saved production scene independently.** Reopen saved bytes and repeat full native readback/collision proof. Run canonical production-setup validation in isolation; this is structural/setup proof, not a gameplay smoke test.
14. **Guard and transfer.** Production must be unloaded in root; Editor outside Play Mode/compilation; no dirty task-owned persistent terrain assets; source/predecessor unchanged. Fingerprint loaded scene objects/components before/after. Copy only new owner, paired metadata, disabled candidate and verified saved production scene. Guard again immediately before replacing the scene.
15. **Finish acceptance.** Native-create/import the proof metadata, ensure root/isolated proof GUID agrees, audit all owned metadata and global GUID uniqueness, refresh full source coverage, render and inspect three map views, run 34 tests against the updated catalog, seal closeout and preservation receipts, stage an exact task-owned path manifest, and commit locally.
16. **Only then start the next section.** Capture the newly accepted native predecessor, including actual quantized heights. Do not start from raw prepared values instead.

Final native errors remained within unchanged gates: step six maximum readback error `0.0274658203125 m`, maximum sampled collider error `0.00006103515625 m`. Native readback tolerance is `1800 / 65532 + 0.00025 m`; collider tolerance is `8 × float32 epsilon × 5000`. Do not relax tolerances to force a join or source mismatch through.

## 7. Tooling map and historical invocation patterns

These are the retained implementation and workflow surfaces. **The row-three contract is sealed to steps 1–6 and N4022584.** Date arguments create owner names; they do not change geographic bounds or authorize a new row.

| Tracked tool / surface | Responsibility |
| --- | --- |
| `Tools/Art/Blender/death_valley/north_row3_contract.py` | Exact six-step geometry, occupancy, product ownership, paths and identity |
| `acquire_north_row3_survey.py`, `audit_north_row3_sources.py` in that folder | Bounded measured acquisition and source reopening/hash/ownership audit |
| `capture_north_row3_predecessor.py`, `north_row3_native_references.py` | Protected committed predecessor, actual native snapshot decoding/model bank |
| `prepare_north_row3.py`, shared `north_row2_preparation_policy.py` | Source preparation, native anchors and colour policy |
| `build_north_row3_scene.py`, `run_north_row3_blender.py`, `author_north_row3_stage.py` | Hidden native build/reopen/verify/render pipeline |
| `export_north_row3_unity.py`, `audit_north_row3_metadata.py` | Guarded export and owned metadata/GUID/payload audit |
| `Assets/_Project/Scripts/Editor/TopDown3D/BadwaterNorthRow3Source.cs` | Manifest/configuration and ownership contracts; distinct source owners parsed once |
| `BadwaterNorthRow3Builder.cs` in that folder | Isolated-only native build and organized integration |
| `Assets/_Project/Scripts/Editor/Validation/BadwaterNorthRow3Validator.cs` | Native grid, neighbors, geographic union, hierarchy and collision |
| `Assets/_Project/Tests/Editor/BadwaterNorthRow3Tests.cs` | Positive/negative third-row contracts; combined with existing map tests |
| `Docs/DeathValley/north_row3_coverage.py`, `audit_coverage.py` | Current native/measurement coverage and developer atlas |
| Existing `BadwaterPlayableSceneBuilder`, `BadwaterTerrainExpansionValidator`, `DeathValleyMapData` | Third-row dispatch, expected counts and full map footprint |

We avoided modifying frozen old-row implementations when a distinct family adapter was needed. Caching source-owner parsing and resolved owner roots removed repeated expensive per-tile JSON/path work without skipping validation.

Tracked Python tests include contract, predecessor rejection, native-reference, acquisition and coverage tests. The coverage test checks all six exact unions plus stale/ambiguous rejection. Every newly added Unity test was executed before committing the tooling. The 34-case filter is `BadwaterNorthRow3Tests;DeathValleyMapTests`; it is not the entire game test suite.

### Local orchestration helpers: present here, absent from a fresh clone

`Logs/DeathValleyNorthRow3/` is ignored and contains:

- `prepare_section.py`, which discovers old accepted owner dates and supplies every retained bank.
- `author` sealing/source README helpers and export seeding helpers.
- `run-unity.ps1`: candidate, candidate-check, integrate, production-check.
- `run-proof.ps1`: tests, current-catalog-tests, playable; supports fresh `-ProofVersion` suffixes.
- `run-map.ps1`: hidden graphics-enabled developer-map capture.
- `live_root_guard.py`, `dirty_terrain_assets.cs`, `current_scene_fingerprint.cs`, `current_console_errors.cs`.
- `transfer_section.py`, `closeout_section.py`, `stage_section.py`, `step_capacity.py`.
- `accepted-commits.json`, exact stage path lists, failed logs and receipts.
- `final_row_audit.py`, `final_capacity.py`, full `final-doctor.json`.
- The narrow, now-historical `committed_reconciliation.py` and `Reconciliation01/` snapshots/dependency backups.

Read their source before reuse. They contain sealed row-three geometry, dates, project paths, freshness assertions and one-time transfer assumptions. The guard/transfer reconciliation option is only the exact committed camp delta for step six; it is not a generic changed-predecessor bypass.

Examples below describe completed invocation shapes. **Do not paste them against the existing output owners.** For another authorized row, implement a distinct contract/family and fresh versioned paths first.

```powershell
# Historical authoring shape, with a separately verified predecessor bank:
& ./.venv/Scripts/python.exe Logs/DeathValleyNorthRow3/prepare_section.py --step 6 --date 2026-10-10
& ./.venv/Scripts/python.exe Tools/Art/Blender/death_valley/author_north_row3_stage.py --step 6 --date 2026-10-10 --stage pilot --blender 'D:/BooterBigArmTools/Blender522/blender-5.2.2-windows-x64/blender.exe'
# Repeat stage section, then combined, with actual image inspection between gates.

# Historical isolated proof shapes:
& ./Logs/DeathValleyNorthRow3/run-unity.ps1 -Stage candidate-check -Step 6 -ExecutionDate 2026-10-10
& ./Logs/DeathValleyNorthRow3/run-proof.ps1 -Stage tests -Step 6 -ExecutionDate 2026-10-10 -ProofVersion 02
& ./Logs/DeathValleyNorthRow3/run-map.ps1 -Step 6
```

Acquisition accepts `--phase dem|imagery --output <fresh owner>` and `--resume-imagery` only for imagery. Source audit accepts `--acquisition`, `--imagery`, and a fresh `--output`. Export requires `--step`, `--date`, `--manifest`, fresh `--output`, and `--checkpoint`. Inspect `--help` and source before adapting any command.

Read-only/current integrity commands that can be run when needed:

```powershell
& ./.venv/Scripts/python.exe Docs/DeathValley/terrain_tools.py --json doctor
& ./.venv/Scripts/python.exe Tools/Art/Blender/death_valley/audit_north_row3_metadata.py --step 6 --date 2026-10-10
```

`--json` is a global argument and belongs **before** `doctor`. `audit_coverage.py` rewrites the catalog/CSV/HTML/SVG outputs; it is not read-only. Run it only for an actual refresh need, then inspect its changes. Final audit/capacity writers reject existing sealed receipts; do not overwrite them for a routine recheck.

## 8. Concurrent camp reconciliation: exact case, not a shortcut

Step six captured predecessor scene SHA `d8a89a0e39426e5085c143dd723ac1a1635b52f00ddd6a08d6473a9aaf833b9c`. During isolated work, another task added one `tool.popup_camp`, quantity 1, to the starter inventory. Root scene SHA became `d78d84cf162415183d8161260be594d66c59de32bb6b9c42a2038a53fad0e716`. The live transfer guard correctly stopped.

We prepared a separate review draft by asserting the root scene differed from the captured predecessor by **exactly those two lines**, in scene document `690964290`. The draft appended the native section while preserving all current root documents except the common-parent child list. No root scene was changed to prepare it.

Original isolated production validation passed with scene SHA `87dfb40666db60fa1731c5b2a8ed1c18324574b2dd92afdd0b234997d7ea2d83`, without the camp overlay. The first alternate-reference reconciliation validation failed: the old isolated catalog lacked the camp item, and the validator rejects arbitrary scene owners. Its log is retained. We did not weaken either check.

We backed up the original production scene byte-exact, copied the committed camp catalog/definition/icon resources and metadata into isolation, and validated the draft at the registered production path. Corrected report `step6-reconciliation-production-readback02.json` passed all 7680 grids and 192000 collider samples at final scene SHA `19c3633...`. Canonical setup and fresh `step6-tests02.xml` then passed.

The camp task committed its scene and dependencies as `46c0bae74474a2179a8292b7157eadf5c4237286`. The earlier question offered keeping the camp item with terrain or waiting for the camp commit; once the independent commit arrived, both options converged. We did not claim a human reply. The strict verifier required that commit to be an ancestor, the shared scene to be clean and byte-identical to the committed checkpoint, and all eight copied dependency hashes to match both root and isolation. Thus the terrain commit included **no camp code/assets or uncommitted camp lines**.

Transfer then used the reconciled report, preserved 38647 current predecessor documents, appended one parent child and 1314 documents, and retained the exact loaded-study fingerprint. The original captured source lineage stayed immutable. The public receipt distinguishes the captured terrain predecessor from the actual committed transfer predecessor. Final audit permits only the terrain-parent appends and this specifically committed inventory change relative to the original 6144-tile scene; it still proves original terrain bytes/GUIDs/heights are unchanged.

Do not reuse this exception for unrelated drift. A new shared-file change requires a new bounded reconciliation, provenance, current ownership decision and relevant fresh proof.

## 9. Operational lessons that prevented data loss or false proof

- **Wait for the actual terminal result.** An observation timeout, unchanged log, or slow protected-file scan is not failure. Track the same exec session and live process, CPU, artifacts and logs. Never restart solely because a wait returned without output. All recorded terrain jobs have now finished; old session IDs/PIDs in local notes are historical.
- **Do not load giant logs or full commit output unnecessarily.** Native runs can take minutes scanning more than 100000 protected files. Use concise tails and final JSON/XML. Use `git commit -q`; one verbose terrain commit produced excessive output.
- **Separate process completion from displayed text.** A wrapper can still be closing while Unity's log reports success. Require the wrapper's terminal exit 0 before dependent work or the next predecessor capture.
- **Guard native command exit codes.** Use Python `subprocess.run(..., check=True)` or explicit PowerShell native-exit handling. A later shell command can otherwise conceal a failed earlier check.
- **Long eval timeout is positional.** For a long Unity CLI eval, pass `60000` after the code/file argument and use `--timeout 60` for transport. Global transport timeout alone does not replace the eval command's default 5000 ms main-thread deadline.
- **Check effects before retrying a timed-out import.** Earlier metadata refresh completed after a reported five-second timeout. At the final import, the proof `.meta` existed despite network failure. Recheck files, GUIDs and live state rather than regenerating metadata or repeating work blindly.
- **Preserve native serialization.** Unity emits canonical empty `m_Name: ` lines with trailing spaces. Do not normalize a sealed scene or proof to satisfy a whitespace preference. Check ordinary text/code separately. For sealed Windows JSON, use an appropriate `cr-at-eol` whitespace setting without rewriting bytes after export bound their hashes.
- **Use UTF-8 explicitly.** Windows default decoding can mangle names and prose. Read with `encoding='utf-8'`; write intended bytes deliberately. Avoid whole-file newline changes in unrelated C#.
- **Use a graphics device for map capture.** The hidden map launcher intentionally omits `-nographics`. A hidden `DeathValleyMapWindow.RenderFromCli` instance does not show a window or open the production scene. Inspect all three PNGs before claiming review.
- **Do not clear diagnostics to make a gate green.** The guard classified only exact known immutable URP test-file messages and a known bridge timeout when present. Unknown compilation or console failures block transfer and must be understood. Later fresh compiler truth had zero errors.
- **Own only your paths.** Concurrent user tasks produced many temporary dirty files and commits. We never broadly copied them into isolation, staged them with terrain, or reverted them. Production inventory reconciliation was explicit and separately verified.

## 10. What the completed proof establishes

The final doctor checked 15374 records with zero issues. The completion audit independently checked all six committed acceptances, occupied keys, current saved scene/catalog match, source/manifest/authoring lineage, all 18 retained source binaries and their reopened proofs, five views per section, native readback/collision counts, test XML, metadata receipt hashes, palette/input provenance, six geographic lookups, retired-source absence, and byte-exact package/editor pins.

It also verified six parent child appends and 7884 added scene documents relative to the original 6144-tile baseline. Source/publication exclusions passed. The final tree and staged path checks contained no raw GIS, `.blend`, `.blend1`, or unrelated user files.

This does **not** establish hands-on traversal, close-range material artistic acceptance, current root GUI responsiveness after its bridge disconnected, camp gameplay acceptance by this terrain task, or Player performance. The final canonical setup proof and all focused tests ran in the hidden physical validation project. No foreground activation, gameplay smoke run, source cleanup, package upgrade, push, or PR was performed.

## 11. Where a new agent should continue

First finish orientation, not implementation:

1. Load current instructions and this handoff; inspect current main, status, scene/source hashes, pinned files, storage and local source availability.
2. Verify the completed-row receipts still match live files. If hashes drifted, classify whether the change is authorized concurrent work before touching anything.
3. Inspect the actual current user request. The completed six-section goal has no outstanding terrain stage. Do not infer that a fourth northern row, source retirement, publication or procedural redesign is approved.
4. If Unity interaction is needed, establish fresh bridge readiness without affecting foreground or unsaved state. Preserve the root Editor if the bridge remains unavailable; use a justified isolated workflow instead.
5. Reuse source/validation patterns through a **new** contract and versioned owner if a new row is explicitly authorized. Do not add step seven or change the sealed third-row bounds to make old scripts accept it.

### If the user explicitly authorizes another complete row

The geometric proposal would be six sections across `[499920,4026680,524496,4030776]`, giving a full `96 × 96` world at step six. This is a proposal, not an acquisition order. Confirm the actual intended extent and derive source coverage afresh.

| Proposed step | Total tiles | Joins | Exterior slots | Collider samples | Sectors |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 7936 | 15680 | 384 | 198400 | 496 |
| 2 | 8192 | 16192 | 384 | 204800 | 512 |
| 3 | 8448 | 16704 | 384 | 211200 | 528 |
| 4 | 8704 | 17216 | 384 | 217600 | 544 |
| 5 | 8960 | 17728 | 384 | 224000 | 560 |
| 6 | 9216 | 18240 | 384 | 230400 | 576 |

The first addition has 480 internal plus 16 south joins, not 512; later additions also have 16 west joins. Final section count would be 36. South anchors would be the six accepted third-row sections. The final third-row accepted model bank must be derived from actual current native snapshots if absent.

That proposal crosses northing 4030000, unlike the completed strip. Under the existing half-open product naming rule it would span both `y403` and `y404` domains across four easting owners. Do not reuse the third-row hardcoded four-product acquisition list. Derive coverage, audit elevation limits, and measure storage before creating inputs or another validation project.

A fourth family would also need explicit Unity dispatch, native-reference/authoring adapters, coverage and map-count support, negative/positive contracts and a fresh committed 7680-tile predecessor. Preserve all existing row implementations and strict gates. The current owner/proof at `NorthRow3E520400N4022584_2026-10-10` is the authoritative final terrain predecessor, including the already committed camp reconciliation.

## 12. Pasteable brief for the next chat

> Work in `D:\Arc & Dust\Project`, on `main` tracking `origin/main`. Read root AGENTS.md, Docs/LOCAL_WORKSPACE.md and Docs/DeathValley/THIRD_NORTHERN_ROW_CONTINUATION_HANDOFF_2026-10-11.md. The third northern row is complete, validated and committed; the world has 7680 terrains, 30 sections, 480 sectors, 15184 joins and 192000 collider samples. The saved GreaterWasteland scene SHA is `19c3633ffd884f11b72cd9f6daae13526315ad252f93da49b5191d99ab87c566`; the final native source owner is NorthRow3E520400N4022584_2026-10-10. Final completion evidence was committed as 912821230 before this handoff. No terrain jobs are active. Preserve user dirty Greybox_BigARM.mat and untracked Fused rock content. All Blender binaries and SourceData/Terrain stay local-only. Root Unity was alive but its Pipeline bridge was undiscovered at the latest check; do not restart, focus or save the user's Editor. Begin with fresh Git/file/bridge/storage checks, preserve concurrent camp and rock work, and obtain the next concrete scope before authoring another row. Do not rerun sealed third-row steps, widen their bounds, activate procedural generation/streaming, run gameplay smoke tests, delete sources or publish externally without current authority.
