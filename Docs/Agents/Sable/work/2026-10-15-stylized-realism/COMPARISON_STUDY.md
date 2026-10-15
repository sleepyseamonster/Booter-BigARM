# Proposed stylized realism comparison study

October 15, 2026. Proposed by Sable from the [art research](README.md), then authorized by the author's follow-up request. The [completed offline comparison](STUDY_RESULTS.md) records delivered views, material methods, verification, and limits; production integration remains separate.

## Question to resolve

How visibly can surfaces be painted while the scene still feels physically believable, grounded, and readable from the production gameplay camera?

The author has already chosen stylized realism as the research direction. This study would resolve the degree of surface simplification and brush visibility, rather than reopening the overall style choice.

## Smallest useful scene

Use an isolated comparison containing a representative existing rock and ground patch, one simple metal or ceramic prop, and Booter or the Legger for scale. Preserve all original source assets and create separate candidate materials or textures. Do not use the current dirty camp scene or character sources as unowned scratch space.

Blender owns any assigned model or source-paint work. Babineaux owns Unity setup, capture, imports, and technical verification. Sable owns the art brief and visual review; Gottspan coordinates integration. This document starts none of those activities automatically.

## Controlled comparisons

| Pass | Change | Keep constant | Decision |
| --- | --- | --- | --- |
| A | Grounded baseline using the same source forms and existing lit-material approach | Geometry, scene arrangement, camera, exposure, and lights | Reference for construction and material legibility |
| B | Reduce noisy detail and add restrained strokes that follow form | Same geometry, camera, exposure, and lights as A | Whether painting improves coherence without losing material identity |
| C | Increase stroke visibility and simplify secondary variation further | Same conditions as A and B | Locate the point where the scene becomes too graphic, miniature-like, or soft |
| Lighting comparison | Use the preferred texture treatment under alternative light/value grouping | Texture, geometry, framing, and exposure fixed within each comparison | Separate lighting improvement from texture improvement |

Do not use a global oil-paint filter as the only painterly treatment. It would not answer whether the underlying assets are coherently authored. A later shader experiment is justified only by a specific visual gap remaining after this study.

## Required views

- Normal gameplay distance, including the surrounding ground and actor silhouette.
- A closer view to judge stone, metal, ceramic, dust, and edge treatment.
- Brighter-sunset and deeper-twilight conditions, paired consistently across candidates.
- Supported camera angles that reveal whether painted shading conflicts with changing light or view.
- A grayscale companion view for judging value grouping; color views remain the approval reference.

Record actual camera, resolution, exposure, light conditions, and asset revisions. Use the project's current camera behavior rather than assuming No Rest for the Wicked's fixed-view advantages apply here.

## Visual review criteria

Prefer the candidate where material identity remains clear, the actor and traversable ground are easy to find, broad forms survive at game distance, and painting adds intentional shape rather than generic noise. Preserve credible rock scale, prop thickness, load relationships, contact shadows, and the barren setting. A small image should still read coherently without depending on close-up texture detail.

Identify failures explicitly: overly smooth plastic, equal roughness on every material, heavy cavity shading, noisy outlines, brush patterns that change apparent scale, exaggerated proportions that weaken vulnerability, or lighting that conceals gameplay information. These are review questions, not claims about the existing assets.

## Delivery and boundaries

Deliver labeled comparison captures, exact candidate paths, a recommendation, and the specific user decision still needed. Store small review artifacts beside the task record; keep native source art and large temporary captures in their established locations. Blender source files remain local-only.

This proposed visual study introduces no placement or gameplay objects with persistent state. Deterministic world identity, generated-object IDs, cell unload/reload, and saved deltas are therefore not applicable unless its implementation scope expands. Authored terrain and exclusion constraints remain protected; terrain generation and terrain streaming remain outside scope. Do not promote the chosen study into Greater Wasteland without the subsequent task's integration scope.
