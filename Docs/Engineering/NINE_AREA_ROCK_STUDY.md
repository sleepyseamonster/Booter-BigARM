# Nine-area rock study — October 9, 2026

The author requested nine flat work areas in the existing study scene, one rock generator/workbench per area, one common Greater Wasteland base texture and the authoring panels ready for texture/terrain painting.

Current scene: `Assets/_Project/Scenes/Reference/FixedTerrainDressingStudy.unity`. Its original scene GUID is preserved. The earlier six-panel contact/relief comparison remains separately at `Assets/_Project/Scenes/Reference/FixedTerrainDressingComparison.unity`, using its original mesh/material assets. Dated initial evidence retains the original filename as historical scope; it does not describe the current nine-area contents.

## Work areas

- 3 by 3 grid; each native terrain is 16 by 16 m, with 4 m gaps between areas.
- Each area contains only `Flat terrain` and one standalone `Rock Workbench`. Its editable source volumes are generator internals, not additional rock formations or clutter.
- Nine independent TerrainData assets start with flat zero heights and one base layer. Alpha maps are 128 by 128, giving 0.125 m weight pixels in these study spaces; production paint-map resolutions are unchanged. Native terrain has a 4 m height range, with the initial surface at its bottom.
- All nine use Greater Wasteland's existing `BadwaterTerrainURP` material and a study-owned clone of the `r11_c08` TerrainLayer. That tile was chosen with seed `20261009` from the original Badwater tile set. It is part of Greater Wasteland's retained production terrain.
- The clone references the original GeographicColor texture. Its tile size and offset are both rescaled by 16/256, preserving the sampled source region on each smaller work area. The original layer, material, image and production TerrainData are untouched.
- Each workbench starts from existing Golden Rock controls, with a distinct saved seed. It retains the normal Generate New Rock, Regenerate This Rock, source-volume editing and surface-language workflow. Editor preview meshes/colliders reconstruct from these controls on load.
- One overview camera and one light sit outside the nine areas. There are no players, atmosphere owners, formations, decorative fragments or prepainted texture stickers.

## Panels

**Booter & BigARM > Geology Studies > Open Nine-Area Authoring Panels** reopens this setup when the study is active:

- **Ground Textures**: manual sticker placement and native texture brush. It starts on Paint terrain with painting stopped.
- **Rock Workbench**: choose Area 01–09 and edit that area's generator through its existing Inspector controls. Select/frame that area's terrain with the provided buttons.
- **Inspector**: starts with Area 01's native terrain selected and Unity's Paint Texture tool active. Other Unity terrain sculpt/paint tools remain accessible through its terrain-tool controls.
- **Scene view**: frames the complete grid at an elevated angle. Authoring windows are positioned over the Project-panel region to keep Scene view and the terrain Inspector visible. No window focus/foreground call is made.

The panel setup unlocks and refreshes the shared Inspector tracker so it follows terrain/sticker selection. Its Paint Texture selection uses the inspected method in the pinned Unity 6000.4 TerrainInspector, guarded by type/method availability. This is UI state only, not a renderer/package/project setting or a painting operation. Stop the custom brush before using Unity's native terrain brush, so only one tool owns Scene-view painting input.

## Ownership and proof

Painting/sculpting here changes study-owned TerrainData; stickers persist in the study scene. These are bounded authored experiments, independent of world seed, terrain streaming and gameplay saved deltas. Rock seeds identify reproducible editor source geometry, not globally unique generated gameplay entities. Future production reuse needs deliberate asset handoff and stable feature ownership.

The builder runs in an isolated background batchmode project and refuses to overwrite existing work-area assets. Do not rebuild over artist edits. Current focused checks establish nine flat areas, exactly one editable generator each, common material/layer identity, relative texture coordinates, missing-script safety and build exclusion. The earlier comparison and texture tool tests remain relevant regressions. The live Editor readback verifies nine terrain objects, nine ready rock previews, clean scene state and the authoring windows.

No gameplay smoke test, foreground activation, production painting, generation/streaming activation, package change or Player-performance claim is part of this setup.
