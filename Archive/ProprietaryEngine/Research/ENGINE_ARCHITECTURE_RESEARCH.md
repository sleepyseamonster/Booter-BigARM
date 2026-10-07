# Engine Architecture Research Update

Researched 2026-09-10 for the whole-engine plan. This updates the scope of the [original foundation survey](../Docs/PROPRIETARY_ENGINE_FOUNDATION_RESEARCH.md); it does not replace its historical platform comparisons or the exact existing source lock. The [system audit](./ENGINE_SYSTEM_AUDIT.md) supplies local facts; the [master plan](../Docs/FOUNDATION_PLAN.md) owns execution order.

## Recommendation and evidence class

Keep the working C++20/CMake/SDL3/bgfx/bimg/ImGui foundation. Own the game-specific architecture: world and entity identity, generation and streaming policy, material semantics, asset cooking, simulation ownership, authoring and persistence. Integrate established libraries for bounded specialist work. Do not build a physics solver, skeletal clip compressor, general JSON parser, audio backend or navigation-mesh implementation from scratch.

The following are **preferred engineering directions**, not newly installed or verified dependencies. Current selected versions remain unchanged. At each adoption, choose one immutable compatible revision, collect notices/transitive dependencies, build a bounded test on Mac and schedule native Windows proof. A library's upstream feature list is capability evidence, not proof of our integration. Avoid unbounded alternative surveys unless the preferred option fails a recorded requirement.

## Build versus integrate

| Area | Default for this engine | Engine-owned responsibility and adoption test |
|---|---|---|
| Platform and input | Existing SDL3 | Gameplay/UI/system action contexts, focus loss, controller lifecycle, remapping and saved settings. SDL's gamepad API normalizes controller input; it does not define player movement. [SDL gamepad API](https://wiki.libsdl.org/SDL3/CategoryGamepad) |
| Graphics | Existing bgfx; Metal development and the current D3D11 Windows path | Pass order, materials, visibility, shadows, effects and performance. Examples demonstrate instancing, HDR, shadow maps and LOD techniques; they are references, not drop-in game systems. [bgfx examples](https://bkaradzic.github.io/bgfx/examples.html) |
| Texture processing | Existing bimg primitives with engine-owned semantic mip processing | Versioned import recipes, channel/transfer correctness and validated runtime resources. The [measured texture limitations](./TEXTURE_SYSTEM_RESEARCH.md) rule out a universal stock texturec mip path at our pin. |
| Entity/component storage | Prefer EnTT at the first shared gameplay world | Own fixed system order, StableObjectId mapping, explicit component schemas and lifecycle commands; use the registry portion only. Prove create/remove/stale-handle and deterministic ordered-command behavior. EnTT supplies C++ component storage and views. [EnTT](https://github.com/skypjack/entt) |
| Collision | Prefer Jolt, initially CharacterVirtual plus static ground/rock collision | Own motor intent, fixed-step ordering, filters, queries, body lifecycle and chunk readiness. Prove slopes/steps/overlaps and contact events. CharacterVirtual is query-driven; ordinary body queries/sensors need an explicit inner-body or character-query policy. [Jolt character controllers](https://jrouwe.github.io/JoltPhysics/#character-controllers) |
| Mesh import | glTF/GLB with fastgltf offline | Validate supported extensions, transforms, indices, UV/tangents, materials and skins. Convert into versioned engine-owned assets; reject unsupported required extensions instead of dropping meaning. [glTF](https://www.khronos.org/gltf/), [fastgltf](https://github.com/spnda/fastgltf) |
| Mesh preparation | Prefer meshoptimizer when indexed/LOD assets arrive | Own silhouette/error and seam constraints, collision representation and selected runtime LOD. Reordering/simplification are library tools, not permission to erase authored boundaries. [meshoptimizer](https://github.com/zeux/meshoptimizer) |
| Animation | Prefer ozz-animation | Own locomotion/state transitions, motor/root-motion policy, event timing, skeleton mapping and renderer skinning. Ozz supplies runtime sampling/blending and offline conversion; its tools have dependencies distinct from its runtime. [ozz-animation](https://github.com/guillaumeblanc/ozz-animation) |
| Navigation | Prefer Recast/Detour for detailed local tiles | Own agent profiles, route graph, tile versioning, chunk integration and BigARM's coarse travel. Recast builds navigation data; Detour provides navigation queries and tiled data workflows. [Recast/Detour](https://github.com/recastnavigation/recastnavigation) |
| Sound | Prefer miniaudio as the single playback engine | Own event IDs, cues, listener/source transforms, buses, limits, subtitles/captions and streaming lifecycle. Prove positional playback, unload and device-loss recovery. Miniaudio supplies low/high-level playback and spatialization facilities. [miniaudio manual](https://miniaud.io/docs/manual/index.html) |
| Developer tools | Existing ImGui with deliberate renderer-adapter extensions | Own recipe documents, commands/undo, selection, asset inspection and validation. Extend custom texture support before thumbnails; current adapter rejects arbitrary texture IDs. |
| Player UI | Prefer RmlUi when the expedition HUD/settings enter production | Own gameplay bindings, gamepad focus, accessibility and shared input arbitration. Implement/test a bounded bgfx render adapter; do not embed a browser. RmlUi provides C++ markup/style UI, data binding and localization hooks. [RmlUi documentation](https://mikke89.github.io/RmlUiDoc/) |
| Structured documents | Prefer nlohmann/json | Own schemas, bounded reads, integer/ID representation, errors, migrations and transactional writes. A parser's exception/error modes do not replace validation. [Parser error handling](https://json.nlohmann.me/features/parsing/parse_exceptions/) |
| Scheduling and math | Bounded engine-owned queue using standard C++ primitives; retain current bx math/adapters initially | Own cancellation, priorities, bounded memory and deterministic adoption; coordinate Jolt worker usage to avoid oversubscription. Do not add a general task-graph package or second math library before a measured need. |
| Profiling | Existing counters plus Tracy when jobs/simulation need attribution | Own workload definitions, budgets, percentile reports and regressions. Tracy provides CPU/GPU/memory profiling facilities; verify the backend-specific integration instead of assuming every metric works. [Tracy](https://github.com/wolfpld/tracy) |
| Packaging | Existing CMake acquisition, then presets/install rules and CPack | Own relocatable runtime assets, configuration, notices, debug-symbol archive and clean-machine verification. Match preset schema to the declared minimum CMake version. [Presets](https://cmake.org/cmake/help/latest/manual/cmake-presets.7.html), [CPack](https://cmake.org/cmake/help/latest/module/CPack.html) |

Flecs was considered for entity storage; its broader framework is useful, but the narrower EnTT registry fits an engine-owned update schedule without adding a second world/scheduling authority. This is an architectural preference, not a comparative performance result. [Flecs documentation](https://www.flecs.dev/flecs/). Assimp/FBX-first import, a new scripting VM and a package-manager migration are deferred; none solves the current source problem better than a deliberately limited glTF/data/C++ path.

No new rendering backend, network service, paid middleware or global installation is needed for this planning result. The [dependency inventory](./dependencies.json) distinguishes current pins from proposed additions.

## Architectural decisions driven by the game

### One runtime, two applications

Workbench and game player should consume the same runtime library, asset records, material shaders, physics adapters and world services. The workbench is a client of document commands and inspection APIs. The game application is a client of input commands and gameplay state. Extract reusable services when their first second consumer appears; do not duplicate a separate preview simulation or first rewrite every fixture class into an abstract framework.

Start with explicit ordered phases: sample platform input; build action commands; run bounded fixed simulation ticks; adopt completed work at declared boundaries; evaluate/interpolate presentation; cull and submit scene passes; compose UI; retire resources. Callbacks from physics/audio/workers enqueue bounded events rather than mutating unrelated world structures. Define stable ordering where game decisions depend on event order.

### Persistent identity and mutable residency

A world contains logical objects whether their render mesh exists or not. Keep StableObjectId, transient ECS entity, asset logical ID, cooked content key and GPU/body/nav handles distinct. Region ownership includes a request epoch. Rejected or cancelled work never adopts resources into a newer owner with the same temporary index.

Represent world locations with an integer region address plus local high-precision position. Convert only nearby geometry/physics/presentation to local coordinates. Region span is versioned technical configuration, not geographic canon. Test negative boundaries, large indices, origin changes, range overflow and material projection phase. Heightfield terrain is appropriate for initial open-ground patches; overhanging rock meshes and future non-heightfield geometry must remain valid through the same surface/query interface.

### Determinism is several contracts

Require exact IDs and discrete generation choices from canonical integer inputs, stable iteration and versioned PRNG/hash behavior. Use declared geometry tolerances or canonical baked assets for floating-point output. Physics determinism is separate: Jolt documents build/order requirements and query/callback ordering caveats. Do not promise cross-platform replay merely by enabling an option. [Jolt deterministic simulation](https://jrouwe.github.io/JoltPhysics/#deterministic-simulation).

Initially save authoritative gameplay state and world deltas; do not depend on bit-identical future physics replay to restore a game. Recorded inputs/ticks are a debugging tool, not a public replay format. Generator changes must not reinterpret existing depleted/removed object IDs. Pin a save to generator/content versions until an explicit migration is verified.

### Traversability is a generation input

Terrain/formation placement must respect authored corridors, exclusion regions, slope/clearance classes and both character profiles. A macro route graph represents known traversal beyond detailed tiles. Detailed navigation then refines local movement over the same generated surfaces. A route claim cannot ignore terrain/collision versions or imply arbitrary straight-line travel through unloaded obstacles.

BigARM's coarse mode advances along a validated route with bounded physical speed and persisted progress. Re-entry requests detail at his simulated coordinate. If collision/nav readiness or route validity fails, hold and replan; do not relocate him to the player. Only one mode advances him during a handoff. This policy is engine-owned and is not supplied by Recast/Detour.

### Persistence before content scale

Use versioned, bounded JSON documents initially for authored recipes, settings and small save snapshots. Store larger region deltas as separately versioned records when needed. Commit a consistent save generation through a manifest/checkpoint protocol so a crash cannot mix new player state with old cargo or region state. Keep a last-known-good generation and test interruption at each write/replace boundary on both operating systems. Do not assume a single standard-library rename call supplies a complete durable transaction.

Authored defaults, generated base data, mutable runtime deltas and regenerable caches are separate. An object removal is a tombstone against stable identity. Saved inventory/transfer commands need transaction identity and idempotent recovery. Cache invalidation may delete disposable cook outputs; it must never delete player changes.

### Renderer scope

The essential path is forward opaque rendering with ordered frame passes, explicit linear color/HDR/display handling, directional shadows, PBR textures, ambient/environment light, visibility, instancing and LOD. Add stable shadow cascades when the visible world exceeds the first bounded shadow volume. Add sky/fog/dust and a small particle/decal system for readability and game events. Provide diagnostic modes and scalable settings before expanding content density.

Start with a supported conventional antialiasing path and measured results; do not introduce temporal AA, motion-vector history, upscaling or complex occlusion until shimmering/aliasing or GPU cost demonstrates the need. Reserve transform-history and pass interfaces so later work has a clear owner. Terrain texture streaming and larger asset systems arrive only after residency is measured; virtual textures and virtualized geometry are not baseline requirements.

### Art and tool interoperability

Preserve editable art separately from cooked runtime data. A DCC tool that exports the accepted glTF subset can feed the engine; Blender is a practical option, not a required new purchase or installation. Export is not shader-graph parity: support explicit material channels and record unsupported constructs. [Khronos Blender glTF exporter documentation](https://github.com/KhronosGroup/glTF-Blender-IO/blob/main/docs/blender_docs/scene_gltf2.rst).

Use a small provenance-recorded animated proxy and synthetic cues to prove infrastructure before final character assets. A real content pipeline still needs rigs/clips, audio, UI typefaces and rights-cleared assets. Those authoring dependencies belong in the roadmap; an engine cannot generate final creative acceptance from technical tests.

## Platform and inspection policy

Continue daily work on Mac. Add native Windows build/shader/package evidence early, then GPU and controller evidence on actual Windows hardware. Use the current D3D11 path first. Only switch daily development when a measured platform problem or target performance investigation warrants it. Backend capability queries and tagged receipts prevent Mac results from being mislabeled as product results.

RenderDoc is a candidate for Windows D3D11 GPU inspection; its supported graphics APIs do not include Metal. Use Apple's Metal tooling for Mac-specific frame investigations. [RenderDoc upstream](https://github.com/baldurk/renderdoc), [Apple Metal debugger](https://developer.apple.com/documentation/xcode/metal-debugger). No debugger was installed or capture taken in this planning task.

## Research limits and refresh triggers

Sources were read on 2026-09-10. Unpinned upstream pages describe current upstream behavior and may drift; inspect the selected source revision during each integration. The direct Blender manual URL failed to fetch, so the exporter project's primary documentation was used. RenderDoc documentation access failed, so its upstream repository supplied the capability reference. No third-party commentary or proprietary-engine marketing claim is used as implementation proof.

This research is sufficient to select the next architecture and integration path. Remaining empirical work is attached to actual milestones: physics controller/contact behavior, skeleton/skin import, nav-tile lifecycle, audio/UI backend integration, Windows execution and representative performance. It does not justify installing every proposed library now or promise a calendar date for completing the game.
