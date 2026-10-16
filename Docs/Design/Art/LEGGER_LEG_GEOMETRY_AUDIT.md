# Legger original leg geometry audit

October 15, 2026. **Audit only. No Blender source, geometry, pose, texture or saved scene setting was edited.** The author accepted the latest sci-fi pass as progress and requested a closer examination of the original leg geometry, height/width ratios and angles before reworking the legs. This document is a future modeling brief, not an applied rework or a rig specification. [Evidence and source identity](../../Evidence/Art/LeggerLegAudit20261015/README.md) accompany it.

## Main finding

The current rebuild misses a major structural feature of the original: **three silhouette-bearing leg links and two alternating bends between the body socket and ankle**. The rebuild has two long links and one almost straight bend. Curving the present armor further or adding more subdivisions will not recover that arrangement.

The original reads as a short forward-swept upper section, a longer rear-swept middle section, and a shorter forward-returning lower section above an arched, broad foot. The machinery is layered around that changing direction: ivory armor overlaps dark cores, a rear actuator spans the middle section, and the distal pivot stands out above the narrow lower shaft. These are shape relationships worth recovering. The original is a generated, fused mesh with no verified articulation axes or engineering skeleton; three visible links do not by themselves prove a particular three-bone rig or working linkage.

## Reference and measurement limits

The authority is the retained **dense original**, `Legger_DetailBake_Source`, 2,667,021 evaluated triangles, at its saved world transform inside the current native file. The earlier `Legger_LeftLeg_Baked` and `Legger_RightLeg_Baked` objects were also inspected for lower detail, but their segmentation cuts off around **Z=1.35 m**. They omit the upper attachment and must not define complete leg height or joint count.

Fresh orthographic full-model, opposite-side, front, half-body side and socket close views were rendered without rescaling either version. The half views use a camera far plane at X=0 to reveal the near leg; they do not modify the mesh. The left side is mirrored only in the comparison board so both silhouettes face the same way. Foot plan views use a camera cut above Z=0.18 m to remove the overlying leg. Their cut surfaces are not evidence of holes in the source mesh.

Use X for body width, forward **−Y**, and Z up. Original landmarks below are analyst estimates in the **Y/Z side plane**: **P** body socket region, **K** upper forward bend/armor junction, **H** lower rear pivot or hock, **A** ankle. P and K are partly covered or non-circular; plausible coordinate tolerances are ±30 mm and ±25 mm respectively. H and A are visible cap centers, estimated within about ±10 mm. These are assumed reading tolerances, not surveyed errors or statistical confidence. Current joint centers are known authoring pivots. Lengths below are side-plane lengths; they are not exact 3D bone lengths. Keep rounded values in a future blockout and solve actual pivot axes on clean geometry.

## Height and length ratios

Overall model heights are effectively the same: dense original **2.2277 m**, current **2.2244 m**. This is a leg proportion and layout issue, not a request to rescale the whole machine.

| Landmark | Original estimate, Z | Current counterpart, Z | Implication |
| --- | ---: | ---: | --- |
| P, body socket | 1.46 m | 1.32 m hip | Original upper attachment is roughly 0.14 m higher and more integrated into the body. |
| K, upper forward bend | 1.205 m | Absent | Recover the short upper section and the first change of direction. |
| H, lower rear pivot | 0.68 m R / 0.715 m L | 0.80 m knee | The current single middle joint is higher than the original distal pivot. These are different layouts, not one-to-one named bones. |
| A, ankle | 0.235 m | 0.170 m | Original ankle sits roughly 65 mm higher above its foot. |

The socket's estimated height is about **65.5%** of total original height, versus **59.3%** in the rebuild. The approximate P-to-A vertical drop is 1.225 m in the original and 1.150 m currently. Moving the future socket upward would require a bounded refit of the chassis hip mounts; it cannot be solved entirely inside the present leg shells while preserving every old pivot.

The clearer forward-standing R leg supplies a useful *visual* proportion guide:

| Side-plane link | Nominal original length | Fraction of chain |
| --- | ---: | ---: |
| P→K, short upper | 0.29 m | 21% |
| K→H, middle | 0.56 m | 40% |
| H→A, lower | 0.53 m | 39% |

The current two links are approximately **0.523 m and 0.632 m** in side projection, or 45%/55% of that two-link chain. Its lower link is about **19% longer** than the sampled original R lower section. This contributes to the long-shin appearance; the absent short upper section is the larger structural loss.

Do not average the two posed source legs into an exact skeleton. The L source path measures roughly **0.28 / 0.89 / 0.49 m** under the same landmark interpretation, with a much longer-looking middle section. The difference reflects the source's asymmetrical pose, generated forms and uncertain linkage landmarks. No original rig establishes equal rigid lengths. Recover a coherent common design, fit one clean template, then mirror it; do not preserve the source's apparent unequal limb lengths.

## Side angles and front flare

Angles here are slopes of the descending link from downward vertical. **Negative slopes move forward (−Y); positive slopes move rearward (+Y).** Joint deflection is the change in slope, not an Euler rotation to enter in Blender.

| Descending link | Original R | Original L | Current |
| --- | ---: | ---: | ---: |
| Upper P→K | about −29° | about −24° | about +6° hip→knee |
| Middle K→H | about +19° | about +56° | No separate middle link |
| Lower H→A | about −33° | about −9° | about −5° knee→ankle |

The original R path therefore changes direction by roughly **48° at K and 53° at H**; the current path has only an **11°** deflection. This identifies the missing zigzag and the much stronger forward return above the original foot. The original L is more folded and staggered; it is evidence of the design under a different generated pose, not a second exact rest-angle target.

These nominal angles have different confidence. Propagating the stated coordinate boxes gives R upper-link sensitivity of approximately **−44° to −15°**, middle **+15° to +24°**, and lower **−36° to −30°**. The short upper link's exact angle is particularly uncertain because its covered socket is not a recovered axle. A future modeling pass should preserve the direction changes and proportion ranges, then verify real joints and clearances, rather than locking the nominal −29° value as mechanically exact.

The front view also needs attention. Fits through unclipped transverse-envelope section centers give original lower-shaft outward slopes of about **4.6° R / 5.8° L**, versus **1.36°** currently. The original middle-section readings vary with pose (roughly 3.6° R / 9.9° L); the current upper section is 3.85°. These are shape slopes, not recovered joint-axis cant. Preserve readable outward taper/flare with both leg planes matched; do not infer toe-out or copy the source's lateral asymmetry as rig truth.

## Width, depth and feet

Cross sections use actual evaluated triangle-edge intersections with planes normal to the estimated side-plane links. The original measurements include the selected body half and a ±0.15 m corridor around each link, so nearby actuator/cover geometry can contribute. They measure **assembly envelopes**, not exact ivory plate widths or wall thickness. The upper original sections touch the corridor/body-center cut and are excluded from precise width targets. The following midpoint sections are clear of those cut boundaries:

| Section | Width across body, X | Depth in side plane | X/depth |
| --- | ---: | ---: | ---: |
| Original R middle K→H | 0.134 m | 0.164 m | 0.82 |
| Original R lower H→A | 0.099 m | 0.123 m | 0.80 |
| Current upper hip→knee | 0.198 m | 0.142 m | 1.40 |
| Current lower knee→ankle | 0.198 m | 0.134 m | 1.47 |

The present lower shaft is approximately **twice as wide across the body** at the sampled midpoint, while its side-plane depth is only about 9% greater. The cross-section balance has changed: the original narrows across the body and carries shaped depth along the walking direction. The rebuild spreads across X with comparatively shallow side depth. Its white armor also reads as narrow border strips in the frontal view, whereas the original has substantial continuous ivory faces wrapping toward the front. Recover that coverage and section orientation; merely narrowing every coordinate uniformly would not solve it.

Foot envelopes are more definite because they are bounded near the ground:

| Foot | Length along Y | Width across X | Length/width |
| --- | ---: | ---: | ---: |
| Dense original R, below Z=0.18 m | 0.687 m | 0.295 m | 2.32 |
| Dense original L, below Z=0.18 m | 0.657 m | 0.296 m | 2.22 |
| Current R foot object | 0.525 m | 0.188 m | 2.79 |

The current R envelope is approximately **24% shorter and 36% narrower** than the original R. Ground-band checks below 35 mm retain approximately the same original spans, so these broad extents are not just the raised ankle cap. These are geometry bounding envelopes, not a contact polygon or stability analysis. Foot center spacing is already close: original envelope midpoints are about ±0.281 m in X, current ±0.275 m. Preserve the mirrored stance concept; enlarging/recovering the foot shape is more justified than arbitrarily spreading the leg roots.

The original feet use substantial arched forward forks, a long rear support, central/lateral pads and open mechanical bracing. The current rear support is short and reads as a small heel; its long forward runners are simple and parallel. Retain the original's broad base, more deliberate toe taper, arch transitions and bracing as major silhouette forms. Tread grooves and tiny hardware can remain texture detail. Plan-silhouette principal-axis yaw is about 9.2° R / 4.4° L in the original and essentially 0° currently. Those PCA readings describe the posed shape, not toe-joint axes or a command to introduce unequal toe-out.

## Future leg rework brief — not implemented

1. **Establish the whole three-section silhouette first.** Use the dense half-side views and front view together. Recover the compact upper section, long swept middle and shorter forward-returning lower section, without transferring the original staggered pose.
2. **Build one coherent neutral template and mirror it.** Both sides should share segment lengths, joint heights, sagittal bend directions and foot placement. Aligned legs can retain deliberate bends in the same side plane; alignment does not require flattening the original design into two almost vertical bars. Keep toes forward for the rest stance.
3. **Resolve the socket and actual joint axes before final topology.** The audit points identify silhouette landmarks, not a verified skeleton. Fit purposeful hinge/cover geometry and actuator attachments around the new chain, and adapt the hip mounts if the higher original attachment is adopted. Existing exact neutral pivot numbers would then need a documented replacement; they cannot all remain fixed while restoring this layout.
4. **Correct the sections and armor coverage.** Recover the narrower lower shaft across X, useful fore/aft depth, tapered transitions, strong original joint-to-shaft contrast, and continuous front/outer ivory armor. Keep dark mechanical recesses and actuator channels legible from front, side and oblique views. Avoid uniform cylinders or flat ivory strips.
5. **Recover the foot envelope and ankle relationship.** Use roughly 0.66–0.69 m length and 0.29–0.30 m overall width as source measurements to test in a future blockout, not an automatic scale operation. Rebuild the major forks, rear support and pad arrangement around matched forward feet. Recheck ground contact and the higher ankle.
6. **Model clean quad control surfaces, then texture.** Use loops along the bend transitions, wrapped plate returns, joint openings and toe arches. Keep major moving links separate and group small fixed hardware. Verify silhouette and section ratios in clay before committing to UVs or rebakes. Retain the current palette and restrained wear. Choose density after the restored forms work; no percentage reduction is justified by this audit alone.

The comparison boards and measurements provide a concrete basis for the next modeling request. This audit does not authorize or apply that implementation. The current one-scene assembly, neutral stance, all packed textures, source references and recovery files remain intact. No Unity import, rig, motion/clearance test, collider, LOD, gameplay smoke test or world-system change was performed. World identity, streaming/unload/reload, generated-object identity and persisted runtime deltas do not apply to this source-art inspection; authored scale, geometry, source preservation and the user's aligned-stance requirement control the analysis.
