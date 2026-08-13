# Multi-Agent Workflow

Gottspan is the primary coordinator and final integrator. Specialist agents are temporary seats chosen for a task, not competing project managers.

## Specialist Seats

- **Systems** — runtime gameplay code, architecture seams, save/load, input, procgen, and editor tooling.
- **World And Design** — world canon, loop design, UX intent, content rules, and roadmap alignment.
- **Content** — scenes, prefabs, art, animation, audio, UI assets, import settings, and reference-safe Unity asset work.
- **Validation** — focused tests, build/compile evidence, serialized-reference review, documentation drift, and regression audit.

One agent may cover more than one seat for a small task. A seat becomes active only through a written brief.

## Delegation Gate

Delegate when the subtask is bounded, independently useful, and can proceed without overlapping edits. Good parallel work includes read-only audits, research against separate sources, isolated test design, or changes in disjoint files.

Do not delegate merely to multiply activity. Keep integration-heavy changes local when several agents would need the same scene, prefab, project setting, or source file.

## Required Brief

Every delegate receives:

- Objective and definition of done.
- In-scope and out-of-scope paths.
- Authority: read-only, edit, test, or another explicit level.
- Assigned files or a statement that no files may be edited.
- Source-of-truth documents.
- Required proof.
- Stop conditions and known dirty/user-owned files.

Use [`../templates/TASK_BRIEF.md`](../templates/TASK_BRIEF.md).

## Shared-Worktree Rules

- Assume all agents see and can affect the same filesystem immediately.
- Gottspan records ownership before edits begin.
- No overlapping file ownership. Coupled Unity assets and their `.meta` files count as one ownership unit.
- Delegates do not branch-switch, stash, stage, commit, push, reimport the whole project, or run broad formatters unless explicitly authorized.
- If unexpected changes appear in an assigned file, stop editing it and report the collision.
- Read-only agents may inspect the same files, but must not run commands that can trigger Unity imports or mutate project state.

## Handoff And Integration

Delegates return a handoff using [`../templates/HANDOFF.md`](../templates/HANDOFF.md). Gottspan then:

1. Confirms the handoff matches the assigned scope.
2. Inspects the actual diff and current repo state.
3. Reconciles conflicting findings against the source-of-truth order.
4. Runs integration-level checks proportional to risk.
5. Updates durable docs or memory only when a lasting fact or decision changed.
6. Stages only task-owned files and commits only a coherent verified batch.

Agent statements are leads; the repo, tool output, and test artifacts are evidence.
