# Slower uphill gait controller checkpoint

October 14, 2026. Implements the author's selected slower gait on very steep uphill ground under the [focused locomotion plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md). The goal remains active. This checkpoint changes the existing Camp Loop motor; it does not select the unqualified forward animations or terrain pose stage.

## Behavior

The lab speed configuration enables a continuous uphill policy. Uphill grade follows the actual surface-tangent travel direction: the speed cap blends from the requested speed at 32 degrees to 1.4m/s at 40 degrees and above. Flat-ground targets remain 1.4/2.7/3.8m/s. Partial input scales the result, and the slow-walk modifier retains precedence. Level and downhill travel retain their requested speed; the remaining sidehill/downhill pose failures are not accepted by this policy.

One additional nonallocating forward ray anticipates rising ground. It runs parallel to the current support, ignores triggers and moving bodies, and requires the existing authored ground marker. Its distance uses the configured sprint-to-walk braking distance, capsule radius and fixed-step margin, capped at two metres. It does not retreat as velocity falls. The existing motor acceleration/deceleration handles braking and release; there is no velocity snap or invented input delay. Sprint-active state drops when the reduced request reaches run speed or below. Presentation cadence continues to read completed displacement rather than the lower target velocity.

The lab ground query now uses the Rigidbody centre instead of an interpolated render Transform, so support and completed movement refer to the same sample. Production/null configuration retains its previous query and speed behavior. Releasing lab posture ownership disables the policy; existing crouch, action and mounted owners keep their priority.

## Verification and limits

The exact [source manifest](source-manifest.json) matches the marked isolated candidate and preserves paired metadata. All [261 playground](tests-playground.json), [18 legacy locomotion](tests-legacy.json) and [six slope](tests-slopes.json) Edit Mode checks pass without skips: 285 total. The 31-case locomotion repair fixture includes 18 new cases for the slope curve, boundary continuity, directional grade, partial input/walk precedence, anticipation, braking distance, release, ownership, dynamic/trigger rejection and steady 48-degree speed along the surface. These are explicit query/velocity-planning fixtures; actor positions remain fixed during planning and physics is never simulated.

The [source-bound diagnostic](qualification.json) measures 8,192 warm stationary planning calls at 10.69 microseconds each and zero recurring managed bytes. It includes the existing motor queries, optional anticipation ray and observer, but duplicate Edit Mode timestamps do not advance movement/contact history. It is neither Player performance nor an end-to-end moving-world measurement.

The [local archive](private-receipt.json) seals 16 source/test/measurement files. [Private verification](private-verification.json) confirms all 127 prior payload/metadata pairs and ten draft files remain identical in both checkouts. No private binding, source FBX, scene, terrain, package or production setting changed. The preceding [terrain reach checkpoint](../ForwardTerrainReach/README.md) remains failed qualification; reducing the uphill operating speed does not erase its stress cases, entry transients, equipment or motion failures.

Continue with the plan's bounded experimental flat-deck author-review candidate, then steering/action handoffs, native support changes and remaining terrain/contact qualification. Author appearance/feel acceptance remains required. No Play Mode, gameplay smoke test or foreground activation occurred. The policy is transient actor behavior on fixed, always-loaded authored ground; it adds no world generation, streaming, generated identities or persisted runtime deltas. Rollback is the scoped motor/test delta with existing metadata, not an older whole scene.
