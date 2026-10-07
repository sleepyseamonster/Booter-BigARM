# Repository reorganization execution plan

**Owner:** Gottspan. **Authority:** the author authorized organizational file moves and document/redesign work on 2026-10-06, provided files remain usable and discoverable. **Status:** implementation sealed in verified batches; current-state evidence and deliberate retention decisions follow below.

The [selected layout](./LAYOUT_PLAN_2026-10-06.md) defines the target. Its [baseline move map](./layout-migration-map-2026-10-06.csv) is a dated proposal. This document tracks execution; [relocation receipts](./Relocations/hosts-2026-10-06.json) record actual source/destination paths and hashes. Refresh the map against current Git state before each new batch.

**Plan review:** [the execution audit](./PLAN_REVIEW_2026-10-06.md) corrected template dependencies, intermediate assembly boundaries, renderer ordering, source/tool path contracts, and reconstruction gates. The reviewed instructions below supersede the dated baseline map where they add conditions. ORG-01 remains complete; pending stages are not declared implementation-ready without their preconditions.

## Execution contract

- Unity remains at the registered repo root. Greater Wasteland remains the primary scene, with controls, terrain identities, actor wiring, and current mechanics preserved.
- Work only on `main`; stage exact owned paths. Check other chats and shared-index ownership before publication. A local commit does not authorize a push or another repository.
- Use background file/process/Unity automation. Preserve the user's foreground application.
- Files move as complete dependency units. Unity assets keep their `.meta` files and GUIDs. Editable studies keep resource directories, LFS objects, and provenance.
- Current canonical documents retain their ownership. Historical evidence and snapshots retain their original contents unless explicitly identified as live navigation needing repair.
- Each batch must pass its specific checks before the next begins. Do not combine a reference migration with mechanics, procedural-generation implementation, package changes, or lore reconnection.

## Ordered work packages

| ID | Batch and deliverable | Depends on | Current state | Required checks |
| --- | --- | --- | --- | --- |
| ORG-01 | Move inactive `Engine` and `Unreal` into `Archive/ProprietaryEngine` and `Archive/Unreal`; repair live routes and root-sensitive tooling | Existing audit | Complete | All 1,287 files accounted for; runtime/source/evidence unchanged except declared route/tool repairs; 12 roadmap tests; 43 copied surface assets and 186 bindings verified |
| ORG-02 | Rebase the candidate map, then group topical documentation while preserving stable canon/status anchors | ORG-01 | Complete; active terrain package retained under its current owner | Owner handoff and clean owned baseline; live C#/Python/PowerShell/HTML/config consumers repaired with Markdown links; read-only smoke checks; immutable snapshots retained |
| ORG-03 | Move Blender study/resource units and CLI tools; establish durable GIS manifests | ORG-02 | Complete; ten Blender reopening checks pass; bounded GIS preserved | Destination LFS rules installed first; all source hashes preserved; CLI root discovery, receipt relocation resolution, offline atlas/catalog tests and resource reopening verified; missing original GIS data explicitly recorded |
| ORG-04 | Isolate historical editor/tests inside their own assemblies, then split mixed validators and remove their references from production assemblies | ORG-02 | Complete; historical assemblies isolated and tested | Every intermediate commit compiles; legacy/isometric editor and test code retain required references until extraction; production validation replaces the legacy-coupled gate |
| ORG-05 | Promote the shared URP profile and prepare an atomic renderer/consumer switch | ORG-04 | Complete; shared profile GUID retained | Profile GUID preserved; production renderer/camera matrix and builders/tests updated; archive configuration frozen before changes; legacy checks remain valid until the extraction switch |
| ORG-06 | Atomically activate production-only rendering/build routing and extract legacy/isometric slices | ORG-04, ORG-05 | Complete; production and standalone restore verified | No active assembly, serialized, Resources or tool dependency on archive; restore candidate opens/compiles offline; production import/compile and focused structural/rendering checks pass |
| ORG-07 | Separate primary scene relocation, production-renderer rename, and conditional prototype archival into independent sub-batches | ORG-06 | Complete; A/B relocated; C explicitly retains disabled reference | GUID/path and builder/test contracts verified per sub-batch; prototype consumers replaced or explicitly preserved/retired before archival; current mechanics unchanged |
| ORG-08 | Review Resources, tool ownership, final indexes and resumption records | ORG-03, ORG-07 | Complete after final consumer and metadata gates | Runtime load paths and referenced resources unchanged; no accidental production payload removals; final layout/metadata/link checks; exact verified local commits |

ORG-01 is independent of Unity's imported assets and can be completed without altering the open Editor project. Later Unity extraction batches require dependency separation and actual import/compile proof. The user has already authorized verified moves; execution does not need another approval for routine placement choices within these contracts. Stop a particular move if its dependencies or validation are unresolved and continue independent work.

Follow dependency edges rather than treating the table as an excuse to make an uncompilable intermediate state. Independent documentation/source preparation can proceed while a Unity gate is unavailable. Each move has an exact candidate manifest, current owner, validation command, expected result, and rollback mapping before execution.

## Reviewed preconditions (satisfied or explicitly retained)

1. **Fresh candidate scope:** the old CSV names pre-migration host paths and omits later files. Apply completed receipts, enumerate current tracked/untracked task ownership, regenerate the candidate subset, reject target collisions under Windows case rules, and record its baseline SHA. Do not replay completed moves or include another chat's unsealed files.
2. **Tool and receipt compatibility before movement:** The verifier now lives in Tools/Repository, discovers repository anchors, and resolves relocation and original-content aliases; five path-resolution tests pass. Keep original receipts immutable. Repair current tools, configs, HTML loading paths, and generated catalog refresh contracts, not only visible document links.
3. **Legacy assembly isolation before removing references:** give the isometric editor code an Editor-only historical assembly with its required legacy/isometric references. Place `ConversionAssetStructureTests` and `IsometricMovementBasisTests` under historical test-assembly ownership, preserving their `.meta` files. Split production and historical validation so the active gate can run after old scenes/renderers leave Assets. Only then remove historical references from `BooterBigArm.Editor` and its production test assembly.
4. **Renderer switch as one sealed candidate:** the current conversion validator requires Renderer2D at index 0 and the production renderer above index 0. Replace that active requirement before switching to a 3D-only list. Update every retained scene/prefab camera, builders such as `IsometricConversionLabBuilder`, and index assertions together. If historical scenes remain imported, preserve their compatible configuration until extraction; do not commit a list change with obsolete index consumers.
5. **Reconstruction proof:** enumerate native/binary dependencies using Unity's asset dependency APIs, together with assembly, path, Resources-load and package dependencies. Save the original compatible settings. A restore manifest alone does not make the archive usable: restore into a separate empty project outside this checkout's active Assets, use pinned packages and preserved GUIDs, then verify import/compile and required scenes without foreground activation. Shared metadata can exist across separate projects, not as duplicate GUIDs inside the production Assets tree.
6. **Prototype is a current tool dependency:** The rebuild builder now uses the verified GameplaySetup template. `RepositoryPlayabilityAudit`, `WorldCreatorProductionPathValidator`, `ScenePlayabilityAuditTests`, source capture tools and Windows preflight also consume it. Preserve the prototype until those consumers are replaced with verified production templates/prefabs, moved into reconstructible historical tooling, or explicitly retired with updated gates. Archival must not silently remove a working rebuild capability or add its generator to Greater Wasteland.
7. **Source completeness:** Blender 5.2.2 reopened all ten files at both locations; original full-region GIS inputs remain incomplete. Hashes alone do not prove `.blend` resources reopen at a new location. Provision the required read-only inspection capability before study moves. For missing GIS data, record unavailable content and recovery requirements; creating a manifest folder does not establish dataset recovery. Durable source placement and hashes must be verified before later terrain authoring depends on local Logs artifacts.

## Candidate-bound validation matrix

| Surface | Required proof | Explicit limit |
| --- | --- | --- |
| Docs/tools/data paths | Exact path coverage; repaired live consumers; affected CLI tests, offline atlas/catalog tests; valid links | Historical references retain dated context; do not rewrite receipts to hide missing data |
| Source art/LFS | Working bytes, indexed OIDs and local LFS payloads match; resources reopen; reference renderer/readback used only if paths/materials changed | File presence and header signatures are insufficient for moved Blender dependency units |
| Unity assemblies/settings/assets | Actual import/compile; current production scene, terrain readback/seam validators; modified assembly tests and renderer/index checks; GUID uniqueness | No second batch editor against the open project; code inspection is not execution proof |
| Historical extraction | Active dependency scan including binary assets; offline restored project import/compile; complete source/settings/package manifests | No native host runtime, Player performance or physical-controller acceptance inferred |
| Every sealed batch | Exact staged ownership and baseline; hash/GUID/path receipt; no unrelated edits; rollback mapping and tested reverse candidate | A push, package upgrade, lore connection or gameplay redesign remains separate authority |

Use the current path resolved by GUID or the candidate's shared constants when invoking validators; avoid introducing another hardcoded scene-path copy. Run only checks affected by the candidate. No gameplay smoke test or executable build is added by this organization plan; visual/feel acceptance remains separately identified if a renderer migration requires it.

## ORG-01 receipt and current discovery

| Old path | Current canonical path |
| --- | --- |
| `Engine/` | `Archive/ProprietaryEngine/` |
| `Unreal/` | `Archive/Unreal/` |

Both hosts moved intact within the workspace. Root navigation and live cross-boundary document links point to the new locations. The engine's optional Unity-source verifier now discovers the repo root by its Assets/Packages anchors rather than assuming the engine is directly below that root. Its collection comparison uses portable slash-separated paths on Windows.

Checks completed: 12 planning tests pass; 43 preserved assets (101,692,582 bytes), 15 material records, and 186 texture bindings pass the archived collection check. The optional `--sources` comparison correctly reports that current Unity shader bytes differ from the historical transfer; that drift predates this move. Neither the Unity shader nor the archived snapshot was changed to suppress it. No native engine executable or game Player was built for this migration.

From the checkout root on Windows:

```powershell
.venv\Scripts\python.exe -X utf8 Archive/ProprietaryEngine/Tools/verify_surface_assets.py
```

From `Archive/ProprietaryEngine/`, run the planning tests with the checkout's Python executable and `-X utf8`:

```text
python -X utf8 -m unittest discover -s Tools/tests -p test_engine_plan.py -q
```

Historical commands mentioning `Engine/`, former Mac executables, or old output folders remain historical evidence. Use current root discovery and these canonical paths for present maintenance. The archive remains inactive; preserving its usability is not reactivating development there.

## Document and redesign discipline

New or reorganized design documents include: purpose, owner, authority/status, current evidence, dependencies, acceptance checks, and open decisions. Use `accepted`, `implementation plan`, `proposal`, `deferred`, or `historical` explicitly. Put accepted decisions in the decision log and current proof in status/evidence, rather than duplicating trackers inside each agent's memory.

Keep `WORLD_BASIS`, roadmap, baseline/status, decision log, and central indexes as stable anchors. Topic-owned docs can move with repaired references. A redesign proposal does not alter game canon, mechanics, or procedural generation until the relevant user-directed implementation task establishes that authority.

Lorekeeper stays available for local canon and clearly labeled proposals. Its external lore source is absent and disconnected; reconnection is a later task. No remote, connector, submodule, corpus mirror, or guessed repository URL is created by this plan.

## Rollback and batch sealing

Before every move, record the source file set, hashes, GUIDs where applicable, dependency consumers, and current Git revision. Verify resolved source/destination paths stay within the authorized workspace and reject conflicting destinations or reparse points. Preserve generated/local artifacts without publishing them.

After moving, compare the complete file set and hashes, inspect all intentional edits, repair current navigation and CLI roots, and run the batch's affected checks. Commit one coherent batch with its receipt. Rollback reverses that receipt's path mapping and restores only its declared route/config changes from the prior revision; it does not reset another chat's work or rewrite history.

For a failed Unity gate, keep the verified earlier batches and repair or reverse only the unsealed candidate. Do not move Legacy2D simply to make the folder tree look finished while active assemblies or rendering settings still depend on it.

## Implemented layout and retained boundaries

The [implementation evidence](./IMPLEMENTATION_EVIDENCE_2026-10-06.md) records batch receipts, current paths, validation and limits. ORG-07C deliberately retains the generated-world reference imported and disabled because current authoring and validation tools still consume it; external archival is conditional future work, not a missing required move. Production build validation uses production gates independently. The gameplay template contains nine configured roots with no terrain, generator or save service.

Docs/DeathValley remains a cohesive terrain research package at its existing location while its owner actively edits it. Moving that package offers no functional benefit sufficient to interrupt ownership; its live consumers use relocated source/tool paths through relocation resolution. SourceData retains the six bounded recovered GIS files unchanged; the full former dataset is not represented as recovered.

Receipts: hosts, docs, legacy, production-paths, art-tools, remaining-docs-tools and generated-reference JSON records in Relocations. Reversal uses each mapping and the recorded source revision/content version, never a whole-worktree reset.
