# Rock Quality And Production Plan

**Status:** active; Golden Rock calibration implemented and awaiting visual confirmation

## Goal

Create excellent, readable rocks through one simple Unity hierarchy workflow, then translate the accepted shape language into deterministic editor-baked production assets for the procedural world.

Crimson Desert is the primary visual model. Horizon Forbidden West supports artist-readable authoring and terrain integration. Path of Exile 2 supports elevated-camera silhouette and material readability. These references guide quality and composition; their assets are not copied.

## Accepted Golden Rock calibration

On 2026-09-05 the user selected a good standalone rock and supplied front, reverse, and top-down views. The useful result came from a two-mass `Blocky Monolith` seed laid almost fully onto its side, with a smaller secondary tilt. The root edit was evidence of a missing generator posture, not a reusable transform requirement.

Captured authoring values:

- seed `2126351350`;
- body dimensions `0.925 m` wide by `1.12 m` long before the resting-pose bake;
- lopsidedness `0`, compaction `0`, major fractures `0`, edge damage `0.617`;
- former root rotation approximately `(-18.374, 147.048, -85.817)` degrees;
- fusion smoothness `0.0657`, surface relaxation `0.45`;
- Dark Fractured Desert material with geology scale `2.4`, surface variation `0.346`, crack amount `0.36`, side grit `0.417`, underside shale `0.905`, side/top shale patches `0`, and worn shine `0.099`.

The generator now bakes that rotation into the editable source volumes and performs a second grounding pass afterward. The Workbench root returns to zero rotation and unit scale. This preserves the raised shoulder and sloped body while preventing the post-generation root rotation from pushing the lower contour below flat terrain.

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

The user has supplied the first Golden Rock. The remaining gate is a direct visual comparison of the regenerated, locally posed version against the accepted views. Architecture work must wait for that confirmation rather than rolling the calibration across production assets.

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

## Current stop condition

Stop after the selected Golden Rock can be regenerated with an identity root and post-pose grounding. Wait for the user's visual confirmation before recipe capture, production-family baking, catalog rollout, formations, or terrain integration.
