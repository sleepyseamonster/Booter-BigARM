# Batch 7 pre-cutover natural-planner baseline

Date: 2026-08-24

This commit protects the existing dirty `TopDown3DNaturalObjectPlanner.cs` before Batch 7 changes its physical-rock authority. The snapshot includes the current resource-node integration, topology-v2 terrain sampling, and cosmetic-density work already present in the worktree.

The protected file is not claimed to be an independently compilable change: it depends on other user-owned landscape and resource work that remains in the dirty worktree. Its purpose is exact recovery and authorship preservation before the production call to the cell-density rock planner is replaced.

Pre-cutover proof inherited from Batch 6:

- Focused natural-object deterministic chunk-plan test: passed 1/1.
- Topology-v2 World Creator selections: passed 65/65 and 9/9.

Batch 7 may modify this protected planner only at the physical-formation authority seam. Cosmetic placement and resource realization remain downstream consumers.
