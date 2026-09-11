# Rock Formation Workbench

The Rock Formation Workbench is the editor-only bridge between one generated rock and a future procedural canyon or cave system. It groups several existing Rock Workbenches without discarding their editable source volumes.

Accepted formations can now be composed into a larger `Rock Landmark Workbench`. See [ROCK_LANDMARK_WORKBENCH.md](./ROCK_LANDMARK_WORKBENCH.md) for the next authoring tier, including deterministic spire complexes and non-destructive capture of hand-arranged formations.

## Reference-driven shape rules

The current direction uses four visible ideas from the supplied formation references:

- Keep several large stone masses readable instead of melting every contact into one soft blob.
- Use deep contact seams and ambient occlusion to give overlapping masses depth.
- Share one material coordinate system so strata, patches, and color feel geological rather than assembled.
- Use a few long, warped fractures across the whole formation in addition to the smaller local crack texture.

These are general visual principles only. No source game assets are copied into this project.

## Fast workflow

Create a complete random rock with:

`Booter & BigARM > Create Rock Workbench`

The same command remains available from the Hierarchy-oriented menu at:

`GameObject > Booter & BigARM > Top Down 3D > New Random Rock`

The main standalone Rock Inspector intentionally shows only `Body Width`, `Body Length`, `Lopsidedness`, `Compaction`, `Major Fractures`, `Edge Damage`, and `Show Editing Volumes`. Body dimensions describe the source before its automatic resting pose. Click `Generate New Rock` for a new seed and bounded bell-shaped dimensions, or `Regenerate This Rock` to preserve the current seed and displayed dimensions. Mesh, collider, material, detailed surface, seed-gallery, formation, and repair controls are under `Advanced`.

Standalone rocks use the accepted small physical scale directly in meter-valued settings while the Rock Workbench root stays at `(1, 1, 1)`. Existing older standalone rocks are converted once when regenerated. Formation members keep their formation-owned scale. The visible rock always uses the normal project Rock Workbench material; there is no diagnostic or A/B material path.

New standalone workbenches begin from the accepted Golden Rock shape and surface calibration: a low sideways `Blocky Monolith` with a raised shoulder, broad sloped face, and full edge damage. Creation assigns a fresh seed immediately. Body width and body length are independently sampled from `0.2–1.2 m` by averaging three seeded uniform samples, producing a bounded bell-shaped distribution centered on `0.7 m`. The resting rotation is baked into the editable source volumes, followed by a seed-stable random vertical-axis turn and a local grounding pass with four-to-twelve-percent burial variation. The root therefore remains at zero rotation and unit scale, while `Regenerate This Rock` preserves its dimensions/facing/contact and `Generate New Rock` produces a new repeatable variation.

Generated rocks use two to four additive source masses plus zero to three subtractive fracture cuts, depending on physical dimensions and the fracture control. `Weathered Block` supplies rounded mass, `Wedge` cuts a broad planar slope, and `Tapered Stone` supplies a truncated pyramidal profile. Those primitives are assembled through eight deterministic silhouette families: clustered `Boulder`, low `Slab`, faceted `Angular Chunk`, notched `Split Lobe`, narrow `Shard`, `Fractured Boulder`, `Blocky Monolith`, and `Broken Slab`. `Auto` selects a family from the rock seed and is intentionally hidden to keep the main workflow simple. Turn on `Show Editing Volumes`, expand `Rock Shape (Edit These)`, and select an individual source volume to change its source shape, operation, rotation, scale, or position manually.

The current individual-rock quality sequence is controlled by [ROCK_QUALITY_AND_PRODUCTION_PLAN.md](./ROCK_QUALITY_AND_PRODUCTION_PLAN.md). The Golden Rock shape language is accepted; stop after the seeded size/facing/contact check before rolling it across the catalog, formations, or terrain.

Create a complete random formation with:

`GameObject > Booter & BigARM > Top Down 3D > New Random Rock Formation`

The Formation Inspector begins with a `Formation Type`, then exposes the same two physical controls: `Width` sets the horizontal footprint and `Height` sets the vertical envelope. Their combined dimensions automatically determine member count and base-rock source complexity; there is no separate complexity slider to balance manually. `Generate New Formation` replaces the generated member rocks with a new seed; Unity Undo restores the previous arrangement.

The current formation categories are:

- `Connected Outcrop` — one fused geological formation built around a dominant anchor, framed crevice, pillars, buttresses, and attached base talus.
- `Scattered Rocks` — 5–20 separate, partially buried boulders distributed across a loose field with a deliberate large/medium/small hierarchy. Width and height automatically move the count through that full range. These rocks do not fuse to one another, so clean sand remains visible between them.
- `Pile Of Rocks` — 5–20 touching stones assembled as an all-sided mound. Broad, partially buried base slabs carry an overlapping middle shelf and one or two offset cap rocks, while restrained gaps preserve readable contact crevices. It uses the fused geological shell by default, so hidden overlap geometry is removed while seams remain visible.
- `Small Ridge Pillars` — 7–18 rocks arranged as a low, gently wandering ridge body with several short upright pillars and attached edge talus. The backbone stays broader and lower than the pillar accents, producing a directional silhouette without becoming a tall spire complex. It uses the fused geological shell by default while leaving every member rock editable.

## Build a formation from hand-arranged rocks

1. Arrange two or more `Rock Workbench` objects in the Scene.
2. Select their root objects. Selecting one of their source volumes also works.
3. Choose `GameObject > Booter & BigARM > Top Down 3D > Create Formation From Selected Rocks`.
4. Select the new `Rock Formation Workbench` parent.
5. Move, rotate, regenerate, or edit any child Rock Workbench normally.

`Fused Geological Seams` is the default for new formations. Each child Rock Workbench remains editable, but its source volumes are kept together as one named scalar-field group. The groups are sampled into one closed exterior shell, so hidden overlapping member shells are not generated. A deterministic vertex-color mask records the boundary between the two closest rock groups and the rock shader turns that mask into a dark, rough contact seam.

`Preserve Natural Seams` keeps each child mesh visible and applies one seed, scale, origin, and long-fracture field to all of them. It remains useful for comparing the original authored masses, but overlapping rocks retain their complete hidden shells in this mode.

`Smooth Fused Preview` rebuilds every contributing source volume beneath every child workbench through the existing scalar-field mesher. It creates one temporary closed mesh on the formation parent and hides the child renderers while keeping all source objects editable. The Inspector status explicitly reports whether the result is one connected surface.

## Useful controls

- `Formation Seed` changes the shared material and long-fracture placement reproducibly.
- `Tessellation Detail` is an intuitive 1–5 surface-sampling control applied to every member rock without regenerating or repositioning the formation. `1` rebuilds fastest; `5` produces the finest silhouette and costs the most editor time.
- `Rock Smoothing` changes how softly the editable source masses blend inside every member rock. It does not fuse separate scattered rocks or move hand-arranged members.
- `Surface Relaxation` is a separate post-mesh pass. It reduces voxel-scale teeth and stair steps after extraction, uses a shrink-resistant two-pass relaxation, and restores the original lowest ground-contact height. It does not change source-volume placement or triangle connectivity.
- `Long Fractures` controls only the formation-scale fracture layer.
- `Fracture Spacing` is the approximate physical spacing between long fracture planes.
- `Fused Voxel Size` controls both single-shell modes. `Fused Join Softness` fully rounds Smooth Fused Preview and supplies only restrained contact smoothing to Fused Geological Seams.
- `Geological Seam Width` and `Geological Seam Strength` are advanced controls for the default fused geological shell.

## Procedural hierarchy

The workbench deliberately keeps three deterministic levels separate:

1. Editable weathered-block, wedge, and tapered-stone volumes are smoothly combined into one base rock.
2. Completed base rocks receive formation-level position, rotation, scale, and composition roles.
3. The grouped rock fields are sampled into one exterior formation shell while member boundaries remain available for seams.

The separate Rock Landmark Workbench adds a fourth level above this hierarchy. It arranges complete formation workbenches as crown, shoulder, apron, and outlier roles while leaving all three lower levels editable.

The `Connected Outcrop` planner starts with one visibly dominant, partially buried anchor mass. It reserves a seeded open wedge in the surrounding members, braces both sides of that wedge with attached buttresses, and distributes the remaining pillars and supports around the other directions. This produces a readable entrance-like crevice without turning the whole formation into a one-sided wall. When complexity permits, an overlapping crown sits opposite the opening. Small, low talus rocks attach to outer structural members instead of collecting inside the core, which gives the base a wider debris transition while keeping the fused shell connected.

The `Scattered Rocks` planner treats the supplied desert reference as a composition guide rather than source art. It generates one or two dominant boulders, several broad slabs, and smaller irregular fragments. Those roles deliberately draw from different silhouette families: dominant rocks vary among boulder, split-lobe, and angular profiles; slabs use low wedge-led profiles; fragments favor angular chunks and shards. This prevents a field of differently sized copies of the same rounded cube. Deterministic rejection placement prevents the boulders from stacking into another outcrop, while elliptical distribution, varied rotation, restrained height, and individual burial keep the result loose and grounded. Grounding uses a broad set of inset bottom samples so a tilted silhouette cannot balance on one extreme point. In the Landscape Authoring Sandbox, member positions and up axes are conformed to the production terrain generator's height and normal; outside that sandbox, the formation's local horizontal plane is the fallback. Formation-level tessellation and smoothing values are copied into every member and can be changed live without replacing the members. Smaller rocks use at least five source masses, and their normal compaction range leaves more of those masses visible while retaining a safe connected contact at extreme settings. The global sample-count and cells-per-axis guards still cap pathological rebuild cost. Each boulder is still a complete blended-volume Rock Workbench and can be moved, stretched, or regenerated independently.

The `Pile Of Rocks` planner is based on the manually arranged reference pile: a low radial foundation, a denser second tier, and a sparse offset crown. Every tier is distributed around the full circle instead of favoring the reference camera, so the silhouette and crevices remain legible from all sides and above. Each middle or cap rock is deliberately attached to a lower host, keeping the formation connected without erasing the layered-stone reading.

The `Small Ridge Pillars` planner is based on the hand-built low ridge reference in the Landscape Authoring Sandbox. Broad overlapping body rocks follow a seeded shallow bend, short narrow pillars alternate across that backbone, and restrained talus anchors both ends and sides. The ridge heading rotates with the formation seed, so its directional composition is not tied to one camera angle. Pillars attach to high contacts on separate body hosts instead of stacking into one tower, while edge talus remains connected to the geological shell.

These composition rules are intentionally derived from `Width` and `Height`. A low width with a high height produces a narrow pillar composition; a high width with a low height produces a broad, low composition. Each generated member bakes its effective vertical size into its own base-rock dimensions, which lets its source-mass budget respond to formation scale while preserving editable nonuniform horizontal shaping. The generator remains quick to reroll, judge, and manually edit without a second set of complexity controls.

The seed hierarchy is `formation seed -> member seed -> silhouette family -> source-volume shape seed`. Repeating the same formation seed and controls reconstructs the same member roles, transforms, base-rock controls, silhouettes, source-shape choices, and final seam mask. This work remains editor-only until its shape language is visually accepted and translated into the runtime planner's absolute-coordinate, stable-identity, chunk-owned generation contract.

The production URP renderer also contains a restrained Screen Space Ambient Occlusion feature named `Rock Contact Occlusion`. It emphasizes close contacts, holes, and creases. It is intentionally downsampled and moderate; hands-on Scene and Game view evaluation still decides whether its intensity or radius should change.

## Procedural-world boundary

This is an authoring and evaluation system, not a second runtime world generator. The formation seed is stable and every result is derived from source transforms and settings, so accepted patterns can later be translated into deterministic runtime rules. Chunk identity, streaming ownership, unload/reload, stable generated-object IDs, and persisted runtime deltas remain unchanged in this iteration.
