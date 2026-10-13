# Camp Loop animation plan reaudit evidence

Date: 2026-10-13. Supports the [audit report](../../../../Design/Gameplay/CAMP_LOOP_ANIMATION_PLAN_AUDIT_2026-10-13.md) and revised [master plan](../../../../Design/Gameplay/CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md). Source inspection and documentation only.

- [findings.json](./findings.json): 22 findings, priorities and correction stages. Runtime implementation remains false.
- [expanded-source-inventory.json](./expanded-source-inventory.json): all 148 collection copies, relative Downloads paths, sizes and SHA-256; 74 byte-distinct payloads.
- [rig-sprint-probe.json](./rig-sprint-probe.json): original/alternative/Mixamo rig inspection and the three sprint-version pose signatures.
- [rig-sprint-probe.py](./rig-sprint-probe.py): reproducible read-only background Blender probe. The executed version used local paths; this saved copy accepts source and output paths as arguments.
- [live-editor.json](./live-editor.json): read-only CLI observation of clean CampLoopTesting Edit Mode, Unity 6000.4.0f1.
- [verification.json](./verification.json): scope, coverage/hash/link/stage/metadata-policy checks and their limits.

The old folder inventory and first plan-validation receipt remain unchanged historical evidence. Expanded collection counts include 73 dated copies, 73 loose motion exports and two standalone accepted-character aliases. The two additional hashes are redundant sprint export attempts; they add source history, not active animation choices. All three share a per-integer-frame signature of sorted pose matrices rounded to six decimals and retain forward travel. This does not prove byte-identical curves or Unity root-motion equivalence.

Original rig alternatives have 74 and 102 bones with 30 finger bones each; the accepted Mixamo rig has 33 bones with eight index-chain bones. Bone/group names do not establish skin weights, usable Humanoid mapping or hand contact. Those remain qualification gates.

Reproduce the probe from the repository root, supplying an output path outside project assets:

```powershell
& 'D:\BooterBigArmTools\Blender522\blender-5.2.2-windows-x64\blender.exe' --background --factory-startup --disable-autoexec --python '.\Docs\Evidence\Gameplay\AnimationPlayground20261013\PlanReaudit\rig-sprint-probe.py' -- 'C:\Users\kbkno\Downloads' 'C:\Users\kbkno\AppData\Local\Temp\rig-sprint-recheck.json'
```

The probe opens existing FBXs in a separate Blender factory scene and writes JSON only. No FBX export, native source overwrite, Unity import, scene mutation, Play Mode, gameplay smoke test, package change, build or publication was performed. Existing code inspection identified typed target dispatch, constrained-motor behavior, preference collision scope, foot-contact/cyclic mapper assumptions, metadata health checks, save timing and scene-test side effects. User-owned Fused art remains excluded.
