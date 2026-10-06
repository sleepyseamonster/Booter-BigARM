# Badwater gameplay terrain plan

**Status:** Audited plan, 2026-10-06. No runtime loading change is approved by performance evidence yet.

## Goal and boundary

Make the existing four-slice Badwater scene a reliable **playable geographic terrain study** with Booter and Legger, while keeping its measured elevation and tile identities. This bounded 4.096 × 4.096 km scene is not a replacement for the effectively infinite production World Creator. The production scene, generator, and save system remain separate until a later integration decision.

The current [scene record](./Agents/Blender/studies/DeathValley/UNITY_BADWATER_SCENE.md) is the source for geographic bounds, source data, validation, and current limits. [WORLD_SYSTEMS_STANDARD.md](./WORLD_SYSTEMS_STANDARD.md) controls deterministic identity, chunk life cycle, collision readiness, near/middle/far representation, and saved deltas. [TOP_DOWN_3D_LANDSCAPE_BENCHMARK.md](./TOP_DOWN_3D_LANDSCAPE_BENCHMARK.md) supplies the existing 2 ms per-frame terrain work guardrail. Live scene assets and a Development Player profile decide performance claims.

## Audit of the earlier streaming recommendation

- **Confirmed:** The scene has 256 separate 256 m Unity Terrain objects and 256 TerrainColliders. Four adjoining focus tiles use 1 m samples; the other 252 use 2 m samples. All are currently loaded together. The four focus tiles are a *resolution tier*, not the only four terrain objects.
- **Correction:** A count of 256 tiles does not prove that full asset streaming is needed. Unity Terrain already reduces distant geometry according to its terrain settings. The saved scene uses a heightmap pixel error of 8 and instanced drawing, but no Development Player frame-time, memory, or boundary-crossing measurement has established its actual cost.
- **Correction:** Disabling renderers or colliders does not unload the TerrainData and textures referenced by this scene. True asset-memory streaming requires an asset ownership/loading boundary and an unloadable dependency path; dividing this 4 km scene into 256 additive scenes is not the first choice.
- **Risk:** Keeping all 256 colliders active may be unnecessary for gameplay, while switching them off without a prepared collision ring could drop Booter or Legger at a tile edge. The existing production world has collision-readiness logic; the Badwater scene does not.

## Chosen method

Use a **profile-first, staged hybrid** for this bounded slice. Keep the complete terrain visible and its source TerrainData unchanged for now. First measure a standalone Development Player. If collision has a material cost, activate colliders around **both** Booter and Legger with prefetch, a ready-before-crossing rule, and unload hysteresis. Do not disable distant visual terrain merely to reduce object count; the valley and mountain horizon is part of the requested experience. Escalate to true asynchronous asset streaming only if measured memory, startup, or traversal cost still fails the target after narrower terrain and collision tuning, or when the geographic area grows substantially.

For eventual production integration, preserve `rNN_cNN`, source bounds, and source-version identity as immutable geographic inputs. Attach any generated content and mutable state to stable world/chunk IDs, with saved deltas outside TerrainData and scene objects. Use the production near/middle/far world-query and streaming path rather than introducing a parallel gameplay world manager in the Badwater scene.

## Implementation sequence and gates

| Step | Work | Proof and decision |
| --- | --- | --- |
| **1. Reproducible baseline** | Keep the current scene intact. Record Unity version, machine, quality/resolution, camera position, and terrain settings. Make a Development Player profile for startup, basin traversal, focus-tile borders, and a mountain view. Capture CPU/GPU frame time, spikes, memory, draw calls, active terrain/collider counts, and build/startup cost. | Distinguish Editor-only slowdowns from Player cost. Establish a fixed target and budget against the project landscape benchmark before changing loading policy. A cold run and a repeated run should be comparable. |
| **2. Narrow tuning** | If needed, tune the existing Terrain LOD, far basemap distance, texture resolution/import settings, and visibility range one variable at a time. Preserve the geographic material reference and DEM samples. Compare identical fixed views for horizon and texture degradation. | Accept only measured improvement without visible gaps, popping, or a materially degraded mountain silhouette. Revert a setting that fails the view comparison. |
| **3. Collision window, conditional** | If the profile shows meaningful physics/startup cost from 256 active colliders, add a Badwater-specific runtime controller over existing tile IDs. Pre-enable a small neighborhood around each traversing actor, retain a wider unload radius, and never retire a tile needed by either Booter or Legger. Keep new colliders ready before actors enter them; handle teleport/spawn explicitly. Do not alter the production generator or source TerrainData. | Focused non-gameplay checks verify tile lookup, union of both actors' neighborhoods, boundary and corner cases, enable-before-disable order, and repeatability. User-owned Play Mode traversal then checks that neither actor falls through or stalls at tile borders. A repeated Development Player profile must show a real gain without a frame spike. |
| **4. Asset-streaming gate, conditional** | If memory/startup or expanded coverage still fails, build a versioned tile catalog from the existing source manifest and load terrain content asynchronously in bounded regions. Keep a coarse visible fallback until fine content and required collision are ready; use prefetch and unload hysteresis. Select the asset delivery method only after measuring packaging and dependency cost. | A cold Player run shows lower peak/steady memory or startup cost, no holes or collision gaps at crossings, no orphaned assets after unload, and stable source IDs. The existing 2 ms/frame terrain-work guardrail remains in force unless a controlled profile justifies a change. |
| **5. Production-world decision** | Decide whether the geographic slice stays a separate reference/playable scene or becomes a bounded authored influence in World Creator. Map source elevation, geography, materials, traversal constraints, and stable IDs to the production world-query contract. | Seed/location replay, chunk unload/reload, Booter and Legger traversal, near/far agreement, and save/restore of runtime deltas are proven before calling it production gameplay terrain. No change to first-playable geography is implicit. |

## Stop conditions and exclusions

The immediate implementation stops after Step 1 if the bounded scene meets the agreed gameplay target. Do not add Addressables, hundreds of additive scenes, a second world generator, new game assets, rock scatter, terrain resampling, or broad project-setting changes merely to make the tile count smaller. Steps 2–4 happen only for a demonstrated bottleneck. Step 5 requires its own integration review because the geographic study and production world have different ownership and persistence models.

This plan does not claim current Play Mode feel or Player performance. Repository and isolated-editor validation established scene structure, terrain seams, and geographic source alignment; interactive traversal and visual acceptance remain user-owned under the repository working agreement.
