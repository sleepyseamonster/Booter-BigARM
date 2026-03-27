# Codex Editor Standard

This is the compact standard for how Codex should operate in this Unity repo and how it should interact with the editor.

## Core Rules

- Keep tasks small, scoped, and verifiable.
- Prefer work that is about an hour or a few hundred lines at a time.
- For larger changes, start with an implementation plan before editing.
- Write prompts like a GitHub issue: include file paths, component names, expected behavior, and relevant snippets.
- Use `AGENTS.md` for persistent repo context that should survive across prompts.

These rules follow OpenAI's published Codex guidance:
- [How OpenAI uses Codex](https://openai.com/business/guides-and-resources/how-openai-uses-codex/)
- [Introducing Codex](https://openai.com/index/introducing-codex/)

## Editor Workflow

- Use the Unity editor GUI for visual iteration, scene work, and Inspector-driven changes.
- Use the command line for repeatable imports, validation, builds, and tests.
- Keep editor automation in `Assets/_Project/Scripts/Editor/` behind an Editor-only asmdef.
- Keep the build/test feedback loop deterministic and short.

## Delegation Rule

- Use subagents for bounded research, file discovery, or parallel checks.
- Keep high-risk design calls, ambiguous refactors, and final judgment in the main workflow.
- Do not delegate a task that is blocking the immediate next step unless it is actually separable.

## Verification Rule

- After finishing a task, self-audit the result for gaps, regressions, and missing documentation.
- Run only the tests or checks directly relevant to the change.
- Commit only when the work is in a good state and not fundamentally broken.
- If the task adds a new repeated workflow, document the exact command or entry point in the repo.

## Practical Prompt Template

When starting a Codex task, prefer prompts that include:

- the exact goal
- the relevant files or folders
- the desired output
- the constraints that matter
- the verification you want

Example shape:

```text
Update Assets/_Project/Scripts/Editor/BuildAutomation.cs so builds accept an explicit target and remain batch-mode safe. Update Docs/UNITY_AUTOMATION.md to match. Verify with a targeted diff check.
```

## Good Defaults

- One topic per task.
- One repo doc only when it changes behavior or structure.
- Small follow-up tasks instead of large ambiguous rewrites.
- Prefer durable standards over temporary notes.

