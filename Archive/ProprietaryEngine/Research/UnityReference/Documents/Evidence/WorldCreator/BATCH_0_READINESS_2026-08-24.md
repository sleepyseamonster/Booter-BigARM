# World Creator Batch 0 Readiness Record

Date: 2026-08-24

Status: **PASS — Batch 1 is technically ready but remains unauthorized until the user explicitly begins it**

Authority: Approved World Creator Charter and Architecture Plan

Runtime change gate: Closed

## 1. Decision

Batch 0 is complete.

The World Creator may proceed to Batch 1 through an isolated, additive foundation lane. Batch 1 must not edit, delete, migrate, or consume the current landscape implementation. It may add only pure identity, coordinate, planning-key, and saved-place contract files plus their deterministic EditMode tests.

This is a **go decision for readiness**, not permission to begin Batch 1. Runtime implementation still requires the user's separate authorization.

The final thematic globe-coordinate system and regional map are intentionally unresolved. This is not a blocker. Batch 1 may define only the replaceable `IWorldCoordinateModel` authority seam, precision-safe absolute identities, local-origin conversion, and disposable non-canon test fixtures. It must not define coordinate bounds, projection, wrapping, notation, named regions, regional meanings, or lore.

## 2. Batch 0 authority and stop boundary

Authorized work:

- read-only audit of the current generator, project settings, documentation, and repository state;
- durable recording of the implementation baseline and unavailable evidence;
- classification of current technology as keep, evolve, replace, retire, or protect;
- definition of the Batch 1 ownership, proof, rollback, and performance gates;
- this documentation-only readiness record.

Not authorized:

- runtime, scene, prefab, asset, shader, material, package, project-setting, or saved-game changes;
- deletion or migration of the current generator;
- production coordinate or regional-map design;
- Unity automation, Play Mode, builds, imports, or performance captures while the GUI owns the project;
- gameplay smoke testing.

No prohibited change was made during Batch 0.

## 3. Exact repository and Unity baseline

| Item | Recorded value |
| --- | --- |
| Repository | `/Users/worldbuilder/Desktop/Booter & BigARM` |
| Branch | `main` |
| HEAD | `d9d5fbb7d1024dd70daa3b9f5d31793726027138` |
| Upstream relation | 15 commits ahead of `origin/main` at capture |
| Worktree | Dirty, with 169 status entries: 67 modified, 14 deleted, 88 untracked |
| Ownership interpretation | Existing dirty work is user-owned and protected |
| Unity editor | `6000.4.0f1` |
| Render pipeline | URP `17.4.0` |
| Unity ownership | `Temp/UnityLockfile` present; Unity GUI and import workers active |
| Safe validation state | Static inspection only; no competing batchmode or automated Unity run |

### Baseline hashes

These hashes identify the exact serialized evidence inspected. They are not claims that the dirty files are committed.

| File | SHA-256 |
| --- | --- |
| `ProjectSettings/ProjectVersion.txt` | `8468d7add4c1613c42224053cb9c889688ccf141875f44065a40e6d133bc3ea7` |
| `Packages/manifest.json` | `8307b58ad158052f055aa1dec4a2ad024cd9b452c06b671e5cc71b3104b69754` |
| `Packages/packages-lock.json` | `b79b648431ca767418c54cf50a7ba433db906692749673894e9b66d679b70b2a` |
| `Assets/_Project/Settings/World/TopDown3DWorldSettings.asset` | `ba9fee531a757abc947a62445b5aac59df3e34f7ddb3623c3fe06c27ab77dc80` |
| `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity` | `48bbc85d8f815040cbac5028c45f0d16d4cedaf32232340810e64d609264ff54` |

## 4. Current landscape baseline

The current generator is a valuable tactical prototype and migration reference, but it is not the correct authority to enlarge into the production World Creator.

Current serialized landscape settings include:

- world seed `24681357`;
- terrain generation version `3`;
- natural-object generation version `3`;
- physical-rock generation version `7`;
- resource generation version `1`;
- 18 m realization chunks with 24 quads per axis;
- terrain streaming radius `7`, decoration radius `3`, immediate-load radius `2`, and two chunks built per frame;
- 288 m geology regions with scalar basin, ridge, mesa, drainage, terrace, talus, and local-relief controls;
- monolithic natural-object, rock, landmark, clutter, and resource density controls;
- deposited-dust generation disabled.

The production camera is serialized as perspective with 25 m distance, 48-degree field of view, 50-degree pitch, 40-degree yaw, and 1,300 m far clip. The URP asset uses render scale 1, MSAA 1, HDR, 60 m shadow distance, 4,096 main-light shadow resolution, four cascades, and SRP Batcher. GPU Resident Drawer and GPU occlusion drawer are disabled. `QualitySettings` currently serializes index 0, named `Very Low`; no Development Player measurement exists for that state.

The live path currently combines deterministic sampling with synchronous `GameObject` and mesh realization, queued chunk work, a provisional 2 ms work guardrail, far square-ring reconstruction, scalar geology, and extensive deterministic rock/decorator planning. Several landscape files are modified, deleted, or untracked, including the current generator, far-landscape path, geology profile, surface sample, world settings, chunk builder, procedural world, natural-object planner, and rock-formation planner.

## 5. Technology decisions

| Current technology | Batch 0 decision |
| --- | --- |
| Seed plus explicit generation versions | Keep and expand into versioned `WorldIdentity` domains |
| Deterministic world-coordinate sampling | Keep behind precision-safe absolute addresses |
| Current Unity `float` XZ as persistent geographic identity | Replace before long-range proof |
| Chunk as realization and streaming unit | Keep; do not let chunks author macro geography |
| Required sets, priority loading, unload padding, and profiler markers | Keep and generalize |
| Current instance-owned monolithic generator | Evolve behind a future `WorldCreator` facade |
| Scalar surface sample | Evolve into semantic surface, volume, and affordance queries |
| Scalar geology, layered-noise macro authoring, sine drainage, and ellipse stamps | Replace with causal plans and grammars |
| Scalar heightfield for ordinary ground | Keep as the efficient base of a hybrid representation |
| Synchronous mesh allocation and realization | Replace incrementally with immutable build results, pooling, cancellation, and bounded integration |
| Destroy/rebuild far square rings | Replace with cached clipmap or HLOD representations |
| Deterministic cell candidates and neighbor competition | Keep as constrained low-level primitives |
| Rock formation plans and stable root keys | Evolve into feature genealogy, strata, silhouette grammar, and novelty ownership |
| Editor-baked rock families and LOD assets | Keep as migration assets; asset count alone cannot solve repetition |
| Runtime native CSG avoidance | Keep |
| Current monolithic world settings asset | Split only in later proven migration batches |
| Resource stable IDs and delta-only persistence | Keep and generalize as the persistence model |
| Current landscape runtime, scene, settings, generated assets, and test edits | Protect as user-owned until an authorized cutover batch |

No existing runtime file is approved for deletion. A “replace” decision means build the new authority additively, prove it, migrate consumers atomically in a later authorized batch, and only then review removal.

## 6. Evidence availability

### Available

- exact source, package, scene, settings, and worktree inspection;
- serialized camera, URP, quality, chunk, version, geology, density, and streaming values;
- source-level lifecycle and profiler-marker inspection;
- earlier documentation that records successful landscape validation and focused EditMode checks for the current tactical lane;
- a development-only stress profile capable of logging CPU/GPU timing, resolution, memory, chunks, renderers, and colliders.

### Explicitly unavailable in this pass

- a current fixed-camera World Creator baseline panel;
- a current Development Player capture;
- current average, percentile, one-percent-low, CPU, GPU, memory, allocation, draw-call, triangle, or traversal-spike measurements;
- current Play Mode or hands-on gameplay proof.

The missing runtime evidence is recorded rather than inferred because the Unity GUI owns the project. Earlier source checks or EditMode results are not treated as present-day visual, gameplay, or player-build proof. Batch 0 permits explicitly recorded unavailable evidence; later batches must produce it at their own gates.

## 7. Target mid-range proof computer

The first World Creator performance contract targets this class of Windows desktop:

- Windows 11 x64 and DirectX 12;
- six-core/twelve-thread CPU in the Ryzen 5 3600 or Core i5-10400 performance class or better;
- DirectX 12 GPU in the GeForce RTX 3060 or Radeon RX 6600 performance class or better, with at least 8 GB VRAM;
- 16 GB system RAM;
- SSD installation;
- 1,920 x 1,080 output;
- the production **World Creator Medium-High** proof preset;
- 60 frames per second traversal target.

This class is a deliberate proof target, not a claim that those exact parts will be the final minimum specification. Steam's current public [Hardware & Software Survey](https://store.steampowered.com/hwsurvey/) is market context; acceptance still requires a real Development Player on a representative machine.

The development host is a 2021-class MacBook Pro with Apple M1 Max, 10 CPU cores, 24 GPU cores, and 32 GB unified memory. It is useful for development and comparative profiling but **does not satisfy Windows mid-range acceptance proof**.

## 8. Provisional performance acceptance contract

These gates apply to the bounded Fractured Transect vertical-slice proof, not to Batch 1 pure contracts.

After shader compilation, cache warmup, and the declared initial spawn gate:

- average frame time at or below 16.67 ms during sustained traversal;
- one-percent-low performance at or above 50 fps;
- no recurring World Creator-caused frame above 33.3 ms during steady travel;
- no World Creator-caused stall above 50 ms after warmup;
- main-thread World Creator planning, integration, upload, and physics admission at or below 2 ms per frame, except a separately reported initialization gate;
- zero recurring managed allocation from steady-state World Creator traversal and no recurring garbage collections caused by it;
- graphics-memory use at or below 6 GB on an 8 GB target card and total process resident memory at or below 8 GB on a 16 GB system during the proof route;
- stable feature identity and traversal topology at every quality level; scalability may reduce distance, proxy resolution, materials, shadows, dust, and cosmetic density only;
- no seam hole, duplicate representation, identity swap, or saved-place movement during streaming or local-origin rebase.

The proof route must use a non-Editor Development Player and sustain traversal through near/mid/far transitions for at least ten minutes or the full 3,000 m proof transect, whichever is longer. It must report CPU/GPU frame time, frame percentiles, allocations, memory, draw calls, triangles, renderers, colliders, cache behavior, cancellation, and worst streaming-boundary spikes.

If representative target hardware is unavailable when the slice reaches its performance gate, the result is **not proven**, not a pass inferred from the development Mac.

## 9. Batch 1 protected and allowed ownership

### Protected existing paths

Batch 1 must not change:

- `Assets/_Project/Scripts/Runtime/TopDown3D/` outside the new `WorldCreator/` child;
- `Assets/_Project/Scripts/Editor/TopDown3D/` outside a future separately authorized `WorldCreator/` child;
- existing tests, asmdefs, assembly-info files, scenes, settings, generated assets, shaders, materials, packages, or project settings;
- the current saved-game writer, reader, or serialized save format;
- any live landscape document already modified or untracked by the user.

### Allowed new roots

Batch 1 may add task-owned files only beneath:

```text
Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/
  Identity/
  Coordinates/
  Planning/
  Persistence/

Assets/_Project/Tests/Editor/WorldCreator/
```

Both roots were absent at Batch 0 capture, and no conflicting `WorldIdentity`, `WorldVersionManifest`, `IWorldCoordinateModel`, `AbsoluteWorldPosition`, or `SavedPlaceRecord` type was found. New runtime files will remain in `BooterBigArm.TopDown3D.Runtime`; new Editor tests can use the existing `BooterBigArm.Editor.Tests` reference without an asmdef edit.

Batch 1 must create and stage only its own new source files and Unity `.meta` files. Any unexpected auto-generated or overlapping edit is a stop condition.

## 10. Batch 1 proof and rollback contract

Batch 1 must prove:

- identical seed, version manifest, namespace, and absolute address produce identical identities and plan keys regardless of request order;
- domain-separated seed namespaces do not collide through accidental reuse;
- stable feature IDs and plan keys are independent of transient `GameObject`, chunk-load order, local float position, and runtime generation tokens;
- far-address values retain exact identity beyond ordinary Unity-float precision;
- local-origin rebases change only temporary Unity-local positions, never canonical address, plan key, feature ID, query identity, or saved-place record;
- immutable records serialize and deserialize without changing canonical identity;
- incompatible topology or coordinate-model versions reject atomically with a typed reason and without changing the live save file;
- ownership and halo rules have one canonical owner and deterministic boundary keys;
- pure tests pass in different build and request orders.

The release-era policy for pinning or migrating old worlds remains user-owned. The Batch 1 development default is strict version compatibility and explicit rejection; it must not silently reinterpret an old place.

Rollback is deletion or reversion of the new, unused Batch 1 files and their `.meta` files. No production consumer, scene, asset, package, setting, or save file may depend on them, so rollback does not require restoring the current generator.

## 11. Go/no-go conditions

### Go

- the charter and architecture are approved;
- the coordinate design remains open behind one replaceable authority seam;
- current landscape work is protected by a non-overlapping new-file boundary;
- the exact repository and serialized baseline is recorded;
- missing visual and performance proof is explicitly identified;
- the target hardware class, resolution, proof preset, frame target, proof route, and provisional budgets are defined;
- Batch 1 proof, rollback, and save-version behavior are defined.

### Stop immediately if

- Batch 1 requires an edit outside its allowed new roots;
- Unity creates or modifies unexpected existing assets during import;
- a type-name, serialization, assembly, or namespace conflict appears;
- the worktree ownership changes or task files overlap another active lane;
- implementation starts to define the user's future globe, regional map, coordinate lore, or named regional meanings;
- a test requires changing the live save path or production consumer;
- Unity validation would require a competing process while `Temp/UnityLockfile` is present;
- deterministic identity cannot remain independent of Unity-local floating-point position.

## 12. Approval boundary

**Batch 0 approval: PASS.**

**Batch 1 readiness: GO, awaiting separate user authorization.**

This record does not approve Batch 2, terrain generation, site generators, runtime cutover, deletion of the old generator, production coordinate design, or any claim of visual/performance success.
