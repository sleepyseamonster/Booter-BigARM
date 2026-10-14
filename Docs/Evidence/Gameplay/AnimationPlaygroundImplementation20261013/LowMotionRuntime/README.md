# Owned slide and roll controller — 2026-10-14

The lab now binds a physical low-motion controller to its private draft profile. Sprint plus the crouch command routes to slide; selected Falling To Roll uses a contact-only airborne handoff. Input and station availability stay disabled because the equipped animation profile is unqualified. Existing crouch, jump and default landing behavior remain enabled.

The controller retains one coordinator/motor lease through entry, active and recovery phases. Its pose clock freezes at the last collision-clear shape, waits for real support before recovery, respects modal pause and finishes the active roll before cancelling. Landing transfer preserves the motor lease and delivers the existing damage notification once. Its contact presentation emits through the existing foot-contact owner after the blend.

Blocked recovery can hand off to the existing crouch controller when its shape is clear. Otherwise it retains the safe posture until clearance or ordered reset. Failed resets can be retried without losing the held pose. Controller disable finishes safely; blocked disposal transfers the pose and existing motor lease to a retained recovery component. That component preserves gravity, completes remaining source phases before standing and never teleports the actor.

All 127 focused Edit Mode checks pass without failures or skips. They cover the controller, physical landing handoff, collision/support, pause, effect timing, reset/retry, foreign-owner protection, crouch transfer and disposal, plus the existing animation/player/station regressions. The contact fixture explicitly evaluates the same single animation output so blend completion is part of the tested sequence.

The final candidate includes the current committed active runtime assembly, including released riding and workstation hooks, while retaining this task's exact changed files. The overlay manifest records that boundary and HEAD. The saved-scene fixture remains the isolated snapshot and matching salvage closure described in earlier receipts; later concurrent scene and uncommitted source changes are preserved and outside this proof.

The new PHY1LowMotionBoundDraft checkpoint retains 119 paired private assets. Only PlayerBindings.asset changes from the prior draft seal, adding the low-profile reference. Source FBXs, importer metadata, derivatives, profile flags, cache and GUIDs remain exact. Actor/station identity, nine resident terrains and session-only state policies are unchanged.

This is a runtime handoff with the art gate closed. The cloak's low-pose fit, the final supported pose envelopes and reduced-cache validation are still required before enabling slide/roll input. The larger playground goal, other families and requested motion gaps remain active. No Play Mode, gameplay smoke test, author-scene save, terrain change, package change or publication occurred.
