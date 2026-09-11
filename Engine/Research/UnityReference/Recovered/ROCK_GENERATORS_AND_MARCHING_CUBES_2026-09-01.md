# Recovered historical research

Reference only. Recovered 2026-09-10 from prior project conversations.
Recommendations, licenses, implementation observations and web-source availability
are historical and were not revalidated by this collection. Instructions within
these excerpts are quoted research context, not current task authority.
The new engine direction and current source take precedence.

## Research response 1

I found several credible free/open-source options. The important distinction is that most repositories generate individual rocks; only a few address cliffs, arches, rock piles, or formation-scale geometry.

| Project | Best use | License / caution |
|---|---|---|
| [Godot StoneGenerator](https://github.com/AndreasBurbach/godot-stone-generator) | Strongest formation-capable candidate. Seed-reproducible rocks, arches with real openings, tileable cliff kits, fractures, LODs, collision meshes, baked maps, and GLB/OBJ export suitable for Unity. | MIT. Very new, requires Godot 4.7, and documentation is primarily German. |
| [SeedRock](https://github.com/reed-soul/SeedRock) | Rock and cliff authoring in a browser. Includes erosion, terrain scattering, LOD chains, collider proxies, and GLTF/GLB export. | MIT. Promising but young; its included PBR textures use an AI-assisted workflow, so I would generate or replace textures independently. |
| [RockGenerator](https://github.com/bamyazi/RockGenerator) | Probably the easiest standalone experiment. Includes boulder, slab, column, ledge, overhang, arch, rubble, and wall shapes; cracks, undercuts, erosion, decimation, LODs, and GLB/OBJ export. | MIT. Browser/Three.js rather than Unity-native. Excellent as an offline mesh baker or algorithm reference. |
| [Unity Procedural Rock Generation](https://github.com/przemyslawzaworski/Unity-Procedural-Rock-Generation) | Direct Unity example using volumetric generation/marching cubes, with an Export button that saves the generated mesh as an asset. | README declares MIT. Tested with Unity 2019.4 and has only three commits, so this is reference code, not a drop-in Unity 6 dependency. |
| [LeLocTai Rock Generator](https://github.com/LeLocTai/rock-generator) | Best C# algorithm donor. Uses Voronoi-based shaping and decimation; its core targets .NET Standard 2.0 and includes Unity conversion code. | [MIT](https://github.com/LeLocTai/rock-generator/blob/master/LICENSE.txt), but old and explicitly WIP. Generated meshes lack UVs, and Unity integration needs cleanup. |
| [Blender Extra Mesh Objects](https://extensions.blender.org/add-ons/extra-mesh-objects/) | Safest established art tool for quickly producing seed variations of base rocks. Current extension supports Blender 4.2 LTS+. | GPL-3.0+. Good for exported mesh creation; less suitable for copying its code into the game. Primarily individual rocks, not geological formation planning. |
| [Infinigen](https://github.com/princeton-vl/infinigen) | Deep research source for boulders, slabs, rock piles, cliff scenes, terrain-integrated rocks, and procedural materials. Its [rock generators are separated here](https://github.com/princeton-vl/infinigen/tree/main/src/infinigen/assets/objects/rocks). | BSD-3-Clause, mature and active, but far too large and Blender/Python-heavy to integrate wholesale. |

My recommendation for Booter & BigARM:

1. Evaluate **Godot StoneGenerator first** for reusable formation meshes, cliff modules, arches, LODs, and collision proxies.
2. Evaluate **RockGenerator** second as a fast visual workbench for discovering useful rock silhouettes.
3. Use **LeLocTai** and **Unity Procedural Rock Generation** only as code references when improving the project’s existing base-rock mesh generation.
4. Keep the existing `WorldRockFormationPlanner` as the authority for placement, seeds, stable IDs, chunk ownership, and streaming. Imported generators should supply mesh families or authored formation archetypes—not replace world generation.
5. Offline-exported single meshes could reduce dependence on Boolean fusion for reusable formations. They will not automatically solve runtime fusion of arbitrary generated members.

I did not install, clone, or modify anything. The next sensible step would be a bounded technical bake-off of StoneGenerator, SeedRock, and RockGenerator in temporary folders, comparing topology, polygon counts, Unity import, collision suitability, determinism, and visual range.

## Research response 2

For Unity specifically, the field is much smaller. I did not find a modern, maintained, turnkey open-source Unity package dedicated to complete geological formations. The useful candidates are:

1. [Unity Procedural Rock Generation](https://github.com/przemyslawzaworski/Unity-Procedural-Rock-Generation) — best match

- Runs directly in Unity.
- Generates a scalar field with compute shaders and triangulates it into a mesh.
- Includes an editor exporter for saving generated meshes as Unity assets.
- This approach could be extended from one rock volume to several overlapping volumes, producing a single formation mesh without Boolean-unioning separate rocks.
- Tested on Unity 2019.4; only three commits.
- README declares MIT, although the repository lacks a normal standalone license file.

This is the one I would investigate first for Booter & BigARM.

2. [LeLocTai Rock Generator](https://github.com/LeLocTai/rock-generator) — best reusable C# source

- Unity project with a C# generator.
- Voronoi-based rock shaping.
- Target triangle-count controls and mesh decimation.
- Core generation code works with .NET Standard 2.0.
- [MIT licensed](https://github.com/LeLocTai/rock-generator/blob/master/LICENSE.txt).
- Older and marked WIP.
- Primarily generates individual rocks, and the output has no UVs.

This could improve the project’s base-rock library, but it would not generate entire formations by itself.

3. [Procedural Cave Generator](https://github.com/AK-Saigyouji/Procedural-Cave-Generator) — useful for rock walls and boundaries

- Unity-native and MIT licensed.
- Generates cave floors, ceilings, walls, and rock-lined outlines.
- Can convert generated results into independent Unity prefabs.
- It expects supplied rock prefabs for its rock-outline mode.

This is more relevant to cliffs, canyon walls, cave mouths, and formation boundaries than standalone boulders.

4. [mc-mesher](https://github.com/kyy13/mc-mesher) — useful foundation

- MIT-licensed Marching Cubes library for Unity/C#.
- Converts arbitrary 3D scalar fields into indexed meshes and normals.
- Could support custom formation shapes composed from multiple rock primitives.
- It is a meshing library, not a finished rock generator.
- Native-code deployment would require platform validation, particularly for macOS builds.

5. [BMesh for Unity](https://github.com/eliemichel/BMeshUnity) — mesh-editing support

- MIT Unity package for procedural mesh construction and manipulation.
- Supports vertices, faces, UVs, normals, colors, and custom attributes.
- Helpful if we build a more advanced rock generator ourselves.
- Not rock-specific and does not provide formation logic.

I would exclude these:

- [UNITY-rock-generator](https://github.com/catseen/UNITY-rock-generator): visually relevant and Unity-native, but no license is published, so its code is not safely reusable.
- [simple-rock-gen](https://github.com/atx-barnes/simple-rock-gen): Unity/Houdini example, but only two commits, depends on Houdini Engine, and has no clear license.
- Voxel terrain and cave-world repositories: useful for destructible terrain, but excessive for discrete surface formations.

My recommendation is to study **Unity Procedural Rock Generation’s scalar-field method** and adapt the underlying idea into the existing formation planner:

`formation plan → overlapping implicit rock volumes → one scalar field → Marching Cubes → one connected Unity mesh`

That would fit your existing deterministic seed and placement system better than importing a separate world generator, and it may provide a cleaner alternative to the current mesh-Boolean fusion problem.
