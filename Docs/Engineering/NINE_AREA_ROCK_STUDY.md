# Nine-area rock study â€” October 9, 2026

On October 10, 2026, the author saved a simplified study with one terrain area. This saved layout supersedes the original nine-area layout. Continue cube authoring here without restoring the grid or adding authoring controls.

The remaining native terrain is the 16 by 16 m pad under Area 05, using `Area_05.asset`. The saved scene contains three volcanic rock cubes, two sculpted basalt instances, the existing comparison workbench, the overview camera and the sun. Area 01–04 and 06–09 remain empty containers. Their old terrain assets are retained; they are not instantiated in the current scene. Source cube poses and other artist edits are authoritative. Saving persists the cube recipe; the generated exterior remains transient.

World identity, streaming, generated gameplay identity and runtime deltas are outside this authored Editor study. Production terrain and save systems are unchanged.

Current-machine verification on October 10 passed 3/3 focused EditMode checks in an isolated Unity 6000.4.0f1 copy: single-terrain saved content and volcanic cube references, retained native-scale texture sampling, and retained lighting/environment identity. Missing scripts, transient-preview serialization and build exclusion are checked. The saved scene was copied byte-for-byte; its SHA-256 is `5F88390731F827356A417CDF0F20724A07A0C4BC93B458DE5C8A39A4618AE383`. No visual or gameplay acceptance is implied.

Current scene: `Assets/_Project/Scenes/Reference/FixedTerrainDressingStudy.unity`. Its original scene GUID is preserved. The earlier six-panel contact/relief comparison remains separately at `Assets/_Project/Scenes/Reference/FixedTerrainDressingComparison.unity`, using its original mesh/material assets. Dated initial evidence retains the original filename as historical scope; it does not describe the current single-terrain contents.

## Original work areas — historical setup

- 3 by 3 grid; each native terrain is 16 by 16 m, with 4 m gaps between areas.
- Each area contains `Flat terrain` and one standalone `Rock Workbench`. Its editable source volumes are generator internals, not additional rock formations or clutter. On October 10, the author added [SculptedBasalt01](SCULPTED_BASALT_01.md) at the centre of Area 05, at authored metre scale and ground pivot. Its existing workbench moves 5 m to the side so both remain visible.
- Nine independent TerrainData assets start with flat zero heights and one base layer. Alpha maps are 128 by 128, giving 0.125 m weight pixels in these study spaces; production paint-map resolutions are unchanged. Native terrain has a 4 m height range, with the initial surface at its bottom.
- All nine use Greater Wasteland's existing `BadwaterTerrainURP` material and a study-owned clone of the `r11_c08` TerrainLayer. That tile was chosen with seed `20261009` from the original Badwater tile set. It is part of Greater Wasteland's retained production terrain.
- The clone references the original GeographicColor texture at its original world scale. Each 16 m pad samples a 16 m patch around the chosen 256 m source tile's centre: tile size stays 4096 m and offset is 2168,1144 m. The October 10 correction replaces the earlier whole-tile miniature. This preserves the source image's resolution; it does not create additional fine detail. The original layer, material, image and production TerrainData are untouched.
- Each workbench starts from existing Golden Rock controls, with a distinct saved seed. It retains the normal Generate New Rock, Regenerate This Rock, source-volume editing and surface-language workflow. Editor preview meshes/colliders reconstruct from these controls on load.
- One overview camera and one Greater Wasteland sun sit outside the nine areas. The sun includes the original twilight controller, cycle, direction, colour, intensity and shadow settings. Saved ambient, skybox, reflection and fog settings match the production scene; the overview camera uses that skybox. No players, dust/atmosphere owners, formations, decorative fragments or prepainted texture stickers are added.

## Minimal cube rock authoring

The initial October 10 [minimal cube implementation](../Evidence/Art/SmoothCubeRock/README.md) adds one `SmoothRockCube` prefab instance to the live Area 05 at `(-3, 0.5, 0)`, preserving the existing workbench and artist basalt poses. That delivery left the previously dirty scene unsaved; the author has since saved the current three-cube layout. The reusable prefab is `Assets/_Project/Prefabs/TopDown3D/Environment/SmoothRockCube.prefab`; normal scene Save persists the live insertion, and an unsaved insertion has Unity's ordinary unsaved-scene lifetime.

Duplicate the cube and use normal Move, Rotate and Scale tools. Touching groups automatically rebuild into one textured exterior after movement settles. Separate them to restore independent cubes. Select an individual source in the Hierarchy, or click the fused exterior to choose a contributing cube; the selected source has a wire outline and normal Transform handles. Fully contained and coincident sources remain accessible in the Hierarchy. No workbench, authoring root, panel, button, slider, source ID or exposed quality setting is added.

The Editor computes a bounded spherical closing of the exact box union, then uses the existing mesh extractor. This preserves duplicate neutrality and input-order independence and rounds reentrant junctions. The [close-up join correction](../Evidence/Art/SmoothCubeRock/SMOOTH_JOIN_2026-10-10.md) removes lattice terraces by smoothing the field and reconstructing its curves before extracting a denser mesh. The editing cubes remain simple; no user subdivision or settings are required. Face, edge and corner contacts are supported by the focused fixtures, including rotations and grid offsets. The author's texture correction selects [volcanic basalt](../Evidence/Art/SmoothCubeRock/VOLCANIC_TEXTURE_2026-10-10.md): a cube-owned material uses the original tiling volcanic maps at 1.25 m per repeat in a fixed world coordinate frame. Group membership does not reset the textures. These are authored study inputs, not world-generation or save-game identities.

This first slice is Edit Mode only, visible in Scene and Game views without entering Play Mode. Saving stores the cube recipe; transient exterior meshes reconstruct on Editor reload and are omitted from scene files and builds. Entering Play Mode restores the source cubes, and returning to Edit Mode rebuilds the preview. There are no gameplay colliders, runtime remesher, LOD/export tools or production terrain changes. Invalid/sheared/mirrored transforms or an oversized group retain visible sources and report a concise Console warning. Internal limits are 32 sources, one million coarse closing samples, 1.6 million extended samples and four million refined extraction samples. Closing uses fixed 0.04 m spacing; the smooth exterior is extracted at 0.02 m. Sampling is not silently coarsened.

The [delivery evidence](../Evidence/Art/SmoothCubeRock/README.md) distinguishes 27 passing current-machine checks, isolated material captures and live placement from author-owned visual acceptance and arbitrary-stack/player-performance proof.

## Retained authoring panels

The study overview camera carries a scene rendering override that suppresses tilt shift in this scene's Game and active Scene views. Greater Wasteland and the shared renderer retain their existing settings. This is independent of the study's matched sun and environment lighting.

**Booter & BigARM > Geology Studies > Open Nine-Area Authoring Panels** reopens this setup when the study is active:

- **Ground Textures**: manual sticker placement and native texture brush. It starts on Paint terrain with painting stopped.
- **Rock Workbench**: choose Area 01â€“09 and edit that area's generator through its existing Inspector controls. Select/frame that area's terrain with the provided buttons.
- **Inspector**: starts with the remaining native terrain selected and Unity's Paint Texture tool active. Other Unity terrain sculpt/paint tools remain accessible through its terrain-tool controls.
- **Scene view**: frames the selected terrain at an elevated angle. Authoring windows are positioned over the Project-panel region to keep Scene view and the terrain Inspector visible. No window focus/foreground call is made.

The panel setup unlocks and refreshes the shared Inspector tracker so it follows terrain/sticker selection. Its Paint Texture selection uses the inspected method in the pinned Unity 6000.4 TerrainInspector, guarded by type/method availability. This is UI state only, not a renderer/package/project setting or a painting operation. Stop the custom brush before using Unity's native terrain brush, so only one tool owns Scene-view painting input.

**Booter & BigARM > Geology Studies > Terrain Sculpting Controls** opens a dedicated
**Terrain Controls** tab with Raise / Lower, Smooth and Set Height buttons and the
native terrain brush settings. It keeps its terrain target when scene selection
changes, avoiding the blank generic Inspector tab. It defaults to the selected
terrain, or Area 05 when opened fresh in this study. Clicking a sculpt button
selects that terrain, stops the custom texture brush and activates the native
height tool. The panel scrolls within small floating windows. Opening or binding
it does not apply paint, change heights, save assets, or modify world identity,
streaming, generated-object identity, placement constraints or persisted deltas;
those concerns are outside this editor-only UI setup.

## Ownership and proof

Painting/sculpting here changes study-owned TerrainData; stickers persist in the study scene. These are bounded authored experiments, independent of world seed, terrain streaming and gameplay saved deltas. Rock seeds identify reproducible editor source geometry, not globally unique generated gameplay entities. Future production reuse needs deliberate asset handoff and stable feature ownership.

The builder runs in an isolated background batchmode project and refuses to overwrite existing work-area assets. Do not rebuild over artist edits. The builder creates the initial nine-area setup; the subsequent artist-rock placement is scene-owned content, not an additional generator default. `NineAreaLightingAndGround.Apply()` aligns a clean, active study without changing terrain heights, painted weights or rock controls. Its small, dated lighting reference was extracted from Greater Wasteland's saved scene, so alignment does not load thousands of production terrain tiles. This is a snapshot: deliberate later world-lighting changes need a new reference. It does not copy baked lightmaps or runtime post-processing/dust owners.

The initial October 10 focused checks passed 3/3 and established nine flat areas, exactly one editable generator each, common material/layer identity, the small patch at native texture scale, matching sun and serialized environment settings, missing-script safety and build exclusion. Evidence is in `Docs/Evidence/WorldCreator/NineAreaRockStudy/lighting-ground-2026-10-10.json` and its test XML. The earlier comparison and texture tool tests remain relevant regressions. The live Editor readback verifies the active study, nine terrains and the copied sun. Later in-memory dirty state after domain reload was preserved rather than saved over possible author edits.

No gameplay smoke test, foreground activation, production painting, generation/streaming activation, package change or Player-performance claim is part of this setup.
