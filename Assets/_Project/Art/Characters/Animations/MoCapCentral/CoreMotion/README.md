# MoCap Central Core Motion

Licensed source: MoCap Central, `Animation Pack - Core Motion`, version `1.8.0`.

Purchase/download date: 2026-08-17.

License: MoCap Central Standard Commercial License. The source animations may be used in finished projects, but the raw animation files must not be redistributed, resold, used to build a competing motion library, or used for AI/ML training. See <https://mocapcentral.com/pages/licensing> and <https://mocapcentral.com/pages/faq>.

Downloaded archives (kept outside the Unity repository):

- `MC_Core_Motion_SourceFiles.zip` — SHA-256 `6eac87b735dedf3bf1930568d5d1442f802a96016cd70380945718d6b4f0342e`
- `MC_Core_Motion_v1_8_0_Unity_2022_3.unitypackage` — SHA-256 `fd44f4485712fa8c503085a00d7bf2b87af1e576ffe721441895140e400bf2a2`

This folder intentionally contains only the Unity-skeleton, no-root-motion Humanoid clips required by Booter's grounded locomotion profile: idle, forward walk/jog/run, directional jog, authored jog start/stop, and a forward pivot. The right-foot start/stop/pivot clips are explicit Humanoid mirror imports of their paired source files. The vendor controllers, demo scenes, characters, materials, props, unrelated animations, UE5 content, and root-motion duplicates are excluded so `TopDown3DPlayerMotor` remains the sole world-displacement authority.

The canonical profile is `Assets/_Project/Settings/Player/TopDown3DLocomotionClipProfile.asset`. Its loop contact phases were measured from the source skeleton rather than copied from the previous animation set. The Editor-only profile reporter can resample the production Humanoid in a hidden preview scene and reports contact candidates, root drift, facing drift, and loop discontinuity without changing gameplay motion.
