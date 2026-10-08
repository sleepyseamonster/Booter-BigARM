# West north and northwest terrain expansion plan

Execution checkpoint 2026-10-08: the [implementation receipt](./EXPANSION_IMPLEMENTATION_2026-10-08.md) records built Blender sources, preserved 256-chunk baseline, validated 1,024-chunk production integration, expanded map coverage, focused tests and Windows Development packaging. The dated planning audit below remains the pre-implementation record. Appearance, traversal and representative performance remain user acceptance.

Build 768 new chunks through the accepted GIS → Blender → exported heights and imagery → native Unity Terrain pipeline. Preserve the existing 256 chunks and integrate a verified 1,024-chunk Greater Wasteland. This October 7 reassessment plans execution; it does not build terrain or accept gameplay appearance.

The controlling [handoff](./HANDOFF_WEST_NORTH_NORTHWEST_2026-10-07.md) settles the footprint and pipeline. The immediate work is baseline capture, native Blender recovery and complete source coverage. Those gates precede terrain construction.

## Fresh audit

Inspection began on `main`, tracking `origin/main`, at `5ca3230290048bab382f99e6c686f9a43f5bc93d`. The working tree contains unrelated BigARM material, input, inventory, radial-menu and new menu-script edits. Preserve them; take another ownership snapshot before implementation or committing.

Fresh repository checks:

- `audit_coverage.py`: 256 height files and 256 color files match their hashes; 480 common source edges agree exactly; all 256 TerrainData references are present. This is export/reference proof, not binary TerrainData readback.
- `terrain_tools.py doctor`: 527 records checked, zero issues.
- Offline Python discovery: 27 tests passed. Existing proposal tests deliberately describe 64-chunk quarters; they do not validate the requested three sections.
- Raster-header inspection under `SourceData/Terrain/DeathValley`: retained EPSG:26911 1 m windows cover the existing section and the southwest quarter of the new west section, with small sampling margins. They do not establish full fine coverage of west, north or northwest. The 40 m corridor covers the union; the 20 m pilot is partial; 200 m overview data is context. Retained fine NAIP imagery covers the existing section; broader imagery is coarser. Header extent alone does not prove finite samples or vertical datum.
- A hidden `--background --factory-startup --disable-autoexec --version` probe of the cached Blender 5.2.2 executable failed with the Windows side-by-side configuration error. Native authoring remains blocked until startup succeeds.
- Live code confirms fixed existing bounds in `export_badwater_unity_source.py`, 256-count assumptions in `BadwaterPlayableSceneBuilder`, and a 16 × 16 name-indexed grid plus fixed ownership paths in `BadwaterTerrainSeamRepair`. `BadwaterTerrainConnectivity` only gathers descendant terrains, so separate section parents need a shared connectivity scope.
- Unity remains pinned to `6000.4.0f1`, URP to `17.4.0`, and the production scene metadata retains its expected GUID. `Temp/UnityLockfile` exists; the process query returned no Unity process. Lock presence does not prove ownership or staleness. Recheck both before any Editor command; this audit did not remove the lock or launch Unity.

The audit refreshed only the catalog timestamp and audited revision. No gameplay or terrain assets were edited. No Unity import, Blender reopen, collider validation, Player launch or performance measurement was performed for this reassessment.

## Spatial and preservation contract

| Section | EPSG:26911 bounds | Chunks |
| --- | --- | --- |
| Existing southeast | `[520400,4006200,524496,4010296]` | 256 preserved |
| West | `[516304,4006200,520400,4010296]` | 256 new |
| North | `[520400,4010296,524496,4014392]` | 256 new |
| Northwest | `[516304,4010296,520400,4014392]` | 256 new |

Each section is 4,096 m square with 16 × 16 chunks of 256 m. The combined bounds are `[516304,4006200,524496,4014392]`. Keep origin `[522448,4008248]`: Unity X is east minus 522448, Z is north minus 4008248. Blender uses X east, Y north, Z elevation. Verify conversion with asymmetric landmark/corner fixtures, rather than relying on a symmetric square.

Rows increase north-to-south, columns west-to-east. Southwest chunk coordinates are `east=xmin+256*col`, `north=ymax-256*(row+1)`. Use section-qualified IDs and geographic keys plus source version; never treat `r00_c00` as globally unique.

Preserve Greater Wasteland scene GUID `4de5dd018ee194314a00fd369e2d3eeb`, all existing TerrainData/source/material GUIDs, existing transforms and heights, gameplay objects, controls, UI and enabled build routing. Do not run the old repair or gameplay builder on production to create the expansion. Inspect baseline render borders first: any existing mismatch is a reported issue, not authority to reshape retained terrain.

This is fixed authored terrain. Geographic keys and immutable source versions supply deterministic identity. Existing authored constraints and measured data remain authoritative. Generation versions and generated-object identities are not applicable because no generator or generated props are introduced. Streaming, unload/reload, runtime deltas, save schema and migration are outside this expansion because the accepted map remains always loaded and this task adds no persistence system. Keep section metadata independent of that loading assumption without implementing streaming.

## Recommended implementation

Extend the existing terrain lane around one versioned expansion manifest. It declares section bounds, source ownership, CRS/datum, spacing, encoding, immutable inputs, Blender proof and output hashes. Use it for Python preparation/export, active Editor creation, validators, coverage and developer-map framing.

Build one common aligned source model, then three section Blender files and a combined review file with a protected existing-section reference. Prepare native 1 m samples where supported and strictly decimate to 2 m; use 257 × 257 Unity render grids. Interpolated vertices are render detail, not new measurements. Record any focus tiles explicitly instead of inheriting old local coordinates.

Proposed production baseline for the new sections is the existing 2 m measured-source detail with 1 m Unity render spacing; native 1 m acquisition/preparation preserves sample alignment. Require native fine coverage rather than silently substituting the 20–200 m studies. Default new Blender chunk meshes to the 129 × 129 prepared 2 m lattice. A new 1 m focus mesh needs an explicit supported detail record. Authoring density and Unity render density are separate contracts; the exporter must reconstruct the same documented render surface from reopened Blender samples.

For Unity, prefer one existing connectivity owner above the four section collections. Audit the current parent and its serialized consumers before choosing the smallest reference-safe hierarchy change. Remove competing child connectivity instances in the candidate so their callbacks cannot overwrite cross-section neighbors. Retain the existing component and scene scope; introduce no second world manager.

Independent section acquisition/builds are possible but risk different resampling and overlap ownership at joins. One enormous Blender source is also possible but raises save/reopen and review costs. The common source model with separate section sources and a bounded combined review preserves consistent samples and makes failure recovery manageable. Use measured memory/file-size evidence to choose review geometry density; retain full-resolution source verification even if the review representation is reduced.

## Batches and gates

### 0. Freeze the retained terrain

Gottspan owns scope and integration; Blender owns authoring; Babineaux owns Unity proof; Gear Ball owns exact local commits. These are role boundaries, not a request to start other chats.

Recheck Git status, HEAD, staged paths, current Unity processes and project lock. Capture baseline file hashes and GUIDs, all 256 placements, sizes, resolutions and material/source references. In an isolated project copy, run readback to capture full render heights and west/north boundaries, including the northwest corner. Record scene gameplay references so integration changes can be distinguished from concurrent menu work.

The existing readback method validates heights but writes only summary errors, not full baseline arrays. Add a read-only baseline-export entry point under the active Editor folder before relying on this gate. Its durable receipt records asset/GUID hashes, per-tile arrays, encoding, transforms, resolutions and boundary ownership; write diagnostics under Logs and retain the required compact proof/source arrays in the versioned expansion directory. Read every existing height without calling SetHeights, SaveAssets, repair or AutoConnect.

Create the isolated copy from a stable snapshot of Assets, Packages, ProjectSettings and the Docs/source dependencies used by the selected tests and map loader. Record copied-file hashes and exact dirty-file inclusions; omit Library/Temp/Logs/UserSettings. Recheck hashes after copying so concurrent writes cannot create an incoherent candidate. A copied unrelated compilation failure is an owner handoff, not authority to fix menu work.

Gate: baseline is complete and source/render differences are explained. Never batchmode a project owned by the open Editor. Do not overwrite a newer user scene with the copied snapshot.

### 1. Recover Blender and prove source coverage

Diagnose the cached Blender failure from Windows loader diagnostics. Repair or replace only the local tool runtime within current authority; no Unity/package changes. Prove hidden background startup, exact version and safe reopen with auto-execution disabled. Preserve existing studies.

Inventory all retained raster formats and metadata, transform any other CRS footprints into EPSG:26911, then query recorded USGS/TNM and imagery services for missing coverage and a sampling halo sufficient for the selected resampler. Header findings above are the initial inventory, not permission to skip other formats or valid reusable sources.

Store new inputs under `SourceData/Terrain/DeathValley/WestNorthNorthwest2026-10-07/` in fresh acquisition directories. Record requests, URLs, dates, source resolution, bare-earth surface, vertical datum, transforms, NoData, hashes and deterministic overlap ownership. Preserve archived Mac JSON; make temporary Windows views through `terrain_archive.py`.

Gate: complete finite source and imagery coverage at explicitly declared resolution, supported datum, reproducible overlap selection and working Blender. Source inventory/acquisition may proceed while Blender is being recovered. Stop terrain construction for unsupported coverage; never label coarse interpolation as native 1 m data.

### 2. Parameterize preparation and verification

Extend the relevant preparation/build/export helpers under `Tools/Art/Blender/death_valley/`. Preserve the existing-section CLI contract or supply explicit existing-manifest compatibility through the same lane. Add section-aware offline checks under `Docs/DeathValley/`.

Create the expansion manifest, common sample grid and section manifests. Keep raw measured elevations immutable; retain separately recorded candidate adjustments if a retained join needs a justified transition. Quantify datum/source discrepancies before any adjustment.

Verify zero NoData, elevations, source hashes, row/axis orientation, disjoint interiors, 768 unique new keys and all shared samples. Check whether new extrema fit the retained −100 to 1,700 m uint16 range. Reject clipping; parameterize new-section encoding if required, leaving existing normalization intact.

Gate: source joins pass against retained accepted boundaries and deterministic rebuilds reproduce samples. Add tests that fail for wrong origin, reversed rows, duplicate IDs, incomplete coverage, changed hashes, overlap ambiguity and out-of-range heights.

Parameterize `BadwaterHeightmapStitching.cs` explicitly. `DecodeSource` currently edits 1 m perimeter samples for local rows 12/13 and columns 1/2. Restrict that compatibility rule to the retained source version and its four recorded focus tiles; decoding a new section must not inherit it. Keep raw source samples and applied render corrections separately accountable.

`StitchBorders` currently selects the first tile in south-to-north, west-to-east order and overwrites later borders in normalized height units. The new west section would precede the retained section at their join. Do not pass all four sections through that method unchanged. Declare the retained readback boundary authoritative regardless of spatial or insertion order; copy only into new candidate border samples, in world metres, then encode with the receiving tile's range. Keep retained arrays immutable and validate new/new joins before stitching so ownership cannot hide a source error. Test reverse insertion order, west-first ordering, four-way corners, different encoding ranges and attempted retained writes.

### 3. Construct and inspect Blender terrain

Create new section sources and combined review under `SourceArt/Blender/Studies/DeathValley/WestNorthNorthwest2026-10-07/`. Build 256 chunk objects per new section using the prepared model and aligned imagery. Keep the existing-section reference protected and never overwrite current `.blend` studies or dated previous saves.

Save, reopen with auto-execution disabled and verify mesh samples, transforms, bounds, elevations, normals, resource availability and imagery orientation. Render north-up overview, oblique overview, four interface close-ups and the shared four-way junction. Inspect the rendered images, not only reports.

First build/reopen/export a bounded pilot containing new/new and retained/new edge fixtures. Establish process memory, render time and file size before building complete sections. At 129 × 129 vertices per chunk, one section has 4,260,096 mesh vertices before deduplication; three have 12,780,288. Full 257 × 257 meshes would have 50,725,632 vertices across the three sections. Record the actual build budget rather than assuming the old study scales safely. Use blockwise or memory-mapped raster preparation and per-section Blender runs. A combined review may use reduced geometry with full edge samples and a declared spacing; it never replaces full section verification.

Generalize `build_game_slice_scene.py` and `verify_game_slice_scene.py` to select section collections, explicit transforms and geographic keys. The current verifier counts all meshes, indexes local IDs and requires an old basin-eye camera/viewport; it cannot validate a combined file unchanged. Apply object world matrices when comparing vertices and reject unrecorded modifiers/deformation. Reopen the combined review too. Blender export must read the reopened authoritative meshes, reconstruct the declared lattice and encode heights; independently compare that result with prepared grids and retain a chain of source, build-script, .blend, reopened-mesh and export hashes. Do not re-export the NPZ alone and call it Blender-derived terrain.

Gate: three saved/reopened native Blender builds, combined review, source correspondence and acceptable seam imagery. Export only from the verified authoring state; tie exported samples and hashes to that state so a Python-only output cannot stand in for Blender construction.

### 4. Build isolated Unity candidates

Add a parameterized active Editor builder under `Assets/_Project/Scripts/Editor/TopDown3D/`, within the existing Editor assembly. Use fresh asset folders under `Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07/` and a candidate scene under `Assets/_Project/Scenes/Reference/`. Keep it outside enabled Build Settings; pair every new asset with its metadata.

Generalize `BadwaterPlayableSceneBuilder`, validation in `BadwaterTerrainSeamRepair`, `Assets/_Project/Scripts/Editor/Validation/BadwaterTerrainReadbackAudit.cs` and focused terrain tests to manifest-defined identity, counts and geographic placement. Separate validation from mutating repair. Any repair writes only explicit new candidate assets; it must reject existing asset paths. Extend `BadwaterTerrainConnectivity` only as needed for the chosen common-parent ownership.

Validators must accept an explicit scene path and manifest for the candidate while retaining production CLI defaults. Otherwise the current hardcoded production paths can report a pass for the old scene while the candidate is untested. Manifest schema must record schema version, immutable original manifest hash, section/key, projected bounds, shared origin, source spacing, render resolution, little-endian north-first encoding and range, relative resource paths/hashes, adjustment records, baseline membership and Blender proof. Reject unsafe paths, unsupported versions and duplicate/missing sections before any write. Candidate output must be fresh and refuse overwrite; integration may add only manifest-listed new assets/objects.

For new native Terrain objects verify explicit `TopDown3DGroundSurface`, collider-to-TerrainData binding, material/import settings, grouping/auto-connect policy and reciprocal neighbor links. Check all 128 external chunk edges have null neighbors, all 1,984 internal pairs link reciprocally, and there is no cross-scene ownership. Test load/reopen and explicit Reconnect without manually repairing links to make assertions pass. Sample physics against TerrainCollider in an isolated EditMode fixture using an appropriate sync/readback path; confirming the collider references the data alone is not collision-surface proof. No actor locomotion or simulated input is required.

If extrema force a new vertical range, world-height agreement is only one gate. First prove native neighbor/LOD and collider compatibility for unequal vertical size/base in the pinned Editor. Do not assume it, clip data or renormalize existing assets. Stop for a concrete range/representation decision if compatibility cannot be demonstrated within the accepted native Terrain contract.

Gate: pinned Unity import/compile and focused EditMode checks pass in the isolated copy; 768 new terrains/colliders and 256 preserved references form a connected candidate. Verify world-height and collider readback, material orientation, unique GUIDs and absence of missing scripts. Baseline TerrainData hashes and full readback remain unchanged.

### 5. Integrate and reconcile coverage

Integrate verified new assets and section objects into the current Greater Wasteland scene after a fresh owner/diff check. Preserve current gameplay and scene GUID. Do not copy the entire stale candidate scene over live production.

Transfer candidate assets together with their original generated .meta files and a hash/GUID receipt; never regenerate GUIDs during transfer. Reconcile against a stable current source snapshot, apply only the additive terrain integration, then refresh the validation copy from the exact integrated live files and run the gates again. Prove enabled Build Settings and the production scene GUID are unchanged. If an owner edit arrives during final validation, preserve it and invalidate only the affected proof; do not replace the scene from an older copy.

Extend `Docs/DeathValley/audit_coverage.py`, its catalog consumers and developer-map data so only built playable sections produce the cyan grid. Update Playable Area framing to the 8,192 m square. Preserve the aerial view, pan/zoom/orbit and two existing buttons. Inspect map tests and serialized consumers before editing; do not reintroduce study/proposal overlays or the inventory sidebar.

Edit `DeathValleyMapData.cs`, `DeathValleyMapWindow.cs` and `DeathValleyMapTests.cs` together with the catalog change. `DeathValleyMapRecord.Key` currently prefers unqualified legacy IDs; terrain records must instead use geographic/section-qualified keys while retaining local IDs as labels. Keep the existing `badwater` region as the retained baseline; derive the combined playable bounds from manifest-verified built tiles and use them in both the toolbar and preview export paths. Replace the map loader's origin-equals-playable-center assumption with validation against the immutable shared origin, without recentering the terrain. Add fixtures for repeated local IDs across sections, the enlarged asymmetric footprint and unchanged origin. Retain the recovered regional relief/imagery and its provenance unless a separately versioned verified update is needed; grid expansion alone must not relabel coarse overview pixels as new fine terrain evidence.

Gate: exactly 1,024 terrains and colliders, all expected keys, no interior overlap, complete neighbors, preserved baseline and gameplay references, passing full-footprint validators, focused map tests and inspected renders. Documentation reports actual built coverage, not merely proposed rectangles.

### 6. Package and close out

Create a fresh Windows Development Player when integration checks pass, using `BadwaterDevelopmentBuild` with a new output directory. Do not launch it. Record build size/time and distinguish packaging from runtime performance.

Build from the refreshed isolated copy of the exact integrated scene, preserving the live Editor session. `BadwaterDevelopmentBuild` calls `BadwaterPlayableSceneBuilder.ValidateFromCli`, so generalization must preserve that default production invocation. Audit the read-only `RepositoryPlayabilityAudit` caller for compatibility without invoking its unrelated generated-world audit. Existing `ScenePlayabilityAuditTests` contain 256-count assumptions and actor/input smoke sequences; record their post-expansion maintenance dependency, but do not run, broaden or weaken those gameplay tests in this task. Require build output and evidence hashes to match the final task-owned asset/script snapshot.

Deliver source/Blender/Unity manifests, reproducible commands, seam reports/renders and preserved-baseline evidence. Commit each verified coherent task batch locally, staging explicit paths only. No pushes, PRs, branch/worktree changes, releases or package upgrades. Supply user-owned traversal checks across each interface, the central junction and outer edges for both actors. Actual appearance, traversal and 1,024-terrain performance remain user acceptance gates.

## Verification matrix

| Claim | Required proof |
| --- | --- |
| Footprint and identity | Manifest/geographic-key checks; 768 new plus 256 retained, exact bounds/origin, no duplicate interiors |
| Measured coverage | Raster metadata and finite sample masks over union plus halo; datum and resolution evidence |
| Reproducibility | Immutable input hashes, deterministic overlap ownership and repeat preparation comparison |
| Blender construction | Native save/reopen, mesh/grid comparison, packed or retained resources, inspected renders |
| All joins | Source, Blender, decoded Unity render and collider checks as separate reports |
| Baseline protection | Before/after existing GUID/hash, transform, full-height and reference comparison |
| Runtime terrain wiring | Import/compile, focused EditMode tests, neighbor and collider readback in isolated project |
| Map coverage | Section-aware catalog, framing/grid tests and inspected map renders |
| Packaging | Successful fresh Development build; no inference of frame time or traversal |
| Gameplay acceptance | User visual review, traversal and representative performance measurements |

Count 1,984 undirected adjacent edge pairs for the 32 × 32 footprint: 1,920 section-internal pairs (480 each) plus 64 cross-section pairs. Of these, 480 are retained and 1,504 involve new terrain. Each of the four 4,096 m interfaces has 16 pairs; check every sample along each and the common vertex `[520400,4010296]`. Measure tolerances from each encoding/source precision. The historical 0.02747 m step and 0.01373 m half-step are references, not automatic acceptance thresholds.

Initial repeatable checks remain:

```powershell
& ./.venv/Scripts/python.exe Docs/DeathValley/audit_coverage.py
& ./.venv/Scripts/python.exe Docs/DeathValley/terrain_tools.py doctor
& ./.venv/Scripts/python.exe -m unittest discover -s Docs/DeathValley -p 'test_*.py' -v
```

New builder/export/validation commands must be documented and exercised in their owning batch; their CLI names are not implemented by this plan. The old `terrain_tools.py plan west` remains a 64-chunk proposal until generalized and cannot serve as the expansion manifest.

Use explicit Unity test-method filters in the isolated copy. Initial permitted existing terrain checks are `BooterBigArm.Tests.BadwaterTerrainSeamTests.CanonicalValidators_AcceptSavedSceneAndAllSourceDerivedVertices`, `ContextGrid_PreservesEverySourceSampleAndInterpolatesNewVertices`, `SharedBorders_CloseEdgesAndFourWayCornerWithoutChangingInteriors`, `SavedScene_PreservesMeasuredDemSamplesAndTileBounds` and `ReloadedScene_RestoresAllNeighborsAndExactBorders` (qualify each with the same class). Update their manifest-aware fixtures where needed while preserving the retained-baseline assertions. Add named expansion EditMode tests for the failure cases above. Select `BooterBigArm.Tests.DeathValleyMapTests` and `BooterBigArm.Tests.BadwaterDevelopmentBuildTests` only in their owning batches. Do not select the whole seam class, its GUI test runner, `ScenePlayabilityAuditTests` or `PlayMode_RestoresAllNeighborsAndExactBorders`: an EditMode label does not prevent those methods from entering Play mode or exercising gameplay.

Publish each new entry point's exact Windows command, explicit candidate/production arguments, test filters and XML/log paths before its batch gate. Baseline readback, source/Blender verification, candidate validation, integration validation and build are distinct invocations and reports. Preserve full-resolution regression assertions; changing 256 to 1,024 everywhere is not a substitute for identity/baseline checks.

## Plan audit corrections on October 7

The plan audit found missing executable baseline capture, inherited focus-tile corrections, unsafe west-first border ownership, candidate validators tied to production paths, unqualified map keys and origin recentering assumptions, unspecified Blender export/resource scaling, and overly broad test selection. The corrections above close those planning gaps. All four interfaces and the central vertex remain required; the geometric counts and original scope are unchanged.

Store compact durable gate receipts under `Docs/Evidence/DeathValley/WestNorthNorthwest2026-10-07/` and heavy reproducibility inputs under the versioned SourceData/SourceArt folders. Inspect `.gitattributes` before each binary commit: .blend, .tif, .npz and imagery already have LFS rules; any new binary format needs an intentional attribute decision and hash-preserving materialization check. Logs are diagnostics, not the sole repository proof. Capture image seam profiles and inspected fixed-view renders with imagery source dates/color adjustments recorded; numerical height equality alone does not accept an imagery seam.

## Failure containment and next move

Use fresh versioned sources, new Blender files and isolated Unity candidates. Stop dependent work for incomplete coverage, loader failure, unexpected hashes/datum, ambiguous ownership, clipping, failed joins, failed compilation/readback or conflicting scene edits. Preserve passing candidates and continue independent work. Never conceal a seam by altering retained terrain or widening a tolerance.

The corrected plan is ready to begin baseline/tooling work; terrain construction remains conditional on Blender startup and complete fine coverage. The first implementation batch is the retained-terrain baseline plus Blender loader diagnosis and complete source inventory. No new footprint or pipeline decision is needed. Completion requires actual reopened Blender sections and validated 1,024-chunk Unity integration; a map overlay or preparation script alone is insufficient.
