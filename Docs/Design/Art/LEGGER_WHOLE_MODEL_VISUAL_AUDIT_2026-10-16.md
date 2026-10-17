# Legger whole-model visual audit

October 16, 2026. **Audit only; no model, texture or saved-scene edits.** The author requested visual inspection of the entire original and opportunities to improve the rebuild. The dense `Legger_DetailBake_Source` is the design reference. The inspected current source is the [front body volume checkpoint](../../Evidence/Art/LeggerFrontVolume20261016/README.md), SHA-256 `90757f7efa6811fbfd9c82c93b08a98d254f60d13d15e447e5eeab03f2db6304`. [Evidence and read-only verification](../../Evidence/Art/LeggerWholeAudit20261016/README.md) bind the native inventory, views, measurements and local image identities.

## Main assessment

The rebuild now has the recognizable layout, three-section legs, saddle rise, ivory cheeks, cargo arrangement and hanging hose families. Its largest remaining weakness is **the design of the medium-size forms and how they connect**. The original combines broad armor with stepped housings, overlapping edges, recessed machinery, varied bearing shoulders and substantial foot bracing. The rebuild often substitutes a smooth shell, an evenly rounded beam, a plain disk or a straight-sided box. More fine scratches or uniformly denser topology would leave those differences in place.

The most valuable visual work is front housing segmentation, the rear central assembly, controls and frame junctions. A separate fitting defect needs attention before rigging: several foot tie-bar inner ends are unsupported. Proposed work below is a recommendation for a later authorized pass, not implementation approval or a claim that every proposed change has already been proven within budget.

The material and clay comparisons agree: many differences remain with textures removed. Preserve silhouette, actual recess depth and major overlaps in geometry. Use maps for shallow retaining rings, seams, vent slats, fasteners, grain, stitching, markings and restrained wear. Do not reproduce the generated source's incidental lumps or ambiguous fused junctions literally.

## Inspection coverage and comparison limits

The audit retains **59 inspection renders**: 28 original/current pairs and three current-only isolation views. Whole-model views cover front, both sides, rear, top, underside, three-quarter and an elevated stand-in. Close-ups cover front body, controls, cockpit, saddle, chassis, hip, cargo, tail, hoses, upper/lower legs, feet and rider stirrups. Three matched clay pairs distinguish form from surfacing. Eight labeled sheets and a priority comparison board organize the images. Four initially off-center foot images were replaced with correctly centered captures; their revised cameras are recorded.

Each pair uses the same camera, framing, lights, exposure and resolution. The foot top pair uses a narrower 1024 × 1200 frame. Far-leg cuts and a top-foot cut remove occlusion; exposed black cut areas are **inspection artifacts, not holes in the asset**. Current-only frame/drive/harness isolation is supplementary; the fused original cannot be separated equivalently without editing it. The original's staggered pose differs intentionally from the accepted mirrored rest stance. Do not infer wrong link lengths solely from their different side projections. The elevated view is an offline stand-in, not the actual Unity camera.

## Preserve the recovered baseline

| Measured item | Original | Current | Audit implication |
| --- | ---: | ---: | --- |
| Whole height | 2.2277 m | 2.2147 m | Close overall scale; do not globally rescale to fix local form differences. |
| Whole width | 0.8586 m | 0.8660 m | Overall width is already close. |
| Whole fore/aft envelope | 1.6463 m | 1.6137 m | Includes the original's staggered feet; not a body-length error metric. |
| Right foot length / width | 0.6868 / 0.2955 m | 0.6800 / 0.2960 m | Footprint is close; improve section weight, bracing and fitted ends rather than enlarging the whole foot. |

The original foot envelope is a fused crop below Z = 0.18 m; the current figure is the complete named foot. Neither is a contact polygon or stability measurement. The current left/right feet share their aligned footprint; the original left foot is displaced by its pose.

Retain the manually chosen scale, accepted joint targets and neutral stance, saddle contact region, grip centers, foot contacts, case arrangement, separate major parts and ivory/dark-metal/brown-leather palette. The visible source remains **76 meshes, 45,762 quad controls, 146,264 evaluated triangles and 55 active maps across eleven sets**. No increased geometry budget is implied. The earlier leg audit describes a superseded two-link model; do not repeat those old corrections as if they were current defects.

## Findings and proposed treatments

### F01 — Front head, central nose and lens: high visual priority

The original has a compact mechanical head with distinct side shoulders, a narrow center, a recessed rectangular lens/bezel and additional breaks below it. The rebuild has useful side depth now, but the head reads as a rounded helmet and the nose as one large smooth shield. Two bright lens strips and a fine outline do not recover the original's layered front.

**Geometry:** reshape the existing head and central body sections into more controlled planes, shoulder steps, a narrower front-facing strip and real recesses at the major openings. Retain the recovered inner-body depth rather than reverting to a thin plate. **Maps:** restore the rectangular lens pattern, shallow grille/retaining detail and local material differences. Verify front, side, low oblique and clay; the silhouette and shadow breaks should work before judging texture detail. Evidence: `front_side`, `front_oblique`, `control_front`, `clay_front_body`.

### F02 — Machinery behind the front cheeks: high visual priority

The original reveals overlapping bearing-like forms and structural pieces within the ivory arch. The recent fill closes the prior empty void, but its exposed side is now mostly a dark flat panel with two baked circles. Those circles cannot reproduce the depth and overlapping outline of the original mechanical masses. The cheek also needs a more deliberate return edge and upper mounting transition.

**Geometry:** distribute the existing backing/body sections into a stepped inner housing, visible bearing shoulders and controlled openings around the retained yoke. Keep the outer ivory silhouette and leg axes. **Maps:** shallow cap recesses, retaining rings and fasteners can stay baked. Verify the side opening and low oblique in clay; avoid replacing an empty gap with a featureless solid slab. Evidence: `front_side`, `hip_inboard`, `clay_chassis`.

### F03 — Tail bridge and central rear housing: high visual priority

This is a major whole-model mismatch. Between the original cases are a compact ivory bridge, a dark upper recess, a substantial vertical rectangular service casing, paired side forms and nested return paths. The current rear shows a large smooth hood above a mostly open central bay with only small plain masses below it. Its rear silhouette and visual center of gravity differ even at overview scale. The object name “cooling” does not prove the source's mechanical function.

**Geometry:** rework the existing rear/spine/drive housings into a compact stepped central assembly with a defined rear face and bridge. Compare the hood's height and projection against the original instead of simply filling the opening with another block. Preserve cargo spacing, leg clearance and the accepted seat region. **Maps:** face inset, small vent relief, cover borders and restrained wear. Verify direct rear, rear oblique, top and underside together. Evidence: `overall_rear`, `cargo_rear`, `tail_engine`, `hoses_under`, `isolated_drive`.

### F04 — Handlebar, controls and instrument housing: high visual priority

The source bar has a visibly dropped center and rising shoulders toward the grips, with substantial switch/clamp housings and a compact integrated instrument pod. Our bar is much flatter from the front, with its center sitting high, a simple upright stem and a thin rectangular instrument assembly. Grip positions were recovered in earlier fit work, but correct contact points do not establish the correct bar contour or housing construction.

**Geometry:** reshape the existing bar's central path and shoulders while holding the accepted grip centers and sweep. Fit the stem, clamps and gauge casing into a convincing connected group; give the gauge a proper wrap and thickness. **Maps:** controls, legends, lens graphics and small parting seams. Verify front, rider-facing/top and side; wrist reach, lever sweep and steering motion still need a later posed-rider/rig check. Evidence: `control_front`, `cockpit_top`, `tail_engine`, `front_side`.

### F05 — Cast diagonals, neck/yoke and frame junctions: high visual priority

The main diagonal direction is broadly recovered. Its section and junctions remain too uniform and softly rounded. The original has broader cast/web faces, tapered transitions, bearing shoulders and more distinct intersections. The present thin supporting members and plain rounded ends can make the body look assembled from generic bars rather than one designed chassis.

**Geometry:** refine broad faces, section changes and junction shoulders using existing rows; fit visible member ends into their supports. Preserve the accepted bearing centers, cargo and rider hardpoints. **Maps:** cast grain, shallow ribs, seams and small attachment detail. Verify bare-frame isolation as well as the assembled clay side view so hidden overlaps do not substitute for readable connections. Evidence: `chassis_side`, `hip_inboard`, `clay_chassis`, `isolated_frame`.

### F06 — Leg armor, joint guards and actuators: high visual priority

The current three-section chain is the correct retained starting point. Source armor has changing section width, broad controlled faces, wrapped returns, stepped terminations and exposed dark channels. The rebuilt links read as similarly smooth tapered strips; several joint housings repeat a plain round form. Actuator barrels and ends also lack the source's layered attachment character.

**Geometry:** distinguish upper, middle and lower shell sections, strengthen their leading/return edges and taper their ends around the existing joints. Differentiate bearing shoulders and fitted partial guards without moving axes or bridging moving links with rigid plating. Refine actuator collars and end fittings at their existing terminations. **Maps:** shallow concentric detail, small slots, local abrasion and fasteners. Verify clipped near-leg side, front/rear and clay. Do not copy the original's unequal posed projection or introduce stagger again. Evidence: `leg_upper`, `leg_lower`, `overall_front`, `overall_rear`, `clay_leg`.

### F07 — Foot bracing and fitted endpoints: high fitting priority

The outline dimensions are already close, but the original's arched members, ankle cradle and pad connections have substantially more weight. Our slender lower tie bars and simplified central cradle leave visibly unsupported ends. A targeted native control-surface check confirms that all eight sampled inner tie-bar cap centers are outside the other foot components: the forward centers are approximately **41.3 mm** and the rear centers **28.2 mm** from the nearest other component surface. These distances exceed the roughly 10 mm cap radius. See the [endpoint evidence](../../Evidence/Art/LeggerWholeAudit20261016/foot_connections.json).

**Geometry:** fit those existing ends into deliberate supports, then refine arch taper, ankle enclosure and pad transitions while retaining footprint and ground contacts. **Maps:** tread, casting relief and small pin detail. Verify both sides, top and underside on evaluated geometry before a future repair is accepted. The measurements are cap-center/control-surface checks, not signed solid clearance or load certification. Evidence: corrected `foot_side`, `foot_top`, `clay_leg`.

### F08 — Saddle, pan and rear support layers: medium priority

The overall pocket and early rear rise are considerably closer than the earlier bench. The source's upholstered roll, compact nose, thin pan edge and shaped support layers still have more deliberate transitions. Our continuous piping/rails and repeated rear wedges read as stacked strips, and the highly regular ribbing emphasizes that simplicity.

**Geometry:** refine nose-to-cowling, rolled edge, pan thickness and rear support transitions around the retained contact region; do not casually change the seat centerline that already has accepted fit evidence. **Maps:** less uniform rib shading, lower-contrast stitching and localized compression/wear. Verify side, top and rear oblique. Final rider fit remains unproven by an empty saddle render. Evidence: `saddle_side`, `saddle_rear`, `cockpit_top`.

### F09 — Forward cowling, knee channels and waist plating: medium priority

The source transitions through a compact dark cowling into the saddle and descending thigh channels. The current broad flat roof and pale sides, long parallel trim bands and smooth dark under-seat mass read more like generic fairing pieces. Existing clearance improvements should be retained, but the surface hierarchy needs closer original-led section changes.

**Geometry:** firm the cowling's front/side breaks, narrow and taper trim returns where needed, and clarify how the waist panels meet the pan and frame. Preserve the recovered thigh channel and rider contact envelope. **Maps:** seals, small access lines and material boundaries. Verify top, side and rider-facing views with the same later rider reference before reducing clearance. Evidence: `cockpit_top`, `saddle_side`, `rider_stirrups`, `front_oblique`.

### F10 — Rider stirrups and mounting webs: medium priority

The original has a substantial shaped upright web, a clearly readable open foot loop and a pale contact tread. Our loops exist at the retained contact locations, but the slender dark support and tread blend into surrounding machinery. The mounting transitions are less convincing than the source.

**Geometry:** recover the web's taper, shoulder and attachment section and refine the loop/tread shapes without relocating contacts. **Maps:** tread grip and shallow inset detail. Verify both obliques and inboard views; a later rider must confirm shin/boot clearance. Evidence: `rider_stirrups`, `hip_inboard`, `chassis_side`.

### F11 — Cargo cases, lids and straps: medium/lower visual priority

The clean case bodies and their major arrangement are useful strengths of the rebuild. The source has more stepped lid edges, corner protection, interrupted strap paths, substantial handles and latch relief. Our large blank faces, uniformly broad straps and simple lids remain noticeably plainer, especially from the rear.

**Geometry:** retain the box-like design and mounting arrangement; refine existing lip/handle/corner sections only where they affect outline or attachment. **Maps:** inset faces, latch relief, strap compression, edge wear and less conspicuous regular stitching. Do not recreate every source fastener as an object or reintroduce its lumpy generated edges. Verify cargo side/rear and elevated scale. Evidence: `cargo_side`, `cargo_rear`, `overall_top`, `tail_engine`.

### F12 — Hose routing and terminal integration: medium priority

The source's gauge hierarchy and hanging route families are recognizable now. Its bundle is more tightly nested around the rear mass, with less uniform curvature and more visually embedded terminals. Our long clean arcs and exposed straight rises emphasize the empty rear bay and can read as an external cage.

**Geometry:** after fixing the rear body, adjust existing route curvature and visible terminal seating; preserve the useful gauge differences and recheck the prior static spacing/cargo constraints. **Maps:** jacket texture, shallow collars and localized contact wear. Do not add a dense wire forest or claim concealed junctions are exact engineering. Verify underside/rear/side and current harness isolation. Evidence: `hoses_under`, `cargo_side`, `overall_underside`, `isolated_harness`.

### F13 — Material response, relief and detail hierarchy: medium priority after forms

The original separates chalky ivory paint, dark machinery and warm straps through varied roughness, edge transitions and mid-size relief. Our broad flat fills, thin graphic panel lines and repeated vent patterns look more generic. Some large mechanical features have been reduced to flat circles or marks, while high-contrast stitching attracts attention away from the machine.

**Maps:** after form approval, match the scale of shallow relief, vary roughness by material and use restrained wear at contact/edge locations. Keep the established palette and avoid blanket grunge. Deep housings/openings belong in geometry; small retaining rings, stitching, markings and grain belong in maps. Check texel density after reshaping and rebake affected normal/AO regions where justified. Verify neutral PBR and clay together, then the eventual real Unity view; offline shading is not Unity appearance or performance proof.

## Recommended order for a later authorized pass

1. Repair fitted foot tie-bar endpoints as a bounded integrity task before rig preparation.
2. Recover front segmentation/inner machinery and the rear central assembly, reviewing side, front and rear together.
3. Refine bar/control construction and chassis sections around fixed contacts and bearings.
4. Refine leg-shell sections, joint guards and foot/ankle weight while preserving the accepted chain and contacts.
5. Resolve saddle/waist/stirrup transitions, then cargo and hose integration around the approved body forms.
6. Update only the surfaces affected by those changes, then assess reduction/LOD and Unity handoff separately.

Work toward stronger forms using the existing control topology and the current ceiling. Do not spend more polygons on uniform subdivision or individual bolts. Each future batch should have a small original/current/candidate comparison, a geometry budget check, relevant contact/clearance checks and saved-source verification. This audit makes no source edits and does not start those batches.

## Verification and remaining limits

Read-only workers opened Blender 5.2.2 LTS with auto-execution disabled. Source hashes before and after inspection/capture match exactly; no Blender save, bake, export, GUI interaction or source replacement ran. Only new audit records and local render outputs were created. The current native remains the preceding front-volume checkpoint.

The original is fused generated geometry without a verified mechanical assembly or rig. Shapes described as covers, housings, bearings or service panels are visual interpretations, not established technology or game-system canon. No actual rider, steering sweep, animated linkage, structural load, collider, Unity import or runtime performance was validated. The source-art budget remains substantial; unchanged triangle/map counts do not establish a game-ready runtime asset. Authored scale, palette, pose and contact constraints apply; world generation, streaming identity and persistence are inapplicable to this read-only audit.
