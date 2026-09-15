# Generic material pipeline result

Implemented 2026-09-14 as an engine-only rendering pass. This batch does not add terrain, rocks, generators, authored landscapes or game rules.

The scene material path now supports two explicit coordinate modes:

- `WorldTriplanar` remains the default for large-scale surfaces that should not require authored UVs.
- `UV` samples the model's imported `TEXCOORD_0` data with independent scale and offset controls.

The UV path carries vertex UVs and tangents through both static and skinned scene shaders. Normal textures are decoded in tangent space and transformed through a TBN basis built from the geometric normal, imported tangent and tangent handedness. Albedo and packed surface channels use the same UV transform, so color, roughness and ambient-occlusion data stay aligned. The transform is runtime material state; it is not baked into geometry or a game-specific asset.

The existing semantic texture contract remains authoritative: color images are sRGB, while normal, surface, height and mask data are linear. `TextureCatalog` and `TextureStore` keep those roles explicit, and `MaterialDefinition` validation rejects missing records, role mismatches and invalid transfer functions before GPU acquisition. Existing world-projected materials keep their previous behavior because the new mode is opt-in.

Proof for this pass:

- `cmake --build build/foundation --target engine_workbench engine_player engine_outdoor_check --parallel 4` passed, including all scene shader variants.
- Focused native suites `fixture_geometry`, `semantic_textures_catalog`, `material_definition_contract`, `collision_character_camera` and `model_import_animation` passed.
- The importer and vertex layout already preserve UV and tangent data; no game-specific content was changed in this batch.

The path is intentionally a first engine contract. Future rendering work can add material instances, texture streaming policy, shared depth/normal resources, AO and fog without changing the mesh or asset semantics established here.
