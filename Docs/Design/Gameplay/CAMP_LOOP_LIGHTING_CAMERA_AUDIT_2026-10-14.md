# Camp Loop lighting sky and camera audit

October 14, 2026. Target: [CampLoopTesting](../../../Assets/_Project/Scenes/Reference/CampLoopTesting.unity). The scene has a coherent low-sun foundation, but its sky presentation, camera clearance and optical effects need separate evaluation before tuning them together. The strongest first work is to separate the moving sun from the painted landscape, establish a clear image baseline, and compare camera framing and clearance at the authored stations.

This is a saved-scene and source audit, including inspection of the sky artwork. No Editor process or CLI connection was running. Appearance in the rendered scene, controller feel and GPU costs remain unmeasured. The [evidence snapshot](../../Evidence/Rendering/CampLoop20261014/saved-scene-audit.json) contains serialized lighting/camera values, source SHA-256 hashes and reproducible geometry calculations. Existing dirty scene, animation and salvage work remains separately owned; this audit changes documentation only.

## Scene and rendering ownership

The saved scene contains one Camera, one directional Light and nine native Terrain components. The 3 by 3 terrain footprint is 768 m square, with local X bounds −2304 to −1536 and Z bounds −1792 to −1024. Terrain hierarchy offsets should be rechecked before future placement work. The builder retains this bounded slice from production; it is not a complete distant landscape. Greater Wasteland remains the only enabled build scene.

The camera uses renderer index 0 of [UniversalRP](../../../Assets/_Project/Settings/Rendering/URP/UniversalRP.asset), backed by [Renderer3D](../../../Assets/_Project/Settings/Rendering/URP/Renderer3D.asset). Unity is pinned to 6000.4.0f1 and URP to 17.4.0. Cinemachine is installed, but the inspected camera uses the project-owned rig.

The camera Inspector's optics controls edit this shared renderer asset, not a camp-specific camera profile. Tuning those controls can affect Greater Wasteland and other scenes using the same renderer. A bounded camp comparison needs an explicit local override or an isolated candidate before accepting shared changes.

## Low sun and environmental light

[PerpetualTwilightSun](../../../Assets/_Project/Scripts/Runtime/TopDown3D/PerpetualTwilightSun.cs) owns the light rotation, direct color/intensity, trilight ambient fill and runtime sky material. The scene's values differ from some script defaults, especially azimuth and ambient fill; use the scene as the baseline.

| Property | Saved scene and runtime contract |
| --- | --- |
| Cycle | Enabled, 1200 scaled seconds, initial phase 0.12 |
| Elevation | 19° centre with 7° amplitude, giving 12–26° |
| Light transform yaw | 18° centre with 14° amplitude, giving 4–32° |
| Direction toward sun | Opposite the light's forward vector; horizontal azimuth 184–212° |
| Initial evaluated sun | Elevation 23.792°, horizontal azimuth toward sun 208.206°, brightness fraction 0.8423 |
| Direct intensity | 0.68–1.18, moving from deep orange to pale warm light |
| Shadows | Soft, strength 0.82, bias 0.2, normal bias 0.35, high soft-shadow quality |
| Ambient | Authored sky, equator and ground colors interpolated through the same cycle |
| Panorama exposure | 0.65–1.05 at runtime |

On level ground, a vertical object's shadow is approximately 4.70 times its height at 12° sun elevation and 2.05 times at 26°. Slopes and occluding geometry alter that result. This supports [World Basis](../../WORLD_BASIS.md): enduring long shadows, orange sky, brighter sunset through deeper twilight, and environmental readability. The implemented path is sinusoidal elevation plus cosine azimuth; literal figure-eight astronomy is not required by canon.

The cycle uses `Time.deltaTime`, so camp sleep acceleration also accelerates the light. Camera orbit, pan, recenter and target damping use scaled time too. Sleep and modal transitions therefore belong in the eventual camera-feel comparison, alongside ordinary traversal.

The pipeline specifies a 4096 px main-light shadow atlas, four cascades and 500 m shadow distance. Cascade splits are 0.025, 0.08 and 0.3. This is a substantial shadow configuration for a small scene. Low-angle grazing light makes contact bias and shadow transitions especially important, but the audit does not establish visible acne, detached shadows or shimmering. Compare the camp screen, Booter's feet, rock contacts and station rails before changing bias or resolution.

The sky is not the ambient-color authority: trilight fill is set separately. The sun controller contains no explicit reflection-environment refresh. Verify metal/ceramic reflections against the runtime sky before assuming a changed sky exposure updates every lighting contribution.

## Sky artwork and mapping findings

The [panorama material](../../../Assets/_Project/Resources/TopDown3D/MartianPanoramaSky.mat) uses [MartianSunsetBasin.png](../../../Assets/_Project/Art/Environment/Sky/MartianSunsetBasin.png). The inspected source presents ochre cloud texture, a bright white disc and aureole, dark mesas and a strongly detailed rocky foreground. Its colors support the world palette; whether its cloud texture feels like suspended dust or an Earth-like overcast sky needs creative acceptance in context.

The [sky shader](../../../Assets/_Project/Shaders/TopDown3D/MartianPanoramaSky.shader) samples only the image from skyline latitude 0.545 upward. It maps the painted disc at U 0.744 and V 0.72 to the physical light. It blends the longitude seam over width 0.025 and pins sky depth to the far plane for both depth conventions.

Three important consequences follow directly from the shader:

1. **The whole painted landscape follows the sun.** Longitude is relative to the changing sun azimuth. Painted mesas and clouds therefore rotate through a 28° azimuth span during the cycle. Vertical remapping also stretches/compresses the sky as the light rises and falls. A fixed ridge layer with a separately moving disc/aureole would preserve landscape identity.
2. **Below-horizon sky is a repeated skyline row.** Negative view elevations clamp to V 0.545. Terrain gaps or missing distant geometry can reveal vertical repetition of that row rather than the panorama's ground. This is a strong candidate explanation for unusual distant strips; a render is needed to associate it with any observed artifact.
3. **Edit Mode and runtime sky differ.** Saved RenderSettings references Unity's built-in sky material. The panorama is cloned and installed only while playing. An Edit Mode scene capture alone cannot establish the shipped panorama appearance. The panorama path changes exposure and sun direction, but does not consume the controller's procedural `_SkyTint` colors.

The texture has mipmaps, sRGB sampling, bilinear filtering, a 2048 px import limit and no HDR source range. Its painted white disc cannot provide the same independently adjustable HDR energy as the custom sun optics. Keep painted-disc exposure, projected flare and bloom as separate controls when judging glare.

The sun restores its previous skybox on destruction, but disabling the component alone does not restore global light/ambient/sky state. Feature A/B tooling must deliberately restore its owned state rather than assume disabling this component produces a neutral baseline.

## Camera framing and far distance

[TopDown3DCameraRig](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DCameraRig.cs) follows Booter in LateUpdate with a 1.15 m target offset, 25 m distance, yaw 40°, pitch 50°, vertical FOV 48° and SmoothDamp time 0.14 s. Pitch is constrained to 26–65°. Booter's Rigidbody is configured for interpolation.

| Pitch | Top centre ray elevation | Distance along that ray to flat ground |
| --- | --- | --- |
| 26° | −2° | 347.0 m |
| 50° | −26° | 46.3 m |
| 65° | −41° | 36.3 m |

These calculations assume the unobstructed 25 m camera distance, flat ground through the player and the authored offset. Actual terrain can block a ray earlier or fall away beneath it. Even at minimum pitch, the entire camera frustum points below the astronomical horizon. The 12–26° sun disc cannot enter normal gameplay framing at this FOV; any sky exposed beyond terrain is viewed below the horizon. The sky still matters as a backdrop and source for distance blending, while direct light and long shadows matter throughout play.

[BadwaterCameraRange](../../../Assets/_Project/Scripts/Runtime/TopDown3D/BadwaterCameraRange.cs) owns far clip. The saved range is 8000 m and near clip is 0.1 m. Local accepted menu preferences can change it:

| Preference | Far clip | Opaque distance blend band |
| --- | --- | --- |
| Low | 500 m | 375–500 m |
| Medium | 1000 m | 750–1000 m |
| High | 2000 m | 1500–2000 m |
| Maximum | 8000 m | None |

The reduced-range blend uses camera eye depth, not horizontal player distance, and renders the same directional sky into a separate texture. It operates independently of global dust. It runs before transparents, so transparent effects do not automatically receive this cutoff treatment.

**The terrain footprint and far clip are different limits.** An 8 km lens cannot display terrain absent from the camp slice. Its outer edges can become visible as terrain falls away or the camera approaches a boundary, without the camera reaching its far plane. The flat-ground estimate at minimum pitch approaches the slice's typical edge distance, so low-pitch comparisons need terrain-edge awareness. Do not hide a missing landscape by casually reducing the far clip or extending near-field volumetric march distance.

## Following input and obstruction findings

Right-stick orbit runs at up to 120°/s yaw and 70°/s pitch. Mouse delta applies 0.3° per pixel. Holding the gamepad left trigger makes the right stick pan the framing target on XZ: up to 12 m at 12.6 m/s, returning at 37.8 m/s when released. Full outward travel takes roughly 0.95 s and full recenter roughly 0.32 s at ordinary world speed. This is manual scouting pan; the rig does not add velocity-based forward look-ahead.

The obstruction query is a nonallocating sphere cast with radius 0.35 m, a 16-hit buffer, all-layer mask and ignored triggers. It excludes colliders under Booter's target transform, then clamps camera distance to 4–25 m.

| Finding | Why it matters | Next focused check |
| --- | --- | --- |
| Obstruction distance changes immediately | Entry/exit around rails, rocks or the companion can cause abrupt zoom even while target translation is damped | Compare slow approaches and exits; consider fast compression and damped release only after observing the issue |
| Minimum distance overrides closer hits | A hit closer than the 4 m clamp can leave the camera in or beyond an obstruction | Establish a defined clearance fallback for enclosed spaces |
| SnapToTarget skips the obstruction cast | Configuration, view-angle changes or resets initially place the full-distance camera until LateUpdate resolves clearance | Inspect every reset/configuration path at constrained stations |
| Query buffer can saturate | A full nonalloc buffer does not establish that every nearer blocker was returned | Add an explicit saturation policy in a future repair and verify a dense collider fixture |
| Cast starts at the panned framing target | Camera-to-framing-target clearance is not a guarantee of Booter visibility | Compare maximum pan beside station cover and rocks |
| Lens settings are reapplied every LateUpdate | Manual Camera Inspector FOV/near-plane/post-processing changes are overwritten in play | Tune through the owning rig/profile, not transient camera values |

The round flare's history resets for a frame gap or projected sun movement above 0.12 viewport units. It does not detect camera translation directly. A reset with unchanged orientation can therefore reuse old visibility briefly even after moving behind a different blocker. Test this in a future optics comparison; do not describe every camera cut as already covered.

## Effects and performance audit

| Surface | Current posture | Rendering work inferred from code |
| --- | --- | --- |
| Global dust | Disabled in the saved scene | No atmosphere initialization, density refresh, close motes/veils or dust raymarch from this controller |
| Distance haze | Reduced view-distance presets only | Extra sky render plus full-resolution composite; no dust scatter texture when dust is disabled |
| SSAO | Active, half resolution, depth normals, 8 samples, high bilateral blur, intensity 1.35, radius 0.12, falloff 75 m | Depth-normal requirement and AO/blur passes; evaluate rock/foot contact benefit separately from shadow cost |
| Tilt-shift | Active; no serialized camp disable component found | Half-resolution horizontal blur and full-resolution composite; screen-space sharp band, not physical focus depth |
| Tilt-shift controls | Centre 0.5, sharp width 0.05, feather 0.427, blur 3.64 | Narrow sharp band can soften silhouettes and contacts outside the centre; compare readability at all stations |
| Anamorphic streak | Enabled, quarter resolution, intensity 0.18, threshold 0.4, orientation −89°, length 0, color separation 0.095 | Extraction plus composite still run; zero length selects a minimum 2-texel reach rather than disabling the feature |
| Round flare | Enabled, intensity 0.34; separate core/aureole/ghost occlusion | Skips when the sun is behind camera; otherwise records a 1 by 1 visibility history and full-resolution composite |
| Custom sun bloom | Enabled, half-resolution start, 6 levels, intensity 0.38, threshold 0.82 | 1 extract, 5 downsample, 5 upsample and 1 full-resolution composite: 12 logical passes |
| Bloom aureole | Intensity 0.62, radius 0.16 | Seeded from projected physical light without depth occlusion; compare glow through silhouettes for readability |
| Built-in Volume effects | Default profile has bloom intensity 0, tonemapping None, DOF off, motion blur intensity 0, neutral color adjustments | Active component flags alone do not mean these effects are visibly applied |
| Camera antialiasing | None; pipeline MSAA 1 | Camera AllowMSAA does not enable multisampling; thin rails, rocks and moving silhouettes deserve a stability comparison |

The Dust Atmosphere Volume is global but has no shared profile. URP's [global settings](../../../Assets/_Project/Settings/Rendering/URP/UniversalRenderPipelineGlobalSettings.asset) instead reference [DefaultVolumeProfile](../../../Assets/_Project/Settings/Profiles/DefaultVolumeProfile.asset); Unity documents this project default separately from scene Volumes in its [URP graphics settings](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/urp-global-settings.html).

Custom streak, tilt-shift, flare and bloom guards do not use the camera's `renderPostProcessing` flag. Moreover, the rig forces that flag on during play. Disabling only the camera checkbox is therefore an invalid way to measure the cost of this complete stack. Streak, tilt-shift and bloom alone request 16 logical passes in total, before SSAO, optional flare or range blending. This is a structural count, not a GPU timing: pass resolution, texture format, graph execution and bandwidth determine actual cost.

Tilt-shift, streak, flare and bloom run before built-in post-processing, in their renderer-list order. Streak runs before the round flare; it does not streak that flare's newly generated highlights. Bloom runs after the flare and can spread its energy. Its persistent aureole and the flare aureole overlap by design, so tune their combined contribution before increasing either independently.

All four optics features preview in Scene View; dust/range blending targets base Game cameras. Scene and Game views therefore need not match, and concurrent Scene rendering can inflate Editor timing. Quality levels all reference the same URP asset, so a label such as Very Low does not independently lower this custom stack or its 4096 px shadows. Quality settings still affect mip limits, LOD and skinning.

The automatic [performance profile](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlaytestPerformanceProfile.cs) defaults to full-content telemetry in Editor/Development builds. An explicit stress preference/argument changes render scale to 0.5, shadows to one 1024 px cascade at 20 m, and texture mip limit to 2. Never compare that result with full-content rendering without recording the mode. Project frame-timing statistics are disabled, so telemetry's CPU/GPU values must be checked for availability; zero is not proof of zero cost.

## Priorities for the next lighting and camera work

1. **Establish local comparison ownership.** Preserve current shared renderer settings and the dirty camp scene. Record sun phase, view-distance preference, aspect ratio, resolution, quality and performance-profile mode with every image or timing sample.
2. **Separate sun movement from the landscape.** Keep the low-angle light cycle and physical sun alignment, but hold distant ridge geography fixed. Resolve below-horizon mapping and runtime/Edit Mode sky parity before judging sky color.
3. **Compare a clear lighting baseline.** Start with sun, ambient, terrain and contact shadows. Add SSAO, tilt-shift, streak, flare and bloom individually. Compare the current narrow blur band and nearly vertical minimum-length streak with disabled versions before redesigning them.
4. **Resolve camera clearance.** Inspect station cover, the companion, nearby rocks, maximum scouting pan and reset positions. Handle the minimum-distance and saturated-query cases before spending time smoothing a camera that cannot guarantee clearance.
5. **Measure the accepted image.** Use matched fixed camera poses at pitches 26°, 50° and 65°, toward and away from the sun, at bright and deep twilight. Include camp-screen shadows, salvage metal, station rails and a terrain boundary. Then use target-hardware CPU/GPU profiling and effect A/B timings; Unity's [URP profiling guidance](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/analyze-your-project.html) identifies separate shadow and post-process markers. Record frame time distributions and graph pass timings, not just FPS.

Future render captures can be static and background-safe in an isolated candidate with graphics; a nographics run cannot accept this visual work. Hands-on follow/orbit/obstruction acceptance remains user-owned. This audit does not launch Play Mode, run gameplay smoke tests or set a performance target on the user's behalf.

## World identity and implementation boundaries

The terrain remains fixed and always loaded. Camera clipping, draw-distance blending and optical passes affect presentation; they must not unload terrain, change collision or reactivate generation. The inspected production terrain-rendering window explicitly targets Greater Wasteland's path and does not make this camp slice a terrain-streaming system.

This documentation introduces no generated objects or runtime deltas. Existing deterministic dust uses its serialized seed only if enabled; future tuning should preserve that identity unless deliberately versioned. Stable generated-object identity and save migration are not applicable to this audit because it creates no gameplay content and writes no saved-world state. A future sky/camera profile should remain presentation configuration, while any changes to sun timing used by survival or camp shade require their own gameplay contract checks.
