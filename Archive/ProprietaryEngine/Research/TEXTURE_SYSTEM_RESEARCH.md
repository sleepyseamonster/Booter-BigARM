# Texture System Research

Researched 2026-09-10. Status: implementation recommendation, with an offline experiment completed; scene texture loading is still absent. [Direction](../Docs/DIRECTION.md), [architecture](../Docs/ARCHITECTURE.md), the [surface collection](../Assets/SurfaceLibrary/README.md) and pinned sources control this design. The [implementation plan](../Docs/TEXTURE_SYSTEM_PLAN.md) translates it into bounded work.

## Research contract

Done means identifying the actual assets and their semantics, testing the most consequential tool assumptions, recommending a source-to-GPU path, and specifying ownership, material and verification contracts. This task covers research, an isolated offline experiment and durable documentation. It does not implement the renderer, change dependency pins, port Unity shaders, accept final art or establish Windows performance.

Procedural compatibility is part of the design: texture identities remain independent of world seeds and generated-object IDs; chunks hold references to shared assets; authored recipes select materials and projection scales; unload cancels obsolete requests; persisted world deltas refer to logical material identities rather than GPU handles. The offline probe itself creates no world objects, chunks or persistence.

## Recommendation

Use the existing C++/bgfx/bimg stack. Build a small engine-owned offline texture cooker that understands color, normals and numerical channels. Start with complete, uncompressed KTX 1 mip chains and a bounded runtime loader. Add BC7/BC5/BC4 compression only after uncompressed reference rendering and semantic comparisons pass. Retain PNG and PSD originals unchanged. Do not ship the broad source-image decoder or texture encoders in the game merely because the offline cooker uses them.

**Do not adopt stock `texturec --mips` as the universal cooking path at the current pin.** TEXTURE-001 demonstrated that its mip behavior depends on the output format. A flag named `--linear` is insufficient to establish the required result.

KTX 1 is the initial container choice for this pinned integration. KTX2 is a valid future option, but the pinned generic parser used by `bgfx::createTexture` does not dispatch KTX2. The separate bimg KTX2 parser exists and rejects supercompression; it would require a deliberate adapter, validation and upload path. KTX2 support is therefore neither wholly absent nor ready through the current generic entry point. See [pinned parser dispatch](https://github.com/bkaradzic/bimg/blob/ddbeeae05779f84f97694553eb41605a60f86f0a/src/image.cpp#L5898) and [dedicated KTX2 parser](https://github.com/bkaradzic/bimg/blob/ddbeeae05779f84f97694553eb41605a60f86f0a/src/image.cpp#L4950).

## Asset evidence

The existing collection has 38 runtime-source PNGs and five editable source-art images. The 38 PNGs comprise 18 base-color, eight normal, five packed-surface, five height and two mask images. Thirty-two are 1024², five are 1254² and one is 1229². All are 8-bit. These are current manifest facts, not a proposed production resolution policy.

| Role | Interpretation | Initial runtime format | Later compression candidate |
|---|---|---|---|
| Base color | sRGB RGB; alpha handled separately as linear data | RGBA8 sRGB | BC7 sRGB |
| Normal | Linear encoded XYZ, decode to a direction | RGBA8 linear | BC5 XY, reconstruct positive Z |
| Packed surface | R = AO, G = source roughness, B = height, A = writer constant 1 | RGBA8 linear | BC7 linear, per-channel error gates |
| Crack mask | R = crack, G = halo, B = deposit, A = writer constant 1 | RGBA8 linear | BC7 linear; retain all three signals |
| Height | Scalar R, with material-specific scale and bias | R8 linear | BC4 linear after height-error checks |

This packing is **not ORM**: blue is height, not metallic. The existing rock materials also reuse the same SideAlbedo in several layer slots. Preserve the recorded bindings instead of assigning images by filename. Fifteen serialized material records resolve 186 texture bindings; only 24 images have references in those inspected records. Code-driven terrain/pebble bindings are a separate reference path. The [manifest](../Assets/SurfaceLibrary/manifest.json) and [material snapshot](../Assets/SurfaceLibrary/material-reference.json) retain that distinction.

A source roughness sample is not the final Unity response: the rock shader remaps it through smoothness bounds and wear adjustments. Transfer this mapping explicitly when pursuing that appearance; do not silently treat the source G value as an already calibrated PBR parameter. Raw normal PNGs contain encoded XYZ, not Unity's platform-imported GPU packing. Normal orientation must be established with an asymmetric test, not by flipping green merely because the backend is named DirectX.

## Experiment results and tool choice

[TEXTURE-001](./Experiments/TEXTURE-001/README.md) built the pinned offline tool and a bounded decoder on this Mac. It generated independent checker/normal inputs, converted and inspected all mip levels, and converted one real 1254² height image. [Final measurements](../Evidence/TEXTURE-001-final-measurements/measurements.json) retain command arguments, tool hashes and source identity through linked receipts.

| Measurement | Expected interpretation | Observed result |
|---|---|---|
| Black/white color checker, RGBA8 + mips | First mip about 188 sRGB | 127; fails color filtering |
| Same checker, BC7 + mips | About 188 sRGB | R188/G189/B189; passes this bounded color check |
| Numerical checker, BC7 + linear + mips | About 128 | R188/G189/B189; fails numerical filtering |
| Numerical checker, RGBA8 or BC4 + linear + mips | About 128 | 127; passes this bounded scalar check |
| Opposing X normal directions, BC5 normal mode | Averaged XY near 128/128 | 128/128; blue omitted by BC5, reconstruct Z in shader |
| Real 1254² height to BC4 | Any resolution change must be explicit | 1256²; source resize, not merely container padding |
| Repeated BC7 conversion | Identical bytes on this tool/build | Identical SHA-256 in two runs |
| Invalid input | Controlled rejection | Nonzero tool exit |
| Tool-produced KTX2 through generic core parser | Determine direct loader compatibility | Rejected; dedicated parser integration would be needed |

The research command passes when measurements are collected and its operational assertions hold. Some semantic results deliberately say **false**; that is the finding, not a completed texture-system test suite. The normal fixture does not establish angular quality on real assets. Determinism here covers one compiler/build and input, not cross-platform bit identity.

The [pinned BC7 mip branch](https://github.com/bkaradzic/bimg/blob/ddbeeae05779f84f97694553eb41605a60f86f0a/tools/texturec/texturec.cpp#L508) applies the color conversion around filtering without the linear-option guard used in its resize path. The RGBA8 observation also means we must test the executed build rather than infer behavior from one reference filter implementation. Upstream source was not patched.

| Candidate | Use now | Reason / revisit condition |
|---|---|---|
| Existing bimg decode/write primitives, engine-owned mip semantics | Recommended | No new top-level dependency; explicit numerical/color behavior; validate NPOT edges and formats ourselves |
| Existing texturec | Experiment tool; possible later base-level compressor | Tested semantic gaps preclude universal mip cooking; compression of project-built levels needs its own integration proof |
| KTX-Software | Deferred alternative | Provides explicit transfer-function and mip controls; evaluate if the small cooker becomes a maintenance burden, including transitive dependencies and pinned builds |
| Basis Universal | Deferred | Useful distribution/transcoding option when platform/package needs justify it; unnecessary complexity for the first Windows/Mac fixture |
| DirectXTex | Optional future Windows reference/tool comparison | Useful texture-processing tooling; no requirement to change daily workstation now |

Primary tool references: [bgfx texturec documentation](https://bkaradzic.github.io/bgfx/tools.html#texture-compiler-texturec), [KTX create options](https://github.khronos.org/KTX-Software/ktxtools/ktx_create.html), [Basis Universal](https://github.com/BinomialLLC/basis_universal), [DirectXTex](https://github.com/microsoft/DirectXTex). These alternatives were researched, not installed or benchmarked. Building the current offline targets also compiles bundled codecs such as AVIF/dav1d and compression libraries. Existing primary notices are not a complete transitive redistribution audit; inventory the actual linked tools before distributing them.

## Cooking and color contract

A versioned import recipe declares source identity/hash, semantic role, channel mapping, transfer function, alpha use, normal convention, resolution policy, wrap/filter settings, mip algorithm/version, target format and tool revision. Filenames and embedded image metadata cannot silently override it. Detect conflicting PNG color metadata and resolve it explicitly; do not guess a full color-management workflow from an importer checkbox.

For color RGB, decode sRGB to linear, filter there, then encode sRGB. Numerical channels and alpha use arithmetic filtering without a color transfer. Normals decode to vectors, average and renormalize, with a defined fallback for a near-zero mean. Generate each destination footprint correctly for odd dimensions and 1×N/N×1 tails; do not drop the last row/column. Repeat-wrapped material filters must use periodic edge treatment. Complete mips reach 1×1. BGFX's `hasMips` describes supplied storage; it is not an automatic mip generator. See [texture API](https://bkaradzic.github.io/bgfx/bgfx.html#_CPPv4N4bgfx15createTexture2DE8uint16_t8uint16_tb8uint16_tN13TextureFormat4EnumE8uint64_tPK6Memory8uint64_t).

For the first implementation, preserve source dimensions in uncompressed output. Do not silently use texturec's block-alignment resampling for compressed assets. Later choose and record either supported native NPOT block storage or an explicit, reviewed derived resolution. Padding a repeating material without a matching sampling contract can create seams.

Average height mips are adequate for scalar display; they do not establish conservative height bounds for parallax or displacement. Those features need separate reduction rules. Normal variance and roughness filtering can require later specular antialiasing work. Start with a documented perceptual roughness parameter and one deliberate mapping to the BRDF. The [Filament material reference](https://google.github.io/filament/main/filament.html#materialsystem/parameterization) is the technical reference for this distinction, not a proposal to adopt its renderer.

The GPU decodes sRGB base color once. Data textures remain linear. Scene lighting happens in linear space. OR-2 owns exposure, display encoding and UI composition so material code does not introduce a second gamma conversion. Calibration must bypass artistic tone mapping when comparing known values.

## Storage and platform policy

All 38 PNGs occupy 78,609,936 disk bytes (~75.0 MiB). An all-RGBA8 full-mip estimate is 228,928,400 bytes (~218.3 MiB). Using R8 for heights and RGBA8 for the rest gives ~194.3 MiB. A hypothetical BC7/BC5/BC4 set using the measured block-aligned resize policy gives 53,091,848 bytes (~50.6 MiB). These are calculated payload estimates across the whole collection, not measured VRAM, a residency target or permission to resize. They exclude temporary decode/upload buffers, allocation overhead and duplicate resources.

BC4 uses 8-byte 4×4 blocks; BC5 and BC7 use 16-byte blocks. D3D11 feature level 11 supports the proposed BC7 path. [Microsoft format table](https://learn.microsoft.com/en-us/windows/win32/direct3d11/texture-block-compression-in-direct3d-11). Metal exposes a device BC-compression capability; the existence of Mac support does not prove the current device/backend combination passed upload and sampling. [Apple capability API](https://developer.apple.com/documentation/metal/mtldevice/supportsbctexturecompression).

Query bgfx native format capabilities, sRGB support, maximum dimensions and sampler limits at runtime. Do not count emulated format support as native compression proof. Keep a bounded uncompressed fallback for development and clear diagnostics. An ASTC-only Mac asset branch is not justified yet. Native Windows configure/build, D3D shader compilation, upload, sampling and measurements remain separate required evidence. KTX is a container; KTX2/Basis supercompression and GPU-native BC compression are different decisions. [KTX specification](https://registry.khronos.org/KTX/specs/2.0/ktxspec.v2.html).

## Resource and material contracts

Use three distinct identities: stable logical asset ID for authoring/persistence, content key for a specific cooked artifact, and generation-checked runtime handle. The content key hashes canonical recipe bytes, source bytes, tool/filter versions and target profile. Store relocatable Engine-relative paths and atomically publish completed artifacts under ignored `out/`; retain the recipe and provenance in Git. A source edit updates content without changing logical identity or world-object IDs.

A shared resource manager owns texture residency. Material instances and chunks reference it; chunks do not each decode/upload identical images. The renderer owns GPU creation/destruction. Start with bounded synchronous loading, explicit byte accounting and reference counts. Preserve a seam for worker decode and bounded render-thread upload later. Reject late results whose request generation/owner epoch is obsolete. Failed reload keeps the last valid resource; first-load failures use role-specific fallback textures and actionable diagnostics. Retire replaced handles through the renderer after submissions stop using them.

Validate paths, file size, header dimensions, layer/mip counts, channel/transfer compatibility and overflow-safe expected payload ranges before upload. Version one supports only ordinary 2D, single-layer textures. Use `bgfx::copy` initially so local buffers may be released after the copy. Any later `makeRef` optimization must retain storage until its release callback, which can run on any thread. [Pinned memory API](https://github.com/bkaradzic/bgfx/blob/9b636df330c81e11c84595651a291b6c59fb7396/include/bgfx/bgfx.h#L2599). Parser acceptance alone is not a complete untrusted-file validator.

Begin with one opaque material: base color, tangent normal, packed AO/roughness/height and explicit constants. Stone/sand metallic defaults to zero. Separate immutable material definitions from per-instance variation and sampler policy. Provide deliberate fallbacks: white base modulation, flat normal, AO 1, documented roughness and neutral height. AO is not a substitute for directional shadows.

Prove UV orientation and tangent handedness on a labeled plane and sloped solid first. Generated rocks later need world/object triplanar projection and correctly oriented per-axis normal frames; blending tangent normals as world vectors is invalid. Scale textures in explicit world units. Preserve projection phase across chunk edges and future origin shifts, and decide intentionally whether a movable object uses object-anchored or world-anchored coordinates.

The layered reference material can consume many texture bindings before shadows/environment lighting. Inventory actual unique bindings and queried sampler limits before porting all layers; repeated albedo slots can share a binding. Texture arrays are a later option only when dimensions/formats/mips match. Full terrain splatting, pebble parallax, virtual texturing, bindless resources, automatic mip streaming and a material graph editor are deferred. The current ImGui adapter accepts its fixed atlas and rejects custom IDs, so thumbnails need an explicit adapter change in a later batch.

## Remaining evidence

Research is sufficient to begin OR-2 and the staged texture implementation. It does not prove a correct cooker exists. The next plan requires independent mip oracles, asset/channel fixtures, guarded resource lifetime checks and GPU captures. Compression quality, real normal-map orientation, triplanar seams, final art acceptance and Windows performance remain open. Revisit tools when those tests expose a concrete limitation; do not extend this into a general engine survey.
