# Unity Tilemap Procgen Reference

This document is a provisional Unity implementation guide built from exploratory discussion.

It is not a source of truth.

Treat it as a technical reference that can inform later planning and implementation. Canonical constraints still live in [AGENTS.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/AGENTS.md), [AGENT_AND_UNITY_PRACTICES.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/AGENT_AND_UNITY_PRACTICES.md), and [WORLD_SYSTEMS_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_SYSTEMS_STANDARD.md).

## Purpose

- Capture the Unity-facing technical themes from the design discussion.
- Keep early procedural world ideas grounded in actual Tilemap workflows.
- Preserve a practical learning and implementation order for later work.

## Current Scope

For the near-term prototype, this guide assumes:

- top-down 2D pixel art
- mostly flat gameplay terrain
- no immediate cliff system
- no need yet for height-band edge rendering
- the first target region is the Greater Wasteland

That means the first pass should focus on ground tile generation, not on top-down elevation illusions.

## Core Unity Stack

The discussion points toward a simple Tilemap-first architecture.

### Recommended Scene Hierarchy

```text
Grid
  Ground Tilemap
  Detail Tilemap
  Collision Tilemap
  Overlay Tilemap
```

For the first pass, only `Ground Tilemap` is strictly required.

### Why Tilemap First

- It fits chunk-based procedural terrain well.
- It keeps content on a grid.
- It is easy to batch-write.
- It leaves room for later layering without rewriting the whole world representation.

## First-Pass Technical Goal

The first useful problem to solve is:

- generate chunked ground tiles
- from a small number of terrain types
- using noise in a meaningful way

Not:

- cliff rims
- ledge shadows
- height transitions
- fully featured biome simulation

## Procedural Mapping Model

The strongest technical model from the discussion was:

`sample signals -> derive terrain properties -> resolve tile family`

This is better than direct one-threshold tile assignment because it creates a place to encode meaning.

### Example Signal Roles

- Base noise: broad terrain distribution
- Detail noise: rough versus smooth breakup
- Directional streak noise: wind-shaped bands and sediment patterns

### Example Derived Properties

- `isFlat`
- `isRough`
- `isExposed`
- `hasSediment`

### Example Resolution Layer

- `LightSand`
- `RedSandstone`
- `MixedDirtRock`
- `BarrenRock`

The point is not that these exact variables are final. The point is that the generator should decide what the ground conditions are before deciding which tile to place.

## Region Influence

The discussion moved away from heavy biome systems and toward light region bias.

That suggests a practical rule:

- keep the procgen core stable
- bias the signal interpretation per region
- avoid building separate generator architectures for every region too early

Example later use:

- Greater Wasteland: balanced mixed terrain with visible wind streaking
- Desert Sea: more sediment and smoother breakup
- Reef: more exposed rock and more extreme patterning

## Chunk Workflow

The current project already has a runtime world generator seam and chunk language, so later work should continue to respect chunk-based generation.

### First-Pass Chunk Pipeline

1. Determine chunk coordinate.
2. Generate terrain values for each cell in the chunk.
3. Resolve those values into tile families.
4. Write tiles to the Tilemap.
5. Optionally stamp authored content after the base pass.

### Practical Unity Notes

- Prefer batch tile writes over thousands of individual calls when performance starts to matter.
- Keep chunk identity stable and deterministic.
- Separate generation inputs from mutable runtime state.

## Handcrafted Area Integration

The design discussion clearly wants authored spaces mixed into procedural terrain.

### Good First Principle

- Generate the base terrain first.
- Stamp authored areas second.
- Blend or locally override only what the authored area needs.

### Good Candidates For Early Authored Inserts

- An outpost
- A hidden waystop
- A landmark rock cluster

### What To Avoid Early

- Full scene-based world assembly
- Manually authored terrain everywhere
- Deep prefab logic before the base terrain pass is trustworthy

## Tile Variation

One repeated concern was repetition.

Useful later rule:

- a terrain family should usually have multiple visual variants
- variant selection should stay stable per cell or per chunk
- noise or deterministic hashing is a better selector than frame-time randomness

This should be treated as a polish layer on top of meaningful terrain resolution, not as a substitute for it.

## Rule Tiles

Rule Tiles are still relevant later, but they are not the first technical problem to solve if the current prototype is staying flat.

### Likely Later Uses

- terrain-family transitions
- patterned overlay seams
- eventual cliff or canyon edge logic

### Recommended Timing

- Start with direct tile placement and clear terrain-family logic.
- Introduce Rule Tiles after the terrain categories themselves feel right.

## Data Shape Suggestions

Without prescribing final code structure, the discussion points toward a clean split:

- config data in `ScriptableObject` assets
- deterministic generation from seed plus chunk coordinate
- runtime chunk state separate from config assets

This aligns with the current project standards and is worth preserving as a constraint.

## Learning Roadmap

If this topic is revisited later, the learning order should stay narrow.

### Phase 1

- Unity Grid and Tilemap basics
- Painting and reading tile positions
- Writing tiles from code

### Phase 2

- Noise-based terrain selection on one Tilemap
- Stable tile-family selection
- Chunk-based generation

### Phase 3

- Variant selection and texture breakup
- Authored-area stamping
- Region bias

### Phase 4

- Rule Tiles for transitions
- Additional tilemap layers
- Collision and traversal restrictions

## Questions Worth Carrying Forward

- How many terrain families are enough for the first Wasteland prototype?
- Should region identity be per chunk, per cell, or per larger field sampled separately?
- When does batching tile writes become necessary for the current chunk sizes?
- What is the smallest authored landmark system that proves hybrid generation works?
- Which parts of the current `PrototypeWorldGenerator` should remain prototype-only versus becoming future architecture seams?

## Recommended Use

Use this file when:

- planning a first-pass wasteland terrain prototype
- deciding what Unity systems matter now versus later
- reviewing whether a new procgen idea is solving the current problem or adding premature complexity

Do not use this file as a commitment to specific biome rules, tile counts, or final code architecture.
