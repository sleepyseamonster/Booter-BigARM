# Large file storage audit and recommendation

October 12, 2026. Owner: Gottspan, with Gear Ball responsible for any later Git migration. Status: research and proposed storage design; no storage migration authorized by this report.

Keep GitHub as the project’s code and coordination home. Use selective Git LFS for approved binary working assets, and a separate archive with versioned manifests for large evidence and source collections. A database adds little value for these files: object storage holds their bytes, while Git can hold the small records that identify the exact version belonging to a project revision.

The first improvement should be classifying the terrain evidence currently imported under `Assets`, rather than moving every large file into LFS. Existing tooling depends on some of these captures, so classification is followed by dependency verification, not immediate removal. Blender files and `SourceData/Terrain/` remain local under the [current policy](../LOCAL_ONLY_SOURCE_POLICY.md). Private cloud backup of those sources would require explicit permission to change that local-only boundary.

## Measured repository state

The [audit evidence](../../Evidence/Repository/LARGE_FILE_STORAGE_AUDIT_2026-10-12.json) records logical file sizes, available Git objects, local LFS cache and limitations. The measured revision is `c3d3f771a6ec3d2431758cf20106f81625dadb3a`; start and end HEAD matched. The branch was `main`, 142 commits ahead of cached `origin/main` at `2c522e4ed50d692aa6ce6e581cf56e3ee4406d8a`. No live remote or billing query was made. The four dirty materials and untracked Fused rock folder were outside this task.

| Collection | Measured size | Meaning |
| --- | ---: | --- |
| Current committed tree | 15.819 GiB, 164,557 files | Logical checkout content, including repeated identical blobs |
| Terrain content under `Assets/_Project/Art/Terrain/` | 13.921 GiB | Dominant tracked collection |
| All tracked `.bytes` files | 10.861 GiB, 51,970 files | Mostly repeated terrain captures and snapshots |
| All tracked `.asset` files | 2.228 GiB, 9,442 files | Includes native Unity data; not all are binary |
| All tracked PNGs | 1.110 GiB, 9,785 files | Includes terrain imagery and other textures |
| Project content on disk excluding caches and Git | 63.512 GiB | Includes ignored local sources; not an upload estimate |
| Local-only source selection | 46.543 GiB, 52,731 files | Terrain sources and Blender files/backups, including retained transfer Blender files |
| Local LFS object cache | 16.632 GiB, 719 files | Includes retained payloads; does not establish remote billing |
| Available reachable ordinary Git blobs | 11.017 GiB, 142,394 unique objects | Uncompressed object lengths in locally available shallow history |
| Git packed objects | 3.92 GiB | Compressed local history, separate from LFS and checkout bytes |

`SourceData/Terrain/DeathValley/` contributes 30.975 GiB and Blender sources in `SourceArt/Blender/Studies/` contribute 14.946 GiB. The disk inventory excludes `Library`, `Temp`, `Logs`, `UserSettings`, build outputs, `.venv`, `.git` and Python caches. It does not measure the old C: rollback checkout, external validation projects or other drive backups. These are logical sizes, not physical allocated space or deduplicated backup size.

There are no committed files over GitHub’s 100 MiB ordinary-Git file limit. The largest tracked file is a terrain manifest at 50,929,698 bytes, below the 50 MiB warning threshold. Small individual files can still produce an expensive repository when there are tens of thousands of them. GitHub recommends keeping repositories ideally below 1 GB and strongly recommends below 5 GB; compressed local pack size and logical checkout size are different measurements. [GitHub large-file guidance](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github).

The committed `.gitattributes` contains only comments; it defines no LFS filters. One pointer-shaped text receipt remains in the current tree, at `Docs/Agents/Blender/archives/2026-10-06-transfer/references/BadwaterGameSlice.lfs-pointer.txt`. It is evidence, not an active Blender asset. There are 677 pointer blobs in available history, referencing about 12.159 GiB of unique payloads. Neither that figure nor the cache size measures GitHub’s current billable storage.

The cached unpublished range has no commits touching the excluded Blender/terrain-source path patterns and no newly reachable excluded-path blobs under the audit’s representative-path check. This is a useful local finding, not a fresh publication clearance: the shallow boundary, other refs, future changes and live remote state still require Gear Ball’s publication gate. Git reports 232.14 MiB of temporary object garbage. Preserve it pending a separate recovery/maintenance review; this task performs no pruning.

## Terrain evidence is the first storage opportunity

The tracked `.bytes` collections divide as follows:

| Path category beneath terrain owner folders | Files | Logical size |
| --- | ---: | ---: |
| `Source/Baseline` | 29,696 | 7.307 GiB |
| `Source/Render` | 8,704 | 2.142 GiB |
| `Source/NativeSnapshot` | 4,608 | 1.134 GiB |
| `Source/Heights` | 8,960 | 0.278 GiB |
| `Source/SerializationVariants` | 2 | Less than 0.001 GiB |

These are path categories, not proof that any file is expendable. Across all tracked `.bytes` paths there are 26,626 distinct Git blobs totaling 4.625 GiB. Git already shares identical blob content; therefore removing 6.236 GiB of repeated checkout paths would not save that amount in remote ordinary Git storage. A future archive could deduplicate by content hash, while preserving every path and its provenance.

Existing contracts require the captures. For example, `BadwaterNorthRowSource`, `BadwaterNorthRow2Source`, `BadwaterNorthRow3Source` and `BadwaterNorthRow4Source` verify render/native source paths and hashes. `Tools/Art/Blender/death_valley/export_north_row2_unity.py` deliberately copies immutable full native snapshots into an imported shared owner and validates protected files. Some manifests approach 50 MB because retained records accumulate. Blanket `.bytes` exclusion, broad `*.asset` LFS tracking or folder movement could break editor validation, rebuilds or Unity references.

Before migrating any category, distinguish runtime dependencies, editor reconstruction inputs and immutable historical evidence. Preserve all `.meta` files and GUIDs, exact sample orientation, normalized heights, geographic keys, manifests and hash contracts. Keep an explicit restore mapping for every displaced path. A smaller Git tree requires changes to consumers and validation ownership, not just a new storage bucket.

## Storage options and fit

| Option | Appropriate role | Project assessment |
| --- | --- | --- |
| Ordinary Git on GitHub | Code, metadata, text scenes, small manifests and receipts | Retain as the main coordination system. Avoid accumulating raw capture sets here. |
| GitHub LFS | Binary working assets needed for a normal checkout | Best initial integration for an explicitly selected asset set; simpler restoration than a custom downloader. |
| Local external disk or NAS with versioned snapshots | Local-only Blender and GIS sources; recovery copies | Fits current authority. Use independent copies and a tested restore process; same-drive copies are insufficient protection from drive loss. |
| Backblaze B2 plus Git manifests | Archives, checkpoint bundles and optional future source backup | Preferred cloud archive candidate if approved. Low storage cost suits infrequent restores. |
| Cloudflare R2 Standard plus Git manifests | Frequently downloaded artifact sets | Preferred object store when collaborators or CI repeatedly download large sets. |
| DVC backed by object storage or NAS | Datasets whose exact versions must follow Git revisions | Useful later for GIS/prepared data; adds a second checkout command and cache. Pilot outside production assets first. |
| Self-hosted LFS backed by object storage | Preserve LFS workflow while controlling storage backend | Feasible but adds server, authentication, locking, availability and backup ownership. Defer until those costs are justified. |
| Unity Version Control or Perforce P4 | Large collaborative asset authoring with locking | Revisit if several artists need coordinated editing. Replacing or splitting source control is larger than an archive-storage fix. |
| SQL or document database containing file blobs | Application queries or transactional metadata | Poor first investment here. Git manifests already provide version linkage; a database would add operations without solving asset restoration. |

GitHub LFS charges each changed version’s entire file size, and downloads consume the repository owner’s bandwidth, including CI downloads. Free/Pro include 10 GiB each of storage and monthly download bandwidth; Team/Enterprise Cloud include 250 GiB each. These are account allowances, not guarantees available exclusively to this repository. [GitHub LFS billing](https://docs.github.com/en/billing/concepts/product-billing/git-lfs).

GitHub’s calculator lists additional LFS storage at $0.07/GiB-month and download bandwidth at $0.0875/GiB. Its introductory wording still describes metered billing as future, while the billing documentation says it is active; use the displayed rates for estimates and confirm the actual account plan, usage, remaining allowance and budget before adoption. LFS per-file limits are 2 GB on Free/Pro, 4 GB on Team and 5 GB on Enterprise Cloud. [GitHub calculator](https://github.com/pricing/calculator?feature=lfs), [LFS file limits](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-git-large-file-storage).

Backblaze B2 advertises $6.95/TB-month, the first 10 GB free, no minimum storage duration, and free downloads up to three times average monthly stored data. Additional direct downloads cost $0.01/GB. Common Class A/B/C API calls are free for pay-as-you-go customers; Class D calls have separate pricing. [B2 pricing](https://www.backblaze.com/cloud-storage/pricing).

R2 Standard costs $0.015/GB-month with 10 GB-month free. Direct egress is free; operations above the included allowances cost $4.50/million Class A and $0.36/million Class B. Infrequent Access introduces retrieval fees and a 30-day minimum, so Standard is the clearer first pilot. Connected services can add their own charges. [R2 pricing](https://developers.cloudflare.com/r2/pricing/).

DVC supports external S3-compatible storage, local directories and NAS, with small tracking/configuration files committed to Git and credentials kept in ignored local configuration. It provides dataset version linkage but not an artist locking workflow. A raw S3-compatible bucket is also not a complete LFS server: Git LFS expects its batch/transfer protocol. [DVC remote storage](https://doc.dvc.org/user-guide/data-management/remote-storage), [Git LFS transfer API](https://github.com/git-lfs/git-lfs/blob/main/docs/api/basic-transfers.md).

Unity Version Control has Smart Locks and a current cloud storage model worth reconsidering if collaborative authoring becomes the issue. Unity lists 25 GB included Standard storage, $0.10/GB from 25–100 GB and $0.06/GB above 100 GB, with 100 GB egress included and $0.10/GB thereafter. These published DevOps allowances need account-level confirmation. Perforce’s self-hosted free tier allows five users and 20 workspaces, but server hosting, backup and administration remain the team’s responsibility; P4 Cloud starts at $39/user/month with 64 GiB included. [Unity pricing](https://unity.com/products), [Unity Smart Locks](https://docs.unity.com/en-us/unity-version-control/smart-locks), [Perforce pricing](https://www.perforce.com/products/p4/pricing).

AWS S3 is another viable object store, especially with an existing AWS account. Its region, storage class, request, retrieval and transfer charges need a workload-specific quote. No current AWS integration was established by this audit. It offers no demonstrated project advantage over the simpler B2/R2 shortlist. [AWS S3 pricing](https://aws.amazon.com/s3/pricing/).

## Illustrative monthly costs

These examples use identical stored bytes and monthly direct downloads, steady storage for a full month, USD, and one dataset version. They exclude taxes, subscriptions, extra retained versions, optional services and operations beyond free allowances. GitHub assumes a Free/Pro account with the full 10 GiB allowance available. B2 assumes decimal TB and the 10 GB allowance; R2 uses its documented whole-unit rounding. GiB is 1,073,741,824 bytes; 100 GiB is approximately 107.374 GB.

| Stored / downloaded each month | GitHub LFS | B2 | R2 Standard |
| --- | ---: | ---: | ---: |
| 100 GiB / 100 GiB | $15.05 | About $0.68 | About $1.47 |
| 1,000 GiB / 1,000 GiB | $155.93 | About $7.39 | About $15.96 |

For the first LFS row, `(100−10)×0.07 + (100−10)×0.0875 = $15.05`. B2 downloads stay below its three-times-storage allowance in both rows. R2’s higher storage rate can be offset by its free direct egress when download volume grows. These prices do not make the workflows equivalent: an archive needs manifests, authentication and restore tooling, whereas LFS integrates restoration into Git operations. Do not compare a full archive price with only the latest working version in LFS; include retained history on both sides.

The current tree cannot be treated as 15.819 GiB of future LFS storage. Text, repeated identical content and evidence should be classified first. The GitHub account’s plan, billable retained LFS objects, other repositories sharing quota, normal monthly download volume, collaborator count and CI cache behavior remain inputs to the final purchase decision.

## Proposed storage design

Keep production code, scenes, prefabs, `.meta` files and small canonical manifests in Git. Select LFS paths by asset ownership and real format, starting with large approved textures, meshes and binary TerrainData that must arrive with a clone. Do not blanket-track every `.asset`, JSON or scene, and do not add Blender/terrain-source LFS rules.

Keep raw GIS, editable Blender sources and their backups local. Establish versioned backups to an independent local device and an offline or physically separate copy, with periodic restoration. The retained C: checkout is a historical rollback copy; its freshness and completeness were not checked. An encrypted private B2 backup is a possible later policy revision, not implied permission under “local-only.”

Use an archive inventory tied to Git for approved nonruntime evidence. Each entry should contain a stable artifact ID, relative restore path, content SHA-256, byte length, schema version, generating tool/version, source provenance, associated Git revision and access class. Store objects under immutable content-derived keys, never a mutable `latest` filename. Small manifests remain versioned; bulk receipts can be separate checkpoint bundles. Store credentials outside Git, give build readers read-only access, and isolate archives from generated caches.

An artifact revision should be available and hash-verified before its Git manifest is committed for publication. A fresh clone must restore every required payload before Unity opens it. Optional historical evidence can restore on demand through a documented command. Archive availability is a build dependency only if production consumers require it; avoid introducing a runtime cloud dependency.

Storage migration does not change deterministic world identity, geographic keys, stable object identities, authored terrain constraints or persisted runtime deltas. Greater Wasteland remains fixed and always loaded. Terrain streaming and unload/reload behavior are not part of this storage task because it only changes authoring/recovery file delivery. No save service or terrain generator is reactivated.

## Migration sequence and acceptance gates

1. **Seal the classification.** Reinventory the current revision, resolve asset ownership and produce exact required/optional/local-only path manifests. Map scene, editor, exporter and receipt consumers. Include archive versions in capacity estimates and inspect the live unpublished range before any publication.
2. **Prove local restoration.** Make independent versioned copies of excluded sources and a recoverable Git/LFS snapshot. Restore a representative checkpoint into an isolated folder and compare SHA-256 inventories. Preserve source data and all current history; do not prune caches as part of migration preparation.
3. **Pilot optional evidence.** Use a bounded completed checkpoint with its existing contracts. A local archive pilot fits current policy. Cloud pilots require an approved provider/account and exact upload scope; excluded sources require a separate policy change. Record bytes, upload/download duration, interruption recovery and operating effort.
4. **Validate references before relocation.** Update current path resolvers and dependency manifests deliberately. Preserve historical receipts, source hashes and Unity GUIDs; add explicit relocation mappings rather than rewriting evidence as if it originated elsewhere. Run only the focused editor/export/restore checks affected by the change.
5. **Pilot selective LFS.** Choose a small approved production binary set. Verify pointer/materialized hashes and asset/meta pairs in an isolated fresh clone, then run relevant Unity import and non-mutating validation in background. Test restoration with missing credentials and interrupted transfer so failures are explicit. Hands-on gameplay smoke testing stays user-owned.
6. **Handle Git history separately.** Adding LFS rules or deleting current files will not shrink existing ordinary Git history. Inspect published versus unpublished lineage and preserve a local recovery snapshot before proposing any rewrite. Published-history changes, force pushes and remote LFS deletion require explicit separate authority.
7. **Adopt after the pilot passes.** Require complete required-asset restore, exact hashes and references, repeatable Windows instructions, a measured cost estimate, recovery ownership and selected retention periods. Keep a rollback mapping. Publish only with current user authority.

GitHub says removed LFS payloads can continue counting toward remote storage, and directs users to Support when purging without deleting/recreating the repository. Use a billing/Support review if retained historical payloads are costly; do not delete the repository or assume local pruning clears the bill. [GitHub LFS removal guidance](https://docs.github.com/en/repositories/working-with-files/managing-large-files/removing-files-from-git-large-file-storage).

## Verification and remaining decision

The audit used `git ls-tree -rlz HEAD`, indexed working-file sizes, a filesystem inventory excluding generated caches, `git rev-list --objects --all` with `git cat-file --batch-check`, bounded reads of blobs no larger than 1 KiB to identify LFS pointers, `.git/lfs/objects` file sizes, and cached unpublished-range path inspection. Equal Git OIDs establish identical content, not interchangeable Unity references. No payloads were uploaded, deleted, moved, converted to LFS or rehashed wholesale. No Unity launch or gameplay test was needed for this documentation task.

The report recommends keeping GitHub, selectively using LFS for required binaries, and prioritizing terrain evidence classification. Choose B2 for an approved cloud archive with occasional restoration, or R2 when repeated downloads dominate. Maintain local-only source backups until the author explicitly changes that policy. The next implementation decision is the exact archive scope and backup location; a source-control replacement or database deployment is not justified by the measured problem.
