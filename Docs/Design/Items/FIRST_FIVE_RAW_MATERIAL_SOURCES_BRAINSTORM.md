# First five raw material sources brainstorm

October 14, 2026. Status: proposals for author review, not accepted species, deposit placement or implemented content.

Keep the husk-fiber insect as source one. Add four sources that make gathering feel different while using simple, finite interactions. Prefer a recognizable object and one raw reward per source before adding tool grades, random loot, regeneration or crafting chains.

This brainstorm builds on the [item source audit](ITEM_SOURCE_AUDIT_AND_HUSK_BEETLE_PLAN.md) and follows the [world basis](../../WORLD_BASIS.md). The four additional outputs already have T0 records in the item database. Candidate source forms and their selection below are new proposals. Names are working descriptions.

## Source one: husk-fiber insect

Retain the proposed husk beetle: a small defensive insectoid that Booter can attack, kill and scrape for chitin fibers. Its recognizable husk and exposed fiber layer teach the connection between fauna and materials. Start with `proposal.bio.chitin_fibers`, not an undefined combined chitin/icore item. Death leaves a finite harvestable carcass rather than automatically adding a reward.

The [beetle plan](ITEM_SOURCE_AUDIT_AND_HUSK_BEETLE_PLAN.md#proposed-husk-beetle-experience) owns its detailed behavior and harvest constraints.

## Source two: Ironstone Ore

Existing material: `resource.ironstone_ore`, audited Items row 21. This provides the first mining source and an existing material connection to dry sorting and the trial battery recipe. Those trial uses do not automatically become production recipes.

| Candidate | Appearance and interaction | Tradeoff |
| --- | --- | --- |
| Exposed ironstone outcrop | A low rust-red rock with dark broken faces; chip ore from an exposed face | Clear mining target and closest fit to the existing Ironstone implementation |
| Loose ironstone scree pocket | Several dark, heavy-looking fragments collected from the foot of a rock face | Easy collectible, but less distinct from ordinary stone dressing |
| Ironstone vein in a canyon wall | A visible mineral band; work a reachable section | Strong geological landmark, but requires careful wall placement and interaction alignment |

**Pick: exposed ironstone outcrop.** It gives the player a large, stationary target with a clear active and depleted appearance. Author a small gathering face and a finite ore allowance; retain the underlying rock after depletion. Chip the source object without deforming native terrain. This is the easiest candidate to build from existing code.

## Source three: dry mineral salts

Existing concept: `proposal.natural.salts`, audited Items row 31. The workbook proposes eventual algae-culture mineral uses, subject to biology design. These are unidentified mineral salts, not automatically edible table salt.

| Candidate | Appearance and interaction | Tradeoff |
| --- | --- | --- |
| Ancient salt crust | A pale, cracked patch over old exposed deposits; scrape off a finite crystal crust | Strong contrast against rusty ground and a simple surface interaction |
| Crystal seam under an overhang | Pale crystals in a sheltered rock recess; break off reachable clusters | Attractive discovery, but more placement and visibility work |
| Loose salt nodules | Chalky mineral lumps scattered beside an ancient evaporite exposure; gather individually | Very easy pickup, but can resemble generic loose stones |

**Pick: ancient salt crust.** A thin authored mesh on supported ground can make a resource patch immediately recognizable. Scraping reveals a spent surface underneath. Its origin is an ancient deposit; no contemporary brine, evaporation simulation or natural water cycle is needed. Crystal detail can stay decorative, with one interaction target for the patch.

## Source four: coarse sensory bristles

Existing concept: `proposal.bio.bristles`, audited Items row 42. Proposed material uses include brushes, dust seals and filter research. Collection grants raw bristles; it does not grant a finished filter or establish filtration performance.

| Candidate | Appearance and interaction | Tradeoff |
| --- | --- | --- |
| Brush-backed crawler | A squat insectoid with conspicuous coarse bristles; harvest its carcass | A second recognizable creature, but duplicates much of the first enemy workload |
| Abandoned bristle resting hollow | A shallow sheltered nook containing coarse shed bristles; gather the loose clumps | Adds animal presence and a quiet scavenging activity without another AI system |
| Discarded bristled molt | An empty segmented outer casing with attached bristles; strip them off | Strong material connection, but visually overlaps the husk beetle and shell collectibles |

**Pick: abandoned bristle resting hollow.** Place a few stiff, curved clumps beside rocks in an existing shallow shelter. Give the hollow one finite harvest target so the decoration does not become many tiny pickups. No creature needs to appear or be simulated. The source species and shedding behavior remain proposed biology. Do not excavate terrain, add plant bedding or imply an unlimited renewable supply.

## Source five: salvaged metal cable

Existing concept: `proposal.raw.metal_cable`, audited Items row 331. This is recovered stranded metal for candidate binding and construction uses. Keep it separate from the existing electrical Wire Bundle. Usable load and condition remain undefined.

| Candidate | Appearance and interaction | Tradeoff |
| --- | --- | --- |
| Dead winch assembly | A broken drum with a visible cable coil; detach a recoverable length | Clear relationship between machine and material, with one compact prop |
| Exposed ruined conduit | A short broken utility housing containing visible cable; pull out a length | Fits ruins, but risks confusion with electrical wire and needs attachment context |
| Collapsed lifting frame | A fallen frame with hanging cable; cut free a section | Strong silhouette, but a larger prop and more terrain-fitting work |

**Pick: dead winch assembly.** The visible coil communicates the reward before the player interacts. A finite dismantle action changes the drum from cable-bearing to stripped. Start with a static mesh and simple spent variant; no working winch, physics rope or cable-cutting simulation. Do not infer a powered machine or reliable load-bearing cable from its appearance.

## Selected set and first build order

| Source | Raw output | Main player activity | First presentation |
| --- | --- | --- | --- |
| Husk beetle | Scraped chitin fibers | Fight, then harvest | Living enemy becomes a scrapeable carcass |
| Exposed ironstone outcrop | Ironstone Ore | Mine | Worked face on a persistent rock |
| Ancient salt crust | Dry mineral salts | Scrape | Pale resource patch becomes a spent surface |
| Abandoned bristle resting hollow | Coarse sensory bristles | Find and scavenge | Bristle clumps are removed from a sheltered nook |
| Dead winch assembly | Salvaged metal cable | Dismantle | Visible coil becomes a stripped drum |

Start by proving the common finite gather/cargo transaction with Ironstone and a beetle carcass placeholder, then complete the requested living beetle. Add the three static sources afterward. This retains the beetle as the first enemy without making every raw material depend on combat.

For any implementation, each authored source needs a stable ID owned by the fixed Greater Wasteland world, a finite unclaimed reward and persisted depletion. Terrain remains unchanged and always loaded. Presentation disable/re-enable must not regenerate rewards. Procedural dressing-cell placement, if later authorized for these sources, needs deterministic candidate identity and authored exclusions independently of terrain. Beetle movement retains its spawn identity. Inventory reward and source depletion must share the checkpoint transaction, with nearby finite Legger cargo and no lost yield when transfer fails. Numeric yields, mass, stacks, tool requirements, respawn and processing recipes remain later tuning decisions.

No workbook, game asset, package or runtime behavior is changed by this brainstorm.
