# Greater Wasteland chunk architecture and performance practices

Research date: October 11, 2026, America/Phoenix. Status: researched recommendation, not a production implementation or a guaranteed frame rate.

Use chunks to own procedural population, spatial queries, rendering batches and saved changes while the fixed terrain remains loaded. My recommended starting design is **256 metre population identity cells, 64 or 128 metre presentation subcells, and the existing 1,024 metre sectors for coarse lookup**. Fix identity independently of the rendering layout so performance tuning cannot reroll the world. First reduce native terrain rendering work; then establish deterministic population and bounded object realization before filling the landscape at scale.

The author confirmed that terrain always stays loaded, landscape population is procedural, and chunks must be used. Procedural population can use authored asset libraries, placement rules, protected anchors and exclusion masks. These constraints shape generation rather than requiring manual placement of every object. Existing accepted camp, waypoint and resource content needs compatible procedural recipes and explicit migration before replacement. This research does not change those objects or activate a generator.

## Current scale and controlling constraints

Greater Wasteland contains 8,448 native Terrain components and TerrainColliders across approximately 553.65 square kilometres. Tiles are 256 metres wide with 257 × 257 height grids; the existing organization has 528 one-kilometre sectors. Unity is pinned to `6000.4.0f1`, URP to `17.4.0`. Terrain instancing and the SRP Batcher are already enabled. The current renderer uses Forward, and GPU Resident Drawer is disabled.

The [performance audit](../../Evidence/Performance/GREATER_WASTELAND_AUDIT_2026-10-11.md) measured 215 ms per offscreen rendered update normally, 83 ms with distant heightmaps hidden, and 9.7 ms with distant Terrain components disabled while terrain data and colliders remained resident. This identifies the first workload to bound; it is not standalone Player FPS or proof that the desktop stability problem is resolved.

[Root authority](../../../AGENTS.md) and [the fixed-terrain dressing direction](../../Design/Gameplay/FIXED_TERRAIN_DRESSING_PLAN.md) control this research. Older infinite/generated-terrain and terrain-streaming language does not authorize terrain streaming here. The generated-world reference's terrain generator and save services remain inactive. [The research receipt](greater-wasteland-best-practices-sources-2026-10-11.json) records sources, current-version checks and scale calculations.

## Give different chunk layers different jobs

| Layer | Initial size to study | Owns | Residency and identity |
| --- | --- | --- | --- |
| Native terrain backend | Current 256 m tiles | Authored height, holes and native terrain collision | Always resident; a later representation study may group tiles |
| Population identity cells | 256 m | Deterministic candidates, recipe keys, sparse gameplay changes | Permanent logical coordinates, independent of renderer lifetime |
| Rendering and work subcells | Compare 64 m and 128 m | Instance batches, visible density, generation jobs and visual LOD | Presentation can be realized or released without changing world identity |
| Coarse sectors | Existing 1,024 m | Candidate-region lookup, large-POI constraints and terrain rendering sets | Directory over smaller cells, not a requirement to evaluate every sector every frame |
| Gameplay relevance regions | Actor-dependent | Interaction, necessary prop collision and active simulation | Include every relevant actor; do not derive these regions solely from the camera |

These sizes are proposals for comparison, not universally optimal Unity settings. Using the native grid first reduces integration risk. Population ownership need not equal the TerrainData size, render-batch size, or physics relevance radius.

At an illustrative density of one population element per 100 square metres, this area could imply roughly 5.5 million elements. That is not a requested density; it explains why logical existence should not require one live GameObject, collider and Update callback for each element. Store seeds/rules and sparse changes, evaluate necessary cells, and instantiate only the representation needed for current rendering or gameplay. Terrain itself remains resident throughout.

## Make procedural identity independent of execution order

Assign candidates from a canonical tuple such as world identity, population schema, recipe identity, fixed identity-cell coordinates and candidate index. Derive variation from that tuple rather than a shared mutable random sequence. Give a candidate its identity **before** mask, slope, spacing or density rejection; do not number only the accepted results. Placement-affecting rule changes require versioning and a migration policy. Rendering-only changes should preserve placements and IDs.

Counter-based random generators demonstrate the stateless key/counter principle: the same input tuple produces the same output regardless of evaluation order. This is a useful design reference; the research does not install Random123 or promise that a particular hash is a statistically adequate production generator. [D. E. Shaw Research Random123](https://github.com/DEShawResearch/random123).

Evaluate overlapping placement constraints using a neighbor halo and a canonical ownership rule. Large formations, roads and POIs can cross several cells while retaining one owning identity. Neighbor evaluation must not publish duplicate resources or change because another cell happened to generate first. [The experimental study](GREATER_WASTELAND_CHUNK_EXPERIMENTS_2026-10-11.md) tests an order-independent candidate-priority approach and explains its limits.

Use terrain revision, recipe revision and relevant authored-mask revision to invalidate derived placement caches. Keep accepted routes, safe spawns, companion clearance, terrain holes and POI footprints in those constraints. Generation decides the baseline; depletion, destruction, movement and player placement are explicit saved changes to that baseline.

## Bound active terrain rendering first

Build a complete scene-owned terrain index once. The existing one-kilometre sector hierarchy can find the local render region without scanning all 8,448 components every frame. Compare the existing 3 × 3 sector neighborhood, up to 144 current tiles, as a conservative initial study. The audit's 52-tile result is useful evidence, not a production hard limit.

Use camera coverage, camera motion/look-ahead, all relevant cameras, and shadow-caster coverage to decide the active set. Twilight's low sun makes shadow coverage particularly important; a caster can affect visible terrain from outside the camera view. Use hysteresis and prepare entering tiles before the camera needs them so cell changes do not create holes or activation spikes. Leave the terrain GameObjects, TerrainData and TerrainColliders present.

Unity's CullingGroup supplies sphere visibility and distance bands, but results arrive during camera culling rather than immediately after a change; multiple cameras require aggregation. It is a candidate for irregular props and markers. A direct grid lookup is the simpler first choice for this fixed rectangular terrain. Neither mechanism automatically removes active Terrain components from native work: the manager must explicitly apply safe presentation state. [Unity CullingGroup guidance](https://docs.unity3d.com/6000.0/Documentation/Manual/CullingGroupAPI.html).

[FixedTerrainSurfaceIndex](../../../Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainSurfaceIndex.cs) currently rejects disabled Terrain components. Decouple render enablement from valid native collision before shipping this approach, and audit uses of `Terrain.activeTerrains` so a reduced render set is not mistaken for the whole world. Verify seams, holes, outer edges, remote route queries and teardown restoration.

Terrain pixel error and LOD complexity settings reduce geometry detail; drawing flags and instancing are different controls. They do not establish a budget on thousands of native components. Keep the current geometry quality until a local comparison shows a useful tradeoff. [Unity terrain settings](https://docs.unity3d.com/6000.0/Documentation/Manual/terrain-OtherSettings.html).

## Compare larger native terrain blocks without changing logical chunks

| Native tile width | Height grid | Whole-footprint component count | Grid spacing | Total grid samples including seams |
| --- | --- | ---: | ---: | ---: |
| 256 m, current | 257 × 257 | 8,448 | 1 m | 557,981,952 |
| 512 m study | 513 × 513 | 2,112 | 1 m | 555,812,928 |
| 1,024 m study | 1025 × 1025 | 528 | 1 m | 554,730,000 |

Unity supports these heightmap resolutions. The counts and retained spacing are calculations for the current footprint, assuming complete aligned groups; they are not measured performance gains or a claim that 1 km is the best tile size. [Unity heightmap resolution API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/TerrainData-heightmapResolution.html).

A 4 × 4 grouping can cut component count sixteenfold while reducing grid samples by only about 0.6%. The benefit sought is native-object, renderer and collider overhead, not a sixteenfold reduction in height-data memory. Compare one accepted sector at 256, 512 and 1,024 metre native layouts before any world conversion. Larger blocks may change culling granularity and activation cost.

Require world-space height and quantization parity, hole parity, texture/splat/detail parity, borders, native collision and material/shadow parity. Adapt the index and connectivity code, which currently assume 256 metre physical tiles, while retaining 256 metre logical population identity. Preserve original assets and metadata; create a versioned representation and reference receipt rather than deleting or regenerating accepted GUIDs.

## Use shared assets and spatially bounded prop batches

Procedural population does not require generating novel mesh geometry for every pebble. Use reusable authored mesh/material libraries, deterministic composition and variation, with selected formations generated or cached where their visible value warrants it. Keep interactive records and simplified physical shapes separate from visual LOD. Use LODGroup's screen-size selection for ordinary mesh props, and pool temporary presentations instead of repeatedly allocating and destroying objects. [Unity LODGroup](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/LODGroup.html).

GPU Resident Drawer is a promising compatible-prop study, not the first native-terrain fix. Unity 6.4 requires compatible MeshRenderers/shaders and Forward+ or Deferred+; MaterialPropertyBlock use is restricted. Our current renderer is Forward, and some existing decoration, sticker and preview paths use property blocks. A renderer-path study therefore needs shader, atmosphere, UI, shadow and lighting validation. Benefits depend on actual repeated meshes and shader variants. [Compatibility](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/make-object-compatible-gpu-rendering.html), [enabling requirements](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/gpu-resident-drawer.html), [performance considerations](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/gpu-resident-drawer-performance.html).

Keep batching bounds local enough to cull. A single combined whole-world mesh prevents individual mesh culling; automatically enabling dynamic batching can also add CPU work. Choose shared instancing or bounded cell batches after comparing their costs. [Unity batching guidance](https://docs.unity3d.com/6000.4/Documentation/Manual/DrawCallBatching.html).

## Budget generation and maintenance work

Use a scheduler with explicit time/work limits for population evaluation, mask processing, mesh construction, buffer upload and presentation activation. Process different portions of a workload over successive frames rather than doing every cell at once every few frames. Prioritize required actor collision and imminent camera coverage; avoid evaluating the whole 554 square kilometres on entry to Play Mode. Unity recommends time slicing, caching and update managers when numerous callbacks become costly. Our stationary script audit was inexpensive, so this is mainly a population-growth architecture and a fix for known spikes, not a reason to rewrite every controller. [Unity code architecture guidance](https://unity.com/how-to/advanced-programming-and-code-architecture).

Apply failed-route retry deadlines even when BigARM's path list is empty. Keep him physically active at his real position regardless of camera visibility. Prop collision relevance must include Booter, BigARM and any other authoritative actors, with sufficient lead time for fast movement and route planning. Releasing a visual batch must not reset resources, stop persistent harvesters, erase placed objects or remove terrain collision.

Capture immutable save data on the main thread, then serialize and write through one ordered worker with a bounded pending queue. Unity permits background `JsonUtility.ToJson`, provided its input is not mutated during serialization. Preserve atomic replacement, durable flush, recovery backups and version checks. Completion must acknowledge the captured revisions, not whatever revision the live state has reached later. Save sparse chunk deltas rather than serializing every unchanged procedural placement. [Unity JSON threading guidance](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/JsonUtility.ToJson.html).

## Keep memory and physics budgets explicit

Inventory CPU/GPU asset copies, native terrain/collision storage, render targets and instance buffers. Read/Write-enabled textures and meshes retain CPU-accessible copies; disable unnecessary copies only after checking deformation, mesh processing and collider requirements. Native terrain remains resident, so rendering control and LOD cannot make its fixed data footprint disappear. [Readable textures](https://docs.unity3d.com/cn/6000.0/ScriptReference/Texture-isReadable.html), [readable meshes](https://docs.unity3d.com/cn/6000.0/ScriptReference/Mesh-isReadable.html).

Texture mip residency is a separate optional study, not terrain streaming. Automatic mip choice has limitations with custom geometry and nonstandard or modified UVs. Our world-space terrain shader needs explicit validation before a global streaming switch; manual requests may be necessary. Begin with shared/compressed assets and eliminated unnecessary copies, then measure. [Unity mipmap streaming limits](https://docs.unity3d.com/6000.4/Documentation/Manual/TextureStreaming-use.html).

The physics project currently uses Sweep and Prune. Unity identifies grid-based broad phases as candidates for large mostly-static scenes, with their own memory and setup costs. Compare Automatic Box Pruning only if moving/blocked-actor profiling shows broad-phase cost; the idle audit did not show physics as the main problem. Preserve collision masks and gameplay accuracy. [Unity broad-phase guidance](https://docs.unity3d.com/6000.4/Documentation/Manual/physics-optimization-cpu-broad-phase.html).

Do not make a whole-world occlusion bake the first optimization. Unity's baked occlusion costs CPU and memory and is especially useful in enclosed, well-separated spaces or GPU-overdraw-bound scenes. A mostly open, terrain-CPU-bound wasteland is a weaker initial fit. Local structures and the optional GPU-culling experiments can still be evaluated separately. [Unity occlusion guidance](https://docs.unity3d.com/6000.4/Documentation/Manual/OcclusionCulling.html).

## Implementation sequence and proof

1. Establish the fixed world and population identity contracts, then implement the bounded terrain rendering set and correct collision-query semantics. No world population is needed to prove this first seam.
2. Implement one constrained population area using identity cells, neighbor context, recipe/version keys and sparse deltas. Compare 64/128 metre presentation batches without changing identities.
3. Add bounded generation/presentation work and actor-aware interaction/collision regions. Verify boundary handoff, offscreen BigARM and save/recreate behavior before scaling density or area.
4. Compare native tile consolidation locally and conventional shared-mesh/LOD rendering. Keep each representation change independently reversible.
5. Evaluate the [experimental techniques](GREATER_WASTELAND_CHUNK_EXPERIMENTS_2026-10-11.md) on that same bounded population, then choose the simplest backend that meets measured budgets.
6. Validate in a Development Player on the current PC, with startup and steady frames measured separately, fixed resolution/content/seed, warmup exclusion, P50/P95/P99, CPU/GPU availability, active counts and memory peaks. A proposed first target is stable 60 FPS at 1080p; this target and subsystem budgets still require a Player baseline and author acceptance. [Unity target-platform profiling](https://docs.unity3d.com/6000.4/Documentation/Manual/profiling-target-device.html).

Determinism checks must vary chunk order, worker completion order, rendering layout and visibility. Boundary checks must include negative coordinates, corners, neighboring spacing, exclusions and multi-cell features. Restore/recreate presentations while retaining depletion and placed-object state. Do not use Unity instance IDs as persistent identities. World/recipe version migration is explicit; unload/reload concerns apply to population representations and caches, while terrain unload/reload is not applicable because the terrain stays resident.

This task used research, static source inspection, offline API compilation and a small synthetic CPU experiment. It launched no Unity Editor or Player, altered no gameplay/terrain/pipeline settings and performed no gameplay smoke test. The installed API check rejected `Terrain.enableHeightmapLODFrustumCulling` despite its appearance in a published API listing, so the plan does not depend on that toggle. Other referenced Terrain, CullingGroup, LODGroup, indirect-rendering and readback signatures compiled against the pinned assemblies; runtime/device support and performance remain separate proof.
