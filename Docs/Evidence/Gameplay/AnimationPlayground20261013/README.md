# Animation playground audit evidence — 2026-10-13

This receipt supports the [Camp Loop animation playground audit](../../../Design/Gameplay/CAMP_LOOP_ANIMATION_PLAYGROUND_AUDIT_2026-10-13.md). It records downloads and read-only inspection, not an implemented playground.

## Local collection

Root on this machine: `C:\Users\kbkno\Downloads`. Five dated folders contain 73 FBX copies, totaling 22,671,616 bytes, with 72 unique SHA-256 hashes. One unique file is the skin-bearing character T-pose; 71 are motions. There are 68 unarmed evaluation candidates and three restricted motion references. The two mining-reference copies are byte-identical and accounted for in the inventory.

The 47 original-category downloads are under `MixamoOriginalActions20261013`; its local download manifest has 47 entries and named preview images support the selected poses. Other folders hold locomotion, sleeping, cover and additional interactions. The download manifests and screenshots remain local in Downloads. Some extra screenshots show rejected selections; a screenshot alone does not mean its animation was downloaded or accepted.

The accepted website character name is `male_basemesh_mixamo_unrigged` because the upload source was mesh-only. Mixamo auto-rigged it. `male_basemesh_mixamo_rigged.fbx` is the local handoff alias, hash-matched to `MixamoLocomotion20261013/Character T-Pose.fbx`. Earlier rigged exports were rejected by Mixamo; no original Blender file was overwritten.

## Included proof

- [animation-inventory.json](./animation-inventory.json): file hashes/sizes, relative download paths, source titles, categories, eligibility limits, frame ranges/durations, mesh counts, hierarchy match and sampled pose/hip data.
- [live-scene.json](./live-scene.json): background query of the already-open editor; scene path, clean state, nine resident terrains, sampled component inventory and expected empty Edit Mode Animator list.
- [live-editor-state.json](./live-editor-state.json): read-only closeout query confirming the same scene remains loaded, clean and outside Play Mode in Unity 6000.4.0f1.
- [live-references.json](./live-references.json): current visual/profile/input/clip references, capsule dimensions and old-model visual scale.
- [fbx-probe.py](./fbx-probe.py): background Blender inspection script. The executed script used the machine's Downloads root directly; this saved copy makes that root a command-line argument. Inspection logic is otherwise retained.
- [verification.json](./verification.json): passed inventory, hash, path, documentation and probe-syntax checks, with their proof limits.

Repository HEAD at source inspection: `fa79cc60d1fa53528cf13c4406b792ed5088ca94`, main tracking origin/main. Editor observed: Unity 6000.4.0f1, project `D:\Arc & Dust\Project`, active scene `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`, clean, loaded, not playing. Blender: installed 5.2.2 LTS at `D:\BooterBigArmTools\Blender522\blender-5.2.2-windows-x64\blender.exe`.

Equivalent reproducible FBX inspection, from the repository root in PowerShell:

```powershell
& 'D:\BooterBigArmTools\Blender522\blender-5.2.2-windows-x64\blender.exe' --background --factory-startup --disable-autoexec --python '.\Docs\Evidence\Gameplay\AnimationPlayground20261013\fbx-probe.py' -- 'C:\Users\kbkno\Downloads'
```

The script reads the base FBX and dated download folders and writes `mixamo-complete-inventory-20261013.json` in Downloads; it does not save a Blender scene or alter an FBX. Each unique hash was imported once. Bone-name/parent pairs match the base's 33-bone hierarchy. Immutable pose matrices were sampled at the first, midpoint and last frames; all motion files changed pose. The static base pose is expected. Frame durations are keyframe spans at 30 fps, not animation-controller timing decisions.

Hip values are **Blender rig-local units**. They are not Unity metres, not normalized root trajectories, and not continuous collision/contact measurements. Skeleton and sampled-animation checks do not prove Unity Humanoid validity, visual quality, complete grip, looping quality or playable mechanics. Only index-finger chains are present; model material slots are empty and scale remains an import gate.

No Unity asset import, scene save, scene builder, gameplay code, input asset, package, project setting, Play Mode, gameplay smoke test, Unity test or build was performed for this audit. Editor inspection used the existing background-safe CLI connection; application windows were not focused. Downloads were explicitly authorized by the author. The repo's unrelated untracked Fused rock assets were excluded from this task.

## Documentation closeout checks

Passed: JSON parseability, SHA-256/file-size correspondence against all 73 local FBX copies, expected counts and restrictions, base-alias equality, source-title coverage, Python syntax, local Markdown link/path existence and Git whitespace errors. Seventy-three table rows, 68 distinct unarmed candidates and all 17 referenced live asset paths were confirmed. These are documentation/evidence checks; they do not substitute for the future Unity import and gameplay acceptance gates.
