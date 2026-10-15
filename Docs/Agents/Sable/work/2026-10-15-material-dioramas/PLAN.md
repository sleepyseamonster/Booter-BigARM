# Material diorama comparison plan

Sable, October 15, 2026. **Status: implementation authorized; audit corrections incorporated.** Authority: the author requested this plan, reviewed its audit, then explicitly directed implementation as a goal. Creative candidates remain proposals until visual review.

## Intended result

Build **three authored dioramas in four surface treatments: twelve comparable scene versions**. Each scene should feel like a small place, with composition, substantial objects, believable material transitions, and purposeful clutter. A separate material plate makes every surface easy to inspect without atmospheric lighting hiding it.

Use the accepted **B ground and restrained B–C rocks/props** as the preferred-direction candidate, not as a final answer imposed on the new comparison. The author's selection remains controlling until they review alternatives. See [accepted comparison](../2026-10-15-stylized-realism/STUDY_RESULTS.md), [expanded references](../2026-10-15-stylized-realism/EXPANDED_GAME_STUDY.md), and [world basis](../../../../WORLD_BASIS.md).

The initial planning goal is complete. The current implementation goal delivers an offline study and evidence, not production integration. See [AUDIT.md](AUDIT.md) for the pre-implementation findings and [IMPLEMENTATION.md](IMPLEMENTATION.md) for the bounded execution revision and its proof requirements.

## Three places to compare

The names and object arrangements are study concepts, not accepted lore or production locations. Use roughly 18 by 16 metres per scene, with a larger surrounding ground apron so all review angles stay inside the dressed environment. Adjust framing after the blockout rather than forcing a cropped hero object into a fixed footprint.

| Diorama | Composition and objects | Material questions |
| --- | --- | --- |
| **Salvage workyard** | A dismantled pump or winch on a metal service skid, a low metal work stand, cut plates, cast housing, pipe sections, fasteners, ceramic containers, sorted stone samples, and compacted paths. Put the machine off-center, leave one clear approach, and cluster small items around the work area | Can processed, bare, blackened, and rusted iron stay distinct? Does clutter feel used and sorted rather than randomly scattered? |
| **Collapsed service gate** | A broken limestone masonry arch on granite footings, a basalt boulder beside the opening, a failed iron brace, fallen blocks, buried metal fragments, and dust accumulated at the wall base. Compose a readable route through the gap and a quieter area beyond | Do masonry, natural rock, metal, and dirt separate under warm light? Do chipped edges have thickness and convincing contact? |
| **Quarry relay cut** | A basalt outcrop behind an abandoned relay base or small derrick, stacked transported granite blocks and cut limestone slabs, ore specimens, a dragged metal sled, loose fragments, and a broad dry ground shelf | Can natural stone families, large machinery, and small rubble share a coherent scale? Do the materials still read in partial shadow and an alternate orbit? |

Mixed rock families appear as transported samples, construction stone, and stockpiles. Do not imply that three unrelated rock types naturally form one interchangeable outcrop. Ground shapes are small authored study meshes, not a modification or replacement of Greater Wasteland terrain.

Each scene uses the same reusable kit, differently arranged. All requested material families appear in each scene, but receive different emphasis; a small sorting tray or off-route stockpile can hold secondary samples. No glowing material labels or showroom spheres inside the beauty composition.

## Material inventory

Create **15 labelled surface conditions per style**. Labels describe the study surface, not a new game resource or crafting recipe. Include bare worked iron and a distinct ore specimen so “raw iron” is covered without confusing unprocessed ore with manufactured metal.

| ID | Surface condition | Representative object and visual target |
| --- | --- | --- |
| M01 | Clean processed ferrous metal | Cut plate and machined rim; directional finish with restrained reflection, visible edge thickness |
| M02 | Brushed processed metal | Panel or sleeve; directional abrasion and a broader highlight than the clean rim |
| M03 | Bare forged iron | Bar, bent strap, or tool blank; hammer variation and dark rough areas, distinct from red rust |
| M04 | Blackened cast iron | Pump housing or wheel; cast texture, substantial section thickness, restrained polished contact points |
| M05 | Partially rusted iron | Plate with exposed substrate, intact dark areas, and localized oxide; identical wear placement across styles |
| M06 | Heavily rusted iron | Failed brace or scrap; broader oxide coverage, pits, and a few exposed edges; avoid uniform orange paint |
| S01 | Dense volcanic basalt | Dark broken boulder with coherent fracture and readable broad planes |
| S02 | Porous volcanic stone | Separate scoria-like sample with visibly larger pores; do not make all volcanic stone equally bubbly |
| S03 | Granite-type stone | Block and natural fragment; coarse mineral grouping, restrained crystalline variation, different from basalt |
| S04 | Limestone | Cut block and broken face; pale warm body, subtle layering and chalky variation, distinct from marble veining |
| G01 | Compacted dirt | Quiet traversable ground, preserving the approved B treatment in the preferred candidate |
| G02 | Loose dry fines | Dust pockets and edge deposits; finer visual scale than G01 without a noisy blanket |
| G03 | Gravelly dirt | Ground with mixed-size clasts and contact fines; actual larger stones rather than every pebble painted flat |
| O01 | Iron-rich ore specimen | Clearly labelled mineral-bearing rock on the sample tray; visibly separate from M03, without claiming measured mineral composition |
| C01 | Ceramic control | Reuse the earlier canister idea with coherent glaze and chips, providing continuity with the first study |

Keep stone, ceramic, oxide, and bare metal behavior distinct. Rust coverage must affect the surface response as well as its color; do not make every rusty region reflect like exposed metal. These are art-authoring requirements, not calibrated laboratory material values. The [Principled material documentation](https://docs.blender.org/manual/en/5.0/render/shader_nodes/shader/principled.html) is a technical reference to check against the installed Blender version during execution. [USGS rock/mineral definitions](https://www.usgs.gov/faqs/what-difference-between-a-rock-and-a-mineral) help keep ore, minerals, and rock labels distinct.

All surfaces remain dry. Rust is a requested surface condition, not evidence of a newly introduced active water cycle. No vegetation, puddles, rain streak animation, lush landscape, or conventional noon sky enters these scenes.

## Four treatments

These names describe original study candidates informed by references. They do not claim to reproduce proprietary game shaders or exact studio styles.

| Candidate | Treatment | What this comparison should reveal |
| --- | --- | --- |
| **A — Grounded realism** | Believable material separation, fine structure at plausible scale, restrained realistic wear and dirt | How much material information we want to retain |
| **B — Painted realism** | The accepted ground B, with B–C balance on rocks and props; medium-scale marks, edited grain, believable surface response. Dishonored 2 guides the material balance | Whether the current direction works across a complete material family and dressed scene |
| **C — Expressive painterly** | More visible Arcane / No Rest for the Wicked-inspired marks, broader plane accents, selected sharp edges, simplified secondary variation | Where painting strengthens the scene and where it starts obscuring material identity |
| **D — Graphic simplification** | Risk of Rain 2-inspired color grouping, reduced microdetail, simpler surface accents, strongly organized values | Whether the author prefers more surface economy than the present B–C balance |

For the first comparison, **geometry, prop arrangement, stone distribution, material assignments, wear locations, dust coverage, camera, and lighting match across styles within each scene**. Surface color treatment, texture frequency, brush character, and fine relief may change deliberately. Record these as multi-variable material candidates, not a scientifically isolated brush-width test.

Keep clean metal clean and heavily rusted metal heavily rusted in every candidate. Keep chips, scratches, and oxide boundaries in the same places. Match broad color/value intent well enough that simply making a version brighter does not become its apparent advantage. Do not force dark basalt and pale limestone to have equal brightness.

Do not use a global oil-paint postfilter to create B or C. The common-geometry material matrix preserves all meshes. A separate workyard whole-style pilot adds documented rock-plane and selected machinery-proportion changes for C/D, with A/B retaining the common form. It uses its own matched views and is explicitly not texture-only evidence.

## Asset and texture authoring

Begin with a source inventory and hash manifest. The bounded filename search found existing basalt FBX/maps and volcanic textures under `Assets/_Project/Art/Environment/Rocks/`, and existing camp iron/metal materials under `Assets/_Project/Materials/TopDown3D/`. Inspect their actual appearance and texture provenance before reuse; a material name is not proof that it meets the brief. Existing baseline dirt is recorded in the first study's [receipt](../2026-10-15-stylized-realism/render_receipt.json). Granite, limestone, and metal finish sets still need explicit source selection or authoring.

- Work toward roughly 25–35 reusable meshes, instanced into about 60–100 visible objects per scene. These are planning targets, not production budgets. Let gameplay-scale readability determine useful clutter density.
- Include plates, bars, housings, bolts, pipe ends, cables, metal containers, ceramic pieces, rock fragments, and broken masonry. Use metal or stone supports; do not add wooden workbenches by habit.
- Establish hero, secondary, and fine detail sizes. A loose fastener pile should read as a cluster at distance; reserve individual screws and crystalline grain for close inspection.
- Use coherent UV scale, dilation, seamless tiling where required, and aligned directional marks. Inspect the far side of every hero rock and prop. Retire the first study's conspicuous atlas transitions and repeated dirt pattern.
- Author or source base color, roughness, normal/height, and metal/oxide masks as appropriate. Document color-space and channel conventions. Do not bake a fixed directional highlight into every albedo.
- Prefer deterministic material construction and fixed masks for consistent comparison. If generated image drafts are useful, record tool, prompts, reference images, exact output hashes, tiling repairs, and derivative maps. Identify them as generated drafts rather than manually painted finished textures.
- Use existing approved resources or sources with a checked license. No purchase, package installation, third-party artwork redistribution, or automatic production adoption is included.

## Lighting that reveals the surfaces

Create one shared lighting rig with two world-compatible presets plus a separate neutral material-plate rig. Use Kena as a reference for grounded light on stylized forms, and the existing research for composed value grouping. Do not make each style look good under a different private light setup.

| Rig | Setup and purpose |
| --- | --- |
| **Brighter sunset** | Low warm key with long shadows, broad restrained sky fill, sufficient bounce to see dark metal and basalt. Use a broad reflection source so clean, brushed, and rough metal visibly differ |
| **Deeper twilight** | Lower warm-key intensity and a cooler low-saturation fill, retaining the orange-sky world identity. Preserve the ability to distinguish the same material pairs instead of hiding them in crushed shadow |
| **Neutral material plate** | Large neutral key, broad reflection fill, quiet background, fixed white balance and exposure. Diagnostic setup only; not a new world lighting proposal |

Scene practical lamps may support composition if justified, but their positions, colors, and intensities stay fixed across candidates. Keep atmosphere light enough to see surfaces; no heavy fog curtain, bloom, or depth of field that conceals comparison subjects. Match the rig in scene coordinates and record any scene-specific occlusion.

Lock exposure and color management across style candidates. Qualify sunset and twilight on all four candidates before accepting the capture batch. Use diagnostic plates and a neutral full-workyard comparison to separate lighting from surface design. Review unfiltered color and grayscale companions; grayscale is not the approval image.

## Cameras and capture matrix

Use a shared camera definition per scene, stored numerically. Preserve the serialized elevated-camera reference at 22 m distance, 50-degree pitch, 40-degree yaw, 1.15 m target height, and 48-degree vertical field of view. The study fixes a 16:9 aspect ratio. This is an offline authored-camera proxy: runtime user preferences, obstruction response, look-ahead, and riding can alter the active view. Compose for this reference rather than changing its distance/FOV to fit. Close and orbit views are separately framed and labelled.

1. **Elevated gameplay:** whole composition, clear route, large material patches, and scale reference visible.
2. **Material close view:** stage a grouping containing processed metal, forged iron, rust, and the three main stone families; enough pixels to inspect roughness, grain, and edges.
3. **Alternate orbit:** substantially different azimuth and lower elevation, exposing rear surfaces, UV seams, thickness, and any painted shading that conflicts with the light.

Place an original neutral human-scale stand-in and a metre reference outside the main close-up. Record its dimensions as a study scale aid, not a new canonical Booter height. Do not borrow an actively changing character or the dirty camp scene as scratch content.

Revised capture target: **72 common-geometry scene images**, **12 neutral detail plates** (metal, stone/ore, and ground/ceramic for each candidate), **4 neutral full-workyard diagnostics**, **1 paired accepted-ground control**, and **8 whole-style pilot images** (four candidates, gameplay and close, sunset). **Total: 97 primary review images.** Plates use identical flat/bevelled and curved coupons at matching orientation, grouped at no more than six materials per plate. Derived contact sheets, grayscale views, and crops do not count as additional scene renders.

Use 960 × 540 previews and 2560 × 1440 final review images, preserving 16:9 framing. Inspect fine-detail crops at native pixels. Benchmark the actual scene and available renderer before selecting samples, denoising, concurrency, or storage. Raise quality only where noise or denoising affects the material decision.

## Execution order and gates

| Step | Work | Evidence required before advancing |
| --- | --- | --- |
| 1. Preflight and source selection | Inspect tools, source availability, licensing, ownership, and local storage; inventory all 15 surface conditions | Source paths/hashes, missing-source list resolved, local-only native destination and narrow raw-output exclusion confirmed |
| 2. Scene blockouts | Build all three compositions in neutral gray with the scale stand-in and broad light | Three readable gameplay previews; no floating props, blocked review subjects, or stage edges in camera |
| 3. Material kit | Finish A's material families and the common wear/dust masks; prepare B/C/D variants | Twelve grouped neutral plates; all 15 surfaces present, labelled, and mapped consistently; original-B/new-ground control retained |
| 4. First complete scene | Dress the salvage workyard and compare all four candidates in sunset and twilight | Internal visual review of scene hierarchy, material distinction, seams, metal reflections, and twilight exposure; correct failures before multiplying scenes |
| 5. Remaining dioramas | Dress the service gate and quarry from the sealed shared kit | Matching scene manifests per style, intentional clutter hierarchy, all requested families represented |
| 6. Final capture and packaging | Render the revised 97-image matrix, build matched comparison boards and a local viewer | Complete matrix, matching camera/light/layout checks, inspected images, working viewer, provenance receipt, packed-dependency reopen and reproduction frame |
| 7. Art review and handoff | Record strengths, failures, material-specific recommendations, and unresolved choices | User-facing review can select ground, stone, metal, and overall finish independently; no global style choice forced by one attractive image |

Steps 2–6 include internal quality gates, not a new permission request at every stage. If implementation is requested, proceed through reversible study work within that scope and repair ordinary defects. Stop for a concrete missing dependency or ownership conflict, not merely because a visual iteration is needed.

## Deliverables and ownership

- **Sable:** this plan, material/style matrix, art review, labelled comparison boards, source/provenance manifest, and `comparison.html` beside the eventual study record. A/B/C names from the original study must be clearly qualified; this plan's B is the accepted combined direction, not simply the old B atlas everywhere.
- **Blender lane:** new isolated native scenes, reusable meshes, UVs, material variants, and reproducible background rendering. Proposed local source directory: `SourceArt/Blender/Studies/Sable/MaterialDioramas20261015/`. All `.blend` files and backups stay local-only. No existing source is overwritten.
- **Local large outputs:** proposed `outputs/Sable/MaterialDioramas20261015/` for intermediate and full render sets, with a receipt identifying retained paths. This path is not currently ignored: establish and verify a narrow local exclusion before capture, without hiding unrelated `outputs/` work. Select reasonably sized review boards and viewer derivatives for `Docs/Agents/Sable/work/2026-10-15-material-dioramas/`; do not commit the entire working texture/render cache by default.
- **Babineaux lane, later:** validate any selected treatment in an isolated Unity reference scene using the existing background-safe automation. Offline Cycles images establish an art preference, not URP appearance, performance, or import readiness. Do not add this scene to enabled build scenes or change Greater Wasteland as part of the offline study.
- **Gottspan:** source ownership, integration order, and any later production scope. **Gear Ball:** exact task-owned commit manifests; pushes require current authority. These are handoff roles, not authorization to spawn agents or message another chat.

Receipts should record asset hashes, stable study object IDs, transforms, source/license notes, texture resolutions and physical scale, material settings, camera matrices, light settings, color management, renderer version, samples, and render hashes. Reopen the saved native source and verify the inventory before claiming reproducibility. Reuse the earlier [capture/packaging pattern](../2026-10-15-stylized-realism/tools/package_comparison.py) where useful; its hard-coded three-candidate assumptions must be updated for the new matrix.

## Review and completion criteria

The study is ready for the author when all three scenes feel deliberately dressed, the four candidates are visibly distinguishable, and the following can be judged from matched images:

- Clean/brushed metal, bare forged iron, cast iron, partial rust, and heavy rust retain different surface identities under both world lights.
- Basalt, porous volcanic stone, granite-type stone, and limestone are recognizable through structure and texture rather than labels alone.
- Dirt supports the scene, contacts objects convincingly, and avoids obvious repeated motifs or the first study's conspicuous seams.
- Broad painted accents reinforce forms; they do not turn granite into colored noise, limestone into marble, or rust into orange metallic paint.
- The scale stand-in, clear route, and small useful props remain legible. Details can recede with distance without making every material identical.
- The alternate orbit reveals no hidden unfinished side, stage edge, severe texture seam, or contradictory baked highlight.
- Images and the viewer are visually inspected, and matrix/hash checks pass. Relevant asset checks are separate from visual approval; no gameplay smoke tests are required for this offline study.

Record recommendations separately for ground, each stone family, manufactured metal, weathered metal, clutter density, and lighting. A mixed outcome is valid: the author may keep B ground, choose C rocks, and prefer more restrained metal. No numerical style blend is assumed.

## Integration and preservation boundaries

The dioramas use fixed authored placement. Deterministic **study** reproduction uses explicit object IDs, saved transforms, fixed seeds where procedural detail is used, and recorded sources. This is not an integration with production world identity or generated gameplay-object IDs.

Greater Wasteland remains fixed and always loaded. Terrain generation and terrain streaming are out of scope. Dressing-cell unload/reload, persisted runtime deltas, and saved-world migrations are not applicable because the study introduces no runtime services, gameplay interactions, or save data. Authored constraints are the study's camera routes, object contacts, placement bounds, and protected original assets. A later Unity handoff must reassess these concerns if its scope changes.

Preserve all unrelated dirty work, Unity GUIDs and metadata, current camp/character assets, existing sources, packages, and project settings. Work stays on `main`; no branch/worktree operation or external publication is implied. All future execution remains background-only and preserves the user's foreground application.
