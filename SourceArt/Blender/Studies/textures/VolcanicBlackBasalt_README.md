# Black volcanic basalt material

This set is for the primary `Formation_00` rock cluster in `BrokenWorldBadlandsStudy.blend`. It follows the supplied porous volcanic stone reference but is darker and has no colored iron veins. Other formations and ground clutter keep their lighter variation.

| Map | File | Interpretation |
| --- | --- | --- |
| Base color | `VolcanicBlackBasalt_Albedo.png` | sRGB, charcoal black; no colored veins. |
| Normal | `VolcanicBlackBasalt_NormalGL.png` | Linear tangent-space normal, OpenGL Y+ convention. In Unity, import as a normal map. |
| Roughness | `VolcanicBlackBasalt_Roughness.png` | Linear roughness, mostly matte with limited mineral highlights. In Unity URP, invert to smoothness and pack per shader convention. |
| Ambient occlusion | `VolcanicBlackBasalt_AO.png` | Linear local cavity occlusion. Use gently to avoid doubling baked shadows. |
| Height | `VolcanicBlackBasalt_Height16.png` | 16-bit linear, image-derived relief estimate for bump or restrained parallax. It is not calibrated displacement. |
| Metallic | `VolcanicBlackBasalt_Metallic.png` | Linear zero throughout. Basalt is a dielectric; its sheen comes from Fresnel reflection and roughness, not metalness. |

All six output maps are 1024 × 1024 pixels. Their opposite border pixels match exactly in the saved files, and [the 2 × 2 repeat preview](VolcanicBlackBasalt_RepeatPreview.png) was inspected for visible joins. `VolcanicBlackBasaltAlbedo_v1.png` is the original generated source, not a seamless delivery map. Rebuild the set from it with `build_black_volcanic_pbr.py` in the parent folder.

The Blender study uses generated coordinates with blended box projection, since the existing fractured rock meshes do not have export-ready UVs. Its material packs base color, roughness, AO, metallic, and height images into the `.blend`; height drives Blender bump. The tangent normal is supplied as a separate map for UV-mapped Unity assets. Do not connect a tangent-space normal directly to the current box-projected shader: its orientation across projection axes would be wrong. For Unity, unwrap or bake the rocks, check texel scale and normal orientation, and convert roughness to the target shader's smoothness channel.

The texture was generated from an image reference. Normal, height, roughness, and AO are estimates derived from that image, not independently captured PBR scans. The base color may retain some baked lighting. Treat this as a visually reviewed source-study material and validate it under the eventual game lighting before final export.
