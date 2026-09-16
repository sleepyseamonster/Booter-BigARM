# Professional procedural terrain architecture research

Researched 2026-09-15. This document supersedes the narrow recommendations in
[TERRAIN_SYSTEM_RESEARCH.md](./TERRAIN_SYSTEM_RESEARCH.md) for future terrain work. The older
document remains the decision record for the already implemented terrain preview.

## Research question

What terrain architecture should this engine build for a regular third-person game whose
landscape is a primary mechanic and visual subject: streamed open wasteland, hills, sand,
earth, shale, rocks and rock formations, with canyons, cliffs, alcoves and overhangs added in
the correct sequence?

The answer must preserve deterministic world identity, stable generated-object identity,
chunk unload/reload, authored constraints and persisted runtime deltas. It must also let the
AI authoring layer inspect and regenerate bounded areas without requiring a large manual
editor.

## Executive decision

Build a **hybrid terrain system**:

1. A tiled, multiresolution **heightfield is the authoritative open-ground surface**. It is
   efficient for queries, collision, fossil drainage, material classification, navigation and distant
   rendering.
2. A separate, sparse **feature-surface layer** represents shapes a heightfield cannot:
   near-vertical canyon walls, overhangs, alcoves, arches, caves and detached formations.
   These are ordinary cooked meshes first. Bounded signed-distance-field (SDF) generation may
   produce those meshes later; the entire world does not become a voxel volume.
3. Heightfield ground and feature surfaces share one engine-facing surface/query contract,
   material semantics, stable identity system, streaming lifecycle and revision fence.
4. Terrain generation is a versioned dependency graph, not one noise function. Macro form,
   paleohydrology, ancient fluvial erosion, dry weathering, sediment, lithology, canyon features, materials, rock placement,
   collision and navigation are explicit stages with recorded inputs and derived outputs.
5. Authoring is non-destructive. Authored splines, masks, volumes, landmarks, routes and
   protected silhouettes constrain regeneration; they do not bake a permanent world into the
   source recipe.

This combines the scalable heightfield/component model used by Unity and Unreal with an
engine-owned feature layer for the formations those systems commonly handle as meshes. It
avoids both weak noise-only terrain and the cost of a full-world voxel engine.

## Lessons from production engines

### Unreal Engine

Unreal Landscape divides terrain into square components used for rendering, visibility and
collision. Component height data lives in textures, shared edge samples are duplicated, and
sections are the unit of LOD. Epic documents the CPU and draw-call tradeoff between many small
components and fewer large components. The transferable rule is to define explicit tile,
render-patch and shared-border contracts rather than treating the landscape as one mesh.

World Partition separates the persistent world into grid cells loaded from streaming sources.
The transferable rule is that spatial storage, runtime residency and source-world identity are
separate concerns. Our existing `RegionStream` already has the beginning of this boundary.

Unreal PCG treats spatial data, attributes and authored constraints as a graph that can serve
small utilities or entire worlds. We should adopt the dataflow principle, but expose it as
versioned C++ stages and AI-callable operations instead of cloning the graph editor.

Landscape materials use weight maps and layered blending. Runtime virtual textures and Nanite
Landscapes solve valuable later-stage rendering problems, but copying them now would introduce
duplicate terrain representations, invalidation cost and substantial GPU complexity before we
have representative terrain. They are comparison points, not initial requirements.

### Unity

Unity `TerrainData` separates height, material alpha maps, holes and detail/tree data while the
runtime terrain is split into patches. Neighbor connections keep adjacent terrain LODs
consistent. Unity also exposes maximum-height-error data per renderable patch and uses a
lower-resolution composite for distant material rendering. The transferable rules are:

- source fields and derived render products need distinct ownership;
- adjacent tiles need an explicit neighbor/border contract;
- LOD selection should be driven by geometric screen error rather than fixed distance alone;
- distant terrain needs a cheap composite representation;
- updates should dirty bounded regions and propagate invalidation to dependent products.

Unity Terrain Tools distinguishes hydraulic, thermal and wind erosion. That is a useful minimum
process taxonomy for this landscape: water incision and sediment transport, slope/talus
relaxation, and aeolian sand movement should not be collapsed into an undifferentiated noise
layer. Unity also notes that erosion detail depends on terrain resolution. Our system must
therefore declare each process's physical sample scale instead of applying the same filter to
every LOD. Hydraulic tools are references for reconstructing the planet's ancient formation
history; they do not imply present-day water.

## World-timescale correction: fossil water, active dry processes

The current world has no natural liquid water, and the user places the end of natural liquid
water at roughly ten thousand years before the game. Terrain generation therefore has two
explicit temporal phases:

1. **Ancient formation phase:** reconstruct plausible drainage graphs, watersheds, channel
   incision, sediment transport and canyon carving. These are paleohydrological construction
   tools used to produce inherited landforms. They are not runtime weather or water simulation.
2. **Dry-age phase:** advance the inherited terrain through roughly ten thousand waterless years
   using wind transport, saltation, dust and sand deposition, thermal/mechanical fracture,
   gravity-driven rockfall, talus relaxation, dry debris movement and exposure/weathering.

The accepted terrain state contains fossil channels, abandoned basins, dissected canyon systems
and reworked sediment. It contains no rainfall, flowing rivers, groundwater cycle, wet terrain
layer, hydraulic runtime process or naturally replenished liquid. A dry channel is classified by
its ancient catchment and current preservation state, not current discharge.

Ten thousand years is brief on geological timescales. The dry-age phase should preserve the
large inherited canyon and watershed geometry while adding restrained infill, softened exposed
edges, rockfall, talus, dust and dune migration. It must not use the dry interval as justification
to completely regenerate the ancient landforms.

The manifest must record the temporal model explicitly, including an ancient-formation recipe,
the dry-transition boundary and a configurable `dry_age_years` value whose initial world-facing
default is approximately 10,000. This number is a creative/world parameter rather than a claim
that the engine can infer geological chronology.

Current dry conditions include windstorms, dust storms and violent electrical storms. The planet
also has a mild low-frequency resonance that can persist for weeks or months. This is not modeled
as a conventional short tectonic earthquake. Terrain fields therefore include susceptibility to
aeolian transport, electrical exposure, fracture fatigue, granular creep, rockfall and talus
release. Long-lived environment state may accumulate stress; accepted permanent changes are
bounded deterministic events and sparse deltas rather than continuous whole-world deformation.
The [planetary environment contract](../Docs/PLANETARY_ENVIRONMENT_CONTRACT.md) owns this seam.

## Current-engine audit

The current system is a sound skeleton, not a production terrain system:

| Existing capability | Keep | Required evolution |
|---|---|---|
| Integer `Region` plus local `WorldPosition` | Yes. It avoids large global floats and already supports stable regional identity. | Add terrain dataset/tile/feature coordinates and revision IDs without changing the public world-position contract. |
| Deterministic `TerrainRecipe` and `GeneratedId` | Yes. | Replace the three-field recipe with a versioned world-terrain manifest that references stage recipes, source hashes and authored constraint layers. |
| `terrainSample()` representation-independent comment | Yes; make it a real interface. | Return surface provenance, material, support, revision and query quality; support feature surfaces as well as heightfields. |
| 256 m regions and bounded asynchronous jobs | Yes as residency/job foundations. | Decouple generation supertiles, render patches, physics tiles and stream regions; add dependency-aware invalidation and adoption budgets. |
| Shared border evaluation | Yes. | Add explicit border ownership, halos and supertile generation for paleodrainage/ancient erosion; point sampling alone cannot keep fossil drainage coherent. |
| Three decimated render LODs with skirts | Preserve as fallback. | Add geometric-error metadata and geomorph/stitch transitions; skirts can hide cracks but do not prevent silhouette popping. |
| Exact triangle collision from render source | Preserve agreement. | Cook heightfield collision for ordinary ground and feature-mesh collision for exceptional surfaces, both tied to the same surface revision. |
| Fixed-distance LOD choice at 96/240 m | No as production policy. | Replace with projected-error selection, hysteresis and bounded upload/adoption. |
| Value-noise height function | Preserve only for compatibility recipes. | Noise becomes one scaffold input. Paleodrainage, inherited erosion, dry weathering, lithology, constraints and feature construction determine the production terrain. |
| Random rock slots | Preserve stable-slot identity concept. | Population must consume geology, slope, curvature, exposure, sediment, fossil drainage and authored formation rules. |
| World-triplanar material and normal maps | Yes as feature-wall baseline. | Add normalized terrain layer weights, height-aware blending, macro variation, distance composites and material diagnostics. |
| Navigation readiness placeholder | Yes. | Build traversability from the authoritative surface revision, then local navigation tiles and a coarse route graph. |

The current 64 cells across 256 m gives 4 m ground samples. That is useful for a streamed
fixture but too coarse for close third-person ground, narrow drainage, sharp ridges and grounded
material transitions. Production resolution must be chosen per level and validated against
camera height and target pixel error; it should not be hard-coded globally.

## Terrain data model

### Authoritative inputs

`WorldTerrainManifest`

- schema and generator versions;
- world seed and deterministic random-stream namespaces;
- world metric/axis convention;
- generation tile, supertile and halo dimensions;
- references to macro-form, ancient-formation, dry-age, lithology, material and population recipes;
- temporal parameters including the liquid-water cutoff and dry-age duration;
- authored constraint layers and their revisions;
- external input hashes, scale, offset and provenance when height/material data is imported.

`TerrainConstraintLayer`

- guide splines: fossil drainage, canyon centerlines, ridges, routes and formation alignments;
- scalar/vector masks: elevation target, hardness, erosion resistance, deposition, material,
  protection and exclusion;
- volumes: flatten, preserve, cut, fill and feature-generation domains;
- stable landmark anchors and navigation corridors;
- blend radius, priority and composition operator for every constraint.

Constraints are sparse, editable and non-destructive. A change invalidates only intersecting
generation products plus required paleodrainage/formation dependencies.

### Canonical generated fields

A generation supertile owns fields at declared sample scales:

- base elevation and final elevation;
- surface gradient, slope and curvature;
- fossil drainage direction, ancient catchment/flow accumulation and watershed ID;
- inherited fluvial-incision potential, fossil channel class and preservation state;
- bedrock elevation, sediment depth and talus/debris depth;
- lithology/stratum ID, hardness, fracture direction and weathering/exposure;
- aeolian transport/exposure, dry rockfall and thermal-fracture potential;
- electrical exposure plus resonance sensitivity and accumulated-stress inputs;
- normalized base-material weights and additive overlay masks;
- traversability inputs: slope, step, support, clearance and surface hazard;
- feature descriptors for canyon walls, cliffs, outcrops and formations.

These fields are authoritative generation results. GPU buffers, meshes, collision shapes,
navigation tiles, distance composites and instance buffers are derived caches with their own
format versions. A derived product is valid only when its manifest, constraint and source-field
revision tuple matches.

### Stable identity and persistence

- Tile identity derives from dataset version and integer tile address.
- Generated feature identity derives from stage namespace, feature cell/graph node and stable
  candidate index, never vector order.
- Regeneration with unchanged authoritative inputs reproduces identity and geometry.
- Persisted runtime deltas target stable IDs and carry the base generator revision they were
  authored against.
- Migration code must explicitly retain, translate or quarantine deltas when a new generator
  version changes identity.

## Generation mathematics

The formulas below are implementation references, not tuned game values.

### 1. Spectral scaffold

A band-limited fractal sum provides broad variation:

```text
fBm(p) = sum(i=0..N-1) amplitude_i * noise(frequency_i * p)
frequency_i = f0 * lacunarity^i
amplitude_i = a0 * gain^i
```

Use independent named seeds per field. Frequencies above the output grid's resolvable band must
be omitted or analytically filtered to avoid aliasing. Ridged transforms such as
`(1 - abs(noise))^2` can suggest divides; domain warping `p' = p + w(p)` breaks obvious axis
alignment. Both are shape priors, not geology. Excess octaves create characteristic “noise
mountains” and should be rejected by spectral and silhouette review.

Use improved gradient noise or another deterministic, well-tested coherent basis. Do not add a
noise library until the stage prototype proves that the current engine-owned implementation is
insufficient. Store noise type and parameters in the recipe so a code change cannot silently
rewrite worlds.

### 2. Authored feature fields

For a guide curve `C`, compute a signed or unsigned distance `d(p,C)` and a smooth influence:

```text
t = clamp(1 - d / radius, 0, 1)
w = t*t*(3 - 2*t)
height = lerp(generatedHeight, targetProfile(p, C), w)
```

Profiles may define route shelves, valley floors, ridge crests or canyon cross-sections. Compose
constraints in stable priority order with explicit `replace`, `min/cut`, `max/fill`, `add` or
`blend` operators. Never rely on authoring insertion order.

### 3. Ancient drainage reconstruction

Noise-derived terrain often contains accidental pits and incoherent fossil channels. Reconstruct
paleohydrology on a supertile with an ancient outlet policy:

1. apply protected outlets and authored paleodrainage constraints;
2. resolve unwanted depressions with a Priority-Flood fill/breach policy;
3. compute continuous flow direction with D-infinity or a validated multiple-flow alternative;
4. accumulate contributing area in topological order;
5. classify ancient channels and watersheds;
6. preserve desired closed basins as explicit authored features rather than accidental pits.

D-infinity uses the steepest slope over triangular facets and divides flow between adjacent
cells. It reduces the rigid eight-direction pattern of D8. The exact policy must be deterministic
for flats and ties.

### 4. Ancient fluvial erosion followed by dry hillslopes

A common geomorphology model is the stream-power law:

```text
E = K * A^m * S^n
```

`E` is ancient incision rate, `K` erodibility, `A` paleodrainage area and `S` local channel
slope. It operates only in the ancient formation phase, with lithology-dependent `K`, bounded
formation age and authored protections. It does not run against the accepted present-day world.
Hillslope diffusion supplies a complementary construction process:

```text
dh/dt = D * laplacian(h) + uplift - incision + deposition
```

Thermal/talus relaxation moves material only when slope exceeds a material angle of repose. It
must conserve tracked mass within the simulation boundary or record boundary flux. Ancient wet
debris flow may contribute to inherited forms; present-day change is restricted to dry
rockfall, granular movement and wind-driven transport.

For the first production implementation, use a deterministic bounded authoring/cook pass rather
than real-time simulation. Prototype graph-based/analytical ancient erosion before committing to
thousands of iterative droplets, then apply a separate dry-age pass. Physically inspired fields,
phase separation and mass accounting matter more than simulating geological time literally.

### 5. Sand and aeolian deposition

Represent mobile sand separately from bedrock/soil. Use wind direction, terrain exposure,
obstacle shadow, transport capacity and angle-of-repose avalanching to derive a sand-depth
field. Sand may alter the visible surface and material without changing the bedrock identity.
Run detailed obstacle deposition only in bounded authored or representative zones until its cost
is measured. Windblown-sand research supports coupling wind transport with saltation and
avalanching rather than painting dunes with unrelated noise.

Storm wind is a time-varying forcing input, while the terrain owns mobile-sediment capacity and
susceptibility. Coarse unloaded-region evolution uses bounded analytic intervals; loaded regions
may resolve local transport. Both paths must produce the same stable event/delta identities for
accepted permanent changes.

### 6. Resonance and electrical forcing

The persistent planetary resonance is represented by low-frequency regional state and terrain
susceptibility, not by translating the terrain mesh or collider. A simple accumulation model is:

```text
stress_next = clamp(stress + exposure * amplitude * duration - relaxation, 0, capacity)
release when stress_next >= threshold(stable feature ID, lithology, support)
```

Release candidates include bounded rockfall, fracture growth, granular creep and talus movement.
The model needs deterministic interval integration so weeks of unloaded time do not require
millions of fixed ticks. Visual vibration, audio and haptics consume the same episode state but
cannot change authoritative stress.

Electrical storms expose a coarse charge/severity field and stable local strike candidates. A
strike may produce a bounded hazard or landscape-change proposal. Exact strike products and
material transformations remain creative decisions; the engine contract requires identity,
bounds, revision and transactional acceptance rather than a predetermined effect.

### 7. Strata and lithology

Evaluate a 3D layer coordinate rather than assigning materials from height alone:

```text
q = dot(worldPosition, layerNormal) + fold(worldPosition.xz) + faultOffset
stratum = lookup(periodicOrAuthoredStack, q)
```

Each stratum defines thickness, hardness, fracture tendencies, color/material family and erosion
response. Bedrock and sediment are separate layers. Exposed strata drive canyon-wall materials,
ledge formation and rock populations. This provides visual causality: shale bands, resistant
caps, talus and detached rocks agree rather than appearing as independent decoration.

### 8. Canyon and cliff construction

Canyons are inherited from a fossil drainage/guide graph and lithology, not a late texture stamp:

1. define or derive a stable centerline graph, longitudinal grade and catchment;
2. construct floor width/depth and asymmetric cross-section fields;
3. carve the heightfield where the surface remains single-valued;
4. extract wall/ledge feature descriptors from strata, curvature and erosion resistance;
5. build explicit wall/overhang meshes in sparse feature cells;
6. place talus, debris and formation candidates from material removed/exposed by the process;
7. cook unified queries, collision, materials, navigation and LOD products.

For bounded 3D features, an SDF `phi(p)` is negative inside and positive outside. Useful
constructive operations are `min(a,b)` for union, `max(a,b)` for intersection and
`max(a,-b)` for subtraction. The surface is `phi=0`, and its normal comes from the normalized
gradient. Marching Cubes is easy but tends to round sharp strata features; Dual Contouring can
preserve sharp features from intersection points and normals. Transvoxel is a future option if
multiresolution volumetric feature cells need crack-free transitions. We should prototype these
only for a representative canyon feature, then select based on topology, cracks, triangle cost,
collision cooking and authoring behavior.

### 9. Rock and formation placement

Generate stable candidates from a deterministic spatial lattice or hierarchical blue-noise
process. Candidate acceptance and type depend on geological fields:

- exposed bedrock/stratum and fracture direction;
- slope, curvature and surface normal;
- fossil-channel, ancient-floodplain and route exclusion;
- sediment/talus depth and depositional direction;
- distance to parent cliff/outcrop and formation grammar;
- authored density, silhouette and navigation constraints.

Formations are structured groups with a stable parent ID and stable child IDs. Scatter is only
one formation type. The existing rock generator supplies shapes; terrain supplies context and
constraints.

### 10. Material weights

Base weights are functions of the generated physical fields, for example:

```text
raw_i = materialRule_i(height, slope, curvature, stratum, sediment, fossilChannel, exposure)
weight_i = max(raw_i, 0) / sum_j(max(raw_j, 0))
```

Keep a small active base-layer set per render tile and store additional overlays such as dust,
sand or ancient mineral staining separately. Use height-aware blending at contacts. Triplanar
projection is appropriate on steep feature walls; gentler ground can use world-aligned or
terrain parameterization with macro variation. Detail normals must fade before aliasing, and a
far composite/basemap replaces full layer evaluation at distance.

Material classification must be inspectable as false-color weights. It must not infer geology
again in the shader from unrelated thresholds.

## Streaming and LOD

Generation, rendering and residency use related but distinct partitions:

- **generation supertile:** paleodrainage/formation domain with halo and deterministic border policy;
- **source tile:** durable field storage and invalidation unit;
- **render patch:** culling and LOD unit;
- **physics tile:** heightfield or feature-mesh collision adoption unit;
- **navigation tile:** local traversal rebuild unit;
- **stream region:** scheduling and residency policy unit.

Do not force every unit to 256 m because the current region is 256 m.

For a patch with object-space approximation error `e`, choose an LOD from projected error:

```text
screenErrorPixels ~= e * projectionScale / max(viewDistance, nearLimit)
```

Use bounding/error pyramids, hysteresis and a per-frame transition budget. Geomorph between
parent and child heights to prevent popping. Stitch compatible edges or retain skirts as a
fallback. Feature meshes use conventional mesh LODs at first. Geometry clipmaps are a later
candidate if measured CPU/draw/upload cost shows that tiled meshes cannot meet the target.

Far terrain uses a low-cost material composite and reduced geometric frequency. Rock and debris
instances use shared assets and instance buffers, with clustered/HLOD representations only when
measurements demand them.

## Collision, navigation and gameplay agreement

The terrain system publishes one `SurfaceRevision` consumed by rendering, physics and
navigation. A stream cell cannot be reported ready for a consumer whose product revision does
not match.

Ground collision should use Jolt heightfields where their measured memory/cook/query behavior
is better than triangle meshes. Feature surfaces use cooked mesh/convex collision as appropriate.
The public query chooses the closest valid surface and returns provenance so character support,
rock seating and AI reasoning agree.

Navigation derives a traversability raster from slope, step, clearance, support, material
hazards and authored corridors. A coarse route graph provides long-range planning; local
Recast/Detour tiles remain the preferred future adapter. Generation must accept required routes
and agent envelopes before terrain is accepted. Navigation cannot repair an impassable canyon
after generation.

## AI authoring operations

The engine layer should eventually expose structured operations:

- `terrain.inspect_manifest`
- `terrain.inspect_tile` and `terrain.sample_fields`
- `terrain.preview_constraint`
- `terrain.apply_constraint`
- `terrain.regenerate_region`
- `terrain.diff_revision`
- `terrain.validate_region`
- `terrain.capture_diagnostics`
- `terrain.publish_revision`

Preview creates a bounded ephemeral revision; apply records a durable transaction; publish
requires completed derived products and validators. Results return stable IDs, affected bounds,
invalidation reasons, stage timings, memory estimates and validation findings. The scene viewer
may expose splines/volumes and transform gizmos, but the engine contract does not depend on UI.

## Diagnostics and quality gates

Every representative terrain corpus must retain:

- deterministic manifest and output hashes;
- maximum height, normal, material-weight and feature-edge seam error;
- fossil-drainage continuity and ancient watershed/outlet checks across tile borders;
- present-state validation that no active water, rainfall, wetness or hydraulic-runtime field is published;
- environment-episode continuity and deterministic stress/change results across unload/reload;
- validation that presentation vibration cannot move authoritative terrain or collision;
- sediment/material mass and normalized-weight checks;
- slope, curvature, catchment and material histograms;
- false-color elevation, slope, curvature, fossil flow, sediment, lithology, weights and LOD views;
- LOD transition pixel-difference/popping measurements;
- render/collision surface distance and normal disagreement;
- terrain/collision/navigation revision agreement;
- stable-ID and persisted-delta behavior after unload/reload and generator migration;
- per-stage CPU time, job wait, adoption time, upload bytes and resident high-water marks;
- a fixed set of human-reviewed ground-level and distant landscape viewpoints.

Visual quality cannot be proven by a unit test. Technical checks reject broken terrain; the user
accepts silhouettes, composition, scale and beauty from retained comparable captures.

## Techniques by adoption status

### Build into the foundation

- versioned terrain manifest and stage graph;
- generation supertiles, halos and deterministic border ownership;
- authored constraints and bounded invalidation;
- multiscale scaffold plus paleohydrology, slope/curvature, sediment/talus, dry-process and lithology fields;
- hybrid heightfield plus feature-surface contract;
- field-driven material weights and rock/formation candidates;
- geometric-error LOD, geomorph/stitch diagnostics and distant composites;
- revision-fenced render/collision/navigation products;
- AI-callable inspect/preview/apply/regenerate/validate operations.
- a persistent-environment input seam and sparse-delta output for storm/resonance terrain forcing.

### Prototype before selection

- analytical stream-power erosion versus graph/iterative erosion;
- D-infinity versus another multiple-flow paleodrainage method;
- marching cubes versus dual contouring for bounded canyon feature cells;
- triangle mesh versus Jolt heightfield collision for normal ground;
- tiled mesh hierarchy versus geometry clipmaps;
- bounded CPU versus GPU generation for expensive field stages;
- aeolian sand simulation resolution and update domain.

### Preserve as later extensions

- runtime virtual-texture-like mesh/terrain blending;
- GPU indirect population/HLOD clustering;
- sparse SDF editing for localized caves and arches;
- learning-based terrain enhancement trained on licensed/owned data;
- full debris-flow and sandstone-structure simulation for authoring cooks.

### Reject for the initial system

- one noise function as the production world model;
- independent per-tile erosion without a halo/global paleodrainage contract;
- active natural-water, rainfall, river, wetness or hydraulic-runtime systems;
- full-world dense voxels or SDFs;
- material selection based only on elevation and slope;
- baking one permanent game landscape before generator constraints stabilize;
- tying source tiles, render patches, physics tiles, navigation tiles and stream regions to one
  fixed dimension;
- copying Nanite, Runtime Virtual Texturing or a visual PCG editor before representative data
  demonstrates the need.

## Source record

[terrain-architecture-sources.json](./terrain-architecture-sources.json) contains the durable
source ledger, retrieval date, finding and adoption decision. The strongest sources are official
Unreal and Unity documentation and primary research from the original authors or institutions.
Web pages and papers can change or disappear; the ledger keeps titles, authors, DOI where
available and the exact conclusion we relied on, avoiding dependence on remembered search
results.
