# Experimental forward gait author review

October 14, 2026. Continues the [focused plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md) after the [uphill controller checkpoint](../ForwardUphillGait/README.md). The review is installed in the author's saved CampLoopTesting scene. This is the plan's limited early author review; the full locomotion goal remains active.

## Playtest handoff

Enter Play Mode, open **F9**, and choose **Reset to gait deck**. This uses AP02's existing registered safe-return anchor through the ordered lab reset. Ground/standing clearance is required, arbitrary destinations are rejected, and the selected return cannot leak into later ordinary resets. Existing reset terminals retain their behavior.

On AP02's flat deck, use normal movement to run; default keyboard controls are WASD, **Left Alt** to walk and **Left Shift** to sprint. Existing rebindings remain authoritative. Try each gait steadily on the marked 20-metre lane, then speed changes and repeated starts/stops. Inspect rhythm, sliding, abrupt poses and responsiveness with equipment. Feedback has been requested; acceptance is pending.

The experimental loops only run on AP02's static, nearly level support, inside its footprint. The earlier locomotion remains the baseline elsewhere and other presentation owners keep priority. Leaving the review boundary is a fallback, not a qualified native-ground transition. The F9 palette and AP02 sign label the mode experimental. Both qualification flags remain false.

## Implementation and verification

Four separate idle/walk/run/sprint inputs form a nested mixer inside the existing graph's single output. They use the measured original sources, time maps, completed-displacement clock, bounded grounding and stop/restart envelope. The original twelve-role binding remains unchanged. Preparing another owner restores baseline weights; disabled/destroyed review owners cannot accidentally enable the unqualified family globally. Required private review data has an explicit missing-dependency failure.

The real-deck test found AP02 lacked the ground marker required by the motor. Its prefab now has one marker component; existing geometry, collider data, object IDs and metadata are preserved. The authoring factory retains this correction. No other station was expanded.

All [282 playground](tests-playground.json), [18 legacy locomotion](tests-legacy.json) and [six slope](tests-slopes.json) Edit Mode checks pass without skips: **306 total**. Twenty new review cases cover the explicit route and normal validation gate, actual authored deck, mixed poses, cold skinned/rigid equipment across one/two/four influences, support/owner boundaries, missing dependencies and completed-motion dispatch. The integration fixture separately preserves baseline and qualified test routes, adds review action/reset regression, and exercises the actual F9 command and label.

The [source-bound presentation sample](qualification.json) records one output and 98.14–99.14 microseconds per warm frame, with zero recurring managed bytes. It includes prepare, the nested clip mix, one zero-time graph evaluation and LateUpdate. It excludes physics simulation, moving-world contact effects, mesh baking, UI and Player/GPU performance. Full mesh baking is only offline test evidence.

The [source manifest](source-manifest.json) pairs final task-owned bytes and metadata with the isolated candidate. Its refreshed dependency snapshot verified 2,547 files and copied 21 changes; the 27 external GUID entries match the prior snapshot. Subsequent task-owned changes are separately paired. Concurrent rendering work remains outside this batch.

## Installed state and remaining gates

The [single-record patch](scene-patch.json) changes only two fields in the existing session. [Verification](scene-verification.json) preserves all 1,355 scene record identities, every other record and scene/prefab metadata. An earlier candidate whole-scene save rewrote unrelated serialization; that rejected trial remains private, and its immediate changes were removed from the isolated copy. The author installer uses the measured record patch, clean-scene/hash guards and a verified loaded runtime; it never saves the whole author scene.

The [author state](author-state.json) confirms the clean saved scene, loaded shortcut/readiness code, nine terrains, original CalibratedLocomotion baseline and false qualification flags. The [installation receipt](author-install.json) records the exact change. No agent entered Play Mode, ran a gameplay smoke test or activated a window.

The [private receipt](private-receipt.json) seals 61 source/evidence files locally. [Private verification](private-verification.json) preserves all 127 prior asset/metadata pairs and ten forward draft files in both checkouts. Restore those sealed dependencies together; no licensed motion payload or raw pose track was published.

Native slopes/support changes, approximately 34–35mm cumulative flat contact travel, steering/moving action handoffs and final author acceptance remain open. The failed [terrain pose qualification](../ForwardTerrainReach/README.md) remains disabled. Fixed authored world identities and always-loaded terrain are unchanged; this transient review adds no generation, streaming, generated IDs or persisted gait state. Rollback is the scoped scene record, AP02 marker and source delta with paired metadata, never an older whole scene.
