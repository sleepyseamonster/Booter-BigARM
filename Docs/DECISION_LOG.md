# Decision Log

This log preserves the rationale and evidence behind consequential project decisions. It is an index, not a substitute for current truth: every accepted decision must also update its controlling canon, standard, roadmap, code, or project setting.

## Entry Template

### YYYY-MM-DD — Decision Title

- **Status:** proposed / accepted / superseded
- **Decision owner:** user / named owner
- **Question:**
- **Decision:**
- **Why:**
- **Evidence:**
- **Controlling files updated:**
- **Consequences / follow-up:**
- **Supersedes:** none / link to prior entry

## 2026-08-12 — Repo And Unity Project Management Model

- **Status:** accepted
- **Decision owner:** user
- **Question:** How should repo ownership and multi-agent development be managed?
- **Decision:** Gottspan is the canonical repo and Unity project manager. Specialists are temporary task-scoped seats; Gottspan owns briefing, integration, evidence, and maintenance of the top-level agent contract.
- **Why:** The user explicitly assigned Gottspan ownership and requested a durable multi-agent game-development environment.
- **Evidence:** The current task and the read-only repo, Unity, and game-design workflow audits.
- **Controlling files updated:** `AGENTS.md`, `Docs/Agents/Gottspan/`, `Docs/DOCS_INDEX.md`.
- **Consequences / follow-up:** Use bounded briefs, disjoint file ownership, single-writer Unity operations, evidence-first handoffs, and narrow integration commits.
- **Supersedes:** none
