# First-pass runtime foundation

Implementation batch P01/P02/P07, 2026-09-10. This advances the [master plan](./FOUNDATION_PLAN.md); it is not the complete engine. The [status page](./STATUS.md) and [package index](./ENGINE_ROADMAP.json) distinguish completed packages from the remaining program.

## Shared data and ownership

`engine_core` has no SDL, bgfx or ImGui linkage. It owns bounded documents, content-path confinement and world identity/coordinate primitives. `engine_state` adds the current inspection document and geometry. The workbench is the first consumer; a Game application and fixed-step simulation remain P08–P13 work.

- Units are metres/seconds, right-handed Y-up. `WorldPosition` uses signed 64-bit X/Z region addresses and double local coordinates. Region span is explicit configuration, not final geography. Normalization uses floor semantics at negative boundaries. Conversion to float requires an explicit local working radius and rejects out-of-range positions; nearby large region addresses retain precision.
- `GeneratedId` is a versioned structural string containing generator namespace, world seed, generator version, region and member identity. It remains independent of load order and transient resources. Generators must assign member identities deterministically; this primitive does not implement a generator or its stable-member policy.
- Authored inspection settings are typed, versioned inputs. Loading parses a separate candidate, rejects unsupported fields/types/ranges and only then replaces live state. Future generated base data and persisted runtime deltas will have separate document kinds and ownership; inspection JSON is not a player save format.
- Unload/reload cannot change a structural ID. Region residency, request epochs and late-job rejection are still later implementation packages; this batch creates no always-loaded production world or fictitious streaming support.
- Source art remains under `Assets/SurfaceLibrary`; derived cook output belongs in ignored build/output directories until promoted by a catalog. `contentPath` accepts portable relative references, rejects traversal and checks resolved containment. Runtime paths must not point back into source caches.

Documents use the envelope `{kind, version, payload}` with a 1 MiB byte limit, 32-level nesting limit and duplicate-key rejection. Inspection version 1 includes camera, shape/scale, sRGB color, light and exposure. Unsupported versions fail explicitly; migrations are not inferred. Writes validate before touching disk, create an exclusive sibling `.writing` file, flush it and atomically replace the destination. A concurrent writer or stale staging file produces a useful error and preserves the prior document. This is single-document replacement, not a multi-file transaction, crash recovery service or power-loss guarantee. P13/P21 own those later requirements.

The coordinate audit also covers division underflow at a negative boundary: a subnormal negative local value must stay in the prior region. [Focused tests](../Evidence/M1-boundary-tests/result.json) pass after that correction.

The selected parser is [nlohmann JSON 3.12.0](https://github.com/nlohmann/json/releases/tag/v3.12.0), pinned at `55f93686c01528224f448c19128836e7df245f72` in [the runtime lock](../Research/runtime-lock.json). The original experiment lock is preserved. Both locks use the same archive/tree verification path; the [MIT notice](../Research/Licenses/nlohmann_json.txt) is retained. Engine code owns validation and replacement semantics.

## Linear scene and display boundary

The existing renderer now renders geometry to a linear RGBA16F color target with D24S8 depth. User colors are decoded from sRGB before lighting. A fullscreen pass applies explicit exposure and exactly one sRGB encoding into the SDR swapchain. Values above 1 survive in the scene target; the display pass clamps after exposure/encoding. This is a neutral SDR baseline, not a final filmic tone mapper or HDR monitor output implementation.

View 0 owns scene output, view 1 owns display conversion, and view 2 draws the display-space inspector. Normal diagnostics bypass exposure/display encoding so the established geometry oracle remains valid. The renderer restores framebuffer bindings after window reset and replaces framebuffer attachments during resize. Target support is checked before allocation; unsupported devices fail explicitly.

[Native Metal proof](../Evidence/M1-final-verification/result.json) measured exact expected gray values for eight bands through the actual scene target, including linear 0.5 → display 188 and linear 2 at −2 stops → display 188. The latter rejects clipping HDR values before exposure. The same values survive repeated target replacement; an opaque inspector title pixel stays unchanged under exposure. Geometry comparisons retain 7,983 visible normal samples and zero sampled disagreement with baked geometry, unculled geometry and reverse draw order. Opposite-face culling changes output. Thirteen captures, resize, inspector/camera event injection and cleanup pass.

The initial run exposed a reset bug that left the postprocess sampling an unbound scene target after resize. Its [failed capture run](../Evidence/M1-native-01/run/result.json) is retained; the independent pixel oracle rejected it despite the application's event checks passing. The corrected native receipt above supersedes it. Neither run is gameplay/physical-input/Windows proof. Six color-oracle tests reject absent scenes, double encoding, early HDR clipping, UI exposure leakage and lost resize targets.

## Portable local package

[Run instructions](./RUN_FOUNDATION.md) cover presets, source preparation, technical verification and local packaging. The workbench uses executable-relative `Shaders/` by default and accepts explicit `--shaders`, `--inspection` and `--save-inspection` paths. `--build-info` prints engine version, dependency cohort and compiler; the package manifest and command receipts supply exact binary/source hashes. The application does not write settings unless an output path is supplied.

[The fresh native build](../Evidence/M1-clean-build-final/result.json) completed in 394.94 seconds from a new build directory, using the verified source cache. [The final local package manifest](../Evidence/M1-package-final/package.json) binds its 15 installed files to source hashes. The initial build command ran before configuration had completed and failed without compiling; its receipt is retained and superseded by the successful build.

`package_workbench.py` installs into a new Engine-local output directory and records payload/source hashes. It does not publish anything. Source caches are build inputs only; the installed runtime contains the executable, shaders and retained primary notices. Complete transitive shipping clearance and native Windows execution remain separate gates.

The next substantial work is P03 semantic texture cooking/catalog/residency and P04 bounded sun shadows, converging on P05 materials. P08 simulation can proceed once the portable core/build boundary is complete. Do not reopen color or document polish unless a new consumer exposes a concrete defect.
