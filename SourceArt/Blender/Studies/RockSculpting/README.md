# Unity-scale rock sculpt starter

Created October 9, 2026 in Blender 5.2.2 LTS. Local source: `RockSculptStarter_2026-10-09.blend`. This file and its numbered backups remain ignored/local-only; do not stage or publish them. [Open-RockSculpt.ps1](Open-RockSculpt.ps1) opens the saved project behind other windows using Blender's supported `--no-window-focus` option.

The saved Sculpting workspace shows an 18 m by 12 m flat ground reference and three side-by-side cubes:

| Position | Object | Dimensions |
| --- | --- | --- |
| Left, blue | Unity unit reference | 1 m on every edge |
| Middle, tan | Booter-height reference | 2.48 m on every edge |
| Right, neutral stone | `Rock_Sculpt_01` | 2 m on every edge |

The player reference uses Greater Wasteland's saved Booter capsule height, 2.48 m at unit root scale. A read-only Unity query measured the visible body mesh at 1.77029 m times its 1.4 visual scale, approximately 2.478405 m. The reference is a cube matching that height, not a humanoid proportion model.

The editable cube has a bottom-centre pivot, unit transforms and 24,576 closed quad faces. It remains a standard cube in shape; no rock has been designed for the author. References and labels are protected from selection, with a separate collection for the sculpture and generous room to its sides. Preview camera/lights are hidden in the viewport and excluded from export ownership.

## Start sculpting

`Rock_Sculpt_01` is selected in Sculpt Mode with **Clay Strips** active. The toolbar, brush shelf and tool settings are available. All mirror axes are off for asymmetric stone. Pressure controls strength, with a fixed starting brush radius. The saved initial view shows all three cubes.

- **Shift** while brushing smooths; **Ctrl** inverts supported brushes.
- Try Clay Strips, Grab, Scrape/Fill and Flatten for broad rock masses and planes.
- **Numpad .** frames the selected sculpture for close work.
- The session helper enables **Alt + left drag** orbit for a pen/three-button-emulation workflow; global preferences are not saved.
- Voxel size starts at 0.04 m. Remesh deliberately after major stretching and save a version first; finer voxel sizes increase geometry. Dyntopo and Multires are not enabled together with this starter workflow.
- **Ctrl+S** saves the local source. Use Save As for distinct accepted rock versions.

The dense mesh is sculpting source geometry, not an optimized game mesh. Later Unity handoff should create an appropriate runtime mesh, UVs/bakes where needed, suitable normals/LOD/collision, and an export copy at the origin. Export only the sculpture: references, labels, plane, camera and lights stay out. Blender Z-up to Unity Y-up conversion and actual import scale/pivot must be verified on the exported sample before production use. No Unity export/import was performed here.

## Integration boundary

This is a manual DCC source, with no deterministic world-placement service. Rock geometry may later feed a versioned catalog; world identity and generated feature IDs belong to the placement owner. Terrain is fixed and always loaded, so this source owns no chunk streaming/unload/reload. The 1 m/player references and ground pivot provide authored scale constraints. Persisted runtime changes do not apply to an unexported static authoring cube; future interactive placement needs its own stable identity/delta contract.

The author owns sculpting/style acceptance. This setup does not resume paused landscape work or turn an experimental rock into canon. No production scene, package, Unity asset or source terrain was edited.

## Evidence and reproduction

[Creation and read-only reopen evidence](../../../../Docs/Evidence/Art/RockSculptStarter/README.md) records dimensions, topology, Sculpt Mode, brush and the interactive workspace readback. The background creator initially saved Layout as the active workspace; the first interactive preparation switched to Sculpting and saved that initial UI state once. The ordinary launcher does not auto-save artist edits or reset their brush/view.

Creator: `Tools/Art/Blender/create_rock_sculpt_starter.py`; first-session preparation: `rock_sculpt_session.py`; read-only verification/render: `verify_rock_sculpt_starter.py`. The creator refuses to overwrite an existing source. Do not rerun it against the working sculpture.

References: [Blender brush introduction](https://docs.blender.org/manual/en/5.2/sculpt_paint/brush/introduction.html), [Blender remeshing](https://docs.blender.org/manual/en/latest/sculpt_paint/sculpting/tool_settings/remesh.html). Tablet-driver pressure and hands-on sculpting feel remain author-owned checks.

## Saved artist rock delivery — October 10, 2026

The author's saved sculpt is preserved in the starter and the local `RockSculpt_ArtistSaved_2026-10-10.blend` copy. Open `RockSculpt_Basalt_GameReady_2026-10-10.blend` for the separate optimized, dark volcanic version. LOD0 has 982 triangles; the original high-detail sculpture is hidden and retained. References remain side by side. The delivery opens in Object Mode / Material Preview, since sculpting the reduced mesh would invalidate its bake. Continue sculpting in the original saved artist source.

[The handoff](../../../../Docs/Engineering/SCULPTED_BASALT_01.md) records the tested reduction error, texture maps, LODs, collision and Unity prefab. Blender sources/backups remain local; only optimized native exports, tooling and evidence enter Git.

## Growing artist asset library — October 11, 2026

The author uses `ArtistVolcanicRock01_GameReady.blend` as the current growing library. The final audited saved revision contains seven completed textured rock families with LOD/collision members and preserved sculpture versions; the seventh progressed from a draft during the audit. Keep the `.blend` and backups local-only. The [library and authored-placement audit](../../../../Docs/Engineering/BLENDER_ASSET_LIBRARY_AUDIT_2026-10-11.md) controls the proposed next slice: reuse existing Unity prefabs in a small authored Greater Wasteland arrangement, protect current gameplay/saved positions, and establish explicit asset/revision tracking before automatic re-export. This README update does not authorize exporting drafts or replacing accepted deliveries.
