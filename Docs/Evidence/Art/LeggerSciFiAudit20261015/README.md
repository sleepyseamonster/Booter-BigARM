# Legger sci-fi form audit evidence

October 15, 2026. The author rejected the overly simplistic major forms of the modular rebuild, identified the crates as its strongest remaining area, and requested an overhaul toward the retained original futuristic sci-fi design. The requested audit is complete. [Findings, part decisions and execution brief](../../../Design/Art/LEGGER_SCI_FI_OVERHAUL_AUDIT.md) distinguish the completed audit from the next modeling pass, which has not been applied.

## Source and cleanup

The sole active native source remains `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, 747,805,680 bytes. SHA-256 before and after both background inspection processes is `c5eadd0501631bbace05551e1453c43a14db55df1365b07a77755cbf80b51be5`. No native file was saved or created by the audit. One scene (`Legger`), one saved view layer (`LEGGER - COMPLETE`) and the complete assembled default remain intact. Prior references were revealed only in process memory to obtain their real transforms and renders.

The [modular technical receipt](../LeggerModular20261015/README.md) remains valid for that exact source identity. Its technical completion is not final visual acceptance. The current audit supersedes its appearance as the desired finished direction and the previous plan's preservation of the main body fairings.

Local recovery/construction files under `C:/Users/kbkno/.codex/art-cache/LeggerChassis20261015/` remain distinct from the active source: `Before.blend`, `Chassis.blend`, `Modules.blend`, `Consolidated.blend`, `ModuleBakeSources.blend` and `TexturedModular.blend`. The latter is the application candidate retained from the completed technical pass, not a second active authoring file. These sources and the original geometry are protected references for the overhaul. Cleanup did not delete them or create duplicate sources. Native files and task `Logs/` remain ignored under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

## Direct inspection

[Inventory](audit_inventory.json) records the saved-transform bounds, evaluated counts and all 67 current object names/pivots. Hidden reference collections and their layer hierarchy were temporarily included before evaluating transforms. The comparison did not apply per-version rescaling or move the objects into separate review scenes.

| Version | Meshes compared | Evaluated triangles | Approximate dimensions, metres |
| --- | ---: | ---: | --- |
| Current modular rebuild | 67 | 86,072 | 0.8660 × 1.5965 × 2.2260 |
| Retained earlier baked design | 4 | 155,999 | 0.8591 × 1.6457 × 2.2274 |
| Dense original design source | 1 | 2,667,021 | 0.8586 × 1.6463 × 2.2277 |

Current control faces remain 21,396 quads; this was recounted from the source, alongside the evaluated totals. Existing manifold, UV, texture and preservation proofs are linked through the prior receipt and were not misrepresented as new full validation runs. Hash identity ensures this audit inspected the same source.

[Part actions](part_actions.json) gives every current mesh an overhaul/refine/retain decision: 37 major form rebuilds, 16 supporting refinements and 14 retained starting parts, including all 12 cargo parts and their two racks. The author called the crates semi-decent, so retention is a starting point rather than final creative approval. No part action is represented as an applied edit.

## Captures and review

Two hidden Blender **5.2.2 LTS** processes produced **19 captures** using Cycles/OPTIX, 28 samples, denoising, 1280 × 1280 PNG, transparent film, AgX with exposure 0 and gamma 1. Each pair uses identical camera/framing, lights, world and exposure. The proof records camera centers, directions and orthographic scale. The elevated camera is an offline proxy rather than the actual gameplay camera.

- Twelve textured captures: original/current × three-quarter, side, front, elevated, body and legs.
- Six clay captures: original/current × three-quarter, body and legs. One common nonmetallic rough clay override removes texture/normal-map differences from this comparison.
- One dense-original three-quarter capture using the same three-quarter setup. It was inspected to confirm the earlier baked design remains a useful form reference; no pixel-equivalence claim is made between different source meshes/materials.

All captures and three assembled boards were opened and visually inspected. [Image manifest](image_manifest.json) records all 22 local PNG sizes, byte lengths and hashes. Local review paths are:

| Local file under `Logs/LeggerSciFiAudit20261015/` | Purpose |
| --- | --- |
| `Legger_Original_vs_Current.png` | Whole-machine textured comparison, original left/current right |
| `Legger_Form_Overhaul_Audit.png` | Shared-clay comparisons for assembly, upper body and legs |
| `Legger_View_Comparisons.png` | Matched front/side/elevated pairs |
| `dense_original_threequarter.png` | Dense-source visual cross-check |
| `audit_render.py` | Read-only inventory/camera/render recipe; `--dense-only` produces the dense-source cross-check |
| `make_boards.py` | Deterministic comparison-board assembly from actual renders |

The review identifies missing crown/taper, simplified slab-like body and limb shells, unrelieved disk joints, ramp-like saddle, blocky cockpit and rake-like feet. These differences remain visible with materials overridden and at the elevated proxy. Larger coherent forms and deliberate layers are the leading correction; more tiny fittings or noise would not resolve them. See the brief for exact treatments and their verification views.

The processes exited normally. Blender emitted its known HIP-library initialization warning before the configured OPTIX render and two forward-looking `use_nodes` deprecation warnings; no render failure occurred. The saved source hash stayed unchanged. No UI was focused, no Unity content changed, and no import, animation, mechanical, collision, player/platform performance or gameplay smoke test is claimed.
