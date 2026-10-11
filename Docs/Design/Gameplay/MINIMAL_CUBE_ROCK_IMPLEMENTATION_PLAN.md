# Minimal cube rock implementation plan

Date: October 10, 2026. Owner: Gottspan; Unity integration: Babineaux. Status: audited plan, not implemented. See the [audit](MINIMAL_CUBE_ROCK_PLAN_AUDIT.md). This request authorizes planning only.

## Intended result

Area 05 starts with one rock-textured cube. Duplicate it and move the copies with Unity's normal Transform tools. Touching cubes automatically form a smooth continuous rock exterior; separating them restores separate cubes. The source cubes remain editable.

There are no workbenches, new panels, generators, presets, buttons, sliders, tessellation settings or advanced sections. Start with this interaction alone. Further features require the author's request.

The proposed first delivery is an Edit Mode study, visible in Scene and Game views without entering Play Mode. This is a planning assumption based on the requested study authoring workflow, not an explicit author decision about Play Mode. It does not promise fused geometry in Play Mode or a Player; the proposed transition restores ordinary source cubes. State this boundary before implementation and in the delivery. If the author requires the fused appearance in Play Mode, revise the small persistence handoff before scene integration rather than silently changing the delivered shape or expanding into runtime generation.

## Starting point and authority

The [research report](../../Research/Art/SMOOTH_CUBE_ROCK_FUSION_RESEARCH.md) contains source inspection, prior evidence, mathematical probes and online references. Planning began at `173a49959` on `main`, tracking `origin/main`, with 37 existing unpublished commits. The BigARM material change and untracked Fused assets are unrelated and remain excluded.

The prior live readback found the study dirty, with nine existing workbenches and two sculpted basalt instances in Area 05. Recheck this before implementation; saved files and live Editor contents are different authorities. Preserve the existing objects, terrain, painting, lighting, production scene, package pins and other rock systems. No history rewrite, push, package change or foreground activation is authorized.

## Approach choice

| Approach | Fit and cost | Disposition |
| --- | --- | --- |
| True box fields and automatic Editor mesh extraction | Direct cube inputs, smooth geometry, existing material and low-level extraction available | Recommended |
| Exact union followed by a small local bevel or remesh | Can retain more planar geometry, but adds Boolean contact degeneracies and extra processing | Reserve only if the simple field fails required fixtures |
| Blender voxel reconstruction and smoothing | Viable baked geometry method, but process latency, temporary files and source handoff complicate movement-driven editing | Not the first interaction |

Choose a new small cube component and an independent Editor service. Reuse the existing marching-tetrahedra extraction through a narrow field-and-bounds API; do not reuse workbench authoring types or inspectors. The shared extractor remains one implementation, with its current callers and defaults unchanged. Do not copy the old extractor into a competing generator.

## Source boundary

Proposed names below are new files, not installed features. Final names may follow adjacent repository conventions without changing scope.

| Path | Responsibility |
| --- | --- |
| `Assets/_Project/Scripts/Runtime/TopDown3D/SmoothRockCube.cs` | Tiny cube marker; no public authoring settings, source-order identifiers or runtime meshing |
| `Assets/_Project/Scripts/Editor/TopDown3D/SmoothRockCubeGeometry.cs` | Immutable box snapshots, contact tests, duplicate suppression and fixed blend evaluation |
| `Assets/_Project/Scripts/Editor/TopDown3D/SmoothRockCubePreview.cs` | Single Editor owner for groups, change detection, debounce, display, source picking and cleanup |
| `Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DRockWorkbenchMesher.cs` | Narrow extraction entry point accepting field bounds and evaluation; old wrapper behavior retained |
| `Assets/_Project/Prefabs/TopDown3D/Environment/SmoothRockCube.prefab` | One plain cube, existing rock material, marker and normal Transform |
| `Assets/_Project/Tests/Editor/SmoothRockCubeTests.cs` | Focused geometry, lifecycle and interaction checks |
| `Assets/_Project/Tests/Editor/NineAreaRockStudyTests.cs` | Inspect original-layout assumptions; change only task-obsolete assertions with explicit preserved-content proof |
| `Assets/_Project/Scenes/Reference/FixedTerrainDressingStudy.unity` | Only the additive Area 05 placement if scene saving can preserve ownership |
| `Docs/Engineering/NINE_AREA_ROCK_STUDY.md` | Describe the actual minimal interaction and saved versus transient state after implementation |

New assets and scripts receive their own `.meta` files. Existing metadata and GUIDs remain intact. Runtime marker stays in `BooterBigArm.TopDown3D.Runtime`; all processing stays in `BooterBigArm.Editor`; tests stay in `BooterBigArm.Editor.Tests`. No new assembly or package is needed.

Existing material: `Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat`. Existing shader: `Assets/_Project/Shaders/TopDown3D/BrokenWorldRockWorkbenchPBR.shader`. Read them and the current surface calibration; use fixed property values without editing shared material assets or attaching a workbench.

## Internal behavior

- Start with a 1 m cube. Place its bottom on Area 05's current terrain at a clear position; inspect existing contents before choosing that position. Do not move the basalt instances or workbench to make room.
- Source transforms alone define geometry. No weathering recipe, source volumes, shape seeds, fractures, seam masks or extra authoring parent.
- Detect touching boxes geometrically. Use bounds only to find candidates, then separating-axis testing with a small numerical tolerance. Include overlap, face, edge and corner contact in the proof; do not silently exclude a contact type.
- Treat independently rotated and positively scaled orthogonal boxes correctly. For zero scale, invalid transforms or parent-induced shear, retain the sources and report a concise error rather than approximating or changing their transforms.
- Build one exterior for each connected contact group. Keep separate groups independent even when they are close. Suppress coincident duplicate contributions without deleting either source cube. With only one distinct box, display one original cube renderer and reversibly suppress any coincident copies, preventing z-fighting; do not introduce a remeshing/shading change just because the author duplicates it. Choosing which identical renderer displays does not determine field geometry.
- Treat compact polynomial blending as a comparison baseline, not the accepted multi-cube algorithm. Trial values of 0.1 m blend width and 0.025 m requested sampling must be evaluated with actual padded bounds and effective grid spacing. The current 96-cell cap coarsens a 3 m arrangement to approximately 0.034 m: it cannot satisfy the former instruction to retain 0.025 m everywhere. Choose a fixed internal sampling policy and supported extent together with a blend that resolves the smallest required neck over grid offsets. Record effective spacing and never present an unresolved or disconnected neck as successful fusion. Existing workbench cap behavior stays unchanged.
- The accepted field must be independent of cube discovery, creation and Hierarchy order for identical geometry, and continuous as cube poses change. A fixed hidden-ID fold cannot establish that contract for a nonassociative operator. Batch 1 must choose and document a symmetric aggregation or another geometry-based blend that passes these checks. Do not add an identity-management subsystem to conceal order-dependent shapes. Exact-duplicate suppression alone also does not solve swelling near partially coincident copies.
- Fix material coordinates and calibration to the study frame so adding a cube does not shift textures or change their scale. Keep shader-painted join seams off; rock texture fissures may remain.
- Observe source changes and rebuild after a short fixed settling delay. Avoid rebuilding while the transform handle is actively dragged. Sample/extract large groups on at most one task-owned worker using immutable numeric snapshots; Unity object access, Mesh creation and display replacement stay on the main thread. If the small synchronous path is retained instead, measure its worst-case update stall over the accepted fixtures before choosing it. One group per update alone is not a latency budget. Bound internal work and memory, invalidate obsolete jobs, cancel/drain on teardown and measure main-thread application; no controls result.
- Generated meshes and display objects are transient. Hide source rendering reversibly and retain enabled states and source transforms. Explicitly handle a duplicated source inheriting service-owned visibility suppression; do not read that copied suppression as the author's intended visibility. Scene Save must serialize ordinary source visibility, not transient hide state, without clearing unrelated changes or moving sources. Prove the chosen clone/save/reload restoration scheme before integration.
- Source cubes remain selectable in the Hierarchy, including fully contained cubes. Scene picking maps an exterior hit to the closest contributing box in that group; ties use a deterministic geometry rule, and all coincident/contained sources remain accessible through ordinary Hierarchy selection. Keep generated display out of persistent authoring selection; do not add selection buttons, cycling tools or panels.
- For any changed pose or membership, immediately retire the old exterior and restore its sources while the new job settles. Applying no stale result is not sufficient if the old shape remains visible during the wait.
- Revalidate input signatures before display replacement. Never display a result for an obsolete arrangement. On failure restore current sources; do not substitute mesh combining or retain a misleading old exterior.
- Scene close, domain reload, Undo/Redo, Play Mode transition, cube disable and deletion release owned meshes and restore source rendering. Keep prefab assets, Prefab Mode and other scenes isolated. `OnValidate` only validates data; it does not create meshes or objects.

## Implementation batches

### Batch 0 Revalidate and preserve ownership

Inspect branch, full status, staged files, pinned editor and assembly boundaries. Inspect the connected target project without opening, saving or focusing a scene. Record study/production scene hashes and live dirtiness, Area 05 contents, source transforms and relevant TerrainData/material hashes. Confirm that no other writer owns proposed files.

Gate: a precise task manifest exists and unrelated edits are preserved. Do not run tests in the dirty live scene or launch a second Editor against its project lock.

### Batch 1 Prove the smallest geometry

Implement the pure box/contact rules and the narrow shared extraction API, plus relevant focused tests in an isolated background project. Check the singleton, unmoved duplicate, touching pair, edge/corner neck, three-way stack and U-shaped opening before building authoring behavior.

Gate: a documented operator and sampling policy pass valid geometry, duplicate neutrality, order independence, continuous near-coincident movement, correct separation and all required contact fixtures. Include a three-cube 3 m chain and fractional-grid offsets. Measure junction swelling, neck thickness and extraction cost. If no simple method passes, stop this geometry experiment with the specific failed fixture; the full authoring buildout is not yet ready. Do not compensate with source IDs, looser assertions or author controls. Preserve the old mesher's focused regressions.

### Batch 2 Add automatic editing

Add the marker and Editor service, debouncing, source selection, transient display and cleanup. Add meaningful lifecycle tests. Use the existing material with a fixed study coordinate frame. Inspect singleton material appearance as well as joined geometry.

Gate: duplication while fused, movement, separation, deletion, Undo, Save and reload require no new UI. No orphan mesh, copied suppression, stranded hidden renderer, stale display/result, cross-scene group or Prefab Mode mutation remains. Use a preview-owned unscaled root so source/Area transforms are not applied twice. No Rigidbody, gameplay MeshCollider or inherited primitive BoxCollider is installed; source selection must work without gameplay collision.

### Batch 3 Place one cube in Area 05

Create the reusable cube prefab and add one instance to Area 05 with Undo, retaining all existing objects. Recheck live dirtiness first. If the scene is still dirty, place the cube without saving or serializing the author's unrelated changes. Commit the prefab/code independently; the author's ordinary scene Save will persist the insertion. A dirty live insertion is not a committed scene change, and an unsaved insertion has Unity's normal unsaved-scene lifetime.

If the scene is clean and its ownership is uncontested, save only the bounded additive study change after reviewing its diff. Never reload an isolated scene over unsaved live work or claim a placement is saved when it is only in memory.

Gate: one added cube, source material correct, no replacement/rebuild of the nine-area scene, other contents and protected assets unchanged. No multiple-placement menu is needed for this delivery because duplication supplies additional cubes.

### Batch 4 Verify and close

Follow the [verification plan](MINIMAL_CUBE_ROCK_VERIFICATION_PLAN.md), record current-machine evidence, update the nine-area documentation with the actual delivered behavior and persistence boundary, and commit only verified task-owned files on `main`. No push. Hand visual acceptance to the author without adding features while awaiting it.

## World and persistence contract

World identity and generation version are not applicable to manual study authoring; this system places no procedural gameplay objects. The geometry is defined by the authored box set, not hidden generation IDs. Stable generated-object identity belongs to a future production placement handoff, not these transient previews.

Terrain streaming is not applicable because Greater Wasteland's fixed native terrain remains always loaded. Dressing-cell ownership and reconstruction are not implemented. Editor scene/domain reload reconstructs the saved cube recipe independently of preview lifetime. No gameplay saved delta or save migration exists because these cubes have no gameplay state. Scene Save preserves source transforms; generated display is not a baked production asset.

Authored constraints are Area 05's current terrain and existing object arrangement. No terrain elevations, player clearances, POI masks, landmark rules or production assets change.

## Completion and stop

Implementation is complete for this slice when one cube is available in Area 05, normal duplication and movement give the verified automatic Edit Mode blend, sources remain recoverable, and the scoped proof passes. Report separately whether placement is saved, whether appearance is author-accepted and whether any evidence is pending.

Batch 0 and the bounded Batch 1 geometry experiment are sufficiently scoped for an implementation request. Full scene integration is conditional on choosing a blend/sampling policy that passes Batch 1 and proving clone/save visibility restoration in Batch 2. Routine names and internal values are engineering choices, not user settings. The proposed Edit Mode boundary must remain explicit. Stop after this minimal interaction; no runtime fusion, collider system, terrain population, LOD/export tools or additional controls follow automatically.
