# Publication and storage readiness reassessment

October 12, 2026. Owner: Gottspan and Gear Ball. This audit establishes the next steps toward a clean worktree and safe publication. It does not treat unfinished authoring, local-only sources or a clean Git status as interchangeable forms of storage.

The terrain LFS migration is sound. Remote `main` remains at `2c522e4ed50d692aa6ce6e581cf56e3ee4406d8a`, and the configured account has repository write access. The LFS dry run selects exactly the approved 18,210 objects, about 3.043 GiB, with no excluded source paths. The ordinary Git transfer estimate is 1.770 GiB. The first priority is current source backup coverage, followed by closing the remaining dirty asset decisions and confirming LFS billing availability. A push should follow those gates without changing or discarding active authoring work.

## Audited Git and publication state

The [machine-readable evidence](../../Evidence/Repository/PUBLICATION_READINESS_AUDIT_2026-10-12.json) distinguishes the initial snapshot from closeout. The initial revision `94b3bdaffd46ea5d99bbe9b80a53cd88665c8933` was 157 commits ahead of remote `main`. During the audit, the input owner committed its completed work as `ead8c891233ab26dc501dd753434d971cb0d53e2`, increasing the range to 158 commits. The audit's own later commit adds another commit. These are unpublished ancestors of a future push, not just the LFS migration commit.

| Check | Result | Practical meaning |
| --- | --- | --- |
| Live remote `main` versus cached `origin/main` | Exact match | No remote divergence was detected; fresh readback is still needed immediately before publication. |
| Authenticated repository permissions | Write and admin access, login `sleepyseamonster` | Authentication is available. No repository setting was changed. |
| Unpublished excluded-path history | No Blender source/backups or `SourceData/Terrain/` touches | The examined range passes the named local-only path gate. Refresh it for every later candidate. |
| Newly reachable ordinary Git blobs | 76,366 blobs, 5.814 GiB uncompressed at the initial snapshot | This measures historical objects still needed by the remote, not the current checkout or compressed network payload. |
| Newly reachable ordinary blobs over 50 or 100 MiB | None | No individual-file warning or hard-limit problem was found in that range. |
| LFS dry run | 18,210 objects, all approved | No uploads occurred. The retained pointer-text receipt did not add an excluded payload to this dry run. |
| Required LFS object availability | All present, expected sizes | Full SHA-256 proof remains in the [migration receipt](../../Evidence/Repository/TerrainLfsBatch20261012/README.md). |
| Git connectivity | Passed | No missing reachable Git object was found. |
| Ordinary thin-pack estimate | 1,900,619,050 bytes, 1.770 GiB | Below 2 GiB for the measured snapshot, with limited headroom. No pack was stored or transferred. |

GitHub limits a single push to 2 GiB. The local estimate excludes the known remote-main ancestry and uses normal thin-pack/delta options; actual negotiation and later commits can alter the result. Refresh the estimate for the final candidate. If it approaches the limit, publish validated ancestor SHAs in ordered non-force fast-forward batches. There is no demonstrated need to rewrite history, delete the repository or force-push. [GitHub push-limit guidance](https://docs.github.com/en/get-started/using-git/troubleshooting-the-2-gb-push-limit).

LFS conversion replaced current Git blobs with pointers but left earlier ordinary Git history intact. The 5.814 GiB historical-object figure therefore remains compatible with a correctly migrated current tree. Future archive migration is a separate dependency-preserving project; it is not necessary merely to make the measured pack fit this push limit.

## LFS account capacity remains unverified

The local LFS cache holds about 19.656 GiB. That includes retained historical source objects as well as the new terrain objects; it is not GitHub's billable storage total. Repository access succeeded, but account plan information was unavailable and the documented user billing-usage endpoint returned HTTP 404. That response does not establish zero usage, a quota problem or an available budget.

Before uploading, inspect the signed-in account's LFS storage, monthly download usage and applicable budget. Free/Pro accounts include 10 GiB each of storage and download allowance, while Team/Enterprise Cloud include 250 GiB each. Existing repositories and retained uploaded versions share the account allowance, and a zero budget can block usage beyond it. New versions consume full-file storage; clone and CI downloads consume bandwidth. [GitHub LFS billing](https://docs.github.com/en/billing/concepts/product-billing/git-lfs).

The API needs the relevant billing access and cannot supply every detailed dashboard value. The audit did not change permissions, payment methods, plans or budgets. Confirm capacity before publication rather than purchasing a plan based on local cache size. [GitHub billing API](https://docs.github.com/en/rest/billing/usage), [billing report limitations](https://docs.github.com/en/billing/reference/billing-reports).

Deleting local LFS cache files or removing files from the latest tree does not clear retained remote billing. GitHub directs users to Support for removed objects that need purging without deleting the repository. Preserve the cache and excluded sources; remote retention review is a separate action. [GitHub LFS removal guidance](https://docs.github.com/en/repositories/working-with-files/managing-large-files/removing-files-from-git-large-file-storage).

## Current source backup coverage is the storage priority

The current logical local-only source selection totals approximately 46.848 GiB, including terrain sources, Blender sources/backups and retained transfer Blender files. The narrower comparison of `SourceData/Terrain/` and Blender sources beneath `SourceArt/Blender/Studies/` against the retained C: checkout found:

| Current source paths versus C: rollback copy | Files | Current logical bytes |
| --- | ---: | ---: |
| Same relative path and size | 649 | 3.100 GiB |
| Missing current relative path | 52,846 | 43.125 GiB |
| Different size at the same path | 0 | 0 GiB |

No hashes were checked in this comparison, so even the matching-size group is only candidate coverage. It proves that the C: rollback copy cannot be described as a complete current source backup. Other copies may exist, but none was established as a full current independent recovery set by this audit.

Free space was approximately 44.4 GiB on C: and 11.1 GiB on D:. Copying the missing 43.125 GiB to C: would leave less than the project's 3 GiB reserve, before fresh LFS/Git recovery data. D: cannot fit that source copy, and another D: folder would not protect against D: drive loss. Choose adequate independent local capacity before transferring a full recovery set. A paid cloud archive is still outside the [local-only policy](../LOCAL_ONLY_SOURCE_POLICY.md) until the author explicitly changes that boundary.

The C: rollback root is under OneDrive. Its sync state is unverified; do not use that path as the default destination for new local-only backups. Use a deliberately selected non-synced local destination. Preserve the existing rollback copy and all originals during a hash-verified copy/restore pilot.

The recent LFS validation clone is useful proof but is not an independent backup: it shares the D: device and uses read-only object alternates for the tested pre-commit tree. Other validation projects also require owner/receipt review before retirement. The audit removed no caches, validation projects, Git objects or source files. Git reports 232.14 MiB of existing temporary-object garbage; that small amount does not solve the backup-capacity gap and remains preserved.

## Remaining dirty work and ownership

The input/runtime/test changes were owned by **Audit Leger inventory controls**. That owner completed its matched capture and committed the exact batch as `ead8c8912`; the structured evidence records 173 passing tests with no failures or skips. This audit did not take over or restage that work. See the [input evidence](../../Evidence/Performance/INPUT_INTERACTION_PROFILE_2026-10-12.md).

The four remaining dirty materials are `PackCanvas.mat`, `PackFrame.mat`, `PackWebbing.mat` and `Greybox_BigARM.mat`. Their differences include `_Color` values as well as whitespace. The three camp materials synchronize an old white `_Color` toward the existing `_BaseColor`; BigARM has a smaller numeric change. They may reflect URP editor synchronization, but this audit does not declare them appearance-neutral or discard them. Resolve intent, effective shader property usage and required import/reference proof in a separate material batch. Preserve the material GUIDs.

The untracked Fused group contains a folder `.meta`, mesh `.asset` with metadata, prefab with metadata, and fusion JSON with metadata: seven files. It was produced by `SculptedRockFusionTool` as persistent content, not a disposable cache. The report records 5,346 triangles, zero nonmanifold/degenerate/loose elements and one component. No reference to its three asset GUIDs was found in the scanned saved YAML scenes/prefabs/materials/assets outside terrain. Binary and unsaved scene references remain unverified.

Determine whether that fused result is accepted reusable/runtime content or a retained local authoring experiment. If accepted, verify mesh/prefab/material/collision references, GUID uniqueness and Unity import, then commit all seven files together. If it is archival authoring work, first verify live/saved dependencies and prepare a preserved local relocation receipt. Do not delete it, regenerate metadata or add a blanket Fused ignore rule to hide the decision.

The Blender owner **Set up Blender character scene** continues authoring separately. During closeout, a new `SourceArt/Blender/Studies/LimestoneBrickWall/` unit and `Tools/Art/Blender/finish_limestone_wall_tile.py` appeared. Those paths are owner work; they remain outside this audit's commit and need the owner's complete, verified batch before a globally clean worktree can be expected.

The four materials and seven Fused files have a byte-verified preservation copy at `Logs/PublicationReadiness20261012/PreservedDirtyArt/`. The originals were not changed or removed. This same-drive copy protects the audit's reviewed bytes from accidental cleanup; it is not a drive-loss backup or a replacement for current unsaved Unity scene state.

## Ordered closeout and publication plan

1. **Provision current local recovery.** Select sufficient independent, non-synced storage. Hash and copy the current local-only sources and needed Git/LFS recovery data, preserve originals, then prove a bounded restoration. This is the strongest unresolved storage concern.
2. **Close author-owned dirty batches.** The input batch is now committed. Let the active Blender author finish its current unit. Resolve the four material diffs and Fused acceptance/dependencies; commit only verified task-owned batches. A clean worktree means settled ownership, not deletion or ignoring to conceal pending work.
3. **Confirm LFS capacity.** Obtain the account's current usage and budget truth. Do not infer it from cache bytes or the billing API's 404 response. Do not change billing/account settings without the author's explicit instruction.
4. **Freeze a final publication candidate.** Require a stable `HEAD`, exact commit/path inventory, classified remaining dirty state and candidate-bound proof. The terrain migration has full native and 41-test evidence; the input batch has its separate 173-test and measured-capture evidence. Verify any later coupled changes rather than promoting older proof to the new candidate by assumption.
5. **Refresh publication gates.** Fetch/read remote `main` immediately before publication, establish ancestry, inspect the complete unpublished range for excluded sources and oversized ordinary blobs, rerun the LFS dry run and local-object checks, and refresh the ordinary-pack estimate. Disclose all ancestor commits included in the push. Preserve all unpublished history and source copies.
6. **Publish the exact approved SHA without force.** Upload only the eligible LFS object set and fast-forward `origin/main` to the sealed SHA, splitting into validated ancestor batches only if pack size requires it. Read the remote ref back and verify convergence. Do not use `--all`, `--mirror`, skipped LFS hooks or a history rewrite as a shortcut.
7. **Verify restoration and final status.** Confirm a remote clone can materialize the required payloads after upload, then check the final index/worktree and report any newly authored files separately. Never claim a clean tree while active authoring is adding files. Preserve source backups and required caches even after publication.

The current request establishes the publication direction, but this audit does not execute a push or manufacture a clean tree while billing, recovery coverage and asset ownership decisions remain unresolved. No new permission is needed merely to repeat already-authorized read-only gates; storage purchases, changing the local-only policy, destructive cleanup, forced history operations and taking over another author's files remain separate decisions.
