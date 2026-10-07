# Portable core and Unreal host boundary

## Ownership

The portable core owns game-specific, deterministic and durable truth:

- world, region, feature and generated-object identity;
- terrain/geology fields and surface semantics;
- rock and formation recipes and placement decisions;
- authored constraints, protected routes and procedural validation;
- persisted runtime deltas and engine-neutral save records;
- structured AI operations and result schemas where they do not require Unreal state.

The Unreal host owns production services:

- rendering, materials, lighting, shadows, atmosphere and effects;
- Unreal asset import, cooking, streaming and derived data;
- Chaos collision/physics integration;
- character presentation, Control Rig, animation graphs and Motion Matching;
- navigation runtime, audio, input, UI, editor integration and packaging.

## Dependency rule

`Project -> PortableCore` is allowed. `PortableCore -> Project/Unreal` is forbidden.

PortableCore public headers contain standard C++ or explicitly adopted portable dependencies only. They contain no `UObject`, `AActor`, `FVector`, `FString`, Unreal reflection macro, Unreal container, asset path or Unreal build assumption. Host adapters convert values at the boundary and retain transient Unreal handles outside durable records.

## Data flow

1. A versioned recipe, seed, integer world address and authored constraints enter PortableCore.
2. PortableCore produces deterministic engine-neutral results: fields, samples, semantics, stable IDs, placement records or mesh-ready buffers.
3. An Unreal plugin validates the result revision and translates it into Landscape, PCG, Nanite/static mesh, material, collision, navigation or actor state.
4. Accepted gameplay changes return as bounded stable deltas rather than serialized Unreal object identity.
5. The core remains runnable through a standalone command-line test without launching Unreal.

## Portability limit

Portability protects the expensive game logic and content definitions. Unreal materials, animation graphs, Control Rigs, Niagara systems, level sequences and host optimization are allowed to remain Unreal-specific. A future proprietary host would replace those adapters and presentation assets rather than receive them automatically.

## Guardrail

Do not create mirrored proprietary implementations of Unreal systems. A new abstraction requires a current game-specific ownership need, independent test value or credible future-host boundary. Speculative wrappers that merely rename Unreal APIs are rejected.
