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

1. Phase 1.
2. Phase 2.
3. Phase 3.
4. Phase 4.
5. Phase 5.
6. Phase 6.
7. Phase 7.

## Notes

- The research plan is not a separate lane; it informs the order and quality bar for the phases above.
- The early game should stay simple until the survival traversal slice is fun on its own.
- Do not expand combat, crafting breadth, or biome count before the foundation and loop are working.
