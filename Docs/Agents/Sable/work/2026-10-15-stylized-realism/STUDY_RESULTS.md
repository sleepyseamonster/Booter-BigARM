# Stylized realism comparison results

October 15, 2026. Sable's material study, authorized by the author's request to show the same rock, ground, and prop in three treatments. Final creative selection remains with the author.

Open [the comparison viewer](comparison.html) to switch between four views and two lighting conditions. The [sunset close board](boards/sunset_close.png) is the quickest overview; the [canister detail board](boards/sunset_prop.png) shows the smaller prop at a useful size.

## The three candidates

| Candidate | Treatment | What to judge |
| --- | --- | --- |
| A | Grounded realistic baseline using the existing basalt and soil imagery, fine normal detail, and a simple lit ceramic and metal canister | Material response, grain, and the amount of detail carried by the original source maps |
| B | Subtle painted albedo drafts with retained small structure and reduced fine relief | Whether painting improves grouping while keeping stone, dirt, ceramic, and metal distinct |
| C | More visibly painted albedo drafts with grouped facets, scumbled edges, and much less fine relief | Whether stronger brushwork remains convincing at gameplay distance and under an alternate light |

Geometry, transforms, prop construction, small-stone placement, camera, lighting, exposure, and color management match across A, B, and C in each set. Surface color, wear, and relief treatment differ. These are three authored material candidates, not a calibrated single-variable experiment: generated maps introduce some differences in local value and wear as well as brushwork.

## Assets and scale

The main mesh is the repository's `SculptedBasalt01_LOD0`, imported from its existing FBX without remodeling. Blender reports dimensions of approximately 3.177 by 3.235 by 4.961 metres. Its height is retained; the canister is approximately 1.2 metres tall and supplies a smaller manufactured object for comparison. The canister is a newly authored study prop, not a proposed canonical item or production delivery.

The ground is a fixed, gently relieved mesh using the existing compacted rust-soil image. Its saturation and brightness adjustments are shared by all candidates. This is a small material review stage, not a copy of Greater Wasteland terrain. No character was imported: the author's requested rock, ground, and prop are the comparison subjects.

## Views and rendering

- Close review: 11 metres, 38 degree pitch, 40 degree yaw.
- Gameplay framing: 22 metres, 50 degree pitch, 40 degree yaw. The distance, pitch, yaw, target height, and 48 degree vertical field of view follow the inspected production camera settings.
- Alternate orbit: 16 metres, 26 degree pitch, 115 degree yaw.
- Prop detail: 3.1 metres, 30 degree pitch, 35 degree yaw, reframed onto the canister.

Every view is rendered under a brighter sunset and a deeper twilight setup. These are controlled study lights, not reproductions of production Unity lighting. All frames use Blender Cycles, 24 samples with denoising, AgX with Medium High Contrast, zero exposure adjustment, and 1440 by 1080 pixels. Grayscale is an analytical companion to the color board.

## Texture provenance and limits

The built-in ImageGen tool produced [B's albedo atlas](textures/B_subtle_atlas.png) and [C's albedo atlas](textures/C_painterly_atlas.png), using the existing rock and ground images as references. The exact prompts and source paths are preserved in [PROMPTS.json](textures/PROMPTS.json). Each atlas contains rock, ground, and ceramic panels. These are AI-authored drafts with a hand-painted appearance, not manually painted finished textures. Metal bands retain the same material family with restrained procedural variation.

The generated B atlas did not obey the requested equal panel widths. The render script uses observed panel boundaries rather than assuming exact thirds. Original rock UVs wrap before atlas remapping; a baseline-color fallback protects areas sampling generated black padding. These measures prevent material-panel bleed but do not establish production UV seam quality. Seam cleanup, texture dilation, tiling refinement, map calibration, and Unity import remain later asset-production work.

The original basalt maps are image-derived study assets rather than measured scans. A is therefore a realistic rendering baseline for the available repo content, not a claim of photogrammetric accuracy. B and C use original rock roughness with reduced normal strength; they are not complete independently authored PBR sets. Roughness variation and baked lighting within albedo can influence the result.

## Sable review

The recommended starting point is between B and C: keep B's restraint across broad ground areas, with C's larger painted accents on selected rock surfaces and manufactured props. C makes the painted treatment more legible in the close views, while the common geometry still carries the object's volume. This is an art recommendation, not the author's selection.

At the 22 metre gameplay framing, the canister's shape and color carry more information than its brushwork. The prop-detail view is necessary to compare its finish. The twilight setup suppresses surface distinctions in all three candidates; material approval should use the sunset views alongside twilight, rather than treating darkness as successful simplification.

Remaining study limitations include visible repeated ground structure, large wear differences on the canister, and a broad texture transition on the rock in some close angles. The low orbit reveals the edge of the isolated ground stage. These are authoring and stage limitations, not evidence that a painterly production style needs those artifacts. Refine the chosen candidate's seams, placement-aware wear, and tile variation before a Unity material handoff.

All eight color boards were visually inspected. The saved native Blender source reopened successfully and its mesh inventory was checked; the intentionally open ground sheet is distinct from the closed rock and prop meshes. The HTML viewer loaded all 24 images, and its lighting and view controls were exercised in a background browser. See [verification.json](verification.json) for the compact check record.

## Preservation and reproduction

[render_receipt.json](render_receipt.json) records source hashes, atlas hashes, geometry fingerprints, camera and light settings, and individual render hashes. The renderer checks unchanged geometry for every frame and rechecks source files at completion. The packaging script checks every render hash, image dimensions, and matching camera and light conditions for each three-image group. The review boards only resize and arrange the rendered frames; no painterly postfilter is applied.

Rebuild from the checkout root with Blender in background mode and [render_comparison.py](tools/render_comparison.py), passing the checkout root after `--`. Then run [package_comparison.py](tools/package_comparison.py) with the existing Python environment and Pillow. Preserve the same atlas files to reproduce the material inputs; generating them again will not reproduce their exact pixels.

The editable native source is retained locally at `SourceArt/Blender/Studies/Sable/StylizedRealism20261015/Sable_StylizedRealism_Comparison.blend`. It remains ignored and local-only. No Unity production asset, source map, scene, package, or project setting was edited. The study has no runtime placement, deterministic world identity, streaming, object-ID, or persisted-delta integration.
