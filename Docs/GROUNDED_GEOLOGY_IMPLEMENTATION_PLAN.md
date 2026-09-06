# Grounded Geology Plan — Superseded

**Status:** superseded on 2026-09-05

The proposed Grounded Geology runtime platform and separate Integration Workbench are no longer an active implementation path.

The proposal duplicated responsibilities already owned by `TopDown3DWorldGenerator`, the editor Rock Workbench, the production rock baker, and the runtime rock catalog. It also inserted an A/B diagnostics workflow between the user and ordinary rock authoring without improving the production asset path.

Continue from [ROCK_QUALITY_AND_PRODUCTION_PLAN.md](./ROCK_QUALITY_AND_PRODUCTION_PLAN.md). The controlling route is:

`Rock Workbench -> user-shaped Golden Rock -> editor bake -> LOD/collider/material catalog -> World Creator placement`

The experimental implementation remains recoverable in Git history and in the named local reassessment stash. It must not be restored piecemeal unless a future approved plan identifies a specific missing capability that the canonical architecture cannot provide.
