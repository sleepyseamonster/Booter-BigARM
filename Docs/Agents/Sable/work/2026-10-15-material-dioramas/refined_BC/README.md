# Refined B–C workyard

October 15, 2026. Sable. The author requested B's ground with rocks and props balanced between B and C, retaining realism and introducing C's broader painted accents. This is a new combined candidate for review, not final art-direction approval.

The existing sealed workyard supplies geometry, composition, fixtures and cameras. G01/G02/G03, including faded deposits, use the exact saved B materials without edits. New E rock/prop materials retain B's roughness, metalness, relief, oxide coverage and coatings, with C's saved pigment graphs mixed more strongly onto exposed planes. This makes the balance selective rather than washing every surface with one painted filter. No new image generation or manually painted texture claim applies.

The six completed captures are sunset close/gameplay, twilight close, neutral gameplay, and neutral metal/stone plates. Their camera poses and lighting match the earlier controlled study. The native source is saved at sunset close view. This is an offline Blender study, with no production import or runtime behavior. Deterministic world identity, streaming/unload, generated-object identity, authored terrain constraints and persisted deltas have no runtime interaction here: the existing authored diorama is rendered offline and production terrain is untouched.

Source: `SourceArt/Blender/Studies/Sable/MaterialDioramas20261015/Refined_BC.blend` (local only). Raw images remain in the existing local-only output folder's `refined_BC/` subfolder. Review artifacts, capture receipt and scripts are saved here. The prior A–D study remains unchanged.

## Reproduce

From the checkout root, open the sealed `MaterialDioramas.blend` with Blender in background mode and run `render_refined.py -- <checkout-root>` using `--python-exit-code 1`. The script clones saved C color ancestors into node groups, retains the B response graphs, packs and saves the new native source, renders the six fixed views, checks unchanged workyard vertices/transforms and verifies the original native source hash. Use the installed Blender 5.2.2 LTS and Cycles OptiX. This pass uses 64 samples with the same AgX look and exposure as the earlier study.

Run `verify_refined.py -- <checkout-root>` in background Blender with `--python-exit-code 1`, then run Python `package_refined.py <checkout-root>` with Pillow. Verification reopens both files and measures a new render from the saved source. Packaging only adds captions, creates contact boards and compresses review JPEGs. It does not recolor or repaint the render.

## Review

Open [the gallery](review.html), [the combined close view](yard_E_sunset_close.jpg), or the [B / combined / C board](comparison_close.jpg). The combined candidate is labelled E and occupies the middle of both comparison boards.

All six captures and both comparison boards were inspected. The combined basalt gains larger, more deliberate pigment breaks than B while retaining softer transitions than C. Machinery keeps its weight, clean reflection shapes and readable edges; painted variation is strongest across upper covers and exposed planes. At gameplay distance the change is deliberately restrained. B's quiet ground continues to separate the objects from their surroundings. Twilight preserves the main silhouette but reduces the visibility of the painted strokes; neutral plates make the surface differences easier to assess.

This is a useful combined direction for the author's judgment. Granite remains too evenly speckled, volcanic pores too uniformly scattered, and rusty boxes still tend toward a chipped-paint reading. The original simplified kit and repeated rock shapes remain; this pass refines treatment rather than rebuilding the assets. No final production-quality or Unity equivalence claim follows from these renders.

## Evidence

[Capture receipt](receipt.json) records six raw image hashes, camera matrices, shader controls and original/native/script hashes. [Verification](verification.json) confirms unchanged workyard geometry/transforms; five exact B ground/deposit graphs; unchanged B metallic, roughness, normal, coating and anisotropy inputs on all twelve other surface families; twelve retained C pigment groups; packed image dependencies; and correct saved workyard material assignments.

Reopening the saved E source reproduces its close view within **one 8-bit channel value**, with mean absolute RGB errors below 0.000073/255. Packaging checked all six raw hashes and dimensions and retained eight review-asset hashes. The background browser gallery loaded all seven displayed images successfully; the additional E gameplay image is retained separately and appears in the gameplay comparison. No gameplay tests were run.

Earlier B/C comparison captures use 40 samples; E uses 64. Geometry, camera, light, exposure and color management match, but sampling is not identical. The E source and raw outputs are ignored by the established local-only rules. Existing A–D sources and captures, production content and unrelated dirty files remain unchanged.
