# Accepted Unreal Direction

Confirmed by the user on 2026-09-16.

Booter & BigARM will use Unreal Engine as its sole active production host. The game remains a regular third-person, fully 3D Windows PC title. The target is finished visual and experiential quality comparable to a high-quality Unreal game within this project's specialized terrain, rock, lighting, material, animation and traversal needs.

The project will also develop a portable C++ game/world kernel beneath the Unreal integration. That kernel preserves deterministic generation, geological and terrain logic, stable identity, authored constraints, procedural recipes, validation, durable world deltas and other game-specific truth that could be hosted by a proprietary engine years later.

Unreal owns production rendering, Lumen/Nanite-era visual capabilities where selected, physics, character presentation, animation tooling, navigation runtime, audio, input, editor integration, cooking and packaging. The project will not build parallel proprietary versions of these systems merely to preserve theoretical portability.

All new implementation belongs under `Unreal/`. The root Unity project, its isolated 2D legacy content and `Engine/` remain preserved references. Nothing is deleted or bulk-migrated by this direction change.

Mac-specific engine work is excluded. Platform-neutral work may proceed on the available host. A lane stops when its next credible proof or implementation requires native Windows behavior.
