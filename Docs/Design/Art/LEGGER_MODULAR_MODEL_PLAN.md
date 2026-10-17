# Legger modular model and texture plan

The Legger is one assembled model in one Blender scene, organized into independently editable major components. The author approved implementation on October 15, 2026. Preserve the manually chosen scale, neutral leg alignment, accepted three-section joint targets and ivory, dark-metal and brown-leather palette. Rider contacts follow the author's accepted original through the [rider-area rebuild](LEGGER_RIDER_REBUILD.md). Group small fittings with their parent components rather than creating an object for each nut or bolt. The [neutral alignment receipt](../../Evidence/Art/LeggerLegAlignment20261015/README.md) supersedes the original staggered-pose constraint.

The latest [front body volume continuation](../../Evidence/Art/LeggerFrontVolume20261016/README.md) identifies the current native: **76 visible major meshes, 45,762 control quads, 146,264 evaluated triangles and 55 active packed maps across eleven sets**, with no added topology. Following the author's marked comparison, four existing meshes recover a deeper enclosed neck/lower front body, a fuller wrapped upper housing, longer ivory cheek tips and a deeper forward lower cowling. Seventy-two active meshes retain exact vertex coordinates; sixty-five retain complete mesh/material-name/world fingerprints. Eight Steering parts share a copied material with four revised 2K maps, including normal relief for access covers and seams. Pixels outside the existing body/head atlas regions and an 8-pixel guard remain exact; AO is retained. All UV coordinates, connectivity, modifiers, transforms, rider contacts, cargo, leg geometry, frame/yokes and hose routes remain unchanged. Eleven hidden copies preserve all 1,010 preceding meshes; all 194 preceding packed image payloads remain byte-identical. Native totals are 1,021 mesh objects and 198 packed images including hidden history.

The preceding [front silhouette and panel correction](../../Evidence/Art/LeggerFront20261016/README.md) recovered the broad exposed ivory cheeks, pointed lower fairings, narrower central nose and broader upper cowling. Its three-map head/lens patch and 1,010-mesh/194-image native are the retained starting point for the latest volume continuation; the author then identified insufficient depth and missing front body mass.

The preceding [original-led hose recreation receipt](../../Evidence/Art/LeggerHoses20261016/README.md) records the pre-front source. Six underbody loops, two bar-following control leads and four actuator service hoses reuse their existing quad rings. Twelve named `Hose_` vertex groups select individual runs and their fittings. That pass retained seventy-one complete active fingerprints and changed only registered hose/fitting vertices in five meshes. Structural vertices in the four edited leg-frame meshes, joint targets, transforms, controls, saddle and cargo stayed unchanged. Existing UVs, modifiers, materials and packed maps were retained.

The preceding [top-down and mounting correction](LEGGER_PLANFORM_AND_MOUNTING.md) and [receipt](../../Evidence/Art/LeggerPlanform20261016/README.md) record the pre-hose source. That pass corrected twenty-three existing meshes: staggered lower cases with original-led angles, fitted rack shelves, a tapered rear frame/plating, a forward cooling mass and the rear rider envelope. Main-pocket checked vertices remained within one micrometre of its preceding source. Those major forms are preserved in the hose pass; height remains approximately 2.2147 m without global rescaling.

The preceding [whole-model detail pass](LEGGER_FULL_DETAIL_PASS.md) and [receipt](../../Evidence/Art/LeggerFullRefinement20261016/README.md) remain the retained pre-pass checkpoint. They firmed leg and front/waist plating using existing vertices, exposed the gauge face and added access seams, slots, retainers, grip/contact texture and control graphics in the maps. The current mounting pass preserves those surfaces and all packed image bytes.

Historical implementation checkpoint: all six phases below were completed in the same native file. The [verified modular assembly receipt](../../Evidence/Art/LeggerModular20261015/README.md) records one scene/view layer, 67 visible major meshes, 21,396 quad control faces, 86,072 evaluated triangles and 55 packed maps across 11 texture sets. The earlier source geometry remains hidden for reference; visual acceptance and later Unity integration remain separate.

Author review after this checkpoint found the major forms too simplistic, with the crates the strongest remaining area. The [sci-fi form audit and overhaul brief](LEGGER_SCI_FI_OVERHAUL_AUDIT.md) superseded the instruction below to preserve existing fairing shapes and directed reconstruction toward the retained original. That [applied refinement](../../Evidence/Art/LeggerSciFi20261015/README.md) rebuilt 53 major meshes: 67 visible parts, 36,302 control quads and 123,664 evaluated triangles, with seven revised PBR sets and four retained cargo sets. It preserved the then-current neutral targets. The later [original-leg audit](LEGGER_LEG_GEOMETRY_AUDIT.md) identified the missing third section and incorrect section/foot proportions. Its implementation rebuilt thirty leg meshes and two mounts, rebaked Frame/LeftLeg/RightLeg and reduced the whole asset to 120,056 evaluated triangles. The subsequent [original-frame audit and rebuild](LEGGER_CHASSIS_REBUILD.md) reconstructs cast diagonals, yokes, footrest supports, service routing and compact drive housings, rebaking Frame/Drive. Its 16,872 additional triangles produced the historical 136,928-triangle checkpoint. The subsequent rider-area rebuild adds 9,336 triangles to recover the seating, controls and front interface, rebaking Frame, BodyArmor, Saddle and Steering; its budget is current above. Earlier counts and two-link numbers remain historical; runtime performance and final visual acceptance remain unverified.

## Scene organization

Keep `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend` as the sole active native file. Its default scene and view layer show the complete assembled machine. Geometry stays in its fitted position; an exploded view is not the authoring default. Each assembly has a named root and clearly named mesh parts. Hidden references live together outside the visible export hierarchy.

| Collection | Major editable parts | Interfaces to preserve |
| --- | --- | --- |
| Chassis and frame | Cast diagonal main frame, fitted seat subframe, forward yokes, rear supports, cargo racks, hip mounts and open foot loops with narrow tread | Recovered rider contacts, bearing-aligned steering support, accepted hip bearings and case supports |
| Drive and cooling | Drive core, spine housing, side drive pods and rear cooling housing | Existing cover placement and chassis attachment points |
| Left leg and right leg | Hip, upper/middle/lower frames and shells, upper bend/knee, hock, actuators, ankle and foot | Current three-section hip/knee/hock/ankle targets from the rework receipt; matched stance and ground contact; separate piston rod and barrel where future motion needs it |
| Body armor | Left/right front fairings, lowered upper shrouds, front cowling/bridge and rear panels | Original-led front silhouette, descending thigh waist and fitted mounts |
| Saddle | Crowned pocket upholstery and trim, matching pan and mounting support | Recovered compact profile, early rear rise and chassis supports |
| Steering and controls | Raked stem, swept raised handlebar, grips, levers, gauge and front control pod | Original-led hand contacts and sweep; bearing guide; final wrist and motion checks remain |
| Cargo | Four case bodies, lids and strap/fitting groups | Approved case proportions, fitted location and packed textures |

Useful rigid parts remain separate. Small fixed hardware joins its parent mesh. Parts on different future moving segments are not combined across the joint. These are modeling pivots and boundaries; this pass does not add a rig or certify joint motion.

## Implementation sequence

1. **Inventory and assembly map.** Record the current file hash, component counts, dimensions, hardpoints, material dependencies and ownership of every visible mesh. Identify useful existing parts, parts requiring reconstruction and small pieces to consolidate. The saved baseline is the [textured cargo checkpoint](../../Evidence/Art/LeggerCargo20261015/README.md).
2. **Chassis and mounting structure.** Replace floating rail ends with closed frame loops and clear crossmembers. Connect the saddle, steering, engine, hip and cargo supports. Check the bare structure from all sides, then fit it under the unchanged cases and body. Retain a hidden prior chassis.
3. **Legs, saddle and steering.** Build regular quad limb shells and cores around the existing joints, improve actuator terminations and foot construction, make a smoother shaped saddle and rebuild the stem/bar/grip/lever interfaces. Keep coherent major parts and useful pivots. Preserve the existing main body fairing shapes unless inspection establishes a concrete reason to change them.
4. **Consolidate the complete asset.** Combine fixed fittings with their parent parts, preserve editable quad control surfaces and deliberate bevel widths, and organize all modules in the same scene. Cargo should be body/lid/strap groups rather than dozens of separately listed fittings. The complete view is the default; optional inspection view layers must not replace it.
5. **UV and texture modules.** Give each module a dedicated nonoverlapping atlas and baked PBR material. Preserve useful existing cargo UVs/maps. Bake restrained surface variation, stitching, grain, seams and small fastener relief; retain silhouette-bearing detail in geometry. Inspect source versus baked materials in matched renders.
6. **Verify and save.** Check topology, evaluated triangle counts, UV coverage, normals, packed images, assembly movement, pivots, fit, source retention and native reopening. Replace the same authoring file only after the candidate passes. Record exact hashes and review images in project evidence.

## Geometry and texture budgets

Use quads and regular spacing where they support clean surfaces, with additional loops around bends and important outlines. Do not subdivide a flat surface merely to increase density. All stored major control surfaces should remain quads where practical; evaluated bevels and game rendering are triangulated. Measure the result with modifiers included. A complete LOD0 authoring target of roughly 85,000-120,000 evaluated triangles is provisional, guided by the current 88,420-triangle baseline and the need for better structure. It is not a Unity performance measurement. Review budget changes against visible benefit.

The October 16 full-detail request sets a stricter rule for the current iteration: **no increase above 76 major meshes, 45,762 control quads or 146,264 evaluated triangles**. The earlier range above is historical planning guidance, not the present source count. Improve existing vertex positions and use textures for small intricacy; later reduction/LOD and Unity cost assessment remain separate work.

| Texture module | Authoring atlas target |
| --- | --- |
| Main cargo cases, individually | Existing 4K sets |
| Auxiliary cargo cases, individually | Existing 2K sets |
| Chassis/frame | One 4K set |
| Drive and cooling | One 2K-4K set, according to inspected density |
| Left leg and right leg | One 4K set per leg |
| Body armor | One 4K set |
| Saddle | One 2K set |
| Steering and controls | One 2K set |

Each texture set provides base color, tangent-space normal, roughness, metallic and AO. Base color uses sRGB; data maps use Non-Color. AO remains available separately rather than baked lighting in albedo. Different parts within a module may share its atlas, allowing separate mesh editing without a material or atlas for every fitting. Pack the maps into the native file. Authoring resolution is not the final runtime texture budget: channel packing, compression and downsampling belong to the later Unity material/import handoff and must be measured there.

## Plan audit and corrections

| Audit finding | Required correction |
| --- | --- |
| A chassis-only saved default would hide the assembled vehicle | Keep one scene and make the complete assembled view the saved default. Isolation remains optional. |
| The first chassis candidate contains 141 mesh objects | Consolidate fixed details into a smaller set of major components before the final save. Do not carry the primitive construction inventory into the final Outliner. |
| A separate atlas for every small object would waste texture space and materials | Use module atlases and preserve the four useful cargo atlases. |
| Joining across articulated boundaries would obstruct later rigging | Keep leg segments, joint pivots and actuator rods/barrels deliberately separated. |
| More quads alone do not improve modeling quality | Inspect surface flow, silhouette, shading and fitted interfaces, then measure evaluated cost. |
| Rebuilding every approved component would discard useful work | Retain the accepted cargo design and useful fairings; rebuild the parts whose shapes or interfaces need improvement. |
| Blindly projecting generated surface detail would restore unwanted lumps | Bake from the clean authored surfaces; use the old generated model as design reference only. |
| Procedural coordinate layers could occupy UV0 and confuse a later game import | Retain procedural layers in hidden bake sources; each visible part has its final texture atlas as its sole UV layer, UV0. |
| Script checks cannot establish visual quality or mechanical behavior | Inspect assembled and isolated renders, front/back/top/underside views and control-edge overlays. Motion, collision and engineering strength remain separate. |
| Hidden source objects could inflate active counts or enter export | Keep all references hidden, nonselectable and outside the active export collection; scope every reported budget to the active assembly. |
| Shared repository work is active | Edit only this model and task documentation; preserve unrelated Git index/worktree changes. Keep native Blender files local-only. |

The audited plan is implemented at the checkpoint above. The author explicitly authorized pursuing it as the active goal. Changes to the silhouette remain reviewable modeling decisions, not world canon.

## Acceptance and integration boundary

The complete assembled model must be visible in one scene, with legible collections and independently editable major parts. Existing scale, stance and contact/hardpoint positions must remain accounted for. Meshes should have no unintended loose vertices, degenerate faces or boundary/nonmanifold defects; deliberate intersections at assembled fittings must be inspected. Atlases must have practical padding and coverage, data maps the correct color space, and every intended image packed. Native reopening must reproduce the reviewed appearance.

Deterministic world identity, streaming/unload/reload, generated-object identity and persisted runtime deltas are inapplicable: this is a local source-art rebuild, with no spawning, world system or saved-state change. Authored constraints are the existing fitted arrangement and scale. No Unity scene, package, setting, gameplay, rig or export changes are authorized by this modeling plan. No gameplay smoke tests are needed. The source and local recovery copies remain excluded from Git/GitHub under the [local-only policy](../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).
