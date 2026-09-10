# Application and Rendering Foundation

Completed within the Mac technical-proof boundary, 2026-09-09; see [result and limitations](./FOUNDATION_RESULT.md). Authority: the user directed execution of the correct sequence with authority to accomplish the tasks. [DIRECTION.md](./DIRECTION.md) and [REQUIREMENTS.md](./REQUIREMENTS.md) control scope. This milestone closes the next application/rendering step; it does not authorize unrelated repository changes.

## Completion Contract

A fresh native C++ build launches a standalone application with a perspective mesh on open ground, adjustable object/material/light controls, orbit inspection, diagnostics, resize handling and explicit cleanup. Actual GPU images and focused failure/lifecycle tests substantiate the result. Windows and hands-on creative/feel acceptance are separately reported if unavailable.

All changes stay in `Engine/`. Preserve Unity and Arc & Dust. No terrain generator, character controller, canyon system, gameplay smoke test, full editor, final geography or distribution/release work belongs in this milestone.

## Sequence and Proof

1. **Build foundation:** add the native CMake entry point, pinned source verification, only required dependencies and source-built shaderc. Configure an empty build directory; preserve configure/build receipts and failures. Compile shaders as dependencies of the executable.
2. **Ownership:** separate platform window, editable fixture state, GPU resources/inspector adapter and application loop. Use the official SDL3 ImGui platform backend and a small project-owned bgfx rendering adapter. Do not patch upstream source trees.
3. **Visible fixture:** perspective geometry, open ground and neutral scale marker; directional diffuse material controls, orbit camera, frame/resource diagnostics. Temporary local conventions: meters, +Y up, right-handed view, origin at the inspection subject. These are fixture conventions, not geographic canon.
4. **Verification:** pure tests cover camera/input capture, clip bounds and invalid arguments; bounded native verification records actual renderer identity, screenshots before/after inspector edits, camera changes and resize, resource counts, shutdown and useful missing-shader failure. Do not equate injected input with physical-device proof.
5. **Closeout:** inspect images and diagnostics; verify source integrity and exact task-owned changes. Update status/decisions/run instructions, commit verified work, and report remaining platform/visual proof.

## Procedural Compatibility

The fixture has no generated world. Deterministic geographic identity, generated-object IDs, chunk unload/reload, authored generation constraints and persisted runtime deltas are inapplicable to its temporary mesh data. No save schema is introduced. CPU settings remain independent of GPU handles. GPU create/destroy/recreate evidence exercises a prerequisite for later streaming without claiming a streamed world. The [architecture contracts](./ARCHITECTURE.md) govern future generation.

## Decisions and Stop Conditions

Use C++20/CMake and the pinned SDL3/bgfx/ImGui cohort for this milestone. The real GPU path will determine whether to carry the cohort forward. Mac uses SDL's supported Metal view; Windows uses SDL's native window handle and the bgfx D3D path. No proprietary platform backend is planned. If supported Mac integration requires a substantial fork, preserve the exact blocker and move the remaining platform work to Windows; do not invent a Windows pass.

Do not continue to rocks by momentum after the completion contract is met. The subsequent milestone is editable, versioned rock recipes with deterministic CPU generation and save/load. Target hardware, final visual budgets and camera feel remain product decisions.

## Pre-Implementation Audit

The plan targets the missing real graphics workflow rather than repeating the headless probe. There is one runtime build and one authoritative fixture state; the historical probe remains evidence. No new service, account, package manager or geography decision is needed. Automated application verification will avoid activating the window or Unity. Captures come from our render surface, not the user's desktop.
