# Legger topology iteration 01

October 15, 2026. Owner: Blender under Gottspan coordination.

The author requested an incremental topology reduction, keeping density high for now. This is a review candidate toward a game asset, not a completed game-ready mesh or approved deformation topology.

## Source and preservation

- Author-saved source: `SourceArt/Blender/Studies/Legger/Legger.blend`.
- Separate candidate: `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration01.blend`.
- The source is not overwritten. Both Blender files remain ignored and local-only under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).
- User-authored world transform is retained exactly, including uniform scale 0.7425782084 and approximately 2.227735 m overall height.
- Source mesh begins with 2,030,602 vertices and 4,064,968 triangles, one material, and one UV layer. Three source image resources are packed.

## Method and evidence

[Iteration tool](../../../../Tools/Art/Blender/iterate_legger_topology.py) uses Blender 5.2.2 LTS in a hidden background process with embedded auto-execution disabled. It handles a source saved in Edit Mode, applies a 0.90 Collapse Decimate ratio, then pairs compatible triangles into quads using a 3 degree face-angle limit and a 40 degree shape-angle limit. Quad pairing respects seam, sharp, UV, color and material differences. It does not create mechanical articulation or planned animation edge loops.

[Review tool](../../../../Tools/Art/Blender/compare_legger_topology.py) compares front, side, elevated and underside orthographic renders in textured and neutral shading. Pixel overlap measures apply only to those views and render resolution. Bidirectional nearest-surface samples do not establish a global Hausdorff bound or prove zero loss of all detail. Distances are reported in mesh-local units; multiply by the preserved uniform world scale for meters.

Raw local renders and diagnostic logs live under ignored `Logs/Legger20261015/Topology01/`. The review candidate preserves the existing textures and UV layer; there is no new UV unwrap, bake, rig, collision, LOD or Unity import in this slice. The open user session is not reloaded, focused or modified.

## World integration boundary

Deterministic world identity, chunk streaming/unload/reload, generated-object identity, authored placement constraints and persisted runtime deltas are not applicable to this local source-mesh operation: no world placement, scene, actor identity, generation or save system is changed. Future Unity integration must handle those actor concerns separately and retain Greater Wasteland's fixed always-loaded terrain.

## Verified result

The candidate reopened successfully in Blender 5.2.2 LTS. See [mesh and sampled-surface report](report.json) and [render comparison metrics](image_comparison.json).

| Measure | Saved source | Iteration 01 |
| --- | ---: | ---: |
| Vertices | 2,030,602 | 1,827,353 |
| Triangles | 4,064,968 | 3,658,470 |
| Polygon faces | 4,064,968 | 3,441,297 |
| Quad faces | 0 | 217,173 |
| Boundary edges | 47 | 47 |
| Non-manifold edges (including boundary) | 127 | 127 |
| Loose vertices | 0 | 0 |

Triangle reduction is 10.00003%; most faces remain triangles because the pairing constraints are deliberately conservative. This is not a full quad retopology. Source transform is identical; height differs only by approximately 0.011 mm from the reduced geometry's bounding extremum.

The largest sampled source-to-candidate distance is approximately 0.4621 mm in world scale; the reverse sample maximum is approximately 0.0709 mm. Both directions use deterministic vertex-index samples, not exhaustive surface coverage. At 1024 square pixels, four-view silhouette overlap ranges from 99.99349% to 99.99669%, with 6–18 differing thresholded silhouette pixels per view. Textured RGB differences on overlapping pixels average 0.80–1.37 on a 0–255 scale. The review found no conspicuous silhouette or large-detail regression at these views. Existing surface irregularities remain present; no cleanup of the source's 127 non-manifold edges was attempted.

Source and candidate retain one UV layer and one material, and the candidate retains three packed images. No source file, Unity asset or open Blender session was modified by this pass. Future reductions should compare against the preserved full-density source, not only against the previous reduced iteration, to avoid hiding cumulative losses.
