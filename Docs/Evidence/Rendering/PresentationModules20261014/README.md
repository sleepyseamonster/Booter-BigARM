# Camp presentation module implementation evidence

October 14, 2026. Implements the [modular presentation contract](../../../Engineering/MODULAR_PRESENTATION.md) following the approved chat plan and its audit corrections.

## Result

Camp camera motion/focus/optics are separate from twilight timing, direct light, ambient fill and camera-local sky presentation. The environment coordinator owns acquisition/restoration, not another simulation clock. Existing sun APIs delegate only when explicitly bound to modules; unconverted scenes retain their legacy path. Camera optics no longer depends on a sky asset or the old Sun Bloom feature.

The live Camp scene has Environment, Camera, Development, Systems, Actors, World and UI grouping roots. Original object names, 979 world transforms and nine native terrain references were preserved. Production/template scenes were not migrated. Grouped ironstone discovery was corrected so the runtime installer cannot duplicate the existing resource overlay after reparenting.

## Proof

- [Focused checks](focused-tests.json): 119 passed, zero failed, across 15 scoped EditMode suites. Includes environment ownership/teardown, clock contracts, independent optics, legacy rendering/profile behavior, grouped resource installation and existing Camp scene installer contracts.
- [Rendering qualification](qualification.json): byte-identical RGB captures before/after migration at phases 0.783, 0.25 and 0.75, 960 × 540, isolated graphics-enabled Unity 6000.4.0f1 with D3D11. All 1,555,200 channels match in each pair. Disabling the old Sun Bloom feature also preserves the modular-camera image exactly.
- The same qualification preserves all 979 existing world matrices within 0.001 and all nine TerrainData references, and leaves the original atmosphere/native-optics/sky source assets unchanged.
- [Saved scene reload](saved-scene-validation.json): task-only scene candidate reloads with zero missing scripts, a valid unique environment owner, independent optics, a transient owned sky clone and an idempotent version-1 migration check.
- [Shader diagnostics](shader-diagnostics.json): all seven inspected shaders are supported and have no compiler errors. Two warnings remain in the unchanged, globally disabled volumetric-dust shader; this task does not alter that shader.
- [Scene delta](scene-migration-delta.json): 35 added records and 27 changed properties, with no introduced dangling local references. Prefab overrides are preserved except for parenting; signed-zero serialization noise is ignored. The staging variant excludes unrelated author changes.
- [Source manifest](source-manifest.json) identifies task source files and the isolated validation owner. The one-line pre-existing material-inspector API modernization remains outside the task commit; its deprecated equivalent compiles in the tested staging variant.

Representative preserved views: [authored phase](authored-phase.png), [brighter twilight](brighter-twilight.png), [deeper twilight](deeper-twilight.png). Before/after PNG pairs were identical; one copy per view is retained here.

## Preservation and limits

Author scene recovery copies are retained locally under `Logs/Modularity20261014/`. Integration used `SaveScene(saveAsCopy:true)` snapshots and a hash-checked property-level merge rather than saving the entire dirty author scene or replacing it with the candidate. Existing dirty scene state and unrelated asset/code work were preserved. The shared renderer receives exactly one optics entry point; existing feature ordering remains intact relative to one another.

The first cold candidate import did not become controllable; its cache and diagnostic log were preserved, and a hidden D3D11 restart completed validation. A registration issue found by initial captures and a scene-reload check was corrected before final qualification. The sun facade participates in Edit Mode lifecycle registration but explicitly does not advance the cycle in Edit Mode.

No gameplay smoke tests, Play Mode acceptance, Player build, GPU-budget measurement, save-system change, production promotion, package change, push or release was performed. Static image equivalence covers these views/phases and does not establish gameplay feel or author visual acceptance.

Unity displayed an external-scene-change dialog after the surgical disk update. Choosing **Ignore** preserves the unsaved live scene; **Reload** would discard those unsaved edits. The author was notified rather than activating Unity against the background-only rule. Final live-Editor reinspection depends on that dialog being dismissed.

During closeout, another task committed the Camp Loop Resource Study. The task-only scene delta was rebased onto that commit, retaining its added root and all resource data. Saved-scene validation and the three Camp scene contracts passed again on that integrated candidate. The live installation's original seven grouping roots remain distinct from that concurrent addition.
