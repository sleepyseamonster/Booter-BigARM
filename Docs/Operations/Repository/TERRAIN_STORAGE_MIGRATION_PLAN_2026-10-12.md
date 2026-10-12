# Terrain storage classification and migration plan

October 12, 2026. Owner: Gottspan; Unity dependency and import gates belong to Babineaux, Git/LFS conversion and publication gates to Gear Ball. TST-01 is complete and verified for local commit; see the [first LFS batch receipt](../../Evidence/Repository/TerrainLfsBatch20261012/README.md). Approved payloads retain their paths and bytes, while Git stores LFS pointers. Metadata and local-only sources remain intact. Later archive batches are still planned.

Keep Greater Wasteland’s native terrain and rendering assets at their existing Unity paths. Select their binary payloads for LFS, preserve text assets and identities in ordinary Git, and archive retained captures through an explicit restoration contract. No imported terrain file is cleared for immediate removal: the existing validators deliberately require historical source folders. The plan separates a safe change of Git storage from a later change of which evidence a normal checkout must carry.

## Exact inventory and scope

The [classification summary](../../Evidence/Repository/TerrainStorageClassification20261012/summary.json) and [compressed file inventory](../../Evidence/Repository/TerrainStorageClassification20261012/terrain-files.tsv.gz) identify every scoped file individually. The inventory contains paths, working and committed byte sizes, committed Git blob IDs, Unity GUIDs, proposed storage classes, migration gates, serialized text consumers, manifest consumer IDs, protection-receipt IDs and current validator source flags. It contains metadata only, not terrain or Blender payloads. Existing sealed manifests contain their SHA-256 expectations; these were not duplicated or presented as fresh payload hashes.

The snapshot is revision `e0eefd78dc20eef4a554b1e06bf7276ec4e6759f`, after completion of the fourth northern row. It covers active `Assets/_Project/Art/Terrain/`, the production and terrain-dependent reference scenes, `SourceData/Terrain/`, the Death Valley Blender study, retained terrain transfer inputs, and terrain documentation/evidence. Standalone historical projects, external validation copies, the C: rollback checkout and unrelated art are outside this bounded classification.

There are 219,136 file rows. Of these, 57,011 are local-only and untracked. Local-only scope totals 44.706 GiB; this is a different selection from the earlier repository-wide audit, which included Blender sources beyond the Death Valley study. The production scene references exactly the same 9,216 TerrainData assets declared by the accepted final manifest. Every scoped TerrainData file has a binary Unity header. There are no duplicate GUIDs in the scanned active metadata and no missing terrain paths declared by source manifests.

The [classifier](../../../Tools/Repository/classify_terrain_storage.py) reproduces the decision inventory without changing project assets. Run it from the checkout with:

```powershell
& .venv/Scripts/python.exe Tools/Repository/classify_terrain_storage.py --output Logs/TerrainStorageReaudit
```

Its output includes a machine-readable summary and compressed TSV. A plain searchable copy is written to ignored `Logs/terrain-storage-classification.tsv`. The summary lists the TSV columns and maps manifest IDs to exact paths. `storage_class` is the proposed destination, not a claim that LFS or an archive has already been configured. Git blob IDs establish committed content; local-only files need fresh SHA-256 inventories before any backup transfer.

## What stays in Git or LFS

| Files | Proposed storage | Reason |
| --- | --- | --- |
| 9,216 native `TerrainData/**/*.asset` payloads, 2.003 GiB | LFS, same paths | Production scene and colliders require these binary assets. |
| 8,961 terrain texture payloads, 0.194 GiB | LFS, same paths | Includes layer-referenced `Source/Colors` images and the retained geographic texture. The word `Source` does not make a texture archival. |
| `.terrainlayer`, `.mat`, all 35 terrain prefabs, production scene and retained `.meta` files | Ordinary Git | Reviewable serialization and Unity identities remain visible. Parent folder metadata stays when any required descendant stays. |
| 28 sealed imported JSON contracts of at least 2 MiB, 0.830 GiB | Exact-path LFS candidates | Large cumulative manifests are read by validators and contain predecessor hashes. Materialized text bytes must remain identical. This is an explicit file list, not a `*.json` rule. |
| Small source manifests, CSV provenance and receipts | Ordinary Git | Keep the authoring/validation contract and provenance discoverable. |
| Active developer map descriptors and coverage catalog | Ordinary Git | Current editor lookup depends on their unique paths and schemas. |
| Five active developer map payloads, 0.016 GiB | Exact-path LFS candidates | One float32 overview and four imagery files under `Docs/DeathValley/visualizer_data/blender_recovered/`; required for the existing map. |
| FixedTerrainDressingStudy and the two current performance reference scenes with their metadata | Ordinary Git | These are active reference work, not completed terrain-expansion candidates. |
| Canonical terrain notes and small evidence receipts | Ordinary Git | Preserve navigation, acceptance records and provenance. |

The binary and large-contract LFS selection totals about 3.043 GiB of logical working payloads, before identical-content deduplication and retained versions. It excludes the archive-candidate captures. Required payloads stay under Git version control through LFS pointers; `.meta` files never become LFS payloads. No file “must” use LFS because of an individual file-size limit here: this is the selected maintenance strategy for required binary/large immutable content.

Changing a sealed manifest’s Git representation is allowed only with unchanged materialized bytes. Its UTF-8 encoding, CRLF/LF representation and all content hashes stay intact. Do not pretty-print JSON, renormalize line endings, replace a manifest with an archive stub, or rewrite `predecessor_manifest_sha256` merely to make migration pass.

Generate explicit approved `.gitattributes` entries using `filter=lfs diff=lfs merge=lfs -text`, with repository-relative paths correctly escaped for attribute syntax. Do not match `.meta` files. The `-text` setting is essential for byte-exact materialization of sealed JSON as well as binaries. Review the resulting exact match set with `git check-attr` before staging any conversion.

For TST-01, bounded row/column patterns represent complete approved 16 × 16 filename grids. Their finite expansion was proven equal to the exact inventory, with literal entries for all other files. This preserves the approved path set while avoiding the substantial Git matching cost of 18,210 literal attribute lines.

The selected LFS set narrows the earlier general advice to keep JSON in ordinary Git: these 28 large immutable contracts are explicit exceptions. Smaller structured documents retain ordinary Git diffs. A later compact current-world catalog can remove cumulative manifest growth from the everyday workflow without overwriting sealed historical records.

## Archive candidates and their blockers

`ARCHIVE_GATED_SOURCE` contains 105,998 rows, including file metadata, totaling 11.072 GiB. These are source captures and rebuild/validation inputs with no detected serialized text consumer that would require the asset in a scene. That is an archive candidate classification, not engine proof of zero dependencies.

The current final validator directly requires 9,216 exact native-reference snapshots, plus the final section’s render and height files. These account for 9,728 capture payloads totaling about 2.339 GiB; their metadata adds a small amount. Other capture payloads account for about 8.725 GiB. Older validators, exporters and protected-folder receipts still depend on that latter group. The inventory marks direct current native/source usage separately from manifest and protected-folder usage.

Until TST-05 passes, a full current validation checkout still needs the whole protected source inventory restored, not just the direct 2.339 GiB subset. After that split, current verification still needs the selected current native seals provisioned from the local archive; a runtime-ready Git clone alone is not full source-validation proof. Archival storage can replace Git delivery of evidence, but cannot replace evidence needed to establish native geometry.

The import tree also contains 29 completed expansion candidate scenes with their `.meta` files. Their `ARCHIVE_GATED_SCENE` classification requires exact restoration of original scene paths and GUIDs because builders and validators name those paths. Active dressing and performance scenes are excluded from that archive set.

`ARCHIVE_GATED_EVIDENCE` identifies bulk terrain documentation evidence and retained study resources. It includes large JSON receipts and images that need an archive index and link resolver. These remain evidence, not disposable generated noise. Small records that explain what was accepted stay in Git. Blender resources must restore beside their corresponding local sources so relative paths still resolve.

All capture/source archives in this plan initially stay local. Copying a previously tracked raw capture into an archive does not authorize cloud publication. Blender files, numbered backups, GIS acquisitions, prepared grids and terrain archives remain governed by the [local-only policy](../LOCAL_ONLY_SOURCE_POLICY.md). The original `SourceData/Terrain/` and Blender paths stay intact during the archive pilot.

## Dependencies that prevent immediate removal

The decisive source consumers are:

- [BadwaterNorthRow4Source](../../../Assets/_Project/Scripts/Editor/TopDown3D/BadwaterNorthRow4Source.cs) reads current and predecessor manifests, validates owned relative paths, and checks full protected-owner inventories. Its `VerifyProtectedFiles` enumerates every file beneath each protected root and the shared snapshot owner, then verifies exact path sets and SHA-256 values.
- [BadwaterNorthRow4Validator](../../../Assets/_Project/Scripts/Editor/Validation/BadwaterNorthRow4Validator.cs) calls `ReadManifest(c, true)` and `VerifyProtectedFiles`. It compares all retained native heights to their referenced snapshots, current new heights to render grids, and new native seals to snapshots. Source removal breaks this gate even if Unity renders the saved terrain.
- Earlier `Badwater*Source` readers and validators retain their own source and predecessor contracts. North rows 2–4 share `NorthRow2Baseline2026-10-09` and reference earlier owners. The older owners cannot be treated as independent archives merely because a newer row exists.
- [export_north_row2_unity.py](../../../Tools/Art/Blender/death_valley/export_north_row2_unity.py) copies immutable snapshots to the shared imported owner and validates protected source files. Related row exporters and [native reference checks](../../../Tools/Art/Blender/death_valley/north_row2_native_references.py) require the original owner paths.
- [DeathValleyMapData](../../../Assets/_Project/Scripts/Editor/TopDown3D/DeathValleyMapData.cs) finds the unique live map descriptors and verifies overview/imagery hashes. Map data cannot silently become optional.

The summary records 171 code consumers and 29 terrain source manifests. Manifest consumer IDs explain direct file reads; protection IDs explain indirect whole-folder dependencies. Their distinction matters: a capture with no direct reader can still be required by a protection receipt.

The static audit scanned YAML references and conservatively retains every native TerrainData, layer, material, prefab and rendering texture. Unity binary external references were not parsed. A background `AssetDatabase.GetDependencies` request against the running D: editor timed out; a subsequent simple read also timed out. There was no scene open/save, focus change, import request or competing editor launch. The [dependency-query record](../../Evidence/Repository/TerrainStorageClassification20261012/unity-query-status.json) records this limitation. Full engine dependency closure remains a gate before any imported-file relocation.

## Migration batches

| Batch | Exact scope | Implementation and acceptance gate |
| --- | --- | --- |
| TST-01 | Classes beginning `LFS_` in the sealed inventory; corresponding `.meta` files stay ordinary Git | Convert only explicit tracked paths at the same locations. Verify every materialized payload and `.meta` hash before/after, keep all GUIDs, and run an isolated fresh-clone LFS restore and focused Unity import/terrain validation. No broad extension filter. |
| TST-02 | `ARCHIVE_GATED_SOURCE`, `ARCHIVE_GATED_SCENE`, `ARCHIVE_GATED_EVIDENCE` | Copy to a local versioned archive first; keep originals. Hash every payload, preserve original path/GUID/sample orientation, verify an independent copy and a restore into a fresh isolated folder. No pruning or untracking in this batch. |
| TST-03 | All proposed archive files plus their exact consumer/metadata boundaries | Capture `AssetDatabase.GetDependencies` for production, terrain prefabs, active references and loaded-by-path consumers. Compare closure to archive candidates. Any runtime/active scene dependency stays imported and version-controlled. Seal the exact remaining archive set. |
| TST-04 | Original archive paths and frozen validators | Add a local archive resolver that restores byte-exact payload/meta pairs at original paths in an isolated verification project. Reject missing, wrong-hash, out-of-root and partial restores. Preserve complete folder inventories so every frozen validator still passes unchanged. |
| TST-05 | New current-world verification catalog and tooling only | Introduce a separate compact catalog for the accepted 9,216 tiles and their native seals. Preserve all existing historical manifests/validators. Prove equivalent current geometry, GUID, source identity, edges, collision and hierarchy checks. Declare historical reconstruction checks as requiring archive hydration; never skip failed source checks. |
| TST-06 | Archive subset that passes TST-03 through TST-05 | Remove only verified archive payload/meta pairs from the normal import/tracking boundary while preserving local archive copies. Keep parent metadata for retained descendants. Record relocation/restoration receipts and provide a same-path rollback. Validate normal-clone production and archive-hydrated historical verification separately. |
| TST-07 | Exact approved Git candidate and publication lineage | Inspect unpublished history and excluded-source paths. LFS conversion/deleting the current tree does not shrink earlier ordinary Git objects. History rewrite, force push, cloud storage and remote publication remain separate user-authorized actions. |

TST-01 changes Git storage only and can precede the archive refactor. A planning request does not execute that conversion. TST-02 is preservation work; moving or deleting the originals is unnecessary to prove restoration. TST-04 allows historical checks to remain exact even after their payloads become optional in a normal checkout. TST-05 is the required new seam for making historical evidence optional during everyday current-world validation.

## Archive and rollback contract

Use a configurable local archive root outside the Unity checkout; a proposed layout is `D:/Arc & Dust/Archives/Terrain/<checkpoint>/`. A D: archive is organizational storage, not an independent drive-loss backup. Choose an independent backup device before calling this protected recovery storage. The existing C: rollback copy’s completeness and freshness are not assumed.

Every artifact record must contain its original repository-relative path, role, SHA-256, byte length, Git blob/revision if tracked, paired `.meta` path/hash/GUID, sample orientation and encoding where relevant, geographic key, source manifest version and consumer set. Use immutable content keys and a manifest mapping original paths to those keys. Never overwrite a distinct version with `latest`, deduplicate by filename, or use a matching payload hash as permission to substitute a different Unity GUID.

Restore into an isolated verification checkout before opening Unity. Preserve complete file bytes and all original metadata, validate the restored manifest, then run the selected frozen validators. The normal checkout must retain the production rendering chain; optional historical scenes/evidence hydrate only when requested. Where old canonical tools require original root paths, restore those paths rather than rewriting old provenance.

Before each migration batch, freeze its revision and owned path manifest, check for concurrent writers, and hash the exact input set. The classifier fails on changed scoped tracked files, moved HEAD, missing declared terrain paths or duplicate active GUIDs. Migration must additionally compare fresh content hashes, verify source/meta pairing and assess binary dependencies. Rollback restores the same path and GUID with the pre-migration bytes; it never regenerates metadata.

World origin, geographic identities, fixed native terrain constraints, player/companion placement, native heights, chunk/object identities and saved runtime deltas remain unchanged. Greater Wasteland stays always loaded. Terrain streaming and unload/reload behavior are outside this file-delivery task; no generator or save system is introduced.

## Completion criteria for the migration

A normal clone must open the saved production terrain with all required native/rendering assets restored; current-world verification must retain its accepted geometric and identity guarantees. A separate archive-hydrated verification checkout must pass the unchanged historical source checks. Local-only sources must remain present and unpublished, with verified independent recovery copies. File, metadata and reference inventories must reconcile before any untracking or removal. Hands-on gameplay smoke testing stays user-owned.

The first LFS conversion batch passed byte-preservation, local-clone restoration, native terrain validation and 41 focused tests. No push occurred. The next planned batch is the local archive copy/restore pilot; imported archive files remain in place until the engine dependency and restoration gates pass.
