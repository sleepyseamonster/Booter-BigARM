# Gottspan Management Decisions

This log records durable repo-management decisions. World and gameplay decisions belong in their canonical design documents.

## 2026-08-12 — Gottspan Is The Canonical Manager

- **Decision:** `Docs/Agents/Gottspan/` is the canonical home for repo-management instructions, SOPs, bounded memory, templates, and Gottspan-specific tools.
- **Reason:** Repo-level ownership needs one discoverable operating surface without mixing agent records into Unity-imported `Assets/`.
- **Effect:** Root `AGENTS.md` remains the top-level contract; Gottspan maintains and routes it.

## 2026-08-12 — Temporary Specialist Seats

- **Decision:** Multi-agent work uses temporary Systems, World And Design, Content, and Validation seats rather than creating permanent agent folders for every task.
- **Reason:** Stable responsibilities are useful, while permanent personalities and duplicate memories would add drift before the team shape is proven.
- **Effect:** Every active delegate requires a bounded brief and evidence-first handoff. Gottspan remains final integrator.

## 2026-08-13 — Unity Must Not Steal Foreground Focus

- **Decision:** Normal repo work, automation, validation, and Unity launch preserve the user's foreground application. Activating Unity or driving it with foreground keystrokes requires an explicit current request for visible interactive Unity work.
- **Reason:** The user should not need to refocus Unity every time agents work, and ordinary automation should not interrupt unrelated desktop activity.
- **Effect:** Background-safe control paths are the default. Gottspan and Babineaux must not make routine progress depend on user-provided Unity focus.

## Supersession Rule

When a management decision changes, add a new dated entry that names the superseded decision and update every controlling instruction in the same batch.
