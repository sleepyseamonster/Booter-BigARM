# Blender asset library and Greater Wasteland rock placement audit

Owner: Gottspan, using the Blender and Babineaux handoff contracts. Date: October 11, 2026. Status: audit and bounded implementation recommendation; production placement has not been performed.

**The seven completed rock families already have exported Unity meshes, materials and prefabs.** All seven existing deliveries passed fresh validation against the current import contract. The next useful slice is a small authored placement pass, plus reliable source-to-delivery tracking. A new procedural generation system or a replacement exporter is unnecessary.

## Scope and saved source

The author's open project is `SourceArt/Blender/Studies/RockSculpting/ArtistVolcanicRock01_GameReady.blend`, confirmed by the attachment title and the running Blender process. Treat it as the current library despite its first-rock filename. Keep it at its existing local path for the initial slice; a neutral library name can be a deliberate later Save As, preserving the current file and its relative dependencies.

Inspection used Blender **5.2.2 LTS** in a separate hidden background process with embedded script execution disabled. It describes **saved disk state**, not unsaved edits in the open artist session. The file changed between early probes as authoring continued; a six-family checkpoint was fingerprinted as SHA-256 `4f3d760addf70220e020bc2735d326c7c828ee466079882e5f121ac8756c041d`. The final inspected seven-family revision was stable during its probe and has SHA-256 `e73326e159f259fb00b308b0c0335867d82bd8492d95faf410b60c94b1e59635`. These are bounded saved revisions, not a live monitoring service. The probe did not save the source or change the live Blender workspace, selection, preferences or foreground application.

The final revision contains 46 objects: 42 meshes, three lights and one camera. Seven families contain named LOD0/LOD1/LOD2/collision meshes, with preserved processed and original sculptures. `Sculpt_Sphere_07` was a draft at the six-family checkpoint and became the delivered seventh family during the audit. This illustrates why explicit draft/ready tracking matters. `Ground_Reference`, originals, high-detail sculptures, lights and camera must stay outside delivery selection.

Units are metric with scale length 1. All seven LOD0 objects have unit scale and UVs. The 28 runtime LOD/collision meshes passed the existing read-only boundary/non-manifold/loose-vertex inspector. This is a structural check, not fresh proof of every UV seam, self-intersection or artistic choice.

There are 51 image datablocks, including 21 packed images and **28 external file images**, all of which currently resolve inside the repository; no missing file-image dependency was found. The `.blend` alone is therefore not the complete portable library. Retain its dependency folders or deliberately pack the required images before an offline transfer. No packing or source rewrite was performed. Normal maps read as Non-Color at 1024².

## Existing deliveries and the version mismatch

| Family | Saved Blender LOD0 / LOD1 / LOD2 triangles | Collision triangles | Delivered Unity height | Current package |
| --- | ---: | ---: | ---: | --- |
| ArtistVolcanicRock01 | 456 / 228 / 114 | 222 | 1.942 m | Validated |
| ArtistFlatVolcanicRock02 | 2046 / 1022 / 510 | 240 | 0.403 m | Validated |
| ArtistVolcanicRock03 | 1028 / 514 / 256 | 240 | 2.021 m | Validated |
| ArtistVolcanicRock04 | 6614 / 3306 / 2644 | 240 | 0.251 m | Validated |
| ArtistVolcanicRock05 | 1512 / 756 / 378 | 240 | 2.122 m | Validated |
| ArtistVolcanicRock06 | 1928 / 964 / 482 | 240 | 2.796 m | Validated |
| ArtistVolcanicRock07 | 606 / 302 / 150 | 196 | 1.423 m | Validated |

Each family has its FBX and maps under `Assets/_Project/Art/Environment/Rocks/<family>/`, a material under `Assets/_Project/Materials/TopDown3D/Rocks/`, and a prefab under `Assets/_Project/Prefabs/TopDown3D/Environment/`. Rocks 06 and 07 were delivered by concurrent separately owned work during this task; those commits and artifacts were preserved.

**Rock 01's delivered Unity LOD1 has 236 triangles, while the current library object has 228.** The committed delivery receipt also records 236. Fresh Unity validation confirmed the delivered package, so this is source/delivery drift, not evidence that the package is unusable. Reconcile the authoritative source revision before adding automatic re-export; do not overwrite the validated mesh blindly from the library. Current placement can use the existing Unity prefab without rebaking.

Rock 04's thin slab and fractured edge use more geometry than the other families. Retain the accepted shape for the bounded placement pass; avoid forcing all models through one triangle target or burying a 25 cm slab like a tall outcrop. LOD/density tuning follows the actual elevated camera view and measured scene cost.

## Reuse the existing bridge

[The sculpted basalt handoff](SCULPTED_BASALT_01.md) documents the existing pipeline. `Tools/Art/Blender/prepare_sculpted_basalt.py` already prepares named sculpture deliveries with measured reduction, UV bakes, three LODs, collision and explicit mesh-only FBX export. It also saves separate game-ready sources, so it is an authoring/export command rather than a harmless library inspector. Its current assumptions suit static rocks; future rigs, animation and other asset types need their own export profiles.

`SculptedBasaltAssetPipeline.BuildAsset(name, triangles, dimensions)` already supports additional named families. It builds URP materials, converts the imported node transforms into native meshes, creates an LODGroup and convex collision, and validates the result. It refuses to overwrite an existing delivery. A future update command must stage a new candidate, preserve existing `.meta` GUIDs and references, and deliberately update compatible native assets; replacing a FBX alone does not rebuild every copied native mesh.

Keep the `.blend` and backups outside Unity's import boundary and local-only under [the source policy](../Operations/LOCAL_ONLY_SOURCE_POLICY.md). Export only explicit game members as FBX plus texture maps. Unity's manual describes FBX as its primary model exchange path and recommends direct FBX export for most workflows; native `.blend` importing would invoke the installed DCC exporter and conflict with this repository's local-source boundary. [Unity 6.4 model import](https://docs.unity3d.com/6000.4/Documentation/Manual/ImportingModelFiles.html).

The new [read-only library inspector](../../Tools/Art/Blender/inspect_asset_library.py) reports objects, collections, transforms, evaluated geometry, UV/material assignments, image dependencies, asset marks and a source fingerprint. It writes only the requested report and detects changes to the disk file during inspection. It has been executed against this actual library. It does not export, mutate a scene, save the artist file or read another Blender process's unsaved memory.

## A library that can grow

Begin with a small versioned manifest rather than a large custom synchronization service. For each accepted asset, record:

- A stable asset ID and asset type, source file/collection, source revision/hash, and draft/ready/retired status.
- Explicit render, LOD and collision members; metre scale, ground/contact pivot and axis convention.
- Texture channels, color spaces and dependencies; export path and settings profile.
- Delivered mesh/material/prefab GUIDs, revision, validation receipt and author acceptance.

Use stable IDs as the binding; names and the selected object are presentation, not export authority. A read-only “what changed?” comparison should explain drift before an export can replace a delivery. Export one ready asset at a time, validate it in a staging copy, then integrate its approved runtime package. Draft objects and hidden originals never qualify by proximity or selection.

Most objects currently sit in the root Scene Collection, with one additional generic collection, and no object is marked as an asset. Group each family into explicit Work/Source and Runtime/Export areas, with Preview/References separate. Keep high-detail originals out of an export bundle. Blender's Asset Browser can optionally provide named previews and catalogs as the library grows; it supports reusable marked datablocks and collection assets. It does not replace the Unity delivery manifest or validation. [Blender 5.2 asset-library introduction](https://docs.blender.org/manual/en/5.2/files/asset_libraries/introduction.html).

Retain the master library now. Split very large future assets into separately editable local files only when file size or editing cost warrants it, with the master retaining organized links/references. Neither a rename nor a wholesale library migration is required to place these rocks.

## Minimal Greater Wasteland placement slice

“Random-looking, with no procedural generation” is interpreted as **varied authored placements**. Current Ironstone also uses twelve authored positions in a runtime-installed area prefab; it does not randomly scatter new nodes each session. Follow that simple arrangement pattern, rather than activating the older generated-world or formation workbenches.

1. Reuse the seven validated prefabs. Start with roughly 7–14 instances in the starting/near-Ironstone area, mixing boulders and thin slabs with deliberate spacing, yaw and modest uniform scale. Keep the artist's textures and silhouettes. These are decorative rocks, not automatically mineable Ironstone.
2. Sample the existing native terrain with `FixedTerrainSurfaceIndex`, checking the whole footprint for support, slope and floating edges. Use conservative per-shape burial, particularly for the two slabs. Do not regenerate, sculpt, replace or stream terrain.
3. Protect Booter/Legger starts, the starting camp footprint and physical return route, the resource cache and unloading space, and reachable approaches/visibility for every Ironstone deposit. Recheck both carriers' collision envelopes and existing saved actor/camp/device positions before adding solid obstacles.
4. Author one versioned rock-arrangement prefab with fixed poses and stable placement IDs. Review it in an isolated copy first. Runtime installation may instantiate that already authored arrangement once, using the existing idempotent overlay pattern; it must not choose new random locations on launch or rebuild an enormous scene during normal gameplay.
5. Gate integration on asset references/GUIDs, grounding, clearance, exactly one arrangement, unchanged native terrain, absence of generation/save duplicates and current compilation. Elevated-camera readability and hands-on traversal remain author-owned acceptance; do not run gameplay smoke tests automatically.

The current inventory checkpoint validates native support for restored positions, but does not itself reject all newly added decorative obstacles. The placement gate must therefore protect saved poses or add an explicit compatible obstruction-validation slice before obstacles can overlap retained positions. Never silently move either character to repair a placement.

## World and persistence contract

Deterministic identity remains the fixed authored world `greater-wasteland.authored.v1`; an arrangement version and stable member IDs identify these static objects. Chunk/terrain streaming and generated-object identity are not applicable because the terrain and first arrangement stay fixed and always loaded. If a view is reconstructed, it receives the same prefab and recorded pose.

Authored constraints are native support, clearance, existing landmarks/routes, resource access and preserved actor positions. Static decorative rocks have no removal, movement, harvest or destruction deltas to persist in this slice. Future interactive rocks require an explicit state-owner/schema extension tied to their stable IDs; do not attach them to Ironstone depletion or the retired generated-world save service by assumption.

Deferred work includes world-wide scatter, procedural formations, fusion, biomes, terrain streaming, dynamic rock physics, new harvesting rules and an automatic live-edit synchronization daemon. Useful later conveniences are a small Unity prefab palette, per-asset preview/validation buttons, and a manifest-driven staged export/update command.

## Evidence and closeout

Saved-library and mesh reports are retained under ignored `Logs/BlenderLibraryAudit20261011-01/`. A concise [audit receipt](../Evidence/Art/AssetLibrary/audit-2026-10-11.json) records the revision and per-family findings. Existing front/top/underside previews remain in each family's `Docs/Evidence/Art/` folder; Rock 04 and Rock 06 previews were inspected during this audit. Those images are retained asset evidence, not new Greater Wasteland renders.

Fresh Unity **6000.4.0f1** validation passed **7/7 existing deliveries** in the owned isolated candidate `D:/BooterBigArmValidation/Inventory20261011-01`, with the new/current rock packages copied there. `SculptedBasaltAssetPipeline.ValidateAsset` checked LOD counts, native metre bounds and ground pivots, UVs/normals/tangents, materials, texture import flags and convex collision. Logs are `Logs/rock-library-validation-final.log` and `Logs/rock-library-validation.json` there. No scene was opened or changed by that command, and no Play Mode or gameplay smoke test was run.

The audit added only the inspector, documentation and receipt. Existing Blender sources, Unity deliveries, production scene, materials, packages and unrelated fusion/terrain/performance work were preserved. No push is authorized.
