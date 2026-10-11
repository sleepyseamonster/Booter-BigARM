# Minimal cube rock fusion research

Research date: October 10, 2026. Status: provisional recommendation for the nine-area study, not an implementation or production design approval.

The intended workflow is one rock-textured cube that the author duplicates and moves. Touching cubes automatically produce a smooth continuous exterior. The author explicitly rejected workbenches, tessellation controls, complexity sliders, panels and feature expansion. Start with this interaction alone; additional features belong to later author requests.

The recommended first experiment is a small, independent cube authoring component and automatic Editor rebuild service. Use a true box field and one fixed internal blend width and sampling resolution. Reuse only useful low-level meshing and material code after inspection; do not inherit the workbench hierarchy, generation recipes, inspectors, formation controls or sculpted-rock Boolean workflow. This is a fresh interaction design, not another workbench with its controls concealed.

## Repository baseline

Audited on `main`, tracking `origin/main`, at `ee7b18d918f85ab6bcdd96782d53e7ea8eca2953`. There were 36 existing unpublished commits. Existing unrelated changes were `Greybox_BigARM.mat` and untracked fused-rock outputs and their metadata. These remain outside this task.

Read-only Unity CLI inspection targeted the D: project, Unity `6000.4.0f1`, process 13084, port 7800. The active scene was [FixedTerrainDressingStudy](../../../Assets/_Project/Scenes/Reference/FixedTerrainDressingStudy.unity), dirty and outside Play Mode. Nine workbenches reported one connected closed-manifold preview each; the loaded scene had zero missing scripts. Area 05 contained its terrain, the workbench and two SculptedBasalt01 instances. Reported preview status is live readback, not fresh independent geometry validation. The unsaved scene was neither saved nor discarded.

Saved-file SHA-256 anchors:

| File | SHA-256 |
| --- | --- |
| FixedTerrainDressingStudy.unity | `7F0D2831A7F38DB87B3BA3043443ABC818E5EC3D4E9489F1D16FC75F57649170` |
| GreaterWasteland.unity | `1D71DDB953A7B32CB28B43391DA183ED8552DB091020261F7B4D817FFE7F2EA9` |

### Existing approaches and their boundaries

| Existing system | Implementation fact | Implication for the new interaction |
| --- | --- | --- |
| Individual workbench | Seeded rounded, tapered, chipped source volumes; polynomial smooth minimum; marching tetrahedra; optional surface relaxation | Useful low-level extraction, but source shapes and authoring workflow are unsuitable defaults for plain cubes |
| Formation workbench | Grouped fields, shared material coordinates, optional vertex-color contact seams, several join styles | Do not import its hierarchy, seam painting, generation controls or presets |
| Sculpted-rock fusion | Background Blender Exact Boolean; retained source UVs, corner normals and material slots; manual default | Preserves creases and requires positive-volume overlap; does not implement the requested smooth touching-cube behavior |
| Old Manifold fusion | Preserved macOS-only Editor asset-baking reference | Not the Windows implementation path and not a production runtime dependency |

Relevant source: [mesher](../../../Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchMesher.cs), [preview service](../../../Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchPreview.cs), [source-volume component](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DRockVolumeNode.cs), [sculpted live fusion](../../../Assets/_Project/Scripts/Editor/TopDown3D/SculptedRockLiveFusion.cs), and [old overlap contract](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DRockVolumeOverlap.cs).

The current mesher uses six tetrahedra per grid cell, shared edge vertices, orientation checks and topology validation. Sampling has a 0.025 m minimum requested spacing, a 96-cell longest-axis cap and a one-million-sample gate. These are existing code limits, not exposed controls or performance promises for the new cubes. The post-mesh relaxation preserves the extracted mesh's volume approximately, with a bounded corrective scale; it does not restore the volume of the original cube union.

### Earlier research and checks

The [formation research](../../Design/Gameplay/ROCK_FORMATION_WORKBENCH.md) deliberately retains geological seams and introduces authoring tiers. That goal differs from this simple interaction. The [layered material contract](../../Engineering/ROCK_WORKBENCH_LAYERED_MATERIAL.md) already provides UV-free directional rock texturing at world-meter scale. The [sculpted fusion contract](../../Engineering/SCULPTED_ROCK_FUSION.md) explains the separate sharp-join asset workflow. The [Blender modeling research](../../Agents/Blender/research/MODELING_TECHNIQUES.md) supports choosing a method from the asset's job rather than adding generic detail or polygon budgets.

Existing [workbench tests](../../../Assets/_Project/Tests/Editor/TopDown3DRockWorkbenchTests.cs) include overlapping volumes, separated components, face-touching volumes, a rotated fixture and relaxation. Their `CreateBox` helper creates the weathered primitive, not an exact unmodified box. The rotated fixture also does not establish arbitrary exact tangent contact. These tests are reusable references, not proof of the new plain-cube system. Recorded nine-area checks passed 3/3 and sculpted manual-default checks passed 7/7 in their original isolated runs; none were rerun for this research.

## Geometry findings

### Use actual box geometry

A box signed-distance function can represent the source directly, without sculpt recipes or seeds. Mercury's own [hg_sdf source](https://mercury.sexy/hg_sdf/hg_sdf.glsl) provides both a box distance function and a compact polynomial soft-union operator equivalent to the current workbench blend. This is a basis for a small field evaluator, not a reason to install a library.

For independently scaled, orthogonal cubes, evaluate distances using world-meter half-extents and an orthonormal orientation. Scaling a local distance by the smallest transform scale, as the weathered primitive currently does, is a conservative bound rather than an exact distance under unequal scale. Avoid that ambiguity if simple world-meter boxes suffice. Rotated children under nonuniformly scaled parents can acquire shear; an oriented-box calculation must either support the resulting parallelepiped or explicitly reject it. No support beyond the initial interaction is assumed.

### Contact and smoothing are separate

Axis-aligned renderer bounds are useful for finding candidate neighbors, but can overlap when rotated boxes are separated. A box contact test using separating axes is a small geometry-only solution. For orthogonal boxes it considers face directions and edge cross products. The primary [Geometric Tools treatment](https://www.geometrictools.com/Documentation/MethodOfSeparatingAxes.pdf) also distinguishes a Boolean intersection test from computing the contact set.

Unity's [ComputePenetration](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.ComputePenetration.html) answers overlap at supplied poses, but its documented contract is not an exact touch classifier. Prefer an explicit box test with a small numerical tolerance; do not use gameplay physics callbacks or inflate collision contact offsets to trigger fusion.

Build one mesh for each connected group of touching cubes. Separate groups must remain independent, even when a smooth operator could bridge the gap. Face contact and overlap are the clear starting fixtures. Edge and corner contact must also be investigated because the author said touching cubes; the earlier suggestion to exclude them was not approved. A finite smooth neck at point or line contact changes the geometry. Do not silently narrow the requested behavior or promise a zero-volume-change connection.

Group detection alone cannot prevent smoothing from closing a narrow gap between two nonadjacent members of the same connected group. Test a U-shaped arrangement and deliberate openings. If the simplest blend loses important gaps, reduce its fixed width or investigate local blending in a later bounded experiment rather than adding controls immediately.

### Smooth fusion changes shape

The present polynomial blend is:

```text
h = clamp(0.5 + 0.5 * (b - a) / k, 0, 1)
smin(a, b, k) = b * (1 - h) + a * h - k * h * (1 - h)
```

A direct PowerShell calculation of this equation, using `k = 0.1 m`, gave:

| Probe | Field result in meters | Meaning |
| --- | --- | --- |
| Two equal zero distances | -0.025 | A common boundary moves inside the new field |
| Three equal zero distances folded in sequence | -0.0390625 | Coincident contributions accumulate expansion |
| Fold -0.02, 0.01, 0.03 from the left | -0.03581265625 | First grouping |
| Fold the same values from the right | -0.03849 | Polynomial blending is not associative |
| Two distances of +0.02 at a gap midpoint | -0.005 | The field fills a midpoint outside both cubes |

These are analytical field probes, not measured mesh volumes or Unity renders. At equal exterior distances `d`, the two-input result is `d - k/4`; two parallel faces separated by less than `k/2` can therefore have their midpoint included by this blend.

Exact coincident duplicates should count once during field evaluation while both editable cube objects remain preserved. This prevents immediate duplication from changing the displayed shape. It does not solve every partially overlapping or three-way junction. Repeatable input ordering can stabilize one recipe, but cannot make a nonassociative operator mathematically order-independent; ordering by mutable positions can also introduce jumps when objects cross that ordering boundary.

An original [overlap-correction investigation](https://www.smish.dev/math/smin_overlap_correction/) demonstrates analogous multiplicity swelling for exponential smooth minimum and proposes capsule-specific corrections. Those corrections are not a proved general cube algorithm. Exponential aggregation is permutation-symmetric but has nonlocal influence and multiplicity effects; normalization is not an automatic solution because it changes the union behavior. Mercury's rounded operator is another comparison candidate, but its distance-coordinate reasoning assumes favorable intersection angles. Neither merits a new exposed setting.

The minimal first comparison should use the existing compact polynomial blend with a fixed modest width and exact-duplicate suppression. Check two- and three-cube junctions before accepting it. Keep operator experimentation internal. If it fails the required interaction, change the method rather than presenting the author with more parameters.

### Keep extraction simple initially

Marching tetrahedra is already available and appropriate for a first bounded study. The [VTK authors' algorithm chapter](https://book.vtk.org/en/latest/VTKBook/06Chapter6.html) explains its lack of ambiguous tetrahedral cases, with additional triangles and a chosen subdivision orientation. This does not establish resolution-independent topology for every thin connection.

[Tao Ju's research overview](https://www.cs.wustl.edu/~taoju/research/octree.htm) explains dual contouring's ability to retain sharp features, while the [Subgrid Marching Tetrahedra project](https://www.cs.cmu.edu/~kmcrane/Projects/SubgridMarching/index.html) addresses thin features missed by classic marching. Both are escalation references, not initial dependencies. The requested smooth joins do not justify replacing the mesher with a more complex framework before a simple trial.

The repository's relaxation is inspired by [Taubin's surface fairing paper](https://graphics.stanford.edu/courses/cs164-10-spring/Handouts/taubin.pdf). Use little or none initially: smoothing an already blended field again can soften the whole cube and alter grounding. Existing edge-incidence and signed-volume checks do not establish absence of all triangle self-intersections or buried internal sheets. Add narrowly relevant checks where the cube fixtures reveal those risks.

## Material and editing findings

Use the existing rock textures and shader, without introducing surface controls. Preserve one common coordinate frame across every fused group. Do not change the origin, seed or broad patch scale every time a cube joins or leaves: the existing shader uses these values, and changing them can make texture patterns jump. A fixed study coordinate frame and a fixed material calibration are the simplest first choice.

Unity's [Triplanar node documentation](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.4/manual/Triplanar-Node.html) describes position-based projection and normal-based blending. Its existence supports the UV-free direction; it does not require replacing the current handwritten shader. [Mikkelsen's reference implementation](https://github.com/mmikk/surfgrad-bump-standalone-demo) is a useful later normal-composition reference if artifacts appear. Smooth-looking shading alone is not smooth geometry: [RecalculateNormals](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.RecalculateNormals.html) averages shared-vertex normals and does not generate tangents or remove interior surfaces.

Use an Editor update service that observes cube transforms and waits briefly for movement to settle. It should rebuild only changed groups and restore independent display when cubes separate. No continuous remesh while dragging, no automatic source renaming or regrouping, and no buttons. Each cube remains selectable through its normal Transform and a simple wire outline when its original renderer is suppressed. Deleting or duplicating a cube and Undo/Redo must update the result automatically.

Do not perform object creation or meshing in `OnValidate`; [Unity's documentation](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/MonoBehaviour.OnValidate.html) notes its possible loading-thread execution. [ExecuteAlways](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/ExecuteAlways.html) includes Prefab Mode and requires separation of playing and nonplaying instances. An isolated Editor service can avoid applying this behavior to prefab assets, other scenes or production rocks.

The first delivery must state exactly how the result survives scene reload and appears in Play Mode. Serialize cube transforms as the editable recipe. For the minimal Edit Mode study, a transient result reconstructed on load is sufficient, but it must not be described as a runtime-ready baked asset. [DontSaveInBuild](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/HideFlags.DontSaveInBuild.html) expressly excludes a preview from a Player. Do not add runtime meshing, export panels, LOD tooling, a production bake pipeline or gameplay collision merely to complete this research. If Play Mode fusion is required for the first delivery, choose and prove a small persistence handoff before implementation; it remains an engineering decision, not another user-facing control.

## Smallest implementation handoff

1. Place one true rock-textured cube in Area 05 using a task-owned, Undo-supported operation that preserves existing objects and unsaved author edits. Do not rebuild the study.
2. Add only the membership component and an independent Editor rebuild service. Use ordinary duplication and Transform editing; no authoring root, extra panel, generator, inspector sliders or exposed quality settings.
3. Detect geometric contacts, partition groups, suppress exact duplicate field contributions, and evaluate a fixed box blend through the smallest reusable meshing seam. Changes to shared code must preserve the old workbench defaults and enums.
4. Show one temporary exterior per connected group with the existing rock material. Keep sources recoverable and selectable. Restore display on separation, deletion, disable, Undo, scene close and reload. Never substitute a stale mesh after a source change.
5. Stop at this interaction. Appearance acceptance remains author-owned. Additional features require the author's request.

Fixed internal values are necessary implementation details, not new product options. Their initial choices should be documented in code and judged on the small fixtures below; do not present an advanced panel to tune them.

## Focused proof before acceptance

| Fixture | Required result |
| --- | --- |
| One cube and an unmoved duplicate | Original box silhouette retained; duplication alone does not inflate it |
| Face contact and modest overlap | One closed connected exterior; no retained join crease or internal contact sheet |
| Edge and corner contact | Explicitly observed behavior; any smooth neck resolved by the grid and disclosed as changed geometry |
| Rotated separated boxes with overlapping axis-aligned bounds | No fusion |
| Chain, three-way stack and U shape | Correct group membership; junction swelling and lost openings assessed |
| Separation, deletion and Undo/Redo | Current result, restored sources, no orphan preview or persistent hidden state |
| Scene reload and domain reload | Source recipe preserved and reconstructed without saving over artist changes |
| Matched textured views | Stable material scale and coordinates, no painted join seam |

Check finite vertices, consistent winding, edge incidence, positive volume, connected components, duplicate/internal surfaces and any relevant self-intersections. Record vertex/triangle counts, sample count and rebuild time for representative groups; fixed settings do not eliminate the need to measure cost. Use focused EditMode fixtures in an isolated background project, never a gameplay smoke test. Rendering captures also belong in an isolated background project, not the live Editor. No numeric responsiveness or whole-world performance claim is established by this research.

## World integration boundaries

Deterministic world identity is not applicable to this manually authored study: it does not place procedural gameplay objects. Recipe fingerprints can identify mesh inputs but are not world IDs. Stable generated-object identity is likewise not implemented; scene objects are editing sources, and a future production handoff must assign identity through its placement owner.

Terrain streaming and unload/reload are not applicable because Greater Wasteland's native terrain remains fixed and always loaded. Editor scene/domain reload is addressed separately above. Any future dressing-cell reconstruction must reuse a versioned baked asset and stable placement record rather than run this Editor service.

Authored constraints are the study's boundary, explicit cube poses and preservation of its existing contents. No terrain elevations, production placement masks, player clearances or landmarks change. Persisted gameplay deltas are not applicable because these sources have no gameplay state; scene serialization preserves authored transforms. No packages, terrain generators, old save services or native libraries are activated.

## Research limits

This pass audited source and prior evidence, performed mathematical probes, and inspected official documentation, original implementation sources and research papers. It did not implement cubes, rerun Unity tests, render images or establish arbitrary-stack reliability. Quilez's original pages and the requested Blender manual endpoints were unavailable through the browser; claims above rely on successfully retrieved sources and the actual repository formula instead. No third-party code was imported.

The next authorized implementation should start with the one-cube interaction and its small geometry fixtures. It should not revive the prior workbench complexity or expand into a general rock-generation system.
