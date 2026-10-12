# Greater Wasteland procedural chunk experiments

Research date: October 11, 2026, America/Phoenix. These experiments extend the [recommended chunk architecture](GREATER_WASTELAND_CHUNK_BEST_PRACTICES_2026-10-11.md). Terrain and native terrain collision remain resident. All landscape population comes from procedural recipes constrained by terrain, authored rules and protected sites. Population identity, presentation residency and gameplay relevance have separate lifecycles.

The most useful first experiments are deterministic chunk ownership, local terrain rendering control, and shared-mesh population rendering. GPU-generated cosmetic clutter and GPU culling follow after those contracts work. A custom terrain mesh/clipmap renderer is a later comparison if simpler native-terrain improvements remain insufficient. None of these techniques requires regenerating the landscape's authored heights.

## Experimental options and decision gates

| Experiment | Potential benefit | Main risk | Accept only after |
| --- | --- | --- | --- |
| Keyed candidates with neighbor halos | Parallel/order-independent population, consistent borders and stable saved IDs | Version mistakes, density artifacts, inconsistent neighboring rules | Reversed/shuffled evaluation, border/corner ownership, exclusions and saved-delta checks |
| 256 m identity cells with 64/128 m presentation work | Smaller culling/upload units without rerolling content | Too many tiny batches or activation bursts | Same generated identities across partitions, measured scheduler and boundary cost |
| Native terrain grouping at 512/1,024 m | Fewer native components while preserving height spacing | Coarser culling, changed material/height/hole/collision contracts | One-sector parity and performance comparison; original assets retained |
| Shared meshes with indirect GPU draws | Reduce per-instance GameObject and draw-submission cost for dense clutter | Oversized bounds, custom shaders, overdraw and GPU memory pressure | A bounded instance-buffer/LOD budget and comparable rendered Player results |
| GPU-generated cosmetic placement | Move suitable bulk placement/variation work to the GPU | CPU/GPU mismatch, readback stalls and accidental gameplay authority | Stable input keys, no authority drift, conservative CPU fallback and measured benefit |
| GPU Resident Drawer plus GPU occlusion | Cull compatible repeated mesh props behind terrain/structures | Renderer-path and shader compatibility; limited benefit in open views | Forward+/Deferred+ compatibility study and actual hidden-object workloads |
| Simplified terrain mesh proxies | Reduce native visual work at selected distances while retaining native collision | Cracks, holes, silhouettes, shadows and material mismatch | Near/far transition and collision/visual parity comparison |
| Fixed-data geometry clipmap rendering | Bounded reusable terrain geometry and GPU height sampling | Custom renderer complexity, height-texture residency and edge handling | Native improvements insufficient, small patch proof, bounded GPU resources |

The entries are hypotheses and proof gates, not measured speedup claims. They should use the same seed, accepted source terrain and population definitions so each rendering change can be compared independently.

## Deterministic candidates and spaced placement

Use fixed identity cells and global candidate coordinates. Derive candidate positions and priorities from immutable keys; evaluate a neighbor halo large enough for the placement rule, including feature footprints and any deformation influence. A deterministic owner publishes each accepted object exactly once. Preserve canonical candidate IDs before filtering so a rejection does not renumber later resources.

Counter-based RNG design is a useful reference for order-independent variation. [Random123](https://github.com/DEShawResearch/random123) describes deterministic stateless functions of a counter and key; a production implementation must still choose and validate its integer operations, distribution and cross-platform behavior. The reference script below uses SHA-256 for clarity and does not copy or install Random123.

For visually natural spacing, compare stratified candidates, priority-thinned candidates and Poisson-disk-like placement. Bridson's paper provides an efficient minimum-spacing sampler, but running that sampler independently inside each chunk does not by itself establish global border agreement or generation-order independence. Those properties need their own ownership/halo design. [Bridson, Fast Poisson Disk Sampling in Arbitrary Dimensions](https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf).

### Completed CPU reference study

[The standalone study](../../../Tools/Diagnostics/chunk_population_reference_study.py) operates on a synthetic 1,024 × 1,024 metre X/Z domain with sixteen 256 metre identity cells, including negative coordinates. It creates keyed jittered candidates, applies a protected-site exclusion that crosses cell borders, and rejects spacing conflicts using globally comparable neighbor priorities. No terrain samples, Unity objects, native collision, rendering or production saves are involved.

[The retained results](../../Evidence/Performance/chunk-population-reference-2026-10-11.json) contain **974 accepted records from 1,024 candidates**. Row-major, reverse and shuffled identity-cell evaluation produced identical placement/ID digests. Fresh regeneration using 64, 128 and 256 metre work regions also produced the same digest, rather than merely regrouping an existing list. Required spacing was 12 metres; the observed minimum was 12.06 metres overall and 12.28 metres across identity-cell borders. Ownership/ID uniqueness and protected-site exclusion passed. A serialized sparse depletion record survived discarding and regenerating the base presentation records.

All six checks passed. Generation timings are Python reference timings only, not Unity generation budgets. The all-pairs spacing verifier is intentionally small and is not a production spatial-query strategy. The study does not prove terrain grounding, optimal density/maximal Poisson packing, CPU/GPU bit parity, real save migration or Player performance. It demonstrates that the proposed identity/processing separation and one order-independent neighbor rule work in this bounded example.

Reproduce from the checkout root with `.venv/Scripts/python.exe Tools/Diagnostics/chunk_population_reference_study.py --output Logs/chunk-population-reference.json`. The script accepts output only as a JSON file under that checkout's ignored Logs folder. It does not install packages or write game assets.

## GPU procedural clutter

Start with CPU-authoritative generated records and a conventional shared-mesh backend. Compare indirect rendering on the same records before moving placement work to a GPU kernel. `Graphics.RenderMeshIndirect` accepts GPU- or CPU-authored commands and renders repeated mesh instances; it requires compute-capable platforms. Its `worldBounds` is a bound for the draw as a single entity, so whole-world draw bounds do not provide useful per-instance culling automatically. Use spatial batches, or explicit instance-level GPU culling and LOD selection. [Unity indirect rendering API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Graphics.RenderMeshIndirect.html).

For the GPU-placement experiment, restrict the first kernel to cosmetic clutter whose rendering can be reconstructed from the same fixed keys, terrain-derived data and masks. Keep resources, POIs, collision-bearing formations and saved mutations under authoritative world-state/placement rules. Do not make resource identity or availability depend on which instances a GPU happened to draw. GPU feedback should not become a mandatory per-frame synchronous readback; asynchronous readback is available but introduces latency. [Unity AsyncGPUReadback](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rendering.AsyncGPUReadback.html).

Bound buffer capacity, material variety, triangles, shadow work and upload volume. Measure fragment overdraw as well as draw calls: large numbers of tiny or alpha-tested instances can remain GPU-expensive even with efficient submission. Compare view-independent candidate generation with visibility-dependent presentation selection; only the latter may vary with camera or LOD. Cross-platform hash/float behavior and conservative boundary masks require verification before any GPU output becomes authoritative.

## GPU occlusion and procedural prop compatibility

Unity's GPU occlusion uses camera depth and works with GPU Resident Drawer, including its compatibility restrictions. It conservatively uses current/previous-frame depth information. Test it in populated canyons, large formations and POI structures where many props can actually be hidden, alongside an open-terrain control case. It is a prop rendering experiment; it does not remove the measured native TerrainManager workload. [Unity GPU occlusion](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/gpu-culling.html).

The current project uses Forward. Unity 6.4's Resident Drawer requires Forward+ or Deferred+, compatible MeshRenderers/materials and additional constraints. Some project paths use MaterialPropertyBlock. A renderer-path migration therefore needs its own atmosphere, material, depth, shadow and UI proof before integration. Native Terrain components do not satisfy the required MeshRenderer route. [Enabling requirements](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/gpu-resident-drawer.html), [object compatibility](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/make-object-compatible-gpu-rendering.html).

## Terrain render proxies and fixed-data clipmaps

A proxy study derives render-only meshes from accepted native heights, keeps native collision and TerrainData present, and disables redundant native visual work where a verified proxy supplies it. Compare bounded sector meshes with conventional LOD before designing a new terrain renderer. Crack handling, holes, low-angle shadow silhouettes, ground materials and interaction-height agreement matter; a cheaper distant mesh is not an alternative collision authority.

Geometry clipmaps use nested regular grids around the viewer and reusable geometry with GPU height sampling. The foundational GPU Gems technique demonstrates a way to shift terrain visual work toward the GPU, including transitions between resolution levels. Its historical benchmark is not evidence for our Unity project. A fixed-data adaptation would sample the authored terrain while retaining its physical source; it must not synthesize replacement heights. [Asirvatham and Hoppe, GPU-based geometry clipmaps](https://developer.nvidia.com/gpugems/gpugems2/part-i-geometric-complexity/chapter-2-terrain-rendering-using-gpu-based-geometry).

This is the most expensive renderer experiment here. It needs an explicit height/normal/holes representation, texture/array/device-limit plan, multi-camera and shadow handling, and smooth near/far transitions. Derived GPU working caches may be bounded, but authored terrain and collision remain resident. A clipmap does not remove the native physical data's memory floor. Begin with one small patch; proceed only if simpler native rendering control and grouping fail their measured goals.

## Population lifecycle and persistence proof

Treat these as separate states: procedural baseline described by keys/rules; cached generated records; resident visual buffers; actor-relevant interaction/collision proxies; and sparse saved mutations. Releasing a cache or visual batch must not reset depletion, regrow destroyed formations, duplicate placed objects, change POIs, or lose harvester progress. A live logical chunk need not mean that every cosmetic element has a GameObject. Terrain and all native TerrainColliders remain loaded even when population presentations change residency.

Gameplay relevance includes Booter, BigARM and other authoritative actors independently of camera coverage. BigARM keeps his real world position and physically traverses; culling his visuals cannot disable the simulation/collision he needs or become a recall/teleport shortcut. Collision-bearing population must be prepared before relevant actors reach it; route and waypoint queries need logical obstacle information beyond the current visual set. Handle large POIs/formations as one owned feature with footprints that constrain all affected neighboring cells.

Require deterministic output under cell order and worker completion order, stable IDs under LOD/backend/work-size changes, exclusion and border parity, negative-coordinate/corner ownership, and depletion/placement restore after presentation release/recreation. Placement-affecting recipe or terrain-revision changes require an explicit migration/retention policy. The generated-world reference's old save/generator services are not the implementation route for this fixed-terrain population system.

## Next bounded research sequence

1. Integrate the identity and chunk contracts into a local design slice, and correct the native terrain render/collision-index seam before creating a dense population.
2. Use one constrained native sector to compare processing subcells, conventional LOD/shared meshes, active terrain bounds and burst-free activation. Ground gameplay-bearing placements on native collision.
3. Compare indirect rendering on the same accepted placements, then cosmetic GPU placement and optional GPU occlusion with fixed instance/memory budgets.
4. Compare native terrain grouping and simple render proxies; retain clipmaps as a conditional deeper study.
5. Measure one bounded Development Player at a time after resource budgets are established. Keep seed, camera path, resolution, population and rendering conditions controlled; measure startup, boundary handoff, save spikes and steady CPU/GPU/memory separately. Hands-on gameplay acceptance remains author-owned.

This research ran only the synthetic CPU study and offline compilation. Indirect rendering, argument-buffer layout, readback, BatchRendererGroup and LODGroup signatures compiled against the pinned installed assemblies. No graphics kernel, renderer migration, Unity session, whole-world generation or gameplay smoke test was run. Device compatibility and visual/performance benefits are still experiment results to obtain, not established facts.
