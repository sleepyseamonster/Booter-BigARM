# Camp Loop animation prerequisites

Date: 2026-10-13. Revised after the [plan audit](./CAMP_LOOP_ANIMATION_PLAN_AUDIT_2026-10-13.md). Build these small contracts before furnishing the full playground. They belong to the existing TopDown3D runtime, Editor and test assemblies; no package, terrain-generation or save-format migration is required.

## Existing foundations to preserve

Reuse `TopDown3DPlayerMotor`, `TopDown3DPlayerAnimationDriver`, the input router and current menu/modal ownership, interaction/gathering, inventory/cargo, survival vitals and camp. Root production is GreaterWasteland; the lab is CampLoopTesting. Existing motor ownership stays authoritative and legacy behavior remains the default without lab configuration.

## Prerequisite slices

| Stage | Source boundary and planned work | Exit gate |
| --- | --- | --- |
| DEP1 | `Tools/Art/Animations/Restore-MixamoAssets.ps1`; new private asset recipe/receipt; scoped future ignore rules and folder scaffolding | Dry run accounts for 148 source aliases/74 payloads, 72 Unity sources and two retained versions. Restore restricted FBXs and their paired metadata together. Public clone health has no orphan metadata; hydration fails explicitly on mismatched bytes/GUIDs. |
| FND1 | New scene session configuration plus existing `GreaterWastelandInventorySaveService`, menu configuration and UI installer | Resolve scene/asset/session context before checkpoint restore or writer creation. World writes disabled only for the configured lab; controls use a separate injected directory. Missing private binding data makes new lab actions unavailable. Null configuration preserves production. |
| FND2 | Existing interaction/action controller and cargo/camp callers; planned `TopDown3DInteractionTarget` descriptor, station target view and activity projection | Discover legacy and new targets through one query, deduplicate colliders, capture target/epoch at press and revalidate on release. Dispatch by action kind. No fake gather reward. Busy rules include pending cargo, camp and lab activity; one motor lease. |
| FND3 | Input router partials, `TopDown3DControlPreferences`, menu Controls panel and menu-owned lab palette | Bind all new commands through the router, disarm/rearm on modal/focus/device transitions, validate effective coactive bindings including fixed new actions, isolate preference writes. Palette shares the existing UI owner/module; selection does not trigger an action on closing. |
| FND4 | Planned action-state/clock/marker/transaction helpers, motor capability branch, presentation snapshot and reset participant contract | Pure clock/commit tests and primitive motor tests pass before source clips are required. One physical writer; posture-aware probes; explicit gravity/support and automatic traversal arbitration; one contact/effect stream; repeatable reset and cleanup. |
| ART1 | Parameterized Editor character/clip qualification fixture, original/Mixamo rig candidates and neutral material | Source and target Avatar roles, physical scale, pose, material, weights and grip quality recorded. The fixture evaluates the calibrated visual, never edits the loaded user scene or canonical profile. |

These stages are ordered where they share ownership; ART1 can qualify a minimal private rig after DEP1/FND1 while other foundation rules use the existing player or synthetic clips in disposable fixtures. Full contact/profile qualification and bulk scene integration follow the master sequence. This breaks the old dependency cycle between asset qualification and tools that did not yet exist.

## Scene bootstrap and private dependencies

Use explicit references and a readiness state: Uninitialized → DependenciesChecked → ServicesBound → VisualReady → PlayableReady, or Failed with a precise reason. The source catalog is a recipe; a private binding set contains resolved imported clips/model references. Public visual host prefabs contain no missing nested model prefab. The scene can report an absent binding set and keep new lab actions inert; it cannot silently substitute the old character and claim the new one works.

Resolve session policy before any save service Start restores or creates a writer. Lab mode neither loads nor writes the existing checkpoint, including manual save, disable and quit. Controls alone use `Application.persistentDataPath/AnimationPlayground/controls-v1.json`, through explicit menu configuration; do not redirect all settings/radial preferences by accident. Compatible production overrides may be read into the owned runtime input instance, but validation must detect collisions with new lab bindings and never overwrite the original file.

On partial initialization failure or domain reload, unsubscribe and release only acquired resources. Existing UI installers remain idempotent. Do not rely on arbitrary Awake order, repeated global scans or enabling a component before its binding set is ready. The scene marker is valid only in CampLoopTesting; it is rejected in production.

## Target and activity contracts

A target descriptor contains stable authored ID, Unity object/reference, registration/reset epoch, action kind, reachable point/range, collider ownership and an availability result. Legacy adapters retain current behavior; station views supply non-resource action data. Query all through the same selector with current hysteresis/line-of-sight rules. Dedicated canister pickup and cargo hold remain separate commands.

The activity projection reads legacy gather/placement/pending-cargo state, camp phase, current motor lease and lab phase. It is not another lock registry. Cargo, inventory, canister, algae, camp and lab commands consume the same availability decision. Damage/cancel asks the current owner to exit; another action never steals its lease. Every effect revalidates the captured target and prop identity before commit.

## Clocks and transactions

Physical actions use one scaled simulation clock with explicit pause policy. Presentation reads its time; it does not also advance the same clip independently. Queue input intent during the input update, apply motor movement and effect commits at the owning FixedUpdate boundary, then present the resulting snapshot. Preview uses a separate no-effect clock/seek path and never changes world time.

Use action instance/epoch and marker/cycle identifiers. Tests cover skipped markers, multi-cycle time jumps, loop quotas, cancellation immediately before/after commit, backwards preview seek, reset, destroyed targets and pause/resume. A committed throw/reward/button action survives cancellation; an uncommitted reservation rolls back. No effects from a previous epoch remain queued after reset. Footstep/contact events come from the active presentation branch; inactive legacy gait cannot emit dust while another pose owns the actor.

## Motor, posture and reset

Advance owned modes inside the current motor's constrained branch; an external component writing velocity after it is not an integration solution. Publish mode, posture, support, actual displacement/velocity, facing and action phase to presentation. Keep existing snapshot callers compatible. Probe geometry follows the current capsule, with feet anchored during height changes. Takeoff has support-release hysteresis; landing requires actual contact. Slide/crouch/ladder/cover/death suppress conflicting automatic vault/steep-climb entry.

Store lease-owned original gravity/collider state and restore it on safe exit, cancel, disable and reset. Death needs low corpse collision and clearance-tested recovery; airborne fatal exits continue gravity and safe descent. Initial damage controls are enabled only for postures with qualified reactions/recovery, while the health authority and teardown remain correct for every posture.

Register reset participants explicitly with authored snapshots and stable IDs. Reset current action/leases/queued effects first, then held/spawned props, station state, corpse/posture/vitals and safe actor return; finish by clearing input edges. Reset only registered lab state and never teleport Legger or delete unrelated scene bodies. Failure to find a clear return pose reports a blocked reset and stays safe, rather than placing inside terrain/walls.

## Foundation proof

Planned focused fixtures: dependency restoration/metadata pairing; session save and preference isolation; target capture/dispatch/dense collider selection; activity gating; router/coactive bindings and modal release; marker clock/transaction epochs; owned motor modes/capsule/support; contact-source routing; reset and bootstrap failure. Regress existing interaction, controls/menu/input, locomotion/traversal, camp/checkpoint and scene fixtures for the touched seams. Tests using scenes run in an approved isolated hydrated candidate; pure rules and primitive fixtures need no new Mixamo assets. Hands-on feel/visual acceptance remains author-owned. No foundation changes are implemented by this audit.
