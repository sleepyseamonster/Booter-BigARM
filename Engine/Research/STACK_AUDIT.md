# Foundation Stack Audit

Recorded 2026-09-09, America/Phoenix. Scope: preparation and EXP-001. The [foundation survey](../Docs/PROPRIETARY_ENGINE_FOUNDATION_RESEARCH.md) contains the broader comparison; this audit tests the first candidate against concrete integration evidence. Production technology decisions remain proposed in [DECISIONS.md](../Docs/DECISIONS.md).

## Recommendation

Continue evaluating C++20, CMake, SDL3, bgfx and Dear ImGui with a small GPU fixture before selecting the production stack. Own our engine data, authoring workflow, resource lifetimes and world logic. Use third-party libraries for bounded services where they meet the game requirements. Writing every graphics backend or collision primitive is not necessary to own the engine.

The next decision is whether this cohort supports an efficient mesh/shader/inspector workflow, followed by native Windows execution. Broadening the framework or adding physics now would not answer that question. Mac work should use supported library paths; if a major platform fork becomes necessary, record the blocker and move the experiment to Windows.

## Exact Source Cohort

All revisions and archive SHA-256 values are in [probe-lock.json](./probe-lock.json). The source-preparation tool verifies archive and extracted content. Sources were retrieved directly from their upstream repositories; no global packages were installed.

| Component | Inspected primary source | What this experiment uses |
|---|---|---|
| SDL 3.4.16 | [Pinned SDL source](https://github.com/libsdl-org/SDL/tree/fa2c02bb6e21974a89ea9824bc53c9932abe5f9c) | Static library and synthetic event delivery |
| bgfx.cmake | [Pinned wrapper and submodule entries](https://github.com/bkaradzic/bgfx.cmake/tree/7cea9ab17c6dc8745cdc5df992145c514a129abb) | CMake integration with that commit's bgfx/bx/bimg revisions; examples, tools and install disabled |
| bgfx | [Pinned public API](https://github.com/bkaradzic/bgfx/blob/9b636df330c81e11c84595651a291b6c59fb7396/include/bgfx/bgfx.h) | Noop initialization, vertex-buffer allocation/destruction, frame and shutdown; current initialization uses `swapChain` |
| Dear ImGui 1.92.9b | [Official release](https://github.com/ocornut/imgui/releases/tag/v1.92.9b), [pinned source](https://github.com/ocornut/imgui/tree/f1cc2ae15e53a861a874c3034aae6798fde194ab) | Standalone core, font atlas and CPU draw data; no renderer/platform adapter |

The [dependency inventory](./dependencies.json) separates this experiment from proposed later libraries. Jolt, GLM, Tracy, fastgltf, meshoptimizer and vcpkg have not been installed, pinned or integration-tested by this package. Ninja is absent on the current host; the experiment uses CMake's available Makefiles generator. That is a bounded local choice, not a production build-system decision.

## Findings From Actual Builds

| Receipt | Result and interpretation |
|---|---|
| [Initial configure](../Evidence/EXP-001-configure/result.json) | Passed for the first five-package cohort. This predates the standalone ImGui revision. |
| [Build 1](../Evidence/EXP-001-build/output.log) | Failed: bgfx's bundled ImGui configuration includes `bx/bx.h`; the independent UI target lacked the include dependency. |
| [Build 2](../Evidence/EXP-001-build-02/output.log) | Failed after adding bx: the bundled rect-packing wrapper requires `stb/stb_rect_pack.h`. Inspection also found custom allocator/configuration support. Pulling in the entire example integration is a separate choice. |
| [Build 3](../Evidence/EXP-001-build-03/output.log) | Standalone official ImGui compiled. The probe failed because it used older `Init.resolution` fields against the pinned bgfx API. The pinned header defines `Init.swapChain` instead. |
| [Build 4](../Evidence/EXP-001-build-04/result.json) | Passed after correcting the probe to the actual API. Upstream library sources were not patched. |

The failure chain is evidence about our integration assumptions, not a claim that the upstream libraries are broken. Preserve these receipts and use the final source cohort when reproducing. An initial successful configuration does not prove a later modified candidate; CMake regenerated the build during the subsequent attempts.

Build 3 also contains six upstream warnings in bimg's third-party encoding sources (operator precedence and a non-trivial `memcpy` destination). They were not suppressed or repaired. The headless resource check does not exercise those encoders; production asset cooking requires its own dependency and behavior review. Raw compiler logs are preserved without whitespace normalization.

The headless execution result and tool validation are summarized in [STATUS.md](../Docs/STATUS.md). Receipts contain exact commands, timestamps, input/output hashes and failure status. Historical failed candidates are represented by their hashes, logs and this explanation; their source variants are not separately archived.

## What Must Be Proven Next

1. A real native window, input routing and clean shutdown, with no Unity process or runtime dependency.
2. A pinned shader compiler workflow that produces a mesh image on the chosen backend, then verifies that image on Windows hardware.
3. An ImGui platform/renderer adapter for the exact version used, including texture/font upload and resize behavior. CPU draw data does not prove this adapter exists or works.
4. Measured mesh upload, instancing, resource destruction and a minimal inspection workflow. Use neutral fixture data until rock recipes are implemented.
5. A fresh production build recipe and actual dependency/notice inventory before promoting any package from experiment to selected.

Noop success cannot establish PBR quality, shadows, GPU feature coverage, streaming performance, Windows compatibility or third-person feel. Advanced rendering requirements and target hardware remain open; revisit the renderer comparison when those requirements or the GPU fixture supply evidence.

## Provenance and Retention

[Primary license notices](./license-manifest.json) are copied byte-for-byte for the six experiment components. This is not full transitive shipping clearance: bgfx/bimg source trees contain additional third-party code and tools. Complete the inventory of what is actually linked and distributed before shipping. No legal conclusion is inferred from an experiment build.

Git retains authored code, pins, checksums, notices, selected references and meaningful logs. Large archives, extracted dependencies and compiler output remain in the ignored cache and can be reacquired using [the reproduction instructions](./Experiments/README.md). Remote archive availability is an external dependency; create an approved artifact mirror if offline retention becomes a requirement.
