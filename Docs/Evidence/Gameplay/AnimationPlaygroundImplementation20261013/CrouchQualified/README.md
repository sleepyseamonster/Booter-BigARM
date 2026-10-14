# Crouch controls enabled — 2026-10-14

The private seven-role crouch profile is now physically enabled. V or the gamepad left-stick press toggles crouching; AP03's interaction offers the same posture change. Movement uses the existing motor, fixed entry facing, measured displacement clock and one Animator output. Ceiling/wall clearance, blocked standing, modal pause, cancellation and safe reset/recovery remain enforced.

AP03's target and status reflect actual family availability. Slide stays unavailable, and AP08 does not treat ordinary crouching as completed cover. Sprint plus the crouch command reports pending slide integration. Pending jump/action commands also report availability without starting movement or effects. Handled station failures retain their specific feedback.

The existing visual owner applies the paired foot projection throughout crouch entry, sustained movement and exit, including changes to the global skin-weight quality. Fifty-seven equipped pose envelopes cover 7,353 samples using one, two and four bone weights. The model renderers retain Auto. The lowest measured sole is -0.910000086m against -0.91m; prediction error stays below one micrometre. Fixture quality changes are restored, and the project quality file remains byte-identical.

The backpack reaches 1.73334m in the measured crouch mixtures. AP03's roof underside is now 1.78m, leaving over 46mm of measured clearance while preventing standing with the 1.82m capsule. Only five prefab transform fields change; asset metadata and file IDs are preserved. The visible capsule placeholder is already hidden by the driver. Its separate CapsuleCollider is an adjustable collision approximation, not a rig/import dependency.

All 75 focused Edit Mode cases pass without failures or skips. They include family readiness, station availability/epoch invalidation, pending-command feedback, private source/rig contracts, presentation, motor/posture, interruption and ordered reset. A saved scene snapshot with the concurrent salvage catalog/asset closure is present in the isolated validation copy. Subsequent unrelated scene and inventory revisions remain outside that frozen fixture proof. This task preserves those edits and does not stage them.

The new PHY1CrouchQualified private checkpoint contains 98 paired assets and passes capture/restoration dry-run. Original FBX files, import metadata, derivative curves and binding GUIDs remain intact; only the private crouch qualification flags change from the prior projection checkpoint.

This enables crouch testing on the authored playground. The full PHY1 stage, other physical families, requested motion gaps and author appearance/feel acceptance remain unfinished. Crouch starts/stops, slope contacts and the cloak hem during exit still need visual review. No Play Mode, gameplay smoke test, terrain edit, production-scene change, package change, push or external publication occurred.
