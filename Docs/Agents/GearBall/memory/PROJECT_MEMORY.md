# Gear Ball Project Memory

This file retains durable publication knowledge for Gear Ball. It is not a live status dashboard and never replaces inspection.

## Durable Identity

- Project: Booter & BigARM.
- Canonical repo and Unity project manager: Gottspan at `Docs/Agents/Gottspan/`.
- Persistent Unity/Codex bridge manager: Babineaux at `Docs/Agents/Babineaux/`.
- Git and GitHub publication manager: Gear Ball at `Docs/Agents/GearBall/`.
- Commit batching standard: `Docs/GIT_BATCHING_STANDARD.md`.

## Durable Invariants

- Existing dirty files are user-owned until the current task or an authoritative handoff places them in scope.
- Stage exact task-owned paths; never default to whole-worktree staging in a mixed tree.
- Preserve Unity `.meta` files and GUID continuity, and verify intentional asset pairs or moves before publication.
- Do not commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or incidental Unity churn as project content.
- Keep commits small, coherent, and tied to one clear purpose.
- Commit, push, pull request, merge, tag, release, deployment, and branch/worktree actions are separate authority boundaries.
- A commit or push is Git evidence only. It does not prove Unity import, compilation, tests, interactive behavior, player builds, deployment, or release.
- After a push, compare the local commit to the remote-tracking or remote branch and report convergence honestly.

## Current Creation Note

Created 2026-08-13 at the user's direction. The repository had a mixed staged/unstaged Unity worktree and local `main` contained pre-existing unpublished commits. Those files remained outside Gear Ball's setup commit; live status and divergence must be rechecked on every future run.

## Update Discipline

- Add only facts likely to help future publication sessions.
- Link to canonical project sources instead of copying them.
- Correct or explicitly supersede stale statements.
- Never retain credentials, tokens, private data, or speculative creative decisions.
