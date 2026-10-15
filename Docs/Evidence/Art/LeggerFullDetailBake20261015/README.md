# Legger full detail bake

The [subsequent protected refinement](../LeggerDetailRefinement20261015/README.md) now describes the current authoring file: 155,999 active triangles and 28 packed maps. Counts and hashes below record the preceding whole-robot bake checkpoint.

October 15, 2026. Owner: Blender under Gottspan coordination. The author requested extracting more surface detail into textures so the robot could continue losing polygons while retaining its appearance. This checkpoint extends the earlier cargo/saddle bake to the remaining frame and both legs.

## Applied authoring file

The sole current model remains `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`. It is local-only and ignored, including its packed textures. The file now contains these active robot objects:

| Object | Triangles | Packed maps |
| --- | ---: | ---: |
| `Legger_Frame_Baked` | 99,999 | 7 |
| `Legger_LeftLeg_Baked` | 30,000 | 7 |
| `Legger_RightLeg_Baked` | 30,000 | 7 |
| `Legger_CargoSeat_Baked` | 12,000 | 4 retained |
| Total active robot | 171,999 | 25 |

Active geometry falls from 2,096,137 triangles to 171,999, a 91.7945% reduction from the preceding cargo-bake checkpoint. Counts exclude the scale mannequin and hidden bake reference. The complete 2,667,021-triangle `Legger_DetailBake_Source` remains hidden and nonselectable in the same file for future rebaking. Export only the four active robot objects. The temporary comparison body was removed from the applied file.

The artist's transforms, ground origin and uniform scale 0.7425782084 remain unchanged. This is an appearance-review checkpoint toward a game asset. It does not establish deformation topology, rigging, collision, LODs or Unity readiness.

## Geometry and textures

The previous body was partitioned by world-space face center: frame at z >= 1.34 m, then left/right leg by x < 0 or x >= 0. These are bake regions, not finished articulated components. Boundary/non-manifold vertices plus one adjacent ring were pinned with zero collapse weight. The [preparation receipt](geometry_preparation.json) records exact preservation of their coordinates. Collapse reduction retains projecting forms and protected interfaces; it does not reconstruct quad animation loops or repair every inherited mesh defect.

Each new region has a fresh UV atlas and seven newly generated map roles: Base Color, Roughness, Metallic, tangent Normal, object Normal, AO and Height. These maps derive from the detailed source geometry and its original packed material. They preserve the existing design rather than inventing replacement artwork. The frame's color and tangent normal masters are 8192 square; all other new maps are 4096 square. The earlier four cargo/saddle maps remain 4096 square, with identical packed payloads and unchanged cargo geometry verified against the inspected input.

Color, roughness, metallic and tangent normal feed the visible PBR shader. AO, object normal and height are retained as unconnected authoring/export references. AO is not multiplied into the material, and height does not displace the mesh. Tangent normals are Non-Color and use the named atlas; color is sRGB and the remaining data maps are Non-Color. All 25 active maps are packed, so external bake-cache files are unnecessary to open the model. These are large authoring masters, not final runtime texture budgets. The fragmented preserved boundary patches also leave inefficient atlas packing; a deliberate retopology/UV pass can improve that later.

Blender 5.2.2 LTS / Cycles OPTIX generated selected-to-active maps with cage extrusion set to 0.018, maximum ray distance 0.07 and 16-pixel Extend padding. Color/data baking uses one sample; the AO node uses 16 samples and distance 0.035. The larger frame bake ran in an isolated scene to fit available memory, then its object/material/maps were merged with the successfully baked legs and unchanged cargo. References: [Blender baking manual](https://docs.blender.org/manual/en/latest/render/cycles/baking.html) and [collapse implementation](https://github.com/blender/blender/blob/main/source/blender/bmesh/tools/bmesh_decimate_collapse.cc). Installed behavior was checked locally.

Height is an auxiliary approximation from projected source position minus target position, along the target normal. Its neutral value is 0.5 and one encoded unit is 0.04 m, representing +/- 2 cm. Invalid/non-finite samples, projection displacement over 9 cm and normal displacement over 2 cm are neutralized. Nine tested frame-core texels, 126 left-leg texels and 65 right-leg texels were neutralized; see [height validation](height_validation.json). This does not certify authored displacement accuracy. The packed 32-bit RGB EXRs use lossless ZIP compression; decoded float32 pixels were verified identical before and after compression.

## Verification

- [Final mesh/material report](final_report.json): triangles, transforms, bounds, map dimensions, colorspaces, connections and packed sizes.
- [Coverage report](coverage_report.json): no uncovered texels in the tested one-pixel-eroded UV interiors, including frame 4K/8K and both 4K legs. Padding, mips and every continuous surface point are outside that test.
- [Seven-view silhouette report](silhouette_report.json): matched 1400 x 1100 front, side, rear, frame, legs, feet and underside renders. Projected overlap spans 99.6888–99.8751%. These samples do not prove zero silhouette loss from every direction.
- [Application receipt](application.json): the inspected source hash matched immediately before atomic replacement of the same authoring path. Only one model file remains in the authoring folder.
- [Native reopen](native_reopen.json) and [saved-render comparison](saved_render_comparison.json): the applied file reopened successfully with 171,999 active triangles, 25 packed maps, matching transforms/UVs, hidden detailed source, and unchanged cargo mesh/maps. It was rendered again from the saved file.

Matched textured close-ups were visually inspected, including the seat/frame, cables, armor, legs, feet and underside. They retain recognizable surface wear, seams and mechanical relief. Small changes remain at grazing edges, narrow cables and shading transitions. Maps cannot reproduce removed geometry's physical silhouette, parallax or cast shadows exactly. The frame retains more geometry than the first trial because that trial visibly coarsened the controls and armor outlines.

Seeded nearest-surface comparison used 10,000 samples per direction on the body: 5,000 vertices and 5,000 triangle centers, uniformly by index. Mean distances are approximately 0.253 mm source-to-reduced and 0.235 mm reduced-to-source; maximum sampled distances are 3.340 mm and 1.394 mm respectively. These world-space samples are not area-weighted, an exhaustive bound or a Hausdorff guarantee.

Raw renders, cache maps, working scripts and the before/after comparison remain under ignored `Logs/LeggerFullBake20261015/`. The comparison image is `Review_Final/comparison.png`. Blender files and these caches remain local under the [source publication policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md); only small receipts are committed. No Unity asset, scene, import or gameplay test was changed or run.

The already-open Blender GUI was not reloaded or focused. Use **File > Revert before saving from that window** to load the updated file and avoid replacing it with the older in-memory scene.

World identity, streaming/unload/reload, generated-object identity, authored placement constraints and persisted runtime deltas are inapplicable here because this local mesh/material operation changes no gameplay objects, terrain, placement or save data.
