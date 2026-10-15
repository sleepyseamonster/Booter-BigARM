# Focused locomotion implementation checkpoint

October 14, 2026. Implements the [audited walking/running/sprinting plan](../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md). The goal is active. The latest [startup lookup repair](StartupLookup/README.md) corrects early character/F9 preparation, passes 304 playground Edit Mode checks and is compiled in the author checkout. Fresh live playback remains pending after the author's static-pose/F9 report. The [turn-source checkpoint](TurnSources/README.md) implements a validated signed native-time source asset with 303 preceding checks. Its cold equipped flat-floor screen passes the sampled clearance envelope, while longer contact windows still reject turn installation. The private asset is restored locally in both checkouts and remains unqualified and unbound.

The preceding [steering/source checkpoint](ForwardSteering/README.md) reproduces reversal misalignment, rejects a contact-failing numerical alternative and screens the existing named rapid-turn clips. Its repeated command/pose measurements remain outside live controller code.

The [complete turn-track checkpoint](TurnContactTracks/README.md) repairs private track serialization, verifies all 736,008 recorded sole-point samples and reproduces the previous six-case metrics. Its persistent-contact analysis rejects a single fixed-pivot correction; a qualified contact/trajectory adaptation or replacement source is still required.

The [experimental forward review](ForwardReview/README.md) remains installed in the saved author scene with 306 previously passing relevant Edit Mode checks. F9 → Reset to gait deck opens the bounded AP02 review, preserving the baseline elsewhere and leaving qualification flags false. Author feedback is pending. Native support/contact, steering, moving action handoffs and full acceptance remain open.

The [uphill motor policy](ForwardUphillGait/README.md), failed [terrain reach qualification](ForwardTerrainReach/README.md), earlier [gated terrain prototype](ForwardTerrainRuntime/README.md), [support-plane experiment](ForwardTerrain/README.md), [transition/current-scene checkpoint](ForwardTransitions/README.md) and [forward draft](ForwardDraft/README.md) retain preceding evidence. The foundation/source-selection sections below are historical checkpoints; they do not supersede the installed review or close its remaining gates.

## Implemented foundation

- Added a session-only sustained diagnostic to the existing animation driver. It requires a calibrated lab character, retains all twelve serialized roles and the original profile validator, and gates legacy start/stop/pivot dispatch and directional overlays. Camp, traversal and other owned presentations retain priority. The author scene never enables this diagnostic.
- Added completed grounded-motion observations to the existing Rigidbody motor. One observation is published per fixed tick after motor commands, reading the completed prior Rigidbody position rather than commanded velocity or interpolated Transform. The existing ground queries supply the support; no new physics queries or body writers were added.
- Samples carry tick, epoch, duration, support tangent displacement and availability. Static support is the initial scope. Support/body ownership changes, suspension, reset, invalid/backward time and discontinuities rebase observations. Constraint acquire/release rebases even if the entire action occurs between ticks. Mounted, crouched, falling and externally owned motion cannot count as standing travel.
- Added an inert displacement-driven gait clock. It uses measured metres per full loop, continuous weighted stride distance and unwrapped cycle/contact ordinals. Render interpolation does not advance it. Contact batches preserve every crossing; a separate bounded cosmetic selection can discard old effects. Obstruction and low-speed hysteresis stop sustained progression. Actual pose settling and driver/contact integration still require qualification.

These foundations add no terrain generation, streaming, world identities, persistence fields or generated objects. The physics capsule, rig scale, author scene, original animation bytes and metadata remain unchanged. Production/null configuration retains its current presentation.

## L0 contracts and rollback

The [baseline manifest](baseline-manifest.json) seals the inspected code, private profile/clips, scene and pinned packages. The marked candidate is D:/BooterBigArmValidation/AnimationPlayground20261013. Its isolated gait inputs match the baseline; its saved Camp scene is older than the author scene and cannot establish current scene integration. The [final task manifest](l0-final-source-manifest.json) records identical C# bytes and paired script GUIDs. Three pre-existing candidate script metadata files differ only by a terminal newline; both hashes and this limited equivalence are recorded, rather than claiming byte identity. Main metadata is preserved.

The existing PHY1LowMotionQualified private recovery checkpoint still matches all 127 payload/metadata pairs; see [private verification](private-baseline-check.json). No private asset was changed. The scoped rollback is the task-owned source delta plus paired new script metadata; do not restore an older whole scene or discard unrelated work.

Startup's deterministic Editor fixture now asserts the real session Start path applies 1.4/2.7/3.8m/s and leaves diagnostics disabled. This establishes wiring in the isolated saved fixture, not live Play Mode. A read-only author-editor query timed out; its [failed result](l0-author-readonly-state.json) is retained as a limitation, not evidence of a successful live read. No scene was saved or activated.

The observation/clock contract reserves at most 0.2ms per fixed observation and zero recurring managed bytes. Initial projection plus explicit graph work reserves 0.5ms per presentation sample; that gate remains open. The [offline microbenchmark](foundation-performance.json) measured 100,000 pure observation/clock/render samples at approximately 0.414 microseconds each with zero recurring allocation. It does not measure player/render/physics performance.

## L1 source comparison

Ten native/derived motions were sampled on the accepted rig at approximately 120Hz: 1,592 frames with 674 fixed weighted-foot vertex identities per frame. Original source assets, paired metadata and the target projection are in the [source manifest](source-manifest.json). Corrected four-bone pose/vertex tracks remain local under SoleTracksV2 and in the independent private archive; see the [verified transfer receipt](private-track-receipt.json).

The [corrected four-bone strict proposals](selection-four-bone-v4.json) select the longest nonoverlapping stable-vertex patches per foot, with at most 12mm projected ground gap, 0.35m/s vertical motion and 60ms minimum duration. This is a contact classifier, not accepted plants. It reports maximum anchor excursion, cumulative tangent travel, median/peak drift and exact vertex identity. Geometry height classification includes proposed visual-plane projection; horizontal tracks never switch vertex identity.

| Source | Initial result |
| --- | --- |
| Current WalkTwoCycles | About 4.2cm maximum fitted patch excursion in this classifier; still needs rolling/window review. |
| Current RunTwoCycles | No sufficiently long strict patch found; not a qualifying source under this classifier. |
| Current SprintTwoCycles | No sufficiently long patch found; absence of a classifier window is not proof of no real contact. |
| Retained MCU walk | About 1.49m/s fitted speed and 1.7cm maximum fitted patch excursion; promising. |
| Retained MCU jog | Only one strict left-foot patch; insufficient bilateral coverage. |
| Retained MCU run | Left patches only; about 3.4cm maximum fitted excursion; insufficient coverage. |

A separate [30mm near-ground proposal pass](selection-near-ground-v3.json) exposes possible rolling/support windows without treating elevated feet as contact. This older pilot used renderer overrides while the headless global quality remained OneBone; the declared four-influence metadata was not sufficient. It is retained for diagnosis, superseded by corrected four-bone measurements, and cannot qualify a source. Widening a classifier is not acceptance. The corrected strict pass proposes about 3.06m/s for the retained jog and 4.40m/s for the retained run, with incomplete bilateral coverage. No trio or effective retiming budget is sealed yet. The forward-travel sprint alternative is catalogued as ineligible for unarmed source selection; it was inspected as a reference only.

The [first equipped screen](native-family-screen-v1.json) and [evaluation-order follow-up](native-family-screen-v2.json) exposed a harness quality mismatch: the candidate global quality was OneBone while the cache followed renderer overrides. Removing a second evaluation alone did not solve it. These pilot matrices are diagnostic evidence, not a physical failure or qualification of the native family. The [corrected screen](native-family-screen-v3.json) explicitly matches global quality to Auto renderers and restores it afterward. Its 54 envelopes x 33 phases (1,782 poses) clear the flat floor within 0.0001mm, with prediction error below 0.001mm. Maximum lift is 43mm; sampled skinned body/equipment height is at most 1.792m. Cold and normalized equipment states are included. Geometry sampling remains coarse, plant drift is unqualified, and some cold timing envelopes exceed the provisional 0.5ms budget; dense and steady-state profiling gates remain open.

## Remaining gates

L1 remains open: select/adapt a bilateral compatible source trio, prove source/derived pose preservation, define native contacts and effective rate budgets, and finish equipment screening. The retained family is a candidate, not a silent source replacement. The mesh screen measures the skinned body, cloak and backpack. Although the probe spawns the existing packed kit, that actor-root rigid object is outside its mesh coverage; the older report's equipment label is broader than the measured set. Boots currently have inventory state without a separate visual renderer in the loadout owner. Those coverage limits remain explicit integration gaps.

The new clock is not driving the author player. L2 must integrate simulation snapshots, rendering, source-time mapping, one graph evaluation order, contact positions and supported idle recovery. L3 must prove final mixed-pose clearance within 1mm, smaller prediction error, equipment/roof clearance and the performance budget. L4–L6 retain transition/steering qualification, current-scene dependency hydration, private restoration sealing and author acceptance.

The broader mining/combat/ladder/cover playground work stays deferred. No agent-owned Play Mode, gameplay smoke test, foreground activation, package change, push or scene promotion is part of this checkpoint.

## Verification checkpoint

The final foundation passed 199 animation-playground Edit Mode checks, including 13 new locomotion repair cases. The startup fixture then passed 7 checks with new exact speed/diagnostic assertions, and 18 legacy locomotion checks passed. See the paired test JSON files and final source manifest. These are offline state/wiring proofs, without agent-owned gameplay simulation or visual acceptance.
