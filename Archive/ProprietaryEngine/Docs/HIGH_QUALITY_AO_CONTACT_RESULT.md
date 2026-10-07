# Higher quality ambient occlusion and contact shadows

Updated 2026-09-14, America/Phoenix.

Historical implementation note: R5 corrected the reconstruction units, indirect-only AO composition and auxiliary formats/allocation policy. See the [corrected current contract and measured cost](./MATERIAL_OCCLUSION_PERFORMANCE_R5_RESULT.md). The details below describe the superseded quality pass.

This pass is limited to screen-space occlusion quality. It does not add terrain, rock generation, authored landscapes or game-specific content.

The display pass now evaluates twelve fixed samples across three radii. Each sample uses a soft depth-closer test, a depth-range weight and an edge-aware normal agreement term. The result is normalized by the kernel weights and clamped to a bounded strength. The existing persisted AO controls remain the authority: `ambient_occlusion`, `ao_strength` (0..1) and `ao_radius` (0.1..4 m). AO is still opt-in, disabled for preview/calibration/normal diagnostics, and applied before exposure and tone mapping.

Contact shadows use a separate opaque depth/normal prepass when `contact_shadows` is enabled. Static and skinned geometry share the same transforms and depth test as the visible scene. The forward scene shader traces eight short steps from each receiver toward the sun, projects each step through the current camera, rejects samples outside the screen, compares against prepass depth with a small bias, and fades at screen edges and along the ray. The resolved visibility multiplies direct sun only; ambient light, fog and material AO are not darkened by this mask. `contact_strength` (0..1) and `contact_distance` (0.1..3 m) are persisted with additive migration defaults.

The prepass owns two `RGBA16F` targets for encoded normals and depth proxy plus a `D24S8` depth/stencil target. It is allocated transactionally with the scene targets and destroyed through the renderer owner on resize/shutdown. The existing scene G-buffer remains available to the display AO pass, while contact sampling never reads from a texture that the forward scene is writing.

## Verification

- CMake rebuilt `fs_prepass`, scene/display shaders, static and skinned programs, and all native targets.
- `ctest --test-dir Engine/build/foundation --output-on-failure`: **14/14 passed**.
- The Metal `engine_outdoor_check` ran with AO and contact shadows enabled, produced five captures at 2240x1440 and 1920x1280 after resize, and exited with zero renderer errors. Captures are retained in the local ignored output directory `Engine/out/contact-shadow-check-20260914c/`.

This is a fixed-quality spatial pass. Temporal reconstruction, hierarchical depth, half-resolution upsampling, contact-shadow thickness heuristics and Windows GPU measurements remain future work only if a representative workload demonstrates the need.
