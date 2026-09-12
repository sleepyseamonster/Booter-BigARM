# Outdoor lighting implementation sequence

Prepared and self-audited 2026-09-11 from [source-backed research](../Research/OUTDOOR_LIGHTING_RESEARCH.md) and [cost/source data](../Research/outdoor-lighting-data.json). **Planned, not implemented.** This is a bounded extension of P02/P04/P05 and preparation for P32; it does not complete P32 or remove its whole-world integration prerequisites. [FOUNDATION_PLAN](./FOUNDATION_PLAN.md) remains the master roadmap.

## Outcome and limits

Make the current open-wasteland rocks readable under a coherent sky/sun, preserve bright surface detail, ground formations with useful shadows/ambient occlusion, and give distant terrain aerial perspective. Use existing rock/ground assets and renderer. Keep the first working pass small; do not block it on GI, weather, a deferred renderer, origin shifting or perfect material authoring.

The research task delivers this plan and source record only. Implementation should follow these coherent batches when work resumes. Preserve concurrent geometry/terrain work and refresh source hashes before editing shared renderer files.

## Ordered batches

| Batch | Implementation and owned seam | Completion evidence |
|---|---|---|
| L1 — Sky and display | Add an engine-owned environment settings record with defaults, sun elevation/color, coupled sky/ambient colors and fixed exposure. Add sky background and named SDR tone operator in Rendering/shaders. Preserve diagnostic and UI composition paths. | Known neutral/bright material and normal diagnostic comparisons; saved settings reload; shader build and one bounded Metal technical capture set |
| L2 — Sun coverage | Replace single shadow region with two stabilized atlas cascades, clamped PCF, bias controls, transition/far fade and receiver/caster bounds. Extend named views and resource ownership. | Near/far grounded rocks, low sun, camera motion and off-camera caster; cascade/coverage debug images; atlas allocation/caster counts; resource replacement check |
| L3 — Shared depth and AO | Add static/skinned prepass and explicit depth/normal encoding adapter. Integrate fixed-quality pinned bgfx ASSAO evaluation and spatial filter. Resolve AO into shared RG visibility; apply to ambient in forward shading. Neutral fallback on unsupported optional features. | Depth/normal/AO views; convex surfaces, crevices and separated objects; AO-off restores baseline; moving geometry has matching depth; measured view cost and resize cleanup |
| L4 — Height fog | Add bounded analytic height fog from environment settings, linear-HDR composition and matching horizon. Prefer shared inline surface/sky evaluation to avoid an unnecessary HDR ping-pong target. | Zero-density identity, horizontal-ray finite limit, near/far and elevated views, sky horizon; fog-off retains visible streaming/coverage diagnostics |
| L5 — Contact refinement, conditional | Only if L2/L3 leave visible near-contact gaps: short depth rays toward the sun, packed in visibility G, limited thickness/length and screen-edge fade. Leave neutral/off if benefit is not demonstrated. | Same contacts with effect on/off; screen edges and off-screen occluder limitations recorded; direct-only composition; incremental cost |

L1 establishes a fair material baseline before evaluating darkness. L2 is the main grounding improvement. L3 is the only new screen-space infrastructure batch. L4 does not require L3 inputs when evaluated from existing surface positions; it may move ahead if AO integration is blocked. L5 is optional, not an excuse to delay a usable first pass.

## Shared ownership and procedural behavior

- **World identity:** lighting settings never change seed, generator versions or generated IDs. Do not regenerate rocks to change lighting.
- **Streaming:** draw only valid resident resources; shadow selection includes relevant casters beyond the camera frustum. Keep visible shadow range bounded by actual residency. Destroy per-frame/effect resources through renderer ownership, not region identity.
- **Stable object identity:** prepass, shadow and forward submissions derive from the same object/transform snapshots, including skin poses. Effects do not create persistent world objects.
- **Authored constraints:** environment defaults are presentation settings, not new biome/geography canon. Preserve existing material channel meanings and camera controls.
- **Persisted deltas:** store versioned inspection/environment settings separately from generated world deltas. Old documents receive explicit defaults; world saves and removed-rock IDs remain untouched.
- **Coordinates:** use the same current local frame for all passes. Fog reference height must be converted consistently if P22 later rebases coordinates. This bounded pass makes no nonzero-origin support claim.

## Smallest useful verification and stopping point

Use the existing workbench/reference scene with a neutral surface, rough rock, ground contact, a separated object, an animated object and near/far formation. Reuse existing data and technical capture facilities. Record sun/exposure/camera, drawable size, backend, source revision, resident geometry, effect settings and which captures are inspected. Native Windows execution remains an explicit missing result until hardware is available; Mac remains the development host.

Check only changed contracts: numerical color/fog limits where appropriate, changed shader compilation, resource ownership on resize and the listed effect comparisons. One capture/measurement pass per integrated batch is sufficient unless it reveals a defect. No gameplay smoke tests or broad stress campaigns. User visual/feel acceptance remains separate from numerical/build proof.

Initial quality candidates: two 1024 or 2048 shadow tiles; shadow distance bounded by residency; one spatial AO preset; fog enabled; contact optional. Record actual resource allocations and GPU timestamps rather than borrowing reference performance figures. If the budget fails, reduce shadow resolution/range or AO resolution before adding temporal infrastructure. No final PC performance target is declared by these candidates.

Stop when L1–L4 work together in the existing workbench with repeatable settings, honest limitations and one focused evidence record; L5 may remain deferred. Return attention to the rock/terrain visual-method gaps after this coherent lighting pass.

## Self-audit and rewrite decisions

The initial feature list was revised against the renderer's actual seams:

1. Separate AO/contact post-effects became a shared prepass plus packed visibility, because final HDR already combines direct/ambient and layered rocks use 14 texture slots.
2. A broad sky/IBL/atmosphere effort became a coupled authored sky/ambient first pass; specular IBL remains a named future gap.
3. A direct XeGTAO port became reuse of pinned bgfx ASSAO first. Its scratch allocations and depth conventions must be adapted, not assumed cheap or drop-in.
4. Multiple independent shadow maps became one two-tile atlas with explicit guard/filter and off-camera caster handling.
5. Contact shadows became conditional. They cannot replace complete shadow coverage and should not compound AO darkness.
6. Total memory claims became explicit nominal attachment models; performance remains unmeasured. The existing generic CPU/frame diagnostics are insufficient to attribute GPU effect cost.
7. P32 remains planned. This local visual improvement does not claim world-origin integration, production rendering or Windows readiness.

The audit leaves no need for another broad research phase before L1. Resolve licensing/pinning for any copied new code within its consuming batch; retain existing bgfx notices for reused examples.
