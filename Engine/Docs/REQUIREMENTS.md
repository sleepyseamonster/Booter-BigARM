# Engine Requirements and Evidence

Status: whole-engine planning baseline; implementation status remains in STATUS.md. [DIRECTION.md](./DIRECTION.md) controls accepted product direction. The implementation proposals below do not set final camera feel, geography, minimum PC hardware or a release date.

| ID | Requirement | First needed | Evidence that matters |
|---|---|---|---|
| R-01 | Standalone engine and all new work under `Engine/` | Foundation | Build without Unity assemblies, assets or editor processes; clean checkout instructions |
| R-02 | Regular third-person viewing | M2 character calibration | Perspective camera, correct scale, close inspection and obstruction handling; user decides feel |
| R-03 | Windows PC target; prefer Mac development while practical | Foundation | Separate native Windows testing checkpoint; compile and GPU execution before target claims; record hardware and backend |
| R-04 | Open Greater Wasteland rock workload; canyons deferred | All planning | Initial fixture uses simple open ground; no canyon generator or regional geography invented |
| R-05 | Editable, versioned rock recipes | Workbench | Load/edit/save/reload preserves declared inputs; invalid recipes produce useful errors |
| R-06 | Deterministic generation and stable identity | World/rock core | Same discrete inputs produce identical IDs; geometric tolerance/bitwise contract explicitly chosen |
| R-07 | Complete third-person rock quality | Workbench | Silhouette from all sides, close surface, ground contact, character scale and distance transitions reviewed separately |
| R-08 | Bounded loading and resource ownership | Foundation, then streaming | No stale job resurrects unloaded resources; measure upload/rebuild cost and memory recovery |
| R-09 | Authored constraints survive generation | Recipe/placement | Variation respects source envelope, exclusions and ownership; explicit override/version behavior |
| R-10 | Runtime deltas separate from generated geometry | World extension | Unload/reload and restart preserve changes against stable IDs; corrupt/incompatible data is detected |
| R-11 | Useful authoring and diagnostics | Foundation | Inspector, logs, input capture, explicit errors and timing; recipe document owns edits, UI does not duplicate state |
| R-12 | Repeatable builds and dependency provenance | First experiment | Exact source revisions, archive hashes, configuration and command receipts; required license notices retained |

The original R-01–R-12 requirements continue through the full program. The following complete-engine capabilities extend that foundation; package mappings belong to [the master plan](./FOUNDATION_PLAN.md), and current capability coverage is audited in [the system inventory](../Research/ENGINE_SYSTEM_AUDIT.md).

| ID | Complete-engine requirement | First integrated proof |
|---|---|---|
| R-13 | Shared fixed-step runtime with explicit entity/command ownership | P08/P12: bounded ticks, actions and the same runtime in Workbench and Game |
| R-14 | Collision-backed third-person traversal and camera obstruction | P09/P10: slopes, steps, ground readiness and camera queries; feel separately reviewed |
| R-15 | Reproducible mesh/skeleton/animation asset path | P11/P12: provenance-recorded proxy, validated import and actual skinning/blending |
| R-16 | Terrain/placement constrained by authored routes and coherent streaming | P18/P20/P22: border, readiness, precision and repeated residency evidence |
| R-17 | BigARM true position, physical regroup and persistent loaded/coarse travel | P23/P24/P27: no teleport, one time owner, validated re-entry and save continuity |
| R-18 | Persistent interactions and conserved finite cargo | P25/P28: presence, stable targets, transaction identity and no duplicate/lost items |
| R-19 | Shared-state player UI and audio with input/accessibility boundaries | P26/P33: action arbitration, readable prompts, cue lifetime, settings and text identity |
| R-20 | A representative survival expedition exercises the engine | P30: bounded integrated loop and durable consequences; user accepts rules/feel |
| R-21 | Useful production authoring through shared runtime/document commands | P17/P31: undo, saved recipes, assets/constraints and validation without duplicate preview systems |
| R-22 | Measured CPU/GPU/IO/memory behavior and scalable quality | P22/P34/W03: declared workloads and supported-PC evidence, not Mac extrapolation |
| R-23 | Player package independent of source checkout/editor | P13/P35: relocatable technical build, clean-machine/update and diagnostics |
| R-24 | Versioned durable state with interruption recovery and migration | P13/P21/P36: consistent generations, old-version migration and last-good retention |
| R-25 | Complete required presentation: lighting, materials, visibility, atmosphere and animation | P05/P16/P20/P32: readable representative third-person scenes and measured quality |
| R-26 | Reproducible candidate validation and explicit proof boundaries | P37: exact source/assets/package, required native gates, notices and known issues |

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

Canyons, final geography, networking, a general-purpose editor, virtualized geometry and ray tracing remain deferred. BigARM, survival/combat/crafting infrastructure, authored encounters and production tools are now explicitly planned in later master-plan milestones; they are not requirements of the first rendering batch. Final game content, settlements and feature breadth remain product scope, not automatic implementation authority.
