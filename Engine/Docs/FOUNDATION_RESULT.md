# Application Foundation Result

Recorded 2026-09-09. The [milestone plan](./APPLICATION_FOUNDATION_PLAN.md) is complete within its Mac technical-proof boundary. The [status page](./STATUS.md) links the final evidence and remaining platform/creative gates.

## Implemented Ownership

- `Source/Core/FixtureState`: CPU settings, orbit bounds, input-capture policy and safe framebuffer clipping. It contains no renderer handles.
- `Source/Platform/Window`: SDL window ownership and native handle access. Mac uses SDL's supported Metal view; Windows has a D3D-compatible native handle path.
- `Source/Rendering/Renderer`: backend initialization, shaders, one reusable mesh, uniforms, frame submission, screenshots and explicit teardown.
- `Source/Rendering/InspectorRenderer`: fixed font-atlas upload and ImGui draw-data submission with alpha blending, scissor bounds, framebuffer scaling and vertex offsets.
- `Apps/Workbench`: event routing, one fixture state, inspector controls and a separately requested bounded technical verification mode.

The runtime shader binaries are built by the exact source-pinned shaderc target. Mac shader profile is `metal`; the untested Windows build path uses `s_5_0` and D3D11. No shader binaries or third-party source trees are vendored into project-owned source. The current development executable references its configured shader directory; packaging remains future work.

## Verification and Corrections

The initial build started from an empty build directory. It built the dependencies, shader compiler, four shaders, native application and state tests. All compiled without project-source diagnostics; upstream shader-tool warnings and duplicate bx linkage warnings remain in the logs.

The first real Metal run passed the application assertions and image checks. Visual inspection then found the camera field-of-view label clipped against the inspector edge. Item widths were corrected, panel height was made responsive, and the window received a minimum usable size. The global wireframe switch was removed because it would also affect inspector readability. These changes were rebuilt and the full native verification was repeated against the final executable and shader hashes.

The verification mode injects an actual ImGui button interaction to change the material, then an SDL mouse-motion event to orbit. It records the resulting application state and compares scene pixels outside the inspector. It resizes the native window and captures the changed framebuffer dimensions. Twenty mesh replacements return the observed vertex-buffer count from five to five after deferred destruction drains. Four screenshots are captured without capture errors, and the injected close event exits through resource cleanup.

Two expected failing commands test invalid arguments and missing shader files. The latter exercises cleanup after backend initialization. Their recorder outcomes are correctly `failed`; the surrounding verification passes only when those failures occur with the expected diagnostics. This is not test suppression.

Pure native tests cover captured versus uncaptured camera input, orbit bounds, nonfinite camera recovery, clipped/empty rectangles and zero-size framebuffer rejection. Fifteen Python tests cover preparation tools plus PNG integrity and scene-only image comparison. Captures were inspected for geometry, inspector readability and resize output; that is technical visual review, not creative acceptance of the game.

## Primary Integration References

SDL's [Metal view API](https://wiki.libsdl.org/SDL3/SDL_Metal_CreateView) supplies a native view/layer for the supported bgfx backend. The [bgfx shader-tool documentation](https://bkaradzic.github.io/bgfx/tools.html) explains the shader pipeline and profiles. The actual code follows the [pinned bgfx header](https://github.com/bkaradzic/bgfx/blob/9b636df330c81e11c84595651a291b6c59fb7396/include/bgfx/bgfx.h) for swap-chain/reset and screenshot signatures, and the [pinned ImGui SDL3 backend](https://github.com/ocornut/imgui/blob/f1cc2ae15e53a861a874c3034aae6798fde194ab/backends/imgui_impl_sdl3.cpp) for platform input. These sources inform the integration; local receipts establish the observed result.

## Deliberate Limits

There is no world generation, streaming, authored-region constraint model or persisted runtime delta in this fixture. GPU mesh replacement tests a prerequisite for streaming, not world identity. The local meter scale and right-handed, +Y-up view do not establish final geography. No character-following/obstruction behavior or game physics is implemented.

The inspector adapter has a fixed font atlas; dynamic font updates, custom image textures and multiple viewports are unsupported. Scene shading is simple diffuse lighting without shadows, PBR or culling validation. Performance diagnostics include pacing and are not benchmarks. Technical input injection does not prove physical mouse, keyboard or controller behavior. Windows, minimize/restore, cross-display DPI transitions, long sessions and separate clean-machine setup remain future checks.

Shipping notices and the complete transitive distribution inventory remain open. The existing source pins and primary license notices are retained, and compiler/dependency caches remain ignored. The preparation's failed headless candidates and the first pre-layout-fix GPU capture remain historical evidence.
