# First Sun and Surface Rendering Pass

Implemented 2026-09-10. P04/P05 now provide a bounded sun shadow pass and opaque material lighting in the native workbench. This is the first usable rendering pass, not final wasteland art or large-world shadow quality.

The renderer uses a 2048-square R32F shadow map with depth testing, a 24-meter orthographic light volume, slope-sensitive receiver bias and nine comparison samples. The ground, subject and scale marker use the same transforms in shadow and color passes. Explicit view IDs order shadow, linear scene, display and inspector. The fixed light volume is independent of camera orbit; outside it, surfaces receive unshadowed lighting. The shadow map has a fixed lifetime and is not resized with the window.

Opaque shading combines Lambert diffuse, GGX distribution, height-correlated Smith visibility and Schlick Fresnel. Roughness and metallic are independent parameters; rock is nonmetal by default. A simple hemispheric ambient term supplies fill. This is not environment-map IBL, global illumination, physically calibrated sun units or a filmic tone mapper. The existing linear HDR/exposure/SDR boundary remains in use. The [Filament material reference](https://google.github.io/filament/main/filament.html) supplied the BRDF equations; the pinned bgfx `15-shadowmaps-simple` example supplied the backend-dependent shadow crop convention. No dependency was added or modified.

The workbench resolves its first surface preset through the shared texture catalog:

| Surface | Color | Normal | Surface data |
|---|---|---|---|
| Rock | RockWorkbenchSide_Albedo | RockWorkbenchSide_Normal, RGB XYZ, positive tangent Y | RockWorkbenchSide_Surface: R AO, G roughness, B preserved height |
| Ground | BrokenWorldSandDirtAlbedo | Explicit neutral normal | Scalar roughness; no substituted ground map |

The shader projects textures in world space using three signed, right-handed bases and blends by geometric normal. Normal perturbations are projected into the surface tangent plane; zero strength restores geometric normals. Base color is sampled as sRGB, numerical maps as linear. AO affects ambient only. Packed B is not metallic and does not displace geometry. This initial projection does not reproduce Unity's layers, crack masks, wear, dust or smoothness remapping. The client owns these provisional preset bindings; reusable authored material documents and editing commands follow in P06.

The **Sun and surface** inspector exposes shadows, bias, texture use, roughness, metallic, normal strength, repeat scale and ambient fill. Lighting settings save with inspection documents. Older v1 documents without the additive `lighting` object receive explicit defaults; malformed fields are rejected before replacing live state. With a complete transferred catalog the workbench starts in the lit scene; texture preview is an optional tool. Without that preset it starts with solid materials.

## Procedural compatibility

Rendering creates no generated objects or seeds and does not change deterministic world identity. Catalog IDs remain separate from ephemeral GPU handles; leases release shared textures when owners unload. Future chunk render instances can reuse these surfaces without persisting handles. Authored constraints are the provisional material preset and editable light settings, not accepted geography. Runtime gameplay deltas are not applicable because this pass only edits inspection state. World projection currently uses bounded local render coordinates; large-world texture phase and camera-relative shadow volumes belong to streamed-world integration, not a claim made by this fixture.

## Run

From the repository root, using the already cooked catalog:

```sh
Engine/build/foundation/engine_workbench --catalog Engine/out/surfaces-accepted-a/catalog.json
```

If that ignored output is absent, use the [texture cooking instructions](./TEXTURE_PIPELINE_RESULT.md) to produce a catalog and pass its path. Build with the existing `Engine/build/foundation` CMake directory. No package rebuild is needed for local development.

## Focused evidence and limits

[One short Metal pass](../Evidence/P04-P05-lighting/result.json) captured eight states. Enabling shadows darkened 3,130 sampled scene pixels and brightened none; real surfaces, normal strength, roughness, light direction, exposure and camera/bias changes produced scene differences. The app recorded eight captures and zero GPU errors. The actual rock/ground capture was visually inspected. [Native tests](../Evidence/P04-P05-native/result.json) passed, including old-document compatibility, lighting roundtrip and rejection without live-state replacement.

This batch used one focused GPU run after correcting a shader compile error. It did not repeat texture cooking, packaging, resource stress loops or gameplay checks. Pixel differences establish active controls, not quantitative BRDF accuracy, normal-map orientation on every projection, independent bias quality, contact quality at every scale or self-shadow quality on a generated concave rock. Those are bounded remaining quality gaps; they do not block simulation work. Windows remains unverified. The historical P03 package predates these shaders; the current local build is the executable verified here.

Next: P08 shared fixed-step simulation and input actions, then P09/P10 physics, motor and obstruction camera. P06 inspection commands are ready. Do not spend another batch polishing this render fixture before advancing the runtime.
