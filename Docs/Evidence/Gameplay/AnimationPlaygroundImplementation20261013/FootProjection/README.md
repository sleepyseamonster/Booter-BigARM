# Transition foot projection — 2026-10-14

The existing pose owner now measures the selected foot vertices through cached bind-space influences during a snapshot transition. It raises only the visual root enough to clear the collider's feet plane; it does not rewrite bones or move the physics root. The paired 674-vertex, six-transform cache stays private. Runtime mesh readability is not required.

The sampler follows the renderer's effective skin-weight quality, including normalized selection of the strongest influences. The initial all-influence calculation differed from the active one-bone rendering policy. Matching that policy reduces the largest measured disagreement with CPU skinning to less than one micrometre. No project quality setting or source importer changed.

All 124 measured source/target transition envelopes clear the -0.91m reference floor within 0.001mm. The largest added correction is 62.1mm. Sources include all twelve standing locomotion roles and cardinal/diagonal crouch mixtures at four phase origins. This establishes the sampled graph behavior, not every possible pose or author acceptance.

Two standing stop snapshots extend the protected upper body about 3cm beyond the provisional rounded collision shape. The capsule is an adjustable collision approximation. The visible placeholder is a separate renderer, already hidden when the Humanoid initializes. Following the author's clarification, this small visual envelope mismatch does not impose a new stop-input gate.

The 98-pair immutable private checkpoint captures the new projection and paired binding. Capture and restoration dry-run pass; the original FBX files, importer metadata and existing binding GUID are preserved. Public reports contain aggregates, paths, hashes and GUIDs, not the cached vertex geometry.

All 75 focused Edit Mode checks pass without failures or skips. A fixture-state reset prevents pause state from carrying between tests. The separate equipped-pose measurement restored the validation copy's missing packed-shade mesh dependency; the author repository already had that file. The crouched backpack reaches 1.6593m, requiring a taller authored tunnel than the current 1.625m underside.

Physical profile flags remain false in this checkpoint while the separate equipped-pose check and family handoff are completed. Real obstacle clearance and safe posture/reset recovery remain enforced by the existing motor. Visual fitting and feel remain author-owned. The full playground implementation goal continues.
