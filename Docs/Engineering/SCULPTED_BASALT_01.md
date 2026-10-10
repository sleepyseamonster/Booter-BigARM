# Sculpted basalt 01 — October 10, 2026

The author's saved `Rock_Sculpt_01` is the source. Preserve its silhouette and rounded cuts, minimize runtime geometry, and use the existing dark volcanic basalt texture set. The October 10 follow-up places one linked prefab at the centre of Area 05 in `FixedTerrainDressingStudy.unity`; its existing generator moves 5 m aside. Production placement remains separate.

## Sources and delivery

- Untouched artist file: `SourceArt/Blender/Studies/RockSculpting/RockSculptStarter_2026-10-09.blend`.
- Local preservation copy: `RockSculpt_ArtistSaved_2026-10-10.blend` in the same folder.
- Separate optimized, textured authoring file: `RockSculpt_Basalt_GameReady_2026-10-10.blend`. Its hidden high-detail source remains editable. The reference ground and blue/tan scale cubes are retained; Object Mode and Material Preview show LOD0. Hidden LODs and collision are handoff internals.
- Export: `Assets/_Project/Art/Environment/Rocks/SculptedBasalt01/SculptedBasalt01.fbx` and its UV texture atlas. All `.blend` sources/backups remain local-only under the [source policy](../Operations/LOCAL_ONLY_SOURCE_POLICY.md).
- Runtime prefab: `Assets/_Project/Prefabs/TopDown3D/Environment/SculptedBasalt01.prefab`.
- Material: `Assets/_Project/Materials/TopDown3D/Rocks/SculptedBasalt01.mat`, URP Lit, one material, instancing enabled.

## Reduction and topology

The saved source has 24,578 vertices, 24,576 quads and 49,152 rendered triangles; dimensions are approximately 3.16 × 3.25 × 4.96 m in Blender XYZ. Unity's height is Y. Do not scale the rock back to the original starter cube dimensions: its present size is the author's sculpt.

Seven collapse ratios were tested against bidirectional nearest-surface samples of source and reduced geometry. The smallest tested candidate within a 10 mm 95th-percentile / 30 mm maximum sampled error budget has 982 triangles: 98% fewer than the source, measured 7.02 mm at the 95th percentile and 27.12 mm maximum across 18,638 samples. These are sampled surface errors, not a mathematical bound or proof of a globally minimal triangulation. This budget preserves shape at the current scale while prioritizing few faces.

| Mesh | Triangles | Purpose |
| --- | ---: | --- |
| LOD0 | 982 | Near and authoring view |
| LOD1 | 490 | Middle distance |
| LOD2 | 244 | Far distance |
| Convex collision | 206 | Static prop collision; conservatively bridges cavities |

All four are closed, have outward volume, no loose vertices, no non-manifold edges and no zero-area faces in the Blender check. Runtime triangles are intentional: Unity triangulates quads. Smart-projected UVs use a 2.5% island margin; baked maps use 16-pixel dilation at 1024². Three LOD meshes retain UVs, normals and tangents. Runtime meshes bake the complete imported FBX node transform so prefab children use identity transforms, a bottom-centre pivot and metre scale. They live beside the FBX as native mesh assets.

LOD thresholds start at 0.35, 0.15 and 0.015 screen-relative height, with no crossfade shader overhead. These are initial asset settings, pending density/performance and distance-pop review in the eventual world placement slice.

## Surface

The source is `SourceArt/Blender/Studies/textures/VolcanicBlackBasalt_*`, described in its `VolcanicBlackBasalt_README.md`. The existing image-derived material is provisional art data, not measured PBR capture. No new image was generated.

Base colour, roughness and local cavity AO are blended box-projected at a nominal 1.25 m period, then baked into the rock's own UV atlas. Tangent-space normals bake high-detail geometry plus restrained height bump onto the triangulated LOD0. This avoids connecting an axis-dependent tangent normal to box projection. The atlas records the blended projection without a runtime box-projection shader; source texture detail does not change silhouette.

Unity imports base colour as sRGB and normals/data as linear. Metallic is zero; inverted roughness is stored in the metallic texture's alpha as URP smoothness. AO strength is 0.6. Mipmaps, clamp wrapping, 4× anisotropy and high-quality texture compression are enabled. Roughness is retained for authoring inspection; the runtime material uses its packed smoothness equivalent. Base colour may retain lighting from its original image source.

## Integration contract

The prefab is visual/collision asset data and supplies no world-generation code. Deterministic world identity and stable generated-object IDs belong to the later dressing placement system. Greater Wasteland's fixed terrain stays always loaded; this asset adds neither terrain generation nor terrain streaming. Any dressing-cell unload/reload must restore the same prefab from its placement record. Authored exclusion, burial, orientation and footprint constraints belong to placement. Persisted runtime deltas such as removal/movement belong to the versioned saved-world slice; this prefab has no persistence service or resource identity.

## Proof and use

`Tools/Art/Blender/prepare_sculpted_basalt.py` measures candidates, builds a separate source, exports and verifies geometry/UV/bake coverage. It refuses to overwrite an existing delivery on a fresh build. `SculptedBasaltAssetPipeline.Build()` imports and creates a new material/prefab; `Validate()` checks the actual imported scale, pivot, triangles, material, UVs/normals/tangents and collision. `Capture()` renders an in-memory asset review without saving a scene or entering Play Mode.

Evidence is under `Docs/Evidence/Art/SculptedBasalt01/`. Blender reopening and front/back/top/underside renders were reviewed. Normal bake checks sample every LOD0 triangle centre for valid upward tangent normals. Unity checks are asset import/structure proof; they do not establish player traversal, collision feel, world density or performance. No gameplay smoke test is run. Artist visual acceptance and eventual placement remain separate.
