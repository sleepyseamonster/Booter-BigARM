# Grounded Geology Implementation Plan

Status: canonical implementation plan; planning only

Owner: Gottspan

Created: 2026-09-04

Target engine: Unity 6000.4.0f1, URP

Target study: one 20–30 m Grounded Geology Study

Implementation authority: not granted by this document

## Evidence labels

- **Confirmed current:** directly observed in the repository on 2026-09-04.
- **Recommendation:** the architecture or default this plan selects.
- **Experiment:** a bounded comparison that must decide a gate.
- **User visual decision:** an aesthetic or perceptual choice that automated checks cannot close.
- **Deferred:** deliberately outside the current implementation sequence.

## 1. Executive decision

### Decision

**Recommendation:** the Grounded Geology Study is the correct next major milestone. It is the smallest surface that can prove the actual problem: whether terrain, coherent rock mass, sediment, materials, debris, collision, and traversal read as one geological event at gameplay scale. Adding more formation categories before that relationship is proven would multiply presets over disconnected systems.

The central architectural decision is:

> A deterministic geology recipe produces one immutable, queryable Grounded Geology Result in absolute meter space. Editor workbenches and the runtime World Creator pipeline are adapters and consumers of that result; neither becomes a second geology generator.

The result is not one giant mesh or manager. It is a bounded data bundle with the representations each consumer needs:

- compact recipe and stable identity;
- a 2D terrain/contact/sediment field with a halo;
- a localized 3D rock occupancy field or an equivalent deterministic sampler;
- deterministic placement records for talus and clutter;
- derived material, traversal, and collision classifications;
- temporary meshes, textures, colliders, and visualization data compiled from those sources.

### Architectural corrections

1. **Scale decision resolved by the user:** preserve the individual Rock Workbench's preferred 0.1-sized appearance by baking a 0.1 conversion into that rock's meter-valued source positions, dimensions, blend/seam widths, and baseline sample spacing, then keep the reusable generated root at Transform scale (1,1,1). This is a physical-size conversion for the individual rock fixture, not a global multiplier for formations, landmarks, terrain, materials, or future recipes. Reconstruction density then belongs in dimensionless cells-per-minimum-dimension, and material frequency belongs in meters per tile.
2. Extend World Creator's existing query, plan, identity, representation, streaming, and persistence seams. Do not revive TopDown3DWorldGenerator as a terrain authority and do not make a workbench MonoBehaviour a runtime authority.
3. Feed uplift and apron intent into the canonical surface query before chunk mesh compilation. A chunk realizes geology; it does not invent it.
4. Keep rock and terrain as separate render/collision representations unless a measured seam failure requires a localized transition mesh. Visual continuity comes from shared fields, material measurements, overlap, sediment, and contact shading—not from forcing everything into one mesh.
5. Preserve marching tetrahedra for the first study. Add scale-relative sampling and scalar-field gradient normals before considering adaptive grids or dual contouring.
6. Treat sediment as a deterministic shallow-layer field with finite supply and angle-of-repose relaxation. Real geometry is reserved for silhouette-bearing deposits and debris; fine grain remains material relief.
7. Preserve existing handmade and baked rock assets as fixtures and fallback representations. The study may use a current formation temporarily but may not encode its hierarchy as the new contract.

### Freeze and defer

- **Freeze:** new Rock Formation Workbench categories, new Landmark Workbench archetypes, and further preset multiplication.
- **Freeze:** replacement or deletion of existing handmade/baked rock families.
- **Deferred:** production-wide extraction until the golden study passes.
- **Deferred:** dual contouring, GPU field generation, runtime TerrainData stamping, production POM/displacement, and a new clutter rendering backend until their decision gates are met.
- **Deferred:** final world-canon geology palettes and the definitive handmade reference. Foundational contracts must remain reference-independent.
- **Deferred:** gameplay smoke testing unless the user separately requests it.

## 2. Definition of done

The overall initiative is done only when every outcome below is met. Milestone completion is narrower and is defined in Section 15.

### Visual outcomes

- At the fixed production camera baseline—25 m follow distance, 48-degree vertical FOV, 50-degree starting pitch—the study reads as terrain rising into one coherent fractured mass rather than rocks placed on a flat surface.
- The formation has readable large, medium, and small structure from at least eight azimuths plus the gameplay camera.
- No obvious floating members, hard circular terrain stamp, black seam, open underside, paper-thin shelf, or uniform root-scale artifact is visible.
- Sediment visibly accumulates at contact, in concavities, and in downwind shelter while exposed or steep faces remain cleaner.
- Talus and debris correlate with fracture discharge, slope, and downslope direction; clutter is neither uniformly random nor visibly gridded.
- Terrain and rock share believable world-space frequency, aligned height/roughness/AO response, and a controlled contact transition.
- LOD and distance fading do not cause silhouette pops, crawling normals, material-frequency jumps, deposit disappearance, or collision mismatch.

### Authoring outcomes

- One Integration Workbench creates or selects a temporary fixture, locks a seed, regenerates all derived outputs, and shows A/B/C comparisons without modifying the source fixture.
- All approved reusable roots remain (1,1,1); the inspector reports physical bounds in meters and resolution as cells across the smallest dimension.
- Field views include footprint, uplift/apron, rock occupancy slice, contact, crevice/concavity, wind shelter, sediment, talus probability, material blend, traversal, and chunk ownership/halo.
- Regeneration is deterministic, Undo-aware, and non-destructive. Generated previews are clearly temporary until explicitly approved or baked.
- The definitive handmade reference can replace the temporary fixture through an adapter with no contract or consumer redesign.

### Runtime outcomes

- The World Creator production authority owns recipe discovery and absolute identity. Chunk realization requests the same geology plan and samples halo data.
- Loading, unloading, and revisiting a chunk regenerates the same recipe, placement records, mesh signatures, and classifications for the same version manifest.
- A feature crossing chunks is owned once, realized in every intersecting chunk, and continuous at shared samples.
- Near representation provides render mesh and synchronized collision; mid/far representations use approved lower-cost outputs without becoming alternate geology.
- Player-authored changes store stable-ID deltas, never copied generated meshes or entire field grids unless a future measured cache justifies it.

### Determinism outcomes

- Identical world identity, recipe ID/version, generation versions, authored constraints, and coordinates produce byte-stable canonical field samples and stable sorted placement records on the supported platform.
- Evaluation order, asynchronous completion order, chunk request order, and origin rebasing do not change results.
- Stable IDs survive unload/reload and identify the same formation, member/representation, and runtime-delta target.
- Any deliberate algorithm/version change invalidates the appropriate domain explicitly and has a migration or regeneration policy.

### Performance outcomes

These are **initial gates**, not claims about current hardware:

- Editor preview: standard-quality 20–30 m study generation completes in at most 2.0 s after warm-up; approval-quality generation in at most 8.0 s.
- Runtime: no geology integration step exceeds 2.0 ms on the main thread in the existing scheduler budget; expensive compilation remains cancellable and off the main thread where Unity API restrictions permit.
- Near study target: at most 150k render triangles and 35k collision triangles for terrain, formation, apron/deposit silhouette meshes, and hand-sized debris combined before LOD; each number is separately reported.
- Peak temporary CPU memory for one approval-quality study stays below 128 MB; production runtime respects the existing 96 MB representation-cache budget or proposes a measured, separately approved change.
- Runtime collider cooking is absent from normal traversal, or scheduled at a safe integration point and measured. Revisit uses cached/prebaked collision where practical.
- Draw-call, SRP Batcher, instancing, and combined-mesh choices are validated with the Frame Debugger/Profiler; no optimization path is selected from theory alone.

### Traversal and collision outcomes

- Booter and BigARM affordance probes use the same uplift, rock boundary, and sediment surface represented by collision.
- Walkable slope, route reservation, obstacle, vaultable edge, blocked formation, loose talus, and cosmetic-only clutter are explicit classifications.
- Rocks intended for current smart traversal carry compatible obstacle metadata; the existing 0.80 m vault ceiling remains authoritative until separately tuned.
- Fine grit and small fragments are non-colliding. Hand-sized debris collides only when it materially changes movement/readability. Formation collision is a measured mesh or conservative compound approximation, never visual displacement.
- Automated collider-vs-render probes stay within the approved tolerance; hands-on movement feel remains user-owned.

### Proof boundary

Automated proof may close compilation, deterministic hashes, field continuity, topology, bounds, identity, collision synchronization, budgets, and screenshot reproducibility. It cannot close geological plausibility, perceived scale, silhouette quality, contact believability, clutter taste, material taste, gameplay-camera readability, or traversal feel. Those are **user visual decisions**.

## 3. Scope and non-goals

### In scope

- The one 20–30 m golden study and its temporary fixture adapter.
- A shared geology recipe/result contract in production runtime code.
- Editor-only integration, visualization, comparison, capture, metrics, and validation tooling.
- Meter-based scale calibration across rock, formation, landmark, clutter, terrain, camera, and agents.
- Deterministic uplift, apron, occupancy, contact, crevice/concavity, shelter, sediment, talus, material, traversal, and collision outputs.
- Staged reconstruction improvement, beginning with current marching tetrahedra.
- Extension of World Creator planning/query/representation/streaming/persistence seams.
- A migration path from the study into Rock, Formation, Landmark, and Landscape generators.

### Non-goals

- Shipping a complete infinite procedural world in the first batch.
- Inventing more formation categories or canonizing additional geology.
- Replacing all existing natural-object meshes, materials, workbenches, or planners at once.
- A fluid, hydraulic, water, rain, river, plant, or biome simulation. The Broken World is dry.
- Destructible terrain, runtime sculpting, mining, or formation fracture gameplay.
- Final shader art, final texture acquisition, final lighting, or final camera tuning.
- A universal volumetric framework for unrelated systems.
- DOTS/ECS conversion, compute-shader generation, or package changes without measured need.
- Using Unity Terrain solely because TerrainData APIs exist. The live production surface is streamed mesh heightfield data.

### Why categories stay frozen

Current category count is not the limiting factor. Existing authored categories and the World Creator composition goals already supply temporary fixtures. New categories would add parameters and validation combinations before scale, contact, sediment, reconstruction, and runtime ownership are stable. Category work resumes only after extraction proves that one recipe can drive all consumers and after the definitive handmade reference exposes a genuine missing structural family.

## 4. Current-system audit

### Repository and editor state

- **Confirmed current:** branch main is 26 commits ahead of origin/main and the worktree contains unrelated deleted generated rock assets, modified materials/scenes, an untracked camera prefab, and Assets/_Recovery content.
- **Confirmed current:** Temp/UnityLockfile exists. This plan does not infer whether the lock is live; it is enough to prohibit intrusive batchmode or GUI automation for this planning task.
- **Confirmed current:** Docs/GROUNDED_GEOLOGY_IMPLEMENTATION_PLAN.md did not exist before this plan.
- **Preservation rule:** all pre-existing dirty files are user-owned and outside this plan's change scope.

### Existing ownership and data flow

| Area | Confirmed current owner/path | Current output | Preserve |
|---|---|---|---|
| Production terrain authority | WorldCreatorProductionRuntime and UnboundedHybridWorldQueryService | absolute surface/material/affordance queries; streamed heightfield representations | yes |
| Compatibility terrain API | TopDown3DWorldGenerator | maps canonical World Creator samples into older TopDown3D samples | yes temporarily; do not extend as authority |
| Chunk compilation | WorldRepresentationCompiler, WorldRepresentationScheduler, TopDown3DChunkMeshBuilder | near/mid/far heightfield data, mesh, near MeshCollider | yes |
| Runtime formation intent | WorldRockFormationPlanner | deterministic formation/member plans and novelty fingerprints | extend |
| Runtime rock bridge | TopDown3DGeologicalRockAdapter | maps canonical plans to legacy formation plans and baked catalog meshes | isolate then narrow |
| Runtime clutter | TopDown3DNaturalObjectPlanner/Decorator | deterministic cell candidates, footprint rejection, combined cosmetic layers | adapt |
| Runtime deposit | TopDown3DDustDepositionPlanner/Decorator | 2D overlay samples and non-colliding dust mesh; feature disabled in current settings | preserve as migration input |
| Surface semantics | WorldSurfaceMaterialService and WorldTerrainMaterialPackingAdapter | exposure/shelter/deposit/material values packed into terrain vertex colors | extend from geology fields |
| Individual rock workbench | TopDown3DRockWorkbenchAuthoring plus editor generator/preview | source volumes, scalar-field mesh, temporary collider/material preview | preserve |
| Formation workbench | TopDown3DRockWorkbenchFormationAuthoring plus editor generator | member hierarchy, fused field, seam color, temporary collider | preserve |
| Landmark workbench | TopDown3DRockLandmarkAuthoring plus editor generator | formation hierarchy and shared field option | preserve |
| Traversal | WorldAffordanceSample, TopDown3DTraversalObstacle, player traversal planner | slope/route queries and obstacle-based vault/sidestep | extend classifications, do not replace |
| Persistence | WorldVersionManifest and saved-place/history records | independent version domains and stable-ID deltas | extend |

Current production flow:

    world identity + profile
      -> World Creator planning/query authority
      -> surface/material/affordance samples
      -> asynchronous representation compiler/scheduler
      -> chunk mesh + near collider
      -> compatibility world generator
      -> natural object planner
      -> geological adapter
      -> baked rock members + simple formation colliders/LOD

Current editor flow:

    workbench MonoBehaviour parameters
      -> editor generator creates source-volume hierarchy
      -> root-local scalar field
      -> uniform-grid marching tetrahedra
      -> relaxation + volume correction
      -> temporary MeshFilter/MeshRenderer/MeshCollider

### Confirmed disconnects

1. Runtime formation planning describes members but does not provide one coherent occupancy, contact, crevice, sediment, or apron result.
2. Workbench scalar fields are editor-only and are not the source used by World Creator runtime plans.
3. Terrain material shelter/deposit is inferred from broad surface semantics and normals; it does not sample rock occupancy/contact/concavity.
4. The dust overlay derives wakes from formation envelopes, not from the actual formation field; it is disabled in current world settings.
5. Terrain uplift does not currently arise from a shared formation recipe. Bounded landforms are simple height additions and formation rocks are decorated afterward.
6. Workbench rock material and production rock material are separate capability stacks. The workbench shader has richer aligned PBR detail; the production shader is comparatively sparse.
7. Terrain and rock shaders share a world-space idea but not a common measurement/configuration contract or contact blend.
8. Cosmetic clutter is deterministic and footprint-aware but uses fixed extra spacing per layer. It does not consume sediment, talus, crevice, or fracture-discharge fields.
9. Current workbench voxel parameters are absolute meters with a 96-cells-per-axis safety cap. The exposed “tessellation detail” is only a remapping of absolute voxel size, not dimensionless resolution.
10. Current workbench normals are recalculated from mesh topology after extraction, not sampled from the scalar-field gradient.
11. A saved workbench scene contains non-unit scaling, but the reported 0.1 root experiment is not serialized in the current scene. The user has nevertheless made its intended meaning authoritative: 0.1 is the preferred physical size of the individual rock, to be baked into meter-valued geometry at a unit root.

### Technical debt and conflicts

- TopDown3DWorldGenerator's class name implies authority although it now adapts World Creator. New geology code must depend on World Creator contracts directly.
- TopDown3DRockFormationPlanner and newer WorldRockFormationPlanner coexist. The latter is canonical for production intent; the former is compatibility-only.
- Some serialized workbench values can exceed current inspector ranges, so OnValidate/property behavior and scene values can disagree.
- The workbench documentation expects richer source masses than at least one current helper count suggests; the study must characterize output rather than rely on prose.
- Current near terrain uses 25 samples across an 18 m chunk, approximately 0.75 m spacing. That is suitable for broad ground but insufficient by itself for sharp contact shoulders or small sediment silhouettes.
- The current production profile and geology catalogs are explicitly non-canon proof data. The study must not silently promote them to creative canon.

### Existing work to preserve

- Workbench source-volume editing, stable member seeding, fused geological seams, temporary mesh lifecycle, Undo, and exact-mirror test practice.
- Current World Creator absolute coordinates, stable IDs, version domains, plan halos, query interfaces, representation scheduling, caching, rebasing, and persistence policy.
- Existing handmade references, baked 9-by-3-by-3 rock catalog, material assets, shaders, formation categories, and landmark content.
- Existing terrain semantic packing, dust-wake logic, natural-object competition, LOD, collision, and traversal tests as regression fixtures.

## 5. World-scale calibration

### Meter contract

**Recommendation:** one world unit equals approximately one meter. Recipe positions, dimensions, sediment depths, apron widths, fracture spacing, agent clearances, and material wavelengths are expressed in meters. Approved generated roots and runtime representation roots remain (1,1,1).

Calibration references:

| Reference | Confirmed or proposed measurement | Use |
|---|---:|---|
| Booter query proof profile | radius 0.55 m, height 2.0 m, max slope 43 degrees | human-scale visual and route clearance |
| BigARM query proof profile | radius 3.5 m, height 5.0 m, max slope 28 degrees | wide-route and landmark clearance; proof-only until reconciled with final body |
| Current BigARM scene collider | 1.1 x 2.2 x 1.3 m | expose current query-vs-scene mismatch; do not average silently |
| Smart traversal vault limit | 0.80 m above feet | obstacle/talus collision threshold |
| Production camera | 25 m, 48-degree vertical FOV, 50-degree starting pitch | projected-size acceptance |
| Streamed terrain chunk | 18 m with 24 quads | chunk boundary and base terrain sampling |
| Golden study | 20–30 m | integration proof, not a new chunk identity |

The BigARM query/collider mismatch is a required gate before final traversal sign-off. The study may visualize both envelopes.

### What the 0.1 experiment changed

The workbench mesher evaluates source transforms in root-local space. Uniformly scaling the root and all children together does not request ten times more local field samples; it scales the completed local mesh in world space.

For root scale s = 0.1:

- world-space width, height, member separation, collider extents, contact depth, and triangle edge lengths become one tenth;
- projected screen size becomes roughly one tenth at the same camera distance;
- local voxel size and local topology stay nominally the same, but effective world triangle spacing becomes one tenth;
- a world-space triplanar material does not inherit the same proportional shrink. A 2.4 m material wavelength stays 2.4 m, so its features become ten times larger relative to the rock;
- uniform scaling preserves normal directions in the ideal case, but smaller projected triangles and different contact shadows change perceived smoothness and mass;
- fixed world-meter seam, fracture, dust, traversal, and physics thresholds no longer agree with the apparent local authoring dimensions;
- non-unit parent scale increases the chance that later child rotation/non-uniform scale produces transform skew or collider mismatch.

The user has clarified that the desired result is the **actual smaller physical rock**, not a permanent scaled-root technique. Therefore physical size is no longer an open A/B choice: B is the target architecture. A/B verifies that the conversion is faithful; C then isolates geometry resolution and material frequency so neither remains accidentally coupled to the former root scale.

### Controlled A/B/C comparison

All panels use the same seed, source hierarchy, camera transforms, light rig, material source maps, ground plane, and capture resolution.

| Panel | Root | Geometry | Material | Purpose |
|---|---|---|---|---|
| A: reported result | scale 0.1 | current local dimensions and current voxel input | current world-space meters-per-tile | reconstruct the approved physical-size reference |
| B: target architecture | scale 1 | multiply the individual fixture's physical source positions, dimensions, smoothing, seams, and baseline voxel spacing by 0.1 | match A's absolute material wavelength | prove a unit-root rock can preserve A's actual size and appearance |
| C1: resolution | scale 1 | B dimensions with 24/40/64 cells across minimum dimension | fixed material scale | isolate silhouette/topology density |
| C2: material | scale 1 | approved C1 mesh | 0.25x/0.5x/1x/2x wavelengths around the matched value | isolate perceived grain/scale |
| C3: presentation | scale 1 | approved geometry/material | current gameplay camera plus tightly bounded alternate distance only as diagnostic | expose camera dependence without changing production baseline |

Acceptance:

- B must match A's world bounds, source landmarks, collider bounds, and rendered silhouette within stated tolerance.
- B is the selected architecture. If it does not match A, fix the conversion or identify the unmatched variable; do not restore a persistent 0.1 root.
- C chooses independent geometry density and material frequency. The selected values must not require a non-unit root.
- If A still wins after equivalent baking, capture the exact measurable difference before changing architecture; do not accept a “feels better” root-scale dependency.

### Initial physical ranges

These are **recommended study ranges**, not final creative canon:

| Element | Physical range | Representation note |
|---|---|---|
| Material grain/pits | 1–15 mm | normal/roughness/AO; no geometry |
| Coarse grit | 15–80 mm | mostly material relief; sparse geometry at near camera |
| Small fragments | 0.08–0.30 m | non-colliding instanced/combined geometry |
| Hand-sized debris | 0.30–0.80 m | selective collision; 0.80 m aligns with vault gate |
| Small obstacle rock | 0.5–1.5 m footprint, 0.25–1.2 m height | collision/traversal classification required |
| Individual Rock Workbench rock | 0.075–1.2 m footprint, 0.05–3.0 m height | initial range produced by baking the current 0.1 visual conversion; user-approved size is the calibration center |
| Formation structural member | 0.25–8 m footprint, 0.2–12 m height | separate formation-scale range; do not blanket-apply the Rock Workbench 0.1 factor |
| Exceptional spire/member | 2–8 m footprint, up to 18 m height | aspect ratio gate and multi-angle proof |
| Formation | 4–30 m footprint, 1–18 m study height | current width range retained; 30 m height only exceptional |
| Landmark | 20–80 m footprint, 8–35 m height | later extraction; current 12–60 m fixture remains valid |
| Formation apron | 1–8 m beyond footprint | relative default 0.15–0.35 of formation width |
| Local terrain shoulder | 0.15–3 m uplift within the study | capped by route/slope constraints |
| Regional terrain feature | 30–300+ m | remains World Creator macro-landform authority |

The system stores absolute meters, while controls such as apron ratio, member size ratios, fracture density, weathering amplitude ratio, and cells-across-dimension are dimensionless.

### Scale-relative resolution

Let m be the smallest non-padding dimension of the bounded rock field and N be the requested cells across that dimension:

    cellSize = m / N
    cellsAxis = ceil(fieldExtentAxis / cellSize)

Recommended presets:

- Draft: N = 24.
- Standard: N = 40.
- Approval: N = 64.
- Diagnostic ceiling: N = 96, subject to point/memory budget.

Report both requested and effective N after caps. Never label an absolute 0.025–0.18 m voxel slider as dimensionless detail.

## 6. Target architecture

### Data layers

#### A. Immutable authoring/recipe data

GroundedGeologyRecipe is a pure serializable value:

- recipe stable ID, recipe schema version, and independent algorithm-version references;
- absolute anchor, orientation, physical bounds, footprint/spine definition;
- structural-member descriptors or handmade-fixture adapter references;
- strata/province references, fracture families, weathering ratios;
- uplift/apron ratios, sediment supply and repose parameters;
- authored constraints, protected routes, and landmark intent;
- seed salt and explicit locked/unlocked seed state.

Recipes contain intent and constraints, not generated vertices, Unity scene references, or cached samples. ScriptableObject assets may author templates, but runtime plans use immutable copies.

#### B. Deterministic generation inputs

GroundedGeologyGenerationInput:

- WorldIdentity and WorldVersionManifest;
- recipe identity/version;
- absolute evaluation bounds plus halo;
- owner cell/chunk coordinate;
- generation quality/resolution profile;
- queried canonical surface/material/affordance context;
- authored constraint snapshot;
- requested representation kinds.

#### C. Temporary generated result

GroundedGeologyResult:

- identity/signature and exact bounds;
- footprint/spine evaluator;
- terrain uplift/apron grid;
- rock occupancy evaluator or bounded samples;
- contact, crevice/concavity, wind, sediment, talus, material, traversal grids;
- sorted placement records;
- diagnostics and budget metrics;
- representation-independent stable sub-IDs.

It must be plain data and disposable. Meshes, colliders, textures, GameObjects, and MaterialPropertyBlocks are representation outputs compiled afterward.

### Semantic representation map

| Semantic output | Canonical generated representation | Consumer realization | Persist? |
|---|---|---|---|
| Formation footprint/spine | analytic 2D signed-distance evaluator plus bounded sampled diagnostic grid | uplift, placement exclusion, workbench overlay | recipe only |
| Terrain uplift/apron | haloed 2D float grid or deterministic sampler in meters | canonical surface query and near/mid/far terrain meshes | no |
| Rock occupancy | localized 3D distance-like scalar field/evaluator in meters | render/collision extraction, contact and visibility queries | no |
| Rock-ground contact | haloed 2D normalized float grid plus gap in meters | sediment, material blend, contact diagnostics | no |
| Crevice depth/concavity | haloed 2D normalized grids with an optional depth-in-meters channel | sediment, dust, material, talus | no |
| Surface slope/curvature | derived 2D slope degrees and scale-normalized curvature grids | sediment, material, traversal | no |
| Wind exposure/shelter | 2D normalized exposure/shelter plus direction vector | sediment, dust, clutter orientation | no |
| Sediment thickness | haloed 2D meter-valued shallow-layer grid | material blend and optional near deposit mesh | no unless future gameplay makes it mutable |
| Talus/debris probability | 2D normalized probability/tier fields | deterministic sorted placement records | only player deltas keyed to placement IDs |
| Ground-material blend | normalized semantic weights sampled or packed per representation vertex | terrain/rock/deposit shaders | no |
| Rock dust coverage | derived rock-surface vertex/field weight | rock shader and near deposit shell | no |
| Traversal classification | compact cell/region bitfield plus queried slope/clearance | WorldAffordanceSample and obstacle metadata | no |
| Collision boundary | representation payload: decimated closed mesh or conservative compound descriptors | MeshCollider/primitive colliders | no; cache only if measured |

The refined pipeline is:

    World Creator identity + immutable geology recipe
      -> footprint/spine and constrained terrain uplift
      -> coherent rock occupancy
      -> contact/crevice/slope/curvature/wind fields
      -> finite sediment target and repose relaxation
      -> talus probability and stable clutter records
      -> shared semantic material/traversal/collision payloads
      -> editor or streamed runtime representations

#### D. Persisted player changes

Persistence stores only stable-ID keyed deltas such as removed debris, mined/damaged state when that gameplay exists, discovered landmark state, or authored history operations. Generated recipes/fields are regenerated from identity and versions. A cache is an optimization and must not become save truth.

### Service boundaries

| Service | Responsibility | Must not own |
|---|---|---|
| GroundedGeologyPlanner | choose/resolve recipes and stable feature ownership from World Creator context | Unity objects or chunk-local randomness |
| GroundedGeologyFieldCompiler | generate bounded scalar/2D fields as pure data | rendering, scene mutation, persistence |
| GroundedGeologyRepresentationCompiler | produce mesh/material/collision/traversal/placement payloads | recipe discovery |
| World Creator query integration | compose uplift/semantics into canonical queries and expose feature lookup | editor UI |
| Integration Workbench | author/select fixture, call shared compiler, visualize and compare | alternate algorithms or runtime authority |
| Runtime landscape adapter | request/intersect plans, schedule compile, attach representations to chunks | geology invention |
| Persistence adapter | apply stable-ID deltas and version policy | generated caches as truth |

### Dependency direction

    immutable recipe + World Creator query contracts
      -> pure geology field compiler
      -> immutable GroundedGeologyResult
      -> editor visualization adapter
      -> runtime representation adapter
      -> terrain / rock / material / clutter / traversal / collision consumers

Editor may depend on runtime contracts and pure generation. Runtime never depends on Editor assemblies. Existing workbench source volumes remain an editor fixture adapter until a runtime-safe structural recipe is extracted.

## 7. Procedural-world contract

### Identity

- World identity remains World Creator's canonical world identity.
- Recipe identity is StableGeologyRecipeId plus RecipeSchemaVersion.
- Algorithm changes use independent version domains: geology-plan, occupancy, sediment, decoration, material-packing, collision, and representation versions. They map into or extend WorldVersionManifest; no single “everything version” invalidates unrelated data.
- Stable feature ID is derived from world identity, recipe ID/version, absolute owner cell, feature ordinal, and relevant plan version using the repository's stable hash conventions.
- Stable placement IDs derive from parent feature ID, tier/role, and deterministic sorted candidate key—not list completion order.

### Seed derivation

    featureSeed = StableHash(
        worldSeed,
        ownerCellA,
        ownerCellB,
        recipeStableId,
        recipeVersion,
        geologyPlanVersion)

Each subsystem derives named/salted streams from featureSeed. Adding a material diagnostic may not perturb member placement; adding a clutter tier may not reseed rock occupancy.

### Ownership and boundaries

- The formation owner is the World Creator planning cell containing its absolute anchor, with deterministic tie-breaking on exact boundaries.
- The owner produces the recipe once. Any terrain chunk whose expanded bounds intersect the feature requests the same plan.
- Evaluation uses a halo at least max(apron width, sediment transport radius, largest clutter competition radius, normal/curvature stencil radius, collision overlap tolerance).
- Shared chunk-edge samples use identical absolute coordinates and the same feature ordering. A chunk never clamps or renormalizes a field independently at its edge.
- Mesh border vertices are keyed by quantized absolute sample coordinates. Neighboring representations must match position/material values exactly at the shared border or use an explicit seam-stitch representation.

### Streaming and regeneration

- Planning and field compilation are pure/cancellable. Unity mesh/collider creation occurs only during controlled integration.
- Unload destroys temporary Unity representations and releases pooled buffers. It does not mutate recipe or save truth.
- Reload re-derives results from identity/version plus player deltas. Request order and async completion order are irrelevant.
- Origin rebase changes local presentation transforms only; absolute recipe coordinates and hashes remain unchanged.
- Caches key on complete identity, bounds, resolution profile, and relevant versions. Cache misses regenerate; corrupt or old entries are disposable.

### Authored constraints and landmarks

- Handmade landmarks provide stable anchors, permitted bounds, exclusion/reservation masks, recipe overrides, and optionally structural source volumes.
- Authored routes, site reservations, approaches, ruins, and encounter footprints constrain uplift, debris, and collision before compilation.
- A landmark anchor does not own adjacent chunks. It supplies absolute intent to the same plan/query system.
- Temporary fixture IDs are explicitly prefixed/flagged as non-canon study content and cannot enter production spawn catalogs accidentally.

### Persisted runtime deltas and saves

- Store parent feature ID, optional subfeature ID, delta type, payload, authored timestamp/order, and version at application.
- On compatible generation changes, reapply a delta by stable ID. If the target no longer exists, retain as orphaned migration evidence and apply the declared policy; never silently retarget nearest geometry.
- Recipe schema changes require a codec/migration test. Pure visual representation changes should not invalidate gameplay deltas.
- Sediment is regenerated unless gameplay later makes it mutable. If mutable sediment is approved, persist sparse operations or compact deltas, not the full generated base field.

### Deterministic proof

- Hash canonical sorted recipe/result data before Unity object creation.
- Compare center, all four borders, corners, and halo overlaps for adjacent chunks.
- Compile the same feature with reversed chunk request order and shuffled task completion.
- unload/reload, origin-rebase, and save/restore must preserve feature and placement IDs.
- Repeat on an exact temporary project mirror when the GUI project is locked; inspect NUnit XML totals.

## 8. Mathematical model

All positions below are absolute meter-space unless stated otherwise. Grids use explicit sample origin and step. Smoothstep is the cubic Hermite interpolation on [0,1].

### 8.1 Formation footprint and spine distance

Represent a footprint as a union of tapered capsules along a 2D polyline. For segment endpoints a and b with radii ra and rb:

    t = clamp(dot(x-a, b-a) / dot(b-a, b-a), 0, 1)
    q = lerp(a, b, t)
    r = lerp(ra, rb, t)
    dSegment(x) = length(x-q) - r
    dFootprint(x) = smoothMin over all dSegment, with blend radius kf

Negative distance is inside. Optional lobes use the same primitive. Recipe controls normalized spine points, radius-to-width ratios, and blend ratio; the compiler multiplies them by physical dimensions.

### 8.2 Terrain uplift and apron

Let d = dFootprint(x), W be apron width, U be maximum uplift, and p a shoulder exponent:

    outside = 1 - smoothstep(0, W, max(0,d))
    inside = 1
    weight = (d <= 0 ? inside : outside)^p
    constrained = weight * routeAndSiteGate(x)
    uplift(x) = U * constrained + A * bandLimitedNoise(x/L) * constrained * (1-constrained)
    terrainHeight(x) = baseCanonicalHeight(x) + uplift(x)

U is meters. W may be authored as width ratio then resolved to meters. p, A/U, noise wavelength/formation width, and gate softness/W are dimensionless. The halo ends where weight is exactly zero. Limit gradients or solve a constrained blend where required so mandatory paths remain within agent slope/clearance.

### 8.3 Rock signed-distance composition

Each structural member uses an analytic or sampled local SDF transformed from recipe space:

    k0 = length(p / axes)
    k1 = length(p / (axes * axes))
    de(p) = k0*(k0-1)/max(k1,epsilon)                ellipsoid distance approximation
    db(p) = roundedBoxDistance(p, halfExtents, roundness)
    dp(p) = dot(p-planePoint, planeNormal)           fracture plane

Compose overlapping masses with a polynomial smooth union:

    h = clamp(0.5 + 0.5*(d2-d1)/k, 0, 1)
    smoothMin(d1,d2,k) = lerp(d2,d1,h) - k*h*(1-h)

Apply bounded macro warp before distance evaluation and bounded surface weathering near the zero set:

    pWarp = p + warpAmplitude * vectorNoise(p / warpWavelength)
    dMass = smoothMin members(pWarp)
    dFractured = max(dMass, -dCut)                   subtract fracture void
    dRock = dFractured + weatherAmplitude * noise(p/weatherWavelength) * surfaceBand

Preserve a protected ground-contact band where vertical erosion amplitude tapers to zero. All amplitudes, blend radii, fracture gaps, and wavelengths resolve from member size ratios with meter clamps.

### 8.4 Contact and crevice masks

At terrain sample x with surface point pt = (x, terrainHeight(x)):

    gap = dRock(pt)
    contact = 1 - smoothstep(contactInner, contactOuter, abs(gap))

Estimate local enclosure by sampling the occupancy field over a fixed normalized hemisphere and horizontal ring:

    enclosure = occupiedRayWeight(pt, directions, radius)
    terrainBowl = clampPositive(laplacian(terrainHeight) / curvatureReference)
    rockPocket = mean_i(1-smoothstep(-creviceBand, creviceBand,
                                    dRock(pt + ringOffset_i)))
    crevice = saturate(max(terrainBowl, rockPocket) * enclosure)

Direction sets and sample order are fixed. Radii are ratios of local member size with meter clamps. For standard/approval quality, central differences share halo samples.

### 8.5 Slope, curvature, and concavity

For scalar height H with grid step e:

    Hx = (H(x+e,z)-H(x-e,z)) / (2e)
    Hz = (H(x,z+e)-H(x,z-e)) / (2e)
    slopeRadians = atan(length(Hx,Hz))
    laplacian = (H(x+e,z)+H(x-e,z)+H(x,z+e)+H(x,z-e)-4H(x,z)) / e^2
    concavity = saturate(max(0, laplacian) / curvatureReference)

For the rock SDF:

    normal = normalize(gradient(dRock))
    meanCurvatureApprox = divergence(normal)

Because curvature sign depends on the SDF convention, unit tests use analytic bowl/dome fixtures to lock the sign. Curvature references scale inversely with feature size, making normalized concavity dimensionless.

### 8.6 Wind exposure and shelter

Let w point downwind and n be the surface normal. Trace fixed samples upwind through terrain and rock occupancy:

    blockage = sum_i opacity(samplePoint - w * distance_i) * distanceWeight_i
    shelter = 1 - exp(-blockage)
    faceExposure = saturate(dot(n, -w))
    horizonExposure = 1 - shelter
    windExposure = saturate(0.65*horizonExposure + 0.35*faceExposure)

Add a deterministic lee-wake envelope whose length is a multiple of obstruction height and whose width tapers by normalized downwind distance. This generalizes the current formation-envelope wake, but occupancy/horizon samples—not only a bounding radius—set blockage.

### 8.7 Sediment target thickness

Define source supply Q, low-slope acceptance L, contact C, crevice V, shelter S, wind exposure E, and talus contribution T:

    L = 1 - smoothstep(slopeStart, slopeStop, slopeDegrees)
    depositionPotential = saturate(
        wq*Q + wc*C + wv*V + ws*S + wt*T - we*E)
    targetThickness = maxThickness * L * depositionPotential

Weights sum to a documented normalized total. maxThickness is meters; weights and masks are dimensionless. Convert the target to an initial finite layer by scaling its integrated volume:

    targetVolume = sum_i(targetThickness_i * cellArea)
    supplyScale = min(1, availableSedimentVolume / max(targetVolume,epsilon))
    initialThickness_i = targetThickness_i * supplyScale

Relaxation only transports this initialized mass. Any open-boundary loss is measured explicitly; no step creates sediment.

### 8.8 Angle-of-repose thermal relaxation

Start with non-negative sediment thickness si and fixed terrain/rock support height bi. Block edges that enter solid rock or protected routes. For every remaining deterministic directed neighbor edge i->j:

    surfaceDifference = (bi + si) - (bj + sj)
    allowedDifference = edgeLength * tan(reposeAngle)
    excess = max(0, surfaceDifference - allowedDifference)
    transfer = min(si, relaxationRate * 0.5 * excess)

Apply transfers in a fixed red/black grid schedule or accumulate into a second buffer, then swap. Repeat a fixed iteration count or stop when maximum transfer is below epsilon. Use the full halo and crop only after relaxation. Assert:

- no negative thickness;
- mass conservation within boundary/source policy;
- maximum remaining slope at or below repose plus epsilon;
- identical results independent of chunk compilation order.

Repose angle, iteration count, and relaxation rate are dimensionless; thickness, edge length, and epsilon are meters.

### 8.9 Talus density

    discharge = normalize(fractureOpening/memberSize) *
                normalize(localRockHeight/formationHeight) * weathering
    slopeFit = smoothstep(thetaMin, thetaPeak, slope) *
               (1-smoothstep(thetaPeak, thetaMax, slope))
    footFit = exp(-max(0,dFootprint)^2 / (2*sigma^2))
    downslope = saturate(dot(normalizedDownslope, fractureFallDirection))
    talusProbability = saturate(discharge * slopeFit * footFit *
                                lerp(0.6,1.0,downslope) *
                                (0.5+0.5*sedimentSupply))

Probability controls deterministic candidates; it is not itself object count. Fragment size distribution and sigma are resolved from parent/member dimensions.

### 8.10 Material blending and rock dust

At a terrain/rock sample:

    sedimentWeight = smoothstep(thicknessLow, thicknessHigh, sedimentThickness)
    contactWeight = contact * (1-windExposure)
    rockWeight = occupancySurface * (1-sedimentWeight)
    soilWeight = saturate(1-rockWeight)
    dustOnRock = saturate(sedimentWeight + contactWeight*contactDustGain)

Normalize terrain material family weights after applying semantic constraints. Pack stable semantic channels, not shader-specific artistic accidents. Rock and terrain shaders consume the same meter-valued macro/micro wavelengths and aligned height/roughness/AO sources.

### 8.11 Variable-radius clutter

Use a deterministic variable-radius Poisson-like candidate set:

    candidateRadius = lerp(radiusMinTier, radiusMaxTier, hash01(candidateId))
    density = fieldTalusSedimentCrevice(candidatePosition)
    priority = hash01(candidateId + prioritySalt)
    admitByDensity when hash01(candidateId + densitySalt) < density
    accept candidate only when, for every accepted neighbor j:
        distance(i,j) >= spacingFactor * (radius_i + radius_j)

Candidates come from absolute cells and include the competition halo. Process by stable priority plus cell-coordinate tie-break, never by chunk traversal order. Orient to a blend of support normal, downslope, fracture direction, and deterministic yaw. Sink depth is a radius ratio clamped in meters.

### 8.12 Dimensionless parameter list

Prefer dimensionless controls for:

- cells across smallest dimension;
- footprint/spine normalized coordinates and radii ratios;
- member size/separation/aspect ratios;
- apron width/formation width;
- uplift/formation height;
- smooth-union/member size;
- warp, weathering, seam, and fracture wavelength/amplitude ratios;
- curvature normalized by feature size;
- sediment weights, shelter gain, density, burial ratio;
- talus tier proportions and spacing factor;
- smoothing pass strength and volume tolerance;
- LOD screen-relative thresholds.

Keep world positions, physical sizes, cell step after resolution, material wavelengths, contact bands, sediment thickness, route clearance, and collision tolerances explicitly in meters.

## 9. Rock reconstruction strategy

### Comparison

| Strategy | Strength | Cost/risk | Decision |
|---|---|---|---|
| Current marching tetrahedra, uniform grid | existing, deterministic, simple topology, proven workbench path | many triangles; topology/silhouette tied to fixed grid; faceting; no sharp-feature intent | keep for study baseline |
| Scalar-field gradient normals | smooth shading follows source field rather than triangulation | extra SDF samples; must handle fracture discontinuities | implement first |
| Scale-relative sampling | consistent density across physical sizes and root scale 1 | large/aspect-ratio bounds can hit sample budget | implement first with explicit caps |
| Feature-preserving smoothing | can reduce grid noise without erasing planes/contact | naïve Laplacian rounds fractures and shrinks volume | bounded second-stage experiment |
| Adaptive octree sampling | concentrates work near surface/detail | transition stitching, determinism, cache complexity | defer until profile shows uniform-grid failure |
| Dual contouring/Hermite QEF | preserves sharp fracture intersections and supports adaptivity | nontrivial topology/QEF robustness; new test surface | gated later experiment, not rewrite |

### Staged recommendation

1. **Baseline:** preserve current marching tetrahedra and current relaxation result for A.
2. **Scale fix:** calculate cell size from cells-across-minimum-dimension; report effective sampling after 96-axis/point/memory caps.
3. **Normals:** compute vertex normals from the central-difference gradient of the same final SDF. Near fracture-cut discontinuities, use one-sided/feature-group gradients or split vertices at an angular threshold so intentional edges remain readable.
4. **Contact protection:** pin or weight the ground-contact band during relaxation; preserve lowest support plane and occupancy sign.
5. **Feature-preserving relaxation:** compare current Taubin-like smoothing to bilateral/normal-aware displacement constrained along the field gradient. Reject any variant that exceeds volume, fracture-angle, contact, or Hausdorff tolerances.
6. **Profile:** measure topology, projected cell size, generation time, and memory at draft/standard/approval settings.
7. **Dual-contouring gate:** only prototype on the identical field fixtures if marching tetrahedra cannot meet silhouette/fracture acceptance within the study budget. Keep it behind an extractor interface; do not fork the field generator.

### Required reconstruction invariants

- closed, consistently wound, finite manifold surface where the recipe calls for a closed mass;
- no degenerate or zero-area triangles after cleanup;
- protected fracture-plane angle and gap remain within tolerance;
- signed-volume drift at most 3% standard and 1.5% approval versus the unsmoothed extracted result;
- ground-contact footprint drift at most one effective cell;
- surface position error versus the zero set at most 0.75 effective cell;
- normals are finite, unit length, outward by SDF convention, and stable across regeneration;
- collider is compiled from the approved collision representation, not from shader displacement.

## 10. Terrain integration

### Current constraint

**Confirmed current:** production terrain is a custom streamed mesh heightfield compiled from World Creator queries, not a Unity Terrain/TerrainData landscape. Near resolution is 25 samples over the current 18 m chunk. The integration path must therefore modify canonical surface evaluation and representation compilation, not introduce direct TerrainData writes.

### Recommended integration

1. GroundedGeologyPlanner makes intersecting formation recipes discoverable from absolute query bounds.
2. Canonical surface query composes formation uplift/apron before returning surface/material/affordance samples.
3. WorldRepresentationCompiler samples the composed query at existing chunk coordinates and shared borders.
4. A localized higher-resolution geology surface patch may overlay/replace the coarse near heightfield inside the study when measured contact shoulders need it.
5. The separate rock mesh penetrates the terrain by a controlled contact depth. Sediment shell/material blend hides residual representational seams.
6. Collision uses the same composed surface data and approved rock collision representation.

### Heightmap limitations

A heightfield can represent one elevation per horizontal point. It cannot represent overhangs, caves, undercut shelves, vertical double surfaces, or a rock underside. It is appropriate for uplift, shoulders, and broad sediment aprons, not the full formation.

Unity TerrainData is additionally a distinct asset/runtime system with constrained heightmap resolutions and expensive synchronization behavior. It is not a shortcut into this mesh-streaming pipeline. A direct TerrainData experiment is permitted only if the project first chooses to replace its terrain representation architecture.

### Terrain stamp versus localized apron mesh

- **Default:** query-composed height uplift and sediment thickness, compiled into the existing chunk mesh.
- **Localized patch:** a deterministic near-only mesh when the 0.75 m base sample spacing cannot represent approved shoulders/deposit silhouettes. It uses the same absolute border samples and fades to the chunk heightfield across an explicit ring.
- **Transition mesh:** only for heightfield/rock overhang contact or holes that cannot be hidden with controlled penetration/sediment. It is a representation of the same result, not a third geometry generator.
- **Terrain holes:** deferred. Use only if a true undercut/cave opening requires removing heightfield triangles; never punch holes just to cure a visible contact seam.

### Continuity

- Compose every feature intersecting the sample halo in stable feature-ID order.
- Use smooth maximum/union rules that are associative by stable ordered fold; never normalize per chunk.
- Shared edge/corner positions, semantic weights, and normals derive from identical absolute samples.
- Sediment relaxation crosses the chunk boundary in its halo and is cropped after convergence.
- At differing LODs, coarser representations sample the same query. Stitch or skirt only representation cracks; do not change geology.

## 11. Sediment and clutter

### Layer model

The study uses a shallow sediment field over support geometry:

- base supply from WorldSurfaceMaterialSample sediment/deposit semantics;
- extra supply from formation weathering/fracture discharge;
- target accumulation from contact, concavity, crevice, shelter, and low slope;
- deterministic thermal relaxation to repose;
- final thickness as a material input everywhere and real geometry only above silhouette thresholds.

No water or hydraulic erosion is introduced.

### Geometry tiers

| Tier | Size | Collision | Realization |
|---|---:|---|---|
| Grain/grit relief | 1–15 mm, dense | none | aligned normal/roughness/AO/height maps |
| Coarse grit | 15–80 mm | none | sparse near geometry or material relief by projected size |
| Small fragments | 0.08–0.30 m | none | combined/instanced simple meshes |
| Hand-sized debris | 0.30–0.80 m | only approved movement-relevant subset | LOD mesh plus stable placement |
| Larger talus/obstacle | 0.8–2.5 m | yes when blocking/traversable | formation child or physical natural object, not “clutter” |

The required three clutter scales are coarse grit, small fragments, and hand-sized debris. Material grain is a fourth sub-geometry scale, and larger talus graduates into the physical-rock system.

### Placement, burial, and orientation

- Density comes from talus probability multiplied by semantic/material constraints; separate hash streams select tier, family, size, and orientation.
- Variable-radius competition spans chunk halos and existing physical formation footprints.
- Burial depth is 8–30% of object minor radius, increased by sediment thickness but capped so silhouettes remain readable.
- Support height and normal come from the final uplift-plus-sediment surface.
- Fragments align partly with strata/fracture fall direction and partly with downslope; deterministic noise prevents regimented alignment.
- Reject candidates on reserved routes, invalid slope, within protected spawn/site masks, inside solid rock, or without stable support.
- Grit and small fragments are cosmetic. Hand debris gets collision only above a physical/gameplay threshold and only after collider-budget proof.

### Real sediment geometry

Create a near-only deposit mesh where thickness exceeds a projected-silhouette threshold or where a contact wedge would otherwise float. Clip it against rock occupancy, weld/overlap it safely at the ground, and keep it non-colliding unless the thickness materially changes traversal. Thin deposits remain shader blend.

### Preserve and migrate

TopDown3DDustDepositionPlanner's deterministic wind and current formation-wake behavior becomes a comparison fixture. TopDown3DNaturalObjectPlanner's stable absolute candidate cells and footprint competition remain useful. The new result supplies field weights/radii; it does not discard those tested mechanisms wholesale.

## 12. Material and rendering strategy

### One measurement contract

Create a GroundedGeologyMaterialScale profile consumed by terrain, formation, deposit, and debris materials:

- macro color/strata wavelength in meters;
- meso break-up wavelength in meters;
- micro grain wavelength in meters;
- normal amplitude in physical-looking bounded units;
- packed-map channel contract: R AO, G roughness, B height, A reserved/mask;
- contact, dust, and distance-fade ranges in meters.

Root/object scale does not alter these measurements. Object-space UV scale is not the authority.

### Surface strategy

- World-space triplanar mapping for rock and steep/complex surfaces.
- World XZ or slope-aware triplanar projection for ground, sharing the same phase/measurement origin where contact continuity matters.
- Macro normal from mesh/SDF gradient; meso normal for fracture/strata; micro normal for grain. Fade high-frequency layers with distance and projected texel size.
- Height, roughness, AO, and normals come from aligned source sets. Do not infer PBR channels from base-color brightness.
- Terrain/rock height blending uses the shared contact and sediment fields. It must not erase the rock silhouette or introduce black overlap.
- Rock dust uses contact/crevice/shelter/sediment masks and decreases with wind exposure and steepness.
- Contact shading is a bounded combination of material AO/contact mask and real scene shadowing. It must not paint a universal dark ring.

### Parallax and displacement

- Baseline: no geometric displacement in the golden study's first material pass.
- Bounded close-range offset/parallax may be compared only where the production camera projects enough pixels to show it.
- POM is accepted only if a profile and eight-angle review show benefit without silhouette/contact artifacts or unacceptable shader cost.
- Tessellation/displacement is deferred in URP unless a supported, measured path is established.
- Visual parallax/displacement never changes collision or affordance. Any gameplay-relevant relief must exist in generated geometry/collision data.

### URP, variants, and batching

- Keep shaders SRP Batcher compatible by default. Do not deliberately break compatibility for automatic GPU instancing without a profile.
- Share material assets and use MaterialPropertyBlock only for per-feature values that do not create uncontrolled variants.
- Keep keyword space bounded: quality tier, optional deposit shell, and explicitly approved feature toggles. Avoid per-category shader variants.
- Combine tiny static cosmetic fragments by chunk/material when CPU submission wins; use Graphics.RenderMeshInstanced/Indirect only if identical mesh density and profiling justify it.
- Preserve LODGroup screen-relative thresholds for physical rocks initially, then calibrate by silhouette error and gameplay camera.
- Use shadow casting on silhouette-bearing rock/talus; reduce or disable it for sub-pixel grit after visual comparison.

## 13. Workbench UX

### Integration Workbench responsibilities

The Grounded Geology Integration Workbench is an editor-only shell that references a fixture adapter and calls shared pure runtime generation. It does not replace the Rock, Formation, or Landmark Workbenches.

Controls:

- fixture: Temporary Existing Formation / Handmade Reference / Recipe Asset;
- explicit non-canon temporary badge and approved-fixture state;
- seed field, Lock Seed toggle, New Seed, Regenerate, Cancel;
- physical study bounds and meter rulers;
- resolution preset and effective cells/axis/point count;
- generation versions and deterministic signature;
- A/B/C panel builder and pairwise isolate buttons;
- fixed gameplay camera presets plus eight azimuth captures;
- geometry-only, material-only, integrated, collision, and traversal review modes;
- field visualization selector.

### Field views

- footprint/spine signed distance;
- terrain uplift and apron;
- rock occupancy slices and zero set;
- contact/gap;
- crevice and normalized concavity;
- slope/curvature;
- wind shelter/exposure;
- sediment target/final thickness and transport residual;
- talus probability and accepted clutter radii;
- terrain/rock/dust material weights;
- traversal/route/exclusion/collision classifications;
- chunk owner, intersecting chunks, halo, and shared-border error;
- SDF gradient normals versus recalculated mesh normals.

### Comparison and diagnostics

- side-by-side and flicker/difference views for two selected variants;
- matching camera/light/exposure locked by default;
- on-screen bounds, triangle count, cell size, projected cell pixels, generation time, peak memory, collider triangles/cooking time, draw calls, and deterministic hash;
- warnings for non-unit root, serialized values outside current contract, resolution caps, open topology, missing material channels, stale result, and temporary fixture in a production catalog.

### Non-destructive behavior

- Generated previews live under a clearly named temporary child/container and are replaced transactionally only after successful compilation.
- Undo records source edits and preview replacement where Unity serialization supports it.
- Source hierarchy/manual edits are never normalized, reordered, or deleted during preview.
- Approve/Bake is a separate explicit action in a later milestone and writes only declared assets.
- Replacing the temporary fixture swaps the adapter input, regenerates the same result contract, and retains comparison captures/metrics by fixture ID. No consumer code changes.

## 14. Experimental methodology

### Locked review environment

- study bounds: one selected 20–30 m rectangle plus declared halo;
- world/recipe seed and generation versions;
- production camera baseline and eight named azimuth cameras;
- capture resolution, color space, URP asset, light transforms/intensity, volume profile, and exposure;
- source fixture transform reset to (1,1,1) for B/C;
- ground/context query snapshot;
- warm-up count and timing method.

If any lock changes, the comparison set gets a new experiment ID.

### Review passes

1. **Geometry only:** neutral clay material, no micro detail; judge silhouette, fracture planes, contact, topology.
2. **Material only:** fixed approved geometry; vary wavelengths, normal/height strength, dust/contact blend.
3. **Integrated:** uplift, rock, sediment, talus, clutter, material, collision/traversal overlays.
4. **Runtime representation:** LOD, streaming borders, reload, origin rebase, collision synchronization.

### Efficient sampling

- Use a seeded Latin hypercube for 4–6 high-impact normalized parameters per experiment, initially 9 variants, not a combinatorial slider grid.
- Rank sensitivity automatically from metrics and visually through pairwise comparisons.
- Present the user with at most three finalists and one baseline at a time.
- The user selects “left,” “right,” “no meaningful difference,” or “reject both,” plus optional notes. They are never required to assign numeric scores to dozens of controls.
- Freeze insensitive parameters at robust midpoints; refine only sensitive dimensions around the chosen interval.

### Quantitative measurements

- canonical result hash and stable-ID list;
- bounds and root scale;
- grid dimensions, effective cell size, cells across minimum dimension, and projected cell size in pixels;
- vertex/triangle counts by render/collision/deposit/debris and LOD;
- connected components, boundary/non-manifold edges, degenerate triangles, signed volume, contact footprint, fracture-angle retention;
- SDF surface residual and normal angular difference;
- sediment mass before/after, maximum repose violation, iteration residual;
- chunk-edge maximum height/normal/material/sediment mismatch;
- planner, field, extraction, smoothing, mesh upload, collider cooking, and integration times;
- managed/native temporary memory, cache weight, draw calls, batches, SetPass calls, overdraw diagnostic, and shader variant count;
- unload/reload and reversed-order equality.

### Approval loop

1. Capture baseline A and baked-equivalent B.
2. If B does not match A quantitatively, fix the experiment.
3. User decides whether perceptual scale from A/B is worth preserving.
4. Run C geometry density; user selects at most one interval.
5. Run C material wavelength on approved geometry.
6. Lock scale contract before terrain/sediment implementation.
7. At every later milestone, show baseline versus integrated result from the fixed gameplay camera and two worst-case azimuths.

## 15. Milestone sequence

No milestone authorizes the next one automatically. Each ends at its stop condition and preserves an isolation path.

### M0 — Baseline and scale calibration

- **Purpose:** faithfully bake the approved 0.1-sized individual rock into meter-valued geometry at a unit root and separate resolution/material calibration.
- **Exact scope:** A reference, target-architecture B, C resolution/material variants, meter references, bounds/resolution/material metrics, locked cameras/lights; no new geology behavior.
- **Expected files/systems:** existing rock workbench authoring/preview/mesher, new experiment data types and Editor comparison capture, EditMode tests.
- **Inputs/outputs:** current temporary individual rock and user-approved 0.1 physical conversion -> reproducible A/B panels, unit-root B fixture, metrics, selected resolution/material intervals.
- **Dependencies:** current workbench and fixed camera baseline.
- **Risks:** current scene no longer contains the exact reference transform; the conversion must not leak into formation/landmark/terrain scales; BigARM scale references conflict.
- **Automated validation:** A/B bounds and sample equivalence, unit roots, hash stability, projected-resolution metrics.
- **Required visual acceptance:** user confirms B preserves the approved smaller rock appearance and selects geometry-resolution/material-frequency intervals.
- **Stop condition:** B matches A at root scale 1 and the cells-across/minimum-dimension and material-frequency intervals are approved; otherwise do not build integration fields.
- **Rollback/isolation:** all experiment objects remain editor-only temporary fixtures; source hierarchy untouched.

### M1 — Geology data contract

- **Purpose:** establish one immutable recipe/result and identity/version boundary.
- **Exact scope:** value types, validators, stable hashes, bounds/halo, empty/reference field interfaces; no real uplift/sediment.
- **Expected files/systems:** new Runtime/TopDown3D/WorldCreator/Geology/GroundedGeology contracts/compiler interfaces; WorldVersionManifest extension; tests.
- **Inputs/outputs:** world identity + recipe + bounds -> empty/fixture GroundedGeologyResult with deterministic signature.
- **Dependencies:** M0 unit-root conversion proof and World Creator contracts.
- **Risks:** over-generalization or duplicating World Creator identities.
- **Automated validation:** immutable validation, stable seed streams/IDs, order independence, codec/version fixtures.
- **Required visual acceptance:** none beyond readable units/names in diagnostics.
- **Stop condition:** contract supports temporary and future handmade fixture adapters without UnityEngine.Object in pure result data.
- **Rollback/isolation:** new namespace/interfaces unused by production path until later adapter milestones.

### M2 — Editor Integration Workbench shell

- **Purpose:** make the contract inspectable before expensive behavior exists.
- **Exact scope:** fixture selection, seed lock, bounds, generation control, diagnostics, A/B, fixed cameras, field-view shell.
- **Expected files/systems:** new editor window/authoring component and visualization/capture utilities; existing workbench adapter.
- **Inputs/outputs:** fixture + M1 result -> temporary scene visualization and metrics.
- **Dependencies:** M1.
- **Risks:** Editor UI becoming a generator or mutating source objects.
- **Automated validation:** Undo, source preservation, temp lifecycle, locked seed, camera equality, no production catalog references.
- **Required visual acceptance:** user confirms usable review layout and fixture readability.
- **Stop condition:** empty/reference fields and A/B can be regenerated and compared non-destructively.
- **Rollback/isolation:** remove temporary workbench root/window; runtime contracts remain.

### M3 — Terrain uplift and apron

- **Purpose:** make terrain rise into the fixture from a shared footprint.
- **Exact scope:** footprint/spine, constrained uplift, apron, canonical query composition, editor view; no sediment.
- **Expected files/systems:** GroundedGeologyFieldCompiler, World Creator query composition seam, representation tests, Integration Workbench views.
- **Inputs/outputs:** recipe + base surface/constraints -> uplift/apron field and composed terrain samples.
- **Dependencies:** M1–M2.
- **Risks:** hard stamp rings, route obstruction, chunk seams, circular blandness.
- **Automated validation:** analytic profile tests, zero halo edge, slope/route gates, neighbor equality, reversed order.
- **Required visual acceptance:** terrain shoulder reads causal and non-stamped from gameplay/eight-angle views.
- **Stop condition:** approved footprint/uplift interval and no boundary/affordance regression.
- **Rollback/isolation:** feature flag returns canonical query to pre-geology surface.

### M4 — Contact and concavity fields

- **Purpose:** describe where rock, terrain, crevices, and shelter truly meet.
- **Exact scope:** coherent fixture occupancy sampler, contact/gap, slope/curvature, crevice/concavity, wind visibility/wake fields.
- **Expected files/systems:** field compiler modules and analytic fixtures; no final mesh rewrite.
- **Inputs/outputs:** uplifted support + structural fixture -> bounded 3D occupancy and 2D derived fields.
- **Dependencies:** M3.
- **Risks:** noisy curvature, sign errors, expensive ray samples, field mismatch with current mesh.
- **Automated validation:** sphere/box/bowl/dome/contact analytic tests, sign and range, halo continuity, fixed direction/order.
- **Required visual acceptance:** overlays correspond to perceived contacts/pockets and do not create arbitrary rings.
- **Stop condition:** contact and shelter explain chosen visible locations across three fixtures.
- **Rollback/isolation:** diagnostic fields remain optional consumers; production rendering unchanged.

### M5 — Sediment relaxation

- **Purpose:** generate finite, stable deposits at contact and shelter.
- **Exact scope:** target thickness, supply, deterministic thermal relaxation, optional editor-only deposit surface preview.
- **Expected files/systems:** GroundedGeologySedimentSolver, tests, workbench visualizations.
- **Inputs/outputs:** M4 fields + material sediment semantics -> target/final thickness and solver metrics.
- **Dependencies:** M4.
- **Risks:** mass creation, border seams, excessive iterations, blanket coverage.
- **Automated validation:** mass/non-negativity/repose/residual, halo crop equality, determinism, cancellation.
- **Required visual acceptance:** deposits pool and taper believably without melted or wet appearance.
- **Stop condition:** approved repose/supply interval and budget at standard quality.
- **Rollback/isolation:** result channel disabled; no terrain/material consumer yet.

### M6 — Talus and clutter

- **Purpose:** derive debris from formation geology.
- **Exact scope:** probability field, three required clutter tiers, variable-radius placement, grounding/burial/orientation; editor visualization first.
- **Expected files/systems:** GroundedGeologyTalusPlanner, placement records, adapter into natural-object realization, tests.
- **Inputs/outputs:** fracture/contact/slope/sediment -> sorted stable placement records.
- **Dependencies:** M4–M5.
- **Risks:** visible cells, category overlap, collision clutter, cross-chunk disagreement.
- **Automated validation:** min-distance, halo competition, route exclusions, ID stability, grounding, density response.
- **Required visual acceptance:** debris tells a downslope/fracture story and avoids uniform noise.
- **Stop condition:** one approved distribution per tier with declared collision policy.
- **Rollback/isolation:** visualization/adapter flag off restores current natural-object planner.

### M7 — Rock reconstruction improvements

- **Purpose:** improve coherent silhouettes and shading at approved scale.
- **Exact scope:** scale-relative sampling, SDF gradient normals, contact protection, bounded smoothing comparison; no adaptive/dual rewrite.
- **Expected files/systems:** workbench/shared extractor seam, mesher tests, metrics.
- **Inputs/outputs:** M4 occupancy + M0 quality profile -> render/collision candidate meshes.
- **Dependencies:** M0, M4.
- **Risks:** increased samples, loss of fractures/volume, editor/runtime code divergence.
- **Automated validation:** topology, residual, normals, volume/contact/fracture retention, timing/memory.
- **Required visual acceptance:** smooth mass plus readable fractures at gameplay camera and all angles.
- **Stop condition:** marching tetrahedra meets acceptance/budget or produces explicit evidence for dual-contouring prototype.
- **Rollback/isolation:** retain baseline extractor selectable in comparison workbench.

### M8 — Material integration

- **Purpose:** make terrain, rock, sediment, and debris share physical scale and contact.
- **Exact scope:** shared scale profile, semantic packing extension, aligned macro/micro layers, dust/contact/height blend, distance fade; POM only as isolated comparison.
- **Expected files/systems:** terrain/rock/deposit shaders and materials, WorldSurfaceMaterialService, packing adapter, validators.
- **Inputs/outputs:** geometry + field weights + material profile -> integrated URP rendering.
- **Dependencies:** M3–M7.
- **Risks:** variants, texture mismatch, black seams, excessive normal/parallax cost.
- **Automated validation:** shader compile/variant manifest, channel/import validation, finite weights, Frame Debugger capture metrics.
- **Required visual acceptance:** coherent scale/material response and subtle contact from gameplay/worst angles.
- **Stop condition:** baseline integrated material approved; POM/displacement separately accepted or rejected.
- **Rollback/isolation:** shared profile and shader keywords allow old material path as explicit comparison only during milestone.

### M9 — Performance and LOD

- **Purpose:** choose measured representation budgets.
- **Exact scope:** quality tiers, mesh/collider LOD, deposit/clutter distance policy, cache weights, batching/instancing comparison.
- **Expected files/systems:** representation profiles, LOD realization, profiler harness, stress tests.
- **Inputs/outputs:** approved study -> measured near/mid/far payloads and budgets.
- **Dependencies:** M6–M8.
- **Risks:** collider cooking spikes, LOD pops, memory pressure, premature GPU complexity.
- **Automated validation:** triangle/memory/time/draw-call budgets, LOD monotonicity, cancellation, cache eviction.
- **Required visual acceptance:** no objectionable pops/crawling or deposit disappearance.
- **Stop condition:** a CPU/runtime path meets budgets, or a specific CPU/GPU/adaptive gate has evidence.
- **Rollback/isolation:** quality profile selects last passing representation.

### M10 — Chunk/runtime integration

- **Purpose:** realize approved geology through World Creator streaming.
- **Exact scope:** plan discovery, intersecting-chunk requests, async compilation, main-thread integration, unload/reload, origin rebase, collision/traversal consumers.
- **Expected files/systems:** WorldCreatorProductionRuntime, scheduler/compiler, chunk builder, geological/natural-object adapters, persistence hooks.
- **Inputs/outputs:** absolute feature plan + chunk requests -> near/mid/far runtime representations.
- **Dependencies:** M3–M9.
- **Risks:** duplicate ownership, stale async results, border seams, hidden compatibility fallback.
- **Automated validation:** request-order equivalence, shared borders, cancellation, reload/rebase/save IDs, collider sync, scheduler budget.
- **Required visual acceptance:** runtime result matches workbench and remains stable through visible LOD/stream transitions.
- **Stop condition:** one study feature streams correctly with no alternate authority.
- **Rollback/isolation:** production feature flag excludes grounded geology plans and restores prior adapter path.

### M11 — Handmade-reference substitution

- **Purpose:** prove the definitive handmade reference is an input, not an architectural dependency.
- **Exact scope:** adapter maps approved handmade hierarchy/constraints into M1 recipe; rerun all golden-study comparisons.
- **Expected files/systems:** fixture adapter and reference-specific validation only; no consumer redesign.
- **Inputs/outputs:** user handmade formation -> same GroundedGeologyResult contract and metrics.
- **Dependencies:** user supplies/approves reference; M10.
- **Risks:** reference contains structure the recipe cannot express or non-unit transforms.
- **Automated validation:** adapter completeness, unit conversion, stable IDs, unchanged consumer APIs, full matrix regression.
- **Required visual acceptance:** user confirms reference character survives integration.
- **Stop condition:** reference passes or produces a narrowly documented contract gap.
- **Rollback/isolation:** retain temporary fixture; reference asset is never destructively normalized.

### M12 — Extract into Rock, Formation, Landmark, and Landscape generators

- **Purpose:** generalize proven rules without parallel generation paths.
- **Exact scope:** move recipe producers/adapters into each existing authoring scale; route runtime formation/landmark/landscape realization through shared result.
- **Expected files/systems:** existing workbench generators/authoring, WorldRockFormationPlanner, landmark generator, runtime adapters, docs/tests.
- **Inputs/outputs:** scale-specific authoring intent -> common recipe/result -> scale-appropriate representations.
- **Dependencies:** M11 and accepted golden study.
- **Risks:** premature universal abstraction, hidden old planners, category regressions.
- **Automated validation:** existing suites plus cross-generator contract, no duplicate authority, deterministic extraction fixtures.
- **Required visual acceptance:** representative existing categories retain identity and benefit from grounded integration.
- **Stop condition:** each generator is a recipe producer/consumer adapter; no duplicated field/reconstruction implementation remains.
- **Rollback/isolation:** migrate one generator per commit/feature gate; keep explicit old adapter until that generator passes, then remove it in a dedicated cleanup.

## 16. File-level change map

These are implementation predictions, not authorization. Exact names may change at M1 review, but assembly direction may not.

### Likely existing runtime files to extend

- Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Identity/WorldVersionManifest.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Geology/WorldRockFormationPlanner.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Materials/WorldSurfaceMaterialService.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Query/WorldQueryContracts.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Query/UnboundedHybridWorldQueryService.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Representation/WorldRepresentationCompiler.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Representation/WorldTerrainMaterialPackingAdapter.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Runtime/WorldCreatorProductionRuntime.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Streaming/WorldRepresentationScheduler.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DChunkMeshBuilder.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DGeologicalRockAdapter.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DNaturalObjectPlanner.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DNaturalObjectDecorator.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DDustDepositionPlanner.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DDustDepositionDecorator.cs
- Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DTraversalObstacle.cs

### Likely new runtime files

Under Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Geology/Grounded/:

- GroundedGeologyRecipe.cs
- GroundedGeologyIdentity.cs
- GroundedGeologyGenerationInput.cs
- GroundedGeologyResult.cs
- GroundedGeologyFieldDescriptors.cs
- GroundedGeologyPlanner.cs
- GroundedGeologyFieldCompiler.cs
- GroundedGeologyFootprint.cs
- GroundedGeologyOccupancy.cs
- GroundedGeologyContactCompiler.cs
- GroundedGeologyWindCompiler.cs
- GroundedGeologySedimentSolver.cs
- GroundedGeologyTalusPlanner.cs
- GroundedGeologyRepresentationProfile.cs
- GroundedGeologyMaterialScale.cs

Prefer cohesive files over one type per file if review shows excessive fragmentation. Pure math/data stays in BooterBigArm.TopDown3D.Runtime.

### Likely existing editor files to extend

- Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchMesher.cs
- Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchPreview.cs
- Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchAuthoringEditor.cs
- Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchFormationEditor.cs
- Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchFormationGenerator.cs
- Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockLandmarkEditor.cs
- Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockLandmarkGenerator.cs

### Likely new editor files

Under Assets/_Project/Scripts/Editor/TopDown3D/GroundedGeology/:

- GroundedGeologyIntegrationWorkbench.cs
- GroundedGeologyFixtureAuthoring.cs
- ExistingWorkbenchFixtureAdapter.cs
- HandmadeFormationFixtureAdapter.cs
- GroundedGeologyFieldVisualizer.cs
- GroundedGeologyComparisonBuilder.cs
- GroundedGeologyCaptureUtility.cs
- GroundedGeologyMetrics.cs
- GroundedGeologyValidationMenu.cs

Editor types remain in the existing editor-only assembly. If shared extraction math is moved from the editor mesher, place it in runtime pure code and keep all UnityEditor calls in Editor.

### Likely shaders/material assets

- Extend Assets/_Project/Shaders/TopDown3D/BrokenWorldTerrainBlend.shader.
- Converge capabilities from BrokenWorldRockWorkbenchPBR.shader into BrokenWorldRockTriplanar.shader or a shared include; do not maintain two production shader authorities.
- Extend BrokenWorldDepositedDust.shader only if a real deposit shell passes M5.
- Add one shared HLSL include for geology material measurements/packing if it reduces duplication.
- Extend existing materials rather than cloning per formation category.

### Likely tests

Under Assets/_Project/Tests/Editor/WorldCreator/GroundedGeology/:

- GroundedGeologyIdentityTests.cs
- GroundedGeologyFootprintTests.cs
- GroundedGeologyFieldTests.cs
- GroundedGeologyChunkBoundaryTests.cs
- GroundedGeologySedimentTests.cs
- GroundedGeologyTalusTests.cs
- GroundedGeologyReconstructionTests.cs
- GroundedGeologyRepresentationTests.cs
- GroundedGeologyPersistenceTests.cs
- GroundedGeologyPerformanceTests.cs

Extend existing WorldRepresentation, WorldSurfaceMaterial, NaturalObject, DustDeposition, RockWorkbench, Formation, Landmark, and SmartTraversal tests instead of duplicating their coverage.

### Predicted scenes/prefabs/settings

- Assets/_Project/Scenes/TopDown3D/LandscapeAuthoringSandbox.unity: later add one temporary Integration Workbench root only after scene authority and dirty-state review.
- A dedicated study scene is preferable only if sandbox ownership or repeatable lighting cannot be isolated; creating it requires a separate approved milestone.
- TopDown3DPrototype.unity: no change before M10.
- Existing rock/formation/landmark prefabs and generated catalog: preserved until extraction milestone.
- World settings/profile: add grounded-geology versions/budgets only after M1/M9; no project-wide setting changes.

### Documentation

- Update Docs/ROCK_FORMATION_WORKBENCH.md, Docs/ROCK_LANDMARK_WORKBENCH.md, Docs/WORLD_CREATOR_ARCHITECTURE_PLAN.md, Docs/WORLD_SYSTEMS_STANDARD.md, Docs/PROJECT_STATUS.md, and Docs/DOCS_INDEX.md only in their owning implementation milestones.
- Mark superseded compatibility routes explicitly only after their replacement is proven.

## 17. Validation matrix

| Concern | Automated evidence | User-owned evidence | Gate |
|---|---|---|---|
| Compilation | exact mirrored Unity batchmode compile/EditMode XML | none | zero compile errors; all selected tests pass |
| Repeated seed | canonical result/mesh/placement hashes over repeated and shuffled runs | same visible result | exact equality |
| Stable identity | owner, feature, subfeature, placement IDs across requests | none | no ID drift |
| Chunk boundaries | shared border/halo sample comparison for height, normals, fields, placements | no visible seam | numeric tolerance plus visual |
| Unload/reload | destroy/recreate representation and compare hashes/IDs | no pop beyond LOD policy | exact canonical equality |
| Save/restore | serialize deltas, regenerate, reapply, compare target IDs | changed/unchanged objects make sense | codec and orphan policy pass |
| Mesh topology | finite vertices/normals, winding, manifold/boundary/degenerate/component counts, volume/contact drift | silhouette/fractures | invariants in Section 9 |
| Collider sync | sampled distance/penetration and bounds against approved collision source | movement/contact feel | tolerance plus user traversal |
| Terrain traversal | Booter/BigARM affordance samples, route preservation, slope/clearance | feel/readability | no protected-route regression |
| Materials | channel/import/weight/shader compilation and capture equality | physical scale/contact/dust taste | no errors plus approval |
| LOD | monotonic triangles, screen thresholds, border matching, collision policy | no objectionable pop/crawl | metrics plus approval |
| Performance | profiler markers, p50/p95 generation/integration/cooking, memory/cache/draw calls | acceptable visual tradeoff | Section 2 budgets |
| Multi-angle | deterministic capture manifest and image-difference diagnostics | gameplay camera + eight azimuth review | user approval |

Test policy:

- Run only directly relevant EditMode/compile/validator checks at each milestone.
- When Temp/UnityLockfile prevents safe batchmode, validate an exact temporary mirror that excludes Library, Temp, Logs, UserSettings, and Assets/_Recovery; report that this is mirror proof, not live GUI proof.
- Do not create or run gameplay smoke tests unless the user separately requests them.
- Visual acceptance is never inferred from passing screenshots, hashes, or automated image differences.

## 18. Risks and decision gates

| Gate | Experiment/evidence | Accept default when | Escalate when |
|---|---|---|---|
| Individual-rock scale conversion | M0 A/B | unit-root B matches A's approved world bounds and appearance after the 0.1 geometry bake | any unmatched variable prevents equivalence; persistent root 0.1 is not the fallback |
| BigARM reference | query envelope vs current scene collider and design authority | one authoritative physical/traversal envelope is approved | mismatch changes terrain/landmark clearance materially |
| Marching tetra vs dual contouring | M7 same fields/cameras/budgets | marching tetra + gradients meets silhouette/fracture budget | sharp features fail or uniform cost is excessive |
| Heightfield vs local apron mesh | M3 edge/contact error at 0.75 m base spacing | query-composed heightfield plus sediment/overlap passes | shoulders/deposit silhouettes cannot pass |
| TerrainData | architecture review only | reject for current custom mesh streaming | project deliberately replaces terrain backend |
| CPU vs GPU fields | M9 p95 compile, memory, cancellation | pure CPU/background path meets scheduler/cache budgets | measured workload misses target after algorithmic fixes |
| POM/displacement | M8 fixed-camera cost/benefit | normal/height blend is adequate | close-range projected detail is visibly lacking |
| Collider complexity | primitive/compound vs decimated mesh vs render mesh profile | conservative lower-cost shape preserves traversal | mismatch causes false blocks/falls or loses required shape |
| Clutter rendering | combined vs SRP-batched GameObjects vs explicit instancing | simplest path meets draw/main-thread/memory budget | density produces measured submission bottleneck |
| Cache vs regenerate | revisit latency, cache hit rate, memory weight | regenerate/canonical cache within 96 MB budget | measured revisit spikes justify versioned cache |
| Sediment geometry | projected-thickness and contact comparison | shader blend plus sparse shell passes | deposits need silhouette or cover a heightfield seam |
| Field storage | analytic sampler vs bounded grids | recomputation/sample cost meets budget | repeated consumers justify pooled cached grids |

### Major risks

- The approved smaller individual rock can be mistaken for permission to scale every geology layer by 0.1. M0 confines the conversion to that fixture and proves its meter-space equivalent.
- A “unified system” can become a monolithic manager. The immutable result and consumer boundaries block that.
- Editor source volumes can leak into runtime authority. Fixture adapters and runtime-safe recipes block that.
- Chunk-local normalization or candidate order can create seams. Absolute samples, owner cells, halos, and stable ordering block that.
- Smoothing can erase geological planes and contact. Protected bands and quantified invariants block that.
- Material polish can disguise bad geometry temporarily. Geometry-only approval precedes integrated material.
- Real debris can overwhelm physics/render budgets. Tiered representation and explicit collision thresholds block that.
- A temporary fixture or non-canon World Creator profile can become accidental canon. Labels, IDs, and catalog exclusion block that.
- Existing dirty generated assets/scenes can be accidentally staged or normalized. Each milestone uses exact file ownership and narrow commits.

## 19. Recommended first implementation batch

### Batch decision

Implement **M0 + M1 + the shell portion of M2 only**. Stop before terrain uplift, final occupancy, sediment, material integration, or clutter.

### Exact first-batch scope

1. Add the immutable GroundedGeologyRecipe, GenerationInput, Result metadata, identity/version, bounds/halo, resolution profile, field-descriptor interfaces, and validation.
2. Add deterministic seed-stream and stable-ID derivation that reuses World Creator identity conventions.
3. Add one ExistingWorkbenchFixtureAdapter that reads the individual Rock Workbench for M0 scale comparison and an existing Formation Workbench hierarchy for the integration shell, producing recipe snapshots without changing either source.
4. Add an editor Integration Workbench shell with fixture selection, temporary/non-canon badge, seed lock, root-scale warning, physical bounds, cells-across-minimum-dimension, effective grid metrics, fixed camera registry, A/B/C slots, and placeholder field modes.
5. Add A/B scale baking utilities and metrics only. Do not alter the production workbench defaults or scene fixture yet.
6. Add tests for identity, validation, A/B physical equivalence, unit-root output, source preservation, deterministic hashes, resolution caps, and temporary preview lifecycle.
7. Produce a user review packet with reference A, target B, and limited C geometry/material panels from the same seed/camera/light. Stop for confirmation of the baked conversion and selection of C intervals.

### First-batch files

Expected new:

- Runtime grounded-geology contract/identity/profile files under WorldCreator/Geology/Grounded/.
- Editor Integration Workbench, fixture adapter, comparison, capture, visualization-shell, and metrics files.
- GroundedGeology M0/M1/M2-shell EditMode tests.

Expected narrowly extended:

- WorldVersionManifest.cs only if a new independent version domain is needed immediately.
- TopDown3DRockWorkbenchMesher.cs or a small extracted pure utility only for A/B metric access; no reconstruction change.
- No production shaders, materials, runtime scene, prototype scene, terrain queries, natural-object planner, or dust system.

### First-batch proof

- compile and selected EditMode suites pass from a safe exact mirror if Unity remains locked;
- exact candidate-file diff and no unrelated dirty paths staged;
- source formation hierarchy hash before/after is identical;
- A and B bounds/collider/sample landmarks match;
- repeated recipe/result metadata hashes match;
- comparison packet uses locked camera/light/seed;
- B's root is exactly (1,1,1) and its baked world bounds match the approved A size;
- user confirms the baked physical result and chooses geometry-resolution and material-frequency intervals.

### Stop line

Do not implement the complete terrain, material, sediment, talus, clutter, runtime streaming, or production scene integration after this batch. The next authorized batch starts only after the M0 baked-equivalence confirmation and M1 contract review.

## Research basis

The plan uses primary/official sources as decision support, not as proof of this project's behavior:

- Unity 6 Transform guidance states that 3D physics assumes roughly one world unit per meter and warns that Transform scale and non-uniform parent scaling affect physics/hierarchies: [Unity Transform component](https://docs.unity3d.com/6000.0/Documentation/Manual/class-Transform.html) and [Transform.lossyScale](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Transform-lossyScale.html).
- Runtime-generated mesh colliders require cooking and are the most expensive static collider class; Unity recommends deliberate cooking/prebaking and simpler/compound colliders where they fit: [Unity collider types and performance](https://docs.unity3d.com/6000.0/Documentation/Manual/physics-optimization-cpu-collider-types.html) and [MeshCollider cooking options](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MeshColliderCookingOptions.html).
- Unity LOD transitions are screen-relative, supporting gameplay-camera calibration rather than distance-only guesses: [LOD.screenRelativeTransitionHeight](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/LOD-screenRelativeTransitionHeight.html).
- Unity documents SRP Batcher/GPU-instancing tradeoffs and recommends profiling the chosen batching path: [GPU instancing](https://docs.unity3d.com/6000.0/Documentation/Manual/gpu-instancing-enable.html) and [batching setup](https://docs.unity3d.com/6000.0/Documentation/Manual/DrawCallBatching-SetUp.html).
- TerrainData stores a single heightmap and SetHeights updates are synchronization/LOD work; these APIs do not justify replacing the current custom mesh terrain path: [TerrainData](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TerrainData.html) and [TerrainData.SetHeights](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TerrainData.SetHeights.html).
- Bridson's original fast Poisson-disk method provides the basis for minimum-radius blue-noise candidates; this plan extends the acceptance test to deterministic variable radii and chunk halos: [Fast Poisson Disk Sampling in Arbitrary Dimensions](https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf).
- Lorensen and Cline's original isosurface work uses scalar-field gradients for surface normals; the study applies that principle to the existing extractor before a topology rewrite: [Marching Cubes: A High Resolution 3D Surface Construction Algorithm](https://graphics.stanford.edu/courses/cs348a-21-winter/Papers/Marching_Cubes.pdf).
- Ju, Losasso, Schaefer, and Warren's dual contouring method uses Hermite intersections/normals and QEF minimization to preserve features and supports adaptive contouring; this supports a gated prototype, not a preemptive rewrite: [Dual Contouring of Hermite Data on Joe Warren's original publications page](https://www.cs.rice.edu/~jwarren/research/index.html).
- Musgrave, Kolb, and Mace's original eroded-terrain work describes thermal erosion and talus behavior; this plan adopts only a dry, shallow, mass-conserving angle-of-repose relaxation appropriate to the Broken World: [The Synthesis and Rendering of Eroded Fractal Terrains](https://doi.org/10.1145/74333.74337).

## Plan self-audit

### Dependency audit

- Scale is resolved before field, material, collision, or LOD tuning.
- Recipe/identity contracts precede the editor integration shell and every consumer.
- Uplift precedes contact; contact precedes sediment/talus; fields precede materials and runtime extraction.
- Reconstruction can improve after occupancy exists without changing the recipe.
- Performance precedes production streaming activation.
- Handmade-reference substitution precedes broad generator extraction but does not block M0–M10.
- Traversal/collision consume the same result and are tested before runtime completion.
- Persistence and version effects are present from M1, not bolted on after streaming.

### Architectural-drift audit

- No new monolithic manager is proposed.
- No second terrain authority, runtime workbench generator, or hidden fallback is introduced.
- World Creator remains canonical for absolute planning/query/identity/streaming.
- Existing compatibility adapters are preserved until proven replacements exist, then removed explicitly.
- Generated meshes, grids, textures, and GameObjects remain disposable representations.
- Player deltas remain separate from generated base data.
- The 20–30 m study is an integration fixture, not a special production-world exception.

### Scope audit

- Additional formation categories are frozen.
- Existing handmade/procedural assets are preserved.
- The later handmade reference is supported but not a foundational blocker.
- TerrainData, dual contouring, GPU generation, POM/displacement, cache expansion, and new rendering backends have evidence gates.
- No water, plants, destructive terrain, new packages, project settings, visual generation, or gameplay smoke tests are included.
- First implementation stops at calibration, contract, workbench shell, and visualization.

### Unresolved user visual decisions

The plan intentionally does not decide:

1. Whether the unit-root B conversion faithfully preserves the already selected smaller individual-rock appearance.
2. The approved geometry-density interval and material wavelength.
3. The final large/medium/small silhouette balance and fracture sharpness.
4. Terrain shoulder/uplift character and sediment amount/repose appearance.
5. Talus density, tier proportions, burial, and clutter negative space.
6. Contact-darkening/dust strength and whether bounded parallax is visibly worthwhile.
7. Acceptable LOD transitions from the gameplay camera.
8. Whether the definitive handmade reference exposes a missing recipe capability.

### Planning blockers

- No blocker prevents M0/M1/M2-shell implementation.
- Exact pixel recreation of the prior 0.1 view is unavailable from current serialized scene state, but this is not an architecture blocker: the user has approved baking the 0.1 physical-size factor into the individual rock, and can confirm the reconstructed B visually.
- Final BigARM traversal scale requires reconciling the 3.5 m-radius World Creator proof profile with the current 1.1 x 2.2 x 1.3 m scene collider.
- Live Unity visual validation is intentionally not performed by this planning task, and the existing Unity lock/dirty scene state must be rechecked before implementation.
