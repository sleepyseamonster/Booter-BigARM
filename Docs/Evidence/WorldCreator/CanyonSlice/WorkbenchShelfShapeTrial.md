# Workbench shelf silhouette study

2026-09-29. Isolated mirror geometry study; no production asset, scene, or dirty World Creator file was changed.

The saved 18-rock `CliffWallSampleReference.prefab` is composed mainly of vertically stretched Workbench rocks. [A trial baker](ReorientedWorkbenchSourceBakerTrial.cs.txt) laid those 18 source rocks on their sides in three staggered courses. Their meshes still read as a chain of separate long boulders. That source is useful for individual cliff accents, but rotating the whole collection does not make a believable sedimentary wall.

To test whether joining the mass solved that problem, [a second temporary baker](ConnectedWorkbenchShelfBakerTrial.cs.txt) used the Rock Workbench implicit-volume mesher on a connected core with offset bedding tiers and crown volumes. It generated one connected component with bounds approximately 36.5 × 8.4 × 7.3 m. LOD0/1/2 had 59,648 / 58,420 / 31,572 triangles. The first two LODs barely simplify because the mesher's grid limits converge on similar resolutions; the third remains much too heavy for a repeated streamed decoration without a separate budget and decimation plan.

The [fixed game-camera render](ConnectedWorkbenchShelfGameCamera.png) reads as one dark elongated boulder, and the [overview](ConnectedWorkbenchShelfOverview.png) shows it detached from the terrain's layered cliff. These are deliberately hand-positioned **silhouette checks**, not generated placement, collision, streaming, or performance proof. The shape fails before those later gates. No source prefab or production mesh was saved from it.

Design conclusion for the next implementation: let the canonical terrain query and its near/middle/far representations create the broad strata and continuous cliff silhouette. Use saved Workbench rocks for terrain-screened outcrops, broken ledge edges, and talus, rather than stamping a large freestanding wall module. A new wall-scale Workbench source would need its own editable authoring and visual acceptance; scaling, rotating, or fusing the current upright source is not enough.
