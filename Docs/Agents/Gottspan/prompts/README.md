# Gottspan Reusable Prompt Catalog

This is the canonical inventory of reusable prompts for Booter & BigARM. Gottspan owns its routing, accuracy, and maintenance. It lists reusable operating prompts only; it does not replace feature plans, task briefs, decisions, or user authority.

When the user asks what prompts are available, which prompt fits a job, or asks Gottspan to relay a prompt, Gottspan reads this catalog first and reports the prompt's purpose, intended sequence, current path, and any authority boundary. A prompt is guidance, not standing permission to edit, publish, or change product direction.

## Current Library

| Prompt | Use it when | Sequence and boundary | Owner | Source |
| --- | --- | --- | --- | --- |
| Implementation-Ready Planning Prompt | A feature, repair, migration, system, tool, or content slice needs a repository-grounded plan before work starts. | First stage. Produces an evidence-backed plan; it is planning-only and does not authorize implementation. | Gottspan | [`IMPLEMENTATION_READY_PLAN_PROMPT.md`](./IMPLEMENTATION_READY_PLAN_PROMPT.md) |
| Approved Plan Buildout Prompt | An approved implementation-ready plan needs to be executed in validated batches. | Second stage. Requires the exact approved plan source, revalidates in Batch 0, and stops at scope, proof, owner, or authority boundaries. | Gottspan, with specialist lanes as named by the plan | [`IMPLEMENTATION_PLAN_BUILDOUT_PROMPT.md`](./IMPLEMENTATION_PLAN_BUILDOUT_PROMPT.md) |

## Catalog Maintenance Contract

- Add an entry whenever a new reusable prompt is accepted into this folder; update the entry in the same change when its purpose, sequence, or authority boundary changes.
- Keep the catalog title, purpose, use condition, owner, and path factual and concise. Do not duplicate the full prompt here.
- Keep prompts in their own Markdown files under this folder. Plans, task-specific handoffs, runbooks, and generated reports belong in their canonical locations rather than this catalog.
- Before relaying a prompt, reopen its linked source and report its current filename/path. If the catalog and source disagree, treat the source file and current repository instructions as authoritative, repair the catalog only with current task authority, and disclose the drift.
- Retire a prompt only with explicit user or controlling-document authority. Mark a replacement relationship here so no duplicate workflow authority survives.

## Requesting A Prompt

Ask Gottspan for the catalog, a prompt by name, or guidance on which prompt fits the current task. Gottspan will return the current source-backed prompt and state whether it is a planning, implementation, Unity bridge, narrative, or Git/publication lane.
