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

## 2026-08-12 — Isometric 2.5D Conversion Program

- **Status:** accepted
- **Decision owner:** user
- **Question:** What presentation direction should Booter & BigARM take, and who should own the conversion program?
- **Decision:** Work toward a top-down, isometric-style 2.5D game using 3D characters, environments, props, lighting, collision, and effects. Keep the existing Unity project and preserve the current 2D prototype as a protected reference during a parallel conversion lane. Gottspan owns planning, task design, architecture coordination, integration order, validation, and evidence for the conversion; the user retains creative, product, spending, destructive, release, and final-acceptance authority.
- **Why:** The user explicitly selected the isometric 2.5D direction, asked Gottspan to take ownership, and requested a complete audit and conversion plan. The repository audit shows substantial reusable game-state code but central 2D coupling in movement, world generation, BigARM locomotion, scenes, renderer setup, prefabs, and assets.
- **Evidence:** Live inspection of runtime/editor code, both enabled scenes, URP and quality assets, packages, input actions, physics/navigation settings, prefabs, art inventory, tests, build automation, and controlling docs.
- **Controlling files updated:** `Docs/3D_CONVERSION_AUDIT_AND_CHECKLIST.md`, `Docs/ROADMAP.md`, `Docs/PROJECT_STATUS.md`, `Docs/DOCS_INDEX.md`, `Docs/DECISION_LOG.md`, and the bounded Gottspan/Babineaux project-memory routing notes.
- **Consequences / follow-up:** Execute the gated conversion program without in-place scene or asset replacement. No Unity implementation is authorized by the planning pass. Final camera, elevation, aiming, art, platform, sourcing, package, cutover, and cleanup decisions remain gated as documented.
- **Supersedes:** none; current 2D implementation standards remain active for the preserved legacy path until individually superseded by evidence and the final cutover.
