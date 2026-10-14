# Owned pose transitions — 2026-10-14

The existing animation driver now captures weighted active clip sources at their current times into a frozen snapshot mixer. It blends that snapshot into the incoming action, crouch or standing pose under the existing single Animator output. Nested transitions capture the currently mixed pose. Foreign owners cannot claim or seek the transition; its explicit elapsed clock rejects reversal.

Crouch entry and exit use the snapshot transition. Their completed poses settle into crouch idle or standing idle under a temporary constraint in the existing motor. Fixed-step settling pauses with the laboratory modal/focus policy. Reset, disable and disposal release the transition graph/constraint before posture restoration or safe recovery handoff. Contact effects remain suppressed while the outgoing snapshot contributes.

The focused suite passes 72 Edit Mode tests, with no failures or skips. It covers source time freezing, one-output/normalized weights, nested snapshot replacement, monotonic clock ownership, controller phases, pause/reset and existing source/player/station/lifecycle contracts.

Sixteen actual runtime blend envelopes retain at least 12.2mm of protected capsule clearance, but the lowest sampled sole reaches -0.924308m against the -0.91m floor. That 14.3mm penetration means the transitions are not physically qualified. The current sustained-posture correction table does not cover these arbitrary outgoing pose snapshots. Transition-specific grounding is the next dependency; the private profile flags remain false.

No source payload, importer metadata, scene, terrain, package or external publication changed. No Play Mode or hands-on smoke test ran. The complete implementation goal remains active.
