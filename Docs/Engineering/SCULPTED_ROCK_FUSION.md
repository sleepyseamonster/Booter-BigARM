# Sculpted rock fusion — October 10, 2026

The author wants overlapping sculpted rocks to become one continuous mesh, retain readable corner creases where the surfaces meet, and remove buried internal faces. Joining or Unity mesh combining alone does not remove these faces. This implementation is an explicit editor bake; it does not change geometry automatically while the artist moves rocks.

## Use

1. Duplicate textured sculpted rock prefab roots in the Unity study scene and overlap them into the desired formation.
2. Select 2–16 separate roots. They must have one mesh renderer each, or one LOD0 renderer under an LODGroup, and share one saved URP Lit material.
3. Open **Booter & BigARM > Geology Studies > Fuse Selected Sculpted Rocks**. The installed Blender executable is configured on this machine; another machine must provide its own path in the panel or `BOOTER_BLENDER_EDITOR`.
4. Click **Fuse Selected Rocks**. A background Blender process performs an Exact Boolean Union. Unity remains editable while it runs.
5. Inspect the new fused prefab instance. Save the scene to keep the arrangement. **Undo** removes that instance and restores the original active rocks. Sources remain in the hierarchy, disabled rather than deleted.

One output MeshRenderer uses the original material. UVs and source corner normals are interpolated through the Boolean cuts, and identical output corners are welded only when their positions, normals and UVs match. Tangents are recalculated. Thus each source's surface shading survives while normals can remain discontinuous at the meeting edges. This produces hard join creases where the two surfaces have different directions; flush or fully buried surfaces do not invent a groove. There is no smoothing-union blend or bevel that rounds away the authored shapes.

The output mesh and prefab are under `Assets/_Project/Art/Environment/Rocks/Fused/`, keyed by input geometry/material and the Blender version. A small `.fusion.json` report records topology, join-edge and buried-face counts. Raw job snapshots/results stay in ignored `Logs/RockFusion/`. No native `.blend` is edited or published.

## Geometry and failure gates

`Tools/Art/Blender/fuse_rock_meshes.py` uses the installed Blender 5.2.2 Exact solver. It welds UV-split input positions for solid operations while retaining loop UVs and normals. Source face provenance identifies join edges. It verifies closed topology, positive volume, no loose or degenerate geometry, and one connected result. Buried-face checks sample each tessellated surface triangle against the original solids, using boundary tolerance and three-ray parity for inside classification. A polygon's vertex average is unsuitable here because it can lie outside a concave polygon.

The bake rejects open meshes, disjoint/touch-only rocks, mixed materials, mirrored/zero transforms, more than 16 roots or over 100,000 input triangles. It is bounded to 120 seconds and can be cancelled. Missing Blender or lost corner attributes fail visibly. Source changes during a bake discard the result before scene changes; closing the tool or a domain reload cancels its task-owned worker. Mesh combining is never silently substituted for a failed union.

Boolean cutting can introduce new triangles even while removing interior surfaces. It is not a minimum-poly retopology operation. In the actual two-basalt fixture, 1,964 source triangles produce 1,840 union triangles, with 178 join edges, one closed component and zero sampled buried faces. Reduction, new LOD generation and optional seam bevel/rebake are separate future asset passes. The current merged asset has one display mesh and a static concave MeshCollider using that same surface. Do not attach a dynamic Rigidbody to it; moving/dynamic collision requires its own convex decomposition.

## Integration contract

This is authored asset creation, not a runtime dressing generator. A content fingerprint caches the bake; it is not a generated-world object ID. Deterministic placement and stable generated-object identities remain the dressing owner's responsibility. Greater Wasteland terrain remains fixed and always loaded, with no terrain or dressing-cell streaming changes here. If later placement cells unload/reload, they should reuse the same baked prefab from their placement record. Authored input selection, transforms, overlap and material are explicit constraints. Original scene sources and the fused instance are preserved through scene serialization and Undo; gameplay runtime deltas and saved-world schemas are untouched.

The older `RockFusion/TopDown3DManifoldNative` implementation remains a macOS-only editor reference. No package, native library or runtime host is changed. The new window only calls the installed Blender from editor code and stores a local executable preference.

## Evidence and limits

Focused integration checks run actual background Blender unions: overlapping cubes have the expected analytic volume, disjoint and open inputs are rejected, and the author's basalt prefab copies fuse as one closed textured surface. A separate isolated Unity render verifies the retained material/UV appearance. Evidence is under `Docs/Evidence/Art/SculptedRockFusion/`.

This proves the bounded fixtures and asset workflow; arbitrary self-intersecting sources, all coplanar/tangent degeneracies and high-density performance are not established. Sharp creases can retain texture seams inherited from the source UV charts. Different materials require a deliberate atlas/rebake workflow. No production population, source sculpt overwrite, gameplay smoke test or foreground activation occurs in this implementation.

Technique sources: [Blender Boolean Union](https://docs.blender.org/manual/en/latest/modeling/modifiers/generate/booleans.html) removes interior faces and documents closed-manifold requirements and solver tradeoffs; [Blender baking](https://docs.blender.org/manual/en/latest/render/cycles/baking.html) supports the later high-to-low/atlas handoff when materials or UVs need rebuilding.
