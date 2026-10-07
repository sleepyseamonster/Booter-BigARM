# Skybox samples

These are candidate environment images for the native environment-light pipeline. They are separate from the visible procedural sky: an accepted panorama can illuminate materials while the renderer keeps its own sky background.

## Martian sunset basin 01

`Samples/martian-sunset-basin-01.png` is the user-provided 2:1 panoramic source from 2026-09-14. It is 1774×887 RGB PNG, with a low yellow sun, rust-colored basin, distant mesas and a dusty atmosphere. The original download remains at `/Users/worldbuilder/Downloads/ChatGPT Image Sep 14, 2026, 09_48_50 PM.png`.

This is an LDR source image, not a measured HDRI. The checked-in recipe `martian-environment.recipe.json` classifies it as a linear environment-light input for the bounded texture cooker. It still needs an explicit exposure calibration and renderer sampling before it is a physically valid HDRI. A future EXR or true HDR capture can use the same asset identity and replace this source without changing the visible-sky contract.
