# Git Batching Standard

This project is still early, so commits should stay small, intentional, and easy to review.

## Goals

- Keep each commit tied to one clear purpose.
- Separate repo instructions, tooling, settings, and gameplay work into different batches when practical.
- Ignore Unity-generated noise unless it is part of a deliberate asset or project-setting change.
- Preserve flexibility by avoiding broad mixed-scope commits.

## Batch Rules

- Batch by concern, not by whatever happened to change at the same time.
- Commit docs, automation, and project structure separately when they can stand alone.
- Commit runtime gameplay systems separately from editor tooling and project settings.
- Do not merge cleanup work with design changes unless they are inseparable.
- Keep `.meta` files only when they correspond to intentional Unity asset work.

## Unity Noise Policy

- Do not commit `Library/`, `Temp/`, `Logs/`, or `UserSettings/`.
- Do not commit incidental editor churn unless it is required for the task.
- If Unity regenerates files that are not part of the task, leave them out unless they are needed to preserve references or project configuration.

## Verification Before Commit

- Check the diff for accidental scope creep.
- Run only checks relevant to the change.
- Self-audit for regressions, stale references, and documentation gaps.
- Commit only when the batch is coherent and not fundamentally broken.

