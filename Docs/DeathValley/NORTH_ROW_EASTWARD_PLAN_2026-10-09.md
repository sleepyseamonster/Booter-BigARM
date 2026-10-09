# Complete the northern row eastward

## Scope and verified starting point

The user requested a plan to continue east from `north_ridge`, building and integrating each section before starting the next, ending directly north of the easternmost completed terrain. This plan covers five remaining sections, not another northern row or an extension beyond the eastern boundary.

The current saved Greater Wasteland scene contains 3,328 chunks: the existing 96 by 32 rectangle plus the westernmost 16 by 16 northern cap. Current scene SHA-256 is `177bd3c7f4445f3523e5c3f1c20e2401bfd56d2bbb97fd5565110dc702ee03a8`. The [northern-cap receipt](./NORTH_RIDGE_IMPLEMENTATION_2026-10-09.md), current coverage catalog and saved scene agree. The live Editor's external-change Reload dialog was pending at the previous closeout; saved-scene validation does not establish that the open Editor has reloaded it.

All bounds below are metres in EPSG:26911. Each section is 4,096 by 4,096 m, containing 16 by 16 chunks of 256 m. Every addition shares its south edge at northing 4014392 with existing terrain and its west edge with the immediately preceding northern section. The eastward order is mandatory.

## Ordered sections and acceptance counts

The proposed section IDs encode location and avoid collisions with the existing `north` section. Seal these IDs and paths in the first implementation manifest before authoring assets.

| Order | Proposed section ID | Bounds `[xmin,ymin,xmax,ymax]` | South anchor | Total chunks after integration | Exact joins | Collider samples |
| --- | --- | --- | --- | ---: | ---: | ---: |
| 1 | `north_row_e504016` | `[504016,4014392,508112,4018488]` | `northwest_far` | 3,584 | 7,024 | 89,600 |
| 2 | `north_row_e508112` | `[508112,4014392,512208,4018488]` | `northwest_next` | 3,840 | 7,536 | 96,000 |
| 3 | `north_row_e512208` | `[512208,4014392,516304,4018488]` | `northwest_outer` | 4,096 | 8,048 | 102,400 |
| 4 | `north_row_e516304` | `[516304,4014392,520400,4018488]` | `northwest` | 4,352 | 8,560 | 108,800 |
| 5 | `north_row_e520400` | `[520400,4014392,524496,4018488]` | `north` | 4,608 | 9,072 | 115,200 |

Order 1 uses `north_ridge` as its west anchor; later orders use the previous table row. Collider counts use the established 25 direct samples per TerrainCollider. Exterior null-neighbor slots remain 288 after every step. Each step adds 512 joins: 480 inside the section, 16 on its south border and 16 on its west border.

At step k, retain exactly `3328 + 256*(k-1)` chunks and add exactly 256. Occupied cells are the full 96 by 32 baseline plus northern rows 32 through 47 in columns 0 through `16*(k+1)-1`. Reject holes, overlap, shifted borders and premature filling of the remaining upper-east cells. The framing AABB remains `[499920,4006200,524496,4018488]`; it becomes fully occupied only after step 5. The final grid is 96 by 48 across 24.576 by 12.288 km, with eighteen sections. Stop at easting 524496 and northing 4018488.

## World and asset constraints

- Preserve fixed Unity origin `[522448,4008248]`, metre scale, geographic chunk keys and deterministic geographic identity. No random generation is introduced.
- Terrain remains fixed and always loaded. Generation, chunk streaming and unload/reload are outside this task; asset ownership must remain separable from that loading assumption.
- Preserve retained TerrainData, TerrainLayers, textures, GUIDs, heights and source bytes. New geographic keys and newly generated metadata establish stable authored asset identity; generated-object identity is not applicable because this work creates no procedural objects.
- Preserve authored player, controls, camera, UI, atmosphere, collision configuration, material assignment and scene hierarchy contracts. No gameplay redesign or new landmarks are implied.
- Persisted runtime deltas and procedural save integration are not applicable to this authored terrain addition; existing gameplay serialization remains unchanged.
- Keep the production scene GUID `4de5dd018ee194314a00fd369e2d3eeb`, sole enabled build-scene routing, package pins and project settings. Preserve separately owned dirty files.

## Repeat the complete pipeline for each section

1. **Resume and baseline.** Check `main` tracking `origin/main`, current status, available D: space, pinned Unity/Blender versions and process ownership. Resolve the pending Reload action within the background-only rule, then confirm the current scene is saved, clean and outside Play Mode. Capture a fresh full-grid baseline and protected-file receipt from the latest accepted scene in a fresh isolated project. Never reuse the original 3,328-chunk baseline for later steps.
2. **Acquire and audit measured sources.** Query official USGS coverage for the exact section, preserving catalog responses, complete 1 m bounded DEM windows, metadata, NAVD88 metre units, CRS, sample orientation and hashes. Likely catalog candidates progress across `x50y402`, `x51y402` and `x52y402`; these are lookup leads, not verified coverage. Prove finite 4,097-square native samples and strict 2 m decimation. Audit actual overlap and same-coordinate ownership boundaries before selecting explicit source ownership. Acquire aligned 2 m NAIP RGB imagery with provenance. Reuse retained source windows only after exact coverage, metadata and hash checks; otherwise acquire the missing windows. Failed requests remain diagnostics and do not become accepted inputs.
3. **Prepare both joins.** Pin the new south edge to retained north-edge native readback and the new west edge to the preceding northern section's east-edge readback. Verify their common southwest corner agrees before preparation. Preserve all retained samples and RGB anchors. Apply the established new-side height-adjustment budget and new-side color feathering, reconciling the two anchors at the corner. Verify every internal and external shared edge. Check the retained -100 to 1,700 m encoding range; never clamp measured elevations or change the world encoding silently.
4. **Author in Blender.** Build a junction pilot covering the south/west/corner joins, then the complete 256-mesh editable section. Use Blender 5.2.2 LTS, metre units, the fixed origin, 2 m section interiors, full 1 m borders, upward normals and packed geographic imagery. Save and reopen native sources; verify mesh/source correspondence, transforms, resources and exact borders. Build and reopen the current combined footprint with the established 4 m review interiors and full borders. Inspect pilot, section and combined overhead/oblique renders. Export only after these gates pass.
5. **Build the isolated Unity candidate.** Use guarded reopened Blender height/color exports to create native TerrainData, TerrainLayers, texture assets and one reusable section prefab. Keep the candidate outside enabled Build Settings. Extend the existing batch contracts narrowly for the ordered northern-row steps, preserving all previous contracts. Validate the exact occupied union, every retained full grid/GUID/hash, reciprocal neighbors, one connectivity owner, exact join count, direct collider samples and native precision budgets. Run focused EditMode checks, including negative checks for omitted cells, duplicate identities, wrong anchors and premature eastward fill. Do not run gameplay smoke tests.
6. **Integrate and read back.** Integrate the verified prefab in isolation, save, reopen and run production validation. Preserve every prior scene document exactly except the common terrain parent's one appended child; expect 1,282 new documents for the 256 terrains and one section group. Transfer only verified additions and the preserved scene after clean/Play/hash guards. Reuse the root Editor for background import and reload; a reload dialog requires user action or explicit foreground authorization. Validate saved production bytes and distinguish isolated proof from live Editor readback.
7. **Close the section before advancing.** Refresh atlas, geographic tile inventory and Unity developer map for the exact step. Inspect map previews; run the relevant refreshed-data checks, terrain doctor, metadata/reference audit and focused final Unity tests. Record sources, Blender proofs, hashes, scene preservation, readback, test results and any live-Editor handoff. Commit only this section's verified task-owned files on `main`, with no push. The next section starts from this new accepted baseline.

## D: placement and execution boundaries

Derive paths from `D:/Arc & Dust/Project`. For each section use a unique execution-dated batch owner, such as `NorthRowE504016YYYY-MM-DD`, in these established lanes:

| Lane | Repository-relative destination |
| --- | --- |
| Measured data, baseline and preparation | `SourceData/Terrain/DeathValley/<batch>/` |
| Editable pilot, section and combined Blender sources | `SourceArt/Blender/Studies/DeathValley/<batch>/` |
| Runtime assets, exports and reusable section prefab | `Assets/_Project/Art/Terrain/<batch>/` |
| Disabled isolated candidate scene | `Assets/_Project/Scenes/Reference/<candidate>.unity` |
| Accepted evidence and section receipt | `Docs/Evidence/DeathValley/<batch>/` and `Docs/DeathValley/` |

Use fresh isolated copies under `D:/BooterBigArmValidation/`, transfer receipts under `D:/Arc & Dust/Transfers/`, and subprocess temporary files under `D:/BooterBigArmTools/Temp`. Preserve source versions and metadata; do not clean the C: rollback checkout. Keep diagnostic outputs in ignored `Logs/` and avoid duplicate authoring sources in active Unity Assets.

This turn creates the plan only. Implementation proceeds one accepted section at a time under the user's requested scope. Pause an individual step on missing source coverage, incompatible anchors, precision failure or unsafe live scene state; retain completed sections and resolve the specific issue. Do not integrate later sections around a failed predecessor. Player traversal and FPS remain user-owned review; structural checks do not establish performance of the larger always-loaded world.
