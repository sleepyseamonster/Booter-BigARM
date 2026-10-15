# Material diorama execution record

October 15, 2026. Sable. Implementation authorized by the author's instruction to pursue the plan as a goal. This record starts with the execution contract; final proof and art review belong in RESULTS.md after capture and inspection.

## Audit corrections adopted

1. Separate **72 common-geometry material images** from **8 workyard form-pilot images**. A/B share the original forms; C consolidates broad rock planes and slightly widens selected housings; D reduces rock facets and emphasizes those silhouettes further. These are bounded original interpretations, not exact game styles.
2. Preserve the earlier B atlas without editing it. Use its exact ground-panel bounds and color grading in a legacy coupon. Compare that with a phase-blended derivative on matching geometry and aligned texture coordinates. The derivative repairs repeated structure; loose fines and gravel remain proposed extensions. Earlier image-generation provenance remains in the [original receipt](../2026-10-15-stylized-realism/render_receipt.json).
3. Split detail inspection into metal (6), stone/ore (5), and ground/ceramic (4) plates per candidate. Every ID maps to the same flat/bevelled slab plus a curved coupon, under shared neutral light. The manifest maps those IDs to dressed-scene objects too.
4. Lock the authored elevated-camera proxy to the production scene's serialized 22 m / 50-degree pitch / 40-degree yaw / 1.15 m target offset / 48-degree vertical FOV values. Study output is explicitly 16:9. This does not claim to reproduce saved user camera preferences or runtime obstruction, look-ahead, or riding states.
5. Inspect all four candidates in both world lights and add four neutral full-workyard diagnostics. Accept shared lighting only when these comparisons remain interpretable; no per-candidate exposure changes.
6. Pack image dependencies into native sources. Inventory them on reopen and reproduce a retained frame from a clean scratch copy. Record script, source, native, image, camera, geometry, and material-graph fingerprints.

The revised deliverable is **97 primary images**: 72 scene comparisons + 12 material plates + 4 neutral diagnostics + 1 ground control + 8 form-pilot images. Preview frames are additional working evidence. No new Unity integration, package, terrain, or runtime behavior is part of this scope.

## Material provenance and craft

Geometry is authored through Blender primitives, edited rock meshes, and the sealed existing SculptedBasalt01 FBX. Materials combine original procedural color, mineral/oxide masks, roughness, and bump graphs with the prior generated B/C atlases for selected stone/ground surfaces. They have a painted appearance; they are not claimed as manually hand-painted production textures. No new external artwork or asset library is downloaded.

M01–M06, S01–S04, G01–G03, O01, and C01 are study labels. Their object-to-material mapping is retained in the receipt. Rust masks use the same spatial coverage across candidates; oxide regions have a different metallic/roughness response from bare substrate. Rock/dirt grain uses a common metre-space origin, avoiding arbitrary texture changes with object size. Larger gravel is geometry.

The control preserves the source atlas pixels, panel bounds `(649, 1444)` in its 2172-wide image, and the earlier desaturation/multiply recipe. Its small coupon has a newly declared metre scale; it is not a re-render of the previous whole terrain sheet. B's new ground blends two phase-shifted samples to reduce repetition. The accepted aesthetic is the anchor; the repair and extensions still need review.

## Execution and storage

- Background Blender 5.2.2 LTS; OptiX NVIDIA GeForce RTX 3060 Ti detected through the installed executable. API input names were checked against that executable.
- One renderer process at a time. Preview: 960 × 540 / 12 samples. Initial final setting: 2560 × 1440 / 40 samples, denoised, Cycles, AgX Medium High Contrast, exposure 0. These are offline settings, not Unity proof.
- Local source: `SourceArt/Blender/Studies/Sable/MaterialDioramas20261015/`. Native files and backups remain ignored by existing policy.
- Working output: `outputs/Sable/MaterialDioramas20261015/`. Added only this path to local `.git/info/exclude`; unrelated output remains visible to Git.
- Initial D: free space was about 4.25 GB. Do not delete unrelated data. The renderer checks remaining space before each frame and stops before writing if below 700 MB. Retain lightweight review derivatives in this task folder; raw renders and source stay local.
- The first workyard preview showed excessive empty framing and hard dust-patch boundaries. The next pass added supporting structures and canyon masses, reused the sculpted rock, and feathered deposit opacity. These revisions preserve the fixed gameplay camera.

## Reproduction commands

From the checkout root, invoke the installed Blender in background mode with `--factory-startup --python-exit-code 1 --python Docs/Agents/Sable/work/2026-10-15-material-dioramas/tools/build_study.py -- <checkout-root>`. `--preview` produces gameplay previews, and `--scene yard` bounds a pilot; a complete final run omits both. Always use `--python-exit-code 1` so script failures do not look successful merely because Blender exited cleanly.

The builder saves an intermediate controlled native source before capturing, then emits separate B/C/D whole-style native variants. The required `seal_native.py` pass retains all unassigned candidates with fake users and packs the four files. `repair_orbits.py` extends the distant background for the 24 rear views. Reopen verification and packaging follow these steps. The complete sequence, actual proof, capture-order lighting limitation and art review are recorded in [RESULTS.md](RESULTS.md). All 97 primary captures and 25 review groups are complete.
