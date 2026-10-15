# Expanded gamepad gait review

October 15, 2026. Continues the [locomotion implementation plan](CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md). The author's latest feedback accepts jogging and running, retains the previous sprint as **fast run**, and requests a distinct, faster full sprint. Gamepad is the primary control: stick deflection traverses ordinary gaits; full deflection reaches fast run; the existing right bumper sprint action adds full sprint. Keyboard stage selection is deferred.

## Audit and selected comparison

The previous top gait derives from MCU **Run**, with six amplified upper-body channels. It is a full run loop, not a truncated sprint. Keep that accepted motion and its 5m/s calibration as fast run.

All inspected sprint imports retain their complete baked takes. TwoCycleSprint contains 32 frames/1.0667 seconds; the alternative Mixamo sprint contains 16 frames/0.5333 seconds. The Unity Standard sprint contains its full baked frames 1–17/0.5333 seconds. Short duration alone does not imply missing motion.

TwoCycleSprint has greater leg reach and forward lean, but its retargeted contact windows fail the fixed-sole screen on Booter. The alternative requires about 95mm of grounding lift; TwoCycleSprint originally measured 112mm, and its phase-shifted in-place derivative reaches approximately 137mm. Neither is selected. A more dramatic pose does not justify silently widening the established +80/−30mm visual envelope.

The selected review uses the complete Unity Standard sprint. Its hand travel relative to the hips is approximately 0.914m versus 0.743m in the accepted fast run. It needs approximately 10mm maximum lift. The full 0.5333-second source is phase shifted and repeated twice to match the existing four-contact semantic loop; horizontal root translation is removed because the existing motor owns travel. Original source clips, import settings, metadata and vertical/limb motion remain intact.

| Stage | Review speed | Pose source |
| --- | ---: | --- |
| Idle | 0m/s | Existing idle |
| Walk | 1.4m/s | Accepted MCU walk |
| Jog | 3.0m/s | Accepted MCU jog |
| Run | 4.0m/s | Complete MCU run |
| Fast run | 5.0m/s | Accepted SprintArmDriveReview |
| Sprint | 6.0m/s | Complete Unity Standard sprint derivative |

These are speed anchors for continuous blending, not six digital stick zones. The existing processed analog magnitude is retained without another deadzone. The walk/jog blend still ends at 2.7m/s, preserving the accepted pure-jog interval through 3.0m/s. Ordinary keyboard movement and its existing slow-walk/sprint actions retain their current speeds.

## Implementation and validation gates

1. Preserve the three-gait policy and original animation payloads. Build a separate private schema-2 profile with five moving sources plus idle. Use fixed six-channel pose weights and the same displacement-driven gait clock, one graph output, foot projection and ownership rules. Do not add a second controller or Animator output.
2. Select the new review through the private binding, preserving the unsaved scene. Explicit fixture/authoring selections remain authoritative. Missing required private review data fails visibly. The normal qualified-binding route remains closed to this unqualified review.
3. Add owner-scoped gamepad speed caps to the existing motor. Track the device supplying movement, so mouse look cannot change a held stick's pace. RB uses the existing Sprint binding. Menu entry, reset, device removal and owner release clear the corresponding state. Preserve slow-walk/crouch/action precedence and uphill limiting.
4. Calibrate the new sprint against fixed foot-vertex identities. Its native contact speed is approximately 4.8899m/s, covering 5.2159m per four-contact loop. At 6m/s, a contact-centre-biased map keeps pure source rates approximately 1.1355–1.3432. A stronger exact-stance warp would exceed the 1.35 pure-rate ceiling and was rejected. Native-window excursion is approximately 28.60mm; accumulated drift remains a separate qualification gate.
5. Verify all stages and blends through the actual graph with equipped Booter, including cloak and rigid carried kit, at one/two/four bone skin qualities. Check continuous weights, dense contributor rates, physical-distance inversion, contact count, stop/resume and action capture, explicit AP02 routing, RB/analog input and keyboard fallback. Do not enter Play Mode or perform agent-driven gameplay smoke testing.
6. Seal local recovery payloads and scalar/hash evidence, import into the already open author project in Edit Mode, and verify loaded six-channel dependencies without saving/reloading the scene. Hands-on feel and final creative acceptance remain with the author.

## Scope and remaining gaps

This is the bounded AP02 flat-deck review. Both forward and terrain qualification flags remain false. Existing terrain, steering, cumulative contact drift, low-analog support and full action-transition qualification are not closed by this comparison. The baseline remains outside the deck. A production-wide cutover requires those gates and a separate authorized handoff.

The fixed authored world, actor identity, chunk/unload behavior and saved-world formats are unaffected. No procedural content, streaming service, generated-object identity or persisted runtime delta is introduced. Lab reset remains session-only. No capsule resize, package change, scene reconstruction or source publication is required.

Current proof and local recovery are recorded in the [expanded gait receipt](../../Evidence/Gameplay/LocomotionImplementation20261014/ExpandedGaitReview20261015/README.md).
