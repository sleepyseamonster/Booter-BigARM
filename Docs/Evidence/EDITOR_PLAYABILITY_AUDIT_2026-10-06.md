# Unity Editor playability audit — 2026-10-06

Audited the local `main` checkout restored from `4f2fe41c8e6074f99b6e55fb92a32e41f6fe9a54`, plus the preceding Windows setup changes. The user's current target is **Unity Editor Play mode**. Temporary standalone builds were removed, and no player-build command was retained in the new audit helper.

**Subsequent repair:** The user's visible-crack report led to the [terrain seam repair](./Badwater/terrain-seam-repair-2026-10-06.md). That record supersedes this audit's original Death Valley grounding failure and border-readback readiness implication. Nine focused background checks now pass, including original-scene controls; the findings below describe the earlier candidate.

## Result

| Scene | Current Editor evidence | Readiness |
| --- | --- | --- |
| `Assets/_Project/Scenes/Reference/GeneratedWorld/TopDown3DPrototype.unity` — generated game world | Original scene loads, generates terrain and decorations, grounds Booter, moves Booter with keyboard and gamepad input, rotates the camera, opens/closes inventory, and physically moves Legger. | Basic playability passed. Visual quality, physical-controller feel, full traversal, and performance acceptance remain unverified. |
| `Assets/_Project/Scenes/TopDown3D/BadwaterFourSlices.unity` — Death Valley | Original scene loads with 256 terrains and matching colliders, but Booter's ground query fails. The terrain objects have no `TopDown3DGroundSurface` components. | Scene wiring must be corrected before calling its locomotion and companion behavior ready. |
| Death Valley diagnostic variant | Adding the missing marker to terrain objects **only in Play-mode memory** restores grounding and passes the same basic controls and companion checks. The original scene was not saved. | Confirms the missing-component diagnosis; this is not a fix to the saved scene. |

`LandscapeAuthoringSandbox.unity` is an authoring scene, not a second game-world entry point. `IsometricConversionLab.unity`, `Legacy2D/`, `Engine/`, and `Unreal/` remain preserved references.

## Findings in priority order

### P1 — Death Valley terrain lacks the gameplay ground marker

All 256 Terrain/TerrainCollider owners lack `TopDown3DGroundSurface`. Booter's `TopDown3DPlayerMotor.TryGetGroundNormal` accepts only colliders with that component in their parent hierarchy. Legger's `TopDown3DBigArmFollower.TryProjectToGround` and avoidance checks require it directly on the collider's GameObject.

Collision exists: the original scene's Booter rests near the measured floor, and a raycast finds `Badwater_2m_r13_c00` with a 5.66° surface slope. However, `IsGrounded` remains false because the marker is absent. A marker on only the shared terrain root would not satisfy Legger's direct component lookup. The appropriate repair is to mark every TerrainCollider owner, preserve terrain/source identities, and add that requirement to the playable scene builder/validator.

Relevant owners:

- [Playable scene builder and validator](../../Assets/_Project/Scripts/Editor/TopDown3D/BadwaterPlayableSceneBuilder.cs).
- [Player ground query](../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.cs).
- [Companion ground query](../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DBigArmFollower.cs).

The existing Badwater structural validator checks terrain count, actors, input, camera, HUDs, lighting, and spawn heights, but does not require these markers. Its successful result alone was insufficient proof of playability.

### P2 — Existing test and production decoration coverage disagree

`TopDown3DFoundationTests.LandscapeCameraPullback_HasMatchingWorldCoverage` expects `DecorationStreamingRadius == 3`, while the committed `TopDown3DWorldSettings.asset` contains `decorationStreamingRadius: 4`. The terrain streaming radius is separately 7. Reconcile the intended coverage and its performance budget before changing either the asset or the test. The current larger decoration radius does not prevent the observed startup and controls checks.

### P2 — Native fusion authoring is not available in Windows Editor

`TopDown3DManifoldNative.IsSupportedPlatform` is true only under `UNITY_EDITOR_OSX`. The repository contains the two macOS `.dylib` dependencies and no Windows counterpart. The existing cube-union test asserts support without a platform guard and fails on Windows.

This concerns editor-side rock Boolean authoring. Production gameplay uses baked rock assets; the observed game-world runtime does not require that native authoring backend. Windows authoring parity and correct platform-specific test expectations remain separate work.

### P2 — Shader warnings need review before visual acceptance

DirectX compilation reported:

- `BrokenWorldTerrainBlend.shader`, line 394: potentially uninitialized `SampleWholePebble` value.
- `BrokenWorldVolumetricDust.shader`, line 128: integer-modulus performance warning.
- URP `Shadows.hlsl`, line 310, through the volumetric dust shader: floating-point division-by-zero warning.

No shader source was changed. Global volumetric haze remains parked by default. These warnings do not establish a startup failure, but compiling and exercising controls does not accept rendered appearance.

### P2 — Full EditMode suite has no completed result

The first full-suite attempt was interrupted after approximately 20.6 minutes without a final XML result. A second instrumented attempt progressed into `TopDown3DNaturalObjectTests.EveryPhysicalTier_GeneratesIndependentRootsAtDescendingFrequency`, which collects formation plans over a 17 × 17 chunk region (289 chunks), and was also bounded and interrupted. No complete full-suite pass or failure count is claimed.

The progress callback and retained logs identify the slow fixture. It needs a dedicated test-cost investigation; tests were not weakened, deleted, or presented as passing.

### Preserved legacy reference issues

Seven unresolved GUID references exist in `Legacy2D/Settings/Rendering/URP/Renderer2D.asset`: six probe-volume debug resources and the falloff lookup texture. The 3D production renderer is index 1 and both legacy 2D build scenes are disabled. These references remain a legacy maintenance issue; no unresolved production GUIDs were found.

## Completeness checks

- Git object integrity and current LFS integrity passed; both large Blender sources are materialized.
- Unity package manifests retain their original versions. Package restoration and compilation completed in Unity `6000.4.0f1_8cf496087c8f`.
- 2,452 serialized asset/metadata files were checked in the initial GUID audit. No duplicate asset GUIDs or unresolved production GUID references were found.
- No unresolved local scene file-ID references were found in the three TopDown3D scenes.
- Unity asset/metadata pairing passed. No existing metadata or scene file was regenerated or replaced.
- All 256 Death Valley RAW/color tile hashes matched their provenance. There are four 1 m focus tiles and 252 2 m tiles.
- Unity readback: maximum height error `0.02746582 m`; maximum border discrepancy `0.0274926424 m`, both below the canonical `0.04 m` tolerance. Terrain dimensions, tile placement, collider data, color links, and URP Terrain Lit materials passed the readback checks.
- The generated world remains the enabled Build Settings scene. Death Valley is deliberately separate. Its absence from normal Build Settings does not prevent opening it and pressing Play in the Editor.
- Death Valley deliberately has no production generator, resource placement, or coordinated save service. It is a bounded exploration study, not a complete copy of the production gameplay economy.

## Executed gameplay evidence

The explicit Editor audit uses the actual scene assets, enters Play mode, waits for world startup and measured physics time, and drives simulated keyboard/gamepad devices. It enables background execution and simulated Game-view input routing only for the test, then restores those settings. It does not activate Unity, inject desktop keystrokes, save scenes, or write game saves.

| Check | Generated world | Death Valley with temporary terrain markers |
| --- | --- | --- |
| Booter keyboard movement over 1.2 s physics interval | 4.45 m | 4.31 m |
| Booter gamepad movement over 1.2 s physics interval | 3.64 m | 3.72 m |
| Right-stick camera yaw | 53.4° | 53.4° |
| Grounding after movement | Passed | Passed |
| Inventory opens/closes and input mode returns | Passed | Passed |
| Legger physical displacement | 2.87 m | 7.54 m |

The final three-case audit produced **2 passed, 1 failed, 0 skipped**. The failing case is the **original Death Valley scene**, at the initial grounding assertion. The positive diagnostic does not hide or replace that failure.

The focused existing-system run covered 22 classes plus one native portability check: **160/162 passed**, with the two failures described above. Coverage includes input/foundation, locomotion, slopes/climbing/traversal, inventory/UI, packing, survival, interactions/resources/harvester, companion pathfinding, dust contracts, world identity/coordinates, persistence, and production routing. It is a selected-system result, not a full-suite pass.

Temporary Windows builds completed with zero build errors, and both headless players remained running for approximately 196 seconds without managed exceptions. The generated player reached initial presentation readiness in 3.2 seconds. At the user's direction those build folders were removed; the continuing workflow is Editor-only. These headless runs do not prove visuals or GPU performance.

## Retained local evidence and tools

Ignored `Logs/` holds the GUID/local-reference reports, source/readback JSON, full-suite interruption records, build/startup diagnostics, and test XML:

- `audit-core-systems.xml` — 162 selected existing tests.
- `audit-playability-final.xml` — original scenes plus the in-memory diagnostic.
- `audit-reference-report.json`, `audit-scene-local-references.json`.
- `audit-badwater-source.json`, `audit-badwater-readback.json`.
- `audit-full-suite-progress.log` and interruption JSON.

[RepositoryPlayabilityAudit](../../Assets/_Project/Scripts/Editor/Validation/RepositoryPlayabilityAudit.cs) runs read-only structural scene checks and reports missing ground markers. [BadwaterTerrainReadbackAudit](../../Assets/_Project/Scripts/Editor/Validation/BadwaterTerrainReadbackAudit.cs) mirrors only the read-only methods from the retained isolated terrain builder. [ScenePlayabilityAuditTests](../../Assets/_Project/Tests/Editor/ScenePlayabilityAuditTests.cs) is marked `Explicit` and runs only when a playability audit is authorized. The progress callback is enabled only by the `-repositoryAuditProgress` CLI flag.

## Closeout and next sequence

At this audit's closeout, the original scenes, runtime gameplay code, terrain data, package versions, Build Settings, and legacy assets remained unchanged. Build/test-generated serializer changes and performance-test artifacts were inspected and removed or restored separately from intentional audit tools. Git author identity was configured repository-locally after the user identified themselves as `sleepyseamonster`, matching the restored publication commit. Windows setup and audit documentation formed separate local commit batches. The eight audit C# source/metadata files were retained uncommitted because the original Death Valley test failed. The subsequent authorized seam repair and passing checks are recorded above. No push was performed.

1. Add the missing ground markers to Death Valley's TerrainCollider owners and enforce the contract in its builder/validator.
2. Re-run the original-scene playability check without diagnostic additions, then inspect controls and appearance in the Editor on a physical keyboard/gamepad.
3. Reconcile the decoration-radius contract and platform-specific authoring tests, and isolate the expensive frequency fixture.
4. Review the shader warnings and perform an Editor visual/performance pass before expanding terrain work. No standalone package is needed for this sequence.
