# Current Engine Handoff

Updated 2026-09-09, America/Phoenix. The first application/rendering foundation is implemented and technically verified on the current Mac. All new work is under `Engine/`; Unity and Arc & Dust remain preserved references.

## Current Result

The standalone C++ executable opens an SDL window, renders a perspective mesh and open ground through bgfx Metal, and displays an interactive Dear ImGui inspector. Object color/rotation, directional light and camera settings share one editable fixture state. Orbit/zoom respects UI input capture. Window resizing, a minimum usable window size, errors, resource cleanup and diagnostics are implemented.

This is a foundation fixture. It has no generated rocks, character controller, physics, shadows, PBR, world streaming or save format. The perspective inspection camera is not final third-person character-following behavior. The neutral scale marker and local coordinates are provisional.

Start with [run instructions](./RUN_FOUNDATION.md), [the completed milestone plan](./APPLICATION_FOUNDATION_PLAN.md), and [the result audit](./FOUNDATION_RESULT.md).

## Current Evidence

| Check | Observed result | Evidence |
|---|---|---|
| Fresh build configuration | Empty `build/foundation` directory configured successfully in about 46 seconds | [Configure receipt](../Evidence/F1-configure/result.json) |
| Source-built native application and shaders | Initial full build passed in about 332 seconds; small inspection-driven UI corrections rebuilt in about 6 seconds | [Fresh build](../Evidence/F1-build/result.json), [final build](../Evidence/F1-build-final/result.json) |
| Native state tests | Input capture, camera bounds/nonfinite state and scissor/framebuffer edge checks passed | [Final native tests](../Evidence/F1-native-tests-final/result.json) |
| Tool tests | All 15 tests passed, including corrupt PNG rejection and excluding inspector-only image changes | [Tool tests](../Evidence/F1-tool-tests-final/result.json) |
| Real GPU fixture | Metal on Apple M1 Max; inspector click, camera event, resize, 20 mesh replacements, close and cleanup passed | [Final verification](../Evidence/F1-verification-final/result.json), [application report](../Evidence/F1-verification-final/captures/verification.json) |
| GPU output | Material and camera edits change scene pixels outside the inspector; 2240x1440 capture resized to 2000x1360 | [Baseline](../Evidence/F1-verification-final/captures/baseline.png), [material](../Evidence/F1-verification-final/captures/material.png), [orbit](../Evidence/F1-verification-final/captures/orbit.png), [resized](../Evidence/F1-verification-final/captures/resized.png) |
| Failure handling | Missing shaders and invalid arguments fail with useful messages; shader startup failure releases the renderer | [Failure checks](../Evidence/F1-verification-final/result.json) |
| Dependency integrity | Six pinned source archives and extracted trees verified after the build | [Final inventory](../Evidence/F1-sources-final.json) |

The fresh build used the current checkout's prepared, verified source cache; a separate clean machine was not tested. Final UI corrections reused the compiled dependency libraries. The final executable, shaders and source hashes are bound to the current verification receipt.

## Selected Foundation Choices

C++20, CMake and the pinned SDL3/bgfx/ImGui cohort are selected for this bounded foundation. Direct pinned upstream sources remain the build acquisition method here. The inspector renderer uses a fixed font atlas and the official SDL3 platform adapter. See [decisions](./DECISIONS.md) for scope and review triggers.

Windows source/build branches are present but have not been executed on Windows. Mac convenience remains bounded: no proprietary Metal backend was written, and upstream source trees were not patched. The next Windows check needs native hardware/toolchain access. Physical-device input, minimize/restore, cross-display DPI transitions, long-duration operation and user-owned creative/feel acceptance remain unverified.

The initial build reports upstream shader-compiler deprecation/unknown-warning diagnostics and duplicate bx linkage warnings. They were retained, not suppressed. No game performance claim follows from this fixture's frame interval or its passing checks.

## Next Milestone

Begin the representative rock workbench from [FOUNDATION_PLAN.md](./FOUNDATION_PLAN.md): define an editable, versioned recipe and stable member identity; implement deterministic CPU geometry independent of GPU handles; support recipe save/load and invalid-input errors; then add a replaceable preview with near/side/far inspection. Define identity/version and geometry tolerance contracts before generation. Collision/LOD enters when the representative rock workload requires it.

Native Windows verification is an outstanding platform gate before target-PC claims. Final hardware budgets, camera feel and accepted close-view visual targets remain open. Canyons, final geographic coordinates and broader gameplay remain deferred.

## Preparation History

The [research index](../Research/README.md), [reference collection](../References/README.md), [requirements](./REQUIREMENTS.md), [architecture](./ARCHITECTURE.md) and [SOPs](../SOPs/SESSION_HANDOFF.md) retain the preparation package. EXP-001 was the earlier headless compatibility probe; its result is not substituted for the real GPU evidence above. Historical rock screenshots still predate the accepted Unity sand treatment and are not the new game's visual acceptance baseline.
