# Camp Loop animation asset import plan

Date: 2026-10-13. Revision 2 of the [master plan](./CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md), corrected by the [reaudit](./CAMP_LOOP_ANIMATION_PLAN_AUDIT_2026-10-13.md). The [schema 2 map](./AnimationPlayground20261013/asset-import-plan.json) is the exact planned transfer. No asset import or source export occurs in this audit.

## Complete collection and destinations

Account for 73 dated-folder copies, 73 loose motion exports and two standalone character aliases: 148 copies and 74 distinct byte payloads. The original folder inventory covered 72 unique FBXs. Two newly accounted payloads are earlier sprint attempts whose sampled poses match the retained forward-travel sprint. Preserve them as source versions, not duplicate gameplay entries.

Keep the existing 72 planned Unity destinations: one Mixamo source character, 68 unarmed motion candidates and three restricted references. Character source: `Assets/_Project/Art/Characters/AnimationPlayground/MaleBaseMeshMixamo.fbx`. Motion sources: `Assets/_Project/Art/Animations/Mixamo/` grouped by family, with restricted sources in Reference. Preserve two export versions under local `SourceArt/Animation/Mixamo/Reference/`. Original 74/102-bone rigs and the mesh-only input remain separately recorded source alternatives; they do not inflate the motion count.

Use PascalCase filenames. Preserve all Downloads and old Blender/FBX sources. Matching byte aliases map to one asset; byte-distinct source versions are retained. Do not deduplicate existing Unity assets by file hash or overwrite a populated destination.

## Private payload and metadata pairing

The current repository is public. Adobe's Content Files terms restrict stand-alone distribution, so raw/derived Mixamo payloads remain private dependencies. [Adobe terms section 3.6](https://www.adobe.com/legal/terms.html?internal_browser=true).

The previous plan's public `.fbx.meta` approach is superseded: both repo health checks reject those files when their FBXs are absent. **Keep each restricted FBX/derived clip and its paired metadata together in a private bundle**, including importer settings, GUID and clip subasset IDs. Track public folder `.meta` plus `.gitkeep` scaffolding, transfer/recipe/GUID receipts, code and project-authored geometry/materials. Both the local hydrated workspace and a public clone must pass ordinary asset/metadata pairing checks; do not weaken those checks globally.

Future scoped ignore rules cover the new private payload files and their paired `.meta`, private binding assets and source versions; they do not hide folder metadata, public recipe/configuration assets or unrelated art. Existing Blender/local source rules remain unchanged. The proposed private working set is `D:\BooterBigArmPrivateAssets\Mixamo\2026-10-13\`; Downloads on C: preserves originals. No external upload, repository visibility change, purchase or LFS change is assumed.

Public catalogs/profiles are recipes with stable source IDs and calibrated parameters. A **private binding set** resolves imported clips, source/target Avatar/model and the actual locomotion profile. The public visual host is project-authored and does not contain a missing nested FBX prefab. Missing private binding data disables new lab actions with an explicit dependency result. Do not put bindings under Resources or another automatic production inclusion path. Hydrated candidates qualify the lab; unhydrated public-core validation must remain coherent and must report the lab as unavailable rather than silently substitute assets.

## DEP1 transfer and restoration contract

1. Verify all source aliases/hashes/sizes against the expanded receipt. Check destination ownership, free capacity, editor lock and task baseline before copying.
2. Stage sources in the private working set before bulk Unity import. Validate dry-run paths and ignore boundaries; never let raw/private files enter staged or unpublished public history through force-add or an archive.
3. Copy with temporary-file/hash verification. Reuse an equal destination; fail on a differing file and retain both source versions. Do not remove Downloads.
4. For first import, generate each new `.meta` once. Capture actual GUID, metadata hash, imported clip IDs, importer state, recipe/source hash and rig mapping in a transfer receipt. Capture folder and authored asset reference units too.
5. For restoration, copy **payload and sealed metadata before importing/opening its references**. Never replace an existing different GUID or regenerate the metadata to make hydration appear successful. Import source Avatar before animation dependents through the existing background-safe editor.
6. Validate GUID uniqueness, subasset references and private binding recipe version. Keep failed families disabled and preserve recoverable bytes/settings.
7. Validate a public-core clone and an authorized hydrated candidate separately. Public health has no orphan metadata; hydrated reimport preserves source GUIDs and bindings. Publication inspects the full unpublished range, not just the final tree.

Planned tooling: `Tools/Art/Animations/Restore-MixamoAssets.ps1`, `Assets/_Project/Scripts/Editor/AnimationPlayground/AnimationPlaygroundAssetImporter.cs` and `AnimationPlaygroundAssetValidator.cs`. Keep import/mutation, dry run and validation separate. No scraping/downloading is required by restoration.

## ART1 source and target rig qualification

The accepted Mixamo source has 33 bones and only index-finger chains. The original `male_basemesh_rigged.fbx` has 74 bones with 30 finger bones; its 102-bone re-export also has full finger-chain names. Mixamo rejecting those uploads does not prove Unity will reject them. Qualify the 74-bone original first as a fuller target candidate, with the 102-bone version as another source to inspect. Material slots are empty; author a neutral material.

Create and validate the Mixamo **source Avatar**. Matching 33-bone animation files use Copy From Other Avatar with that source. A different target rig uses its **own** mapped Humanoid Avatar and retargets qualified source clips through Humanoid animation; do not copy a 33-bone definition onto a 74/102-bone hierarchy. [Unity Humanoid import](https://docs.unity3d.com/6000.4/Documentation/Manual/ConfiguringtheAvatar.html).

Choose and record the target rig/quality tier before station offsets are authored. Validate T-pose, axes, units, weights, shoulder/knee/hand/foot deformation, renderer bounds, material, feet plane and capsule against a one-metre gauge. The measured character profile determines scale/offset, not Blender's raw dimensions or the previous 1.4 visual scale. Fuller bones alone do not create finger motion missing from the Mixamo sources: qualify authored grip poses/hand presentation in the same pose owner. A limited-rig prototype can support body preview, but its grip limitation remains an explicit family gate.

The 72 canonical collection destinations do not include additional target-rig experiments. The map records two optional private qualification destinations under `Art/Characters/AnimationPlayground/Qualification/`: `MaleBaseMeshOriginalRig.fbx` and `MaleBaseMeshBlenderRig.fbx`. Import only the candidates being tested, with their paired metadata and receipt; this adds at most two qualification FBXs, not motion candidates. The Mixamo target option reuses the canonical source model. Record the selected target binding and source/target mapping before bulk qualification; preserve rejected experiment bytes privately.

Use a parameterized preview fixture with the **actual calibrated visual**, accepting model/profile/clip explicitly. The current profiler hardcodes canonical paths and its measurement helper is private; extend it or add a scoped fixture before using it as lab proof. Never modify canonical profile contacts through its menu command accidentally.

## ART2 clip, root and gait policy

Keep raw source bytes intact and Unity animation compression Off for baseline measurement. Record trim, loop intent, source/target Avatar roles, root/facing policy, playback bounds, contact/marker windows and readiness per role. Names and three sampled poses do not establish continuous loop/contact quality.

The existing mapper requires **R0 → L0 → R1 → L1 cyclic order**, as well as two ordered contacts per foot. Measure the calibrated target, normalize phase origin and prepare a separate two-cycle/phase-rotated derivative when needed. Record source hashes and exact recipe; retain original curves/metadata and verify seams/stride/cadence after import. Never manufacture a contact to fill a profile. Reuse and recalibrate the existing eight transition/directional roles where compatible, alongside the four new preferred loops. All twelve legacy profile roles must pass before cutover.

Motor/action paths own physical displacement. Root motion stays disabled by default. Per-clip import extracts/discards source trajectory or a reviewed derivative normalizes it; Bake Into Pose alone does not erase travel. Test actor and visual drift across full clips, including strafes and airborne phases. Do not feed Animator delta motion and independent Rigidbody motion simultaneously. [Unity root-transform controls](https://docs.unity3d.com/6000.4/Documentation/Manual/RootMotion.html).

Loops require seam/phase qualification. One-shots require enter/contact/release/exit ranges; jump clips may supply separate takeoff/airborne/landing roles. Death holds a reviewed terminal pose. Mining remains an adaptation source; forward sprint remains a comparison source; the weapon-style crouched death is excluded from unarmed playback. Preserve its source privately without binding it into the active catalog.

Readiness is SourceRecorded → ImportQualified → PreviewQualified → PhysicalQualified → AuthorAccepted. Recipes contain IDs, prerequisites, contact/motion ownership and missing-role reasons; private bindings contain real clip/model references. Physical effects require approved marker values and complete recovery sequences. New/derived assets are appended with provenance; they do not reuse a different clip's GUID.

## Planned source map

Aliases include dated folders and loose downloads; the character additionally has two standalone copies. Existing canonical paths/IDs remain stable. The two retained versions follow the main map and have no Unity metadata destination. All operations remain planned.

| Source file | Planned destination | Copies | Kind |
| --- | --- | ---: | --- |
| Death - falling backward.fbx | Art/Animations/Mixamo/Combat/Death/DeathFallingBackward.fbx | 2 | candidate |
| Death - falling forward.fbx | Art/Animations/Mixamo/Combat/Death/DeathFallingForward.fbx | 2 | candidate |
| Death - standing backward.fbx | Art/Animations/Mixamo/Combat/Death/DeathStandingBackward.fbx | 2 | candidate |
| Hit - body.fbx | Art/Animations/Mixamo/Combat/Hurt/HitBody.fbx | 2 | candidate |
| Hit large - back.fbx | Art/Animations/Mixamo/Combat/Hurt/HitLargeBack.fbx | 2 | candidate |
| Hit large - front.fbx | Art/Animations/Mixamo/Combat/Hurt/HitLargeFront.fbx | 2 | candidate |
| Hit large - left.fbx | Art/Animations/Mixamo/Combat/Hurt/HitLargeLeft.fbx | 2 | candidate |
| Hit large - right.fbx | Art/Animations/Mixamo/Combat/Hurt/HitLargeRight.fbx | 2 | candidate |
| Hit small - back.fbx | Art/Animations/Mixamo/Combat/Hurt/HitSmallBack.fbx | 2 | candidate |
| Hit small - front.fbx | Art/Animations/Mixamo/Combat/Hurt/HitSmallFront.fbx | 2 | candidate |
| Hit small - left.fbx | Art/Animations/Mixamo/Combat/Hurt/HitSmallLeft.fbx | 2 | candidate |
| Hit small - right.fbx | Art/Animations/Mixamo/Combat/Hurt/HitSmallRight.fbx | 2 | candidate |
| Kick - front snap.fbx | Art/Animations/Mixamo/Combat/Kick/KickFrontSnap.fbx | 2 | candidate |
| Kick - MMA low.fbx | Art/Animations/Mixamo/Combat/Kick/KickMmaLow.fbx | 2 | candidate |
| Kick - side.fbx | Art/Animations/Mixamo/Combat/Kick/KickSide.fbx | 2 | candidate |
| Kick - thrust.fbx | Art/Animations/Mixamo/Combat/Kick/KickThrust.fbx | 2 | candidate |
| Punch - cross.fbx | Art/Animations/Mixamo/Combat/Punch/PunchCross.fbx | 2 | candidate |
| Punch - hook.fbx | Art/Animations/Mixamo/Combat/Punch/PunchHook.fbx | 2 | candidate |
| Punch - jab.fbx | Art/Animations/Mixamo/Combat/Punch/PunchJab.fbx | 2 | candidate |
| Button press.fbx | Art/Animations/Mixamo/Interactions/Button/ButtonPress.fbx | 2 | candidate |
| Pickup and examine.fbx | Art/Animations/Mixamo/Interactions/Pickup/PickupAndExamine.fbx | 2 | candidate |
| Pickup - one hand.fbx | Art/Animations/Mixamo/Interactions/Pickup/PickupOneHand.fbx | 2 | candidate |
| Push - cycle.fbx | Art/Animations/Mixamo/Interactions/Push/PushCycle.fbx | 2 | candidate |
| Push - start.fbx | Art/Animations/Mixamo/Interactions/Push/PushStart.fbx | 2 | candidate |
| Push - stop.fbx | Art/Animations/Mixamo/Interactions/Push/PushStop.fbx | 2 | candidate |
| Sit down - chair.fbx | Art/Animations/Mixamo/Interactions/Sit/SitDownChair.fbx | 2 | candidate |
| Sitting idle - chair.fbx | Art/Animations/Mixamo/Interactions/Sit/SittingIdleChair.fbx | 2 | candidate |
| Sitting idle - ground.fbx | Art/Animations/Mixamo/Interactions/Sit/SittingIdleGround.fbx | 2 | candidate |
| Sleeping Idle - Sleeping Deeply.fbx | Art/Animations/Mixamo/Interactions/Sleep/SleepingIdleSleepingDeeply.fbx | 2 | candidate |
| Throw - alternate.fbx | Art/Animations/Mixamo/Interactions/Throw/ThrowAlternate.fbx | 2 | candidate |
| Throw - pickup and throw.fbx | Art/Animations/Mixamo/Interactions/Throw/ThrowPickupAndThrow.fbx | 2 | candidate |
| Throw - standard.fbx | Art/Animations/Mixamo/Interactions/Throw/ThrowStandard.fbx | 2 | candidate |
| Breathing Idle.fbx | Art/Animations/Mixamo/Locomotion/BreathingIdle.fbx | 2 | candidate |
| Crouch - enter.fbx | Art/Animations/Mixamo/Locomotion/Crouch/CrouchEnter.fbx | 2 | candidate |
| Crouch - exit.fbx | Art/Animations/Mixamo/Locomotion/Crouch/CrouchExit.fbx | 2 | candidate |
| Crouch - idle.fbx | Art/Animations/Mixamo/Locomotion/Crouch/CrouchIdle.fbx | 2 | candidate |
| Crouch walk - backward.fbx | Art/Animations/Mixamo/Locomotion/Crouch/CrouchWalkBackward.fbx | 2 | candidate |
| Crouch walk - forward.fbx | Art/Animations/Mixamo/Locomotion/Crouch/CrouchWalkForward.fbx | 2 | candidate |
| Crouch walk - left.fbx | Art/Animations/Mixamo/Locomotion/Crouch/CrouchWalkLeft.fbx | 2 | candidate |
| Crouch walk - right.fbx | Art/Animations/Mixamo/Locomotion/Crouch/CrouchWalkRight.fbx | 2 | candidate |
| Standard Run.fbx | Art/Animations/Mixamo/Locomotion/StandardRun.fbx | 2 | candidate |
| Standard Walk.fbx | Art/Animations/Mixamo/Locomotion/StandardWalk.fbx | 2 | candidate |
| Strafe run - left.fbx | Art/Animations/Mixamo/Locomotion/Strafe/StrafeRunLeft.fbx | 2 | candidate |
| Strafe run - right.fbx | Art/Animations/Mixamo/Locomotion/Strafe/StrafeRunRight.fbx | 2 | candidate |
| Strafe walk - left.fbx | Art/Animations/Mixamo/Locomotion/Strafe/StrafeWalkLeft.fbx | 2 | candidate |
| Strafe walk - right.fbx | Art/Animations/Mixamo/Locomotion/Strafe/StrafeWalkRight.fbx | 2 | candidate |
| Two Cycle Sprint.fbx | Art/Animations/Mixamo/Locomotion/TwoCycleSprint.fbx | 2 | candidate |
| Reference only - crouched death weapon pose.fbx | Art/Animations/Mixamo/Reference/Death/ReferenceOnlyCrouchedDeathWeaponPose.fbx | 2 | reference |
| Sprint - forward travel alternative.fbx | Art/Animations/Mixamo/Reference/Locomotion/SprintForwardTravelAlternative.fbx | 2 | reference |
| Mining reference - downward tool swing.fbx | Art/Animations/Mixamo/Reference/Mining/MiningReferenceDownwardToolSwing.fbx | 3 | reference |
| Cover Idle.fbx | Art/Animations/Mixamo/Traversal/Cover/CoverIdle.fbx | 2 | candidate |
| Left Cover Sneak.fbx | Art/Animations/Mixamo/Traversal/Cover/LeftCoverSneak.fbx | 2 | candidate |
| Right Cover Sneak.fbx | Art/Animations/Mixamo/Traversal/Cover/RightCoverSneak.fbx | 2 | candidate |
| Stand To Cover.fbx | Art/Animations/Mixamo/Traversal/Cover/StandToCover.fbx | 2 | candidate |
| Dodge - backward.fbx | Art/Animations/Mixamo/Traversal/Evade/DodgeBackward.fbx | 2 | candidate |
| Dodge - right.fbx | Art/Animations/Mixamo/Traversal/Evade/DodgeRight.fbx | 2 | candidate |
| Duck from standing.fbx | Art/Animations/Mixamo/Traversal/Evade/DuckFromStanding.fbx | 2 | candidate |
| Fall - airborne idle.fbx | Art/Animations/Mixamo/Traversal/Fall/FallAirborneIdle.fbx | 2 | candidate |
| Fall - landing.fbx | Art/Animations/Mixamo/Traversal/Fall/FallLanding.fbx | 2 | candidate |
| Fall - roll landing.fbx | Art/Animations/Mixamo/Traversal/Fall/FallRollLanding.fbx | 2 | candidate |
| Jump - running.fbx | Art/Animations/Mixamo/Traversal/Jump/JumpRunning.fbx | 2 | candidate |
| Jump - standing.fbx | Art/Animations/Mixamo/Traversal/Jump/JumpStanding.fbx | 2 | candidate |
| Jump - unarmed.fbx | Art/Animations/Mixamo/Traversal/Jump/JumpUnarmed.fbx | 2 | candidate |
| Ladder - climb.fbx | Art/Animations/Mixamo/Traversal/Ladder/LadderClimb.fbx | 2 | candidate |
| Ladder - one step.fbx | Art/Animations/Mixamo/Traversal/Ladder/LadderOneStep.fbx | 2 | candidate |
| Ladder - start.fbx | Art/Animations/Mixamo/Traversal/Ladder/LadderStart.fbx | 2 | candidate |
| Ladder - top exit.fbx | Art/Animations/Mixamo/Traversal/Ladder/LadderTopExit.fbx | 2 | candidate |
| Ladder - two steps.fbx | Art/Animations/Mixamo/Traversal/Ladder/LadderTwoSteps.fbx | 2 | candidate |
| Get up - back.fbx | Art/Animations/Mixamo/Traversal/Recovery/GetUpBack.fbx | 2 | candidate |
| Get up - stomach.fbx | Art/Animations/Mixamo/Traversal/Recovery/GetUpStomach.fbx | 2 | candidate |
| Running slide.fbx | Art/Animations/Mixamo/Traversal/Slide/RunningSlide.fbx | 2 | candidate |
| Character T-Pose.fbx | Art/Characters/AnimationPlayground/MaleBaseMeshMixamo.fbx | 3 | character |
| male_basemesh_mixamo_unrigged@Sprint (1).fbx | SourceArt/Mixamo/Reference/SprintExportAttempt01.fbx | 1 | retained-version |
| male_basemesh_mixamo_unrigged@Sprint.fbx | SourceArt/Mixamo/Reference/SprintExportAttempt02.fbx | 1 | retained-version |
