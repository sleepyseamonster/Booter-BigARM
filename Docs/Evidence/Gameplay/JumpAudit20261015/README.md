# Camp Loop jump audit evidence

Collected October 15, 2026, Phoenix time. The [audit report](../../../Design/Gameplay/CAMP_LOOP_JUMP_AUDIT_2026-10-15.md) interprets these observations and proposes future work. This directory contains audit output only; no implementation was changed and no jump was simulated.

| Evidence | Boundary |
| --- | --- |
| [Inspected source hashes](inspected-source-hashes.json) | Exact 77-file code, test, clip, profile, setting and scene snapshot |
| [Live selection and sweeps](live-selection-and-sweeps.json) | Read-only active Editor/profile metadata and eight current resting-capsule sweeps; all eight clear |
| [Native collision probes](native-collision-probes.json) | 960 hypothetical casts against loaded collision geometry; no actor moved and no physics stepped |
| [Clip curves](clip-curves-and-transitions.json) | Current RootT.y curves and source endpoint offsets before rendering/retargeting/projection |
| [Body-muscle endpoints](muscle-channel-endpoints.json) | 55 exact HumanTrait muscle channels; auxiliary quaternion/translation channels excluded |
| [Calculations](audit-calculations.json) | Offline timing, ideal gravity arc and ideal planar-slope geometry; not a gameplay recording |
| [Console status](console-status.json) | Current compilation and visible-console snapshot |
| [Recent console errors](console-errors.json) | Five recent input-cancel/teardown entries; older bridge history excluded |

The author Editor remained in Edit Mode with its dirty scene intact. No scene save/reload, import/recompile, foreground action, graph evaluation, Play Mode, gameplay smoke test, package operation or external publication occurred. Previous passing tests are discussed as historical evidence and coverage limits, not rerun results.
