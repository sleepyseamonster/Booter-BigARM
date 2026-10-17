# Legger model refinement — saved-source receipt

October 16, 2026. The author authorized implementation of the [audited plan](../../../Design/Art/LEGGER_MODEL_REFINEMENT_AND_RIG_READINESS_PLAN.md). The complete source modeling slice is installed and independently reopened. Author creative acceptance and the later production rig/Unity work remain separate.

## Applied native

- Sole active file: [Legger_Topology_Iteration04.blend](../../../../SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend).
- After SHA-256: `87ba450b0f957e8f71924bb50ed73ae7aec1f20efa8b215cefdfffb121b78eea`; 897,922,001 bytes.
- Before SHA-256: `90757f7efa6811fbfd9c82c93b08a98d254f60d13d15e447e5eeab03f2db6304`; 799,090,806 bytes.
- Verified recovery: `D:\BooterBigArmPrivateAssets\Legger\ModelRefinement20261016\Before.blend`.
- One complete scene/view layer, eleven authoring module roots, 76 active meshes, 45,762 quad controls and 146,264 evaluated triangles. Eleven texture sets retain 55 active packed maps and their preceding resolutions.

The source and recovery copies remain local under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). The native was replaced atomically only after candidate validation and visual review. No foreground application was activated. An older loaded Blender session must reopen this file before a later author save.

## Result

Forty-one major meshes receive local form or fitting changes: firmer front/head planes and shoulders, tapered leg armor, stepped bearing faces, fitted foot ties, a clearer cast frame and a compact rear service housing. Actuators move into an inboard corridor, with parent-carried first clevises, deeper blind bores and more rod overlap. Control leads clear the gauge/body; leg hoses have explicit sockets and slack. Lever blades clear the unchanged grips through the sampled pull. Cargo, saddle/pan, footrests, grips, joint centers, manual scale and protected ground contacts are retained.

The palette remains ivory armor, dark machinery and brown leather. Native analytic finishes and sparse access details replace repeated surface motifs; existing cargo and saddle detail is retained. Frame, Drive, both legs and Steering receive repaired atlases. Self-baking these authored shaders needs no high/low projection cage. AO is isolated by motion ownership and remains auxiliary, rather than lighting in base color.

A final reopen caught three inverted mirrored cable/socket islands. Their winding was corrected without moving vertices or changing UV-to-vertex correspondence, and Frame was rebaked. The preceding five Frame maps remain recoverable as inactive resources. All 1,021 preceding mesh versions and all 198 preceding packed image payloads remain exact. Native totals are 1,097 mesh objects and 258 packed images, including hidden history; these are not active budgets. Twelve copied hidden roots keep the new recovery hierarchy outside the active export hierarchy.

## Verification and limits

| Gate | Current evidence |
| --- | --- |
| Complete source and preservation | [Native validation](source_validation.json), [prior fingerprints](source_preservation.json): closed, nondegenerate quad controls; positive edited-island volumes; preserved transforms, protected contacts and existing groups; mirrored geometry; eleven independently movable authoring modules; packed dependencies and explicit export allowlist. |
| Articulation preparation | [58 sampled tests](motion_study.json): symmetric and one-sided high step/forward/rear travel, crouch, near-extension and steering +/-20 degrees. Crossing checks pass outside the stated bearing/socket engagement regions. Links retain their lengths; minimum actuator overlap is about 108 mm and minimum bore-bottom gap about 38.5 mm. Relative hose-length drift is at most about 0.0000011. Lever pulls through 12 degrees clear the grips. |
| Ownership and rigging handoff | [Every active island](movement_ownership.json) has a unique guide carrier; native `MotionStudy_` selections cover each vertex once. Physical clevis guides, cable endpoints, lever hinges, steering bearing and a native JSON handoff are recorded. Guides are not production deform weights. |
| UV and PBR | [Materials and map checks](materials.json), [density/distortion comparison](uv_density.json): all 55 maps validated; no degenerate control-UV surface area; retained UVs exact where not repaired. Final derived positions, indices and split normals match the bake records; evaluated UV differences remain within 0.0005 texel at 4K. Normal data is normalized and unused normal texels padded flat. |
| Original/before/after appearance | [59 local review images and camera records](image_manifest.json): whole front/back/sides/top/underside, oblique/elevated, regional clay and surface details, a 384-pixel distance view, six pose renders, proportional rider comparisons and three comparison boards. Review found stronger planes, fitted supports and readable material separation without visible atlas errors in these views. |
| Installed-native reopen | [Independent verification and matched render comparison](native_reopen.json): source bytes match the accepted candidate. Two matched images differ by at most one 8-bit channel value, with RMS below 0.003 channel value. |

The collision diagnostic checks sampled crossing surfaces, with explicit intentional bearing/clevis/socket insertions. It is not continuous collision detection, complete containment certification or an engineering strength assessment. The retained proportional rider remains reachable at sampled steering angles; its existing approximately 2.5 mm thigh-radius approach near the cowling is unchanged. Radius-subtracted proximity is not signed penetration. Final clothing, wrists, seat compression and mounting/dismounting require the later rider/rig checks.

The editable cages remain quads. The final `Bake and export triangulation` modifier fixes derived quad splits after bevel and weighted normals. Use that same sequence for export; tangent behavior, compression, target mip levels, bounds and runtime cost still require Unity validation. Source appearance, geometry and sampled preparation are verified here; the asset is not represented as already rigged, animated or runtime accepted.

## Handoff and scope

The [model/rigging handoff](../../../Design/Art/LEGGER_MODEL_REFINEMENT_HANDOFF.md) explains carrier ownership, physical anchors and the derived-surface contract. The native text block `Legger_ModelRefinement_RigHandoff.json` carries the allowlist and key guides. Module roots are authoring groups: the FrontControlPod remains on the fixed body, and piston guides differ from preserved authoring origins.

Only this source model, related documentation and verification records changed in this task. No Unity asset, `.meta`, scene, package, game control, world service, export or production animation changed. No gameplay smoke test was run. Deterministic world identity, terrain streaming, generated-object identity and persisted runtime deltas do not apply to this local source-art slice; accepted authored constraints do apply.

[Local recipe identities](local_recipe_manifest.json) account for the native inspection, authoring and capture scripts. Raw native files, maps, arrays and image pixels remain local; this repository records the handoff and proof.
