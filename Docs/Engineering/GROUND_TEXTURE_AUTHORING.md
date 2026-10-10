# Ground texture authoring

User-approved tools, October 9, 2026. Active host: Greater Wasteland's fixed native terrain. The earlier Landscape Authoring Sandbox stamps remain a generated-world reference and are not this tool's production control path.

## Open the tool

Unity menu: **Booter & BigARM > Ground Textures > Texture Sticker and Paint Tool**.

Choose **Place stickers** or **Paint terrain**. Starter presets are Sand, Gravel, Rocky ground, and Sand and dirt; an imported project texture can also be assigned directly. The tool does not start placing/painting until **Start** is pressed. **Escape** or **Stop** ends the operation. Alt and right/middle mouse preserve Scene-view navigation; modifier clicks do not paint.

The separate `Assets/_Project/Scenes/Reference/TextureAuthoringDemo.unity` demonstrates both modes on synthetic native terrain. It is not an enabled build scene. The [current nine-area rock study](NINE_AREA_ROCK_STUDY.md) supports both stickers and native painting. The earlier `FixedTerrainDressingComparison.unity` accepts stickers on its sampled-ground meshes; native painting requires a native Terrain, so it is unavailable on those older comparison mesh patches.

## Movable texture stickers

1. Select a preset or assign **Texture**. Set **Size in metres** and **Rotation**.
2. Click **Start placing stickers**, then click ground in Scene view.
3. Select the resulting `Ground Texture - ...` object. Its Inspector controls texture, optional normal map, tint, size, opacity, edge softness, round/rectangle shape, tiling, metres per repeat, normal strength, smoothness and overlap order. Higher overlap order draws later. Disable **Tile Texture** to display one complete image; leave it enabled for sand/gravel surfaces. Tiling textures should use Repeat wrapping in their Unity importer.
4. Move, rotate or scale with Unity's tools. Selected stickers conform to ground after movement; **Conform to ground** explicitly rebuilds after other surface edits.
5. Save the scene to preserve placements. Ctrl+Z undoes placement/edits; normal Unity duplication is supported.

These are conforming mesh decals: a sampled mesh sits 1.5 cm above ground with a transparent lit material and feathered edges. Imported alpha is respected; opaque textures also work because the tool supplies the edge mask. The tool does not require a URP Decal Renderer Feature or alter renderer settings. It receives lighting/shadows but does not cast shadows, add collision, displace native terrain, or provide parallax relief. It is intended for ground patches, not projecting labels onto walls or arbitrary rock faces.

Native TerrainCollider samples, explicitly marked `TopDown3DGroundSurface` MeshColliders, and the fixed-terrain study's named sampled ground are supported. Props and other additive scenes are excluded from picking. Missing samples and holes clip cells instead of creating a floating sticker across missing ground. Conformance is a bounded sampling approximation; sharp relief or holes smaller than the sample grid need finer sampling or a smaller patch. Stickers can overlap, but large stacks of transparent patches incur overdraw. Avoid using thousands of stickers as a whole-world replacement material.

The runtime component persists authoring settings and sampled vertex/normal/UV/index arrays in the scene. It reconstructs an owned mesh on enable, uses one shared material with per-object property blocks, and releases owned geometry on disable/destroy. It performs no runtime ground query. Nonselected stickers must be explicitly reconformed after parent/terrain transforms change. Do not animate the sticker as a ground-following gameplay object.

## Native terrain brush

1. Choose a preset **Paint layer**, or assign a texture and click **Create / use paint layer from texture**. Existing TerrainLayer assets can also be assigned.
2. Set brush radius, strength and soft edge. **Start painting**, then drag on native terrain in Scene view.
3. **Restore base texture** paints toward each tile's first/original layer instead of adding a new layer. Ctrl+Z restores an entire stroke, including added layer assignments.
4. Use **Save painted terrain assets** to save the TerrainData assets touched by this tool session, or Unity's **File > Save Project**. Scene saving alone should not be relied on for these asset edits.

Painting edits native alpha-map weights and appends the chosen TerrainLayer only to tiles actually touched. The brush carries across tile boundaries and samples one world-space footprint. Existing layer order is retained; holes, heights, terrain size and colliders are unchanged. Only participating TerrainData and their alpha textures are registered for Undo, once per stroke. Weights are packed with a largest-remainder allocation to preserve a total of 255 across native UNorm8 channels. No paint occurs on disabled terrain/collision or an empty/outside/hole-only brush footprint.

Greater Wasteland currently has 128 by 128 alpha maps on 256 m tiles: roughly 2 m per weight pixel. Small brushes can miss all pixel centres. Use stickers for fine details; changing alpha-map resolution is not part of this delivery. The demo uses finer maps only on its own synthetic terrain. Terrain layer tiling is configured on the shared TerrainLayer asset and affects all tiles using it. Keep active layers limited: additional native terrain layers can increase rendering passes and sampling cost.

Painting intentionally changes TerrainData file hashes even though heights are preserved. Existing full-file terrain receipts remain evidence of their earlier checkpoint. Record a new painted authoring baseline before subsequent terrain migration/source validation; do not weaken height, seam or collision gates to hide material edits. This delivery does not paint production TerrainData.

## Identity and world integration

Each sticker gets a stable authored ID, with duplicate/missing IDs repaired in Editor scene hierarchy order. Namespace that ID by scene identity when integrating multiple scenes. IDs and geometry survive scene serialization/reload; duplicating an object assigns the copy a new ID while retaining the original.

These authored patches constrain later procedural dressing. There is no world-seed dependency, generated-object ownership or new gameplay save delta in this authoring slice. Native terrain remains always loaded. Scene unload/reload reconstructs sticker presentation from saved samples; native paint remains part of its authored TerrainData. Destructible/interactive patches or runtime painting would require a separate persisted-delta design.

## Research and tradeoff

- [Unity terrain texture painting](https://docs.unity3d.com/6000.4/Documentation/Manual/terrain-PaintTexture.html) is the native route for broad blended surface coverage.
- [URP Decal Projector](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/renderer-feature-decal-projector-reference.html) projects onto surfaces within a volume, with size, opacity and fade controls.
- [URP decal renderer techniques](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/renderer-feature-decal-reference.html) have different normal/material capabilities and rendering costs.

For this ground-only tool, editable conforming mesh stickers provide soft local patches on both native terrain and study ground without introducing a renderer feature. They approximate surface integration through lit alpha blending; they do not blend directly into the receiver's G-buffer or provide projected-decal normal/MAOS behavior. A projected-decal extension is a separate feature for rock/wall placement.

## Verification boundary

Focused EditMode tests cover tile-boundary weights, normalized native packing, untouched heights/holes, full-stroke Undo, no-op brush behavior, sampled geometry serialization, duplicate identity and no added collision. Shader variants and an offscreen fixture are verified in an isolated background Direct3D 11 Editor. No foreground activation, Play Mode or gameplay smoke testing is used. Artist brush feel, overlap readability under production dust/lighting and Player performance remain acceptance tasks.
