# Manifold Native Dependency

## Purpose And Authority

Booter & BigARM uses the Manifold C API only for runtime Boolean union of multi-member physical rock formation visuals. `TopDown3DManifoldNative` is the sole runtime interop owner. The dependency does not own formation placement, identity, streaming policy, collision, materials, or save data.

## Pinned Source

- Project: <https://github.com/elalish/manifold>
- Version: `v3.5.2`
- Commit: `11235e6b8ebea2dbed8aec4285685aafd3d95667`
- Source archive: <https://github.com/elalish/manifold/archive/refs/tags/v3.5.2.tar.gz>
- Source archive SHA-256: `1e17743a7a0a2c07e9258f5618494c5caa2527063af31b46e4c24947657fe5ef`
- License: Apache-2.0; the upstream text is preserved in `Docs/ThirdParty/Manifold/LICENSE`.

The upstream CMake configuration pins its fetched build dependencies. The project build keeps internal Manifold parallelism off, enables the C binding, builds universal `arm64;x86_64` binaries, and targets macOS 12.0 or newer.

## Supported Proof Boundary

The checked-in binaries are enabled only for the macOS Unity Editor and macOS Standalone player. No Windows, Linux, console, mobile, or production-player compatibility is claimed. Adding another target requires a platform decision, a pinned build for that target, importer validation, focused ABI tests, and profiling.

## Current Verified Artifacts

Built on 2026-08-14 with Apple Clang 21.0.0 and CMake 3.28.1:

- `libmanifold.dylib`: SHA-256 `d11e68c985dee63b57dbf54d18f6ec030caf348cd893ee76dad225a40fa1dc70`
- `libmanifoldc.dylib`: SHA-256 `e7f3f0fea0482e89d16e5490ae68c2d2e47b4872153501cf764a6b9d87847dbb`

Both binaries contain `arm64` and `x86_64`. `libmanifoldc.dylib` resolves its sole project-shipped dependency through `@loader_path/libmanifold.dylib`.

The upstream universal build passed all 427 upstream tests. A direct C ABI probe then unioned two overlapping unit cubes and proved status `MANIFOLD_NO_ERROR`, one connected component, 16 output vertices, 28 triangles, volume `1.5`, extraction, and explicit cleanup.

## Rebuild

Close the Unity Editor for this checkout, then run:

```sh
Tools/Native/Manifold/build-macos.sh
```

The script refuses to replace a native plugin while `Temp/UnityLockfile` exists. It downloads the pinned source archive, verifies the archive checksum, builds and runs upstream tests, installs the two universal libraries, repairs the loader-relative dependency, and prints artifact hashes. Preserve the existing Unity `.meta` files and their GUIDs when replacing the binaries.

An upgrade requires a separately reviewed version/commit/checksum change, a clean upstream test run, the project ABI and rock-fusion tests, current-platform profiling, documentation updates, and new artifact hashes.
