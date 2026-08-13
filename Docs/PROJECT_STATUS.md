# Project Status

This is the shared implementation pulse for Booter & BigARM. It records what live repo evidence establishes, what still needs Unity or playtest proof, and which decisions are waiting for the user. It does not replace the strategic order in [ROADMAP.md](./ROADMAP.md).

Last reconciled: 2026-08-12 by Gottspan through the protected isometric technical spike.

## Active Program

- The user has selected a top-down, isometric-style 2.5D direction using 3D runtime assets.
- Gottspan owns the conversion program under the user's creative and product authority.
- CP-01 through CP-05 have produced a protected parallel technical spike. The program is stopped at CP-06 for user acceptance before shared-system conversion.
- The canonical program plan is [3D_CONVERSION_AUDIT_AND_CHECKLIST.md](./3D_CONVERSION_AUDIT_AND_CHECKLIST.md).
- The current evidence packet is [ConversionEvidence/PROTECTED_SPIKE_REPORT_2026-08-12.md](./ConversionEvidence/PROTECTED_SPIKE_REPORT_2026-08-12.md).
- The existing 2D prototype remains the working implementation and protected comparison baseline.

## Current Foundation

| Workstream | Repo evidence | Proof state |
| --- | --- | --- |
| Input | Gameplay, System, and UI action maps plus runtime-facing adapters exist. | Structure inspected; interactive navigation and device behavior not re-tested in this pass. |
| Movement and camera | Preserved 2D implementation plus isolated 3D Rigidbody motor, camera-basis math, fixed Cinemachine rig, and projection toggle exist. | Conversion tests and keyboard/ramp live proof pass; gamepad and user feel/lens acceptance remain. |
| World generation | World identity, settings, generator, prop catalog, and editor tooling exist. | Structural evidence only; determinism and streaming behavior need focused validation. |
| Save/load | Versioned schema, DTOs, service, controller, player/survival/inventory/BigARM state exist. | Structural evidence only; round-trip and migration behavior need automated tests. |
| Survival economy | Survival state, inventory, harvesting, pickups, item definitions, and dust canister exist. | Prototype implementation exists; balance and loop quality need playtests. |
| BigARM | Command adapter, threat signal, AI controller, save data, and capability documentation exist. | Prototype implementation exists; companion behavior and recovery-loop value need playtests. |
| Tooling | Build CLI, prototype bootstrap/repair, protected lab builder, non-mutating conversion validator, and focused EditMode suite exist. | Conversion baseline validation passes; 8 focused tests pass. The lab intentionally remains outside Build Settings. |

## Management Gates

- Review the CP-06 evidence and choose proceed, revise, or stop before CP-07 shared runtime seams.
- Keep the implemented spike bounded to its parallel renderer/scene/motor/camera/interaction/BigARM experiment until that decision.
- Do not install navigation or asset-format packages, source production assets, alter Build Settings, or retire legacy content merely because the conversion plan exists.
- Treat current architecture and gameplay systems as prototype seams, not finished design.
- Reconcile roadmap wording against implementation before selecting a new large feature; implemented code does not mean a phase's exit criteria are met.
- Establish a non-mutating Unity validation entry point and focused tests when the next implementation task needs reliable regression proof.
- Use [PLAYTEST_LOG.md](./PLAYTEST_LOG.md) to test movement feel, survival pressure, world readability, and BigARM's value before expanding breadth.
- Keep active dirty-file ownership in the current task brief or handoff, not in this durable status page.

## Next Decisions For The User

The immediate CP-06 decision is whether to proceed, revise, or stop. If proceeding, record working choices for:

- Orthographic versus mild-perspective isometric framing, fixed camera angle/zoom, and whether rotation is ever allowed.
- Elevation scope and the first-slice facing/aiming model.
- Visual style family and eventual asset-sourcing approach.
- Supported input schemes for the conversion slice.
- Target platform and performance floor before representative production assets and optimization.

## Update Rule

Update this page when a workstream crosses a meaningful evidence boundary: absent to implemented, implemented to automatically verified, or verified to playtested. Link consequential decisions in [DECISION_LOG.md](./DECISION_LOG.md).
