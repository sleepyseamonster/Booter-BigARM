# Rock Quality And Production Plan

**Status:** family expansion implemented; mixed pile/scatter composition accepted on 2026-09-06, awaiting saved editable scene source

## Goal

Create excellent, readable rocks through one simple Unity hierarchy workflow, then translate the accepted shape language into deterministic editor-baked production assets for the procedural world.

Crimson Desert is the primary visual model. Horizon Forbidden West supports artist-readable authoring and terrain integration. Path of Exile 2 supports elevated-camera silhouette and material readability. These references guide quality and composition; their assets are not copied.

## Accepted Golden Rock calibration

On 2026-09-05 the user selected a good standalone rock and supplied front, reverse, and top-down views. The useful result came from a two-mass `Blocky Monolith` seed laid almost fully onto its side, with a smaller secondary tilt. The root edit was evidence of a missing generator posture, not a reusable transform requirement. After generating a wider set, the user confirmed that this grammar consistently produces liked rocks.

Captured authoring values:

- seed `2126351350`;
- reference dimensions `1 m` wide by `0.75 m` long before the resting-pose bake;
- lopsidedness `0`, compaction `0`, major fractures `0`, edge damage `1`;
- former root rotation approximately `(-18.374, 147.048, -85.817)` degrees;
- fusion smoothness `0.0657`, surface relaxation `0.45`;
- Dark Fractured Desert material with geology scale `2.4`, surface variation `0.346`, crack amount `0.36`, side grit `0.417`, underside shale `0.905`, side/top shale patches `0`, and worn shine `0.099`.

The generator now bakes that rotation into the editable source volumes and performs a second grounding pass afterward. Creating a Workbench assigns a fresh seed. Width and body length are independently sampled from `0.3–1.2 m` using the mean of three seeded uniform samples: the distribution is bounded and bell-shaped around `0.75 m`, so the middle is common and either limit is rare. The user raised the lower limit from `0.2 m` to `0.3 m` on 2026-09-06. Each seed also receives a stable random turn around the vertical axis and a restrained burial depth of four to twelve percent of its posed height. The Workbench root returns to zero rotation and unit scale. This preserves the raised shoulder and sloped body, avoids a repeated facing direction, and produces controlled size/contact variation without manual root edits. The user visually accepted the original combined result on 2026-09-05, completing the Golden Rock gate.

## Done

The rock lane is complete when:

- one user-shaped Golden Rock establishes the accepted silhouette, fracture, edge, grounding, and material language;
- that accepted language is captured by deterministic editor recipes rather than runtime mesh generation;
- the production baker emits reusable LOD meshes, colliders, and material-ready catalog entries;
- `TopDown3DWorldGenerator` remains the sole runtime world and placement authority;
- the accepted family is readable from the production camera before broader family rollout.

## Architecture

```text
Rock Workbench
    -> editable source volumes in Rock Shape (Edit These)
    -> user-shaped Golden Rock
    -> deterministic editor recipe
    -> TopDown3DProductionRockBaker
    -> baked LOD meshes + collider + material catalog
    -> TopDown3DWorldGenerator placement
```

The Rock Workbench is an editor authoring tool, not a runtime world system. Its temporary preview mesh may use editor-only implicit-surface meshing. Shipped runtime rocks come from the existing baked catalog. Runtime placement uses stable world identity, absolute-coordinate sampling, chunk ownership, and the existing persisted-delta boundary.

## Explicit exclusions

- no separate Grounded Geology window, A/B comparison mode, or diagnostics hierarchy;
- no runtime SDF, marching-tetrahedra, rock-recipe, terrain-uplift, sediment, talus, or geology-field service;
- no second terrain, placement, streaming, persistence, or material authority;
- no bulk regeneration of production families before the Golden Rock is accepted;
- no new formation or landmark categories during the Golden Rock gate.

## Correct sequence

### 1. Recover one authoring path

- Use the direct top-level `Booter & BigARM > Create Rock Workbench` command. Keep `GameObject > Booter & BigARM > Top Down 3D > New Random Rock` as the hierarchy shortcut.
- Keep the normal Inspector limited to physical size, lopsidedness, compaction, major fractures, edge damage, editing-volume visibility, and the two generation actions.
- Put manual source objects beneath `Rock Shape (Edit These)`.
- Bake the accepted standalone-rock `0.1` size into meter-valued settings once and keep the reusable root at `(1, 1, 1)`.
- Use the existing project rock material and surface controls; do not substitute diagnostic textures.

### 2. User builds the Golden Rock — stop gate

Complete. The user accepted the Golden Rock shape language, bounded size distribution, seeded facing, and burial-depth variation across multiple generated rocks on 2026-09-05.

The Golden Rock should answer only these visible questions:

- Is the large silhouette strong from the production camera?
- Do one to three major fractures create structure rather than noise?
- Is edge damage subordinate to the silhouette?
- Does the base look planted rather than balanced or floating?
- Does the normal project rock material read correctly at the accepted physical scale?

### 3. Capture the accepted grammar

After user acceptance, add the smallest non-destructive editor capture needed to preserve the Golden Rock's source shapes, roles, transforms, surface preset, dimensions, and seed as a deterministic recipe. Manual source objects remain intact. The recipe is authoring data, not runtime state.

### 4. Bake one representative production family

Feed the accepted recipe into the existing production baker. Emit the existing LOD/collider/catalog format and keep imported/source data separate from generated runtime assets. Do not replace all 27 families yet.

### 5. Integrate through the existing catalog

Let the existing World Creator selection and placement paths consume the baked representative family. Generated-object identity, chunk unload/reload, authored placement constraints, and persisted runtime deltas remain unchanged because runtime architecture is not being replaced.

### 6. Expand only after the representative family is accepted

Derive a small, purposeful family set from the accepted grammar. Then tune distribution, grounding, contact treatment, and terrain transitions at composition scale. Formations and landmarks resume only after individual-rock quality is stable.

## Proof boundary

Source review and compile safety can show that the tool is coherent. They cannot approve appearance. The user owns Scene/Game visual acceptance and hands-on testing. No automated screenshot, A/B scaffold, or test count substitutes for the Golden Rock decision.

## Completed family expansion

Steps 3–5 are implemented: the saved `ApprovedBoulderFamilyRecipe.asset` captures the accepted source, the existing baker emits three Boulder variants with LODs and collider bounds, and World Creator consumes those catalog slots. Subsequent user-directed corrections preserved recipe burial, unified the rock surface color, and increased formation burial and distribution (`d3cc446`). These are implementation facts; visual acceptance remains user-owned.

On 2026-09-06 the user directed continuation of the plan. The next bounded step is to extend the accepted grammar into the existing `Slab` and `Nodule` roles, which still use older mesh shapes alongside the approved Boulders. Use the same saved source and existing **Build Approved Rocks Into World Creator** command. Slabs lower and broaden the accepted mass; nodules make it more compact. Apply the same derivation to all three LODs around the source ground plane, retaining fractures, seeded variation, burial, and source geometry. These are initial derived shapes for user review, not new hand-authored reference rocks.

Scope is nine existing catalog slots: three variants each of Boulder, Slab, and Nodule. Keep existing mesh GUIDs and catalog IDs, derive collider bounds from the new meshes, and preserve the other 18 slots. World seeds, chunk ownership, generation versions, unload/reload, and persisted-delta schemas remain unchanged. Mesh-bound placement can adjust to the new silhouettes through the existing planner; no new runtime authority is introduced.

Stop when these three roles are baked into the current catalog and the simple build command reproduces them. Validate compilation, mesh/LOD/collider references, source preservation, and transformed normals. Leave appearance to the user. Do not advance to formations, landmarks, further density tuning, or terrain systems in this batch; composition is the following plan step once this family set is visually settled.

Implementation checkpoint: the existing command now bakes all nine slots. Six Slab/Nodule variants (18 LOD assets) and their catalog collider bounds were updated; Boulder geometry, all mesh GUIDs, the source recipe, and production materials were preserved. Unity 6000.4.0f1 compiled and baked successfully in an isolated copy, including the catalog/LOD validator. The five focused baker checks and five canonical formation checks passed. No gameplay or visual test was run. The next user-facing work is formation composition with this small family set, after visual review; further individual-rock tuning is not the automatic next step.

## Accepted composition and current stop condition

On 2026-09-06 the user built and accepted a mixed pile/scatter arrangement in `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`, not the sandbox. The supplied [top view](./Evidence/WorldCreator/RockCompositionReference/MixedPileScatter_Top_2026-09-06.png) and [side view](./Evidence/WorldCreator/RockCompositionReference/MixedPileScatter_Side_2026-09-06.png) are retained as visual authoring references. They show a dense, raised central group, smaller satellite pairs, isolated stones, unequal spacing, and open ground between groups. This is acceptance of the rock composition; sand, slope integration, and clutter are still to be developed.

The user requests three distinct composition types:

- **Open scatter:** more separation and exposed ground between individual rocks and small groups.
- **Mixed pile and scatter:** preserve this accepted arrangement as the reference, including the compact center and outlying rocks.
- **Rock pile:** tighter concentration and more supported stacking than the mixed reference.

At capture time the screenshot showed an unsaved `TopDown3DPrototype*` scene, and the prototype file on disk contained no Rock Workbench objects. The screenshots are preserved; the exact editable arrangement is not yet captured. First save the scene in Unity, then inspect and preserve the actual member transforms and source volumes. Do not reconstruct exact geometry or dimensions from the screenshots, regenerate the user's members, or move the reference to the sandbox automatically.

After source preservation, use the mixed reference for one complete rock-and-ground patch: slope-aware contact, sand accumulation, then gravel/pebble material depth and sparse protruding mesh stones. The open-scatter and pile variants should derive from the existing composition path while keeping the accepted source intact. Keep world identity, chunk ownership, streaming, and saved deltas in the current runtime owners. Tiny embedded clutter is material detail; substantial terrain height changes must agree with collision. Visible blowing sand follows the settled ground treatment. The next implementation brief must name this bounded patch and its terrain/material integration before edits; no broad catalog expansion or new geology platform is implied.
