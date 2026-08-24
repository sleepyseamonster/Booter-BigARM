# World Creator Charter

Status: revised proposal awaiting user approval

Authority: the user remains creative and product authority

Change boundary: this charter authorizes planning only; it does not authorize runtime-code, scene, asset, package, or project-setting changes

On approval, this charter becomes the durable product and quality authority for the World Creator. [WORLD_CREATOR_ARCHITECTURE_PLAN.md](./WORLD_CREATOR_ARCHITECTURE_PLAN.md) remains subordinate implementation guidance and may evolve without weakening the charter. Canonical routing through `DOCS_INDEX.md` and the decision log occurs only after approval in a clean, task-owned documentation lane.

## Purpose

The World Creator is not a terrain randomizer. It is the production system that makes the Broken World feel effectively infinite, geographically coherent, visually singular, mechanically legible, and worth exploring before later gameplay systems add activity.

The landscape is a title-defining feature. A successful result should make a player wonder how such a large world can remain this composed, dense, and specific without revealing a small library of repeated stamps.

This charter governs the landscape foundation and the future generator family that will generate and integrate canyon systems, rock formations, ruins, dig sites, landmarks, towns, cities, and other world structures. The implementation-ready design is in [WORLD_CREATOR_ARCHITECTURE_PLAN.md](./WORLD_CREATOR_ARCHITECTURE_PLAN.md).

## Creative promise

The World Creator shall produce a continuous world in which:

- a seed and precision-safe absolute coordinate identify a reproducible place;
- one coordinate authority supports both believable player-facing globe navigation and the actual regional input to procedural generation;
- travel through coordinate space changes regional character without exposing implementation grids, while intentional faults, chasms, contacts, and authored boundaries may remain abrupt;
- important navigable views, landmark approaches, reveals, and route sequences have geological cause, compositional intent, and navigational meaning;
- significant distant landforms and landmarks that act as navigational promises are genuinely reachable through the intended route graph, or their obstruction and inaccessibility are legible, intentional parts of the world;
- macro landforms, local formations, surface breakup, atmosphere, and later sites agree about the same place;
- explored places can be named or marked and reconstructed without saving their entire generated geometry;
- the player can simply walk, look, orient, and discover in a landscape that feels deliberately made;
- later emergent systems receive a rich stage of routes, shelters, chokepoints, overlooks, extraction spaces, concealment, and memorable reference points.

The visual aspiration is the apparent density, regional identity, silhouette variety, and walk-through credibility that the user admires in Crimson Desert, translated into the dead, canyon-dominated Broken World and into a budget appropriate for a mid-range computer. This is a quality bar, not a claim that this project will copy another game's technology or content.

## The Broken World constraints

The canonical setting remains defined by [WORLD_BASIS.md](./WORLD_BASIS.md). For World Creator work, the decisive constraints are:

- a dead planet-wide canyon system;
- no oceans, rivers, rain, or plant-life ecology;
- permanent orange sky and perpetual-twilight light;
- reds, rusts, iron hues, pale deposits, exposed strata, dust, and rock;
- effectively infinite continuous travel;
- ancient ruins, broken war machines, buried megastructures, settlements, and encounter spaces embedded in terrain;
- weighty, deliberate traversal in a hostile world where distance and preparation matter.

The World Creator serves the current elevated top-down, fully 3D production direction. Separate documentation maintenance must keep the canonical world basis aligned with that accepted representation.

## Non-negotiable design principles

### 1. World truth precedes chunks

A chunk is a streaming, realization, cache, and ownership unit. It is never the creative author of geology. Canyon networks, regional forms, routes, landmarks, and formation systems must cross chunk boundaries because their plans exist at larger scales.

### 2. Causal history precedes realization

The world shall be generated through causal history: coordinate context, geologic province, structural history, canyon and ridge systems, erosion and deposition, inhabitation or construction, destruction, burial and weathering, and the present landscape. The exact phases vary by place, but each visible result must be traceable to coherent causes.

Sites are not decoration. Landmark, ruin, dig-site, outpost, town, and city intent may reserve space and constrain routes, landforms, deposits, sightlines, and approaches before final terrain compilation. Later site realization must adapt to that shared history and may request bounded declared terrain operations. It must not paste a finished layout onto unrelated ground or establish a second terrain authority.

Noise is allowed as variation, distortion, material breakup, and stochastic choice. Noise shall not be the sole author of macro geography.

The present Broken World remains dry. Canyon, channel, drainage, and deposition logic may model ancient, fossil, tectonic, wind-driven, or abstract erosional causes, but it must not introduce present-day rivers, rain, oceans, or exposed water.

### 3. Infinite does not mean globally simulated

The system shall create local deterministic plans on demand from bounded cells, halos, canonical ownership rules, and stable seed namespaces. It shall never require the entire world to exist in memory or a planet-wide simulation to finish before play.

### 4. Coordinates are world DNA

The final coordinate system is a future user-authored design. It is intended to feel like a believable globe-navigation system analogous to latitude and longitude while also being the actual authority through which the user's regional map controls landscape generation.

This charter deliberately does not define its bounds, origin, axes, units, notation, projection, wrapping, named regions, or regional map. Those decisions remain open until the user specifies them. The architecture shall expose one replaceable coordinate contract so the future system can be added without rewriting world identity, generation, streaming, saving, or marked-place logic.

The coordinate contract shall distinguish precision-safe absolute world identity from Unity's temporary local scene coordinates. Origin rebasing, streaming, and representation changes may move local objects, but they must never change a place's thematic coordinate, generated geography, stable identity, or saved-location address. The eventual coordinate representation may use integer, fixed-point, double-precision, projected, spherical, wrapped, or another suitable model chosen during its dedicated design.

The user's future regional map shall define which landscape influences apply at a coordinate and how they transition. Implementation grids may index or cache results but must never become visible regional borders. The system shall support both gradual transitions and intentional authored discontinuities such as faults, canyon walls, geological contacts, or territorial works.

Required coordinate outputs are landscape context. Danger, occupation, narrative history, and other gameplay meanings remain optional future overlays unless the user explicitly makes them part of the coordinate design.

### 5. Handmade quality comes from authored grammars

Artists and designers shall author samples, shape families, rules, constraints, exclusions, relationships, composition goals, and playable beats. The generator shall adapt those ingredients to the local world plan. Completed towns, ruins, or landmarks must not simply be dropped onto unrelated terrain as isolated stamps.

Sample-driven authoring is an iterative quality loop: build a representative place, identify the causal and compositional rules that make it successful, apply those rules through bounded deterministic generation, review the generated results in play, and improve the sample or rules. A sample is evidence for a grammar, not a finished patch of terrain to repeat.

### 6. Variation must be hierarchical

Uniqueness must exist at every visible scale:

- continental/provincial: broad elevation, canyon density, structural direction, history;
- regional/system: canyon networks, basins, ridges, scarps, routes, landmark constellations;
- landform: individual shelves, walls, spires, outcrops, deposits, and negative spaces;
- local: fractured silhouettes, talus, dust, material response, erosion marks, and debris;
- view: foreground framing, middle-ground structure, horizon anchor, navigable opening, and deliberate rest.

Random rotation and scale do not count as sufficient variation.

Regional coherence requires controlled recurrence. Strata language, rock families, erosion motifs, construction vocabulary, and other characteristic forms may repeat as recognizable regional grammar. What must not repeat recognizably is the same complete arrangement, silhouette hierarchy, spacing rhythm, approach sequence, and contextual relationship.

### 7. Negative space is generated content

Open ground, long sightlines, quiet basins, narrow reveals, and sparse intervals must be deliberately composed. Density means meaningful spatial information, not uniform object count.

Density shall be conditioned by spatial purpose. Travel corridors, discovery spaces, combat grounds, landmark approaches, settlements, exposed wilderness, and deliberate rest areas require different rhythms of rock, dust, debris, ruins, formations, concealment, and openness. The absence of water and plant life does not justify empty undifferentiated ground, and visual richness does not justify filling every surface.

### 8. Playable geology and traversal are first-class outputs

The World Creator shall design playable geology rather than attempt a literal scientific reconstruction. Geological and historical cause establish credibility, but the resolved landscape must guide attention, reveal destinations, support deliberate movement and combat, and remain readable from the production camera. When scenic complexity and playability conflict, the system shall preserve the world fiction while giving playability priority.

The landscape generator shall emit affordance data rather than force every later system to infer gameplay from render meshes. Required concepts include walkability, slope class, ledge and wall boundaries, corridor width, cover, shelter, overlook, choke, arena potential, route cost, and site-support capacity.

Affordances and routes shall be evaluated against declared agent profiles. Booter-only paths, BigARM-compatible routes, separations, detours, and inaccessible spaces may all create gameplay, but they must be intentional. The macro world must retain enough stable route truth for BigARM to remain at a real world position, traverse while detailed terrain is unloaded, and physically regroup without teleporting.

Player-perspective traversal and approach review outrank attractive overhead views or isolated screenshots. A landscape that works only from a development camera, one approach, or one distance has not passed composition review.

### 9. One place, multiple representations

Near terrain, collision, mid-distance forms, far horizons, map-like coordinate records, save identity, and later site generators shall derive from the same stable plans. Representation may change with distance; world truth may not.

### 10. Stable identity is part of geography

Every persistent generated feature must have an identity derived from world seed, world-plan version, generator namespace/version, canonical owner cell, feature type, and deterministic ordinal or key. Saved locations and mutable deltas refer to these identities, not transient GameObjects or runtime build order.

### 11. Version changes are explicit

Topology, physical affordances, cosmetic distribution, resources, and site content shall have separate version domains. Changing surface clutter must not reshuffle canyon systems. A saved location shall record enough world identity to be reconstructed, rejected clearly, or migrated deliberately.

### 12. Performance and scalability are architectural features

The target is a visually rich world that runs well on an agreed mid-range computer. The system shall:

- plan and materialize only bounded working sets;
- reuse data across render, collision, placement, and queries;
- prevent distant or unloaded detail from consuming near-field simulation cost;
- support caching, asynchronous work, pooling, simplified representations, or other measured techniques where appropriate;
- ensure quality settings change representation cost without changing canonical geography, traversal topology, or saved-place identity;
- prevent expensive authoring, erosion, boolean, or optimization processes from becoming unbounded per-frame requirements;
- define and enforce generation, integration, memory, rendering, and physics budgets from controlled profiling.

No final performance claim exists until a Development Player is profiled on an agreed target machine.

## Perceptual uniqueness standard

“No cookie cutter” is not an absolute promise that mathematical similarity will never occur in an infinite world. It is an enforceable perceptual standard:

- no complete macro landform arrangement repeats recognizably within the configured landscape-memory radius;
- nearby formations do not reuse the same dominant silhouette, member topology, orientation, scale rhythm, and material treatment together;
- regional plans preserve characteristic motifs and material language without repeating a fixed complete layout;
- composition evaluation detects repeated horizon profiles, overly even spacing, noisy uniform density, blocked routes, weak focal hierarchy, and implausible intersections;
- automated novelty checks may gate candidates, but they must preserve geological and cultural coherence; production-camera panels across multiple approaches and distances, followed by human walk-through review, remain the authority for visual success.

The generator may reuse a finite asset vocabulary. It must combine and transform that vocabulary through local causal plans deeply enough that the player perceives places, not kit pieces.

## The generator family

The World Creator is a platform for cooperating generators, not one monolithic function. Expected members include:

- coordinate-context and geologic-province generators;
- tectonic/fault, canyon-network, canyon-form, basin, ridge, mesa, shelf, and dry paleo-drainage/deposition generators;
- rock-formation, cliff, talus, dust, fracture, and surface-material generators;
- traversal-route and affordance generators;
- landmark, ruin, buried-megastructure, war-machine-wreck, dig-site, encounter-space, outpost, town, and city generators;
- atmosphere, vista, audio-zone, and environmental-story generators.

These are not independent decorators. Each consumes shared world context, history, reservations, constraints, and outputs from other relevant scales. Site intent may participate before final landscape compilation; site realization may later request terrain adaptation through declared operations. No generator may secretly invent a second terrain authority.

## Authored anchors and Lorekeeper boundary

Authored narrative anchors are welcome and necessary. They may reserve coordinates, impose approach routes, require silhouettes, or constrain local generation while still being adapted to their site.

The game repository's canonical documents control game truth. The broader Arc & Dust source repository remains Lorekeeper reference-only under [Lorekeeper's authority boundary](./Agents/Lorekeeper/README.md). Ideas retrieved from it are proposals until the user accepts them into game canon; the World Creator must not silently encode reference-lore assumptions.

## Definition of a successful landscape foundation

The foundation is successful when it can demonstrate, through the canonical production path:

- deterministic reconstruction from seed, version, and coordinates;
- stable absolute place identity across local-origin rebasing and representation changes;
- coherent cross-chunk macro systems rather than chunk-local noise;
- a branching canyon system, elevation layers, ridges, basins, shelves, and readable routes;
- multiple simultaneous visual scales in the fixed production camera;
- seamless near, middle, and far representations of the same place;
- stable feature identity across unload and reload;
- explicit traversal and site-support affordances;
- deliberate Booter and BigARM route compatibility, including unloaded-world route truth;
- historical integration seams through which later sites can shape and inherit their surrounding terrain;
- navigationally significant visible destinations that are reachable, or whose obstruction is intentionally legible;
- measurable novelty and composition checks;
- bounded memory, generation work, and rendering cost;
- compelling fixed-camera evidence across several seeds and coordinate transects;
- user acceptance that walking and looking at the landscape is itself engaging.

## Scope exclusions for the first foundation slice

The first production slice does not include finished ruins, dig sites, towns, cities, encounters, quests, a world-map UI, the final thematic coordinate system, its bounds or projection, the user's regional map, a full coordinate-region catalog, destructible terrain, planet-wide offline simulation, or every future biome/geology family.

It must, however, expose the contracts those systems will need: a replaceable precision-safe coordinate interface, stable plans and identities, history and reservations, agent-profiled semantic queries and traversal affordances, terrain adaptation requests, streaming lifecycle, saved coordinate records, and representation tiers. Any temporary proof coordinates or region influences must be explicitly non-canon and disposable.

## Decision authority and change control

The user approves:

- this charter and its quality bar;
- the first geology family used for production proof;
- the final thematic coordinate system, its lore, globe model, bounds, notation, projection, regional map, and landscape meanings when that dedicated design begins;
- the target mid-range hardware class and performance threshold;
- any product decision that changes world topology, save compatibility, creative canon, or visual acceptance.

Implementation may begin only after the user approves this charter and the architecture plan. Approval of planning does not authorize deletion of the current generator; replacement shall occur through verified migration batches with an explicit rollback boundary.
