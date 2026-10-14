# Initial animation playground foundation evidence

Date: 2026-10-13. Supports the [implementation status](../../../../Design/Gameplay/CAMP_LOOP_ANIMATION_IMPLEMENTATION_STATUS_2026-10-13.md).

- [verification.json](./verification.json) records source, scaffold, scene preservation and focused test results.
- Private immutable source bytes and their alias receipt remain at `D:\BooterBigArmPrivateAssets\Mixamo\2026-10-13\Originals\` and `source-receipt.json`. The 76 payloads comprise the 74 collection payloads plus two original rig alternatives; 150 aliases includes the collection's 148 copies plus those rig sources.
- Restoration tests: `Tools/Art/Animations/Test-MixamoRestoration.ps1`, nine passing assertions over isolated file/Git fixtures. Tests cover source preservation, first-import authority, sealed pair/GUID continuity, conflicting GUID/metadata refusal, no writes after failed preflight and path containment.
- Unity logs/XML: `D:\BooterBigArmValidation\AnimationPlayground20261013\Logs\foundation-targeting-regressions.*`, `foundation-menu-regressions.*` and `foundation-installed-session-tests.*`. The first regression run lacked SystemMenu.prefab in the candidate; all 25 affected menu cases passed after restoring its exact source dependency. New session/target and legacy interaction/query/HUD cases passed 44 tests.
- The candidate is a bounded dependency copy on D:, not another Editor on the production checkout. Test launches used the pinned executable in hidden background batchmode. The author's scene stayed loaded and was never replaced by test scene-opening operations.

Scene preservation compares original serialized YAML blocks with the saved candidate. Only the three new session records and one root entry are added. An incidental cargo-reference serializer change was restored before closeout. The new marker blocks checkpoint restoration and all write entry points before a writer is created, even when private bindings are absent. Controls alone receive a lab directory; normal settings retain their existing domain.

These checks prove foundation contracts and regressions, not animation import, Humanoid/contact quality, physical station behavior or visual/gameplay acceptance. No private FBX is committed or imported by this batch.
