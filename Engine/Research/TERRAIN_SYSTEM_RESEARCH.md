# Terrain implementation choices

Researched 2026-09-10; selected scope is the [preview plan](../Docs/TERRAIN_PREVIEW_PLAN.md).

| Source / option | Finding and decision |
|---|---|
| [FastNoiseLite](https://github.com/Auburn/FastNoiseLite), [releases](https://github.com/Auburn/FastNoiseLite/releases) | Portable C++ noise, MIT; v1.1.1 is the listed release. Useful if more noise types are needed. Existing integer-address value noise is enough for broad hills and shelves now; avoid adding an unused dependency. |
| [Terrain3D](https://github.com/TokisanGames/Terrain3D) | C++ MIT terrain addon for Godot. Reference for terrain organization; its Godot dependency makes it an adaptation project rather than a drop-in bgfx component. |
| [GPU geometry clipmaps](https://developer.nvidia.com/gpugems/gpugems2/part-i-geometric-complexity/chapter-2-terrain-rendering-using-gpu-based-geometry) | Nested grids offer continuous terrain LOD with reusable topology. Current chunk meshes can use three discrete resolutions first. Clipmaps/morphing follow only when draw/upload cost or visible transitions justify them; historic performance figures are not our budget. |
| [Jolt heightfields](https://jrouwe.github.io/JoltPhysics/class_height_field_shape.html) | Existing physics dependency offers static heightfield collision. Keep the current combined terrain/rock triangle adapter now to avoid two collision ownership paths. Revisit with measured cooking/memory cost. |
| [World Creator export](https://docs.world-creator.com/reference/export/conventional-export) | Height, normal, color and distribution maps can feed our own terrain/material system. Exported maps do not replace runtime streaming, collision or persistence. |
| [Gaea universal export](https://docs.gaea.app/guides/use-in/), [download](https://www.quadspinner.com/Download) | Heightmap export with explicit terrain dimensions/height; current download is Windows x64. Optional authoring input, no purchase or workstation change needed now. |

Recommended representation: a heightfield for open ground plus independent rock
meshes for more complex silhouettes/overhangs. V2 terrain samples one continuous
field through integer region addresses. Mesh and collision use the same triangle
diagonal; surface queries interpolate those triangles instead of a different smooth
function. LODs sample that field, keep collision fixed, and hide mixed-resolution
edge gaps with skirts. Ground colors blend by world-space broad patches and slope;
only paired rocky layers use their actual normal maps. Height-only maps must not be
misread as AO/roughness or color.

Future import adapter: offline 16-bit linear height data (or 32-bit float), explicit
horizontal metres/sample, vertical scale/offset, axis convention and source hash.
Cook into engine-owned tiled samples with a shared edge/normal halo. Reuse the same
mesh/query/material/streaming path. Bake erosion offline with tile context, rather
than running unrelated per-chunk simulations that disagree at boundaries. Treat
material distribution maps as linear weights, normalize weights, and preserve the
original material texture identities. Source/tool revision belongs in cook metadata.
This adapter is not implemented by the preview pass.
