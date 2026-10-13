# Audit of the publication closeout plan

October 12, 2026. Reviewed plan: [publication closeout plan](PUBLICATION_CLOSEOUT_PLAN_2026-10-12.md), committed in `ed484a4f54fd9743e98be84946ea2e8a77187bea`. Verdict: sound preservation rules, but revise the execution gates before using it as a publication checklist. This audit proposes corrections; it does not modify the reviewed plan or execute its asset, backup or publication batches.

The current worktree still has the four material diffs and the seven-file Fused group. The limestone wall batch is committed. No Git changes were staged at audit start. The earlier source-backup gap, LFS accounting gap and pack measurement remain dated evidence requiring refresh when their corresponding operations begin.

## Findings

### P1 Separate publication readiness from complete storage and worktree closeout

Plan lines 23 and 28 make PUB-07 wait for PUB-02 through PUB-06 and require a clean ordinary worktree before publication. Line 81 combines all outcomes into one final completion condition. This makes an external backup destination or an unrelated Fused acceptance decision block otherwise verified local commits and a sealed publication candidate indefinitely.

The [Gear Ball SOP](../../Agents/GearBall/sops/GIT_AND_GITHUB_PUBLICATION.md), sections 8–10, permits an exact owned commit and a sealed push while explicitly naming preserved unrelated dirty work. The [source policy](../LOCAL_ONLY_SOURCE_POLICY.md) requires source preservation and publication exclusion; it does not require a new independent device before every non-destructive commit or push. Independent backup remains an important outstanding outcome, but the plan invented a universal dependency.

Proposed correction: track three separate milestones: **verified local commits**, **verified publication**, and **complete storage/clean-worktree closeout**. After ownership is settled, commit verified batches immediately. A sealed SHA may be published when its scope, authority, history, LFS/account capacity and candidate-bound validation pass, even if unrelated authoring remains preserved and disclosed. Keep PUB-02 as a parallel storage workstream and a prerequisite to destructive cleanup or source relocation. Mark the overall storage/clean-worktree goal incomplete until its separate gates pass. Do not weaken required publication validation or stage unfinished assets.

### P2 Make recovery snapshots consistent and prove independent Git/LFS restoration

Plan lines 36–38 include Git objects/refs/config/index and local LFS objects, then describe version sealing chiefly in terms of authoring units. The restoration examples cover Blender and terrain source units, but do not establish a coherent recovered Git repository or recovered LFS object store. A raw `.git` copy during a commit, object repack or index write can be inconsistent. A copied alternates file or external `lfs.storage` configuration can silently retain dependence on the original machine's object store.

Proposed correction: before copying Git state, verify no Git writer is active and record stable `HEAD`, refs, shallow boundary, index digest and configuration. Repeat the state comparison after the copy; discard or version the candidate snapshot for reconciliation if it changed. Inventory alternates and external LFS storage explicitly. Include every required referenced object in the independent recovery set and eliminate unresolved dependencies on the production checkout in the recovery test. Run `git fsck --connectivity-only` on the restored repository and materialize a representative required LFS payload using only the restored cache, with no production alternates or network fallback. Compare its SHA-256 to the recorded baseline. Preserve the original repository and older copies throughout.

Source transfer still requires complete file coverage and destination SHA-256 verification. An authoring version changing during transfer invalidates that version's receipt; an initial source hash alone is insufficient.

### P2 Complete the material and prefab validation gates

PUB-04 at line 20 requires focused import/reference checks but omits the canonical non-mutating validator and focused structural tests required by the SOP for material changes. PUB-05 at line 21 lists useful mesh/prefab checks but leaves the executable proof selection unspecified. That makes a batch easy to declare complete after import alone.

Proposed correction: before either asset batch is edited, name the exact relevant validator and focused test classes in its manifest. For materials, include GUID/reference checks, import, applicable canonical validation and structural tests of affected consumers. For accepted Fused content, include mesh topology and bounds, collider/material/submesh relationships, prefab/component serialization, metadata/GUID integrity, import and the applicable fusion tests. Inspect the methods for side effects before invoking them. Existing [SculptedRockFusionTests](../../../Assets/_Project/Tests/Editor/SculptedRockFusionTests.cs) concern this authoring lane; [TopDown3DRockFusionValidator](../../../Assets/_Project/Scripts/Editor/Validation/TopDown3DRockFusionValidator.cs) checks macOS native-plugin dependencies and is not sufficient proof for this Windows-generated prefab. Choose checks by implementation, not similarly named tools.

If a field is proven irrelevant to rendering and all consumers, a comparison need not become a new visual acceptance task. If appearance meaning remains uncertain, keep the author's creative decision separate from technical validation. Do not run a broad gameplay smoke test or repeat unrelated suites.

### P2 Add an affordable and capacity-checked remote restoration gate

PUB-10 at line 67 requires a fresh remote clone plus Unity validation but gives no disk/RAM budget or distinction between transport verification and repeated engine verification. Current D: free space is roughly 11 GiB, while the preceding fresh validation project required the checkout, Git and LFS objects, package cache and Unity import artifacts. The plan could successfully publish and then fail its own final gate for predictable capacity reasons. Repeating a full cold terrain import merely because the identical bytes arrived from GitHub also conflicts with the plan's own evidence-reuse rule at line 59.

Proposed correction: before publication, budget the remote restore destination, Git data, materialized files, LFS cache, package/import artifacts and the 3 GiB reserve; also confirm download allowance. Use a fresh object/cache location with no local production alternates or LFS cache redirection. Prove the sealed remote SHA and required payload/meta hashes first. When they match an already validated candidate and editor/package pins and relevant dependencies match, reuse that engine proof and record the equality argument. Run new focused Unity checks only for changed or unresolved imported/dependency contracts. Reserve full fresh import/terrain validation for a concrete concern that requires it, and choose its destination before launching.

### P2 Treat 100 GiB as an allowance and make rollback rights explicit

Line 34 says both “at least 100 GiB” and “as a planning allowance.” This mixes a convenience estimate with a hard gate. Line 73 describes same-path rollback without explicitly conditioning it on current ownership or later edits. Either can create needless delay or overwrite authoring that changed after the preservation copy.

Proposed correction: derive required destination space from the sealed manifest, temporary/restore overhead and reserve. Keep 100 GiB as an initial estimate, not a purchasing requirement or a reason to reject a smaller sufficient destination. Before rollback, verify the current file still matches the task's post-change hash and that no owner wrote a later version. On mismatch, preserve both versions and stop that rollback for reconciliation. Material restoration and Fused relocation require their own explicit owned scope; a general clean-worktree goal is not permission to restore user-owned dirty files by assumption.

## Corrected execution order

1. Record current ownership and preserve the exact reviewed bytes; account for unsaved state without forcing saves or focusing Unity.
2. Progress independent local recovery, account-capacity confirmation and owned asset decisions in parallel. Avoid a hardware purchase or unresolved unrelated asset becoming a blanket commit blocker.
3. Commit each verified owned batch. Include the exact validator/test plan for material or prefab changes before calling that batch complete.
4. Seal a publication SHA and report its complete unpublished ancestry. Keep preserved unrelated dirty work explicitly outside that SHA.
5. Refresh remote ancestry, source exclusions, the exact LFS object allowlist, account capacity and pack estimate; allocate the remote restoration destination before upload.
6. Publish only the exact eligible SHA without force, preserve the LFS hook, reconcile partial transfers and read the remote ref back.
7. Verify remote payload/meta equality and use candidate-bound engine proof appropriately. Report publication separately from remaining source recovery, asset decisions or authoring.
8. Complete independent source/Git/LFS recovery and settle all remaining owned work before claiming the overall clean-worktree/storage objective complete.

The publication checks should retain an exact LFS object allowlist from the frozen candidate's complete unpublished range, not only current-tree paths. Reconcile the dry-run OIDs to that allowlist and verify local object hashes/sizes before the hook uploads anything. This makes the plan's existing full-history exclusion rule executable without introducing a second upload path.

## Requirements that already hold

Keep `main`, exact non-force publication, source exclusion across unpublished history, unchanged `.meta` files and GUIDs, classified ownership, no metadata regeneration, no hidden deletion/ignore cleanup, no unrequested account changes, background-only Unity operation, and no gameplay smoke testing. The plan correctly preserves partial-transfer state and avoids `--all`, `--mirror`, force pushes and skipped LFS hooks.

The material, Fused, source and account decisions remain unresolved implementation inputs. This audit does not claim those decisions were made or those batches were executed. Documentation links and the reviewed line references were checked; no Unity launch, source transfer, asset edit or push was needed to audit the plan.
