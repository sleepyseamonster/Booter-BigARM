# First-pass texture pipeline

P03 (T1/T2) implementation, 2026-09-10. The engine now cooks, validates, shares, previews and packages the transferred textures. PBR lighting/material interpretation remains P05; this is an unlit texture inspection path, not Unity shader parity or finished rock rendering.

## What is implemented

The legacy teal-luster mask remains uninterpreted linear mask data; it does not inherit the crack/halo/deposit labels. [The final recipe correction](../Evidence/P03-accepted-cooking.json) changes only cook identity: every image payload remains byte-identical to the GPU-verified bundle. No extra GPU rerun was needed.

The [recipe library](../Assets/TextureRecipes/library.json) defines the 38 transferred textures plus three small diagnostic fixtures. Every recipe declares logical ID, source, semantic role, transfer function, channel meaning, independent linear alpha, positive-Y normal convention, dimension preservation, repeat edges, area-box filter version and RGBA8 KTX1 output. Unsupported combinations fail explicitly.

The C++ offline cooker uses the pinned bimg PNG decoder and KTX payload writer. Engine code builds the mip chain: color RGB is decoded to linear before area averaging; alpha and data channels remain linear; normal XYZ vectors are decoded, averaged and renormalized with a positive-Z fallback for cancelling vectors. Floating-point intermediates avoid repeatedly quantizing lower mips. Area footprints include odd-dimension tails and work for one-dimensional chains. Aligned box footprints cover the full repeating period without inventing edge samples. Inputs are restricted to noninterlaced PNG8 grayscale/RGB/RGBA with a validated IHDR and dimensions at most 2048 per axis.

The exact pinned bimg writer leaves `glType` and `glFormat` zero for this uncompressed output. The cooker canonicalizes those fields to `GL_UNSIGNED_BYTE` and `GL_RGBA`, then validates the result with the strict engine parser and the generic bimg parser. The [KTX1 specification](https://registry.khronos.org/KTX/specs/1.0/ktxspec.v1.html) defines the uncompressed fields and mip payload layout. Source archives remain unmodified.

Cooked keys include the canonical recipe, source SHA-256 and cooker/orchestrator/cohort identity. The portable catalog records logical ID, content key, role, dimensions, complete mip count, resident/file byte counts, output SHA-256 and CRC32. SHA-256 binds build/package artifacts; runtime CRC32 detects payload corruption alongside strict structural validation. Runtime code accepts only little-endian 2D RGBA8 KTX1 with a complete chain, no arrays/cubemaps or metadata, and consistent transfer function, dimensions and payload sizes. It rejects trailing, truncated and oversized data before GPU upload.

The renderer owns a `TextureStore`. Move-only leases share one allocation for repeated requests; weak tokens include generations and cannot revive a released or replaced slot. Releasing one lease retains other owners; final release destroys the GPU resource and restores byte accounting. Store shutdown also invalidates outstanding leases safely. Failed acquisition leaves a prior lease unchanged. Explicit fallback images cover color, normal, packed surface, height and mask roles.

Residency is bounded to 256 MiB per store; the file/per-image profile is bounded to 32 MiB and 2048 per axis. Synchronous admission is the first-pass policy. A valid larger-than-frame-budget image is admitted as a bounded startup/inspection operation; this does not claim asynchronous streaming or the later per-frame upload scheduler. Per-operation CPU staging includes the decoded mip chain, packed upload and bgfx-owned copy; its maximum RGBA8 payload profile is below 64 MiB after the original file buffer has been released. Outstanding bgfx copies across consecutive admissions are not yet governed by a global staging queue; P19 must add that before bulk streamed loading. These are payload figures, not measured total process or driver memory.

World/generated identity remains separate from logical texture IDs, content keys and GPU tokens. Future chunk owners acquire/release leases; persisted deltas reference logical assets. Authored channel/normal conventions live in recipes. This batch implements no chunk scheduler or persisted world-delta format, so it cannot claim late-job cancellation or streamed-world behavior.

## Workbench and package

`--catalog` loads a cooked catalog. The texture inspection section provides asset selection, RGB/single-channel views, explicit mip selection and repeated UVs. Numeric data previews preserve encoded byte values; color previews traverse hardware sRGB sampling and the established linear/display pipeline. The previous geometric fixture remains available by disabling texture preview.

Installed packages optionally include the catalog and cooked textures beside the executable under `Assets/`. The workbench discovers that catalog automatically; source art and the dependency cache are not runtime requirements. The offline decoder/cooker is a separate build target rather than a runtime import service.

From Engine:

```sh
cmake --build build/foundation --parallel 4 --target engine_texture_cook engine_workbench engine_texture_tests
python3 Tools/cook_textures.py --cooker build/foundation/engine_texture_cook --recipes Assets/TextureRecipes/library.json --out out/my-surfaces
build/foundation/engine_workbench --catalog out/my-surfaces/catalog.json
python3 Tools/package_workbench.py --build build/foundation --catalog out/my-surfaces/catalog.json --out out/my-textured-workbench
out/my-textured-workbench/bin/engine_workbench
```

Choose new output directories. Cook failures preserve original sources and leave the failed output for inspection. The full 41-image uncompressed catalog accounts for 228,928,652 resident payload bytes if all images are resident simultaneously; the inspection path loads its selected image, not the entire catalog. Compression remains a later measured gate.

## Evidence

- [Two complete final cooks](../Evidence/P03-cooking-final.json) produced byte-identical output for all 41 textures using the same final tool. Source and semantic recipe variants changed keys; reinterpreting the diagnostic color image as linear data changed its final RGB mip from 188 to 128. Dimensions 1024², 1229² and 1254² are retained; all 43 transferred texture/source-art files retain their original hashes.
- [Four native test suites](../Evidence/P03-native-tests/result.json) include semantic RGB/alpha/data/vector checks, cancelling normals, NPOT/one-dimensional/repeat cases, rejection at every truncated byte count, unsupported KTX fields, catalog confinement, metadata mismatch and CRC failure.
- [Tool tests](../Evidence/P03-tool-tests/result.json) cover recipe contracts and independent pixel-oracle failures. The existing roadmap test now explicitly clears evidence in its missing-evidence case, so real package completion no longer invalidates the test setup.
- [Native Metal evidence](../Evidence/P03-native-final/result.json) retains the previous geometry/color checks and adds six texture captures. Quadrant orientation, known color mip values, normal bytes and packed-channel values match. Six sampled rock pixels and six ground pixels match their original transferred PNGs through the GPU path.
- [Native resource checks](../Evidence/P03-native-final/captures/texture-resources.json) verify two shared references, failed replacement preservation, stale-token rejection, all five fallback roles, budget rejection and 100 replacement/release cycles. GPU texture count returns from 3 to 3; managed resident bytes return to zero.

The first native attempt caught an argument-routing error: `--catalog` was mistakenly routed to the inspection output argument, overwriting only the generated scratch catalog. Input-hash verification rejected that run. The routing is corrected; fresh cooked catalogs replace the scratch output, and original source assets were unchanged. The [failed receipt](../Evidence/P03-native-initial/run/result.json) remains distinguishable from passing GPU evidence.

These are Mac technical checks, not physical input, artistic acceptance, PBR, Windows or target-PC performance proof. Next are P04 directional shadows and P05 rock/ground materials; P08 simulation is independently ready. P06 will own saved texture/material inspection commands and broader editor workflow.
