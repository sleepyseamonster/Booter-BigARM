# Blender transfer receipt — 2026-10-06

The user supplied `wetransfer_blender_2026-10-06_2121/Blender`. Its 252 files totaled 756,017,456 bytes. Every original byte sequence has a verified repository destination in [transfer-manifest.json](./transfer-manifest.json). The incoming folder was consolidated into canonical sources and this receipt. Automatic approval review rejected deletion of the verified redundant copies with the reason “blocked by policy.” The original transfer was instead moved intact to ignored `Logs/TransferHolding/wetransfer_blender_2026-10-06_2121/`. Those local holding files are not published or required by the repository; nothing was discarded.

| Incoming content | Count | Retained location |
| --- | ---: | --- |
| Exact copies already present | 210 | Existing canonical files recorded in the manifest |
| Distinct Blender previous saves | 2 | Beside their current sources, with dated `PreviousSave` names |
| Git LFS reference to the existing 144 MB Badwater scene | 1 | [Original pointer](./references/BadwaterGameSlice.lfs-pointer.txt); the full scene remains at its canonical path |
| Superseded scene notes and terrain builder | 2 | `snapshots/`, preserving the exact restored `4f2fe41` baseline versions |
| Python 3.13 bytecode | 37 | `bytecode/`, with `.pyc.bin` filenames so they are inert archival data |

## Previous saves

- [Badlands previous save](../../studies/BrokenWorldBadlandsStudy_PreviousSave_2026-10-06.blend)
- [Reference slate previous save](../../studies/reference_rock/ReferenceSlate_PreviousSave_2026-10-06.blend)

The date records receipt, not the unknown original save time. These files are distinct automatic-save versions, not exact duplicates. Their bytes are unchanged. Keeping them beside the current sources preserves their relative resource directories. The primary sources remain `BrokenWorldBadlandsStudy.blend` and `reference_rock/ReferenceSlate.blend`; authoring remains paused until the user resumes it.

## Other recovered content

The archived [scene notes](./snapshots/studies/DeathValley/UNITY_BADWATER_SCENE.md.snapshot) and [builder](./snapshots/tools/death_valley/BadwaterUnitySceneBuilder.cs.snapshot) describe the earlier baseline. Current terrain repair code and instructions take precedence. The snapshots are source evidence, not active tools or instructions.

Thirty-six bytecode modules have corresponding source files already present. [game_slice_grid.cpython-313.pyc.bin](./bytecode/tools/death_valley/game_slice_grid.cpython-313.pyc.bin) has no matching `game_slice_grid.py` in the transfer or checkout. It is preserved exactly; editable source recovery has not been established. No transferred code was executed.

The recovered Blender saves have Zstandard container signatures matching the retained source family. This receipt verifies byte identity and storage, not Blender reopening, mesh quality, exports, or Unity import. No scene, material, gameplay asset, or project setting was changed during this organizational pass.

From the repository root, verify all destinations with:

```powershell
.venv\Scripts\python.exe Docs/Agents/Gottspan/tools/verify-transfer-manifest.py Docs/Agents/Blender/archives/2026-10-06-transfer/transfer-manifest.json
```

The repository-wide findings and duplicate-retention decisions are recorded in [the organization audit](../../../../Evidence/Repository/organization-audit-2026-10-06.md).

Add `--incoming` to verify the retained local holding copy as well. This optional check requires that copy on the current machine; the default verifies repository destinations alone.
