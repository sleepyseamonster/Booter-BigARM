# Camp Loop animation playground scene plan

Date: 2026-10-13. Part three of the [implementation plan](./CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md). Furnish the existing Camp Loop Testing scene with a walkable set of physical interactions, clear animation comparisons and reliable resets.

## Scene boundary and placement

Keep the scene at `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`, with its existing nine native terrain tiles, Booter, Legger, camera, lighting, HUD, camp rocks, Ironstone and cache. Do not rebuild it from production. Add one owned root named `Animation Playground` containing explicit station instances and layout/configuration. TerrainData and existing prefabs/profiles are shared with production; pads and new prefabs provide level test surfaces without terrain edits.

Use the authored camp location as the layout reference. Proposed hub offset is 90 m east and 80 m north of camp, with station centres on a 40 m grid around the hub. This is a starting placement proposal, not a claim that the ground is flat or unoccupied. Sample native ground, footprint, slope, clearance and existing content before fixing coordinates. Y always comes from ground/pad placement, never a copied absolute height. Keep at least a 20 m camp reserve, a 3 m return route and companion access. If a pad conflicts, adjust the owned layout within the nine-tile bounds; do not move existing content or sculpt terrain to fit it.

Most station pads are up to 28 × 24 m, connected by clear paths. Use modular flat decks, short ramps or legs with tested collision. Begin with simple rust-toned/neutral project-authored geometry. Every station and resettable prop receives an authored stable ID. Moving an instance preserves its ID; rebuilding an existing instance requires an explicit migration rather than new runtime identities.

Proposed station arrangement, viewed from above with north at the top:

```text
                     North
       Cover       Crowd corridor      Ladder
       Mining          Hub           Seat and rest
     Pickup/throw      Push           Combat/recovery
      Locomotion    Crouch/slide        Jump/fall
           Return route toward the existing camp
```

This arrangement separates throwing/strike lanes from ordinary movement and places rest near the hub. Final per-station orientation keeps impacts and falling contained inside the pad; pad edges and existing route intersections remain visible to the author.

## Station and prop specification

All dimensions below are prototype values to be fitted to the accepted character and measured clips. Adjust them through authored station data, not hardcoded source units. Each station has a name/sign, available actions, variant selector, availability reason, reset button and a safe actor return point.

| ID and station | Build or reuse | Player behavior and acceptance |
| --- | --- | --- |
| AP01 Hub and calibration | One-metre gauge, marked feet plane, catalog/reset terminal, character turn platform, station map | Switch accepted variants, preview/seek without effects, inspect scale/facing/contact, return to physical mode without stale input or duplicate actor. |
| AP02 Locomotion | 20 m marked lane, start/stop zones, wide turn circle, strafe markings and slight ramp | Idle/walk/run/sprint, starts/stops/pivots and left/right walk/run strafe. Evaluate foot slip, speed, facing and contact-driven dust. |
| AP03 Crouch and slide | Adjustable low beam, crouch tunnel, open 16 m slide lane, blocked stand-up section | Enter/idle/exit and four-direction crouch, duck, running slide. A blocked ceiling prevents standing; collision stops slide without crossing the barrier. |
| AP04 Jump and fall | Low vault obstacle, 0.5/1 m steps, 2/4 m fall platforms, contained landing area and clear floor | Standing/running jump, actual airborne fall, landing/roll, supplementary one/two-step climb on suitable obstacles and stable recovery. No early timer-based landing or double vertical trajectory. |
| AP05 Pickup throw and button | Low table, small scrap objects, examine object, throw lane/target/backstop, catch volume and switch panel | One-hand pickup/examine, three throw variants and button press. Contact/attach/release/activation occur once; props restore without duplication. |
| AP06 Push | Heavy crate with handle/contact anchors, 12 m lane, stop marker and movable obstruction | Push start/cycle/stop. Crate and actor stay aligned; obstruction stops movement; cancellation/reset restores both. |
| AP07 Mining | Resettable lab Ironstone stand-in, blunt mining tool, hand/impact anchors, strike marker and feedback placeholder | Adapted tool-use action hits the intended surface and grants only session test rewards. Existing production nodes and inventory remain accounted for. |
| AP08 Cover and concealment | 10 m low barrier, high wall, wall ends, L-shaped corner and passive observer dummies | Stand-to-cover, cover idle, left/right sneak; wall limits and safe exit. Feedback distinguishes standing exposed from crouched concealed using actual occlusion. |
| AP09 Crowd and wall corridor | Static dummy figures, two solid wall sections and adjustable 0.9–1.5 m passage | Standard walking/strafe can test spacing and clearance. Crowd-specific and standing wall-hug actions are labelled unavailable until matching clips exist; no crowd AI claim. |
| AP10 Ladder | Approximately 4 m ladder, adjustable rung anchors, bottom entry, top platform/guard, blocked top-exit variant and return ramp/stairs | Start/climb/top exit and supplementary step variants. Measure rung spacing and vertical progress. Reject obstructed exits; descent remains unavailable until reviewed. The return ramp completes the physical test loop. |
| AP11 Seat and rest | Chair with seat/feet/hand anchors, ground mat, side-sleep bedroll, entry/exit space; optional later seat/saddle research fixture | Chair sit/idle, ground sitting and sleep loop, with approved adapted/reused entry/exit transitions. Existing camp owns actual sleep/rest timing. Mounting is not implemented by sitting. |
| AP12 Combat and recovery | Padded training target with hit zones, directional damage controls, clear floor and impact marker | Jab/cross/hook, four kicks, small/large directional hurt, three accepted deaths and compatible back/stomach get-up. No weapons; one hit/health change per effect and recoverable lab reset. |

Cover prototype heights are about 1.1 m for low cover and 2.2 m for the high wall. Fit them to the final crouched/standing head positions. Ladder rung spacing starts around 0.3 m and must follow measured contacts. A prop's interaction point and hand socket are separate from its collision bounds. Chair legs, crate handles, wall corners and ladder top must not intrude into the actor's clearance envelope.

Required reusable prefab families under `Assets/_Project/Prefabs/AnimationPlayground/`:

- `Stations/`: Hub, Locomotion, CrouchSlide, JumpFall, PickupThrowButton, Push, Mining, Cover, CrowdCorridor, Ladder, SeatRest and CombatRecovery.
- `Props/`: Platform, Ramp, LowBeam, LowBarrier, HighWall, CornerWall, Ladder, Chair, Mat, Bedroll, PushCrate, ScrapObject, ButtonPanel, TrainingDummy, ThrowTarget, MiningTool and MiningNode.
- `UI/`: ActionPalette and StationFeedback. Reuse the existing EventSystem/canvas ownership and device prompt authority.

Create project-owned materials under `Assets/_Project/Materials/AnimationPlayground/` and minimal feedback under existing Audio/VFX boundaries only as needed. Initial audio/VFX are placeholders for impact readability, not new licensed purchases. Reuse existing resource visual definitions only through lab instances with separate IDs/state; never repoint shared production prefabs to test data.

## Hiding and wall interaction

Walls require solid colliders, a cover plane/tangent, valid action segments, entry/exit anchors and clearance volumes. The cover target supplies these data to the action coordinator; it does not move the actor independently. Test short and long segments, ends, corner blockage and standing exit under a beam. Keep crouched cover distinct from the missing standing wall-hug behavior.

Add a passive visibility probe for playtest feedback. Observer eye anchors check line of sight to the player's reviewed head/chest/pelvis sample points through opaque station/world colliders. Ignore observer/player self-colliders and nonblocking triggers; validate existing layer/mask semantics before proposing any new project-wide layer. Report exposed samples and which wall blocks them. The probe demonstrates concealment, not enemy awareness or combat AI. The camera can still show the player while the observer cannot see him.

A low-wall experiment passes when standing is exposed, crouching appropriately reduces/blocks the observer's line of sight, and wall-end movement exposes the player again. Mere proximity to a wall or playing Cover Idle cannot set a hidden flag. Use a bounded probe interval and show its result in the station UI.

## Authoring and scene installation

Plan `AnimationPlaygroundSceneInstaller.cs` and `AnimationPlaygroundSceneValidator.cs` under `Assets/_Project/Scripts/Editor/AnimationPlayground/`. The installer accepts the already-loaded target scene and explicit reviewed layout/configuration. It verifies the correct scene, editor state, private asset gate and baseline references, then creates only task-owned prefabs/root/player bindings with Undo support. A dirty scene is not automatically saved, reopened or discarded; reconcile the author-owned state first.

Separate install/update from validation. Install once by stable root/station IDs; subsequent updates change only owned instances and preserve IDs. Report a conflicting root or missing authored station rather than duplicating it. Do not invoke `CampLoopTestingSceneBuilder.BuildFromCli` or `CompleteFromCli`; they are not this installer. The scene save is an explicit final implementation step after owned changes and references validate.

The validator inspects the loaded scene without opening, saving, installing or entering Play Mode. It checks nine resident terrains, original TerrainData GUIDs/bytes, original production routing, one Booter/camera/input/EventSystem, new visual/profile/catalog references, twelve unique station IDs, bounded/grounded pads, required anchors/colliders, usable return points, temporary prop ownership limits and the private asset gate. Extend the existing Camp Loop scene tests without weakening terrain/resource preservation assertions.

Layout settings: `Assets/_Project/Settings/AnimationPlayground/PlaygroundLayout.asset`. Scene-runtime coordination/reset uses `TopDown3DAnimationPlaygroundController` and station/reset participant components in the runtime assembly. The scene stores references to authored data; it does not generate a different obstacle course each launch.

## Reset and lifecycle

The scene root carries an explicit session-only checkpoint policy. Resolve it before the existing save service restores a file or creates its writer, so test damage, position and rest state cannot enter existing checkpoint files. Autosave, manual save, disable and quit honor the same policy. The validator checks this policy and rejects it in production.

Keep the original twelve Ironstone nodes and one resource cache counts. The lab mining target uses a distinct component and session reward ledger, preserving the current scene validator and installer contracts.

Capture each owned station's authored prop transform, Rigidbody mode, switch state, resource uses and availability on initialization. Reset Station rolls back its session state and safely ends an action using it. Reset All clears every temporary participant/effect, restores lab health and the player's safe return state, and rearms input after release. Cap active thrown props, keep them in the catch/return region and remove only registered lab spawns. Reset does not delete unrelated scene objects or reset production inventories/checkpoints.

Scene unload or disable releases all target reservations, held objects, action leases, visibility listeners and temporary graph/feedback state. Reload reconstructs authored IDs and session defaults; no automatic lab persistence is added. Nine always-loaded tiles mean no terrain-streaming component. Generated-object persistence is not applicable to these authored stations and session props; future persistent thrown objects need a separate stable-ID/save contract.

## Budget and hands on review

Start with one skinned player, simple crowd dummies, mostly static geometry and bounded transient physics. Proposed initial limits are 16 live throwable props, one active action target/held prop, a single active training target and visibility probes at 10 Hz. Use explicit registration and non-allocating bounded queries where practical; no per-frame scene-wide scans or animation graph rebuilds. These are starting limits to validate, not measured performance claims. Do not add weather, terrain streaming or unrelated simulation to make the lab richer.

The final author checklist covers:

1. New character height, facing, skin and feet/hand contact; idle/run/walk/sprint quality and transitions.
2. All crouch directions, blocked standing, slide collision, grounded takeoff, fall and landing/roll.
3. Pickup/examine, all throw variants, button contact and push start/stop against obstruction.
4. Cover entry/idle/travel/end/exit and observer visibility; ladder entry/rungs/top exit/blocked exit.
5. Mining grip/impact/reward, chair/ground/sleep contact and complete accepted exits.
6. Strike timing, all hurt/death variants, compatible get-up and no weapon-style pose in the unarmed set.
7. Gamepad and keyboard operation, cargo tap/hold, menus/device transfer, cancel/reset and repeated station use without duplicate objects/effects.

Automation proves structural wiring, ownership, collision/transaction rules and reset invariants. The author assesses appearance, feel and physical contact in Play Mode. Mounting, crowd-specific motion and other missing transitions stay visible as gaps until acquired/authored and checked; a fixture alone never satisfies those actions.
