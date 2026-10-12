# Minimal inventory loop implementation

Owner: Gottspan. Date: October 11, 2026. The minimal loop is implemented under the author's approved quick gamepad transfers and optional packing direction. Hands-on feel acceptance remains with the author.

## Verified core batch

Booter kit and Legger cargo remain independent authorities using one item catalog. Stock ore and dust are authored cargo-only; resource definitions and storage policies enforce that rule for additions, transfers, rewards and restores. Mining requires physically accessible active cargo, requests bounded physical approach when nearby, and preserves the deposit on failure. Dust-filled canister recovery prepares a joint transaction returning the tool to Booter and dust to cargo; unavailable capacity/access preserves the deployed device.

Joint inventory transactions prepare both states, preserve the exact selected source stack, install all quantities before inventory notifications, and reject reentrant mutations. Partial quick transfers and bulk unloading retain every remainder. Committed results remain successful even if a presentation observer throws, with the exception logged.

The screen labels BOOTER KIT and LEGGER CARGO. Left stick/D-pad navigation has explicit grid/footer links; LB/RB switch owners, X quick-transfers, A selects/moves a stack, B cancels selection before closing, and Y closes. The distant manifest stays inspectable; movement requires active, same-scene, range and visibility access. Held sticks must return to neutral before controlling gameplay after closing inventory. Mass shows the enforced maximum, occupied slots and actual load band. The unsupported balance score is hidden; optional Auto-Pack only organizes existing stacks.

A finite 24-slot resource-only cache prefab is installed at runtime beside the starting area. Its authored location was sampled on native terrain with slope and overlap checks; authoring left the production scene clean. Opening the cache replaces the left pane with cache storage. Quick transfer works both ways, and UNLOAD RESOURCES moves the greatest fitting quantities atomically. It does not destroy goods, create infinite storage or change resource ownership rules.

Private inventory models are excluded from Unity's automatic field serialization. Each carrier retains a typed snapshot for reconstruction; incompatible retained data is preserved and mutations are blocked for recovery. Default empty serialized snapshot fields are treated as absent. The canvas reconnects to authored grid children rather than creating duplicate slots when its transient view lists are reconstructed.

## Verification

The isolated candidate was created at `C:/BooterBigArmValidation/Inventory20261011-01` and moved intact to `D:/BooterBigArmValidation/Inventory20261011-01` after concurrent disk use exhausted C:. The interrupted disk-full run is not passing evidence. The incomplete compiled assemblies were retained under the candidate's Logs directory before Unity rebuilt them.

Current verified core result: **71/71 focused EditMode tests passed**, `Logs/inventory-ui-final.xml`, with source copied into the isolated candidate. This includes resource ownership, mining failure/cancellation, 73/74-ore capacity, selected-stack transfers, coherent notifications, partial unloading, controller cancel/focus, distant inspection, cache context, and existing inventory/packing/dust/input contracts. The source Editor independently validated the cache asset, reported no compilation failure, remained outside Play Mode, and reported a clean production scene. No gameplay smoke test or foreground activation was performed.

## Authored-world checkpoint

The new production-only `GreaterWastelandInventorySaveService` uses a separate version-1 file under `Application.persistentDataPath/GreaterWasteland/inventory-v1.json`. It captures Booter kit, Legger cargo, cache contents/identity, resource depletion, deployed canisters with their collection clock, survival/algae, deployed popup camp, and both characters' true positions/rotations. The Legger restores to his own saved location, even when far from Booter. He is not relocated to make cargo accessible.

Checkpoint writes happen in LateUpdate after a completed inventory change, every thirty seconds during ordinary supported movement, and on an eligible application quit. No write occurs while an inventory is committing, a gather is running, camp activity is active, or either actor lacks native terrain support. Closing during an action keeps the preceding completed checkpoint. This is one local automatic profile, not a multi-slot save UI.

The file has a size limit and SHA-256 envelope. Restore validates the whole header, carrier policies, exact capacities, mass, known resource IDs/versions, canister identity/position, native support, camp placement and survival capacities before any inventory installation. Joint inventory observers see all restored quantities and world state. Writes flush a new `.pending` file before atomic replacement; `.backup` retains the preceding file. Corrupt/incompatible files, orphan pending files and missing-primary backup situations preserve recovery data and disable automatic saving. No migration truncates a backpack, refills a canister, or remotely transfers resources. Recover by preserving the files first and deliberately selecting a compatible checkpoint; the first slice does not offer an in-game recovery picker.

Resource/device deltas and carrier snapshots also retain state through Unity reconstruction. Camp records, established-site packability, and companion position restore have explicit seams. This does not claim that every unrelated gameplay controller supports script reload during an active animation.

## Bounded hauling study

`Assets/_Project/Scenes/Reference/InventoryHaulingStudy.unity` is a disabled, separate reference scene with authored flat ground, twelve one-ore deposits, eight kit slots, and a two-slot resource cache. It retains the existing controller/camera/UI lane but removes generated terrain and generated-world save authorities. It does not write the production checkpoint.

Its independent packing asset uses comfortable/recommended/hard mass **7.5 / 10 / 11.5**. With ore mass 1.25, six ore remain comfortable, eight reach the recommended boundary, nine are heavy, and the tenth is rejected. Production retains **60 / 80 / 92**, twelve slots and twelve one-ore deposits. Tools carried on the Legger reduce available mass as usual. No breakdown, damage, cargo loss, spatial packing or unenforced per-mount limits are presented as functioning challenges.

Suggested author-owned trial: mine six ore, inspect the load, choose whether to take three more, attempt the tenth, return to the cache, unload, and try again. Also transfer a spare tool, move a stack, cancel a selection, and inspect cargo while separated. Judge whether the extra haul is worth the slower movement and whether each menu action is understandable; the study supplies conditions for that judgment, not proof of fun.

## World contract

Greater Wasteland retains deterministic authored identity `greater-wasteland.authored.v1`. Existing resource stable IDs and a stable cache ID identify runtime deltas. Terrain is fixed and always loaded; chunk streaming/unload is not applicable. UI/view reconstruction reconnects to carrier state and cannot move the Legger. No terrain generator or old generated-world save service is installed.

The core batch is committed as `240127522`. Final combined regression proof is **90/90 passing EditMode checks**, `Logs/inventory-release.xml`, followed by **12/12 checkpoint/study checks** after the simulated-position and bounded terrain-query refinement, `Logs/inventory-checkpoint-final.xml`, and **12/12 UI/study checks** after reconnecting authored grids, `Logs/inventory-ui-reconstruction.xml`. The camp observer test proves that an already committed ownership change returns success, logs a failed presentation observer, continues notifying later observers, and blocks reentrant mutation. Gameplay B has one context action: pickup while idle, cancel while gathering; it does not issue competing pickup and cancel actions.

The source Editor cache validation and isolated study authoring/structural validation are distinct from the focused runtime-model proof. No production scene, enabled-build routing, package version, native terrain or Blender source was changed by this inventory task. No Player build, gameplay smoke test, performance replay or subjective controller acceptance is claimed. Subjective controller comfort, challenge and hauling decisions require the author's hands-on acceptance.

Concurrent camp/waypoint, material, rock and terrain work is separately owned. Only inventory task paths are included in its commits; no push is authorized.
