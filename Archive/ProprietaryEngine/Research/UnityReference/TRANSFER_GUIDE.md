# How to use the recovered research in the native engine

2026-09-10. This guide distinguishes useful methods from historical implementation
and recommendations. The [manifest](./manifest.json) inventories the collection.
No packages were installed, Unity assets moved, or engine behavior changed by collection.

## LOD, baking and streaming

Read [rock production and LOD work](./Documents/ROCK_QUALITY_AND_PRODUCTION_PLAN.md),
[the landscape benchmark](./Documents/TOP_DOWN_3D_LANDSCAPE_BENCHMARK.md),
[performance audit](./Documents/PERFORMANCE_AUDIT_AND_OPTIMIZATION.md) and
[World Creator architecture](./Documents/WORLD_CREATOR_ARCHITECTURE_PLAN.md).

The rock production document preserves the 19-member / three-variant / three-LOD
bake: 57 mesh families and 171 meshes. Its triangle totals describe a whole library,
not one frame. The useful method is to cook a small, reviewed shape library and
reuse it across deterministic placements; unloading a region releases instances,
not the shared source assets. Maintain a consistent size envelope across LODs.

For the native engine, keep generation and authoring resolution separate from
runtime cost. Evaluate a cooked LOD chain with screen-size selection and hysteresis
when close-up shape quality exceeds the present stream budget. Keep the old visible
representation until its replacement is ready. Collision needs its own explicit
quality/lifetime policy, and far proxies must honor removed-object deltas.

Current native terrain already has three render levels, while the latest streamed
rock recipe uses detail 0 and one level. Those facts do not mean the recovered
Unity LOD pipeline is already transferred. See [current result](../../Docs/UNITY_FORMATION_PARITY_RESULT.md).

## “Wandering cubes”: likely Marching Cubes

No research document with the exact term “wandering cubes” was found. The recovered
[rock generator study](./Recovered/ROCK_GENERATORS_AND_MARCHING_CUBES_2026-09-01.md)
explicitly discusses **Marching Cubes**, scalar fields, mesh decimation, and
`mc-mesher`. It also retains the initial cross-engine shortlist before the user
narrowed that historical task to Unity-native tools.

Read this alongside [formation fusion](./Documents/ROCK_FORMATION_MESH_FUSION_PLAN.md),
[formation workbench](./Documents/ROCK_FORMATION_WORKBENCH.md),
[Manifold research/integration](./Documents/ThirdParty/Manifold/README.md) and the
[original grounded-geology study](./History/GROUNDED_GEOLOGY_3cdb655_SUPERSEDED.md).
The latter contains meshing, gradient-normal, contact/sediment and LOD methods that
were lost from the current short supersession notice. Its replacement architecture
was rejected; recovering it does not revive the separate platform/workbench.

Marching Cubes and marching tetrahedra extract surfaces from scalar fields. They do
not supply the rock's shape grammar, distribution, sediment or material meaning.
The native generator already uses marching tetrahedra. Improve its shape inputs
and physical sampling before replacing the extractor. Full-formation fusion is an
optional representation for appropriate outcrops, not a requirement for every pile.
The source audit below explains why.

## Crimson Desert / BlackSpace

Three entries cover the main research:

- [Ground-detail/displacement study](./Recovered/CRIMSON_DESERT_GROUND_DETAIL_2026-08-14.md).
- [Streaming and LOD reporting comparison](./Recovered/CRIMSON_DESERT_STREAMING_2026-08-24.md).
- [Durable gamescom technical notes](./Documents/Research/CRIMSON_DESERT_GAMESCOM_DEV_2026_TECHNICAL_NOTES.md).

Preserve the historical evidence distinction: official statements, third-party
reporting and visual inference are different. The earlier research did not prove
an exact SSDM implementation or a finished Nanite-like micropolygon system.
Recording/transcript availability was assessed at the time, not refreshed here.

Transfer the hybrid approach: material relief for small surface detail, real geometry
for silhouette-important debris, coherent contact shading, scalable representations,
and view/movement-aware streaming. Do not copy reported kilometer bands, sector
sizes, vegetation counts or world area into our engine defaults. POM/height shading
cannot stand in for player collision. The current native ground masks also need
actual formation/contact inputs before higher-end displacement would be useful.

## Other recovered research

The [library index](./README.md) also covers terrain constraints, sparse-world
composition, authored landmarks, material channels, pebble texture prompts,
world identity/persistence, movement/input, companion traversal, inventory,
resource interaction, animation, and historical UI/Unity rendering studies.

Treat the top-down/2D conversion documents and procedural escarpment design as
historical context. We are building regular third person in the open Greater
Wasteland; canyons remain deferred. Gameplay research does not authorize gameplay
expansion during the current rock/ground work.

## Immediate application

Use [the current visual methods audit](./ENVIRONMENT_METHODS_AUDIT.md) to select the
next implementation batch: varied rock silhouettes and shared ground-contact data,
then sand, gravel and clustered debris. This changes the prior assumption that a
formation-wide fused shell should automatically come first.

A small optional LOD comparison can use an error-bounded mesh simplifier once needed.
Meshoptimizer exposes C/C++ simplification and attribute-aware error controls; it
has not been added to this engine. Pin and evaluate a version before adoption.
[Primary project documentation](https://github.com/zeux/meshoptimizer#simplification).
