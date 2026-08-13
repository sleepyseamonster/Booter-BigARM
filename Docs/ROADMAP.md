# Roadmap

This is the consolidated working roadmap for the prototype. It combines the current repo standards, the implementation sequence, the research plan, and the current game design constraints into one ordered plan.

## Design Constraints

- The game should feel like endurance, not empowerment.
- The player should be vulnerable, constrained, and forced to make tradeoffs.
- Movement, survival, and failure should accumulate over time rather than spike instantly.
- The world should be readable first and expansive second.
- Procedural generation should vary curated structure, not replace structure.
- Minimal UI should still communicate critical state clearly.
- BigARM is part of the world simulation, not a menu abstraction.

## Current Program Priority

The accepted planning direction is a top-down, isometric-style 2.5D presentation using 3D runtime assets. Gottspan owns the conversion program under the user's creative and product authority.

- [3D_CONVERSION_AUDIT_AND_CHECKLIST.md](./3D_CONVERSION_AUDIT_AND_CHECKLIST.md) is the canonical conversion master plan, system migration matrix, decision register, work breakdown, and acceptance checklist.
- The current phase is audit and planning only. No renderer, scene, package, settings, asset, or gameplay conversion has been implemented yet.
- Preserve the existing 2D prototype as a reference and recovery point until the 2.5D vertical slice and later cutover candidate are explicitly accepted.
- Do not expand the 2D presentation path except for preservation or separately prioritized fixes that remain valuable during conversion.
- The gameplay phases below remain desired game outcomes. Their implementation should proceed on the accepted 2.5D foundation rather than deepening soon-to-be-replaced 2D presentation systems.

## Workstreams

1. `Core Runtime`
- Input adapter and action map separation.
- Physics-backed player movement.
- Camera follow and framing.
- Save/load and runtime state ownership.

2. `World Generation`
- Deterministic seed, chunk, and version contract.
- Chunk streaming and runtime deltas.
- Biome and prop rules.
- Landmark and routing support.

3. `Survival Economy`
- Resource pressure and carry constraints.
- Salvage value, depletion, and recovery.
- Return-to-safety pacing.

4. `BigARM Loop`
- Safe-zone behavior.
- Storage, crafting, and recovery.
- Upgrade and home-base progression.

5. `Interaction Content`
- Harvesting and pickup interactions.
- Hazards, enemies, and hunt setup.
- Outposts, ruins, and other points of interest.

6. `Presentation And UX`
- Minimal HUD.
- Contextual prompts and feedback.
- Audio, motion, and world cues.
- Map, journal, or intel tools if needed.

7. `Tooling And Validation`
- Editor bootstrap and repair flows.
- Build automation.
- Smoke checks and targeted tests.
- Documentation alignment.

## Phases

### Phase 1: Structural Foundation

Finish the seams that the current prototype already implies.

Deliverables:
- Versioned save/load DTOs and JSON persistence in `persistentDataPath`.
- Explicit world identity with `seed`, `generationVersion`, and chunk identity.
- Input split into `Gameplay`, `UI`, and `System`.
- Debug/system actions moved off raw keyboard polling.
- Clear ownership between authored config and mutable runtime state.

Exit criteria:
- The prototype can save and restore basic runtime state without depending on scene objects.
- Input and world state boundaries are explicit enough to support later systems.

### Phase 2: Survival Traversal Slice

Build the first loop around going out, spending resources, and coming back.

Deliverables:
- Refined movement and camera feel.
- One survival resource or pressure mechanic.
- Carry pressure or salvage weight.
- A temporary BigARM or home-safe-zone return loop.

Exit criteria:
- The player can travel, lose something meaningful, and recover by returning home.

### Phase 3: Readable World Slice

Make the world navigable and memorable.

Deliverables:
- Landmark classes such as ruins, debris, chokepoints, and settlement silhouettes.
- Macro-routing so the world has legible paths and decision points.
- Procgen rules that support scouting and recognition, not just variance.

Exit criteria:
- Players can orient themselves in the world and remember important locations.

### Phase 4: BigARM And Recovery Slice

Turn BigARM into a real game object in the loop.

Deliverables:
- Storage and recovery behavior.
- Basic crafting or repair support.
- Upgrade path scaffolding.
- Home-base state that matters to the player.

Exit criteria:
- BigARM is a functional reason to return, recover, and prepare.

### Phase 5: Pressure And Purpose Slice

Add the first reasons to prepare for risk.

Deliverables:
- Salvage and harvesting interactions.
- Hazards or enemy pressure.
- Hunt preparation or other high-stakes excursions.
- Outposts or similar risk/reward locations.

Exit criteria:
- The world contains decisions that force preparation rather than casual wandering.

### Phase 6: Presentation And UX Slice

Make the game readable without becoming UI-heavy.

Deliverables:
- Minimal HUD for survival state and world state.
- Contextual prompts and feedback hierarchy.
- Audio and motion cues for tension and safety.
- Optional map, journal, or intel tools if they reduce confusion.

Exit criteria:
- The game communicates what matters at a glance and does not rely on dense menus.

### Phase 7: Tooling And Productionization

Make the prototype safe to extend.

Deliverables:
- Editor validation and scene bootstrap hardening.
- Build automation and repeatable checks.
- Runtime/editor test assemblies when the codebase needs them.
- Documentation updates that track actual implementation.

Exit criteria:
- The project can be rebuilt, validated, and extended without relying on tribal knowledge.

## Order Of Operations

1. Complete conversion planning decisions and the protected isometric technical spike through CP-06.
2. If the spike is accepted, establish shared runtime/validation seams and finite-area loop parity through CP-09.
3. Prove deterministic 3D world generation, streaming, and BigARM navigation through CP-12.
4. Build the representative asset pipeline, then integrate production content through CP-15.
5. Harden the exact candidate through CP-16.
6. Perform CP-17 cutover only with fresh user acceptance.
7. Perform CP-18 legacy retirement only with separate destructive authority.

The original Phase 1–7 gameplay order remains the feature-development order inside that conversion sequence. Conversion work must not be used as an excuse to expand combat, crafting breadth, biome count, or other adjacent systems before the survival traversal slice works.

## Notes

- The research plan is not a separate lane; it informs the order and quality bar for the phases above.
- The early game should stay simple until the survival traversal slice is fun on its own.
- Do not expand combat, crafting breadth, or biome count before the foundation and loop are working.
- The current prototype HUDs are intentionally immediate-mode; see [PROTOTYPE_UI_PLAN.md](./PROTOTYPE_UI_PLAN.md) for the later Canvas-based GUI pass.
