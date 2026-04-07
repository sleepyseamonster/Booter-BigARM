# World Generation Research Summary

This document summarizes external research gathered after the initial exploratory world-generation discussion.

It is a reference document, not a source of truth.

Use it to understand what official Unity docs support, what other developers commonly do, and what comparable projects suggest. If this summary conflicts with later project decisions, update or replace it.

Related docs:

- Canonical world setting: [WORLD_BASIS.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_BASIS.md)
- Canonical world systems baseline: [WORLD_SYSTEMS_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_SYSTEMS_STANDARD.md)
- Exploratory design notes: [WORLD_GEN_REFERENCE_NOTES.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_GEN_REFERENCE_NOTES.md)
- Unity-facing exploratory guide: [UNITY_TILEMAP_PROCGEN_REFERENCE.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/UNITY_TILEMAP_PROCGEN_REFERENCE.md)

## Status

- Status: external research summary
- Confidence: moderate
- Intended use: inform planning and architecture choices
- Not for: direct canon or implementation lock-in

## Research Scope

This summary focused on:

- official Unity docs relevant to top-down 2D tilemap worlds
- common practices from Unity developers and procedural generation guides
- lessons from comparable games and talks about sparse procedural worlds and handcrafted anchors

## What The Earlier Discussion Got Right

Several recurring ideas from the earlier conversation hold up well under research.

- Tilemap is the right Unity foundation for a top-down pixel-art procedural ground layer.
- Rule Tile is useful, but mostly as a visual adjacency tool after terrain meaning has been decided.
- Multiple noise fields are common and often better than a single noise field.
- Chunk generation and deterministic reconstruction are standard concerns for large worlds.
- Hybrid procedural terrain plus handcrafted landmarks is common practice.
- Sparse worlds work better when emptiness is intentional and reinforced with landmarks and route logic.

## What Official Unity Docs Support

Unity does not publish one complete recipe for infinite procedural tile worlds. The relevant pieces are spread across several systems.

### Tilemap Foundation

- `Grid` is the coordinate basis for cell layout and conversions.
- `Tilemap` stores tiles and connects to renderers and colliders.
- `Tile Palette` is the practical authoring workflow for layered tilemaps.

Official sources:

- https://docs.unity3d.com/6000.0/Documentation/Manual/tilemaps/grid-reference.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/tilemaps/work-with-tilemaps/tilemap-reference.html
- https://docs.unity3d.com/Manual/tilemaps/tile-palettes/tile-palette-editor-reference.html

### Rule Tiles And Extras

- The 2D Tilemap Extras package is the official Unity-supported home for Rule Tile and related brushes.
- Rule Tile is appropriate for autotiling and neighbor-aware visuals.
- It should not be confused with the higher-level logic that decides which terrain family belongs in a cell.

Official sources:

- https://docs.unity3d.com/cn/current/Manual/com.unity.2d.tilemap.extras.html
- https://docs.unity3d.com/kr/Packages/com.unity.2d.tilemap.extras%406.0/manual/RuleTile.html

### Batch Placement And Rendering

- `Tilemap.SetTilesBlock` is the official bulk-placement API and is explicitly more efficient than repeated `SetTile` calls.
- Tilemap Renderer `Chunk` mode is a renderer optimization, not the same thing as runtime chunk streaming.
- In URP 2D, renderer mode and sorting behavior can affect how tilemaps interleave with other sprites.

Official sources:

- https://docs.unity3d.com/cn/2019.4/ScriptReference/Tilemaps.Tilemap.SetTilesBlock.html
- https://docs.unity3d.com/2023.2/Documentation/Manual/class-TilemapRenderer.html
- https://docs.unity3d.com/kr/Packages/com.unity.render-pipelines.universal%4015.0/manual/2D/tilemap-renderer-2d-renderer.html

### Collision

- Tilemap Collider 2D supports incremental or full rebuild behavior depending on change volume.
- Composite Collider 2D is the standard way to merge many tile collider shapes into fewer final shapes.

Official sources:

- https://docs.unity3d.com/6000.0/Documentation/Manual/tilemaps/work-with-tilemaps/tilemap-collider-2d-reference.html
- https://docs.unity3d.com/jp/current/Manual/2d-physics/collider/composite-collider/composite-collider-2d-reference.html

### Data And Presentation

- `ScriptableObject` is the official Unity pattern for shared config data and tuning assets.
- Pixel Perfect Camera remains the standard path for crisp 2D pixel-art presentation.

Official sources:

- https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html
- https://docs.unity3d.com/Manual/urp/2d-pixelperfect-ref.html
- https://docs.unity3d.com/kr/6000.0/Manual/com.unity.2d.pixel-perfect.html

## What Other Developers Commonly Do

Across tutorials, sample projects, and technical writeups, a few patterns repeat often.

### 1. Generate Data First, Render Second

Many implementations separate terrain data generation from tilemap painting.

Why it matters:

- easier chunk streaming
- easier save/load
- easier debugging
- easier replacement of rendering rules later

Example sources:

- https://github.com/ToberoCat/InfiniteWorld
- https://github.com/zerppa/StreamingTilemap

### 2. Use Multiple Signals, Not One Magical Map

Multi-noise setups are common, often with values like height, moisture, and temperature in general-purpose terrain systems.

The transferable lesson is not the exact field names. The lesson is that terrain usually reads better when more than one continuous signal influences it.

Example sources:

- https://gamedevacademy.org/procedural-2d-maps-unity-tutorial/
- https://www.gamedeveloper.com/programming/2d-procedurally-generated-world-building-in-unity

### 3. Keep Rules Data-Driven

Developers often use `ScriptableObject` assets, tile metadata, and rule tables so terrain can be tuned without rewriting core generation logic.

Example sources:

- https://www.gamedeveloper.com/design/2d-procedural-generation-in-unity-with-scriptableobjects
- https://github.com/Seanba/ST2U_TileProperties

### 4. Hybrid Procedural Plus Authored Is Normal

Procedural base terrain plus authored chunks, rooms, or landmarks is a recurring pattern used to avoid sameness.

Example sources:

- https://www.gamedeveloper.com/design/2d-procedural-generation-in-unity-with-scriptableobjects
- https://github.com/UnityTechnologies/2D_IsoTilemaps

### 5. Domain Warping Exists, But Is Usually Secondary

Directional warping and additional shaping layers are useful quality multipliers, but they are usually added after the core terrain logic works.

Example source:

- https://docs.unity.cn/Packages/com.unity.terrain-tools%405.0/manual/noise-editor.html

## What Comparable Projects Suggest

The most useful lessons from comparable projects are higher-level design lessons rather than direct implementation recipes.

### Procedural Worlds Need Anchors

Projects like Caves of Qud and RimWorld show that large procedural spaces feel better when they contain stable reference points, history, landmarks, or named places.

Sources:

- https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf
- https://wiki.cavesofqud.com/wiki/World_generation
- https://ludeon.com/blog/2025/06/odyssey-preview-1-map-features-landmarks-and-biomes/

### Sparse Worlds Need Intentional Emptiness

Comparable games suggest that emptiness works when it is legible and structured, not when it is simply underpopulated.

This usually means:

- long sightline logic
- route pressure
- landmarks
- clear region identity

### Art Direction Should Lead The Generator

Talks and postmortem-style material repeatedly reinforce that procedural systems work best when the art direction and authored constraints shape the algorithm, rather than the algorithm dictating the entire look.

Sources:

- https://gdcvault.com/play/1035493/Evolving-Worlds-from-the-Crumbling
- https://gdcvault.com/play/1022000/Galak-Z-Forever-Building-Space

## Gaps The Research Helped Clarify

The earlier discussion left some useful gaps that research makes clearer.

### Rule Tile Is Not The Core Generator

Rule Tile helps resolve neighboring visuals. It does not replace terrain classification or world logic.

### Render Chunking Is Not World Chunking

Tilemap Renderer chunk mode is about draw efficiency. Runtime chunk streaming is a separate architecture concern.

### The Most Important Split Is Data Versus Presentation

A robust world system should separate:

- generation config
- deterministic terrain data
- tilemap rendering
- mutable runtime deltas

### Sparse Desert Worlds Need Landmarks Early

If the world stays broad and empty, landmarks and authored anchors are not optional polish. They are part of the structure that makes the world readable and memorable.

## Recommended Takeaways For Booter & BigARM

For the current stage of this project, the safest takeaways are:

1. Build the Greater Wasteland first rather than the whole region stack at once.
2. Keep the early terrain model property-driven rather than biome-heavy.
3. Use a few interpretable signals such as exposure, roughness, sediment, and directional streaking.
4. Keep tile-family selection separate from tile variant selection.
5. Treat Rule Tiles as a later visual layer, not the core terrain-decider.
6. Plan for authored landmarks and outposts early so emptiness stays intentional.
7. Keep deterministic chunk reconstruction as a hard constraint from the start.

## Suggested Follow-Up

If this research becomes operationally important, the next repo-facing step should be a tighter implementation blueprint that translates these findings into:

- one first-pass Greater Wasteland terrain model
- one chunk data shape
- one tile-family resolver
- one authored landmark stamping seam
