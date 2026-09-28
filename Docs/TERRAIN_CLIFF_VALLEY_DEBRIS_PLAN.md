# Terrain Cliff, Valley, and Debris Plan

Status: proposed implementation plan, 2026-09-28. This document records an audit and a preferred experiment. It does not approve a new visual direction, alter the accepted terrain shapes, enable canyons in the first playable area, or authorize a terrain/save cutover.

Implementation checkpoint, 2026-09-28: `WorldCliffSectionStudy` now provides a read-only, absolute-coordinate candidate and toe-influence study with stable landform-domain IDs and a Booter/BigARM route screen. Its technical thresholds are unapproved calibration values. No terrain material, mesh, collider, scatter, save record, or production streaming path consumes it yet. Four focused EditMode tests passed in an isolated Unity project mirror. The next step is to measure candidates on fixed approved terrain sections and compare the visible methods below before choosing production face geometry.

Calibration checkpoint, 2026-09-28: the [production-query section survey](Evidence/WorldCreator/CliffStudy/README.md) sampled four fixed 48 m windows at seed `24681357`. It found 129 candidate steep sections, including 34 near an 18 m chunk border, with high-drop examples around 5–6 m. One window had none. Broad-ground query context IDs proved point-specific, so the study now uses absolute owner cells for candidate identity and leaves the parent feature empty until a larger scarp plan exists. The sampled slopes do not yet express the hard caprock, layered ledges, and scree apron in the user's reference. This survey is a working-tree technical snapshot, not visual acceptance; the next design step is stable cross-section grouping and a fixed-camera material/modules/fitted-face comparison.

## User visual reference, 2026-09-28

The user supplied [this stepped-mesa cliff image](VisualReferences/Terrain/SteppedMesaCliffs_2026-09-28.png) as the intended terrain feel. It is a composition and geological-form reference, not a production screenshot or a request to replace the accepted heightfield, sky, camera, or first-area no-deep-canyon rule.

The defining shapes are broad open valley floors between long, staggered mesas; a hard, nearly horizontal dark caprock edge that breaks into ledges and buttresses; exposed lighter strata beneath it; and a sloping scree apron joining each wall to the valley floor. The large walls have quiet stretches and abrupt broken corners rather than a uniform jagged contour. Near rocks are large and irregular, while smaller fragments gather near cliff toes and thin out across the open floor. Repeated mesa bands create depth without filling every route with rock.

For the controlled comparison, judge whether the approved terrain can read as **top/edge/face/apron/floor**, especially from the production camera. Test material bands and caprock accents before adding large fitted geometry. The present `WorldCliffSectionStudy` supplies only a rim, toe, and deposit candidate; it does not yet infer the caprock break, intermediate strata ledges, or the full debris fan shown here. Those features need a shared parent-face plan and visual proof, not independent scatter at each steep sample.

### Authored cliff wall sample, 2026-09-28

The user also supplied [a Unity screenshot of their wall](VisualReferences/Terrain/CliffWallSample_2026-09-28.png). The exact saved `cliff wall` scene hierarchy was captured as [`CliffWallSampleReference.prefab`](../Assets/_Project/Art/Environment/Rocks/Source/CliffWallSampleReference.prefab): 18 editable rock workbenches with their original transforms, seeds, and source volumes. The source prefab uses a separate `LightCliffGray` workbench preset; the original scene and the shared material were not recolored. The material already contains gray layered albedo maps, while the previous `DarkFracturedDesert` preset applied a strong charcoal multiplier. The new preset removes that multiplier and adds restrained seed-based neutral-gray variation. Its final appearance still needs a game-camera review in the existing light.

This is the **fractured face vocabulary**, not one wall to stamp repeatedly. The saved source has no durable preview mesh; rebuilding its 18 members produced about 61,000 vertices and 122,000 triangles for a roughly 3 m wide, 3.3 m high composition. It is an authoring source, not a runtime prefab. Before streamed placement, bake a bounded set of variants by changing member shapes and spacing within the approved silhouette, combine/simplify the visible exterior for near/mid LODs, and use a smaller collision proxy. Select a variant and restrained transform offsets from a stable scarp-section ID. Fit each section to the measured rim/toe and leave quiet gaps, ledges, and route openings. Never reseed by chunk load order or place 18 raw workbench meshes per repeated wall.

## Goal and authority

Make steep terrain read as exposed rock, make valleys and scarps feel like connected places, and make debris visibly originate from nearby faces. Preserve the current broad terrain shapes while testing the treatment. The controlling product rules are `WORLD_BASIS.md` and `WORLD_CREATOR_CHARTER.md`; `WORLD_CREATOR_ARCHITECTURE_PLAN.md` controls world identity and representation; `TOP_DOWN_3D_LANDSCAPE_BENCHMARK.md` controls the production-camera comparison. The root Unity project is the sole production lane.

The Broken World is dry. Valley and drainage forms can encode ancient or abstract causes but must not imply an active river or rain cycle. The provisional first playable area has rolling terrain and no deep canyon system; the serialized `ProductionWorldCreatorProfile.asset` currently leaves canyon excavation disabled there. A separate technical comparison area may exercise the existing canyon planner without changing that rule.

## Audit of the current terrain

| Surface | Current implementation fact | Consequence |
| --- | --- | --- |
| World truth | `WorldCreatorProductionRuntime` exposes a shared query; `TopDown3DWorldGenerator` is a compatibility-facing adapter, not an independent terrain generator. `HybridTerrainPlan` holds canyon segments and bounded landform requests with stable IDs. | Extend the shared plan/query family. Do not revive the retired escarpment sampler or add a chunk-local geology authority. |
| First-area shape | With canyons disabled, `HybridTerrainWindowQueryService.SampleBaseHeight` strongly attenuates broad relief and adds faceted 36 m shelf/rise signals, 24 m pits, 4.5 m small shelves/divots, and 3 m fine relief. The current uncommitted query also blends broad flat cores from absolute 256 m owner cells. | The immediate cliff study should follow the approved shelf edges and pit shoulders. It should not replace them with generic canyon walls or mistake fine relief for a cliff. The flat-core work is live but uncommitted and must be re-audited before implementation. |
| Terrain representation | `WorldRepresentationCompiler` samples absolute positions into near/mid/far grids. The near 18 m tile is 25 vertices across, or 0.75 m per edge. `TopDown3DProceduralWorld` builds its near mesh and later assigns the same mesh to a streamed terrain `MeshCollider`. | Broad hills, slopes, and shelves fit this surface. Sub-grid cracks, true vertical walls, caves, and overhangs require bounded feature geometry. A face overlay must not misrepresent its physical contact. |
| Materials | `WorldSurfaceMaterialService` already raises strata exposure on slopes and canyon walls. `WorldTerrainMaterialPackingAdapter` maps deposit, gravel, bedrock, and weathering to vertex color for the existing terrain shader. | Material calibration is one candidate, but another slope-only color rule cannot create a fractured silhouette or a coherent cliff-to-talus relationship. |
| Valley semantics | `WorldSurfaceSemantic` has canyon floor, shelf, and wall, but no general rim/toe/gully/debris-source contract. The active first-area query does not excavate canyon segments. | A first-area scarp needs its own planned cause or a measured terrain-derived face band; it must not be labeled a canyon simply to borrow the wall flag. |
| Debris | The natural-object catalog has baked `Cliff` and `Talus` families. General scatter is density/competition based. The current `TopDown3DWorldGenerator.Sample` adapter places a slope proxy in its `Talus` field, and the scatter planner uses that value for admission. | The present value describes a steep *source*, not an accumulation at the *toe*. A source-linked debris field is needed before increasing rock count. |
| Traversal | The climbing plan uses the true collidable surface. BigARM requires a real route and physical regroup. | Decorative faces cannot claim climbable ledges. Feature collision, contact, top-out, and route reservations must derive from the same plan. |
| Evidence | Batch 10 proved deterministic long transects and near/mid/far data agreement, but its visual acceptance and full-content target profile remained open. The current worktree also contains unrelated uncommitted World Creator and gameplay edits. | Treat this as a source-and-contract audit, not a fresh visual or frame-rate verdict. Start experiments in isolated comparable fixtures and preserve the live worktree. |

## Candidate methods and decision

| Method | What it does well | Limitation in this project | Decision |
| --- | --- | --- | --- |
| A. Slope-driven material alone | Cheap, seamless with existing terrain and shader. | Cannot change rim/toe silhouette or create large fractures; already partly implemented. | Keep as the base response and tune only against fixed-camera evidence. |
| B. Scatter cliff prefabs at steep samples | Reuses baked catalog and LODs quickly. | Independent pieces make repeated stamps, floating contacts, blocked routes, and debris without a parent face. | Use individual assets as accents, not as the layout authority. |
| C. Place baked rock modules along a planned contour | Strong authored silhouettes, reusable LODs and simple baked collision. | Curved rims and unequal drops can leave gaps or repetitive joins. | Test as a face treatment and retain for prominent ledges, buttresses, and broken corners. |
| D. Generate a low-poly, contour-fitted face representation | Continuous rim-to-toe coverage and deterministic chunk seams; supports varying drop and strata bands. | Runtime mesh creation, overlaps, shadowing, and collider mismatch need proof. | Preferred continuous substrate; keep it bounded and visual-only until contact is proven. |
| E. Use volumetric/SDF geometry for every cliff | Can represent undercuts, arches, and caves. | Unnecessary complexity and meshing/collision cost for ordinary slopes. | Reserve for rare bounded physical features with an explicit traversal role. |
| F. Run erosion simulation during chunk loading | Can produce natural channel and debris patterns. | Load-order, runtime cost, and save/topology stability are difficult to bound. | Use editor/offline experiments to derive rules; ship deterministic plans and samples. |

**Preferred combination:** one absolute-coordinate valley/scarp plan defines the route, floor, shoulders, rim, toe, strata, and debris sources. The existing heightfield renders broad ground. The existing material service exposes rock along the face. A bounded contour-fitted face provides continuity; baked catalog pieces add selective silhouette breaks. A separate source-linked talus/debris plan places fragments below that face. True vertical or overhanging pieces are sparse collidable feature meshes with the climbing and route contracts. Compare C alone against D+C before choosing the final face representation; do not assume the generated substrate is visually necessary.

This selection follows feature-primitive terrain research: high-level valleys and ridges are planned first, then combined with local detail. It also adopts the useful part of modular cliff production—authored rock vocabulary and simplified collision—without making fixed modules define an effectively infinite world.

## Generation contract

1. **Purpose and parent:** Each valley, scarp, or gully has a stable parent feature, cause, strata family, and intended spatial role: open travel, reveal, overlook, narrow passage, or quiet basin. A valley centerline and its crossings are planned at a larger scale than an 18 m chunk. First-area scarps can be derived from the existing approved landform shape without enabling canyon excavation.
2. **Face admission:** Detect a continuous band with sufficient slope, vertical drop, run length, convex upper rim, and lower toe. Use hysteresis and a bounded world-space halo so small normal changes or chunk boundaries do not split a face. Reject isolated steep triangles and keep authored site, spawn, formation, and Booter/BigARM route reservations visible to the classifier. Thresholds are calibration variables, not accepted design numbers.
3. **Face form:** Derive strata breaks and fracture orientation from the parent geological context. Vary long stretches by unequal ledge spacing, eroded notches, buttresses, and quiet intervals; do not repeat a complete module arrangement. Ground the face against the *rendered near terrain triangles*, not only the continuous sampled height, to avoid visible/collider disagreement at 0.75 m mesh spacing.
4. **Debris:** Emit a stable source intensity from each exposed face section. Transport it downhill in a bounded field to produce a dense toe apron; widen to a fan at a gully mouth; allow rare outliers. Match fragment family and weathering to the source, grade size and density with travel distance, and preserve clear movement/camera sightline corridors. Talus is a deposit field, not the local slope value.
5. **Representations:** Near receives face detail and physical contact only where authorized; mid/far retain the same large silhouette and stable feature identity without full debris geometry. Cosmetic changes use the appropriate material/decoration version; changed landform or collision truth requires a deliberate topology/landform version and saved-place review.
6. **Lifecycle:** A feature ID derives from world seed, relevant version domain, canonical parent/owner, and stable ordinal. Chunk roots hold only the clipped representation and release it on unload. Reload and local-origin rebase reproduce the same absolute face/debris plan. Static geology regenerates; any future mineable/destructible face saves only deltas against its stable ID.

## Controlled comparison before production integration

Use one 20–30 m approved steep section, one valley shoulder, one gully/toe, and a section crossing a chunk boundary. Fix seed, absolute coordinates, camera, quality, lighting, and material set. Capture near, gameplay, and mid-distance views from below, above, and along the face. Compare:

1. current terrain/material baseline;
2. material calibration only;
3. baked contour modules only;
4. fitted face alone;
5. fitted face plus sparse baked accents;
6. winning face with source-linked toe apron and gully fan.

Score each on geological attachment, silhouette, joins, stratum continuity, route readability, contact honesty, camera occlusion, visible repetition, render cost, collider cost, and chunk transition. A treatment wins only when it improves the fixed-camera view and does not create a worse contact or streaming result. Preserve the current height profile through this comparison. The user's visual choice is the gate before applying the winner broadly.

## Implementation sequence after the comparison

### 1. Semantic plan and deterministic field

Add a bounded face/debris plan under World Creator, keyed to existing feature IDs and absolute coordinates. Expose rim, toe, source, transport/deposit, strata, and route-clearance signals without encoding them as GameObjects. Add repeated-seed, different-build-order, border, rebase, and ownership tests. Stop if the same world place changes with load order or if a protected route is narrowed.

### 2. Terrain material and near face

Connect exposure/deposit signals to the existing surface-material and packing path. Implement the selected face treatment under the near chunk/feature representation lifecycle, with no second height authority. Verify common border samples, no z-fighting or gaps, correct normals and shadows, and safe unload/reload. Keep purely cosmetic overlays non-colliding.

### 3. Source-linked debris and art

Use the existing baked catalog for fragments and accent modules; add only families proven missing by the comparison. Generate apron, fan, and outlier plans with a fixed per-feature and per-chunk budget. Check parent lithology, spacing, route clearance, contact sand/gravel, LOD, and repeated-seed identity. Do not copy the expensive editor-only formation treatment directly into runtime streaming.

### 4. Physical walls and climbing

For the limited sections selected for vertical traversal, add simplified static collision that matches the visible face and publish the same feature identity to climbing and path/affordance queries. Verify contact loss on unload, top-out clearance, Booter/BigARM route truth, and save restore. Apply a terrain-version change only if physical geography changes; document the old-save behavior before cutover.

### 5. Broader valley grammar and profile

Extend from the accepted section to variable-width valleys, shoulders, shelves, scarps, gullies, basin pauses, and openings. Keep the first playable area's no-deep-canyon rule unless the user changes it. In a normal-content Development Player, profile startup, steady traversal, and chunk crossings separately, including terrain integration, decoration, renderers, triangles, shadows, mesh-collider cooking, memory, and frame percentiles. Do not infer full-content performance from a fast-terrain or decoration-disabled profile.

## Acceptance and stop conditions

- A fixed-camera comparison demonstrates a substantial improvement over the current terrain in at least two approach directions and three distances; the current approved broad shapes remain recognizable.
- Valley lows, rims, toes, shelves, and debris sources cross chunk boundaries without a visible or semantic discontinuity.
- A face's visible contact agrees with its authoritative collider. Vertical climbing is offered only where a matching collider, clearance, and top-out exist.
- Booter and BigARM retain their declared route connectivity, including when the detailed chunk is unloaded. No decorative feature silently blocks a reserved route.
- Stable IDs, plan hashes, unload/reload, local-origin rebase, and saved-place/version behavior pass focused checks.
- Full-content Development Player profiling shows that the chosen representation fits the agreed target hardware budget. The target machine, resolution, preset, and frame-rate threshold remain a product gate rather than invented numbers.
- The user accepts the production-camera look and hands-on traversal. Automated checks and a technical screenshot do not substitute for that decision.

No gameplay smoke test is part of agent validation unless the user explicitly requests one. The user's hands-on review is recorded as a separate acceptance gate.

## Research basis

- Génevaux et al., [Terrain Modeling from Feature Primitives](https://perso.liris.cnrs.fr/eric.galin/Articles/2015-terrain-from-primitives.pdf): hierarchical feature primitives for valleys and ridges, combined by carving and blending. This supports a planned parent landform rather than independent slope props.
- Génevaux et al., [Terrain Generation Using Procedural Models Based on Hydrology](https://perso.liris.cnrs.fr/eric.galin/Articles/2013-river-networks.pdf): connected networks and valley profiles. Used as shape grammar for dry ancient landforms, not as an active-water rule.
- Unity's [Survival Kids terrain workflow](https://unity.com/blog/level-layout-and-terrain-workflows-in-survival-kids): authored cliff modules, continuous terrain tops, simplified collision, and low-density context-aware small objects. Its fixed-level layout is not the streaming authority here.
- [USGS talus definition](https://apps.usgs.gov/thesaurus/term-simple.php?code=1.5.5&thcode=4) and [USGS rockfall study](https://www.usgs.gov/publications/rock-fall-dynamics-and-deposition-integrated-analysis-2009-ahwiyah-point-rock-fall): face-derived toe debris and occasional farther runout motivate the source-linked field.
- Unity [mesh-collider performance guidance](https://docs.unity.com/en-us/engine/6000.3/manual/physics-section/physics-overview/physics-optimization/cpu/collider-types): simplified, pre-cooked static collision is preferable for bounded physical features; runtime cooking must be profiled.
