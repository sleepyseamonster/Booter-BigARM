# Badwater four-slice Unity terrain scene

**State (2026-10-06):** `Assets/_Project/Scenes/TopDown3D/BadwaterFourSlices.unity` is a separate, playable Unity terrain scene. It contains the production Booter controller, Legger follower, input router, orbit camera, game and inventory HUD, and the production twilight/dust lighting. It is not added to Build Settings and does not alter `TopDown3DPrototype.unity`.

## Open and inspect

In Unity's Project window, open `Assets/_Project/Scenes/TopDown3D/BadwaterFourSlices.unity`. Select the `Badwater Four Slices` root in the Hierarchy and press **F** in Scene view to frame the 4.096 × 4.096 km terrain. Press **Play** to start near the basin at UTM `(520600,4006750)`. The existing TopDown3D controls operate Booter and call Legger; the scene includes the canonical game and inventory HUD. `BadwaterCameraRange` extends this scene's camera distance to 8 km so the mountains remain visible without changing the production camera rig.

The scene transfers the actual configured production objects, rather than duplicating their setup in new gameplay code. The earlier `Review Sun` is removed; `Directional Key Light` with `PerpetualTwilightSun` and `Dust Atmosphere` own lighting and atmosphere. No rocks or other generated game-world assets were added. The imported terrain colliders remain active. The production procedural terrain generator and coordinated save service are intentionally absent from this bounded exploration scene.

## Geography and source

- EPSG:26911 bounds: `[520400,4006200,524496,4010296]`; Unity local origin is UTM `(522448,4008248)`. Unity X points east and Z north. Heights are NAVD88 metres; the Terrain object bases sit at `Y=-100 m` with a `1800 m` vertical range.
- 256 independently named 256 × 256 m Unity Terrain objects preserve the Blender `rNN_cNN` tile identities. The four focus tiles at rows 12–13, columns 1–2 have 257² samples at 1 m spacing. The other 252 have 129² samples at 2 m spacing.
- Each TerrainData asset includes its own collider and a linked 128² geographic color tile. The colors come from the aligned 2 m USGS NAIP composite with Landsat fallback. A shared URP Terrain Lit material displays the TerrainLayers. This is a geographic color reference, not the complete procedural close material from Blender.
- RAW source tiles, color tiles, tile list, and source hashes are kept under `Assets/_Project/Art/Terrain/BadwaterFourSlices/Source/`. The source remains reproducible from the external `four_slices_v1` data manifest via `export_badwater_unity_source.py`; `BadwaterUnitySceneBuilder.cs` builds or verifies it in an isolated Unity 6000.4.0f1 project.

## Validation and limits

The scene and assets were built and reopened in an isolated Unity 6000.4.0f1 project with URP 17.4.0 while the main project remained open. The isolated `ValidateFromCli` readback found all **256 terrains**, all **four 1 m focus tiles**, valid terrain colliders, geographic texture links, and a shared URP Terrain Lit material. The largest saved Unity height readback difference from the encoded source was **0.02747 m**; the largest sampled border mismatch after readback was **0.02750 m**.

Unity Terrain uses regular heightmap grids. The four 1 m focus tiles meet 2 m tiles on their outer perimeter. Only the intervening 1 m vertices on those **outer edges** are set to the straight interpolation between the neighboring 2 m samples so their borders meet. The largest such adjustment was **1.1810 m**. All interior focus samples remain the recorded DEM heights. The Blender scene still retains its exact source-edge geometry.

This is an always-loaded exploration scene. The 256 Unity Terrain objects are individually addressable but have no runtime load or unload system, no game-world placement rules, and no persisted terrain deltas. The source bounds plus `rNN_cNN` identifiers are stable; Booter and Legger are authored scene instances. Future streaming can use the terrain identities without putting mutable save state in the scene. Performance, controls, and visible appearance in the main open Unity editor have not yet been accepted by an interactive Play Mode review.

The playable assembly was built from the saved production scene in an isolated copy of the project. `BadwaterPlayableSceneBuilder.ValidateFromCli` reopened the result and checked all 256 terrains, Booter, Legger, input, camera, audio, HUD, twilight light, dust atmosphere, and measured spawn heights. It also checked that the procedural terrain generator and production save service are absent. This is structural/editor proof; it is not a Play Mode acceptance test.

## Rebuild

Prepare `Source/` with the Python script using `four_slices_v1/manifest.json` and `geographic_color.png`. Place the C# builder in an isolated project's `Assets/Editor/`, then run `BadwaterUnitySceneBuilder.BuildFromCli`, `ApplyUrpMaterialFromCli`, and `ValidateFromCli` through Unity batch mode. Copy the resulting scene, TerrainData, TerrainLayers, material, source tiles, and their `.meta` files into the main project together. Do not run batch mode against the main project while its Unity GUI is open.

For the playable assembly, run `BooterBigArm.Editor.BadwaterPlayableSceneBuilder.BuildFromCli` against a terrain-only copy of this scene in an isolated full-project mirror, then `ValidateFromCli`. The builder transfers only the play roots listed in its source and refuses to duplicate them if the destination is already playable. Copy only the validated scene and any new script `.meta` files back. It never writes the production prototype scene.
