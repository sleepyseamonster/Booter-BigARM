# Isometric Conversion Direction Brief

**Status:** Approved working contract for the protected technical spike
**Owner:** Gottspan under the user's creative and product authority
**Applies to:** CP-01 through CP-05 in `3D_CONVERSION_AUDIT_AND_CHECKLIST.md`
**Does not authorize:** purchases, package installation, Build Settings cutover, legacy deletion, or final art lock

## Direction

Booter & BigARM is moving toward a top-down, isometric-style 2.5D presentation built from 3D characters, environments, props, collision, lighting, and effects. Gameplay remains readable from a deliberately constrained elevated camera rather than becoming a free-camera third-person game.

The current 2D prototype remains intact as a behavioral reference and recovery point. The first 3D work is a separate conversion lab, not an in-place rewrite of `PrototypeScene.unity`.

## Spike Decisions

The user delegated execution of the conversion plan. Gottspan is therefore using the plan's documented recommendations as the working decisions needed to construct the spike:

| Area | Spike contract | Deferred decision |
| --- | --- | --- |
| UI | Existing screen-space HUD/menu approach remains the baseline. World-space markers are limited to interaction/readability experiments. | Final UI art and layout |
| Lens | Fixed orthographic isometric-style camera is the primary version. One mild-perspective comparison remains part of the acceptance evidence. | Final lens choice |
| Rotation | Fixed yaw; no player-controlled camera rotation. | Whether later combat or exploration needs rotation |
| Elevation | XZ ground plane with Y elevation. Modest ramps and height changes only. | Jumping, climbing, stacked floors, caves |
| Facing | Movement-facing interaction for the spike. Existing look input remains preserved for later aim evaluation. | Combat aiming model |
| Art | Stylized proportions, strong silhouettes, limited material families, readable values at gameplay-camera distance. | Final character/environment art bible |
| Platform | Current desktop development environment is the only performance target assumed by the spike. | Shipping platforms and budgets |
| Sourcing | Greybox primitives and project-owned temporary materials only. | Commission, purchase, marketplace, or AI asset decisions |
| Input | Gamepad primary; keyboard/mouse first-class. Touch, Joystick, XR, Attack, and Jump are outside the spike. | Additional input/platform support |

## Camera And Composition Contract

- Ground uses the XZ plane and vertical height uses Y.
- The first camera uses an orthographic projection, approximately 35 degrees down from horizontal and 45 degrees around world Y.
- Camera yaw is fixed so movement, silhouettes, entrances, and interaction sides remain predictable.
- Booter stays near the visual center with modest forward look-ahead only if play evidence supports it.
- The lab includes camera-side blockers so the occlusion experiment is tested intentionally rather than inferred from an empty room.
- The mild-perspective comparison must preserve approximately the same framing and view footprint as the orthographic version.

## Greybox Visual Contract

- Primitive geometry is intentional evidence, not production art.
- Booter, BigARM, resources, pickups, walkable ground, hazards/blockers, and interactable markers use distinct silhouette and color families.
- BigARM must read as much larger than Booter without hiding the player or making narrow traversal unusable.
- Lighting starts with one directional key light, ambient fill, and restrained shadows. Post-processing is optional and must not conceal readability problems.
- Materials remain few, opaque, and SRP-batcher-friendly during the spike. Occlusion behavior is allowed to use a temporary binary hide/reveal experiment.

## Interaction Contract

- Camera-relative movement is projected onto XZ and normalized so diagonal input is not faster.
- Rigidbody-based 3D collision is used for Booter in the lab.
- A ramp verifies grounded movement and modest elevation.
- One 3D harvest node verifies facing, reach, hold progress, inventory delivery, and depletion feedback.
- One 3D pickup verifies trigger-based collection and inventory delivery.
- A temporary BigARM follower verifies scale, follow distance, recall response, and camera occupancy. It is not the permanent AI conversion.

## Preservation Contract

- `Assets/_Project/Scenes/PrototypeScene.unity` and `SampleScene.unity` are not modified by the spike.
- `Renderer2D.asset` stays the default renderer at index 0.
- The new renderer is added at a non-default index and only the conversion camera selects it.
- The conversion lab is not added to enabled Build Settings during the spike.
- `Sand Patch Grid` and `Ground Grid` remain disabled in the legacy prototype.
- Existing user-owned dirty ground-art files are outside this batch and remain untouched.

## Acceptance Questions For CP-06

The spike is evidence for a later user decision, not automatic approval to convert every system. CP-06 must answer:

1. Is Booter movement clear and comfortable with the fixed isometric camera on both gamepad and keyboard?
2. Does orthographic or mild perspective better serve the desired look without damaging navigation clarity?
3. Can Booter remain visible around tall geometry with the proposed occlusion approach?
4. Can BigARM feel enormous while remaining navigable and camera-readable?
5. Are harvesting and pickup prompts legible at the intended camera distance?
6. Do modest ramps add useful depth without implying a larger vertical traversal system?
7. Is the result strong enough to proceed to shared runtime seams and current-loop parity?

## Stop Conditions

Stop the batch and preserve evidence if any implementation would require overwriting the legacy scene, changing the default renderer, editing Build Settings, installing a package, buying or licensing assets, deleting legacy content, or choosing final art/platform scope. Those actions remain separately gated in the master plan.
