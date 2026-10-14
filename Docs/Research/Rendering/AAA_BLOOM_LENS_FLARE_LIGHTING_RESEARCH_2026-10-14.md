# AAA bloom lens flare and lighting research

Research date: October 14, 2026. Research and proposed comparisons only; no scene, shader, setting or runtime changes.

For Camp Loop, the strongest starting combination is **temporally stable bloom informed by Call of Duty: Advanced Warfare, restrained authored sun flare informed by Crytek, and exposure/readability discipline informed by Ghost of Tsushima**. The Order: 1886 provides a useful alternative with fixed optical filters. These are recommendations for comparison, not acceptance of a new rendering design.

The study below uses developer presentations, firsthand developer explanations, engine documentation and optics research. It separates documented game work from engine capabilities and research prototypes. Publication dates identify historical techniques; they do not describe the current versions of those games or engines. The [source ledger](aaa-optics-sources-2026-10-14.json) records versions, locators, retrieval limitations and local inspection hashes.

## What each effect contributes

| Effect | Intended contribution | Camp implication |
| --- | --- | --- |
| Bloom or veiling glare | Spreads scene brightness over multiple scales | Emphasize bright reflections and emitters while keeping ground detail readable |
| Lens ghosts and halos | Structured artifacts from the camera's optical system | Choose one coherent lens character and a small set of elements |
| Aperture starburst or anamorphic streak | Directional optical character | A deliberate art choice; strength and orientation should be consistent |
| Atmospheric glow and shafts | Light scattered in the world between source, surface and camera | Dust and horizon haze need their own visibility, density and lighting treatment |
| Exposure and tonemapping | Fit scene brightness and color into the displayed image | Protect ochre sky saturation, yellow sun color and shaded gameplay visibility |

Crytek explicitly distinguishes its screen-space shaft element from lens flare, and documents the offscreen limitation of that particular element. The physical lens paper models optical inter-reflections rather than world atmosphere. Treating these as separate controls makes the proposed comparison easier to judge. [Crytek optical system](https://www.cryengine.com/docs/static/engines/cryengine-5/categories/23756816/pages/36868680), [Hullin et al. paper](https://resources.mpi-inf.mpg.de/lensflareRendering/pdf/flare.pdf).

## Documented game approaches

### Call of Duty Advanced Warfare

Jorge Jimenez's SIGGRAPH 2014 presentation describes a development pipeline for Sledgehammer Games; the talk identifies its images as development builds. In the author-hosted **v18** deck, slide **145** uses non-thresholded HDR color for bloom/veil. Slides **146–158** address resampling instability with a custom **13-bilinear-fetch downsample**; **161** progressively upsamples and accumulates using a **3 × 3 tent filter**. **167–168** discuss partial Karis averaging for the first downsample to control bright subpixel fireflies. **170** summarizes six R11G11B10 mips. This is more specific than simply adding more blur levels.

Slide **137** reports veil at **0.35 ms on Xbox One**, under the table's “Fixed” label; the inspected table does not establish resolution or a transferable hardware budget. For us, the useful lesson is stable filtering during motion and small highlights, with exposure and energy handling considered before strength. [Author's explanation](https://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare/), [original v18 deck](https://www.iryoku.com/downloads/Next-Generation-Post-Processing-in-Call-of-Duty-Advanced-Warfare-v18.pptx).

### The Order 1886

Matt Pettineo's firsthand September 2015 explanation gives a concrete production chain: half-resolution bloom with a separable **21 × 21 Gaussian compute filter**, and quarter-resolution lens flares using custom shape filters. They initially used FFT convolution for artist-defined textures, then changed to fixed kernels because the project used one fixed shape set. Bloom and flare compositing joined the tonemapping stage.

His follow-up describes manual or simple geometric-mean auto exposure with clamps and temporal adaptation. He also reports dissatisfaction with its middle-gray behavior and the amount of exposure hand-tuning. This is a useful warning against copying a praised game's entire camera pipeline without assessing its working limitations. [Developer explanation and follow-up](https://gamedev.net/forums/topic/671939-deconstructing-post-process-of-the-order-1886/).

### Crysis 3 and Crytek's optical system

Crytek's August 2012 Crysis 3 announcement explicitly showcases composite 3D lens flares and procedural HDR flare shapes. It establishes the visual feature association, not a detailed shipped algorithm. Later CRYENGINE V documentation describes authored, light-associated optical elements: ghosts, multiple ghosts, orbs, shafts and glare. Its occlusion-interaction control lets supported elements respond to partial blocking; ghosts/orbs can show an eclipse-like change, while iris shafts redistribute toward unblocked regions. Ghost geometry is fitted to texture dimensions to reduce wasted bandwidth; camera orbs can create substantial overdraw.

This supports a modular sun flare with independent element intensity, shape and visibility. It does not prove that the later documented system exactly matches Crysis 3's release implementation, or establish a temporal-history policy. [Crysis 3 feature announcement](https://www.crytek.com/news/feast-your-eyes-on-a-new-cryengine-3-tech-trailer), [optical system documentation](https://www.cryengine.com/docs/static/engines/cryengine-5/categories/23756816/pages/36868680).

### Killzone Shadow Fall

Michal Valient's 2013 demo postmortem lists artist-driven flares alongside exposure control, bloom, screen-space godrays and other post effects. This confirms an authored optical layer in the documented demo. It does not disclose a flare kernel, visibility/history algorithm or isolated flare timing. The widely repeated description of Killzone 2 as mirrored bloom was found in outside reverse engineering and is not treated as Guerrilla-confirmed implementation. [Developer postmortem, “Post processing” slide](https://www.guerrilla-games.com/media/News/Files/Valient_Killzone_Shadow_Fall_Demo_Postmortem.pdf).

### Destiny

Natalya Tatarchuk's GDC 2015 rendering presentation shows a light-shaft occlusion buffer, then lens flares and the completed frame, on PDF pages **155–157**. Page **158** identifies distinct paths for first-person, third-person and cinematics. That is useful evidence for camera-specific presentation and visibility work; the inspected sequence supplies no bloom filter, flare temporal rule or isolated GPU cost. [Developer presentation](https://advances.realtimerendering.com/destiny/gdc_2015/Tatarchuk_GDC_2015__Destiny_Renderer_web.pdf).

### Mirror's Edge Catalyst

Fabien Christin's GDC 2016 *Lighting the City of Glass* connects physical camera exposure, photographed HDR references and art-directed atmosphere. Slides **46–47** animate sun flare, bloom, exposure and disc size with the time-of-day system. Slides **24–34** explain why photographed skies were unsuitable for that project's dynamic sky: memory, visible blending and the need to relight clouds. The presented sky/cloud solution cost approximately **0.75 ms on consoles**, with the talk's work-in-progress context.

For our short twilight cycle, a single approved artistic panorama remains a reasonable bounded study; the useful lesson is to coordinate sun, atmosphere and camera presentation rather than tune each in isolation. The talk does not require replacing our sky with a full dynamic cloud system. [EA/DICE's presentation, slides 14, 22–34 and 43–47](https://www.slideshare.net/slideshow/lighting-the-city-of-glass/60383382).

### Ghost of Tsushima

Jasmin Patry's SIGGRAPH 2021 talk is especially relevant to saturated skies and readable terrain. Slides **129–130** describe primarily illuminance-based exposure, with a highlight-luminance exception, to avoid an exposure/albedo feedback loop and preserve sky color while keeping dim areas visible. **135** describes a bilateral grid for detail-preserving contrast adjustment, with approximately **0.25 ms overhead at 1080p on PS4**. Subsequent slides discuss ringing and halo tradeoffs.

Slides **102–105** separate temporal filtering of haze lighting from analytic per-pixel opacity, helping horizon and thin-layer stability. These are lighting, atmosphere and tonemapping techniques; the talk does not establish a Ghost of Tsushima bloom or lens-flare algorithm. [Developer talk and notes](https://advances.realtimerendering.com/s2021/jpatry_advances2021/index.html), [PDF slides](https://www.glowybits.com/talks/real-time_samurai_cinema/real-time_samurai_cinema.pdf).

### Horizon Zero Dawn and Horizon Forbidden West

Andrew Schneider's *Nubis, Evolved* discusses dynamic volumetric skyboxes in Zero Dawn, reported under **2 ms on PS4**, and later close clouds, fast storms and temporal artifacts in Forbidden West. It demonstrates that atmosphere has its own substantial rendering and stability problems. This is a cloud-rendering reference, not evidence for either game's bloom kernel or lens-flare implementation. For Camp Loop, cloud simulation should remain a separate decision from modest optical improvements. [Guerrilla's 2022 publication](https://www.guerrilla-games.com/read/nubis-evolved).

## Supporting technical references

Sébastien Lagarde and Charles de Rousiers' *Moving Frostbite to Physically Based Rendering*, **v3.2**, supplies the exposure foundation. Section **5.1**, particularly printed pages **84–87** / PDF pages **85–88**, relates lighting, camera exposure and display transformation. It documents histogram-based auto exposure and exposure-compensated emissive controls. Those controls can deliberately keep an artistic emitter blooming across exposure changes; they should be an explicit choice. This engine guidance is not a claim that every Frostbite game uses identical settings. [Author-hosted course notes](https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf).

Hullin, Eisemann, Seidel and Lee's SIGGRAPH 2011 lens-flare paper supplies a physical reference: rays through a lens system produce ghosts, with bounding/precomputation and selective reflection orders used to make rendering tractable. It is useful for choosing consistent flare shapes as light direction changes. No shipped-game adoption is established here, and a physical lens simulation is not the first recommended implementation for this project. [Research paper](https://resources.mpi-inf.mpg.de/lensflareRendering/pdf/flare.pdf).

Epic's **UE 4.27** image-based flare documentation offers a separate cost model: more threshold-passing pixels increase fill cost, and doubling bokeh radius quadruples fill area. An image-based source also cannot recover a sun absent from the source image without another mechanism—an inference from its input. This is engine documentation, not attribution to a particular AAA title. [Epic lens-flare documentation](https://dev.epicgames.com/documentation/unreal-engine/lens-flare?application_version=4.27).

## Gaps in the current camp study

These findings come from local source inspection, not a new render test. The [installed study](../../Evidence/Rendering/CampAtmosphere20261014/README.md) remains unchanged.

| Current behavior | Research implication and proposed check |
| --- | --- |
| Bloom uses a soft threshold, nine-fetch downsample, tent upsample and four study levels | The architecture already has a pyramid. Compare filtering and first-stage anti-firefly handling before increasing level count. CoD's non-thresholded approach is a separate experiment, not a drop-in intensity preset. |
| `PersistentSunGlow` injects a projected halo with frame-edge fading but no depth visibility input | If the sun approaches the frame, an occluder can fail to suppress this synthetic contribution. Compare a visible-source bloom path and a separately occluded authored halo. Ordinary bloom spreading a visible highlight across a silhouette is a different effect. |
| Flare `DepthVisibility` returns visible for offscreen sample positions | Offscreen depth absence is not proof that the sun is unblocked. Establish an angular fade/visibility policy before enabling an offscreen ghost chain. |
| Flare occlusion uses a fixed history blend coefficient | Evaluate response at several frame rates and on camera cuts; a blend coefficient alone is not a measured, frame-rate-independent fade time. Existing camera-cut invalidation remains useful. |
| Study bloom applies a warm composite tint to the entire extracted buffer | Check whether colored emitters and neutral material highlights retain their intended color. Compare neutral scene bloom with independently yellow sun optics. |
| Default Volume has built-in bloom intensity zero and tonemapping None | Compare a deliberate exposure/display transform before increasing HDR sun energy; keep shared production settings intact. |
| Generated sky/reflection is art-derived rather than calibrated radiance | Choose an exposure baseline deliberately. Raising sky texture exposure alone does not establish physically correct sun/sky illumination or emitter brightness. |

The ordinary camera has vertical FOV 48° and a downward pitch range of 26–65°. At the shallowest pitch, the upper central ray is approximately 2° below the horizon, before roll or other changes. The positive-elevation sun therefore often stays outside gameplay framing. A camera can plausibly receive stray light from outside the visible image, but that requires a chosen lens/visibility policy. Constant strong solar ghosts are not implied by the documented techniques.

The installed **URP 17.4.0** bloom shader already contains a **13-sample high-quality prefilter**, threshold/knee handling, and separable Gaussian pyramid passes. This is verified in the pinned local package, not assumed from current online documentation. It is not identical to CoD's complete non-thresholded/partial-Karis method. A camp-only built-in bloom comparison is therefore a useful baseline before maintaining another custom filter; disable custom bloom in that comparison so two bloom pipelines do not accumulate. No package or shared profile change is needed for this research conclusion.

## Recommended bounded comparisons

The following is a research-derived experiment order, not implementation authorization or accepted creative direction:

1. **Establish lighting and exposure.** Retain Clearer twilight and the current sun clock. Compare ground, shadow and metal readability at the bright/deep phases with optics disabled. Keep yellow solar color and ochre sky saturation as explicit acceptance criteria. Use consistent material reference swatches so changing albedo does not mask exposure problems.
2. **Compare bloom alone.** Current pyramid versus pinned URP's high-quality bloom in a private camp comparison, followed by a CoD-informed first-downsample/filter variant and a low-strength non-thresholded variant if needed. Preserve HDR processing before display tonemapping and track highlight energy, not just image softness. A reduced-resolution fixed Gaussian alternative can follow if its look differs materially. Avoid introducing local tonemapping and new bloom simultaneously.
3. **Compare a minimal sun flare.** Start with a tight yellow core/aureole and at most one subdued ghost or ring. Establish sun visibility, frame-edge fade, partial occlusion and consistent angular scale. Use world dust for atmosphere; add lens dirt only if a deliberate camera/visor interpretation is accepted.
4. **Assess lens character.** A restrained spherical lens is the first recommendation. Anamorphic streaks, starbursts and stronger ghosts are optional art comparisons; one coherent lens style should determine their shape and movement. Fixed authored shapes offer a simpler initial path than runtime FFT or lens-system ray tracing.
5. **Separate atmosphere work.** Horizon and Ghost of Tsushima motivate stable haze and sky lighting, but neither establishes a need for a full cloud renderer here. Compare terrain-edge haze separately so an optical halo cannot hide a geometry or atmospheric-depth problem.

For each comparison, keep camera transform/FOV, sun phase, exposure, resolution and scene content matched. Include bright subpixel metal highlights, a visible source crossing a rock or canopy, partial source blockage, frame-edge entry/exit, the existing gameplay pitches, slow orbit, abrupt reset and movement. For offscreen cases, inspect the chosen visibility fallback explicitly. These are proposed future fixtures and author-owned motion checks; none was executed in this research task.

Measure CPU and GPU separately in a Development Player at named settings. Record pass time, transient targets, bandwidth/overdraw, stable-frame percentiles and total frame time; exercise both a quiet camp and many bright emitters. Half/quarter resolution reduces pixel count but does not guarantee a cost proportional to that count. Historical Xbox One/PS4 numbers are context, not budgets for URP on the current RTX 3060 Ti. The previous static Editor submission timing cannot establish Player performance.

The first implementation candidate, if requested, should address **halo visibility and bloom stability**, then test exposure/color preservation. Full physical lens simulation, FFT flare convolution, automatic exposure and local tonemapping remain separate choices with distinct acceptance and cost checks. No world identity, terrain streaming, generated-object identity or saved-runtime delta changes are implied by these camera-presentation proposals.
