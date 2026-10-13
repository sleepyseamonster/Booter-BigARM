# Project publication and storage closeout plan

October 12, 2026. Owner: Gottspan. Git integration/publication: Gear Ball. Unity validation: Babineaux. Authoring decisions: the author and the existing Blender owner. Status: prepared for execution; this document does not execute publication, asset cleanup, account changes or source transfers.

Complete local recovery, resolve the remaining asset ownership decisions, commit verified batches, then publish an exact validated `main` revision and verify remote restoration. Keep Blender sources/backups and `SourceData/Terrain/` local. A clean worktree must result from completed work, not discarded files or newly added ignore rules.

## Starting checkpoint

The planning snapshot is `fb4e2b792a1f3bf201db956f78487461edd44993` on `main`, 159 commits ahead of cached `origin/main`. The [readiness audit](PUBLICATION_READINESS_REASSESSMENT_2026-10-12.md) established a matching live remote, repository write access, an eligible 18,210-object LFS dry run and a 1.770 GiB ordinary Git pack estimate. These publication checks must be refreshed for the final SHA.

The terrain LFS batch has byte-preservation, fresh local restoration, full native terrain validation and 41-test proof. The input owner subsequently committed its 173-test and profiling batch. During this planning turn the Blender owner committed the wall unit as `275b57f24c2040a560e96687ddb6885bd7d4162e`, advancing the branch to 160 unpublished commits before this plan's commit. The remaining dirty work is four materials and the seven-file Fused group. Preserve the [existing source-publication policy](../LOCAL_ONLY_SOURCE_POLICY.md), exact Unity paths, `.meta` files and GUIDs throughout.

## Execution batches

| Batch | Owner | Action and deliverable | Completion gate |
| --- | --- | --- | --- |
| PUB-01 | Gottspan with authoring owners | Record a fresh `HEAD`, status, staged ownership, active writers and exact path manifests. Separate the four materials, Fused content and Blender wall unit. Record any unsaved editor state through a background-safe read. | Each path has an owner and disposition. No source file or scene is overwritten to establish the baseline. |
| PUB-02 | Gottspan; destination selected by the author | Create a current recovery set on independent, non-synced local storage. Preserve originals and older copies. Produce source, Git/LFS and uncommitted-work manifests plus transfer receipts. | Fresh source/destination SHA-256 equality, complete coverage, sufficient free space and a successful isolated restore. |
| PUB-03 | Gear Ball | Verify the account's actual LFS usage, applicable allowance and budget through authorized account access. Record whether the planned upload and a restore fit. | Available capacity and download budget are established. A 404 billing response or local cache size is not accepted as quota proof. |
| PUB-04 | Gottspan and Babineaux | Resolve the four material diffs as one bounded batch: inspect `_Color`, `_BaseColor`, actual shader consumers and intended appearance. Preserve the reviewed bytes and GUIDs. | Retain and commit justified edits after focused import/reference checks, or restore proven incidental serialization with a preserved copy and documented rationale. Uncertain appearance changes require the author's decision. |
| PUB-05 | Blender owner and Babineaux | Determine the Fused group's intended status and its saved, binary and unsaved scene dependencies. Choose accepted reusable content or preserved local authoring work. | Accepted content: verify mesh, collider, materials, prefab, GUID uniqueness and import; commit all seven files together. Local authoring: prove reference safety and a hash/GUID-preserving local relocation before removing any original path. |
| PUB-06 | Existing Blender owner; Gottspan reconciles | Reconcile wall commit `275b57f24` with its owner proof and exact path manifest. Keep `.blend` files and backups excluded. | The committed owner unit is accounted for in the publication range; any subsequent authoring has a separate disposition. No takeover of actively edited files. |
| PUB-07 | Gottspan and Gear Ball | Freeze the integrated candidate, commit remaining verified task-owned batches and seal the final SHA. Assemble current validation receipts. | Stable `HEAD`, clean index and clean ordinary worktree; ignored local sources remain present. All coupled changes have candidate-bound proof. |
| PUB-08 | Gear Ball | Refresh remote ancestry, complete unpublished-history exclusions, ordinary object limits, LFS selection/cache integrity and pack estimate. Prepare the exact push scope. | No excluded source objects, missing LFS payloads, divergence, unknown staged content or unresolved validation failure. Disclose every included ancestor commit. |
| PUB-09 | Gear Ball | Publish the exact sealed SHA by non-force fast-forward, then read remote `main` back. Use validated ancestor batches only if the pack limit requires them. | Remote ref equals the intended SHA and required LFS objects are available. A failed transfer is reconciled before retrying. |
| PUB-10 | Gear Ball and Babineaux | Restore the published revision into a fresh isolated clone, verify payload/meta identities, run the directly relevant checks and record final status. | Remote restoration succeeds; intended revision, index and worktree converge; no excluded source was published or lost. |

PUB-02, PUB-03 and owner-driven PUB-04 through PUB-06 can progress independently after PUB-01. PUB-07 waits for their gates. PUB-08 follows the exact integrated candidate, PUB-09 follows the publication checks, and PUB-10 closes the job. Parallel work means independent ownership in the existing lanes; this plan does not start delegates or authorize messages to other chats.

## Recovery set and capacity

Current audited local-only sources total approximately 46.848 GiB. The retained C: checkout lacks about 43.125 GiB of current source paths. Its same-path/size matches are not SHA-256 proof. Neither the D: validation clone nor a second D: folder is an independent drive-loss backup.

Use a non-synced destination on a separate physical device. Reserve at least 100 GiB of free space for the initial recovery set as a planning allowance: roughly 47 GiB of sources, 20 GiB of retained LFS objects, 5 GiB of available Git history, uncommitted files, manifests and operating reserve. Recalculate the exact size before copying; add capacity if a full materialized project snapshot is required. Maintain at least 3 GiB free on each working drive. Current C:/D: free space does not safely fit the missing sources plus recovery overhead.

Include all local-only Blender files and numbered backups, complete terrain source units and resource dependencies, current available Git objects/refs/config/index, local LFS objects, and accepted uncommitted authoring bytes. Preserve the existing shallow-history boundary rather than representing the snapshot as complete historical GitHub history. Do not rely on a shared-object clone or a Git bundle alone for LFS and ignored sources.

For the copy, seal saved authoring versions and prevent concurrent writes to the selected units. Record relative paths, SHA-256, byte sizes, provenance, source revision and paired Unity metadata where relevant. Copy complete dependency units to versioned destinations; never overwrite a distinct backup. Verify the destination independently, then restore a representative Blender/resource unit and terrain source/checkpoint unit into an isolated location. Byte-copy proof does not capture unsaved editor memory; record that boundary and let the owner resolve it without forcing a save or foreground activation.

A new device purchase, a cloud backup or a local-only policy change needs an explicit author decision. No source deletion, mirror-with-delete, LFS pruning or validation-project retirement is part of this recovery batch.

## Exact dirty asset boundaries

The material batch is limited to:

- `Assets/_Project/Materials/TopDown3D/Camp/PackCanvas.mat`
- `Assets/_Project/Materials/TopDown3D/Camp/PackFrame.mat`
- `Assets/_Project/Materials/TopDown3D/Camp/PackWebbing.mat`
- `Assets/_Project/Materials/TopDown3D/Greybox_BigARM.mat`

Keep their `.meta` bytes and all serialized references intact. The three camp `_Color` diffs are actual serialized value changes, so whitespace cleanup alone does not resolve the batch. If a retained change depends on visual acceptance, prepare the concrete comparison and leave that creative choice with the author. Do not redesign shaders or colors to achieve a clean status.

The Fused batch is `Assets/_Project/Art/Environment/Rocks/Fused.meta` plus the six `RockFusion_A7196D3D8461480E` mesh/prefab/report files and their metadata beneath `Fused/`. Preserve this complete unit. The saved-YAML audit found no external references, but that does not establish absence of binary or unsaved references. Do not save the production scene, delete the result or ignore the folder by assumption.

The Blender wall batch is `SourceArt/Blender/Studies/LimestoneBrickWall/` and `Tools/Art/Blender/finish_limestone_wall_tile.py`, plus only the owner's deliberately identified dependencies and receipts. The owner committed this unit during planning. Reconcile that commit and inspect later status before integration; its existence is not permission to publish native Blender sources.

## Candidate validation and publication

Use existing proof when content and relevant dependencies match the tested candidate. Run new focused checks when material/mesh changes or integration affect that proof; do not repeat the earlier full cold terrain validation solely because documentation changed. New binary assets require format, metadata, dependency and Unity import proof. Runtime changes require current compilation and their focused tests. Keep smoke testing, gameplay feel and appearance acceptance user-owned.

Before PUB-09, read/fetch remote `main`, verify it is an ancestor of the candidate, and inspect every commit that would be sent. Exclude `.blend` files/backups and `SourceData/Terrain/` from the complete unpublished range, not just the final tree. Verify each required LFS object's SHA-256/size and the effective attribute match set. Preserve the LFS hook. Confirm the account budget and restore bandwidth before uploading.

Refresh the ordinary Git thin-pack estimate. The earlier 1.770 GiB measurement is below the documented 2 GiB push limit but is not a permanent guarantee. If needed, choose smaller already-validated ancestor SHAs with a margin below the limit and publish them in order. A single oversized commit requires a separate reviewed solution; do not rewrite history automatically. Use an exact non-force refspec such as `git push origin <sealed-sha>:refs/heads/main`; never substitute `--all`, `--mirror`, force flags or skipped LFS upload for a failed gate. The source-backed [audit](PUBLICATION_READINESS_REASSESSMENT_2026-10-12.md) contains the current GitHub references.

After each transfer, verify the remote SHA before claiming publication. If LFS upload succeeds but the Git ref update fails, record that partial external state and refresh ancestry; do not prune objects or upload a different candidate blindly. Remote LFS deletion and billing changes are separate operations.

For PUB-10, use a fresh clone without production-object alternates and materialize the required LFS payloads from the published remote. Compare required asset and `.meta` hashes with the sealed candidate. Run the relevant non-mutating Unity validator in an isolated background project; never open or focus the production editor for this check. Keep package/editor pins fixed.

## Stop conditions and rollback

Stop the affected batch on changing ownership, unexpected active writes, destination capacity below reserve, a hash/GUID/reference mismatch, missing current account capacity, remote divergence or failed candidate-bound validation. Continue independent read-only/preparation work where possible. Do not widen the task into gameplay fixes, purchasing, account changes or destructive cleanup.

Before asset changes, preserve exact original bytes and metadata with a source-to-destination receipt. Before a local commit, compare staged paths to the approved manifest and inspect the diff. Rollback restores the same paths, bytes and GUIDs from the preserved version; no metadata regeneration. Preserve old source/recovery versions and the current LFS cache even after publication succeeds.

World identities, geographic keys, authored terrain constraints and runtime save deltas remain unchanged. Greater Wasteland stays fixed and always loaded. Terrain streaming, generation and unload/reload behavior are outside this repository closeout.

## Decisions and final completion

The author must select the independent backup destination, resolve any genuinely ambiguous material/Fused acceptance decision, and confirm any account-budget change if required. The earlier instruction establishes the direction toward publication; this planning turn adds no push or cleanup action. Do not ask again for routine already-authorized inspection and verification.

The job is complete when current local-only sources have verified independent recovery, all owned batches are settled and committed, required Git/LFS data restore from the published revision, local and remote `main` match the sealed SHA, and the index/worktree are clean. Ignored local sources remain intact. Report exact commits, validation results, backup receipts, remote readback and any author-owned acceptance still outside the completed technical scope.
