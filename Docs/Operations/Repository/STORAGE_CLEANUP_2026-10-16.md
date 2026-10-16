# Local Unity and publication storage cleanup — October 16, 2026

Owner: Gottspan, with Gear Ball for Git storage. Authority: the author's October 16 request to audit C: and D:, plan safe removal of unused Unity/publication temporary files, and execute that plan.

## Audit and preservation rules

Production remains `D:\Arc & Dust\Project`, `main` tracking `origin/main`. Unity is open on production and other authoring work is active. The initial inspection found 251 unpublished commits, unrelated dirty/untracked files, an empty staging index, and passing Git connectivity. These are dated observations; concurrent work may advance them.

The metadata inventory covers the known project, legacy C: checkout, validation projects, transfers, downloads, Unity installations/caches, user temporary directory, private assets and independent recovery locations on both drives. It does not classify unrelated Windows, games, or other applications' files as disposable. Reparse points are skipped; no cloud placeholder is hydrated. Detailed inventories and execution helpers live locally under ignored `Logs/StorageCleanup20261016/`.

Preserve all production Assets and `.meta` files, source art/data, Blender autosaves, package/version pins, project settings, saved worlds, player builds, test evidence, Git index/history/refs/reflogs, LFS object stores, the published-terrain restore clone and independent recovery store. Preserve production `Library`, `Temp`, `Logs` and `UserSettings`, plus shared global Unity caches, while the Editor is open. An ignored path is not automatically disposable: production Logs contains unique Blender work and a pre-publication Git recovery bundle.

The independent source/Git recovery store described in [the publication closeout](../../Evidence/Repository/PublicationCloseout20261012/README.md) remains protected. The legacy C: checkout retains its sources, Git/LFS state and rollback role; only proven generated caches qualify for cleanup.

## Execution plan

1. Seal a local metadata inventory, initial Git status, free-space baseline and Git connectivity result before removing anything.
2. Enumerate exact `Library`, `Temp` and `obj` cache directories only in recognized, inactive validation projects and the old C: checkout. Require every contained file to have been idle for at least 24 hours. Defer any candidate with recent files, reparse points, or scene/Blender recovery data in Temp. Preserve validation source snapshots and external logs/results.
3. Before each deletion, validate the absolute path against the explicit plan and its allowed parent, reject reparse ancestors, recheck current Unity/process ownership and file freshness, and confirm that the candidate contains no tracked files. Remove only the sealed cache target, with native PowerShell literal paths. Stop that target on an ownership, path, lock, or access conflict.
4. Remove only inventoried abandoned `tmp_pack_*` and `tmp_obj_*` scratch files in the production and legacy C: Git object directories after confirming Git connectivity, at least one hour of inactivity, unchanged size/time, no current Git writer and exclusive accessibility. The initial production pass uses the stricter 24-hour threshold; the supplemental pass includes old failed writes from the previous evening. Preserve valid object files, pack/index pairs, refs, index and lock files. Do not run broad Git garbage collection or reflog expiry.
5. Losslessly gzip the oversized, inactive `forward-review-hydration-editor.log` in `C:\BooterBigArmValidationRecovery\2026-10-16\Logs`. Verify decoded byte count and SHA-256 against the original and its crash-directory duplicate before removing either original. Leave restoration pointers at the original locations. If the two sources differ, retain both until independently archived and verified.
6. Recheck Git connectivity, cache-target absence, protected paths, remaining deferred targets, index state and actual drive free space. Retain a compact tracked receipt and commit only this task's documentation after checking for concurrent Git writers.

Removal of regenerable caches is permanent; Unity recreates them on the next import, which will take time. Unique diagnostic data is retained losslessly. Cache sizes are logical bytes and may differ from recovered disk allocation because of compression, hardlinks and filesystem overhead; report actual volume free-space measurements separately.

## LFS analysis and external boundary

Production has approximately 19.66 GiB of local LFS objects. The local dry run inspected 18,924 objects and retained 18,212; it is not a proof that all other objects are disposable. Historical local-only Blender/source payloads and recovery requirements make automatic pruning inappropriate. All LFS object stores are retained. Production LFS temporary/incomplete transfer directories were empty at audit time.

Local LFS deletion does not reduce GitHub's remote storage quota. [GitHub's removal documentation](https://docs.github.com/en/repositories/working-with-files/managing-large-files/removing-files-from-git-large-file-storage) says removed LFS payloads continue to count, and directs repository owners to Support when a purge is needed without deleting the repository. Remote purge, repository deletion/recreation, history rewriting and publication are outside this local-drive cleanup. No such operation is performed.

## Gameplay and validation scope

This is local storage maintenance. Deterministic world identity, always-loaded authored terrain, streaming/unload/reload behavior, stable object identity, authored constraints and persisted runtime deltas are unchanged because no gameplay/source/save content is edited. Unity is neither activated nor restarted. No gameplay smoke tests are needed or authorized for this task.

## Execution result

Completed. The [verification receipt](../../Evidence/Repository/StorageCleanup20261016/verification.json) records every removed cache target and Git scratch file, the archive identity, protected paths, deferred targets, checks and volume measurements. The exact local plan and detailed execution records remain under `Logs/StorageCleanup20261016/`.

- Removed 43 idle cache directories containing 2,078,611 generated files and 158.681 GiB of logical data. Validation source snapshots, package manifests and external test results remain.
- Removed all 19 inventoried abandoned Git scratch files across production and the old C: checkout. Both now report zero Git garbage; no valid object, pack/index pair, index, ref, reflog or LFS object was removed.
- Retained the oversized diagnostic log as a 26,990,308-byte gzip archive at `C:\BooterBigArmValidationRecovery\2026-10-16\Logs\forward-review-hydration-editor.log.gz`. Full decoding reproduced all 4,235,335,901 original bytes with SHA-256 `290b31536b6f99e21e3bd11f0c15fae87767435da42ae9a42bac21b02137c541`. The identical original and crash-directory copy were removed after verification; `.archived.txt` restoration pointers remain at both locations.
- Deferred `AnimationPlayground20261013/Library` and `CameraOptions20261015-01/Library` because they had activity within 24 hours. Production Unity remained open under its original PID 9748 throughout cleanup.

| Volume | Free before cleanup | Free after cleanup | Net increase |
| --- | ---: | ---: | ---: |
| C: | 7.53 GiB | 20.40 GiB | 12.87 GiB |
| D: | 4.87 GiB | 161.88 GiB | 157.01 GiB |

The measured combined increase is approximately 169.88 GiB. These are volume measurements, including filesystem overhead and concurrent authoring/OS activity, rather than a claim that logical file sizes equal allocated storage.

Verification passed: production and legacy Git connectivity, current-HEAD LFS object integrity (`git lfs fsck --dry-run --objects HEAD`), absence of all removed cache targets, preservation of protected paths, empty staging index before the task documentation commit, and lossless log restoration. Concurrent author commits advanced `main` during the audit; unrelated changes were preserved. No Unity import, gameplay tests, package changes, remote writes or LFS pruning were performed.
