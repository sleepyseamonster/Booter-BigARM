# Crouch interruption and boundary checkpoint — 2026-10-14

Controller disposal under a low ceiling now transfers the existing crouch pose and posture lease to the existing animation driver. Movement stays suspended while standing is obstructed. The driver restores the original standing shape and releases recovery ownership once clearance exists, without teleporting the actor or adding another Animator/physics writer. New timed presentations cannot steal the recovery pose.

Disabling the crouch controller in clear space restores standing and ordinary movement. Disabling it beneath a ceiling retains the smaller shape and safe pose; re-enabling resumes the controller. Foreign owners cannot transfer or release the posture lease. Reset, pause, commanded-velocity, source-grid and station regressions remain covered by the focused run.

Sixteen measured pose boundaries show unresolved hard cuts: feet differ by about 12–15cm, and the largest hand difference is about 82cm between idle and exit. Entry/idle/exit and return-to-standing need an owned blend under the existing graph, with floor/body clearance checked during that blend. The measured boundary report is aggregate evidence, not transition acceptance.

The private profile remains gated. Equipment and terrain acceptance, transition blending, the remaining physical families and requested motion gaps remain part of the active goal. No Play Mode, hands-on smoke test, new asset payload, scene/terrain change, package change or publication occurred in this batch.
