# Rock Workbench Layered Material

## Purpose

The Rock Workbench uses one fused, UV-free material to evaluate geometry and surface language together. This is an editor-authoring system. It does not yet replace the production streamed-world rock material or define a permanent runtime asset library.

## Surface Contract

- Vertical and steep faces use a directional side texture with world-up geological strata.
- Upward faces blend into a distinct weathered top texture.
- Fine texture scale is measured in world meters rather than stretched to each mesh.
- Rock size controls the scale of broad smooth-versus-grainy patches.
- The generation seed offsets the surface fields deterministically.
- Cracks, crack halos, mineral patches, worn shine, and upward dust remain layers of one material; they do not create submeshes or decal GameObjects.
- Smooth and mineral patches alter roughness while the rock remains non-metallic.

The workbench exposes four direct surface-language controls:

- **Geology Scale** — physical size of the repeated grain and strata.
- **Surface Variation** — strength of broad smooth and grainy regions.
- **Crack Amount** — visibility of integrated fissures.
- **Worn Shine** — additional highlight response on smoother patches.

## Assets

The canonical workbench shader and material remain:

- `Assets/_Project/Shaders/TopDown3D/BrokenWorldRockWorkbenchPBR.shader`
- `Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat`

Layered textures live under:

- `Assets/_Project/Art/Environment/Rocks/Workbench/Layered/`

The `Source/` images are project-owned AI-generated albedo sources. The aligned 1024 x 1024 albedo, normal, and packed surface maps are rebuilt by:

- `Tools > Booter & BigARM > Rock Workbench > Rebuild Layered Textures`

The deterministic builder is `TopDown3DRockWorkbenchTextureBuilder.cs`. Packed surface maps use `R=AO`, `G=Roughness`, and `B=Height`. The crack map uses `R=Crack`, `G=CrackHalo`, and `B=MineralDeposit`.

## Source Prompts

The source images were created with the built-in image-generation tool using the prior neutral workbench albedo only as a palette and material-family reference.

Top source prompt: create a seamless, flat, neutral-diffuse desert-rock top surface with broad weathered plates, granular pockets, smoother worn patches, and restrained warm mineral staining; no directional light, perspective, silhouette, text, or watermark.

Side source prompt: create a seamless, flat, neutral-diffuse desert-rock wall with broad horizontal bedding, vertically stretched erosion, intermittent vertical fractures, granular seams, and smoother worn bands; preserve world-up direction and avoid regular stripes, perspective, text, or watermark.

## Procedural-World Boundary

The editor preview derives its material seed from the same workbench generation seed, so identical settings and seed reproduce both shape and surface placement. Chunk streaming, stable runtime feature identity, unload/reload reconstruction, and persisted gameplay deltas are not implemented by this editor-only pass. A runtime adoption must derive material identity from canonical world seed, absolute feature identity, generator version, and stable coordinate context without creating per-rock material instances.

## Proof Boundary

Shader compilation, material assignments, texture import settings, deterministic seed mapping, geometry topology, and generator contracts are covered by focused EditMode tests. Final top/side transition, crack intensity, shine, texture scale, repetition, and aesthetic acceptance require inspection in the Unity Scene view.
