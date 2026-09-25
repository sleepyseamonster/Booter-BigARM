# Martian Panorama Sky

The active Unity world uses the user-supplied `MartianSunsetBasin.png` as its visible sky during Play and in the production scene's Scene view. The source is a 1774 x 887, 8-bit RGB PNG panorama, not a calibrated HDR image. Its SHA-256 is `a6e36819282b239e474efccdf2f39303b99164b25cf95a1762d9a2f5857ac381`.

`PerpetualTwilightSun` remains the authority for the twilight cycle, directional light, and ambient fill. It loads `Resources/TopDown3D/MartianPanoramaSky.mat`, clones it for runtime use, and sends the current sun direction to the sky shader. Ambient lighting remains gradient-driven. The scene's Environment Reflections setting is Skybox, so Unity may also use this LDR image for reflections; it is not a calibrated HDR lighting probe.

The shader maps the painted sun onto the directional light and restricts sampling to the sky and distant ridge above the image's painted foreground. It blends the panorama's left and right edges near their join. The source image was not edited.

The production scene assigns the material in Lighting > Environment > Skybox Material. The saved material sun direction matches the scene's initial directional-light rotation for the edit-time preview; Play uses a runtime material copy that tracks the moving light.

The sky has no per-chunk identity or persisted delta. Streaming, unload/reload, and world seeds do not change it; authored sky material settings are global. This keeps the infinite procedural landscape authoritative in front of the backdrop.

Visual acceptance remains to be checked in the game's camera, especially the distant ridge, seam, flare alignment, and exposure through the twilight cycle. The PNG cannot supply HDR dynamic range or physically accurate reflections. A true HDR capture would be a separate lighting asset if that becomes a goal.
