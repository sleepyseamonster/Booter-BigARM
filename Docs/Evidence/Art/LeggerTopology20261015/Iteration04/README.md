# Legger topology iteration 04

October 15, 2026. Continues the author's incremental reduction workflow from [iteration 03](../Iteration03/README.md), whose saved input hash matches its prior receipt. All earlier Blender versions remain preserved locally.

- Input: `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration03.blend`.
- Candidate: `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`.
- Original reference: `SourceArt/Blender/Studies/Legger/Legger.blend`.
- Method: [conservative topology iteration](../../../../../Tools/Art/Blender/iterate_legger_topology.py) at ratio 0.90, followed by seam/sharp/UV/color/material-aware quad pairing with 3 degree face and 40 degree shape limits.
- Runtime: Blender 5.2.2 LTS, hidden background processes, embedded auto-execution disabled and Python errors mapped to exit code 1.

## Geometry and preservation

| Measure | Iteration 03 | Iteration 04 |
| --- | ---: | ---: |
| Vertices | 1,479,798 | 1,331,630 |
| Triangles | 2,963,358 | 2,667,021 |
| Faces | 2,805,471 | 2,531,044 |
| Quads | 157,887 | 135,977 |
| Boundary edges | 49 | 50 |
| Non-manifold edges (including boundary) | 128 | 130 |
| Loose vertices | 0 | 0 |

Triangle count falls 10.00004% from iteration 03, or 34.39011% from the original. The world transform and UV layer are unchanged. The candidate keeps one material and all three packed source textures. A separate native Blender reopen supplies the cumulative validation.

Immediate textured and neutral orthographic comparisons use identical framing before/after the reduction. Cumulative reviews use original-source framing and compare directly against the full-density mesh. Deterministic bidirectional world-space samples put the largest original-to-candidate deviation at approximately 0.7687 mm, unchanged from iteration 03's sampled maximum; the 99th percentile is approximately 0.0854 mm. The reverse sample maximum is approximately 0.1238 mm. Samples do not establish a global error bound or mathematically lossless detail.

The defect-edge audit locates the change near an existing four-face junction in the upper-front region at approximately world height 2.079 m. It adds one 1.132 mm boundary edge and one approximately 1.589 mm three-face edge near that existing defect. The changed endpoints remain within approximately 1.132 mm of prior defect vertices. Total boundary length rises by 1.132 mm, from 0.0697421393 to 0.0708737791 m. No speculative weld or hole fill was applied. This candidate is a visual reduction review, not certified watertight or completed game-ready topology.

See [iteration report](report.json), [immediate image metrics](image_comparison.json), [cumulative surface report](cumulative_surface_report.json), [cumulative image metrics](cumulative_image_comparison.json) and [edge audit summary](edge_audit_summary.json). Raw images, full edge records and logs remain under ignored `Logs/Legger20261015/Topology04/`, including `Cumulative/` reviews.

Source, input, output and earlier-version hashes are verified at closeout. Blender sources remain local-only under the [publication policy](../../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). No existing GUI session, Unity asset, new bake, UV unwrap, rig, collision, LOD or runtime system changes. World identity, streaming/unload/reload, generated-object identity, authored placement constraints and persisted runtime deltas remain inapplicable to this local mesh operation because no actor, scene, placement or save-system work occurs. Only this receipt and small measurements are committed; no push occurs.

Immediate four-view silhouette overlap is at least 99.99421%; cumulative overlap against the original is at least 99.98156%. Visual review assesses large-detail and silhouette continuity at these views; it does not establish pixel-identical shading or detail preservation at every viewpoint.

Review of the textured four-view sheet and neutral side view found no conspicuous silhouette or large-detail regression at those views. Existing source irregularities remain; the changed defect area above is explicitly retained for later cleanup.
