# Unreal Production Status

Updated 2026-09-16.

## Current state

- `Unreal/` is the sole active production area.
- Repository routing and preservation boundaries are established.
- The portable-core/Unreal-host dependency contract is established.
- The Unity 3D project, isolated Unity 2D legacy content and proprietary `Engine/` remain unchanged as reference implementations.
- No Unreal version, `.uproject`, plugin module, migrated asset or runtime capability has been selected or created yet.

## Next coherent package

Perform the Unreal foundation and migration audit:

1. inventory installed/available Unreal versions and the platform toolchain;
2. select a production starting version from verified requirements;
3. classify existing proprietary-engine and Unity work as portable core, Unreal adapter/input, reference-only or obsolete;
4. define the first vertical proof: a standalone deterministic terrain-domain result consumed by an Unreal plugin;
5. create the minimal project/plugin/core build only after those choices are recorded.

The audit must not modify, launch or bulk-import the preserved Unity and proprietary implementations. It must distinguish a successful project compile from editor execution, rendered proof, visual acceptance and Windows target proof.
