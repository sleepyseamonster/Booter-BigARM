# Rock Formation Workbench

The Rock Formation Workbench is the editor-only bridge between one generated rock and a future procedural canyon or cave system. It groups several existing Rock Workbenches without discarding their editable source cubes.

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

The main Rock Inspector intentionally shows only `Overall Size`, `Height`, `Lopsidedness`, `Compaction`, and `Show Editing Cubes`. Click `Generate New Rock` for a new seed or `Update Current Rock With These Settings` to preserve the current seed. Mesh, collider, material, detailed surface, seed-gallery, and repair controls are under `Advanced`.

Create a complete random formation with:

`GameObject > Booter & BigARM > Top Down 3D > New Random Rock Formation`

The Formation Inspector intentionally shows only `Overall Size`, `Complexity`, `Height`, `Rock Connections`, and `Long Cracks`. Overall size and complexity automatically determine the member-rock count. `Generate New Formation` replaces the generated member rocks with a new seed; Unity Undo restores the previous arrangement.

## Build a formation from hand-arranged rocks

1. Arrange two or more `Rock Workbench` objects in the Scene.
2. Select their root objects. Selecting one of their source cubes also works.
3. Choose `GameObject > Booter & BigARM > Top Down 3D > Create Formation From Selected Rocks`.
4. Select the new `Rock Formation Workbench` parent.
5. Move, rotate, regenerate, or edit any child Rock Workbench normally.

`Fused Geological Seams` is the default for new formations. Each child Rock Workbench remains editable, but its cube volumes are kept together as one named scalar-field group. The groups are sampled into one closed exterior shell, so hidden overlapping member shells are not generated. A deterministic vertex-color mask records the boundary between the two closest rock groups and the rock shader turns that mask into a dark, rough contact seam.

`Preserve Natural Seams` keeps each child mesh visible and applies one seed, scale, origin, and long-fracture field to all of them. It remains useful for comparing the original authored masses, but overlapping rocks retain their complete hidden shells in this mode.

`Smooth Fused Preview` rebuilds every contributing source cube beneath every child workbench through the existing scalar-field mesher. It creates one temporary closed mesh on the formation parent and hides the child renderers while keeping all source objects editable. The Inspector status explicitly reports whether the result is one connected surface.

## Useful controls

- `Formation Seed` changes the shared material and long-fracture placement reproducibly.
- `Long Fractures` controls only the formation-scale fracture layer.
- `Fracture Spacing` is the approximate physical spacing between long fracture planes.
- `Fused Voxel Size` controls both single-shell modes. `Fused Join Softness` fully rounds Smooth Fused Preview and supplies only restrained contact smoothing to Fused Geological Seams.
- `Geological Seam Width` and `Geological Seam Strength` are advanced controls for the default fused geological shell.

## Procedural hierarchy

The workbench deliberately keeps three deterministic levels separate:

1. Editable cube volumes are smoothly combined into one base rock.
2. Completed base rocks receive formation-level position, rotation, scale, and composition roles.
3. The grouped rock fields are sampled into one exterior formation shell while member boundaries remain available for seams.

The current automatic formation planner is still a first-pass linear arrangement. The next composition phase must distribute primary masses, buttresses, upper tiers, crevices, and perimeter debris volumetrically, then reject formations that read as thin facades from top or surrounding viewpoints. That work remains editor-only until its shape language is visually accepted and translated into the deterministic runtime planner.

The production URP renderer also contains a restrained Screen Space Ambient Occlusion feature named `Rock Contact Occlusion`. It emphasizes close contacts, holes, and creases. It is intentionally downsampled and moderate; hands-on Scene and Game view evaluation still decides whether its intensity or radius should change.

## Procedural-world boundary

This is an authoring and evaluation system, not a second runtime world generator. The formation seed is stable and every result is derived from source transforms and settings, so accepted patterns can later be translated into deterministic runtime rules. Chunk identity, streaming ownership, unload/reload, stable generated-object IDs, and persisted runtime deltas remain unchanged in this iteration.
