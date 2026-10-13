# Camp Loop animation asset import plan

Date: 2026-10-13. Part one of the [implementation plan](./CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md). Bring all downloaded sources into the local Unity project with stable identity, reproducible import settings and preserved originals. The [planned manifest](./AnimationPlayground20261013/asset-import-plan.json) is the exact copy map; no copy or import has been executed by this planning task.

## Canonical files and storage

The five Downloads folders contain 73 FBX copies. Copy 72 unique FBXs to canonical paths: one skin-bearing character, 68 unarmed motion candidates and three restricted motion references. Both mining copies map to one destination. The standalone `male_basemesh_mixamo_rigged.fbx` is another alias of the Character T-Pose bytes; it does not create a second Avatar. Preserve every download and earlier Blender/FBX source.

Use PascalCase names without spaces. Character: `Assets/_Project/Art/Characters/AnimationPlayground/MaleBaseMeshMixamo.fbx`. Motions: `Assets/_Project/Art/Animations/Mixamo/`, grouped into Locomotion, Traversal, Combat and Interactions. References have their own `Reference/` family folders. Proposed settings live under `Assets/_Project/Settings/AnimationPlayground/`; character/station prefabs under `Assets/_Project/Prefabs/AnimationPlayground/`.

The current GitHub repository is public, as verified through its [repository metadata endpoint](https://api.github.com/repos/sleepyseamonster/Booter-BigARM). Adobe permits game use, while its Content Files terms restrict stand-alone distribution. The storage plan treats raw and derived Mixamo clips as private project dependencies. [Mixamo FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html), [Adobe terms section 3.6](https://www.adobe.com/legal/terms.html#use-of-services-and-software).

During implementation, add narrow repository ignore rules **before** importing: this new character FBX; all FBX payloads under the new Mixamo folder; any derived `.anim`, `.blend`, source archive or `.unitypackage` under its private payload boundary. Do not ignore `.meta`, catalog/profile `.asset`, prefabs, code or transfer manifests. No blanket FBX/LFS rule or existing-source untracking is needed. Keep all Blender sources local according to the existing [source policy](../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

Stage and commit metadata, references, restoration tools and project-authored stations normally. Never embed animation keyframes in a public prefab, scene, ScriptableObject, generated C# or unrestricted archive to bypass the payload boundary. A license receipt could later clear the character separately; it does not clear Mixamo motions. Making GitHub private or selecting a cloud sharing service would be a separate account decision, not this plan's default.

Create a hash-verified private working set outside public Git storage, proposed at `D:\BooterBigArmPrivateAssets\Mixamo\2026-10-13\`. Preserve Downloads on C: as a second original source location. The manifest and a restoration helper let this machine or an authorized project teammate hydrate the exact Assets paths from a supplied private directory. This plan authorizes no external upload or sharing action. A fresh public clone can continue using production; the playground explicitly reports missing private dependencies until hydrated rather than pretending its animations are present.

## Transfer and metadata procedure

1. Refresh all source hashes/sizes against the audited inventory, inspect destination existence and free capacity, and record the current editor/scene and task-owned asset baseline. Stop if source bytes changed or a proposed path is owned by unrelated work.
2. Validate the private-payload ignore boundary and verify no restricted payload is already tracked. Generate the planned transfer list before any copy. The helper takes explicit source root and dry-run/apply options; a dry run does not call AssetDatabase refresh or write files.
3. Copy rather than remove the original. For a missing destination, use a temporary sibling then verify hash before finalizing the destination. If an existing file has the expected hash, reuse it. If the hash differs, fail and report both versions; never overwrite it silently.
4. Import the base first through the existing editor connection. Let Unity generate its `.meta` once, validate the Avatar and record the actual GUID/subasset identity. Import dependent motion files only after that source Avatar is valid.
5. On the first successful handoff record every destination hash, `.meta` hash/GUID, Source Avatar reference, clip local file ID, import policy and source aliases in `Docs/Evidence/Gameplay/AnimationPlayground20261013/asset-transfer-receipt.json`. Include newly created folder metadata and authored prefab/profile/material reference units in the receipt. Future hydration restores bytes beside the already tracked metadata; it never regenerates the GUIDs.
6. Validate all new GUIDs for uniqueness and each catalog/profile/scene reference against its intended file and subasset. Do not deduplicate any existing Unity asset by byte hash.
7. Audit staged paths and the full unpublished range before publication. Raw/private payloads remain unstaged even if someone used force-add. An ignored file can still be tracked; check tracked paths, not only ignore output. A public push is a separately authorized operation.

Future tooling belongs in `Assets/_Project/Scripts/Editor/AnimationPlayground/AnimationPlaygroundAssetImporter.cs` and `AnimationPlaygroundAssetValidator.cs`, in `BooterBigArm.Editor`; the private hydration helper belongs in `Tools/Art/Animations/Restore-MixamoAssets.ps1`. These names describe planned files. The restoration helper must restore files and use existing metadata without scraping Mixamo or assuming a search title regenerates identical bytes.

## Character acceptance gate

Import as Humanoid / Create From This Model. Check the required and optional bone mapping, T-pose, weights and axes; a valid automatic mapping is only the first gate. Animation-only FBXs have matching hierarchies and should use Humanoid / Copy From Other Avatar with this base. [Unity 6.4 Humanoid import](https://docs.unity3d.com/6000.4/Documentation/Manual/ConfiguringtheAvatar.html).

Measure against a one-metre gauge and the existing Booter collider. Fix importer/visual scale intentionally, then record physical height, visual offset, foot plane, head/hand positions and capsule fit. Do not apply the existing 1.4 visual scale or infer a new Scale Factor from Blender's 20–23 unit dimensions. Test idle, crouch, ladder, chair, sleep, throw and death on the actual imported skin.

The base has approximately 63,134 triangles and no material slots. Author a neutral URP material and a visual prefab; confirm renderer bounds and material assignment. One player is enough for the first lab; use simple static crowd dummies instead of many copies of this mesh. Add reduction/LOD only if measured cost requires it.

The accepted 33-bone rig has index-finger chains only. Treat hand grip as a required visible check. If the lab needs fuller fingers, prepare a compatible fuller rig or a verified Humanoid retarget handoff first, preserve the original character/Avatar as a separate version, and update the mapping deliberately. Do not add fake finger bones and call it solved.

## Per clip import and runtime metadata

Preserve authored source keyframes in the imported FBX. Disable Unity animation compression for the initial measurement/import baseline; exported keyframe reduction being off does not guarantee Unity's default compression is off. A separate clip definition records reviewed trim, looping, root-transform settings, playback rate, markers, contact anchors, motion owner and readiness. Metadata values begin unreviewed; a guessed release or hit time cannot enable an effect.

| Family | Import and integration rule |
| --- | --- |
| Idle, walk/run/sprint, crouch/strafe travel, cover sneak, ladder cycle, falling idle, push cycle, sitting/sleep idle | Loop only after seam/pose review. Preserve facing and measured foot/hand phases. Locomotion displacement belongs to the Rigidbody; constrained travel belongs to the action motor. |
| Enter/exit, throw, jump/land/roll, strikes, reactions, get-up, button, pickup and push start/stop | One-shot; choose reviewed active range, contact/release windows and completion pose. Separate takeoff/airborne/landing roles if a source contains a full jump. |
| Death | One-shot with terminal pose held until reset or an explicitly compatible recovery. Do not loop or auto-return to idle. |
| Mining reference | Imported for adaptation and measurement; unavailable as mining until an unarmed/tool-use result passes grip/impact/return checks. |
| Forward-travel sprint reference | Keep as a source comparison; preferred sprint is Two Cycle Sprint. Normalize or consume its travel before any active use. |
| Crouched death weapon pose | Keep source/accounting metadata; exclude clip playback from the unarmed playground catalog and active build references. |

The existing driver disables `applyRootMotion`. Keep that default. Inspect Root Transform Rotation, Position Y and XZ for each family. **Bake Into Pose changes where motion is stored; it does not automatically erase retained travel.** Compare sampled actor/visual motion and either discard extracted root motion under the motor, normalize a derived clip, or consume it through a collision-tested action path. Avoid double vertical motion on jumps and rotating the visual incorrectly on strafes. [Unity root motion](https://docs.unity3d.com/6000.4/Documentation/Manual/RootMotion.html).

The current profile and phase mapper assume two ordered contacts per foot. Measure Standard Walk/Run and the chosen directional loops. If a source supplies only one contact per foot, Blender prepares a separate two-cycle animation-only FBX under `Mixamo/Derived/`, preserving hierarchy and recording the source hash and repeat recipe. Validate its seam and measure actual contacts; never fabricate a second contact in the profile. Keep the current profile/phase contract unchanged for this slice. Two Cycle Sprint already provides a candidate two-cycle source. Existing licensed start/stop/pivot/directional profile roles may be retargeted and recalibrated in the new lab profile; all twelve roles must pass before player locomotion cutover.

Each catalog entry carries a stable ID, source file/hash, imported clip reference, readiness, action family/variant, loop and trim, blend times, required posture/target/prop, motion policy, timing markers, cancel/recovery policy and reviewed contact data. Metadata for each restricted reference is visible, but a missing or excluded source never silently maps to another action. Preview can evaluate an accepted body clip before physical markers pass; physical execution remains unavailable until its own gates pass.

## Acceptance and rollback

Pass only when the manifest accounts for all 73 folder copies with 72 unique canonical destinations, the separate character alias matches, every imported clip uses the intended Avatar, GUID/subasset references resolve, and the private working set restores a fresh authorized validation checkout with identical bytes and metadata. Verify metadata stability on reimport. Run import/compile and focused importer/catalog tests; hands-on deformation and contacts are author review.

On failure, retain source and destination versions, report the affected family, and prevent its catalog entry from enabling. Restore only task-owned metadata/settings from the sealed baseline. Downloads, existing `.meta` files, private backups and unrelated assets remain preserved.

## Exact proposed source map

The following map is generated from the audited hashes. `Aliases` is the number of folder copies represented by a canonical file; the character's separate handoff alias is additionally recorded in JSON. All destinations are planned and all physical actions start disabled until validation.

| Source file | Destination below Assets/_Project/Art | Aliases | Readiness |
| --- | --- | ---: | --- |
| Death - falling backward.fbx | Animations/Mixamo/Combat/Death/DeathFallingBackward.fbx | 1 | candidate |
| Death - falling forward.fbx | Animations/Mixamo/Combat/Death/DeathFallingForward.fbx | 1 | candidate |
| Death - standing backward.fbx | Animations/Mixamo/Combat/Death/DeathStandingBackward.fbx | 1 | candidate |
| Hit - body.fbx | Animations/Mixamo/Combat/Hurt/HitBody.fbx | 1 | candidate |
| Hit large - back.fbx | Animations/Mixamo/Combat/Hurt/HitLargeBack.fbx | 1 | candidate |
| Hit large - front.fbx | Animations/Mixamo/Combat/Hurt/HitLargeFront.fbx | 1 | candidate |
| Hit large - left.fbx | Animations/Mixamo/Combat/Hurt/HitLargeLeft.fbx | 1 | candidate |
| Hit large - right.fbx | Animations/Mixamo/Combat/Hurt/HitLargeRight.fbx | 1 | candidate |
| Hit small - back.fbx | Animations/Mixamo/Combat/Hurt/HitSmallBack.fbx | 1 | candidate |
| Hit small - front.fbx | Animations/Mixamo/Combat/Hurt/HitSmallFront.fbx | 1 | candidate |
| Hit small - left.fbx | Animations/Mixamo/Combat/Hurt/HitSmallLeft.fbx | 1 | candidate |
| Hit small - right.fbx | Animations/Mixamo/Combat/Hurt/HitSmallRight.fbx | 1 | candidate |
| Kick - front snap.fbx | Animations/Mixamo/Combat/Kick/KickFrontSnap.fbx | 1 | candidate |
| Kick - MMA low.fbx | Animations/Mixamo/Combat/Kick/KickMmaLow.fbx | 1 | candidate |
| Kick - side.fbx | Animations/Mixamo/Combat/Kick/KickSide.fbx | 1 | candidate |
| Kick - thrust.fbx | Animations/Mixamo/Combat/Kick/KickThrust.fbx | 1 | candidate |
| Punch - cross.fbx | Animations/Mixamo/Combat/Punch/PunchCross.fbx | 1 | candidate |
| Punch - hook.fbx | Animations/Mixamo/Combat/Punch/PunchHook.fbx | 1 | candidate |
| Punch - jab.fbx | Animations/Mixamo/Combat/Punch/PunchJab.fbx | 1 | candidate |
| Button press.fbx | Animations/Mixamo/Interactions/Button/ButtonPress.fbx | 1 | candidate |
| Pickup and examine.fbx | Animations/Mixamo/Interactions/Pickup/PickupAndExamine.fbx | 1 | candidate |
| Pickup - one hand.fbx | Animations/Mixamo/Interactions/Pickup/PickupOneHand.fbx | 1 | candidate |
| Push - cycle.fbx | Animations/Mixamo/Interactions/Push/PushCycle.fbx | 1 | candidate |
| Push - start.fbx | Animations/Mixamo/Interactions/Push/PushStart.fbx | 1 | candidate |
| Push - stop.fbx | Animations/Mixamo/Interactions/Push/PushStop.fbx | 1 | candidate |
| Sit down - chair.fbx | Animations/Mixamo/Interactions/Sit/SitDownChair.fbx | 1 | candidate |
| Sitting idle - chair.fbx | Animations/Mixamo/Interactions/Sit/SittingIdleChair.fbx | 1 | candidate |
| Sitting idle - ground.fbx | Animations/Mixamo/Interactions/Sit/SittingIdleGround.fbx | 1 | candidate |
| Sleeping Idle - Sleeping Deeply.fbx | Animations/Mixamo/Interactions/Sleep/SleepingIdleSleepingDeeply.fbx | 1 | candidate |
| Throw - alternate.fbx | Animations/Mixamo/Interactions/Throw/ThrowAlternate.fbx | 1 | candidate |
| Throw - pickup and throw.fbx | Animations/Mixamo/Interactions/Throw/ThrowPickupAndThrow.fbx | 1 | candidate |
| Throw - standard.fbx | Animations/Mixamo/Interactions/Throw/ThrowStandard.fbx | 1 | candidate |
| Breathing Idle.fbx | Animations/Mixamo/Locomotion/BreathingIdle.fbx | 1 | candidate |
| Crouch - enter.fbx | Animations/Mixamo/Locomotion/Crouch/CrouchEnter.fbx | 1 | candidate |
| Crouch - exit.fbx | Animations/Mixamo/Locomotion/Crouch/CrouchExit.fbx | 1 | candidate |
| Crouch - idle.fbx | Animations/Mixamo/Locomotion/Crouch/CrouchIdle.fbx | 1 | candidate |
| Crouch walk - backward.fbx | Animations/Mixamo/Locomotion/Crouch/CrouchWalkBackward.fbx | 1 | candidate |
| Crouch walk - forward.fbx | Animations/Mixamo/Locomotion/Crouch/CrouchWalkForward.fbx | 1 | candidate |
| Crouch walk - left.fbx | Animations/Mixamo/Locomotion/Crouch/CrouchWalkLeft.fbx | 1 | candidate |
| Crouch walk - right.fbx | Animations/Mixamo/Locomotion/Crouch/CrouchWalkRight.fbx | 1 | candidate |
| Standard Run.fbx | Animations/Mixamo/Locomotion/StandardRun.fbx | 1 | candidate |
| Standard Walk.fbx | Animations/Mixamo/Locomotion/StandardWalk.fbx | 1 | candidate |
| Strafe run - left.fbx | Animations/Mixamo/Locomotion/Strafe/StrafeRunLeft.fbx | 1 | candidate |
| Strafe run - right.fbx | Animations/Mixamo/Locomotion/Strafe/StrafeRunRight.fbx | 1 | candidate |
| Strafe walk - left.fbx | Animations/Mixamo/Locomotion/Strafe/StrafeWalkLeft.fbx | 1 | candidate |
| Strafe walk - right.fbx | Animations/Mixamo/Locomotion/Strafe/StrafeWalkRight.fbx | 1 | candidate |
| Two Cycle Sprint.fbx | Animations/Mixamo/Locomotion/TwoCycleSprint.fbx | 1 | candidate |
| Reference only - crouched death weapon pose.fbx | Animations/Mixamo/Reference/Death/ReferenceOnlyCrouchedDeathWeaponPose.fbx | 1 | reference |
| Sprint - forward travel alternative.fbx | Animations/Mixamo/Reference/Locomotion/SprintForwardTravelAlternative.fbx | 1 | reference |
| Mining reference - downward tool swing.fbx | Animations/Mixamo/Reference/Mining/MiningReferenceDownwardToolSwing.fbx | 2 | reference |
| Cover Idle.fbx | Animations/Mixamo/Traversal/Cover/CoverIdle.fbx | 1 | candidate |
| Left Cover Sneak.fbx | Animations/Mixamo/Traversal/Cover/LeftCoverSneak.fbx | 1 | candidate |
| Right Cover Sneak.fbx | Animations/Mixamo/Traversal/Cover/RightCoverSneak.fbx | 1 | candidate |
| Stand To Cover.fbx | Animations/Mixamo/Traversal/Cover/StandToCover.fbx | 1 | candidate |
| Dodge - backward.fbx | Animations/Mixamo/Traversal/Evade/DodgeBackward.fbx | 1 | candidate |
| Dodge - right.fbx | Animations/Mixamo/Traversal/Evade/DodgeRight.fbx | 1 | candidate |
| Duck from standing.fbx | Animations/Mixamo/Traversal/Evade/DuckFromStanding.fbx | 1 | candidate |
| Fall - airborne idle.fbx | Animations/Mixamo/Traversal/Fall/FallAirborneIdle.fbx | 1 | candidate |
| Fall - landing.fbx | Animations/Mixamo/Traversal/Fall/FallLanding.fbx | 1 | candidate |
| Fall - roll landing.fbx | Animations/Mixamo/Traversal/Fall/FallRollLanding.fbx | 1 | candidate |
| Jump - running.fbx | Animations/Mixamo/Traversal/Jump/JumpRunning.fbx | 1 | candidate |
| Jump - standing.fbx | Animations/Mixamo/Traversal/Jump/JumpStanding.fbx | 1 | candidate |
| Jump - unarmed.fbx | Animations/Mixamo/Traversal/Jump/JumpUnarmed.fbx | 1 | candidate |
| Ladder - climb.fbx | Animations/Mixamo/Traversal/Ladder/LadderClimb.fbx | 1 | candidate |
| Ladder - one step.fbx | Animations/Mixamo/Traversal/Ladder/LadderOneStep.fbx | 1 | candidate |
| Ladder - start.fbx | Animations/Mixamo/Traversal/Ladder/LadderStart.fbx | 1 | candidate |
| Ladder - top exit.fbx | Animations/Mixamo/Traversal/Ladder/LadderTopExit.fbx | 1 | candidate |
| Ladder - two steps.fbx | Animations/Mixamo/Traversal/Ladder/LadderTwoSteps.fbx | 1 | candidate |
| Get up - back.fbx | Animations/Mixamo/Traversal/Recovery/GetUpBack.fbx | 1 | candidate |
| Get up - stomach.fbx | Animations/Mixamo/Traversal/Recovery/GetUpStomach.fbx | 1 | candidate |
| Running slide.fbx | Animations/Mixamo/Traversal/Slide/RunningSlide.fbx | 1 | candidate |
| Character T-Pose.fbx | Characters/AnimationPlayground/MaleBaseMeshMixamo.fbx | 1 | character |
