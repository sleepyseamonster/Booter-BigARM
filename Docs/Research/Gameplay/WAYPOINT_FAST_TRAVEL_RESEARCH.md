# Waypoint fast travel and camp hub research

Research date: October 11, 2026. Exploration for Greater Wasteland, with one starting camp at Booter's current starting area. The author has confirmed that established camps will be crafting and item-management hubs; those services will be iterated later. Travel method, costs, storage connectivity, hazard rules, and relocation remain design choices.

The closest reference is Monster Hunter Wilds: portable-looking camps occupy fixed suitable sites and support expedition preparation as well as travel. Combine that camp role with clear discovered-destination rules. Preserve the Legger's physical movement and cargo ownership. Keep travel convenience and hub functionality separate so that adding a stash or crafting station does not require redesigning the destination system.

## Comparison of six systems

The observations below describe documented mechanics, not claims that a design is universally successful. The lessons for Greater Wasteland are design inferences. Sources are publisher or developer materials except the explicitly identified Dragon's Dogma 2 reporting and the publisher-hosted player advice for Death Stranding cargo.

| Reference | Documented pattern | Useful lesson for Greater Wasteland |
| --- | --- | --- |
| Monster Hunter Wilds | A limited number of camps at fixed locations provide travel and item/equipment management. Suitable sites have risk ratings. | A camp can be portable in appearance and fixed as a destination; hub usefulness gives return travel a purpose. |
| Elden Ring | Discovered Sites of Grace appear on the map and can be selected for travel. Rest offers several preparation functions. | One recognizable anchor can support several actions without requiring a settlement or a large menu hierarchy. |
| Guild Wars 2 | Approaching a waypoint unlocks it; map travel has a fee and destinations can temporarily become unavailable. | Keep permanent knowledge separate from current travel availability, and make failure reasons visible. |
| Tomb Raider 2013 | Both camp types support upgrades, but only Fast Travel Camps connect previously visited camps. | A useful field rest stop does not have to become a fast-travel anchor. |
| Death Stranding Director's Cut | Fragile Jump connects known facilities; carried cargo and tools are left behind. | Decide explicitly whether travel serves personal repositioning or bringing an expedition's haul home. |
| Dragon's Dogma 2 | Oxcarts illustrate routes with time passage and interruption; June 2026 reporting documents a reusable teleport item. | Travel friction should earn its place, and an earlier restrictive model need not become a permanent design commitment. |

## Monster Hunter Wilds as the closest camp reference

Capcom's Gamescom description establishes a limited number of pop-up camps at fixed suitable locations, with fast travel, item/equipment management, quest starts, and return after fainting. That closely matches the desired geographic anchor and expedition hub. [Capcom on PlayStation Blog](https://blog.playstation.com/2024/08/20/monster-hunter-wilds-all-the-news-from-gamescom-opening-night-live/).

PlayStation's starter guide describes site risk ratings, monster destruction and repair, and tent customization. It also distinguishes crafting from carried materials in the field from crafting with stored resources, which requires being in a tent. This is item crafting; it does not establish a full equipment forge at every pop-up camp. [PlayStation starter guide](https://www.playstation.com/en-us/games/monster-hunter-wilds/monster-hunter-wilds-starters-guide/).

**Design inference:** borrow fixed suitable sites and the return-to-prepare rhythm. Start with a recognizable shade, marker, arrival lane, future storage area, and future work surface. Establish separate capabilities for travel, rest, storage, and crafting. Destruction, repair fees, quest services, and death return are optional mechanics and need not accompany the first camp.

The strongest storage lesson is local access. Camp materials can eventually make crafting convenient while Booter is there, without granting access to the distant Legger or another camp's inventory. Avoid implying that identical camp interfaces mean shared storage.

## Elden Ring as a compact preparation anchor

Bandai Namco's guide describes discovered Sites of Grace as map destinations, with travel restricted in some dungeons and map access unavailable during battle. The resting menu supports passing time, character preparation, flask configuration, spell selection, and chest management. Rest also restores resources and revives most defeated enemies. [Official starter guide](https://en.bandainamcoent.eu/elden-ring/news/elden-ring-starter-guide-tips-know-playing-the-game).

**Design inference:** use one memorable site and a compact list of meaningful actions. Discovery, travel, rest, and item management can share an anchor while remaining separate operations. For Booter, arrival should not automatically heal, restore supplies, reset enemies, or enter sleep. The camp's convenience comes from access to preparation, not automatic recovery.

A known camp remains known even while unavailable. Booter should not have to rediscover it after an interruption. Differentiate the site marker, travel command, and local service interaction rather than letting one press perform all three.

## Guild Wars 2 as a discovery and availability model

ArenaNet's new-player guide describes unlocking a waypoint by approaching it, selecting it through the map, a small travel fee outside cities, and temporary unavailability when enemies attack the nearby area. Support documentation explains that open-world waypoint fees depend on distance and character level. [ArenaNet new-player guide](https://www.guildwars2.com/en/new-player-guide/part-2/), [ArenaNet support](https://help.guildwars2.com/hc/en-us/articles/222384087-Missing-Gold).

**Design inference:** record discovery independently from establishment, destination clearance, and route eligibility. A disabled camp marker should explain the actual constraint. Do not introduce a per-trip coin fee merely because another game uses one: Greater Wasteland already has meaningful travel resources and cargo weight.

Departure restrictions and destination restrictions are different. “Finish your current action” describes Booter's state; “arrival lane blocked” describes the camp; “no shared route” describes actor compatibility. Keep these reasons distinct. The first camp can prove that distinction without a network of additional sites.

## Tomb Raider as a distinction between camp types

The 2013 manual distinguishes Day Camps and Fast Travel Camps, represented by different map icons. Both support survival skills and salvage-funded gear upgrades. Fast Travel Camps additionally connect previously visited campsites for revisiting explored regions. [Square Enix manual, printed page 5](https://support.eu.square-enix.com/document/manual/6501/ManualPS3.pdf).

**Design inference:** retain Booter's portable shade as a useful field camp while established sites become the travel network. A shade's local usefulness should not grant destination status, storage capacity, or a crafting station automatically. This prevents players from creating unlimited travel anchors by repeatedly deploying a reusable item.

The same distinction also helps future visual design: a small deployed shade can read as temporary recovery equipment; an established site adds a persistent marker and organized service spaces. No additional camp tier system is necessary yet.

## Death Stranding as a cargo decision

Kojima Productions describes Fragile Jump as instant travel to known centers, facilities, and safe houses within the Chiral Network, accessed from a private room after Episode 3. [Developer beginner guide](https://www.kojimaproductions.jp/en/death-stranding-directors-cut-beginners-guide).

505 Games' publisher-hosted player advice explains that the jump leaves carried tools and cargo behind while equipped items such as boots or exoskeletons stay with the player. This cargo observation is attributed player advice, rather than a developer specification. [Publisher-hosted advice](https://deathstrandingpc.505games.com/en/7-days-to-go-porters-advice-for-newcomers/).

**Design inference:** player convenience and freight transport need explicit, separate rules. For Booter, a return command that leaves his salvage behind may fail the main reason to return to camp. Conversely, moving every inventory to the destination would erase physical hauling and violate distant Legger access.

Favor preserving cargo with its actual owner during a shared physical journey. If a future Booter-only relocation option is considered, explain what remains behind before confirmation. Do not disguise that consequence behind a generic “fast travel” button. Do not turn the Legger into Death Stranding's vehicle or a mobile base by analogy.

## Dragon's Dogma 2 as route travel and changing friction

A launch-era March 2024 Gamepressure guide describes paid oxcart journeys on settlement routes, dozing to advance the journey, elapsed time, enemy interruptions, and possible cart destruction. This is a historical route-travel example, not a current price or balance specification. [Launch-era guide](https://www.gamepressure.com/dragons-dogma-2/is-there-fast-travel/za1131d).

Capcom's June 2026 Steam announcement describes adding a reusable Eternal Ferrystone to make fast travel easier. PC Gamer's June 11 report also documents the reusable item and changes to oxcart convenience. Current comparisons must therefore distinguish the launch restrictions from subsequent adjustments. [Capcom Steam announcements](https://steamcommunity.com/app/2054970/announcements/?l=english), [June update reporting](https://www.pcgamer.com/games/rpg/dragons-dogma-2-hands-out-an-infinite-fast-travel-item-because-people-wouldnt-stop-moaning-about-how-laborious-it-was-to-go-anywhere/).

The official update page returned an access error during this research. The Steam announcement was available in search content, while opening its page reached a content-preference gate. The accessible June report corroborates the reusable-item change; detailed current patch behavior is not treated as independently verified here.

**Design inference:** a journey can preserve elapsed time and interruption while shortening the player's wait. That is relevant to a later compressed expedition return. The later convenience changes are also a reason to measure repetition rather than assuming that travel friction always improves survival design. They do not prove that fast travel caused or solved a particular retention outcome.

## Recommended synthesis for Greater Wasteland

The author-selected hub role gives the camp a clear future loop: **return with salvage, unload locally, craft or prepare, choose the next expedition, depart**. Camp services should make that loop efficient. The [inventory specification](../../Design/Gameplay/INVENTORY_AND_ITEM_MANAGEMENT_SYSTEM.md) already prioritizes expedition decisions and quick packing over repeated sorting.

| Decision | Recommended direction | Status |
| --- | --- | --- |
| Starting scope | One established camp at the existing spawn area; no landscape population | User-selected scope |
| Camp purpose | Crafting and item-management hub, with rest and expedition preparation | Hub role user-confirmed; service details later |
| Destination unlock | Starting Camp known initially; future camps discovered physically | Direction established; activation details proposed |
| Portable shade | Independent recovery equipment; no automatic travel anchor | Proposal consistent with existing ownership |
| Camp inventory | Local, finite storage as a future service; no global stash assumed | Recommendation, not an approved inventory redesign |
| Crafting inputs | Booter's local kit and explicitly accessible camp storage; Legger inputs require real presence | Proposed service rule; recipes and workstations deferred |
| First travel study | A short accelerated shared journey with actual cargo and elapsed supply costs | Prototype hypothesis |
| Long-distance experience | Compare physical acceleration with compressed validated route progress | Open; no teleport or time-skip authority granted |
| Threats and recovery | Clear travel availability; arrival does not heal or reset the world | Recommendation grounded in current world rules |

## Travel models to evaluate

The earlier [camp brainstorm](../../Design/Gameplay/WAYPOINT_CAMP_BRAINSTORM.md) recommends accelerated physical return as a bounded starting study. Research supports keeping it provisional. The current 4x speed limit is an implementation fact, not a product requirement or proof that large journeys will feel fast.

| Candidate | What the player gains | What remains to resolve |
| --- | --- | --- |
| Live accelerated journey | Physical progress, cancel at current position, actual supplies and cargo | Long routes, camera comfort, interruptions, and total waiting time |
| Compressed route journey | Brief presentation of an otherwise lengthy return with elapsed expedition time | Authoritative intermediate positions, hazard resolution, simulation deltas, and restored route progress |
| Booter-only instant return | Very quick access to a discovered camp | Legger and cargo left at actual positions; survival balance and explicit arrival rules |

For compressed travel, actors must advance through an authoritative legal route. A fade is presentation; it cannot justify assigning the Legger a destination transform and calling that traversal. This would require a separate simulation design and verification slice. No researched game's companion behavior overrides the [Legger standard](../../Design/Gameplay/BIGARM_COMPANION_STANDARD.md).

Leave outbound exploration manual initially. Known camps can later be destinations from elsewhere once the selected journey model supports that scope. An initial return-only study should be labeled as such, rather than presented as a complete camp-to-camp network.

## One camp exploration sequence

Begin with Starting Camp's geographic identity, marker, arrival lane, resting spot, and reserved future storage/work areas. Record the hub role now; defer functional crafting, stash transfers, recipes, capacities, and automatic resource consumption until their own slices.

Next show one known destination and a return summary. The summary should answer where Booter is going, whether the Legger participates, how long the journey takes, what supplies it consumes, and why it cannot start. Avoid empty Craft or Storage buttons before those actions exist.

Then compare short departure-and-return journeys with a light and a heavier haul. Evaluate whether the command actually reduces repeated navigation, whether the Legger's route is understandable, and whether the player arrives ready to use the future hub. User-owned hands-on acceptance should measure time spent waiting, interruption frequency, and clarity of cargo outcomes. Do not create or run gameplay smoke tests for this research task.

Only expand to a second site after the first camp loop earns repeat use. A larger network should shorten already-understood journeys without replacing exploration of unknown terrain. Camp spacing should follow meaningful routes and expedition reach, rather than a uniform distance grid.

## World and implementation boundaries

Starting Camp remains authored on fixed, always-loaded terrain. Its stable site ID and world revision own geographic identity; visual culling must not reset discovery or inventories. Terrain generation and terrain streaming are not part of this proposal. Future dressing must respect camp footprint, approach, sleeping, and arrival exclusions.

Discovery, local camp inventory, future upgrades, and actor/journey progress are eventual persisted runtime deltas for one versioned authored-world save owner. UI preferences must not store those deltas. Current session-only camp behavior remains distinct from persistent hub state. This research changes documentation only and does not install deferred world-save services, navigation packages, or a new gameplay authority.

The next design decision is the expected return experience: how brief it must feel, whether it must bring a shared haul home, and how much danger or resource cost should remain. Crafting and item management are now established hub goals; selecting their specific interfaces or economies can wait.
