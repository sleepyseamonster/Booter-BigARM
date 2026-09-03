# Rock Formation Workbench

The Rock Formation Workbench is the editor-only bridge between one generated rock and a future procedural canyon or cave system. It groups several existing Rock Workbenches without discarding their editable source cubes.

## Reference-driven shape rules

The current direction uses four visible ideas from the supplied formation references:

- Keep several large stone masses readable instead of melting every contact into one soft blob.
- Use deep contact seams and ambient occlusion to give overlapping masses depth.
- Share one material coordinate system so strata, patches, and color feel geological rather than assembled.
- Use a few long, warped fractures across the whole formation in addition to the smaller local crack texture.

These are general visual principles only. No source game assets are copied into this project.

## Create and edit a formation

1. Arrange two or more `Rock Workbench` objects in the Scene.
2. Select their root objects. Selecting one of their source cubes also works.
3. Choose `GameObject > Booter & BigARM > Top Down 3D > Create Formation From Selected Rocks`.
4. Select the new `Rock Formation Workbench` parent.
5. Move, rotate, regenerate, or edit any child Rock Workbench normally.

`Preserve Natural Seams` is the default. Each child keeps its own mesh, but the formation applies one seed, scale, origin, and long-fracture field to all of them. This is the preferred mode for the layered slab language in the references.

`Smooth Fused Preview` rebuilds every contributing source cube beneath every child workbench through the existing scalar-field mesher. It creates one temporary closed mesh on the formation parent and hides the child renderers while keeping all source objects editable. The Inspector status explicitly reports whether the result is one connected surface.

## Useful controls

- `Formation Seed` changes the shared material and long-fracture placement reproducibly.
- `Long Fractures` controls only the formation-scale fracture layer.
- `Fracture Spacing` is the approximate physical spacing between long fracture planes.
- `Fused Voxel Size` and `Fused Join Softness` apply only to Smooth Fused Preview.

The production URP renderer also contains a restrained Screen Space Ambient Occlusion feature named `Rock Contact Occlusion`. It emphasizes close contacts, holes, and creases. It is intentionally downsampled and moderate; hands-on Scene and Game view evaluation still decides whether its intensity or radius should change.

## Procedural-world boundary

This is an authoring and evaluation system, not a second runtime world generator. The formation seed is stable and every result is derived from source transforms and settings, so accepted patterns can later be translated into deterministic runtime rules. Chunk identity, streaming ownership, unload/reload, stable generated-object IDs, and persisted runtime deltas remain unchanged in this iteration.
