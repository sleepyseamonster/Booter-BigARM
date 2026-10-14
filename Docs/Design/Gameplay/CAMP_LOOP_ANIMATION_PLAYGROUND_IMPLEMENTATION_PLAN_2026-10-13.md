# Camp Loop animation playground implementation plan

Date: 2026-10-13. Revision 2, following the [plan audit](./CAMP_LOOP_ANIMATION_PLAN_AUDIT_2026-10-13.md). Target: `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`. Status: implementation active; verified slices are recorded in the [implementation status](./CAMP_LOOP_ANIMATION_IMPLEMENTATION_STATUS_2026-10-13.md). This revision replaces the earlier batch order and storage/input/contact assumptions.

Build a playground where the author controls the existing Booter actor, interacts with real objects, compares accepted animation variants, and resets experiments. Source download, Unity import, preview, physical integration and hands-on acceptance are separate readiness levels. A functioning clip viewer does not establish a functioning ladder, throw or mining action.

## Plans and source boundaries

| Plan | Responsibility |
| --- | --- |
| [Prerequisites](./CAMP_LOOP_ANIMATION_FOUNDATIONS_PLAN_2026-10-13.md) | Dependency restoration, session safety, targeting/activity, input/modal policy, action clocks/transactions, owned motor/presentation/reset and qualification fixture |
| [Asset import](./CAMP_LOOP_ANIMATION_ASSET_IMPORT_PLAN_2026-10-13.md) | Exact transfer/alias map, private payload/metadata pairing, source/target rig qualification, individual clip policy and reproducible bindings |
| [Player integration](./CAMP_LOOP_ANIMATION_PLAYER_INTEGRATION_PLAN_2026-10-13.md) | New visual on the existing actor, one pose/motion authority, action families, controls, props and damage/recovery |
| [Scene playground](./CAMP_LOOP_ANIMATION_PLAYGROUND_SCENE_PLAN_2026-10-13.md) | Twelve authored station areas, cover visibility, anchors/colliders, reset, layout and author acceptance |
| [Motion backlog](./CAMP_LOOP_ANIMATION_MOTION_BACKLOG_2026-10-13.md) | Missing roles, existing substitutes to qualify, acquisition/adaptation priorities and requested gap closure |

The original [folder inventory](../../Evidence/Gameplay/AnimationPlayground20261013/animation-inventory.json) remains historical evidence. The [expanded source receipt](../../Evidence/Gameplay/AnimationPlayground20261013/PlanReaudit/expanded-source-inventory.json) accounts for 148 collection copies and 74 byte-distinct payloads. Keep 72 Unity FBX destinations: one source character, 68 unarmed candidates and three restricted imported references. The additional two payloads are redundant sprint export versions retained outside Assets, not new gameplay clips. The [schema 2 transfer plan](./AnimationPlayground20261013/asset-import-plan.json) records both sets and original rig alternatives separately.

ART1 may add at most two private target-rig qualification FBXs from the original exports, with separately recorded destinations and paired metadata. They are outside the 72-file motion-collection map; the accepted Mixamo target option reuses the canonical source character. Choosing a target cannot silently change these counts or replace the source Avatar.

## What must precede station construction

The repo already has movement, input/UI, gathering, inventory, camp, vitals and fixed terrain. Extend those owners. The missing prerequisites are generic station discovery/dispatch; consistent busy/availability policy; lab command/rebinding/menu integration; simulation/preview clocks and effect ledger; owned motor/posture snapshots; reset/bootstrap readiness; and private asset plus calibrated Avatar/contact qualification. Build and test these seams with existing/synthetic fixture data before adding all interaction props.

Preserve main tracking origin/main. Planning began at `9f891713136ef8336ddf2b0aab676a1ac897de7e`; unrelated Fused rock files remain excluded. Unity 6000.4.0f1 owns the checkout and was observed in clean Edit Mode. Refresh this state at implementation start, preserve foreground applications and never start batchmode against the open project.

Gottspan coordinates and integrates. Babineaux owns Unity wiring/validation. Blender owns rig/source normalization, contacts and motion adaptation. Gear Ball owns exact local commit manifests and separately authorized publication. The author owns appearance/feel and creative acceptance. This plan starts no agents, imports or account actions.

## Recommended architecture

Keep one Booter root, Rigidbody, capsule, vitals/inventory owner, input router, camera/EventSystem and Animator output. Extend the existing Playables driver with optional lab configuration; absent configuration retains legacy behavior and assets. The motor remains the physical writer. Station/action components submit requests through its owned mode, not a competing Transform or velocity update.

A generic target descriptor feeds the existing selector and capture/revalidation flow. Dispatch non-resource actions explicitly; preserve legacy gather/cargo/canister/camp handling. A shared activity projection reads these owners rather than introducing another lock registry. The action palette belongs to the existing menu/modal owner. Public recipes describe source IDs and policies; private binding assets resolve imported clips/models after hydration.

An Animator Controller migration remains viable but would require moving existing contact/camp/gather logic together and has greater scope. An independent Timeline preview actor would not test the real controller. Retain one driver and a preview mode on the actual lab actor. No new motion-matching, crowd AI, network, vehicle or ragdoll framework is prerequisite to this scope.

## Dependency order

| Stage | Dependency and work | Exit gate |
| --- | --- | --- |
| PRE0 | Refresh authority, branch/dirty ownership, editor/lock, scene, source hashes, destination ownership and capacity | Task baseline is stable; foreground and unrelated files protected. |
| DEP1 | After PRE0: private source/metadata restoration contract, scoped guards, public folder scaffolding and dry-run mapping | All 148 aliases/74 payloads accounted for; no public raw files or orphan metadata; original bytes preserved. |
| FND1 | After DEP1: scene bootstrap, private binding readiness, checkpoint policy and injected lab controls directory | No world restore/write in configured lab, including quit; missing assets are explicit; production/null configuration unchanged. |
| FND2 | After FND1: target descriptor/query/dispatch and activity availability | Legacy targets and primitive test stations select correctly; press target cannot redirect; no fake gather reward or stolen lease. |
| FND3 | After FND2: router commands, owned input instance, coactive rebinding and menu-hosted palette | New commands and overrides validate; tap/hold, modal release and device/focus behavior preserved; no production preference overwrite. |
| FND4 | After FND2–3: clock/marker/transaction rules, motor modes/snapshots, pose/contact routing and reset contract | Pure/primitive fixtures prove ownership, posture/support, pause/cancel/epoch effects and cleanup without requiring all source clips. |
| ART1 | After DEP1/FND1: minimal source/target rig and calibrated fixture | Choose documented rig/quality tier; scale, material, Avatar, weights and representative grip/contact pass. Other foundation rules may use existing fixture art. |
| ART2 | After ART1/FND4: bulk canonical import, private binding set, calibrated contacts/markers and complete profile | Twelve locomotion roles pass; cyclic R-L-R-L order and root/facing policy measured; private metadata and IDs restore exactly. |
| INT1 | After ART2: existing Booter visual/profile cutover and optional graph branches | One output/motor/contact authority; private readiness and legacy/camp callers verified; preview causes no effects. |
| LAB1 | After INT1: hub and twelve station shells/layout | Owned bounded pads, IDs, routes/clearance and validator pass without TerrainData or production edits. |
| PHY1 | After LAB1: locomotion/strafe, crouch/duck, jump/fall/land, slide/dodge | Keyboard walk access; unsupported backpedal gated; takeoff/landing and posture-aware collision/recovery pass. |
| PHY2 | After PHY1 and family clips: pickup/hold/drop/throw, button and push | Qualified hold/release; no unqualified moving carry; exactly-once effects, obstruction and reset. |
| PHY3 | After PHY1 and family clips: cover/wall and ladder | Wall/rung/end/top clearance and visibility pass; ascent has physical return route; no reversed-ascent descent claim. |
| PHY4 | After PHY2 and complete sequences: chair/ground/sleep and adapted mining | Enter/idle/exit, camp ownership/equipment and grip/impact qualified; session reward sink preserves original node counts/economy. |
| PHY5 | After PHY1–2 and qualified posture sets: strikes, hurt/death/get-up | Direction/severity, collision/gravity, terminal hold, recovery clearance and reset pass; missing posture variants remain explicit. |
| EXT1 | After relevant families: requested mounting and standing-wall/crowd alternative, plus remaining requested transitions | Licensed/custom suitable motion and target sequence pass; a static seat fixture is the initial mounting scope, not a vehicle simulation. |
| ACC1 | After implemented families: coverage, focused regressions, hydration/reload/reset and author checklist | Candidate and full-requested milestones reported separately; no missing role silently called complete. |

These dependencies do not authorize concurrent edits to coupled player files. Commit only verified task-owned batches. Push, worktree/branch operations, package changes, purchases or account visibility changes require their own authority.

## Motion and completion gates

Prioritize backward locomotion/standing turns for full facing-independent movement; a qualified hold and safe release before pickup completion; complete chair/ground/bed transitions; and posture-compatible reactions/death. New downloads are not automatically required for starts/stops/pivots, jump phases, cover exit or get-up: existing assets may be retargeted, segmented or adapted after qualification. The backlog records the method and exact gate.

Milestone A is a candidate playground with accepted source families, preview and physical behavior, while explicit gaps stay unavailable. Milestone B fulfills the complete requested playground, including actual mining adaptation, rest/seat transitions, a mount/dismount test and the requested wall/crowd motion alternative. Do not relabel requested gaps as optional polish. Lean/peek, use-item presentation, carry variants and extra idles can follow as separately recorded enhancements.

## World, storage and reset contract

Use CampLoopTesting's authored identity. Nine native tiles remain resident; terrain generation/streaming are not applicable. Authored station IDs and session prop epochs survive layout edits and prevent stale target requests; scene disable/unload/reload releases leases/listeners/graph state. Preserve camp, resources, rocks and companion paths. Legger never teleports as part of reset.

New station state, rewards, damage and actor resets are session-only. Configure the checkpoint service before Start so existing CampLoopTesting and GreaterWasteland files are neither restored into the lab nor written by it. Controls preferences have their own lab directory; ordinary settings are not redirected accidentally. Private FBXs, their paired metadata and bound clip/model assets stay together privately. Public recipes, GUID/import receipts and folder scaffolding support restoration without breaking metadata pairing. No new public payload/LFS or account policy is assumed.

Reset invalidates action/target epochs first, clears pending effects, releases ownership, restores registered props/targets and actor posture/gravity/vitals, selects a collision-clear return, then rearms inputs after release. Avoid reset rewards, arbitrary scene-body deletion, hidden movement fallbacks and unresolved queued writes.

## Verification and readiness

Verify every stage against its named contract: file/GUID restoration; bootstrap/save/preferences; target/busy/input/modal; clock/transactions; motor/posture/support; Avatar/profile/contact; graph/private bindings; scene layout; each physical family; and coverage/reset/reload. Existing tests that open scenes run in an approved isolated hydrated candidate. Loaded-scene validation must not open/save/install content. Gameplay smoke tests and visual/feel acceptance remain author-owned. Performance captures use a named authorized scenario; builds/publication are separate outcomes.

The [reaudit receipt](../../Evidence/Gameplay/AnimationPlayground20261013/PlanReaudit/verification.json) establishes source and plan consistency. It does not establish Unity import or gameplay. The package is ready for bounded prerequisite implementation when requested; full interaction completion remains conditional on rig/contact qualification and the named motion gaps. This audit rewrites documentation only.
