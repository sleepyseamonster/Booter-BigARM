# Environment-light panorama sequence

Status: asset contract and offline cooking are implemented; renderer sampling is the next integration seam.

The supplied Martian sunset image is treated as an **environment-light panorama**, not as the visible sky. The visible background remains the renderer's procedural sky until an explicit sky-image feature is enabled. This keeps artistic background choice independent from material illumination.

`Assets/Skyboxes/martian-environment.recipe.json` registers the source with the semantic `environment` role. Environment textures are linear RGB inputs and are never tagged as sRGB base color. The existing bounded cooker produces the same portable KTX1 mip chain used by the material pipeline:

```text
python3 Tools/cook_textures.py \
  --cooker build/foundation/engine_texture_cook \
  --recipes Assets/Skyboxes/martian-environment.recipe.json \
  --out out/martian-environment-cook-20260914-b
```

The checked run produced one 1774×887 panorama with an 8,386,712-byte resident mip chain. This is an LDR PNG promoted to a linear lighting input; it is not a measured HDRI. The renderer integration must apply an explicit exposure and should be validated against a true HDR/EXR source before physically meaningful sun luminance is claimed.

Remaining renderer work is intentionally narrow: bind the cooked environment texture to scene materials, sample the equirectangular map for ambient image-based fill, and leave the procedural sky shader unchanged. Full irradiance convolution, specular prefiltering and production HDR calibration remain separate follow-up gates.
