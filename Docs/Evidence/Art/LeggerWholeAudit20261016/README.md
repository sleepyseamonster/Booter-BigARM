# Legger whole-model audit evidence

October 16, 2026. The author requested a visual audit of the entire original and opportunities for improvement, with no edits yet. The [full visual audit](../../../Design/Art/LEGGER_WHOLE_MODEL_VISUAL_AUDIT_2026-10-16.md) records thirteen prioritized findings and geometry-versus-texture recommendations. Implementation has not started under this audit.

## Read-only source identity

Source: `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, **799,090,806 bytes**, SHA-256 **`90757f7efa6811fbfd9c82c93b08a98d254f60d13d15e447e5eeab03f2db6304`**. It remains the preceding [front-volume checkpoint](../LeggerFrontVolume20261016/README.md). [Verification](verification.json) and [inventory](inventory.json) bind identical source hashes before and after the audit. No native save, bake, geometry/texture/material/transform edit, saved-scene change, source replacement, GUI reload or foreground interaction occurred. Only new audit records and local render outputs were created.

The current assembly retains 76 visible meshes, 45,762 quad controls, 146,264 evaluated triangles, 55 active maps across eleven sets, one scene and one view layer. Including hidden history, the file contains 1,021 mesh objects and 198 packed images. The dense original is `Legger_DetailBake_Source`, with 2,667,021 evaluated triangles. Inventory includes its original image dependencies, geometry fingerprint, active part bounds/pivots and packed payload identities.

## Visual evidence

[Views](views.json) bind **59 retained renders**: 28 original/current pairs and three current-only isolation views. Matching pairs use equivalent cameras, framing, lighting, exposure, render settings and resolution. The narrow foot top pair is 1024 × 1200; all others are 1200 square. Four initial off-center foot captures were replaced with correctly centered frames; the final camera metadata records the repair. The total retained image count does not include those superseded frames.

Whole-model captures cover front, both sides, rear, top, underside, three-quarter and an elevated stand-in. Close-ups cover the front, controls, cockpit, saddle, chassis, hip, cargo, rear housing, hoses, legs, feet and stirrups. Three clay pairs help distinguish form from surface detail. Current-only frame/drive/harness isolation cannot be matched to separate original parts because the source is fused. Far-leg and top-foot camera cuts expose occluded areas; their cut surfaces are not actual holes. The original staggered pose is intentionally different from the accepted aligned rest stance. The elevated view is not an actual Unity camera.

[Review](review.json) names the inspected sheets, selected full-size views and thirteen findings. [Image identities](image_manifest.json) bind **68 local images**: the 59 valid frames, eight sheets and `Legger_WholeAudit_Priorities.png`. Cache: `C:/Users/kbkno/.codex/art-cache/LeggerWholeAudit20261016/`. The local `Legger_Whole_Model_Audit.html` gallery provides labeled image pairs and full-resolution links; its file identity is recorded. Gallery file/image references were checked statically; no browser render or foreground interaction is claimed. Raw pixels and native files remain local under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

## Targeted fitting check

Corrected foot side/top views exposed unsupported inner tie-bar ends. [Foot endpoint evidence](foot_connections.json) samples eight cap centers from the retained foot authoring recipe against connected native control-surface components. The forward centers are about 41.3 mm from the nearest other component surface, and rear centers about 28.2 mm; all are outside the other foot components under the recorded ray-parity check. These exceed their roughly 10 mm cap radius and support the visual fitting finding. This is an auxiliary unsigned point/control-surface test, not a complete evaluated solid-intersection, engineering-load or motion certificate. No repair was made.

The current right foot footprint is approximately 0.680 × 0.296 m, versus 0.687 × 0.295 m for the original right-foot crop below Z = 0.18 m. This supports improving connections and section weight rather than globally enlarging the feet. Bounding envelopes are not contact polygons; the original left-foot displacement comes from its pose.

[Local recipes](local_recipe_manifest.json) bind the ignored capture, inspection, comparison and record-building scripts, the original foot-authoring recipe used for endpoint coordinates, and measured inputs. Workers used Blender 5.2.2 LTS with auto-execution disabled and temporary discarded render overrides.

No Unity asset, metadata, package, export, rig, collider or gameplay test changed. There is no new animation, rider-fit or Unity runtime/performance proof. The original's concealed construction and device functions remain visual interpretations; recommendations do not establish technology or game canon. World identity, streaming/unload/reload, generated runtime identity and persisted deltas are inapplicable to a read-only source-art audit. Scale, palette, accepted stance, contacts and current geometry ceiling remain constraints for proposed later work.
