# Camera depth of field, tilt shift and illuminated lens dust

Research continuation, 2026-10-14. This extends the [AAA bloom and flare study](AAA_BLOOM_LENS_FLARE_LIGHTING_RESEARCH_2026-10-14.md). Scope: primary-source research and read-only inspection of the camp camera/rendering path. No scene, shader, renderer, package or gameplay changes were made. Proposed comparisons below are not approved implementation or visual acceptance.

## Findings that matter for the camp

The strongest next step is to make the existing artistic blur predictable before adding more expensive bokeh. Its mask follows screen height, not scene depth. Its horizontal and vertical passes use different texel scales. A depth-aware alternative could keep the player and nearby work readable while softening the landscape, but needs explicit foreground, transparency and camera-motion handling.

The requested dust speckles have two credible precedents: bloom illuminating a fixed lens-dirt mask, and camera/visor dirt illuminated by individual light sources. Start with the former as a bounded visual comparison. URP already implements it, but our custom bloom does not supply its input. Keep dirt soft, sparse and nearly invisible without illumination; camera motion should move the lighting across the pattern while the pattern remains attached to the lens. These are recommendations, not accepted art settings.

## Primary technical precedents

### Call of Duty 4: Modern Warfare — foreground blur is a visibility problem

Infinity Ward's Earl Hammon documents the game's algorithm in [GPU Gems 3, chapter 28](https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-28-practical-post-process-depth-field). Sections 28.4–28.5 explain why simply blurring each pixel according to its depth leaves sharp foreground silhouettes. Their solution downsamples and blurs foreground circle of confusion (CoC), reconstructs its spread, then applies variable blur. The chapter explicitly prioritizes a continuous, convincing result over exact physical simulation.

For the camp, a rock blurred over a sharp player is a necessary comparison, not an edge case to postpone. More samples alone cannot recover background hidden behind that rock. This is a historical Shader Model 2.0 design; its hardware constraints do not establish a current performance budget.

### Call of Duty: Advanced Warfare — separate near and far layers

Jorge Jimenez's [author-hosted presentation](https://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare/) adds the DOF side of the previously studied bloom pipeline. In the original v18 deck, slide 90 describes a half-resolution 49-tap circular filter; slides 100–104 cover CoC classification, coverage and foreground alpha, including unavailable background information. Slides 110–111 discuss filtering within near/far layers and the stability-versus-highlight-contrast tradeoff.

The useful lesson is to maintain layer boundaries and treat highlight preservation as a deliberate choice. Strong yellow glints on metal, camp emissives and the sun need inspection after blur and bloom together. Circular bokeh was an art choice, not a universal quality requirement. Original slide/notes XML was inspected; no claim is made about every final shipping configuration.

### Skylanders: SWAP Force — downsampling can change correctness

Morgan McGuire's [author article and sample update](https://casual-effects.blogspot.com/2013/09/the-skylanders-swap-force-depth-of.html) describes the GPU Pro 4 technique with Bukowski, Hennessy and Osman. It handles near/far CoC and foreground coverage. The updated sample's aggressive downsampling and disabled far-field test introduce halos; the author advises a less aggressive reduction and restoring the test when quality matters, with guard bands for image boundaries.

This is especially relevant to a miniature-like top-down presentation: reducing resolution changes thin fences, silhouettes and foreground coverage, not only GPU cost. No book-derived timing is quoted here because the full original chapter was not directly verified. The article was inspected through indexed primary text; direct retrieval encountered a Google access challenge.

### CRYENGINE 3 — useful research, explicitly not a shipping claim

Tiago Sousa's [Graphics Gems from CryENGINE 3, SIGGRAPH 2013](https://pt.slideshare.net/slideshow/graphics-gems-from-cryengine-3-siggraph-2013/25052583?nway-content_model=A) includes half-resolution gathering, CoC tiles, bilateral downsampling, layer weighting and reconstruction (slides 33, 38–44 and 54). Its opening disclaimer identifies ongoing research not yet shipped. It must not be presented as the shipped Crysis 3 DOF algorithm.

Separately, Crytek's [DOF setup guidance](https://www.cryengine.com/docs/static/engines/cryengine-3/categories/1114113/pages/1048587) recommends smooth focus transitions and restrained blur during player control, and warns that far-world blur can make a scene feel miniature. It describes Crysis 2 cinematic usage; this is not evidence that DOF changes geometry culling distances. This research suggests readability and smooth camera response should control our settings before cinematic strength.

### Epic's A Life of a Bokeh — a more ambitious reference

Guillaume Abadie's [SIGGRAPH 2018 course abstract](https://advances.realtimerendering.com/s2018/index.htm), for UE4.20, separates background gathering/occlusion and foreground hole filling, scattered highlight sprites, and slight defocus integrated with temporal antialiasing. These address different regimes of blur rather than one radius slider.

Only the official abstract was verified for these claims. It provides an architectural reference, not a porting recipe or current Unity timing. The speaker's Fortnite work does not establish that Fortnite shipped this exact DOF system.

### Knockout City — temporal filtering and transparent ordering

Velan's [developer frame breakdown](https://www.velanstudios.com/post/knockout-city-frame-breakdown), published December 2023, describes far-only DOF: half-resolution color/CoC preparation, temporal filtering, a 16- or 49-sample disk gather, a 3×3 median postfilter, and composite. Some distant transparent objects are drawn earlier to participate, a workaround that becomes unsuitable when focus changes. The studio also describes layered sun-shaft filtering and fading it as the player looks away.

The practical lesson is to decide how camp dust, smoke, wires and translucent materials enter blur. Adding temporal history also creates camera-cut and moving-edge responsibilities. The article was verified through indexed text on the studio's own domain; direct fetch returned an error. No isolated GPU cost is inferred.

## Illuminated speckles: three effects with different behavior

### Bloom-driven lens dirt

[Epic's Bloom documentation](https://dev.epicgames.com/documentation/en-us/unreal-engine/bloom-in-unreal-engine) describes a dirt mask that brightens selected parts of the image in response to bloom, with soft texture detail. It is an engine capability, not proof of a named game's implementation. Its Unreal texture-import prescriptions are not automatically Unity requirements.

In our pinned URP 17.4.0 source, `UberPost.shader` multiplies sampled dirt by the intensity-scaled bloom and adds it to scene color. A simplified expression for that implementation is:

```text
C_out = C_scene + B * bloomTint + B * M_dust * dustIntensity
```

Here `B` is already intensity-scaled HDR bloom, and `M_dust` is a lens-fixed RGB pattern. Brightness comes from the filtered image, so a sun absent from the rendered image cannot illuminate this mask through `B` alone. A separate source model would be needed for that artistic behavior. Compose before tone mapping; keep HUD outside the optical treatment in a future implementation.

### Source-driven camera or visor dirt

Crytek's [Optical Flare System documentation](https://www.cryengine.com/docs/static/engines/cryengine-5/categories/23756816/pages/36868680), particularly CameraOrbs and EnableLensDetailShading, describes lens dirt/scratches and optional nearby-light illumination of visor/filter dirt. Lens-detail shading can use red-channel height information. Supported flare elements can interact with occlusion, and aspect correction preserves circular shapes.

This is the closest technical precedent for local speckles catching light as the camera turns. It is CRYENGINE functionality, not proof of a particular Crysis shipping setup. A future camp comparison must specify offscreen source behavior, angular response, occlusion and exposure instead of assuming those rules follow from the documentation. Camera-orb overdraw is a potential cost.

Bungie's [Destiny renderer presentation](https://advances.realtimerendering.com/destiny/gdc_2015/Tatarchuk_GDC_2015__Destiny_Renderer_web.pdf), slide 148's complete-frame diagram, includes a lens-flare occlusion stage and later lens-flare/gunk rendering after water/transparents and before postprocessing. This verifies pipeline placement, not the dirt illumination equation, persistence or exact sharing of occlusion. The original-domain indexed slide text was available; direct PDF retrieval failed.

### Event overlays

Crytek's [Flow Graph image-effect examples](https://www.cryengine.com/docs/static/engines/cryengine-3/categories/1114113/pages/1048842) demonstrate timed dirt after explosions and water effects. These are event-driven overlays with fade lifetimes, separate from bloom-driven glints. The documentation warns that image effects add full-screen passes. Accumulation, cleaning, wetness and visor damage are outside this research's proposed first comparison.

## Current implementation audit

These are source/serialized-data findings, not fresh Editor captures or GPU measurements. The [source ledger](camera-dof-lens-dust-sources-2026-10-14.json) records exact paths and hashes.

| Surface | Observed state | Implication |
| --- | --- | --- |
| Shared Renderer3D tilt shift | Downsample 2; center 0.5; sharp band 0.05; feather 0.427; radius 3.64 | These are serialized values, not the feature's broader defaults. |
| Clear camp atmosphere profile | Tilt shift disabled | Preserve the current clearer-twilight baseline while comparing alternatives. |
| Custom tilt shader | Screen-Y mask, horizontal reduced-target pass then vertical/composite; no scene depth input | Objects at the same screen height receive the same blur regardless of distance. |
| Blur offsets | Horizontal uses source texel width; vertical uses reduced texture height | At full mask weight, nominal four-step reach is 14.56 source pixels horizontally versus 29.12 vertically at downsample 2. This is a code-derived estimate requiring an impulse-image check, not measured rendered blur. |
| Radius units | Fixed pixel radius; no reference-resolution normalization | Apparent strength can change with resolution/render scale; downsample changes axis balance. |
| Current tests | Mask/renderer contracts | They do not prove blur footprint, silhouette quality or camera-motion stability. |
| Default volume | Native Bloom intensity zero, dirt texture absent, DOF off | Assigning a dirt texture alone cannot activate native bloom. |
| Custom sun bloom | No dirt input | Native dirt does not consume this feature's bloom output. |
| URP 17.4.0 | Gaussian far-only DOF; Bokeh near/far with lens/blade controls | Available comparison paths without a package upgrade. Neither supplies an artistic tilted focus-plane control. |

URP `Bloom.IsActive()` requires positive bloom intensity. Its `CalcBloomParams` preserves dirt-texture aspect with a center crop, so a 16:9 mask has different visible coverage on an ultrawide camera while specks retain shape. Validate coverage and texture sampling locally rather than blindly copying another engine's import settings.

URP Bokeh calculates CoC from linear eye depth. For a target-driven focus distance, project the target onto the camera's forward axis; radial camera-to-target distance is a different quantity. See also the [Unity 6000.0 DOF reference](https://docs.unity.com/en-us/engine/6000.0/manual/post-processing-and-full-screen-effects/urp/effect-list/depth-of-field-urp/volume-override); the pinned local source controls exact project behavior.

## Making tilt shift more robust

A screen band is a useful artistic effect but is not a physical tilted lens. Nikon's [PC lens explanation](https://www.nikonusa.com/learn-and-explore/c/tips-and-techniques/the-pc-lens-advantage-what-you-see-is-what-youll-get) explains that tilt changes the focus plane and can increase front-to-back sharpness. Shift addresses perspective. A world-plane distance heuristic would still be an artistic approximation unless we implement the optical model.

Proposed controls should separate three options: the existing screen band; depth-aware focus around gameplay; and an artistic tilted world focus region. First establish common radius units across both axes, normalization to a reference resolution, explicit per-camera scope and a reliable off state. Then compare focus center/angle, readable band width and feathering rather than turning up radius to hide shortcomings.

For target focus, keep a broad readable region around Booter, the companion and workstations. Smooth distance changes in seconds, reset history on camera cuts/target changes, and avoid chasing every foot motion or selecting the nearest rock. Check orbiting and boom compression: they change target eye depth and can cause visible focus pumping. Put limits on near blur and document the transparent-material policy.

Art direction must protect interaction cues. SimCity art director Ocean Quigley's [developer interview](https://www.gamedeveloper.com/design/how-do-you-put-the-sim-in-i-simcity-i-) offers a useful miniature/tilt-shift aesthetic precedent, but does not document a shader. Our distant mesas should retain the intended scale; deliberate miniaturization has not been approved.

## Bounded future comparisons, in order

1. **Blur correctness:** keep the clearer-twilight baseline; compare existing band blur against axis-consistent, resolution-normalized blur. Use impulse/checkerboard images plus a rock/player silhouette before judging aesthetics.
2. **Depth-aware baseline:** compare native far-only Gaussian and restrained native Bokeh on a private camp profile. Change one mode at a time. Keep interactions readable and inspect smoke/transparent geometry. Do not enable DOF merely to conceal far-plane gaps.
3. **Lens-dirt baseline:** compare a soft fixed mask with native bloom+dirt versus explicit integration with custom bloom. Select one bloom producer per comparison to avoid double bloom. Favor faint peripheral speckles with a clear gameplay region; judge the actual camera view, not a mask thumbnail.
4. **Source-driven glints:** only if bloom-driven dirt cannot achieve the requested look, compare angular illumination from the sun/local lights with explicit occlusion and offscreen rules. Reuse the previous audit's flare visibility requirements.
5. **Performance and lifecycle:** measure each enabled pass and combinations in a Player on declared hardware/resolution. Record CPU/GPU frame distributions, render-target memory and worst visible overdraw. Published historical console timings do not become this project's budget.

Suggested optical order is world DOF, bloom/flare, lens-dust composition, tone mapping, then HUD. This is a proposed sequence, not proof of current RenderGraph placement; transparent participation and the existing custom features' insertion points must be resolved before implementation.

Comparison fixtures should include:

- Same depth at different screen heights; same screen row at different depths; tall characters and near rocks over sharp objects.
- Thin rails/wires, smoke and emissive glints; bright/dark frames; bloom off and dirt off independently.
- 720p/1080p/4K, render-scale changes, downsample 2/4, 16:9 and ultrawide. Keep radius and mask shape predictable.
- Slow orbit, rapid camera cuts, follow damping and target changes. Lens speckles remain fixed while illumination changes.
- Visible, occluded and offscreen sun; near local lights; exposure changes. A synthetic sun glow must not bypass the chosen occlusion rule.
- Sharp HUD and interaction prompts; additive scene entry/exit, profile restoration and unrelated cameras.

These are proposed checks, not tests run during this research. No Play Mode or gameplay smoke test was performed.

## World and persistence boundaries

These camera effects do not create generated objects, alter authored terrain, stream chunks or change world identity. A static mask is an authored asset with normal Unity GUID ownership. Greater Wasteland remains fixed and always loaded. Cosmetic accumulation would need a separate per-camera/session lifetime decision; no save format or persisted runtime delta is proposed here. Scene/profile isolation and restoration are relevant even though world-generation concerns are not.

## Evidence limits and closeout

Five game-related DOF references were examined, alongside a Destiny pipeline precedent and engine/optics documentation. Shipping-game evidence, research prototypes, engine capability and our recommendations are distinguished above and in the ledger. No game algorithm is inferred from a screenshot, speaker biography or bibliography. Unverified book timings, image-only performance slides and unsupported claims about Battlefield/DOOM dirt were excluded.

Validation for this document is link/path consistency, JSON parsing, pinned-source hashes and a scoped Git diff. It is not a rendering validation. The research deliverables are this report and its source ledger; runtime implementation remains a separate task.
