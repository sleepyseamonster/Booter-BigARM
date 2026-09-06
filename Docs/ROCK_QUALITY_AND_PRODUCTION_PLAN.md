# Rock Quality And Production Plan

**Status:** mixed-reference ground-contact authoring implemented on 2026-09-06; visual acceptance remains user-owned; deposited sand is next

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

The user subsequently saved the prototype and clarified that **open scatter and rock pile will be hand-built separately by the user**. Do not derive or generate those references automatically. Only the mixed composition is active now.

The saved source has been preserved as [MixedPileScatterReference.prefab](../Assets/_Project/Art/Environment/Rocks/Source/MixedPileScatterReference.prefab), with persistent meshes in [MixedPileScatterReferenceMeshes.asset](../Assets/_Project/Art/Environment/Rocks/Source/MixedPileScatterReferenceMeshes.asset). It contains 19 independently editable Rock Workbench members and 38 source volumes. Its rendered bounds are approximately 5.6705 m wide, 6.9391 m long, and 1.1584 m high, centered at `(1.5698, 0.5064, -1.6461)` in the original prototype frame. The prefab root is identity-transformed and tagged `EditorOnly`: this is an authoring reference, not a new runtime formation or production mesh family.

Capture used the saved scene in an isolated Unity copy, cloned the source members without regeneration, and baked their existing source volumes for persistent reference visibility. Reload validation compared all member authoring settings, descendant transforms, active states, and source-volume settings, and checked mesh/collider reference agreement. The original prototype scene and its unrelated saved changes were not rewritten or included in the capture commit. Do not reconstruct exact geometry or dimensions from the screenshots, regenerate the user's members, or move the reference to the sandbox automatically.

## Next bounded batch: mixed formation ground contact

Use the mixed reference for one complete rock-and-ground patch. Ground contact comes first, followed by deposited sand, gravel/pebble material depth with sparse protruding mesh stones, and finally visible blowing sand. The current batch begins with the following integration constraints:

- Preserve the central pile's support relationships and the outlying rocks' horizontal spacing. Share slope orientation within contact groups, but sample burial separately for each ground-contact rock. Upper rocks follow their supports rather than being flattened to the ground. Never overwrite the accepted reference.
- Keep `TopDown3DWorldGenerator` as the adapter to the existing World Creator surface query. Rock plans already depend on that query, so terrain sampling must not recursively ask the same rock planner for its own height. Establish the base-ground/contact ordering before adding rock-driven sand heights.
- Use the existing semantic wind and surface-material inputs for ground coverage. The currently disabled deposited-dust decorator is a visual overlay without collision; simply enabling it does not satisfy the ground-contact outcome.
- Any substantial deposited-sand height must feed the same surface used for terrain rendering, normals, collision, and subsequent placement. Keep world seed/version ownership, absolute coordinates, chunk borders, unload/reload, and persisted deltas in the current runtime owners.
- Tiny embedded gravel belongs in coordinated ground color, height, normal, and roughness textures; reserve sparse meshes for visible protrusions. Extend the existing terrain material instead of introducing a separate clutter world or duplicating materials by formation.
- Expose only controls needed to shape this mixed patch in the existing authoring workflow. The captured prefab remains the untouched reference; do not add an A/B panel or generate the user's future scatter/pile examples.

The next stop condition is one mixed composition seated convincingly against the ground with its central stack intact, followed by coherent sand and clutter treatment. Source and compile checks verify implementation safety; appearance remains user-owned. No broad catalog expansion or new geology platform is implied.

### Ground-contact implementation checkpoint — 2026-09-06

`Booter & BigARM > Create Mixed Formation Ground` creates one EditorOnly hierarchy root in the active scene, reusing the existing landscape terrain-context component. The Inspector exposes Terrain Location (chunk coordinates), Shallow Burial, Deep Burial, Maximum Ground Tilt, and Update Ground Contact. It starts one chunk east of the original reference to avoid overlapping the original rocks. Save the scene after creating the setup; its disposable ground and rock copy rebuild on editor reload and are removed in Play Mode.

The displayed terrain uses the production World Creator near-representation compiler, including production material packing and a matching mesh collider. Contact fitting samples those actual terrain collision triangles. Contact groups share slope orientation, capped at 20 degrees. Per the user's correction, ground-contact rocks now settle independently, with bell-shaped burial between the Inspector's Shallow Burial and Deep Burial values (defaults 0.035–0.15 m, midpoint 0.0925 m). Each rock averages three independent seeded uniform samples: middle depths are common and either extreme is rare. Seeds combine the captured rock seed with its saved hierarchy slot, remaining stable on rebuild and terrain-location changes; reordering the source hierarchy can change samples. For small rocks, narrow the range to at most 45 percent of their height before sampling, avoiding clipped probability spikes and complete disappearance. Upper rocks inherit their highest supporting rock's vertical displacement, retaining authored support overlap instead of flattening the pile. This remains a conservative bounds-based contact heuristic, not a rock physics simulation.

The source prefab, its persistent meshes, original scene rocks, production catalog, and runtime generation are unchanged. The temporary copy disables automatic remeshing and retains the saved material settings. This batch adds no world-identity/version changes, streaming ownership, or persisted deltas: it is editor-only authoring against the existing world surface. Production formation placement still requires a later integration step; the preview is not shipped placement.

Source review and Unity 6000.4.0f1 compilation succeeded in an isolated project copy. No visual, gameplay, or automated test suite was run. The next implementation batch is deposited sand around this same mixed patch, respecting the shared height/collision constraints above, followed by pebble material depth and sparse protrusions. Do not generate the user's future open-scatter or pile references.
