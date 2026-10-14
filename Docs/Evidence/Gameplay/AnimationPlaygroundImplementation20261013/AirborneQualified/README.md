# Jump and fall controls enabled — 2026-10-14

Jump input and AP04's jump target now use the qualified airborne family on the existing Booter motor. Standing, moving and selected unarmed variants share one owned sequence through takeoff, flight, contact and recovery. Walking off a ledge enters the falling branch automatically. AP04's fall target explains the drop platform rather than teleporting the actor.

The Rigidbody supplies the vertical arc. Twelve private Humanoid derivatives retain source limb poses while removing the authored second arc. Takeoff commits once; measured support and ground distance determine landing. Flight cancellation retains gravity and continues through contact. Surface loss returns an approaching pose to the falling loop, and failed presentation acquisition releases only this sequence's lease. Pause, disable, disposal and ordered reset clear ownership safely.

Landing severity uses the existing survival-vitals owner. Downward impact speed above 6.5m/s removes 10 health per additional m/s; reset restores the captured session vitals. This is a lab policy, not a completed hurt/death presentation family. Roll recovery remains pending and AP04 reports that gap.

The actual rig and backpack were sampled through the runtime's single Animator output at one, two and four global skin weights. The 171 envelopes include all clip roles, moving takeoff phases, falling/approach boundaries and interrupted landing snapshots. The lowest sole is -0.910000265m against the -0.91m reference plane; maximum whole-pose correction is 0.074223m. Temporary fixture quality settings were restored. Airborne equipment/body envelopes remain fitting and visual-review evidence rather than requiring the visible placeholder capsule to match the character.

All 87 focused Edit Mode checks pass with no failures or skips. They cover the changed airborne/session/station contracts and the existing presentation, motor, crouch, projection and reset seams. The isolated saved-scene snapshot includes its matching salvage closure and has SHA-256 94566c148d7f81b495968a7b92dc20fe2ed32fac88cfaf3d9a82841926c564b8. Later concurrent scene, riding, NPC, inventory and atmosphere revisions are outside this frozen fixture proof and are preserved.

The new PHY1AirborneQualified private checkpoint seals 111 asset/metadata pairs and passes restoration dry-run. Compared with the draft checkpoint, only the private airborne qualification flag changes; original FBX bytes, import metadata, derivatives and GUIDs remain exact. The paired player binding enables input only when the qualified family and foot projection are present.

This completes a bounded jump/fall handoff, not PHY1 or the full playground goal. Slide/roll, remaining interaction families and requested motion gaps remain active work. No Play Mode, gameplay smoke test, terrain edit, author-scene write, package change, push or external publication occurred.
