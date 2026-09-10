# Decisions and Open Questions

Update this record when evidence changes a decision. Keep the rationale and replacement date; do not silently rewrite an earlier selection. Accepted direction lives in [DIRECTION.md](./DIRECTION.md).

| ID | State | Decision / question | Owner and next evidence |
|---|---|---|---|
| D-01 | Accepted | Proprietary engine; regular third-person game | User, 2026-09-09 |
| D-02 | Accepted | Work exclusively inside `Engine/` in the existing repository | User, 2026-09-09 |
| D-03 | Accepted | Foundation first, including D-13 outdoor rendering; then open Greater Wasteland rock workbench; canyons deferred | User, 2026-09-09 |
| D-04 | Accepted; clarified | Windows product target; Mac remains primary development while practical; Windows testing is a separate checkpoint | User clarification, 2026-09-09; supersedes optional-only Mac wording |
| D-05 | Accepted for foundation | C++20 and CMake | Native fresh build and final fixture checks; broader engine evolution remains open |
| D-06 | Accepted for foundation | Pinned SDL3, bgfx and Dear ImGui | Real Metal scene/inspector verified; Windows, advanced renderer requirements and shipping readiness remain open |
| D-07 | Proposed | Jolt when collision enters scope | Character/collision and streamed resource experiment later |
| D-08 | Accepted for foundation | Direct pinned upstream CMake sources with archive/content verification | One acquisition path for the fixture; revisit packaging/vcpkg when distribution or wider dependencies require it |
| D-09 | Open | Target Windows PC, minimum specification and frame-time budget | User/hardware evidence; do not extrapolate from the Mac |
| D-10 | Open | Camera feel, visual quality and final asset scale | User review after a functioning fixture |
| D-11 | Deferred | Final world coordinates, geology parameters and survival terminology alignment | User creative authority; no foundation dependency |
| D-12 | Accepted for foundation | Official SDL3 ImGui platform backend plus a project-owned fixed-atlas bgfx adapter | GPU output, clipping and injected inspector interaction verified; dynamic fonts, custom UI textures and multi-viewports deferred |
| D-13 | Accepted for next milestone | Complete bounded outdoor rendering before the rock workbench | User's foundation-first direction and continuing authority, 2026-09-09; [OR-1 through OR-5](./OUTDOOR_RENDERING_PLAN.md) replace immediate rock implementation as the next step |
| D-14 | Accepted planning direction | Whole-engine roadmap and agent-owned routine technical sequencing | User request, 2026-09-10; [rewritten master plan](./FOUNDATION_PLAN.md) expands the earlier F1–F4 horizon. Product, creative and external-action boundaries remain. |
| D-15 | Proposed engineering integrations | EnTT, bounded JSON documents, Jolt, glTF/fastgltf, meshoptimizer, ozz, Recast/Detour, miniaudio and RmlUi at their first consuming stage | [Whole-engine research](../Research/ENGINE_ARCHITECTURE_RESEARCH.md); exact pins, notices and compatibility proof required on adoption. No installation or new runtime selection claimed by planning. |

| D-16 | Accepted first-pass integration | nlohmann JSON 3.12.0 for bounded typed engine documents; preserve the original cohort and extend its acquisition path through runtime-lock.json | [Runtime boundary and failure cases](./FIRST_PASS_RUNTIME.md); multi-file saves/migration remain later work |
| D-17 | Accepted first-pass rendering | Linear RGBA16F scene, explicit exposure, one SDR sRGB encode, display-space inspector | [Measured Metal pixels](../Evidence/M1-final-verification/result.json); final tone mapping and native Windows/HDR-display proof remain open |

| D-18 | Accepted first-pass simulation | EnTT v4.0.0 registry storage behind engine-owned stable IDs, ordered commands and fixed ticks | [Runtime integration and tests](./SIMULATION_FOUNDATION_RESULT.md); physics, streamed deltas and Windows remain subsequent work |

Foundation selections were made under the user's implementation authority on 2026-09-09, following [the application result](./FOUNDATION_RESULT.md). These supersede the earlier proposals for this bounded milestone; they do not establish final game performance, complete engine architecture or shipping readiness.

## Decision Procedure

A new entry names the requirement, alternatives actually considered, selected option or unresolved question, evidence paths, material limitations and next review trigger. Use `proposed`, `accepted`, `rejected`, `superseded`, `open` or `deferred`. A library version pinned for an experiment is not an accepted production architecture.
