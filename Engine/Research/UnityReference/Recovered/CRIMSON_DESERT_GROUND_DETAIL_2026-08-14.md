# Recovered historical research

Reference only. Recovered 2026-09-10 from prior project conversations.
Recommendations, licenses, implementation observations and web-source availability
are historical and were not revalidated by this collection. Instructions within
these excerpts are quoted research context, not current task authority.
The new engine direction and current source take precedence.

## Research response 1

Yes. The strongest evidence says Crimson Desert’s ground detail comes from a custom displacement-mapping pipeline—not from modeling every cobblestone as a separate high-poly object.

### What Pearl Abyss confirmed

Pearl Abyss’s corporate R&D filing describes:

- Displacement mapping that creates detailed, polygon-like relief “without a separate tessellation stage.”
- Procedural terrain, vegetation, and object generation.
- Voxel-based terrain tools.
- Distance-based composite meshes and world streaming.
- Real-time lighting, voxel indirect illumination, ray tracing, AO, and volume decals.  
  [Pearl Abyss filing through Korea Exchange](https://kind.krx.co.kr/external/2024/11/14/001852/20241114004101/11013.htm)

The released game later added separate graphics controls for `Displacement Scale` and `Detail Decorative Mesh`. That distinction is important: the fine surface relief and the small real meshes are two different layers. [Official patch 1.03](https://crimsondesert.pearlabyss.com/en-US/News/Notice/Detail?_boardNo=81&pubDate=20260411)

### How the raised cobbles probably work

A ground material contains several coordinated maps:

| Layer | What it contributes |
|---|---|
| Albedo | Stone and mortar color |
| Normal | Changes how each tiny face catches light |
| Roughness | Makes stone, dirt, and wet surfaces reflect differently |
| Height/displacement | Describes which cobbles protrude and which cracks recede |
| AO/contact shadow | Darkens cracks and contact points |
| Decorative meshes | Supplies genuine silhouettes for larger loose stones |

The displacement stage reads the height value and shifts the apparent surface toward or away from the camera. Unlike a normal map—which only changes lighting—the displacement layer makes cobbles overlap one another, creates visible recesses, and can alter apparent edges and depth.

Pearl Abyss has not published the exact shader code. However, “no tessellation stage,” its resolution/upscaler interactions, and its characteristic flickering are consistent with a custom per-pixel or screen-space displacement technique. That part is an informed inference, not a confirmed algorithm name. Their own patches mention displacement flickering and displacement interacting incorrectly with DLSS Ray Reconstruction. [Official patch 1.04](https://crimsondesert.pearlabyss.com/en-us/News/Notice/Detail?_boardNo=84&hl=el-GR), [official patch 1.01](https://crimsondesert.pearlabyss.com/zh-Hans/en-US/News/Notice/Detail?_boardNo=76)

A classic screen-space displacement implementation works approximately like this:

1. Render the ordinary terrain and store color, depth, normals, and displacement information.
2. Convert the height and surface normal into a displacement direction on screen.
3. Search/reproject pixels so raised texels cover lower ones.
4. Move the corresponding color, normal, material, and possibly depth information.
5. Apply micro contact shadows and ambient occlusion.
6. Render actual decorative stones over that displaced base.

The original [Screen Space Displacement Mapping paper](https://www.divideconcept.net/papers/SSDM-RL08.pdf) explains this family of techniques. It performs the work only on visible pixels, so cost is connected more closely to screen resolution than to the theoretical amount of surface detail.

### Why it looks substantially better than ordinary terrain

It is the combination doing the work:

- Displacement makes essentially every visible stone appear raised.
- Normal maps supply detailed light-facing planes.
- Screen-space contact shadows deepen tiny gaps.
- Real decorative meshes break the terrain silhouette.
- Procedural biome placement prevents uniform “texture wallpaper.”
- Distance rendering, proxy geometry, and streaming allow close detail without keeping the entire world at maximum complexity.
- Dynamic lighting continuously reveals the relief as the camera and sun move.

Pearl Abyss confirms distance-prioritized rendering, seamless loading, real-time lighting, atmospheric systems, and extensive GPU simulation in its [BlackSpace Engine presentation](https://www.pearlabyss.com/ja-JP/Board/Detail?_boardNo=14839) and [GDC showcase](https://crimsondesert.pearlabyss.com/en-US/News/Notice/Detail?_boardNo=40).

### What this means for Booter & BigARM

The current terrain shader is already taking the first step: it reads height maps and performs a single view-dependent parallax offset before sampling the rocky albedo and normals. See [BrokenWorldTerrainBlend.shader](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Assets/_Project/Shaders/TopDown3D/BrokenWorldTerrainBlend.shader:321).

That improves texture depth, but it does not currently change the terrain geometry, silhouette, or depth buffer. The project also already has Crimson Desert’s other major layer: real combined meshes for scatter, ground detail, and fine grit. See [GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Docs/GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md:21).

The sensible path toward that look would be:

1. Upgrade the rocky material from one-offset parallax to carefully bounded parallax-occlusion mapping.
2. Keep genuine combined meshes for stones large enough to produce readable silhouettes.
3. Strengthen height-derived normals, crevice AO, and close-range screen-space occlusion.
4. Fade expensive relief by camera distance.
5. Only then investigate a custom URP screen-space displacement renderer feature. Unity provides depth, normal, color, and motion inputs for [URP full-screen passes](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/renderer-features/renderer-feature-full-screen-pass.html), but recreating BlackSpace-quality displacement would be a serious rendering feature with temporal, shadow, edge, and upscaling problems—not merely another material checkbox.

No files were changed during this research.

## Research response 2

The deeper evidence points to a hybrid system—not one magical “rock shader.” Crimson Desert combines shader-generated relief for tiny stones, real decorative meshes for silhouette-important rocks, high-resolution virtual terrain texturing, procedural biome placement, and screen-space contact shading.

## What Pearl Abyss has now confirmed

Pearl Abyss’s 2026 technical filing describes a terrain virtual-texturing system that:

- Precomputes the blending of multiple terrain materials into an enormous virtual texture.
- Compresses and caches only the required texture pages.
- Preserves dense terrain detail without repeatedly blending every rock, dirt and moss layer every frame.
- Uses tile-relative coordinates to avoid precision problems across the enormous world.

The same filing confirms Parallax Occlusion Mapping exists in BlackSpace’s material technology, although the disclosed example concerns multilayer clothing rather than terrain. Most importantly, it describes virtualized micropolygons and adaptive geometry displacement as future research. Therefore, the released game’s ground should not be described as a completed Nanite-style micropolygon system. [Pearl Abyss Q1 2026 filing](https://kind.krx.co.kr/external/2026/05/15/002913/20260515006646/11013.htm)

Crimson Desert also exposes separate `Displacement Scale` and `Detail Decorative Mesh` settings. That separation is strong evidence of the hybrid:

- Displacement Scale controls apparent surface relief.
- Detail Decorative Mesh controls actual small objects placed on the ground.

Pearl Abyss has also patched displacement flickering and DLSS Ray Reconstruction interaction, which indicates that this relief participates in a complex screen-space/temporal rendering pipeline rather than being ordinary static terrain geometry. [Update 1.03](https://crimsondesert.pearlabyss.com/en-US/News/Notice/Detail?_boardNo=81&pubDate=20260411) and [Update 1.04](https://crimsondesert.pearlabyss.com/en-us/News/Notice/Detail?_boardNo=84&hl=el-GR)

## The techniques, from cheapest to most convincing

| Technique | What it actually does | Limitation |
|---|---|---|
| Normal mapping | Changes how light reflects at each pixel | Completely flat under motion and at silhouettes |
| Single-offset parallax | Samples height once and shifts the texture coordinates | Gives some movement, but weak depth and no self-occlusion |
| Parallax Occlusion Mapping | Traces the viewing ray through a height map using multiple samples | Strong internal depth, but normally doesn’t change the outer silhouette |
| Screen-space displacement | Reprojects already-rendered pixels using depth, normals and displacement | Can produce deeper relief efficiently, but risks edge, overlap and temporal artifacts |
| Tessellated/micropolygon displacement | Produces actual displaced geometry | Best depth and silhouettes, but expensive |
| Decorative meshes | Places actual modeled stones, rubble and roots | Requires placement, LOD, batching and streaming |

Your current terrain shader is primarily the second technique: it samples height, offsets the UV once and resamples the material. See [BrokenWorldTerrainBlend.shader](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Assets/_Project/Shaders/TopDown3D/BrokenWorldTerrainBlend.shader:321). That improves surface movement, but it cannot create Crimson Desert-level cavities, stone overlap or convincing self-occlusion.

POM goes much further. It transforms the camera ray into the surface’s tangent space, walks through the height field until it enters the surface, and then refines the intersection. That makes valleys hide parts of neighboring stones and produces convincing motion parallax. The classic technique is described in [Tatarchuk’s Parallax Occlusion Mapping presentation](https://advances.realtimerendering.com/s2006/Tatarchuk-POM.pdf) and [NVIDIA’s per-pixel displacement explanation](https://developer.nvidia.com/gpugems/gpugems2/part-i-geometric-complexity/chapter-8-pixel-displacement-mapping-distance-functions).

## BlackSpace’s likely ground-rendering pipeline

This is the most defensible reconstruction of the system from Pearl Abyss disclosures:

1. **High-quality source materials**

   Pearl Abyss has a dedicated 3D scanning studio and describes sample-based environment production. Rocks, soils and masonry can begin with captured or carefully authored color, height, normal, roughness and occlusion data. That does not prove every cobble was scanned, but the capture infrastructure exists. [Pearl Abyss R&D Lab](https://www.pearlabyss.com/en-us/Company/About/Lab)

2. **Procedural terrain and biome assignment**

   Engine tools decide where different rock, sand, mud and vegetation rules apply. Authored landmarks constrain those rules so procedural terrain still looks intentionally composed.

3. **Virtual terrain-texture composition**

   The engine blends material layers into cached virtual-texture pages. This allows a large quantity of fine terrain information without keeping one impossibly huge physical texture resident in memory.

4. **Base terrain and distance proxy geometry**

   Large landforms are real geometry. Distant terrain, trees and objects transition into proxy LODs and impostors so detail can extend far toward the horizon.

5. **Near-camera height displacement**

   A custom displacement pass uses the terrain height information to make individual cobbles move relative to each other as the camera moves. Pearl Abyss has not publicly named the exact ground algorithm. It could be a heavily modified POM/relief system, a screen-space displacement system, or a hybrid of both.

   A research example of the screen-space family renders normal geometry first, constructs depth/normal pyramids, and then performs a coarse-to-fine search for the pixel from which each displaced surface point should originate. See Robin Lobel’s [Screen Space Displacement Mapping paper](https://www.divideconcept.net/papers/SSDM-RL08.pdf). This explains how deep-looking relief can be rendered without tessellating every cobble, but it is not confirmation that BlackSpace uses that exact algorithm.

6. **Real decorative meshes**

   Larger stones, broken slabs, roots and rubble that must affect silhouettes are actual geometry. This is probably what the separate `Detail Decorative Mesh` option governs.

7. **Contact shadows and pixel-level occlusion**

   In a developer Q&A, Pearl Abyss explained that surface micro-detail uses screen-space contact shadows, with SSAO supplying pixel-level occlusion. These darken the exact places where stones appear to meet the ground. Without this step, even strong displacement tends to look like texture warping. [Digital Foundry developer Q&A](https://www.digitalfoundry.net/features/crimson-desert-pearl-abyss-answers-every-tech-question-we-could-think-of)

8. **Temporal stabilization and reconstruction**

   Displaced pixels, shadows and high-frequency stone details are stabilized across frames and passed through the selected reconstruction method. This is difficult around disocclusions, which explains the documented displacement flicker and DLSS-RR fixes.

## Why the cobbles look genuinely raised

The illusion works because several representations agree about the same stone:

- The height field shifts its visible surface.
- The normal map changes its lighting direction.
- Roughness changes the highlight across its upper face.
- Ambient occlusion darkens its crevices.
- Contact shadows attach it to neighboring stones.
- A real mesh replaces it when its silhouette becomes important.
- Virtual texturing prevents its source detail from becoming blurry.
- LOD transitions reduce the expensive effects gradually with distance.

If only the normal map is present, it looks embossed. If only POM is present, it can look warped. When all those signals agree, the visual system reads it as actual geometry.

## Best equivalent direction for Booter & BigARM

For this project, the practical ladder would be:

- Upgrade near-ground materials from one-offset parallax to bounded, angle-adaptive POM.
- Use roughly 8–16 height samples near the player, reducing toward 4–8 with distance.
- Preserve normal, roughness and AO alignment with the same height field.
- Use the existing deterministic combined-mesh scatter system for stones that need silhouettes; its intended tiers are already documented in [GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Docs/GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md:21).
- Add inexpensive terrain-local contact occlusion before attempting a custom screen-space displacement renderer.
- Fade POM into ordinary normal mapping before chunk LOD transitions.
- Keep displacement strictly visual unless separately represented in collision and navigation data.

A custom SSDM-like renderer feature could eventually exceed POM, but it is considerably more difficult because it must handle disocclusions, depth writing, shadow disagreement, motion vectors, TAA and upscaling. With this game’s elevated top-down camera, POM plus deterministic real micro-meshes should capture most of the visible benefit at much lower engineering risk.

One more first-party disclosure is imminent: Pearl Abyss’s gamescom dev sessions on August 23–25 are scheduled to cover its procedural terrain tools, biome-based placement, sample-based environment production, terrain complexity and automated consistency pipelines. That should reveal more of the authoring side, but the presentation has not happened yet as of August 14, 2026.

No project files were changed.
