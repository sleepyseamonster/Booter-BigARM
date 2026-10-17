# Legger model refinement and rigging handoff

October 16, 2026. Implementation of the [audited modeling plan](LEGGER_MODEL_REFINEMENT_AND_RIG_READINESS_PLAN.md). The [saved-source receipt](../../Evidence/Art/LeggerModelRefinement20261016/README.md) binds the installed native, recovery copy and independent validation.

The direction remains layered ivory armor, compact dark machinery and brown leather. Geometry carries the broad planes, silhouettes and fitted mechanical interfaces. Small access covers, retaining marks, grain and finish variation live in the existing texture modules.

## Modeling changes

- Firm the front head, neck shoulders and fairing sections; flatten the broad faces and taper the three leg shells without moving their joint centers.
- Deepen the joint shoulders and recess their centers. Fit the foot ties into the central cradle and firm the runner sweep while keeping the footprint and ground-contact vertices.
- Give the actuator assemblies an inboard corridor, parent-carried first clevises, deeper blind bores and longer rod overlap. Existing authoring object origins stay fixed; physical clevis guides are recorded separately.
- Route control leads outside the gauge/front body. Leg service loops have slack and explicit sockets. The disposable motion study preserves centerline length; the control leads retain rigid endpoint sections at the sockets.
- Reuse the rear cooling return and drive core to form a compact central service housing. Cargo geometry, case arrangement, supports and rider contacts remain fixed.
- Lower the lever finger blades slightly to clear the existing grips through the sampled pull. Keep their geometry-derived hinges and all hand contact transforms.

No extra active objects or control faces are needed: 76 major meshes, 45,762 quad controls and 146,264 evaluated triangles. Eleven texture sets still provide 55 packed active maps at their existing resolutions. These are source preservation ceilings; runtime memory, LODs and renderer/skinning costs remain unmeasured.

## Motion ownership for the future rig

The eleven module roots are authoring/texture groups. They are not a production skeleton. In particular, `Legger_Part_FrontControlPod` belongs to the fixed body even though its texture module is Steering. The stem, handlebar, grips, levers and gauge rotate about the existing bearing guide.

Every active mesh island has a `MotionStudy_` guide group and recorded carrier. Existing groups remain intact. These groups identify rigid islands and flexible runs; they are not final deform weights. Joint housing, turning ring and guard islands can follow different carriers without becoming separate objects.

Use the `actuator_anchor_world_m` properties for the four barrels and four rods. Their preserved authoring origins predate the clearance correction and are not the physical clevis guides. Aim barrels and rods independently between the corresponding parent/child anchors without scaling them. Use the bore/stroke measurements in the receipt to set limits.

The leg chain uses the accepted Hip, Knee, Hock and Ankle targets. Its source study pitches about X and preserves segment lengths. A production rig still needs animator controls, a stable folding convention and agreed gameplay limits. Feet and cargo are rigid/fixed in this slice; cargo lid geometry remains separately editable for a later opening interaction.

Hose endpoint properties describe both carriers and the neutral centerline length. Control cables retain four endpoint rings at each rigid socket; solve the intervening slack without longitudinal stretch. The future rig must reproduce the demonstrated deformation, rather than treating the whole authoring mesh as one rigid object.

The native text block `Legger_ModelRefinement_RigHandoff.json` carries the geometry allowlist, steering guide, endpoint records, surface contract and pending integration checks. Export only the allowlisted active meshes; dense originals, prior versions and review aids remain hidden references.

## Source surface contract

The editable cages remain all quads. A final `Bake and export triangulation` modifier uses FIXED quad splits and CLIP ngons after the existing bevel and weighted-normal modifiers. Evaluate that same sequence for a derived export, retaining split normals and UV0. Native validation compares positions, indices and split normals to the bake records and checks evaluated UV coordinates within float precision.

Frame, Drive, both legs and Steering receive repaired atlases where the reshaping changes surface layout. Other control UVs remain exact. Revised PBR maps replace active assignments; preceding resources remain recoverable. Base color is sRGB, data maps are Non-Color, tangent normals use +Y, and AO is auxiliary rather than directional lighting in albedo. AO sources are isolated by rigid carrier or individual flexible/independently moving element.

These are source-qualified bakes. The later Unity Generic import must validate tangents, materials, units, axes, attachment retention and animation bounds before the asset is called game-ready. Do not silently change triangulation after baking.

## Limits and next dependency

The sampled study covers high step, forward/rear travel, planted-foot crouch/extension, one-sided leg travel, steering and lever pull. It is a modeling diagnostic, not an approved gait, continuous collision certificate or engineering load assessment. Named bearing/socket insertions are intentional assembled contacts.

The same proportional rider remains reachable at the sampled steering angles. Its existing approximately 2.5 mm thigh-radius approach near the cowling is unchanged; this nearest-surface measure is not signed penetration. Final clothed rider fit, wrists, seat compression and mounting/dismounting remain rigging checks.

Production skeleton, final flexible weights, clips, export, Unity installation and measured gameplay performance follow this source modeling slice. No Unity asset, metadata, scene, package, world service or gameplay code changes. Deterministic identity, streaming and persisted deltas are inapplicable because no runtime object is created.
