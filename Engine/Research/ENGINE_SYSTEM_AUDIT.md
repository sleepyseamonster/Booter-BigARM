# Whole-Engine System Audit

Audited 2026-09-10 at `f19e5503bb0f0a5e46436ce7fdb107b59c034e87`. This is a live-source planning audit, not a new build/run result. The current three dirty Unity files are excluded and preserved. All new work belongs to Engine.

## Finding

The repository contains a useful native rendering/inspection foundation. It does **not** yet contain a game runtime, general asset pipeline or streamed world. The old Engine foundation plan ended at F4 streamed open space, leaving the route to an actual playable and distributable game unspecified. The issue is incomplete program scope and ownership, not just a missing lighting feature.

The [rewritten master plan](../Docs/FOUNDATION_PLAN.md) is the response: build one shared runtime and prove it through progressively more representative game workloads. The engine does not need every feature in a commercial general-purpose engine. It does need the full lifecycle of this particular game, including failure, authoring, persistence and delivery.

## Source and authority

| Source | What it establishes | Boundary |
|---|---|---|
| [Direction](../Docs/DIRECTION.md) and latest user request | Proprietary C++, regular third person, open Greater Wasteland first, Mac daily work/Windows product, complete planning authority | No final camera tuning, PC minimum specification or release date |
| [CMake targets](../CMakeLists.txt) | `engine_workbench`, fixture-state library, two native test executables; pinned SDL/bgfx/bimg/bx/ImGui sources | No standalone game target, physics/audio/animation/navigation dependency or install/package rule |
| [Workbench loop](../Apps/Workbench/main.cpp) | SDL video/events, ImGui inspector, mouse orbit, frame timing, scripted technical capture path | No fixed gameplay tick, gameplay action map, gamepad ownership or world simulation |
| [Renderer](../Source/Rendering/Renderer.cpp) and [scene shader](../Shaders/fs_scene.sc) | Metal / D3D11 selection in source; fixed fixture meshes, depth/culling, simple directional diffuse and constant ambient | No scene texture loader, PBR, shadow pass, general visibility/instance submission or HDR pipeline |
| [Fixture geometry](../Source/Core/FixtureGeometry.h) | Position/normal vertices, reference meshes and tested normal transform | No UV/tangent/skinning/indexed asset model; not a general mesh importer |
| [Inspector adapter](../Source/Rendering/InspectorRenderer.cpp) | Fixed font atlas and current draw-data submission | Custom texture IDs rejected; no general asset thumbnails or player UI |
| [Status and receipts](../Docs/STATUS.md) | Existing Mac geometry proof and texture-tool findings | No native Windows execution, physics, gameplay or final-art acceptance |
| [Surface library](../Assets/SurfaceLibrary/README.md) | 38 PNG textures, five source-art images, channel/binding provenance | Preserved inputs only; shaders/material behavior require deliberate porting |
| [World context](../References/WORLD_CONTEXT.md), [game basis](../../Docs/WORLD_BASIS.md), [companion standard](../../Docs/BIGARM_COMPANION_STANDARD.md) | Dry twilight setting; survival/crafting context; continuous world; BigARM true position and physical regroup | Old 2D/top-down/canyon priorities are superseded by Engine direction. Broader lore proposals are not accepted game canon. |

The preserved [Unity roadmap](../../Docs/ROADMAP.md) was read for game outcomes and risk, not adopted as the native-engine implementation sequence. No Unity code or scene is required to build the engine. The terminology discrepancy around survival biology remains a content question; a generic resource-pressure system does not need it resolved.

## Capability coverage

`Partial` means only the stated portion exists. `Missing` is based on inspected native targets/source and the current handoff. Milestones refer to the rewritten plan. CAP IDs are indexed by [the roadmap record](../Docs/ENGINE_ROADMAP.json), which is checked for coverage and dependency cycles.

| ID | Capability the game needs | Current evidence / gap | First implementation → maturity |
|---|---|---|---|
| CAP-01 | Reproducible build and platform delivery | Partial: source pins/build receipts; no portable player package or executed Windows build | M1 → M8 |
| CAP-02 | Input, windows and device lifecycle | Partial: window/mouse/keyboard inspector; game actions, controllers, remapping, focus/disconnect missing | M1/M2 → M7 |
| CAP-03 | Simulation clock, entities, transforms and commands | Missing: fixture data only; no fixed simulation or gameplay ownership | M1/M2 → M6 |
| CAP-04 | Stable identity and world coordinates | Proposed only: no runtime world/entity/save identity | M1 → M4 |
| CAP-05 | Shared resources, jobs and budgets | Partial: fixture replacement cleanup; no registry, worker ownership, admission/backpressure | M1 → M4/M7 |
| CAP-06 | Asset import/cook/dependency pipeline | Partial: shader build and source collection; mesh/skeleton/audio/document pipeline missing | M1/M2 → M7 |
| CAP-07 | Texture/material system | Research only: semantic mip issues measured, runtime loader absent | M1 → M3/M7 |
| CAP-08 | Frame passes, color, lighting and shadows | Partial: opaque fixture and simple light; explicit color/HDR/shadows/PBR missing | M1 → M7 |
| CAP-09 | Visibility, instancing and LOD | Missing: fixed draw list, no spatial visibility or asset LOD path | M3 → M4/M7 |
| CAP-10 | Sky, atmosphere, particles and effects | Missing: clear color only; low-sun readability and dust effects needed | M1 basic ambient → M6/M7 |
| CAP-11 | Collision and physical queries | Missing: no linked physics library or collision representation | M2 → M4/M6 |
| CAP-12 | Third-person motor and obstruction camera | Missing: inspection orbit is not character following or collision-aware | M2 → M6/M7 |
| CAP-13 | Skeletons, animation and skinning | Missing: no animated vertex/data format | M2 → M6/M7 |
| CAP-14 | Terrain generation and authored route constraints | Missing: bounded flat fixture only | M3 planning seam → M4/M7 |
| CAP-15 | Native rock recipes and generator | Missing: source textures transferred; native rock generation absent | M3 → M4/M7 |
| CAP-16 | Region streaming and multiple simulation anchors | Proposed only: no desired/ready/resident state owner | M4 → M5/M7 |
| CAP-17 | Durable state, save/recovery and migration | Missing: even inspection settings are not persisted | M1 documents → M2 snapshot → M4/M8 |
| CAP-18 | Local navigation, coarse routes and AI tasks | Missing: no pathfinding or companion state | M3 route contract → M5/M6 |
| CAP-19 | Interactions, cargo, pressure, combat and content rules | Missing: no game command/state model | M2 interaction seam → M5/M6 |
| CAP-20 | Audio engine and event routing | Missing: SDL starts video/events only | M2 cue → M5/M7 |
| CAP-21 | Player UI, settings, accessibility/localization | Missing: developer inspector only | M2 debug presentation → M5/M7 |
| CAP-22 | Practical authoring tools and shared game/editor runtime | Partial: inspect fixed fixture; no document/undo/asset/placement tools | M1 → M3/M7 |
| CAP-23 | Profiling, automated evidence and long-run validation | Partial: captures/counts/frame interval and focused tests; no system time/allocation budgets | Every milestone; broaden M4/M7 |
| CAP-24 | Package, diagnostics, save compatibility and release lifecycle | Missing: no install/package/player/crash-recovery distribution path | M1 path seam → M2 portable package → M8 |

## Cross-system risks

1. **Rendering is not the simulation boundary.** Invisible regions may still contain BigARM, pending changes or traversal knowledge. Rendering residency, collision residency, navigation availability and authoritative existence need separate state.
2. **The fixture can become an accidental architecture.** `Renderer::draw(FixtureState)` and logic in `Apps/Workbench/main.cpp` must be extracted when reused; adding every feature to those types would couple gameplay to the inspector. Keep one runtime consumed by workbench and player applications.
3. **Save formats cannot wait for “the world is finished.”** IDs, versions and minimal snapshots must precede generated placement and mutable interactions. Cache data and permanent deltas must have different invalidation rules.
4. **A third-person game exposes different defects.** Camera obstruction, silhouettes from below, foot contact, normal orientation, animation, texture repetition and LOD transitions need character-scale proof early.
5. **Libraries leave engine responsibilities.** Jolt does not own movement intent; Recast does not own travel through absent terrain; EnTT does not own save identity; ImGui does not supply an authoring document model; bgfx does not supply a completed rendering pipeline.
6. **No proof supports loading everything at once.** The surface collection's all-RGBA8 mip payload alone is roughly 218 MiB. CPU staging, collision, animation, nav tiles and duplicate ownership are additional costs. Instrument admission and residency before streamed scale.
7. **World generation must respect traversal.** Agent widths, slope/step classes, landmark exclusions and route corridors must constrain geometry/placement. Navigation cannot repair every impassable generated region after the fact.
8. **Mac success cannot silently become Windows success.** The existing source selects D3D11, not D3D12. Test that actual path before optional backend expansion. A headless build does not establish native GPU sampling or physical controller behavior.
9. **Content production is a real dependency.** Skeletons, clips, audio and encounter authoring are absent from the transferred surface library. Use legal, bounded technical proxies first; record provenance and do not treat missing art as an engine failure.
10. **Completeness is not a feature wish list.** Water/ocean simulation, canyons, multiplayer, vehicles, cloth, general destruction, ray tracing and virtualized geometry are not current prerequisites. Their triggers belong in the plan rather than in the immediate implementation queue.

## Recommended program boundary

Near-term done: a reusable, inspectable outdoor runtime, a collision-aware animated third-person proxy, a native rock workbench and a portable technical player build. Vertical-slice done: a streamed expedition with persistent consequences and physical BigARM regroup. Production-engine done: the intended content can be authored, validated, packaged and recovered on declared Windows hardware with measured budgets. Shipping the actual game additionally needs accepted content and a separate release decision.

The [research update](./ENGINE_ARCHITECTURE_RESEARCH.md) evaluates supporting libraries and the [plan audit](../Docs/ENGINE_PLAN_AUDIT.md) records the rewrite. No new runtime capability was implemented by this audit.
