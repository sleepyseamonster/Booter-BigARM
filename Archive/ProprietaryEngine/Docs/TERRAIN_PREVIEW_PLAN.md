# Wasteland terrain preview

Completed: [result and evidence](./TERRAIN_PREVIEW_RESULT.md).

2026-09-10. User-authorized extension of P18/P20, under the existing foundation plan.

Done means one packaged native outdoor preview with continuous seeded landforms,
three terrain detail levels, blended original ground textures and grounded v3
boulders, using the existing streaming/save path. Unity remains reference-only.

1. Preserve terrain v1; add v2 with 4 m samples and multiscale continuous landforms.
2. Derive render LODs from the same samples, with edge skirts; keep full-resolution
   collision and queries. Bound staging by the largest of all shared rock variants.
3. Seat low-detail v3 boulders against the actual terrain triangles, preserve stable
   placement slots and remove/reload deltas, and honor authored exclusions/routes.
4. Bind existing sand, gravel and rocky maps; add a scene-camera preview mode and
   package launcher without changing the existing rock workbench's default behavior.
5. Run focused terrain/stream/save checks and one Metal load/retire/return capture.
   Package, retain the result, and commit only this work.

Procedural contract: recipe version/seed identify generated ground; region and cell
slot identify placements; unloading regenerates the same samples/IDs. Authored
constraints filter placement. Existing saved removed-rock deltas remain authoritative.
V2 uses a separate world profile; v1 is never silently upgraded. Reserved routes are
clearance constraints, not a claim of graded roads or AI navigation.

Plan audit: use the existing region runtime and collider adapter, not a second
terrain engine. Keep the current engine-owned integer-address noise primitive;
FastNoiseLite is a useful candidate but introduces no necessary capability for this
bounded pass. This also preserves nearby samples at very large region addresses.
No erosion simulator, voxel world, canyon system, live sculpting, origin shifting,
new paid tool or Windows compatibility project. Heightmap import is specified in
the research note as a later offline input adapter, not required for this preview.
