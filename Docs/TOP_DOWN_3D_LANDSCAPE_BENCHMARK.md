# Top-Down 3D Landscape Benchmark And Asset Contract

This is the measurable review contract for the representative landscape family in `TOP_DOWN_3D_LANDSCAPE_IMPLEMENTATION_PLAN.md`. It does not replace user visual acceptance.

## Fixed production view

- Scene: `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`.
- Camera: perspective, 25 m follow distance, 48-degree vertical FOV, 50-degree starting pitch, current production yaw and obstruction behavior.
- Preserve camera distance, FOV, pitch limits, and orbit behavior. Far clip may increase from the current 300 m only to expose the approved distant-landscape system.
- Evidence seeds: the production seed `24681357` plus two fixed comparison seeds chosen by tests. For each seed, capture the same spawn-facing view and one view across a drainage corridor.
- Capture set: near material/ground breakup, mid-ground basin and rock silhouette, near-to-far transition, and horizon/atmospheric-depth view. Record resolution and Quality level with each capture.

## Visual success criteria

- The first read is a broad eroded basin or route, not uniform rolling noise.
- At least three spatial scales are visible together: regional basin/uplift, landmark mesa/scarp/outcrop, and local sediment/rock breakup.
- Drainage lows connect plausibly across chunk and regional boundaries and remain readable as traversal corridors.
- Mesa and scarp silhouettes have unequal, eroded profiles; cliff faces do not read as smooth cylinders or repeated radial stacks.
- Talus and smaller rocks collect beneath steep/exposed geology rather than forming uniform independent scatter.
- Sand, gravel, weathered rock, and bedrock transitions follow geology, slope, curvature, drainage, and deposition. Material patches must not be driven by an unrelated shader-only noise authority.
- Base colour is neutral-lit and does not contain directional shadows or ambient-occlusion halos. Warm sunset character comes primarily from lighting and atmosphere.
- Near textures retain readable grain without obvious tiling. Mid-distance material frequency diminishes cleanly. The far field reads primarily by form, value, and haze.
- Hero features remain recognizable across near/far representation changes without popping to a different silhouette or location.
- Chunk borders, near/far rings, collider boundaries, and material-weight interpolation are not visible in fixed captures.
- Twilight preserves warm key light, cooler/neutral fill, readable shadow planes, and progressively lower contrast/saturation with distance.

## Spatial scale bands

| Band | Approximate scale | Primary job |
| --- | ---: | --- |
| Regional | 288–1152 m | basin/uplift organization, drainage direction, horizon rhythm |
| Major feature | 45–220 m | mesas, ridges, scarps, canyon walls, hero anchors |
| Local landform | 6–45 m | terraces, gullies, outcrops, talus aprons, traversal choices |
| Ground material | 0.03–6 m | sand ripples, gravel, fractured bedrock, small sediment variation |

The current 18 m chunk is a streaming unit, not a geological design unit.

## Terrain mesh and material contract

- Near mesh continues to use the current 24 quads per 18 m chunk (0.75 m vertex spacing) unless profiling proves a change necessary.
- Generated surface weights use vertex colour: R sand/deposit, G gravel/talus, B exposed bedrock, A weathering/lithology. The weights must be continuous because they are sampled from world coordinates, not derived per chunk.
- UV0 remains world-scaled ground coordinates for texture sampling. A future packed UV channel is allowed only if vertex colour cannot carry a required stable value.
- Existing `BrokenWorldTerrainBlend` and `Greybox_Terrain.mat` are the only production terrain shader/material authority for this slice.
- Ground albedo maps are sRGB; height, normal, roughness, AO, and packed masks are linear. Normal maps use Unity Normal Map import type.
- Target source resolution is 2048 square for the reusable base material family and 1024 square only for transitional support maps whose screen footprint proves sufficient.
- Initial tile scales remain in the 2.25–4 m range, with distance fading of high-frequency normal/height response. Texture streaming mipmaps should be enabled for production ground maps when the import pass can be validated in Unity.
- Every production ground family should converge on albedo, normal, and packed mask data where packed channels are R AO, G roughness, B height, A reserved. Until those maps exist, the shader may use explicit scalar defaults; it must not infer PBR values from base-colour brightness.

## Rock, cliff, and landmark contract

- Representative family: small fragment, medium boulder, large slab, extra-large outcrop, massive formation, cliff/scarp segment, talus cluster, and hero spire.
- Each family needs at least three silhouette variants except the hero spire, which may use a single stable identity with rotation/scale constraints.
- Forms should express a shared stratified, wind-eroded geology: broken planes, undercut or weathered edges, directional strata, and debris appropriate to the parent face.
- Runtime authority is the natural-object catalog. Player builds consume baked mesh/prefab assets only; they do not invoke native CSG.
- LOD review targets: no visible silhouette jump at the production camera; distant LODs preserve the outer contour and major strata while dropping crevice detail. Colliders remain simpler than render meshes and must not extend materially beyond visible rock.
- Materials use triplanar/world-aligned projection where appropriate and converge on neutral-lit albedo, normal, roughness, AO, and optional authored luster/mask channels.

## Current asset inventory and gaps

- Terrain has sand/dirt, swept-sand, gravel, mixed-rock, and transition albedos; mixed-rock and two rock transitions have height/normal support.
- Existing ground maps are predominantly 2048 square; the new medium transition set is 1024 square. Texture-streaming metadata is enabled with anisotropy 4 and has passed Unity import; visual proof remains pending.
- The terrain shader consumes generated vertex surface weights and fades normal/height detail by camera distance. Unity import and material/channel validation pass; fixed-camera visual proof remains pending.
- Rock surfaces currently provide base-colour variants and a teal luster mask, but no complete normal/roughness/AO family.
- The catalog now contains 27 baked families and 81 LOD mesh assets across nine archetypes. Runtime rendering, collision, combined cosmetic geometry, support/bounds, and overlap checks all consume this catalog; no runtime procedural-mesh fallback remains.
- No approved external DCC/source model family is present. The autonomous lane therefore uses deterministic editor-baked project meshes and existing owned textures; external sourcing remains a user decision.

## Performance and proof guardrails

- World streaming/decorating keeps the existing 2 ms per-frame work guardrail unless a controlled Development Player profile justifies changing it.
- Far terrain generation must be budgeted, allocation-conscious, and non-colliding. Never create full-detail colliders outside the near field.
- Unity import, landscape validation, and the owned 27-test EditMode selection now pass. Fixed-camera captures and controlled Development Player profiling remain separate required evidence stages.
