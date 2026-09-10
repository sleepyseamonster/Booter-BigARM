# Proprietary Engine Foundation Research

Research date: 2026-09-09, America/Phoenix.

Status: research and proposed implementation sequence. [DIRECTION.md](./DIRECTION.md) controls the accepted new-engine direction and work boundary. The user has directed a move toward a proprietary engine, standard third-person presentation, and the open Greater Wasteland. Library selections below are recommendations, not an implemented or benchmarked stack.

## Recommendation

Build a game-specific engine in **C++20**, using **CMake**, **SDL3**, and an existing graphics library. Start the graphics evaluation with **bgfx**. Use **Jolt** for physics and **Dear ImGui** for development tools when those capabilities enter the foundation. Own the rock generator, world identity, streaming policy, runtime state, asset pipeline, renderer features, and authoring workflow.

Treat **Windows PC as the product target**. Mac development is optional convenience. The user's clarification explicitly permits moving development to Windows and rejects spending substantial effort making a dual-platform development arrangement work. Do not design or maintain two proprietary graphics backends to preserve Mac support.

The first implementation should establish the executable, build process, diagnostics, graphics, input, asset loading, and basic tooling. The rock workbench follows as its first real application. A complete general-purpose editor, full game, and large landscape are not prerequisites for that workbench.

These recommendations reflect integration fit and scope, not measured superiority. No dependency was installed, sample compiled, renderer benchmarked, or new engine scaffold created during this research.

## 1. Scope and source of truth

The current user brief controls:

- Standalone proprietary engine rather than another Unity-hosted intermediate architecture.
- Standard 3D third-person camera, rather than elevated top-down presentation.
- Open spaces of the Greater Wasteland; rock and formation generation is the initial content workload.
- Canyons are deferred. They are not the initial terrain target or the renderer acceptance scene.
- Windows PC is the intended game platform. Work currently happens on Mac, but Windows development is an acceptable alternative.
- Research the technology and construction sequence before implementation.

The existing [world systems standard](../../Docs/WORLD_SYSTEMS_STANDARD.md), [World Creator charter](../../Docs/WORLD_CREATOR_CHARTER.md), [architecture plan](../../Docs/WORLD_CREATOR_ARCHITECTURE_PLAN.md), and [rock production plan](../../Docs/ROCK_QUALITY_AND_PRODUCTION_PLAN.md) preserve useful constraints and reference work. Their Unity-specific implementation prescriptions do not dictate the new engine. The old top-down language and canyon-focused descriptions must not override this task's explicit brief.

The final geographic coordinate system remains deliberately undefined in the charter. An engineering coordinate representation must not silently invent the world's canonical map, axes, regional boundaries, or wrapping rules. [Lorekeeper](../../Docs/Agents/Lorekeeper/README.md) remains the route for relevant world context; the Arc & Dust repository remains reference-only.

Done for this research: a recommended stack, credible alternatives, ownership boundaries, a platform policy, a staged construction plan, and explicit unresolved proof. Out of scope: installations, engine implementation, Unity asset migration, purchases, account setup, external publication, and gameplay smoke testing.

## 2. What “our own engine” means

An engine is the collection of systems that repeatedly turns assets, input, and simulation state into an interactive application. A renderer is one part of it.

| Layer | Responsibility | Initial ownership decision |
|---|---|---|
| Platform | Window, events, controller input, timing and OS integration | Use SDL3; keep OS details at the application boundary |
| Graphics backend | GPU buffers, textures, shader programs, draw submission and API differences | Use one graphics library initially |
| Game renderer | Materials, lighting, shadows, atmosphere, visibility and levels of detail | Build the features this game needs over that library |
| Simulation | Fixed updates, entity state, interaction and companion behavior | Project-owned systems, added in bounded stages |
| Physics | Collision shapes, queries and rigid bodies | Integrate Jolt; own movement rules and collision lifecycle |
| World generation | Recipes, deterministic identities, placement and chunk ownership | Project-owned core, callable without a graphics window |
| Asset tools | Import, validation, conversion, caching and dependency tracking | Project-owned pipeline using focused import/processing libraries |
| Authoring | Rock parameters, previews, selection, save/load and later undo | Project-owned tools built with Dear ImGui |
| Diagnostics | Logs, assertions, performance counters, tests and crash evidence | Present from the foundation, not a final cleanup phase |

Using libraries still leaves substantial engine work. An ImGui panel does not supply an editor document model or undo system; a physics library does not supply Booter's movement feel; a graphics library does not supply a finished wasteland lighting pipeline.

## 3. Language and build system

### Language comparison

| Option | Why it is credible | Tradeoff for this project | Recommendation |
|---|---|---|---|
| C++20 | Direct access to the proposed native libraries and explicit resource lifetimes | Memory errors, concurrency mistakes and build complexity require disciplined ownership and diagnostics | Preferred runtime and tools language |
| Rust | A credible native alternative; wgpu supplies graphics and compute across desktop backends | Using the proposed C++ physics/tooling components adds foreign-function interfaces and binding ownership | Reconsider if Rust itself becomes a project preference |
| C#/.NET | Familiar from the Unity implementation; Silk.NET provides low-level graphics bindings | Unity-dependent code still requires porting; native interop and managed allocation behavior must be designed | Viable, but not the preferred foundation for this native stack |

The language ranking is an engineering judgment, not a benchmark. Rust is not disqualified from engine work, and C# is not inherently too slow. The deciding factor is the integration burden for the libraries we intend to use. See [wgpu's platform and bindings overview](https://wgpu.rs/), [Silk.NET](https://dotnet.github.io/Silk.NET/), and [MSVC language conformance](https://learn.microsoft.com/en-us/cpp/overview/visual-cpp-language-conformance?view=msvc-170).

Use a conservative C++20 subset initially, conventional headers, RAII ownership, explicit handles, and standard containers where adequate. Do not begin with a custom allocator suite, scripting runtime, or elaborate template framework. Shader programs are a separate language/toolchain: with bgfx, start with its GLSL-like shader dialect and `shaderc`; an HLSL-first workflow would accompany a Diligent or direct Direct3D choice. [bgfx shader tools](https://bkaradzic.github.io/bgfx/tools.html).

### Build and dependency recommendation

- **CMake with checked-in presets** for repeatable configure, build and test commands. Keep personal paths in untracked user presets. [CMake presets](https://cmake.org/cmake/help/latest/manual/cmake-presets.7.html).
- **Ninja** as the proposed routine build runner; Xcode or Visual Studio remains available for debugging. CMake describes the project; it is not itself the C++ compiler. [Visual Studio CMake support](https://learn.microsoft.com/en-us/cpp/build/cmake-projects-in-visual-studio?view=msvc-170).
- **vcpkg manifest mode**, with a pinned registry baseline and version overrides where required. Pin a compatible dependency set rather than resolving each component to whatever is newest. [Manifest mode](https://learn.microsoft.com/en-us/vcpkg/concepts/manifest-mode), [versioning](https://learn.microsoft.com/en-us/vcpkg/users/versioning).
- Apple Clang/LLDB on the current Mac; MSVC and the Windows SDK on Windows. Keep compiler-specific settings out of gameplay/world code.
- Compile asset and shader tools for the machine that runs them; produce separately identified runtime outputs for the selected graphics backend. Do not assume one shader binary serves Metal and Direct3D.

The [bgfx vcpkg port](https://github.com/microsoft/vcpkg/blob/master/ports/bgfx/vcpkg.json) exists, but package availability is not proof of a compatible build. The inspected port snapshot and current upstream documentation carry different version numbers. Initial setup must verify the selected bgfx, shader compiler, ImGui integration, CMake requirements, and dependency licenses together. Do not combine arbitrary upstream heads or maintain a custom package registry unless a concrete blocker requires it.

## 4. Rendering choices

Graphics abstraction and a complete rendering engine offer different amounts of help:

| Candidate | What it supplies | What remains / relevant constraint | Assessment |
|---|---|---|---|
| **bgfx** | Graphics API abstraction with Direct3D 11/12, Vulkan and Metal backends | We implement the game's rendering features; its submission model and shader dialect influence that design | First candidate for the foundation |
| **SDL3 GPU** | Modern raster and compute API using Direct3D 12, Vulkan or Metal, alongside SDL platform services | Its documented target is a portable feature set; mesh shaders and ray tracing are not current near-term promises | Strong compact alternative if its feature boundaries fit |
| **Diligent Engine** | Lower-level graphics framework, HLSL workflow and optional higher-level rendering components | Advanced features remain backend/hardware-dependent; native Metal is commercially licensed, with Vulkan portability an alternative on Mac | Strong Windows-oriented alternative if explicit GPU control becomes decisive |
| **Filament** | More complete physically based rendering and material infrastructure | We would adopt its material/rendering model and test custom terrain/rock needs against it | Strong alternative when obtaining polished lighting quickly outranks renderer ownership |
| **Direct3D 12 directly** | Native Windows GPU API and maximum control over that layer | We own descriptor management, synchronization, resource transitions and the rest of the backend | Defer until a demonstrated requirement justifies the added work |

Sources: [bgfx overview](https://bkaradzic.github.io/bgfx/overview.html), [SDL GPU contract and limits](https://wiki.libsdl.org/SDL3/CategoryGPU), [Diligent features and platform/license matrix](https://github.com/DiligentGraphics/DiligentEngine), [Filament](https://github.com/google/filament), [Direct3D 12 programming guide](https://learn.microsoft.com/en-us/windows/win32/direct3d12/directx-12-programming-guide).

**Why start with bgfx?** Its backend coverage and separation from a complete game engine fit the immediate goal: own our rendering behavior without first implementing low-level GPU infrastructure. This remains useful on Windows alone. It is not selected merely to keep the Mac working. Its API should stay inside the rendering module, but do not build a second universal graphics abstraction over it.

**What could change that choice?** A concrete requirement for GPU features or scheduling that its interface cannot support acceptably; excessive integration friction; or a demonstrated need for a fuller ready-made renderer. In that event compare one replacement against the failed requirement. Do not build multiple competing engines in parallel.

For the initial renderer, propose conventional rasterization, ordinary mesh LODs, instancing, a directional light and shadow maps, and a modest material model. These are project proposals, not claims that bgfx automatically supplies them. Virtualized geometry, ray tracing, and elaborate global illumination are deferred requirements, not promised upgrades. Open wasteland scale alone does not prove they are necessary.

## 5. Supporting tools and libraries

“Foundation” means introduce when the first executable needs the capability. “Rock workbench” and “later” entries are a shortlist, not an instruction to install everything now.

| Need | Candidate and evidence | When / integration boundary |
|---|---|---|
| Windows, input and controllers | [SDL3](https://wiki.libsdl.org/SDL3/FrontPage) | Foundation. SDL owns platform events; our action layer determines behavior |
| Vectors, matrices and quaternions | [GLM](https://github.com/g-truc/glm) | Foundation. Explicitly configure render conventions; the name does not require an OpenGL renderer |
| Development UI | [Dear ImGui](https://github.com/ocornut/imgui) | Foundation. Inspector, logs, camera controls and frame timings; not a commitment to final player UI |
| Collision and rigid bodies | [Jolt Physics](https://github.com/jrouwe/JoltPhysics) | When validating a ground surface and character capsule. Mesh/heightfield shapes and character options are relevant; we still own movement and chunk lifecycle |
| Recipe/config serialization | [nlohmann JSON](https://github.com/nlohmann/json) | Foundation/workbench. Versioned readable documents; bulk mesh data belongs in an appropriate cache format |
| Unit/component checks | [Catch2](https://github.com/catchorg/Catch2) with CTest | Foundation. Stable IDs, math/conventions, recipe validation and resource lifetime checks |
| CPU instrumentation | [Tracy](https://github.com/wolfpld/tracy) | Add useful generation, loading and frame timing zones early; GPU instrumentation depends on the chosen backend |
| Windows GPU analysis | [PIX](https://devblogs.microsoft.com/pix/introduction/) | For the Direct3D 12 path; use native Windows captures before making Windows GPU claims |
| Mac GPU analysis | [Xcode Metal debugger](https://developer.apple.com/documentation/xcode/metal-debugger) | For local Metal work while Mac remains useful; not evidence of PC performance |
| Interchange assets | [glTF/GLB](https://www.khronos.org/gltf/) and [fastgltf](https://github.com/spnda/fastgltf) | Rock workbench. Separate import data from engine-owned runtime resources |
| Mesh preparation and LOD processing | [meshoptimizer](https://github.com/zeux/meshoptimizer) | Rock workbench. Optimization and simplification complement our shape generator; they do not generate geology |
| Texture processing | [KTX-Software](https://github.com/KhronosGroup/KTX-Software) | When texture compression and shipping formats enter scope. Verify backend formats and include decoder/transcoder dependencies in the inventory |
| Audio playback and spatialization | [miniaudio](https://miniaud.io/) | Later. Avoid duplicating SDL and miniaudio as competing audio owners |
| Skeletal animation | [ozz-animation](https://github.com/guillaumeblanc/ozz-animation) | Later. Sampling/blending and offline conversion; character behavior and animation authoring remain ours |
| Navigation | [Recast/Detour](https://github.com/recastnavigation/recastnavigation) | Later. Loaded-region navigation candidate; not a complete solution for BigARM's unloaded-world travel |
| Entity storage | [EnTT](https://github.com/skypjack/entt) | Optional later. Use if component queries justify it; runtime handles must not become permanent generated-object IDs |

There is an important UI integration detail: Dear ImGui has an official SDL3 platform backend, but bgfx is not in its standard renderer-backend list. bgfx supplies its own example integration. Use a compatible version of that integration rather than assuming any ImGui/bgfx pair plugs together. [ImGui backend documentation](https://github.com/ocornut/imgui/blob/master/docs/BACKENDS.md), [bgfx ImGui integration](https://github.com/bkaradzic/bgfx/blob/master/examples/common/imgui/imgui.cpp).

Retain existing art tools where useful; choose an interchange workflow before selecting new purchases. Asset authoring applications are distinct from runtime dependencies. Existing Unity shaders, prefabs and scenes are not native engine assets. Export/port only the source material actually needed, checking the ownership and license of each imported asset or package.

The main proposed libraries use permissive licenses: bgfx documents BSD-2-Clause; Jolt and Dear ImGui document MIT. That supports the proposed architecture but does not erase attribution, notices, or dependency-specific conditions. Record exact versions and their license files during setup. Diligent's commercial Metal exception is explicitly documented above; no paid backend is recommended for purchase by this report. [bgfx](https://bkaradzic.github.io/bgfx/overview.html), [Jolt](https://github.com/jrouwe/JoltPhysics), [Dear ImGui](https://github.com/ocornut/imgui).

## 6. Mac development versus Windows development

### Observed local environment

Read-only inspection found an Apple M1 Max MacBook Pro with 32 GB memory and a 24-core GPU, running macOS 26.5. Xcode 26.6, Apple Clang 21.0.0, and CMake 3.28.1 are available. Ninja was not found on the current PATH. This is an installation inventory, not compilation or performance proof.

No Windows machine, GPU, minimum OS, resolution or target frame rate was specified or inspected. Windows x64 is a working target assumption, not a confirmed minimum specification.

### Practical platform policy

1. Use the current Mac for research and the first foundation build if the chosen dependency set works through its ordinary supported setup.
2. Use the library's Metal backend locally and plan a native Direct3D backend build on Windows. Do not write our own Metal compatibility layer.
3. Bring in a native Windows build during the foundation stage, before extensive renderer/material investment. If Windows hardware is unavailable then, explicitly retain that proof gap.
4. If Mac setup requires a paid backend, bespoke patches, a custom Vulkan portability investigation, or sacrificing a required Windows feature, move primary development to Windows rather than expanding portability work.
5. Treat virtual machines, translated binaries and successful cross-compilation as limited evidence. Actual Windows GPU execution is required to judge target behavior and performance.

**There is no research finding here that requires buying a PC immediately.** Equally, the Mac is not a permanent architectural requirement. If an appropriate Windows machine is already available, developing there would shorten the path to target-platform validation.

The normal arrangement is separate native builds from the same source, using the selected library's existing backends. This report does not propose a Mac-to-Windows cross-compilation project, remote build service, dual-platform release commitment, or hardware shopping list.

## 7. Proposed engine boundaries

Keep the initial codebase modular without turning every capability into a plugin framework:

```text
Engine core                 Resource ownership, logging, time, IDs, task scheduling
Platform adapter            SDL events, window and filesystem integration
Renderer                    Mesh/material resources, visibility and graphics submission
World and rock core         Recipes, deterministic planning and CPU geometry results
Asset tools                 Import -> validate -> prepare -> cache/package
Workbench application       Inspect, edit, preview and persist rock recipes
Later game application      Third-person controller, simulation and gameplay
```

The world/rock core returns engine-owned data, not graphics-library handles. The renderer receives generated meshes and material parameters. Rendering resources and physics bodies can disappear when a region unloads without deleting that region's durable identity or recorded changes.

The initial CPU generator is a proposed correctness baseline. Worker jobs may generate and prepare data; render-thread uploads must have bounded ownership and lifetime. GPU generation is a later measured optimization, not the required source of persistent world identity.

The required procedural concerns apply as follows:

| Concern | Foundation contract |
|---|---|
| Deterministic world identity | Explicit world/generator/recipe versions and integer seed derivation. No wall-clock seeds or unordered iteration in authoritative output |
| Stable generated-object identity | IDs derive from stable source keys and placement ownership; never from memory addresses, draw order or temporary ECS handles |
| Chunk lifecycle | Separate planned/generated data, CPU caches, GPU resources and collision resources; stale asynchronous results cannot reactivate an unloaded owner |
| Authored constraints | Rock recipes, material choices and placement exclusions remain editable inputs; random variation operates within them |
| Persisted runtime deltas | Keep edits and player changes separate from regenerable geometry; version recipes and saves; caches may be rebuilt |
| Coordinate precision | Keep durable location identity separate from camera-local render coordinates; leave the final geographical model replaceable |

These are proposed translations of the project's existing [world systems contracts](../../Docs/WORLD_SYSTEMS_STANDARD.md), not implemented behavior. An isolated rendering fixture can omit chunks, but must be labeled a fixture rather than become the production world's ownership model.

Identical seeds alone do not guarantee bit-identical floating-point meshes on ARM and x64. Specify exactness for IDs and discrete decisions, define tolerances or canonical baking where appropriate for geometry, and verify on both architectures before promising cross-platform output equivalence. Likewise, deterministic generation and deterministic physics are separate requirements; Jolt documents limits on its simulation determinism. [Jolt design considerations](https://github.com/jrouwe/JoltPhysics).

## 8. Construction sequence and evidence

The order should produce a usable foundation before expanding the rock system, with evidence at each step. These are future work packages, not tests run during research.

| Stage | Deliverable | Evidence needed before expanding |
|---|---|---|
| 0. Freeze the technical candidate | Separate engine location; exact dependency/tool versions; Windows target assumption; source ownership and license inventory | Reproducible setup instructions and one consistent dependency set; resolve build integration before a broader renderer comparison |
| 1. Buildable application shell | CMake presets, logging, SDL window/input, clean shutdown and headless core tests | Clean native build; resource/error-path checks; no dependence on Unity installation or project files |
| 2. Rendering foundation | bgfx initialization, depth-tested mesh, movable perspective camera, viewport resize, material parameters, ImGui and timing display | Correct buffer/resource lifecycle and shader errors; inspect known geometry, winding, normals, depth, input capture and resize behavior |
| 3. Target and workload check | A neutral open-ground fixture with repeated meshes, directional lighting/shadows, asset loading and simple capsule collision | Native Windows build/run when available; capture backend/GPU identity and CPU/GPU timings; verify resource churn and camera obstruction queries |
| 4. Rock workbench | Seeded recipe -> mesh -> material -> LOD/collision preview; parameter editing and recipe save/load | Determinism and malformed-recipe checks; user review at eye level, around every side and at distance; performance evidence against declared workloads |
| 5. Streamed open-space extension | Bounded region loading, resource budgets, instance placement and persistent overrides | Stable identities through unload/reload, no obsolete job resurrection, bounded memory and correct saved changes |

Stage 3 is where the renderer recommendation becomes an evidence-backed choice. If it fails a required capability, record the failure and evaluate one suitable alternative. It is not permission to maintain all shortlisted renderers.

The first workbench needs a simple ground surface for scale and contact assessment. It does not require a completed Greater Wasteland landscape. Character animation, a complete BigARM simulation, harvesting, settlements and survival mechanics follow later. Canyons remain deferred.

Performance reports must name hardware, backend, build configuration, resolution, visible instances/triangles, generation settings and workload. Record frame-time distributions and transitions, not FPS alone. A successful build is not visual acceptance; a good Mac result is not Windows performance proof. User-owned gameplay smoke testing remains separate from focused technical checks.

## 9. Risks and decisions still open

- **Renderer effort:** bgfx leaves materials, shadows, atmosphere and visibility policy to us. If this work dominates without supporting the game's needs, Filament deserves renewed consideration.
- **Backend limits:** advanced features are not automatically portable. A specific requirement may favor Diligent or direct Direct3D and justify Windows-only development.
- **Tool integration:** bgfx/shaderc/ImGui/package versions must form a tested combination. Documentation alone does not prove this combination on the installed tools.
- **Content quality:** a previously accepted top-down rock appearance is reference evidence, not third-person approval. Close views add surface, scale, ground-contact and collision requirements.
- **Asset ownership:** Unity-specific data and third-party content need individual portability checks. No bulk migration is implied.
- **Product target:** Windows hardware, minimum OS, resolution, frame-time goal, game distribution requirements and final visual reference remain open. Do not invent a minimum spec from the Mac's capabilities.
- **Project location — resolved by user direction:** new work lives in `Engine/` inside this repository. It has its own instructions and documentation; the existing Unity project remains in place. A separate repository is not the selected arrangement.
- **Schedule:** no credible parity date is established. Estimate the next stage after its predecessor supplies build and integration evidence.

A new engine does not need every possible subsystem before it can do useful work. The appropriate first commitment is a reproducible native application with rendering, diagnostics and a small authoring surface. The rock generator then supplies a concrete reason to extend it.

## 10. Learning and verification path

Use primary project material to learn and verify each layer in the same order as implementation:

1. [SDL3 documentation](https://wiki.libsdl.org/SDL3/FrontPage) for application events and platform integration.
2. [bgfx examples](https://bkaradzic.github.io/bgfx/examples.html) and [shader tools](https://bkaradzic.github.io/bgfx/tools.html) for mesh rendering, resources and the shader workflow. Examples are teaching/integration references, not a production renderer specification.
3. [Dear ImGui backend guide](https://github.com/ocornut/imgui/blob/master/docs/BACKENDS.md) for separating input handling from UI rendering.
4. [Jolt documentation and HelloWorld entry](https://github.com/jrouwe/JoltPhysics) for collision integration before building a character controller.
5. [CMake presets](https://cmake.org/cmake/help/latest/manual/cmake-presets.7.html) and [vcpkg versioning](https://learn.microsoft.com/en-us/vcpkg/users/versioning) for repeatable local/native Windows builds.
6. [PIX](https://devblogs.microsoft.com/pix/introduction/) for target GPU evidence once the Windows Direct3D 12 build exists.

All external references were inspected during this research, except where explicitly described as an additional learning link. Upstream documentation and default branches are mutable; freeze exact dependency revisions during Stage 0. The renderer comparison is source-based and does not constitute a comparative benchmark.

## Research verification

- Local documentation links and Markdown code fences were checked; the report is routed from the docs index.
- The repository health check found required files, Unity metadata pairing and generated-file tracking checks intact. Its whitespace check failed on the pre-existing edited Unity scene, outside this documentation task; that scene was left untouched.
- The original research commit changed only this report and its docs-index entry. The report now lives under `Engine/Docs/`; existing material and scene edits remain outside new-engine work.
- No engine build, runtime test, GPU benchmark, visual acceptance or Windows proof is claimed.
