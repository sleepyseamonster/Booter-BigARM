# Camp Loop effects and processing plan audit

October 14, 2026. Audited the [overhaul plan](CAMP_LOOP_EFFECTS_PROCESSING_OVERHAUL_PLAN_2026-10-14.md) against the installed camp binding, serialized camera/pipeline, pinned URP 17.4.0 source and background Unity workflow. Revision 2 incorporates the corrections below. Audit scope is documentation and read-only inspection.

## Verdict

The design direction is sound. The original plan needed a feasibility gate before visual tuning and more precise contracts for exposure, solar visibility, blur modes, texture data and profiling. Revision 2 is ready for a bounded implementation request, starting with feasibility proof. The final native/custom pipeline remains a measured comparison decision.

## Findings and corrections

| Priority | Gap | Evidence and correction in revision 2 |
| --- | --- | --- |
| High | Preferred native bloom and late lens composition lacked a concrete compatible sequence | Pinned `Runtime/PostProcess.cs:208` records DOF, `:255` bloom, `:284` data-driven flare and `:303` UberPost. Added native flare comparison and explicit resource-order receipt. No private package internals are treated as a supported extension API. |
| High | Camera-specific volumes were proposed without lifecycle/API proof | `UniversalAdditionalCameraData.cs:256` guards stack updates before VolumeManager initialization; `:745` exposes stack ownership/caching. Added initialization, restoration and borrowed-stack rules, same-scene camera and additive-scene fixtures. New global layer allocation is not silently authorized. |
| High | One visibility signal could be interpreted as gating the sky sun | Offscreen artifacts and the sky are different paths. Added artifact-only gating, normal sky occlusion, reversed-Z/depth-background/behind-camera cases, angular disc size and transparent limitations. Render-only landscape sectors have no colliders, so a physics-only offscreen visibility fallback would miss them. |
| Medium | Exposure was underspecified relative to bloom threshold | `UberPost.shader:216` adds dirt × bloom; `:245` applies color grading/post exposure. `UberPostProcessPass.cs:287` converts EV to a multiplier. Defined postExposure as the initial display control and kept scene illumination/threshold inputs separate. |
| Medium | Focus controls implied the same settings across Gaussian and Bokeh | Native modes differ: Gaussian has far-start/end; Bokeh uses focus distance and lens parameters. Added mappings, broad-region limitations and proxy-anchor fixtures that avoid edits to the actively owned rig/motor. |
| Medium | Grayscale masks were described using transparency without defining data channels | Native dirt samples RGB. Added zero/positive contribution semantics, explicit encoding conversion, sRGB-off data imports, inspected generation sizes and derivative receipts. |
| Medium | Standalone fixture could accidentally build production or invalidate GPU timing | Production's only enabled build scene is Greater Wasteland. Added explicit isolated harness scene list, background graphics launch, fixed matched conditions, full-frame paired cost measurement and memory/allocation checks. |
| Medium | Completion allowed unresolved performance despite a budget gate | Added separate candidate-handoff and performance-qualified outcomes. Keep Clearer twilight active pending selection; a passing structural test does not select an art direction. |
| Low | AA findings only covered the camera | Camera AA None and pipeline MSAA 1 are both serialized. Added HDR/MSAA/depth/runtime override records and controlled AA comparisons. |

## Retained decisions

Keep the clearer-twilight art, one sun authority, native-height landscape context and existing camera movement/clearance contracts. Preserve per-camera optics selection and active-scene lighting restoration. Use one bloom producer and one world-blur mode, then compare softer illuminated lens dust. Resource generation permission remains sufficient for bounded local assets. Package pins and production routing remain unchanged.

The proposed 1/2/3.5 ms incremental GPU targets are provisional engineering goals, not hardware proof. Full physical lenses, automatic exposure, dynamic clouds and persistent grime remain separate decisions. A failed feasibility path should lead to a supported narrower alternative, not a package fork or a global renderer mutation.

## Verification and remaining implementation evidence

Documentation verification covers relative links, source locators, six-stage continuity, revised completion gates, scoped diff and whitespace. No scene, shader, runtime, resource image or project setting was changed. No Unity launch or gameplay smoke test was needed for this audit.

Implementation must still prove native/custom ordering in captured RenderGraph execution, supported per-camera volume isolation, utility-camera exclusion, render-target restoration, occlusion and foreground fixtures, background harness execution and target GPU measurements. Author appearance/feel selection remains distinct from those checks.
