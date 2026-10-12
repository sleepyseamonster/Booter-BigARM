# Booter's framed backpack — October 11, 2026

Authority: the author requested an audit and a substantial, proportionate framed backpack on Booter in Greater Wasteland, fitted over his cloak.

## Audit and result

The production player uses the Unity Standard Humanoid at visual scale 1.4. The camp system instantiates the canonical `WornCloak` prefab and a separate hip-mounted folded shade; no backpack existed. The cloak is bound after the first evaluated pose to chest and hips. Applying that varying cloth weighting to a framed pack would bend its frame.

Created `Assets/_Project/Art/Characters/Booter/Backpack/BooterFramedBackpack.fbx`, the `FramedBackpack` prefab and three canvas/webbing/frame materials. The pack is a nested child of `WornCloak`, so Greater Wasteland receives it through existing camp configuration without a scene rewrite. Its main sack measures .43 × .27 × .61 metres in author space (.60 × .38 × .85 at character scale), with lid, bellows pocket, side pouches, compression straps, shoulder harness, waist harness, buckles, carry loop and exposed frame. The contact face begins behind the existing cloak. The complete export includes the wrapping harness as well as the sack.

`Backpack_` mesh names form the rigid binding contract in `TopDown3DPlayerAnimationDriver`: all pack vertices follow the chest; cloak weights continue blending chest and hips. Camp asset rebuilding preserves the nested pack. It remains visible after deploying the separate shade, hides with the worn cloak while reclining, and returns on waking. There is no collider, equipment capacity, independent inventory item or survival modifier. During sleep this presentation hides the pack rather than placing a separate world prop.

## World integration contract

- Deterministic world identity: no procedural placement or seed input; the existing player presentation owns this asset.
- Chunk unload/reload: fixed Greater Wasteland terrain remains always loaded. The pack follows the player rig's lifecycle, independent of dressing cells.
- Stable generated-object identity: not applicable; this is an authored prefab attachment, not a generated world object.
- Authored constraints: existing character scale, cloak, rig, material boundaries and camp poses are retained. No terrain, movement, input, package or project-setting change.
- Persisted runtime deltas: no new save fields. The attachment is reconstructed from the canonical prefab; it adds no persisted equipment state.

## Verification

- Blender 5.2.2 LTS authored the mesh; the saved native source reopened with auto execution disabled. One mesh, 5,620 triangles, three materials, unit object scale and zero loose vertices. The 96 boundary/non-manifold edges are intentional open tube ends in frame/harness details, not a collision surface. No texture/bake dependency.
- FBX export: selected mesh only, unit scale applied, `-Z` forward / `Y` up, no animation/camera/light export. Unity import is readable with animation/camera/light import disabled. Materials are explicitly assigned to URP assets.
- Unity 6000.4.0f1 import and compilation completed with zero console errors. Existing unrelated warnings remain; this is not a warning-free project claim.
- Executed `TopDown3DBackpackTests.PackIsRigidThroughChestAndHipMotionAndUsesWornPresentation` directly through the connected Editor's `eval`. It creates a disposable preview scene, invokes the actual animation binding, checks sampled inter-vertex distances after independent hip/chest rotations, confirms the original cloth still blends, and checks hide/restore independently of camp-kit possession. Passed. This is one focused EditMode test invocation, not a TestRunner suite or gameplay smoke test.
- Production camp settings resolve one pack through `WornCloak`; all its mesh sources are readable. Existing scene remained clean. Rear, side, front and elevated Unity inspection views were captured in a disposable preview scene and visually reviewed. No foreground activation, scene save, Play Mode start/stop or gameplay replay was performed by this task.
- Native authoring source and its backup remain local at `SourceArt/Blender/Studies/BooterBackpack/`; Git ignore was verified. Only the optimized FBX and task-owned tooling, metadata and evidence are committed.

Hands-on walking, climbing, traversal and final art acceptance remain with the author. The fit is verified against the current standing rig; extreme animation poses may still require harness tuning.

## Reproduction

Run the pinned Blender in background with `--factory-startup --python Tools/Art/Blender/author_booter_backpack.py -- <checkout-root>`. The source is kept local and versioned Blender backups are retained.

Use the connected production Editor with `unity command run_script --file <checkout-root>/Tools/Art/Blender/BooterBackpackAuthoring.cs --entry BooterBackpackAuthoring.Build --project-path <checkout-root> --json` for bounded import/prefab authoring. `BooterBackpackAuthoring.Capture` requires Edit Mode and captures the actual serialized production player's rig in a disposable preview scene. It does not save the scene. Never force Play Mode to stop for capture.

See [Unity's SkinnedMeshRenderer API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/SkinnedMeshRenderer.html) for the underlying mesh/bone interface. Local proof is recorded in [mesh-report.json](mesh-report.json), [source-inspection.json](source-inspection.json), and the four adjacent inspection PNGs.
