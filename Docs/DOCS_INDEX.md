# Docs Index

This file is a lightweight guide to what the major docs in this repo are for.

It exists to reduce overlap and to make it easier to tell which docs are canonical, which are standards, and which are exploratory reference.

## Canonical Docs

These should be treated as the source of truth unless they are intentionally revised.

- [WORLD_BASIS.md](./WORLD_BASIS.md)
  The setting, tone, and core world fantasy.
- [FIXED_TERRAIN_DRESSING_PLAN.md](./Design/Gameplay/FIXED_TERRAIN_DRESSING_PLAN.md)
  Current Greater Wasteland direction: persistent native terrain with procedural rocks, ground dressing and POIs; bounded contact/relief study and later integration gates.
- [Greater Wasteland world architecture baseline](./Engineering/GREATER_WASTELAND_WORLD_ARCHITECTURE_BASELINE_2026-10-11.md)
  Fixed resident terrain and procedural population baseline: performance evidence, chunk identity, logical state, local checkpoints, prior research and implementation gates. Proposed budgets and backend choices remain conditional.
- [GROUND_TEXTURE_AUTHORING.md](./Engineering/GROUND_TEXTURE_AUTHORING.md)
  Manual ground texture stickers and native terrain painting, with Undo, authoring persistence and terrain-preservation boundaries.
- [NINE_AREA_ROCK_STUDY.md](./Engineering/NINE_AREA_ROCK_STUDY.md)
  Current single-terrain rock study, minimal volcanic cube authoring, retained comparison tools and historical nine-area setup.
- [Minimal cube rock delivery](./Evidence/Art/SmoothCubeRock/README.md)
  Automatic Edit Mode fusion of duplicated textured cubes, no added authoring controls, current-machine proof and live versus saved placement boundaries.
- [SCULPTED_BASALT_01.md](./Engineering/SCULPTED_BASALT_01.md)
  Artist-sculpted rock reduction, volcanic texture bake, local Blender source and Unity prefab handoff.
- [SCULPTED_ROCK_FUSION.md](./Engineering/SCULPTED_ROCK_FUSION.md)
  Fuse overlapping sculpted rock copies into one outer mesh with sharp joins and preserved sources.
- [WORLD_CREATOR_CHARTER.md](./Design/DeferredGeneration/WORLD_CREATOR_CHARTER.md)
  Preserved infinite World Creator promise, quality bar and lore constraints. Its terrain-generation scope is deferred; current Greater Wasteland work follows the fixed-terrain dressing plan.
- [WORLD_SYSTEMS_STANDARD.md](./Engineering/WORLD_SYSTEMS_STANDARD.md)
  The procedural generation, chunking, and save/load baseline.
- [PROJECT_STRUCTURE.md](./Engineering/PROJECT_STRUCTURE.md)
  The target layout for `Assets/_Project/`.
- [LEGACY_2D_BOUNDARY.md](./Operations/LEGACY_2D_BOUNDARY.md)
  The production-versus-legacy asset, assembly, renderer, and Build Settings boundary after the accepted TopDown3D cutover.
- [UNITY_PROJECT_STANDARDS.md](./Engineering/UNITY_PROJECT_STANDARDS.md)
  Compact naming and organization standards.
- [AGENT_AND_UNITY_PRACTICES.md](./Operations/AGENT_AND_UNITY_PRACTICES.md)
  Working norms for Codex and Unity repo practice.

## Agent Operations

- [Audit of the publication closeout plan](./Operations/Repository/PUBLICATION_CLOSEOUT_PLAN_AUDIT_2026-10-12.md)
  Proposed corrections to publication dependencies, consistent recovery, asset validation, remote restore capacity and guarded rollback.

- [Project publication and storage closeout plan](./Operations/Repository/PUBLICATION_CLOSEOUT_PLAN_2026-10-12.md)
  Executable batches for independent local recovery, dirty asset decisions, verified commits, Git/LFS publication and remote restoration.

- [Verified publication and storage closeout](./Evidence/Repository/PublicationCloseout20261012/README.md)
  Published native LFS payloads, full remote identity verification, independent source recovery and the preserved unfinished authoring exception.

- [Publication and storage readiness reassessment](./Operations/Repository/PUBLICATION_READINESS_REASSESSMENT_2026-10-12.md)
  Live remote and LFS dry-run checks, pack-size estimate, local backup coverage, dirty ownership and ordered publication gates.

- [Terrain storage classification and migration plan](./Operations/Repository/TERRAIN_STORAGE_MIGRATION_PLAN_2026-10-12.md)
  Exact terrain file inventory, required Git/LFS assets, conditional local archive scope, Unity reference protection and phased restore gates.

- [Large file storage audit and recommendation](./Operations/Repository/LARGE_FILE_STORAGE_RESEARCH_2026-10-12.md)
  Measured Git, terrain evidence and local source sizes; LFS and archive alternatives, current pricing, and proposed restoration and migration gates.

- [Operations/Repository/PLAN_REVIEW_2026-10-06.md](./Operations/Repository/PLAN_REVIEW_2026-10-06.md)
  Findings and corrected execution gates for template dependencies, historical assemblies, renderer routing, source tooling, and reconstruction proof.

- [Operations/Repository/REORGANIZATION_EXECUTION_PLAN.md](./Operations/Repository/REORGANIZATION_EXECUTION_PLAN.md)
  Authorized reorganization batches, completed inactive-host segregation, current path discovery, checks, and rollback contracts.

- [Operations/Repository/LAYOUT_PLAN_2026-10-06.md](./Operations/Repository/LAYOUT_PLAN_2026-10-06.md)
  Second-pass organizational audit, selected professional layout, complete proposed move map, legacy extraction gates, and disconnected Lorekeeper status.

- [REPOSITORY_ORGANIZATION.md](./REPOSITORY_ORGANIZATION.md)
  Repository ownership, Blender source storage, transfer receipts, and safe duplicate handling.

- [Evidence/Repository/organization-audit-2026-10-06.md](./Evidence/Repository/organization-audit-2026-10-06.md)
  Complete Blender transfer accounting and repository-wide layout, reference, metadata, and duplicate audit.

- [LOCAL_WORKSPACE.md](./LOCAL_WORKSPACE.md)
  Windows checkout setup, package restoration, existing agent roles, and the October 6 resume checkpoint.

- [Agents/Gottspan/README.md](./Agents/Gottspan/README.md)
  The canonical repo-manager contract and entry point for project coordination.
- [Agents/Gottspan/instructions/MULTI_AGENT_WORKFLOW.md](./Agents/Gottspan/instructions/MULTI_AGENT_WORKFLOW.md)
  Delegation, shared-worktree ownership, handoff, and integration rules.
- [Agents/Gottspan/sops/REPO_AND_UNITY_MANAGEMENT.md](./Agents/Gottspan/sops/REPO_AND_UNITY_MANAGEMENT.md)
  Gottspan's startup, planning, change-control, Unity validation, and closeout SOP.
- [Agents/Gottspan/memory/PROJECT_MEMORY.md](./Agents/Gottspan/memory/PROJECT_MEMORY.md)
  Bounded durable project facts and known management gaps; it does not replace live inspection.
- [Agents/Lorekeeper/README.md](./Agents/Lorekeeper/README.md)
  The persistent worldbuilding, storytelling, lore, plot, and thematic specialist, including the read-only-by-default bridge to the Arc & Dust source repository.
- [Agents/Blender/README.md](./Agents/Blender/README.md)
  The persistent Blender modeling specialist and entry point for 3D asset research, authoring, and Unity handoff.

- [Agents/Sable/README.md](./Agents/Sable/README.md)
  The persistent art director's workspace, agent instructions, review SOP, briefs, references, and decision notes.

## Program Plans

- [Master item database](./Design/Items/MASTER_ITEM_DATABASE.xlsx)
  Organization audit: seventeen active primary categories cover all 827 items once; overlapping families and planning scopes are labeled separately. All 22 tables have filters and frozen headers. Master search text updates from current labels/classifications, with extra aliases in V; use relationships resolve by ID. In Items, filter Tier (C), beside Item name, or sort Tier sort order (AN) ascending for T0–T4 followed by unassigned/not-applicable records. Crafting browser independently filters Input tier (B) and Output tier (G). Existing item facts, definitions and recipes are preserved.
  All 827 entries have controlled type, stage, origin, primary-use, material, form and design-status classifications. The master table includes search terms; **Item uses** records primary/secondary use relationships linked by stable IDs, and **Taxonomy** owns dropdown vocabulary. The Guide explains filtering, sorting and new-entry upkeep.
  Crafting organization: **Crafting browser** traces inputs to possible outputs and outputs to listed requirements, with recipe, tier, type, category, status and completeness filters. **Crafting recipes** owns 655 route records; **Material routes** retains its tab name but now owns 2091 normalized input/output/prerequisite links. The browser contains 1436 linked input/output pairs. Stable item/recipe/link IDs keep labels and quantities connected when records are renamed or sorted. Unknown quantities remain blank; original Recipes stays unchanged as the trial-source record.
  Tier C is separate from crafting role AI and from rarity/quality. AJ:AM shows listed recipe coverage, including proposals and withheld outlines rather than a claim of craftability. All 47 prior tier edges and 29 resource-chain outlines are accounted for; tier gaps and withheld anatomy remain visible. Four earlier partial finished-item routes demonstrate paper, armor, ceramic and steel-tool endpoints. The October 15 pass adds proposed T3/T4 material and component branches; Tier review now accounts for the earlier unassigned stock, with unsupported biological premises kept visible. The Guide owns the schema, migration notes, highest-input-tier proposal, audit counts and browser-pair refresh instructions. Existing recipe quantities, multi-output sorting and contained-content refill distinctions are preserved. No runtime crafting or canon material tiers are installed.
  Higher-tier brainstorming: **Material branches** summarizes fifteen stock families with linked T1/T2/T3/T4 options and possible finished uses. The pass adds 34 new proposed items and reuses six existing families: 20 T2 proposals, nine T3 proposals, five T4 proposals, five T1 precursors and one hypothetical raw faunal secretion. Current tier totals are T0 130, T1 81, T2 62, T3 111 and T4 63, with two unassigned and 378 not-applicable entries. All new routes are partial proposals with blank quantities and unresolved performance. New flux, refractory feed, alloy feed, flexible binder and conditioned etchant fill precursor gaps using existing raw resources where possible. Advanced current engineering is distinct from ancient-material reproduction; no ordinary cloth, plant rubber, conventional fat or natural free water is introduced. Two additional partial finished-item examples use prepared stock for a crucible and insulating gloves. Existing item facts, source quantities and original design statuses are preserved.
  Tier-three expansion: 20 additional proposed stocks/components reuse existing T2 feed and add 20 partial refinement routes, 65 normalized links and 45 browser pairs. New candidates cover fitted bearings, gear blanks, steel tubes, grounding braid, coils, metal mesh, ceramic electrical parts, reflectors, packing, service grease, wing-panel joints, husk joints, abrasive faces, inlay stock, tuned inserts, hinged shell panels and vessel linings. Material branches and Guide identify possible T4 assemblies and missing fittings, service requirements, tools and qualification. The initial T4 planning notes are now supplemented by the candidate T4 routes below; the original five T4 records remain unchanged. No previous tier assignments, recipe quantities or original item facts are replaced.
  Tier-four expansion: 20 new proposals (18 components and two material stocks) bring T4 to 25 entries. Twenty partial routes add 88 normalized links and 68 browser pairs using existing T3 and lower-tier inputs. Candidates include drive/bearing modules, grounding and electrical interfaces, radio tuning/chassis parts, articulated guards, valve/seal carriers, sail joints/panels, wind and dirtboard hubs, vessel liners, abrasive heads, insulating board, ceremonial inlay stock and an acoustic cassette. Material branches links the new outputs; Guide explains classification and remaining design work. No T5, finished machine, complete radio, guaranteed protective rating or present-day ancient nanocarbon/plasteel manufacture is introduced. Existing facts, tiers, quantities and five earlier T4 entries remain preserved.
  Unassigned-item review: **Tier review** records all 95 previous unassigned items, with current ID-linked tiers/names, candidate ingredients, route IDs, rationale and outstanding decisions. Seventy-seven receive proposed numeric tiers (T0 1; T1 9; T2 10; T3 44; T4 13); ten ancient recoveries, five intact machine recoveries and currency move to Not applicable for the contemporary manufacturing ladder. The withheld fat-like tissue and biological wax stay Unassigned. One explicit hypothetical T1 oxide-reactive faunal fraction fills a missing conversion-chemistry premise without claiming a known reagent. Seventy-four new partial routes add 224 links and 150 browser pairs; the three original dry-sort fractions reuse their original multi-output source recipe and quantities. 8 earlier incompatible outlines are flagged Needs tier review. Browser labels now distinguish non-tiered recovered outputs from finished endpoints. Original item-design statuses and source facts remain intact; numeric tier decisions and ingredient formulations remain proposals.
  Lore-coherence pass: read-only source HEAD rechecked at `f6f98515c005dc3c73cccd9cbc031691745740c6`. Industrial materials, chemical agents, Iron Growth stages, native fauna, the domestic burden-beetle proposal, ancient synthetics and energy records inform 34 new explicitly labeled game proposals. Seven new resource/stock/recovery category labels reorganize 221 prior records; the empty Resources bucket remains a legacy label. Taxonomy adds livestock-feed and assay-sample types, animal-husbandry/material-analysis purposes and the Iron Growth family; all affected dropdowns are rebuilt. New Growth, contained Phagia-processing, biological, husbandry and iron-electrical chains add 30 partial routes; 21 extra hardware links connect rivets, fasteners and fittings to earlier proposals. All original tiers, item-design statuses, source quantities and authored trial recipes remain intact. Source records are proposals, not automatically accepted game canon: no ordinary cloth, free environmental liquid, conventional-fat anatomy, powered harvested muscle or modern ancient-synthetic reproduction is assumed. Per-item Sources and Guide preserve provenance and conflicts.
  Collection expansion: 274 new entries meet all 27 requested minimum-count groups, tracked by live formulas in **Collection targets**. Withheld biological assumptions are excluded from usable raw/processed totals. The generic Shield family, empty-battery state, Imperial Commons umbrella and packed camp do not satisfy specific-design/architecture/mark/content quotas. **Machine part** owns 50 distinct elements (35 contemporary proposals and 15 rare ancient recoveries) separately from 100 generic crafting components. **Item relationships** owns 33 compatibility/loadout records: all 20 weapons have proposed ammunition, energy supply or explicit reusable-melee treatment, and Pop-up camp has ten proposed individual shelter contents. Shelter item replaces the Shelter kit type. Four material mark currencies follow the author's latest direction, with no invented exchange ratios or grade schedule; see [material marks](./WORLD_BASIS.md#material-mark-currencies). The pass also supplies 20 specific shield designs, 20 trips/traps, 20 musical instruments, culinary Phagia stocks/drinks and diverse energy-storage forms. It adds 224 partial recipes, 606 normalized links and 382 browser pairs; Material routes reference bounds extend to row 1505. New mechanisms, edible fractions and formulations remain proposals; ancient manufacture and currency minting are not assigned as player recipes.
  Crafting/lore re-audit: **Craft audit** reviews all 827 records with ID-linked names, types, tiers and listed output-route counts, plus reviewed material coverage, facilities, readiness and open decisions. 142 earlier contemporary/unresolved endpoints lacked a selected production route; 36 support/source proposals and 174 partial routes close those structural gaps. Another 96 candidate ingredient links and selected reusable-tool/facility links improve existing mechanisms, storage, instruments and traps. All selected material paths reach cataloged T0 or recovered feeds with no missing IDs, duplicate role/input links, material cycles, numeric tier jumps, ancient manufacturing route or selected dependency on withheld fat/wax. This proves structural coverage, not qualified craftability: 78 manufactured/finished records remain research/source gated, alongside conditional raw selection and salvage assessment. Historical Needs tier review / Withheld outlines remain distinct from selected routes; listed-output counts include them. New support includes mail rings, grips, hinges, vessel blanks, trigger/switch parts, drill parts, instrument fittings, electrode stock, a contained gas source, a kiln and dedicated food tools. Atmospheric resources becomes the seventeenth primary category. Physical forms and material families are corrected across food, instruments, utensils, weapons and mechanism parts. Alarms/capture devices have distinct purposes, the magnetic catchplate is a collection device, and installed storage has explicit handling. Human-food paths use only food-designated Phagia and selected fauna/culinary-mineral sources; industrial salts/cultures/toxins are excluded from those ingredient paths. Roe gets a reproductive-source hypothesis, the ocarina gets an acoustic mouthpiece, and the lyre gets steel music wire. Camp kit assembly and recovered-turbine reassembly are explicit. No currency minting, free-energy charging, ordinary cloth/free-water processes or ancient reproduction is introduced. Original item identities/statuses/tiers, serialized facts and authored recipe quantities remain intact; quantities, heat/power, yields, fit, species edibility and operating qualification remain open. Lookup capacities now extend Material routes through row 2205 and Crafting browser through row 1505.
  Editable 827-entry design register: 18 existing item IDs and 809 design/source concepts. Includes author-directed advanced de-rusting material, plus proposed rust-conversion treatment and corrosion-inhibiting finish. Real-world function references are separated from game formulations; actual corrosion conditions and mineral/faunal inputs remain unresolved. Includes author-directed inner-city carriage and car families in a predominantly bipedal transport context; individual locomotion, propulsion and manufacturing remain open. Includes steel stock and author-directed fairly advanced contemporary steelmaking; particular grades, properties and crafting tiers remain open. Includes author-directed forging hammers and anvils, plus eight supporting smithing/workshop proposals. Tool construction, heating methods and input tiers remain open; existing crucible, kiln lining and cooking tongs are preserved. Includes ten author-directed electrical item families: three magnetic field-control devices, two static-field devices, an arc ray gun, prepared insulation stock and insulating suit/gloves/boots. Specific materials, origins, operating limits and crafting tiers remain open; existing ceramic and biological insulator formulations retain proposal status. Includes author-directed ancient laser swords, laser blasters, rockets and rocket launchers, plus a crude Iron Age reverse-engineering tradition combining robotics, rocket science and advanced engineering. Specific reproduced weapon families and manufacturing tiers remain unassigned. Includes author-directed crude two-way radios reverse engineered from machine technology; construction, range, power and crafting inputs remain undefined. Includes 40 concrete raw-feedstock proposals: ten stone/mineral, eleven fauna hard-part/waste and nineteen industrial-salvage inputs. These are explicitly brainstorming candidates, not claims retrieved from the lore repository. Includes nine processed Phagia consumables, two selected edible-fauna ingredient intermediates and three food-service carriers. Higher-performing preparations and fauna delicacies are author direction; specific recipes, effects and preservation remain open. Includes 60 proposed Legger cargo-rigging, chassis, mechanical, service, armor and rare ancient-replacement items; specific fits and ballistic properties remain brainstorming. Includes author-directed salvaged wind turbines, insect-wing sail panels, Booter collapsible handheld sail and four-wheel dirtboard, with proposed frame, wheel, steering and conversion parts. Includes eight proposed faunal lubricant intermediates/products and a staged collection, separation, stabilization, formulation and compatibility workflow; source metabolic oil is distinguished from unverified conventional fat. Includes oxygen-releasing stone, glow stone, flint, chitin-char and cooking/fire-management equipment. Source WLD-0083 atmosphere/fire constraints and unresolved extracted-stone behavior are documented in the Guide. Includes the Imperial Commons currency family from WLD-0032, with Commons/marks as aliases; denominations and physical form remain unspecified in that source proposal. Includes 28 new ceramic proposals covering feedstocks, household pottery, storage, workshop tools, thermal and electrical components, fittings, ceremonial objects and protection. Retains all 20 serialized definition variants and current recipes, with equipment, chemical, container and canister-economy detail views. Inner wings are resources for vertical sun shades; a prepared wing panel is proposed and the existing popup camp is reused. Ordinary raw shell is cheap bulk material. Fifteen canister-part/service/identity candidates derive from proposal-stage lore records, without adopting their air-reserve, maintenance or ancient-polymer requirements. Adds 24 religious, political/civic, ritual and everyday household object proposals, with simple shell/metal forms and costly ceremonial craft variants. Source institutions remain proposal-stage. Also retains everyday metal-based wear, rare fashion, gemstones, fauna and ancient materials. Records [material-culture rules](./WORLD_BASIS.md#material-culture) and [faunal biology/material-use rules](./WORLD_BASIS.md#faunal-biology-and-material-use). No new gameplay effects, recipes or trade prices are implemented.

- [Camera options audit and third person camera research](./Design/Gameplay/CAMERA_OPTIONS_AUDIT_AND_RESEARCH_2026-10-15.md)
  Existing camera, input, movement, riding and rendering contracts; primary-source research and a proposed second behind-character rig using installed Cinemachine 3.1.5. Audit and planning only.

- [Camp Loop atmosphere implementation](./Design/Gameplay/CAMP_LOOP_ATMOSPHERE_IMPLEMENTATION_PLAN_2026-10-14.md) and [evidence](./Evidence/Rendering/CampAtmosphere20261014/README.md)
  Author-selected Clearer twilight study, separate yellow sun, camera-local optics and render-only native-height surroundings. Includes ownership/clearance proof and static capture limits.

- [First Lager enemy implementation](./Design/Gameplay/CAMP_LOOP_LAGER_ENEMY_IMPLEMENTATION_PLAN_2026-10-14.md) and [evidence](./Evidence/Gameplay/CampLoopNpc20261014/README.md)
  One tunable skittish NPC in Camp Loop, with isolated perception/behavior/movement ownership, bounded safe escape routes, temporary session tuning and coordinated lab reset. Final model design and hands-on feel remain open.

- [Camp Loop inventory audit and redesign brainstorm](./Design/Gameplay/CAMP_LOOP_INVENTORY_REDESIGN_BRAINSTORM_2026-10-14.md)
  Proposed six-slot Booter equipment loadout, separate supply pockets and Legger cargo, with current inventory findings, deployed-equipment ownership and session-only trial boundaries. Audit and brainstorming only.

- [Camp Loop ghost machine salvage implementation plan](./Design/Gameplay/CAMP_LOOP_GHOST_MACHINE_SALVAGE_PLAN_2026-10-14.md)
  Implemented three-part scavenging wreck in the session-only playground, with private trial items, safe reward reset and fixed-terrain placement. [Implementation proof](./Evidence/Gameplay/CampLoopSalvage20261014/README.md) records 44 passing focused checks and an isolated blockout capture.

- [Camp Loop animation implementation status](./Design/Gameplay/CAMP_LOOP_ANIMATION_IMPLEMENTATION_STATUS_2026-10-13.md)
  Verified source/session/targeting foundations and current dependency gates for the active animation playground implementation goal.

- [Camp Loop animation plan audit](./Design/Gameplay/CAMP_LOOP_ANIMATION_PLAN_AUDIT_2026-10-13.md)
  Twenty-two integration/dependency findings, expanded download and rig accounting, corrected prerequisite order and rewritten plans; links to the foundation plan and prioritized motion-role backlog.

- [Camp Loop animation playground implementation plan](./Design/Gameplay/CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md)
  Revised prerequisite, asset transfer/private metadata restoration, player/Playables and twelve-station plans, with explicit readiness levels, requested gap closure and focused acceptance gates.

- [Camp Loop animation playground audit](./Design/Gameplay/CAMP_LOOP_ANIMATION_PLAYGROUND_AUDIT_2026-10-13.md)
  Complete Mixamo download inventory, rig and live-scene findings, unarmed/gap classifications, controller and interaction systems, station props, reset/persistence boundaries and proposed implementation gates. Inventory and proposal only; no Unity import or implementation.

- [Algae canister control brainstorm](./Design/Gameplay/ALGAE_CANISTER_BRAINSTORM.md) and [implementation plan](./Design/Gameplay/ALGAE_CANISTER_IMPLEMENTATION_PLAN.md)
  Implemented session-scoped lower-left shared algae reserve control for D-pad thirst, hunger and health restoration, with audited HUD reuse, dose accounting, input conflict handling and future persistence gates.

- [Menu system and GUI implementation plan](./Design/Gameplay/MENU_SYSTEM_IMPLEMENTATION_PLAN.md)
  Compact Start-button menu, radial setup and settings GUI, input/lifecycle integration, proposed pause/exit policies, implementation batches, and proof gates.

- [BADWATER_GAMEPLAY_TERRAIN_PLAN.md](./Design/Gameplay/BADWATER_GAMEPLAY_TERRAIN_PLAN.md)
  Audited, profile-first gameplay plan for the four-slice geographic terrain scene and its conditional collision and asset-streaming gates.
- [MICRO_DUST_HARVESTER_IMPLEMENTATION_PLAN.md](./Design/Gameplay/MICRO_DUST_HARVESTER_IMPLEMENTATION_PLAN.md)
  Candidate designs and an implementation-ready TopDown3D plan for the player-placed micro dust harvester.
- [Agents/Blender/DEATH_VALLEY_BUILD_PLAN.md](./Agents/Blender/DEATH_VALLEY_BUILD_PLAN.md)
  The proposed data-to-Blender-to-Unity plan for a bounded Death Valley-inspired landscape slice.
- [TERRAIN_CLIFF_VALLEY_DEBRIS_PLAN.md](./Design/Gameplay/TERRAIN_CLIFF_VALLEY_DEBRIS_PLAN.md)
  Proposed audit, method comparison, controlled visual experiment, and staged plan for cliffs, valleys, and source-linked debris.
- [IRONSTONE_FORMATION_MINERALIZATION_PLAN.md](./Design/Gameplay/IRONSTONE_FORMATION_MINERALIZATION_PLAN.md)
  Proposed versioned redesign from standalone Ironstone nodes to mineralized generated formations, including seam-visual experiments and save gates.
- [ROCK_QUALITY_AND_PRODUCTION_PLAN.md](./Design/Gameplay/ROCK_QUALITY_AND_PRODUCTION_PLAN.md)
  The active visual-first sequence from one user-shaped Golden Rock through the existing editor baker, catalog, and World Creator placement path.
- [WORLD_CREATOR_ARCHITECTURE_PLAN.md](./Design/DeferredGeneration/WORLD_CREATOR_ARCHITECTURE_PLAN.md)
  The approved hybrid causal world-compiler architecture, migration sequence, first vertical slice, and evidence gates subordinate to the World Creator Charter.
- [WEIGHTY_LOCOMOTION_IMPLEMENTATION_PLAN.md](./Design/Gameplay/WEIGHTY_LOCOMOTION_IMPLEMENTATION_PLAN.md)
  The implementation-ready recovery plan for Booter's grounded locomotion animation, calibrated clip authority, authored starts/stops/pivots, validation, and hands-on proof boundary.
- [TOP_DOWN_3D_LANDSCAPE_IMPLEMENTATION_PLAN.md](./Design/DeferredGeneration/TOP_DOWN_3D_LANDSCAPE_IMPLEMENTATION_PLAN.md)
  The approved production landscape objective, canonical generator architecture, implementation batches, proof boundaries, and stop conditions.
- [TOP_DOWN_3D_LANDSCAPE_BENCHMARK.md](./Design/DeferredGeneration/TOP_DOWN_3D_LANDSCAPE_BENCHMARK.md)
  The fixed-camera review, scale-band, terrain-material, rock-family, asset, and performance contract for the representative landscape slice.
- [TOP_DOWN_3D_FOUNDATION_PLAN.md](./Design/DeferredGeneration/TOP_DOWN_3D_FOUNDATION_PLAN.md)
  The accepted perspective top-down 3D foundation scope, architecture, and historical proof requirements; landscape work now follows the dedicated production landscape plan.
- [3D_CONVERSION_AUDIT_AND_CHECKLIST.md](Archive/IsometricConversion/3D_CONVERSION_AUDIT_AND_CHECKLIST.md)
  The original conversion audit, ownership contract, migration matrix, work breakdown, decision gates, asset requirements, and risks. Its orthographic/isometric direction is superseded for new work by the active perspective foundation plan.
- [3D_CONVERSION_START_READINESS.md](Archive/IsometricConversion/3D_CONVERSION_START_READINESS.md)
  The live Level A and Level B inventory for closing the protected-spike blockers, accepting CP-06, preparing CP-07, and later authorizing production 3D asset work.
- [ISOMETRIC_DIRECTION_BRIEF.md](Archive/IsometricConversion/ISOMETRIC_DIRECTION_BRIEF.md)
  The historical working contract for the completed protected isometric conversion spike; it remains evidence rather than current production direction.
- [GROUNDED_GEOLOGY_IMPLEMENTATION_PLAN.md](./Design/DeferredGeneration/GROUNDED_GEOLOGY_IMPLEMENTATION_PLAN.md)
  Superseded notice for the retired runtime Grounded Geology and separate Integration Workbench proposal.

## Implementation Standards And Seams

These define current preferred approaches or sequencing.

- [Menu system direction](./Design/Gameplay/MENU_SYSTEM_DIRECTION.md)
  Accepted simple supporting-menu scope, optional immersive presentation, and low management burden.

- [MICRO_DUST_HARVESTER.md](./Design/Gameplay/MICRO_DUST_HARVESTER.md)
  The player-stated canister placement, airborne dust collection, fill, pickup, presentation, and procedural persistence contract; tuning and inventory representation remain open.
- [BIGARM_COMPANION_STANDARD.md](./Design/Gameplay/BIGARM_COMPANION_STANDARD.md)
  The canonical product and implementation rules for BigARM's companion role, physical follow behavior, and future unloaded-world traversal seam.
- [GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md](./Design/DeferredGeneration/GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md)
  The deterministic TopDown3D natural-object placement, cost-layer, rendering, art, performance, and proof contract.
- [IRONSTONE_MINING_AND_INVENTORY_SYSTEM.md](./Design/Gameplay/IRONSTONE_MINING_AND_INVENTORY_SYSTEM.md)
  The implemented deterministic Ironstone node, gathering transaction, inventory, UI, and snapshot boundary.
- [IMPLEMENTATION_SEQUENCE.md](./Operations/IMPLEMENTATION_SEQUENCE.md)
- [GAMEPLAY_ARCHITECTURE_BASELINES.md](./Engineering/GAMEPLAY_ARCHITECTURE_BASELINES.md)
- [INPUT_ARCHITECTURE_STANDARD.md](./Engineering/INPUT_ARCHITECTURE_STANDARD.md)
- [MOVEMENT_CAMERA_STANDARD.md](./Engineering/MOVEMENT_CAMERA_STANDARD.md)
- [CLIMBING_TRAVERSAL_PLAN.md](./Design/Gameplay/CLIMBING_TRAVERSAL_PLAN.md)
  The proposal for steep incline, scramble, wall, and overhang traversal and animation.
- [SMART_TRAVERSAL_STANDARD.md](./Design/Gameplay/SMART_TRAVERSAL_STANDARD.md)
  The contextual sprint traversal contract, first spin/vault move set, safety boundaries, and extension seam.
- [SURVIVAL_SYSTEM_STANDARD.md](./Design/Gameplay/SURVIVAL_SYSTEM_STANDARD.md)
  The five-vital player-state, depletion, persistence, procedural-world, and compact HUD contract.
- [CAMP_CLOAK_AND_SLEEP_BRAINSTORM.md](./Design/Gameplay/CAMP_CLOAK_AND_SLEEP_BRAINSTORM.md)
  The provisional vertical camp shade, cloak mesh brief, Reserve recovery, and accelerated live-world sleep proposal.
- [CAMP_CLOAK_AND_SLEEP_IMPLEMENTATION_PLAN.md](./Design/Gameplay/CAMP_CLOAK_AND_SLEEP_IMPLEMENTATION_PLAN.md)
  The dependency-ordered mesh, camp placement, controller, recovery, and live sleep prototype plan, with validation gates and a separate persistence milestone.
- [Camp sleep implementation evidence](./Evidence/Gameplay/CAMP_SLEEP_IMPLEMENTATION_2026-10-10.md)
  Session-only camp and cloak integration, controls, 101 focused checks, live simulation capture, production validators, and acceptance limits.
- [WAYPOINT_CAMP_BRAINSTORM.md](./Design/Gameplay/WAYPOINT_CAMP_BRAINSTORM.md)
  Starting waypoint camp exploration, discovered destinations, fast-travel options, Legger constraints, and a bounded implementation sequence.
- [Starting Camp implementation evidence](./Evidence/Gameplay/STARTING_CAMP_IMPLEMENTATION_2026-10-11.md)
  Established recovery, independent portable kit ownership, Camps view, bounded physical return, 114 focused checks, and session-only limitations.
- [Camp interaction audit and iteration](./Evidence/Gameplay/CAMP_INTERACTION_AUDIT_2026-10-11.md)
  Nearby camp shortcuts, regroup without reopening menus, route guidance, clearer companion feedback, and 123 focused checks.
- [Camp compass audit and proposal](./Design/Gameplay/CAMP_COMPASS_BRAINSTORM.md)
  Facing-relative algae HUD ring, darkward/lightward directions, local camp origin, layout gaps, and a bounded future implementation slice.
- [Simple camp compass implementation](./Evidence/Gameplay/CAMP_COMPASS_IMPLEMENTATION_2026-10-12.md)
  Facing compass and arrival indicator around algae controls, coordinated layout, focused tests, and static UI proof boundaries.
- [URP_2D_STANDARD.md](Archive/Legacy2D/URP_2D_STANDARD.md) — legacy 2D maintenance only.
- [UNITY_AUTOMATION.md](./Engineering/UNITY_AUTOMATION.md)
- [CODEX_EDITOR_STANDARD.md](./Operations/CODEX_EDITOR_STANDARD.md)

## Project Snapshot Docs

- [Compact menu implementation evidence](./Evidence/UI/MENU_SYSTEM_IMPLEMENTATION_2026-10-07.md)
  Start-button menu and GUI, live-world behavior, radial/inventory integration, focused Windows proof, and user acceptance limits.

These describe current repo state rather than durable design truth.

- [Evidence/EDITOR_PLAYABILITY_AUDIT_2026-10-06.md](./Evidence/EDITOR_PLAYABILITY_AUDIT_2026-10-06.md)
  Windows Editor scene completeness, runtime control checks, missing Death Valley ground markers, test gaps, and the next repair sequence.
- [Evidence/Badwater/terrain-seam-repair-2026-10-06.md](./Evidence/Badwater/terrain-seam-repair-2026-10-06.md)
  Death Valley compatible render grids, exact shared edges, persistent LOD stitching, source preservation, and passing Editor regression checks.

- [PROJECT_BASELINE.md](./PROJECT_BASELINE.md)
- [PROJECT_STATUS.md](./PROJECT_STATUS.md)
- [ROADMAP.md](./ROADMAP.md)
- [RESEARCH_PLAN.md](./Operations/RESEARCH_PLAN.md)
- [ConversionEvidence/LEGACY_BASELINE_2026-08-12.md](Archive/IsometricConversion/ConversionEvidence/LEGACY_BASELINE_2026-08-12.md)
  The immutable CP-02 scene, renderer, hierarchy, Build Settings, Git-blob, and SHA-256 preservation anchor for the protected 2D path.
- [ConversionEvidence/PROTECTED_SPIKE_REPORT_2026-08-12.md](Archive/IsometricConversion/ConversionEvidence/PROTECTED_SPIKE_REPORT_2026-08-12.md)
  CP-01 through CP-05 implementation evidence, screenshots, corrections, validation results, limitations, and the CP-06 decision boundary.
- [ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md](Archive/IsometricConversion/ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md)
  Current perspective foundation implementation, automated proof, protected-baseline evidence, deferred scope, and user-owned acceptance boundary.

## Design Evidence And Decisions

- [Menu implementation plan audit](./Evidence/UI/MENU_PLAN_AUDIT_2026-10-07.md)
  Corrected input arbitration, return/pause ownership, inventory suspension, GUI installation/layout, settings persistence, and proof boundaries.

- [DECISION_LOG.md](./DECISION_LOG.md)
  Pointers and rationale for consequential design, architecture, and workflow decisions; controlling docs still hold current truth.
- [PLAYTEST_LOG.md](./Evidence/Playtests/PLAYTEST_LOG.md)
  Repeatable playtest observations tied to a build or commit and a specific design question.

## Provisional Reference Docs

These are intentionally non-canonical. They preserve ideas, research, or working context that may later be refined, replaced, or discarded.

- [ARC Raiders item management and crafting audit](./Research/Gameplay/ARC_RAIDERS_ITEM_MANAGEMENT_AND_CRAFTING_AUDIT_2026-10-15.md)
  October 15 public-source audit covering inventory, loss protection, stash and furniture storage, crafting gates, refining, blueprints, research, amplification, traders and recovery; includes dated evidence conflicts and current-client verification limits.

- [Sable's stylized realism research](./Agents/Sable/work/2026-10-15-stylized-realism/README.md)
  Arcane and No Rest for the Wicked as primary influences, Wayfinder and Ori supporting references, comparable styles, a resource catalogue, and a proposed comparison study. The author's broad preference guides research; specific treatments remain proposals.

- [Waypoint fast travel and camp hub research](./Research/Gameplay/WAYPOINT_FAST_TRAVEL_RESEARCH.md)
  Six game comparisons covering camp services, discovery, travel friction, cargo, and implications for the single Greater Wasteland starting camp.

- [Minimal cube rock fusion research](./Research/Art/SMOOTH_CUBE_ROCK_FUSION_RESEARCH.md)
  Repository audit and primary-source research for duplicating and moving rock-textured cubes that blend automatically, with no workbench or exposed complexity controls.

- [Minimal cube rock implementation plan](./Design/Gameplay/MINIMAL_CUBE_ROCK_IMPLEMENTATION_PLAN.md) and [verification plan](./Design/Gameplay/MINIMAL_CUBE_ROCK_VERIFICATION_PLAN.md)
  Gated minimal Edit Mode cube interaction, bounded source changes and verification contract; actual implementation and proof are recorded in the delivery above.
  The [plan audit](./Design/Gameplay/MINIMAL_CUBE_ROCK_PLAN_AUDIT.md) preserves the pre-implementation sampling, order-independence, responsiveness and clone/save findings.

- [Monster Hunter Wilds menu research and Arc and Dust audit](./Research/UI/MONSTER_HUNTER_WILDS_MENU_AUDIT.md)
  Official reference analysis, current production UI source audit, and proposed menu presentation, navigation, accessibility, and implementation sequence.

- [Immersive menu systems research for Arc and Dust](./Research/UI/IMMERSIVE_MENU_SYSTEMS_RESEARCH.md)
  Metro Exodus and Fallout physical-interface research, effectiveness evidence, and candidate presentation approaches for the elevated top-down game.

- [Engine foundation research](../Archive/ProprietaryEngine/Docs/PROPRIETARY_ENGINE_FOUNDATION_RESEARCH.md)
  Research for the user-directed proprietary engine, standard third-person view and open Greater Wasteland rock workbench; proposed language, libraries, Windows-first platform policy and foundation evidence gates. No engine implementation is claimed.
- [WORLD_GEN_REFERENCE_NOTES.md](./Design/DeferredGeneration/WORLD_GEN_REFERENCE_NOTES.md)
  Distilled world-generation ideas from exploratory discussion.
- [UNITY_TILEMAP_PROCGEN_REFERENCE.md](Archive/Legacy2D/UNITY_TILEMAP_PROCGEN_REFERENCE.md)
  Unity-facing implementation notes from exploratory discussion.
- [WORLD_GEN_RESEARCH_SUMMARY.md](./Design/DeferredGeneration/WORLD_GEN_RESEARCH_SUMMARY.md)
  External research findings from Unity docs, developer practice, and similar projects.

## Working Rule

When adding a new doc, decide which bucket it belongs in:

- canonical
- implementation standard
- project snapshot
- provisional reference

If a provisional reference becomes stable enough to drive work repeatedly, move its useful parts into the appropriate canonical or implementation doc instead of letting both drift indefinitely.

- [Verified organization implementation and current paths](./Operations/Repository/IMPLEMENTATION_EVIDENCE_2026-10-06.md).
