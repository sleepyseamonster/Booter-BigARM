# Camp Loop animation playground implementation

Started 2026-10-13 from `e5a6e8b9d`, following the [revised plan](./CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md). The implementation goal remains active. This document records completed slices and current dependencies, not full playground acceptance.

## Initial foundation batch

- PRE0: main/origin-main, Unity 6000.4.0f1 and loaded clean CampLoopTesting confirmed. Unrelated Fused rock files remain excluded.
- DEP1: scoped private dependency ignores, source recovery script, immutable original-byte archive, sealed payload/metadata restoration and public folder scaffolding. The archive holds 74 collection payloads plus two original rig candidates; all 150 source aliases, including the two additional rig sources, were verified. Restoration refuses different bytes/metadata, duplicate GUIDs and escaping paths. No source FBX has entered Unity yet.
- FND1: authored session marker, readiness/private binding contract, checkpoint restore/write guard before startup, and separate controls-directory injection. The saved scene gains only the three session GameObject/component/transform records and its root entry; existing serialized objects remain intact.
- FND2: common non-resource target interface and captured identity/epoch, compound-collider ownership, deduplicated selection and explicit station dispatch. Existing resource-facing APIs remain compatible. Shared activity projection now gates cargo, canister, camp and algae use against legacy and station/motor ownership. A station without a receiver reports unavailable and never creates a gather reward.

FND3 is implemented: a runtime-owned lab input clone preserves original binding IDs, adds deterministic lab bindings, routes commands through release/focus/device/modal gates, and gives owned-action cancel priority over idle pickup. Controls include new actions in coactive conflict checks and expose them in Settings. Compatible production preferences can seed a new lab preference set without writing either file; existing lab data wins. The authored 71-motion public catalog retains three restricted sources and offers 68 candidates through an eight-row paged panel inside the existing menu owner. Selection remains separate from physical execution; preview stays unavailable until private imports qualify. See the [input/palette evidence](../../Evidence/Gameplay/AnimationPlaygroundImplementation20261013/Input/README.md).

The timed physical coordinator, rig/profile/contact qualification, animation imports, player cutover, station construction and motion-gap closure follow next. Private binding readiness remains unavailable until imports qualify; no physical action is promoted from preview alone.

## Validation and source boundaries

File restoration fixtures pass nine checks. The isolated Unity candidate at `D:\BooterBigArmValidation\AnimationPlayground20261013` contains the Camp Loop dependency closure and active scripts/settings/UI/character/test assets; it is a validation copy, not a Git worktree. Initial session and targeting regressions pass 44 tests. All 25 menu regressions pass after the candidate's missing SystemMenu prefab dependency was restored. Installed-session checks are repeated against the saved scene. See the [foundation evidence](../../Evidence/Gameplay/AnimationPlaygroundImplementation20261013/Foundations/README.md).

No Play Mode, gameplay smoke test, production scene/terrain modification, package change, build or publication is part of this batch. Appearance/contact/feel acceptance remains author-owned. Scene-opening tests run only in the isolated candidate. Future local commits record bounded verified stages while the larger goal continues.
