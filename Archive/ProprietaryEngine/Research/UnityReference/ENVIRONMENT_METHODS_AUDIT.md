# Unity-to-native environment methods audit

2026-09-10. Source audit following the user's terrain screenshot, completed alongside
research recovery. This is an implementation guide, not a new completed rendering
pass. [Source receipt](./source-audit.json) identifies current files inspected.

## What the screenshot establishes

The supplied screenshot visibly labels the application **Terrain v2**, showing a
repeating orange/gray/sand pattern, relatively uniform solitary stones and a
“Waiting for ground collision” status. A still frame cannot establish floating
geometry, the cause/duration of the wait, or performance. The latest terrain-v3
package changes material masks and places groups, but its retained captures still
show repeated block-like members and sparse ground contact treatment. Both need
substantial visual development. Passing native/GPU checks is not appearance parity.

## Correct the implementation order

The active Unity Mixed Formation Ground preview copies authored members, fits them
to the ground, optionally regenerates each member's mesh, then applies sand and
clutter. This is visible in `TopDown3DLandscapeAuthoringSandboxEditor` around lines
310–375 and `TopDown3DMixedFormationShapeVariation.BuildMesh`. It does **not** call
whole-formation grouped meshing along that path. `TryBuildGrouped` and geological
seams are a separate Rock Formation Workbench capability.

Therefore, the previous native result's “fused shell next” sequence is superseded
by this audit: first improve shape grammar and ground relationships. Preserve the
fusion method for formations that actually benefit from a continuous exterior.

## Transfer map

| Visible problem | Unity method and source | Native adaptation |
|---|---|---|
| Repeated rounded blocks | `TopDown3DRockWorkbenchBaseRockGenerator`: slab, angular chunk, split-lobe, shard, fractured boulder, monolith and broken slab profiles. `TopDown3DRockWorkbenchMesher.Evaluate`: taper, wedge planes and corner cuts, transformed in full 3D. | Extend versioned `RockVolume` inputs beyond rounded yaw-only boxes; introduce a small profile grammar with tilted planes and a distinct primary silhouette before adding fine noise. Start with boulder, broken slab and angular fragment. |
| Soft or coarse shape detail | Unity `RelaxSurface`: bounded positive/negative smoothing passes, signed-volume restoration and ground-anchor correction. Native grid detail currently also limits runtime cost. | Use physical sampling targets and restrained relaxation; review silhouette in neutral shading. Keep authoring quality separate from cooked runtime LOD/collider budgets. Increasing noise or every grid resolution is not the fix. |
| Mechanical layouts | Authored member matrices/IDs, varied profiles by role, terrain-conforming group orientation and support-aware burial. | Add captured native composition templates or role-specific profile selection; preserve varied sizes, open crevices and restrained lean. Ground the actual accepted mesh, with stable host/member IDs. Compare any post-placement shape regeneration deliberately: Unity preserves some authored gaps instead of automatically repairing all contacts. |
| Rock/ground separation | `TopDown3DMixedFormationSand.ContactFootprint` intersects triangles near the ground-contact height. Upper members are excluded from sand sources. | Build immutable contact contours from final placed geometry; use one contact record for sediment, gravel and debris. Avoid rectangular renderer bounds as visible masks. |
| Sand looks painted on | `SampleAuthoredDeposit` uses contact distance, wind, supply/slope gates, a rounded outer toe and a bounded weighted mean of overlapping banks. `MixedFormationSand.Apply` displaces the same ground mesh used for collision. | Generate a shallow deposit-height/coverage field from frozen contacts. Replace affected ground triangles locally; rendering, normals, collision and queries must use the same final surface. Do not stack a second opaque plane/collider over the terrain. |
| Empty or polka-dot ground | `MixedFormationClutter`: contact-derived masks and seeded pockets, round-robin attempts, size hierarchy, overlap rejection, slope seating and sand burial/suppression. The grid-stamped near-rock experiment is explicitly disabled. | Start with reusable small fragment meshes clustered around lower contact members, not another world grid. Keep fine grit in material shading. Apply group removal to dependent clutter; give interactive fragments their own stable identity if introduced later. |
| Unrelated material patches | Unity ground shader has shared deposit/erosion/exposure inputs, explicit transitions, paired alternate UV sampling and distance detail treatment. | Feed contact/sand/gravel meaning into the native material path. Apply matching UV transformations to albedo, height and normals; rotate sampled normals consistently. Keep world-scale patch meaning separate from breaking texture-tile repetition. |
| Dark seams or contact ambiguity | Unity rock shader consumes an explicit geological-seam channel; grouped mesher derives it from the two closest group fields and contact proximity. | Add this channel only with a documented vertex/material contract. Shader cracks already present in the native material are not equivalent to geometric ownership seams. Preserve crevices; excessive smooth union erases them. |

## Concrete next batch

Build one bounded native formation-and-ground reference scene using the existing
workbench, generator and streaming modules. The primary deliverable is a better
formation in its ground context, not more formation menu entries.

1. **Silhouette pass:** versioned profile/primitive parameters; three distinct shape
   families, restrained 3D orientation, four or more visibly useful variants. Carry
   dimensions, source seed and material controls through save/export. Compare the
   same camera and physical scale with neutral and textured views.
2. **Contact field:** freeze base terrain and placed mesh inputs, derive lower-member
   contours, then evaluate sand and gravel. Order is base terrain → placement →
   frozen contacts → deposits → final ground/collision → clutter. Do not feed new
   sand height back into placement and create a regeneration loop.
3. **Ground dressing:** one local refined patch with rounded banks, original ground
   maps and a small bounded debris set. Choose patch sampling against the existing
   triangle/staging budget. Unity's roughly 10 cm authoring sampling is not a safe
   blanket resolution for a 256 m native region: that would exceed 13 million ground
   triangles. Begin locally, merge/replace coarse coverage, and make edge samples
   deterministic; do not globally increase resolution.
4. **Stream integration:** preserve the same recipe/field through neighboring-region
   halos and retirement/reload. Contact influence crosses region borders, so removed
   formations must invalidate every affected ground/debris region, not just their
   owning region. Retain old ready geometry until the replacement and collider are
   adopted together. Cook/reuse geometry where live generation exceeds the budget.

The core contract remains version + seed + region + stable group/member identity.
Authored exclusions and route clearances constrain the entire influence footprint,
including banks and debris. Persisted group-removal deltas affect all derived
representations. Renderer/physics handles remain transient. Old recipes/world
profiles must remain readable; no implicit migration or changed identity meaning.

Use the existing mesh/terrain/stream owners. Add a dedicated terrain attribute
stream or versioned geometry layout for semantic masks: `SkinVertex` currently has
only one UV pair and no Unity-style color/UV2/UV3 contract. Do not overwrite skinning
attributes or reinterpret serialized model bytes silently.

## Bounded proof and stopping point

One native scene, fixed camera/seed/scale, before/after captures, and a focused check
for saved regeneration, contact/collider agreement and one region-border removal/
reload case are sufficient for the first connected pass. Inspect the actual images
for varied silhouettes, preserved crevices, rounded contact banks and clustered
size transitions. Do not call GPU success visual acceptance or begin repeated
stress campaigns. Hands-on gameplay and Windows proof remain separate.

Defer whole-formation fusion, dense POM/SSDM, virtual textures, dynamic sediment,
large-area erosion and expanded shadow systems until the representative scene
shows which remaining defect needs them. External research informs methods;
it does not substitute for looking at the output.
