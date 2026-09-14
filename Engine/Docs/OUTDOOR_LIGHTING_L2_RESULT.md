# Wider sun shadows and shared frame timing

Implemented 2026-09-14 inside Engine. This batch connects a useful outdoor rendering improvement to shared runtime measurement. It does not complete the full engine or P32.

## What changed

Both applications use the same optional frame recorder and explicit renderer presentation configuration. A fixed 128-frame ring retains CPU simulation, streaming and draw phases, frame-call duration, delayed GPU frame/view timings, drawable dimensions and resource estimates. JSON is written after the render loop. `ENGINE_PRESENT=vsync|immediate` survives resize; VSync remains the default. No new UI or dependency was introduced.

The sun now uses two stabilized 2048-square tiles in a 4096×2048 atlas. Camera-depth coverage is 24 m near, blending from 21.6 m into a far cascade ending at 128 m in the streamed world or 80 m in local inspection. The final 10% fades. Projection extent stays constant under camera rotation, with center snapping in an origin-anchored light basis. Each cascade selects resident casters using its light frustum, including casters outside the scene camera. Both tiles clear independently, and renderer shutdown owns their resources.

Clamped 3×3 PCF compares against the receiver plane at each shadow texel center. The first low-sun capture exposed broad self-shadow stripes; this correction removed them without increasing bias enough to detach contacts. The UI agent's exact Golden Rock camera/lighting/recipe also renders clean ground with shadows enabled at bias 0.0003. Hard shadow edges still show finite texel resolution; this is not final shadow quality.

Nominal atlas attachment storage is 64 MiB: R32F and D24S8, eight bytes per pixel combined. Coverage uses a bounded 96 m upstream caster allowance and resident geometry only. Tall or unloaded distant casters, nonzero origins, night lighting, temporal filtering, IBL and native Windows rendering remain outside this result.

## Focused evidence

- [CPU fit contract](../Evidence/L2-native/result.json): camera-rotation invariant extent, subtexel snap, frustum coverage and invalid input rejection.
- [Corrected native captures](../Evidence/L2-receiver-plane/captures/result.json) and [pixel comparisons](../Evidence/L2-receiver-plane/captures/comparison.json): low sun, near/far objects, confirmed off-camera caster, camera step and target resize from 2240×1440 to 1920×1280. Shadows darken 10,167 sampled pixels; the off-camera caster accounts for 2,880, with no sampled brightening. Shadowed and Golden Rock images were visually inspected; camera-step stability also has the CPU fit contract, not a subjective motion-quality approval.
- [Golden Rock contact capture](../Evidence/L2-golden-contact/captures/rock.png): original reported ground-line settings, shadows on, clean ground and connected cast shadow. Exact recipe/settings are retained under that evidence directory's `inputs/`.
- [Corrected streaming check](../Evidence/L2-stream-corrected/check/result.json): 18 region retirements, 41 vertex/34 index buffers before and after, and zero sampled differences after returning. No gameplay inputs were simulated.
- Workbench, player, check executable and Metal shaders compiled. Local package executable and shaders match the tested build byte-for-byte. This is Mac technical proof; Windows, user gameplay/feel and final art acceptance remain open.

## Measured baseline and next decision

The synthetic resident workload ran 160 frames per mode without captures or deliberate sleep, retaining the last 128 at 2240×1440. Other desktop activity was not controlled; these are local diagnostic samples, not a benchmark promise.

| Mode | Draw CPU median / p95 | GPU median / p95 | Frame-call CPU median / p95 |
|---|---|---|---|
| [VSync](../Evidence/L2-bench-vsync/frame-trace.json) | 0.013 / 0.031 ms | 1.091 / 4.188 ms | 8.222 / 8.551 ms |
| [Immediate](../Evidence/L2-bench-immediate/frame-trace.json) | 0.012 / 0.032 ms | 1.196 / 5.434 ms | 2.901 / 10.073 ms |

Immediate reduced median API wait but did not improve the tail here. Keep VSync as default. Neither these intervals nor backend GPU timestamps measure input-to-photon latency. This backend returned no per-view timing rows, so individual cascade costs remain unmeasured. Its reported texture-memory field was only 49,152 bytes in this workload and plainly does not include the nominal 64 MiB atlas; do not use that field as total GPU residency.

The [streaming trace](../Evidence/L2-stream-corrected/frame-trace.json) exposes a more important CPU burst: streaming median 0.021 ms, p95 about 53 ms, maximum 68 ms. The phase includes adoption and instance assembly. Source inspection identifies terrain LOD construction/upload and Jolt mesh creation within adoption; this trace does not distinguish their individual costs. The next batch attributes those stages and moves the dominant CPU preparation into bounded jobs, preserving cancellation, byte budgets and ownership. Do not infer a particular culprit or claim that this batch removed the stall.

After that bounded correction, continue shared depth/normal inputs and AO, then height fog, alongside the typed AI-authoring operations in the [runtime priorities](./ENGINE_RUNTIME_PRIORITIES.md). The existing full [master plan](./FOUNDATION_PLAN.md) retains animation, physics, assets, audio, persistence, world streaming and platform work. This is not a decision to build only rendering infrastructure.

## Use and reproduce

Local scene-only package: `out/engine-cascaded-shadows/Launch-Rock-Generator.command`. It contains the tested executable/shaders and a separate editable recipe copy. The user-owned running application was not restarted. Package inventories describe the local mixed checkout and are not an immutable whole-source build attestation; task source/binary receipts and the scoped commit establish this rendering change.

From Engine, build `engine_workbench`, `engine_player`, `engine_outdoor_check` and `engine_sun_tests`, then run `ctest --test-dir build/foundation -R sun_cascade_contract --output-on-failure`. Run `engine_outdoor_check` with shader directory and a new output directory; add `bench` for the fixed resident timing workload. `python3 Tools/check_cascade_captures.py <capture-directory>` verifies the image comparisons. The retained `run/result.json` files contain exact executed commands and input hashes.

For either application set `ENGINE_FRAME_TRACE` to a new file with an existing parent directory, then close normally to export. Unset it to avoid CPU collection. GPU backend profiling remains enabled as before this batch. Trace export errors are reported, never silently treated as a successful evidence capture. Unsupported/invalid GPU times are null, and GPU frame identity is separate from submission identity. The ring is a recent sample window, not a complete session log or a streaming queue profiler.

## World invariants and audit

Lighting/telemetry change no seeds, generation versions, stable IDs, authored constraints or persisted gameplay deltas. Shadow submission consumes current resident transforms; it creates no durable objects and resurrects no unloaded geometry. Renderer/physics handles remain outside world identity. Saved inspection shadow controls retain their document shape. The separate UI agent's scene-only/windowed-fullscreen controls and concurrent rock-generator work were preserved.

Self-audit: corrected a demonstrated shader defect, retained failed first-pass diagnostics only in ignored local output, verified corrected captures and unload/reload, avoided an unmeasured scheduling rewrite, and kept AO/fog/Windows explicitly pending. No dependency upgrade, Unity change, external publication or gameplay smoke test was part of this batch.
