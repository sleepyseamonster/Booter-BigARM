# Camp 1 authored rock placement

Owner: Gottspan. Date: October 12, 2026. Authority: the author's request to bring completed Blender rocks into a small area around Camp 1, with no procedural generation.

Seven decorative rocks are now in the saved Greater Wasteland scene, one from each completed family: ArtistVolcanicRock01, ArtistFlatVolcanicRock02, and ArtistVolcanicRock03–07. The linked arrangement is `Assets/_Project/Prefabs/TopDown3D/Environment/Camp1AuthoredRocks.prefab`, under the scene root `Camp 1 · Authored Rocks`.

## Placement and scope

Camp 1 resolves to the established `camp.starting` / Starting Camp definition at XZ `(-1842, -1498)`. Fixed authored offsets are `(-14,-10)`, `(8,-15)`, `(20,-4)`, `(-12,19)`, `(-23,-2)`, `(24,7)` and `(-6,-23)` metres from that anchor. All roots remain within 28 m; existing metre scale, materials, LODs and convex collision are retained. Each rock has a deliberate yaw, gentle terrain-normal alignment and a measured contact adjustment. The thin slabs use stricter grounding tolerance than the boulders.

Two initial positions were moved to preserve authored Ironstone access; the thin slab was moved to a flatter spot. No terrain was sculpted or replaced to fit the rocks. Existing Blender source/library geometry, textures and delivered prefabs were not rebuilt, renamed or exported.

Placement excludes the camp footprint, physical return corridor, Booter/Legger starts, resource-cache access/unloading space, Ironstone approaches, and characters/camps/canisters in the current checkpoint and its backup. Checkpoint files were read only. No character, camp, device or resource was moved to accommodate this arrangement.

## Fixed-world contract

`TopDown3DCampRockArrangement` stores world identity `greater-wasteland.authored.v1`, site `camp.starting`, layout version 1 and seven unique IDs `greater-wasteland:camp1:rock:v1:01` through `07`. It is descriptive metadata, with no placement/generation update loop. The scene contains one linked prefab instance with fixed poses; repeated loading produces the same arrangement.

Terrain and this decorative arrangement are fixed and always loaded. Chunk streaming and generated-object identity are not applicable. The authored IDs remain stable for future references. These rocks have no harvesting, destruction, movement or removal gameplay state, so no persisted runtime deltas or new save owner are introduced. They are not Ironstone deposits.

## Integration safety

`Camp1AuthoredRockAuthoring.BuildForLoadedScene()` measures and validates the seven explicit poses in a temporary authoring scene. It refuses to replace an existing arrangement. `InstallForLoadedScene()` serializes a miniature integration fragment and merges only its new prefab document and root reference into production, rather than resaving the enormous terrain/actor hierarchy. It refuses unsaved scene work, Play Mode, malformed fragments and concurrent source-file changes.

The final scene change is **58 added lines, zero removed lines**: one prefab instance and one root reference. Byte-level comparison against the captured predecessor confirmed **45,217 existing scene documents unchanged**. Existing scene and asset metadata/GUIDs remain intact. A full serialized-scene reopen independently resolved the new linked arrangement and its references.

## Verification

- **3/3 focused EditMode tests passed** in the owned isolated candidate `D:/BooterBigArmValidation/Inventory20261011-01`, under `Logs/camp1-rocks-final-tests.xml`. Tests cover exclusion rejection, scene-fragment preservation/rejection, seven distinct ready families, fixed IDs, bounded positions, linked prefabs, LOD/collision and decorative-only behavior.
- The loaded production validator passed after reopening the actual saved scene: exactly one seven-rock arrangement, native support, protected camp/route/resource/saved poses. It also ran the existing read-only waypoint/camp validator.
- Current Unity compilation succeeded. New source files have paired `.meta` files; existing rock asset references are reused.
- A static isolated capture using the actual native terrain tile and authored assets was inspected for arrangement/placement coverage. It uses review lighting and does not represent full production lighting or interactive gameplay. Retained local output: `Logs/camp1-rocks-review.png` in the isolated candidate.

No Play Mode, gameplay smoke test, performance replay, foreground activation, package change or push was performed. Hands-on traversal and visual acceptance remain with the author. Unrelated camp-material, fusion, terrain-expansion and population-study work remains separately owned and excluded from this commit.

The [placement receipt](Camp1Rocks/receipt-2026-10-12.json) records exact poses and scene integrity. The preceding [library audit](../../Engineering/BLENDER_ASSET_LIBRARY_AUDIT_2026-10-11.md) remains the source-to-delivery guidance, including Rock 01's LOD revision mismatch; this placement reuses its already validated Unity delivery.
