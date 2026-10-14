# Camp Loop effects and processing overhaul plan

October 14, 2026. Revision 2 incorporates the [plan audit](CAMP_LOOP_EFFECTS_PROCESSING_OVERHAUL_PLAN_AUDIT_2026-10-14.md). Prepared for the author's request to consolidate the rendering research into an overhaul plan, with permission to generate needed resources. This task creates and audits the plan; runtime implementation remains a subsequent task.

## Intended result

Give CampLoopTesting a coherent camera and lighting treatment: red ochre and orange twilight, a deep yellow low sun, readable shaded ground, stable highlights, restrained solar flare, controlled depth blur and soft lens speckles that catch light. Effects should support the landscape's scale and the player's interactions through orbit, follow and riding camera transitions.

Keep the selected Clearer twilight study as the comparison baseline. Deliver a camp-only replacement profile and quality presets first. Production promotion is a later, explicit integration decision after the camp comparison passes. Preserve sun timing and direction because gameplay shade uses the same authority.

The [bloom and flare research](../../Research/Rendering/AAA_BLOOM_LENS_FLARE_LIGHTING_RESEARCH_2026-10-14.md), [DOF and lens-dust research](../../Research/Rendering/CAMERA_DEPTH_OF_FIELD_TILT_SHIFT_LENS_DUST_RESEARCH_2026-10-14.md), [lighting audit](CAMP_LOOP_LIGHTING_CAMERA_AUDIT_2026-10-14.md) and [installed atmosphere receipt](../../Evidence/Rendering/CampAtmosphere20261014/README.md) supply the design and implementation evidence. The prior [atmosphere plan](CAMP_LOOP_ATMOSPHERE_IMPLEMENTATION_PLAN_2026-10-14.md) continues to govern the installed sky, native landscape context and ownership contracts.

## Recommended design choices

| Area | Initial candidate | Escalation condition |
| --- | --- | --- |
| Bloom | Pinned URP high-quality bloom, neutral scene tint, restrained strength | Retain a custom producer only if a matched filter comparison demonstrates a necessary visual advantage. |
| Sun flare | Authored spherical character: small aureole and one faint ring/ghost | Add further elements only if they improve normal gameplay framing. |
| Lens dust | Soft fixed mask illuminated by bloom | Add source-driven glints when an offscreen sun or nearby light needs a response that image bloom cannot supply. |
| Depth blur | Resolution-consistent artistic band; compare far-only Gaussian and restrained Bokeh | Add an artistic world focus plane only when simpler options fail the intended composition. |
| Exposure | Explicit fixed baseline with bright/deep phase controls; compare Neutral and ACES display transforms | Auto exposure requires a demonstrated lighting range problem and its own adaptation tests. |
| Atmosphere | Existing sunless sky, native context and restrained distance haze | Add localized world dust when matched views show a specific need. |
| Antialiasing | Evaluate existing pipeline AA and lightweight alternatives with small highlights | TAA is a separate candidate with history and ghosting costs; do not assume it is already active. |

These choices prioritize a small, maintainable pipeline. The custom features remain available during comparison. Select one bloom producer and one world-blur mode at a time. Do not stack artistic tilt shift and native DOF by default.

## Rendering architecture

Extend the existing camp presentation binding with a versioned optics profile, per-camera enable/quality controls and validated ranges. Keep lighting state under PerpetualTwilightSun and the existing active-scene owner. Optics must read that sun rather than create a second clock. Preserve existing serialized profiles and feature behavior for cameras without the new selection.

Separate **world atmosphere**, **world focus**, and **lens artifacts**. Lens dust uses screen coordinates and remains fixed during a pan; airborne dust belongs in the world and has depth and parallax. Screen dirt does not act as a world decal or obscure every frame with a constant dirty overlay.

The intended data order is:

```text
HDR world and depth
  -> world haze and participating transparents
  -> selected world blur
  -> scene bloom extraction and filtering
  -> separately visible sun optics and lens-dust composition
  -> exposure/display transform
  -> sharp HUD and interaction prompts
```

Resolve actual URP 17.4.0 RenderGraph insertion points before implementing this order. Current custom blur, bloom, streak and round-flare features all use BeforeRenderingPostProcessing; an enum value alone does not prove their relative order. Native DOF and bloom also have their own internal sequencing. In particular, pre-postprocess flare imagery must not accidentally be blurred as world geometry or bloomed twice. If native processing cannot accommodate the desired late HDR lens composition through a supported hook, select a simpler compatible ordering or a narrowly scoped custom composite and document it before coding.

The pinned `Runtime/PostProcess.cs` records native DOF before bloom extraction, data-driven lens flare after bloom extraction, then UberPost. UberPost adds bloom/dirt before color grading. Start the feasibility comparison with this native sequence: it avoids blurring lens flare as world geometry, while bloom-lit dirt responds to the scene bloom buffer rather than newly drawn procedural flare. Evaluate native data-driven flare against the existing custom flare before authoring another compositor. Its shared flare registry/occlusion resources require camera-isolation proof; native availability alone does not satisfy that contract. No package-source modification is planned.

Native bloom/dirt needs camera-specific volume selection. Prove a supported camera volume-stack/layer arrangement in the isolated candidate before choosing it; do not write the shared default profile or renderer asset at runtime. Restore camera/volume ownership on disable, destruction and scene changes. Reject duplicate owners. Scene View, preview, reflection and unrelated cameras must have explicit participation rules.

Before stage 2, produce a small feasibility receipt showing the exact pass/resource order, supported volume-selection API, AA compatibility, overlay HUD placement and two same-scene cameras plus an additive unrelated scene. Prefer existing layer allocations or supported per-camera stack APIs; a new project layer requires a separately reviewed settings diff. Restore the original camera update mode, layer mask, trigger and owned stack without destroying borrowed stacks. `UpdateVolumeStack` must run after pipeline/VolumeManager initialization. If any isolation or ordering proof fails, continue fixtures/resource preparation while resolving that dependency; do not proceed with visual tuning on a leaking configuration.

Temporal visibility or focus state is owned per camera. Reset on a cut, target replacement, profile/quality change, resolution change or invalid source. Use time-based smoothing rather than a fixed per-frame blend coefficient. Release all render targets and histories on teardown. A quality switch must not leave stale flare light or ghosted blur.

## Implementation stages and acceptance gates

### 1 Establish a reproducible baseline

Freeze a committed isolated candidate, record protected asset hashes and capture Clearer twilight at matched sun phases, camera poses and resolution. Include ground, shaded Booter, companion, workstation, bright metal and emissive reference objects. Record far-plane and distance-culling boundaries independently of optical effects.

Saved camp camera data currently has an 8000 m far clip, postprocessing enabled and additional-camera AA set to None. Audit runtime overrides, depth precision, range presets and renderer AA before selecting settings. Preserve terrain context, camera FOV/orbit/pitch and current clearance behavior. Riding is actively modifying the rig; coordinate against its finished contract rather than edit shared camera movement in parallel.

The shared pipeline currently serializes MSAA at 1 (disabled) with HDR enabled. Record both camera and pipeline settings, depth format and runtime range resolution. Fix camera poses and sun phases in fixtures; compare AA using the same small-highlight sequence before attributing flicker to bloom. Keep AA unchanged during each bloom/blur comparison.

Gate: repeatable baseline and effect-off capture, stable profile restoration, declared candidate inputs and no unrelated scene or terrain changes.

### 2 Establish lighting and display transformation

With optics disabled, tune exposure, ambient fill and shadow readability through the existing bright/deep sun phases. Compare current display output, Neutral and ACES in a private profile. Check whether yellow highlights bleach and whether ochre hues shift. Select a fixed/manual exposure curve first; optical intensity follows that choice.

Use URP ColorAdjustments.postExposure in EV for the initial display adjustment. Pinned UberPost applies it during color grading after bloom/dirt composition, so it does not alter the HDR input used by bloom threshold extraction. Record post-exposure EV, sky exposure, directional/ambient lighting and bloom threshold separately. Changing scene illumination is a different operation; hold it fixed during the display-transform comparison. Automatic pre-exposure is outside this slice.

Keep sky artwork sunless and world aligned. Continue the separate yellow disc. Treat current generated panorama and reflection cubemap as artistic inputs; increasing file precision cannot turn them into measured radiance. Generate a revised sky only if seam, horizon, pole or landscape matching fails a comparison. Evaluate reflection roughness/mip behavior separately before increasing environment brightness.

Gate: readable shadows, stable sky color and controlled highlights without bloom hiding exposure problems. Far-plane gaps remain visible for diagnosis rather than covered by blur or glare.

### 3 Rebuild bloom and solar visibility

Compare existing custom bloom against native high-quality bloom at matched perceived strength. Test fine moving highlights, colored emitters and low-strength veil. Use the CoD-informed first-downsample stability/anti-firefly variant only if native bloom fails a specific fixture. Keep scene bloom neutral; solar optics can remain yellow independently.

Remove the synthetic halo's ability to shine through occluders. Define one solar visibility signal for analytic halo, ring/ghost and any source-driven dust. Depth samples near the projected solar disc provide partial visibility where available. Offscreen depth is unknown, not visible: begin with a conservative angular fade to zero outside the supported region. Compare a bounded occlusion-query alternative only if offscreen solar response materially improves the game view. No full-scene physics raycast loop per flare element.

This visibility signal gates camera artifacts, including the injected bloom aureole; it does not disable the analytic sky disc or world lighting. The sky disc remains ray-projected and naturally hidden by opaque geometry. Define valid foreground-depth versus sky/background samples, reversed-Z handling, near-plane/behind-camera rejection, sampling bounds and no-depth fallback in the visibility fixture. Derive disc radius from its angular size/FOV; do not compare raw solar distance to the 8000 m far clip. Transparent occluders need an explicit material policy and a supported visibility input; ordinary opaque depth cannot promise smoke opacity.

Use a documented fade duration and independent attack/release behavior. Camera cuts reset immediately. Check bright/deep phases and the existing 26–65 degree pitches: the sun is often outside the normal camera image, so effects must earn their visibility there.

Gate: no unoccluded synthetic halo through terrain, no persistent history after cuts, stable highlights and one active bloom path.

### 4 Make world blur predictable

Correct horizontal/vertical radius units and normalize to a reference resolution before changing art strength. The source audit estimates current full-mask reach at 14.56 versus 29.12 source pixels across the two axes at downsample 2; verify with an impulse fixture. Expose band angle, center, sharp width and feather with meaningful bounds.

Compare this corrected artistic band with native Gaussian far blur and restrained native Bokeh. For target focus, use camera-forward eye depth, a broad readable gameplay region and a stable body/interaction anchor. Avoid focusing on feet, nearby rocks or every tiny target displacement. Smooth in seconds and clamp near blur. Orbit, boom compression and riding must not pump focus.

Map focus controls by mode: Gaussian uses far-start/far-end distances around the readable region; Bokeh uses focus distance and lens/aperture parameters. A single native Bokeh plane cannot independently keep widely separated subjects sharp. Increase depth of field or use the corrected artistic band when the player/companion/workstation region cannot fit. Use pose fixtures and rendering-only proxy anchors to test orbit/riding without making motor or follow-controller changes.

Specify smoke/transparent participation. Foreground blur over a sharp player needs an edge/coverage comparison; depth checks alone cannot reconstruct hidden background. An artistic tilted world plane remains a later alternative and must be labeled as such rather than a physical lens simulation.

Gate: consistent blur at resolution/downsample changes, readable interactions and silhouettes, stable motion, explicit transparent behavior and a reliable off state.

### 5 Add illuminated lens dust

Generate two grayscale artistic masks: sparse fine speckles and broader soft dusty patches. Concentrate detail toward the periphery while retaining subtle irregularities elsewhere. Derive data textures deterministically from the selected artwork. Keep base darkness nearly transparent; bloom supplies the light response. Do not bake a sun, flare, color cast or opaque dirt veil into the mask.

Store zero as no dirt contribution and positive grayscale values as scattering strength; alpha is not the native URP dirt control, which samples RGB. Specify whether generated pixels are artist data or display-encoded artwork before conversion, then record that conversion and disable sRGB for runtime data. Preserve a clear mask variant and seed/hash the deterministic derivatives. Generation output dimensions are inspected, not assumed; resampling to the target size belongs to the derivation receipt.

Use native dirt with native bloom, or explicitly connect the selected custom bloom texture to a lens composite. Preserve mask aspect and define center cropping for ultrawide views. Composite in linear HDR before the display transform. Keep UI separate. A lens mask is intentionally out of focus and does not require high-resolution pin-sharp particles.

If needed, compare a second source-driven path with camera-relative angular illumination and the visibility signal from stage 3. Local-light selection must be bounded, with distance/angular falloff and opt-out flags. An optional height/normal mask can produce subtle glints, following Crytek's engine precedent. Wet overlays and persistent dirt accumulation are separate future features.

Gate: mask fixed to the lens during movement; glints move with illumination; negligible effect without light; no hidden-source leaks; HUD and central gameplay remain readable.

### 6 Tune atmosphere and quality presets

Tune horizon haze and any localized airborne dust after the optics pass is chosen. Preserve the 95 native-height context sectors and nine playable tiles. The southern geographical continuation remains artistic; haze cannot establish missing terrain accuracy. Keep distant silhouettes stable at range changes. Add light shafts only for a demonstrated view, with depth visibility and an independent cost measurement.

Ship candidate Low, Standard and High profiles. Low disables DOF and source-driven dust/extra ghosts; Standard uses the selected restrained blur and bloom-lit dirt; High may improve filtering or enable a proven optional element. The chosen lens character stays consistent across tiers. Provide a master optics-off comparison plus independent bloom, blur, flare and lens-dust controls.

Gate: all tiers restore correctly, degradation is predictable, and optional passes allocate no targets while disabled.

## Resources and file ownership

Generation permission covers the needed local resources. Create only assets justified by the comparisons; preserve existing sky and texture GUIDs. Source-art generation is part of implementation, so the plan itself needs no new image.

| Resource | Preparation and proposed location | Acceptance |
| --- | --- | --- |
| Lens-dust masks | Two 2048-square generated masters under `SourceArt/Rendering/CampOptics/`; derived 1024-square linear runtime masks under `Assets/_Project/VFX/Textures/CampOptics/` | Soft shapes, clean gameplay region, aspect-safe mapping; select texture format/mips after sampling tests. |
| Lens shapes | Deterministic radial ring/ghost/glare masks in the same runtime folder; optional authored variant | Consistent spherical character; neutral data with shader-controlled yellow tint. |
| Optical fixtures | Deterministic impulse, checkerboard, HDR glints and depth-edge fixtures under `Assets/_Project/Tests/` | Reproducible inputs independent of generative artwork. |
| Sky revision if needed | Sunless 2:1 artwork and seam/pole review using the existing sky import workflow | Clearer twilight palette, Death Valley-like landforms, no baked sun; provenance explicitly describes artistic geography. |
| Reflection if needed | Linear half-float derived cubemap; separate roughness-prefilter study if justified | No radiance-calibration claim; inspect metallic highlights and memory. |
| Profiles and evidence | Candidate presets under `Assets/_Project/Settings/Studies/`; receipts and comparisons under `Docs/Evidence/Rendering/` | Stable GUIDs, valid references, generated-source hashes and import settings recorded. |

Runtime changes stay in `BooterBigArm.TopDown3D.Runtime`, with editor tooling in the established Editor assembly. Gottspan integrates; Babineaux owns background Unity proof and asset import; Gear Ball commits verified task-owned batches. No delegate edits coupled shaders, profiles or scene bindings concurrently. No package upgrade is needed.

## Performance and verification

Use the current RTX 3060 Ti at 1920×1080/D3D11 as the first declared measurement configuration. Proposed incremental GPU targets above the matched optics-off scene are **1 ms Low, 2 ms Standard and 3.5 ms High** at the 95th percentile. These are engineering targets to test, not measured capability. A 60 Hz overall frame target is 16.67 ms, including gameplay; passing an optics budget alone does not establish 60 FPS.

Run a background standalone rendering fixture in an isolated candidate, with deterministic camera/light motion and no gameplay smoke test. Separate profiling from screenshots/readback. Warm up at least 120 frames and collect at least 600 measured frames per configuration; repeat noisy runs. Record GPU pass and total time, CPU render time, resolution/render scale, quality, transient/persistent target memory and allocations. If reliable GPU timing is unavailable, report the performance gate as unresolved. CPU `Camera.Render` submission duration is not a substitute.

Build the rendering harness with an explicit scene list through isolated editor automation; do not alter production Build Settings or invoke a production builder that selects Greater Wasteland. Launch hidden/background with graphics enabled, not `-nographics`. Keep VSync/frame caps, API, power state and build instrumentation declared. Pair identical full-frame GPU measurements with optics off/on to estimate incremental cost; pass samples explain that difference and are not blindly summed. Report p50/p95/p99, peak target memory, bounded history lifetime and zero steady-state managed allocations by the new optics. Test render-scale and quality switches for reallocation spikes.

Include quiet camp and many emissives; visible/partial/hidden/offscreen sun; thin geometry and smoke; slow orbit and cuts; 720p/1080p/4K and ultrawide; quality/profile swaps and additive scene transitions. Exercise eye-depth targets during follow/riding through rendering fixtures, while the author owns hands-on gameplay and feel testing.

Required automated proof comprises shader/import diagnostics, focused visibility/history/focus-unit tests, radius/edge rendering fixtures, profile compatibility, per-camera isolation and lifecycle restoration. Retain existing sun, optics, view-distance and clearance regressions that intersect the changed contracts. Capture matched static images at each stage; recheck protected terrain, scene records and shared assets before integration.

## World boundaries and completion

Optics create no generated-world identities, terrain streaming or saved-world deltas. Greater Wasteland remains fixed and always loaded. Masks/profiles use normal authored Unity GUIDs. Cosmetic history is per camera/session and releases on teardown; persistent grime would require a separately defined lifetime and save contract. Authored terrain and gameplay shade constraints remain authoritative.

Each stage ends in a small verified commit and an evidence handoff. Keep the old camp baseline available until the replacement passes visual comparison, structural checks and its performance gate. Integrate only bounded camera/binding records into the actively edited camp scene. Do not overwrite riding, animation, inventory or workstation changes.

Completion means one selected camp pipeline, generated-resource provenance, tested quality tiers, verified scene/camera isolation, matched captures and measured or explicitly unresolved GPU results. The author then evaluates appearance and camera feel. Production rollout follows a separate integration decision; full lens ray tracing, FFT flares, dynamic clouds, local tone mapping, automatic exposure and persistent visor dirt are outside the initial overhaul.

An unresolved GPU gate permits an evidence-backed camp candidate handoff, but not a performance-qualified completion claim. Until a new look is selected, deliver the recommended candidate and retain Clearer twilight as the active baseline. Each comparison receipt records changed parameters, matched inputs, measured costs, decision and fallback; selection is not inferred from a successful test run.
