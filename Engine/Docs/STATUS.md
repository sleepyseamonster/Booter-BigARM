# Current Engine Handoff

Updated 2026-09-10, America/Phoenix. The first-pass shared document/identity foundation (P01) and linear HDR/display pipeline (P02) are implemented and technically verified on the current Mac. Portable builds (P07) are complete on Mac. All new work is under `Engine/`; Unity and Arc & Dust remain preserved references.

## Current Result

The standalone C++ executable opens an SDL window, renders a perspective mesh and open ground through bgfx Metal, and displays an interactive Dear ImGui inspector. Object color/rotation, directional light and camera settings share one editable fixture state. Orbit/zoom respects UI input capture. Window resizing, a minimum usable window size, errors, resource cleanup and diagnostics are implemented.

OR-1 adds a cube/sloped-solid/sphere selector, independent XYZ scale and world-normal display. Inverse-transpose normal transforms correct lighting directions under nonuniform scale; outward winding, backface culling and opaque depth ordering have bounded technical evidence. See [the geometry result](./OUTDOOR_GEOMETRY_RESULT.md) for exact comparisons and limits.

P01/P02 add UI-independent core libraries, structural generated-object IDs, negative/large region coordinates, strict versioned inspection documents with atomic replacement, linear RGBA16F scene rendering, exposure and a separate display-space inspector. See [the implementation result](./FIRST_PASS_RUNTIME.md). P04/P05 now add a first sun shadow and opaque material pass. Generated rock editing and player snapshots are now implemented; streaming remains ahead. P11 adds imported skeletal animation. A first capsule motor and third-person follow/obstruction camera are now implemented in P09/P10. The perspective inspection camera is not final third-person character-following behavior. The neutral scale marker and local coordinates are provisional.

The [surface library](../Assets/SurfaceLibrary/README.md) now preserves 38 existing rock/ground textures and five source-art files inside `Engine/`, plus 15 textured material reference records and importer/channel mappings. [Transfer verification](../Evidence/SURFACE-transfer-final/result.json) passed: all 43 copies match their source hashes, 186 serialized texture bindings resolve, and 102 original image/metadata/material/shader/code files remained unchanged. No scene-texture loader or material shader port is implemented by this transfer.

Start with [run instructions](./RUN_FOUNDATION.md), [the master plan](./FOUNDATION_PLAN.md), and [the first-pass runtime result](./FIRST_PASS_RUNTIME.md).

## Texture Research Result

The [texture-system research](../Research/TEXTURE_SYSTEM_RESEARCH.md), [implementation plan](./TEXTURE_SYSTEM_PLAN.md) and [cooking/verification SOP](../SOPs/COOK_AND_VERIFY_TEXTURES.md) are complete as research preparation. TEXTURE-001 built the pinned offline texture tool on Mac and retained [final measurements](../Evidence/TEXTURE-001-final-measurements/measurements.json). It found that stock mip generation produces incorrect numerical BC7 averages and incorrect RGBA8 color averages for the tested checker. KTX 1 works through the generic parser; tool-produced KTX2 needs a different parser/upload integration.

Recommendation: existing bimg primitives with engine-owned semantic mip generation, uncompressed KTX 1 first, then shared runtime resources and one rock/ground material pair. Compression follows comparisons against uncompressed references. That research batch added no runtime texture loader or GPU texture proof. OR-2/P02 is now complete; P03 texture cooking/residency is complete; P04/P05 sun and surface rendering are now implemented.

## Current Evidence

P20 adds [live bounded region streaming](./STREAMING_FIRST_PASS_RESULT.md), shared by both apps: asynchronous terrain generation, shared rock buffers, real region colliders, safe waiting, visibility and retirement. [Focused native cases](../Evidence/P20-runtime/result.json) and [the Metal load/retire/return check](../Evidence/P20-render/result.json) passed. Next: P21 persistent world deltas, then P22 origin/integration handling.

**First user-facing milestone delivered:** [the runnable native rock generator](./ROCK_GENERATOR_MILESTONE.md) is packaged at `out/rock-generator/`. The launcher opens the recipe and textures, preserves a working copy, and the workbench now exports its accepted mesh directly. [Native export/document checks](../Evidence/ROCK-delivery-native/result.json) and [the packaged Metal launch](../Evidence/ROCK-delivery-package/result.json) passed. P20 live streaming is now implemented; next is P21 persistent world deltas.

P18/P19 add [open terrain and bounded CPU generation jobs](./TERRAIN_JOBS_RESULT.md). [The combined native check](../Evidence/P18-P19-runtime/result.json) and [terrain cook](../Evidence/P18-terrain-cook/result.json) passed. Live terrain rendering/streaming remains unimplemented. The requested rock-generator package and handoff are now delivered; P20 live streaming is now implemented; P21 world persistence is next.

P06/P16/P17 add [live rock editing, document history and render/collision/LOD adapters](./ROCK_WORKBENCH_RESULT.md). [One combined CPU check](../Evidence/P06-P16-P17-runtime/result.json) and [one six-capture Metal pass](../Evidence/P16-P17-render/result.json) passed. Edits rebuild the preview and its stable collider; undo restores the original image, with no net buffer growth. P18/P19 now supply the CPU terrain/jobs foundation. Rock-generator delivery comes before P20 live streaming.

P14/P15 add [native rock generation and placement constraints](./NATIVE_ROCK_FOUNDATION_RESULT.md). [The combined CPU check](../Evidence/P14-P15-generation/result.json) passed for deterministic geometry, closed topology, stable identity, authored exclusions and two-agent route clearance. [The first rock cook](../Evidence/P15-rock-cook/result.json) writes the existing model format at `out/rocks/wasteland-001/`. P06/P16/P17 now add the live editing and adapter integration below.

P13 adds [basic player snapshots and a runnable package](./PLAYER_SNAPSHOT_PACKAGE_RESULT.md). [Native snapshot cases](../Evidence/P13-snapshots/result.json) passed for restart and interrupted/corrupt recovery. [The installed player](../Evidence/P13-package-launch/result.json) loaded its own shaders/model from an unrelated working directory and restored saved player/marker state. The current Mac package is `out/player-skeleton/`. P14/P15 now provide basic authored placement constraints and native rock generation.

P12 adds [the shared player skeleton](./PLAYER_RUNTIME_RESULT.md): one runtime now serves both apps, with an imported animated proxy, nearby marker interaction, minimal player overlay and first miniaudio cue. [Runtime/offline audio cases](../Evidence/P12-runtime/result.json) and [one render-only player capture](../Evidence/P12-player-render/result.json) passed. Player input feel and speaker/device output remain unverified. P13 now supplies snapshot/restart and a local portable package.

P11 adds [restricted model cooking, ozz animation/blending and GPU skinning](./MODEL_ANIMATION_RESULT.md). The repository-owned animated proxy renders in the workbench with matching shadows; the [single Metal comparison](../Evidence/P11-animation/result.json) matches CPU-skinned geometry and normals with zero sampled differences above three code values. [Focused native import/animation cases](../Evidence/P11-runtime/result.json) passed. P12 now supplies the player application and first cue; P13 now adds snapshot/package integration.

P09/P10 add [Jolt collision, capsule traversal and camera obstruction](./COLLISION_CHARACTER_RESULT.md), connected to the shared runtime and workbench. [Native technical cases](../Evidence/P09-P10-runtime/result.json) passed for collision queries/lifetime, slopes/steps/jump/ground loss and camera wall clearance. The graphical character path compiled; hands-on feel and native Windows remain open. P11 mesh/skin import and animation are now implemented; next is P16/P17 rock integration and workbench editing.

P08 adds [the shared fixed-tick/component/action runtime](./SIMULATION_FOUNDATION_RESULT.md), integrated into the workbench as an optional free-motion proxy. [Native compilation and focused runtime cases](../Evidence/P08-runtime/result.json) passed. EnTT registry storage and Jolt collision are now pinned. This is not physics, a character motor or a gameplay smoke-test result.

P04/P05 add [sun shadows, GGX material lighting, world-projected rock/ground textures, normal maps and saved lighting controls](./SUN_AND_SURFACE_RESULT.md). [One focused eight-capture Metal run](../Evidence/P04-P05-lighting/result.json) and native checks passed. This bounded first pass leaves larger-area shadows, IBL and final art quality open. Current rendering runs from `build/foundation`; the earlier portable package is historical. P08 now supplies shared simulation; P09/P10 collision and third-person control are now implemented as first passes.

P03 adds [semantic cooking, shared GPU textures and portable textured inspection](./TEXTURE_PIPELINE_RESULT.md). All 38 transferred textures and three diagnostics cook with complete mips. [Nineteen Metal captures](../Evidence/P03-native-final/result.json) include original rock/ground pixel comparisons; [100 GPU replacement/release cycles](../Evidence/P03-native-final/captures/texture-resources.json) restore resident bytes and texture counts. [Four native suites](../Evidence/P03-native-tests/result.json) pass. These are texture/runtime foundations; the following rendering batch binds them to 3D surfaces.

## Prior Foundation Evidence

[Core/document tests](../Evidence/M1-boundary-tests/result.json), [29 tool/oracle tests](../Evidence/M1-tool-tests/result.json) and [installed-package Metal verification](../Evidence/M1-final-verification/result.json) pass. Thirteen GPU captures retain the geometry checks, match all eight color bands at both exposures and after repeated target replacement, and keep the inspector independent of scene exposure. The installed workbench launches from an unrelated Engine-local working directory without source-relative shaders. [A fresh native build](../Evidence/M1-clean-build-final/result.json), [boundary correction](../Evidence/M1-boundary-tests/result.json) and [15-file local package](../Evidence/M1-package-final/package.json) are recorded. W01/W02/W03 remain unverified.

## Historical Geometry Evidence

The [reviewed OR-1 build](../Evidence/OR1-build-reviewed/result.json), [two native test executables](../Evidence/OR1-native-tests-final/result.json), [16 tool tests](../Evidence/OR1-tool-tests/result.json), and [final native GPU verification](../Evidence/OR1-verification-final/result.json) passed. Normal/reference, cull/unculled and reversed-order comparisons each had zero differences above the rounding threshold across 163,200 sampled scene pixels. Opposite culling changed 111,802 pixels; 7,983 visible reference samples guard against empty-image passes. Twenty four-mesh replacements kept vertex buffers at 8; ten captures had no errors. The OR-1 executable, shaders and source hashes are bound to that historical verification; current binaries use the M1 receipts above.

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

## Whole-Engine Plan and Next Milestone

**Execution depth:** the user clarified that the immediate goal is a base engine skeleton. Connect minimal working subsystems, then move on; advanced features, polish and production hardening remain later work. P06/P16/P17 inspection commands, rock adapters and live workbench editing are complete as first passes. P18/P19 terrain and bounded jobs are also implemented. The rock-generator package/handoff is now delivered. P20 live streaming is implemented. Next is P21 world persistence.

The user requested complete engine planning and autonomous technical sequencing on 2026-09-10. The [rewritten master implementation plan](./FOUNDATION_PLAN.md) now covers eight milestones from the reusable foundation through a supported Windows candidate. The [system audit](../Research/ENGINE_SYSTEM_AUDIT.md) covers 24 capabilities; [research](../Research/ENGINE_ARCHITECTURE_RESEARCH.md) records preferred integrations; [the retained draft and audit](./ENGINE_PLAN_AUDIT.md) explain the rewrite. The [roadmap index](./ENGINE_ROADMAP.json) contains 37 implementation packages and three native Windows gates. P01 through P20 are implemented as bounded first passes; use the index for the remaining package states.

[Plan structure/coverage](../Evidence/ENGINE-plan-structure/result.json), [seven focused planning-tool tests](../Evidence/ENGINE-plan-tool-tests/result.json) and [workspace documentation checks](../Evidence/ENGINE-plan-workspace/result.json) passed. These establish planning consistency and tool behavior only; no runtime rebuild, gameplay test or new GPU check was needed for this planning batch.

**Current program milestone: M1, reusable outdoor foundation.** The active user goal is autonomous implementation of a coherent first engine pass. P01/P02/P03/P04/P05/P07 are complete as bounded first passes on Mac. P08 simulation is now complete. P09/P10 physics and third-person traversal are now implemented. P11 animation/import is now complete as a bounded first pass. P12 shared player integration is complete as a first pass. P13 is complete as a first pass. P14/P15 are complete as first passes. Next are P16/P17 rock adapters and live workbench editing. P06 inspection commands are also ready. Continue coherent packages without asking the user to select routine engine subsystems. Rendering polish is not the next task.

The master plan remains the long-range program; completing this foundation batch does not complete the user's whole-engine goal. Product/creative, native Windows, external-action and destructive-operation boundaries remain explicit.

Mac remains the main development machine while practical. W01/W02/W03 define native Windows build, GPU/input and product-workload evidence; none is complete. Final hardware budgets, feel, accepted content and release decisions remain open. Canyons and final geographic coordinates remain deferred.

## Preparation History

The [research index](../Research/README.md), [reference collection](../References/README.md), [requirements](./REQUIREMENTS.md), [architecture](./ARCHITECTURE.md) and [SOPs](../SOPs/SESSION_HANDOFF.md) retain the preparation package. EXP-001 was the earlier headless compatibility probe; its result is not substituted for the real GPU evidence above. Historical rock screenshots still predate the accepted Unity sand treatment and are not the new game's visual acceptance baseline.
