# World Creator Architecture Plan

Status: implementation-ready proposal awaiting user review

Plan date: 2026-08-23

Authority: planning and documentation only

Runtime change gate: closed until the user approves this plan and [WORLD_CREATOR_CHARTER.md](./WORLD_CREATOR_CHARTER.md)

## 1. Done, scope, source of truth, proof, and stop

### Done for this planning task

This task is done when the repository contains:

1. a read-only audit of the live generator and its documentation;
2. a World Creator Charter capturing the established product vision;
3. a chosen multi-scale architecture compared against viable alternatives;
4. an explicit keep/evolve/replace migration map;
5. an implementation-ready first production vertical slice;
6. a visual, deterministic, streaming, persistence, and performance proof matrix;
7. an explicit stop for user review before runtime code changes.

### In scope

- Current elevated top-down 3D production world path.
- Deterministic world identity and coordinate-driven generation.
- Terrain, canyon systems, landform features, rocks, surface response, near/mid/far representation, traversal semantics, stable identity, streaming, and persistence seams.
- Contracts required by later ruin, dig-site, landmark, town, city, and encounter generators.
- A staged replacement strategy that can retain proven infrastructure.

### Out of scope

- Runtime implementation, Unity assets, scenes, settings, packages, builder execution, tests, captures, or profiler runs.
- Final coordinate-region definitions or lore canon not yet supplied by the user.
- Finished settlement/site generators, destructible terrain, caves as a universal system, or gameplay-density systems.
- Visual or performance claims based solely on source inspection.

### Sources of truth

1. The user's stated creative direction and approval.
2. [WORLD_CREATOR_CHARTER.md](./WORLD_CREATOR_CHARTER.md) after approval.
3. [WORLD_BASIS.md](./WORLD_BASIS.md) for setting rules, subject to its identified obsolete 2D representation sentence.
4. [WORLD_SYSTEMS_STANDARD.md](./WORLD_SYSTEMS_STANDARD.md) for deterministic identity, streaming, authored constraints, and delta persistence.
5. Live project code and serialized settings for current implementation truth.
6. The live working-tree files `Docs/TOP_DOWN_3D_LANDSCAPE_BENCHMARK.md` and `Docs/TOP_DOWN_3D_LANDSCAPE_IMPLEMENTATION_PLAN.md` as the current landscape lane's tactical baseline, not the new long-term authority. They were untracked during this audit and are therefore evidence from the live checkout, not committed sources.
7. External primary references listed in section 15 as design evidence, not proof of this project.

### Stop condition

Stop after the two planning documents are verified. Do not change runtime code, Unity assets, scenes, settings, packages, existing dirty documentation, or run Unity. Implementation begins only after the user reviews and approves the charter, selected architecture, slice boundary, and provisional proof target.

## 2. Live repository and evidence boundary

Audit baseline:

- Repository: `/Users/worldbuilder/Desktop/Booter & BigARM`
- Branch: `main`, nine commits ahead of `origin/main`
- Inspected HEAD: `e1bf5152181a2d5ced44784fa06fa28f26130f24`
- Unity: `6000.4.0f1`
- Render pipeline: URP 17.4.0
- Production scene: `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`
- Unity GUI lock: present during audit
- Worktree: heavily dirty, including the landscape lane, scene, tests, assets, docs, and untracked current generator files

Consequences:

- Existing changes are user-owned and were read only.
- No Unity import, batchmode, EditMode, PlayMode, visual, gameplay, or profiler proof was run.
- Current code findings describe the live working tree, not necessarily the checked-in HEAD.
- Existing reports of prior passing tests are historical documentation evidence, not revalidated results from this task.
- `Docs/DOCS_INDEX.md` is already dirty, so routing these new documents there is intentionally deferred rather than overlapping user work.

## 3. Current system audit

### 3.1 Present authority chain

The live path is:

```text
TopDown3DWorldSettings
        |
        v
TopDown3DWorldGenerator.Sample(world XZ)
        |-- scalar height + derived normal/material/affordance-like weights
        |-- coarse hashed feature ID
        |
        +--> TopDown3DChunkMeshBuilder --> colliding 18 m near chunks
        +--> TopDown3DFarLandscape --> non-colliding middle/far square rings
        +--> safe spawn / height consumers
        +--> TopDown3DNaturalObjectPlanner
                |-- cell-scattered cosmetic layers
                |-- TopDown3DRockFormationPlanner
                |-- TopDown3DResourceNodePlanner
                +-- dust deposition/decorators
```

`TopDown3DProceduralWorld` owns synchronous GameObject realization, required-chunk sets, pending queues, unload hysteresis, decoration radius, far-landscape refresh, and a hard 2 ms pending-work guardrail.

### 3.2 Terrain kernel

`TopDown3DWorldGenerator` is deterministic, instance-owned, sampled in world space, and versioned. Those are sound decisions. Its present macro geography is nevertheless a scalar heightfield assembled from fractal/value noise, domain warp, thresholded mesa masks, quantized terraces, local relief, and fixed formulas. “Drainage” is formed from rotated sine bands warped by noise; it is not a connected network or hydrologic plan. Sparse sand traps use admitted elliptical cells. The regional feature ID hashes coarse region coordinates but does not identify a semantic landform or system.

This kernel can make seamless rolling terrain and material signals. It cannot by itself author a planet-wide canyon grammar, branching networks, long causal ridges, true vertical or overhanging features, site relationships, or view-scale composition. Continued tuning would improve a prototype but would not remove this ceiling.

### 3.3 Chunks and streaming

The current 18 m chunk is correctly treated as a near-field streaming and realization unit rather than a geology unit. Required sets, distance ordering, immediate-load radius, unload padding, separate decoration radius, and instrumentation are useful foundations.

Current realization is synchronous on the main thread. Each terrain build allocates new arrays, a mesh, a GameObject, renderer, and collider. Decoration planning and realization share the same 2 ms loop and can still execute a whole expensive item before the elapsed-time check. A runtime-only incrementing `GenerationToken` is lifecycle bookkeeping, not stable world identity.

### 3.4 Middle and far landscape

`TopDown3DFarLandscape` samples the same world kernel, which preserves broad height continuity. It rebuilds two complete square-ring meshes whenever an anchor cell changes, allocates new arrays/meshes/GameObjects, and destroys the previous root. It has no stable feature proxies, HLOD cache, morph seam, asynchronous build, horizon-composition plan, or feature-specific near/far identity beyond sharing scalar samples.

The shared-world-truth principle should be kept; this particular rebuild mechanism should be replaced.

### 3.5 Natural objects and rock formations

The natural-object system has valuable deterministic planning structures, global-cell candidates, neighbor competition, root-cell ownership, generation namespaces, stable IDs, exclusion rules, sorted outputs, baked mesh families, LODs, and a division between plans and decorators.

Its composition is still local and statistical. Cosmetic layers sample separate noise fields and density factors. Physical rock roots are admitted by tier-specific cells and local surface suitability. Children form deterministic branching clusters through overlap/contact attempts. This is substantially stronger than independent random props, but it remains an object-scatter/cluster system. It does not derive formations from shared strata, faults, canyon genealogy, erosion direction, regional silhouette goals, or a novelty memory. Its many scalar settings have become a centralized tuning surface without a higher-order spatial grammar.

The editor-baked rock library and deterministic plan/identity seams are reusable. Root distribution, formation grammar, and relationship to macro geology need evolution or replacement.

### 3.6 Dust, materials, and surface sample

The compact `TopDown3DWorldSurfaceSample` gives terrain, shader colors, placement, and dust a common query. This is a good seam, but it mixes render packing with world semantics and is derived only from a local scalar sample.

Dust and surface clustering respond to height, slope, shelter approximations, noise, material weights, and placed rocks. The causal idea is good. These systems should eventually consume explicit wind exposure, deposition basins, strata, occlusion, and feature plans rather than reconstructing context locally. Existing material assets and shader investment remain useful during migration, but the packed vertex-color contract should become a representation adapter rather than the canonical world-data schema.

### 3.7 Persistence and saved locations

The resource lane demonstrates the right delta model: stable generated resource IDs plus versioned, sorted mutable deltas. Generation-version namespaces are isolated so resource changes need not reshuffle rocks or cosmetics.

The broader game snapshot currently records a snapshot version, world seed, Booter position, inventory, and BigARM state. It does not yet record the complete world-plan/topology version, coordinate-region definition version, saved-place records, or the resource-world snapshot. That is sufficient for the current prototype but not for permanent marked locations in an evolving generated world.

### 3.8 Tests and validators

Current source includes useful contracts for deterministic normalized surface samples, range/continuity, exact adjacent-chunk surface weights, stable regional IDs, deterministic root ownership, cross-border spacing, spawn exclusion, rock topology, separate physical/cosmetic versions, baked LOD assets, resource delta round trips, and deferred decoration at startup.

Missing production-proof categories include:

- connected cross-region canyon/system topology;
- planning-cell halo and ownership agreement;
- topology-version isolation from cosmetic versions;
- saved-place reconstruction and migration behavior;
- multi-representation feature agreement;
- asynchronous cancellation and stale-result rejection;
- traversal-graph connectivity and minimum-width constraints;
- site reservations and terrain-adaptation contracts;
- novelty/silhouette/composition metrics;
- bounded generation time, allocation, cache, memory, and Development Player frame-time evidence;
- fixed-camera and human walk-through acceptance.

### 3.9 Documentation audit

- `WORLD_SYSTEMS_STANDARD.md` remains a sound system baseline and should be kept.
- `WORLD_BASIS.md` remains setting authority but contains an obsolete 2D representation sentence that conflicts with the 3D live project.
- `WORLD_GEN_RESEARCH_SUMMARY.md` and `WORLD_GEN_REFERENCE_NOTES.md` contain useful data-first, hybrid-authored, intentional-emptiness lessons, but are explicitly provisional and partly anchored to the former 2D approach.
- `TOP_DOWN_3D_LANDSCAPE_IMPLEMENTATION_PLAN.md` successfully describes the present representative landscape family and its proof boundary. It should be closed as a tactical predecessor after its dirty work is integrated, not stretched into the new engine architecture.
- `TOP_DOWN_3D_LANDSCAPE_BENCHMARK.md` provides useful camera, scale-band, asset, and proof guardrails. Its current 18 m/24-quad mesh contract is a measured baseline, not a permanent World Creator law.

## 4. Keep, evolve, replace, and retire

| Existing technology or decision | Decision | Reason and destination |
| --- | --- | --- |
| World seed + explicit generation versions | Keep and expand | Becomes `WorldIdentity` with separate topology, landform, material, decoration, resource, and site domains. |
| World-coordinate deterministic sampling | Keep | Required for seams, queries, and reconstruction. Sampling consumes compiled plans rather than inventing macro geography locally. |
| Chunk as streaming/realization unit | Keep | Planning cells and feature ownership operate at larger scales; render chunks stay replaceable. |
| Instance-owned generator authority | Evolve | Replace one monolithic sampler with a `WorldCreator` facade over explicit planners, plan cache, query service, and representation compiler. |
| `TopDown3DWorldSurfaceSample` seam | Evolve | Split canonical semantic surface/volume/affordance data from renderer-specific packed channels. |
| `TopDown3DGeologyProfile` scalar settings | Replace | Introduce authored coordinate regions, geologic province definitions, strata families, landform grammars, and composition profiles. |
| Layered noise macro terrain | Replace as macro author | Retain noise only for continuous fields, warp, local irregularity, selection, and materials. |
| Sine-band drainage | Replace | Use deterministic network/graph planning with canonical cross-cell ownership and terrain-conforming canyon profiles. |
| Elliptical sand-trap stamps | Replace | Deposits derive from basins, shelter, wind, slope, canyon history, and feature geometry. |
| Scalar heightfield for all topology | Evolve into hybrid base | Keep a 2.5D ground/collision base; add bounded feature meshes or SDF-compiled volumes where vertical silhouettes, overhangs, arches, or cave mouths justify them. |
| Synchronous chunk mesh allocation | Replace incrementally | Data jobs/tasks, pooled buffers, cancellation tokens, immutable build results, main-thread integration budget. |
| Required-chunk sets, priority order, unload padding | Keep and generalize | Becomes representation request scheduling with near/mid/far priorities and hysteresis. |
| 2 ms per-frame work guardrail and profiler markers | Keep provisionally | Split planning, compilation, upload, physics, and decoration markers; final budget awaits target-hardware proof. |
| Destroy/rebuild far square rings | Replace | Clipmap or tiled HLOD representation cache with stable feature proxies and transition rules. |
| Deterministic cell candidates and neighbor competition | Keep as a low-level primitive | Useful within a constrained plan; no longer owns regional composition. |
| Rock formation plan structs and stable root keys | Evolve | Consume feature genealogy, strata, orientation, silhouette grammar, novelty score, and landform reservations. |
| Editor-baked rock mesh families and LOD assets | Keep as migration assets | Expand vocabulary only after perceptual audits; the World Creator must hide kit repetition through grammar, not merely add assets. |
| Runtime native CSG avoidance | Keep | Any heavy fusion/boolean/SDF compilation is editor/offline or bounded/cacheable; shipped world must degrade safely without editor plugins. |
| Natural-object monolithic settings asset | Split | Streaming/performance settings, world identity, geology, materials, landforms, and content catalogs get separate authorities. |
| Vertex-color material packing | Keep temporarily, then adapt | Preserve current shader during early topology cutover; representation compiler owns packing. |
| Resource stable IDs and delta-only world state | Keep and generalize | Becomes the model for all mutable generated objects and saved places. |
| Runtime incrementing chunk `GenerationToken` | Keep only for stale-build lifecycle | Never use as geographic or save identity. |
| Current tactical landscape plan | Retire after integration record | Preserve as historical implementation evidence; this plan becomes the forward authority after approval. |

No existing runtime file is deleted at the start. “Replace” means migrate consumers to a proven canonical path, validate, then remove the obsolete authority in a separately reviewed batch.

## 5. Architecture alternatives

| Option | Strengths | Weaknesses | Decision |
| --- | --- | --- | --- |
| A. Enhanced scalar heightfield | Lowest migration cost; easy collision; fast sampling; current shaders/chunks remain useful | Cannot naturally represent true canyon walls, arches, undercuts, caves, stacked surfaces, or distinctive silhouette volumes; risks endless noise tuning | Reject as final architecture; retain as broad-ground component |
| B. Fully volumetric voxel/SDF world | Arbitrary topology; caves/destruction; unified volume queries | High memory, mesh extraction, collision, persistence, LOD, tooling, and CPU complexity; poor default fit for mid-range target and mostly surface gameplay | Reject as universal representation; permit bounded SDF feature compilation |
| C. Offline-generated finite authored continent | Strong global composition and art control; heavy processes can be baked | Conflicts with coordinate-driven effectively infinite play and on-demand seeds; large storage; saved-place portability is awkward | Reject as world model; reuse offline tools for samples, evaluation, and caches |
| D. Hybrid causal world compiler | Infinite deterministic plans; heightfield efficiency for ground; bounded volumetric features; shared semantics; strong authoring control; scalable representations | More up-front architecture and tooling; cross-cell planning and version migration require discipline | Select |

## 6. Selected architecture: hybrid causal world compiler

### 6.1 Canonical authority flow

```text
WorldIdentity(seed + version manifest)
        |
        v
WorldCoordinateContext (continuous authored fields)
        |
        v
MacroRegionPlan (province + structural history + canonical ownership/halo)
        |
        +--> CanyonSystemPlan / RidgeSystemPlan / BasinSystemPlan / RouteSkeleton
        |
        v
LandformFeaturePlans (walls, shelves, spires, outcrops, deposits, negative spaces)
        |
        v
Composition + Constraint Solver
        |-- connectivity / clearance / site reservations
        |-- silhouette novelty / density rhythm / view goals
        |
        v
WorldQueryService
        |-- surface       |-- volume       |-- affordance       |-- feature identity
        |
        v
Representation Compiler + Cache
        |-- near render/collision
        |-- mid HLOD feature/terrain tiles
        |-- far horizon/province representation
        +-- later site, map-record, AI, audio, and gameplay consumers
```

World truth is immutable generated data plus separately stored runtime deltas. Unity GameObjects are temporary representations.

### 6.2 Scale model

Scale names define responsibilities, not visible grid borders. Exact sizes are profile data and may change after profiling.

| Scale | Provisional span | Owns |
| --- | ---: | --- |
| Coordinate field | continuous | authored regional influences and transitions |
| Province cell | 2,304 m | geologic family, elevation regime, dominant structural direction, history parameters |
| System-planning cell | 576 m | canyon/ridge/basin/route graphs, feature reservations, cross-cell ports |
| Landform cell | 144 m | walls, shelves, outcrops, spires, local negative space, composition candidates |
| Near realization chunk | current 18 m baseline | high-resolution ground mesh, collision, close features, gameplay representations |
| Material/detail domain | continuous / sub-metre | surface response, decals, shader microdetail, dust, fractures |

These powers-of-two relationships align with the existing 18 m chunk and current 288–1,152 m regional benchmark while creating explicit planning layers. They are proposed defaults, not canon.

### 6.3 Coordinate context

`WorldCoordinateContext` samples continuous fields from authored coordinate-region definitions. Each definition supplies influence shapes/falloffs and parameter ranges; overlapping influences blend or resolve by declared rules. Example outputs:

- province family weights;
- base elevation and relief regime;
- structural/fault orientation and anisotropy;
- canyon-network density, branching, depth, width, and age;
- strata family, hardness, folding, fracture, and color tendencies;
- weathering, sediment, prevailing wind, and exposure;
- danger/history/occupation potential for later systems;
- landmark cadence and visual-density targets.

Grid cells only index/caches results. Coordinate character is continuous.

### 6.4 Macro planning and seams

Each planning cell is built with a deterministic halo. Cross-boundary systems use canonical feature ownership:

1. Candidate ports and anchors are derived from the shared boundary key.
2. The lowest stable owner key owns each cross-cell feature.
3. Neighbor plans import the owner's immutable feature reference.
4. Outputs are sorted before IDs/ordinals are assigned.
5. Tests build cells in different orders and assert byte-equivalent plans.

No neighbor runtime load is required merely to know a boundary. A plan may compute its bounded halo or load a deterministic cached neighbor plan.

### 6.5 Canyon systems

The first true macro generator is a canyon-system planner, not another height modifier. It emits a directed graph with source/branch/confluence/spine/termination nodes, widths, depths, strata interactions, shelf levels, traversal ports, and stable segment IDs.

The canyon-form compiler converts graph segments into terrain operations:

- broad heightfield incision and shoulders;
- profiles that vary along arc length rather than repeat a fixed cross-section;
- walls/shelves from strata and age;
- feature-volume requests for undercuts, arches, cave mouths, or vertical hero sections;
- talus/deposit zones and wind shelter;
- route and crossing opportunities;
- reserved negative spaces for future sites and encounters.

This is an abstract geologic grammar, not a literal real-time fluid simulation. Expensive erosion may be an editor evaluation tool, never the runtime prerequisite for loading a chunk.

### 6.6 Landform grammar and perceptual novelty

Every feature plan records genealogy: which province/system/segment caused it, which strata it exposes, which forces shaped it, which route/site role it supports, and its stable parent feature.

Candidate selection uses a bounded novelty index containing compact fingerprints rather than remembered geometry. Fingerprints include horizon profile, footprint/aspect, height rhythm, branching topology, dominant orientation, member count/tier pattern, material distribution, and spatial relation to nearby features. The solver rejects candidates that are too similar inside a configurable memory radius and relaxes deterministically if constraints become impossible.

Composition operates on bounded view cells and route approaches. It scores:

- readable foreground/middle/horizon hierarchy;
- at least one navigable opening;
- focal anchor versus supporting forms;
- density rhythm and intentional rest;
- occlusion and reveal sequence;
- repeated silhouettes/spacing;
- route, spawn, and site clearances;
- implausible intersections or unsupported features.

This scorer is a quality gate and debug tool, not a claim that aesthetics can be fully automated.

### 6.7 Surface, volume, and affordance queries

Canonical queries are split:

- `SurfaceSample`: height(s), normal, strata/material semantics, exposure, weathering, deposit, wind, feature IDs.
- `VolumeSample`: solid/empty classification and nearest feature/feature-volume distance for bounded non-heightfield forms.
- `AffordanceSample`: walkability, cost, corridor, ledge, wall, cover, shelter, overlook, choke, arena potential, site support, and semantic tags.
- `FeatureQuery`: stable feature plan, genealogy, bounds, representation tier, reservations, and persistence domain.

Render packing, collider meshes, nav data, AI queries, and later generator inputs are adapters over these services.

### 6.8 Later site generators

Ruin, dig-site, landmark, outpost, town, and city generators enter after landscape planning. A site recipe declares:

- required and preferred affordances;
- minimum/maximum footprint and vertical range;
- approach, route, sightline, defense, resource, and storytelling constraints;
- terrain operations it is allowed to request;
- modules/grammars and semantic beats;
- exclusion and coexistence rules;
- stable identity/persistence domain.

The site solver selects a compatible reservation, adapts its layout to landforms and routes, requests bounded terrain integration, and emits its own plan. It does not paste a completed flat prefab or directly deform loaded chunk meshes.

### 6.9 Streaming and performance model

Planning, compilation, and Unity-object integration are separate stages:

1. Request immutable plan/representation keys around the streaming target.
2. Serve cache hits immediately.
3. Build missing pure-data plans and mesh buffers off the main thread where Unity API restrictions allow.
4. Cancel obsolete requests and reject results whose request token no longer matches.
5. Integrate bounded mesh/renderer/physics work on the main thread under measured budgets.
6. Pool buffers and representation objects.
7. Retain near collision only where gameplay needs it.
8. Evict caches by memory budget and reproducible priority, not arbitrary destruction.

The visual density strategy is geometry where silhouette matters, shader/material detail where parallax does not, and proxies/HLOD where distance hides local structure.

## 7. Planned code and data boundaries

These are proposed implementation owners; no files are created by this planning task.

```text
Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/
  Identity/
    WorldIdentity.cs
    WorldVersionManifest.cs
    WorldFeatureId.cs
    WorldSeedNamespace.cs
  Coordinates/
    WorldCoordinateContext.cs
    CoordinateRegionDefinition.cs
    CoordinateRegionCatalog.cs
  Planning/
    WorldPlanKey.cs
    MacroRegionPlan.cs
    WorldPlanCache.cs
    IWorldPlanGenerator.cs
    WorldPlanningScheduler.cs
  Geology/
    GeologicProvinceDefinition.cs
    StrataFamilyDefinition.cs
    CanyonSystemPlan.cs
    CanyonSystemPlanner.cs
    CanyonFormCompiler.cs
    LandformFeaturePlan.cs
    LandformGrammar.cs
  Composition/
    WorldConstraintSet.cs
    WorldReservation.cs
    LandscapeFingerprint.cs
    LandscapeNoveltyIndex.cs
    LandscapeCompositionScorer.cs
  Query/
    WorldSurfaceSample.cs
    WorldVolumeSample.cs
    WorldAffordanceSample.cs
    IWorldQueryService.cs
  Representation/
    WorldRepresentationKey.cs
    WorldRepresentationScheduler.cs
    NearTerrainCompiler.cs
    MidLandscapeCompiler.cs
    FarLandscapeCompiler.cs
    WorldRepresentationCache.cs
  Streaming/
    WorldStreamingController.cs
    WorldStreamingBudget.cs
  Persistence/
    SavedPlaceRecord.cs
    WorldDeltaRecord.cs
    WorldPersistenceManifest.cs

Assets/_Project/Scripts/Editor/TopDown3D/WorldCreator/
  WorldCreatorDebugWindow.cs
  WorldPlanInspector.cs
  LandscapeProofCapture.cs
  LandscapeFingerprintReport.cs
  WorldCreatorAssetValidator.cs

Assets/_Project/Settings/WorldCreator/
  CoordinateRegionCatalog.asset
  GeologicProvinceCatalog.asset
  StrataFamilyCatalog.asset
  LandformGrammarCatalog.asset
  WorldCreatorPerformanceProfile.asset
```

All production runtime types remain inside `BooterBigArm.TopDown3D.Runtime` unless profiling or dependency pressure justifies a separate asmdef. Pure planner types should minimize `UnityEngine.Object` dependencies so they can be tested and scheduled independently.

## 8. Migration sequence

Each batch is a separately reviewable change. The production scene remains Unity-compatible and the old path remains available until the cutover batch is proven.

### Batch 0 — Approval and baseline capture

Scope:

- User approves charter, architecture D, slice, and provisional proof assumptions.
- Resolve or isolate the current dirty landscape lane before implementation.
- Record exact branch/SHA, Unity lock, package/editor versions, serialized production settings, and current visual/performance evidence.
- Decide the target mid-range hardware definition.

Proof: clean task ownership, documented authority, baseline captures/profile or explicitly recorded unavailable evidence.

Stop: any overlapping unowned work or unresolved product decision.

### Batch 1 — Identity and pure plan contracts

Scope:

- Add world/version manifest, namespaced seeds, stable feature IDs, plan keys, immutable plan records, ownership/halo rules, and cache interface.
- Add saved-place record schema and migration rejection rules without yet changing the live save file.

Proof: pure deterministic tests, different build-order equivalence, ID uniqueness, namespace isolation, serialization round trip.

Rollback: remove unused new assembly files; no production consumer changed.

### Batch 2 — Coordinate context and authored geology data

Scope:

- Add coordinate-region blending and province/strata catalogs.
- Provide debug sampling and plan inspection.
- Configure one provisional proof region without encoding unapproved global lore.

Proof: continuity across influence boundaries, deterministic samples, bounded ranges, asset validation, coordinate transect debug output.

Rollback: new data remains unused by production.

### Batch 3 — Canyon-system planner

Scope:

- Generate stable cross-cell canyon graphs, ports, hierarchy, widths/depths, shelves, and traversal reservations.
- No render cutover.

Proof: connectivity, acyclic/declared-cycle rules, boundary agreement, minimum widths, build-order equivalence, bounded planning cost, visual graph overlays.

Stop: graph seams, unbounded neighbor dependency, or no deterministic ownership.

### Batch 4 — Hybrid terrain query prototype behind a dormant adapter

Scope:

- Compile canyon/province plans into broad heightfield ground plus bounded landform-feature requests.
- Implement semantic surface/volume/affordance queries.
- Keep old production terrain active.

Proof: query determinism, seam equality, finite/range tests, route connectivity, feature identity, sampled comparison panels generated through an isolated editor tool.

Rollback: dormant adapter removed without production impact.

### Batch 5 — Representation compiler and streaming scheduler

Scope:

- Add pooled near buffers, asynchronous/cancellable data builds, main-thread integration queue, cache budgets, mid/far tiles/proxies, and instrumentation.
- Prove with non-production fixtures before scene cutover.

Proof: cancellation/stale rejection, bounded allocations after warmup, cache hit/eviction, near/mid/far identity agreement, no colliders outside near policy, stress transect.

Stop: main-thread spikes or memory growth exceed provisional gates.

### Batch 6 — Atomic production terrain cutover

Scope:

- Route production terrain, spawn, ground queries, collision, materials, and far representation through `IWorldQueryService` and the representation scheduler.
- Preserve a short-lived explicit rollback switch only for the batch; do not ship dual authorities.
- Bump topology version.

Proof: no live consumer of old macro authority, deterministic and seam suites, production-scene validator, fixed-camera old/new evidence, traversal review, Development Player profile.

Rollback: revert the cutover commit as a unit if gates fail.

### Batch 7 — Geological rock-formation evolution

Scope:

- Keep baked families and LODs.
- Replace cell-density root authorship with landform reservations, strata orientation, genealogy, composition goals, and novelty fingerprints.
- Retain stable delta-compatible IDs through the new version domain.

Proof: formation identity/reload, boundary ownership, contact/collision, silhouette fingerprint report, density rhythm, fixed-camera review, profiler.

Stop: recognizable nearby repetition or broken traversal clearances.

### Batch 8 — Material, dust, and surface integration

Scope:

- Feed shaders/deposition from semantic strata, erosion, wind, shelter, and deposit data.
- Migrate vertex packing behind the representation adapter.

Proof: material continuity, no seams, bounded shader cost, near/mid/far cohesion, fixed-lighting comparison, player profile.

Rollback: retain adapter to current shader packing.

### Batch 9 — Persistence and saved places

Scope:

- Add world manifest, saved-place coordinates/name/feature references, generalized deltas, and resource snapshot to game save.
- Define load rejection/migration for incompatible topology versions.

Proof: save/reload/cross-chunk travel, depleted resources, saved-place reconstruction, wrong-version atomic rejection, migration fixtures, no whole-chunk geometry in save.

Stop: a marked place silently resolves to different topology.

### Batch 10 — Vertical-slice proof and old-path removal

Scope:

- Run the complete proof province/transects.
- Remove obsolete old terrain/far/planner authorities only after source and runtime consumers are absent.
- Update canonical docs and routing in a clean owned lane.

Proof: section 10 matrix, user visual acceptance, controlled target profile, task-only diff, no duplicate authority.

Stop: any proof gap remains labeled; do not claim completion or broaden into site generators.

## 9. First production vertical slice

### Slice name

**The Fractured Transect** — a production-path proof of one coordinate/geology family, not a throwaway demo and not final lore canon.

### World extent and exploration path

- Deterministic architecture remains unbounded.
- Proof envelope: 4,608 m by 4,608 m (two provisional province cells per axis).
- Required walked/profiled transect: at least 3,000 continuous metres across province and system-cell boundaries.
- Evidence set: three seeds and three coordinate transects per seed, including one diagonal boundary crossing.
- Production camera and character scale remain unchanged unless the user separately approves a gameplay/camera change.

### Required generated content

- one branching canyon system with a main spine, tributaries, confluences, changing profiles, and cross-cell continuity;
- broad elevation regime, at least three readable depth/shelf levels, basins, ridges, scarps, and quiet open ground;
- one bounded vertical/overhang-capable feature family proving the hybrid path;
- geology-driven rock formations with hierarchical silhouette and novelty constraints;
- explicit deposits, talus, strata/material response, wind exposure, and shelter;
- a connected primary traversal route plus optional overlooks, chokepoints, crossings, cover, and future-site reservations;
- stable near/mid/far representations and feature IDs;
- debug overlays for plans, ownership, routes, affordances, fingerprints, and budgets.

### Deliberately absent

No finished ruin, dig site, town, city, quest, encounter population, vegetation, world-map UI, or global region catalog. Later systems receive reservations and affordances only.

### Visual composition shots

For every seed, capture the same declared camera recipe at:

1. canyon approach/reveal;
2. canyon interior with shelf/depth hierarchy;
3. overlook with near/mid/far agreement;
4. quiet negative-space basin;
5. rock/landform silhouette cluster;
6. coordinate transition;
7. streaming seam/representation transition stress point.

Each panel includes seed, coordinate, topology version, camera transform, quality preset, build identifier, and debug-overlay companion where useful.

## 10. Proof matrix and provisional acceptance gates

| Claim | Required evidence | Provisional gate |
| --- | --- | --- |
| Same world reconstructs | Pure tests + serialized plan hashes + unload/reload | Identical seed/version/coordinates produce identical canonical plans and IDs independent of request order |
| Canyon systems cross cells | Graph tests + overlays + seam samples | Boundary ports/segments and compiled surfaces agree exactly; required route remains connected |
| Coordinate changes are continuous | Transect sampling + captures | No unintentional parameter or visual discontinuity at index cells or region influence boundaries |
| Chunks do not author geology | Source ownership scan + plan inspector | All macro/system features trace to plans whose scale exceeds realization chunks |
| Saved locations remain real | Save/load/migration fixtures | Saved seed, world manifest, coordinate, and stable feature references reconstruct or fail/migrate explicitly |
| Near/mid/far show one place | Feature-ID overlay + fixed captures | Same stable features persist with bounded transition error and no visible hole, overlap, or identity swap |
| Traversal remains playable | Affordance/graph tests + user walk-through | Primary transect connected; configured clearances/slopes respected; no required route blocked by decoration |
| No obvious cookie-cutter repetition | Fingerprint report + panel covering nine seed-and-transect cases + user review | No materially similar macro/meso fingerprint within provisional 1,500 m memory radius; deterministic relaxation events reported |
| Views feel composed | Scorer report + fixed-camera review | No hard violations; focal hierarchy, navigable opening, density rhythm, and negative-space shots pass user review |
| Streaming work is bounded | Profiler markers + stress travel | Main-thread world integration remains at or below provisional 2 ms budget except declared initialization gate; no recurring GC after warmup |
| Rendering fits target | Development Player profile | Final threshold set in Batch 0; collect average, 1% low, CPU/GPU frame time, spikes, draw calls, triangles, memory, allocations |
| Visual target is achieved | Production-scene captures + hands-on review | User accepts the landscape as compelling to walk through and not recognizably procedural |

The 1,500 m novelty radius, 4,608 m envelope, 3,000 m transect, and 2 ms integration budget are proposed engineering defaults. They can be changed at review without changing the selected architecture.

## 11. Performance budgets to instrument

Before implementation, the project needs an agreed hardware target and output resolution. Until then, collect rather than claim:

- total CPU/GPU frame time and 1% low;
- plan generation time by scale and cache state;
- mesh/feature compilation time off-thread and integration time on-thread;
- main-thread upload/renderer/physics cost;
- managed and native allocations after warmup;
- resident plan, mesh, collider, texture, and GameObject memory;
- cache hit/miss/eviction/cancellation counts;
- loaded near/mid/far representation counts;
- renderers, batches, draw calls, triangles/vertices, shadow casters;
- collider count and physics step cost;
- worst streaming-boundary spike during sustained traversal.

Quality scalability must reduce representation cost without changing canonical feature identity or traversal topology. Candidate controls include far distance, proxy resolution, material detail, shadow range, dust density, and cosmetic realization radius—not canyon existence or saved-place geography.

## 12. Risk register

| Risk | Impact | Mitigation / gate |
| --- | --- | --- |
| Architecture grows into an engine rewrite | Delivery stalls | Keep production assembly, Unity/URP path, and bounded slice; add only interfaces needed by the slice |
| Cross-cell plans become recursively global | Infinite work/deadlocks | Canonical boundary keys, fixed halos, owner rules, bounded graph spans, deterministic termination tests |
| Hybrid features create terrain/collider seams | Traversal and visual failure | One query authority, signed operations, transition skirts/morphs, exact seam fixtures, near collision proof |
| Async work uses Unity APIs or stale results | Crashes/corruption | Pure data jobs, immutable results, request tokens, cancellation/stale-result tests, main-thread integration only |
| Novelty solver becomes expensive or unsatisfiable | Hitches or nondeterminism | Compact fingerprints, bounded neighborhood/candidates, deterministic relaxation ladder, report every relaxation |
| Procedural scoring produces technically varied ugliness | Missed visual target | Authored grammars/samples, fixed-camera panels, user review remains final visual authority |
| Finite asset library remains recognizable | Cookie-cutter silhouettes | Genealogy-driven composition, mesh/material variation where valuable, novelty fingerprints, expand assets only from evidence |
| Version bumps invalidate saved places | Player trust loss | Version manifest, topology pin/migration policy, explicit rejection, stable records, fixtures before cutover |
| Settings become another monolithic tuning asset | Fragile iteration | Split data ownership and validators; expose derived debug context rather than hundreds of unrelated sliders |
| Far landscape diverges from gameplay world | False landmarks/navigation | Shared plans and stable feature IDs; representation-error tests and overlay captures |
| Performance target stays undefined | Unprovable “mid computer” claim | Batch 0 requires hardware/resolution/quality decision; all earlier budgets remain provisional |
| Current dirty landscape lane is overwritten | Loss of user work | Re-audit ownership at implementation start; isolate/stage only task files; atomic cutover after integration |
| Canon drift from Arc & Dust reference | Wrong world encoded | Lorekeeper reference-only boundary; proposals require user acceptance into game docs |

## 13. Product decisions reserved for review

The architecture can begin with defaults, but the following decisions remain user-owned:

1. Approve or revise the charter's definition of perceptual uniqueness.
2. Approve hybrid architecture D and bounded volumetric features instead of universal voxels.
3. Approve **The Fractured Transect** as the first slice and its proposed proof envelope.
4. Choose the first visual geology/coordinate family; the plan will otherwise use a non-canon fractured red-canyon proof profile.
5. Define “mid-range computer,” target resolution, quality preset, and desired frame-rate threshold.
6. Decide how old saved worlds behave after topology-version changes: pin, migrate when supported, or declare incompatible during development.

None blocks review of this plan. All must be resolved no later than the implementation batch that consumes the decision.

## 14. Review checkpoint

At this checkpoint:

- the current generator and documentation have been audited read-only;
- the World Creator Charter is drafted;
- architecture alternatives are compared and the hybrid causal compiler is selected;
- keep/evolve/replace decisions are explicit;
- the first production slice and proof matrix are defined;
- no runtime code or Unity content has been changed;
- implementation authority remains closed.

The requested next action is user review, not implementation.

## 15. Research basis

Primary references that informed the architecture:

- Pearl Abyss describes Crimson Desert's sample-based environment workflow, automated visual-cohesion pipeline, procedural terrain tools, biome-based placement, art control, regional identity, landscape complexity, landmark placement, and environmental storytelling: [Pearl Abyss gamescom dev announcement](https://www.pearlabyss.com/ko-KR/Board/Detail?_boardNo=15097).
- Pearl Abyss describes BlackSpace as an in-house engine built around large seamless spaces and systemic environment presentation; it is an aspiration reference, not evidence that this project should recreate its engine: [BlackSpace Engine developer archive](https://crimsondesert.pearlabyss.com/en-US/News/Notice/Detail?_boardNo=40).
- Hydrology-driven procedural terrain research demonstrates hierarchical drainage networks as explicit terrain structure rather than noise-shaped appearance: [Large Scale Terrain Generation from Tectonic Uplift and Fluvial Erosion](https://doi.org/10.1145/2461912.2461996).
- Procedural city research demonstrates global goals combined with local rules and road grammars: [Procedural Modeling of Cities](https://people.eecs.berkeley.edu/~sequin/CS285/PAPERS/Parish_Muller01.pdf).
- Settlement research demonstrates adapting generated villages to arbitrary terrain and accessibility constraints rather than stamping a flat layout: [Procedural Generation of Villages on Arbitrary Terrains](https://perso.liris.cnrs.fr/egalin/Articles/2012-villages.pdf).
- The existing repository research remains useful for data-first generation, hybrid authored anchors, intentional emptiness, and deterministic chunk reconstruction: [WORLD_GEN_RESEARCH_SUMMARY.md](./WORLD_GEN_RESEARCH_SUMMARY.md).

These references support the chosen principles. They do not prove the proposed implementation, performance, or final visual quality; those require the evidence in section 10.
