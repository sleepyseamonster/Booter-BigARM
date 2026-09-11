# Rock transfer first pass

The accepted concept-transfer audit controls this work. Done means the native workbench can generate, adjust, save/reload and export layered fused rocks and a small deterministic formation using the existing assets. Unity remains reference-only.

1. Add a versioned layered material family using original side/top/underside/grit normal/surface maps and crack mask, with saved controls for grit, shale, cracks, dust and variation.
2. Add generator v3: seeded additive volumes, subtractive fracture cuts, bounded implicit meshing and editable source volumes. Keep v1/v2 output and documents compatible.
3. Extend the existing accepted-recipe, LOD, collision and export paths; add small deterministic outcrop/scatter/pile plans with stable member IDs and terrain-query seating.
4. Produce presets and a runnable Mac package. Run focused native geometry/persistence/collision checks and one bounded Metal visual pass, then commit task-owned changes.

Plan audit: reuse existing runtime/tool paths and original textures; do not add a second editor, third-party geometry package, terrain renderer, canyon system or live-world placement rollout. Formation generation belongs to the authoring workbench for this pass. Persist recipes/material IDs and member IDs, never GPU handles. Explicit generator versions protect existing world identity and regeneration; authored volumes are recipe constraints. Chunk reload can regenerate recipes; runtime deltas remain owned by existing world systems and are not altered by a preview edit. Windows and exact Unity visual parity are not completion claims.

## Completed

The first-pass checkpoints are implemented and verified. See [the result, runnable package, evidence and limits](./ROCK_TRANSFER_RESULT.md). Further visual parity work is separate from this completed pass.
