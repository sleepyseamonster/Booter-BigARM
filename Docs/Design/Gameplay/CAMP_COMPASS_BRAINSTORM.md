# Camp compass audit and proposal

2026-10-12. Exploration only; this document does not install a Unity feature. The user confirmed that **Booter's facing direction** drives the compass. Camera yaw does not drive it.

## Proposed behavior

Wrap the lower-left algae consumption control cluster in a compass ring. Keep the consumption prompts, canister, quantities, and feedback upright. The compass has three separate cues:

- A dark sphere marks north / darkward.
- A light sun marks south / lightward, exactly opposite the dark sphere.
- A narrow outlined arrow points toward Starting Camp, independently of the north/south pair. Amber is a candidate color; shape and radial placement must distinguish it from the sun and consumption prompts.

The top of the instrument means **ahead of Booter**. A small fixed forward notch makes that reference visible. As Booter turns east, north moves to the left and south to the right. This is a body-relative navigation instrument; with an independently orbiting elevated camera its arrow can differ from the camp's apparent screen direction. Teach it once with a brief “Top = ahead of Booter” cue, then let the symbols carry the interface.

Use Starting Camp as the provisional local navigation origin `(0, 0)`. This is an offset applied to displayed coordinates, not a relocation of terrain, actors, camp definitions, or existing world origin. The first slice needs only this one camp; target selection among discovered camps belongs to a later iteration.

## What the existing project supports

| Surface | Evidence | Consequence |
|---|---|---|
| HUD sizing | [Action D-pad HUD](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DActionDpadHud.cs) uses a 168-unit square and 34-unit lower-left margin. | Anchor the ring to this cluster rather than duplicating its safe-area calculation. |
| Algae layout | [Algae HUD](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Survival/TopDown3DAlgaeCanisterHud.cs) places a 235 × 196 detail block 16 units to the right, with a backdrop extending left by 10 units. Dust text sits 23 units below the cluster. | A new outer ring would collide with existing content without a coordinated layout change. |
| Shared canvas | [Game HUD canvas](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DGameHudCanvas.cs) uses a 1920 × 1080 reference resolution and screen scaling. | Specify dimensions in reference units and verify safe areas and narrow displays. |
| Physical heading | [Player motor](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.cs) exposes `FacingDirection`. | Read actual facing, not input axes or velocity. Walking backward must not flip the instrument. |
| Independent camera | [Camera rig](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DCameraRig.cs) exposes camera yaw separately. | Orbiting the camera alone leaves the compass unchanged. |
| Authored grid | [Badwater scene builder](../../../Assets/_Project/Scripts/Editor/TopDown3D/BadwaterPlayableSceneBuilder.cs) maps easting to X and northing to Z. | East is +X; north is +Z. Treat north=darkward and south=lightward as this user's provisional navigation convention. |
| Stable destination | [Waypoint settings](../../../Assets/_Project/Settings/Camping/Resources/TopDown3DWaypointCampSettings.asset) defines `camp.starting` in `greater-wasteland.authored.v1`, at (-1842, -48.08643, -1498). | Resolve the established site by identity; do not point to a portable kit or a remembered actor transform. |

[World Basis](../../WORLD_BASIS.md) establishes darkward/lightward planetary poles and perpetual twilight, but does not independently assign their map axis signs. These symbols indicate directions, not time of day, sun altitude, or a new astronomical simulation.

## Geometry contract

Use the horizontal X/Z plane and ignore height for bearing and displayed distance. Define headings in degrees clockwise from world north:

```text
heading = atan2(facing.x, facing.z)
campBearing = atan2(camp.x - player.x, camp.z - player.z)
darkwardAngle = -heading
lightwardAngle = 180 - heading
campArrowAngle = campBearing - heading
localEast = player.x - camp.x
localNorth = player.z - camp.z
```

Normalize displayed angles consistently. Unity UI Z rotation uses the opposite sign to these clockwise angles; apply that conversion at the rendering boundary. Circular symbols need their positions rotated, while labels remain upright.

| Booter position relative to camp | Booter facing | Dark sphere | Sun | Camp arrow |
|---|---|---|---|---|
| West of camp | North | Top | Bottom | Right |
| West of camp | East | Left | Right | Top |
| North of camp | North | Top | Bottom | Bottom |
| North of camp | East | Left | Right | Right |
| At camp | Any | Follows north | Opposite north | Arrival cue |

## Layout and legibility

A circular ring enclosing the full 168-unit square would require about 238 units of diameter before symbol clearance. Do not assume a 220-unit circle encloses square corner content. First inspect the actual visible glyph bounds, then fit the ring around the consumption marks with room for pulse animation, labels, and the spheres.

Start with a roughly 240–260-unit outer envelope, subject to visual acceptance. Place dark/sun symbols on the outer track and the smaller camp arrow on an inner track. This separates coincident bearings without jitter, occlusion, or a fourth apparent consumption action. The ring is a passive graphic and must not intercept pointer input or become another menu button.

Shift the cluster inward enough to contain the outer envelope and dust text within the safe area. Move the canister/details block right according to the ring's measured bounds; its current backdrop begins only 174 units from the cluster's left edge. Derive sibling offsets from a shared layout envelope rather than accumulating unrelated constants. At narrow widths, keep navigation and consumption legible and reduce optional detail text before shrinking symbols into unreadability.

Use a rim on the dark sphere so it survives dark terrain, and a distinct sun treatment so the light sphere and camp arrow differ by more than color. Keep the ring restrained: no permanent coordinate grid, distance card, or large directional labels. A small “Starting Camp · 20 m” line is optional and should reuse a clear area rather than overwrite survival feedback. Compass distance is horizontal straight-line distance; it must not be labeled as route length.

## Gaps and edge cases to resolve

1. **At the origin:** bearing is undefined. Propose an arrival state within 2 m, leaving it beyond 3 m to prevent flicker; these are presentation thresholds, not camp interaction or travel rules. Show a small stable camp cue and remove the directional arrow. Match established camp proximity semantics if their existing thresholds provide a better fit.
2. **Angle wrap:** 359° to 0° must take the short path. If smoothing is added, use shortest-angle interpolation and unscaled UI time so 1×–4× world speed does not change instrument responsiveness. Prefer immediate truthful orientation first; tune smoothing through user play acceptance.
3. **Invalid sources:** preserve the last valid heading if its horizontal vector briefly degenerates. Hide/mute the destination arrow when the camp or player is unavailable; do not substitute Unity world `(0, 0)` as a destination.
4. **Lifecycle:** bind after the player and established camp resolve, tolerate respawn/scene changes, and avoid duplicate HUDs or stale scene references. At teleport/fast-travel arrival, recompute directly instead of sweeping the arrow across obsolete bearings.
5. **Different navigation claims:** the arrow points toward camp through terrain. It does not promise a walkable path or return availability. Existing bounded return-corridor guidance remains responsible for getting onto the supported route; the compass must not override that guidance.
6. **Camp distinction:** Starting Camp remains the reference even when the portable recovery kit is carried or deployed elsewhere. Later selected destinations can change the arrow without silently changing the coordinate origin.
7. **Companion and cargo:** the instrument must not imply Legger or cargo will teleport, change regroup conditions, or alter existing return restrictions.

## World and persistence boundary

- Deterministic identity: use the existing authored world/site IDs. No new world seed or generated content is involved.
- Streaming/unload: Greater Wasteland terrain is fixed and always loaded. No terrain streaming work applies; UI still needs ordinary player/scene lifetime handling.
- Generated-object identity: not applicable because the feature renders a direction to an established authored site and creates no world objects.
- Authored constraints: read the existing grid, camp position, HUD, and survival state. Do not move terrain or turn the provisional planetary mapping into additional lore claims.
- Persisted runtime deltas: none for the first slice. Heading and bearing are derived every frame; do not create a save-format change. Future discovery/selection persistence belongs to the camp system's own versioned work.

## Small implementation slice, when approved

1. Add a small compass presenter in the active TopDown3D runtime assembly, with a separate pure bearing calculation seam. Resolve the starting site through the existing camp settings/controller contract.
2. Add a passive ring graphic to the shared HUD, scoped to the algae cluster. Coordinate its layout with the D-pad and algae details; do not rotate the existing controls.
3. Implement the confirmed facing-relative north/south and camp positions, arrival cue, null-source handling, and safe-area placement. Leave target selection, crafting, storage, and new travel rules out of this slice.
4. Verify focused EditMode contracts for the orientation table, angle wrap, world-origin offset, arrival hysteresis, invalid sources, camera independence, and HUD lifetime. Check layout bounds at representative aspect ratios through static inspection; Unity compilation/import is required for runtime work.
5. Let the user judge legibility and feel in play. No gameplay smoke test is authorized by this exploration request.

## Exploration proof

The accompanying interactive orientation study uses Booter's confirmed facing reference, the five relative-position cases above, separate marker tracks, and an at-camp cue. It is a geometry explainer, not a screenshot or installed HUD. Its dimensions do not claim to validate the production HUD fit.

Documentation links and whitespace were checked. Browser inspection covers the explainer at narrow and normal conversation widths and verifies its heading/position controls. Unity gameplay, import, and play feel are not validated by this documentation-only work.
