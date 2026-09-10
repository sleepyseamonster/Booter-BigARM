# Session Start and Handoff

At startup read `Engine/AGENTS.md`, [direction](../Docs/DIRECTION.md), [status](../Docs/STATUS.md) and the current bounded plan. Inspect Git state; treat unrelated changes as owned by the user or another task. Read only the research/source slice needed next.

For implementation, use the [master roadmap](../Docs/FOUNDATION_PLAN.md) and run `python3 Tools/check_engine_plan.py`. Select the next coherent ready package within current task authority. Manage routine technical decisions directly; do not ask the user to rediscover engine requirements or select every subsystem. Native Windows gates and product/creative/external-action boundaries remain explicit.

Before editing identify the result, allowed paths, source of truth and relevant proof. Work inside `Engine/`. Access historical game/lore material read-only when needed; do not restart the Unity workstream.

Before closing:

1. Run focused checks for the exact changed content. Documentation uses the workspace checker; tool behavior uses its focused tests; engine work uses the applicable native build/tests.
2. Update status with completed work, evidence paths, failed attempts and limitations. Update decisions only when evidence or user direction changes them.
3. Update completed package evidence in [the roadmap index](../Docs/ENGINE_ROADMAP.json), identify the next ready package and any genuinely required product/platform input. Continue safe ready work within the task's scope rather than adding a confirmation checkpoint for each technical step. Do not make the user reconstruct command sequences from conversation history.
4. Stage the exact task-owned manifest, inspect it and commit verified work. Preserve other dirty files; pushing or other external publication needs current authority.
5. Report outcome, commit, checks and material proof gaps. Leave no running experiment or delegate without an explicit reason.

The status file is the current handoff. Detailed evidence remains in receipts and audits; do not duplicate the entire research brief into every update.
