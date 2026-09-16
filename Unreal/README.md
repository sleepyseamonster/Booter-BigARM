# Booter & BigARM — Unreal Production

This is the sole active production area for Booter & BigARM. Unreal Engine supplies the production renderer, physics, animation, navigation, audio, editor, cooking and Windows platform path. A standalone portable core owns the game-specific procedural and durable systems that should survive a future host change.

## Start Here

1. Read [the working agreement](./AGENTS.md).
2. Read [accepted direction](./Docs/DIRECTION.md).
3. Read [the architecture boundary](./Docs/ARCHITECTURE_BOUNDARY.md).
4. Read [current status and next work](./Docs/STATUS.md).

## Layout

| Path | Ownership |
|---|---|
| [Project/](./Project/README.md) | Unreal host project and integration plugins |
| [PortableCore/](./PortableCore/README.md) | Engine-neutral C++ world/game kernel |
| [Docs/](./Docs/STATUS.md) | Current plans, architecture, decisions and evidence routing |
| [Tools/](./Tools/README.md) | Automation and migration tools |
| [Evidence/](./Evidence/README.md) | Retained verification receipts |
| [References/](./References/README.md) | Links to preserved Unity and proprietary-engine sources |

No `.uproject` has been created yet. The first implementation step is to select and verify the Unreal version and toolchain, then create the host project and portable-core plugin boundary without importing old projects wholesale.
