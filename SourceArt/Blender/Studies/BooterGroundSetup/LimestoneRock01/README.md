# Limestone rock 01

October 12, 2026. The author requested the rock sculpt in the Booter ground scene be reduced as far as practical while preserving its shape and detail, and textured with a new light limestone derivative of the dark workbench rock texture.

Open the local `LimestoneRock01_GameReady.blend` beside this note. It is a separate version of the Booter ground scene, with `LimestoneRock01_LOD0` selected and textured. The original `Icosphere` and the high-detail bake source remain hidden. The artist's saved `../BooterGroundSetup.blend` was not overwritten. All six texture maps are packed into the delivered file, including maps not needed by the Blender preview shader. Blender files and numbered backups remain local-only.

| Mesh | Triangles | Mesh vertices |
| --- | ---: | ---: |
| Artist sculpt | 5,120 | 2,562 |
| Visible LOD0 | 712 | 358 |
| Distant LOD1 | 356 | 180 |
| Distant LOD2 | 178 | 91 |
| Convex collision proxy | 220 | 112 |

LOD0 reduces triangle count by 86.1%. These are mesh topology counts; a game importer may split rendering vertices at UV or tangent seams. The optional distant LODs share the texture set. Their transition distances require Unity integration and visual acceptance.

## Reduction evidence

The [preparation tool](../../../../../Tools/Art/Blender/prepare_limestone_rock.py) tested 64 through 3,072 triangles, then refined the first passing bracket to eight triangles. The lowest passing tested candidate was 712; 704 failed the silhouette threshold. This is a measured practical selection, not proof of a mathematical global minimum or of every possible camera view.

Selection gates were closed manifold geometry, 95th-percentile sampled bidirectional surface error at most 8 mm, maximum sampled error at most 20 mm, and minimum silhouette intersection-over-union of 99.4%. The selected mesh measured 6.95 mm, 19.37 mm and 99.416%, respectively. Silhouettes were tested along eight directions on a 192-pixel orthographic grid. Surface comparison included all source vertices plus 10,000 area-weighted triangle samples in each direction. Front, back, top, underside, and high-source comparison renders were visually reviewed. Small surface differences are transferred into the tangent normal bake; silhouette changes cannot be recovered by a normal map.

## Texture set and export

The source is the workbench material's [dark side albedo](../../../../../Assets/_Project/Art/Environment/Rocks/Workbench/Layered/RockWorkbenchSide_Albedo.png) and matching [surface map](../../../../../Assets/_Project/Art/Environment/Rocks/Workbench/Layered/RockWorkbenchSide_Surface.png). The surface channels are R occlusion, G roughness, B height. A limestone palette is applied in the Blender material, retaining the original source pattern rather than generating unrelated cracks or grain. A 1.1 m projection scale matches the workbench's side texture scale. The original source textures remain unchanged.

The [Exports folder](Exports/) contains `LimestoneRock01.fbx` and six 2,048 by 2,048 PNG maps:

- BaseColor: sRGB, light warm limestone with source tonal variation.
- Normal: linear, tangent space, +Y; baked from the sculpt plus restrained workbench height relief.
- Roughness: linear, matte range 0.65–0.95.
- Occlusion: linear, source micro-occlusion multiplied by sculpt geometry occlusion.
- Height: linear, projected source micro-height; optional, not a geometry displacement requirement.
- MetallicSmoothness: linear, RGB zero for nonmetal stone, alpha equal to one minus roughness.

The FBX contains only LOD0, LOD1, LOD2 and collision, with a bottom-centre pivot, metre units, unit object scale, baked transforms, smoothing and tangents, no animation, and Blender export axes `-Z` forward / `Y` up. It excludes Booter, the ground, sculpt source, lights and camera. The Blender preview uses the baked base color, roughness and normal map. Engine occlusion and metallic/smoothness bindings belong to the Unity handoff.

The method follows Blender's [Decimate documentation](https://docs.blender.org/manual/en/5.2/modeling/modifiers/generate/decimate.html) and [selected-to-active baking workflow](https://docs.blender.org/manual/en/5.2/render/cycles/baking.html). The installed runtime is Blender 5.2.2 LTS. Bakes use Cycles, 24-pixel island dilation, a 35 mm projection cage and 80 mm ray limit.

## Verification and boundaries

[delivery.json](delivery.json) records source hashes, candidates and output counts. [verification.json](verification.json) records a fresh reopen, manifold/loose/degenerate/volume checks, UV bounds, 2,848 samples per texture, finite pixels, valid tangent normals, packed image retention, and an FBX reimport with matching names, triangle counts and dimensions. The [verification tool](../../../../../Tools/Art/Blender/verify_limestone_rock.py) does not save the scene. [Finalization](../../../../../Tools/Art/Blender/finalize_limestone_rock.py) retains all optional maps and adds the in-file instructions. Diagnostic renders and the advisory `inspect_blend.py` inventory remain in ignored `Logs/LimestoneRock01/`.

No Unity production asset, scene, material, metadata, package, settings or gameplay behavior was changed. These are export-ready artifacts; current Unity import, material appearance, collider/LOD setup and runtime performance are not yet verified. World seed, streaming, generated-object identity, authored world placement and persisted runtime deltas are not applicable to this independent static asset preparation. Those contracts must be addressed when it is integrated or placed in Greater Wasteland.
