# Retained terrain source attempts — October 9, 2026

Owner: Gottspan. Status: historical source evidence, except the accepted NorthRidge acquisition.

The worktree reconciliation preserves previously untracked source attempts in their established locations. Their presence does not change the production source contract. Current production authority remains the sealed Unity Source manifests, ending with NorthRowE5204002026-10-09, and the saved Greater Wasteland scene.

- NorthRidge2026-10-09/Acquisition01/DEM contains the accepted bounded acquisition, metadata and DEM grids. File formats and payload hashes were inspected.
- WestNorthNorthwest2026-10-07/Prepared01 and Prepared02 are intermediate preparations. Prepared03 is the accepted expansion preparation.
- WestPair2026-10-08/Baseline01 and Baseline02 capture earlier sculpt/serialization states. Baseline03 is the accepted integration capture. Do not restore the earlier attempts as production terrain.
- WestPair2026-10-08/Prepared01 is the superseded candidate. Prepared02 is the accepted preparation. The historical retained_terrain_drift_audit.json records its failed gate; later integration evidence controls completion.
- Historical Blender proofs retain their original source hashes. Completed native Blender files were retired under the author's separate cleanup instruction; these records do not claim fresh native reopen proof.

The [source inventory](../Evidence/Repository/SOURCE_EVIDENCE_INVENTORY_2026-10-09.json) records all 59 added source/evidence files, their hashes and format checks. The [worktree classification](../Evidence/Repository/WORKTREE_CLASSIFICATION_2026-10-09.json) records 31,488 raw per-tile captures whose arrays match their compressed render_heights.npz archives exactly. These raw duplicates stay on disk under narrow ignore rules; no source file was deleted. The two earlier compressed baselines are included in publication as historical evidence.

The inventory proves payload and format integrity, not acceptance of a failed candidate, new terrain integration, gameplay traversal, or Player performance.

Source manifests, CSV captures and receipts retain their original line endings and bytes. Whitespace checks use Git's cr-at-eol option for these preserved Windows captures; immutable production_before.unity snapshots use -diff so their Unity serializer whitespace is preserved. Active scene/code/document changes still pass the ordinary whitespace check.
