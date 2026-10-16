# Legger modular model and texture plan

The Legger will be one assembled model in one Blender scene, organized into independently editable major components. The author approved implementation on October 15, 2026. Preserve the current manually chosen scale, staggered leg pose, joint locations, rider contact points and ivory, dark-metal and brown-leather palette. Group small fittings with their parent components rather than creating an object for each nut or bolt.

Implementation checkpoint: all six phases below are complete in the same native file. The [verified modular assembly receipt](../../Evidence/Art/LeggerModular20261015/README.md) records one scene/view layer, 67 visible major meshes, 21,396 quad control faces, 86,072 evaluated triangles and 55 packed maps across 11 texture sets. The earlier source geometry remains hidden for reference; visual acceptance and later Unity integration remain separate.

## Scene organization

Keep `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend` as the sole active native file. Its default scene and view layer show the complete assembled machine. Geometry stays in its fitted position; an exploded view is not the authoring default. Each assembly has a named root and clearly named mesh parts. Hidden references live together outside the visible export hierarchy.

| Collection | Major editable parts | Interfaces to preserve |
| --- | --- | --- |
| Chassis and frame | Main frame, seat subframe, cradle, cargo racks, hip mounts and footrests | Steering head, seat mounts, hip bearings, case supports |
| Drive and cooling | Drive core, spine housing, side drive pods and rear cooling housing | Existing cover placement and chassis attachment points |
| Left leg and right leg | Hip, thigh frame and shells, knee, shin frame and shells, actuators, ankle and foot | Current hip/knee/ankle centers; ground contact; separate piston rod and barrel where future motion needs it |
| Body armor | Left/right front fairings, upper shrouds and rear panels | Existing body outline, rider clearance and mounting positions |
| Saddle | Upholstery and its trim, underside pan and mounting support | Current height, width, rising rear profile and chassis supports |
| Steering and controls | Stem, handlebar, grips, levers, instrument assembly and front control pod | Current grip positions, steering head and gauge position |
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
