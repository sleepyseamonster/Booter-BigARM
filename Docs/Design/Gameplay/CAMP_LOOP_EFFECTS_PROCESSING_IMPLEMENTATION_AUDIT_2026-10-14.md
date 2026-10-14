# Camp optics implementation audit

October 14, 2026. The optics candidates have a coherent bloom and lens-dust path, but the camp does not yet have a selected, fully combined bloom/flare/DOF/dust pipeline. Two focus/lifecycle defects and several integration gates remain. This audit changes documentation only.

The [approved plan](CAMP_LOOP_EFFECTS_PROCESSING_OVERHAUL_PLAN_2026-10-14.md) and [implementation evidence](../../Evidence/Rendering/CampOptics20261014/README.md) control scope. Preserving Clearer twilight until selection was intentional; it is not a failed installation.

The subsequent authorized [correction implementation](../../Evidence/Rendering/CampOpticsCorrections20261014/README.md) fixes the focus/lifecycle/control defects and installs the combined Gaussian/flare/bloom/dust profile in camp. The findings below preserve the audited starting state. GPU qualification and author appearance/feel acceptance remain open.

## Saved scene and effect ordering

The current [CampLoopTesting scene](../../../Assets/_Project/Scenes/Reference/CampLoopTesting.unity) selects `CampAtmosphere20261014/Clear.asset` at line 28158. Its base camera uses renderer 0, postprocessing enabled and antialiasing None. The required Sun Bloom renderer feature is currently active. Clear has custom warm bloom at 0.1 and synthetic aureole at 0.12, with tilt shift, streak and flare disabled and no native optics profile. Thus the new bloom-lit dirt and native DOF are available candidates, rather than the saved camp default.

| Selection | World blur | Bloom and lens dust | Solar flare |
| --- | --- | --- | --- |
| Clear baseline | Disabled | Legacy custom bloom; no new dirt | Disabled; legacy synthetic aureole remains |
| Low | Disabled | Native bloom, fine dust at 0.15 | Disabled |
| Standard / Aces | Gaussian far blur | Native bloom, fine dust at 0.3 | Disabled |
| High | Bokeh | Native bloom, soft dust at 0.3 | Disabled |
| Band | Corrected artistic band | Native bloom, fine dust at 0.3 | Disabled |
| Flare | Disabled | Native bloom, fine dust at 0.3 | Conservative custom flare before postprocessing |
| Off | Disabled | Disabled | Disabled |

The installed URP 17.4 source runs native DOF before bloom, then adds bloom and `dirt × bloom` in Uber Post before the display transform. This keeps the dirt mask out of world DOF. Post exposure is applied after bloom extraction, so changing exposure alone does not change which scene highlights cross the bloom threshold. Off intentionally retains the candidate's +1 EV and display transform for matched comparisons.

In Flare, the custom artifact enters bloom extraction and can therefore illuminate dust. In Standard/High, flare is explicitly suppressed to avoid passing a lens artifact through world DOF. There is no delivered all-four combination.

## Prioritized findings

### P1 Complete flare and DOF composition before combined acceptance

[TopDown3DRoundLensFlareFeature.cs](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Rendering/TopDown3DRoundLensFlareFeature.cs) rejects every native DOF mode other than Off at line 126. [CampOpticsAuthoring.cs](../../../Assets/_Project/Scripts/Editor/TopDown3D/CampOpticsAuthoring.cs) also rejects flare plus native DOF in its validator. This is a documented safe limitation, but it prevents the requested effects from working simultaneously. Adding a flare flag to Standard will silently suppress it at runtime.

Define and prove a camera-scoped HDR composition stage after world blur and before the display transform. Decide explicitly whether flare light should feed scene bloom/dirt; the current Flare candidate does so. Keep one bloom producer and one solar visibility signal. Acceptance needs a combined render with an in-focus subject, defocused HDR emitters, visible/partial/blocked sun and unchanged lens-mask sharpness.

The saved Clear baseline also retains its old unoccluded synthetic aureole: `PersistentSunGlow` in [BrokenWorldSunBloom.shader](../../../Assets/_Project/Shaders/TopDown3D/BrokenWorldSunBloom.shader), lines 39–61, has screen-edge attenuation but no depth input. Candidate native bloom suppresses this producer. Its blocked-source proof must not be attributed to the currently selected baseline.

### P2 Automatic focus cannot recover from delayed or replaced players

[TopDown3DCampOpticsStack.cs](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Rendering/TopDown3DCampOpticsStack.cs), lines 70–86, marks the automatic search complete on its first attempt. If no motor exists then, or its transform is later destroyed, the same profile never searches again. Processing falls back to manual distances until a profile change or explicit `ConfigureFocusTarget` call resets state. Standard and High depend on this fallback search unless an explicit anchor is supplied.

Use a bounded reacquisition policy or player-lifecycle binding. Test initial absence followed by spawn, destruction/replacement and scene transitions. Retain forward eye-depth focus and existing warp resets. Behind-camera anchors currently clamp to 0.1 m; define a safe manual fallback instead of treating them as a valid near focus plane. Existing tests cover explicit anchors and warps, not automatic recovery.

### P2 Previously eligible cameras retain state on some rejection paths

`TryApply`, line 42, returns for absent camera data, Overlay mode or disabled postprocessing without calling `Release`. Initial rejection is safe, but an already-owned private stack and focus history survive until another cleanup route runs. In direct/manual application paths, that return also does not restore a currently selected owned manager stack.

Route these transitions through the existing release/restore logic. Test eligible → postprocessing off, eligible → Overlay and eligible → missing camera data, both after restoration and while the owned stack is selected. Existing tests establish initial rejection and binding-disable cleanup; they do not establish these transitions. Disabling the Camera component while leaving its binding enabled also deserves a defined retention policy.

### P2 Renderer prerequisites are missing from the candidate validator

Native profile application is hosted in [TopDown3DSunBloomFeature.cs](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Rendering/TopDown3DSunBloomFeature.cs), line 116. Disabling/removing that feature or choosing another renderer removes the bridge as well as custom bloom. Candidate assets can still pass `ValidateFromCli`, which checks assets, masks and shaders but not the selected camera's renderer feature, base-camera/postprocessing settings or actual bridge participation.

Add a scene/camera prerequisite check with a clear diagnostic. Include inactive/missing feature and alternate-renderer cases. Current saved settings satisfy these prerequisites; this is a configuration robustness gap rather than a currently broken renderer.

### P2 Combined visual evidence and GPU qualification remain open

The [lens fixture](../../Evidence/Rendering/CampOptics20261014/lens-render-proof.json) proves bloom-fed dirt and immediate opaque blocking at 512 square. Its dirt comparison uses intensity 3, ten times Standard's 0.3, with DOF Off. It does not establish perceptible, restrained dust at shipped strength or its response after Gaussian/Bokeh alters small highlights before bloom extraction.

The retained 45 captures and 10,800 standalone measured renders establish participation and selected static behavior. They do not qualify GPU budgets: every case has zero valid GPU timing samples. The harness freezes/manual-binds focus, removes gameplay scripts, and does not rasterize the actual overlay HUD into its offscreen target. Retained images also use capsule/companion placeholders and an earlier camp scene.

Render intended strengths with Gaussian/Bokeh/Band, defocused colored highlights and depth edges. Add deterministic camera motion, partial occlusion, thin blockers, explicit smoke policy, riding-height proxy anchors, resolution/render-scale changes and ultrawide crops. Native URP already preserves the mask's aspect with a centered cover crop; actual peripheral content still needs appearance inspection. Measure incremental GPU p95 and peak target/history memory before judging Low/Standard/High cost. Author-owned gameplay/HUD acceptance remains separate.

### P3 Quality and optional controls need consistent contracts

High substitutes a different dust mask and Bokeh, so it changes lens character and world blur, rather than only filtering quality. Keep these as named look comparisons until a lens character is selected; then derive quality tiers that preserve that character. Gaussian high-quality sampling, bloom filter/downscale/iteration count and Bokeh blade settings remain inherited from volumes, rather than owned by the optics profile. The current default volume uses Gaussian HQ sampling off and six bloom iterations. Record those effective settings in future comparison receipts or explicitly own the required controls.

Two smaller source defects should accompany the robustness slice:

- [TopDown3DAnamorphicStreakFeature.cs](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Rendering/TopDown3DAnamorphicStreakFeature.cs), line 81, honors `streakEnabled` but ignores native master-off/validity/DOF. An authored streak can survive master-off. All delivered candidates set streak off, so present comparisons are unaffected.
- The flare tint at line 183 interpolates with clamped absolute light intensity, rather than `PerpetualTwilightSun.Brightness01`. With the default 0.68–1.18 intensity range, deep twilight already uses about 68% of the bright tint. Use the normalized phase for color and retain intensity for energy. The native profile validator also accepts an undefined tonemapping enum; reject it explicitly.

## Verified strengths and evidence boundary

Candidate selection suppresses duplicate custom bloom; native scene bloom is neutral, and solar artifacts retain their own warm tint. Bloom-disabled dirt contributes exactly zero in the retained readback. Native DOF and corrected band blur are exclusive. The corrected blur has actual impulse symmetry/downsample proof. Conservative flare rejects offscreen/missing-depth sources, uses angular size and immediately suppresses a fully opaque blocker. Private stacks restore after normal rendering, and binding teardown releases owned state.

All 66 files listed in the implementation receipt still match their recorded SHA-256 hashes. The retained 73/73 checks remain evidence for those bytes; they were not rerun during this documentation-only audit. No Unity launch, Play Mode, gameplay smoke test or foreground activation was performed.

Scene identity was checked against repository HEAD `b6e6a247fe00443a185c8ffe16bf71ea43362936`. Current scene SHA-256 is `d892a31615284db7d396e91a21bcf0a1f635ed29845b3ee1ab5fb2072d8ab865`; the optics candidate scene is `d2d095c387e6f3c1ca1693e92c106cfea35249ee18fd24f6002e557e3ee6bfb2`. Later camp workstation, riding and Ironstone integrations explain why old captures are not current-scene visual proof. Preserve those current records in any future optical binding save.

## Correction sequence

1. Repair focus reacquisition, ineligible-camera release, prerequisite diagnostics and small control/phase-validation defects. Add focused transition checks rather than broad gameplay tests.
2. Prove the HDR flare composition stage and a combined bloom/DOF/dust/flare fixture. Keep the current exclusive candidates as comparison fallbacks.
3. Capture the current camp at intended strengths, establish transparent participation and inspect resolution/aspect/motion behavior. Select lens character, blur method and predictable quality tiers from those comparisons.
4. Qualify GPU cost and actual HUD appearance, then make only the selected camera/profile binding change. Preserve Clearer twilight as a fallback and current camp-management/gameplay records.

Optics affect no deterministic world identity, terrain generation/streaming, generated-object identity or persisted runtime deltas. Terrain stays authored and always loaded. Assets retain normal Unity GUID identity; focus and flare history are camera/session state with teardown requirements. No rendering or gameplay behavior was changed by this audit.
