# Badlands realism authoring, 2026-09-30

Target: improve the same `BrokenWorldBadlandsStudy.blend` through inspected passes against the user's fractured-outcrop and exposed-bedrock references. Keep low, open terrain with distinct shale and red-dirt regions. The references' tall cliffs are background cues, not the local height target.

## Audit

- The large rocks are radially tapered, smoothly shaded masses. Fine displacement leaves the rounded primary silhouette intact.
- Ground images suggest shale but do not provide the overlapping plate edges or contact shadows visible in the references.
- Separate rock clusters have too little intermediate debris joining large rocks to fine ground texture.
- An orange sun and orange LDR environment tint nearly every surface together. Existing materials lack coordinated height and roughness variation.
- The 360 m surface and panoramic background have a visible finite join. Close and wide terrain views are the immediate acceptance views; complete distant-landscape authoring remains a larger task.

## Current application status

Pass one was applied through the live Blender console and saved in the same working file. The Mac then locked and computer control reported that automatic unlock was unavailable. Passes two through five have been applied and inspected in a temporary render candidate at `/tmp/badlands-realism-iterations/BrokenWorldBadlandsStudy.blend`. They are **not yet in the open working scene**. Do not overwrite the working file externally; apply the functions in the open scene after the user unlocks the Mac, then save and verify it.

## Passes and evidence

| Pass | Work and inspection | Evidence |
| --- | --- | --- |
| Baseline | Smooth tapered rocks, sparse connecting debris, uniformly orange light, and a finite ground/background join. | [Baseline wide](RealismBaselineWide.png) |
| 1, applied live | Replaced 245 formation, stone, and talus meshes with fractured planes, irregular wedge tops, shallow joints, and chipped edges. A first overly columnar draft was revised before live application. Originals remain in a hidden archive collection. | [Close inspection](RealismPass01Close.png) |
| 2, candidate | Added 189 exposed shale plates, clustered fine/medium debris, small sand deposits, a distant surface continuation, coordinated bump/roughness, and separated sun/environment lighting. Cycles inspection exposed pale soil and overly smooth rock tops. | [Rejected material balance](RealismPass02Close.png) |
| 3, candidate | Replaced the smooth top texture with the fractured texture, cut shallow irregular fissures into upward faces, and restored burnt-rust ground color. | [Correction draft](RealismPass03Draft.png) |
| 4, candidate | Added low irregular middle-distance erosion banks and 140 shared strata instances. Central terrain remains low and open. Cycles wide inspection exposed a hard sky band. | [Close](RealismPass04Close.png), [wide with clipping defect](RealismPass04Wide.png) |
| 5, candidate | Extended review-camera far clipping from 1 km to 10 km and made atmospheric density decay with height. This removes the hard horizontal atmosphere band; exposure increased slightly for readable shadow detail. | [Ground-level candidate](RealismCandidateGroundLevel.png), [close candidate](RealismCandidateClose.png), [wide candidate](RealismCandidateWide.png) |

The review renderer uses fixed close/wide/ground-level cameras. Final candidate close and ground-level images use Cycles Metal, 48 samples and denoising at 1400 × 840; wide uses 32 samples. Drafts are explicitly retained as iteration evidence, not presented as the final working-scene state.

## Resume and verification

In the existing Blender Python console, import `author_badlands_realism.py` with `runpy.run_path`, then call `pass_two()`, `pass_three()`, `pass_four()`, and `pass_five()` in order. The functions have terrain markers to prevent repeated application. Save the same file and return the editor to a 3D view. Render the saved working file with `render_realism_review.py` and confirm the inspected candidate survived application.

Candidate structural inspection after pass four: 97,969 terrain vertices, 360 × 360 m authoring area, terrain height range approximately −0.70 to 4.07 m, 247 archived original objects, 585 visible mesh objects, and 2,303,249 unique source vertices. No nonfinite vertex coordinates were found. Seven surface images in used visible materials were packed. The exact user panorama remains in the world shader and is not replaced or edited. Pass five changes only camera limits, atmosphere bounds/density, and exposure.

## Remaining visual limits

- This is a stronger geological study, **not photorealistic acceptance**. Some major silhouettes still read as simple fractured wedges, and repeated strata scale remains visible.
- Broad open areas still need more locally authored transitions between embedded bedrock, gravel, and fine sediment to reach the reference's richness.
- The original LDR panorama is visibly soft and has no true scene depth. Height-faded dust removes the clipped band but does not make its distant terrain photorealistic or turn the PNG into measured HDR lighting.
- The rock surface maps were reused from the existing project workbench. Their image-derived relief is useful for this study but is not calibrated scan data.
- This is a dense source study with archived originals, not an optimized Unity runtime asset. Export, LODs, baking, and Unity integration remain separate work.

Stop after the inspected passes produce a clear improvement in these views and the saved scene passes preservation checks; do not claim photographic equivalence from a script report.

## Scope and procedural integration

This is source art in a bounded Blender study. Deterministic seeds make authoring repeatable. Runtime world identity, chunk streaming, stable generated-object IDs, authored placement constraints, and persisted deltas remain owned by Unity World Creator; no runtime integration is included here. The working file, its materials and composition, and the supplied references control this pass. Existing unrelated Unity changes are outside this task.
