# Badwater gameplay terrain baseline â€” 2026-10-06

This is the first implementation record for the [gameplay terrain plan](../../Design/Gameplay/BADWATER_GAMEPLAY_TERRAIN_PLAN.md). It separates source and build proof from Player performance and interactive traversal.

## Fixed review configuration

- Target machine: Apple M1 Max MacBook Pro, 32 GB RAM; Unity 6000.4.0f1; macOS Standalone Development Player; URP 17.4.0.
- Scene: `Assets/_Project/Scenes/TopDown3D/BadwaterFourSlices.unity` alone, selected explicitly by `BadwaterDevelopmentBuild.BuildFromCli`. Production Build Settings and the production prototype scene remain untouched.
- Fixed comparison view: native 1920 Ã— 1080 window, full-content rendering, 8 km Badwater camera far clip. Capture a basin view, mountain view, ordinary 2 m tile border, and the 1 m/2 m focus perimeter. Keep the same camera poses and quality tier on repeated runs.
- **Provisional diagnostic targets, not accepted product budgets:** 60 fps with p95 frame time at or below 16.7 ms and p99 at or below 33.3 ms; no visible terrain/collision gap; record native process memory and startup time before setting a memory or load-time pass threshold. CPU, GPU, and native memory need Player measurements, not Editor estimates.
- The existing `TopDown3DPlaytestPerformanceProfile` logs full-content frame, CPU, GPU, resolution, and managed-memory telemetry in a Development Player. It does not measure all native texture memory or replace Unity Profiler captures. Do not launch with `-topDown3DStressProfile` for this baseline.

## Source and structural evidence

- The read-only [`audit_badwater_source.py`](../../Agents/Babineaux/tools/audit_badwater_source.py) pass verified the recorded SHA-256 of all 256 imported RAW elevation and PNG color tiles. Its [machine-readable report](./source-terrain-audit.json) records the bounds, resolution tiers, and slope estimates. It did not modify source data.
- The four 1 m focus tiles are `r12_c01`, `r12_c02`, `r13_c01`, and `r13_c02`. The other 252 tiles use 2 m height spacing. Every geographic color tile is 128 Ã— 128 px over 256 Ã— 256 m, or 2 m per pixel.
- The scene serializes 256 Terrain and 256 TerrainCollider components. The existing isolated Unity validator previously found matching terrain borders, geographic texture links, and at most 0.02750 m sampled border mismatch after Unity height readback. This pass has not repeated that Editor validation.
- All 256 TerrainLayers have a geographic diffuse image and **no normal or mask map**. That is appropriate for a geographic overview, but does not prove convincing walking-distance soil, rock, talus, or wash materials.
- Approximate slope area above the serialized Legger 38Â° route threshold is 36.343% across the four quarters; 13.992% is above Booter's serialized 48Â° walkable-slope threshold. These are centered differences of source height samples, not Unity collision normals or connected-route findings. Large steep areas are expected and should be treated as physical obstacles rather than automatically flattened.

## Isolated build result

The Badwater-only Development Player built successfully in an isolated Unity project copy and passed `BadwaterPlayableSceneBuilder.ValidateFromCli` immediately before packaging. Unity reported `419,291,257` bytes for the build and `00:03:50.8689690` build time; the resulting `.app` occupies about 401 MiB on disk at `Builds/BadwaterFourSlices/BadwaterFourSlicesDevelopment.app`. The app has **not** been launched or playtested.

The first isolated attempt from committed `HEAD` failed compilation because committed editor files reference `WorldCreatorProductionProfile.TryGetCanyonShowcasePosition`, which exists only in the current unrelated working-tree edits. A fresh import also emitted package compiler errors. The successful second attempt copied the **current** `Assets/` working tree into the isolated project. It therefore proves that current in-progress sources plus the new Badwater build method can compile and package this scene; it is not proof of a clean-commit build. No unrelated live files were modified or staged to resolve this.

## Player and visual evidence still required

| Question | Evidence needed | Current state |
| --- | --- | --- |
| Is the 4 km scene fast enough when fully visible? | Cold and repeat Development Player frame-time distributions, GPU/CPU time, native memory, startup, draw calls, and same-view comparison. | Build exists; Player profile pending. Tile count alone is not a bottleneck measurement. |
| Can Booter and Legger traverse the intended routes? | User-owned interactive pass at basin, ordinary tile borders, the focus perimeter, and a selected mountain approach; record blocked slopes and any falling or false route. | Pending. Source slopes only identify candidates. |
| Does the ground read as a game-world surface near the camera? | Fixed-view close captures, terrain-layer/material inspection, and user visual acceptance. | Not yet. Geographic albedo and absence of normal/mask maps are documented limits. |
| Should collision or assets stream? | Demonstrated collider physics/startup or asset-memory bottleneck after the baseline. | No decision justified yet; keep all 256 visual tiles and colliders active. |

## Build method

The dedicated editor method validates the scene and builds a Development Player from an explicit one-scene list. It refuses to overwrite an existing `.app` and never edits Build Settings. Run it only in an isolated project copy while the main Unity GUI owns the repository:

```sh
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -projectPath "<isolated-project-copy>" -batchmode -nographics -quit \
  -buildTarget StandaloneOSX \
  -executeMethod BooterBigArm.Editor.BadwaterDevelopmentBuild.BuildFromCli \
  -buildOutput "<new-output-directory>/BadwaterFourSlicesDevelopment.app"
```

The build compiles and packages a scene; it does not establish traversal feel, visual acceptance, or Player runtime performance.
