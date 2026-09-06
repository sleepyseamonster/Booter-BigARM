# Mixed-ground pebble texture source — 2026-09-06

Created using the built-in image-generation tool. These are generated art sources, not measured photogrammetry or a validated physical scan. The second image uses the first as an edit target to preserve the gravel layout while interpreting elevation. Normal response is derived from that height in the terrain shader; no unrelated procedural dot relief remains.

Project assets:

- `Assets/_Project/Art/Environment/Ground/SandDirt/MixedGroundPebbles_Albedo.png`
- `Assets/_Project/Art/Environment/Ground/SandDirt/MixedGroundPebbles_Height.png`

Both maps share the same world-space sampling. Albedo imports as sRGB; height imports as linear uncompressed data. Both use repeating wrap, mipmaps and trilinear filtering. In-game appearance and tiling remain user-owned visual acceptance.

Validation: the isolated Unity 6000.4.0f1 editor completed imports, C# compilation, and explicit standard/fast terrain-pass compilation on Metal without shader errors. That editor stalled during final Mono cleanup and was terminated after the successful compiler message; this was not a clean process exit. No live scene, gameplay, or visual test was performed.

## Color texture prompt

Use case: photorealistic-natural
Asset type: seamless square tileable ground albedo texture for a realistic Unity game, viewed from elevated top-down camera.
Primary request: high-quality natural dry angular pebble gravel in fine dusty earth, a one-square-meter patch seen orthographically straight down. Photogrammetry-quality material detail, not a rendered scene. Irregular broken shale chips and worn angular pebbles with flaked, rough surfaces; continuous size hierarchy from fine grains through many 1–3 cm fragments to a few 5–8 cm pieces. Uneven organic clusters and connected pockets of exposed compacted dirt, about half the ground covered by fragments. Different orientations, overlapping chips, partly embedded edges. Restrained warm medium-gray and gray-brown stones, occasional dark charcoal chips, dusty muted reddish-brown soil. Natural mineral color variations inside stones. No white stones.
Lighting: flat diffuse cross-polarized albedo lighting, no directional cast shadows, no specular shine, no vignetting, no overall gradient, no baked ambient occlusion black outlines.
Composition: entire square is the material, edge-to-edge, seamless repeat on all four edges, high detail everywhere, no perspective or depth of field.
Avoid: evenly spaced dots, grid, identical round pebbles, polka dots, black pepper speckles, decorative cobblestones, large boulders, vegetation, text, borders, labels or watermark.

## Height-map edit prompt

Use case: precise-object-edit
Asset type: grayscale HEIGHT DATA MAP matching the attached game-ground albedo texture pixel for pixel.
Input image is the EDIT TARGET and exact layout authority. Convert only its material colors into geometric elevation data. Preserve the exact square canvas, every stone boundary, stone placement, scale, angle, cluster and exposed soil gap. No resizing, no new stones, no removed stones, no framing shift.
Output one grayscale seamless heightmap, NOT a lit grayscale photograph and NOT a normal map. Soil is a uniform near-black 8 percent gray baseline with very subtle fine elevation texture. Small embedded grains sit slightly above baseline. Pebbles have irregular gently beveled dark edges rising to medium-gray raised angular tops; the few larger fragments reach 70–85 percent gray. Tops vary modestly in slope/facets but do not include their albedo mottling or mineral color. Medium and dark colored stones must still be elevated. Black means low, white means high. No directional shading, highlights, ambient-occlusion shadows, outlines, text, legend or watermark. Give the stones smooth enough topographic ramps for strong, clean derived normals rather than noisy grain everywhere.
