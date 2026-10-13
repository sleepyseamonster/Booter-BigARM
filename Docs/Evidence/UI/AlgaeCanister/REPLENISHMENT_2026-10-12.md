# Passive algae replenishment

October 12, 2026. The author specified that algae naturally replenishes so ordinary hunger and thirst remain sustainable, with mild resource pressure in the wilds. This supersedes the finite-only reserve in the original canister milestone.

The canonical `TopDown3DAlgaeCanisterSettings.asset` now authors `replenishmentPerSecond: 0.02`. `TopDown3DAlgaeCanisterState.Advance` adds growth to the existing player-owned reserve, including from zero, and clamps at capacity. Runtime updates use `Time.deltaTime`, after vitals and before consumption, so growth and depletion share simulation time. Sleep acceleration accelerates both; a paused simulation advances neither. There is no seed dependency, terrain/dressing lifecycle, generated-object identity, offline refill, snapshot version change or new save owner. Configuration and snapshot restore retain their existing contents semantics.

## Provisional balance

| Quantity | Rate or time |
| --- | --- |
| Passive growth | 1.2 algae per game-minute |
| Ordinary hunger cost | 0.375 algae per game-minute |
| Ordinary thirst cost | 0.6 algae per game-minute |
| Combined maintenance cost | 0.975 algae per game-minute |
| Surplus after maintaining both needs | 0.225 algae per game-minute, about 23 percent above demand |
| Regrow 5 units with no consumption | 4 minutes 10 seconds |
| Regrow 10 units with no consumption | 8 minutes 20 seconds |

These figures use the actual production depletion and dose costs. Basic needs are sustainable while using the controls; healing still draws on the shared buffer. The rate has no separate wilderness or settlement modifier in this slice. Wilds pressure is provisional and requires later hands-on balancing. No automatic drinking/eating or starvation-damage system was introduced.

The existing HUD gauge follows growth. Its numeric label updates only when the displayed tenth changes, with `<0.1` for positive trace contents, rather than allocating fresh text every growth frame.

## Verification

Unity 6000.4.0f1 imported and compiled the changes. The connected background-safe fixture runner executed **101 cases, 101 passed, 0 failed**, including error-log capture. New checks cover recovery from empty, capacity clamping, invalid/zero elapsed intervals, frame subdivision, 4x simulation-time equivalence, retained contents on restore/configuration, reentrant growth, zero/invalid rates and one-hour supply sustainability. The one-hour check kept hunger and thirst full while contents rose from 50 to 63.5. Existing consumption, controller, input, survival, camp, sleep, menu and radial regressions also passed.

This is direct EditMode fixture execution with setup/teardown in an additive sandbox and disposable preview scenes, not a gameplay smoke test. The loaded scene and foreground application were preserved. Unrelated rock work remains outside the commit. The [individual results](replenishment-tests.json) record the executed cases. No package, terrain, scene, asset GUID or publication change is included.
