# World Creator Charter

Status: proposed for creative and technical review

Authority: the user remains creative and product authority

Change boundary: this charter authorizes planning only; it does not authorize runtime-code, scene, asset, package, or project-setting changes

## Purpose

The World Creator is not a terrain randomizer. It is the production system that makes the Broken World feel effectively infinite, geographically coherent, visually singular, mechanically legible, and worth exploring before later gameplay systems add activity.

The landscape is a title-defining feature. A successful result should make a player wonder how such a large world can remain this composed, dense, and specific without revealing a small library of repeated stamps.

This charter governs the landscape foundation and the future generator family that will place canyon systems, rock formations, ruins, dig sites, landmarks, towns, cities, and other world structures. The implementation-ready design is in [WORLD_CREATOR_ARCHITECTURE_PLAN.md](./WORLD_CREATOR_ARCHITECTURE_PLAN.md).

## Creative promise

The World Creator shall produce a continuous world in which:

- a seed and coordinate identify a reproducible world;
- coordinates are both real navigation and a thematic game system;
- travel through coordinate space changes regional character continuously rather than crossing obvious square biome borders;
- every view has geological cause, compositional intent, and navigational meaning;
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

The opening sentence of `WORLD_BASIS.md` still describes the former 2D representation. The live project authority and this charter use the current elevated top-down, fully 3D production direction. That documentation drift must be resolved separately; this planning task does not overwrite the user's dirty canonical file.

## Non-negotiable design principles

### 1. World truth precedes chunks

A chunk is a streaming, realization, cache, and ownership unit. It is never the creative author of geology. Canyon networks, regional forms, routes, landmarks, and formation systems must cross chunk boundaries because their plans exist at larger scales.

### 2. Cause precedes decoration

The world shall be generated as a causal stack: coordinate context, geologic province, structural history, canyon and ridge systems, erosion and deposition, landform features, surface materials, traversal affordances, and only then decoration and sites.

Noise is allowed as variation, distortion, material breakup, and stochastic choice. Noise shall not be the sole author of macro geography.

### 3. Infinite does not mean globally simulated

The system shall create local deterministic plans on demand from bounded cells, halos, canonical ownership rules, and stable seed namespaces. It shall never require the entire world to exist in memory or a planet-wide simulation to finish before play.

### 4. Coordinates are world DNA

The coordinate system shall feed continuous, inspectable regional fields such as structural orientation, elevation regime, canyon intensity, strata family, weathering, sediment, age, danger potential, and human-history influence. Later coordinate regions may constrain or override these fields through authored data without introducing visible grid seams.

### 5. Handmade quality comes from authored grammars

Artists and designers shall author samples, shape families, rules, constraints, exclusions, relationships, composition goals, and playable beats. The generator shall adapt those ingredients to the local world plan. Completed towns, ruins, or landmarks must not simply be dropped onto unrelated terrain as isolated stamps.

### 6. Variation must be hierarchical

Uniqueness must exist at every visible scale:

- continental/provincial: broad elevation, canyon density, structural direction, history;
- regional/system: canyon networks, basins, ridges, scarps, routes, landmark constellations;
- landform: individual shelves, walls, spires, outcrops, deposits, and negative spaces;
- local: fractured silhouettes, talus, dust, material response, erosion marks, and debris;
- view: foreground framing, middle-ground structure, horizon anchor, navigable opening, and deliberate rest.

Random rotation and scale do not count as sufficient variation.

### 7. Negative space is generated content

Open ground, long sightlines, quiet basins, narrow reveals, and sparse intervals must be deliberately composed. Density means meaningful spatial information, not uniform object count.

### 8. Traversal is a first-class output

The landscape generator shall emit affordance data rather than force every later system to infer gameplay from render meshes. Required concepts include walkability, slope class, ledge and wall boundaries, corridor width, cover, shelter, overlook, choke, arena potential, route cost, and site-support capacity.

### 9. One place, multiple representations

Near terrain, collision, mid-distance forms, far horizons, map-like coordinate records, save identity, and later site generators shall derive from the same stable plans. Representation may change with distance; world truth may not.

### 10. Stable identity is part of geography

Every persistent generated feature must have an identity derived from world seed, world-plan version, generator namespace/version, canonical owner cell, feature type, and deterministic ordinal or key. Saved locations and mutable deltas refer to these identities, not transient GameObjects or runtime build order.

### 11. Version changes are explicit

Topology, physical affordances, cosmetic distribution, resources, and site content shall have separate version domains. Changing surface clutter must not reshuffle canyon systems. A saved location shall record enough world identity to be reconstructed, rejected clearly, or migrated deliberately.

### 12. Performance is an architectural feature

The target is a visually rich world that runs well on an agreed mid-range computer. The system shall:

- plan and materialize only bounded working sets;
- reuse data across render, collision, placement, and queries;
- generate asynchronously or through jobs where practical;
- pool buffers and representations;
- keep collision near the player;
- use HLOD, impostors, simplified landform meshes, and material detail at distance;
- cache deterministic plans and expensive compiled outputs;
- never solve expensive CSG, erosion, or global optimization every frame;
- retain a measured per-frame integration budget, provisionally the current 2 ms guardrail, until controlled profiling justifies a change.

No final performance claim exists until a Development Player is profiled on an agreed target machine.

## Perceptual uniqueness standard

“No cookie cutter” is not an absolute promise that mathematical similarity will never occur in an infinite world. It is an enforceable perceptual standard:

- no recognizable macro landform stamp repeats within the configured landscape-memory radius;
- nearby formations do not reuse the same dominant silhouette, member topology, orientation, scale rhythm, and material treatment together;
- regional plans maintain characteristic identity without repeating a fixed layout;
- a composition scorer detects repeated horizon profiles, overly even spacing, noisy uniform density, blocked routes, weak focal hierarchy, and implausible intersections;
- automated novelty checks gate candidates, while fixed-camera panels and human walk-through review remain the authority for visual success.

The generator may reuse a finite asset vocabulary. It must combine and transform that vocabulary through local causal plans deeply enough that the player perceives places, not kit pieces.

## The generator family

The World Creator is a platform for cooperating generators, not one monolithic function. Expected members include:

- coordinate-context and geologic-province generators;
- tectonic/fault, canyon-network, canyon-form, basin, ridge, mesa, shelf, and drainage/deposition generators;
- rock-formation, cliff, talus, dust, fracture, and surface-material generators;
- traversal-route and affordance generators;
- landmark, ruin, buried-megastructure, war-machine-wreck, dig-site, encounter-space, outpost, town, and city generators;
- atmosphere, vista, audio-zone, and environmental-story generators.

These are not independent decorators. Each consumes shared world context, reservations, constraints, and outputs from earlier scales. Later site generators may request terrain adaptation through declared operations; they may not secretly invent a second terrain authority.

## Authored anchors and Lorekeeper boundary

Authored narrative anchors are welcome and necessary. They may reserve coordinates, impose approach routes, require silhouettes, or constrain local generation while still being adapted to their site.

The game repository's canonical documents control game truth. `/Users/worldbuilder/Desktop/D&D Arc & Dust` remains Lorekeeper reference-only. Ideas retrieved from it are proposals until the user accepts them into game canon; the World Creator must not silently encode reference-lore assumptions.

## Definition of a successful landscape foundation

The foundation is successful when it can demonstrate, through the canonical production path:

- deterministic reconstruction from seed, version, and coordinates;
- coherent cross-chunk macro systems rather than chunk-local noise;
- a branching canyon system, elevation layers, ridges, basins, shelves, and readable routes;
- multiple simultaneous visual scales in the fixed production camera;
- seamless near, middle, and far representations of the same place;
- stable feature identity across unload and reload;
- explicit traversal and site-support affordances;
- measurable novelty and composition checks;
- bounded memory, generation work, and rendering cost;
- compelling fixed-camera evidence across several seeds and coordinate transects;
- user acceptance that walking and looking at the landscape is itself engaging.

## Scope exclusions for the first foundation slice

The first production slice does not include finished ruins, dig sites, towns, cities, encounters, quests, a world-map UI, a full coordinate-region catalog, destructible terrain, planet-wide offline simulation, or every future biome/geology family.

It must, however, expose the contracts those systems will need: stable plans and identities, reservations, semantic queries, traversal affordances, terrain adaptation requests, streaming lifecycle, saved coordinate records, and representation tiers.

## Decision authority and change control

The user approves:

- this charter and its quality bar;
- the first coordinate/geology family used for production proof;
- the target mid-range hardware class and performance threshold;
- any product decision that changes world topology, save compatibility, creative canon, or visual acceptance.

Implementation may begin only after the user approves this charter and the architecture plan. Approval of planning does not authorize deletion of the current generator; replacement shall occur through verified migration batches with an explicit rollback boundary.
