# Gear Ball Runtime Load Policy

Gear Ball should load enough context to publish safely without absorbing the entire project or mistaking old evidence for current Git state.

## Always Load

- Root `AGENTS.md`.
- Gear Ball's `README.md`.
- `memory/PROJECT_MEMORY.md`.
- `Docs/GIT_BATCHING_STANDARD.md`.
- The current user request and its exact authority.
- Live branch, upstream, remotes, worktrees, status, staged and unstaged diffs, and recent relevant history.
- Gottspan's `README.md` when publication follows repo-wide integration or the worktree is mixed.

## Load By Lane

- Commit-only work: exact task manifest, owner handoff, relevant checks, staged diff, and commit history for message consistency.
- Push work: commit-only context plus remote URL, authentication, upstream, divergence, and remote default branch.
- Pull-request work: push context plus target repository/base branch, full branch diff, existing PR state, required checks, and review instructions.
- Branch or worktree work: all linked-worktree state, active branches, uncommitted ownership, upstream topology, and explicit current authority.
- Conflict, rebase, revert, or recovery work: controlling owner handoff, exact commit graph, reflog as needed, recovery plan, and explicit destructive/history authority before mutation.
- Unity asset publication: paired `.meta` files, rename/reference intent, and the validation handoff; Gear Ball validates the batch boundary, not the gameplay.

## Authority Rules

- Commit authority does not imply push authority.
- Push authority does not imply pull-request, merge, tag, release, deployment, or branch/worktree authority.
- A request to publish known task-owned work does not make every dirty or already-staged file task-owned.
- If a push would publish pre-existing local commits, count and inspect them and disclose that scope before the push.
- Never use destructive cleanup, history rewrite, or force push without explicit target-specific authority.

## Memory Rules

- Live Git and GitHub inspection outranks memory for mutable facts.
- Store durable role rules, recurring hazards, stable paths, and superseding decisions—not live status snapshots presented as current fact.
- Date mutable facts and name their verification source when retaining them is useful.
- Never store credentials, tokens, private identifiers, full transcripts, or generated logs.
