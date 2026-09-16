# Shared G-buffer and screen-space AO

Updated 2026-09-14, America/Phoenix.

Historical implementation note: R5 replaced this three-color-attachment layout and normalized depth proxy with a two-attachment direct/indirect scene target plus conditional `RGBA8` view normals and `R32F` linear view depth. See the [corrected current contract](./MATERIAL_OCCLUSION_PERFORMANCE_R5_RESULT.md). The details below describe the superseded first pass.

The generic scene target now carries the inputs needed by later post-processing in one bounded pass:

- attachment 0: linear HDR scene color (`RGBA16F`)
- attachment 1: encoded shading normal (`RGBA16F`, normal mapped to 0..1)
- attachment 2: normalized depth proxy (`RGBA16F`, `gl_FragCoord.z`)
- attachment 3: hardware depth/stencil (`D24S8`) for opaque ordering

Scene, sky, texture-preview and calibration shaders write all three color attachments. Geometry marks the normal attachment as valid; sky writes a far-depth sentinel and empty pixels retain the clear mask. The display pass samples these shared inputs and applies a four-tap, normal-aware occlusion estimate only when the persisted `ambient_occlusion` setting is enabled. Strength is bounded to 0..1 and radius to 0.1..4 world units; the radius is normalized against the output target before sampling.

AO is intentionally a small infrastructure pass. It runs after scene shading and before exposure/tone mapping, is disabled for calibration/preview/normal diagnostics, and keeps the default output unchanged. It does not add terrain, rock, generator or game-specific behavior. A later renderer iteration can replace the four-tap estimate with a platform-specific kernel while preserving the G-buffer contract.

## Verification

- CMake shader generation rebuilt `fs_scene`, `fs_display`, `fs_sky`, `fs_texture_preview` and `fs_calibration`.
- Native targets `engine_workbench`, `engine_player`, `engine_core_tests` and `engine_outdoor_check` built successfully on the Mac toolchain.
- `ctest --test-dir Engine/build/foundation --output-on-failure`: 14/14 passed, including persisted AO settings and existing migration/lighting contracts.

Windows GPU behavior, long-run bandwidth cost and visual tuning remain open until the first Windows validation pass. The target layout is explicit so those checks can be performed without changing the material or scene-authoring contracts.
