# Greater Wasteland reconciliation — October 9, 2026

Owner: Gottspan, with Babineaux Unity validation and Gear Ball publication responsibilities. Status: verified saved-scene baseline; publication is recorded in Git history.

The author requested complete worktree reconciliation, terrain repair, hierarchy organization, safe commit batches and a push to main. The author explicitly allowed removal of manual sculpt edits. Three TerrainData files were restored byte-for-byte to the immediately preceding sealed assets, without replacing their GUIDs or metadata. Ignored backups retain the prior edited payloads for recovery.

Greater Wasteland now has 18 geographic sections, 288 organizational sectors and 4,608 active native terrain tiles. Sector grouping retains original terrain objects, assets, transforms, collider ownership, ground markers and the single connectivity owner. Booter is first in the hierarchy and its existing MeshRenderer is enabled in the saved scene. No duplicate player or runtime spawn owner was added.

Editor tools provide a player-following 3×3 sector view, a transient lightweight landscape overview and native-collider player placement with Undo. Scene visibility does not unload terrain or disable runtime collision. Fixed geographic identities remain authoritative; no procedural generation, chunk streaming, unload/reload service or persisted runtime delta format was introduced.

## Current-machine proof

- Unity 6000.4.0f1 imported and compiled the Editor and test candidates. Nine GreaterWasteland EditMode tests passed.
- Sixteen Start-menu tests and twelve radial-menu tests passed. No Play Mode integration or gameplay smoke test was run.
- The full terrain validator passed all 9,072 exact shared borders and 115,200 collision samples. Maximum collider deviation was 0.00006103515625 m. Newest-section source/readback deviation was 0.027492642402648927 m, within native encoding tolerance; all retained grids matched exactly.
- BadwaterPlayableSceneBuilder.ValidateFromCli and ConversionBaselineValidator.ValidateFromCli passed on the saved production scene. All 5,040 GameObjects have their scripts. Each section contains 16 sectors and 256 tiles.
- No duplicate GUIDs among 53,645 project asset GUIDs. All 4,665 scene GUID references resolve: fourteen are installed package references and the others are project/built-in assets.
- The [baseline JSON](GREATER_WASTELAND_BASELINE_2026-10-09.json) records the saved scene hash, current source manifest, restored payloads, focused-test summaries and all 4,608 TerrainData/metadata hashes. The full physics capture preceded final scene saving; the saved-scene validator then independently rechecked the complete terrain readback and borders.
- The local Windows preflight passed branch, package, LFS, asset pairing and Python checks. Its original whitespace failure was resolved; final active-content whitespace and relative-link checks passed. Immutable source captures use CRLF-aware checks and preserve receipt hashes.

The pinned Editor serialized QualitySettings at schema 5, adding its meshLodThreshold defaults and Nintendo Switch 2 platform mapping. Existing quality choices and package pins were retained. This serialization is committed separately from scene/tooling changes. Inspected insignificant material serialization drift was removed.

## Background visual review

[Player-area capture](greater-wasteland-player-baseline-2026-10-09.png) and [whole-terrain capture](greater-wasteland-whole-terrain-2026-10-09.png) were rendered offscreen without foreground activation or Play Mode. The whole saved terrain footprint renders continuously, and the player-area capture shows Booter and Legger on the authored surface. The Pipeline screenshot helper's Game camera initially returned only its background because it omitted the Editor scene culling mask. The diagnostic capture temporarily supplied that mask, then restored the camera's mask, target, pose and clipping; the scene stayed clean. No project camera change was needed.

These captures are limited Editor visual evidence. They do not establish physical-controller response, gameplay feel, complete material/lighting acceptance, runtime HUD installation, traversal, Player performance or a new executable build. Those remain user-owned acceptance.

## Worktree classification and batches

The [classification receipt](../Repository/WORKTREE_CLASSIFICATION_2026-10-09.json) accounts for every original pending path. 31,488 raw captures match their compressed arrays exactly and remain on disk under narrow ignore rules. Useful acquisition data, historical failed preparations, compressed earlier baselines and render evidence were preserved in Git with their historical authority explicitly labeled. No source file was deleted.

Publication batches: source/evidence preservation and duplicate policy; scene hierarchy/editor tools/tests; pinned Unity quality serialization; current baseline and workflow routing. The push also includes the 44 pre-existing local main commits inspected during the publication audit. No package upgrade, branch change, history rewrite, pull request, release or archive reactivation was performed.
