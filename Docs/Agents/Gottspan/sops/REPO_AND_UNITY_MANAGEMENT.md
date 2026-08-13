# Repo And Unity Management SOP

This is Gottspan's default operating procedure for work in Booter & BigARM.

## 1. Establish Control

1. Confirm the repo root and read the applicable `AGENTS.md` files.
2. Inspect branch, upstream, status, recent history, and task-relevant diffs.
3. Classify existing dirty files as user-owned unless the task explicitly says otherwise.
4. Load the minimum task context from the runtime load policy.
5. Verify mutable Unity facts from `ProjectVersion.txt`, package manifests, build settings, and current assets rather than memory.

Stop if the requested operation would overwrite unrelated work, requires missing authority, or depends on an unresolved source-of-truth conflict.

## 2. Define The Work Contract

Before editing, state:

- **Done** — the observable result required.
- **Scope** — allowed systems and paths.
- **Out** — adjacent work intentionally excluded.
- **Source** — the controlling design, code, asset, or setting.
- **Proof** — the smallest check that establishes the result.
- **Stop** — conditions that require user direction.

For large work, sequence checkpoints. For multi-agent work, create disjoint briefs and retain final integration locally.

## 3. Protect Unity State

- Treat an asset and its `.meta` file as one unit.
- Prefer Unity GUI moves for referenced assets. If a filesystem move is necessary, preserve the `.meta` file and verify GUID references.
- Do not hand-edit scenes, prefabs, or ScriptableObjects unless text serialization is understood and the edit is tightly reviewable.
- Do not use scene bootstrap or repair commands as harmless validators; they write project content.
- Keep runtime/editor assembly boundaries intact.
- Do not launch a second Unity process against a project already open in the editor.
- Preserve the user's foreground application. Do not activate, raise, focus, or send keystrokes to Unity unless the user explicitly requests visible interactive Unity work in the current task.
- Ordinary repo work and validation must not depend on the user focusing Unity. Prefer background-safe launch and command-line/editor automation, then hand off only the genuinely interactive remainder.
- Remember that batchmode can import and serialize assets. Inspect status before and after it runs.

## 4. Implement And Integrate

1. Make the smallest coherent change at the canonical owner.
2. Keep delegates inside assigned files and authority.
3. Inspect delegate output before accepting it.
4. Update docs when commands, ownership, structure, canon, or actual baselines change.
5. Avoid duplicate trackers: `Docs/ROADMAP.md` owns roadmap sequencing; `Docs/WORLD_BASIS.md` owns world canon; Gottspan memory stores only durable coordination facts.
6. Keep `Docs/PROJECT_STATUS.md` focused on current evidence, blockers, and decisions—not a second roadmap.
7. Record consequential rationale in `Docs/DECISION_LOG.md` and player-facing observations in `Docs/PLAYTEST_LOG.md`.

## 5. Validation Ladder

Choose the lowest level that proves the change. Higher levels do not erase the need for lower-level diff review.

1. **Structural** — run `tools/repo-health.sh`, `git diff --check`, focused searches, and reference checks.
2. **Compile/import** — run Unity batchmode only when the editor is closed and import-side mutations are acceptable.
3. **Focused tests** — run relevant EditMode or PlayMode tests when test assemblies exist.
4. **Interactive Unity** — inspect scene, prefab, animation, input, physics, and visual behavior in the GUI.
5. **Build** — build the active target from enabled scenes for build-pipeline or release-relevant work.

Record exactly what each check proves. A shell check cannot prove scene feel; a compile does not prove saved references; a build does not prove game design quality.

## 6. Closeout And Git

1. Re-read the request and audit for missing scope, regressions, stale references, and doc drift.
2. Inspect `git status`, the task diff, and staged diff separately.
3. Do not stage user-owned changes.
4. Commit a coherent verified batch when required by `AGENTS.md`.
5. Report changed files, checks, unverified behavior, preserved unrelated state, commit identifier, and only relevant next steps.

## Recovery Rule

If an operation is interrupted or creates unexpected changes, stop. Inspect status and diffs before any cleanup. Do not use broad reset, checkout, clean, stash, or asset regeneration as automatic recovery. Preserve evidence and ask for authority when recovery could discard work.
