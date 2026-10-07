# World Generation Reference Notes

This document is a provisional reference distilled from exploratory design discussion.

It is not a source of truth.

If this document conflicts with [WORLD_BASIS.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_BASIS.md), [WORLD_SYSTEMS_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_SYSTEMS_STANDARD.md), or later implementation decisions, treat this file as disposable working context.

Related docs:

- Canonical world tone and rules: [WORLD_BASIS.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_BASIS.md)
- Canonical world systems baseline: [WORLD_SYSTEMS_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_SYSTEMS_STANDARD.md)
- Unity-facing reference for this topic: [UNITY_TILEMAP_PROCGEN_REFERENCE.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/UNITY_TILEMAP_PROCGEN_REFERENCE.md)
- External research summary: [WORLD_GEN_RESEARCH_SUMMARY.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_GEN_RESEARCH_SUMMARY.md)

## Status

- Status: provisional reference
- Confidence: mixed exploratory notes, not validated design
- Intended use: brainstorming, later synthesis, implementation planning
- Not for: canon lock-in, exact gameplay promises, or final procedural rules

## Purpose

- Preserve useful world-generation thinking from early discussion.
- Capture plausible region, terrain, and procedural ideas without locking the project into them.
- Provide a later reference when shaping the real world-generation blueprint.

## Current Working Frame

- The game is top-down 2D pixel art.
- The world should read as dry, dead, hostile, and wind-carved.
- There is no liquid and no living ecological biome layer driving terrain.
- Terrain identity comes from geology, erosion, sediment, emptiness, and human or ghost occupation.
- The immediate procedural focus is not a full biome simulator.
- The real technical challenge is mapping noise data into meaningful terrain tiles.

## Macro World Sketch

These names and positions are rough world-shaping ideas, not locked canon.

### East

- Abyssal Canyons
- Deepest and most dangerous terrain.
- Severe erosion, sharp cuts, hard traversal pressure.

### Center

- Central Dominance, also described as the Greater Wasteland.
- Main exploratory play space.
- Vast, sparse, and difficult to read at a glance.
- Encounters should feel rare rather than constant.

### West

- The Reef.
- Wind-twisted rock formations, tall spires, and strange silhouettes.
- No normal human habitation.
- Strong association with ghosts.

### North

- Alabaster Valley, also called the Valley of Bones.
- Pale, quiet, dry, eerie, and visually open.

### South

- Burned Lands / Ash Lands.
- Darker, harsher, scorched, ash-heavy terrain.

### Far South

- Desert Sea.
- Broad, smoother, heavily sedimented terrain.
- Beyond that, an impassable world edge.

## Human Presence And Risk Gradient

The discussion implies a world shaped by safety bands rather than evenly distributed activity.

### Frontier Outposts

- Located near the border between the Abyssal Canyons and the Central Dominance.
- Large and small imperial outposts act as visible hubs of commerce and life.
- These likely serve as orientation anchors and stable player-facing reference points.

### Hidden Interior Tradeposts And Waystops

- Deeper in the Greater Wasteland.
- Hidden, guarded, and intentionally harder to discover.
- Better treated as reward spaces than as routine traffic nodes.

### Forbidden And High-Risk Zones

- The Reef should read as uninhabited by normal people.
- The Reef and the deeper canyons are the real danger concentrations.
- The Central Dominance should feel broad and sparse by comparison.

## Terrain Language

The terrain palette discussed most often was:

- Red sandstone dirt
- Barren rock
- Mixed dirt-rock terrain
- Light sand patches
- Scorched or ash-like variants for southern terrain
- Pale dust or chalk-like variants for northern pale terrain

The desired result is not generic desert randomness. It should feel shaped by:

- Wind
- Exposure
- Sediment deposition
- Geological abrasion
- Emptiness

## Procedural Design Direction

The strongest recurring idea from the discussion was:

`noise -> terrain properties -> tile`

Not:

`noise -> tile`

This suggests a property-driven generator instead of a direct threshold-only tile selector.

### Useful Terrain Properties

Possible derived properties mentioned or implied:

- Exposure
- Roughness
- Sediment
- Flatness
- Wind alignment
- Regional bias

These can later resolve into concrete tile families.

## Greater Wasteland First-Pass Model

The Greater Wasteland is the best first biome to prototype because it is broad, central, and mechanically simpler than canyon or cliff-heavy terrain.

### Intended Feel

- Vast
- Mostly flat in gameplay terms
- Wind-shaped
- Visually varied without becoming noisy
- Sparse and believable rather than heavily authored everywhere

### Candidate Signal Stack

This came up as a useful simplified model:

- Base terrain noise for broad surface identity
- Detail noise for rough versus smooth breakup
- Directional streak noise for wind-shaped bands and sediment flow

### Candidate Derived Logic

- Flat + sediment-rich + wind-favored areas tend toward light sand.
- More exposed and rough areas tend toward barren rock.
- Mid exposure areas can resolve toward red sandstone.
- Transitional areas can resolve toward mixed dirt-rock.

The important takeaway is that terrain should appear for a reason, even if the system remains abstract.

## Wind-Carved World Principle

One of the strongest constraints in the discussion was that the world should feel carved by wind.

That implies:

- Directionality matters.
- Terrain patterns should stretch and flow rather than form circular blobs.
- Sediment should collect in some spaces and strip from others.
- Surface variation should feel aligned, worn, and consistent.

Even if the final generator does not simulate literal erosion, it should preserve the visual logic of erosion.

## Hybrid Procedural And Authored Spaces

The discussion repeatedly returned to a hybrid approach instead of fully procedural emptiness.

### Procedural Base

- Large-scale ground coverage
- Region identity
- Terrain variation
- Discoverable travel space

### Authored Injection

- Outposts
- Tradeposts
- Waystops
- Unique formations
- Landmark spaces

The key idea is that authored spaces should be stamped into a procedural base rather than replacing the whole generator.

## Simplification Decision Worth Preserving

One useful narrowing decision from the discussion:

- Defer cliffs, canyon-edge readability, and top-down height representation.
- First solve flat-world terrain generation meaningfully.

That is a good scoping decision for early implementation because it isolates the actual core problem:

- meaningful terrain selection
- chunk generation
- procedural plus authored integration

## Implementation Implications

If these notes remain useful, they point toward a later production blueprint with:

- deterministic chunk generation
- a region map or region bias system
- multiple noise signals with directional influence
- property-driven tile resolution
- handcrafted area stamping
- a risk gradient tied to geography rather than random spawn density alone

## Open Questions

- Is the world truly infinite in all directions, or should named macro regions be treated as strong directional biases around a starting dominance?
- Should wind direction be globally fixed, region-specific, or only visually implied in the first pass?
- How much of region identity should come from tile palette versus terrain patterning?
- How visible should hidden tradeposts be from a distance?
- How much authored structure should exist before the wasteland loses its scale and emptiness?

## Recommended Use

Use this file when:

- turning discussion into a tighter world-generation blueprint
- deciding what the first wasteland terrain prototype should optimize for
- checking whether a procedural idea supports the discussed feel

Do not use this file as proof that any region, rule, or world term is finalized.
