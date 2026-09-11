# Unity formation comparison and implementation

2026-09-10. User requested continued source analysis and native implementation to
bring the wasteland closer to the Unity prototype. This is a bounded continuation
of the rock transfer and terrain preview, under the existing foundation plan.

## Source findings

The current serialized prototype's active `Mixed Formation Ground` sandbox points
to `MixedPileScatterReference.prefab` and `Greybox_Terrain.mat`. Its settings enable
variation, burial, sand buildup and ground clutter. `MixedPileScatter.asset` retains
19 source member identities and baked variants. These are source observations;
the retained September 6 screenshot predates the later accepted sand treatment.
Unity was not launched, changed or used as a native runtime dependency.

`TopDown3DRockWorkbenchFormationGenerator.cs` supplies dominant anchors, buttresses,
pillars/talus, spaced size hierarchies, and piles with base/middle/cap hosts.
`BrokenWorldTerrainBlend.shader` uses semantic ground signals and explicit transition
maps, rather than periodic alternating color blobs. At the start of this pass, the native preview lacked these
composition and placement connections despite preserving the textures.

## This pass

1. Add recipe v4 formation composition: outcrop, scatter, supported pile, low ridge,
   and a mixed selection. Retain v1-v3 recipe behavior. Use varied member proportions
   and explicit support relationships through a common plan/seating function.
2. Add terrain v3 as a separate saved-world configuration. Stream bounded formation
   groups using shared member meshes, matching per-member render/collision transforms,
   and stable group/member IDs. Existing removal deltas remove a complete group.
3. Use irregular broad terrain material masks and the original sand/gravel/rock
   transition textures. Keep the old terrain v2 comparison path available.
4. Supply runnable terrain and authoring presets, native generation/placement/save
   checks, and native Metal screenshots. Retain source hashes and a clear gap list.

Procedural contract: versioned recipes plus region/group slot identify generated
content; child IDs derive from the group without renumbering after rejection.
Unload/reload regenerates plans. Authored route/exclusion checks use conservative
whole-group footprints. Persisted removed-group deltas filter all its members.
Render and collision consume the same seated plan. Artist-facing presets are
editable native documents; material handles stay transient.

Plan audit: reuse the current generator/workbench/stream/save paths. Do not import
Unity engine dependencies or replace source textures. This pass builds supported
multi-member compositions, not a formation-wide fused shell: geological seam masks,
editable individual formation members, terrain sand banks, clutter and exact Unity
silhouette families remain explicit later gaps. No canyon or origin-shift work.
Stop after a working, visibly closer native preview and focused checks; no broad
test corpus, gameplay smoke test or repeated aesthetic tuning.

Completed as a bounded first pass; see [result, launchers, evidence and remaining gaps](./UNITY_FORMATION_PARITY_RESULT.md).
