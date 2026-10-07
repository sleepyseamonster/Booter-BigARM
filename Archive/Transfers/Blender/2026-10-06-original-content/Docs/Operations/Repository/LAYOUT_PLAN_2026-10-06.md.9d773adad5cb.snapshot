# Professional repository layout — audit and migration plan

**Status:** selected target layout from the author's 2026-10-06 organizational audit request. Physical migration is pending. This audit changes documentation and connection status, not Unity assets, host folders, packages, or runtime behavior.

**Snapshot boundary:** this is the original audit design. Later authorized ORG-01 execution moved the two inactive hosts; the [execution plan](./REORGANIZATION_EXECUTION_PLAN.md) owns current paths and reviewed gate ordering. The [plan review](./PLAN_REVIEW_2026-10-06.md) adds implementation prerequisites. Do not interpret the original pending-status wording or baseline CSV as current executable instructions.

The repository should read as one active Unity product, with explicit source inputs and one preservation area. Keep the Unity project at the repo root so the registered workspace, Unity Hub path, tooling, and relative build paths remain stable. Greater Wasteland remains the production scene; procedural generation implementation remains deferred.

## Findings that determine the design

| Current area | Tracked files | Materialized bytes | Finding |
| --- | ---: | ---: | --- |
| `Engine/` | 1,276 | 278,446,567 | Inactive proprietary host, its assets, tools, docs, and evidence still occupy a root production-looking lane |
| `Unreal/` | 11 | 12,856 | Inactive host-direction documents; retain together |
| `Assets/_Project/Legacy2D/` | 258 | 6,387,690 | Still imported and compiled by Unity despite disabled scenes |
| Isometric runtime and scene | 24 | 93,728 | Separate historical experiment, dependent on legacy runtime/assets |
| `Docs/Agents/Blender/` | 257 | 900,249,230 | Editable sources, textures, renders, scripts, and receipts dominate an agent-instructions folder |
| Root `Docs/` files | 62 | — | Current standards, proposals, historical conversion work, and operations are mixed |
| `Assets/_Project/Resources/` | 44 | 867,810 | Runtime-loaded content needs explicit ownership; disabling a reference scene does not exclude Resources content |

The inventory baseline contains 5,927 tracked paths, including the then-staged coverage package. That package was subsequently committed as `bd71288`. Counts overlap where an area is a child of another; sizes are working-tree bytes, not Git/LFS transfer cost. [Inventory evidence](../../Evidence/Repository/professional-structure-audit-2026-10-06.json) records exact assembly edges and inbound serialized legacy references. [The complete proposed move map](./layout-migration-map-2026-10-06.csv) classifies every baseline path. The map is a plan, not a log of executed moves.

## Target layout

```text
repository root/                 Unity project and registered workspace stay here
  Assets/_Project/               active imported Unity content only
    Art/                        production-ready imported source and runtime art
    Audio/
    Materials/
    Plugins/                    Unity-owned native/third-party integrations
    Prefabs/
    Resources/                  deliberate runtime-load contracts only
    Scenes/Production/          GreaterWasteland.unity
    Scripts/Runtime/TopDown3D/   preserve current namespace and assembly identity
    Scripts/Editor/             production tools and validators only
    Settings/                   production/shared settings with clear ownership
    Shaders/
    Tests/Editor/               production tests only
    UI/
    VFX/
  Packages/
  ProjectSettings/
  Docs/
    DOCS_INDEX.md               navigation
    WORLD_BASIS.md              stable local game-canon anchor
    ROADMAP.md                  sequencing owner
    PROJECT_STATUS.md           current proof and open gates
    PROJECT_BASELINE.md         current product snapshot
    DECISION_LOG.md             accepted decisions
    LOCAL_WORKSPACE.md          local setup and resumption
    REPOSITORY_ORGANIZATION.md   layout and placement rules
    Agents/                    five persistent role packages, instructions only
    Design/                    gameplay, world design, art direction, deferred designs
    Engineering/               architecture, Unity, rendering, and performance
    Operations/                workflow, publication, repository maintenance
    Research/                  source-backed investigation and terrain coverage
    Evidence/                  dated verification records and receipts
    ThirdParty/                licenses and attribution
  SourceArt/Blender/Studies/     provisional editable sources and resource directories
  SourceData/Terrain/            tracked dataset manifests, bounds, hashes, provenance
  Tools/                        repository, Unity-adjacent, art, and GIS CLI tooling
  Archive/
    ProprietaryEngine/           complete former Engine area
    Unreal/                    complete former Unreal area
    Unity/Legacy2D/             legacy assets/code/docs and restoration dependencies
    Unity/Isometric/            historical experiment and restoration dependencies
    Unity/GeneratedWorldReference/  disabled prototype scene and reference manifest
    Unity/Conversions/          historical conversion evidence
    Transfers/Blender/          immutable transfer receipts and inert recovered data
```

`Library`, `Temp`, `Logs`, `UserSettings`, local environments, and build outputs remain ignored local state. A new shared folder is introduced only when it has actual content and an owner; empty placeholder trees are unnecessary. Full-size original GIS datasets belong in separately provisioned external storage, with tracked manifests here. The current bounded raster proof windows under `Logs` are not durable source storage.

Keep the existing runtime namespace, assembly names, and core canonical document anchors. Moving folders does not justify mass-renaming script types, rewriting accepted canon, or changing mechanics. The historical generated-world scene can be segregated while shared 3D runtime classes remain active; future generation design is not implemented by this migration.

Classify by actual role and dependencies, not names alone. A `Prototype` art directory can contain a model used by Greater Wasteland, and a native C++ plugin can be part of Unity tooling rather than the proprietary engine. Those remain active until dependency evidence establishes otherwise.

## Specific source-to-target decisions

| Current source | Selected target | Required gate |
| --- | --- | --- |
| `Engine/` | `Archive/ProprietaryEngine/` | Preserve all source/evidence; repair live entry-point links, command paths, and root discovery; leave historical command receipts unchanged |
| `Unreal/` | `Archive/Unreal/` | Preserve its instructions and docs; update current root routing |
| `Assets/_Project/Legacy2D/` | `Archive/Unity/Legacy2D/Assets/_Project/Legacy2D/` | Separate assemblies, shared URP assets, validators, scenes, and restore dependencies first |
| Isometric scene/runtime/editor code | `Archive/Unity/Isometric/Assets/_Project/...` | Move their whole dependency slice and associated tests; legacy runtime dependency is explicit |
| Disabled `TopDown3DPrototype.unity` | Conditional future archive under `Archive/Unity/GeneratedWorldReference/` | First replace or preserve its live play-setup template, validators, capture tools, tests and preflight consumers; shared runtime remains active |
| `GreaterWasteland.unity` | `Assets/_Project/Scenes/Production/GreaterWasteland.unity` | Preserve its GUID; update Build Settings, path constants, tests, and GUID-based discovery consumers |
| `Docs/Agents/Blender/studies/` | `SourceArt/Blender/Studies/` | Move complete resource directories together; keep dated previous saves beside their source; inspect relative/packed resource paths |
| Blender CLI scripts/configs | `Tools/Art/Blender/` | Replace fixed ancestor-depth assumptions and old absolute paths with explicit/derived repo roots |
| Blender terrain build/expansion plans and terrain-pipeline research | `Docs/Research/Terrain/DeathValley/` | Keep research with its project topic; agent techniques/instructions remain in the role package |
| `Docs/DeathValley/` | `Docs/Research/Terrain/DeathValley/` | Coordinate with its owner; preserve data/atlas receipts and update current discovery/link paths |
| Blender transfer archives | `Archive/Transfers/Blender/` | Keep original receipt bytes; add a separate relocation map rather than rewriting historical evidence |
| Root topical docs | `Docs/Design`, `Engineering`, `Operations`, or relevant archive | Explicit owner/status classification and full live-link repair; no filename-only declaration of canon or obsolescence |

The CSV distinguishes direct proposals from shared assets or mixed files requiring a split. It deliberately retains production runtime code and assets that cannot be identified as exclusive legacy content. For example, `IsometricRenderer.asset` is actually the active 3D renderer; it should eventually become `Renderer3D.asset`, preserving its GUID and settings, rather than being archived by name.

Update `.gitattributes` before moving any Blender source: the current LFS rule matches `Docs/Agents/Blender/**/*.blend`, not the future SourceArt/Archive paths. Verify destination attributes and indexed OIDs, then retain both source bytes and LFS objects. No history rewrite or separate repository/submodule is needed for the selected in-repo archive. A future remote archive split would be a separately authorized publication task.

## Legacy extraction is a dependency migration

Current explicit assembly edges:

- `BooterBigArm.TopDown3D.Runtime` has no explicit legacy or isometric assembly reference.
- `BooterBigArm.Editor` references both `BooterBigArm.Runtime` and `BooterBigArm.Isometric.Runtime`.
- `BooterBigArm.Editor.Tests` references those same historical runtimes.
- `BooterBigArm.Isometric.Runtime` references the legacy `BooterBigArm.Runtime`.

The active Editor/test assemblies therefore need production-only boundaries before the historical code leaves `Assets`. Mixed conversion validation should split into production validation and preserved legacy validation. The isometric item database, input adapter, and inventory references belong to its reconstruction slice.

Seven scanned inbound YAML references currently cross into Legacy2D: three from the isometric scene, two from project Build Settings, and two from production URP settings. The URP global default volume profile points to `Legacy2D/Settings/Profiles/DefaultVolumeProfile.asset`. Promote that genuinely shared profile into production settings with its GUID unchanged before extracting the legacy subtree.

The URP renderer list still uses legacy Renderer2D at index 0 and the production 3D renderer at index 1. Greater Wasteland's camera explicitly selects index 1. A production-only renderer list must update the default and every affected active camera consistently; removing list element 0 by itself changes the indexing contract. Preserve the original configuration with the archive's restoration manifest.

Removing old 2D package pins is a later dependency decision. PSD importer support is used by source art, and UI/URP packages can serve 3D production. Do not infer unused packages from their names. Retain current pins throughout the initial migration.

Unity GUIDs and asset metadata must move with assets, and explicit assembly references define compile dependencies. [Unity 6.4 asset metadata](https://docs.unity3d.com/6000.4/Documentation/Manual/AssetMetadata.html), [assembly references](https://docs.unity3d.com/6000.4/Documentation/Manual/assembly-definitions-referencing.html). Unity includes Resources assets in builds even when a scene does not reference them, so review load contracts separately from scene enablement. [Unity 6.4 Resources API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Resources.html).

## Agent and lore ownership

| Role | Stable home | Responsibility in the selected layout |
| --- | --- | --- |
| Gottspan | `Docs/Agents/Gottspan/` | Layout contract, migration manifests, integration order and ownership |
| Babineaux | `Docs/Agents/Babineaux/` | Unity moves/import, renderer and scene references, background validation |
| Gear Ball | `Docs/Agents/GearBall/` | Exact task staging, verified local commits, separately authorized publication |
| Blender | `Docs/Agents/Blender/` | Instructions and source-art handoff; actual sources under SourceArt, shared CLI tools under Tools |
| Lorekeeper | `Docs/Agents/Lorekeeper/` | Local canon/proposals now; narrowly retrieved external references after a later connection |

**Lore connection:** disconnected, confirmed by the author on 2026-10-06. The lore checkout is absent on this machine; no lore GitHub URL or connection is configured here. The old Mac path is historical. This audit does not clone, link, mirror, create a submodule, or configure a remote for that repository.

`Docs/WORLD_BASIS.md` remains the game's canonical anchor. It is not a substitute for the unavailable wider corpus. Later reconnection needs the user's repository identity, authenticated access, a real local reference location, and the source's governance files. The external source remains read-only for Lorekeeper. Record machine-local paths privately, keep credentials out of Git, and use provenance pointers rather than importing the lore encyclopedia.

## Migration sequence and completion gates

| Phase | Concrete batch | Exit gate |
| --- | --- | --- |
| 0 — contract | This audit, complete per-file map, disconnected lore status, corrected legacy-boundary wording | Link checks, mapping coverage, no target collisions, documentation-only Git boundary |
| 1 — inactive hosts | Move Engine and Unreal as complete areas under Archive | File hashes/counts unchanged; source/evidence retained; current entry-point links and CLI roots valid; isolated from active Unity |
| 2 — docs and source inputs | Group topical docs; move study/resource units and CLI configs; provision tracked GIS manifests | Relative resource/link contracts verified; all ten Blender hashes and LFS objects preserved; transfer receipts resolvable through relocation records |
| 3 — code separation | Split mixed editor/tests/validators; promote shared URP profile; prepare production-only renderer configuration | Current Unity import/compile and focused structural/rendering tests pass; Greater Wasteland actor/control wiring preserved |
| 4 — legacy extraction | Move Legacy2D and isometric slices outside Assets; update production Build Settings and validation | No active assembly/serialized dependency on archive; production-only renderer/camera indices valid; archive restore manifest complete |
| 5 — production polish | Move primary scene to Scenes/Production; rename active 3D renderer; review runtime Resources and tool placement | GUID continuity and load contracts verified; focused scene/asset validators pass |
| 6 — seal | Ownership indexes, baseline/status and resumption instructions, exact local commits | No incidental churn; historical evidence unchanged; task files only; pushes remain separately authorized |

Perform one independently verifiable batch at a time on `main`. Before each batch, refresh the shared index and current writers; no other chat's work is incorporated by convenience. Asset moves use a background-safe Unity path or an isolated candidate and preserve `.meta` files. Do not bring windows forward to perform this work. Scene feel and Player profiling remain separate from structural proof. The migration introduces no world generation, gameplay feature, package upgrade, or lore canon change.

## What this audit completes

The selected layout, complete file classification, dependency blockers, owners, sequence, and proof gates are concrete and reviewable. Lorekeeper now reports the real disconnected state. Physical migrations remain future execution batches; no folder tree, asset, assembly, renderer, or build configuration was moved by this audit. Re-run inventory immediately before migration because the project is active and the CSV is a dated snapshot.
