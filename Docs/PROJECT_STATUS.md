# Project Status

This is the shared implementation pulse for Booter & BigARM. It records what live repo evidence establishes, what still needs Unity or playtest proof, and which decisions are waiting for the user. It does not replace the strategic order in [ROADMAP.md](./ROADMAP.md).

Last reconciled: 2026-08-13 by Gottspan through the traversal-hardening and right-stick camera pass.

## Active Program

- The user has revised the presentation direction to a perspective, elevated top-down game with a fully 3D runtime world and assets.
- Gottspan owns the conversion program under the user's creative and product authority.
- The first perspective foundation was accepted by the user and has received a second traversal-hardening pass. It remains in a separate generated development scene, deliberately outside Build Settings, and does not cut over the game.
- The active plan is [TOP_DOWN_3D_FOUNDATION_PLAN.md](./TOP_DOWN_3D_FOUNDATION_PLAN.md).
- The current evidence packet is [ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md](./ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md).
- The former isometric lab and its audit remain historical comparison evidence, not the camera direction for new work.
- The existing 2D prototype remains the working implementation and protected comparison baseline.

## Current Foundation

| Workstream | Repo evidence | Proof state |
| --- | --- | --- |
| Input | A single `TopDown3DInputRouter` owns Gameplay input in the new scene. Existing bindings provide keyboard/gamepad movement, sprint, BigARM recall, and `Gameplay/Look` on the gamepad right stick. The camera relies on the Input System's radial stick deadzone rather than stacking another processor. | Binding structure is automatically verified. Physical-controller response remains user-owned acceptance. |
| Movement and camera | An isolated 3D Rigidbody motor and perspective camera rig provide camera-relative XZ movement, acceleration, sprint, facing, slope grounding, damped follow, obstruction pull-in, right-stick yaw orbit, and constrained right-stick pitch. | Compilation, scene validation, orbit math, movement-basis tests, and initial visual rendering pass. Right-stick direction/speed and pitch-range feel remain user-owned tuning. |
| World generation | Seeded height sampling, geometry-and-normal seam-matched chunk meshes/colliders, walkable safe-spawn selection, collision-aware prop placement, budgeted chunk creation, and padded unload hysteresis now exist. | Determinism, safe-spawn, and adjacent height/normal seam tests pass. Extended multi-chunk traversal remains user-owned acceptance. |
| Save/load | The existing versioned 2D prototype save systems remain preserved. | Perspective-world persistence and migration were explicitly deferred. |
| Survival economy | Existing prototype systems remain preserved. | Harvesting, items, balance, and loop redesign were explicitly deferred. |
| BigARM | The perspective foundation has a smaller 1.6 by 1.9 collider footprint with idle, follow, avoidance, stuck-recovery, and recall states. | Structure and compact footprint are validated; hands-on behavior acceptance remains user-owned. |
| Tooling | A guarded generated-scene builder, open command, non-mutating validator, isolated runtime assembly, and focused EditMode suite exist. | Perspective validation passes; all 16 EditMode tests pass. No smoke-test suite is retained. |

## Management Gates

- Keep the perspective foundation bounded to its separate scene and runtime assembly until the user accepts camera, movement, controller, terrain, and companion feel.
- Do not install navigation or asset-format packages, source production assets, alter Build Settings, or retire legacy content merely because the conversion plan exists.
- Treat the current camera values, generated terrain, greybox visuals, and BigARM states as tuning-ready foundations rather than finished design.
- Reconcile roadmap wording against implementation before selecting a new large feature; implemented code does not mean a phase's exit criteria are met.
- Use [PLAYTEST_LOG.md](./PLAYTEST_LOG.md) for the user's hands-on observations before expanding breadth. Codex does not create or run smoke tests unless the user explicitly requests them.
- Keep active dirty-file ownership in the current task brief or handoff, not in this durable status page.

## Next Decisions For The User

The next acceptance pass belongs to the user and should answer:

- whether the 120-degrees-per-second horizontal orbit, 70-degrees-per-second vertical pitch response, and 38-to-65-degree pitch range feel natural on a physical gamepad;
- whether movement remains intuitive and screen-relative while the camera is actively rotating;
- whether keyboard and a physical gamepad produce comfortable walk, sprint, recall, and camera behavior;
- whether terrain relief, chunk traversal, camera obstruction, and Booter grounding remain readable in motion;
- whether BigARM is now small enough and whether its follow spacing, avoidance, recovery, and recall feel intelligent enough for this foundation;
- which one of those foundation areas should be tuned first before deferred mechanics or production assets resume.

## Update Rule

Update this page when a workstream crosses a meaningful evidence boundary: absent to implemented, implemented to automatically verified, or verified to playtested. Link consequential decisions in [DECISION_LOG.md](./DECISION_LOG.md).
