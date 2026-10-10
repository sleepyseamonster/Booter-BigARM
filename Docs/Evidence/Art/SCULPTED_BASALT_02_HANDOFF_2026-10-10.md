# SculptedBasalt02 game asset handoff — 2026-10-10

The author's new `Cube_Sculpt` rock is reduced, UV-mapped, textured and imported
as a separate reusable Unity prefab. The earlier SculptedBasalt01 asset and the
scale-reference objects remain intact. No production scene is saved or populated.

## Deliverables

- [Unity prefab](../../../Assets/_Project/Prefabs/TopDown3D/Environment/SculptedBasalt02.prefab)
- [URP material](../../../Assets/_Project/Materials/TopDown3D/Rocks/SculptedBasalt02.mat)
- [FBX and texture folder](../../../Assets/_Project/Art/Environment/Rocks/SculptedBasalt02/)
- Local-only artist snapshot: `SourceArt/Blender/Studies/RockSculpting/SculptedBasalt02_ArtistSource_20261010_154736.blend`.
- Local-only textured review/source: `SourceArt/Blender/Studies/RockSculpting/SculptedBasalt02_GameReady.blend`.

The artist snapshot includes the unsaved sculpt captured from the authorized
Blender session. An additional local backup was saved immediately before opening
the finished version in that session. Blender sources and numbered backups stay
ignored and are not publication content. The game-ready Blender file also retains
the high-detail sculpt, hidden and protected from selection.

## Geometry and scale

| Mesh | Blender vertices | Unity vertices, including UV splits | Triangles |
| --- | ---: | ---: | ---: |
| Author sculpt | 98,306 | Not imported | 196,608 |
| LOD0 | 985 | 1,456 | 1,966 |
| LOD1 | 493 | 814 | 982 |
| LOD2 | 247 | 460 | 490 |
| Convex collision | 122 | Separate collision mesh | 240 |

LOD0 removes about 99% of the source topology. A deterministic bidirectional
surface sampling comparison measured 4.33 mm at the 95th percentile and 27.83 mm
at the maximum sampled point, across 19,130 samples. These are sampled surface
distances, not a guarantee of every silhouette or vertex error. The smaller
982-triangle candidate was rejected because its maximum sampled error was 12 cm.

Unity LOD0 bounds are 2.3208 m wide × 2.4328 m tall × 2.2802 m deep. The pivot is
at the horizontal bounds centre and source bottom; reduced geometry starts about
6.5 mm above it. Export is FBX, selected meshes only, metre scale, -Z forward,
Y up, imported normals/tangents and baked FBX axis conversion. The prefab has
identity mesh transforms, one shared instanced URP/Lit material, three LOD levels
(screen-relative thresholds 0.35, 0.15 and 0.015), and a separate convex collider.

## Textures

1024 × 1024 PNG maps: BaseColor, Normal, Roughness, Occlusion and
MetallicSmoothness. The runtime material consumes base color, tangent normals,
occlusion and packed metallic/smoothness. Roughness remains an inspection map.
Metallic is zero; smoothness is inverted roughness in the packed map's alpha.
Only base color imports as sRGB; normals use Unity's normal-map importer. Maps
have mipmaps and clamp wrapping; the material uses GPU instancing.

The existing [original basalt texture set](../../../SourceArt/Blender/Studies/textures/VolcanicBlackBasalt_README.md)
was box-projected and baked to the new UV layout. Sculpt detail and restrained
estimated height bump were baked high-to-low into tangent normals. The original
texture set is image-derived rather than a calibrated PBR scan; inherited baked
lighting and estimated relief remain visual limitations. Final game-lighting and
placement approval belong to the author.

## Verification

- Blender 5.2.2 LTS reopened the finished file. All three LODs and the collider
  have positive volume, zero non-manifold edges, zero loose vertices and zero
  degenerate faces. Runtime meshes have finite UV coordinates in [0, 1].
- Normal bake pixels are finite; all 1,966 LOD0 triangle UV centres have valid
  outward tangent normals. Front, back, top and underside renders were inspected.
- The repository mesh inventory tool ran on the saved delivery file.
- Unity 6000.4.0f1 compiled the updated editor pipeline and imported the FBX/maps.
  `ValidateAsset` passed for SculptedBasalt02: triangle counts, UVs, normals,
  tangents, dimensions, pivot, transforms, material, texture import and collider.
- The original SculptedBasalt01 validator still passes. An attempted repeat build
  was rejected by the overwrite guard, and the original asset context was restored
  after that exception. These are direct Editor contract checks, not gameplay
  smoke tests. No gameplay test, production placement or performance acceptance
  is claimed.
- The finished textured file is open in Blender in Object Mode with LOD0 selected.

Small retained proof: [delivery](SculptedBasalt02/delivery.json),
[Blender verification](SculptedBasalt02/blender-verification.json),
[Unity import](SculptedBasalt02/unity-import.json).
Local render/inventory diagnostics are under `Logs/SculptedBasalt02/`.

## Integration boundary and reproduction

The prefab is content rather than a placement/world service. Deterministic world
identity and stable instance IDs are assigned by the approved dressing system,
not embedded in this model. Terrain is unchanged. Dressing-cell load/unload and
persisted runtime deltas remain outside this asset slice. Authored constraints
are the sculpt's approved form, metre-scale references, ground pivot and separate
collision; no procedural placement or saved-world integration is installed.

The existing [Blender preparation tool](../../../Tools/Art/Blender/prepare_sculpted_basalt.py)
now accepts an asset identifier and source object after the root argument, while
preserving its original default paths. For this asset: `inspect ROOT
SculptedBasalt02 Cube_Sculpt`, then `build ROOT SculptedBasalt02 Cube_Sculpt`,
using the local artist snapshot as Blender input. The build refuses existing
deliverables. Verification uses `verify ROOT SculptedBasalt02` on the ready file.

The [Unity importer](../../../Assets/_Project/Scripts/Editor/TopDown3D/SculptedBasaltAssetPipeline.cs)
uses `BuildAsset("SculptedBasalt02", new[] {1966,982,490},
new Vector3(2.3153563f,2.4371643f,2.2756197f))`. `ValidateAsset` uses the same
contract without rebuilding. The original parameterless methods retain their
SculptedBasalt01 contract. No package or rendering-setting changes were made.

Method references: Blender's [Decimate modifier](https://docs.blender.org/manual/en/5.2/modeling/modifiers/generate/decimate.html)
and Unity's [URP shader documentation](https://docs.unity.com/en-us/engine/6000.5/manual/materials-and-shaders/built-in/shaders-in-universalrp).
