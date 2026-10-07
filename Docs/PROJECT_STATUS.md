# Project Status

This is the shared implementation pulse for Booter & BigARM. It records what live repo evidence establishes, what still needs Unity or playtest proof, and which decisions are waiting for the user. It does not replace the strategic order in [ROADMAP.md](./ROADMAP.md).

Last reconciled: 2026-09-21 by Gottspan for production-host routing and Unity rock-work resumption. Existing workstream proof rows retain their earlier evidence boundaries.

## Greater Wasteland production scene — 2026-10-06

The user selected the existing playable Badwater terrain as **Greater Wasteland**, the primary scene for all continuing mechanics and controls. Its scene GUID, 256 terrain tiles, spawn, actors, input, camera, HUD and atmosphere wiring are retained. `GreaterWasteland.unity` is the only enabled build scene; `TopDown3DPrototype.unity` is a disabled generated-world reference. Do not add procedural terrain, generated props, chunk streaming or procedural save integration to Greater Wasteland until the user approves a redesigned generation approach. This promotes the current playable setup; it does not claim harvesting, persistence or other deferred mechanics are complete.

Verification: serialized checks confirm unchanged scene wiring/metadata, 256 terrains and ground markers, valid project/package GUID references and primary build routing. The coordinated terrain agent compiled and built the renamed scene in an isolated Unity 6000.4.0f1 Windows copy; the playable scene validator passed. After syncing the final HUD installer, `ConversionBaselineValidator.ValidateFromCli` exited 0 with "Production and legacy-boundary validation passed." Logs: ignored `Logs/DeathValleyInventory/windows-development-build.log` and `greater-wasteland-conversion-validator.log`. The live GUI was untouched; no gameplay test or Player run was performed. Earlier terrain/controls evidence below retains its original scope.

## Windows Editor Audit — 2026-10-06

The subsequent [Death Valley seam repair](./Evidence/Badwater/terrain-seam-repair-2026-10-06.md) resolves the missing-ground-marker finding and the user's visible cracks. Every tile now has a compatible rendering grid and persistent native neighbor stitching; all 480 shared edges match exactly. Nine focused background Editor checks pass, including the original Death Valley controls check. The earlier audit below remains historical evidence; its two unrelated existing-system failures and incomplete full-suite result are unchanged.

The [Editor playability audit](./Evidence/EDITOR_PLAYABILITY_AUDIT_2026-10-06.md) supersedes older snapshot rows for the narrow evidence it establishes: the original generated-world scene passes basic grounding, keyboard/gamepad movement, camera, inventory, and companion motion checks. The original Death Valley scene lacks `TopDown3DGroundSurface` on its 256 terrain owners and fails grounding; adding markers only in runtime memory confirms the repair direction. The focused system run passed 160/162 tests; decoration-radius expectation drift and macOS-only native authoring account for the two failures. The complete broad suite remains unverified. The user's current target is Editor Play mode; temporary executable builds were removed. Visual, physical-controller, extended traversal, and performance acceptance remain separate.

## Active Program

- The repository-root Unity project is again the sole active production implementation. `Unreal/`, `Engine/`, and `Assets/_Project/Legacy2D/` are preserved references and receive no new production work without a new explicit user decision.
- The user has revised the presentation direction to a perspective, elevated top-down game with a fully 3D runtime world and assets.
- Gottspan owns the conversion program under the user's creative and product authority.
- The perspective foundation was accepted and cut over as the primary production path. `GreaterWasteland.unity` is the only enabled Build Settings scene; former 2D scenes remain disabled, isolated legacy reference content.
- The prior landscape plan is [TOP_DOWN_3D_LANDSCAPE_IMPLEMENTATION_PLAN.md](./Design/DeferredGeneration/TOP_DOWN_3D_LANDSCAPE_IMPLEMENTATION_PLAN.md). The earlier [TOP_DOWN_3D_FOUNDATION_PLAN.md](./Design/DeferredGeneration/TOP_DOWN_3D_FOUNDATION_PLAN.md) remains historical foundation rationale and evidence.
- The current evidence packet is [ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md](./ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md).
- The former isometric lab and its audit remain historical comparison evidence, not the camera direction for new work.
- The existing 2D prototype remains a protected comparison baseline, not the production implementation.

## Current Foundation

| Workstream | Repo evidence | Proof state |
| --- | --- | --- |
| Input | A single `TopDown3DInputRouter` owns Gameplay input in the new scene. Existing bindings provide keyboard/gamepad movement, sprint, BigARM recall, and `Gameplay/Look` on the gamepad right stick. The camera relies on the Input System's radial stick deadzone rather than stacking another processor. | Binding structure is automatically verified. Physical-controller response remains user-owned acceptance. |
| Movement and camera | An isolated 3D Rigidbody motor and perspective camera rig provide camera-relative XZ movement, acceleration, sprint, facing, slope grounding, damped follow, obstruction pull-in, right-stick yaw orbit, constrained right-stick pitch, and a 25-world-unit landscape framing distance. | Compilation, scene validation, orbit math, movement-basis tests, and initial visual rendering pass. Final framing, right-stick direction/speed, and pitch-range feel remain user-owned tuning. |
| World generation | A versioned shared geological generator owns terrain height, normals, feature identity, material weights, drainage/corridors, placement masks, safe spawn, near streamed chunks, and non-colliding middle/far rings. The natural-object catalog now owns 27 baked rock families and 81 LOD mesh assets; runtime procedural rock construction and native fusion are removed from player generation. | Unity import and the canonical landscape validator pass; the owned landscape selection passes 27/27 EditMode tests. Fixed-camera near/mid/far review, seam and traversal acceptance, final material calibration, and controlled Development Player profiling remain separate proof gates. |
| Dust atmosphere | The global haze is deliberately parked: its retained controller defaults off, never becomes `Active`, and is not installed by the scene-load bootstrap. Volumetric haze, dust post-processing, global motes, and veils therefore do not render. Pocket sampling, authored zones, shaders, renderer feature, optics, tuning, and tests remain intact for a later return; ground-deposited drifts and footstep kick-up remain independent. | Source-level contracts guard the default-off posture while preserving deterministic distribution, optics, renderer-feature installation, and particle tuning. Runtime/editor-test compilation is green. A clean Play Mode restart and user visual acceptance remain the final proof that no global haze presentation is visible. |
| Save/load | The existing versioned 2D prototype save systems remain preserved. | Perspective-world persistence and migration were explicitly deferred. |
| Survival economy | Existing prototype systems remain preserved. | Harvesting, items, balance, and loop redesign were explicitly deferred. |
| BigARM | The new companion direction is canonical. The perspective follower tracks Booter's route, uses a follow band, acceleration/deceleration, turn-weighted movement, local avoidance, stuck recovery, and physical catch-up. Call and distance recovery no longer relocate BigARM; unavailable ground produces an explicit `WaitingForTerrain` state. | Source inspection confirms the relocation path was removed and focused EditMode checks were added. Their exact post-change run is pending a safe editor opportunity; hands-on feel and unloaded-world traversal remain open. |
| Tooling | A guarded generated-scene builder, open command, non-mutating validator, isolated runtime assembly, and focused EditMode suite exist. | Perspective validation passes; all 16 EditMode tests pass. No smoke-test suite is retained. |

## Management Gates

- Keep production TopDown3D code in its isolated runtime assembly and preserve the accepted legacy boundary.
- Do not install navigation or asset-format packages, source external production assets, alter Build Settings, or retire legacy content merely because the landscape plan exists.
- Treat the current camera values, generated terrain, greybox visuals, and BigARM states as tuning-ready foundations rather than finished design.
- Reconcile roadmap wording against implementation before selecting a new large feature; implemented code does not mean a phase's exit criteria are met.
- Use [PLAYTEST_LOG.md](./Evidence/Playtests/PLAYTEST_LOG.md) for the user's hands-on observations before expanding breadth. Codex does not create or run smoke tests unless the user explicitly requests them.
- Keep active dirty-file ownership in the current task brief or handoff, not in this durable status page.

## Next Decisions For The User

The next acceptance pass belongs to the user and should answer:

- whether the 120-degrees-per-second horizontal orbit, 70-degrees-per-second vertical pitch response, and 38-to-65-degree pitch range feel natural on a physical gamepad;
- whether movement remains intuitive and screen-relative while the camera is actively rotating;
- whether keyboard and a physical gamepad produce comfortable walk, sprint, recall, and camera behavior;
- whether terrain relief, chunk traversal, camera obstruction, and Booter grounding remain readable in motion;
- whether BigARM's route-following, follow distance, acceleration, turning, avoidance, and physical catch-up feel natural;
- whether pocket size and spacing create satisfying clear-air travel intervals, and whether entering/leaving pockets, sun-facing forward scattering, shadowed haze, far-visibility cutoff, close suspended dust, twilight color, and denser/sheltered transitions feel atmospheric without halos, shimmer, or obscured immediate navigation;
- which one of those foundation areas should be tuned first before deferred mechanics or production assets resume.

## Update Rule

Update this page when a workstream crosses a meaningful evidence boundary: absent to implemented, implemented to automatically verified, or verified to playtested. Link consequential decisions in [DECISION_LOG.md](./DECISION_LOG.md).
