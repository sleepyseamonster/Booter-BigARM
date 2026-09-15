# Skybox samples

These are candidate environment images for the native skybox pipeline. They remain source samples until the engine's HDR conversion, equirectangular validation, exposure handling and cubemap/IBL upload path accept them.

## Martian sunset basin 01

`Samples/martian-sunset-basin-01.png` is the user-provided 2:1 panoramic source from 2026-09-14. It is 1774×887 RGB PNG, with a low yellow sun, rust-colored basin, distant mesas and a dusty atmosphere. The original download remains at `/Users/worldbuilder/Downloads/ChatGPT Image Sep 14, 2026, 09_48_50 PM.png`.

This is an LDR source image, not a measured HDRI. Do not treat the sun pixels as physically valid luminance until the engine's conversion workflow assigns an exposure and validates the horizon seam, latitude distortion, and left/right wrap.
