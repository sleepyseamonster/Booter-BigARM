# Camp Loop five resource sources creative plan

October 14, 2026. Original scope: creative plan only. The author subsequently requested implementation with the [plan audit](CAMP_LOOP_FIVE_RESOURCE_SOURCES_PLAN_AUDIT.md) incorporated. Target: the existing `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`; production installation remains outside this scope. The [implementation receipt](../../Evidence/Gameplay/CampResources20261014/README.md) records current delivery and verification.

Build a small, coherent gathering circuit outside the camp's working space: an Ironstone outcrop, an ancient salt crust, an abandoned bristle hollow, a dead winch and one husk beetle. Each source should explain its material through shape, placement and the change left after harvesting. The circuit should feel like a piece of the wasteland rather than a row of vending machines.

This Camp Loop scope supersedes the production destination and durable-save stages in the [earlier audit and beetle plan](../Items/ITEM_SOURCE_AUDIT_AND_HUSK_BEETLE_PLAN.md) for this creative slice. The [five-source brainstorm](../Items/FIRST_FIVE_RAW_MATERIAL_SOURCES_BRAINSTORM.md) supplies the selected concepts. Material and biological detail remain proposals under the [world basis](../../WORLD_BASIS.md).

## Existing scene and design boundaries

Saved-scene inspection found separate roots for `Animation Playground`, `Camp Workstations`, `Camp Management`, `Camp Look`, `Legger Riding Study`, Booter, Legger, input, HUDs and native terrain. A prefab instance named `Camp Loop Lager Trial` is also present. Existing plans describe the animation stations, twelve original Ironstone nodes and ghost-machine salvage fixture. Their current locations, routes, identities and interactions are protected.

Booter's saved safe-return position was approximately X -1848, Z -1498. The saved Lager anchor was approximately X -1842, Z -1470. These are orientation references, not new placement coordinates. The scene and NPC authoring files are concurrently dirty; the implementation task must inspect their latest state and visible authoring changes before choosing positions. Disk inspection does not establish the live Editor state or current rendered appearance.

The [playground scene plan](CAMP_LOOP_ANIMATION_PLAYGROUND_SCENE_PLAN_2026-10-13.md) describes an initial layout proposal. Its later implementation status records that the station grid moved southwest. Do not use the original east/north diagram as a current occupancy map. Use the actual saved and live scene during future placement.

Follow the existing [Camp Loop salvage session policy](CAMP_LOOP_GHOST_MACHINE_SALVAGE_PLAN_2026-10-14.md): session-only state and coordinated laboratory reset. The existing [Lager](CAMP_LOOP_LAGER_ENEMY_IMPLEMENTATION_PLAN_2026-10-14.md) remains a separate skittish NPC. The husk beetle is a new actor with its own model, identity and behavior; do not rename or convert Lager.

## Spatial composition

Use three small environmental pockets linked by a clear walking route. Treat the following dimensions as layout proposals pending a terrain and occupancy survey.

| Pocket | Contents | Suggested distance from camp working edge | Composition |
| --- | --- | --- | --- |
| A: mineral shelf | Ironstone outcrop and salt crust | About 20–35 m | Two contrasting sources on a low, supported shoulder; separate their interaction areas |
| B: sheltered scrap edge | Bristle hollow and dead winch | About 30–45 m | Bristles sit in a quiet rock recess; winch occupies the more exposed edge with a few inert fragments |
| C: beetle ground | One husk beetle and its home anchor | About 40–60 m | Open native ground with a readable approach, room to step back and a bounded return area |

The intended sequence is camp → mineral shelf → sheltered scrap edge → beetle ground → camp. It is an optional circuit, not a gated quest. The player can visit any source first, and one source never requires another's reward.

Choose compass directions only after surveying the actual station, riding, Lager and salvage reservations. Preserve at least the existing 20 m camp reserve and 3 m return corridor where applicable; measure the full activity footprints rather than root-to-root distances. Maintain a proposed 3 m usable approach/turning strip at each static source for Booter and nearby Legger cargo. Leave the beetle roughly a 12 m clear encounter area as a starting footprint, enlarged if actual actor movement and retreat need it. Its territory and attack reach cannot cross a camp or station safety boundary.

Avoid cliffs, unsupported terrain edges, steep side slopes, station decks/ramps, ladder landings, throw lanes, riding routes, companion dismount clearances and the existing Lager's escape reservations. Do not place the new enemy within the existing NPC's activity area. If the three pockets cannot fit without moving protected content, shorten or relocate the proposed circuit within supported scene bounds, or report the conflict. Do not sculpt terrain, enlarge the scene or shrink existing fixtures to force it in.

Static sources should be visible from the normal elevated camera before their prompt appears. Rotate their readable side toward the approach and keep prompts from competing. Use modest, distinct silhouettes and material contrast instead of permanent floating labels or bright effects. A rock shoulder, pale patch, bristle silhouette and cable drum should each be identifiable without opening inventory.

## Scene hierarchy

Add one owned root alongside the existing scene roots. Existing objects are not gathered into or reparented beneath it. The following names are proposed scene display names; use PascalCase names for asset files and folders.

```text
Camp Loop Resource Study
  Study Services
    Resource Trial Session
    Source Registry
  Layout Anchors
    Circuit Entry
    Mineral Shelf Anchor
    Sheltered Scrap Anchor
    Beetle Home Anchor
  Resource Sites
    RS01 Ironstone Outcrop
      Visuals
        Rock Body
        Ore Face Available
        Ore Face Worked
      Collision
      Interaction Anchor
    RS02 Ancient Salt Crust
      Visuals
        Spent Deposit Base
        Collectible Crust
      Interaction Volume
      Interaction Anchor
    RS03 Bristle Hollow
      Visuals
        Shelter Stones
        Shed Bristle Clumps
      Collision
      Interaction Anchor
    RS04 Dead Winch
      Visuals
        Frame And Drum
        Recoverable Cable Coil
        Retained Cut Ends
      Collision
      Interaction Anchor
    RS05 Husk Beetle Site
      Site Dressing
      Actors
        Husk Beetle 01
          Visuals
            Body And Legs
            Exposed Fiber Layer
          Collision
          Attack Origin
          Harvest Anchor
  Editor Guides
    Route Guide
    Site Footprints
    Protected Area Guides
```

Keep the study root, service/site parents and moving actor root at unit scale with no runtime parent animation. The actor's own transform owns movement; visual children never move the physics root. Beetle home is an explicit reference to its anchor, not an obligation to keep the moving beetle parented to a translated marker. Anchors and editor guides have no gameplay collision, cannot be selected as harvest targets and are hidden in normal play.

Each site has one clear state owner and one selectable harvest target. Decorative salt crystals, bristle strands and cable loops are not separate rewards or extra interaction colliders. Keep collider ownership explicit so decorative children do not steal targeting or block access. `Visuals` can change or be replaced while source identity remains stable.

Use five reusable prefabs, with scene-local placement and session bindings. Future asset homes should follow existing project conventions: resource prefabs under `Assets/_Project/Prefabs/TopDown3D/Resources/`, the beetle under the existing `TopDown3D/NPC/` prefab lane, study settings under a dedicated `Settings/Studies/CampLoopResources/` folder, and corresponding Art, Materials and UI icon folders. These are proposed destinations, not file moves. Preserve GUIDs and `.meta` files when assets are eventually authored.

## Building the five sources

### RS01 Ironstone outcrop

Create a squat rust-red rock about 2–3 m across and 1–1.5 m high, with one dark fractured ore-bearing face at standing reach. Embed the rock base naturally without changing terrain. Make the accessible face visibly different from surrounding decorative rocks.

The player chips the face through one finite mining interaction. On success, the fresh ore surface becomes a worked, dull face; the main rock and obstacle remain. Reward the existing `resource.ironstone_ore` item.

This is a separate private laboratory source that yields the existing item. Do not register it as a thirteenth production Ironstone node, alter the twelve original nodes, or convert AP07's animation-only mining prop into a second real reward source. The new outcrop belongs entirely to the resource study and its reset ledger.

### RS02 ancient salt crust

Build a low, irregular patch roughly 1.5–2.5 m across. Pale mineral crust should sit on a darker spent-deposit base, following the actual supported surface without hovering, intersecting badly or flickering against terrain. Keep crystals small enough to read as crust rather than gemstones.

The player scrapes the patch. Harvest removes the collectible crust while leaving the dry substrate. Use a shallow selection volume and a reachable interaction point; it should not become an invisible walking obstacle. Output maps to `proposal.natural.salts`. It is an ancient dry deposit, with no water pool, contemporary evaporation or automatic food use.

### RS03 abandoned bristle hollow

Compose a roughly 2–3 m sheltered nook using a few low stones and several coarse, stiff bristle clumps. The bristles should resemble rigid animal structures, not grass, roots, woven cloth or soft bedding. Leave an open mouth wide enough for interaction from outside; do not build a narrow trap that needs crouching or terrain excavation.

The player gathers the loose clumps through one target. All harvestable clumps disappear together after the finite reward, while the stones and traces of the resting place remain. Output maps to `proposal.bio.bristles`. The source animal stays unidentified for this slice, so the set dressing establishes a proposed animal trace without adding another NPC.

### RS04 dead winch

Build a compact drum in a broken low metal frame, about 1.5–2 m across. Use enough wrapped cable to make the reward obvious, with one accessible loose end on the approach side. Keep decoration sparse; this is an independent discarded mechanism, not a new module on the existing ghost-machine wreck.

The player detaches a recoverable length. The removable coil disappears or becomes visibly stripped, leaving the drum, cut ends and frame. Output maps to `proposal.raw.metal_cable`, not the existing electrical Wire Bundle. No operating motor, live current, physics rope, powered recovery or assumed load rating is needed.

### RS05 husk beetle

Build a low, broad insectoid with overlapping shell plates, a distinct front and six simple articulated legs. A starting body envelope around 1–1.4 m long and 0.5–0.7 m high should be compared with Booter's approved 1.80 m reference height and the normal camera. These are blockout proportions, not final anatomy. Use a replaceable visual subtree and a simple collision body rather than one collider on every leg.

At rest, slow leg/body motion should show it is alive. It wanders only within its home area, alerts to Booter, and defends itself with one readable close-range attack. Telegraph, strike and recovery must look distinct. Retreating out of its territory ends pursuit. It does not chase into camp or attack the companion in this slice.

Booter can attack it using a real qualified damage interaction. An animation preview or the existing skittish Lager controller is not sufficient combat behavior. If that combat path is not ready, retain an explicitly incomplete beetle fixture until it is; do not substitute interaction-to-kill and call the enemy complete.

Death stops locomotion and attacks, settles the body against supported ground and reveals a scrapeable fiber layer. `Scrape husk` yields chitin fibers mapped to `proposal.bio.chitin_fibers`. The depleted body remains as a visibly worked carcass. There is no automatic death loot, icore/ichor mixture, extra meat or generic organ reward.

## Shared interaction and material presentation

All five sources produce one known raw item type each. The implementation uses the explicit trial quantities and metadata below, with finite gather durations and the existing in-place gather presentation. These are prototype settings, not values inferred from prop sizes or blanks in the spreadsheet. Dedicated extraction animation and final tool presentation remain later art/feel work.

Use the current device-aware prompt style with short verbs: `Mine ironstone`, `Scrape salts`, `Gather bristles`, `Recover cable`, and `Scrape husk`. Show the defined reward where existing UI supports it. Inventory icons should match the recognizable harvested form: ore fragment, pale crystals, stiff bristle bundle, stranded cable and scraped fiber tuft.

Extend the currently selected Camp Loop item catalog as one coherent set, retaining all existing definitions and explicitly resolving same-ID variants. Booter, Legger, cache and UI must resolve the same selected definitions. Do not replace the current catalog with an older baseline, create parallel competing catalogs, change starting supplies or add these concepts to the production catalog.

Raw rewards follow current nearby, finite Legger-cargo rules. A full, overweight or out-of-range destination leaves the source unconsumed and gives clear existing-style feedback. The player can cancel work. Do not create ground-loot scatter, auto-pickup or remote cargo transfer as a convenience. Each successful transaction changes source state and adds inventory exactly once. The new generic source does not inherit Ironstone's special automatic call-over branch; use the existing companion regroup command when needed. The winch has its own source record and never adds a second hard-coded ghost-machine wreck owner.

Implemented trial mappings are explicit. These quantities and new-item mass/stack values are prototype tuning, not spreadsheet edits or final economy:

| Source | Runtime reward ID | Quantity per trial | Unit mass / stack |
| --- | --- | --- | --- |
| Ironstone outcrop | `resource.ironstone_ore` | 2 | Existing 1.25 / 99 |
| Salt crust | `lab.resource.dry_mineral_salts` | 2 | 0.15 / 20 |
| Bristle hollow | `lab.resource.coarse_sensory_bristles` | 3 | 0.08 / 20 |
| Winch | `lab.resource.salvaged_metal_cable` | 1 | 0.8 / 20 |
| Beetle carcass | `lab.resource.chitin_fibers` | 3 | 0.1 / 20 |

All four new definitions use runtime category `Resource`, Legger-only carrying and distinct icons. Extend the existing private salvage catalog without replacing its asset identity or current definitions, retaining the scene's existing inventory/session bindings and loadout resolution. Confirmed trial reset owns the aggregate inventory drafts; the new study adds only its own prepared world-state mutation.

## Identity and repeatable Camp Loop trials

Use a private study identity plus immutable source IDs, for example `lab.resource-study.ironstone.001` through distinct named IDs for salt, bristles, winch and beetle. Display names, hierarchy order, coordinates and prefab instance IDs do not determine logical identity. The beetle keeps its identity after movement, death and harvesting.

Terrain generation and terrain streaming are not applicable: the study uses fixed, always-loaded native terrain. Procedural source spawning is also outside this authored five-instance layout. Disable/re-enable, camera culling, domain reload and visual replacement do not renew stock or resurrect the beetle. A versioned serialized study ledger retains five consumption records, health, revision and target epoch. The beetle's transform remains scene runtime state; its authored home is separate. Invalid retained state disables new harvesting without altering inventory.

Keep source consumption, beetle health/death and reward mutations in session-only state. Animation reset only cancels study attacks and invalidates stale captures; it preserves stock, health and inventory. The existing confirmed inventory-trial reset restores its agreed inventory baseline and all five sources together, including the beetle's starting health and supported home anchor. Its confirmation explicitly names this beetle exception while preserving Booter, Legger and their vitals. A failed world preflight or changed revision cancels the compound transaction. Never reset or teleport the Legger, and never create a second reset menu or save service.

Restore inventory through `TopDown3DCampLoopInventoryTrial` and its prepared participant contract, not through the sequential animation reset registry. Do not subtract quantities by item ID: the Ironstone item already exists elsewhere, so that could erase unrelated ore. Repeated confirmed resets restore the agreed whole-trial baseline without multiplying rewards. New availability requires that existing trial owner and session readiness. Failed reset does not independently restore study stock or publish a fresh beetle.

Prototype melee has one attack instance, an explicit hit window, range/facing/height and obstruction checks, and one potential target hit per attack. Player strikes use the existing imported unrestricted jab clip through the current presentation owner and a motor action lease. They cannot compete with gathering, riding, station actions, posture changes or modal UI. Mode/readiness/focus/pause loss cancels study intent. The beetle has 30 starting health, takes 10 damage per player strike and delivers one 6-damage bite after a 0.65-second telegraph, followed by 1.1 seconds of recovery. This establishes functional prototype behavior; animation contact, feel and final art acceptance remain author-owned.

There are no disk-persisted creature or harvest deltas in this slice. Preserve the scene's session-only save gate. Promotion into production, respawn, offline simulation, automatic corpse removal and durable-world migration require later scope.

## Build sequence for a later authorized implementation

1. **Survey and mark the circuit.** Inspect the latest authored scene, record existing reservations, choose supported site footprints, show editor-only anchors and preserve baseline object/GUID/reference state. Do not assign guessed Y values.
2. **Place the five blockouts.** Fit scale, silhouette, contact and approach access with no gameplay rewards. Check the view from the actual camera and both Booter and Legger movement clearances. Keep the beetle placeholder distinct from Lager.
3. **Finish the four static sources.** Add known item mappings, active/spent presentation, finite shared harvesting and coordinated session reset. Verify that original Ironstone and existing salvage remain separate and functional.
4. **Finish the living beetle.** Qualify movement, home limits, player attack, hit/death rules and carcass scraping, then connect that same reward/reset transaction. Reuse safe NPC movement seams where appropriate without changing the Lager profile or behavior.
5. **Compose and review the complete circuit.** Refine ground contact, source visibility, sound/motion cues and spacing. Keep working-area and hazard separation visible in editor guides. Remove temporary player-facing scaffolding after the sources explain themselves visually.

The build is complete only when all five are present under the intended owned hierarchy, rewards resolve correctly, active/depleted states are readable, repeated reset is coherent, and protected scene content remains intact. The beetle cannot move or attack after death. Death adds no inventory reward; its unharvested carcass can award one finite scrape reward, then stays unavailable until confirmed trial reset. Background import/compilation and focused structural/transaction checks support that handoff. Hands-on feel, appearance and gathering/combat acceptance remain author-owned; no gameplay smoke testing is requested here.

## Planning closeout

The original hierarchy, dimensions and distance bands guided the bounded implementation. Consult the implementation receipt for the surveyed positions, final owned hierarchy, offscreen blockout captures and exact test evidence. No whole-world population or production adoption is implied.
