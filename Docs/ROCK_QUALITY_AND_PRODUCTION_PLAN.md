# Rock Quality And Production Plan

**Status:** user accepted the improved gravel and deferred blowing sand on 2026-09-06. Blowing sand is off. Next: preserve the mixed patch's current ground settings, then prepare production integration through the existing world owners.

## Goal

Create excellent, readable rocks through one simple Unity hierarchy workflow, then translate the accepted shape language into deterministic editor-baked production assets for the procedural world.

## Current next step — blowing sand deferred, 2026-09-06

Independent-texture correction: user rejected the second layer sharing the original gravel's shapes. New built-in image generation produced `NearRockPebbles_Albedo.png` and a matching interpreted `NearRockPebbles_Height.png`, with a distinct layout of irregular nodules and gaps. The second layer now uses those new heights for silhouettes/relief and new albedo for restrained mineral variation, while retaining the actual rock base texture and nearest-rock tint for palette continuity. It remains ABOVE sand and confined near rocks; the original gravel textures and underlying sand behavior are unchanged. Both new resources share the existing pebble sampler, so custom sampler count stays four. Prompts, provenance, import settings and interpretation limits are in [NearRockPebbleTexturePrompts_2026-09-06.md](./Evidence/WorldCreator/RockCompositionReference/NearRockPebbleTexturePrompts_2026-09-06.md). The earlier same-height-pattern implementation described below is superseded, not the shipped outcome of this batch.

Layer-order clarification: the user explicitly wants the NEW rock-colored pebble layer on top of the sand. It is applied last, after sand and original gravel, without sand-depth suppression. Its height-derived relief follows the already displaced ground and its proximity/patch mask still limits coverage near rocks. The original gravel retains its existing deeper-sand suppression. This supersedes the new-layer burial description below; no change to bank geometry or the real clutter-rock burial is implied.

Additional near-rock pebble layer: user requested a second textured layer concentrated around generated rocks and matching their texture colors, while retaining the accepted gravel. The mixed authoring adapter writes nearest ground-contact rock tint plus a 0.6 m falloff into UV3, preserving UV2 gravel/bank channels. It binds the reference rock material's existing base-color texture; the shader samples this with the nearest rock's tint on an independently offset pebble-height pattern, after the original gravel layer. Both layers retain local anti-repeat sampling, height-derived relief, and deeper-sand suppression; the new layer is patchy and does not form a continuous painted halo. This matches base texture/palette, not the rock shader's complete triplanar/mineral/crack/dust treatment. The current reference uses one shared rock material; different per-rock texture sets would need explicit handling later. Ground Clutter controls both layers; no extra hierarchy root or mesh stones are added. One texture resource reuses the existing base-map sampler, retaining four terrain sampler states. Layer sampling increases near-rock shader work; performance and visual blending await review. This remains an editor-only binding with no runtime generation/version, streaming, identity, or persistence changes; rocks, bank geometry, original maps and off blowing-sand state are preserved.

Clutter-shape verification: isolated Unity 6000.4.0f1 import/C# compilation passed with exit 0 (`/tmp/booter-soft-clutter-compile.log`). Source review checked positive scale, retained bottom-based placement and unchanged candidate/exclusion logic. No gameplay, visual, or automated test suite was run; silhouette/height and performance remain user review items.

Latest clutter-shape correction: the user rejected the repeated hexagonal, flat fragment silhouettes and requested softer shapes with taller large pieces. The preview now uses the approved Nodule family's LOD2 meshes, retaining their irregular silhouettes and smooth normals, instead of the old Pebble prism meshes. Existing source/catalog assets are not rebuilt or modified. Horizontal span still follows the accepted 7–42 cm size mix, with restrained independent aspect variation. Pre-burial height is 32–50% of size for small pieces, 50–70% for medium, and 75–95% for large, replacing uniform vertical flattening. The existing depth placement remains, so larger fragments expose more volume without floating. Pocket candidates, density, yaw, exclusion rules, material, sand geometry, gravel texture and blowing-sand state are unchanged. Nodule LOD2 is more detailed than the old Pebble meshes (variant 0: 1,800 versus 36 triangles); the 192-member combined-preview cap remains, and production LOD/performance integration is not implied. This changes editor presentation only, with no world identity/streaming/persistence changes. Review silhouette softness and exposed height before capture.

Latest surface correction: user likes the sand banks but reports the gravel texture disappeared, and requests a somewhat more orange-red earth color for the banks. Preserve bank geometry and rock transforms. The broad terrain sand weight had both selected pale swept sand and suppressed pebble detail over the patch. The authoring terrain now stores actual added-depth coverage in UV2.y (smooth 0–25 cm), while clutter writes only proximity into UV2.x and preserves that depth channel. Both shader paths suppress gravel by this dedicated deposit mask instead of the broad terrain sand signal; exposed ground retains full admitted gravel, deeper banks retain only 15 percent. Bank sand/transition albedo blends 55 percent toward the existing local base-earth sample at full depth coverage, preserving texture detail without new maps or sampler states. This changes authoring material blending only: bank heights/normals/collision, clustered real stones, seed/streaming/identity/persistence, and blowing-sand state are unchanged. Rebuild once to populate the new depth channel; visual acceptance remains user-owned.

Surface-correction verification: isolated Unity 6000.4.0f1 import/C# compilation and six standard/fast Metal lighting variants passed with no shader errors and exit 0 (`/tmp/booter-bank-gravel-blend-compile.log`). No gameplay, visual, or automated test suite was run. User scene/material files remain untouched.

Latest sand revision follows the user's scene/paintover/arrow references: deposited banks should rise into selected lower rock faces and slope outward, rather than sit as shallow surrounding skirts. The authoring adapter now retains triangle-section edge pairs near the anticipated bank crest, not just their bounding ellipse. The existing authored-deposit sampler measures outside distance against that contour (including concavities), peaks at contact, and tapers to a broader gentle toe. Seeded spatial variation and a restrained wind bias create uneven coverage; directional wakes are subordinate to the contact bank. Only the two strongest overlapping deposits contribute to a small bounded join, filling close inter-rock pockets without summing every pile member. Suspended upper members still do not create separate ground banks. Nearby disposable terrain is refined to about 0.1 m; heights, normals, sand coverage, and collision use that same mesh. Existing clutter rebuilds afterward against the new surface, including its sand suppression and partial burial. No rock transforms, source shapes, texture assets, or blowing-sand state are changed. This is an authoring-only contour heuristic, not simulated sediment flow or a volumetric coating of rock faces. World seed/version, streamed ownership, member identities, and persisted deltas remain unchanged. The existing Sand Buildup control remains the only sand-height control; review the result before further tuning or capture.

Sand-bank verification: source review confirmed the authored-deposit entry point is consumed only by the editor adapter. Isolated Unity 6000.4.0f1 import/C# compilation passed with exit 0 (`/tmp/booter-sand-banks-compile.log`). No gameplay, visual, or automated test suite was run. Bank height, contour contact, join appearance, and rebuild performance remain user review items, not compiler-proven results.

Latest clutter correction: the user rejected the denser grid-based scatter as polka dots. This supersedes the area-wide stone-density adjustment below. Real clutter now forms two seeded, asymmetrically placed pockets at each ground-contact rock's bounds edge; upper pile members are excluded as pocket sources. Each pocket has bell-shaped positional jitter rather than a uniform ring, with round-robin candidate attempts across pockets before the shared 192-stone cap. Candidate size mix is 55% small (7–16 cm), 30% medium (16–28 cm), and 15% larger (28–42 cm), each bell-shaped within its range; rejection can change the final proportions. Larger fragments use existing Pebble LOD1 meshes, smaller fragments LOD2. Size-aware rock exclusions and minimum inter-fragment spacing limit overlap. Sand suppresses some members and increases their burial instead of erasing all pockets beside deposited skirts. The texture/height sampling, large-rock composition, ground geometry, and off blowing-sand setting are unchanged. This remains disposable editor authoring, seeded by world seed plus source rock seed/index and pocket/attempt, with no new runtime identity, streaming, or persistence ownership. Bounds-based contact/pocket placement remains a visual heuristic; review clustering and scale before capture.

Clustered-clutter verification: source review and isolated Unity 6000.4.0f1 import/C# compilation passed with exit 0 (`/tmp/booter-clustered-clutter-compile.log`). No gameplay, visual, or automated test suite was run. Rebuild through Update Rocks & Ground for user visual review; scene and material edits remain preserved.

The user directed that blowing sand remain off and work move on. Its earlier 0.35 default is superseded by zero, with a one-time reset of existing preview settings. The slider remains available for later opt-in; animation acceptance is no longer a gate for the static patch. Rock shape, arrangement, burial, deposited sand, and clutter are not retuned.

The user subsequently requested more ground-clutter rocks before the capture. Real surface-stone placement probability is raised from 0.16 to 0.48 times the existing proximity/Clutter mask, and the combined-mesh cap from 64 to 192. This targets approximately three times as many accepted candidates, not a guaranteed exact count: slope, sand coverage, rock exclusions, and the cap still apply. The candidate grid/seed, 5.5–16 cm size range, burial, material, texture coverage, and large-rock arrangement remain unchanged. Blowing sand stays off. This is editor-preview density tuning only, with no production identity, streaming, or saved-world changes. Review the denser patch, then save the scene for capture.

The same request was extended to reduce recognizable repeats in the pebble texture. Pebble-only sampling now blends four local world-space mappings with independently hashed rotations and offsets at the existing 1.25 m scale, replacing the two fixed mappings whose blend varied over roughly 13 m. Squared smooth weights favor crisp stone shapes outside transition regions. Color and height share transforms/weights; normals derive from the blended height, and rotated explicit texture gradients preserve mip selection across cell boundaries. This leaves original image assets, terrain layers, coverage masks, and the four shared sampler states unchanged. Cost rises from two to four texture fetches per pebble sample; existing distance relief fading remains. Appearance, blend softness, and runtime performance are not established by compiler checks and remain review items.

Clutter/anti-repeat verification: isolated Unity 6000.4.0f1 import/C# compilation and six Metal terrain lighting variants passed with no shader errors and exit 0 (`/tmp/booter-clutter-antitile-compile.log`). No gameplay, visual, or automated test suite was run. Update Rocks & Ground rebuilds the higher stone density; the shader change applies on import. User scene/material edits remain untouched.

First preserve the approved mixed patch's actual serialized authoring settings alongside its already captured source formation. Read-only inspection found no Mixed Formation Ground root or its burial/sand/clutter fields in the saved TopDown3D scene files at this checkpoint. The user must save the current scene before an exact capture; do not infer Inspector values from screenshots or substitute defaults. Do not overwrite the live scene from a batch-mode copy.

After capture, scope the production handoff around the existing World Creator formation planner, baked catalog, and terrain representation pipeline: retain authored member/support relationships and stable member IDs; fit rocks to immutable base ground; derive deposited heights and clutter from the frozen placed rocks; share the final surface between rendering and collision. Check chunk ownership/border evaluation and unload/reload before enabling production placement. Any generation-version or persisted-delta compatibility change must be explicitly identified, not silently folded into visual tuning. This is the next integration direction, not a claim that the editor patch already ships. No new runtime world authority, automatic scatter/pile reference, or further blowing-sand work is authorized by this handoff.

Crimson Desert is the primary visual model. Horizon Forbidden West supports artist-readable authoring and terrain integration. Path of Exile 2 supports elevated-camera silhouette and material readability. These references guide quality and composition; their assets are not copied.

## Accepted Golden Rock calibration

On 2026-09-05 the user selected a good standalone rock and supplied front, reverse, and top-down views. The useful result came from a two-mass `Blocky Monolith` seed laid almost fully onto its side, with a smaller secondary tilt. The root edit was evidence of a missing generator posture, not a reusable transform requirement. After generating a wider set, the user confirmed that this grammar consistently produces liked rocks.

Captured authoring values:

- seed `2126351350`;
- reference dimensions `1 m` wide by `0.75 m` long before the resting-pose bake;
- lopsidedness `0`, compaction `0`, major fractures `0`, edge damage `1`;
- former root rotation approximately `(-18.374, 147.048, -85.817)` degrees;
- fusion smoothness `0.0657`, surface relaxation `0.45`;
- Dark Fractured Desert material with geology scale `2.4`, surface variation `0.346`, crack amount `0.36`, side grit `0.417`, underside shale `0.905`, side/top shale patches `0`, and worn shine `0.099`.

The generator now bakes that rotation into the editable source volumes and performs a second grounding pass afterward. Creating a Workbench assigns a fresh seed. Width and body length are independently sampled from `0.3–1.2 m` using the mean of three seeded uniform samples: the distribution is bounded and bell-shaped around `0.75 m`, so the middle is common and either limit is rare. The user raised the lower limit from `0.2 m` to `0.3 m` on 2026-09-06. Each seed also receives a stable random turn around the vertical axis and a restrained burial depth of four to twelve percent of its posed height. The Workbench root returns to zero rotation and unit scale. This preserves the raised shoulder and sloped body, avoids a repeated facing direction, and produces controlled size/contact variation without manual root edits. The user visually accepted the original combined result on 2026-09-05, completing the Golden Rock gate.

## Done

The rock lane is complete when:

- one user-shaped Golden Rock establishes the accepted silhouette, fracture, edge, grounding, and material language;
- that accepted language is captured by deterministic editor recipes rather than runtime mesh generation;
- the production baker emits reusable LOD meshes, colliders, and material-ready catalog entries;
- `TopDown3DWorldGenerator` remains the sole runtime world and placement authority;
- the accepted family is readable from the production camera before broader family rollout.

## Architecture

```text
Rock Workbench
    -> editable source volumes in Rock Shape (Edit These)
    -> user-shaped Golden Rock
    -> deterministic editor recipe
    -> TopDown3DProductionRockBaker
    -> baked LOD meshes + collider + material catalog
    -> TopDown3DWorldGenerator placement
```

The Rock Workbench is an editor authoring tool, not a runtime world system. Its temporary preview mesh may use editor-only implicit-surface meshing. Shipped runtime rocks come from the existing baked catalog. Runtime placement uses stable world identity, absolute-coordinate sampling, chunk ownership, and the existing persisted-delta boundary.

## Explicit exclusions

- no separate Grounded Geology window, A/B comparison mode, or diagnostics hierarchy;
- no runtime SDF, marching-tetrahedra, rock-recipe, terrain-uplift, sediment, talus, or geology-field service;
- no second terrain, placement, streaming, persistence, or material authority;
- no bulk regeneration of production families before the Golden Rock is accepted;
- no new formation or landmark categories during the Golden Rock gate.

## Correct sequence

### 1. Recover one authoring path

- Use the direct top-level `Booter & BigARM > Create Rock Workbench` command. Keep `GameObject > Booter & BigARM > Top Down 3D > New Random Rock` as the hierarchy shortcut.
- Keep the normal Inspector limited to physical size, lopsidedness, compaction, major fractures, edge damage, editing-volume visibility, and the two generation actions.
- Put manual source objects beneath `Rock Shape (Edit These)`.
- Bake the accepted standalone-rock `0.1` size into meter-valued settings once and keep the reusable root at `(1, 1, 1)`.
- Use the existing project rock material and surface controls; do not substitute diagnostic textures.

### 2. User builds the Golden Rock — stop gate

Complete. The user accepted the Golden Rock shape language, bounded size distribution, seeded facing, and burial-depth variation across multiple generated rocks on 2026-09-05.

The Golden Rock should answer only these visible questions:

- Is the large silhouette strong from the production camera?
- Do one to three major fractures create structure rather than noise?
- Is edge damage subordinate to the silhouette?
- Does the base look planted rather than balanced or floating?
- Does the normal project rock material read correctly at the accepted physical scale?

### 3. Capture the accepted grammar

After user acceptance, add the smallest non-destructive editor capture needed to preserve the Golden Rock's source shapes, roles, transforms, surface preset, dimensions, and seed as a deterministic recipe. Manual source objects remain intact. The recipe is authoring data, not runtime state.

### 4. Bake one representative production family

Feed the accepted recipe into the existing production baker. Emit the existing LOD/collider/catalog format and keep imported/source data separate from generated runtime assets. Do not replace all 27 families yet.

### 5. Integrate through the existing catalog

Let the existing World Creator selection and placement paths consume the baked representative family. Generated-object identity, chunk unload/reload, authored placement constraints, and persisted runtime deltas remain unchanged because runtime architecture is not being replaced.

### 6. Expand only after the representative family is accepted

Derive a small, purposeful family set from the accepted grammar. Then tune distribution, grounding, contact treatment, and terrain transitions at composition scale. Formations and landmarks resume only after individual-rock quality is stable.

## Proof boundary

Source review and compile safety can show that the tool is coherent. They cannot approve appearance. The user owns Scene/Game visual acceptance and hands-on testing. No automated screenshot, A/B scaffold, or test count substitutes for the Golden Rock decision.

## Completed family expansion

Steps 3–5 are implemented: the saved `ApprovedBoulderFamilyRecipe.asset` captures the accepted source, the existing baker emits three Boulder variants with LODs and collider bounds, and World Creator consumes those catalog slots. Subsequent user-directed corrections preserved recipe burial, unified the rock surface color, and increased formation burial and distribution (`d3cc446`). These are implementation facts; visual acceptance remains user-owned.

On 2026-09-06 the user directed continuation of the plan. The next bounded step is to extend the accepted grammar into the existing `Slab` and `Nodule` roles, which still use older mesh shapes alongside the approved Boulders. Use the same saved source and existing **Build Approved Rocks Into World Creator** command. Slabs lower and broaden the accepted mass; nodules make it more compact. Apply the same derivation to all three LODs around the source ground plane, retaining fractures, seeded variation, burial, and source geometry. These are initial derived shapes for user review, not new hand-authored reference rocks.

Scope is nine existing catalog slots: three variants each of Boulder, Slab, and Nodule. Keep existing mesh GUIDs and catalog IDs, derive collider bounds from the new meshes, and preserve the other 18 slots. World seeds, chunk ownership, generation versions, unload/reload, and persisted-delta schemas remain unchanged. Mesh-bound placement can adjust to the new silhouettes through the existing planner; no new runtime authority is introduced.

Stop when these three roles are baked into the current catalog and the simple build command reproduces them. Validate compilation, mesh/LOD/collider references, source preservation, and transformed normals. Leave appearance to the user. Do not advance to formations, landmarks, further density tuning, or terrain systems in this batch; composition is the following plan step once this family set is visually settled.

Implementation checkpoint: the existing command now bakes all nine slots. Six Slab/Nodule variants (18 LOD assets) and their catalog collider bounds were updated; Boulder geometry, all mesh GUIDs, the source recipe, and production materials were preserved. Unity 6000.4.0f1 compiled and baked successfully in an isolated copy, including the catalog/LOD validator. The five focused baker checks and five canonical formation checks passed. No gameplay or visual test was run. The next user-facing work is formation composition with this small family set, after visual review; further individual-rock tuning is not the automatic next step.

## Accepted composition and current stop condition

On 2026-09-06 the user built and accepted a mixed pile/scatter arrangement in `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`, not the sandbox. The supplied [top view](./Evidence/WorldCreator/RockCompositionReference/MixedPileScatter_Top_2026-09-06.png) and [side view](./Evidence/WorldCreator/RockCompositionReference/MixedPileScatter_Side_2026-09-06.png) are retained as visual authoring references. They show a dense, raised central group, smaller satellite pairs, isolated stones, unequal spacing, and open ground between groups. This is acceptance of the rock composition; sand, slope integration, and clutter are still to be developed.

The user requests three distinct composition types:

- **Open scatter:** more separation and exposed ground between individual rocks and small groups.
- **Mixed pile and scatter:** preserve this accepted arrangement as the reference, including the compact center and outlying rocks.
- **Rock pile:** tighter concentration and more supported stacking than the mixed reference.

The user subsequently saved the prototype and clarified that **open scatter and rock pile will be hand-built separately by the user**. Do not derive or generate those references automatically. Only the mixed composition is active now.

The saved source has been preserved as [MixedPileScatterReference.prefab](../Assets/_Project/Art/Environment/Rocks/Source/MixedPileScatterReference.prefab), with persistent meshes in [MixedPileScatterReferenceMeshes.asset](../Assets/_Project/Art/Environment/Rocks/Source/MixedPileScatterReferenceMeshes.asset). It contains 19 independently editable Rock Workbench members and 38 source volumes. Its rendered bounds are approximately 5.6705 m wide, 6.9391 m long, and 1.1584 m high, centered at `(1.5698, 0.5064, -1.6461)` in the original prototype frame. The prefab root is identity-transformed and tagged `EditorOnly`: this is an authoring reference, not a new runtime formation or production mesh family.

Capture used the saved scene in an isolated Unity copy, cloned the source members without regeneration, and baked their existing source volumes for persistent reference visibility. Reload validation compared all member authoring settings, descendant transforms, active states, and source-volume settings, and checked mesh/collider reference agreement. The original prototype scene and its unrelated saved changes were not rewritten or included in the capture commit. Do not reconstruct exact geometry or dimensions from the screenshots, regenerate the user's members, or move the reference to the sandbox automatically.

## Next bounded batch: mixed formation ground contact

Use the mixed reference for one complete rock-and-ground patch. Ground contact comes first, followed by deposited sand, gravel/pebble material depth with sparse protruding mesh stones, and finally visible blowing sand. The current batch begins with the following integration constraints:

- Preserve the central pile's support relationships and the outlying rocks' horizontal spacing. Share slope orientation within contact groups, but sample burial separately for each ground-contact rock. Upper rocks follow their supports rather than being flattened to the ground. Never overwrite the accepted reference.
- Keep `TopDown3DWorldGenerator` as the adapter to the existing World Creator surface query. Rock plans already depend on that query, so terrain sampling must not recursively ask the same rock planner for its own height. Establish the base-ground/contact ordering before adding rock-driven sand heights.
- Use the existing semantic wind and surface-material inputs for ground coverage. The currently disabled deposited-dust decorator is a visual overlay without collision; simply enabling it does not satisfy the ground-contact outcome.
- Any substantial deposited-sand height must feed the same surface used for terrain rendering, normals, collision, and subsequent placement. Keep world seed/version ownership, absolute coordinates, chunk borders, unload/reload, and persisted deltas in the current runtime owners.
- Tiny embedded gravel belongs in coordinated ground color, height, normal, and roughness textures; reserve sparse meshes for visible protrusions. Extend the existing terrain material instead of introducing a separate clutter world or duplicating materials by formation.
- Expose only controls needed to shape this mixed patch in the existing authoring workflow. The captured prefab remains the untouched reference; do not add an A/B panel or generate the user's future scatter/pile examples.

The next stop condition is one mixed composition seated convincingly against the ground with its central stack intact, followed by coherent sand and clutter treatment. Source and compile checks verify implementation safety; appearance remains user-owned. No broad catalog expansion or new geology platform is implied.

### Ground-contact implementation checkpoint — 2026-09-06

`Booter & BigARM > Create Mixed Formation Ground` creates one EditorOnly hierarchy root in the active scene, reusing the existing landscape terrain-context component. The Inspector exposes Terrain Location (chunk coordinates), one Burial Range slider with shallow/deep handles, Maximum Ground Tilt, and Update Ground Contact. It starts one chunk east of the original reference to avoid overlapping the original rocks. Save the scene after creating the setup; its disposable ground and rock copy rebuild on editor reload and are removed in Play Mode.

The displayed terrain uses the production World Creator near-representation compiler, including production material packing and a matching mesh collider. Contact fitting samples those actual terrain collision triangles. Contact groups share slope orientation, capped at 20 degrees. Per the user's correction, ground-contact rocks settle independently, with bell-shaped burial between the single range slider's endpoints. The shallow default remains 0.035 m; the deep default is now 0.6 m instead of 0.15 m, with the slider allowing 0–1 m. Existing mixed setups receive the deeper endpoint once, preserving their shallow setting and subsequent tuning. Each rock averages three independent seeded uniform samples: middle depths are common and either extreme is rare. Seeds combine the captured rock seed with its saved hierarchy slot, remaining stable on rebuild and terrain-location changes; reordering the source hierarchy can change samples. For small rocks, narrow the range to at most 90 percent of their height before sampling (previously 45 percent), allowing much deeper burial without clipping probability into a spike. Upper rocks inherit their highest supporting rock's vertical displacement, retaining authored support overlap instead of flattening the pile. This remains a conservative bounds-based contact heuristic, not a rock physics simulation.

The source prefab, its persistent meshes, original scene rocks, production catalog, and runtime generation are unchanged. The temporary copy disables automatic remeshing and retains the saved material settings. This batch adds no world-identity/version changes, streaming ownership, or persisted deltas: it is editor-only authoring against the existing world surface. Production formation placement still requires a later integration step; the preview is not shipped placement.

Source review and Unity 6000.4.0f1 compilation succeeded in an isolated project copy. No visual, gameplay, or automated test suite was run. The next implementation batch is deposited sand around this same mixed patch, respecting the shared height/collision constraints above, followed by pebble material depth and sparse protrusions. Do not generate the user's future open-scatter or pile references.

### Deposited sand on the mixed authoring patch — implementation checkpoint

Done for this batch means the existing mixed hierarchy control builds localized sand skirts and leeward tails around seated rocks, with a single Sand Buildup control, matching ground mesh/collision, and the existing terrain material. Extend the existing deposition planner with an explicit authored-obstruction input; do not call procedural rock planning from surface sampling. The sequence is immutable base terrain, rock grounding, frozen exposed-base constraints, then deposited height and material coverage. Do not re-ground the same rocks on their own deposit.

Use current canonical material/wind semantics and absolute positions. Refinement interpolates the existing production triangles instead of inventing another base surface. Shared coordinates evaluate the same deposit across tile boundaries. The source formation stays immutable. All modified geometry is disposable editor context, removed on clear/rebuild/play; runtime world versions, generated identities, chunk streaming and persisted deltas remain unchanged in this authoring-only batch. Production adoption is not implied. Check source and compile in the isolated Unity copy; visual acceptance remains user-owned. Pebble assets, blowing sand, new formation examples, and further tilt tuning are outside this batch.

Implemented: the existing mixed Inspector has Sand Buildup (0–1 m, default 0.36 m) and Update Rocks & Ground. The user liked the initial sand treatment and requested greater height: existing nonzero buildup settings are doubled once, while zero remains off; the old default was 0.18 m. The pre-weight height cap increases from 65 to 95 percent of exposed rock height. Spread, wind direction, source placement and burial remain unchanged. Grounding runs first. Exposed ground-contact rock bounds then become frozen inputs to the existing dust-deposition planner's authored sampling entry point. It uses canonical prevailing wind, sediment supply, deposit, erosion, and slope semantics for skirts and tapered leeward tails. Deposits combine by maximum rather than stacking additive mounds; upper, suspended pile members do not receive their own ground collars. Actual sand heights remain attenuated by those semantic and shape weights.

Only nearby disposable terrain tiles are refined to approximately 0.15 m spacing. Their base positions/materials interpolate the production triangle mesh; deposited height, shading normals, and existing terrain sand/gravel/strata weights are applied together. The renderer and MeshCollider share the resulting mesh. No separate overlay, new material, texture, collider surface, or automatic rock re-grounding is introduced. Zero Sand Buildup plus Update Rocks & Ground restores the base terrain, and rebuilding starts from the source rather than accumulating deformation.

The user then requested that the higher sand sit closer to the rocks. Sand footprints now come from triangle-edge intersections at a low horizontal cross-section of each seated rock, rather than the full renderer bounding box. This follows the exposed base more closely when the rock is widest above ground. The local skirt width is narrowed from 0.22–0.85 m to 0.14–0.55 m; directional tails remain. The original formation, rock placement, burial distribution and tilt are untouched.

Unity 6000.4.0f1 compilation passed in the isolated project copy. Source review covered ordering, mesh ownership, source preservation and boundary sampling; no automated, gameplay or visual tests were run. Runtime production use remains deferred. Next is coordinated pebble/gravel material depth with sparse protruding stones in this same patch, then visible blowing sand; do not resume tilt tuning or generate the user's remaining hand-built examples.

### Pebble and gravel detail — authoring implementation

The existing mixed Inspector adds one Ground Clutter control (default 0.7). Update Rocks & Ground rebuilds in order: base terrain, seated rocks, deposited sand, then ground clutter. No rock or sand geometry tuning is part of this batch. Zero clutter removes both the material detail and sparse protruding stones on rebuild.

The initial analytic pebble pattern was visually rejected: the user's screenshot showed evenly distributed dark dots instead of convincing gravel. That code is removed. Tiny pebbles now use new generated angular-gravel color and corresponding height textures: `MixedGroundPebbles_Albedo.png` and `MixedGroundPebbles_Height.png` under `Assets/_Project/Art/Environment/Ground/SandDirt/`. Source generation used the built-in image tool; full prompts and provenance are in [PebbleTexturePrompts_2026-09-06.md](./Evidence/WorldCreator/RockCompositionReference/PebbleTexturePrompts_2026-09-06.md). This is generated art, not measured scan data. A low-frequency patch mask breaks up coverage; shared world-space sampling keeps color and interpreted height together. Height supplies relief normals and subtle parallax, with restrained height-based smoothness/occlusion. Color retains mipmapped readability at distance while high-frequency relief fades with pixel footprint. Standard and fast terrain paths both use the textures. Sand coverage still suppresses the detail. The existing UV2 proximity mask and renderer-only bindings keep the change on mixed authoring ground; production material assets and the sparse real stones are unchanged. No image is used as a substitute for the actual scene, and in-game quality remains unverified until user review.

Sparse 5.5–16 cm stones reuse the existing baked Pebble LOD2 families, with seeded placement, size and yaw. They are seated into the finished sand collider, avoid the large rocks' bounds, and are suppressed in strong sand coverage. At most 64 are combined into one disposable Surface Stones mesh rather than filling the hierarchy with individual pebbles. These small decorations have no colliders. Their material comes from the accepted reference rock, not a replacement palette.

This remains editor-only: world seed plus fixed spatial cells controls the small-stone placement, absolute coordinates control the material pattern, and the same source inputs rebuild deterministically. Clear/rebuild/play removes all disposable geometry. Production generation versions, entity identities, chunk ownership, and persisted deltas are unchanged; runtime integration remains a later step. Source formation, materials with pre-existing user edits, and both user scenes remain untouched. Blowing sand and the user's separate hand-built scatter/pile examples are excluded from this batch.

Verification: Unity 6000.4.0f1 compiled the C# candidate and explicitly compiled both standard and fast terrain passes on Metal in the isolated project copy. No gameplay, visual, or automated test suite was run. Placement density, material readability and camera-distance appearance remain user-owned acceptance. Next is visible blowing sand in this same patch, without reopening rock shape or tilt tuning.

### Terrain shader sampler repair — 2026-09-06

The user's next screenshot and live editor log exposed a missed keyword combination: instancing, cascaded main-light shadows, additional lights, and soft shadows exceeded Metal's 16-sampler limit. The earlier standard/fast pass check did not cover that lighting combination. The pebble maps had brought the terrain's independent sampler declarations to 16 before URP's lighting samplers were counted.

Compatible terrain texture maps now share four sampler states: terrain color, rocky height, rocky normal, and pebble color/height. Texture resources, sRGB/linear interpretation, and the existing compatible repeat/filter settings are preserved. No textures, lighting features, rock placement, scene settings, or material assets were removed or retuned. This changes rendering resource usage only; deterministic world identity, streaming, generated-object identity, authored constraints, and persisted deltas remain unchanged.

`LayeredTerrainShaderValidator.CompileLightingVariants` now explicitly warms six variants: basic, the reported failing combination, and that combination with additional-light shadows and linear fog, each in standard and fast mode. Unity 6000.4.0f1 completed the isolated Metal compiler check with six warmed variants, no shader errors, and process exit 0 (`/tmp/booter-terrain-sampler-fix.log`). No scene or gameplay tests were run. This establishes compilation, not live visual acceptance; review the restored pebble appearance before proceeding to blowing sand.

### Blowing sand authoring checkpoint — 2026-09-06

The user accepted the improved pebble screenshot and authorized this next slice. Done for this batch means low drifting sand in the existing mixed preview, one control, and unchanged approved rock/ground/clutter appearance beneath it. A new atmosphere system, terrain shader changes, production streaming integration, and the separate hand-built pile/scatter examples are out of scope.

Mixed Formation Ground now exposes **Blowing Sand**, default 0.35, range 0–1. It changes live in Scene view; zero hides the effect without rebuilding terrain. Update Rocks & Ground reconstructs the preview when ground settings change. One disposable child uses 64 paused particles driven at up to 24 Hz, reusing the existing soft dust texture/material helpers and rust tint. No new image assets or shader samplers are introduced. Paths run at roughly 0.45–0.85 m/s with gentle lateral movement, soft lifetime fades and an opacity gust. Their centers follow cached final-ground collision samples about 6.5 cm above the surface. Expanded rock footprints suppress particles and a short downwind mask reduces flow in shelter. This is a bounded visual heuristic, not airflow, erosion, or collision simulation; close side-view contact remains subject to visual review.

World seed and chunk coordinates deterministically generate the path layout; each path samples the same semantic prevailing wind used by deposited sand. Editor elapsed time animates presentation only, with no persisted phase or gameplay delta. The accepted source formation constrains the patch footprint and occlusion. Clearing, rebuilding, closing the scene, domain reload, or entering Play Mode releases the preview and its owned material/texture. Disabled owners stop displaying sand. Runtime world versions, entity identity, chunk streaming owners, and save data are untouched; production integration remains later work after this patch is accepted.

Verification: source review and isolated Unity 6000.4.0f1 import/C# compilation passed, with exit 0. The existing six terrain lighting variants also warmed on Metal without shader errors (`/tmp/booter-blowing-sand-final-compile.log`). Batch mode deliberately does not instantiate this editor-only particle preview; motion/rendering is not verified by compilation. No gameplay, visual, or automated test suite was run. Stop for user motion/readability review before extending the effect or integrating the formation into production generation.
