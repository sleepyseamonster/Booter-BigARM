# Fixed terrain dressing — October 9, 2026

Owner: Gottspan; Unity integration belongs to Babineaux. Status: user-approved direction, first bounded implementation study. Appearance remains author-owned; no whole-world production dressing is installed.

## Direction and authority

Greater Wasteland's native terrain is persistent, prebuilt and always loaded. Procedural rocks, formations, ground dressing and points of interest operate on that surface. This supersedes the October 6 blanket procedural deferral for dressing only. Existing generated-landscape research and implementations remain reference material; their height generators, streaming lifecycles and save services are not production dependencies.

The broader infinite-world statements in WORLD_BASIS are retained setting history, not an instruction to generate Greater Wasteland terrain. This implementation does not settle the scope of future playable regions or rewrite unrelated lore.

The user's first hardware target is the current machine: RTX 3060 Ti, i7-9700K, approximately 64 GiB system RAM. Frame rate, resolution and quality budgets require a rendered Player baseline before numeric tuning becomes a production requirement.

## First slice

- `FixedTerrainSurfaceIndex` indexes the native grid once. Main-thread queries use TerrainCollider height/hole authority and native smooth normals. Negative coordinates, inclusive seams, outer-edge raycasting, duplicate ownership and unsupported transforms have explicit behavior. The caller rebuilds the index after ownership/transform changes and synchronizes physics before querying. No TerrainData is written.
- `FixedTerrainRockContactField` evaluates one bounded field from placed rock footprints and wind. Sand coverage, gravel and shallow relief share those inputs. Elliptical transformed-bounds footprints are an approximation, not exact mesh contact or simulated wind/erosion.
- `FixedTerrainDressingStudyBuilder` samples a 24 m patch around the saved Booter position, with 97 by 97 collision samples. It clones neither native terrain nor the generated-world runtime.
- The two approved formation stages are centered and uniformly fitted into the study footprint. Ground fitting samples transformed mesh vertices against the cached patch, with shallow burial. Thirty-six seeded slab/shard fragments accompany each family. Fragment positions are an intentionally simple comparison arrangement, not the accepted talus-placement grammar.
- Each family appears in three matched panels: geometry baseline on sand; shared sand/gravel contact; the same contact with up to 8 cm shallow relief. Each row shares rock transforms, source patch, texture coordinates and field seed. Relief uses real mesh vertices, recalculated normals and a collider referencing the same mesh.
- Existing artist-derived rock support maps are reused for comparison, not accepted as measured geometric height. The study uses LOD0 rock renderers only and establishes no production density/LOD budget. Rock-face sand staining and exact inter-rock contact are not implemented in this slice.
- The separate saved scene is `Assets/_Project/Scenes/Reference/FixedTerrainDressingStudy.unity`. It is absent from enabled Build Settings. Production terrain, player, lighting, renderer settings and packages remain unchanged.

Study geometry and materials live in `Assets/_Project/Art/Environment/Rocks/Studies/FixedTerrainDressing/`. The builder refuses to overwrite an existing study. Further iterations require versioned output destinations; hand edits must not be discarded by a rebuild.

## Identity, ownership and persistence

The study receipt records source scene hash, sampled TerrainData paths/hashes, source center and seed. These identify this experiment, not a final saved-world identity format. Scene hash is deliberately conservative and includes changes unrelated to terrain; a dedicated terrain manifest is the next production identity seam.

Study instance names derive from template source GUID plus baked instance ID, or a study-fragment ordinal. They are stable within this matched comparison but are not globally unique gameplay identities across repeated placements.

Production world identity will include terrain-manifest revision, world seed, dressing version and catalog version. Placement uses stable absolute cells/candidates; seed streams are independent by feature type. A new world chooses dressing once; resuming its save restores that dressing and applies runtime changes.

Terrain streaming/unload/reload is not applicable: native terrain stays loaded. Dressing-cell reconstruction must eventually preserve identity independently from renderer lifetimes. Interactive objects need stable feature IDs and persisted deltas; study rocks are noninteractive, have no obstacle collision and own no gameplay save state. Cosmetic ground meshes own matching collision only to verify the relief experiment.

Authored constraints are mandatory before production population: routes, spawn/companion clearances, exclusions, protected landmarks and POI footprints. This study does not scatter into gameplay, so those masks are not yet installed. No placement may change native terrain elevations to fit a POI.

## Experimental sequence

1. Establish readable rock/ground contact and compare shallow relief against shader-only coverage at the real camera distance and low sun angle.
2. Replace approximate contact where visible errors justify it with mesh-derived distance/contact data. Validate grazing views, burial and inter-rock gaps.
3. Prepare terrain analysis caches and authored masks; implement one bounded, seeded composition area with protected traversal and POI reservations.
4. Compare normal mapping, bounded parallax and physical relief using matched inputs. Parallax cannot be assumed to provide physical collision, silhouette or cast-shadow parity in URP.
5. Investigate structure-aware anti-tiling, keeping albedo, height, roughness and normals aligned. Directional strata constrain rotations. Compare surface-gradient normal composition with the current implementation.
6. Measure shared rendering/instancing and optional GPU-driven cosmetic debris when density warrants it. Rendering optimization cannot change authoritative placement or interactive identities.

Neural texture compression remains a separate feasibility experiment. Current NVIDIA guidance recommends Ada or newer for inference during sampling; no compression SDK, native dependency, driver change or renderer replacement is introduced here.

## Proof and acceptance

Focused EditMode checks cover native queries/holes, cell ownership, finite inputs, order-independent bounded fields, matched meshes/masks and relief-collider ownership. Shader import/compiler messages and the reference scene's asset topology require inspection. Production scene and sampled native data hashes must remain identical.

Offscreen captures are permitted in an isolated background batchmode project only. The first live Editor capture attempt stalled its main thread, so this workflow must not schedule further live captures or restart the user's Editor automatically. Capture operations and test execution are sequential.

Visual acceptance and hands-on smoke testing belong to the author. A rendered Player measurement is still required for CPU/GPU frame time, memory, generation latency, shadows, temporal stability and frame spikes. This slice makes no performance claim and does not install POM, new texture synthesis, runtime dressing, terrain streaming or save migration.

## Research basis

- [Guerrilla procedural placement](https://www.guerrilla-games.com/read/gpu-based-procedural-placement-in-horizon-zero-dawn): artist-authored rules plus procedural assembly.
- [Activision terrain rendering](https://advances.realtimerendering.com/s2023/Etienne%28ATVI%29-Large%20Scale%20Terrain%20Rendering%20with%20notes%20%28Advances%202023%29.pdf): bounded material composition, repetition control, cached surfaces and displacement.
- [Surface-gradient bump framework](https://github.com/mmikk/surfgrad-bump-standalone-demo): layered/projection-aware bump composition.
- [Local-statistics texture synthesis, May 2026](https://onlinelibrary.wiley.com/doi/10.1111/cgf.70355): irregular region textures and filtering considerations.
- [NVIDIA NTC SDK](https://github.com/NVIDIA-RTX/Rtxntc): current hardware and integration requirements.

Repository precursors: [clutter contract](../DeferredGeneration/GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md), [rock production](ROCK_QUALITY_AND_PRODUCTION_PLAN.md), [layered material](../../Engineering/ROCK_WORKBENCH_LAYERED_MATERIAL.md). Those documents retain their previous generated-world scope and proof limits.
