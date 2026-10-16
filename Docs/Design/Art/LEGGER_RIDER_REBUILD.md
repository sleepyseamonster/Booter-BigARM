# Legger original-led rider rebuild

October 16, 2026. Implementation of the author's request to recreate the original rider angles, build the saddle into the frame/chassis, and correct the handlebar and front end. The [rider audit](LEGGER_RIDER_FIT_AUDIT.md) controls the design: **the original Legger has good rider geometry**. This pass restores that layout in the same assembled native scene with separate major editable parts. Final creative acceptance remains with the author.

The [saved-source receipt](../../Evidence/Art/LeggerRiderRebuild20261016/README.md) binds the applied file, recovery copy, geometry/material checks and visual evidence. `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend` remains the sole active source; native files and render pixels stay local.

## Modeled result

- **Saddle:** replace the long shallow bench with a compact crowned pocket, rolled edges and early rear rise. Its matching pan and lower rails follow the recovered profile. Ribbing, stitching and small surface variation are baked rather than modeled as individual fittings.
- **Rider waist:** lower and tuck the upper body shrouds beneath the descending thighs. The forward transition is a separate shaped cowling, rather than extending the cushion into the front body. The front cheeks have a firmer outline and shallower crown, with a connected brow/bridge over the central nose.
- **Controls:** return the hand centers to X ±0.356, Y approximately -0.270, Z 2.183 m; sweep the grips back about 30°. The handlebar rises through a broad bridge with stable cross-section orientation. Refit the stem, clamps, levers and angled gauge to that layout.
- **Frame/chassis:** retain the accepted lower cast diagonal construction and leg bearings, while reshaping the upper rails, seat subframe, steering support and front hose route around the new contacts. The steering assembly root has a bearing-aligned local Z guide; this is not an animated steering rig or a collision certificate.
- **Feet:** replace the inboard broad pads with outboard open loops and narrow tread, supported back into the chassis. Tread reference is |X| 0.333, Y -0.025, Z 1.5125 m. Its fore/aft contact is 20 mm forward of the interpreted original rail center to improve shin space beside the retained cases. Source rail and rebuilt tread are different contact shapes.

Twenty prior major meshes are rebuilt and one front cowling is added, giving **76 active major meshes, 45,762 quad control faces and 146,264 evaluated triangles**. This adds 9,336 evaluated triangles over the chassis checkpoint. Controls are all quads; modifiers/rendering produce triangles. Spacing follows the designed curvature rather than forcing equal-sized squares. This remains a detailed source iteration; Unity cost, LODs and final runtime texture consolidation are unmeasured.

All thirty accepted aligned three-section leg meshes and their hip/knee/hock/ankle targets remain exact, along with the twelve cargo meshes and five drive meshes. Those 47 active meshes retain geometry, UV, material-assignment and world fingerprints. Eight additional retained Frame/BodyArmor parts receive the revised atlas while their prior exact versions remain hidden. The saved manual scale, ground contacts, one-scene organization and eleven module roots are retained. Small height/bounds changes from reshaped controls are form changes, not global rescaling.

## Comparison with the original

[Fit evidence](../../Evidence/Art/LeggerRiderRebuild20261016/rider_fit.json) uses Blender metres, with forward -Y and Z up. The main pocket centerline, sampled from Y -0.18 to +0.14 m, is approximately **2.75 mm RMS** from the original envelope; the prior bench was about **45.27 mm RMS** away. This is a descriptive ray comparison to fused geometry, not exact recovery of separate upholstery or a tolerance requirement. The source's small irregularities are deliberately interpreted into clean symmetric forms.

The transverse section regains a crown instead of a center trough. The early rear slope follows the original's roughly 22° mean rise over Y -0.06 to +0.14 m. The restored grip locations recover the compact reach: under the same temporary 1.770 m proportional proxy, pelvis and 15° torso lean, the right elbow is about **98°**, compared with about **99° on the original and 146° before this pass**. These are comparative joint calculations, not universal ergonomic targets.

Selected thigh/waist envelopes are brought inward toward the original. The temporary shin capsules have about 18 mm nearest-surface gap beside the retained cargo in the fixed comparison. The assumed thigh radius approaches the upper shroud by roughly 2 mm; source soft/hard surfaces are not segmented and this is not a certified signed penetration depth. Final posed rider mesh, clothing, seat compression, wrist/lever reach, gauge visibility and posture range still require confirmation. Steering sweep and mounting/dismounting are not certified by these static renders.

## Surfaces, retention and handoff

Frame and BodyArmor receive 4096² atlases; Saddle and Steering receive 2048² atlases. Each has packed sRGB base color and Non-Color tangent +Y normal, roughness, metallic and isolated-module AO. AO remains auxiliary rather than multiplied lighting in albedo. Twenty new maps plus 35 retained active maps keep 55 active maps across eleven sets. The ivory, dark metal, brown leather and restrained wear continue from the approved palette.

Every visible part retains only its final atlas as UV0. Hidden authored copies retain the procedural material coordinates and prior surface resources. All 850 pre-pass mesh objects and all 146 pre-pass packed image payloads are preserved, including the dense original. Superseded shapes are hidden references rather than extra visible layers. No reference deletion or source publication occurs.

Native geometry checks cover closed, nondegenerate quad controls, UVs, module independence, mirrored new pairs, original joints, packed-image identities and an independent saved-file reopen. Raster checks sample atlas interiors, excluding recorded tiny/eroded faces; they do not certify every seam or mip. Equal-camera geometry/PBR comparisons and a native matched render check the saved appearance. The evidence receipt records the exact limits and results.

No Unity asset, `.meta`, scene, gameplay code, package, export, collider, rig or animation was changed. The existing Unity mounted placeholder remains a separate integration lane. Deterministic world identity, streaming/unload/reload, stable generated-object identity and persisted runtime deltas are inapplicable because no runtime world object or save system changes. Authored constraints do apply: original rider layout, saved scale, accepted leg alignment, separate major parts and current palette. Files follow the [local-only source policy](../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).
