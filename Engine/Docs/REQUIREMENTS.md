# Engine Requirements and Evidence

Status: planning baseline. [DIRECTION.md](./DIRECTION.md) controls accepted product direction. The implementation proposals below do not set final camera feel, geography, minimum PC hardware or a release date.

| ID | Requirement | First needed | Evidence that matters |
|---|---|---|---|
| R-01 | Standalone engine and all new work under `Engine/` | Foundation | Build without Unity assemblies, assets or editor processes; clean checkout instructions |
| R-02 | Regular third-person viewing | Rendering fixture | Perspective camera, correct scale, close inspection and obstruction handling; user decides feel |
| R-03 | Windows PC target; Mac development optional | Foundation | Native Windows compile and GPU execution before target claims; record hardware and backend |
| R-04 | Open Greater Wasteland rock workload; canyons deferred | All planning | Initial fixture uses simple open ground; no canyon generator or regional geography invented |
| R-05 | Editable, versioned rock recipes | Workbench | Load/edit/save/reload preserves declared inputs; invalid recipes produce useful errors |
| R-06 | Deterministic generation and stable identity | World/rock core | Same discrete inputs produce identical IDs; geometric tolerance/bitwise contract explicitly chosen |
| R-07 | Complete third-person rock quality | Workbench | Silhouette from all sides, close surface, ground contact, character scale and distance transitions reviewed separately |
| R-08 | Bounded loading and resource ownership | Foundation, then streaming | No stale job resurrects unloaded resources; measure upload/rebuild cost and memory recovery |
| R-09 | Authored constraints survive generation | Recipe/placement | Variation respects source envelope, exclusions and ownership; explicit override/version behavior |
| R-10 | Runtime deltas separate from generated geometry | World extension | Unload/reload and restart preserve changes against stable IDs; corrupt/incompatible data is detected |
| R-11 | Useful authoring and diagnostics | Foundation | Inspector, logs, input capture, explicit errors and timing; recipe document owns edits, UI does not duplicate state |
| R-12 | Repeatable builds and dependency provenance | First experiment | Exact source revisions, archive hashes, configuration and command receipts; required license notices retained |

For R-06, seed equality is not a guarantee of floating-point equivalence across ARM/x64 or shader backends. Decide exact discrete outputs separately from mesh tolerances. A GPU handle, pointer or temporary entity index is never a permanent generated-object ID.

## Open Product Inputs

| Input | Why it matters | Work that can proceed without it |
|---|---|---|
| Target Windows hardware, minimum OS and available test machine | Backend support, performance budgets and portability evidence | Tool preparation, headless integration, provisional renderer fixture |
| Visual reference and expected close-view detail | Material/lighting scope and triangle/texture budgets | Collect dated references and build adjustable inspection tools |
| Camera/control feel and character scale | Framing, input and collision tuning | A labeled neutral fixture; no inherited top-down limits |
| Resolution/frame-time objective | Measurable performance acceptance | Instrumentation and workload reporting; no pass/fail FPS target invented |
| GPU feature requirements | Whether bgfx is sufficient | Raster/mesh/instance compatibility experiment before advanced renderer investment |

These are decisions for the product owner where needed, not reasons to halt all preparation. Do not turn provisional defaults into accepted design.

## Deferred Scope

Canyons, final geographic coordinates, complete BigARM behavior, combat/crafting/survival systems, settlements, networking, a general-purpose editor, virtualized geometry and ray tracing. Preserve architectural compatibility where relevant; do not implement these during preparation.
