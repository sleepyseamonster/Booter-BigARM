# Camp Loop animation playground implementation plan

Date: 2026-10-13. Status: planning complete; implementation pending. Target: `Assets/_Project/Scenes/Reference/CampLoopTesting.unity` in the root Unity project. The author requested three plans: bring the downloads into the project correctly, integrate the character and animation behavior into the player, and furnish the scene with interactions for hands-on playtesting.

The finished playground will let the author walk up to an object, use the normal player controls, perform an appropriate animation with real collision and contact behavior, change variants, and reset the experiment. A clip viewer supports diagnosis; it does not count as implementing the physical actions. The production Greater Wasteland scene remains a separate promotion decision.

## The three plans

| Plan | Concrete deliverable |
| --- | --- |
| [Asset import and storage](./CAMP_LOOP_ANIMATION_ASSET_IMPORT_PLAN_2026-10-13.md) | A complete source-to-destination map for 72 unique FBXs, preserved hashes and GUIDs, a validated character Avatar, individual clip import policies, and reproducible private asset restoration |
| [Player and animation integration](./CAMP_LOOP_ANIMATION_PLAYER_INTEGRATION_PLAN_2026-10-13.md) | The existing Booter actor with the new visual, one animation graph, catalog-driven actions, physical movement modes, props, damage/recovery, and controller/keyboard input |
| [Scene and interaction stations](./CAMP_LOOP_ANIMATION_PLAYGROUND_SCENE_PLAN_2026-10-13.md) | Twelve authored station areas with cover walls, ladders, obstacles, movable/throwable items, mining, training targets, seats and rest props, plus reset and visible feedback |

The [source audit](./CAMP_LOOP_ANIMATION_PLAYGROUND_AUDIT_2026-10-13.md) and its [inventory](../../Evidence/Gameplay/AnimationPlayground20261013/animation-inventory.json) control the downloaded collection. The [planned import manifest](./AnimationPlayground20261013/asset-import-plan.json) accounts for every source copy and classifies readiness without claiming a completed import.

## Current boundary and decisions

Planning started at `f4b73f1b8dac286a6955edae3a393416171d425d`, main tracking origin/main. The unrelated Fused rock files remain untracked and outside this work. The existing Unity 6000.4.0f1 editor owns the checkout; Camp Loop Testing was confirmed clean and outside Play Mode. Preserve the user's foreground application and use its existing background CLI connection. An implementation preflight must refresh this state.

The collection contains 73 folder copies, 72 unique FBXs, one character and 71 motions. Sixty-eight motions are unarmed evaluation candidates; the mining adaptation source, forward-travel sprint and weapon-pose crouched death are restricted references. Importing all sources does not enable all actions. Mounting, standing wall sidestep, crowd-specific motion and several exit/recovery transitions still require suitable clips or deliberate adaptation.

Two live findings refine the audit's early proposals. Keyboard C is already canister pickup and camp rotation, so the player plan uses V for crouch. The locomotion phase mapper requires two contacts per foot; single-cycle sources must be prepared and measured to satisfy that contract. Neither issue is solved by assigning an FBX to a field.

The GitHub API reported the current repository as public on October 13. The asset plan therefore imports raw Mixamo files into the **local repository working tree**, preserves them in private recovery storage, and tracks their metadata and restoration manifest rather than exposing their payloads through public Git history. It does not change account visibility or purchase storage. This is a storage choice, not a reason to defer local Unity integration. See the asset plan for the governing sources and exact guard.

## Ownership and architecture

Gottspan owns sequencing, scope and integration. Babineaux owns background-safe Unity imports, wiring and validation. Blender owns source normalization, two-cycle preparation and mining/contact adaptation when required. Gear Ball owns exact local commit manifests and any separately authorized publication. The author owns animation appearance, control feel and creative decisions such as mounting content. These are responsibilities; this plan starts no delegates or separate chats.

Three approaches were considered:

| Approach | Fit and cost |
| --- | --- |
| Extend the existing Playables driver with optional catalog playback and movement capabilities | Preserves the actor, camp/gather contracts and single pose owner. Adds bounded seams and focused regressions; chosen. |
| Replace the existing animation driver with an Animator Controller and blend trees | Viable as a full animation migration, but requires migrating locomotion phase/contact logic, camp and gather callers together. Broader serialization and production risk than this lab requires. |
| Build an independent preview character with Timeline/clip selection | Useful for contact inspection and marker authoring, but a standalone preview actor would not exercise the actual player, physical interactions or existing input ownership. Keep preview as a mode of the same lab actor instead. |

The chosen path keeps one Booter root, one Rigidbody, one capsule, one vitals owner, one input router and one Animator output. New capabilities are enabled by lab configuration assigned only in Camp Loop Testing. Shared code keeps its existing path when that configuration is absent. Reusable station prefabs carry authored IDs and anchors; they do not become another world generator or save service.

## Execution order and gates

Each row is a coherent implementation batch. The detailed plans name the files, data contracts, station objects and acceptance checks. Stop a failing batch before wiring dependent actions; independently completed families may remain usable.

| Batch | Owner and dependency | Work and exit gate |
| --- | --- | --- |
| 0 | Gottspan and Babineaux | Refresh branch/dirty ownership, editor lock/scene state, source hashes and capacity. Capture task-owned baseline bytes/GUIDs and shared references. No batchmode against the open checkout; no mutation of unrelated assets. |
| 1 | Gear Ball and Babineaux; after 0 | Install the narrowly scoped private-payload guard and restoration contract before copying. Copy 72 canonical FBXs, preserve Downloads, verify hashes, generate metadata once, and produce a transfer receipt. No raw Mixamo payload enters the staged/publication range. |
| 2 | Blender and Babineaux; after 1 | Verify Avatar, units, material, deformation and representative contacts. Prepare locomotion cycles when needed, measure all twelve profile roles and author action metadata. Unsupported or unreviewed clips remain disabled. |
| 3 | Babineaux; after 2 | Add the optional animation catalog branch, action lifecycle, motor ownership, lab input instance, session-only checkpoint policy, diagnostics and reset. Wire the existing Booter visual/profile once. Null lab configuration preserves current production behavior. |
| 4 | Babineaux; after 3 | Create the hub, calibration lane and lab station prefabs/root. Place on validated pads without editing TerrainData. All twelve IDs, bounds, colliders and the return route validate. |
| 5 | Babineaux; after 4 | Locomotion/strafe, crouch/duck, jump/fall/landing and slide. Gates: profile contacts, overhead clearance, physical state transitions, collisions and recovery. |
| 6 | Babineaux; after 5 | Pickup/examine/throw, button and push. Gates: target reservation, attachment, release/hit markers, exactly-once effects, interruption and reset. |
| 7 | Babineaux; after 5 | Cover and ladder. Gates: wall/rung alignment, visibility feedback, limits, entry/exit clearance and safe abort. Missing standing-wall/descent clips stay unavailable. |
| 8 | Babineaux and Blender; after 6 | Seat/ground rest/sleep and adapted mining. Gates: contact and exit transitions, existing camp lifecycle, no duplicate reward or checkpoint write. |
| 9 | Babineaux; after 6 | Punch/kick, directional hurt, death and get-up. Gates: one hit per strike/target, one health authority, interruption priority, terminal pose and recoverable lab reset. |
| 10 | Gottspan and Babineaux; after 5–9 | Complete catalog/station coverage and focused regressions; measure the bounded scene budget and reset/scene lifecycle. Produce a user playtest checklist and implementation receipt. Gameplay feel/visual acceptance remains with the author. |
| 11 | Author direction and relevant asset owner | Acquire/author mounting, crowd-specific or missing transitions if still wanted. These cannot be presented as implemented while only a fixture or substitute exists. Production promotion and publication remain separate. |

Batches 6, 7 and 8 are dependency ordering, not authorization for concurrent edits to shared player files. Commit verified task-owned units locally after their relevant gates. A local commit does not authorize a push, branch/worktree operation, package change or account action.

## World and persistence contract

Camp Loop Testing uses its existing authored world identity and separate checkpoint namespace. Its nine fixed terrain tiles remain resident, so terrain generation and terrain/chunk streaming are not applicable. Scene unload, disable and reload still tear down action ownership, graph resources and prop reservations. Station IDs are authored and stable; temporary props use a session allocator. No station identity depends on child order or random GUID generation at runtime.

Keep authored camp, terrain, rocks, resources and Legger access accounted for. Lab switches, props, damage and station rewards are session-only, resettable state. The existing save service snapshots health and actor position every 30 seconds when safe, so separate station state alone cannot prevent test damage from being saved. Batch 3 adds an explicit session-only policy for the configured Camp Loop playground, applied before checkpoint restore/writer creation and honored by autosave, manual save and quit. Existing CampLoopTesting and production files remain intact. Without the lab marker, existing checkpoint behavior remains unchanged. If persistent lab state is later requested, design its version/migration separately. Legger reset or recovery never teleports him to Booter.

## Validation and recovery

| Risk | Containment and affected gate |
| --- | --- |
| Incorrect scale, Avatar or limited grip | Character/contact gate in batch 2; do not propagate bad offsets into all station prefabs. |
| Single-cycle loops incompatible with contact mapping | Measured two-cycle preparation before batch 5; no invented contact phases. |
| Action movement overwritten by the motor's constrained early return | Extend the owned mode inside the existing motor path in batch 3; a separate component cannot bypass it by writing the Rigidbody afterwards. |
| Private payload missing or exposed to public Git | Hydration/preflight and staged/unpublished-range guard in batch 1; production remains independent of the lab's private dependencies. |
| Test health/position saved automatically | Session policy resolved before checkpoint startup in batch 3; autosave/manual/quit regression checks. |
| Extra lab resource breaks twelve-node preservation or farms rewards | Distinct lab target/component and session reward sink; keep existing resource tests and identities intact. |
| Missing entry/exit or incompatible death/get-up pairing | Adapt/review the complete sequence; keep that physical variant disabled until its family gate passes. |

Incomplete sets have explicit closure work. Batch 8 reuses verified compatible camp transitions where possible and authors/adapts chair stand-up, ground sit/stand and lie-down/wake transitions where needed. Batch 7 provides a physical return ramp from the ladder platform while descent remains unavailable. Cover exits through reviewed crouched movement and the accepted crouch-to-stand transition after clearance passes. Batch 11 can author a mount/dismount pair for a static mechanical seat fixture, standing wall-hug motion and a crowd-navigation motion set; these are lab experiments, not horse/vehicle canon or automatic asset purchases. Keep left/forward dodge and unarmed crouched death on the same named gap list. Mirroring or reversing a source counts only after contact, balance and transition review.

Asset gates prove file integrity, mapping, importer configuration, GUID continuity and representative deformation. Current Unity import/compile and focused EditMode tests prove integration contracts. The scene validator must inspect the loaded scene without opening, saving or installing content. Tests that open scenes run in an approved task-owned isolated validation copy, never by discarding the author's unsaved scene. Do not use the existing destructive scene builder as a playground installer.

Regression selection follows the changed seam: locomotion/smart traversal; interaction/resource/inventory; input/modal/rebinding; survival/camp/checkpoints; and the existing CampLoopTesting scene tests. Add focused tests for new behavioral contracts. No gameplay smoke tests are part of automated closeout. Performance evidence must use an explicit approved scenario; a player build is required only for a separately requested build outcome.

Recover a failing batch from its recorded task-owned bytes and metadata, preserving GUIDs. Disable the lab capability/root to contain an incomplete family; do not silently fall back to an unrelated animation while claiming it works. Preserve Downloads, private recovery files and unrelated edits. Do not reset or clean the whole checkout.

## Completion condition

The [planning verification receipt](../../Evidence/Gameplay/AnimationPlayground20261013/plan-validation.json) records source/hash coverage, map uniqueness, document routes, station coverage and the fresh input/save/storage findings. It establishes the plan's consistency, not completed Unity integration.

Implementation is complete when every source has a verified destination or alias; every accepted motion is reachable through its intended player/station behavior and preview; each unresolved clip is visibly unavailable with a named gap; station/reset/lifecycle and focused regression gates pass; and the author has a clear hands-on acceptance checklist. The plan is ready to execute in these bounded batches when implementation is requested. This planning task changes documentation and the planned mapping only.
