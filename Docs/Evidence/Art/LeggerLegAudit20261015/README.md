# Legger original leg audit evidence

October 15, 2026. The author requested an audit of original leg geometry, height/width ratios and angles before any rework. **No `.blend` was saved or modified.** Only new audit artifacts and this documentation were written. Existing model plans and prior receipts were not edited. The [leg geometry audit and future brief](../../../Design/Art/LEGGER_LEG_GEOMETRY_AUDIT.md) report the conclusions and measurement limits.

The inspected source remains `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, **810,807,494 bytes**, SHA-256 **`fcb639d849c25866508a05b9546175140f44fadaa79a1ff177bc499dc08d1b1f`** before and after inspection. [Verification](verification.json) seals this identity and records the read-only boundary. This is the [applied sci-fi checkpoint](../LeggerSciFi20261015/README.md), with one scene/view layer, 67 visible major meshes, 36,302 stored quads and 123,664 evaluated triangles. Counts are not an additional modeling change.

## Inspection

[Inventory](inventory.json) records evaluated world-space bounds and object identities for the dense original, two earlier split leg references, both current leg modules and the current full assembly. The dense original has 1,331,630 vertices and 2,667,021 evaluated triangles. It is authoritative for the upper leg because the earlier split leg objects stop near Z=1.35 m. Their cut edges and floating boundary remnants are historical segmentation artifacts, not new damage or complete mechanical boundaries.

Blender 5.2.2 LTS ran with embedded auto-execution disabled in hidden background processes. Visibility, camera, lighting and clipping overrides lived only in process memory. The user GUI was not focused, reloaded, closed or sent keystrokes. No source meshes, object transforms, image payloads or native defaults were saved. All task-owned Blender processes exited normally.

Nineteen fresh raw views include full dense side/front/opposite, split-reference side/front/oblique, current side/front, four dense half-body side/socket views and three foot plans. [Half-view calibration](half_views.json) describes the camera far planes at X=0; [foot-view calibration](foot_views.json) describes the camera cut at Z=0.18 m. The original geometry itself was not sliced. One side is mirrored only in the board presentation. Socket, opposite-side, oblique, front and plan views were visually inspected along with the two comparison boards.

## Measurements and proof limits

[Measurements](measurements.json) provide analyst-estimated P/K/H/A side-plane landmarks, link lengths and ratios, signed side slopes, joint deflections, coordinate-box sensitivities, front-envelope slopes, foot envelopes and actual triangle-plane sections. P/K are partly obscured and estimated to ±30/25 mm per Y/Z coordinate; H/A are estimated visible cap centers to about ±10 mm. The nominal R side slopes are approximately −29°, +19°, −33°; current slopes are +6°, −5°. These are silhouette estimates, not recovered bones. Worst-corner sensitivity is explicitly recorded; the upper angle is much less certain than the lower angle.

Cross sections intersect actual evaluated triangle edges, avoiding a false width result from sparse longitudinal control vertices. Corridor/body-center boundary flags identify contaminated samples. Clear original R middle and lower midpoint widths are approximately 0.134/0.099 m across X; current upper/lower midpoints are both approximately 0.198 m. These are assembly-envelope dimensions including nearby components, not isolated armor or wall thickness. Measurements do not establish every intersection, articulation range, functional linkage or mechanical clearance.

The dense original R foot envelope is approximately 0.687 × 0.295 m in Y/X, versus current 0.525 × 0.188 m. A separate geometry band below 35 mm confirms broadly the same extents, without certifying a support/contact polygon. [Foot plan axes](foot_plan_axes.json) measure alpha-silhouette PCA orientation; they do not infer toe-joint axes. The generated original is asymmetric and unrigged, so its two posed paths are not a proven equal-length rest skeleton.

## Local visual evidence and preservation

[Image manifest](image_manifest.json) identifies raw renders, analytical overlays and the two review boards under ignored `Logs/LeggerLegAudit20261015/`:

- `Legger_Leg_Geometry_Audit.png`: calibrated original R/L and current side silhouettes, estimated landmarks, link lengths and slopes. All cards share metres per pixel.
- `Legger_Leg_Widths_and_Feet.png`: identical front crops plus foot plans at the same scale, with dimensions and camera-cut disclosure.

Local NumPy point/triangle caches and [recipe identities](local_recipe_manifest.json) preserve the analysis path. No native source or recovered checkpoint was duplicated, replaced or deleted by this audit. Source files, data caches and raw PNGs remain local under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md); only the bounded text audit and evidence records are committed. Unrelated live repository work remains outside this task. No Unity scene, asset, package, rig or gameplay test changed or ran. Future modeling remains unimplemented.
