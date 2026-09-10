# Decisions and Open Questions

Update this record when evidence changes a decision. Keep the rationale and replacement date; do not silently rewrite an earlier selection. Accepted direction lives in [DIRECTION.md](./DIRECTION.md).

| ID | State | Decision / question | Owner and next evidence |
|---|---|---|---|
| D-01 | Accepted | Proprietary engine; regular third-person game | User, 2026-09-09 |
| D-02 | Accepted | Work exclusively inside `Engine/` in the existing repository | User, 2026-09-09 |
| D-03 | Accepted | Foundation first; open Greater Wasteland rock workbench next; canyons deferred | User, 2026-09-09 |
| D-04 | Accepted | Windows product target; Mac development optional | User, 2026-09-09 |
| D-05 | Accepted for foundation | C++20 and CMake | Native fresh build and final fixture checks; broader engine evolution remains open |
| D-06 | Accepted for foundation | Pinned SDL3, bgfx and Dear ImGui | Real Metal scene/inspector verified; Windows, advanced renderer requirements and shipping readiness remain open |
| D-07 | Proposed | Jolt when collision enters scope | Character/collision and streamed resource experiment later |
| D-08 | Accepted for foundation | Direct pinned upstream CMake sources with archive/content verification | One acquisition path for the fixture; revisit packaging/vcpkg when distribution or wider dependencies require it |
| D-09 | Open | Target Windows PC, minimum specification and frame-time budget | User/hardware evidence; do not extrapolate from the Mac |
| D-10 | Open | Camera feel, visual quality and final asset scale | User review after a functioning fixture |
| D-11 | Deferred | Final world coordinates, geology parameters and survival terminology alignment | User creative authority; no foundation dependency |
| D-12 | Accepted for foundation | Official SDL3 ImGui platform backend plus a project-owned fixed-atlas bgfx adapter | GPU output, clipping and injected inspector interaction verified; dynamic fonts, custom UI textures and multi-viewports deferred |

Foundation selections were made under the user's implementation authority on 2026-09-09, following [the application result](./FOUNDATION_RESULT.md). These supersede the earlier proposals for this bounded milestone; they do not establish final game performance, complete engine architecture or shipping readiness.

## Decision Procedure

A new entry names the requirement, alternatives actually considered, selected option or unresolved question, evidence paths, material limitations and next review trigger. Use `proposed`, `accepted`, `rejected`, `superseded`, `open` or `deferred`. A library version pinned for an experiment is not an accepted production architecture.
