# Superseded Unreal Direction

Confirmed by the user on 2026-09-16 and superseded on 2026-09-21 when the user returned production exclusively to Unity. This document is retained as historical rationale and does not direct new work.

The superseded direction selected Unreal Engine as the sole active production host and targeted a regular third-person, fully 3D Windows PC game with high-quality terrain, rock, lighting, material, animation and traversal.

The superseded plan also proposed a portable C++ game/world kernel beneath the Unreal integration. That kernel would have preserved deterministic generation, geological and terrain logic, stable identity, authored constraints, procedural recipes, validation, durable world deltas and other game-specific truth.

Under that superseded plan, Unreal would have owned production rendering, Lumen/Nanite-era visual capabilities where selected, physics, character presentation, animation tooling, navigation runtime, audio, input, editor integration, cooking and packaging.

Under the current direction, new implementation belongs to the repository-root Unity project. `Unreal/`, `Engine/`, and the isolated Unity 2D legacy content remain preserved references. Nothing is deleted or bulk-migrated by the 2026-09-21 direction change.

The historical plan excluded Mac-specific engine behavior and deferred Windows-dependent proof until native Windows access. Those constraints do not direct the active Unity lane.
