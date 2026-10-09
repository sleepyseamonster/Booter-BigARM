# Greater Wasteland terrain workspace

The authored production landscape is organized as the existing common terrain owner, 18 named geographic sections, 16 sectors per section, and 16 native Terrain tiles per sector. Sectors cover 1,024 × 1,024 metres; tiles remain 256 × 256 metres. `badwater_core` groups the original 256 tiles. The other geographic section names remain unchanged.

Use **Booter & BigARM → Greater Wasteland Terrain → Terrain Workspace**. The tool works only with Greater Wasteland active in Edit Mode:

- **Follow Booter — centre plus 8 neighbours** shows the player's current 1,024 m sector and the eight neighbouring cells, including diagonals. It checks the player Transform every 0.2 seconds and shifts the 3×3 grid when Booter crosses a sector boundary, including ordinary Edit Mode Move-tool drags. This is the default local-view mode.
- **Work around selection** uses the selected terrain tile or group's bounds centre, or an ordinary object's position.
- **Work around Scene view** uses the last Scene view pivot as the 3×3 grid's centre cell. **Follow Scene view** updates that centre as the pivot crosses sector boundaries. Player follow and Scene view follow are mutually exclusive.
- **Show all terrain** stops automatic control and clears terrain visibility, including hidden terrain ancestors.
- **Stop and restore previous visibility** returns managed sectors and tiles to the visibility state captured before local view.
- The same commands are available directly in the menu. Hierarchy eye icons support manual per-section, per-sector, and per-tile visibility while automatic control is off.

Local view remains active when its window closes. Its mode, focus and follow setting survive script reloads in the current Editor session. Visibility is restored before script reload, Play Mode, scene closure or Editor quit; local view resumes in Edit Mode. A new Editor session defaults to following Booter with the ghost overview enabled. Show All or Stop disables automatic control for that session until local view is re-enabled.

The 3×3 neighbourhood uses world X/Z sector coordinates, so it crosses the older geographic section parents correctly. At the landscape's edge, only neighbours that actually exist are shown; absent cells remain empty and are not generated. Detailed terrain never exceeds nine sectors (144 native tiles). When Booter is outside the footprint, the overview marker says so and any existing neighbouring cells remain visible. These are editor visibility cells, with no loading/unloading or world-identity changes.

October 9 player-follow change: the pinned Unity Roslyn compiler compiled both the current Editor and Editor.Tests source candidates successfully using the current Bee response-file references, with output redirected to ignored `Logs/ThreeByThree/`. The live connection subsequently recovered. Four focused grid/overview NUnit test methods were invoked directly through eval and passed without the TestRunner scene-management flow. Live readback checked all 288 sector visibility states against the exact neighbourhood rule, with player follow and the overview enabled. Six detailed sectors exist around the current edge-of-landscape player position. Booter's existing MeshRenderer was re-enabled at the user's request with Undo; no character was moved or scene saved. Visual acceptance remains user-owned, and the retained-terrain source drift described below still prevents a full canonical validation/commit gate.

## Lightweight whole-landscape context

**Show Lightweight Overview** is enabled by default in the Greater Wasteland Scene view. Hidden native terrain is represented by a translucent blue coarse surface: four corner vertices and two triangles per 256 m tile, at most 9,216 triangles for all 4,608 tiles. Visible tiles keep their native surface instead of a duplicate filled proxy. A cyan outline follows the occupied geographic footprint, including concave boundaries and missing-tile gaps; sparse 1,024 m guide lines communicate scale. **Frame Entire Landscape** frames these cached bounds without loading or revealing all detailed terrain.

The overview is an approximation between corner samples, suitable for navigation and placement context. It does not represent fine slopes, terrain holes, or certify walkable ground. It has no GameObject, collider, texture, lighting, shadow or Player rendering costs. Mesh/material data is transient and editor-only. Geometry is cached until hierarchy, heightmap or Undo changes invalidate it; visibility changes update only the hidden-tile triangle list. Detailed TerrainData remains resident as before. Rendering overhead has not been profiled.

The existing **Click Terrain To Place Booter** tool can place onto hidden native terrain while the overview is enabled. Its raycast still uses the actual TerrainCollider, and final ground placement still rejects holes or missing/disabled collision. The coarse mesh is never the collision authority. Booter has a cyan editor marker regardless of mesh-renderer state, with a dotted projection to the local sampled terrain height or an explicit **outside terrain footprint** label. The marker does not move Booter or change the renderer. The overview does not automatically snap ordinary Transform drags.

This feature changes neither terrain assets, geographic identities, authored constraints, runtime object identities, loading/streaming nor saved runtime deltas. It does not save the scene or change the user's pending character placement. These concerns remain outside this editor-only visualization.

This uses Unity's editor-only SceneVisibilityManager. Hidden terrain does not render in the Scene view, reducing its rendering workload. Game view and Player rendering, colliders, active states, TerrainData, source assets, neighbour connections and gameplay remain unchanged. It does **not** unload terrain data, reduce its resident memory, or remove terrain physics costs. No frame-time improvement has been measured; user performance/visual acceptance remains pending. Manual GameObject deactivation is not the recommended control because it would also deactivate collision and could become a saved gameplay change.

World identity remains the existing authored geographic tile identity. Sector parents are organizational objects, not new world chunks or generated identities. Procedural generation, streaming, unload/reload and persisted runtime deltas are not applicable to this editor-only visibility feature: no generation, loading service, runtime code or save format is introduced. The authored terrain footprint and constraints remain fixed.

## Placing Booter in the authored scene

Booter already exists in Greater Wasteland as the root **Booter Perspective 3D Controller**, with its mesh, capsule, Rigidbody and player components. Its identity and name remain stable for existing authoring tools and serialized references. The player root is at the top of the hierarchy and has a coloured label icon. No duplicate player or runtime spawn marker is added.

Use **Booter & BigARM → Player Placement → Select Booter** or select the first hierarchy object. Its player motor Inspector now has a **BOOTER — PLAYER CHARACTER** header and placement buttons:

- **Find Booter in Scene view** selects the player, enables the Move tool and frames it in an existing Scene view.
- Move it with the standard Transform/Move tool, then **Snap Booter to ground** to align the capsule's bottom 0.12 m above the terrain collider. Capsule centre and scale are accounted for.
- **Place at Scene view centre** uses the current view pivot's horizontal position and the terrain collision height.
- **Click terrain to place Booter** enters a one-click tool. Click a visible terrain surface to position the character; Escape cancels, and Alt remains available for view navigation. Placing stops the tool. Holes, disabled colliders and locations outside the authored terrain are rejected without moving the player.

Placement is Edit Mode only, uses Undo, and marks the scene dirty. **Save the scene** to retain the starting position. The player motor initializes physics in Awake without resetting the authored Transform. Legger retains his separate world position; placing Booter never teleports the companion. Save/runtime delta formats, tile IDs and generation/streaming remain unchanged. Hidden terrain stays collidable, so ground snapping works in local terrain view. With a fixed local view active, terrain visibility follows the newly placed player; the optional Scene view follow mode is retained.

The playable-scene validator checks grounding at each character's current authored X/Z coordinates instead of assuming the original demonstration spawn. Scene construction still uses that original spawn when explicitly building the initial playable setup. Moving characters onto steep or inaccessible terrain remains an authoring choice; these tools do not certify gameplay traversal.

Player placement verification on October 9: two focused EditMode tests passed in isolated preview scenes (collision height, scaled/offset capsule clearance, out-of-bounds/disabled colliders and terrain holes). A live Edit Mode placement followed by Undo returned Booter to the exact original position, without changing the player motor payload or Legger's position. The saved scene changes from this placement task are limited to Booter's icon and scene-root order. Reopening confirms one player at hierarchy index zero, unchanged component references, a clean scene and Booter selected. No Play Mode or gameplay smoke test was run. The previously recorded retained-terrain source drift continues to block the full canonical validation/commit gate; these terrain files remain untouched by placement tooling.

`GreaterWastelandTerrainWorkspace.OrganizeCurrentScene()` intentionally changes hierarchy and supports Undo; it does not save automatically. It refuses terrain prefab instances instead of silently unpacking them. Existing terrain objects, names, asset GUIDs and world transforms are retained. Repeated organization reuses the groups. Expansion builders remain source handoff tools; future authorized additions can be grouped with this command after integration without modifying their source prefabs.

## Reconciliation and saved baseline — October 9, 2026

The later whole-worktree reconciliation supersedes the earlier blocked publication gate below. At the author's direction, the three manual sculpt edits were restored to their exact sealed TerrainData payloads; GUIDs and metadata were preserved. The saved scene keeps all 18 sections, 288 sectors and 4,608 native tiles, with Booter's mesh enabled and the player first in the hierarchy.

Current-machine checks passed: all nine authoring tests, full native terrain readback, 9,072 exact shared borders, 115,200 collider samples, the saved playable-scene validator and production/legacy-boundary validator. All 5,040 scene objects have their scripts; the 4,665 referenced GUIDs resolve through project assets or installed packages. The [fresh baseline](../Evidence/Badwater/GREATER_WASTELAND_BASELINE_2026-10-09.json) records exact scene, source-manifest, TerrainData and metadata hashes.

Earlier paragraphs describing unresolved terrain drift remain historical evidence. The restoration resolves that drift without accepting alternative hashes or weakening terrain gates. No generation, runtime loading, physics behavior, stable identity or save format is introduced. Background offscreen captures do not establish complete visual acceptance; no Play Mode, gameplay smoke test, new Player build or performance profiling was run.

## October 9, 2026 verification

- 4,608 terrain tiles grouped into 288 sectors under 18 sections.
- Initial radius-based implementation around Booter: 9 sectors visible, 279 hidden (144 visible tiles, 4,464 hidden). The later player-follow change replaces radius selection with an exact 3×3 neighbourhood; edge cells can have fewer existing neighbours.
- Before/after live fingerprints match for all terrain component IDs, names, world positions/rotations/scales, active/enabled states, TerrainData paths and serialized Terrain/TerrainCollider payloads.
- Scene serialization audit retains every original component block, changing only Transform parent/child links and adding 289 GameObjects with their Transforms (18 sections include the added core grouping).
- Focused workspace tests cover exactly nine cells including diagonals, boundary-crossing recentering at negative coordinates, organization idempotency, cross-sector neighbour links, terrain/collider preservation and named section ownership.

Current-machine compilation and all four focused tests passed. Live checks verified Show All, restoration of an existing individually hidden tile, local views at two distant positions, all 288 sector memberships, unchanged collision/active states and no scene dirtiness from visibility control. These are structural checks; gameplay smoke testing and visual/performance acceptance remain user-owned.

The canonical `BadwaterPlayableSceneBuilder.ValidateFromCli` was run and stopped at retained-source hash validation: `WestNorthNorthwest2026-10-07/TerrainData/north/r10_c01.asset` differs from its sealed receipt. Three TerrainData files were saved by Unity with editor terrain height edits during this task: that file, `north/r11_c02.asset` in the same batch, and `WestPair2026-10-08/TerrainData/west_outer/r01_c10.asset`. These are outside this hierarchy/visibility change and were preserved. Direct readback confirms the first differs in height from its archived normalized source. The grouping code never writes heightmaps or TerrainData; tests create their own temporary TerrainData. Source-baseline reconciliation and a passing full canonical validator remain unresolved, so the task was not committed under the repository's required validation gate. Local diagnostic JSON and before/after scene/fingerprint snapshots are under ignored `Logs/terrain-*` paths.
