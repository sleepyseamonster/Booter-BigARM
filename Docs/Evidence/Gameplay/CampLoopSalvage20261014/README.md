# Camp Loop ghost machine salvage implementation

Implemented October 14, 2026 in the existing [CampLoopTesting scene](../../../../Assets/_Project/Scenes/Reference/CampLoopTesting.unity), following the [audited plan](../../../Design/Gameplay/CAMP_LOOP_GHOST_MACHINE_SALVAGE_PLAN_2026-10-14.md).

## Playable study

One linked machine blockout is placed 28 metres east of Starting Camp at approximately (-1814, -46.27, -1498). Its exposed side contains three independently scavengable sockets: two Metal Plates in 2.5 seconds, one Wire Bundle in 2 seconds, and one Drive Assembly in 3.5 seconds. Together they weigh 10.5 kg under trial tuning.

Use the existing Interact command while standing beside a socket. Bring the Legger into physical cargo access first using the existing call/regroup controls. Prompts include item and quantity; completion receipts name the cargo destination. The loose part geometry disappears after a committed extraction while the structural obstacle remains. Current in-place gather animation is reused and stretched to each duration; the machine and icons are functional blockout art, not final assets. No dedicated extraction motion or new sound set is claimed.

Use the existing animation playground Reset control for another trial. The reset cancels work, invalidates stale target epochs, removes all three lab item types from kit/cargo/cache regardless of distance, and restores the wreck as one coordinated state change. Other inventory contents and Ironstone depletion are preserved. The Legger is not teleported.

## Isolation and continuity

The private item catalog includes existing production definitions plus three `lab.salvage` items. Scene-local serialized catalog assignments preserve Booter's 16 slots, Legger packing settings and the cache's original prefab/identity. Runtime catalog reconfiguration is never used. The production catalog and resource prefabs remain unchanged.

The wreck's stable ID, version, removed-part bitmask and epoch remain serialized across domain reload alongside the inventory owners' retained snapshots. Initialization checks catalog consistency and aggregate trial rewards against removed bits. A mismatch disables extraction and preserves storage. Views reconcile from the retained owner; hiding/recreating a view cannot replenish salvage.

The marked playground's disk-checkpoint suppression remains active. This study neither reads/writes production checkpoints nor adds machine deltas to the Ironstone-specific save registry. Terrain is fixed and always loaded; no generation, streaming, native sculpting, package change or production population is introduced.

## Current machine proof

Unity 6000.4.0f1 completed hidden background import/compilation, camp-loop structural validation, salvage validation and a repeated authoring invocation. The repeated invocation validated the existing study without rebuilding or resaving it. No foreground window or gameplay smoke test was used.

In the isolated candidate at `D:/BooterBigArmValidation/CampSalvage20261014-02`, **44 focused EditMode tests passed, zero failures/skips**: `CampLoopSalvageTests`, `TopDown3DInteractionTests`, `TopDown3DInventoryLoopTests` and `TopDown3DInventoryTests`. The [test results](tests-verified.xml) include finite extraction in arbitrary order, cargo-only restrictions, cargo departure, mass rejection, blocked reach/cancel, stale epoch, paired reset after split/cache transfer, missing/duplicate storage rejection, serialized reconstruction and the actual scene sockets/catalog/checkpoint policy. The scene fixture directly qualifies dependencies and interaction contracts in Edit Mode; it does not enter Play Mode or prove subjective animation feel.

An initial isolated scene check lacked a dynamically loaded camp-settings asset; hydration was corrected. A second check exposed Unity's EditMode restriction on invoking `Update` through `SendMessage`; the fixture now calls an explicit binding method. No assertion was weakened. The final 44-test run passed after these corrections.

The [offscreen capture](salvage-blockout.png) was rendered in the isolated candidate with the existing URP settings and low-angle scene lighting, without entering Play Mode. An initial capture omitted the render-profile dependencies and was discarded as visual evidence; the corrected candidate includes the source rendering settings. This image shows an inspection camera, not the normal gameplay camera or a Player performance measurement.

Protected production scene, shared resource prefabs, stock item catalog, package manifests and build routing matched their pre-task SHA-256 hashes. Native TerrainData references were retained; the nine materialized data payloads match their committed LFS object identities. See [verification](verification.json) for hashes and the exact feature file manifest.

Hands-on extraction feel, controller comfort, final appearance and Player performance remain author-owned. Production persistence and wider wreck distribution remain later slices. Unrelated animation/station/cargo work and fused rocks were excluded from this feature commit.

Ignored diagnostics remain under the checkout's `Logs/camp-salvage-*.log` and the isolated candidate's `Logs/`. New authoring/validation commands are documented in [Unity automation](../../../Engineering/UNITY_AUTOMATION.md).
