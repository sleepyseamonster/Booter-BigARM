# Legger rider-area audit

October 16, 2026. Analysis only. The author's clarification controls this study: **the original Legger has good rider geometry**. Recover its rider layout in the rebuilt assembly. The existing playable-character prototype supplies a dimensional cross-check; it does not replace the original as the design authority.

The rebuild changes the relationship between the saddle, thigh channels, handlebars and foot supports. Restoring that relationship should precede further surface detailing or baking in this area. No native model edits were made during this audit.

## Sources and proof boundary

The active local source is `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`. SHA-256 remains `788e4de73bac5a52b9853e0071b1df23b9433edc296b981468e2d991c2cd7f93`, 745,433,568 bytes, matching the [chassis checkpoint](../../Evidence/Art/LeggerChassis20261016/README.md). Retained `Legger_DetailBake_Source` is the original reference at its saved manual scale: approximately 2.22775 m tall. Current assembly height remains approximately 2.22441 m. No global scaling is proposed.

Eighteen calibrated native views cover side, top, oblique, seat, knee and grip geometry, including clay views. Eight additional captures show a temporary proportional rider and foot-contact camera cuts. Three review boards and two contact sheets accompany the [evidence receipt](../../Evidence/Art/LeggerRiderAudit20261016/README.md). All pixels and native sources remain local. The original is a fused generated mesh; apparent seams do not prove independent part boundaries. Camera cuts do not create physical holes.

Measurements use Blender metres: width X, forward -Y, height Z. Original grip locations are interpreted surface estimates with about 10 mm uncertainty. Seat and body rays measure envelopes rather than anatomical clearance. Differences smaller than that interpretation scale should not become precision design requirements.

## Saddle shape and seating angle

The original has a compact seating pocket between a raised front body/nose and an early rear support. Its cushion has a rounded transverse crown, rolled edges and a rearward rise that holds the rider near the narrow waist. The rebuild has a longer teardrop cushion with a shallow longitudinal ramp, a transverse center trough, raised side bolsters and a much later rear rise. Ribbing or stitching cannot recover those missing shapes.

| Sample | Original | Current rebuild | Implication |
|---|---:|---:|---|
| Center height at Y -0.10 m | 2.002 m | 1.986 m | Centerline alone is about 16 mm lower; this does not describe the whole contact surface. |
| Center above mean surface at X ±0.06 m, same Y | +10.7 mm | -12.0 mm | Crosswise curvature has reversed from crown to trough. |
| Upper contact-band width at Y -0.10 m | 0.175 m | 0.230 m | The rebuild's upper seating region is broader. |
| Mean centerline rise, Y -0.06 to +0.14 m | 22.2° | 6.4° | The original begins rear support much earlier. |
| Rear top-envelope crest | Y +0.28 m | Y +0.49 m | High rear structure sits about 0.21 m farther aft in the rebuild. |

The contact band is the contiguous top-ray region within ±30 mm of center height, sampled every 5 mm in X. It is **not full upholstery width**. The original crest can include its back support/grab structure; it is not an isolated cushion measurement. The original's raised front tank/nose is likewise separate from the main pocket. Copying the full fused top envelope into one cushion would misread the design.

Restore the compact pocket, rounded section, front transition and early rear rise together. Do not simply shorten the current bench or lower it uniformly. The seat pan and supporting rails should follow the recovered contact shape afterward.

## Thigh and knee channels

The original narrows beneath the rider so the thighs can descend beside the front of the saddle. The rebuild's upper body shrouds occupy more of that space. Selected right-side outside-in rays show:

| Y / Z station (m) | Original outer X | Current outer X | Outward change per side |
|---|---:|---:|---:|
| -0.20 / 1.90 | 0.148 m | 0.176 m | 28 mm |
| -0.10 / 1.95 | 0.108 m | 0.160 m | 51 mm |
| -0.05 / 1.85 | 0.096 m | 0.182 m | 85 mm |

These are local envelope differences, not measured limb penetration or universal loss of clearance. At Y -0.25 / Z 1.85 the current ray passes through a gap; a missing hit must not be treated as zero body width. The original ray includes all fused geometry; current rays exclude robot leg modules but can hit frame, body or cargo.

The revision needs a shaped waist and descending knee/thigh channels across the seat pan, upper body shrouds and front transition. Narrowing the cushion alone will leave the hard walls in the rider's way. Retain clean, separate major parts with loops following these curves; surface textures cannot replace rider-space geometry.

## Handlebars, hand reach and rider posture

At equal hand span, using grip contact X ±0.356 m:

| Contact | Original estimate | Current |
|---|---|---|
| Left hand Y / Z | -0.264 / 2.185 m | -0.443 / 2.105 m |
| Right hand Y / Z | -0.273 / 2.181 m | -0.443 / 2.105 m |
| Grip backsweep in plan | Roughly 28–36° | 5.1° |

The rebuilt grips are approximately **17–18 cm farther forward and 8 cm lower**. This strongly changes the rider's arms even though hand span is similar. The original's handlebar sweep, rise and relation to the raised nose should guide the reconstruction. Generated left/right irregularities are not rig targets: use a symmetric interpretation around Y -0.27 m, Z 2.18 m and roughly 30° backsweep as a first blockout, then verify against the original views and actual posed rider. These are provisional restoration anchors, not edits applied by this audit.

The imported local `defaultmale_rig.fbx` has a standing mesh height of approximately 1.770 m. Joint-head distances give thigh/shin lengths around 0.436/0.425 m and upper-arm/forearm lengths around 0.260/0.258 m. A temporary two-link proxy uses those proportions, common hip Y -0.10 / Z 2.10 m, 15° torso lean, a 55 mm palm allowance and explicit ankle offsets. At the same setup its right elbow is about 99° on the original and 146° on the rebuild; shoulder-to-grip distance increases from about 0.436 to 0.548 m. This illustrates the reach change, not an ideal angle prescription.

The 54-case reach-only sensitivity study varies body scale 0.9–1.1 and lean 0–40° in 5° increments. The original's contacts are reachable throughout that grid. The rebuild requires greater lean, especially for the shorter proxy. Reach alone does not establish comfort, collision freedom or animation quality. Capsule proximity flags the rebuilt thigh shroud and footrest for later checks; negative radius-subtracted gaps are not certified signed penetration depths. Original soft/hard contact surfaces are not segmented.

Hand placement also requires wrist roll, grip diameter, lever reach and instrument visibility checks. This audit does not solve those or the steering sweep.

## Feet and supporting structure

The original presents an open foot loop with an outer rail around |X| 0.33 m; sampled rail tops are approximately Z 1.509–1.510 m near Y zero. The rebuild has a broad tread pad centered at |X| 0.25 m, top Z 1.4765 m, plus an outer guard near |X| 0.29 m and top Z 1.519 m. Thus the pad centers are about 8 cm inward per side relative to the original outer rail. Comparing the original rail directly to the current pad suggests a 33 mm height difference, but the current guard is already near the original rail height. **Do not automatically raise the whole footrest by 33 mm.** Establish the intended boot-bearing surface first.

Recover the original foot stance and check shin space against the footrest web, cargo and body channels. Refit the bounded footrest supports, steering mount and seat rails after the rider contacts settle. Avoid shifting accepted robot leg axes or changing the overall chassis simply to compensate for misplaced contacts.

## Revision order and acceptance checks

1. Build temporary symmetric contact guides from the original: pocket, front/rear support, hand positions and boot support. Retain the source/manual scale.
2. Recover the saddle upholstery and matching pan, including crown and rolled edges. Model its silhouette; bake ribbing, stitching and small wear later.
3. Reshape the upper body panels and front nose around descending thigh/knee space. Check both sides in front, side and top views with the rider.
4. Rebuild the swept handlebar/stem and fit grips, levers and gauge around the recovered hands. Keep separate major editable parts without splitting every fastener.
5. Fit the boot contact, footrest web and adjacent supports. Check a seated rider, modest posture shifts, steering sweep and mounting/dismounting before final textures or reduction.

This brief defines a future modeling pass; the native file remains unchanged. The actual final rider mesh, clothing and rig pose must confirm the restored original layout. The [Unity mounting slice](../../Evidence/Gameplay/LeggerRiding20261014/README.md) uses a placeholder standing arrangement and capsule; it supplies no accepted seated fit. Unity import, runtime anchoring, motion, collision and performance remain separate validation work.

## Verification and scope

Hidden Blender 5.2.2 workers completed extraction and rendering without saving. Native source and prototype FBX hashes match before/after identities. GUI Blender was not reloaded or changed. No runtime files, Unity scenes, textures, UVs, topology, rig, or gameplay tests were changed. The docs/evidence are the task's only repository changes.

Deterministic world identity, streaming/unload/reload, generated-object identity and persisted runtime deltas are inapplicable because this is a read-only local source-art audit with no world objects or runtime integration. Authored constraints do apply: original rider geometry, saved manual scale, accepted leg alignment, separate major parts, one organized scene, current palette and restrained wear remain the constraints for any later implementation. Native files and render pixels follow the [local-only source policy](../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).
