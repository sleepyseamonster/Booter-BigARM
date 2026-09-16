# T01 terrain contract and representative corpus result

Implemented on macOS on 2026-09-15. This is the production terrain program's first data boundary. It adds no finished landscape and accepts no permanent geography.

## Shared authority

`WorldTerrainManifest` now owns:

- dataset, world seed and generator version;
- generation-tile, supertile and halo dimensions;
- signed tile addresses with floor-stable parent supertiles across the world origin;
- manifest, authored-constraint, source-field and storm-forcing revisions;
- versioned authored-constraint references with explicit composition operators and deterministic priority/ID order;
- ancient-formation and dry-age process versions;
- the liquid-water cutoff and dry-age duration;
- the explicit rule that current terrain publishes no active natural-water state.

Every derived terrain product receives a revision tuple. Base source/query/render/collision/navigation/material/population/feature products do not consume transient storm state. Only the storm-response product carries the storm-forcing revision until a bounded accepted change becomes a sparse delta.

Stable feature IDs derive from the terrain dataset seed/version, integer region address, feature namespace and member index. Render, physics, navigation and GPU handles remain transient.

## Terrain interaction contract

`TerrainSurfaceSemantics` provides one revision-fenced record for terrain consumers:

- surface class: bedrock, shale, sediment, talus, gravel or sand;
- lithology and stratum IDs;
- sediment and talus depth;
- slope, curvature, support and friction;
- stable support, walk, vault-edge, climb, slide, loose-footing and blocked flags.

These are capabilities and physical facts, not final movement thresholds. The character motor, animation selection, detailed navigation, rock seating and AI queries must consume this record rather than independently interpreting render triangles.

## Representative corpus

The durable inputs under [Assets/Terrain/Corpus](../Assets/Terrain/Corpus/README.md) define six technical domains:

1. Open sediment, gravel and sand plain.
2. Broad asymmetric rolling ridges.
3. Fossil drainage with banks and depositional floor.
4. Layered mesa, cliff steps and talus apron.
5. Boulder and iron-rich formation ridge with navigable gaps.
6. Bounded wall/overhang domain for non-heightfield queries.

The cases exist to drive algorithms, diagnostics, collision, navigation and third-person silhouette review. They are not game-map locations or final recipes.

## Sparse changes

`TerrainChangeDelta` establishes stable event and target IDs, base generator revision, affected region, bounded change kind and explicit acceptance. Initial kinds cover rockfall, sediment movement, electrical strike scar and authored collapse. Planetary resonance is absent from the runtime contract.

## Verification and limits

The retained [T01 evidence](../Evidence/T01-contract/result.json) passes strict manifest/corpus round trips, future-version and malformed-type rejection, insertion-independent constraint ordering, negative tile-to-supertile mapping, dependency invalidation, large positive/negative addresses, surface-semantic validation, sparse-delta validation and legacy terrain v1–v3 round trips. Existing terrain jobs, region streaming and the P22 traversal corpus also pass against the new module.

This proves contracts and technical inputs. T02 must implement deterministic multiscale fields, derivatives, halos and diagnostics. Terrain generation quality, final traversal values, character animation behavior, visual acceptance, native Windows behavior and production performance remain open.
