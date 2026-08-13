# Git And GitHub Publication SOP

This is Gear Ball's default procedure for turning approved repository work into exact Git history and authorized GitHub state.

## 1. Establish The Publication Contract

Before mutating Git, identify:

- **Done** — commit, push, pull request, or another explicitly requested result.
- **Scope** — exact task-owned files and commits.
- **Source** — user instruction, Gottspan handoff, and implementation-owner evidence.
- **Proof** — relevant validation plus Git diff and convergence checks.
- **Out** — unrelated dirty files and external actions not authorized.
- **Stop** — ambiguous ownership, failed validation, remote divergence, or authority gaps.

Do not infer push from commit, pull request from push, merge from approval, or release from merge.

## 2. Inspect Before Git Writes

1. Confirm the repository root and applicable instructions.
2. Inspect current branch, upstream, remotes, linked worktrees, concise status, and full untracked-file status.
3. Inspect staged and unstaged diffs separately.
4. Compare local and remote history before a push; fetch when a current remote comparison is required and safe.
5. Check GitHub authentication and repository access for external operations.
6. Classify every candidate path as task-owned, unrelated, generated noise, or unresolved.

Stop before staging if any candidate path is unresolved. Do not clean, reset, restore, stash, or unstage someone else's work to manufacture a clean tree.

## 3. Validate The Candidate

1. Require the implementation owner's relevant test or validation handoff.
2. Run only publication-level checks that are safe and relevant, such as whitespace checks, path/reference checks, or documentation links.
3. Do not run Unity smoke tests unless the user explicitly requests them.
4. Treat failed or missing behavior validation as an owner handoff issue, not permission for Gear Ball to patch implementation.

## 4. Stage Exactly

1. Stage explicit task-owned paths. Avoid `git add -A` in a mixed worktree.
2. Capture and review the staged name-status and full staged diff.
3. Verify the staged path set exactly matches the approved manifest.
4. Recheck concise status so unrelated staged or unstaged work remains visible.

If the index already contains unrelated work, do not silently unstage it or include it. Stop and obtain an exact integration decision unless the task can safely commit by an explicit path-only method that preserves the existing index.

## 5. Commit Intentionally

1. Use a terse message describing the single concern.
2. Confirm the resulting commit contains only the approved manifest.
3. Run the narrow post-commit checks needed to catch accidental omissions.
4. Record the full commit SHA and remaining worktree state.

## 6. Push Only With Authority

1. Refresh remote divergence immediately before pushing.
2. Disclose pre-existing unpublished commits that the push will also publish.
3. Push the exact authorized branch without force.
4. Verify the remote branch resolves to the expected commit.
5. Report rejected pushes or new remote divergence without rebasing, merging, or force-pushing by assumption.

## 7. Pull Requests And Later Actions

- Create or update a pull request only when explicitly authorized.
- Verify repository, head, base, draft state, title, body, and full branch diff before creation.
- Do not merge, tag, release, deploy, change repository settings, or delete branches unless the current task separately authorizes the exact action.

## 8. Closeout

Report:

- branch and upstream;
- commit SHA and message;
- exact files included;
- checks run and what they prove;
- push target and convergence evidence;
- preserved unrelated staged, unstaged, and untracked state;
- any requested result blocked by ownership, validation, divergence, or authority.

Self-audit the final state for accidental scope expansion, stale remote assumptions, missing Unity asset pairs, unreported dirty state, and documentation gaps.
