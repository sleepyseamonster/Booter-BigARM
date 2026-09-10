# Texture and Material Implementation Plan

Prepared 2026-09-10 from the [texture research](../Research/TEXTURE_SYSTEM_RESEARCH.md) and TEXTURE-001 evidence. T1/T2 are now implemented; see [the texture pipeline result](./TEXTURE_PIPELINE_RESULT.md). Remaining stages are planned. The [outdoor rendering plan](./OUTDOOR_RENDERING_PLAN.md) remains the milestone authority.

## Sequence and done condition

Scheduling update 2026-09-10: [the whole-engine master plan](./FOUNDATION_PLAN.md) controls implementation. P01 and OR-2/P02 are now complete on Mac; [the first-pass result](./FIRST_PASS_RUNTIME.md) fixes the linear/HDR/display convention for P03. T1/T2 map to P03, T3 to P05, compression to the measured quality gate, and T5 to rock/world integration. The offline cooker can proceed once its color convention is fixed, independently of shadow implementation; it must not be mistaken for completed GPU materials. No Windows workstation switch is required to start.

OR-4 texture support is done when one transferred rock material and one ground material render through shared, validated texture resources with correct color/channels/normals, complete mipmaps, bounded lifetime behavior and reproducible inspection evidence. This does not require full Unity appearance parity or all 38 images loaded together.

## Stages

| Stage | Concrete work | Required evidence / stop boundary |
|---|---|---|
| OR-2 prerequisite | Linear scene values; one display conversion; explicit exposure and UI composition. Document whether an HDR intermediate is used and its fallback; keep numerical calibration independent of tone mapping. | Known values: sRGB byte 128 decodes to approximately 0.21586 linear; 0.5 linear displays at approximately 188; unlit round trip within quantization tolerance. Check double conversion, scene/UI boundary and resize on the actual Metal path. No texture or PBR completion claim. |
| T1 — Cooked texture contract | Introduce versioned import recipes, stable asset IDs, deterministic content keys, explicit role/channel/transfer declarations and engine-owned semantic mip generation using existing bimg primitives. Write uncompressed KTX 1, preserve original dimensions and source files. | Independent CPU reference checks for sRGB/data/alpha/vector averaging; odd dimensions, 1×N/N×1, periodic borders, zero-vector normal fallback and complete mip ranges. Reject mismatched roles, unsupported metadata and invalid dimensions. Two unchanged cooks match; source or recipe changes invalidate the key. Stop before compression. |
| T2 — Runtime texture resources | Bounded 2D KTX validation and upload, reference-counted shared residency, generation-checked handles, byte accounting, role fallbacks and failure-preserving replacement. Keep all GPU calls in the renderer owner. | Truncated/oversized/malformed payload rejection before upload; incorrect sRGB data rejected; same asset requested twice shares storage; release one owner keeps it alive; final release and repeated replacement restore counts. Demonstrate stale generation rejection and clean shutdown. Capture UV/channel/mip diagnostic fixtures. |
| T3 — First materials | One opaque material definition, constants and texture bindings; initial UV/tangent contract; normal strength; explicit roughness mapping; ambient/environment contribution and fixed inspection exposure. Load one documented rock binding set and one ground set. | Calibration plane/sloped solid, asymmetric normal orientation, channel isolation and mip-distance checks. Compare material response under neutral and low sun, with backend/resolution/exposure recorded. Retain authored binding choices. User judges artistic result separately. |
| T4 — Compression gate | Use project-built semantic mip chains; evaluate existing offline encoders for BC7 color/packed, BC5 normal and BC4 scalar; choose and record explicit resolution policy. | Compare every decoded mip to uncompressed reference. Record per-channel errors, normal angular errors and oblique/distance captures. Query native GPU format support; measure resident/upload bytes and conversion time. Do not accept global RGB error alone for data maps. Thresholds belong to the evaluated asset profile and require recorded rationale. |
| T5 — Procedural material seam | Add triplanar projection, authored layer constraints and deterministic material variation when rock geometry work begins. | Adjacent chunk phase, origin shift, nonuniform scaling, mirrored faces, object movement and unload/reload keep intended placement and identities. Profile actual sampler/bandwidth use before adding all reference layers. |

T1–T3 are the initial uncompressed path. T4 precedes scaling the visible library but is not a prerequisite for the first textured fixture. T5 belongs to the rock workbench integration and does not expand OR-4 into a terrain generator. OR-5 continues to own saved inspection settings and repeatable measurements.

## Proposed data boundary

The following fields describe a contract to implement and validate, not an existing runtime schema:

- `TextureImportRecipe`: schema/filter versions, stable logical asset ID, Engine-relative source, source hash, semantic role, transfer function, channel mapping, alpha use, normal encoding/green orientation, dimension policy, edge behavior, target format and profile.
- `CookedTextureRecord`: recipe/content hashes, tool and dependency revisions, output hash/path, exact dimensions/format/mip offsets and payload sizes. Reject disagreement between record and container.
- `MaterialDefinition`: schema/version and logical ID; texture asset references; UV/projection mode and scale; base-color factor, roughness mapping, normal strength, AO contribution and height scale/bias. No bgfx handles or Unity GUID dependency.
- `TextureResource`: generation-checked handle, state, owners, resident/upload byte counts, content key and last error. GPU identifiers remain renderer-private.

Sampler state is explicit at material binding: repeating linear/trilinear for ground/rock, with anisotropy enabled only through supported settings; diagnostic point/clamp modes are separate. Include filtering/wrap policy in import identity when it changes mip generation, while allowing runtime sampler choices to share compatible image storage.

Initially use synchronous loads with a strict per-asset/total byte limit configured for the fixture. Reject work before allocation when it exceeds those limits. Add worker decode and upload budgets only when measured stalls justify them. Keep the API ready for pending-request cancellation and chunk owner epochs; do not build a general streaming service in T2.

## Procedural and persistence responsibilities

World identity and generated-object IDs do not derive from GPU handles, source filenames or cook order. Deterministic recipes select stable material IDs and seeded variation. Shared textures survive while any chunk/material owner needs them; an unloaded chunk cannot publish a late replacement. Authored constraints control layer selection, physical scale and permitted variation. Persisted runtime deltas store logical material/instance changes, not decoded pixels or renderer state. Reload reconstructs derived residency from those references.

## Verification and authority

Use [the texture SOP](../SOPs/COOK_AND_VERIFY_TEXTURES.md), preserve source hashes and retain source-bound receipts. For each implementation stage, commit only its reviewed work after relevant checks. Do not launch Unity or run gameplay smoke tests. Technical captures are distinct from user-owned artistic/feel acceptance.

Repeat T2/T3 upload, color/normal, sampler and resource checks on native Windows with the selected backend and compiler. Record device/feature capabilities and shader compilation. A Mac build or container parse never satisfies this checkpoint. Introduce that check during outdoor rendering when hardware is available; keep Mac daily development while practical.

Deferred: full layered shader parity, terrain/pebble parallax, automatic texture streaming, virtual textures, texture painting, general material graph tooling, final target-PC budgets, canyons and gameplay.

## Plan audit

The research closes the initial tool/format uncertainty without changing the selected stack. Stock mip generation is specifically excluded because of measured semantic failures. Source images remain immutable; recipes and content keys make conversions reviewable. The first renderer slice has one rock and one ground material, avoiding a premature port of every layered shader. Each stage has independent numerical/resource evidence and an explicit stop point. Current implementation status: OR-1, OR-2/P02 and P03/T1/T2 are complete on Mac; PBR materials remain pending.
