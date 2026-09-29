# Canyon wall source isolation

2026-09-29. Same isolated mirror, seed, terrain apron, dense Workbench rocks, fixed rim camera, and temporary lighting as the [apron trial](ApronAndDenseDebrisTrial.md). No live production asset changed.

The [normal rim render](ApronDenseRimDiagnostic.png) and [control with fitted cliff meshes disabled](NoFittedFaceTerrainControl.png) retain essentially the same giant smooth wall. The missing stratified mass is therefore in the generated terrain silhouette, not primarily the fitted face overlay. This is a visual comparison, not a measured pixel-diff or a claim that the overlays have no collision cost.

To test whether the existing generated shelf controls only needed more bench width, the mirror narrowed all three smooth drop bands from 0.13–0.15 to 0.07 of the canyon half-width. The [result](NarrowDropWideBenchRejected.png) sharpened the drops into even taller flat panels, exposed sawtooth edges, and left fewer Workbench rocks in the fixed view. This direction was rejected on the visual screen before running a Player or collision suite. The query was restored byte-for-byte to the apron candidate afterward.

The next geometry candidate needs a deliberately broken escarpment profile: parent-scale ledge offsets and erosion that vary along the segment, broad sloped debris runout, and rock outcrops tied to the same shelf positions. It should derive all choices from the canonical segment and absolute coordinates, preserve continuous border samples, and prove floor reservations and near/mid/far agreement before full Player profiling. Surface color and Workbench debris should then follow those same shelf and exposure fields so the layers read at game-camera distance.
