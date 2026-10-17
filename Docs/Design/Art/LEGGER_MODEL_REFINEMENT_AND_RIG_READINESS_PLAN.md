# Legger model refinement and rig readiness plan

October 16, 2026. **Source modeling implementation complete and independently verified.** The author requested execution after auditing this plan. Refine the Legger into a convincing distant-future machine through stronger forms, fitted mechanical connections and restrained surface detail, while preparing the assembly for later rigging and animation. The audit corrections below governed execution. The [saved-source receipt](../../Evidence/Art/LeggerModelRefinement20261016/README.md) and [rigging handoff](LEGGER_MODEL_REFINEMENT_HANDOFF.md) record the installed result; production rigging, animation and Unity validation remain the later slice.

The recommended direction is layered ivory armor over a compact dark mechanical core, with precise panel transitions and selected exposed structure. Preserve the original's recognizable design and the accepted clean reconstruction. Use the original's fused details as visual references, rather than mechanical specifications.

## Scope and responsibilities

This implementation slice covers source modeling, articulation preparation, UV and material repairs caused by that modeling, and verified native saving. A disposable articulation study checks whether proposed forms can move; a production rig, animation clips, Unity installation, gameplay integration and performance acceptance belong to subsequent work.

Gottspan coordinates the batch; Blender owns source geometry and preparation; Sable reviews the visual result; Babineaux owns the later Unity handoff. These are responsibility boundaries, not instructions to start other agents. The user retains creative authority. Continue in the existing `main` checkout, with background workers that preserve the user's foreground application.

## Starting point and constraints

The sole active native is [Legger_Topology_Iteration04.blend](../../../SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend). Planning readback found 799,090,806 bytes and SHA-256 `90757f7efa6811fbfd9c82c93b08a98d254f60d13d15e447e5eeab03f2db6304`, matching the [front body volume checkpoint](../../Evidence/Art/LeggerFrontVolume20261016/README.md). Recheck this identity before implementation; a different source requires a fresh inventory and comparison before applying recipes.

| Constraint | Requirement |
| --- | --- |
| Scale and pose | Preserve saved manual scale, the matched neutral legs and existing ground contacts. Refine local sections without global rescaling. |
| Leg mechanism | Preserve the accepted three-section layout and hip, knee, hock and ankle targets from the [leg rework](../../Evidence/Art/LeggerLegRework20261015/README.md). Determine rotation axes and allowed movement explicitly; visual landmarks alone do not certify a working linkage. |
| Rider contacts | Preserve the seating pocket, grip centers and orientations, and tread contacts established by the [rider rebuild](LEGGER_RIDER_REBUILD.md). Review nearby forms against a rider proxy before and after changes. |
| Cargo | Preserve the corrected case dimensions, root transforms, mounting angles and support contacts from the [mounting pass](LEGGER_PLANFORM_AND_MOUNTING.md). Improve surfaces and interfaces within those constraints. |
| Source budget | No increase above 76 active major meshes, 45,762 control quads or 146,264 evaluated triangles, including modifiers. Maintain useful all-quad control surfaces. Disposable review proxies are excluded from the saved active assembly. |
| Materials | Retain ivory, dark metal and brown leather. Keep at most the current eleven active texture sets and 55 active maps; no resolution increase. Revised maps replace active assignments while preceding resources remain recoverable. |
| Organization | One complete assembled default scene and view layer, eleven existing module roots, editable major parts and hidden historical references outside export selection. |
| Preservation | Preserve prior mesh versions, packed resources and unrelated work. Native files, backups and raw review pixels remain local under the [source policy](../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). |

Use the [modular model plan](LEGGER_MODULAR_MODEL_PLAN.md) for the existing assembly and texture ownership. Earlier numerical measurements in the [original leg audit](LEGGER_LEG_GEOMETRY_AUDIT.md) describe its historical pre-rework comparison; they must not be applied again as current defects.

## Modeling decisions

| Area | Proposed treatment | Geometry and movement boundary |
| --- | --- | --- |
| Front housing and cheeks | Establish a firmer central spine, controlled shoulders, stepped backing and deliberate lower termination. Preserve the recovered body depth and exposed ivory cheeks. | Reshape existing surfaces for visible depth and silhouette. Keep shallow access seams, retaining details and seals in maps. Preserve steering bearings and rider reach. |
| Leg armor | Give each section a broad controlled face, a distinct leading edge, changing cross-section and tapered termination. Retain readable dark channels around the mechanism. | Each shell belongs to one rigid link. Shape overlaps around that link's movement envelope; do not bridge bends with a rigid continuous skin. |
| Joint housings | Differentiate hip, knee, hock and ankle through stepped bearing shoulders, recessed centers and fitted partial guards. | Keep cap depth and guard clearance visible in geometry. Bake shallow retaining rings and fasteners. Avoid additional moving covers unless clearance makes one necessary. |
| Feet | Refine runner taper, ankle enclosure, bracing and the transition into contact pads. Preserve the current footprint and contacts. | Begin with a rigid foot assembly. Independent toes and suspension are future options, rather than requirements for this refinement. |
| Chassis and supports | Firm the cast diagonals, define junctions and make changes in section intentional. | Keep bearing centers and rider/cargo hardpoints exact. Small fixed fittings stay with their parent structure. |
| Rear drive and cooling | Make the existing housings read as a compact central service assembly, with a defined rear face and clearer interfaces between the cases. | Reuse existing meshes; preserve leg and cargo clearance. Do not add internal machinery merely to fill space or invent an operating technology. |
| Cargo surfaces | Improve lid seals, inset faces, corner protection and attachment interfaces. | Retain the case arrangement and body/lid separation. Use maps for shallow relief and wear; preserve useful UVs. |
| Controls and saddle | Refine control housing sections, collars, cable sockets, saddle binding and rear transitions. | Preserve hand, seat and foot contacts. Keep levers and future steering movement separable; avoid adding steering complexity without a demonstrated purpose. |
| Hoses and service leads | Clarify sockets, sleeves and attachment points; distinguish flexible runs from structural bars. | Preserve accepted routes initially. Every run needs a recorded endpoint pair and rigid or flexible ownership. Change a route only to resolve an observed fit or movement issue. |
| Surface language | Use a small consistent family of panel corners, chamfers, service recesses and finishes. Concentrate wear at credible contact locations. | Geometry carries silhouette and substantial parallax. Maps carry grain, shallow seams, small hardware and finish variation. Preserve broad quiet surfaces between detailed areas. |

## Execution sequence

### Audit corrections governing execution

- Treat module roots as authoring groups, not qualified motion roots. The expanded `Legger_Part_FrontControlPod` stays fixed to the body; the stem, handlebar, grips and gauge form the steering group. Brake levers require geometry-derived hinge guides because their existing object origins are not verified hinges. Preserve source transforms and store the motion map separately from those authoring groups.
- Use diagnostic target displacements from the accepted neutral pose: foot lift 0.20 m with 0.15 m forward travel; forward and rearward travel 0.20 m with 0.06 m lift; body lowering 0.15 m with planted feet; near-extension through 0.05 m body rise; steering +/-20 degrees. These are modeling study targets, not approved gameplay stride or joint limits. Preserve rigid segment lengths to 10 micrometres; never use bone stretch to reach a target. Test intermediate poses. An unreachable target or unresolved interference is reported, not silently replaced with an easier pass.
- Preserve root/part matrices and protected anchor coordinates within 1 micrometre, protected grip orientation within 0.001 degree, and unchanged mesh/UV/image data exactly by fingerprint. Preserve the main saddle pocket through Y=0.14 m and all cargo and footrest vertices. Protect foot contact vertices below Z=0.028 m. Clearance checks distinguish intentional assembled overlaps from unintended moving-part intersections and report both the pair and pose.
- Keep a source bake contract recording evaluated triangulation and normals. These bakes remain source-qualified until the later Unity import validates its tangent handling; an exporter/importer mismatch requires a derived-mesh correction and affected rebake. No production export or Unity import is added to this slice.
- Frame each complete reviewed component with a margin before locking matched cameras. Validate the projected union of original/before/candidate bounds; never reuse the cropped foot-top setup as a full-foot comparison. Label any deliberate near/far clipping.

### Phase 1 Seal the baseline and map movement

Inventory the independently reopened saved source: active names and roots, evaluated geometry, mesh islands, transforms, pivots, UV layers, materials, packed images and existing vertex groups. Record the protected rider, leg, cargo and ground contacts in a common coordinate system: Blender metres, forward -Y, Z up. Preserve a verified local recovery copy before candidate authoring.

Create a movement ownership table for every active mesh or separately moving island. Record its parent link, rotation center and axis, rigid or flexible treatment, connected parts and relevant clearances. Specifically account for piston rods and barrels, hose endpoints and fittings, armor caps, cargo lids and straps, steering and levers. A single editable mesh can contain separately weighted rigid islands; the 76-mesh ceiling does not require welding moving parts together.

**Exit check:** all active geometry has explicit ownership, protected measurements and recovery identities are recorded, and uncertain axes or mechanisms are listed as decisions to resolve before detailing the affected region.

### Phase 2 Establish articulation envelopes

In a disposable copy, use simple pivots or a temporary skeleton to inspect the accepted rest pose, crouch, high step, forward and rearward leg travel, near-extension, planted-foot body movement, and steering in both directions. Define the intended pose ranges in that study rather than assuming arbitrary joint angles are safe. Check intermediate poses as well as endpoints.

Give the three-section chain a stable folding convention and consistent axes. Check piston stroke and alignment, armor overlap, hose slack and endpoint behavior, foot clearance, and interference with the chassis and opposite leg. Review the rider envelope, hand reach and boot space during body and steering movement. A proportional proxy is a clearance aid; final dressed-character fit remains a later check.

**Exit check:** each affected region has a recorded usable pose range, screenshots of its limiting clearances and a list of corrections. Do not finalize shells around an unresolved collision. If acceptable movement requires moving protected joints or rider contacts, record the conflict for a specific design decision instead of silently changing them.

### Phase 3 Refine primary forms in clay

Work in small regional batches in this order:

1. Front housing, cowling and cheeks.
2. One leg's shells and joint guards, followed by the matched opposite leg.
3. Feet and ankle transitions.
4. Chassis junctions and compact rear drive forms.
5. Cargo interfaces, controls and saddle finishing.

Prefer existing vertex and plane corrections. Change connectivity only where a visible improvement or articulation requirement justifies it, retaining the whole-asset ceilings. Review each batch in the assembled machine as well as in isolation. Recheck its affected articulation poses before continuing.

**Exit check:** front, side, top and oblique clay views show the intended plane changes and fitted layers; the elevated view retains a clear silhouette; protected contacts and budget checks pass. Fine texture detail cannot substitute for an unresolved form.

### Phase 4 Clean geometry and prepare the runtime structure

Remove unintended loose or duplicate geometry, degenerate faces and shading defects within edited regions. Inspect deliberate overlaps and backing surfaces that motion exposes. Keep rigid link boundaries and articulation islands identifiable. Use deformation loops only where a flexible element needs them; retain controlled bevels where they improve visible highlights.

Verify transform consistency, mirrored orientation and pivot/axis records without casually applying transforms that would alter the established arrangement. Preserve the editable source and define an explicit export allowlist. Hidden dense originals, historical versions, cameras and review proxies must be outside that allowlist.

**Exit check:** edited controls retain clean quad topology, evaluated counts remain within the ceilings, ownership boundaries survive cleanup, and the complete model reopens in its accepted neutral arrangement.

### Phase 5 Repair UVs and refine surfaces

Retain current atlases where they remain suitable. Inspect distortion and texel density after reshaping; repair only affected UV regions or modules. Establish final export triangulation and normal/tangent handling on a derived mesh before the final tangent-normal bake, then keep those choices consistent with later import.

Bake from clean authored detail, with controlled cages and isolation between neighboring moving parts. Avoid permanent occlusion from a different moving link. Recompute affected AO after substantial geometry changes. Preserve base color without directional lighting, use sRGB for color and Non-Color for data, and check atlas padding at reduced mip levels.

Differentiate armor, structural metal, exposed bearing surfaces, leather and flexible service lines through restrained roughness and surface detail. Keep the eleven-set/55-map active ceiling and verify that revisions do not damage unrelated atlas regions.

**Exit check:** matched clay and textured views both remain convincing; edited maps have correct assignments, usable padding, no visible projection errors and packed dependencies. Independent native reopening reproduces the candidate appearance.

### Phase 6 Verify the complete native and hand off

Repeat the source inventory and protected comparisons. Recheck all affected articulation poses, mesh/material ownership, module independence, UVs, map assignments and export allowlist. Inspect assembled front, back, both sides, top, underside and oblique views, plus detailed views of edited areas. Keep comparison camera, framing, lighting and exposure fixed. An elevated offline proxy supports source review; later Unity captures establish actual gameplay-camera appearance.

Replace the sole active native only after the candidate and an independent reopen pass. Use a guarded replacement that verifies the expected input identity and preserves the preceding local source. Leave any older loaded GUI session untouched; it must be reopened before a later author save can overwrite the revised file.

**Deliverable:** the refined native, recovery identity, before/after comparison set, active geometry and texture counts, movement ownership table, articulation ranges and unresolved limits, and a concise rigging handoff. This establishes verified modeling preparation; game readiness also requires the later export, animation and Unity checks below.

## Later rigging and Unity work

This section defines dependencies for the subsequent animation slice; it does not add that implementation to the current modeling plan.

1. Build a custom skeleton for the three-section legs, body and steering, with deliberate folding controls and mechanical limits. Keep animator controls distinct from the exported deform hierarchy.
2. Assign each rigid shell, cap and structural island explicitly to its carrying bone. Provide controlled flexible weighting only for hoses or other intended flexible pieces. Keep piston rod and barrel motion independent.
3. Start with rigid feet and fixed cargo. Add toe roll, secondary movement or opening cargo only when required by an agreed animation or interaction.
4. Define stable rider, hand, foot and cargo attachment transforms. Verify their presence after any hierarchy optimization. Preserve separate world movement and body motion responsibilities when choosing the animation root.
5. Bake exportable animation from authoring constraints and test the resulting bone transforms; do not assume Blender control constraints transfer directly. Export an explicit set of game geometry, skeleton and clips.
6. Import a sample as a Unity Generic rig. Check units, axes, rest pose, materials, tangent normals, attachment transforms, skinning and animation bounds in the pinned Editor before the full library is produced.
7. Validate idle, walking, running, turning, stopping, stepping, crouching and rider fit with the agreed rig. Compare foot planting, link rigidity, piston travel and hose clearance throughout the clips.
8. Measure triangles, material/submesh and renderer counts, skinning cost and texture memory in Unity. Derive LODs and texture/channel packing from those measurements. The current source ceilings are preservation limits, not a measured runtime performance budget.

Unity's [Generic animation guidance](https://docs.unity3d.com/6000.4/Documentation/Manual/GenericAnimations.html) and [rig import settings](https://docs.unity3d.com/6000.4/Documentation/Manual/FBXImporter-Rig.html) control the later import contract. Blender's [skinning documentation](https://docs.blender.org/manual/en/5.3/animation/armatures/skinning/parenting.html) describes explicit vertex-group assignment; verify export and modifier behavior against the installed Blender 5.2.2 LTS runtime before execution. Its [5.2 bake settings](https://docs.blender.org/api/5.2/bpy.types.BakeSettings.html) define the available bake controls.

## Evidence and completion requirements

Each regional checkpoint records the exact source/candidate identities, changed part names, evaluated counts, protected measurements, inspected images and outstanding decisions. Keep technical checks distinct from visual judgment and user acceptance. Preserve useful intermediate comparisons locally; avoid proliferating hidden copies that add no distinct recovery value.

The modeling slice is complete when the full assembly has stronger original-led forms, fits its recorded articulation ranges, preserves accepted constraints, passes independent native inspection and reopening, and carries a usable movement/rigging handoff. Failed clearances, missing texture dependencies or an exceeded ceiling prevent candidate promotion. Do not describe the asset as fully rigged, animated or game-ready before the later checks establish those results.

## World and repository boundaries

Deterministic world identity, chunk streaming and unload/reload, stable generated-object identity and persisted runtime deltas are inapplicable to this source-art plan because it creates no runtime objects or world services. Authored constraints apply through the saved scale, original design, contacts, mounting arrangement and geometry budget. Greater Wasteland's fixed terrain and gameplay remain unchanged.

No Unity assets, metadata, scenes, packages, settings or gameplay smoke tests are part of planning or the proposed source refinement. Keep native sources and raw captures local. Commit only task-owned documentation and later verified records; a local commit grants no push, branch or release authority.

## Plan review

| Risk found while planning | Resolution |
| --- | --- |
| Surface work could hide a mechanism that cannot fold | Establish articulation envelopes before final geometry and repeat affected poses after every regional pass. |
| A source mesh count could force unrelated moving parts together | Track ownership per mesh island and vertex group as well as per object. Preserve independent motion without adding an object for each fitting. |
| Better rear density could obstruct the legs or rider | Reuse existing housings and require clearance checks in the assembled machine. |
| New geometry could invalidate the current maps | Review UV distortion and rebake only affected modules after geometry settles, including affected AO. |
| A normal bake could change when exported | Establish and retain the derived export triangulation and tangent/normal contract before final baking. |
| Existing source budgets could be mistaken for performance proof | Preserve the ceilings now; measure the actual exported render and skinning costs in the later Unity slice. |
| Historical measurements could reverse completed improvements | Bind work to the current source identity and use the later rework/rider/mounting receipts as the accepted baseline. |
| A modeling plan could silently expand into gameplay implementation | Keep production rigging, clips, exports and Unity integration as the explicitly separated subsequent slice. |

## Completion audit — October 16, 2026

| Phase | Verified result |
| --- | --- |
| 1 Baseline and movement map | Verified recovery copy; all preceding mesh/image identities preserved; every active island has a guide carrier and all protected transforms/contacts pass. |
| 2 Articulation envelopes | 58 repaired-geometry samples, link/stroke/hose measurements, steering/lever checks and a retained rider proxy; limiting pose renders are recorded. |
| 3 Primary forms | Regional clay and textured comparisons against the original and accepted input; local form/fitting changes in 41 meshes with unchanged source ceilings. |
| 4 Cleanup and structure | Closed nondegenerate quads, positive edited-island volumes, corrected mirrored cable winding, independent hidden recovery roots and a native geometry allowlist. |
| 5 Surfaces | Five repaired atlases, 55 active packed maps at existing resolutions, density/distortion comparison, derived triangulation/normal contract, isolated AO and distance-view review. |
| 6 Save and handoff | Guarded installation into the same native; independent reopen/inspection and matched renders; [receipt](../../Evidence/Art/LeggerModelRefinement20261016/README.md) and [later rigging handoff](LEGGER_MODEL_REFINEMENT_HANDOFF.md). |

Author visual acceptance, production deform weights and controls, clips, export and Unity cost/material/tangent checks remain separately named dependencies. They are not claimed by this source modeling completion.
