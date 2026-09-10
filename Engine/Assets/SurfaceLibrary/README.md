# Rock and Ground Surface Library

Transferred 2026-09-10 at the user's direction from the preserved Unity work area. These are byte-for-byte copies of existing project images, ready for the new engine's future texture/material pipeline. No image was regenerated, recolored, resized or repacked.

## Contents

- `Textures/Rocks/`: rock albedos, layered side/top/underside/grit maps, normal maps, packed surface data, crack mask and older rock variants.
- `Textures/Ground/SandDirt/`: sand/dirt, swept sand, gravel, mixed rocky ground, transition textures and pebble color/height maps.
- `SourceArt/Rocks/`: four source PNGs and the layered side-albedo PSD, preserved for editing rather than runtime loading.
- [manifest.json](./manifest.json): 38 texture images plus five source-art files; dimensions, byte sizes, SHA-256 hashes, origin paths/GUIDs, importer settings and serialized material references. Total: 101,692,582 bytes.
- [material-reference.json](./material-reference.json): 15 textured material records from six material files and the mixed-formation asset, with resolved texture keys, tiling/offset, scalar values and colors. This is a reference snapshot, not the new engine's material format.

The collection includes the whole existing rock/ground image family so associated variants and editing sources survive the transition. Twenty-four images are referenced by the inspected serialized materials. Other images include code-bound pebble textures, source art and retained family variants; their presence does not make every image an active visual baseline. The old Legacy2D tiles, unrelated item icons, generated Unity meshes and `.meta` files are outside this transfer.

## Material Interpretation

Preserve these source facts when implementing the engine's materials:

| Map | Source meaning | Engine integration requirement |
|---|---|---|
| Albedo/base color | Imported with sRGB enabled | Decode color for linear lighting; preserve alpha and original pixels |
| Normal | Imported as normal data with sRGB off; existing layered writer encodes XYZ into RGB | Use raw source-image decoding, then verify orientation and projection; Unity's imported GPU packing is not the PNG format |
| Rock `Surface` maps | R = ambient occlusion, G = roughness, B = height; writer sets A = 1 | Preserve this packing; it is not an occlusion/roughness/metallic texture |
| Ground and pebble height | Scalar height data sampled from R; sRGB off | Define displacement/parallax use separately; texture presence does not create geometry |
| `RockWorkbenchCrack_Mask` | R = crack, G = halo, B = deposit; writer sets A = 1 | Recreate the shader's mask interpretation when that material feature enters scope |
| Teal luster mask | Existing mask retained, sRGB off | Preserve as reference; older luster behavior is not automatically the selected engine material |

The current rock material records bind the **same side albedo to base, top, underside and grit color slots**, while retaining different normal/surface maps. Do not substitute images merely because their filenames say Top or Underside. The saved bindings are the observed source fact; final new-engine appearance remains user-owned.

The Unity rock shader also applies world-space texture projection, slope/layer blending, cracks, dust and per-rock variation. Terrain uses multiple material/transition maps and pebble detail. Deposited dust uses its own opacity/edge behavior. Copying images does not port those shaders or the rounded sand geometry. Shader and builder source paths/hashes are retained in the manifest; implementation should follow the staged [outdoor rendering plan](../../Docs/OUTDOOR_RENDERING_PLAN.md).

Pebble textures are additionally assigned by `TopDown3DMixedFormationClutter.cs` through material property blocks: `_PebbleAlbedoMap` and `_PebbleHeightMap` use `MixedGroundPebbles`; `_NearRockPebbleAlbedoMap` and `_NearRockPebbleHeightMap` use `NearRockPebbles`. `_RockPebbleColorMap` is assigned a rock color map by that code. Serialized material snapshots do not capture every runtime override or prove active scene assignment.

Image dimensions range from 1024 square to non-power-of-two 1229/1254 square. Preserve originals; choose derived sizes, mipmaps and platform compression in the future import pipeline. Unity importer values are recorded reference data, not a selected engine compression policy.

## Verification and Ownership

From `Engine/`:

```sh
python3 Tools/verify_surface_assets.py
python3 Tools/verify_surface_assets.py --sources
```

The first command needs only the engine collection. The second also reads the preserved Unity sources and compares their hashes. Neither runs Unity or changes assets. [Transfer verification](../../Evidence/SURFACE-transfer-final/result.json) records the source comparison using the final verifier.

The manifest's collection keys identify these assets independently of GPU handles; runtime asset IDs are not finalized. The transfer creates no world objects, seeds, chunk state, authored geography or persisted gameplay deltas. Future materials should share texture resources across generated rocks and chunk lifetimes without making GPU handles into persistent IDs.

The original image files and Unity settings remain untouched. Source provenance here is the existing project checkout; no external asset acquisition occurred. The engine currently has no scene-texture loader, so these files are collected assets rather than integrated rendering output. Color handling remains next, followed by the planned lighting and material work.

P03 now supplies [semantic recipes, cooking and shared GPU previews](../../Docs/TEXTURE_PIPELINE_RESULT.md). Runtime IDs use a `surface/` namespace over collection keys. The sources and historical bindings above remain unchanged; PBR material interpretation is separate work.
