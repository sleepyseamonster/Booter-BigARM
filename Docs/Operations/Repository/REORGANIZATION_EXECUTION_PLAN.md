# Repository reorganization execution plan

**Owner:** Gottspan. **Authority:** the author authorized organizational file moves and document/redesign work on 2026-10-06, provided files remain usable and discoverable. **Status:** phased execution authorized; inactive-host segregation completed, remaining batches pending their checks.

The [selected layout](./LAYOUT_PLAN_2026-10-06.md) defines the target. Its [baseline move map](./layout-migration-map-2026-10-06.csv) is a dated proposal. This document tracks execution; [relocation receipts](./Relocations/hosts-2026-10-06.json) record actual source/destination paths and hashes. Refresh the map against current Git state before each new batch.

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
| ORG-02 | Group topical documentation into Design, Engineering, Operations, Research, and historical archives; preserve stable canon/status anchors | ORG-01 | Pending | Classify owner and authority status; repair every live inbound link and command reference; retain historical records; no duplicate canon owners |
| ORG-03 | Move Blender study/resource units into SourceArt and shared CLI tools into Tools; establish durable GIS manifest placement | ORG-02 | Pending | Destination LFS rules installed before moves; all ten Blender hashes preserved; relative/packed resources inspected; CLI roots/configs verified; receipt relocation support verified |
| ORG-04 | Split production editor/tests/validators from legacy and isometric responsibilities | ORG-02 | Pending | Unity import/compile and focused assembly tests; production-only assembly references; preserved legacy test restoration manifest |
| ORG-05 | Promote shared URP profile and prepare production-only renderer routing | ORG-04 | Pending | Profile GUID preserved; every affected camera/default renderer index updated together; production rendering/scene validators pass |
| ORG-06 | Extract Legacy2D and isometric assets/code/docs outside active Assets | ORG-04, ORG-05 | Pending | No active dependency into the archive; complete restore manifest and shared dependencies; clean compile/import and focused scene checks |
| ORG-07 | Move Greater Wasteland to Scenes/Production; rename the actual production 3D renderer; segregate disabled prototype scene | ORG-06 | Pending | Scene/renderer GUID continuity; Build Settings, validators, commands, tests, and atlas discovery agree; shared runtime code retained |
| ORG-08 | Review Resources, tool ownership, final indexes and resumption records | ORG-03, ORG-07 | Pending | Runtime load paths and referenced resources unchanged; no accidental production payload removals; final layout/metadata/link checks; exact verified local commits |

ORG-01 is independent of Unity's imported assets and can be completed without altering the open Editor project. Later Unity extraction batches require dependency separation and actual import/compile proof. The user has already authorized verified moves; execution does not need another approval for routine placement choices within these contracts. Stop a particular move if its dependencies or validation are unresolved and continue independent work.

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
