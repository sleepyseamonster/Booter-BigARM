# Camp Loop animation playground: inventory and audit

Date: 2026-10-13. Status: **audit and proposal only**. Owner: Gottspan coordination, Babineaux Unity integration, Blender asset handoff. The author requested a playground in the existing Camp Loop Testing scene, and explicitly limited this stage to inventory, audit and brainstorming. The subsequent instruction authorized downloading the original action shortlist; those downloads are included here. This document does not authorize implementation or production promotion.

## Result and immediate decisions

The collection contains **73 FBX copies, 72 unique files: one character T-pose and 71 motion candidates**. The mining swing is duplicated across two download folders. There are **68 distinct unarmed candidates for playground evaluation**, plus three restricted references: the downward axe swing requiring mining adaptation, a sprint that retains forward travel, and a crouched death with a weapon-style pose. “Candidate” means downloaded and structurally inspected; it does not mean Unity import, contact quality or gameplay readiness has passed.

All files have the same 33-bone hierarchy. All 71 unique motion files change pose at sampled times; the T-pose is correctly static. Animation exports are FBX for Unity, 30 fps, without skin and without keyframe reduction; the base character includes skin. Defaults and authored travel are retained in the original-action exports. The recommended four locomotion exports and the two cover sneak loops were exported in place. Hip samples support those six start/end closures, but do not prove the absence of travel at every frame.

Camp Loop Testing is open, loaded, clean, and outside Play Mode. It already has the production movement/input/camera/interaction/camping foundations. The current animation driver uses a 21-slot Playables mixer and disables root motion. The full collection cannot be integrated simply by replacing four locomotion clips or adding a second Animator Controller.

Recommended next implementation boundary: a scene-scoped playground player/profile, reusable interaction stations and an action catalog feeding one animation authority. Keep the current terrain, production scene, input preferences and checkpoint contracts intact. First prove the new character's Avatar, scale and contacts; then add real systems in small batches. Each batch should support both a clip preview and its applicable physical interaction.

## Download inventory and original shortlist coverage

Files remain under `C:\Users\kbkno\Downloads`. No FBX was copied into `Assets` in this audit. The [machine inventory](../../Evidence/Gameplay/AnimationPlayground20261013/animation-inventory.json) records each file's location, SHA-256, size, frame range, duration, skeleton check, pose sample result and restriction. The [evidence receipt](../../Evidence/Gameplay/AnimationPlayground20261013/README.md) records how it was measured.

| Download folder | FBX copies | Contents |
| --- | ---: | --- |
| `MixamoLocomotion20261013` | 6 | Character T-pose, Breathing Idle, Standard Walk, Standard Run, Two Cycle Sprint, forward-travel Sprint alternative |
| `MixamoRest20261013` | 1 | Sleeping Idle / Sleeping Deeply |
| `MixamoUnarmedCover20261013` | 4 | Left/Right Cover Sneak, Stand To Cover, Cover Idle |
| `MixamoUnarmedInteractions20261013` | 15 | Fourteen additional interactions below, plus the duplicate mining reference |
| `MixamoOriginalActions20261013` | 47 | All twelve original categories, including explicitly restricted references |

| Original action | Downloaded selection | Readiness limit |
| --- | --- | --- |
| Mining | Standing Melee Attack Downward | One-handed axe-style reference. No dedicated mining/pickaxe/hammer result was found. Adapt tool grip, swing plane, impact and return; it is not a finished mining animation. |
| Crouch | Enter, idle, exit, forward/backward/left/right walk: 7 | Source families vary; blend transitions and timing need review. Enter is about 4.07 s, so responsiveness may require trimming or deliberate playback tuning. |
| Slide | Running Slide: 1 | About 1.53 s; needs speed, reduced collider, clearance and stand-up rules. |
| Strafe | Walk left/right and running Left/Right Strafe: 4 | Facing and movement are independent; retained lateral travel needs per-clip import treatment. |
| Ladder | Start, climb loop, top exit, one-step and two-step variants: 5 | Needs measured rung spacing, bottom/top anchors and exit clearance. Step variants are supplementary candidates, not proof of a complete ladder cycle. |
| Throw | Throw, Throwing, Throw Object: 3 | Needs prop ownership, grip, release timing, trajectory and cancellation. |
| Punch | Jab, cross, hook: 3 | Needs contact windows and recovery, beyond playing the clip. |
| Kick | Front snap, thrust, side, MMA low: 4 | Balance, hit reach and recovery need review. |
| Jump | Standing, running, unarmed: 3 | Full source jumps need splitting/timing against real takeoff, apex and touchdown. |
| Fall | Airborne idle, landing, roll landing: 3 | Ground detection owns landing; falling loop must not complete into a landing while airborne. |
| Hurt | Body hit; small and large reactions from front/back/left/right: 9 | Body-hit variant replaces the generic rifle-posture Hit Reaction. Need severity, direction and interruption rules. |
| Death | Standing backward, falling backward, falling forward; crouched death reference: 4 | Standing backward replaces the generic rifle-posture Death. Crouched Death retains a weapon-style starting pose and is excluded from the unarmed set. |

The additional fourteen interaction candidates are: one-hand pickup, pickup/examine, get up from back, get up from stomach, button press, push start/cycle/stop, chair sit-down, chair idle, ground sitting idle, backward dodge, right dodge and duck from standing. They were previewed without visible weapons. Their translation remains authored; Unity retargeting and contact verification are pending.

Requests still lacking a dedicated downloaded clip:

- Horse or motorcycle mounting/dismounting. A chair transition can test seat alignment, but does not fulfill either mounting action. Custom authoring or another licensed source is needed if these remain required.
- Walking through a crowd. Standard Walk can test a narrow corridor of dummy figures; actual yielding, shoulder turns or crowd avoidance need additional clips and/or navigation logic.
- Standing wall sidestep. The downloaded cover sneak clips are crouched wall travel; they do not fulfill a standing wall-hug action.
- True mining, unarmed crouched death, ladder descent/bottom exit, chair stand-up, ground sit-down/stand-up, lying down/waking up, and left/forward dodge. Some may be adapted or mirrored after review; completeness is not claimed.
- New-character locomotion starts, stops and pivots. Existing licensed Humanoid clips may be retargeted for the lab, but the four new locomotion loops alone cannot fill the existing profile.

## Character and import audit

The active Mixamo character was auto-rigged from the separately exported mesh-only FBX. The accepted website name remains `male_basemesh_mixamo_unrigged`, even though Mixamo has now rigged it. The local skin-bearing handoff is `Downloads\male_basemesh_mixamo_rigged.fbx`, byte-equivalent to the collection's `Character T-Pose.fbx`. The original Rigify Blender source and the earlier rigged FBX attempts remain separate; Mixamo rejected those existing skeletons. They are not substitutes for the accepted Mixamo Avatar.

The imported base has 31,739 vertices, 31,580 polygons, approximately 63,134 triangulated faces, and zero material slots. It needs a Unity material and a deliberate visual prefab. The 33-bone rig includes only index-finger chains; there are no thumb, middle, ring or little-finger chains. Body animation can still be evaluated, but closed grips and finger contact cannot be assumed. Tool, button and ladder tests must expose this limitation. If grip fidelity fails, choose between a deliberately limited lab rig and a fuller compatible rig/retargeting handoff before building dozens of contact offsets.

Blender reimport shows object scales near 0.01 and dimensions around 20–23 Blender units. These are not certified Unity metres. Measure the imported character against a one-metre reference, verify facing/up axes and feet, then tune the visual scale, offset and capsule together. The current scene capsule is height 2.48, radius 0.52; its old visual scale 1.4 and offset are calibrated for the existing default male, not this model.

Proposed import gate:

1. Preserve original download hashes and local Blender sources. Import one canonical skin-bearing FBX as Humanoid, create its Avatar, verify required and optional mapping and a correct T-pose. For matching animation hierarchies use Humanoid / Copy From Other Avatar with that validated source. A successful automatic mapping alone does not establish quality. [Unity 6.4 Humanoid import](https://docs.unity3d.com/6000.4/Documentation/Manual/ConfiguringtheAvatar.html).
2. Verify units, bind pose, shoulders, knees, fingers, skin weights, mesh orientation, material and capsule. Compare representative idle, crouch, ladder, chair, sleep, jump and death clips before accepting the character.
3. Set loop flags only for actual loops. One-shots retain a defined entry, contact/release and exit; death holds its terminal pose until reset. Trim source rests intentionally and retain the source frame range in metadata.
4. Record each clip's root translation/rotation policy. The motor owns free movement and jump gravity. Station actions own collision-tested movement between anchors. Preserve source trajectories for measurement, then remove or consume their travel deliberately; do not blanket-bake all axes or enable `applyRootMotion` while the Rigidbody also moves. Strafes require explicit facing checks because movement is perpendicular to facing. [Unity root-transform controls](https://docs.unity3d.com/6000.4/Documentation/Manual/RootMotion.html).
5. Give every accepted action a stable catalog ID, variant, clip, loop flag, timing markers, motion owner, cancellation/recovery policy, allowed posture, required target/prop, hand socket and reviewed status. Use normalized markers or catalog timing with exactly-once gameplay effects; imported clip events alone should not own rewards or releases.

Proposed paths, subject to the repository's transfer/metadata gates:

| Content | Destination |
| --- | --- |
| Canonical character FBX and character material | `Assets/_Project/Art/Characters/AnimationPlayground/` and `Assets/_Project/Materials/Characters/` |
| Canonical motion FBXs by action family | `Assets/_Project/Art/Animations/Mixamo/` |
| Lab-only catalog, locomotion/contact profiles and input clone | `Assets/_Project/Settings/AnimationPlayground/` |
| Player variant and reusable station/prop prefabs | `Assets/_Project/Prefabs/AnimationPlayground/` |
| Runtime components in the existing production runtime assembly | `Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/` |
| Isolated editor import/validation helpers, if required | `Assets/_Project/Scripts/Editor/AnimationPlayground/` |
| Evidence, provenance and transfer receipts | `Docs/Evidence/Gameplay/AnimationPlayground20261013/` |

Keep one canonical copy of the mining reference, account for both download hashes, and preserve the originals. Unity creates `.meta` files once during the future import; keep each FBX and its GUID paired. Do not regenerate existing metadata. Native Blender files remain local under the repository's source-publication policy.

Adobe's current FAQ permits game use. The Adobe-hosted licensing guidance distinguishes team access from distributing raw files to non-team members. Before future GitHub publication, establish repository visibility and a compliant storage route for the raw FBXs; a local Unity import is a separate step. Character-source provenance also needs a license receipt. This audit changes no publication policy, LFS rules or package versions. [Adobe Mixamo FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html), [Adobe Community licensing guidance](https://community.adobe.com/questions-696/mixamo-faq-licensing-royalties-ownership-eula-and-tos-589400?lang=en).

## Existing scene and reusable systems

The live scene is `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`. It contains nine always-loaded 256 × 256 m native terrain tiles, a 768 × 768 m footprint, copied Booter/Legger/input/camera/HUD systems, seven authored camp rocks, twelve Ironstone nodes and a resource cache. It is not an enabled build scene. Several tiles share TerrainData, and scene content shares production assets: editing a terrain, prefab or shared profile here can still affect production. Use new lab prefabs/profiles and additive station objects rather than changing shared content.

The [scene creation receipt](../../Evidence/Gameplay/CAMP_LOOP_TESTING_SCENE_2026-10-13.md) describes the existing scene; the [live scene snapshot](../../Evidence/Gameplay/AnimationPlayground20261013/live-scene.json) and [live reference snapshot](../../Evidence/Gameplay/AnimationPlayground20261013/live-references.json) prove this audit's editor observations. No saved Animator is present in Edit Mode because `TopDown3DPlayerAnimationDriver` instantiates its visual in `Awake`; this is expected.

| Existing owner | Reuse and required extension |
| --- | --- |
| `TopDown3DPlayerMotor` | Camera-relative Rigidbody movement, slopes, ground probes, sidesteps/vaults and owner-scoped action constraint. Add explicit posture/airborne/ladder/slide/cover modes with collision checks. Existing steep-terrain climbing is distinct from ladder climbing. |
| `TopDown3DPlayerAnimationDriver` | Sole Playables pose owner, root motion and IK currently off, fixed 21 slots including locomotion, traversal, gathering and four camp actions. Add catalog-driven action playback under this owner or a reviewed scene-only variant. Avoid two systems writing the same Animator. |
| `TopDown3DLocomotionClipProfile` | Twelve required roles include directional loops, starts, stops and pivots. Create a lab profile and measure cycle speed/foot contacts; preserve missing transitions through verified retargeting or acquire them. |
| `TopDown3DInputRouter` and Input System | Gamepad-first movement, camera, tap interaction/cargo hold, UI/modal handoff and saved rebinding. Extend through a lab input instance and the established router contracts. |
| Interaction/action controllers | Existing stable target identity, range/availability checks, approach, cancellation and gather completion. Existing interface is resource/reward-oriented; introduce a non-reward action target adapter for seats, buttons and traversal without breaking Ironstone consumption. |
| Survival vitals | Existing health authority and health-reduced notification. Add a lab damage request carrying direction/severity and a reaction/death policy; avoid a second health value drifting from vitals. |
| Camping | Existing setup, sleep/rest, wake, interruption and persistence lifecycle. Sleeping Idle supplies a pose loop, not lying down, wake-up or another camp system. |
| Camera/HUD/settings | Reuse the rig and device-specific prompts. Add selected action, phase, target, held prop, motion owner, availability reason and reset controls; optional close preview belongs to the camera/UI owners. |
| Checkpoint writer | Existing CampLoopTesting namespace is separate from GreaterWasteland. New lab stations default to session-only state and never write production checkpoints. |

The evidence records current driver references to `UnityStandardHumanoid/defaultmale_rig.fbx`, `TopDown3DLocomotionClipProfile.asset`, existing strafe/vault clips and `Booter_GatherIronstone.anim`. No swap was made. Pinned packages remain Input System 1.19.0, URP 17.4.0, Cinemachine 3.1.5 and Timeline 1.8.11. The initial playground needs no new package. Evaluate contact IK only as a later bounded feature, respecting the current disabled-foot-IK baseline.

## Proposed systems and input behavior

Use a single action coordinator above the motor, animation driver, target and prop systems. Each accepted action progresses through availability → approach/alignment → enter → active/contact → exit/recovery → completion. It reserves the motor's existing owner constraint, releases it only for that owner, and centralizes interruption rules. The coordinator must arbitrate resource gathering, camping, canister placement, inventory, damage and lab actions rather than layering independent busy flags.

Implement action effects transactionally: mining consumes/rewards once at the approved contact; pickup transfers ownership once; throw releases one prop once; button press changes one target once. Reserve required targets/props before enter and revalidate at the effect marker. Early cancellation rolls back reservations; cancellation after an effect preserves that effect and performs recovery. Do not spawn a new object or award a reward whenever a loop wraps.

Movement/physics extensions need distinct decisions:

- Crouch smoothly changes capsule height/centre while keeping feet grounded; a ceiling probe can deny standing. Crouch travel blends forward/back/side directions without forcing the character to face the travel vector.
- Jump begins only from an eligible grounded state, applies a physical impulse and enters airborne state. Select standing/running takeoff by velocity. Ground contact selects ordinary or roll landing; hard landings can request damage. Never double-apply the clip's vertical arc and motor gravity.
- Slide requires adequate speed and surface clearance, controls friction/deceleration and reduced posture, and checks standing clearance at exit. Collision shortens motion safely.
- Cover records the wall plane, tangent, height, bounds and valid end points. Wall probing determines when to stop or exit. The existing crouched cover loops fit a low-cover station; standing wall-hug remains a gap.
- Ladder reserves a climb axis and entry/exit anchors, suppresses ordinary gravity only while owned, drives rung travel against collision, handles top clearance and restores the motor on abort. Descending needs its own reviewed clip/path; do not quietly play ascent backwards as the shipped descent.
- Throwing needs a held-object socket, release position and velocity, ignored self-collision at release, collision restoration, maximum live props and reset. Punches/kicks need short hit windows, one hit per target per strike and no invisible projectile or weapon.
- Damage/death/get-up need explicit priority and state. Death stops action effects and input, holds the terminal pose and permits lab reset. Get-up requires floor space; arbitrary death poses do not automatically match the two get-up clips.

Proposed controls are **lab bindings**, not approved changes to the shipped controls. The existing gameplay West/E tap means interact, while its hold opens Legger cargo. Sprint uses right shoulder/Shift; Jump already has South/Space bindings but no observed movement consumer. Attack exists as a dormant keyboard/mouse action; no gamepad attack binding was found in this audit.

| Lab intent | Proposed interaction |
| --- | --- |
| Move, camera, sprint | Preserve current controls; choose walk/run through analog magnitude and existing speed rules. |
| Jump | Activate existing South/Space only in the lab after implementing airborne states. |
| Crouch / slide | Keyboard C is a candidate; expose a gamepad action through the lab palette until a free coactive button/modifier is verified. Sprint plus crouch requests slide when eligible. |
| Use ladder, seat, button, pickup, push, bedroll | Contextual West/E with the existing tap/hold resolver and target prompts. |
| Punch, kick, throw, dodge, reaction/death demonstration | Select a variant in a controller-navigable action palette, then execute; right trigger is a candidate attack binding after coactive-map checks. |
| Preview, pause/seek a clip, change variant, reset station/all | Separate playground UI map; gameplay cannot receive the same confirmation press. Preview selection alone causes no gameplay effect. |

Do not repurpose East (canister pickup), right-stick press (danger ping), D-pad (algae/deploy) or inventory controls casually. Verify existing binding overrides, menus, cargo timing and focus-loss/neutral-return behavior. A lab input clone must not overwrite `controls-v1.json` or export its extra actions as production preferences. Exact new gamepad bindings remain an implementation design decision after conflict analysis.

## Stations and object inventory

Place stations on local reusable pads after sampling terrain normals and clearance; do not flatten or repaint shared TerrainData. Preserve camp/Legger access and the existing loop route. A proposed walkable hub has numbered station signs, a return marker, an action selector and Reset All. Dimensions derive from the calibrated character and clips, not assumed source units.

| Station | Objects to build or reuse | Systems and contact requirements |
| --- | --- | --- |
| Character/locomotion | One-metre gauge, marked lane, turn circles, slight ramp, contact markers | Avatar/scale check, speed/cycle match, start/stop/pivot/strafe comparison, slip and facing diagnostics |
| Crouch/duck/slide | Adjustable low beam, tall/low tunnel, slide lane and blocked exit | Capsule transitions, overhead probe, speed gate, recovery, obstacle collision |
| Jump/fall/get-up | Low obstacles, stepped ledges, landing pad, safe fall platform and floor clearance grid | Real jump/fall/land states, roll threshold, damage demonstration, back/stomach recovery |
| Ladder | Ladder prefab, rung anchors, bottom trigger, top platform/guard, blocked-exit variant | Entry alignment, rung/cycle calibration, climb motion ownership, safe abort and exit; descent gap visible |
| Cover/wall/crowd | Low solid barrier, long wall/end caps, corner, narrow corridor and static dummy figures | Wall plane/tangent constraints, cover entry/idle/sneak/exit; corridor tests walking, not crowd AI |
| Mining | Existing Ironstone reference target plus resettable lab node, blunt mining-tool stand-in, hand socket, impact marker and particles/audio placeholder | Adapted swing, grip, hit timing, repeatable node state and exactly-once reward. The original axe clip stays restricted until adapted. |
| Pickup/examine/throw | Scrap object, low table, small crate, grip sockets, throw target and catch/reset volume | Object reservation, parenting, examine orientation, release trajectory and self-collision rules |
| Punch/kick/hurt/death | Training dummy or padded target, hit zones, directional damage emitters, impact markers | Strike windows, target ledger, directional reactions, vitals/death policy and reset; no weapons |
| Push | Heavy crate with handles, clear lane, stop marker and obstacle | Start/loop/stop, crate/player motion constraint, obstruction and cancellation |
| Chair/ground rest/sleep | Chair with seat/feet anchors, ground mat, side-sleep bedroll and wake clearance | Enter/idle/exit posture, attachment/contact calibration, missing transitions clearly identified; reuse camp lifecycle where applicable |
| Button | Switch panel, press anchor, indicator and reset | Hand contact, one activation, busy/availability prompt |
| Mounting research | Optional static saddle/seat fixture only after a suitable clip is acquired | Approach, mount/dismount, seated alignment and collision; horse/motorcycle gameplay is not established by this audit |

Use primitive prototypes initially. Replace visuals after proportions/contact pass, keeping station IDs and prefab interfaces stable. Mounting and crowd fixtures are lab research proposals, not new world canon. Legger remains the existing physical carrier/follower, not an implicitly rideable vehicle.

## World identity, lifecycle and reset contract

| Concern required by the working agreement | Playground treatment |
| --- | --- |
| Deterministic world identity | Reuse CampLoopTesting's authored-world/checkpoint identity. No generated-world or terrain-generation service. Optional randomized variants must use a recorded lab seed; fixed station IDs do not depend on hierarchy order. |
| Chunk streaming / unload and reload | Terrain streaming is not applicable: all nine authored tiles are resident. Scene unload, disable, restart and target removal still release constraints, props, listeners and Playables safely. |
| Stable generated-object identity | Stations are authored prefab instances with stable IDs. Spawned throw props are session-scoped with an explicit allocator/reset ledger; persistent generated-object identity is not required until persistence is separately designed. |
| Authored constraints | Keep terrain, camp, rocks, node placements and access routes intact. Use station pads/anchors, collision layers and clearance volumes; duplicate shared assets before lab-only modification. |
| Persisted runtime deltas | Lab damage, switches, held/thrown props, pose and station resets default to memory only. Do not write these into GreaterWasteland or silently extend CampLoopTesting inventory/camp saves. A future persistent lab slice requires its own version and migration decision. |

Reset Station and Reset All must end the current action, restore motor/capsule/gravity, clear hit ledgers and reservations, return props/crates/switches to authored transforms, restore lab vitals and the safe return pose, and clean temporary effects. Reset must not duplicate inventory rewards or save over a production checkpoint. Verify blocked stand-up, destroyed targets, exhausted nodes, prop loss, pause, inventory/camp interruption, scene unload and input held across reset. Preview playback uses its own speed control and must not accelerate survival time accidentally.

## Ordered implementation and acceptance gates

| Batch | Deliverable | Required proof before advancing |
| --- | --- | --- |
| 0: Asset handoff | Canonical import manifest, character Avatar/material/prefab, accepted/excluded clip catalog and provenance/storage decision | Hashes, paired metadata/GUIDs, Avatar mapping, representative deformation and contact review, units/facing, per-clip root/loop policy; Unity import/compile |
| 1: Preview and reset | Scene-scoped lab player/profile, controller-friendly catalog viewer, phase diagnostics and reset | One pose/movement owner; all candidates selectable with restrictions visible; one-shots/loops/end holds correct; reset and modal ownership structurally tested |
| 2: Locomotion and posture | Calibrated new idle/walk/run/sprint plus transitions, strafing, crouch, jump/fall/landing | Focused profile/motor/input tests, capsule/ceiling and ground-state tests, no doubled travel; hands-on feel/foot-contact review by the author |
| 3: Ordinary interactions | Pickup/examine, button, push, chair/ground/sleep stations | Contact markers, transactional effects, cancel-before/after-effect, ownership cleanup, camp and existing gather compatibility |
| 4: Constrained traversal | Slide, cover and ladder stations | Collision/exit-clearance and action-owner tests, rung/wall alignment; unsupported descent/standing-wall variants remain visibly unavailable |
| 5: Physical actions | Throw, punch/kick, hit/death/get-up; adapted mining | Release/hit/reward exactly once, directional damage, reset, target loss, interruptions and no production persistence spillover |
| 6: Gap closure | Mount/dismount, crowd/standing-wall motion and missing transitions if retained | Suitable licensed/custom clips and contact/system design; separate decision before production use |

Keep production promotion separate from the lab. Do not enable Camp Loop Testing in Build Settings or change GreaterWasteland just to run the playground. Runtime and editor code stay in their established assembly boundaries. Focused EditMode tests should cover behavior contracts, not mirror implementation. Use current import/compile and bounded non-mutating validators for future asset/scene batches; do not start batchmode against the already-open project. Gameplay smoke tests and visual/feel acceptance remain author-owned unless explicitly delegated.

This audit performed file/hash/FBX inspections and background read-only editor queries. It did **not** run Play Mode, gameplay smoke tests, imports, scene builders, Unity tests or builds. Prior scene validation receipts are context, not fresh proof for this proposal.

## Complete per-file inventory

The following table is generated from the checked FBXs. Duration is `(last keyframe - first keyframe) / 30`; it is not a proposed gameplay action duration. Category and eligibility are audit recommendations.

| Folder | File | Category | Duration (s) | Status |
| --- | --- | --- | ---: | --- |
| MixamoLocomotion20261013 | Breathing Idle.fbx | Locomotion | 9.93 | unarmed-candidate |
| MixamoLocomotion20261013 | Character T-Pose.fbx | Character | 0.03 | base-character |
| MixamoLocomotion20261013 | Sprint - forward travel alternative.fbx | Locomotion | 0.53 | reference-forward-travel |
| MixamoLocomotion20261013 | Standard Run.fbx | Locomotion | 0.73 | unarmed-candidate |
| MixamoLocomotion20261013 | Standard Walk.fbx | Locomotion | 1.17 | unarmed-candidate |
| MixamoLocomotion20261013 | Two Cycle Sprint.fbx | Locomotion | 1.07 | unarmed-candidate |
| MixamoOriginalActions20261013 | Crouch - enter.fbx | Crouch | 4.07 | unarmed-candidate |
| MixamoOriginalActions20261013 | Crouch - exit.fbx | Crouch | 2.57 | unarmed-candidate |
| MixamoOriginalActions20261013 | Crouch - idle.fbx | Crouch | 1.37 | unarmed-candidate |
| MixamoOriginalActions20261013 | Crouch walk - backward.fbx | Crouch | 1.23 | unarmed-candidate |
| MixamoOriginalActions20261013 | Crouch walk - forward.fbx | Crouch | 1.13 | unarmed-candidate |
| MixamoOriginalActions20261013 | Crouch walk - left.fbx | Crouch | 1.17 | unarmed-candidate |
| MixamoOriginalActions20261013 | Crouch walk - right.fbx | Crouch | 1.27 | unarmed-candidate |
| MixamoOriginalActions20261013 | Death - falling backward.fbx | Death | 2.60 | unarmed-candidate |
| MixamoOriginalActions20261013 | Death - falling forward.fbx | Death | 2.57 | unarmed-candidate |
| MixamoOriginalActions20261013 | Death - standing backward.fbx | Death | 3.63 | unarmed-candidate |
| MixamoOriginalActions20261013 | Fall - airborne idle.fbx | Fall | 0.70 | unarmed-candidate |
| MixamoOriginalActions20261013 | Fall - landing.fbx | Fall | 1.07 | unarmed-candidate |
| MixamoOriginalActions20261013 | Fall - roll landing.fbx | Fall | 2.13 | unarmed-candidate |
| MixamoOriginalActions20261013 | Hit - body.fbx | Hurt | 1.60 | unarmed-candidate |
| MixamoOriginalActions20261013 | Hit large - back.fbx | Hurt | 1.67 | unarmed-candidate |
| MixamoOriginalActions20261013 | Hit large - front.fbx | Hurt | 1.37 | unarmed-candidate |
| MixamoOriginalActions20261013 | Hit large - left.fbx | Hurt | 1.57 | unarmed-candidate |
| MixamoOriginalActions20261013 | Hit large - right.fbx | Hurt | 1.63 | unarmed-candidate |
| MixamoOriginalActions20261013 | Hit small - back.fbx | Hurt | 1.27 | unarmed-candidate |
| MixamoOriginalActions20261013 | Hit small - front.fbx | Hurt | 1.20 | unarmed-candidate |
| MixamoOriginalActions20261013 | Hit small - left.fbx | Hurt | 1.20 | unarmed-candidate |
| MixamoOriginalActions20261013 | Hit small - right.fbx | Hurt | 1.00 | unarmed-candidate |
| MixamoOriginalActions20261013 | Jump - running.fbx | Jump | 0.90 | unarmed-candidate |
| MixamoOriginalActions20261013 | Jump - standing.fbx | Jump | 2.37 | unarmed-candidate |
| MixamoOriginalActions20261013 | Jump - unarmed.fbx | Jump | 2.33 | unarmed-candidate |
| MixamoOriginalActions20261013 | Kick - front snap.fbx | Kick | 1.60 | unarmed-candidate |
| MixamoOriginalActions20261013 | Kick - MMA low.fbx | Kick | 1.50 | unarmed-candidate |
| MixamoOriginalActions20261013 | Kick - side.fbx | Kick | 2.00 | unarmed-candidate |
| MixamoOriginalActions20261013 | Kick - thrust.fbx | Kick | 2.17 | unarmed-candidate |
| MixamoOriginalActions20261013 | Ladder - climb.fbx | Climb | 0.77 | unarmed-candidate |
| MixamoOriginalActions20261013 | Ladder - one step.fbx | Climb | 1.57 | unarmed-candidate |
| MixamoOriginalActions20261013 | Ladder - start.fbx | Climb | 2.03 | unarmed-candidate |
| MixamoOriginalActions20261013 | Ladder - top exit.fbx | Climb | 4.00 | unarmed-candidate |
| MixamoOriginalActions20261013 | Ladder - two steps.fbx | Climb | 2.97 | unarmed-candidate |
| MixamoOriginalActions20261013 | Mining reference - downward tool swing.fbx | Mining | 2.27 | reference-adaptation-required |
| MixamoOriginalActions20261013 | Punch - cross.fbx | Punch | 2.00 | unarmed-candidate |
| MixamoOriginalActions20261013 | Punch - hook.fbx | Punch | 2.17 | unarmed-candidate |
| MixamoOriginalActions20261013 | Punch - jab.fbx | Punch | 1.03 | unarmed-candidate |
| MixamoOriginalActions20261013 | Reference only - crouched death weapon pose.fbx | Death | 2.37 | reference-excluded-weapon-pose |
| MixamoOriginalActions20261013 | Running slide.fbx | Slide | 1.53 | unarmed-candidate |
| MixamoOriginalActions20261013 | Strafe run - left.fbx | Strafe | 0.67 | unarmed-candidate |
| MixamoOriginalActions20261013 | Strafe run - right.fbx | Strafe | 0.67 | unarmed-candidate |
| MixamoOriginalActions20261013 | Strafe walk - left.fbx | Strafe | 1.47 | unarmed-candidate |
| MixamoOriginalActions20261013 | Strafe walk - right.fbx | Strafe | 1.47 | unarmed-candidate |
| MixamoOriginalActions20261013 | Throw - alternate.fbx | Throw | 5.57 | unarmed-candidate |
| MixamoOriginalActions20261013 | Throw - pickup and throw.fbx | Throw | 4.87 | unarmed-candidate |
| MixamoOriginalActions20261013 | Throw - standard.fbx | Throw | 2.20 | unarmed-candidate |
| MixamoRest20261013 | Sleeping Idle - Sleeping Deeply.fbx | Sleep | 6.87 | unarmed-candidate |
| MixamoUnarmedCover20261013 | Cover Idle.fbx | Cover | 2.57 | unarmed-candidate |
| MixamoUnarmedCover20261013 | Left Cover Sneak.fbx | Cover | 1.13 | unarmed-candidate |
| MixamoUnarmedCover20261013 | Right Cover Sneak.fbx | Cover | 1.13 | unarmed-candidate |
| MixamoUnarmedCover20261013 | Stand To Cover.fbx | Cover | 1.27 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Button press.fbx | Button | 3.23 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Dodge - backward.fbx | Evade | 2.67 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Dodge - right.fbx | Evade | 1.43 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Duck from standing.fbx | Evade | 1.53 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Get up - back.fbx | Recovery | 8.33 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Get up - stomach.fbx | Recovery | 8.60 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Mining reference - downward tool swing.fbx | Mining | 2.27 | duplicate mining reference |
| MixamoUnarmedInteractions20261013 | Pickup - one hand.fbx | Pickup | 3.43 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Pickup and examine.fbx | Pickup | 7.20 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Push - cycle.fbx | Push | 2.67 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Push - start.fbx | Push | 4.73 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Push - stop.fbx | Push | 2.60 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Sit down - chair.fbx | Sit | 4.43 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Sitting idle - chair.fbx | Sit | 2.97 | unarmed-candidate |
| MixamoUnarmedInteractions20261013 | Sitting idle - ground.fbx | Sit | 10.87 | unarmed-candidate |
