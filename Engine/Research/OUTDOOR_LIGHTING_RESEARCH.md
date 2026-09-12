# Outdoor lighting research

Researched 2026-09-11. **Research complete; implementation proposed.** This is a focused extension of the existing sun/material foundation, supporting the open Greater Wasteland and rock workbench. It does not reopen the engine stack or require moving development off Mac.

Start implementation from the [audited sequence](../Docs/OUTDOOR_LIGHTING_IMPLEMENTATION_PLAN.md). The [source and cost record](./outdoor-lighting-data.json) preserves URLs, audited source hashes, allocation assumptions and upstream measurements. Current runtime proof remains in [STATUS](../Docs/STATUS.md).

## 1. What our renderer actually has

Audited at repository baseline `5b8c6a20f62c2e4d50303b8fd0be40eb4f1fd52c`; relevant file hashes are recorded separately so concurrent geology work does not invalidate this lighting audit.

| Area | Current source evidence | Consequence |
|---|---|---|
| Sun | Directional GGX/Smith/Schlick lighting; azimuth changes but vertical direction is fixed | Add elevation/color controls; present intensity as relative until calibrated |
| Shadows | One 2048² R32F + D24S8 map; orthographic bounds ±12 m, manual 3×3 PCF | Useful locally; receivers outside the 24 m footprint become unshadowed |
| Ambient | Fixed sky/ground hemisphere, texture AO multiplying ambient | No environmental visibility or actual sky/reflection lighting |
| Scene target | RGBA16F color + D24S8; forward shading combines direct and ambient immediately | Scene AO must be ready before forward lighting, not multiplied into final scene color |
| Display | Exposure, sRGB encoding, then hard clamp | Bright colors lose highlight detail; a tone curve is missing |
| Atmosphere | Flat clear color; no fog or atmosphere pass | No coherent horizon or aerial perspective |
| Effect inputs | No dedicated depth/normal effect pipeline, motion vectors or TAA | AO/contact shadows need shared inputs and spatial filtering first |
| Bindings | Shadow slot 0, material slots 1–3, ten rock-layer slots 4–13 | Multiple shadow textures plus separate effects would exhaust a 16-slot path |

Evidence: [Renderer.cpp](../Source/Rendering/Renderer.cpp), [scene shader](../Shaders/fs_scene.sc), [display shader](../Shaders/fs_display.sc), [view allocation](../Source/Rendering/RenderViews.h). Shadows, AO and fog cannot repair poor silhouettes, unsupported rock placement or repetitive material masks. Those remain separate findings in the [Unity methods audit](./UnityReference/ENVIRONMENT_METHODS_AUDIT.md).

## 2. Research and decisions

### Stable sun shadows

Microsoft's CSM guidance divides the camera frustum into ranges, allocates near precision, stabilizes projection bounds, snaps movement to shadow texels and blends transitions. It also explains why near-parallel light/view directions complicate fitting. Filtering must compare individual depths rather than blur depth and compare once. [Microsoft CSM](https://learn.microsoft.com/en-us/windows/win32/dxtecharts/cascaded-shadow-maps)

Bias balances self-shadow acne against detached shadows; projection fitting and resolution affect that balance. [Microsoft shadow artifacts](https://learn.microsoft.com/en-us/windows/win32/dxtecharts/common-techniques-to-improve-shadow-depth-maps)

**Engine decision:** begin with two stabilized cascades in one atlas, preserving the current portable color-depth comparison path. Use fixed split policy per quality preset, a transition band and texel snapping. Start evaluating a 24 m near split and 128 m shadow distance; these are camera-depth distances, not tile footprints or promises of sufficient coverage. Expose cascade coloring and valid coverage. Clamp PCF taps within each tile, including its guard border. Fade the final range deliberately.

Use the pinned bgfx `16-shadowmaps` implementation for API conventions, not as an unexamined copy of its split defaults. Include off-camera casters capable of shading visible receivers. Current nearby-caster selection needs revision; simply enlarging projection bounds is insufficient. Keep shadow range within demonstrably resident caster coverage.

### Sky, exposure and environment illumination

Filament separates material response, environment lighting, indirect occlusion and display processing. Its IBL treatment includes diffuse irradiance and roughness-dependent specular illumination; this is substantially more than assigning a blue ambient color. [Filament technical guide](https://google.github.io/filament/main/filament.html)

Khronos PBR Neutral is designed for material color fidelity under controlled lighting. Its shader accepts and returns linear Rec.709, with output bounded for SDR; sRGB encoding is a separate operation. [Khronos overview](https://github.com/KhronosGroup/ToneMapping), [shader contract](https://github.com/KhronosGroup/ToneMapping/blob/main/PBR_Neutral/pbrNeutral.glsl)

**Engine decision:** use a simple authored sky gradient with a sun direction shared by the surface light. Derive diffuse ambient colors from the same environment settings. This is an approximation, not global illumination. Add PBR Neutral as the initial named SDR tone operator, fixed exposure and a linear diagnostic bypass. Order: lighting → fog → exposure → tone map → sRGB → UI. Preserve texture-preview and normal diagnostic behavior explicitly.

A cooked environment cubemap with diffuse irradiance and prefiltered specular is the next lighting upgrade if metal/roughness references expose the ambient approximation. It is outside this first pass: it needs asset cooking, sampler planning and reference evidence. Do not add a second renderer or a full atmospheric simulation to obtain a visible sky. Before copying new upstream code, pin its revision and collect its applicable license/attribution; this research imports no shader code.

### Scene ambient occlusion and contact shadows

Intel XeGTAO uses depth, optional normals, depth prefiltering, occlusion evaluation and spatial denoising. Temporal noise depends on a temporal reconstruction path; without TAA, spatial filtering remains necessary. The repository is archived and Intel no longer maintains it. Its published RTX 2060 results are approximately 0.56 ms for XeGTAO High and 0.72 ms for ASSAO Medium at 1920×1080; these compare different presets in the author's workload, not our engine. [Intel XeGTAO](https://github.com/GameTechDev/XeGTAO/blob/master/README.md)

**Engine decision:** adapt the already pinned bgfx `39-assao` compute path first, using one fixed quality mode and spatial filtering. It already expresses resource/shader operations in our graphics abstraction. This lowers integration work but does not prove performance or visual quality. Keep XeGTAO as a method/comparison reference; undertake a new HLSL-to-bgfx port only if the first result has an identified deficiency. Do not import the example's whole deferred renderer or adaptive quality machinery merely to acquire AO.

Provide geometric view normals and depth from the same static/skinned geometry and transforms used for visible shading. Agree on linear-depth versus hardware-depth decoding at the adapter boundary; the example's input convention cannot consume a different encoding unchanged. Keep AO radius in meters and use edge-aware filtering/upsampling. AO belongs on indirect illumination; texture AO and scene AO need a bounded combination with separate toggles to avoid doubled dark creases. Begin with `min(materialAO, sceneAO)` as an explicitly artistic conservative combination, not a physical identity. Revisit directional/specular occlusion when true IBL exists.

Contact shadows are short screen-space rays toward the sun, complementing the map at rock/ground contacts. Pinned bgfx `44-sss` supplies a reference. Their visibility affects direct sun, never ambient or fog. Limit ray length in meters, account for thickness, fade at screen edges and use unoccluded fallback for missing depth. They cannot see off-screen occluders. Add only after shared depth/AO works and remaining gaps justify them.

### Fog and distance

Epic describes exponential height fog as denser at low elevations, with height falloff, distance and directional color controls; volumetric fog is a separate density/light evaluation system. This supports distinguishing basic aerial perspective from light shafts. [Epic height fog](https://dev.epicgames.com/documentation/en-us/unreal-engine/exponential-height-fog-in-unreal-engine)

**Engine decision:** use one analytic height-fog layer, initially without volumetric shadows, animated noise or clouds. Expose extinction per meter, reference height, positive scale height and a color consistent with the sky. Apply once in linear HDR; evaluate the background with a deliberate sky-distance convention so it meets the horizon. Fog must not conceal missing region/caster coverage during debugging.

Own derivation for a ray of length `d`, start height `y0`, vertical unit direction `vY`, scale height `H` and reference extinction `sigma0` at `h0`:

```
k = vY / H
sigmaStart = sigma0 * exp(-(y0 - h0) / H)
tau = sigmaStart * (1 - exp(-k*d)) / k
T = exp(-tau)
out = T * litSurface + (1 - T) * fogColor
```

For near-horizontal rays use the limit `tau = sigmaStart*d` (or a stable series); zero distance/extinction returns the unchanged surface. Clamp valid parameter/exponent ranges to prevent overflow on downward rays. A fog start distance changes the integration origin and remaining length, not just the final blend factor. This is a single-color extinction model, not a complete multiple-scattering atmosphere.

## 3. Integration contract

Keep forward rendering. Expand named view IDs and make ordering explicit: shadow tiles → depth/normal prepass → AO/filter and optional contact mask → sky/forward HDR → display → inspector. A small owned pass sequence is sufficient; no general render-graph rewrite.

Use one shadow atlas in slot 0. Pack resolved AO/contact visibility into one RG texture at slot 14; neutral value is `(1,1)`. Existing surface slots remain 1–13. Query actual caps and supported formats rather than assuming 16 samplers; slot 15 alone is not sufficient for every future IBL implementation. Additional environment/BRDF resources require another binding audit.

A separate R32F linear-depth color attachment and RGBA8 encoded geometric normals avoid depending on portable sampling of the attached hardware depth. Reuse hardware depth across prepass/forward only with explicit single ownership; do not sample a texture while writing it, or let two owning framebuffers destroy a shared attachment. Respect bgfx depth-range and texture-origin conventions on Metal and D3D11. Allocate replacement effect targets transactionally on resize, validate compute/image formats and restore neutral masks when an optional effect is unavailable. Fixed sampling avoids requiring motion vectors/TAA in this pass.

## 4. Data, costs and unresolved proof

The [machine-readable record](./outdoor-lighting-data.json) contains nominal uncompressed allocation calculations, including the actual pinned ASSAO scratch layout. These are resource planning data, **not measured VRAM or GPU time**. They exclude driver alignment, transient overlap, geometry/textures, command buffers and debug targets.

| Resource | Nominal storage |
|---|---:|
| Current one 2048² shadow tile, R32F + D24S8 | 32 MiB |
| Proposed two 2048² tiles in 4096×2048 atlas | 64 MiB |
| Lower-cost two 1024² tiles in 2048×1024 atlas | 16 MiB |
| Existing 1080p HDR + hardware depth | 23.73 MiB |
| Proposed 1080p R32F depth + RGBA8 normals | 15.82 MiB |
| Proposed 1080p RG8 combined visibility | 3.96 MiB |
| Pinned ASSAO scratch layout at 1080p, including generated normals | 23.32 MiB |
| Same scratch layout at screenshot dimensions 3456×2234 | 86.82 MiB |

ASSAO also allocates deinterleaved depth mip chains, layered intermediate results, importance maps, normal generation storage and a resolved output. Counting only two half-resolution AO textures materially understates its example allocation. Adaptation may omit unused resources, but the savings must be recorded from implemented allocations. A three-tile configuration in a 2×2 2048 atlas costs 128 MiB with these formats, including the unused tile; do not report it as 96 MiB.

Measure actual drawable pixels, backend/GPU, quality and resident geometry. Retina dimensions can dominate screen-space cost. Use bgfx per-view GPU timestamps/frequency where available; CPU submission or frame wall time is not effect GPU duration. Delayed/unsupported timing is unavailable, never zero. Collect one bounded baseline/effect comparison at 1080p and native drawable size after implementation. PC hardware and a total frame budget remain open; upstream timings cannot close Windows proof.

Research is sufficient to begin the [bounded implementation](../Docs/OUTDOOR_LIGHTING_IMPLEMENTATION_PLAN.md). Deferred: ray-traced GI, shadowed volumetric fog, clouds/weather, auto exposure, general local-light clustering, reflection probes and temporal reconstruction. Reopen a deferred method when a representative scene demonstrates the missing capability, rather than treating a feature list as the milestone.
