# Legger top-down silhouette and mounting correction

October 16, 2026. The author requested analysis of the original top-down silhouette, cargo/bag mounting angles, frame angles and rider geometry, followed by corrections and visual checkpoints. This continues the [whole-model detail pass](LEGGER_FULL_DETAIL_PASS.md) in the same native scene. The retained dense `Legger_DetailBake_Source` controls design and rider fit; a proportional rider proxy supplies a limited comparative check.

## Findings and implementation

| Area | Original-led finding | Applied correction |
| --- | --- | --- |
| Cargo plan and height | The original carries staggered luggage, with an angled taller forward pair and a compact, nearly upright rear pair. The rebuilt model instead had long, parallel cases at the rear, reaching almost to handlebar height. | Reuse the four existing case modules. Rescale their existing vertices, move the taller pair forward/inward and put the smaller pair farther rearward. Retain bodies, straps and lids as separate editable objects and retain useful hinge origins. |
| Mounting angles | Voxel-balanced plane fits on multiple crops of the fused source indicate approximately 8 degrees of forward-case yaw and 6 degrees of inward top lean; rear planes are nearly upright. Crop variation and generated lumps prevent CAD precision. | Use symmetric forward yaw of ±8 degrees with 6 degrees of inward top lean; rear yaw ±0.75 degrees with 1 degree of inward lean. Apply dimensions to mesh coordinates and the shared rigid angle to each module root, keeping unit object scales. |
| Rack contacts | Existing shelf locations would no longer support the revised case arrangement. | Reposition the existing four beam/cushion components per side under the new case bottoms; taper the rack scaffold and retain its stays. Eight cushion-top centroid checks lie within 2.6 mm of evaluated case surfaces. This is a static proximity check, not structural certification. |
| Rear frame and plating | Straight rear rails and high white plating give a long, parallel tail. The original opens behind the rider and tapers around a higher central cooling mass. | Reshape upper rear frame rails into a tapered V, follow them with the waist/tail plating, bring the cooling mass forward and shorten the rear endpoint. Checkpoint review caught the white perimeter level with the cooling crown; lower the existing layered skins beneath it. Keep lower frame junctions and leg/steering anchors. |
| Rider envelope | The main pocket and controls already follow the original. The source rises earlier behind the rider into a narrow bolster/pouch envelope; the rebuild loses that mass and leaves excess space before the tail housing. | Preserve the forward seating pocket through Y = 0.14 m within one micrometre at checked vertices. Rework only the rear saddle/pan and upper support rows, and extend the existing cooling housing forward to close the gap. The generated rear envelope includes luggage/support forms; it is not treated as a precisely segmented seat. |

Symmetric case centers, using absolute X and positive Y rearward:

| Module | Previous center, metres | Revised center, metres | Applied mesh dimension factors X/Y/Z |
| --- | --- | --- | --- |
| Taller pair (`Main`) | 0.320 / 0.500 / 1.790 | 0.255 / 0.335 / 1.735 | 0.84 / 0.72 / 0.80 |
| Compact pair (`Aux`) | 0.305 / 0.170 / 1.735 | 0.303 / 0.635 / 1.735 | 1.04 / 1.20 / 1.15 |

## Verification and limits

The [saved-source receipt](../../Evidence/Art/LeggerPlanform20261016/README.md) binds the final native, original/before snapshots, first checkpoint, corrected candidate, rider comparison and independent native reopen. A downward-ray grid gives cargo footprint intersection-over-union of approximately **56.4% before and 80.5% after** within the recorded crop. Rear rider-envelope centerline RMS error improves from approximately **149 mm to 5.5 mm**. The main pocket stays at approximately 2.7 mm RMS. These are bounded sampling diagnostics; they do not establish complete fidelity to every generated detail. Rear-core footprint overlap drops slightly while its sampled height fit improves.

The hard budget stays **76 active major meshes, 45,762 control quads and 146,264 evaluated triangles**, with one complete scene/view layer and eleven independently movable module roots. Twenty-three active meshes receive coordinate/placement corrections; fifty-three retain their complete geometry/material/UV/world fingerprint, including all thirty leg meshes and the controls. Control connectivity, UV coordinates, materials and modifier parameters remain unchanged. All controls remain closed, nondegenerate quads. The accepted joint targets, saved scale, ground contacts and approved palette remain.

All **55 active packed maps** and all 191 packed historical image payloads remain byte-identical. No detail objects or new maps are added. Existing textures follow the reshaped parts; case resizing changes texel density. Four clean case modules approximate the generated stacked luggage masses. Final dressed-character fit, wrist roll, moving steering/joints, opening lids, mounting/dismounting, structural loads and Unity performance remain unverified.

The active local source remains `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`. All 971 preceding mesh versions remain accounted for in hidden references; an independent C: recovery preserves the preceding native exactly. The user's loaded Blender GUI is neither reloaded nor saved by the background workers. Reopen the saved source before saving from an older loaded scene.

Native sources, checkpoints and review pixels remain local under the [source-publication policy](../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). No Unity asset, `.meta`, package, export, rig or gameplay test changes. World identity, streaming/unload/reload, generated runtime identity and persisted deltas are inapplicable to this source-art task; the authored design, rider, scale and geometry-budget constraints above apply.
