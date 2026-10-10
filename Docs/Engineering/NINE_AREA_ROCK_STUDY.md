# Nine-area rock study — October 9, 2026

The author requested nine flat work areas in the existing study scene, one rock generator/workbench per area, one common Greater Wasteland base texture and the authoring panels ready for texture/terrain painting.

Current scene: `Assets/_Project/Scenes/Reference/FixedTerrainDressingStudy.unity`. Its original scene GUID is preserved. The earlier six-panel contact/relief comparison remains separately at `Assets/_Project/Scenes/Reference/FixedTerrainDressingComparison.unity`, using its original mesh/material assets. Dated initial evidence retains the original filename as historical scope; it does not describe the current nine-area contents.

## Work areas

- 3 by 3 grid; each native terrain is 16 by 16 m, with 4 m gaps between areas.
- Each area contains only `Flat terrain` and one standalone `Rock Workbench`. Its editable source volumes are generator internals, not additional rock formations or clutter.
- Nine independent TerrainData assets start with flat zero heights and one base layer. Alpha maps are 128 by 128, giving 0.125 m weight pixels in these study spaces; production paint-map resolutions are unchanged. Native terrain has a 4 m height range, with the initial surface at its bottom.
- All nine use Greater Wasteland's existing `BadwaterTerrainURP` material and a study-owned clone of the `r11_c08` TerrainLayer. That tile was chosen with seed `20261009` from the original Badwater tile set. It is part of Greater Wasteland's retained production terrain.
- The clone references the original GeographicColor texture at its original world scale. Each 16 m pad samples a 16 m patch around the chosen 256 m source tile's centre: tile size stays 4096 m and offset is 2168,1144 m. The October 10 correction replaces the earlier whole-tile miniature. This preserves the source image's resolution; it does not create additional fine detail. The original layer, material, image and production TerrainData are untouched.
- Each workbench starts from existing Golden Rock controls, with a distinct saved seed. It retains the normal Generate New Rock, Regenerate This Rock, source-volume editing and surface-language workflow. Editor preview meshes/colliders reconstruct from these controls on load.
- One overview camera and one Greater Wasteland sun sit outside the nine areas. The sun includes the original twilight controller, cycle, direction, colour, intensity and shadow settings. Saved ambient, skybox, reflection and fog settings match the production scene; the overview camera uses that skybox. No players, dust/atmosphere owners, formations, decorative fragments or prepainted texture stickers are added.

## Panels

**Booter & BigARM > Geology Studies > Open Nine-Area Authoring Panels** reopens this setup when the study is active:

- **Ground Textures**: manual sticker placement and native texture brush. It starts on Paint terrain with painting stopped.
- **Rock Workbench**: choose Area 01–09 and edit that area's generator through its existing Inspector controls. Select/frame that area's terrain with the provided buttons.
- **Inspector**: starts with Area 01's native terrain selected and Unity's Paint Texture tool active. Other Unity terrain sculpt/paint tools remain accessible through its terrain-tool controls.
- **Scene view**: frames the complete grid at an elevated angle. Authoring windows are positioned over the Project-panel region to keep Scene view and the terrain Inspector visible. No window focus/foreground call is made.

The panel setup unlocks and refreshes the shared Inspector tracker so it follows terrain/sticker selection. Its Paint Texture selection uses the inspected method in the pinned Unity 6000.4 TerrainInspector, guarded by type/method availability. This is UI state only, not a renderer/package/project setting or a painting operation. Stop the custom brush before using Unity's native terrain brush, so only one tool owns Scene-view painting input.

## Ownership and proof

Painting/sculpting here changes study-owned TerrainData; stickers persist in the study scene. These are bounded authored experiments, independent of world seed, terrain streaming and gameplay saved deltas. Rock seeds identify reproducible editor source geometry, not globally unique generated gameplay entities. Future production reuse needs deliberate asset handoff and stable feature ownership.

The builder runs in an isolated background batchmode project and refuses to overwrite existing work-area assets. Do not rebuild over artist edits. `NineAreaLightingAndGround.Apply()` aligns a clean, active study without changing terrain heights, painted weights or rock controls. Its small, dated lighting reference was extracted from Greater Wasteland's saved scene, so alignment does not load thousands of production terrain tiles. This is a snapshot: deliberate later world-lighting changes need a new reference. It does not copy baked lightmaps or runtime post-processing/dust owners.

The October 10 focused checks pass 3/3 and establish nine flat areas, exactly one editable generator each, common material/layer identity, the small patch at native texture scale, matching sun and serialized environment settings, missing-script safety and build exclusion. Evidence is in `Docs/Evidence/WorldCreator/NineAreaRockStudy/lighting-ground-2026-10-10.json` and its test XML. The earlier comparison and texture tool tests remain relevant regressions. The live Editor readback verifies the active study, nine terrains and the copied sun. Later in-memory dirty state after domain reload was preserved rather than saved over possible author edits.

No gameplay smoke test, foreground activation, production painting, generation/streaming activation, package change or Player-performance claim is part of this setup.
