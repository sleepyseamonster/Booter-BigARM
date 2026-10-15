# Camp Loop five resource sources implementation

October 14, 2026. Saved-project implementation complete; live unsaved Editor synchronization is pending the author's external-scene-change dialog. See [creative plan](../../../Design/Gameplay/CAMP_LOOP_FIVE_RESOURCE_SOURCES_CREATIVE_PLAN.md) and [incorporated audit](../../../Design/Gameplay/CAMP_LOOP_FIVE_RESOURCE_SOURCES_PLAN_AUDIT.md).

## Delivered content

One linked `Camp Loop Resource Study` prefab is installed additively in `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`. Its hierarchy separates Study Services, Layout Anchors and Resource Sites, with five numbered sites. Each site separates visuals, targeting and solid collision. Existing actors, original resource sources, terrain and camp fixtures were not moved or replaced by this implementation.

| Site | Reward per confirmed harvest | Surveyed root X, Y, Z |
| --- | --- | --- |
| RS01 Ironstone Outcrop | 2 Ironstone Ore | -1809.43, -46.58, -1508.56 |
| RS02 Ancient Salt Crust | 2 Dry Mineral Salts | -1805.72, -45.66, -1500.12 |
| RS03 Bristle Hollow | 3 Coarse Sensory Bristles | -1794.15, -44.44, -1493.24 |
| RS04 Dead Winch | 1 Salvaged Metal Cable | -1789.45, -43.58, -1485.00 |
| RS05 Husk Beetle Site | 3 Scraped Chitin Fibers from its carcass | -1788.83, -46.92, -1522.85 |

Positions came from native terrain and obstacle/route queries, not guessed elevations. The static sources are fitted to the slope; salt uses sampled ground-conforming meshes. The beetle has a separately grounded home anchor above its site pivot. An 8 m radius clear footprint bounds the proposed beetle encounter; static sources reserve 3.8 m survey footprints and the linked approach corridor uses 1.5 m clearance on either side of its sampled centerline. The search excludes existing solid obstacles, the camp return route and existing NPC proximity. [survey.json](survey.json) retains full coordinates.

The four static sources deplete once per confirmed inventory trial. The beetle wanders, approaches a nearby visible Booter within its home territory, telegraphs a close bite, recovers, takes damage and becomes a finite harvestable carcass. Blocked support/collision stops movement; it never teleports to recover. Six simple moving legs and overlapping husk plates are replaceable blockout art.

Player melee uses the existing lab Attack command (default mouse left button / gamepad right trigger), the existing unrestricted imported jab clip and an owned motor/presentation action. Three successful 10-damage strikes kill the 30-health beetle. The 0.85-second player action has one bounded hit opportunity; eligibility checks range, facing, height and obstruction without multiplying child-collider hits. The beetle bite deals 6 damage after a 0.65-second telegraph and has a 1.1-second recovery. These are prototype values. No damage originates from animation preview or death notifications.

Use existing device-aware Interact prompts to harvest. All raw rewards require physically accessible Legger cargo with available mass/stack capacity. Missing or full cargo leaves sources unconsumed. The generic study sources deliberately use manual companion regrouping rather than Ironstone's special automatic call-over path. The original twelve Ironstone nodes and original three-point ghost-machine wreck remain independent.

## State and reset

- Five immutable source IDs, stock bits, beetle health, revision and target epoch belong to a versioned serialized study ledger. Domain reload and view disable/re-enable preserve consumed/dead state. Invalid retained state fails closed and preserves inventory.
- Animation reset cancels study attack/gather captures through existing action owners and invalidates epochs. It does not heal the beetle, replenish stock or remove rewards.
- The existing confirmed inventory-trial reset prepares one study world mutation alongside the baseline inventory/world drafts. It restores study stock, beetle health and its supported home together with inventory. Its confirmation explicitly names the beetle restoration while preserving Booter, Legger and their vitals. Failed preflight, changed revision or blocked home cancels that compound transaction.
- The winch uses the study ledger, not a second `TopDown3DSalvageWreckState`. No competing reset menu, source-only item subtraction, disk checkpoint, production save migration or procedural spawning was added.

Four new Resource item definitions, icons and prototype stack/mass metadata extend the existing private salvage catalog without changing its GUID or original definitions. The existing Ironstone definition remains unchanged. The spreadsheet was not edited by this task; its concurrent author changes remain outside this implementation.

## Verification

The isolated Windows Unity 6000.4.0f1 candidate is `D:/BooterBigArmValidation/CampResources20261014`. Both the original saved scene and the latest saved author scene with concurrent hierarchy changes were inspected there. The latest saved scene SHA-256 at integration was `d47cd677d03b8fe9160e6be291467965e7ab9981b3551aac5c394a8548896a73`.

All **87 focused EditMode cases passed**, with no skipped or inconclusive cases:

| Suite | Passing cases |
| --- | --- |
| CampResourceStudyTests | 28 |
| CampLoopLoadoutTests | 16 |
| CampEconomySessionTests | 7 |
| TopDown3DInteractionTests | 17 |
| CampLoopSalvageTests | 9 |
| TopDown3DInventoryLoopTests | 10 |

[tests.json](tests.json) records names and results. New coverage includes one-time rewards, live-versus-carcass availability, cancellation/epoch invalidation, serialized retained state, range/facing/height, hit-window cancellation, unobstructed authored work faces, atomic reset rejection/success, actual study binding to the existing economy and the confirmed beetle reset.

Initial candidate failures exposed omitted dynamically loaded Resources dependencies; these were hydrated without changing author assets or weakening tests. The default-binding contract was rerun with default input overrides in the candidate so earlier UI-test scheme overrides did not contaminate it. The final six-suite run passed against the latest saved scene.

The scene validator passes with five unique study sources, one beetle, twelve original Ironstone nodes, one original wreck and the existing Camp Loop baseline. Offscreen captures of all five blockouts were rendered without entering Play Mode and visually inspected: [outcrop](Captures/Resource0.png), [salt](Captures/Resource1.png), [bristles](Captures/Resource2.png), [winch](Captures/Resource3.png), [beetle](Captures/Resource4.png). These establish blockout visibility/contact from proof views, not hands-on gameplay or final art acceptance.

[asset-manifest.json](asset-manifest.json) identifies 66 task-owned Unity source/asset files. Thirty-two task asset GUIDs are unique across project metadata. Candidate imports changed no protected baseline Assets outside the intended manifest and scene. Scene integration added one linked prefab record and one root-list entry while preserving 1,389 existing serialized records byte-for-byte; the mixed worktree scene was not staged wholesale. Production scene, terrain data, package pins and unrelated changes remain outside the task.

## Live Editor handoff

The author's open CampLoopTesting scene was dirty before this task. During concurrent scene work, Unity displayed **The open scene(s) have been modified externally**, blocking the CLI's main-thread dispatcher. The task neither dismissed the dialog by reloading/discarding edits nor focused the app. Source/assets and the strictly additive saved-scene record are installed, but insertion into that already-open unsaved scene is not yet confirmed.

After retaining current unsaved work through that dialog, `BooterBigArm.Editor.CampResourceStudyAuthoring.InstallInOpenAuthorScene()` can add the already verified prefab with Undo and no scene save. The equivalent menu is **Booter & BigARM → Camp Loop → Install Five Resource Sources**. An existing study is validated rather than duplicated. A failed validation removes only the newly created study object. No whole-scene save or reload is required for live insertion.

No gameplay smoke test, Play Mode session, player build, package change, GitHub push or foreground activation was performed. Hands-on attack feel, route traversal and final visual acceptance remain with the author.
