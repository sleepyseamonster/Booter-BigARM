# Material diorama plan audit

Sable, October 15, 2026. Reviewed [PLAN.md](PLAN.md) as committed in `1042060a1`, against the author's requested material-rich dioramas, accepted art direction, earlier comparison evidence, current production camera serialization, and repository preservation rules.

**Verdict: useful art brief, but revise it before authoring the full comparison batch.** One scope mismatch and five execution gaps could make the results less decisive than their size suggests. This audit records proposed corrections; it does not revise the plan, approve a new scope, or start scene construction.

## Findings

### 1. High priority — Surface treatments do not cover the promised art-style comparison

**Location:** [four treatments](PLAN.md#four-treatments), especially the common-geometry rule and deferred shape comparison.

The four candidates vary painted color, texture frequency, and fine relief while holding geometry fixed. That is a useful material comparison, but it cannot establish preference for the silhouette, proportions, sculpted planes, or rendering vocabulary of Arcane, No Rest for the Wicked, and Risk of Rain 2. The plan acknowledges the restriction, yet presents twelve versions as the answer to the broader art-style request. A negative result for D, for example, would concern graphic surfaces on the common forms rather than the full reference style.

**Proposed correction:** explicitly divide the exercise into a common-geometry material comparison and a bounded whole-style study. Complete the material comparison first. Use the salvage workyard as the one whole-style pilot, with documented changes to major planes/silhouettes and, where justified, shading treatment. Keep composition, physical scale, camera, and light inputs comparable. Show these images separately; do not treat their differences as texture-only evidence. Resolve this addition and its capture count in the revised plan instead of silently adding it during execution.

**Closure evidence:** each candidate has a surface brief and a form/rendering brief, or the deliverable is explicitly narrowed to material treatments. The review states what the images can and cannot decide.

### 2. Medium priority — The accepted B ground has no preserved visual anchor

**Location:** [material inventory](PLAN.md#material-inventory), [four treatments](PLAN.md#four-treatments), and the B naming note under deliverables.

The old B atlas is one compacted-ground treatment. The new B candidate extends it to loose fines, gravel, many additional materials, repaired tiling, and different lighting. The plan correctly warns that new B is not the old B atlas everywhere, but it does not require the old accepted ground to remain available as a comparison control. The first study also records that the atlas is a generated draft, not a finished independent material set. It would be easy to make an attractive replacement and mistakenly call it the already accepted ground.

**Proposed correction:** preserve the original atlas, its crop/remapping, settings, and hashes. Include a reference inset or neutral control patch of the original B ground beside the new compacted-ground derivative under the same diagnostic light. Label loose fines and gravel as proposed extensions. Use descriptive candidate names in the viewer, with a legend distinguishing the old A/B/C study from this study's A/B/C/D.

**Closure evidence:** a source/derivative mapping and a matched old-B/new-ground comparison show what changed. The user's accepted appearance is distinguishable from newly proposed extensions.

### 3. Medium priority — The close-up and plate requirements do not guarantee usable evidence for every surface

**Location:** [cameras and capture matrix](PLAN.md#cameras-and-capture-matrix), especially the single material close view and 15-sample plate.

One close view must contain several metal finishes and three main stone families, while the inventory contains fifteen conditions. Putting a sample in the scene or a five-by-three plate does not ensure its pores, grain, brush marks, or roughness are large enough to judge. M01's machined rim and M02's sleeve also present different shapes; their highlights cannot be compared confidently without matching diagnostic geometry and orientation.

**Proposed correction:** create a coverage table for all fifteen IDs with a named scene object, diagnostic coupon, view/crop, and intended decision. Use the same flat, curved, and bevelled coupon geometry, scale, orientation, and light for material-to-material comparison. Inspect the plate at native pixels. Split it into metal, stone/ore, and ground/control detail boards if it cannot show the relevant structures; do not rely on enlarging an undersized crop. Any additional rendered views must update the manifest and capture count.

**Closure evidence:** every material has an adequate native-resolution inspection view. Close metal distinctions can be judged on matching geometry, while natural object views still show contextual appearance.

### 4. Medium priority — Framing adjustments can invalidate the gameplay-distance claim

**Location:** [camera definition](PLAN.md#cameras-and-capture-matrix).

The plan allows changing the reference gameplay framing to fit each diorama. This preserves fairness between styles, but changing distance or field of view changes how much texture and clutter remains visible. An attractive overview could therefore be mistaken for evidence that the same detail reads from the actual game camera. Current scene serialization still contains distance 22 and field of view 48; those fields alone do not establish the complete active runtime view.

**Proposed correction:** keep a locked gameplay-reference view using the verified active camera mode, distance, pitch, field of view, target height, and output aspect. Compose the diorama for that view. Use close and alternate views for artistic framing; label any additional adjusted overview as an overview rather than gameplay proof. An offline camera match remains a proxy for Unity appearance.

**Closure evidence:** the receipt identifies camera provenance, complete framing parameters, and all intentional deviations. The actual gameplay reference remains present even if an overview is needed.

### 5. Medium priority — Lighting is frozen before checking half the candidates

**Location:** [lighting setup](PLAN.md#lighting-that-reveals-the-surfaces), specifically tuning sunset and twilight against A and B before freezing them for all four.

C and D intentionally change local value grouping and fine relief. A rig judged only on A and B could clip painted highlights or hide broad accents in the other two, making a lighting problem look like a style preference. Neutral plates help, but do not show how the complete scene's painted value groups behave together. This matters because the earlier study already found twilight too dark to resolve surface differences reliably.

**Proposed correction:** qualify the shared rig on all four candidates before freezing it. Use the same diagnostic chips and representative light/dark material patches, inspect clipping and shadow separation, and keep exposure fixed across candidates. Add a neutral-light full-scene pilot comparison if plates cannot explain a discrepancy; record that as a diagnostic addition rather than silently changing the final matrix. Do not retune each candidate's beauty lighting independently.

**Closure evidence:** a four-candidate lighting qualification sheet demonstrates that the preset reveals the intended differences. The pilot review records any intentionally lost detail rather than treating darkness itself as success.

### 6. Medium priority — Reopening the source does not establish a self-contained reproducible study

**Location:** [asset authoring](PLAN.md#asset-and-texture-authoring) and [receipt/reopen requirements](PLAN.md#deliverables-and-ownership).

The receipt lists hashes and calls for reopening the saved native source, but does not require retaining every external texture/mask or proving that the source no longer depends on a temporary generation/cache path. A file can reopen with the expected object inventory while textures are missing or still resolving through machine-specific locations. This risks losing the selected appearance between review and handoff.

**Proposed correction:** define a task-local dependency manifest covering all images, masks, linked geometry, material graphs, fonts if rendered, render/packaging scripts, and color-management settings. Retain licensed source dependencies locally, use relative links or packed resources as appropriate, and verify missing resources as well as object inventory after reopening. Render at least one comparison frame from the saved source in a clean scratch copy and compare it with the recorded original. Keep raw textures and native sources local; provenance does not authorize redistribution.

**Closure evidence:** dependency resolution passes, the reopened source renders correctly without temporary source paths, and the receipt distinguishes exact reproducibility from tolerances caused by device or denoising differences.

## What already holds up

- Three scene contexts are concrete, distinct, and relevant to the requested materials. Transported samples explain mixed rock families without inventing a geological outcrop.
- The fifteen-condition inventory covers the request, including bare worked iron and an ore specimen to resolve “raw iron.” Six metal finishes, four stone types, three ground types, ore, and ceramic total fifteen.
- The stated capture arithmetic is correct: `3 × 4 × 2 × 3 + 4 = 76`. This proves the current matrix's count, not its adequacy after revision.
- Reusing geometry, placement, wear locations, and dust coverage makes the material stage meaningfully comparable. The plan correctly identifies it as a multi-variable material comparison.
- The first dressed workyard is already an internal quality gate before multiplying final scenes. Benchmarking before an ETA and using previews before final renders are appropriate.
- Native Blender files remain local-only. The raw-output destination is correctly identified as not currently ignored, and a narrow exclusion is required before capture.
- Production terrain, the dirty camp/character work, Unity settings, packages, foreground-window state, and world canon are protected. No new runtime identity, streaming, or save integration is needed for an offline study.

## Revision order

1. Resolve the material-versus-whole-style scope and give candidates descriptive names.
2. Add the accepted-ground control, material coverage/coupon table, and locked gameplay-reference definition.
3. Qualify lighting across all four candidates and seal dependencies for the first complete scene.
4. Recalculate the final image manifest only after the pilot establishes which extra detail or style views are necessary. Keep the complete proposed scope visible; avoid unbounded additional variants.

The next useful action is to revise the plan using these findings. This audit does not require another research sweep or a new production system.

## Verification and limits

Read the plan, prior comparison results, source/render receipt, verification record, and rendering/packaging code excerpts. Checked the current production scene's serialized camera distance and field of view. No Unity or Blender process was launched; no new imagery, performance, or visual-quality proof is claimed. Local documentation links were checked. No production files or the audited plan were changed.
