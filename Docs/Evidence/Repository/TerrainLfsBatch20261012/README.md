# First terrain LFS migration batch

October 12, 2026. TST-01 is verified for local commit. The approved 18,210 payloads now use Git LFS pointers in the index at their original paths. Their materialized bytes, Unity metadata, production scene and package pins are unchanged. No archive migration, source deletion, history rewrite, LFS upload or push was performed.

The [verification record](verification.json) covers 3,267,022,905 payload bytes, about 3.043 GiB, in 18,210 distinct local LFS objects. Scope comes from the `LFS_` classes in the [sealed classification inventory](../TerrainStorageClassification20261012/terrain-files.tsv.gz): 9,216 binary TerrainData assets, 8,961 terrain textures, 28 large sealed JSON contracts and five active developer-map payloads. `.meta` files and local-only Blender/terrain sources are excluded.

## References and file integrity

Every approved working file first matched its original committed Git blob, then received a fresh SHA-256 baseline. Every staged pointer has exactly the expected SHA-256 and size. The working payloads and each cached LFS object passed the same SHA-256 checks. All 80,941 recorded terrain metadata and other protected files, including the production scene and package/build settings, retained their hashes.

The 318 attribute rules represent exactly the approved filename set. Seventy-one complete 16 × 16 tile grids use four bounded row/column patterns each; the remaining 34 paths are literal entries. The range patterns expand only to `r00` through `r15` and `c00` through `c15` beneath explicitly named owner/section directories. Expansion was checked against the complete approved set, and `git check-attr` checked the tracked and inventoried local-only namespaces for unexpected LFS selection. No broad extension rule was added. All selected rules use `filter=lfs diff=lfs merge=lfs -text` so sealed JSON and binary content preserve exact bytes.

The 57,011 local-only files in the classification retained their presence, sizes and modification times before and after the work. This is a stat-preservation check, not a fresh content hash of all source archives. No migration command wrote to those paths; LFS rules cannot select them. Other local Blender studies remain outside the approved LFS path set as well.

## Fresh local restoration and Unity proof

A fresh local clone under `D:/BooterBigArmValidation/TerrainLfs20261012` restored the candidate tree before the production commit. Because the source checkout is shallow and the candidate was not yet committed, the clone uses a local Git pack plus read-only object alternates for the candidate tree. Verified LFS objects were copied from the local cache, then materialized with `git lfs checkout`; no network fetch was used. All 18,210 restored payloads and 80,941 protected files passed SHA-256 comparison. This validation checkout is not an independent backup.

Unity `6000.4.0f1` ran in hidden background batchmode against that isolated checkout. Its cold import initially reported Shader Graph and Pixel Perfect compatibility errors; Unity's API updater updated the ignored package cache, and the final native validator completed successfully without changing pinned packages or tracked Unity content. The later focused-test log contains no script compiler errors.

The native validator passed all 9,216 terrains, 18,240 exact joins, 384 exterior edges, 576 sectors and 230,400 collider samples. It verified the protected inventory, native GUID/payload identity, full height readback, neighbor topology and saved-scene source lineage. The production scene SHA-256 remains `bc0b465de9a1b75b82a20d99c25fa4f10f00377a18f9564f9e8b0516a4c8f93e`; the final source manifest remains `9568e30edb234cb4737187c42bb53a335e6729d545a99a3134cab249cc0365ff`.

The three existing EditMode suites `BadwaterNorthRow4Tests`, `BadwaterNorthRow4HashTests` and `DeathValleyMapTests` passed 41 of 41 cases, with zero failures or skips. After import and tests, the isolated `Assets`, `Packages` and `ProjectSettings` trees still match the tested candidate. Git connectivity, exact staging ownership, documentation links and whitespace checks passed. No Player build, visual acceptance or gameplay smoke test was part of this Git-storage change.

## Local closeout and restoration requirements

The payload baseline is commit `abe1525bef951b41f36dac836800ca9e022374b8`, with validation tree `477426cbd6071cb4ef49b29f2af6a118e9973848`. Two concurrent Blender-source documentation/tool commits advanced the parent to `5986d62294514de86f99f1a61dad88e7a3f62976`. They changed none of `Assets`, `Packages`, `ProjectSettings` or the LFS selection; the existing staged batch and those commits were preserved. Unrelated dirty materials and Fused rock content stayed outside the migration commit.

Detailed baseline hashes remain in ignored `Logs/TerrainLfsBatch20261012/baseline.json.gz`; its digest is recorded in the verification JSON. Unity diagnostics and test XML are under the validation checkout's `Logs/lfs-*` paths. Preserve the main checkout's local LFS cache: the new payloads have not been uploaded to GitHub, so a remote clone cannot restore this unpublished batch yet. Future publication must pass the existing unpublished-history and local-only-source gates. Existing ordinary Git history has not been shrunk or pruned.

World identities, geographic keys, authored terrain constraints and persisted runtime deltas are unchanged because materialized assets and metadata are byte-identical. Terrain remains fixed and always loaded; terrain streaming and unload/reload behavior do not apply to this storage conversion. Later archive batches remain governed by the [migration plan](../../../Operations/Repository/TERRAIN_STORAGE_MIGRATION_PLAN_2026-10-12.md).
