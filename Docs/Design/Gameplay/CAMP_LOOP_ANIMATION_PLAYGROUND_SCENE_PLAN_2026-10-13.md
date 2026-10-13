# Camp Loop animation playground scene plan

Date: 2026-10-13. Revision 2 of the [master plan](./CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md). Furnish the existing scene after foundations, rig/profile qualification and player integration pass. The [motion backlog](./CAMP_LOOP_ANIMATION_MOTION_BACKLOG_2026-10-13.md) supplies per-station missing-role gates.

## Authored layout and scene ownership

Keep `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`, its nine native terrains, Booter/Legger/input/camera/EventSystem, camp rocks, twelve Ironstone nodes, cache and existing routes. Add one owned `Animation Playground` root with session configuration, private readiness binding and station instances. Do not rebuild from production or edit shared TerrainData/prefabs/profiles to fit the lab.

Start layout sampling at camp plus approximately 90 m east/80 m north, with 40 m station spacing and pads up to 28 × 24 m. These are proposals pending native ground/slope/clearance/content sampling. Derive Y from support; preserve at least a 20 m camp reserve and a 3 m return corridor. Move owned stations within the nine-tile bounds when needed; report an impossible footprint instead of moving existing content or sculpting terrain.

Use modular flat decks, short ramps/legs and project-authored geometry/materials. Every station/prop has authored identity plus reset epoch. Repositioning preserves identity; reset invalidates old captures. Prefer one skinned player and static simple dummies, not copies of the full character.

```text
                     North
        Cover      Crowd/wall corridor      Ladder
        Mining           Hub              Seat/rest/mount
     Pickup/throw        Push              Combat/recovery
      Locomotion      Crouch/slide          Jump/fall
                Return toward existing camp
```

Build the hub/calibration and empty station shells first. Add complete physical families only after their roles qualify. Each station shows source/import/preview/physical/author readiness, chosen variant, availability reason and reset. A fixture without its animation/behavior is labelled incomplete.

## Twelve station areas

Dimensions below are prototype values to fit to the measured target rig. Interaction point, grip/contact anchors and collision bounds are separate. Outward throw/strike/fall paths remain contained within the pad.

| ID | Objects and action coverage | Required gate |
| --- | --- | --- |
| AP01 Hub | One-metre gauge, feet plane, turn platform, menu-owned catalog/preview/reset terminal and station signs | Calibrated visual/private bindings; preview effects disabled; one actor and clear return. |
| AP02 Locomotion | 20 m lane, start/stop zones, turn circle, strafe markings and mild ramp | Keyboard slow walk, measured loops/transitions/footstream. Backpedal/idle turn remain gated until their roles qualify. |
| AP03 Crouch/slide | Adjustable beam/tunnel, 16 m slide lane, blocked stand-up area | Four-direction crouch, enter/exit/duck and slide phases; shape sweeps/ceiling, no auto-vault conflict, safe recovery. |
| AP04 Jump/fall | Low vault, 0.5/1 m steps, 2/4 m platforms, contained landing/floor zone | Takeoff/airborne/landing/roll/step roles, support hysteresis and real contact. Fall/roll are not completed by a timer. |
| AP05 Pickup/throw/button | Table/scrap objects, hold/contact markers, throw target/backstop/catch volume, switch | Qualified hold/drop/release, all three throw variants and one activation. Moving carry/place-down require their own roles. |
| AP06 Push | Heavy crate/handles, 12 m lane, stop marker and obstruction | Start/loop/stop, crate/actor collision ownership, cancellation and reset. |
| AP07 Mining | Separate lab node, blunt mining tool, grip/impact marker and feedback | Adapted swing/return and hand quality; local reward ledger. No thirteenth production node or real resource sink. |
| AP08 Cover | About 10 m low barrier, high wall, ends/L-corner and passive observer eyes | Crouched enter/idle/sneak/exit, tangent/end/clearance and measured occlusion. Suggested 1.1/2.2 m heights are fitted to actual posture. |
| AP09 Crowd/wall | Simple dummies, two walls, adjustable 0.9–1.5 m corridor | Standard walking can test spacing. Requested standing wall-hug/crowd-squeeze alternative needs a qualified motion set; no crowd AI claim. |
| AP10 Ladder | About 4 m ladder, adjustable rungs (~0.3 m initial), entry/top anchors, guard/blocked exit and return ramp/stairs | Start/cycle/top exit and hand/foot contacts; safe gravity/abort. Descent requires its own reviewed role; ascent is not reversed silently. |
| AP11 Rest/mount | Chair/seat/feet anchors, ground mat, side-sleep bedroll, exit/rise clearance; later static mechanical saddle/seat fixture | Complete rest enter/idle/exit sets and camp ownership. Requested mount/dismount needs actual motion; chair sitting is not mounting. |
| AP12 Combat/recovery | Padded target, hit zones, directional damage controls and clear floor | Jab/cross/hook, kicks, standing/posture-compatible reactions/deaths/get-up, corpse collision and exactly-once damage/reset. No weapons. |

Prefab root: `Assets/_Project/Prefabs/AnimationPlayground/`, with Stations, Props, Player and UI folders. Public Player is a project-authored visual host; model/clip bindings remain private as the asset plan specifies. Public materials belong under `Assets/_Project/Materials/AnimationPlayground/`. Primitive props are enough initially; replace visuals without changing station IDs/interfaces.

## Cover visibility and collision

Wall targets supply plane/tangent, bounds, collider ownership, contact and entry/exit clearance. The action motor owns movement. Line-of-sight selection must recognize the target's compound colliders, and station triggers must not falsely block observation rays.

Passive observer eye anchors probe the reviewed head/chest/pelvis samples against opaque colliders, excluding self and nonblocking triggers. Show exposed samples/blocking wall at a bounded interval. Crouched concealment follows actual rays and posture; being near a wall or playing Cover Idle does not set a hidden flag. Test standing exposure, crouched occlusion and wall-end exposure. This demonstrates concealment without inventing enemy AI. The camera remains free to show the player.

Reuse existing masks only after their semantics validate. Do not introduce global layers or turn off production collision merely to make a station work. Obstructions, slopes, ceiling clearance, pad edges and landing supports all belong to scene acceptance, not only animation review.

## Install and validate separately

Planned Editor tools: `AnimationPlaygroundSceneInstaller.cs` and `AnimationPlaygroundSceneValidator.cs` under `Assets/_Project/Scripts/Editor/AnimationPlayground/`. Install accepts the already-loaded target scene, verified recipe/bindings and explicit layout. It checks editor state, ownership, capacity, dependencies and baseline references; creates/updates only owned IDs with Undo; and refuses conflicting roots. Do not call the old scene Build/Complete methods or reopen/discard a dirty scene to continue.

Validation inspects without opening, saving, installing or entering Play Mode. Check nine original terrain/collider pairs and TerrainData hashes/GUIDs; original camp/resources/route; one actor/input/camera/EventSystem; session policy and private readiness; twelve IDs/pads/anchors/colliders; clear returns/exits; target dispatch; prop budget and excluded source gating. Saved scene changes are an explicit final implementation step after these checks.

Existing `CampLoopTestingSceneTests` open/clear scenes and install UI. Run them and new integration fixtures only in an approved isolated hydrated candidate, retaining the exact twelve-node/one-cache assertions. Pure contracts and primitive physics fixtures can qualify foundations before this scene is furnished.

## Reset, lifecycle and budgets

Session-only checkpoint policy resolves before existing save restore/writer startup and applies to timer/manual/disable/quit. Controls use the separate lab domain. Capture authored station transform, Rigidbody mode, switch/use state and reset epoch. Reset releases action/effect/prop ownership first, restores registered state and lab vitals/posture, finds a clear actor return and rearms after release. Never delete unrelated bodies, farm persistent rewards or teleport Legger.

Scene unload/reload/disable reconstructs authored IDs and session defaults, releasing leases/listeners/private graph state. The nine fixed tiles do not stream or regenerate. Temporary props use an explicit allocator/epoch; persisted generated-object identity is not applicable until a separately designed save slice. Existing world identity and authored constraints remain intact.

Initial budgets to measure: one skinned actor, up to 16 active throwables, one held prop/action target, static crowd dummies, one active training target and observer probes at 10 Hz. Use explicit registration and bounded queries; no per-frame scene scans/graph rebuilds. Scene size, physics, skinning and private binding load cost are measured after integration; these values are not current performance proof.

## Author acceptance and milestones

Check rig/scale/skin/grip/feet; all gait/control variants and transitions; crouch/slide/ceiling; takeoff/fall/contact/roll; held object/release/push/button; cover/rungs/blocked exits; mining impact/reward; full rest/mount sequences; damage/posture/corpse/rise; modal/device/cargo behavior; repeated reset and unload without duplicated effects or altered checkpoint files.

Milestone A demonstrates qualified candidate families and labels all gaps. Milestone B also closes the requested mount and wall/crowd alternative and complete requested sequences. Missing clips are not silently represented by dummies, chair sitting, backwards ascent or an unrelated reaction. Appearance/feel/contact acceptance belongs to the author; static validation and clip preview prove narrower properties.
