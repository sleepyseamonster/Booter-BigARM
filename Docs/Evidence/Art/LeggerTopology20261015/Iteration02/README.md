# Legger topology iteration 02

October 15, 2026. The author visually accepted iteration 01 and requested another 10% reduction while preserving detail and silhouette. [Iteration 01](../README.md) remains the prior approved source; its Blender file is preserved.

- Input: local-only `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration01.blend`.
- New version: local-only `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration02.blend`.
- Full-density comparison reference: local-only `SourceArt/Blender/Studies/Legger/Legger.blend`.
- Tool: [conservative topology iteration](../../../../../Tools/Art/Blender/iterate_legger_topology.py), ratio 0.90, using Blender 5.2.2 LTS in hidden background mode with embedded auto-execution disabled and Python failures mapped to exit code 1.
- [Image comparison tool](../../../../../Tools/Art/Blender/compare_legger_topology.py) now accepts explicit before/after labels so review sheets identify the correct versions.

The same 3 degree face-angle and 40 degree shape-angle limits are used for UV/seam/sharp/color/material-aware quad pairing. World transforms and the UV layer are retained. This continues a mixed triangle-and-quad review mesh, not complete animation retopology or a Unity integration.

Immediate comparisons use identical view framing before and after reduction. A separate native Blender reopen compares iteration 02 directly against the full-density source with bidirectional world-space nearest-surface samples and fixed original-source framing. Cumulative comparisons guard against hiding losses from earlier iterations. Samples and four orthographic views do not prove zero loss or bound all possible errors.

Raw renders and logs remain under ignored `Logs/Legger20261015/Topology02/`, with cumulative evidence in its `Cumulative/` subdirectory. Blender sources stay local-only under the [source-publication policy](../../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). No Unity asset, runtime identity, world generation, streaming, authored placement constraint or persisted runtime delta is changed. The five integration concerns remain inapplicable for the reasons recorded in iteration 01. No existing Blender GUI session is reloaded or modified.

## Verified result

| Measure | Approved iteration 01 | Iteration 02 |
| --- | ---: | ---: |
| Vertices | 1,827,353 | 1,644,429 |
| Triangles | 3,658,470 | 3,292,620 |
| Faces | 3,441,297 | 3,109,089 |
| Quads | 217,173 | 183,531 |
| Boundary edges | 47 | 49 |
| Non-manifold edges (including boundary) | 127 | 128 |
| Loose vertices | 0 | 0 |

The reduction is 10.00008% from the approved version and 19.00010% from the full-detail source. The world matrix is unchanged, retaining the author's scale and orientation. One material, one UV layer and all three packed source textures remain present. The newly saved file reopened in a separate native Blender process for cumulative validation.

See [iteration report](report.json), [immediate image metrics](image_comparison.json), [full-source surface comparison](cumulative_surface_report.json) and [full-source image comparison](cumulative_image_comparison.json). Immediate silhouette overlap is at least 99.9945%; cumulative overlap against the original is at least 99.9906% across the four 1024-square views. The largest sampled cumulative full-source-to-candidate distance is approximately 0.5514 mm; the reverse maximum is approximately 0.0939 mm. Visual review found no conspicuous large-detail or silhouette regression in these views. These measurements do not guarantee lossless geometry or pixel-identical shading.

The [edge audit summary](edge_audit_summary.json) records the changed defect area: two extra boundary edges approximately 0.879 and 0.908 mm long, next to existing three/four-face non-manifold source junctions near a foot at world height approximately 0.14 m. All changed boundary endpoints are within 0.88 mm of prior defect vertices. Total boundary length increases by approximately 1.787 mm. This is retained and disclosed as a small change at an existing source defect; the candidate is not certified watertight or ready for final animation/physics integration. No speculative hole fill, welding or geometry reconstruction was applied to hide this condition.

The full-density source and approved iteration 01 remain byte-for-byte unchanged, verified against their recorded SHA-256 hashes. All work remains local; no push or publication was performed.
