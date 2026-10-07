# Ground Clutter And Natural Object System

This is the implementation contract for natural decoration in the perspective TopDown3D world. It extends the deterministic chunk generator without changing the protected 2D prototype.

## Current Production Posture

The previous world-wide cosmetic Scatter, GroundDetail, and FineGrayCluster layers are disabled in `TopDown3DWorldSettings.asset` with zero per-chunk targets. Their generators and catalog assets remain available for later tuning, but streamed chunks currently produce no loose cosmetic stones from these layers. The approved Scatter and Handbuilt Spire formations, their local rock sand-band shader, and their nearby terrain shader treatment remain separate and enabled.

## World Fit

The Broken World has no plant life or open water. Natural clutter is therefore geological: ironstone boulders, fractured slabs, shale shards, wind-scoured stones, mineral nodules, crust plates, gravel, and scree. Vegetation, moss, wood, wet mud, and ordinary river stones are out of scope. Machine debris belongs to a separate authored layer so decorative scrap is not mistaken for collectible salvage.

## Generation Contract

- `TopDown3DWorldSettings` owns global density, clustering, slope, spacing, seed-version, and spawn-clearance tuning.
- `TopDown3DNaturalObjectCatalog` owns stable content IDs, cost layers, physical size tiers, weighted shape families, scale/proportion ranges, sink depth, tilt, and footprints.
- `TopDown3DNaturalObjectPlanner` returns one immutable chunk plan. Cosmetic placement retains the natural-object generation version, while `TopDown3DRockFormationPlanner` owns all physical root and formation decisions under a separate physical-rock generation version.
- Physical rocks and every cosmetic clutter layer sample one shared abundance field with a zero-density floor and compact rock-rich islands. Scatter and ground-detail also derive their tighter masks from one common cluster seed, so the layers visibly group together instead of filling one another's gaps.
- Candidates are anchored to global cells and tested against neighboring cells before being assigned to a chunk. This keeps borders seamless and makes output independent of chunk load order.
- Terrain height, normals, feature identity, material weights, and placement masks come from the shared versioned `TopDown3DWorldGenerator`, the same authority used by the terrain mesh and far-landscape rings.
- Cosmetic placements are reconstructed from seed and are not save data. Future interactive or harvestable natural objects require stable gameplay identities and saved deltas in a separate layer.

## Cost Layers

1. `Obstacle`: sparse readable rocks from the shared baked catalog, one simple box collider per member, realtime shadows, and three screen-relative render LODs.
2. `Scatter`: small non-colliding stones combined into one mesh per chunk so the chunk remains the culling and lifetime boundary.
3. `GroundDetail`: dense chips and flakes combined into one mesh per chunk, with no colliders and no realtime shadow casting.
4. `FineGrayCluster`: small neutral-gray grit and shale pieces using a higher-frequency local cluster mask inside the shared abundance islands, a separate shared material, no colliders, and no realtime shadow casting.
5. `Landmark`: rare, extra-large spires and monumental outcrops with conservative slope limits, broad cross-chunk spacing, simple collision, and full obstacle shadows.

The preserved gray-layer design targets 156 candidates per 18-meter chunk when enabled, but its current production target is zero. Its sharpened local density mask and shared broad abundance field remain available for later use.

The per-chunk combined meshes are destroyed with their owning streamed chunk. They combine catalog LOD0 geometry at deterministic placements, while the reusable baked mesh assets remain shared and owned by the catalog.

## Formation Sandbox Ground Review

The Landscape Authoring Sandbox previews the surrounding ground from the same production authorities without turning the preview into a second generator:

For the active player-start area, select the `Landscape Authoring Sandbox` ground object and click `Use Current Player-Start Region` before placing stamps. The tool reads the current production profile and selects its terrain chunk; it does not modify the profile or production scene.

- `Landscape Sand` is a terrain-owned generator. It adds broad, deterministic semantic deposition from world identity and terrain material fields even when no rock formation exists.
- `Raised Sand Relief (m)` lives on the separate `Landscape Authoring Sandbox` authority and controls the actual raised terrain-sand geometry independently from coverage. Mixed formation workbenches only preview that terrain-owned result and cannot retune it. The same continuous world-space windrow field shapes low beds, irregular crests, and scoured gaps without changing rock scale or formation layout.
- `Sand Buildup` remains a separate formation-contact contributor. The preview composites those local banks with the terrain sand without making either generator own the other.
- `Landscape Rock Clutter` reuses `TopDown3DNaturalObjectPlanner`, the production catalog mesh families, world seed, generation version, abundance islands, geology masks, and chunk ownership. It only remaps the planned cosmetic stones onto the disposable flat review stage; the current production densities are zero.
- `Formation Clutter` remains a separate local control for the tighter pebble transition immediately around the reference formation.
- Rebuilding the preview regenerates all landscape clutter from stable inputs. Nothing under `__Generated Terrain Context` is authored content or save data.
- The active production `generateDepositedDust` setting is disabled. Streamed chunks still realize the approved authored formations and their shader-only contact treatment.

For a small hand-built area, open `Booter & BigARM > Top Down 3D > Open Landscape Authoring Sandbox`, then choose `Booter & BigARM > Top Down 3D > Add Ground Paint and Sand Bank Stamp`. Move the new child control with Unity's Move tool, choose `Red Dirt`, `Tan Sand`, `Shale Gravel`, or `Rocky Shale`, and adjust its radius, edge blend, opacity, and sand-bank height. Set height to zero for paint alone. `Update Ground Preview` rebuilds the disposable terrain mesh; its MeshCollider is rebuilt from that same mesh, and adjacent tiles use the same world-space stamp sample at their shared edge. The selected wire sphere is an editing guide, not rendered rock geometry. Stamps are saved as editable sandbox scene objects. They are not yet published into streamed gameplay; production terrain, scene, world settings, seed, chunk identity, and saved deltas remain unchanged. Sandbox stamps therefore have no streamed owner, generated-object ID, or runtime delta; publishing an accepted authored area must assign a stable authored ID and apply its ground rule through all terrain representation levels. Larger physical stones still require placed rock assets rather than the `Rocky Shale` texture blend.

The production family contains nine archetypes (`Pebble`, `Shard`, `Slab`, `Boulder`, `Nodule`, `Outcrop`, `Cliff`, `Talus`, and `HeroSpire`), three deterministic silhouette variants per archetype, and three progressively reduced LOD meshes per variant. The editor baker owns mesh creation; player/runtime code only resolves the catalogued assets.

Physical rocks have six independent root tiers: `Small`, `Medium`, `Large`, `Extra Large`, `Massive`, and `Towering`. Every tier can appear as a standalone rock. Deterministic multi-class precedence keeps `Towering > Massive > Extra Large > Large > Medium > Small` spacing stable across chunk borders while root density increases toward the smaller tiers.

Touching formation growth follows the strict hierarchy `Towering -> Massive -> Extra Large -> Large -> Medium -> Small`. Each parent receives bounded, independently seeded child opportunities, allowing organic branching rather than only linear chains. Contact distance is calculated from the actual directional support of both catalog LOD0 meshes and sampled across a controlled blend range. Physical-rock generation version 5 owns the baked-geometry cutover and accepts a child only when the center of the pair's overlapping bounds has deterministic interior clearance inside both closed source volumes. Non-parent projected support circles may not overlap, keeping the contact graph tree-shaped and avoiding ambiguous multi-way intersections. Candidate children are selected from twelve seed-rotated golden-angle directions, grounded from the shared geological generator, tested for spawn clearance, then scored deterministically. Hard child/member/depth caps prevent runaway formations, and failed child attempts do not discard valid ancestors or siblings.

The root chunk owns the whole formation even when a child crosses a chunk boundary. One immutable plan supplies stable root/member identity, world transforms, projected support, bounds, envelope, and height to every consumer. Production rendering reconstructs each member from the shared mesh catalog under one formation root, with one simple collider per member and one traversal component on the root. The former runtime Manifold fusion path is retained only as an editor-side asset-baking experiment; streaming and gameplay no longer depend on native Boolean work. Dust shelter uses the same actual envelope and height rather than reconstructing formation size from a member count.

## Wind-Deposited Dust

Deposited dust is a deterministic ground layer, separate from the airborne atmosphere system. A world-space field combines broad low-frequency pockets with anisotropic noise aligned to one prevailing wind direction. This creates long windrows, exposed scoured gaps, and coherent dust-rich basins across chunk borders.

The broad terrain field has its own `BuildTerrainPlan` entry point and depends only on world/terrain identity. Formation versions, catalogs, or missing formations cannot change that base field. Sand coverage comes from the semantic terrain deposit field, while raised height is modulated by the deterministic windrow field and a shallow minimum bed. The production composite may add local formation shelter afterward without transferring ownership of the terrain sand to the rock generator.

Physical obstacles and landmarks contribute shelter wakes. Dust accumulates only on their downwind side, curves slightly around each seeded formation, and fades with lateral and downwind distance. Larger formations produce wider wakes, while rare landmarks can anchor longer and taller banks. Steep slopes attenuate both broad deposits and sheltered piles.

Each chunk samples the continuous deposition field on a denser overlay grid than the base terrain. Visible cells become one opaque, non-colliding combined mesh using the swept-sand texture, soft mesh normals, a matte shared material, no realtime shadow casting, and ordinary shadow receiving. The material renders in the depth-writing opaque phase before the volumetric-dust composite, so deposits contribute scene color and depth, receive the same twilight shadows as terrain, and remain integrated when airborne dust is present. Neighboring chunks sample identical world positions and include a physical-rock halo, so height and coverage match exactly at borders. Generated meshes remain owned and destroyed by their chunk.

## Rock Surface Families

Ordinary obstacles, scatter, ground-detail rocks, and landmarks share one world-anchored surface field with three outcomes: regular stone, dark charcoal stone, and restrained teal mineral stone. The low-frequency field forms coherent geological patches across chunk seams; it does not alter positions, collision, scale, or the independent fine-gray layer. Each family uses the same triplanar shader and tuning with its own authored albedo texture, and combined visual layers are split by material so shared materials remain batch-friendly.

## Visual Standard

- Detail should come from top-down silhouette, deliberate broad planes, asymmetric proportions, controlled sinking/contact, and restrained distribution rather than hidden polygon density.
- Each shape has three deterministic baked variants and three LODs. Broad planar normals and asymmetric strata are intentional so silhouettes and light-facing planes remain legible from the elevated top-down camera.
- Use shared opaque URP materials; do not clone a material per object.
- Keep small clutter non-colliding and avoid realtime shadows where its screen contribution is tiny.
- The production catalog is the only runtime mesh authority. Missing or incomplete families fail closed; there is no procedural runtime fallback.

## Performance And Proof

The runtime uses spatially tight per-chunk combined meshes for visual layers. Each physical formation keeps one simple box collider per member but consolidates all member geometry into one three-level `LODGroup` per formation, avoiding per-member renderer multiplication while preserving the baked shapes and deterministic hierarchy. It performs no native Boolean work while chunks stream. Before raising density or adding shader features, verify with Unity Profiler and Frame Debugger at the canonical elevated top-down camera on target hardware. GPU instancing, GPU Resident Drawer, and GPU occlusion remain profile-driven options, not assumed wins.

Automated proof covers deterministic chunk plans, cosmetic-stream independence from physical versioning, independent roots across all six physical tiers, chunk ownership, multi-tier cross-border spacing, full-member spawn exclusion, strict downward branching topology and caps, directional immediate-parent contact, non-parent overlap limits, all 27 catalog families and 81 LOD assets, stable IDs, per-member LOD/collider topology, dust-envelope response, and bounded mesh generation. Visual quality, camera-distance readability, controller traversal, and Development Player performance acceptance remain separate checks. The editor-only fusion experiment retains its own isolated validation and is not a production dependency.

## Research Basis

- [Fast Poisson Disk Sampling in Arbitrary Dimensions](https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf)
- [Unity 6 GPU instancing](https://docs.unity3d.com/6000.0/Documentation/Manual/GPUInstancing.html)
- [Unity 6 mesh LOD configuration](https://docs.unity3d.com/6000.0/Documentation/Manual/configure-mesh-lod.html)
- [Unity 6 draw-call optimization choices](https://docs.unity3d.com/6000.0/Documentation/Manual/optimizing-draw-calls-choose-method.html)
- [Unity 6 URP GPU occlusion constraints](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/gpu-culling.html)
- [Unity mesh combination API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh.CombineMeshes.html)
