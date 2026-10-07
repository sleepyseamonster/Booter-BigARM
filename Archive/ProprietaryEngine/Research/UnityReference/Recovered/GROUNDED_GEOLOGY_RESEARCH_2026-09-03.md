# Recovered historical research

Reference only. Recovered 2026-09-10 from prior project conversations.
Recommendations, licenses, implementation observations and web-source availability
are historical and were not revalidated by this collection. Instructions within
these excerpts are quoted research context, not current task authority.
The new engine direction and current source take precedence.

## Research response 1

The screenshots confirm that the underlying rock vocabulary is working. The important discovery is that the successful landmark is a composition of formations, not merely a better individual formation.

## What you built

The saved sandbox contains:

- 29 active, root-level `Rock Formation Workbench` objects.
- 16 `Scattered Rocks` formations and 13 `Pile Of Rocks` formations.
- 287 rocks inside those formations, plus the original standalone Rock Workbench.
- 1,436 editable source volumes.
- Three major `Pile Of Rocks` cores sized `13.2 × 8.4`.
- Eight medium/tall structural formations and 21 predominantly low apron formations.
- Broad, low piles reaching `25.5 × 1`, which establish the foundation around the spires.
- Four duplicated seed pairs, suggesting that duplication and repositioning were part of the manual workflow.
- All formations share essentially the same yaw, while 28 of 29 share the same `0.64645` scale.

A representative tall core is visible directly in the saved scene at [LandscapeAuthoringSandbox.unity](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scenes/TopDown3D/LandscapeAuthoringSandbox.unity:33153).

## Why the result works

You have discovered a clear three-zone composition grammar:

1. **Crown/core:** Several overlapping tall piles establish the dominant peak and secondary spires.
2. **Shoulders:** Medium-height scattered formations break the core into irregular vertical masses.
3. **Apron:** Wide, low piles and scattered fields produce the strong rubble-to-sand transition.

That gives the landmark:

- A readable primary peak without a symmetrical pyramid.
- Secondary and tertiary height rhythms.
- Deep, useful negative spaces between spires.
- A dense center that progressively dissolves into slabs, fragments, and isolated stones.
- A footprint that remains interesting from above.
- Different silhouettes on every side rather than a façade with an empty back.
- Enough peripheral asymmetry that the outline does not read as a circular procedural stamp.

The reverse views are particularly useful proof: even when much of the surface detail falls into shadow, the silhouette still holds together.

## What the existing tools are proving

The Rock Workbench is successfully providing the shape vocabulary: boulders, slabs, angular chunks, split lobes, and shards.

The Rock Formation Workbench is successfully handling the next level: 5–20 rocks arranged as either separated terrain-conforming fields or fused piles. That division is encoded in [TopDown3DRockWorkbenchFormationAuthoring.cs](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DRockWorkbenchFormationAuthoring.cs:6), with the placement logic in [TopDown3DRockWorkbenchFormationGenerator.cs](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchFormationGenerator.cs:387).

The missing level is now clear:

`source volumes → rock → formation → landmark complex`

Your manual arrangement is effectively the prototype for that fourth level.

## Recommended next step

I recommend adding an editor-only **Rock Formation Cluster Workbench** above the existing Formation Workbenches.

Its first archetype could be a provisional “Spire Complex,” reconstructed exclusively from the two formation types you already like. It should:

- Generate 2–4 tall pile cores.
- Add medium scattered or pile shoulders around them.
- Build a much larger low apron from piles and scattered fields.
- Derive unique subformation seeds, rotations, scales, and roles from one cluster seed.
- Preserve one or two open approach wedges rather than surrounding the core uniformly.
- Apply height and size falloff from core to perimeter.
- Conform every subformation to terrain—not only scattered rocks. Presently, terrain conformation is applied only to `Scattered Rocks` at [TopDown3DRockWorkbenchFormationGenerator.cs](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchFormationGenerator.cs:887).
- Share one material/fracture coordinate system across the entire complex.
- Keep every child Formation Workbench editable and rerollable.
- Provide “Create Cluster From Selected Formations” so this exact hand-built landmark can become our reference composition without flattening or replacing it.

I would not attempt to fuse all 1,436 source volumes into one live editing mesh. Each pile can retain its current fused shell while the cluster controls composition. A combined optimized representation belongs to a later baking/runtime slice.

## Refinements to keep in view

There are several secondary issues worth addressing inside that cluster slice:

- Every dimensioned formation currently has pitch and roll removed, producing perfectly upright members. That supports the clean spire language, but it also contributes to occasional parallel “standing slab” rhythms. The behavior comes from [TopDown3DRockWorkbenchFormationGenerator.cs](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchFormationGenerator.cs:1349). Any reintroduced lean should be restrained and role-aware.
- Fused shells stop at each Formation Workbench boundary. Overlapping formations can therefore contain hidden cross-formation geometry.
- With 29 auto-rebuilding formations, scene reload and editing cost will continue growing. The preview manager scans every formation and rebuilds one per editor update at [TopDown3DRockWorkbenchFormationEditor.cs](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchFormationEditor.cs:285). Cluster generation should temporarily suspend child auto-rebuilds and rebuild them as a controlled batch.
- Four repeated seed pairs are acceptable in this curated example, but an automatic cluster planner should guarantee unique derived seeds unless repetition is deliberately requested.

## Ground clutter and sand

Those should be the slice immediately after cluster composition—not before it.

Once the macro structure is reproducible, a grounding layer can add:

- Smaller scree with size falloff away from the core.
- Partial sand burial at rock contacts.
- Lee-side sand accumulation based on a visible wind-direction control.
- Narrow wind-scoured gaps on exposed sides.
- Denser fragments beneath steep spires and quieter sand around broad slabs.

The project already has a wind- and formation-aware dust deposition model in [TopDown3DDustDepositionPlanner.cs](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DDustDepositionPlanner.cs:237), so we can later reuse that reasoning rather than inventing a purely decorative circular sand ring.

No files were changed. The scene and its currently deleted generated assets remain untouched, and Unity’s lockfile is present, so any later automated verification should use the established exact-mirror workflow.

## Research response 11

These references point toward a coherent new visual family: **Dark Fractured Desert Rock**. The important insight is that we cannot reach it through texture changes alone—the fractures must also influence the geometry.

| Target quality | Current limitation | Recommended change |
|---|---|---|
| Blocky, irregular silhouettes | Rocks are assembled from smoothly fused positive masses | Add fractured-block, monolith, and broken-slab shape grammars |
| Deep structural cracks | Most cracks are shader details | Introduce subtractive fracture volumes that create real gaps and broken edges |
| Large/medium/small hierarchy | Current scattered sizes are relatively close together | Use a strongly weighted size distribution with a few anchors and many fragments |
| Natural clustering | Placement is mostly evenly spaced inside an ellipse | Generate anchor clusters, satellite rocks, debris wakes, gaps, and occasional outliers |
| Dark charcoal material | One broadly neutral shared treatment | Add restrained per-rock charcoal, blue-gray, and warm-black variation |
| Sand or red-earth integration | Burial is currently only around 2–6% | Vary burial by size and feed terrain color into dust accumulation |

### Individual rock improvements

The base generator should produce rocks from four geological layers:

1. **Primary mass** — one strong block, slab, rounded boulder, or upright monolith.
2. **Secondary facets** — several asymmetrical masses that break the outline without turning it into a collection of bubbles.
3. **Major fractures** — one to three deep, mostly planar cuts that cross substantial portions of the rock.
4. **Edge damage** — missing corners, shallow chips, and smaller surface cracks.

I would add these silhouette families:

- Fractured Boulder
- Blocky Monolith
- Broken Slab
- Angular Fragment
- Weathered Boulder
- Existing Shard for smaller debris

Large rocks would favor boulders and monoliths; medium rocks would favor slabs and angular chunks; small rocks would favor fragments and shards.

### Surface treatment

The current [rock shader](</Users/worldbuilder/Desktop/Booter & BigARM/Assets/_Project/Shaders/TopDown3D/BrokenWorldRockWorkbenchPBR.shader>) already supports cracks, grit, shale, mineral variation, and dust. It needs to be retuned and extended rather than replaced:

- Near-black charcoal base with roughly 8–12% brightness variation
- Mostly matte roughness with restrained highlights on worn faces
- Much darker crack interiors
- Occasional pale mineral-filled joints
- Fine pitting and abrasion at a smaller scale than the major cracks
- Environment-controlled dust color so the same rocks work in yellow sand, red soil, or another biome
- Material variation passed through property blocks, avoiding unique material instances for every rock

Hero and medium rocks should receive geometric fractures. Small rocks can rely more heavily on normal-map and shader cracks for performance.

### Scattered-rock improvements

The current [scattered formation planner](</Users/worldbuilder/Desktop/Booter & BigARM/Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchFormationGenerator.cs>) already distinguishes boulders, slabs, and fragments, but the scale range and placement are too even.

A better distribution would be approximately:

- 10% large anchor rocks
- 25–30% medium rocks
- 60–65% small fragments
- Additional micro-debris handled later as inexpensive ground clutter

Instead of one uniform field, each formation should contain one to three loose clusters. Each cluster would share a subtle fracture direction and material family, while retaining separate deterministic rock seeds. Empty sand corridors and isolated outliers are as important as the rocks themselves.

### Recommended implementation order

1. Add subtractive fracture volumes and the new fractured silhouette families to the [individual rock generator](</Users/worldbuilder/Desktop/Booter & BigARM/Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchBaseRockGenerator.cs>).
2. Add a **Dark Fractured Desert** surface preset and per-rock material variation.
3. Rework Scattered Rocks around hierarchical sizes and clustered placement.
4. Add micro-debris and terrain-colored sand accumulation as a separate ground-clutter layer.

This order improves every formation automatically because formations are built from individual Rock Workbenches. Deterministic seeds and editable source volumes should remain intact; runtime chunk ownership, stable world IDs, streaming, and persisted deltas would remain unchanged until this visually approved rock family is connected to the landscape generator.

No project files were changed during this analysis.

## Research response 15

You’ve reached the point where more generator sliders will give diminishing returns. The system currently generates rocks, terrain, materials, and clutter as mostly separate things—but your vision depends on them sharing one geological cause.

The right next move is to freeze new formation categories and build one “golden” Grounded Geology Study.

| Scale | What must improve |
|---|---|
| Macro | Terrain rises into the formation and creates believable shoulders/aprons |
| Meso | Rock silhouettes are smooth, coherent, fractured geological masses |
| Micro | PBR texture, cracks, grit, pebbles, sand accumulation, contact shading |

The eventual procedural pipeline should be:

```text
Formation identity
  → geological terrain stamp
  → rock placement and shape
  → crevice/contact masks
  → accumulated sand
  → talus and ground clutter
  → shared terrain/rock material blending
```

The important change is that a formation should generate more than rock meshes. It should output semantic fields such as:

- Terrain uplift and slope
- Rock occupancy and contact
- Crevice depth
- Sediment accumulation
- Talus/debris probability
- Ground-material blending
- Traversal and collision boundaries

That lets red dirt naturally slope into a ridge, fill the spaces between rocks, and control where debris collects.

For the stair-step problem: that is fundamentally a geometry reconstruction problem, not something textures can hide. We need to separate:

- Large silhouette and fracture planes: real geometry
- Medium chips and erosion: selective geometry or displacement
- Fine cracks and grain: height, normal, roughness, and parallax
- Tiny clutter: instanced or combined real meshes

Simply increasing voxel resolution everywhere would be expensive and still wouldn’t guarantee good shapes. The better direction is cleaner surface reconstruction, feature-preserving smoothing, improved normals, and geological fracture rules applied to coherent masses—not more randomly overlapping volumes.

For the ground, I’d use a hybrid approach:

- Real mesh fragments where silhouette and shadows matter
- High-quality triplanar rock and soil materials
- Macro and micro normal layers
- Height-based sand/rock blending
- Bounded parallax or displacement for close terrain detail
- Contact and concavity masks for sand accumulation
- True terrain or apron geometry for the large red-dirt slopes

The first milestone should be deliberately small:

1. One manually approved rock formation.
2. One approximately 20–30 meter terrain patch.
3. Smooth rock silhouettes at the actual gameplay camera distance.
4. One convincing dark-rock material.
5. Red sand that slopes against the formation and fills its crevices.
6. Three clutter scales: grit, small fragments, and hand-sized debris.
7. Approval from several angles and in play mode.

Only after that slice looks right should we extract its rules into the Rock, Formation, Landmark, and landscape generators.

So my recommendation is: the next implementation should be a **Geology Integration Workbench**, but not another generator tier. It should use one of your handmade formations as the visual authority and prove geometry, terrain contact, materials, and debris together. The existing deterministic seeds, editable hierarchies, and formation roles remain valuable scaffolding—we just stop expanding them until the visual foundation is convincing.

## Research response 16

Yes—the “golden Grounded Geology Study” is the right next direction. It aligns with both Unity architecture and the research. The critical design rule is that it should be an integration laboratory driven by shared geological data, not another increasingly complicated generator or hierarchy tier.

## What the research supports

Believable terrain is produced when erosion, deposition, surface shape, and material appearance share causes. Early terrain research already demonstrated that adding thermal and hydraulic erosion produces recognizable valleys and talus slopes instead of generic fractal noise. Weathered-stone research likewise models shape change and surface appearance together rather than treating texture as decoration applied afterward. [Musgrave, Kolb, and Mace](https://citeseerx.ist.psu.edu/document?doi=8af1ba21a954c910994f5ad599777640452a3d6b&repid=rep1&type=pdf), [Dorsey et al.](https://groups.csail.mit.edu/graphics/pubs/stone.pdf)

Your proposed pipeline is therefore conceptually sound:

```text
Geology recipe
  → terrain uplift
  → coherent rock mass
  → fracture and weathering
  → contact and concavity
  → sediment deposition
  → talus and clutter
  → shared material blending
```

The important phrase is “geology recipe.” The workbench should store a compact description of the cause, then derive the outputs.

## Good Unity architecture

The recommended separation is:

```text
GeologyRecipe asset
  Immutable parameters, authored constraints, generation version

GroundedGeologyGenerator
  Pure deterministic calculations from recipe + seed + chunk coordinate

GroundedGeologyResult
  Temporary height fields, masks, rock data, clutter placements

GeologyIntegrationWorkbench
  Editor visualization, regeneration, comparison, approval

Landscape runtime adapter
  Converts approved rules into streamed chunk content
```

This fits the project’s existing procedural-world standard: immutable tuning data belongs in `ScriptableObject` assets, while generated runtime state stays separate and traceable to seed, version, and chunk identity. [WORLD_SYSTEMS_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Docs/WORLD_SYSTEMS_STANDARD.md:22) Unity also describes ScriptableObjects as project assets for shared, persistent authoring data rather than scene-bound component state. [Unity ScriptableObject documentation](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html)

The semantic outputs should not all be GameObjects. Use the representation appropriate to the information:

| Output | Representation |
|---|---|
| Terrain uplift | 2D float field |
| Sediment thickness | 2D float field |
| Rock body | Localized 3D signed-distance field |
| Contact/crevice/slope | Derived 2D masks |
| Talus probability | 2D density field |
| Clutter | Deterministic placement records |
| Traversability | Slope/clearance classifications |
| Rendered output | Terrain patch, rock meshes, material masks, instances |

Unity Terrain is heightmap-based, so it is well suited to shoulders, aprons, dunes, and broad slopes—but not overhangs or vertical rock faces. Those remain meshes. Unity’s Terrain Tools support heightmap and mesh stamping, and `TerrainData` exposes height and alphamap data for procedural integration. [Terrain stamping](https://docs.unity3d.com/Packages/com.unity.terrain-tools%405.0/manual/stamp-terrain.html), [TerrainData](https://docs.unity3d.com/6000.0/ScriptReference/TerrainData.html)

## Mathematical fields to use

A practical first version does not require a full geological simulation. It needs a believable constrained approximation.

Let the formation have a footprint or spine. From that derive:

- `F(x,z)`: distance/falloff from the formation footprint.
- `U(x,z)`: terrain uplift and shoulder height.
- `R(x,y,z)`: signed distance to the rock mass.
- `K(x,z)`: ground concavity, where depressions receive sediment.
- `C(x,z)`: rock-ground contact proximity.
- `W(x,z)`: wind shelter/exposure.
- `S(x,z)`: resulting sediment thickness.
- `T(x,z)`: talus and debris probability.

A useful sediment target is conceptually:

```text
sediment =
    supply
  + contact attraction
  + concavity
  + wind shelter
  - steep-slope rejection
  - exposed-surface rejection
```

Then apply an iterative angle-of-repose relaxation: if sediment between neighboring cells exceeds an allowed slope, move material downhill until stable. This is a standard simplified “thermal erosion” model and is much better than painting a red blend around the rocks. Hydraulic erosion research similarly tracks water, dissolved material, transport, and deposition as related fields. [Mei, Decaudin, and Hu](https://www-evasion.imag.fr/Publications/2007/MDH07/FastErosion_PG07.pdf)

For this project, we should begin with angle-of-repose relaxation and wind shelter. Full hydraulic erosion is unnecessary for the first study.

## Fixing the rock geometry

The current system uses a scalar field followed by marching tetrahedra, then recalculates normals from the extracted triangles and applies bounded Taubin-style relaxation:

- [Current mesher](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchMesher.cs:287)
- [Current normal generation](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchMesher.cs:267)
- [Current smoothing](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchMesher.cs:576)

That is a legitimate prototype foundation, but there are four better improvements than merely raising tessellation:

1. **Scale-relative sampling**

   Define resolution as a ratio, such as:

   ```text
   voxel size = smallest rock dimension / target cells across
   ```

   Test 16, 24, 32, and 48 cells across the smallest dimension. This prevents a fixed voxel size from behaving completely differently on a hand-sized rock and a landmark.

2. **Scalar-field gradient normals**

   The original Marching Cubes method calculates shading normals from the scalar-field gradient. Our current mesh instead calls `RecalculateNormals()`, so visible facets can reflect triangle structure rather than the intended implicit surface. [Original Marching Cubes paper](https://graphics.stanford.edu/courses/cs348a-21-winter/Papers/Marching_Cubes.pdf)

3. **Feature-aware smoothing**

   The existing Taubin-style smoothing is useful, but stronger smoothing alone will erase fracture planes. Relax smooth weathered regions while constraining vertices near detected joints, ridges, and ground contact.

4. **Dual-contouring experiment**

   Dual contouring uses surface intersections and normals to preserve sharp features and supports adaptive resolution. It is promising for blocky, fractured rocks, although it is more complex and carries greater topology risk. It should be an isolated comparison, not an immediate replacement. [Dual Contouring paper](https://people.eecs.berkeley.edu/~jrs/meshpapers/JuLosassoSchaeferWarren.pdf)

The actual geological improvement is to start with one coherent parent mass, then apply a few correlated joint planes, chips, and weathering operations. Randomly overlapping many volumes creates visual complexity without geological coherence.

## Materials and ground relief

Your macro/meso/micro separation is correct:

- Silhouette, overhangs, fracture planes: real geometry.
- Medium erosion and chips: selective geometry or bounded displacement.
- Cracks, grain, pitting: height, normal, roughness, AO.
- Loose silhouette-producing debris: real instanced meshes.

Unity specifically recommends normal maps for small grooves and surface details instead of modeling all of them as geometry. [Unity normal-map guidance](https://docs.unity3d.com/6000.1/Documentation/Manual/StandardShaderMaterialParameterNormalMap.html)

Parallax occlusion mapping is appropriate for close ground detail, but it remains visual depth. Unity’s implementation exposes step count and LOD fade controls, which supports using it near the camera and fading to normal mapping at distance. It must not define collision or traversal. [Unity POM documentation](https://docs.unity3d.com/Packages/com.unity.shadergraph%4010.5/manual/Parallax-Occlusion-Mapping-Node.html)

The material should consume sediment thickness and exposure—not independently guess them from world height. That lets the same `S(x,z)` field control:

- Terrain color blending.
- Sand normal attenuation.
- Rock dust coating.
- Crevice darkening.
- Pebble density.
- Sand apron geometry.

## Clutter distribution

Uniform random scattering is not sufficient. Use density-weighted Poisson-disk sampling:

```text
minimum spacing =
    clutter-class diameter × local spacing multiplier
```

Then modulate acceptance using talus, contact, sediment, slope, and wind fields. Bridson’s method produces minimum-distance blue-noise placement in linear expected time and is a strong foundation for this. [Bridson’s Poisson-disk paper](https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf)

Use three separate populations:

- Grit: primarily material relief or combined tiny meshes.
- Small fragments: dense variable-radius instances.
- Hand-sized debris: sparse real meshes with visible silhouettes.

This also fixes the scattered-rock issue: give that formation a narrow size distribution, but select among more unique rock shapes. Variation should come mostly from silhouette, fracture orientation, burial, and rotation—not from wildly different overall sizes.

## The experimental technique we were missing

One-slider-at-a-time tuning is particularly poor here because parameters interact. A better process is:

1. Choose 6–8 meaningful, dimensionless parameters.
2. Generate 16–24 deterministic candidates using Latin hypercube sampling.
3. Render every candidate with the same camera, lighting, seed policy, and physical bounds.
4. Compare candidates in pairs.
5. Record your preference and eliminate consistently poor parameter regions.
6. Run a second, narrower batch around the winners.

Latin hypercube sampling covers a multidimensional space more evenly than naïve random trials. [McKay, Beckman, and Conover](https://www.tandfonline.com/doi/abs/10.1080/00401706.1979.10489755) Pairwise preference analysis is appropriate because “A looks more believable than B” is much easier and more reliable than assigning an absolute realism score. [Bradley–Terry method](https://academic.oup.com/biomet/article-abstract/39/3-4/324/326091)

The first parameters should be:

- Cells across smallest dimension.
- Major mass aspect ratio.
- Fracture-plane spacing/rock width.
- Chip scale/rock width.
- Edge rounding/voxel size.
- Burial depth/rock height.
- Apron radius/formation width.
- Sediment relaxation angle.

Every batch should be evaluated in three passes:

1. Gray geometry only.
2. Final rock and terrain materials, no clutter.
3. Full sediment and clutter integration.

That prevents a strong material from disguising bad geometry—or bad lighting from causing us to reject good geometry.

## Returning to the 0.1 scale discovery

The 0.1 change is valuable evidence, but it should not become a permanent magic parent scale.

The current controls explicitly describe width, height, and geology scale as meters. [Rock authoring dimensions](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DRockWorkbenchAuthoring.cs:53) In the example shown earlier, approximately `5.12 × 11.8` becomes `0.512 × 1.18` world meters at scale `0.1`. That is much more plausible for an individual rock.

However, the root scaling changed three things together:

- Physical rock dimensions became ten times smaller.
- Each voxel step became ten times smaller in world space.
- The world-space material covered ten times less of its pattern across the rock, making geological texture features broader relative to the rock. The shader samples `worldPosition / metersPerTile`. [World-space sampling](/Users/worldbuilder/Desktop/Booter%20%26%20BigARM/Assets/_Project/Shaders/TopDown3D/BrokenWorldRockWorkbenchPBR.shader:214)

It did not improve normalized topology. If you move the camera closer until the smaller rock occupies the same screen area, the original tessellation profile should become visible again.

Unity’s physics assumes one world unit is approximately one meter, and Unity recommends creating content at its intended real-world size rather than routinely correcting it through Transform scale. [Unity Transform and scale guidance](https://docs.unity3d.com/6000.0/Documentation/Manual/class-Transform.html)

So the recommendation is:

- Keep generated rock roots at `(1,1,1)`.
- Reduce individual-rock default dimensions to the physical scale that looked correct at `0.1`.
- Scale source volumes and voxel size together when baking that correction.
- Replace absolute voxel controls with “cells across the rock.”
- Keep material measurements in real meters.
- Give formations and landmarks their own larger physical ranges rather than enlarging individual-rock assets through parent transforms.

Before changing code, make an exact controlled comparison:

- **A:** Current rock at root scale `0.1`.
- **B:** Root scale `1`, but source dimensions and voxel size multiplied by `0.1`.
- **C:** Same physical dimensions as B, while independently testing mesh resolution and material scale.

A and B should be visually and physically equivalent. C will tell us which part actually creates the desired look.

## Recommended next milestone

Freeze formation categories and build one study containing:

- One approved handmade formation.
- One 20–30 meter terrain patch.
- Unit-scale generated roots.
- Terrain uplift and apron field.
- Coherent rock SDF and fractures.
- Contact, concavity, shelter, and sediment fields.
- Angle-of-repose sediment relaxation.
- One calibrated rock/soil material family.
- Three clutter populations.
- Fixed comparison cameras and deterministic candidate generation.
- Play-mode traversal and collider validation only after visual approval.

That is good Unity design, good procedural design, and a much more productive direction than adding more formation presets. No project files were changed during this research.
