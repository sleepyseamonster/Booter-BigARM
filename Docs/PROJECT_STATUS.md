# Project Status

This is the shared implementation pulse for Booter & BigARM. It records what live repo evidence establishes, what still needs Unity or playtest proof, and which decisions are waiting for the user. It does not replace the strategic order in [ROADMAP.md](./ROADMAP.md).

Last reconciled: 2026-08-12 by Gottspan through the isometric conversion planning audit.

## Active Program

- The user has selected a top-down, isometric-style 2.5D direction using 3D runtime assets.
- Gottspan owns the conversion program under the user's creative and product authority.
- The current state is **planning only**: the master audit, migration matrix, risks, gates, and work breakdown exist, but no Unity conversion has begun.
- The canonical program plan is [3D_CONVERSION_AUDIT_AND_CHECKLIST.md](./3D_CONVERSION_AUDIT_AND_CHECKLIST.md).
- The existing 2D prototype remains the working implementation and protected comparison baseline.

## Current Foundation

| Workstream | Repo evidence | Proof state |
| --- | --- | --- |
| Input | Gameplay, System, and UI action maps plus runtime-facing adapters exist. | Structure inspected; interactive navigation and device behavior not re-tested in this pass. |
| Movement and camera | Physics-backed player motor and dedicated camera target/controller exist. | Code and scene surfaces exist; feel requires playtest evidence. |
| World generation | World identity, settings, generator, prop catalog, and editor tooling exist. | Structural evidence only; determinism and streaming behavior need focused validation. |
| Save/load | Versioned schema, DTOs, service, controller, player/survival/inventory/BigARM state exist. | Structural evidence only; round-trip and migration behavior need automated tests. |
| Survival economy | Survival state, inventory, harvesting, pickups, item definitions, and dust canister exist. | Prototype implementation exists; balance and loop quality need playtests. |
| BigARM | Command adapter, threat signal, AI controller, save data, and capability documentation exist. | Prototype implementation exists; companion behavior and recovery-loop value need playtests. |
| Tooling | Build CLI plus prototype scene bootstrap/repair commands exist. | Build command is documented; no dedicated non-mutating validator or committed tests yet. |

## Management Gates

- Resolve only the Gate 0 choices needed for the protected technical spike before any Unity implementation.
- Keep the first implementation bounded to a parallel renderer/scene/motor/camera/interaction/BigARM scale experiment and stop for a go/no-go review.
- Do not install navigation or asset-format packages, source production assets, alter Build Settings, or retire legacy content merely because the conversion plan exists.
- Treat current architecture and gameplay systems as prototype seams, not finished design.
- Reconcile roadmap wording against implementation before selecting a new large feature; implemented code does not mean a phase's exit criteria are met.
- Establish a non-mutating Unity validation entry point and focused tests when the next implementation task needs reliable regression proof.
- Use [PLAYTEST_LOG.md](./PLAYTEST_LOG.md) to test movement feel, survival pressure, world readability, and BigARM's value before expanding breadth.
- Keep active dirty-file ownership in the current task brief or handoff, not in this durable status page.

## Next Decisions For The User

Gottspan will frame these through CP-01 rather than requiring all production decisions at once:

- Orthographic versus mild-perspective isometric framing, fixed camera angle/zoom, and whether rotation is ever allowed.
- Elevation scope and the first-slice facing/aiming model.
- Visual style family and eventual asset-sourcing approach.
- Supported input schemes for the conversion slice.
- Target platform and performance floor before representative production assets and optimization.

## Update Rule

Update this page when a workstream crosses a meaningful evidence boundary: absent to implemented, implemented to automatically verified, or verified to playtested. Link consequential decisions in [DECISION_LOG.md](./DECISION_LOG.md).
