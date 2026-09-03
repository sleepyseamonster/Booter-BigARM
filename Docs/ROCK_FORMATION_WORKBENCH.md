# Rock Formation Workbench

The Rock Formation Workbench is the editor-only bridge between one generated rock and a future procedural canyon or cave system. It groups several existing Rock Workbenches without discarding their editable source volumes.

## Reference-driven shape rules

The current direction uses four visible ideas from the supplied formation references:

- Keep several large stone masses readable instead of melting every contact into one soft blob.
- Use deep contact seams and ambient occlusion to give overlapping masses depth.
- Share one material coordinate system so strata, patches, and color feel geological rather than assembled.
- Use a few long, warped fractures across the whole formation in addition to the smaller local crack texture.

These are general visual principles only. No source game assets are copied into this project.

## Fast workflow

Create a complete random rock with:

`GameObject > Booter & BigARM > Top Down 3D > New Random Rock`

The main Rock Inspector intentionally shows only `Width`, `Height`, `Lopsidedness`, `Compaction`, and `Show Editing Volumes`. `Width` controls both horizontal axes while `Height` controls the vertical axis independently, so narrow pillars are not shortened by their footprint and broad rocks can remain genuinely low. The editable source-mass count is automatic: small dimensions use a simple cluster while greater width or height adds masses for a richer silhouette. Click `Generate New Rock` for a new seed or `Update Current Rock With These Settings` to preserve the current seed. Mesh, collider, material, detailed surface, seed-gallery, and repair controls are under `Advanced`.

Generated rocks now use three editable source shapes. `Weathered Block` supplies a dependable central mass, `Wedge` adds a broad planar slope, and `Tapered Stone` adds a truncated pyramidal profile. Every rock with enough source masses receives an outward-facing wedge, larger rocks also receive a tapered detail, and the remaining supports and details use a seeded mixture. Their overlap remains sufficient for one connected scalar-field exterior, but angled sources are oriented toward the silhouette instead of disappearing inside the core. This does not add another top-level slider. Turn on `Show Editing Volumes`, expand the rock, and select an individual source volume to change its `Source Shape`, rotation, scale, or position manually.

Create a complete random formation with:

`GameObject > Booter & BigARM > Top Down 3D > New Random Rock Formation`

The Formation Inspector begins with a `Formation Type`, then exposes the same two physical controls: `Width` sets the horizontal footprint and `Height` sets the vertical envelope. Their combined dimensions automatically determine member count and base-rock source complexity; there is no separate complexity slider to balance manually. `Generate New Formation` replaces the generated member rocks with a new seed; Unity Undo restores the previous arrangement.

The current formation categories are:

- `Connected Outcrop` — one fused geological formation built around a dominant anchor, framed crevice, pillars, buttresses, and attached base talus.
- `Scattered Rocks` — 8–15 separate, partially buried boulders distributed across a loose field with a deliberate large/medium/small hierarchy. These rocks do not fuse to one another, so clean sand remains visible between them.

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
- `Long Fractures` controls only the formation-scale fracture layer.
- `Fracture Spacing` is the approximate physical spacing between long fracture planes.
- `Fused Voxel Size` controls both single-shell modes. `Fused Join Softness` fully rounds Smooth Fused Preview and supplies only restrained contact smoothing to Fused Geological Seams.
- `Geological Seam Width` and `Geological Seam Strength` are advanced controls for the default fused geological shell.

## Procedural hierarchy

The workbench deliberately keeps three deterministic levels separate:

1. Editable weathered-block, wedge, and tapered-stone volumes are smoothly combined into one base rock.
2. Completed base rocks receive formation-level position, rotation, scale, and composition roles.
3. The grouped rock fields are sampled into one exterior formation shell while member boundaries remain available for seams.

The `Connected Outcrop` planner starts with one visibly dominant, partially buried anchor mass. It reserves a seeded open wedge in the surrounding members, braces both sides of that wedge with attached buttresses, and distributes the remaining pillars and supports around the other directions. This produces a readable entrance-like crevice without turning the whole formation into a one-sided wall. When complexity permits, an overlapping crown sits opposite the opening. Small, low talus rocks attach to outer structural members instead of collecting inside the core, which gives the base a wider debris transition while keeping the fused shell connected.

The `Scattered Rocks` planner treats the supplied desert reference as a composition guide rather than source art. It generates one or two dominant boulders, several broad slabs, and smaller irregular fragments. Deterministic rejection placement prevents the boulders from stacking into another outcrop, while elliptical distribution, varied rotation, restrained height, and individual burial keep the result loose and grounded. Grounding uses a broad set of inset bottom samples so a tilted silhouette cannot balance on one extreme point. In the Landscape Authoring Sandbox, member positions and up axes are conformed to the production terrain generator's height and normal; outside that sandbox, the formation's local horizontal plane is the fallback. Small scattered members also use scale-aware voxel size and join softness so their angled planes remain readable. Each boulder is still a complete blended-volume Rock Workbench and can be moved, stretched, or regenerated independently.

These composition rules are intentionally derived from `Width` and `Height`. A low width with a high height produces a narrow pillar composition; a high width with a low height produces a broad, low composition. Each generated member bakes its effective vertical size into its own base-rock dimensions, which lets its source-mass budget respond to formation scale while preserving editable nonuniform horizontal shaping. The generator remains quick to reroll, judge, and manually edit without a second set of complexity controls.

The seed hierarchy is `formation seed -> member seed -> source-volume shape seed`. Repeating the same formation seed and controls reconstructs the same member roles, transforms, base-rock controls, source-shape choices, and final seam mask. This work remains editor-only until its shape language is visually accepted and translated into the runtime planner's absolute-coordinate, stable-identity, chunk-owned generation contract.

The production URP renderer also contains a restrained Screen Space Ambient Occlusion feature named `Rock Contact Occlusion`. It emphasizes close contacts, holes, and creases. It is intentionally downsampled and moderate; hands-on Scene and Game view evaluation still decides whether its intensity or radius should change.

## Procedural-world boundary

This is an authoring and evaluation system, not a second runtime world generator. The formation seed is stable and every result is derived from source transforms and settings, so accepted patterns can later be translated into deterministic runtime rules. Chunk identity, streaming ownership, unload/reload, stable generated-object IDs, and persisted runtime deltas remain unchanged in this iteration.
