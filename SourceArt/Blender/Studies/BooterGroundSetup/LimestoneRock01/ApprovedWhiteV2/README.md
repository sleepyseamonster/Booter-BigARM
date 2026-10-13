# Approved white limestone

The author approved the generated preview on October 12, 2026: white limestone with fine grit, shallow pores and a smooth worn surface. This is the current material on the rock in the original local `../../BooterGroundSetup.blend`. Its 712 triangles, 358 mesh vertices, UVs, scene pose and silhouette remain unchanged. The scene still contains one scene, Booter and the adjustable ground. The previous scene save is retained locally as `../../BooterGroundSetup_BeforeWhiteLimestone_2026-10-12.blend`.

The [approved albedo](SourceTextures/WhiteLimestone_ApprovedAlbedo.png) is a byte-identical workspace copy of the generated preview, at its native 1,254 by 1,254 pixels. [Provenance and exact prompt](source_provenance.json) record the built-in image generation tool, the matching source/copy SHA-256 hashes and the material interpretation. No new image variation was generated after approval.

The material uses the approved albedo directly. Very shallow relief is derived from small-scale luminance variation, with a 0.6 mm bump amplitude and 1.1 m projection tile. This is an artistic interpretation of grain/pores, not measured scan data. Roughness varies gently around 0.8; broad planes remain matte and smooth. Geometry occlusion and sculpted detail are baked from the retained artist source. None of the older layered workbench grain or height is used in the visible material.

## Texture set and export

The [Exports folder](Exports/) contains six 2,048 by 2,048 asset-UV maps and `LimestoneRock01_WhiteV2.fbx`:

- BaseColor: sRGB, approved white/ivory limestone palette.
- Normal: Non-Color, tangent space +Y, sculpted geometry plus shallow grain relief.
- Roughness: Non-Color, restrained matte variation.
- Occlusion: Non-Color, interpreted tiny pores and sculpt geometry occlusion.
- Height: Non-Color, interpreted microrelief, optional for engine use.
- MetallicSmoothness: Non-Color, zero metallic RGB and inverse roughness in alpha.

All six maps are packed into the original Blender scene. LOD0, LOD1 and LOD2 use the new material; the hidden collision proxy retains its prior material binding and is not render content. The FBX includes only those three detail levels and the 220-triangle convex collision proxy, at the same bottom-centre pivot, metre scale, `-Z` forward / `Y` up export convention, with smoothing and tangents. The player, ground, lights, camera and sculpt source are excluded. The distant detail levels share the texture set; transitions still require Unity integration.

## Verification

[Application receipt](application.json) records the before/after scene hashes, retained backup, source hash, unchanged geometry/UV/transform/unrelated-material signatures and export hashes. [Fresh verification](verification.json) confirms one scene, all unchanged signatures, the original polygon counts, material assignment, manifold positive-volume meshes, six packed maps with correct color spaces, 2,848 valid UV samples per map and a passing FBX round trip. The same-scene and close-up renders were visually reviewed and are retained in ignored `Logs/LimestoneRock01/WhiteV2/`.

Tools: [source maps](../../../../../../Tools/Art/Blender/generate_white_limestone_maps.py), [bake and apply](../../../../../../Tools/Art/Blender/apply_approved_white_limestone.py), and [read-only verification](../../../../../../Tools/Art/Blender/verify_approved_white_limestone.py). The bake/apply tool preserves a backup and updates the existing scene file; it does not create another Blender project. Its `refresh-export` option only refreshes the task-owned FBX without saving the scene.

This material-only revision does not change world identity, chunk streaming, generated-object identity, authored world placement or persisted runtime deltas. No Unity assets, metadata, package, scene or gameplay state was changed. Unity import, shader appearance, LOD/collider configuration and runtime performance remain separate proof. Blender sources and backups remain local-only. The prior workbench-derived texture/export set remains retained as an earlier version, rather than being overwritten.
