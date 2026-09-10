# Current Engine Handoff

Updated 2026-09-10, America/Phoenix. The first application foundation and OR-1 geometry correctness batch are implemented and technically verified on the current Mac. All new work is under `Engine/`; Unity and Arc & Dust remain preserved references.

## Current Result

The standalone C++ executable opens an SDL window, renders a perspective mesh and open ground through bgfx Metal, and displays an interactive Dear ImGui inspector. Object color/rotation, directional light and camera settings share one editable fixture state. Orbit/zoom respects UI input capture. Window resizing, a minimum usable window size, errors, resource cleanup and diagnostics are implemented.

OR-1 adds a cube/sloped-solid/sphere selector, independent XYZ scale and world-normal display. Inverse-transpose normal transforms correct lighting directions under nonuniform scale; outward winding, backface culling and opaque depth ordering have bounded technical evidence. See [the geometry result](./OUTDOOR_GEOMETRY_RESULT.md) for exact comparisons and limits.

This is a foundation fixture. It has no generated rocks, character controller, physics, shadows, PBR, world streaming or save format. The perspective inspection camera is not final third-person character-following behavior. The neutral scale marker and local coordinates are provisional.

The [surface library](../Assets/SurfaceLibrary/README.md) now preserves 38 existing rock/ground textures and five source-art files inside `Engine/`, plus 15 textured material reference records and importer/channel mappings. [Transfer verification](../Evidence/SURFACE-transfer-final/result.json) passed: all 43 copies match their source hashes, 186 serialized texture bindings resolve, and 102 original image/metadata/material/shader/code files remained unchanged. No scene-texture loader or material shader port is implemented by this transfer.

Start with [run instructions](./RUN_FOUNDATION.md), [the outdoor plan](./OUTDOOR_RENDERING_PLAN.md), and [the latest result audit](./OUTDOOR_GEOMETRY_RESULT.md).

## Texture Research Result

The [texture-system research](../Research/TEXTURE_SYSTEM_RESEARCH.md), [implementation plan](./TEXTURE_SYSTEM_PLAN.md) and [cooking/verification SOP](../SOPs/COOK_AND_VERIFY_TEXTURES.md) are complete as research preparation. TEXTURE-001 built the pinned offline texture tool on Mac and retained [final measurements](../Evidence/TEXTURE-001-final-measurements/measurements.json). It found that stock mip generation produces incorrect numerical BC7 averages and incorrect RGBA8 color averages for the tested checker. KTX 1 works through the generic parser; tool-produced KTX2 needs a different parser/upload integration.

Recommendation: existing bimg primitives with engine-owned semantic mip generation, uncompressed KTX 1 first, then shared runtime resources and one rock/ground material pair. Compression follows comparisons against uncompressed references. No new dependency pins, runtime texture loader, material shader, GPU texture proof or Windows result was added. OR-2 remains next.

## Current Evidence

The [reviewed OR-1 build](../Evidence/OR1-build-reviewed/result.json), [two native test executables](../Evidence/OR1-native-tests-final/result.json), [16 tool tests](../Evidence/OR1-tool-tests/result.json), and [final native GPU verification](../Evidence/OR1-verification-final/result.json) passed. Normal/reference, cull/unculled and reversed-order comparisons each had zero differences above the rounding threshold across 163,200 sampled scene pixels. Opposite culling changed 111,802 pixels; 7,983 visible reference samples guard against empty-image passes. Twenty four-mesh replacements kept vertex buffers at 8; ten captures had no errors. Current executable, shaders and source hashes are bound to this verification.

## Original Application Evidence

The following receipts establish the earlier F1 slice. They are historical and do not identify the current executable after OR-1.

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

The fresh F1 build used the checkout's prepared, verified source cache; a separate clean machine was not tested. Final F1 UI corrections reused the compiled dependency libraries. OR-1 also reused the existing dependency build and changed no dependency pins.

## Selected Foundation Choices

C++20, CMake and the pinned SDL3/bgfx/ImGui cohort are selected for this bounded foundation. Direct pinned upstream sources remain the build acquisition method here. The inspector renderer uses a fixed font atlas and the official SDL3 platform adapter. See [decisions](./DECISIONS.md) for scope and review triggers.

Windows source/build branches are present but have not been executed on Windows. Mac convenience remains bounded: no proprietary Metal backend was written, and upstream source trees were not patched. The next Windows check needs native hardware/toolchain access. Physical-device input, minimize/restore, cross-display DPI transitions, long-duration operation and user-owned creative/feel acceptance remain unverified.

The initial build reports upstream shader-compiler deprecation/unknown-warning diagnostics and duplicate bx linkage warnings. They were retained, not suppressed. No game performance claim follows from this fixture's frame interval or its passing checks.

## Next Milestone

OR-2 is next: define and implement explicit linear lighting and display conversion, verify known color/illumination values, and define the HDR/exposure/UI composition boundary. OR-1 geometry is complete. Continue with sun/shadows, basic materials/ambient and repeatable inspection settings as ordered in [the outdoor plan](./OUTDOOR_RENDERING_PLAN.md). Rock recipes/generation begin after these rendering prerequisites.

Mac remains the main development machine while practical. Native Windows verification is a separate checkpoint during this milestone and remains required before target-PC claims; it does not require switching daily development now. Final hardware budgets, camera feel and accepted close-view visual targets remain open. Canyons, final geographic coordinates and broader gameplay remain deferred.

## Preparation History

The [research index](../Research/README.md), [reference collection](../References/README.md), [requirements](./REQUIREMENTS.md), [architecture](./ARCHITECTURE.md) and [SOPs](../SOPs/SESSION_HANDOFF.md) retain the preparation package. EXP-001 was the earlier headless compatibility probe; its result is not substituted for the real GPU evidence above. Historical rock screenshots still predate the accepted Unity sand treatment and are not the new game's visual acceptance baseline.
