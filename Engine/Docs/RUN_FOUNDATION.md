# Run the Engine Foundation

This executable is the first application/rendering fixture. See [the implementation plan](./APPLICATION_FOUNDATION_PLAN.md) for its completion contract and [status](./STATUS.md) for actual evidence.

## Build

From `Engine/`, with Python 3.9+, CMake 3.20+ and the platform's native C++ toolchain:

```sh
python3 Tools/prepare_probe.py --fetch
cmake -S . -B build/foundation -DCMAKE_BUILD_TYPE=Release
cmake --build build/foundation --config Release --target engine_workbench engine_state_tests engine_geometry_tests --parallel 4
ctest --test-dir build/foundation -C Release --output-on-failure
```

Configuration verifies the exact dependency archives and source content. It never downloads silently. The build compiles bgfx's shaderc and then our four shaders. First-time shader-tool compilation is larger than rebuilding the application. Build output is ignored. No global package installation is required by these commands.

Mac uses the available Makefiles generator, SDL's Metal view and bgfx Metal. On Windows run from a Visual Studio development environment; use `python` if that is the installed command. The Windows path uses D3D11 and shader model 5.0. Platform code is not Windows proof; consult the status page before claiming a tested Windows build.

## Launch and Controls

Mac: `build/foundation/engine_workbench`. With a Windows multi-configuration generator: `build/foundation/Release/engine_workbench.exe`. Shader paths default to the configured build directory; override with `--shaders <directory>` when needed. This is a development executable, not a relocatable shipping package.

The window opens without requesting foreground activation. Click it to interact. Right-drag outside the inspector orbits the inspection subject; the wheel changes distance. Inspector controls change object rotation/color, light direction/intensity and camera settings. The turquoise column is a temporary 1.8-meter scale marker. Escape closes when the inspector is not capturing the keyboard; the window close control also exits.

Expand **Geometry inspection** to select a cube, sloped solid or sphere, adjust positive XYZ scale independently, and display world-space normals as RGB. Nonuniform scaling uses an inverse-transpose normal matrix. The reference objects use outward counterclockwise winding and backface culling. Their fixed center means scale/shape edits can lift or intersect the ground; grounding and shadows are later work.

The view is a perspective inspection camera. It does not implement character following, camera obstruction or final third-person controls. Materials use simple directional diffuse shading. Shadows, PBR, terrain, physics and gameplay are not part of this foundation fixture.

## Technical Verification

Use a new output name each time:

```sh
python3 Tools/verify_foundation.py --executable build/foundation/engine_workbench --shaders build/foundation/Shaders --out Evidence/F1-verification-new
```

Adjust the executable path for Windows. Verification creates a non-focusable application window, injects a real inspector-button click through ImGui's input queue and an SDL camera event, changes window size, rebuilds a mesh twenty times, captures our GPU surface, checks sampled scene-pixel changes, then closes and checks two expected error paths. It does not capture the desktop or exercise gameplay. Automated input is not physical-device proof.

The current verification also captures a transformed sloped solid, an independently CPU-baked flat-normal reference, culling controls, reversed opaque submission order and a stretched sphere. Ten captures are expected. The normal/reference, cull/unculled and order comparisons exclude the inspector and tolerate only narrow rasterization differences; empty references and ineffective opposite-cull controls fail. The normal diagnostic encodes vectors directly and does not establish a linear lighting/display-color pipeline.

Captures, application assertions and command receipts live together in the output directory. Inspect all three; no one alone establishes correct appearance. The PNG reader in the tool supports this fixture's noninterlaced RGB/RGBA8 screenshots and is not a general asset importer.

## Current Implementation Limits

The ImGui renderer uses a fixed font atlas and the official SDL3 platform backend. It supports clipping, framebuffer scale, alpha blending and vertex offsets. Custom image textures, dynamic font-atlas updates and multiple viewports are explicitly unsupported. The fixture's editable state is the single source of truth; settings are not saved yet.

Generated world identity, chunk streaming and persisted deltas are not implemented. Repeated GPU mesh replacement validates one resource-lifetime prerequisite, not those world contracts. Frame timing shown in the inspector includes pacing; it is diagnostic information rather than a target-hardware performance benchmark.

## First-pass documents and local packages

The [runtime foundation](./FIRST_PASS_RUNTIME.md) adds strict inspection documents, linear display conversion and portable shader lookup. From Engine, CMake 3.21 or newer supports the checked-in presets (the direct CMake command path still supports 3.20):

```sh
python3 Tools/prepare_probe.py --fetch
cmake --preset mac-dev
cmake --build --preset mac-dev
ctest --preset mac-dev
python3 Tools/package_workbench.py --build build/foundation --out out/my-workbench
out/my-workbench/bin/engine_workbench --build-info
out/my-workbench/bin/engine_workbench --save-inspection out/inspection.json
out/my-workbench/bin/engine_workbench --inspection out/inspection.json
```

The interactive commands save on normal exit and load before renderer startup respectively. They are user launch instructions, not additional automated gameplay tests. Choose a new package output directory. Inspection output parents must exist. No automatic user-profile writes occur.

For the dependency-free UI boundary use `cmake --preset core`, `cmake --build --preset core`, and `ctest --preset core`; this still needs the pinned JSON source but never configures SDL/bgfx/ImGui. `windows-dev` selects Visual Studio 2022 x64; it is a source path awaiting W01 native execution, not Windows build proof.

Bounded installed-package technical verification:

```sh
python3 Tools/verify_foundation.py --executable out/my-workbench/bin/engine_workbench --shaders out/my-workbench/bin/Shaders --portable --out Evidence/my-native-check
```

The extra color captures preserve the existing geometry checks, measure eight known linear values at two exposures and recheck after target replacement. The inspector remains outside the scene display transform. These checks do not establish physical input, artistic acceptance, gameplay, Windows compatibility or target-PC performance.
