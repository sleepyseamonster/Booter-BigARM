# Outdoor Rendering Foundation

Planned 2026-09-09 under the user's continuing implementation authority. This extends the completed [application fixture](./FOUNDATION_RESULT.md) before the rock workbench. [Direction](./DIRECTION.md) controls product scope; live source and retained receipts control implementation claims.

Scheduling update 2026-09-10: [the whole-engine master plan](./FOUNDATION_PLAN.md) now owns program order. OR-2–OR-5 map to P02/P04/P05/P06, with P03 supplying textures. Independent core/document/build/cooker work may proceed alongside renderer work when its prerequisites are met. OR labels remain completion contracts; they are not a requirement to finish each unrelated workstream serially.

## Done and Scope

Provide a repeatable outdoor inspection scene in which known geometry responds predictably to camera, sun and material changes. Establish correct surfaces, useful cast/self shadows, basic materials, ambient illumination, saved inspection settings and measured rendering cost before judging generated rocks. This is a bounded inspection area, not a renderer for the entire wasteland.

| Step | Work and proof | Status |
|---|---|---|
| OR-1 — Geometry correctness | Inverse-transpose normals; known cube, sloped solid and sphere; winding/culling and depth checks; CPU geometry tests and native GPU comparisons | Complete on Mac; [result and limits](./OUTDOOR_GEOMETRY_RESULT.md) |
| OR-2 — Color contract | Explicit linear lighting and display conversion; known color/illumination checks; define the HDR, exposure and UI composition boundary | Complete on Mac; [linear/HDR/display proof](./FIRST_PASS_RUNTIME.md) |
| OR-3 — Sun and shadows | One directional sun and bounded shadow map; cast/self shadows, bias and edge behavior; moving-light/camera evidence | Pending |
| OR-4 — Materials and ambient | Base color, roughness and normal textures; minimal owned asset fixtures/loading; ambient/environment illumination and consistent exposure | Pending PBR/ambient; [P03 catalog and residency](./TEXTURE_PIPELINE_RESULT.md) are implemented |
| OR-5 — Repeatable inspection | Save/reload camera, light and material settings; neutral and low-angle inspection presets; CPU/GPU measurements with backend and resolution | Pending |
| Windows checkpoint | Native configure, compile, shader compilation, launch and technical captures on a Windows PC during this milestone | Pending hardware access; does not require moving daily development off Mac |

Keep Mac as the main development machine while practical. Switch daily development only when compatibility effort materially impedes progress or target-specific debugging/performance work requires Windows. Passing Mac checks never closes the Windows checkpoint.

The user directed reuse of the existing rock and ground textures on 2026-09-10. The [surface library](../Assets/SurfaceLibrary/README.md) contains verified copies and source material mappings for OR-4. Use these assets when extending beyond simple calibration fixtures; preserve their recorded channel packing and authored bindings. The transfer itself does not complete texture loading, material implementation or visual acceptance.

The [texture research](../Research/TEXTURE_SYSTEM_RESEARCH.md) and [staged implementation plan](./TEXTURE_SYSTEM_PLAN.md) define OR-4's cooking/loading/material path. The offline experiment found format-dependent mip errors in the pinned texturec and a separate KTX2 parser requirement. Begin with engine-owned semantic mip generation and uncompressed KTX 1 references; gate later compression against those references. Color precedes lighting/material judgment; texture cooking need not wait for shadow implementation. No pending renderer step is completed by this research.

## First Batch Contract

OR-1 is done when nonuniformly scaled/rotated surfaces retain normals perpendicular to their surfaces, generated reference meshes have outward winding and nondegenerate triangles, and native GPU captures demonstrate correct culling and order-independent opaque depth. Retain the existing inspector/input/resize/resource/error checks. Audit color handling now but implement its correction in OR-2; do not add shadows in this batch.

Use one production mesh submission/shader path for the normal comparison. Compare its transformed sloped mesh with a CPU-baked reference whose flat normals are recomputed from transformed triangle edges, independently of the shader's normal matrix. Compare culling against uncull/front-cull diagnostics and reverse submission order in a sequential view. Require visible subject pixels so empty images cannot pass. CPU tests cover invalid/singular transforms and smooth sphere geometry. Inspect captures as well as numerical differences.

## Procedural and Ownership Boundaries

Reference meshes are bounded technical fixtures, not a geology generator. World seeds, chunk identity, authored world constraints and persisted runtime deltas are inapplicable because this scene creates no world objects or saved world state. CPU vertices/transforms remain independent of GPU handles; replacement and cleanup checks exercise the resource boundary that future chunk unload/reload will use. Temporary coordinates and scales establish no map canon. OR-5 settings will be inspection data, not a world save format.

No Unity edits, canyons, terrain streaming, physics, character controller, animation, gameplay smoke tests, new dependencies or renderer replacement. Additional light types, advanced global illumination and a full editor are outside this milestone. User-owned visual/feel acceptance and Windows performance remain distinct from technical proof.

## Audit and Sources

At the start of OR-1, the vertex shader applied the position matrix directly to normals, which fails under nonuniform scaling for non-axis-aligned surfaces; face culling was neither enabled nor proved. OR-1 corrected those geometry issues and retained the existing depth path. Current lit shading still multiplies display-like RGB values by light intensity without an explicit linear/display contract. These are renderer prerequisites rather than rock-generator problems.

Use the exact pinned bgfx/bx source for API and matrix conventions. The upstream [examples index](https://bkaradzic.github.io/bgfx/examples.html) identifies focused mesh, texture, HDR and shadow examples; it does not provide a finished game renderer. No dependency upgrade is required for OR-1.

At each completed step, update [STATUS.md](./STATUS.md) with source-bound build/tests/captures and remaining gaps. Stop at the batch's proof boundary rather than expanding into the following step.
