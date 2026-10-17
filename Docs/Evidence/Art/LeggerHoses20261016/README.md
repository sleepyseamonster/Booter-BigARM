# Legger original-led hoses and tubing

October 16, 2026. The author requested analysis and recreation of the original hoses/tubing. This pass follows the [top-down mounting correction](../LeggerPlanform20261016/README.md) in the same assembled native scene. The dense retained `Legger_DetailBake_Source` is the visual authority; its fused geometry does not identify every concealed junction or engineering function. Route names are modeling labels, not new game-system canon.

## Saved native and scope

Source: `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, **796,299,694 bytes**, SHA-256 **`49a8a0e4322fc388ea2cfbb3d3ede9b94323109aa41e1573020d0fd5f88a6a17`**. [Application](application.json) records source/recovery guards, same-directory staging, fsync and atomic replacement. The preceding native is retained independently at `C:/Users/kbkno/.codex/art-cache/LeggerHoses20261016/Before.blend`, SHA-256 `f31f4beae80173df81f6d9da649049de66cc967f47382a8bec5bf75fb919eb87`. The first visual candidate is separately retained as `Checkpoint01.blend`. All native binaries and review pixels remain local under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

The model still has **76 visible major mesh objects, 45,762 control quads and 146,264 evaluated triangles**, with one complete scene/view layer and eleven independent module roots. No active vertices, faces or mesh objects were added. Five meshes change: the service harness and the two middle/lower leg-frame meshes per side. Only registered hose/coupler/socket vertices change; structural vertices in the four leg frames stay exact. Seventy-one active mesh fingerprints remain unchanged, including armor, actuators, joints, cargo, frame, saddle and controls. All active transforms, accepted joint targets, scale and ground contacts stay unchanged.

## Audit and recreated routes

The original side/underside views show broader, deeper underbody loops with different gauges. The preceding rebuild tucked its hoses too tightly into the belly. Original control leads follow the handlebar into the steering neck; the previous front pair instead dropped toward the drive area. The leg close-up shows bounded service bows beside actuator channels rather than another large dangling bundle.

| Recreated runs | Shape and placement | Diameter / retained sections |
| --- | --- | --- |
| Two outer underbody loops | Broad sweep beneath the cases, lower sag and an inboard return to the rear frame. Existing drive-end and frame-end fittings move with the run. | 23 mm; 32 ten-sided rings each |
| Two inner service loops | Lighter nested arc, separated laterally and vertically from the outer loop. | 15 mm; 35 ten-sided rings each |
| Two thin return loops | A distinctly thinner, deeper hanging bend; keep the original gauge hierarchy without adding small wires everywhere. | 9 mm; 34 ten-sided rings each |
| Two steering control leads | Follow the bar underside from the control housing, then turn down into the neck/cowling region. Grip/bar geometry stays fixed. | 9 mm; 27 ten-sided rings each |
| Four actuator service hoses | Rework only the existing seven-ring tube component beside each middle/lower actuator, from the existing inboard fitting toward the barrel. | 9 mm; seven eight-sided rings each |

[Construction](construction.json) records all stations, native ring centerlines, radii, edited vertex IDs and terminal regions. Twelve named `Hose_` vertex groups select each run; harness groups include the existing end fittings. The hoses remain grouped with their major service or leg assembly, preserving the author's preference against a separate object for every small fitting.

First-checkpoint analysis caught the rear return rising into case space and angularity in the thin low bend. The second build tucks the rising sections inboard of the unchanged case bodies. The final refinement redistributes existing rings toward bends using arc length plus curvature weighting; topology and UVs stay fixed. Existing socket barrels bridge the underbody endpoints to evaluated drive-pod/frame surfaces with a recorded 4 mm inset. Leg starts remain in their existing fitting region; leg ends inset 1.5 mm into the evaluated actuator barrel. Concealed routing is an authored reconstruction rather than an exact segmentation of the generated source.

## Verification and preservation

[Native QA](native_qa.json) and [canonical inspection](native_canonical.json) passed after independently reopening the final source. Controls remain all-quads, closed and nondegenerate, without detected boundary/nonmanifold edges or loose vertices. Connectivity, UV coordinates, modifier parameters and materials remain unchanged. All twelve editing groups are present. Route centerlines are mirrored within three micrometres; the leg-frame geometry is exactly mirrored in this check. The whole harness has a maximum mirrored nearest-vertex difference of approximately 3.51 mm. Centerline symmetry does not imply matching circumferential vertex phase or exact symmetry of every terminal fitting.

[Route spacing](route_spacing.json) samples native piecewise-linear centerlines at at most 2 mm intervals: the minimum nominal-radius-subtracted inter-route gap is **9.95 mm**. [Cargo clearance](cargo_clearance.json) checks those samples against the four evaluated closed case bodies and finds a minimum nominal-radius-subtracted gap of **22.57 mm**. These are static auxiliary checks. They do not certify signed solid collision for every collar/strap, hose flex, moving legs or steering, final dressed-rider clearance, pressure-system design or structural loads.

[Preservation](source_preservation.json) accounts for **all 994 preceding mesh versions** and **all 191 packed image payloads** by the recorded fingerprints/bytes. Five hidden pre-pass mesh copies bring the native total to 999 mesh objects; the visible budget remains 76. All **55 active packed maps across eleven sets** remain byte-identical. Existing rubber/metal grain and wear follow the reshaped surfaces. No new corrugation geometry, texture maps or atlas rebake is claimed; bend redistribution and length changes alter texel density locally.

[Views](views.json) bind **34 renders**: sixteen original/before audit views, eight first-checkpoint views, eight final-candidate views and two reopened-native views. All audit/checkpoint/final captures were inspected in labeled sheets, with selected source side/underside/cockpit/leg and candidate side/engine details at full size. The reopened native assembly and final comparison board were also inspected. The isolated routing view can isolate the rebuilt service harness; the original is fused and remains shown in equivalent full-source framing.

[Matched render comparison](render_comparison.json) checks native versus candidate side and assembly views. [Image identities](image_manifest.json) bind **38 local images**, including `Legger_Hose_Comparison.png` and the three review sheets. Cache: `C:/Users/kbkno/.codex/art-cache/LeggerHoses20261016/`. [Visual review](visual_review.json), [local recipes and measured inputs](local_recipe_manifest.json) and [verification](verification.json) retain provenance.

Work used hidden Blender 5.2.2 LTS workers with auto-execution disabled. No foreground activation or GUI reload/save occurred. Reopen the current source before saving from an older loaded Blender session. No Unity asset, `.meta`, package, export, rig, collider, gameplay smoke test or import changed. World identity, streaming/unload/reload, generated runtime identity and persisted deltas are inapplicable to this source-art-only task; original-led design, budget, scale, pose and rider constraints apply.
