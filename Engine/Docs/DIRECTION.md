# Accepted New-Engine Direction

Confirmed by the user on 2026-09-09. This document controls direction for `Engine/`; the research document contains proposed technical choices.

## Game and Presentation

Booter & BigARM will be a **regular third-person, fully 3D game**, built on a proprietary engine. The user's explicit confirmation is: "this will now be a regular 3rd person game".

Use a character-following perspective view as the presentation target. Exact distance, field of view, pitch limits, aiming, orbit controls and movement tuning remain open. Elevated top-down framing from the Unity prototype is reference behavior, not a new-engine requirement.

## Initial Work

Research and build the engine foundation before extending the rock generator. Then develop the rock generator/workbench for the open spaces of the Greater Wasteland. Canyons are a separate, deferred technical effort.

The initial foundation should establish a reproducible executable, graphics, input, diagnostics, resource ownership and development tools. A complete gameplay loop or finished landscape is not the first milestone.

## Current implementation depth

Updated by the user on 2026-09-14: develop toward a professional, AAA-capable proprietary engine through successive iterations. Initial visual fidelity need not be AAA, but each increment must contribute to sound architecture, engine performance, speed, predictable frame times and low latency. The earlier skeleton milestone does not justify accumulating disposable fixture features or polishing isolated controls.

Authoring is AI-driven: the user directs the agent to build the game through engine capabilities. Engine operations and authoritative data must be usable without UI widgets. A minimal scene viewer is sufficient. The separate UI agent owns viewer/UI changes; this engine lane owns runtime, rendering, assets, scheduling, authoring interfaces and performance. Preserve existing controls until their owner changes them. AI authoring does not imply model inference in the real-time simulation/render loop.

The [runtime priorities](./ENGINE_RUNTIME_PRIORITIES.md) apply this clarification to the next work. Continue using the master plan; the lighting sequence is subordinate to these architecture/performance priorities.

Clarified by the user on 2026-09-10: “we just need a base skeleton of an engine.” Build the smallest working version of each necessary subsystem and connect it to the shared runtime. Once that path works, move on. Advanced import coverage, visual polish, production tools, tuning and broad hardening are later work. Use focused checks to catch broken integration; do not turn each subsystem into repeated testing or perfection cycles. The long-range roadmap remains a reference, not a requirement to finish production features before the skeleton is usable.

## First user-facing milestone

On 2026-09-10 the user clarified that the first milestone should be a completed rock generator. Deliver the working native generation/material/collision/LOD/edit/save loop as a convenient runnable workbench before expanding into live world streaming. The terrain/job foundation already underway may finish; it does not shift the first deliverable to a whole streamed world. Continue at skeleton depth.

## Interface direction

On 2026-09-14 the user selected an AI-controlled engine interface, with only a handful of manual toggles, sliders or inputs and a scene viewer that can fill the screen. The user then clarified that even the menu and Advanced controls are unwanted: normal viewing must be scene-only, with controls exposed solely for a specific test. The UI agent owns sensible scene adjustments and should not make the user choose routine parameters. This records product direction, not proof of an implemented AI connection. See the [UI/UX workspace](../UIUX/README.md).

## Platform

Windows PC is the intended game platform. The user clarified on 2026-09-09 that Mac should remain the primary development machine for as long as practical. Introduce native Windows testing during the outdoor rendering milestone without requiring a daily-workstation switch. Move primary development when meaningful compatibility effort or target-specific debugging/performance work warrants it; the user permits that move. Do not constrain the Windows game to preserve Mac development. Specific PC hardware and performance targets remain open.

## Work Boundary

The user requested separation from Unity within this repository and then instructed: "lets focus on only the new work folder in the repo". `Engine/` is that folder.

- Keep all new-engine work here, including its documentation and future code, tools, assets, tests and build outputs.
- Preserve the existing Unity project and its unrelated edits. Do not move or restructure Unity assets.
- Read previous work only when it helps the current engine task; do not automatically continue Unity implementation plans.
- Keep one overarching foundation survey here, with focused audits and experiment records linked from it. References to historical work are not runtime dependencies or competing copies of game canon.

## Decision Status

On 2026-09-10 the user requested whole-engine analysis, a complete implementation plan, its audit and rewrite, and agent-owned technical sequencing. [The master plan](./FOUNDATION_PLAN.md) records that program. Routine engine requirements and implementation order should be managed by the agent; the user remains the creative/product authority. Planning does not claim that later capabilities or proposed libraries are implemented.

Accepted: proprietary engine, regular third person, open Greater Wasteland rock workload, deferred canyons, Windows target, preferred Mac development while practical, and `Engine/` as the working area in this repository.

Selected for the current application foundation under the user's implementation authority: C++20, CMake and the pinned SDL3/bgfx/Dear ImGui cohort, extended during P01 with nlohmann JSON for bounded engine documents and during P08 with EnTT registry storage for the shared simulation. The native Metal result is recorded in [STATUS.md](./STATUS.md); Windows and broader production suitability remain open. Jolt is selected for the P09/P10 collision/controller pass. Later supporting components in [the foundation research](./PROPRIETARY_ENGINE_FOUNDATION_RESEARCH.md) remain proposed. [DECISIONS.md](./DECISIONS.md) records selection scope and review triggers.
