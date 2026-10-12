# Greater Wasteland waypoint camp proposal

October 11, 2026. Design exploration and repository audit for the author. Establish one starting camp at Booter's present starting area, then use it to develop camp interaction, map discovery, and return travel. The user selected the single starting camp and discovered camps as future destinations. The rules and features below remain proposals; this document does not authorize runtime implementation or change world canon.

The recommended system is a network of sparse expedition camps with portable equipment and fixed geographic anchors. Each camp gives an expedition a place to return to, prepare, and depart from. Start with one readable camp and a useful return loop before expanding the network.

## Camp identity and purpose

Separate two scales of camping. Booter's existing portable shade is a temporary rest stop that can be deployed and packed locally. An established waypoint camp is a known place on the map, with a permanent site identity and a validated arrival area. Deploying the portable shade anywhere should not automatically create a fast-travel destination.

“Mobile” can describe the equipment: folded shade, strapped salvage boxes, removable frames, and tools that could eventually be carried forward. The waypoint remains fixed while established. Actual relocation is a later feature with physical packing, transport, and setup; moving a camp would invalidate its old travel destination. The first camp stays in place throughout iteration.

Camp should support the existing expedition rhythm: prepare, leave together or coordinate separate tasks, salvage, return, unload locally, and recover. A camp offers familiarity and preparation rather than automatic safety. Shelter, security, storage, and crafting should be distinct capabilities with their own eventual rules.

## First camp at the starting area

Use **Starting Camp** as a descriptive working label. The saved production scene places Booter's controller at local world coordinates `(-1848, -47.05604, -1498)`. The [scene builder](../../../Assets/_Project/Scripts/Editor/TopDown3D/BadwaterPlayableSceneBuilder.cs) independently uses the same horizontal spawn coordinates. This is a placement reference, not a ground-level camp transform: the player's vertical position includes body clearance.

Establish the camp around that starting area after checking live ground, support, player capsule clearance, Legger clearance, and departure paths. Keep the current player spawn unobstructed. Give the camp separate ground, interaction, resting, Booter arrival, and Legger staging anchors. Do not place every actor and prop on one coordinate or reshape native terrain to fit the camp.

The first visual arrangement can be small:

- A deployed folding shade and cloak resting spot reuse the current camp language.
- A modest anchored marker identifies the site from the elevated camera. A metal mast with a fabric strip is a candidate; a powered beacon is optional later fiction.
- A clear unloading patch gives the Legger somewhere to physically stop without blocking the sleeping space.
- One closed salvage box and a work surface can communicate expedition use. Keep them decorative until their inventory or crafting functions are explicitly implemented.
- An open departure lane makes orientation and arrival legible.

Avoid a conventional bonfire, plant decoration, or a water well. [World basis](../../WORLD_BASIS.md) establishes a barren world with algae-based survival and no natural liquid water. Algae reserves, sealed biological stores, and a dust collection device may eventually support preparation, but the camp must not silently generate unlimited supplies or refill vitals.

The first camp begins discovered because Booter starts there. New-session initialization can establish that known site once; it must not overwrite restored discovery or camp state. Future camps require physical discovery and a local interaction. No second camp is needed to test a return: walk away from Starting Camp, then return to it.

## Discovery and map presentation

Use an authored camp registry with a stable site ID, map position, arrival anchors, and display label. Future placements should serve expedition routes, meaningful landmarks, and useful regroup areas. Spacing should follow terrain and journey needs rather than a uniform map grid. This proposal adds exactly one site.

| State | Map presentation | Travel behavior |
| --- | --- | --- |
| Undiscovered | Hidden as a destination | Cannot select it for travel |
| Discovered and established | Small camp symbol with label on focus | Destination can be selected and eligibility checked |
| Temporarily unavailable | Known symbol with a clear reason on focus | Remains known; travel is disabled or interrupted |
| Packed or retired in a future relocation feature | Historical site marker only if useful | No active arrival destination |

Discovery is knowledge; travel availability depends on current conditions. Do not erase discovery because a route is blocked. For future camps, reaching and interacting with the site records knowledge; an extra setup cost is an optional later economy choice, not a prerequisite invented for the first camp.

A full world map is unnecessary for the initial loop. Begin with a compact **Camps** view showing Starting Camp, bearing, distance, and **Return to camp**. Later, the same registered location becomes a fixed point on the terrain map. Label straight-line distance separately from route distance; estimate travel time only when a validated route exists. The menu must not reveal unknown camps or remote threats.

Keep **Set up camp** and **Return to camp** distinguishable. The existing radial Camp command deploys or interacts with the portable kit; a return shortcut can be separately assignable without replacing customized sectors. Use existing UI focus, Submit, Cancel, and live binding prompts. Selecting a camp shows the journey summary; a fresh confirmation begins travel. Opening a menu or highlighting an icon does not move either actor.

## Fast travel choices

The [Legger standard](./BIGARM_COMPANION_STANDARD.md) requires him to keep a real world position and traverse physically. His cargo cannot become remote storage. Any travel design must preserve those rules.

| Model | Benefits | Main consequence |
| --- | --- | --- |
| Instant relocation of Booter only | Shortest interaction and simplest destination concept | Leaves the Legger at his real location; weakens distance and exposure, and still requires safe arrival and state handling |
| Accelerated physical return | Reuses live simulation principles, preserves both actors and supply costs | Requires a validated route and controlled movement; even acceleration may feel slow over great distances |
| Compressed journey with a fade or travel montage | Can make distant journeys brief while retaining elapsed time | Requires authoritative route progress, hazards, resource accounting, and companion simulation; currently lacks those foundations |

Recommend **accelerated physical return** for the first exploration slice. Booter follows a validated route back to the known camp under movement authority, with the live world accelerated through the existing speed owner. The Legger physically follows when participating. The player can cancel and take control at the actual position reached. This is a proposed fast-travel experience, not an existing capability.

Prefer traveling together initially. If the Legger is separated or cannot use the route, offer **Regroup first** through his existing physical approach command. Do not automatically issue a teleport or grant cargo access. Solo return can remain a later explicit choice that clearly leaves him behind; the first slice need not solve autonomous travel for a distant companion.

The current speed owner allows only 1x to 4x acceleration, and the Legger's existing local route search is bounded. Therefore begin with a short, validated corridor near Starting Camp. Do not promise a seconds-long crossing of Greater Wasteland using those systems. If player testing finds physical acceleration too slow at larger distances, develop compressed journeys as a separate world-simulation slice. Teleporting the Legger would require an explicit change to his established rules.

## Return journey rules

1. Select a discovered, established camp from the Camps view or assigned return shortcut.
2. Validate current action state, actor positions, a legal route, destination clearance, and resource margin. Recheck when confirmation is pressed.
3. Show estimated simulation duration and nourishment, hydration, and Reserve costs using current authored rates. Include the Legger's participation status. No additional camp token or arbitrary scrap toll is needed initially.
4. Confirm with a fresh input. Close the selection view, hand movement to the travel coordinator, and acquire the shared speed request only after the route is ready.
5. Move physically along the route. Apply elapsed simulation and survival depletion once through their existing owners. Travel does not count as sleep or heal Booter.
6. Cancel or interrupt at the current valid position. Release acceleration before restoring player control and clear held movement, attack, and menu inputs.
7. Arrive through a clear approach lane, stop in separate valid actor spaces, and restore ordinary speed. Arrival alone does not open storage, start sleep, or refill supplies.

Disable departure during sleep, camp setup or packing, harvesting, placement, traversal, or another action that owns movement. Require sufficient resource margin; recheck it during travel rather than spending the whole estimate upfront. Danger, damage, route blockage, lost companion progress, unsafe destination, low supplies, focus loss, device loss, and competing speed ownership should stop or reject travel with a useful reason. Exact threat detection and resource thresholds remain tunable later choices.

An interrupted journey spends only the resources consumed during actual simulated progress. A canceled journey leaves all inventories and world objects at their real locations. Do not clear enemies, reset depletion, duplicate cargo, pack a distant shade, or move a blocked actor to the arrival anchor. If an arrival area becomes blocked, stop safely on the approach instead of selecting an unchecked fallback position.

Travel availability should never be sold as emergency invulnerability. Starting Camp is not a guaranteed safe zone. A future security upgrade could change specific hazards only through an explicit mechanic.

## Repository audit and reuse boundaries

Live files inspected on October 11, 2026 establish these foundations and gaps:

| Foundation | Evidence | Reuse boundary |
| --- | --- | --- |
| Production starting position | [Greater Wasteland scene](../../../Assets/_Project/Scenes/Production/GreaterWasteland.unity), controller transform; scene builder spawn constants | Position a nearby authored site after ground and clearance checks |
| Deployable camp and recovery | [Camp controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Camping/TopDown3DCampController.cs) and [installer](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Camping/TopDown3DCampSceneInstaller.cs) | Controller owns one deployed kit and recognizes its own camp view; it is not a multiple-site registry |
| Camp identity | [Camp state](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Camping/TopDown3DCampState.cs) creates an instance ID on deployment | A deployment ID is not a deterministic authored waypoint ID; camp discovery needs a separate stable site identity |
| Live acceleration | [World speed controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Simulation/TopDown3DWorldSpeedController.cs) | One requester at a time, 1x to 4x; travel must not compete with sleep or write a second speed override |
| Recovery and survival | [Survival standard](./SURVIVAL_SYSTEM_STANDARD.md) | Reuse Rest and Sleep rules; existing session camp state does not establish disk persistence |
| Quiet menus and radial integration | [Menu direction](./MENU_SYSTEM_DIRECTION.md), [radial design](./RADIAL_MENU_SYSTEM_DESIGN.md) | Add a compact destination view; preserve personalized commands and live-world input arbitration |
| Companion movement | [Legger standard](./BIGARM_COMPANION_STANDARD.md) | Local pathfinding does not prove a route across the terrain or around a Booter-only climb |

A source search of the active TopDown3D runtime found no named fast-travel, waypoint, world-map, or minimap implementation. A camp destination registry, discovery state, map presentation, and return-journey coordinator would be new work. Existing generated-world save and world-generation references must not be installed into Greater Wasteland to fill these gaps.

An established site cannot simply be attached to the current portable-camp controller: its single-record ownership and packing flow would allow the destination to disappear or reject other camp views. Reuse the visual, placement checks, and recovery rules through an explicit integration plan that keeps site ownership separate from carried-kit ownership.

## World identity and persistence contract

- **Deterministic world identity:** bind site definitions to the authored Greater Wasteland world revision and immutable site IDs. A future seed can govern dressing without changing the first camp's identity. Do not derive identity from a display name, array index, or player position on launch.
- **Chunk streaming and unload or reload:** terrain remains fixed and always loaded, so terrain streaming is not applicable to this slice. If camp visuals are later culled with dressing cells, retain logical site and discovery state; visual reload must not recreate inventory or rediscover the site. Scene reload cancels transient travel safely.
- **Stable generated-object identity:** Starting Camp is authored, so procedural generation is not applicable to its placement. Future generated camps need stable IDs and authored exclusion rules before they enter the registry. Portable kit instance IDs and site IDs remain separate.
- **Authored constraints:** reserve camp footprint, resting and arrival spaces, Legger clearance, and departure corridor from future dressing. Never flatten or regenerate terrain for automatic camp placement. Route eligibility depends on each participating actor's actual traversal constraints.
- **Persisted runtime deltas:** eventually save discovery, establishment state, local container contents, upgrades, actor positions, and committed journey progress through one versioned authored-world save authority. Revalidate routes on restore. Map preferences must not own gameplay discovery, inventories, or actor relocation.

For an explicitly session-only prototype, show that discovery and runtime camp changes are not saved. Reloading should return to authored initial state without claiming durable progress. Persistent discovery requires a separate approved save slice; do not add a competing camp-only save file or reuse the deferred generated-world service.

## Expansion and implementation sequence

1. **Starting site:** one authored camp near the existing spawn, readable silhouette, valid approach and arrival anchors, and clear separation from the portable kit. Keep terrain and current gameplay intact.
2. **Known destination:** one site in a registry and a compact Camps view. Starting Camp begins known; verify discovery logic with isolated data rather than adding camps across production.
3. **Bounded return:** one short validated corridor, physical movement, shared acceleration, supply projection, cancellation, and interruption. No whole-terrain routing claim.
4. **Persistence:** integrate site knowledge and camp deltas with a versioned authored-world save owner when that slice is approved.
5. **Useful camp services:** evaluate physical stash transfer, a salvage work surface, supply preparation, and a placed dust collector one at a time. Preserve inventory quantities and actual access.
6. **Second site and longer journeys:** add a deliberately chosen expedition destination only after the first return loop is useful. Assess macro routing or compressed travel before increasing network scale.

Candidate upgrades should strengthen expedition planning: better shade, local storage capacity, a repair surface, a recognizable marker, and explicit protection from a defined hazard. Defer base-building grids, settlement simulation, relocation, destruction, respawn binding, automatic healing, passive production, and arbitrary camp spam. Fast travel and death recovery are separate decisions.

## Evaluation and next decision

Focused implementation checks should cover stable site identity, discovered-only destination selection, no duplicate initial site, portable-kit independence, actor-specific routes, resource accounting, speed lease release, blocked arrival, interrupted progress, input handoff, and save migration when implemented. The author owns hands-on acceptance and gameplay smoke testing.

The main player questions are whether Starting Camp feels worth returning to, whether its silhouette reads at gameplay distance, whether return travel removes repetitive navigation while preserving preparation, and whether coordinating with the Legger remains understandable. Test the short departure-and-return loop before tuning distant journeys.

The recommended next implementation brief is **one established starting site plus a known destination view**, followed by bounded accelerated return. Approval of this brainstorm should select the travel model and first camp services; it should not be treated as blanket authority for world-scale navigation, persistent saves, or new terrain content.
