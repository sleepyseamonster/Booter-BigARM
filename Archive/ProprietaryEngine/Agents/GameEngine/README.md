# Game Engine Agent

This is the owner workspace for building the Booter & BigARM proprietary game engine.

The agent owns engine runtime architecture, rendering, platform integration, resource and asset pipelines, simulation, world generation, streaming, persistence, authoring APIs, performance work, native tools and their technical verification. The product direction remains regular third-person, fully 3D, Windows-targeted, and developed on Mac while practical.

## Start here

1. Read [the agent agreement](./AGENTS.md).
2. Read [the engine agreement](../../AGENTS.md), [accepted direction](../../Docs/DIRECTION.md), and [current status](../../Docs/STATUS.md).
3. Use [the research map](./Research/README.md) to select the smallest relevant source-backed research slice.
4. Follow the applicable [SOP](./SOPs/README.md), then use the canonical engine tools under [../../Tools](../../Tools/README.md).
5. Record implementation results in the canonical engine documentation and evidence locations; this folder is the agent's control plane, not a second runtime tree.

## Durable homes

| Concern | Canonical home |
|---|---|
| Runtime, applications, assets and tests | `Engine/Source`, `Engine/Apps`, `Engine/Assets`, `Engine/Tests` |
| Product direction and implementation roadmap | `Engine/Docs` |
| Research, references, dependency locks and licenses | `Engine/Research` |
| Repeatable build, preparation and verification tools | `Engine/Tools` |
| Generated output and receipts | `Engine/build`, `Engine/out`, `Engine/Evidence` |
| Viewer and scene UI specialist work | `Engine/UIUX` |

The canonical stores remain where they are so existing links, evidence provenance and build workflows do not fork. This workspace indexes and governs them; it does not silently duplicate or relocate them.
