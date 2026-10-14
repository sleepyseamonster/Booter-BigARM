# Airborne controller and clip draft — 2026-10-14

The shared action coordinator now supports a physical sequence with a stable motor lease and owner-checked stage replacement. Stage time and pose time are separate from finite effect markers. A falling loop can outlast its clip without ending physics ownership or repeating effects; pause freezes both clocks and the mapped pose. Preview cannot replace a physical stage, and foreign owners cannot switch or cancel it.

The new airborne controller submits intent through the existing motor. Takeoff is an exactly-once marker impulse. Rising poses follow vertical velocity; descending poses continue under gravity. Landing approach follows measured distance below the feet, and landing requires support plus the contact-distance gate. A clip ending alone cannot land the player. Cancel during flight retains falling ownership through contact. Ordered reset and external cancellation clear stale sequence state.

Fourteen candidate-only FBX policy comparisons preserve canonical source bytes/importers. Six root-curve probes establish the height conversion used for private derivatives. Twelve derived clips supply standing, running and unarmed takeoff/rising/landing roles, a falling loop, contact approach and hard recovery. Rising pelvis height stays constant after removing the source arc, so it does not add another arc to Rigidbody motion. A constant clearance adjustment keeps those rising soles above the reference collider foot plane.

All 85 focused Edit Mode cases pass without failures or skips, including six new airborne cases, sequence/presentation ownership, ground-distance queries and existing crouch/player/station contracts. Tests use the isolated frozen animation candidate. They do not establish hands-on animation feel or the latest unrelated scene/source revisions.

The 111-pair immutable PHY1AirborneDraft checkpoint captures the twelve derivatives, private airborne profile and paired binding update. Capture and restoration dry-run pass. Canonical FBX/import metadata and existing GUIDs remain intact. Experimental policy FBXs and gauges stay in the candidate, outside the author asset set.

The profile remains unqualified and is not subscribed to the author scene's jump command yet. Actual rigged transition envelopes, landing/contact presentation, equipment and input/session handoff remain before enabling it. Roll recovery and its collision/damage policy are still required. This is a verified implementation draft, not completed PHY1 or full playground acceptance.

The animation lane writes neither CampLoopTesting.unity nor Session.Player.cs during the coordinated NPC/riding integration slots. Handoffs are in the ignored coordination logs. No foreground activation, Play Mode, gameplay smoke test, production change, terrain edit, package change or external publication occurred.
