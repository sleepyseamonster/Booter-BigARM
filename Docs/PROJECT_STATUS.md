# Project Status

This is the shared implementation pulse for Booter & BigARM. It records what live repo evidence establishes, what still needs Unity or playtest proof, and which decisions are waiting for the user. It does not replace the strategic order in [ROADMAP.md](./ROADMAP.md).

Last reconciled: 2026-08-13 by Gottspan through the perspective top-down 3D foundation.

## Active Program

- The user has revised the presentation direction to a perspective, elevated top-down game with a fully 3D runtime world and assets.
- Gottspan owns the conversion program under the user's creative and product authority.
- The first perspective foundation is implemented in a separate generated development scene. It is deliberately outside Build Settings and does not cut over the game.
- The active plan is [TOP_DOWN_3D_FOUNDATION_PLAN.md](./TOP_DOWN_3D_FOUNDATION_PLAN.md).
- The current evidence packet is [ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md](./ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md).
- The former isometric lab and its audit remain historical comparison evidence, not the camera direction for new work.
- The existing 2D prototype remains the working implementation and protected comparison baseline.

## Current Foundation

| Workstream | Repo evidence | Proof state |
| --- | --- | --- |
| Input | A single `TopDown3DInputRouter` owns Gameplay input in the new scene. Existing bindings provide keyboard/gamepad movement, sprint, and BigARM recall with radial movement deadzone handling. | Binding structure is automatically verified. Physical-controller and hands-on response are user-owned acceptance. |
| Movement and camera | An isolated 3D Rigidbody motor and fixed perspective camera rig provide camera-relative XZ movement, acceleration, sprint, facing, slope grounding, damped follow, and obstruction pull-in. | Compilation, scene validation, movement-basis tests, and visual rendering pass. User feel/tuning remains. |
| World generation | Seeded height sampling, seam-matched chunk meshes/colliders, deterministic prop placement, and radius-based load/unload now exist. | Determinism and adjacent-seam tests pass; the generated scene loads the expected 25 chunks. Extended traversal acceptance remains user-owned. |
| Save/load | The existing versioned 2D prototype save systems remain preserved. | Perspective-world persistence and migration were explicitly deferred. |
| Survival economy | Existing prototype systems remain preserved. | Harvesting, items, balance, and loop redesign were explicitly deferred. |
| BigARM | The perspective foundation has a smaller 1.6 by 1.9 collider footprint with idle, follow, avoidance, stuck-recovery, and recall states. | Structure and compact footprint are validated; hands-on behavior acceptance remains user-owned. |
| Tooling | A guarded generated-scene builder, open command, non-mutating validator, isolated runtime assembly, and focused EditMode suite exist. | Perspective validation passes; all 13 EditMode tests pass. No smoke-test suite is retained. |

## Management Gates

- Keep the perspective foundation bounded to its separate scene and runtime assembly until the user accepts camera, movement, controller, terrain, and companion feel.
- Do not install navigation or asset-format packages, source production assets, alter Build Settings, or retire legacy content merely because the conversion plan exists.
- Treat the current camera values, generated terrain, greybox visuals, and BigARM states as tuning-ready foundations rather than finished design.
- Reconcile roadmap wording against implementation before selecting a new large feature; implemented code does not mean a phase's exit criteria are met.
- Use [PLAYTEST_LOG.md](./PLAYTEST_LOG.md) for the user's hands-on observations before expanding breadth. Codex does not create or run smoke tests unless the user explicitly requests them.
- Keep active dirty-file ownership in the current task brief or handoff, not in this durable status page.

## Next Decisions For The User

The next acceptance pass belongs to the user and should answer:

- whether the 50-degree pitch, 40-degree yaw, 48-degree field of view, and 16-unit distance have the intended top-down feel;
- whether keyboard and a physical gamepad produce comfortable walk, sprint, and recall behavior;
- whether terrain relief, chunk traversal, camera obstruction, and Booter grounding remain readable in motion;
- whether BigARM is now small enough and whether its follow spacing, avoidance, recovery, and recall feel intelligent enough for this foundation;
- which one of those foundation areas should be tuned first before deferred mechanics or production assets resume.

## Update Rule

Update this page when a workstream crosses a meaningful evidence boundary: absent to implemented, implemented to automatically verified, or verified to playtested. Link consequential decisions in [DECISION_LOG.md](./DECISION_LOG.md).
