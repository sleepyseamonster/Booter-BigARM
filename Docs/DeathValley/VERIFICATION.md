# Death Valley inventory verification

Evidence date: October 6, 2026, on the Windows checkout. This record distinguishes retained terrain facts, new source checks, and the profiling build. No gameplay smoke test or Player traversal was performed by this task.

## Coverage and source checks

- `audit_coverage.py --refresh-boundary` retrieved the official NPS DEVA polygon in EPSG:26911 and retained its request, response, timestamp and hash. The park polygon is distinct from the larger study rectangle.
- The audit discovered eight configured study footprints and six materialized canonical Death Valley Blender files. File hashes and byte counts are recorded; Blender was not available for fresh scene reopening.
- All 256 height-export hashes and all 256 color-export hashes match `tiles.csv`. All 480 source-border pairs match at common source samples. Every source tile address is valid and unique, dimensions and byte counts agree, and all 256 TerrainData GUIDs occur in the scene.
- The saved scene contains 256 Terrain and 256 TerrainCollider components. This audit does not decode Unity's binary TerrainData or assert fresh rendering/physics acceptance.
- `pip check` passed for the existing pinned GIS environment. Both new Python tools passed syntax parsing, and all package JSON files parsed successfully.

The [catalog](./coverage_catalog.json) records the audited Git revision, actual scene path and preserved scene GUID. The snapshot is refreshed after the coordinated Greater Wasteland rename. Concurrent ownership changes are preserved rather than included in this task's scope.

## Atlas review

The standalone atlas was loaded in headless installed Chrome. Study selection, keyboard selection of a 1 m focus tile and proposed-candidate selection displayed the expected records, with zero JavaScript errors. Desktop and 390 px mobile layouts were captured; the mobile layout had no horizontal overflow. Both captures were visually inspected. Screenshots are local ignored diagnostics under `Logs/DeathValleyInventory/`.

## West candidate source check

The [TNM catalog response](./usgs_west_catalog.json) lists two 1 m products intersecting the proposed west batch. Raster headers confirm EPSG:26911, native 1 m spacing and product bounds. Bounded source windows and individual XML metadata establish the bare-earth NAVD88 surface used in the candidate check.

The prepared 1025 × 1025 grid has zero missing samples. Its eastern boundary differs from retained encoded Badwater heights by at most **0.013733 m**, below the **0.027467 m** encoding-step threshold. The two sources differ by at most **0.033844 m** in their overlap. Composition uses a recorded source-owner split rather than silently averaging them.

Direct reprojection onto a 2 m grid initially produced a 0.069448 m boundary discrepancy and was rejected. The corrected method prepares at native 1 m spacing and strictly decimates to 2 m, preserving the existing pipeline's shared-sample convention. The passing [assessment](./west_source_assessment.json) records source-window, metadata and prepared-grid hashes. The candidate has not been selected or imported into Unity.

## Windows build support

`BadwaterDevelopmentBuild.BuildFromCli` now supports active `StandaloneWindows64` as well as `StandaloneOSX`. Both use an explicit one-scene list and Development build options. Output must be outside Assets, use the target's extension, and not already exist. Windows requires a new parent directory so sibling data and support files cannot be overwritten. The method does not change Build Settings or launch the resulting Player.

Seven focused guard test cases cover both accepted targets, mismatched extensions, existing Windows support files, normalized paths into Assets, existing macOS applications and an unsupported target. A separately selected canonical terrain-validator test checks the scene and source-derived vertices.

The focused tests ran in `%LOCALAPPDATA%/BooterBigArm/DeathValleyBaseline-20261006`, copied from Assets, Packages and ProjectSettings before the scene-promotion task. **8 passed, 0 failed, 0 skipped**; Unity exited 0. Test XML is at ignored `Logs/DeathValleyInventory/windows-build-tests.xml`. Fresh package import emitted transient API-migration compiler messages; the same run completed import/compilation and executed the passing tests without changing package pins.

The isolated copy was then refreshed with the coordinated Greater Wasteland scene, preserved scene metadata, build routing and changed source references. Its stale old scene/metadata pair was removed only in that isolated directory to avoid duplicate GUIDs. **Windows Development packaging succeeded**, with Unity exit 0, reported build size **282,466,421 bytes** and build time **5 minutes 9.961 seconds**. The output is `Builds/DeathValleyBaseline-20261006/GreaterWastelandDevelopment.exe`; full build diagnostics are at ignored `Logs/DeathValleyInventory/windows-development-build.log`. The executable has not been launched. The open live Unity project was not batchmoded.

[windows_player_build.json](./windows_player_build.json) records executable and log hashes. [windows_focused_tests.json](./windows_focused_tests.json) records the focused XML result and hash. The copied project remains local under `%LOCALAPPDATA%/BooterBigArm/DeathValleyBaseline-20261006`; it is a project snapshot, not a Git worktree or new production lane.

After syncing the promotion task's final HUD installer, `BooterBigArm.Editor.ConversionBaselineValidator.ValidateFromCli` exited **0** and reported **Production and legacy-boundary validation passed**. Its ignored log is `Logs/DeathValleyInventory/greater-wasteland-conversion-validator.log`. This confirms current production routing and preservation checks in the isolated copy, not runtime traversal.

## Remaining gates

The [Windows baseline configuration](./windows_baseline_configuration.json) records the machine and proposed comparison setup. A successful build establishes packaging, not performance. User-owned traversal and visual review, actual Player quality/resolution, cold and repeat startup, frame-time distributions, CPU/GPU cost and native memory are still required before choosing performance changes or integrating the proposed expansion.

Procedural generation remains deferred under the user's Greater Wasteland direction. Original full-region GIS data recovery and Blender scene readability remain separate gaps. No raw source proof is considered archived merely because it exists under ignored Logs.
