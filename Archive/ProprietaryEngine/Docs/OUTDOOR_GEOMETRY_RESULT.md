# Outdoor Geometry Result — OR-1

Recorded 2026-09-09. OR-1 from [the outdoor rendering plan](./OUTDOOR_RENDERING_PLAN.md) is complete within the current Mac technical-proof boundary. OR-2 color handling is next; the outdoor rendering milestone as a whole remains incomplete.

## Change and Reason

The previous vertex shader transformed normals with the position matrix. This only gives the desired normal direction for restricted transforms/surfaces and concealed a lighting defect behind the axis-aligned cube. Scene submission now supplies the affine inverse-transpose normal matrix. Invalid/nonfinite/singular or nearly collinear transforms fail explicitly. Inspector scale is finite, positive and bounded; mirrored object rendering is not exposed, although the CPU normal-matrix tests exercise a negative determinant.

CPU-owned cube, sloped tetrahedral solid and smooth sphere vertices are separate from renderer buffers. The inspector selects shapes, independent XYZ scale and world-normal display. Opaque reference meshes now use outward counterclockwise winding and clockwise-face culling. A sequential scene view makes the reversed-order depth diagnostic meaningful. Four fixture buffers, including the independent baked reference, replace transactionally and release on shutdown.

The technical verification changes the same fixture state displayed by the inspector. It compares the normal shader against a transformed-vertex reference whose flat normals come from triangle-edge cross products, not the normal-matrix function. Reference/culling/order diagnostics use the same submission and shader path. This is inspection geometry, not a rock generator or production mesh importer.

## Evidence

| Check | Result | Receipt |
|---|---|---|
| Native build and Metal shader compilation | Passed; final reviewed incremental build about 5.4 seconds | [Reviewed build](../Evidence/OR1-build-reviewed/result.json) |
| CPU state and geometry | Both CTest executables passed; outward/nondegenerate meshes, sphere radial normals, affine perpendicularity, independent baked normals and invalid input | [Final native tests](../Evidence/OR1-native-tests-final/result.json) |
| Evidence/preparation tools | 16 tests passed, including empty-image, wrong-normal, ineffective-culling-control and draw-order mismatch rejection | [Tool tests](../Evidence/OR1-tool-tests/result.json) |
| Native GPU comparisons | Metal; 163,200 sampled scene pixels per comparison, 7,983 visible sloped-reference samples; zero differences above three channel levels for baked normals, uncull comparison and reversed order | [Final verification](../Evidence/OR1-verification-final/result.json) |
| Culling control | Opposite face culling changed 111,802 sampled scene pixels | [Final verification](../Evidence/OR1-verification-final/result.json) |
| Retained application checks | Inspector click, camera event, resize, close, two expected failures; 20 four-mesh replacements, vertex buffers stable at 8 before/after; 10 captures, zero capture errors | [Application report](../Evidence/OR1-verification-final/captures/verification.json), [verification](../Evidence/OR1-verification-final/result.json) |

The [normal view](../Evidence/OR1-verification-final/captures/normals.png) and [baked reference](../Evidence/OR1-verification-final/captures/baked.png) retain independently rasterized outputs. The [sphere](../Evidence/OR1-verification-final/captures/sphere.png) provides a curved-surface diagnostic. Visual inspection covered the sloped/sphere output and readable inspector; after the fixture-state correction the final normal capture confirmed matching inspector rotation. These are technical observations, not art-direction acceptance.

The initial [build attempt](../Evidence/OR1-build/result.json) compiled the application/shaders, then failed to find the newly added geometry target in that invocation after CMake regenerated the build files. The [following build](../Evidence/OR1-build-final/result.json) passed. Initial [GPU verification](../Evidence/OR1-verification/result.json) passed; review then moved the scripted geometry state into the application's shared state so inspector values matched the proof scene. The reviewed build and final verification above supersede those earlier sources. The existing duplicate-bx linker warning remains visible and unsuppressed. Dependency revisions are unchanged.

## Limits and Next Step

R-01/R-04 scope separation, R-08 resource ownership, R-11 inspection/diagnostics and R-12 reproducible evidence are exercised. R-02 gains geometry inspection prerequisites, not a character-following camera. R-03 native Windows evidence and R-07 final rock visual quality remain open. No world IDs, chunk generation, authored geography or persisted deltas are introduced.

The comparisons are bounded fixed-camera geometry checks, not full visibility/occlusion validation, transparent rendering, mirrored-instance culling, long-session stability or target-PC performance. The sphere is CPU-tested and visually inspected; it has no independent analytic GPU normal oracle. Fixed object centers can lift/intersect ground when shapes or scale change.

The lit shader still has no explicit linear/display-color contract, HDR target or tone mapping. The diagnostic normal colors encode vectors directly. Implement OR-2 and verify known color/illumination values before adding the directional shadow map. Materials, ambient/environment light, saved settings, Windows checks and the rock generator remain later steps. Mac remains primary development while practical.
