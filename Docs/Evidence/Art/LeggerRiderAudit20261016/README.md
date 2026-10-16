# Legger original-led rider-area audit

October 16, 2026. Read-only source-art analysis. The author confirmed that the original Legger has good rider geometry; that is the fit authority. The [audit and revision brief](../../../Design/Art/LEGGER_RIDER_FIT_AUDIT.md) explains the coupled saddle, thigh-channel, control and footrest differences. No modeling, texture, UV, scale, rig or Unity changes were made.

## Source identity

`SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend` remains **745,433,568 bytes**, SHA-256 **`788e4de73bac5a52b9853e0071b1df23b9433edc296b981468e2d991c2cd7f93`**, matching the [accepted chassis checkpoint](../LeggerChassis20261016/README.md). The original is retained `Legger_DetailBake_Source`. [Inventory](inventory.json) binds the measured saddle, controls and footrest objects. [Extraction verification](extraction_verification.json) and [final verification](verification.json) record whole-file preservation, completed Blender workers and the unchanged prototype FBX.

The active source retains 75 major meshes and the prior checkpoint's 42,552 quad control faces / 136,928 evaluated triangles. Those counts are carried forward by unchanged file identity; this audit is not another topology inspection or reduction pass. Original height is about 2.22775 m; rebuilt height remains about 2.22441 m.

## Measurements and interpretation

- [Measurements](measurements.json) bind selected seat contact bands, centerline slope, transverse crown, rear top-envelope crest, grip offsets, knee envelopes, foot rail samples and reach sensitivity summaries.
- [Contour profiles](contour_profiles.json) preserve the plotted main pocket centerlines, Y -0.10 m cross sections and rear top envelopes. Original fused surfaces require the accompanying visual interpretation; a raised tank or rear handle is not all cushion.
- [Grip estimates](grip_estimates.json) retain the original 9 mm bin medians and linear fits at |X| 0.356 m. About 10 mm interpretation uncertainty applies. The rebuild moves hand contacts roughly 17–18 cm forward and 8 cm down; source asymmetry is not a symmetric rig target.
- [Prototype dimensions](prototype_dimensions.json) record the local imported 1.770 m humanoid and joint heads. These dimensions are a cross-check, not final character placement or a replacement fit authority.
- [Pose analysis](pose_analysis.json) contains the fixed proportional proxy, explicit assumptions, sampled surface approaches and all 54 reach-only cases. Fixed torso lean is 15° with common pelvis position. The comparison gives about 99° versus 146° at the right elbow. No ideal joint-angle, comfort, signed penetration, wrist, deformation or motion certification follows from it.

The original outer foot loop is around |X| 0.33 m / Z 1.51 m. Current pad centers are |X| 0.25 m / Z 1.4765 m, with an outer guard near |X| 0.29 m / Z 1.519 m. These are different support shapes. Choose the boot-bearing surface before prescribing a height change.

## Visual evidence

[View calibration](view_calibration.json) records **18 native geometry/material views and eight temporary proxy/foot-contact-cut views**. Same cameras and scale are used for paired comparisons. The foot cut clips geometry above Z 1.56 m for inspection; it does not reveal native holes. All 26 captures were reviewed through contact sheets, with selected detailed views and all three final boards inspected.

[Image manifest](image_manifest.json) binds **31 local images**: 26 captures, three boards and two contact sheets. The cache is `C:/Users/kbkno/.codex/art-cache/LeggerRiderAudit20261016/`:

- `01_rider_area_comparison.png` — original/current seat, thigh region and controls.
- `02_contours_and_reach.png` — measured pocket and transverse sections, contact layout.
- `03_proportional_rider_check.png` — identical torso comparison and different foot support shapes.
- `native_review_sheet.jpg` and `pose_review_sheet.jpg` — all paired captures.

The proxy is an analytical set of joints/capsules, not a finished seated rider mesh. Seat compression, clothing, joint limits, wrist roll, lever reach, instrument visibility, mounting/dismounting and steering sweep remain unverified. Surface proximity subtracts assumed radii from nearest-surface distances; negative values are warnings for later inspection, not certified signed collision depths. This analysis does not contradict the author's acceptance of the original rider layout.

## Reproducibility and scope

[Local recipe manifest](local_recipe_manifest.json) binds extraction, captures, proportional analysis, reduction of measurements, board recipes and their local raw data. Recipes are retained under `Logs/LeggerRiderAudit20261016/`; point subsets and PNGs remain in the art cache. No native save was called. Hidden Blender 5.2.2 workers completed normally. Logged messages are future node-API deprecations and unsupported FBX short user-property metadata; they do not indicate failed geometry import or captures. The corrected proxy torso display was rerendered before review.

The read-only comparison, source preservation, image decoding/identities, JSON structure, documentation links and exact Git scope are the relevant checks. No gameplay smoke test, Unity import, runtime fit, performance or hands-on animation claim is made. The user's foreground app and open Blender scene were not changed. Native files, images and source subsets remain local under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). Only this receipt, its JSON evidence and the audit brief belong to the documentation commit.
