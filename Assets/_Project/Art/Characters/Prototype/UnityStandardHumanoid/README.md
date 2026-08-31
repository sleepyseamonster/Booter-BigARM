# Unity Standard Humanoid Prototype Asset

This folder contains the male Humanoid rig and a narrow animation subset copied from Unity Technologies' public `Standard-Assets-Characters` repository at commit `8f185aa3c782d824976653dd6a6476312daad241`.

Source: https://github.com/Unity-Technologies/Standard-Assets-Characters

License: Unity Companion License for Unity-dependent projects. See https://unity.com/legal/licenses/unity-companion-license

Imported content is limited to the prototype Humanoid model plus the earlier animation subset. The model remains Booter's production visual and the older traversal/gather clips remain in use where explicitly referenced. Grounded idle, walk, run, sprint, directional gait, start, stop, and pivot animation now come from the single MoCap Central Core Motion profile under `Assets/_Project/Settings/Player/`; the older grounded loops and rapid-turn files are retained licensed source material, not a second runtime authority. Sprint-jump remains retained source material rather than part of the current move set. The source controller, input, camera, scripts, scenes, audio, and sample environment are intentionally excluded so the prototype's existing Rigidbody motor remains authoritative.
