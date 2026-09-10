# Rock Workbench First Pass

P06/P16/P17 implemented 2026-09-10. The workbench now edits native rock recipes, rebuilds their textured render meshes and collider together, and supports undo/redo plus recipe save/reload. Inspection settings also have document history and save/reload controls. These are connected skeleton systems; advanced authoring and rock polish are deferred.

## Runtime and ownership

A shared bounded value-document history prepares an edit before accepting it. Failed generation or collider preparation preserves the last accepted recipe and history. GPU resources are prepared before the collider is replaced; the existing static physics body retains its token and generated identity. The workbench owns one rock at local offset (-3.5, 0, 0), and releases its GPU meshes before renderer shutdown. Recipe editing is disabled while character simulation is enabled.

The default rock has three render levels with 128, 32 and 8 triangles. Lower levels regenerate the same seed/identity using reduced subdivision directions and the same ground anchor. Automatic selection changes at 12 and 24 metres; the inspector can force a level. Collision always uses the exact LOD0 triangle list, independently of the displayed level. Existing triplanar rock textures, normal/surface maps and the sun shadow pass are shared with the renderer. This first pass accepts visible LOD transitions and silhouette differences.

The [meshoptimizer upstream documentation](https://github.com/zeux/meshoptimizer) was assessed for indexing, cache optimization and simplification. Native lower-resolution regeneration supplies the required first path without another dependency. No meshoptimizer integration or benchmark was performed. Revisit it for imported or larger meshes when measured simplification error or rendering cost warrants it.

Generated identity remains separate from GPU and physics handles. This is a fixed workbench preview, not a streamed population or a persisted world entity. Recipe documents persist authoring inputs; world runtime deltas, chunk unload/reload and generated placement integration remain downstream work. Authored placement constraints are provided by P14 and are not applied to this deliberately placed inspection rock.

## Run and edit

From `Engine/`, use the current build:

```sh
build/foundation/engine_workbench --rock Assets/Recipes/wasteland-rock.json --catalog out/surfaces-accepted-a/catalog.json --model out/models/calibration/model.json
```

The model argument is optional; without it character mode uses the capsule proxy. The recipe panel exposes seed, radii, detail, distortion and bands. **Apply recipe** accepts the draft; **Undo** and **Redo** rebuild the accepted preview. **Save recipe** writes the accepted recipe to the displayed path, and **Reload** reads that path. Copy the source recipe to an Engine-local working file or change the save path before creative iteration if you want to preserve the fixture. No automated check overwrote it.

The **Inspection document** section groups completed camera/slider gestures into history and saves or reloads lighting, material and camera settings. The buttons are compiled UI paths; the evidence below exercises their document/command implementations, not automated mouse clicks. Character mode can use the rock collider, but hands-on traversal and control feel remain user-owned.

The earlier `out/player-skeleton/` package predates this batch. Use `build/foundation/engine_workbench` for these controls; the player app has not gained a rock-recipe CLI in this batch.

## Focused evidence and limits

[One combined CPU check](../Evidence/P06-P16-P17-runtime/result.json) passed for LOD identity and anchors, distance selection, collision hits matching an independent barycentric calculation over the render triangles, stable body identity through replacement, edit/undo/redo, failed-edit preservation, body lifetime, recipe roundtrip and full-field inspection save/reload. Default CPU mesh/collision payload is 40,896 bytes.

[One six-capture Metal pass](../Evidence/P16-P17-render/result.json) passed with real surfaces and zero GPU errors. Vertex buffer counts remained 13 before/after; index buffer counts remained 6. Each comparison sampled 358,560 scene pixels. LOD changes, the height edit and the opposite view produced visible differences; undo returned to the original with zero differences above three code values. The rejected invalid edit preserved the accepted recipe. Simulation remained paused and the inspector was hidden for scene comparisons.

This proves one bounded Mac editing/rendering path, not Windows, final art, population performance, gameplay feel or long-duration resource stability. The workbench and combined native target built successfully. No additional texture cooking, gameplay smoke testing or repeated stress campaign was needed.

Next: P18/P19 add open terrain, constrained placement and bounded background jobs; P20 then connects region readiness and residency. Continue at skeleton depth.
