# EXP-001 — Native Library Compatibility

Question: can one exact C++20/SDL3/bgfx/ImGui cohort configure, link and perform bounded headless technical work on the current Mac?

The first executable deliberately uses bgfx's **Noop** backend. It checks synthetic SDL event delivery, graphics resource calls and ImGui CPU draw-data creation. It does not open or render a window, validate a GPU backend, compile shaders, render an inspector, test physical input or exercise gameplay.

Sources are pinned in [probe-lock.json](../probe-lock.json). bgfx, bx and bimg revisions come from the selected bgfx.cmake commit's Git submodule entries. The first two builds exposed configuration/support dependencies in bgfx's bundled ImGui copy. The final candidate uses an independently pinned official ImGui 1.92.9b release for this CPU-only check; it does not reuse bgfx's ImGui renderer adapter. The third build exposed a probe API mismatch, corrected against the pinned bgfx header. See [the stack audit](../STACK_AUDIT.md) for the evidence and remaining GPU integration question.

## Reproduce

From `Engine/`, with Python 3.9+, CMake 3.20+ and a working native C/C++ toolchain:

```sh
python3 Tools/prepare_probe.py --fetch --output Evidence/source-inventory.json
python3 Tools/record.py --out Evidence/configure-new --input Research/probe-lock.json --input Research/Experiments/IntegrationProbe/CMakeLists.txt -- cmake -S Research/Experiments/IntegrationProbe -B .cache/probe-build -DCMAKE_BUILD_TYPE=Release
python3 Tools/record.py --out Evidence/build-new --input Research/probe-lock.json --input Research/Experiments/IntegrationProbe/main.cpp -- cmake --build .cache/probe-build --config Release --parallel 4
python3 Tools/record.py --out Evidence/run-new --input Research/probe-lock.json --input Research/Experiments/IntegrationProbe/main.cpp -- ctest --test-dir .cache/probe-build -C Release --output-on-failure
```

Use new evidence names on each run. The source-preparation tool refuses altered cached sources. The archive downloads total approximately 146 MB; unpacked sources and compiler output consume more disk. The command does not install globally. On Windows use the appropriate Python command and a Visual Studio development environment; no Windows result has been established by the Mac run.

The initial Mac uses CMake's available Makefiles generator, avoiding a global Ninja install for this bounded test. Production dependency management and generator selection remain open. Direct upstream CMake sources here are not a decision to replace vcpkg throughout the eventual engine.

Acceptance: configure and build succeed, the executable's explicit checks pass, and the recorded inputs remain unchanged. Any one failure is a failed experiment until explained and re-run. Passing only advances compatibility knowledge; it does not close the renderer decision.

Next experiment: actual GPU mesh plus inspector rendering with a pinned shader toolchain, followed by native Windows evidence. Do not expand this headless probe into the production engine.
