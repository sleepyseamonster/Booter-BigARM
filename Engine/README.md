# Booter & BigARM — New Engine

This is the working home for the proprietary engine and the regular third-person version of Booter & BigARM. New work stays in this folder. The existing Unity project remains elsewhere in the same repository as reference.

Start here:

- [Working agreement](./AGENTS.md)
- [Accepted direction and scope](./Docs/DIRECTION.md)
- [Engine foundation research and proposed construction sequence](./Docs/PROPRIETARY_ENGINE_FOUNDATION_RESEARCH.md)

Current state: direction, research and workspace separation are recorded. No engine runtime, dependency installation or build system has been implemented yet.

The next implementation begins with a standalone engine foundation. The rock generator for the open Greater Wasteland is its first content workload; canyons remain deferred.

Future source, tools, assets and tests will be created here as their implementation stages need them. `build/`, `out/` and `.cache/` are reserved for ignored generated output. Do not put new-engine content into the Unity `Assets/` tree.
