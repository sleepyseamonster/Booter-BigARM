# Camp cloak and sleep prototype implementation

October 10, 2026 implementation of the [audited plan](../../Design/Gameplay/CAMP_CLOAK_AND_SLEEP_IMPLEMENTATION_PLAN.md). Batches 0 through 6 passed the automated prototype gates: asset import and render inspection, 101 focused tests, live 1x/4x simulation capture, and the production radial, menu, and camp validators. The prototype is session-only. Restarting or reloading the scene creates a fresh starter inventory and does not restore a deployed camp.

## Implemented loop

Greater Wasteland's initial inventory adds one reusable `tool.popup_camp`. New radial profiles place Camp in the east sector; existing saved layouts remain unchanged. To assign Camp on an existing profile, use the system menu's Radial Setup and select Camp for a desired sector.

Camp selection opens a nearby, sun-oriented placement preview. Booter stays stationary while the left stick or WASD moves the footprint. D-pad left/right or Z/C rotates; D-pad up or R faces the sun. South/Enter confirms and East/Escape cancels. The preview shows ground, slope, support, and space rejection reasons and uses the live binding labels.

Interact with the deployed screen using West/E. The local panel offers Sleep, Rest, and Pack up. A fresh confirmation is required after opening. Packing rechecks reach, line of sight, and inventory capacity; failure leaves the kit deployed. Setup and packing each take roughly two normal-speed seconds. No remote inventory consumption, camp retrieval, companion recall, or guarding is added.

By user direction on October 11, both Sleep and Rest can start at any Reserve level, including full, subject to existing camp and supply checks. Sleep continues at 4x simulation speed until manual wake, low supplies, or interruption; reaching 80 percent Reserve only stops recovery. Its rates are +0.4 Reserve below that cap, -0.015 food, -0.028 water, and -0.010 oxygen per simulation second. Awake Rest lasts up to 20 simulation seconds, restores +0.3 Reserve per second only below 40 percent, and uses the separate authored rest rates. Neither action lowers Reserve that is already above its recovery cap. Both keep spending supplies while recovery is capped. Ordinary activity, including idle standing, retains its original depletion rates.

East/Escape wakes. Health reduction, exhausted recovery supplies, displaced sleeping space, destroyed camp view, focus loss, device loss, system-menu handoff, component teardown, or scene change releases acceleration. Interrupted sleep preserves actual elapsed recovery and costs. Camp colliders and the player capsule stay enabled; no invulnerability, automatic healing, or generated enemy interruption is introduced.

## Canonical ownership and data

The input router owns Camp modes, action maps, cursor, prompts, neutral-return guards, and Escape/Start arbitration. The existing interaction dispatcher opens local camp actions before the gathering path. The existing motor owns short physical approach, collision, and stationary action constraints. The existing Playables animation driver owns camp poses and cloak presentation.

The existing vitals component remains the only meter writer. It reconciles activity timestamps in order and uses internal double accumulators so tiny frame intervals do not suppress or round away depletion. Public meter values and snapshot versions 1 and 2 remain unchanged. Restore/reset cancels transient recovery; general oxygen exposure and world saving remain separate features.

The camp coordinator owns one session record with a durable instance ID and a stationary world view. Inventory commit conditions transfer kit ownership before notifying observers. Notification failure is treated as an already committed transfer; it does not blindly add a second kit. A releasable speed request restores its baseline only while it still owns that speed.

Greater Wasteland terrain remains fixed and always loaded. Camp placement creates a player-authored session delta without changing terrain identity or source data. The production scene patch changes only two lines in the player inventory document; the other 38,648 YAML documents were preserved by the bounded patch. The patch receipt is recorded in the accompanying evidence JSON.

## Art and local sources

Local editable source: `SourceArt/Blender/Studies/CampAndCloak/CampAndCloak.blend`, with local Blender backups. Sources remain excluded from publication. The repeatable authoring script is `Tools/Art/Blender/author_camp_cloak.py`; Unity asset authoring is `TopDown3DCampAssetBuilder.BuildAssets` and never rebuilds Greater Wasteland.

| Export | Evaluated triangles |
| --- | --- |
| Three-panel vertical shade | 2,808 |
| Packed kit | 412 |
| Split-hem worn cloak | 1,700 |
| Draped sleeping cloak | 984 |

Prefab roots are normalized to unit scale, containing the imported FBX conversion beneath them. This keeps collision and gameplay anchors in metres. Cloth materials and renderer flags support rendering and shadow casting from both sides. The worn cloak binds runtime-owned meshes to the existing chest and hips; the blanket is a separate presentation of the same starter garment. The packed kit is visible when carried. Four authored Humanoid clips retain the canonical idle pose's body translation.

The reference scene is `Assets/_Project/Scenes/Reference/CampSleepStudy.unity`, outside enabled build scenes. Blender sources reopened with auto-execution disabled. Native Blender and Unity standing/sleeping/rear render views are retained under [CampSleep art evidence](../Art/CampSleep/).

## Verification and limits

### Sleep and rest at any Reserve level — October 11, 2026

Removed Reserve eligibility from UI availability, approach checks, and activity continuation. A quote now separates safe duration from actual Reserve gain; recovery caps no longer shorten activity or trigger automatic wake. The panel labels the action Sleep and shows ongoing supply rates. This supersedes the original sleep-until-ready behavior and the initial navigation repair's retained Reserve restrictions. Fixed terrain, camp instance identity, placement, and session persistence remain unchanged.

Fresh Unity 6000.4.0f1 compilation and **40/40 focused EditMode tests** passed in `D:\BooterBigArmValidation\CampSleep20261010-01`. Tested fixtures: `TopDown3DCampInputTests`, `TopDown3DCampIntegrationTests`, `TopDown3DSleepTests`, and `TopDown3DSurvivalTests`. Regression coverage includes entering both real camp activities from full Reserve, timestamped activity activation, continued supply consumption above the recovery caps, low-supply wake at low/capped/full Reserve, interval splitting across a cap, unchanged ordinary survival, and speed release on wake. The tested seven source files matched production by SHA-256. Local proof is `Logs/camp-any-reserve-verified.xml` and `Logs/camp-any-reserve-verified.log`; the runner exited 0. The live playtest was not stopped or focused. Restart Play Mode after compilation for hands-on acceptance.

### Camp option navigation repair — October 11, 2026

The playtest exposed missing selection when Sleep until ready was unavailable. The panel previously focused that disabled button on opening, before the fresh-confirmation guard armed the choices. Focus now resolves after arming and availability updates, chooses the first interactable camp action, preserves a valid current selection, and restores selection if it is lost or its action becomes unavailable. The initial fix preserved Reserve eligibility; the author's subsequent October 11 direction removed that eligibility, as described in the current loop above. Supply eligibility and selection recovery remain active.

Fresh Unity 6000.4.0f1 compilation and **12/12 focused EditMode tests** passed in the isolated `D:\BooterBigArmValidation\CampSleep20261010-01` candidate on October 11. The run covered `TopDown3DCampInputTests` (including disabled choices, horizontal navigation, retained/lost focus, and confirmation guarding) and `TopDown3DCampIntegrationTests`. Local diagnostics are `Logs/camp-navigation-fix.log` and `Logs/camp-navigation-fix.xml` in that candidate. The user's live Editor was not stopped or focused. Hands-on acceptance requires recompilation and a new Play session; no scene, input binding, recovery balance, world identity, or persistence contract changed.

The final focused EditMode run passed **101/101 tests**, with zero failures or skips; see [executed results](./CAMP_SLEEP_IMPLEMENTATION_2026-10-10_tests.xml). Fixtures cover kit transactions, rejected/revalidated placement, input ownership, interval splitting, tiny-frame rate preservation, health signals, speed cleanup, actual deploy/pack/wake lifecycle, repeated installation, focus loss, and cloak bone binding. Existing affected survival, radial, menu, inventory UI, harvester, locomotion, and traversal fixtures passed in the same run. `TopDown3DCampProductionValidator.ValidateAllFromCli` completed the existing radial/menu checks and new camp wiring check with process return code 0; no scenes were saved.

The uninterrupted live capture passed its declared budget:

| Hidden Editor measurement | 1x | 4x |
| --- | --- | --- |
| Simulation seconds in approximately 30 real seconds | 30.0012 | 120.0013 |
| Sun-cycle seconds | 30.0007 | 120.0003 |
| Reserve gained | 12.0005 | 48.0005 |
| Water spent | 0.8400 | 3.3600 |
| Script/player-loop p95 frame time | 0.7132 ms | 1.4633 ms |
| Maximum fixed steps observed in one measured frame | 1 | 5 |
| Absolute wall-clock versus simulation discrepancy | 0.0003 s | 0.0003 s |
| Synchronous wake request to speed release | 0.3569 ms | 0.0780 ms |

The p95 gate was 33.3 ms and both scenarios remained asleep through measurement. Read [1x metrics](./CAMP_SLEEP_IMPLEMENTATION_2026-10-10_performance-1x.json), [4x metrics](./CAMP_SLEEP_IMPLEMENTATION_2026-10-10_performance-4x.json), and [gate result](./CAMP_SLEEP_IMPLEMENTATION_2026-10-10_performance-result.json). These measurements do not establish GPU frame time, visible Player performance, actual attacks, or the player's physical reaction time.

Validation uses an isolated `6000.4.0f1` checkout at `D:\BooterBigArmValidation\CampSleep20261010-01`, with unrelated dirty rock changes excluded. The initial cold import stalled; a cache from an unowned checkout with the same pinned Editor was used, followed by fresh source compilation and imports. The user's Editor and terrain job were not stopped or focused.

Live capture uses a process-local virtual gamepad and temporary in-memory input routing for a hidden Editor. Each 1x/4x scenario reloads the same production scene, deploys the real kit, starts real sleep, warms for 10 real seconds, and measures 30 real seconds. Art readback occurs before warmup. Measurement uses wall-clock timestamps and the simulation clock, without diagnostic eval calls during capture. These are hidden-Editor simulation/performance results; GPU/player-build performance and physical controller feel are not established by them.

No hands-on gameplay smoke test, external publication, package change, broad project-setting change, saved-world integration, storm mechanic, guarding, rested buff, or camp crafting economy is claimed. The sleeping visual uses an upright collision capsule in this first slice. Final art acceptance, cloak clipping through the full movement set, repeated-use balance, and future combat reaction fairness remain user-owned acceptance or later feature proof.
