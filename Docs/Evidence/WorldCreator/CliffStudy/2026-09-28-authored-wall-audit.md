# Authored cliff wall audit

Date: 2026-09-28. Source: the saved `cliff wall` root in `TopDown3DPrototype.unity`, compared with `CliffWallSampleReference.prefab`. The Unity source was read and rebuilt in an isolated project mirror; the live Editor and scene were not modified. [The complete 36-volume inventory](2026-09-28-authored-wall-parts.csv) records every member's transform, source volumes, seeds, and rebuilt bounds. The user's sample and subsequent clarification establish that the **nonuniform member scales, especially vertical scale, are part of the intended shape**.

## What the wall actually is

- The wall is an **assembly of 18 separate rock workbenches**, not one generated cliff mesh. Each member has exactly two active, additive `WeatheredBlock` source volumes. All use `BlockyMonolith`; none has a subtractive fracture volume and each reports zero generated major fractures. The convincing crevices come mainly from overlaps, silhouette gaps, surface texture, and light.
- There are **five underlying source recipes** by seed and rounded source-volume data: A (`801855505`, one member), B (`839315657`, one), C (`1602256044`, eleven), D (`-309069799`, four), and E (`117959393`, one). Repetition is disguised by scale, placement, occlusion, and a few different base shapes. A procedural generator should vary those recipes further rather than repeat the entire 18-rock arrangement.
- Most source rocks are less than 1 m high before their root transforms. The saved vertical scales range **1.57–4.73**, versus horizontal scales **0.71–1.54**. Median vertical-to-horizontal stretch is **2.74**. The rebuilt visible heights are **1.07–3.19 m** with a **2.39 m median**. Rebuilding a base recipe at the final height without baking the user's transform would change its profile.
- The assembled rebuilt bounds are approximately **2.93 m in X, 3.30 m in Y, and 2.87 m in Z**. Seventeen rocks extend below the wall's zero-height plane by about 0.25–0.45 m. One small rock is raised as an upper accent. There are seven tall anchors, six middle-height stones, and five short gap fillers. The highest crests vary along the wall; they do not form a straight cap.
- Rock centers run along a diagonal axis of about **50° in local X/Z**. Their center offsets across the perpendicular axis span about **0.94 m**, and individual pieces have substantial depth. This is a thick, interleaved rock mass, not a single surface sheet. Ninety-two of 153 member pairs have overlapping world-axis bounds; that is an upper-bound indicator of deliberate packing, not proof of triangle intersection.
- The source prefab stores the editable volumes and transforms but **no durable mesh**. Headless preview rebuild produced **61,088 vertices and 122,104 triangles** for all 18 rocks at 0.05 m source voxel size. This is an authoring reference; stamping all 18 raw previews repeatedly would be too expensive without baking and LOD reduction.

### Every visible member

The table is ordered along the wall's approximate diagonal axis. “Built height” is the rebuilt mesh's world bounds after the user's scale; “crest” is the local wall-space top height. Roles are descriptive observations, not serialized labels.

| Member | Recipe | Along wall (m) | Scale X/Y/Z | Built height (m) | Crest Y (m) | Observed role |
|---|---:|---:|---:|---:|---:|---|
| Rock Workbench (7) | C | -1.37 | 0.71/1.57/0.71 | 1.07 | 0.77 | low gap filler |
| Rock Workbench (8) | C | -1.21 | 0.71/1.57/0.71 | 1.07 | 1.91 | raised accent |
| Rock Workbench (4) | C | -1.11 | 1.32/2.90/1.32 | 1.97 | 1.70 | middle |
| Rock Workbench (13) | C | -0.90 | 1.30/4.56/1.30 | 3.10 | 2.85 | tall anchor |
| Rock Workbench (6) | D | -0.68 | 1.21/3.32/1.21 | 3.03 | 2.59 | tall anchor |
| Rock Workbench (9) | C | -0.61 | 0.71/1.57/0.71 | 1.07 | 0.77 | low gap filler |
| Rock Workbench (3) | B | -0.35 | 1.54/3.27/1.15 | 1.89 | 1.47 | middle |
| Rock Workbench (14) | C | -0.16 | 1.30/4.56/1.30 | 3.10 | 2.85 | tall anchor |
| Rock Workbench (12) | C | -0.04 | 0.71/1.57/0.71 | 1.07 | 0.77 | low gap filler |
| Rock Workbench (5) | D | +0.01 | 1.00/2.74/1.00 | 2.50 | 2.09 | middle |
| Rock Workbench (1) | C | +0.09 | 1.00/3.51/1.00 | 2.38 | 2.12 | middle |
| Rock Workbench | A | +0.23 | 1.00/4.73/1.00 | 2.81 | 2.41 | tall anchor |
| Rock Workbench (17) | C | +0.44 | 0.87/3.21/0.87 | 2.18 | 1.91 | middle |
| Rock Workbench (10) | E | +0.69 | 1.42/3.32/1.42 | 2.40 | 1.98 | middle |
| Rock Workbench (11) | C | +0.88 | 0.71/1.57/0.71 | 1.07 | 0.61 | low gap filler |
| Rock Workbench (16) | D | +1.17 | 1.21/3.32/1.21 | 3.03 | 2.59 | tall anchor |
| Rock Workbench (2) | D | +1.37 | 1.27/3.49/1.27 | 3.19 | 2.74 | tall anchor |
| Rock Workbench (15) | C | +1.56 | 1.30/4.56/1.30 | 3.10 | 2.85 | tall anchor |

## Surface recipe

All 18 members use `RockWorkbench_NeutralPBR` with the same authored surface controls: geology scale **2.4 m**, surface patch strength **0.346**, crack amount **0.36**, side grit **0.22**, underside shale **0.20**, side and top shale patches **0**, worn shine **0.099**, color variation **0.62**, and muted neutral dust `(0.32, 0.30, 0.28)`. The saved scene keeps `DarkFracturedDesert`; the captured source prefab changes only the preset to `LightCliffGray` while preserving the source shapes and member transforms to practical floating-point precision. The scene's root sits at `(5.173, 0, -1.265)`; the prefab normalizes that root to the origin.

The workbench preview applies these controls in a per-renderer property block. The current runtime `CliffWall_LightGray` material and decorator set only base, crack, and mineral colors per rock. They leave the material defaults at geology scale **1.1 m**, surface patch strength **0.72**, and top shale **0.42**. Therefore even a correctly shaped runtime rock would have different grain and patch behavior until the runtime applies the complete authored surface recipe.

## Why the current generated cliffs differ

`TopDown3DCliffFaceDecorator` presently picks the catalog's three `Cliff` meshes and places **two rocks per steep section**. Those baked source meshes are wide, low blocks: about **3.86–4.14 m wide × 1.75 m high × 1.11–1.35 m deep** before runtime scaling. Stretching two of them vertically does not create the sample's many narrow, irregularly overlapping upright stones or its broken crown. The present `BoxCollider` per rock is tied to that generic block, not to a sample-derived exterior or stable scarp-wide route plan. The current implementation is an interim physical-rock proof, not a visual match to the user's sample.

The general “Bake All Authored Formation Gameplay Meshes” command searches every `*Reference.prefab` in the source folder. It would also discover `CliffWallSampleReference.prefab`; the cliff source needs an explicit dedicated bake path or exclusion before that broad command is used for routine production baking. No bake was run during this audit.

## Replacement generation rules

1. **Bake the user's rock vocabulary.** Extract the five source-volume recipes, preserve each member's vertical stretch as part of its approved form, and bake a bounded family of closed 3D rock meshes with near/mid/far LODs. New seed-based variants may perturb the two volumes and proportions within measured limits. Do not bake the complete 18-member wall as one repeated stamp, and do not substitute generic wide `Cliff` blocks.
2. **Plan a connected wall before placing stones.** Group neighboring steep sections into one stable scarp parent in absolute world space, with rim, toe, facing, and route openings. The parent owns deterministic module positions across chunk borders. A chunk only instantiates the representation it owns and removes it on unload; reloading or rebasing recreates the same stones.
3. **Compose the measured silhouette.** Use irregular tall anchors, middle stones, short gap fillers, and occasional raised accents. Place multiple depth rows with meaningful overlaps, embed most bases into the sampled terrain, and vary crest heights. Fit the assembly to the full rim-to-toe shape instead of scaling two broad blocks to the drop.
4. **Match the surface and contact.** Apply the authored shader parameters per rock with restrained light-gray tint variation. Bake a simplified collision envelope from the accepted exterior, verify it against the visible rocks and terrain, and screen the entire footprint against site, spawn, formation, and Booter/BigARM route reservations. A climbing affordance requires a separate verified contact and top-out contract.
5. **Keep generation and persistence separate.** The stable scarp/member IDs derive from world seed, the relevant generation versions, parent feature, and absolute owner. Static rock geometry regenerates; only future mineable or destructible changes become saved deltas. Compare fixed-camera views, chunk seams, collision, and full-content streaming cost before replacing the interim runtime family.

This report is source and code evidence, not a visual acceptance of a new generated wall. The user's game-camera review remains the appearance gate.
