# Flat-region ground clutter — 2026-09-29

The first region remains on the existing gentle Big/Little Noise terrain. This slice
activates the existing cosmetic natural-object layers in the production world settings:
32 scatter targets, 72 ground-detail targets, and 48 fine-gray grit targets per 18 m
chunk. Actual counts remain conditional on terrain, spacing, clusters, and exclusions.
The active terrain shader already blends sand, gravel, and rocky textures from World
Creator surface semantics; this slice adds physical small-stone silhouettes above
those materials without increasing terrain relief or changing approved formations.

Cosmetic candidates still use seed and absolute world cells, own one chunk, respect
neighbor spacing, slope, surface weights, and the spawn exclusion, and rebuild on
unload/reload. A 0.45 minimum on cosmetic abundance keeps a thin background of
stones between richer rock pockets. The large-formation abundance function and
generation version remain unchanged. These non-interactive stones add no colliders
or persisted runtime deltas. The existing authored Rock Workbench formation rules
and off-camera chunk publication continue to own large silhouettes and visibility.

An isolated exact-source Unity 6000.4.0f1 mirror passed the focused production
clutter test (1/1). At the representative flat-region chunk `(27, 17)`, the plan
contained 23 scatter, 44 ground-detail, and 38 fine-gray placements. Focused
determinism and spawn-exclusion checks also passed (1/1 each) against the final
settings and source. The production prototype validator exited successfully in
the same mirror after opening the production scene.

This is placement and compilation proof. It does not establish perceived density,
texture quality, streaming feel, collision gameplay, or moving Player performance.
Hands-on Play Mode evaluation is user-owned. The next visual review should compare
the flat-region start at the production camera against the supplied landscape
reference, judging foreground grit, mid-ground rock groups, readable routes, and
whether the existing sand/gravel/rock material transitions are strong enough.
