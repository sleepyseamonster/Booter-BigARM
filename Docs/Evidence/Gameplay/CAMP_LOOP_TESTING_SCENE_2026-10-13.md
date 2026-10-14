# Camp-loop testing scene — October 13, 2026

The author requested the current Greater Wasteland camp playtesting slice in a smaller scene containing the camp's terrain tile and its eight neighbors. The result is [CampLoopTesting.unity](../../../Assets/_Project/Scenes/Reference/CampLoopTesting.unity), a separate reference/testing scene. Greater Wasteland remains the sole enabled production build scene.

## Retained content

- Nine original native Terrain/TerrainCollider pairs, each 256 × 256 m: a complete 3 × 3 grid covering x = -2304 to -1536 and z = -1792 to -1024.
- Center tile: `Badwater_2m_r13_c00`, containing Starting Camp at (-1842, -48.08643, -1498). All authored return-route points remain supported.
- Original Booter and Legger actors, their positions, cargo mounts, player input, movement/traversal components, survival systems and camera.
- Original authored arrangement of seven camp rocks, preserving prefab links, transforms and placement IDs.
- Twelve ironstone nodes and the starting resource cache, explicitly saved as linked instances of the existing production overlay prefabs.
- Original scene lighting/RenderSettings, dust atmosphere, HUD and EventSystem.
- Existing runtime camp/sleep, waypoint return/regroup, compass, inventory, radial, system-menu and harvester controls, enabled for this exact testing-scene path.

The extracted tiles are `Badwater_2m_r12_c00`, `Badwater_1m_r12_c01`, `Terrain_west_2m_r12_c15`, `Badwater_2m_r13_c00`, `Badwater_1m_r13_c01`, `Terrain_west_2m_r13_c15`, `Badwater_2m_r14_c00`, `Badwater_2m_r14_c01`, and `Terrain_west_2m_r14_c15`.

## Identity, lifecycle and persistence

The terrain remains fixed, authored and always loaded. No terrain generation, terrain streaming or generated-world save service is added. Only nine tiles render in this scene; the production terrain render-residency controller is unnecessary for this bounded grid.

World coordinates, camp identity, authored rock placement IDs, ironstone IDs, cache ID and harvester world identity are retained. There are no newly generated objects or dressing cells, so chunk-stream unload/reload and generated-object identity do not apply. Scene reload uses the existing authored installers and checkpoint restoration; the ironstone/cache installers detect the saved instances rather than duplicating them.

Runtime deltas use the existing authored checkpoint format and writer, routed to `Application.persistentDataPath/CampLoopTesting/inventory-v1.json`. Production keeps `GreaterWasteland/inventory-v1.json`. This separates testing inventory, depletion, survival, actor positions, harvesters and deployed camp changes from the production profile. No saved production checkpoint is imported into the test slice.

TerrainData and other authored assets remain shared references. Terrain sculpting or editing shared materials/prefabs affects both scenes. This task creates a separate scene, not independent source-art or terrain-asset copies. The test footprint has natural terrain edges; no new boundary walls or gameplay constraints were introduced.

## Current-machine verification

Unity 6000.4.0f1 ran in hidden background batchmode against this checkout with no other target-project Editor running. No app was focused and no Play Mode or gameplay smoke test was run.

- Unity import and compilation passed.
- `CampLoopTestingSceneBuilder.Validate` passed against the completed scene, checking the native grid/collision, camp/route and actor coverage, seven rock identities, twelve ironstone identities, cache, controls, camera, lighting, missing scripts, prohibited generated services and build routing.
- 52 focused EditMode checks passed, zero failures/skips: `CampLoopTestingSceneTests`, `TopDown3DWaypointRouteTests`, `TopDown3DCampTests`, `TopDown3DCampInputTests`, `TopDown3DCompassNavigationTests`, and `TopDown3DRadialTests`.
- The new fixture checks native TerrainData references against production, camp-route height support, runtime UI/camp/compass/harvester installation, installer idempotency and a separate checkpoint path. It never writes checkpoint files; UI preferences use a disposable temporary directory.
- Serialized scene audit found nine terrains and zero dangling local fileID references. RenderSettings are identical to production.
- Production scene remained byte-identical to the starting revision: SHA-256 `bc0b465de9a1b75b82a20d99c25fa4f10f00377a18f9564f9e8b0516a4c8f93e`.
- Existing untracked `Art/Environment/Rocks/Fused` work stayed outside this task and its commit. No package, project setting, production scene, native terrain asset or build-routing changes were made.

Ignored diagnostics: `Logs/camp-loop-complete.log`, `Logs/camp-loop-tests.log`, `Logs/camp-loop-tests.xml`, and `Logs/camp-loop-structure.json`. An initial extraction validator inspected uninitialized runtime node IDs in Edit Mode; completion corrected the validator to inspect the serialized authored ironstone identities and saved linked prefab instances. Final import, validation and all tests passed.

Hands-on visual, controller-feel, camp-loop acceptance, disk-checkpoint round trips and Player-build performance are not claimed by these structural/EditMode checks. Open CampLoopTesting and use the existing controls for the author-owned playtest.

## Visibility adjustment — October 13, 2026

The author reported camp rocks and ironstone disappearing too close to the player. Their final LOD threshold was 0.015 (1.5% screen height); this could hide small landmarks well inside the camera's view range. The seven shared authored camp-rock prefabs and `IronstoneNode.prefab` now retain their existing inexpensive third LOD down to a zero screen-height threshold. Camera/frustum clipping still applies. Higher-detail transitions, mesh/material references, colliders, placement IDs, terrain and gameplay state are unchanged. This shared-prefab adjustment also applies to Greater Wasteland. No procedural generation, streaming or persistence identity is changed.

The running author's graphics preference was Low (500 m). It was changed through the existing settings service to High (2,000 m), saved in the local menu-settings profile, and applied to the current camera. This local preference is not a repository/project-setting change.

All eight changed prefabs imported through the existing background CLI connection. The existing `Camp1AuthoredRockTests.AuthoredPrefabUsesSevenDistinctReadyFamiliesAndFixedIdentities` check passed directly without invoking scene-changing TestRunner operations. Readback verified all nineteen live camp rock/ironstone groups retain three populated LODs with the final threshold zero, and the current camera reports High / 2,000 m. The ongoing Play Mode session was preserved; no scene was saved, no app was focused, and no gameplay smoke test was run. Visual acceptance remains with the author.

## Shadow visibility adjustment — October 13, 2026

The author reported shadows disappearing too close and requested longer visibility, following the rock-distance adjustment. The active shared URP asset's shadow distance increased from 60 m to 500 m. Its four cascade splits changed from (0.12, 0.34, 0.65) to (0.025, 0.08, 0.30), placing the first three boundaries at 12.5, 40 and 150 m while extending the last cascade to 500 m. The existing 4096 main-light atlas, high-quality soft shadows, shadow bias and final cascade fade remain unchanged. This balances nearby detail with the expanded distant coverage; distant shadows use the existing final cascade's coarser resolution.

The change applies to CampLoopTesting and Greater Wasteland through their shared rendering asset. It changes only visual shadow coverage. Fixed terrain, collision, object identities, authored placement, generation/streaming behavior and checkpoint deltas are unchanged. Greater Wasteland's existing render-residency controller already reads the active pipeline shadow distance when collecting shadow coverage; no new terrain-streaming system is introduced.

Unity 6000.4.0f1 imported the asset through the existing background connection. Active-pipeline readback confirmed 500 m, four cascades, the exact new splits and the unchanged 4096 atlas. The existing `PerpetualTwilightSunTests.Pipeline_SupportsReadableMainLightShadows` check passed directly, and the camp-loop structural validator passed against the open scene. No new test, Play Mode session, gameplay smoke test, scene save or app focus was introduced. Visual acceptance and performance at the increased shadow range remain author-owned; these checks establish settings and structural validity.
