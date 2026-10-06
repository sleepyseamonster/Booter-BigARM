# Badwater four-slice Unity terrain scene

**State (2026-10-06):** `Assets/_Project/Scenes/TopDown3D/BadwaterFourSlices.unity` is a separate Unity **Scene view** terrain study. It is not added to Build Settings and does not alter `TopDown3DPrototype.unity`.

## Open and inspect

In Unity's Project window, open `Assets/_Project/Scenes/TopDown3D/BadwaterFourSlices.unity`. Select the `Badwater Four Slices` root in the Hierarchy and press **F** in Scene view to frame the terrain. The view spans 4.096 × 4.096 km. The scene has a review sun and terrain colliders, but no player, gameplay camera, rock instances, or runtime streaming.

## Geography and source

- EPSG:26911 bounds: `[520400,4006200,524496,4010296]`; Unity local origin is UTM `(522448,4008248)`. Unity X points east and Z north. Heights are NAVD88 metres; the Terrain object bases sit at `Y=-100 m` with a `1800 m` vertical range.
- 256 independently named 256 × 256 m Unity Terrain objects preserve the Blender `rNN_cNN` tile identities. The four focus tiles at rows 12–13, columns 1–2 have 257² samples at 1 m spacing. The other 252 have 129² samples at 2 m spacing.
- Each TerrainData asset includes its own collider and a linked 128² geographic color tile. The colors come from the aligned 2 m USGS NAIP composite with Landsat fallback. A shared URP Terrain Lit material displays the TerrainLayers. This is a geographic color reference, not the complete procedural close material from Blender.
- RAW source tiles, color tiles, tile list, and source hashes are kept under `Assets/_Project/Art/Terrain/BadwaterFourSlices/Source/`. The source remains reproducible from the external `four_slices_v1` data manifest via `export_badwater_unity_source.py`; `BadwaterUnitySceneBuilder.cs` builds or verifies it in an isolated Unity 6000.4.0f1 project.

## Validation and limits

The scene and assets were built and reopened in an isolated Unity 6000.4.0f1 project with URP 17.4.0 while the main project remained open. The isolated `ValidateFromCli` readback found all **256 terrains**, all **four 1 m focus tiles**, valid terrain colliders, geographic texture links, and a shared URP Terrain Lit material. The largest saved Unity height readback difference from the encoded source was **0.02747 m**; the largest sampled border mismatch after readback was **0.02750 m**.

Unity Terrain uses regular heightmap grids. The four 1 m focus tiles meet 2 m tiles on their outer perimeter. Only the intervening 1 m vertices on those **outer edges** are set to the straight interpolation between the neighboring 2 m samples so their borders meet. The largest such adjustment was **1.1810 m**. All interior focus samples remain the recorded DEM heights. The Blender scene still retains its exact source-edge geometry.

This is an always-loaded review scene. The 256 Unity Terrain objects are individually addressable but have no runtime load or unload system, no game-world placement rules, and no persisted terrain deltas. The source bounds plus `rNN_cNN` identifiers are stable; future streaming can use those identities without putting mutable save state in the scene. Performance and visible appearance in the main open Unity editor have not yet been accepted by an interactive Scene view review.

## Rebuild

Prepare `Source/` with the Python script using `four_slices_v1/manifest.json` and `geographic_color.png`. Place the C# builder in an isolated project's `Assets/Editor/`, then run `BadwaterUnitySceneBuilder.BuildFromCli`, `ApplyUrpMaterialFromCli`, and `ValidateFromCli` through Unity batch mode. Copy the resulting scene, TerrainData, TerrainLayers, material, source tiles, and their `.meta` files into the main project together. Do not run batch mode against the main project while its Unity GUI is open.
