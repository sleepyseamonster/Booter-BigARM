# Item source audit and husk beetle plan

October 14, 2026. Owner: Gottspan. Status: audit completed; creature behavior, yields, placement and implementation below are proposals. The current request prioritizes auditing and planning before creating gameplay.

Start with one husk beetle and one fiber reward. Prove the complete attack, death, harvest and cargo transaction before expanding into mineral nodes, loose collectibles or additional creatures. The item database is a useful design register, but most rows do not yet specify playable sources or complete processing recipes.

## Source and authority

- Audited [MASTER_ITEM_DATABASE.xlsx](MASTER_ITEM_DATABASE.xlsx), all 15 sheets, without changing or exporting it. Source SHA-256: `dc27ff46d7db7e0b0cd1df633d0c8e68824cff95a4342a9036459cc7368d3982`.
- [World basis](../../WORLD_BASIS.md#material-culture) controls material culture. Shell husks supply scraped chitin and icore fibers; paper and difficult pressed materials are downstream products. No cloth, plant-derived inputs or natural free water should be introduced.
- [Root agreement](../../../AGENTS.md), [fixed-terrain dressing plan](../Gameplay/FIXED_TERRAIN_DRESSING_PLAN.md) and [companion standard](../Gameplay/BIGARM_COMPANION_STANDARD.md) control production scope. Greater Wasteland has fixed, always-loaded terrain. Older generated-world assumptions in engineering standards do not override that direction.
- The workbook Guide!B47 identifies source biology and domestic burden-beetle records as proposals. The husk beetle requested here is a new local gameplay concept; do not equate it with that domestic creature or the three megafauna concepts.
- Existing definition, accepted material-family direction, workbook proposal and production installation are different claims. A serialized asset alone does not prove production availability.

## Workbook inventory

| Measure | Verified result | Workbook location |
| --- | --- | --- |
| Master items | 408, with unique nonblank IDs and names | Items!A6:B413 |
| Existing definition IDs | 18 | Items!AC6:AC413; Guide!B8 |
| Other design/source concepts | 390 | Items!AC6:AC413 |
| Design status | 18 existing definitions, 75 author direction, 311 proposals, 2 source proposals, 2 withheld assumptions | Items!AC6:AC413 |
| Material tiers | 103 T0, 41 T1, 4 T2, 152 finished items, 108 unassigned; no T3/T4 assignments | Items!AF6:AF413 |
| Item uses | 567 relationships: 408 primary and 159 secondary | Item uses!A6:F572 |
| Serialized variants | 20 asset rows covering 18 unique IDs | Variants!A6:M25 |
| Existing recipe references | 10 input/output rows, not 10 separate recipes | Recipes!A6:I15 |
| Resource chains | 29 proposed processing relationships | Resource chains!A6:H34 |
| Material routes | 47 candidate ingredient edges | Material routes!A6:J52 |
| Creature concepts | 3 megafauna proposals | Creature concepts!A6:I8 |

Primary categories reconcile exactly: Equipment 91; Resources 163; Crafting components 83; Containers 20; Cultural objects 21; Everyday objects 9; Consumables 9; Weapons 6; Field supplies 5; Currency 1. These counts partition the master rows. Overlapping families elsewhere in Categories must not be added to this total.

The 103 T0 entries divide into 47 Geological, 36 Fauna, 19 Industrial salvage and 1 Atmospheric source. These are material-tier counts, not the Resources category count or counts of implemented nodes. T0 also includes recovered or extracted crafting inputs; it does not mean every row can be picked up from the ground as a naturally pure material.

## Data integrity and readiness

The saved workbook has no error-typed cells across 2,174 formula cells. Checked ID references resolve in Variants (20), Recipes (10), Item uses (567), Resource chains (58), Material routes (94), Creature concepts (20) and Regional demand (38). Multi-ID fields were split on semicolons. Every item has one primary-use relationship matching its master use, with no duplicate item/use pair. Linked use names/categories and material-route names/tiers were checked against master rows. All 47 material-route edges advance one tier. Checked categorical values match Taxonomy. All 20 variant source assets exist and their serialized item IDs match the workbook.

These checks compare stored XML values and formula text with independently derived relationships. They do not establish fresh native Excel recalculation, interactive filter behavior or Unity gameplay. The source file remained byte-identical throughout the audit.

The principal implementation gaps are:

1. **Acquisition is descriptive rather than executable.** Items!K describes where a material might come from, but there is no shared source definition with source ID, species/deposit, interaction, output quantity, depletion, placement constraints and persistence contract. Do not infer these from a material name.
2. **Most materials lack processing coverage.** Only 36 of 103 T0 IDs occur as Material routes inputs; 67 do not. This is incomplete proposed-route coverage, not proof that those 67 have no direct use. Route edges also lack quantities and alternative-versus-combined ingredient rules.
3. **Tuning remains unspecified.** All 390 non-existing concepts have blank stack and mass cells. All 408 entries have blank numeric trade value, damage, protection and durability. These blanks mean undecided, not zero. Supply rates cannot be balanced from this register yet.
4. **Classification is richer than the runtime schema.** Workbook category `Resources` is not the runtime category literal `Resource`. Display categories and design statuses must be deliberately mapped, never imported verbatim as behavior flags.
5. **Existing variants need deliberate selection.** The dust harvester and pop-up camp each have two asset variants sharing an ID. Do not load all variants into one runtime catalog or treat them as distinct master items.
6. **Source families overlap.** Magnetite feed versus existing Iron-rich Concentrate, and silica-derived abrasive feed versus Mineral Sand, already carry reconciliation notes in Resource chains!F6:F8. Keep reuse/merge decisions ahead of new asset creation.
7. **High-value biology is not a universal loot table.** Ichor, organs, venom, hide, fur, wings and other anatomy need species-specific justification. Withheld assumptions are excluded. The first beetle does not grant all 36 faunal T0 materials.
8. **Persistence needs a creature-specific extension.** Existing production checkpoints cover inventory and resource state, and optionally population receipts. They do not currently encode beetle health, death or remaining carcass yield.

## How materials should enter the world

Use a small set of acquisition behaviors with separately authored source content. One behavior can serve several resources; one resource can have several approved sources. World entities are not inventory items: a beetle, its carcass, the harvestable shell layer and the fiber stack have different identities and lifecycles.

| Acquisition behavior | Workbook candidates | First implementation direction | Boundary |
| --- | --- | --- | --- |
| Fauna carcass harvest | Shell, fibers, jaw, tendon, membrane | Husk beetle: scrape one finite carcass reward | Assign only chitin fiber initially; anatomy-specific rewards later |
| Loose natural collectible | Shed plates, bristles, underfur, loose mineral fragments | A detached husk using the same harvest transaction | Separate placed source ID; no automatic farming/renewal |
| Fixed mineral outcrop | Ironstone, magnetite-rich stone, copper-rich stone, salts | Extend the existing Ironstone pattern after reviewing material overlap | Native terrain untouched; finite source state and authored material identity |
| Site salvage | Metal cable, ceramic/glass shards, chain, workshop waste | Bounded wreck or workshop source using existing salvage as reference | Preserve item identity and avoid rewarding one part twice |
| Atmospheric collection | Airborne Dust | Reuse the existing deployed-canister lane | Collector behavior differs from instant ground pickup |
| Contained biological extraction | Ichor, secretion, enzyme, venom | Later dedicated extraction with compatible vessel requirements | No generic liquid reward without containment, identity and hazard decisions |
| Ancient recovery | Extremely rare ancient manufactured materials | Later authored salvage opportunity | Recovery grants no present-day manufacturing capability |

The appendix maps every T0 row to a proposed acquisition lane. Material processing and finished objects stay out of natural spawn tables unless separately justified as salvage.

## Husk materials and processing

| Record | Current workbook meaning | Beetle implication |
| --- | --- | --- |
| Items row 40, `proposal.bio.shell` | T0 shell husks / shed plates | Potential intact-shell reward or loose molt; not required for the first direct-scrape loop |
| Items row 75, `proposal.bio.chitin_fibers` | T0 scraped chitin fibers | Recommended first reward; working runtime ID `resource.chitin_fibers` must be explicitly mapped to this design ID |
| Items row 76, `proposal.bio.icore_fibers` | T0 scraped icore fibers | Keep distinct; spelling and identity unresolved; not liquid ichor |
| Items row 77, `proposal.bio.husk_paper` | T2 husk-fiber paper | Later processing endpoint; no direct beetle drop |
| Items row 78, `proposal.bio.pressed_husk` | T1 pressed material | Later difficult processing endpoint; no direct beetle drop |

Resource chains rows 28–33 describe shell scraping and possible paper/pressed-material uses. Material routes row 35 introduces T1 prepared husk pulp, and row 47 links it to T2 paper. The broader resource-chain paper edge is conceptual shorthand, not permission to bypass that proposed intermediate or treat two fiber alternatives as a mandatory combined recipe. Scraping shell into raw fibers remains a T0 acquisition operation in the workbook; tier increases do not count every physical work step.

For the first beetle, choose **direct carcass scraping into chitin fibers**. This satisfies the requested harvest with one new inventory definition. An intact-shell collection branch could follow later, but it must consume the same finite carcass stock; it cannot award a complete shell and then let the player scrape that same shell again for free. Paper and pressing add separate stationary processing work later. No automatic paper recipe, icore chemistry, sale price or textile guarantee is approved here.

## Proposed husk beetle experience

A squat, low armored insectoid should be readable from the existing elevated camera: broad husk plates, simple articulated legs, a clear front and a visible exposed fiber layer after death. Start with a placeholder assembled mesh and a few animations. Blender source authoring and Unity import remain separate specialist lanes if final art is requested.

The proposed first enemy is defensive. It idles or wanders within a small authored home area, notices a nearby threat, turns and telegraphs one short bite or shove, then recovers. It can approach within that area and return home when the player retreats. It does not pursue across the world, burrow into terrain, summon allies, launch projectiles or affect the Legger's behavior.

| State | Behavior and exit |
| --- | --- |
| Idle / wander | Pick supported short destinations within the home boundary; stop at invalid slope, obstacle or drop |
| Alert / approach | Face and approach a valid nearby player; lost target returns to home behavior |
| Attack windup | Readable preparation with direction committed before the strike |
| Strike / recovery | A bounded hit window can damage Booter once per attack, followed by a recovery interval |
| Hurt | React without replaying the same incoming hit; health reaching zero overrides all living states |
| Dead, harvestable | Stop AI and damage, retain an identifiable grounded carcass, expose `Scrape husk` interaction |
| Depleted | Disable further harvesting, retain a spent carcass presentation initially |

Booter attacks deliberately using the shared input/action ownership rules. A player attack must have an authoritative hit window, range, facing and obstruction checks, and one hit per target per attack. Animation playback by itself must never award damage. Attack and harvest cannot run together; menus, riding, camp/station activity and other action owners must reject or cancel incompatible commands predictably. Both keyboard/mouse and gamepad paths need verification.

The carcass harvest is a timed interaction using existing targeting and cargo routing. Death does not instantly add fibers. Cancellation, a blocked path, missing Legger, full cargo, overweight cargo or a save/restore conflict must leave the uncommitted yield available. Repeated input, multiple child colliders and repeated death notifications cannot multiply rewards.

Recommended first tuning envelope: one enemy, one short melee attack, one finite scrape action, one output item, no respawn and no autonomous population. Health, damage, attack timings, ranges, yield, fiber stack limit and mass become explicit prototype tuning values in the later implementation, not accepted economy or species canon.

## Existing Unity seams and required changes

All production runtime work belongs in `BooterBigArm.TopDown3D.Runtime` under `Assets/_Project/Scripts/Runtime/TopDown3D/`.

| Existing seam | Evidence and reuse | Required addition or restriction |
| --- | --- | --- |
| [Item definition](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Items/TopDown3DItemDefinition.cs) | Stable ID, icon, stack, mass, carry metadata; validates `Resource` as Legger-only | One fiber definition, icon and selected catalog entry; keep design/runtime ID mapping explicit |
| [Interaction controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DInteractionController.cs) and [interactable contract](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/ITopDown3DInteractable.cs) | Facing, distance, line of sight, target availability and timed single-item rewards | Carcass adapter implementing availability and finite consume; living target is not harvestable |
| [Gather action](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DGatherActionState.cs) | Final destination checks and source consume inside inventory transaction guard | Reuse for single reward; prove state notifications and reentrancy cannot duplicate it |
| [Item destination service](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DItemDestinationService.cs) | Resource rewards require accessible finite Legger cargo | Preserve range/mass/capacity rules; no remote transfer or Booter-pocket fallback |
| [Ironstone node](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DIronstoneNode.cs) and [resource state](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DResourceWorldState.cs) | Reference for finite source deltas and depleted presentation | Resource registry is typed to Ironstone nodes; do not disguise beetles as rock nodes or broadly refactor it for this slice |
| [Animation input](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DInputRouter.AnimationPlayground.cs) | Existing Attack binding path is gated through the animation laboratory | Add a deliberate production combat owner; do not promote a lab animation event into damage logic |
| [Survival vitals](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Survival/TopDown3DSurvivalVitals.cs) | Existing Booter health and health-reduction event | Minimal enemy damage receiver/state plus bounded player damage integration; no general armor redesign |
| [Fixed population identity](../../../Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainPopulationIdentity.cs) and [population state](../../../Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainPopulationState.cs) | Deterministic candidate IDs and sparse removal receipts independent of presentation | Identity pattern is reusable; current population snapshot stores removals rather than living enemy state |
| [Greater Wasteland save service](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandInventorySaveService.cs) | Production inventory/world checkpoint and session-only laboratory policy | Versioned creature records and joint reward/depletion checkpoint integration; preserve incompatible saves and backups |

Suggested new owners are a small creature definition, a testable husk-beetle state model, a terrain-aware scene controller, a carcass interactable and a player melee action controller. Keep creature identity/state separate from renderer lifetime. Add editor-only setup/validation only if needed; do not create a generic enemy framework, loot generator or navigation-package dependency before the first enemy proves the need.

## World identity and persistence contract

- **World identity:** use Greater Wasteland's existing explicit world identity. First placement is a saved authored spawn anchor with a stable authored ID. Later procedural sources use explicit population seed, generation/recipe versions, cell and candidate ordinal. Do not infer a new world seed or reuse generated-terrain services.
- **Stable object identity:** beetle ID survives death, carcass conversion, movement, save and reload. Carcass state is another phase of that creature, not a newly randomized loot object. A moving beetle retains its spawn ownership; current position is not its identity.
- **Terrain streaming:** not applicable because Greater Wasteland terrain is fixed and always loaded. Presentation culling or future dressing-cell activation must not reset creature logical state or source depletion. Terrain unload/reload infrastructure is outside this slice.
- **Authored constraints:** hand-place the first enemy on supported ground away from spawn safety, camp placement, travel corridors, cliffs and protected landmarks. Movement remains within a bounded home area with collision, slope and ledge checks. No whole-world navigation bake is assumed.
- **Runtime deltas:** persist health, alive/dead/depleted phase, remaining harvest stock and the required transform/home identity. Prototype policy is no respawn and no healing/reset from culling. Pause AI when the session is paused and stop updates after death. Offline simulation and corpse expiry are deferred.
- **Atomic save outcome:** checkpoint cargo reward and carcass depletion together. Never write an independently saved carcass state that can diverge from saved inventory. Add creature revision tracking and combat/action safety to the production checkpoint boundary. Specify and test old-save migration before raising schema version; missing creature fields must not silently resurrect a killed enemy or erase files.
- **Proof:** authored-ID uniqueness, same identity after movement, duplicate registration rejection, cull/re-enable, fresh load, death-before-harvest, partial damage, harvest-before-save, incompatible snapshot and interrupted-checkpoint recovery all have explicit expected states. Later generated population additionally needs seed/order/cell-boundary determinism checks.

## Implementation sequence for later execution

| Step | Scope and deliverable | Completion gate |
| --- | --- | --- |
| 1. Freeze the small content contract | Direct-scrape chitin reward, design/runtime ID mapping, explicit prototype mass/stack/yield, defensive behavior and authored spawn | No icore/ichor conflation, generic husk duplicate or invented anatomy reward |
| 2. Build finite harvest in isolation | Session-only reference scene with carcass placeholder, item asset, catalog and existing gather/cargo flow | Focused tests prove reward exactly once, cancellation, blocked access, full/overweight/missing cargo and reentrant input |
| 3. Add living enemy and player attack | Testable health/AI state, bounded movement, one strike, production-ready action ownership and hit resolution | Focused tests prove windup/recovery, no through-wall/double hits, death precedence, no post-death attack and menu/action gating |
| 4. Add durable creature state | Versioned checkpoint extension with creature and inventory transactions | Focused save/restore, migration, interruption and duplicate-reward tests; no source-file or save loss |
| 5. Integrate one authored production encounter | One prefab instance in Greater Wasteland with reference-safe setup and unchanged terrain | Background Unity import/compile, relevant structural validator and focused tests; user owns hands-on combat/harvest acceptance |
| 6. Expand acquisition behaviors | Loose husk, one mineral node and one salvage source using the proven transaction | Every source has explicit identity, output, depletion and persistence; no broad spawn rollout |

The first isolated scene is a temporary verification surface. The final gameplay target remains Greater Wasteland, and no dormant generated prototype service is installed there. Prototype session-only behavior must be clearly indicated until durable save integration passes. No gameplay implementation, smoke test, Unity launch, package change or production-scene edit is part of this audit.

## Decisions to settle before the implementation slice

The recommendations make the next task concrete without assigning final balance. Confirm the chitin-first reward versus a separately defined generic husk fiber, direct scrape versus intact-shell collection, defensive versus immediately hostile behavior, preferred melee presentation, prototype tuning and the first encounter location. Keep numeric economy, icore biology, advanced loot, respawn, processing recipes and final art outside the first enemy unless the author explicitly expands it.

## Complete T0 acquisition register

All 103 T0 entries are listed below. Source descriptions remain workbook concepts. Proposed lanes identify implementation work; they do not approve deposits, fauna anatomy, abundance or drop tables. Stable item IDs link back to Items even if the workbook is later sorted. Row numbers refer to the audited source hash.

| Items row | Stable item ID | Material | Proposed acquisition lane |
| --- | --- | --- | --- |
| 18 | `resource.airborne_dust` | Airborne Dust | Existing deployed collector |
| 21 | `resource.ironstone_ore` | Ironstone Ore | Existing finite Ironstone gathering |
| 26 | `proposal.natural.magnetite` | Magnetite-rich stone | Authored mineral outcrop or loose stone collectible |
| 27 | `proposal.natural.silica` | Silica-rich grit | Authored mineral outcrop or loose stone collectible |
| 28 | `proposal.natural.alumina` | Alumina-rich rock | Authored mineral outcrop or loose stone collectible |
| 29 | `proposal.natural.claystone` | Claystone | Authored mineral outcrop or loose stone collectible |
| 30 | `proposal.natural.carbonate` | Carbonate stone | Authored mineral outcrop or loose stone collectible |
| 31 | `proposal.natural.salts` | Dry mineral salts | Authored mineral outcrop or loose stone collectible |
| 32 | `proposal.natural.graphite` | Graphite-bearing stone | Authored mineral outcrop or loose stone collectible |
| 33 | `proposal.natural.mica` | Mica sheets | Authored mineral outcrop or loose stone collectible |
| 40 | `proposal.bio.shell` | Shell husks / shed plates | Compatible shed/remnant collectible or finite carcass harvest |
| 41 | `proposal.bio.tendon` | Dried tendon bundles | Later species-specific carcass harvest |
| 42 | `proposal.bio.bristles` | Coarse sensory bristles | Compatible shed/remnant collectible or finite carcass harvest |
| 43 | `proposal.bio.underfur` | Shed underfur | Compatible shed/remnant collectible or finite carcass harvest |
| 44 | `proposal.bio.jaw` | Dense jaw plates | Later species-specific carcass harvest |
| 45 | `proposal.bio.membrane` | Dried joint membrane | Later species-specific carcass harvest |
| 47 | `proposal.bio.organ` | Charge-organ tissue | Later species-specific carcass harvest |
| 57 | `proposal.natural.copper_stone` | Unprocessed copper-rich stone | Authored mineral outcrop or loose stone collectible |
| 59 | `proposal.natural.raw_copper` | Raw copper lump | Specialist extraction or identified salvage; not a pure-metal ground spawn |
| 60 | `proposal.natural.tungsten_ore` | Tungsten-bearing ore | Authored mineral outcrop or loose stone collectible |
| 61 | `proposal.natural.magnesium_rock` | Magnesium-bearing rock | Authored mineral outcrop or loose stone collectible |
| 62 | `proposal.natural.raw_magnesium` | Raw magnesium metal | Specialist extraction or identified salvage; not a pure-metal ground spawn |
| 63 | `proposal.natural.hematite` | Raw hematite | Authored mineral outcrop or loose stone collectible |
| 64 | `proposal.natural.sulfur` | Raw sulfur | Authored mineral outcrop or loose stone collectible |
| 65 | `proposal.natural.borate` | Raw borate minerals | Authored mineral outcrop or loose stone collectible |
| 66 | `proposal.natural.gypsum` | Raw gypsum | Authored mineral outcrop or loose stone collectible |
| 67 | `proposal.natural.talc` | Raw talc | Authored mineral outcrop or loose stone collectible |
| 68 | `proposal.natural.manganese_ore` | Manganese-bearing ore | Authored mineral outcrop or loose stone collectible |
| 69 | `proposal.natural.lead_ore` | Lead-bearing ore | Authored mineral outcrop or loose stone collectible |
| 70 | `proposal.natural.zinc_ore` | Zinc-bearing ore | Authored mineral outcrop or loose stone collectible |
| 71 | `proposal.natural.silver_ore` | Silver-bearing ore | Authored mineral outcrop or loose stone collectible |
| 72 | `proposal.natural.gold_rock` | Gold-bearing rock | Authored mineral outcrop or loose stone collectible |
| 73 | `proposal.natural.fluorite` | Raw fluorite | Authored mineral outcrop or loose stone collectible |
| 74 | `proposal.natural.antimony_ore` | Antimony-bearing ore | Authored mineral outcrop or loose stone collectible |
| 75 | `proposal.bio.chitin_fibers` | Scraped chitin fibers | First beetle carcass scrape proposal |
| 76 | `proposal.bio.icore_fibers` | Scraped icore fibers | Later husk scraping; fiber identity unresolved |
| 79 | `proposal.bio.hide` | Raw hide | Later species-specific carcass harvest |
| 82 | `proposal.bio.chemical_material` | Faunal chemical material (unspecified) | Later species-specific contained extraction |
| 83 | `proposal.bio.liquid_material` | Faunal liquid material (unspecified) | Later species-specific contained extraction |
| 84 | `proposal.chemical.digestive_etchant` | Digestive etchant | Later species-specific contained extraction |
| 85 | `proposal.chemical.husk_enzyme` | Husk-separation enzyme | Later species-specific contained extraction |
| 86 | `proposal.chemical.shell_binder` | Shell-binding secretion | Later species-specific contained extraction |
| 87 | `proposal.chemical.clotting_compound` | Mineral clotting compound | Later species-specific contained extraction |
| 88 | `proposal.chemical.mineral_ichor` | Mineral-rich ichor | Later species-specific contained extraction |
| 89 | `proposal.chemical.joint_lubricant` | Joint-lubricating secretion | Later species-specific contained extraction |
| 90 | `proposal.chemical.insulating_secretion` | Insulating secretion | Later species-specific contained extraction |
| 91 | `proposal.chemical.conductive_gel` | Charge-conducting gel | Later species-specific contained extraction |
| 92 | `proposal.chemical.molt_release` | Molt-release compound | Later species-specific contained extraction |
| 93 | `proposal.chemical.defensive_irritant` | Defensive irritant secretion | Later species-specific contained extraction |
| 94 | `proposal.chemical.disabling_venom` | Disabling venom fraction | Later species-specific contained extraction |
| 95 | `proposal.chemical.scent_marker` | Chemical scent marker | Later species-specific contained extraction |
| 132 | `proposal.gem.clear_quartz` | Clear quartz | Authored mineral outcrop or loose stone collectible |
| 133 | `proposal.gem.smoky_quartz` | Smoky quartz | Authored mineral outcrop or loose stone collectible |
| 134 | `proposal.gem.garnet` | Garnet | Authored mineral outcrop or loose stone collectible |
| 135 | `proposal.gem.spinel` | Spinel | Authored mineral outcrop or loose stone collectible |
| 136 | `proposal.gem.peridot` | Peridot | Authored mineral outcrop or loose stone collectible |
| 137 | `proposal.gem.corundum` | Ruby / sapphire corundum | Authored mineral outcrop or loose stone collectible |
| 138 | `proposal.gem.tourmaline` | Tourmaline | Authored mineral outcrop or loose stone collectible |
| 139 | `proposal.gem.zircon` | Zircon | Authored mineral outcrop or loose stone collectible |
| 141 | `proposal.bio.inner_wing` | Delicate inner wing | Later species-specific carcass harvest |
| 211 | `proposal.fire.oxygen_stone` | Oxygen-releasing stone | Special authored stone source; detached behavior unresolved |
| 212 | `proposal.fire.glow_stone` | Glowing stone | Special authored stone source; detached behavior unresolved |
| 213 | `proposal.fire.flint` | Flint stone | Authored mineral outcrop or loose stone collectible |
| 319 | `proposal.raw.basalt` | Basalt blocks | Authored mineral outcrop or loose stone collectible |
| 320 | `proposal.raw.slate` | Slate plates | Authored mineral outcrop or loose stone collectible |
| 321 | `proposal.raw.pumice` | Pumice lumps | Authored mineral outcrop or loose stone collectible |
| 322 | `proposal.raw.feldspar` | Feldspar fragments | Authored mineral outcrop or loose stone collectible |
| 323 | `proposal.raw.spines` | Loose fauna spines | Compatible shed/remnant collectible or finite carcass harvest |
| 324 | `proposal.raw.claws` | Fauna claw tips | Later species-specific carcass harvest |
| 325 | `proposal.raw.brood_casing` | Empty brood casings | Compatible shed/remnant collectible or finite carcass harvest |
| 326 | `proposal.raw.antenna_sheath` | Empty antenna sheaths | Compatible shed/remnant collectible or finite carcass harvest |
| 327 | `proposal.raw.wing_ribs` | Insectoid wing ribs | Later species-specific carcass harvest |
| 328 | `proposal.raw.ceramic_shards` | Broken ceramic shards | Identified salvage site or wreck |
| 329 | `proposal.raw.glass_shards` | Broken glass pieces | Identified salvage site or wreck |
| 330 | `proposal.raw.spring_strips` | Salvaged spring-metal strips | Identified salvage site or wreck |
| 331 | `proposal.raw.metal_cable` | Salvaged metal cable | Identified salvage site or wreck |
| 332 | `proposal.raw.metal_mesh` | Metal mesh scrap | Identified salvage site or wreck |
| 333 | `proposal.raw.clinker` | Furnace clinker chunks | Identified salvage site or wreck |
| 334 | `proposal.raw.spent_refractory` | Spent refractory chunks | Identified salvage site or wreck |
| 335 | `proposal.raw.sandstone` | Sandstone blocks | Authored mineral outcrop or loose stone collectible |
| 336 | `proposal.raw.obsidian` | Obsidian chunks | Authored mineral outcrop or loose stone collectible |
| 337 | `proposal.raw.emery` | Emery-bearing lumps | Authored mineral outcrop or loose stone collectible |
| 338 | `proposal.raw.barite` | Dense barite lumps | Authored mineral outcrop or loose stone collectible |
| 339 | `proposal.raw.grinding_pebbles` | Rounded grinding stones | Authored mineral outcrop or loose stone collectible |
| 340 | `proposal.raw.ochre` | Yellow ochre clods | Authored mineral outcrop or loose stone collectible |
| 341 | `proposal.raw.horn_core` | Fauna horn cores | Later species-specific carcass harvest |
| 342 | `proposal.raw.tail_rings` | Fauna tail rings | Later species-specific carcass harvest |
| 343 | `proposal.raw.socket_cups` | Fauna joint-socket cups | Later species-specific carcass harvest |
| 344 | `proposal.raw.eye_lenses` | Fauna eye lenses | Later species-specific carcass harvest |
| 345 | `proposal.raw.filter_plates` | Fauna breathing-filter plates | Later species-specific carcass harvest |
| 346 | `proposal.raw.droppings` | Dried mineral-rich fauna droppings | Compatible shed/remnant collectible or finite carcass harvest |
| 347 | `proposal.raw.chain` | Rusted chain lengths | Identified salvage site or wreck |
| 348 | `proposal.raw.brass_scrap` | Salvaged brass fragments | Identified salvage site or wreck |
| 349 | `proposal.raw.bronze_scrap` | Salvaged bronze fragments | Identified salvage site or wreck |
| 350 | `proposal.raw.hinge_halves` | Discarded hinge halves | Identified salvage site or wreck |
| 351 | `proposal.raw.hoop_bands` | Salvaged metal hoop bands | Identified salvage site or wreck |
| 352 | `proposal.raw.pipe_sections` | Old metal pipe sections | Identified salvage site or wreck |
| 353 | `proposal.raw.coated_sheet` | Coated sheet-metal scrap | Identified salvage site or wreck |
| 354 | `proposal.raw.bent_fasteners` | Bent nails and rivets | Identified salvage site or wreck |
| 355 | `proposal.raw.furnace_ash` | Coarse furnace ash | Identified salvage site or wreck |
| 356 | `proposal.raw.abrasive_fragments` | Broken abrasive-wheel fragments | Identified salvage site or wreck |
| 357 | `proposal.raw.magnet_fragments` | Broken magnet fragments | Identified salvage site or wreck |
| 358 | `proposal.raw.punching_offcuts` | Metal punching offcuts | Identified salvage site or wreck |
