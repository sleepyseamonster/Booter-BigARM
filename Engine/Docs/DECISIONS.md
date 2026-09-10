# Decisions and Open Questions

Update this record when evidence changes a decision. Keep the rationale and replacement date; do not silently rewrite an earlier selection. Accepted direction lives in [DIRECTION.md](./DIRECTION.md).

| ID | State | Decision / question | Owner and next evidence |
|---|---|---|---|
| D-01 | Accepted | Proprietary engine; regular third-person game | User, 2026-09-09 |
| D-02 | Accepted | Work exclusively inside `Engine/` in the existing repository | User, 2026-09-09 |
| D-03 | Accepted | Foundation first; open Greater Wasteland rock workbench next; canyons deferred | User, 2026-09-09 |
| D-04 | Accepted | Windows product target; Mac development optional | User, 2026-09-09 |
| D-05 | Proposed | C++20 for the engine, CMake for builds | Native compatibility experiment; production selection follows review of the result |
| D-06 | Proposed | SDL3 and bgfx as the initial platform/graphics candidate | Verify exact source cohort, GPU workflow and inspector integration; Noop execution cannot close this decision |
| D-07 | Proposed | Jolt when collision enters scope | Character/collision and streamed resource experiment later |
| D-08 | Open | Production dependency acquisition: vcpkg versus pinned upstream CMake sources | The initial probe may use a bounded direct-source cohort without selecting the production package workflow |
| D-09 | Open | Target Windows PC, minimum specification and frame-time budget | User/hardware evidence; do not extrapolate from the Mac |
| D-10 | Open | Camera feel, visual quality and final asset scale | User review after a functioning fixture |
| D-11 | Deferred | Final world coordinates, geology parameters and survival terminology alignment | User creative authority; no foundation dependency |

## Decision Procedure

A new entry names the requirement, alternatives actually considered, selected option or unresolved question, evidence paths, material limitations and next review trigger. Use `proposed`, `accepted`, `rejected`, `superseded`, `open` or `deferred`. A library version pinned for an experiment is not an accepted production architecture.
