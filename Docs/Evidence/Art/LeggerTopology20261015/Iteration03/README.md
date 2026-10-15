# Legger topology iteration 03

October 15, 2026. Continues the author's incremental 10% reduction workflow after [iteration 02](../Iteration02/README.md). The input matches iteration 02's previously verified SHA-256 hash. All earlier source files remain preserved.

- Input: local-only `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration02.blend`.
- Candidate: local-only `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration03.blend`.
- Full-detail reference: local-only `SourceArt/Blender/Studies/Legger/Legger.blend`.
- Method: the [conservative topology tool](../../../../../Tools/Art/Blender/iterate_legger_topology.py), 0.90 Collapse Decimate followed by 3 degree face / 40 degree shape quad pairing, respecting seams, sharp edges, UVs, colors and materials.
- Runtime: Blender 5.2.2 LTS in hidden background processes with embedded auto-execution disabled and Python exceptions mapped to exit code 1.

Immediate four-view textured/neutral reviews compare against iteration 02 using identical framing. A separate native reopen compares the candidate to the original full-density source using world-space bidirectional surface samples and original-source camera framing. The defect-edge audit compares iteration 02 to iteration 03. Sampling and four 1024-square images cannot prove that every detail is unchanged or provide a global error bound.

Raw local evidence is under ignored `Logs/Legger20261015/Topology03/`, with cumulative reviews in `Cumulative/`. The candidate remains a mixed triangle-and-quad mesh, without new UVs, baking, articulation, rig, collision, LODs or Unity import. Blender files remain excluded from Git/Git LFS under the [local-only source policy](../../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). Existing GUI sessions remain untouched.

World identity, terrain streaming/unload/reload, generated-object identity, authored placement constraints and persisted runtime deltas remain inapplicable because this is a local mesh-only change with no Unity actor, placement, generation or save-system edits. The original source and earlier candidate hashes are checked again at closeout.

## Verified result

| Measure | Iteration 02 | Iteration 03 |
| --- | ---: | ---: |
| Vertices | 1,644,429 | 1,479,798 |
| Triangles | 3,292,620 | 2,963,358 |
| Faces | 3,109,089 | 2,805,471 |
| Quads | 183,531 | 157,887 |
| Boundary edges | 49 | 49 |
| Non-manifold edges (including boundary) | 128 | 128 |
| Loose vertices | 0 | 0 |

The reduction is exactly 10% from iteration 02, or 27.10009% from the original. The world matrix, UV layer, one material and three packed textures are preserved. The output reopened successfully in the separate native cumulative-validation process.

See [iteration report](report.json), [immediate image metrics](image_comparison.json), [cumulative surface report](cumulative_surface_report.json), [cumulative image metrics](cumulative_image_comparison.json) and [defect-edge audit](edge_audit_summary.json).

Four-view silhouette overlap is at least 99.9920% against iteration 02 and at least 99.98596% against the original. Full-source-to-candidate sampled deviation has a maximum of approximately 0.7687 mm and a 99th percentile of approximately 0.0750 mm. Reverse samples have a maximum of approximately 0.1222 mm. The four full-source textured comparisons average 1.26–2.11 RGB levels of difference on overlapping pixels on a 0–255 scale. Review of textured and neutral comparison images found no conspicuous silhouette or large-detail regression at these viewpoints; pixel-identical shading and lossless geometry are not established.

All 128 defect edges have exactly the same endpoints, boundary classification and incident-face counts as iteration 02. Boundary length remains 0.0697421393 m, with zero nearest-defect endpoint displacement. The known source and prior-pass foot defects therefore remain present without worsening under this audit; this is not a watertightness or game-ready certification.

The full-density source and all previous candidates are retained locally. Source, input and output hashes are verified at closeout. Only this receipt and its small JSON measurements are committed; Blender sources, raw images and diagnostics remain local. No push was performed.
