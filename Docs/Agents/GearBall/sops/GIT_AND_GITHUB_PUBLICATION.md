# Gear Ball Primary Publication SOP

This is Gear Ball's controlling procedure for every commit-and-push publication run in Booter & BigARM. It requires relevant candidate-bound tests, permits only proven behavior-neutral repairs, and turns approved work into exact Git history and verified GitHub state.

The user's current instruction and root `AGENTS.md` remain authoritative. This SOP narrows ordinary execution; it does not create standing authority for external actions.

## 1. Establish The Publication Contract

Before mutating Git, identify:

- **Done** — the exact requested result: commit, push, pull request, or another named Git/GitHub state.
- **Scope** — the exact task-owned files, existing local commits, and approved branch.
- **Source** — the current user instruction, Gottspan integration handoff, canonical project documents, and implementation-owner evidence.
- **Tests** — the smallest relevant checks that prove the exact candidate without substituting unrelated broad activity.
- **Out** — unrelated dirty files, generated noise, visual acceptance, releases, deployments, and external actions not authorized.
- **Stop** — ambiguous ownership, an active writer, an unavailable required test, a non-neutral failure, remote divergence, or missing authority.

Commit, push, pull request, merge, tag, release, deployment, branch switch, and worktree operations are separate authority boundaries. Do not infer one from another.

## 2. Freeze And Classify Live State

1. Confirm the repository root and load Gear Ball's contract, runtime policy, memory, this SOP, Gottspan's manager contract, and the Git batching standard.
2. Inspect branch, upstream, remotes, linked worktrees, `HEAD`, concise status, and the full untracked manifest.
3. Use `git status --porcelain=v1 -z -uall` when filenames or complete manifest fidelity matter.
4. Inspect staged and unstaged diffs separately, including binary and rename summaries.
5. Classify every path as task-owned, unrelated, generated noise, or unresolved.
6. Compare local and remote lineage and count every existing commit that a push would publish.
7. Take repeated read-only status and `HEAD` snapshots when the tree may have active writers.

Do not stage while `HEAD` or the manifest is moving. Wait for stability, refresh the classification, and preserve owner commits as their own logical batches. Do not reset, restore, clean, stash, or unstage someone else's work to manufacture a clean tree.

## 3. Design Logical Batches

Group by one coherent concern, not by the order files happened to change.

- Keep runtime, editor tooling, tests, docs, serialized consumers, and paired `.meta` files together when they form one inseparable feature contract.
- Separate legacy preservation, gameplay systems, rendering, performance, governance, and generated-noise cleanup when they can stand alone.
- Do not split a Unity asset from its `.meta`, an intentional rename from its reference updates, or a signature change from required callers.
- Do not force tiny commits when the intermediate commit would fail to compile or serialize safely.
- Name every batch before staging and maintain an exact path manifest for it.

## 4. Select Relevant Tests

Tests are chosen from the affected surfaces. A broad unrelated check cannot replace the relevant gate.

| Changed surface | Required minimum proof |
|---|---|
| Agent contracts or Markdown docs | Diff/whitespace check, relative-link validation, routing/path existence, and applicable repo-health checks. |
| Runtime or editor C# | Current Unity import and compilation plus the focused EditMode tests for the changed behavior. Run the affected assembly suite when shared infrastructure changes. |
| Test code | Test-assembly compilation and execution of every added or modified test; never weaken assertions to obtain green. |
| Scene, prefab, material, renderer, or project settings | Unity import, the canonical non-mutating validator, and focused structural tests. Interactive visual or feel acceptance remains separate unless explicitly delegated. |
| Shader or rendering code | Unity shader import/compiler-message inspection, focused rendering/validator tests, and serialized renderer-reference checks. Visual acceptance remains separately named. |
| Art, animation, binary assets, or `.meta` files | File-format inspection, asset/metadata pairing, GUID uniqueness and continuity, reference checks, and Unity import. |
| Procedural generation or save identity | Determinism, chunk-border, unload/reload, stable-identity, authored-constraint, and persisted-delta tests that apply to the changed contract. |
| Build Settings or build automation | Structural validation, focused tests, and the relevant player build only when the changed outcome depends on build proof. |
| Git-only lineage or publication docs | Git integrity, exact manifests, documentation checks, and remote readback; Unity tests are not relevant unless project content also changed. |

Read task-specific standards and existing test inventories before inventing a new command. Use the narrowest test that exercises the changed owner, then add broader regression coverage only where shared coupling warrants it.

## 5. Run The Pre-Commit Test Gate

1. Verify the candidate manifest is stable immediately before testing.
2. Run the selected relevant tests against the complete working-tree candidate.
3. Capture the command, exit result, important counts, and proof boundary.
4. Inspect compiler, shader, validator, and test logs for failures that a summary could hide.
5. Distinguish compilation, executed tests, structural validation, interactive behavior, player build, and Git proof.

If the Unity GUI is open, never start batchmode against the same project. Use a documented background-safe path through the existing editor when one exists. Otherwise stop before commit or push and report that the required Unity test cannot run safely. Assembly timestamps or a successful domain reload may prove compilation, but they do not prove test execution.

Do not create or run gameplay smoke tests unless the user explicitly requests them. Focused EditMode, validator, import, compile, and build checks are not smoke tests and should run when relevant and safe.

## 6. Behavior-Neutral Repair Authority

Gear Ball may repair a publication candidate only when the exact diff and canonical project rules establish that player-visible behavior, game rules, world identity, serialization meaning, and asset references are unchanged.

Normally allowed within task-owned files:

- repository-standard formatter output;
- line-ending and trailing-whitespace normalization;
- established behavior-neutral lint autofixes;
- broken relative documentation links or stale documentation paths when the canonical destination is unambiguous;
- missing imports, namespace qualification, or API spelling corrections that preserve the existing implementation contract;
- deterministic metadata normalization for an already-intentional asset, while preserving its GUID;
- test-harness or validation plumbing fixes that make the intended production contract executable without weakening the contract;
- removal of inspected incidental Unity serializer noise when the before/after values are semantically identical.

Not allowed under ordinary Gear Ball repair authority:

- gameplay logic, input semantics, movement, physics, balance, timing, AI, survival rules, or camera feel;
- procedural seeds, generation versions, stable identities, save formats, persistence semantics, or runtime deltas;
- shader appearance, lighting, materials, VFX tuning, scene behavior, prefabs, or serialized gameplay values;
- asset moves, `.meta` regeneration, GUID replacement, packages, build targets, or project-wide settings;
- deleting, skipping, quarantining, weakening, or rewriting a failing test to make publication pass;
- speculative fixes whose behavior impact cannot be proved from the diff and relevant tests.

When a failure requires a non-neutral change, stop and return it to the implementation owner or user. Do not publish a knowingly failing candidate.

## 7. Repair And Retest Loop

For every allowed repair:

1. Identify the exact failure and canonical owner.
2. Make the smallest behavior-neutral change.
3. Inspect the exact diff for scope expansion or serialization risk.
4. Refresh the complete live manifest and confirm no writer introduced new work.
5. Rerun the directly affected test.
6. Rerun every downstream gate whose inputs changed.
7. Record the repair and evidence for closeout.

A passing check from before a repair is stale if the repair could affect it. Repeated or ambiguous failure is a stop condition, not permission to broaden the fix.

## 8. Stage Exactly And Commit Intentionally

1. Stage explicit task-owned paths for one logical batch. Avoid `git add -A` in a mixed worktree.
2. Review staged name-status, rename detection, binary summary, full staged diff, and `git diff --cached --check`.
3. Compare the staged path set exactly with the approved batch manifest.
4. Recheck live status so unrelated staged, unstaged, and untracked work remains visible.
5. Commit with a terse message naming the single concern.
6. Confirm the resulting commit contains only the approved manifest.
7. Repeat for each independently coherent batch.

If unrelated content is already staged, do not silently unstage or include it. Use an explicit path-only commit only when it preserves the existing index and the exact task manifest can still be proved; otherwise stop for an integration decision.

## 9. Seal The Final Candidate

After the last commit and before push:

1. Confirm the intended commits and their order.
2. Confirm the worktree and index are clean, or name every intentionally preserved unrelated path.
3. Rerun final checks required by the test plan when batching, repair, generated metadata, or serialization could have changed the tested candidate.
4. Confirm all required tests passed against content identical to the final commit.
5. Record the full candidate SHA.

No missing required test may be presented as green. If an explicitly user-authorized proof-limited exception overrides this SOP for one run, report the missing proof prominently; never infer that exception.

## 10. Push Only The Authorized SHA

1. Check GitHub authentication and repository access.
2. Fetch the authorized remote branch and refresh divergence immediately before push.
3. Verify the remote branch is an ancestor of the candidate or stop on divergence.
4. Count and disclose every unpublished ancestor commit included in the push.
5. Use a non-force exact-SHA push when concurrent local writers could advance the branch.
6. Read the remote branch ref back directly after the push.
7. Verify local `HEAD`, remote-tracking state, and the remote branch match the expected result, accounting honestly for any later concurrent local commit.

Never rebase, merge, pull, force-push, or rewrite history by assumption after a rejected push.

## 11. Pull Requests And Later Actions

- Create or update a pull request only when explicitly authorized.
- Verify repository, head, base, draft state, title, body, checks, and full branch diff before creation.
- Do not merge, tag, release, deploy, change repository settings, or delete branches unless the current task separately authorizes the exact action.

## 12. Closeout

Report:

- branch, upstream, final candidate SHA, and remote readback;
- every logical commit and its exact concern;
- tests run, commands or named gates, results, and what they prove;
- every behavior-neutral repair and its retest evidence;
- any unexecuted interactive, visual, build, or user-owned acceptance;
- preserved unrelated staged, unstaged, and untracked state;
- any action intentionally outside current authority.

Self-audit for accidental scope expansion, stale test evidence, active-writer races, missing Unity asset pairs, GUID/reference drift, test weakening, remote assumption, unreported dirty state, and documentation gaps.

## Hard Stop Conditions

Stop before commit or push when any of these remains unresolved:

- candidate ownership is ambiguous;
- `HEAD` or the manifest is actively changing;
- a required relevant test cannot run safely;
- compilation, focused tests, validators, or relevant build proof fail;
- the only available fix may change gameplay or serialization behavior;
- a Unity asset lacks its intended `.meta` or GUID continuity is uncertain;
- the staged manifest differs from the approved batch;
- remote lineage diverges or authentication is unavailable;
- the requested Git or GitHub action lacks current authority.
