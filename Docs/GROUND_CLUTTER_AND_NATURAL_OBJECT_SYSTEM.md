# Ground Clutter And Natural Object System

This is the implementation contract for natural decoration in the perspective TopDown3D world. It extends the deterministic chunk generator without changing the protected 2D prototype.

## World Fit

The Broken World has no plant life or open water. Natural clutter is therefore geological: ironstone boulders, fractured slabs, shale shards, wind-scoured stones, mineral nodules, crust plates, gravel, and scree. Vegetation, moss, wood, wet mud, and ordinary river stones are out of scope. Machine debris belongs to a separate authored layer so decorative scrap is not mistaken for collectible salvage.

## Generation Contract

- `TopDown3DWorldSettings` owns global density, clustering, slope, spacing, seed-version, and spawn-clearance tuning.
- `TopDown3DNaturalObjectCatalog` owns stable content IDs, cost layers, weighted shape families, scale/proportion ranges, sink depth, tilt, and footprints.
- `TopDown3DNaturalObjectPlanner` is the pure placement layer. It uses independent seed namespaces for obstacle, scatter, and ground-detail layers.
- Candidates are anchored to global cells and tested against neighboring cells before being assigned to a chunk. This keeps borders seamless and makes output independent of chunk load order.
- Terrain height and normals come from `TopDown3DHeightSampler`, the same source used by the terrain mesh.
- Cosmetic placements are reconstructed from seed and are not save data. Future interactive or harvestable natural objects require stable gameplay identities and saved deltas in a separate layer.

## Cost Layers

1. `Obstacle`: sparse readable rocks with shared procedural meshes, simple box colliders, and realtime shadows. Obstacles remain rendered for the full lifetime of their off-camera-generated chunk so a screen-size cutoff cannot make them pop into view.
2. `Scatter`: small non-colliding stones combined into one mesh per chunk so the chunk remains the culling and lifetime boundary.
3. `GroundDetail`: dense chips and flakes combined into one mesh per chunk, with no colliders and no realtime shadow casting.
4. `FineGrayCluster`: small neutral-gray grit and shale pieces using an independent, stronger cluster mask, a separate shared material, no colliders, and no realtime shadow casting.

The default gray layer targets 156 candidates per 18-meter chunk, but its sharpened density mask rejects all candidates across broad low-value regions. The surviving 4.5–14 cm pieces bunch into substantially denser local pockets with ample bare ground between them, without changing the seed streams or placement of the original three layers.

The per-chunk combined meshes are destroyed with their owning streamed chunk. The small reusable faceted shape family is cached and shared.

## Rock Surface Families

Ordinary obstacles, scatter, and ground-detail rocks share one world-anchored surface field with three outcomes: regular stone, dark charcoal stone, and restrained teal mineral stone. The low-frequency field forms coherent geological patches across chunk seams; it does not alter positions, collision, scale, or the independent fine-gray layer. Each family uses the same triplanar shader and tuning with its own authored albedo texture, and combined visual layers are split by material so shared materials remain batch-friendly.

## Visual Standard

- Detail should come from top-down silhouette, deliberate broad planes, asymmetric proportions, controlled sinking/contact, and restrained distribution rather than hidden polygon density.
- Each shape has six deterministic variants. Meshes use flat per-face normals for a clean low-poly plane rhythm.
- Use shared opaque URP materials; do not clone a material per object.
- Keep small clutter non-colliding and avoid realtime shadows where its screen contribution is tiny.
- Medium and large production assets can replace generated meshes later without changing the planner contract. They should use authored LODs, simple collision, stable pivots, and the same catalog IDs.

## Performance And Proof

The first backend uses spatially tight per-chunk combined meshes for visual layers and individual objects only for sparse physical rocks. Before raising density or adding shader features, verify with Unity Profiler and Frame Debugger on the target camera and hardware. GPU instancing, GPU Resident Drawer, and GPU occlusion are later profile-driven options, not assumed wins.

Automated proof covers deterministic placement, chunk ownership, cross-border obstacle spacing, all-layer spawn exclusion, catalog completeness, stable IDs, and bounded mesh generation. Visual quality, camera-distance readability, controller traversal, and performance acceptance remain user-owned hands-on checks.

## Research Basis

- [Fast Poisson Disk Sampling in Arbitrary Dimensions](https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf)
- [Unity 6 GPU instancing](https://docs.unity3d.com/6000.0/Documentation/Manual/GPUInstancing.html)
- [Unity 6 mesh LOD configuration](https://docs.unity3d.com/6000.0/Documentation/Manual/configure-mesh-lod.html)
- [Unity 6 draw-call optimization choices](https://docs.unity3d.com/6000.0/Documentation/Manual/optimizing-draw-calls-choose-method.html)
- [Unity 6 URP GPU occlusion constraints](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/gpu-culling.html)
- [Unity mesh combination API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh.CombineMeshes.html)
