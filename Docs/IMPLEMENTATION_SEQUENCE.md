# Implementation Sequence

This repo is still early, so the first gameplay work should establish swappable seams rather than final systems.

## Order Of Operations

1. Input adapter
- Wire the Unity Input System into a small runtime-facing adapter.
- Keep gameplay code isolated from direct `InputAction` calls.
- Preserve explicit `Gameplay` and `UI` separation.

2. Movement motor
- Add a physics-backed 2D motor behind a narrow interface.
- Keep input intent separate from movement execution.
- Leave room for future dash, stun, knockback, and vehicle-style mechanics.

3. Camera follow layer
- Keep camera behavior outside the movement controller.
- Follow the player through a dedicated camera layer.
- Treat camera tuning as data, not hardcoded logic.

4. World systems seam
- Add a deterministic generation contract for seed, chunk coordinate, and version.
- Keep runtime deltas separate from authored generation data.
- Leave the internal procgen algorithm open for iteration.

5. Save/load seam
- Define a versioned save DTO before writing any persistent gameplay state.
- Keep save data independent from scene objects and authored assets.
- Allow schema migration from the start.

## Working Rule

- Build the smallest version of each seam first.
- Prefer interfaces, data assets, and adapters over one-off hardcoded logic.
- Do not treat the initial implementation as final unless the design has stabilized.

