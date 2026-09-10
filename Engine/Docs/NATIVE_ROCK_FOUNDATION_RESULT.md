# Native Rock and Placement Foundation

P14/P15 implemented 2026-09-10. The new `engine_world` library provides authored placement constraints and deterministic CPU rock generation. `engine_rock_cook` writes generated geometry through the existing cooked-model path, with the recipe, stable object identity, bounds, horizontal footprint and per-triangle surface classification alongside it. This is a basic native generator, not a port of the Unity geology system or accepted final rock art.

The recipe uses versioned integer radii in millimetres, distortion/banding strengths in permille, a seed and a subdivision level. The generator subdivides octahedron faces, displaces their normalized directions and emits faceted triangles with normals, a planar UV placeholder and an orthogonal tangent basis. Reduced integer directions keep shared edges identical; triangle winding faces outward. Poles remain anchored at y=0 and twice the vertical radius. The supported profile is 0–4 subdivisions (8–2,048 triangles), radii 0.1–20 m, distortion up to 250 permille and banding up to 150 permille. These are technical bounds, not geological or performance claims. UV/tangent authoring can expand when a consuming material needs it; the existing world-projected surface path does not require a full character-style UV unwrap.

Geometry derives from recipe seed plus world seed, signed region address and stable member ID. No mutable global random state or GPU call participates. `GeneratedId` retains the `rock` namespace and generator version. Recipe edits change shape without changing the placement object's identity; changing the generator version requires an explicit identity/version decision. Repeated generation on this Mac is byte-identical. Cross-platform bitwise float identity is not claimed; integer identity and recipe data remain exact.

Placement constraints serialize two technical traversal profiles, exclusion circles and reserved route segments. Candidate rock footprints cannot overlap exclusions or reserved corridor widths expanded by the applicable agent radii. A shared route therefore reserves the larger agent's clearance. Separate traversal checks consider radius, height, slope and step capability. These are placement rules, not a navigation solver or proof of routes across future generated terrain. Coordinates use the existing 256 m region frame, including negative and large addresses, without flattening world positions into one large float. Route reservations operate in the horizontal plane; segments are bounded to 512 m, with up to 256 routes and 256 exclusions per constraint document.

The [recipe fixtures](../Assets/Recipes/README.md) include a first rock and a spawn/route reservation. The larger profile is provisional calibration data, not final BigARM dimensions. Canyons, world geography and creative geology acceptance remain deferred.

## Procedural ownership

Generated-object identity is separate from cooked mesh/resource handles. A future region owner can regenerate base geometry in any order, apply authored constraints using its footprint, then attach render/collision resources and persisted object deltas. Unload discards those resources and generated CPU data; reload uses the same seed/version/region/member inputs. No triangle ID is persisted. This batch does not implement chunk residency, runtime delta storage, population or asynchronous scheduling; those remain downstream owners.

## Use and focused evidence

From `Engine/`:

```sh
build/foundation/engine_rock_cook Assets/Recipes/wasteland-rock.json out/rocks/example
build/foundation/engine_workbench --model out/rocks/example/model.json
```

The optional four identity arguments are world seed, region X, region Z and member. Output directories must be new. The current generated example is `out/rocks/wasteland-001/`; the workbench's existing model loader can inspect it. Live recipe edits, material/collision adapters and LOD selection are the next package.

[One combined native check](../Evidence/P14-P15-generation/result.json) passed for stable IDs, repeated/order-independent generation, seed edits, resolution identity, closed edges with opposite winding, nondegenerate normals, bounds/ground anchor, triangle semantics, cooked asset roundtrip, recipe roundtrip, negative-region constraints, two-agent route clearance, exclusions, order independence and invalid-input rejection. [The native cooker](../Evidence/P15-rock-cook/result.json) produced the 128-triangle example with its recipe and semantic sidecar. No new dependency, texture cook, gameplay smoke test, package cycle or rendering stress check was needed. GPU/collision/LOD integration is not claimed by these CPU results.

P06 inspection commands remain unfinished. Their prerequisite moves from independent CPU generation to P17 live authoring, where they are consumed; the requirement remains in the roadmap.

Next: P06/P16/P17 connect generated rocks to the existing material/physics adapters and a minimal live recipe workbench. Continue at skeleton depth rather than polishing procedural geology here.
