# Unity And Codex Bridge SOP

This is Babineaux's default procedure for moving an approved request safely between conversation, repository work, and the Unity Editor.

## 1. Translate The Request

State the work contract before editing:

- **Done** — the player-, editor-, or repository-visible outcome.
- **Scope** — the exact systems and paths allowed to change.
- **Out** — adjacent work intentionally excluded.
- **Source** — the controlling design document, asset, script, setting, or current user decision.
- **Proof** — the smallest checks that establish the result.
- **Stop** — conflicts or risks that require user or Gottspan direction.

Do not convert provisional creative discussion into implementation or canon without approval.

## 2. Establish Unity And Repo State

1. Confirm the repo root and applicable instructions.
2. Inspect branch, upstream, status, task-relevant diffs, and recent task history.
3. Treat unrelated dirty files as user-owned.
4. Verify the installed/project Unity version and relevant packages/settings when they matter.
5. Check whether Unity is open before considering batchmode.
6. Identify every serialized asset and `.meta` file the task could affect.

Stop if the work could overwrite unrelated state, break GUID references, start a second editor, or requires authority the task did not grant.

## 3. Choose The Control Surface

- Use repository edits for focused C#, documentation, tests, and text-safe configuration changes.
- Use the Unity GUI for scene composition, prefab wiring, Inspector changes, animation, physics/input feel, and visual review.
- Use documented command-line automation for repeatable imports, validation, tests, and builds.
- Preserve the user's foreground application during normal work. Background-safe launch and automation are the default; activate or drive Unity only after an explicit current request for visible interactive work.
- Do not turn user-provided Unity focus into a routine prerequisite. If a result genuinely requires hands-on interaction, finish everything else first and provide one bounded handoff.
- Add reusable Unity Editor automation under `Assets/_Project/Scripts/Editor/`; record its purpose and entry point in Babineaux's tool inventory.
- Keep agent-local read-only helpers under `Docs/Agents/Babineaux/tools/`.

If a workflow crosses surfaces, define the handoff explicitly: what Codex changed, what the user should open or click in Unity, and what evidence should come back.

## 4. Implement Narrowly

1. Change the canonical owner instead of adding a parallel workaround.
2. Preserve assembly, namespace, serialization, GUID, and reference boundaries.
3. Keep scenes and reusable prefabs purpose-built.
4. Avoid broad settings or package changes unless they are the approved task.
5. Reinspect Git status after any Unity import, batchmode, or GUI save because incidental serialization may appear.

## 5. Validate By Evidence Level

Use only the levels relevant to the requested outcome and report what remains unproved:

1. **Structural** — diff review, `git diff --check`, paths, GUID/reference searches, and focused static checks.
2. **Compile/import** — Unity imports and compiles in the intended editor version.
3. **Focused tests** — relevant EditMode or PlayMode checks pass.
4. **Interactive** — the exact scene/prefab behavior, input feel, physics, animation, and visuals are inspected in the GUI.
5. **Build** — the intended target builds from the enabled scenes.

A higher level does not erase the need to review the files it changed.

## 6. Close The Bridge

1. Re-read the request and self-audit for missed edges, regressions, serialization risk, and documentation drift.
2. Separate task-owned changes from unrelated workspace state.
3. Commit only verified task-owned work when root instructions require it.
4. Report what changed, what each check proved, what remains for interactive Unity verification, preserved unrelated state, and the commit identifier when applicable.
5. Route repo-wide integration or ownership conflicts to Gottspan.

## Recovery Rule

If Unity or an automation command creates unexpected changes, stop and inspect before cleanup. Do not reset, clean, regenerate, delete `.meta` files, move assets, or re-run a repair tool by reflex. Preserve evidence and request direction if recovery could discard work.
