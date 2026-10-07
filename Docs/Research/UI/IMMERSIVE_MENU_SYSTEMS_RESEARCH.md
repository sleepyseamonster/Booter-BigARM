# Immersive menu systems research for Arc and Dust

Research date: 2026-10-07. Status: provisional research and candidate direction. The user favors Monster Hunter Wilds menus and the immersive physical interfaces of Metro Exodus and Fallout. This preference authorizes research, not a new canonical device, camera mode, crafting system, or pause policy.

Arc & Dust can combine Metro's physical task ownership, Fallout's recognizable personal interface, and Wilds' clear information hierarchy. The strongest candidate is a readable interface presented through Booter's equipment, with cargo management anchored to the Legger's actual loadframe. The player should feel that Booter is inspecting his tools and supplies while routine operations remain quick.

This extends the [Wilds menu audit](./MONSTER_HUNTER_WILDS_MENU_AUDIT.md). Its shared navigation, input ownership, focus restoration, and accessibility recommendations still apply.

## What makes a menu immersive

A diegetic interface exists in the fiction and can be perceived by the character: a map, instrument, notebook, or device screen. A world-positioned marker is spatial but is not necessarily something Booter can see. A screen-space panel with industrial decoration can feel thematically consistent without being a literal world object.

These distinctions matter because a convincing physical interface needs a reason to contain its information. A personal instrument might report vitals or recorded observations; it does not inherently know every hidden resource, unexplored route, or distant cargo change. Information access, physical storage access, presentation, and simulation time are separate design choices.

## Metro Exodus

Deep Silver's Huw Beynon described minimal UI and communication through physical objects in a [2018 publisher interview](https://mcvuk.com/development-news/metro-exodus-its-huge-in-scope-and-scale-but-its-not-an-open-world-game/). This is design intent, not proof that every release configuration has zero HUD.

In [another publisher interview](https://gamingbolt.com/metro-exodus-interview-level-design-weapon-customization-horror-elements-and-more), Beynon describes a physical map that can be flipped to read the current objective, with discovered points of interest recorded through observation. Map consultation was described as occurring without pausing. The lesson is that information gathering and route planning can become actions inside the journey.

A [firsthand GameSpot preview](https://www.gamespot.com/articles/metro-exodus-makes-strides-into-a-more-challenging/1100-6459755/) describes deploying the backpack to manage equipment, craft, and change attachments, with map/backpack use exposing the player to nearby danger. These observations concern the 2018 demonstration, not an exhaustive current-release controls audit.

**Transferable strengths:** equipment explains available actions; distinct tools keep tasks coherent; physical handling gives preparation weight; leaving the world audible and partially visible can preserve a sense of place.

**Design risks inferred for Arc & Dust:** opening rituals become repetitive; tool handling can obscure threats; low light, perspective, and small surfaces can impair reading; multiple separate objects can make information hard to find. The elevated production camera cannot show a wrist gauge at the readability available in Metro's first-person view. Metro's crafting or maintenance rules are not authority to add those mechanics here.

## Fallout and the Pip Boy

The [official Fallout New Vegas manual](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/22380/manuals/fnv_gfw_manual-10-steam.pdf?t=1765992876), printed pages 6–11, describes a Pip-Boy organized into Stats, Items, and Data, with submenus for character state, carried equipment, maps, notes, quests, and radio. It documents keyboard/mouse list navigation and hotkeys for frequently used equipment. One recognizable device provides a stable place to return to; shortcuts complement detailed management.

Fallout entries should not be treated as identical. [Bethesda support](https://help.bethesda.net/app/answers/detail/a_id/43397/~/why-am-i-being-attacked-while-accessing-my-pip-boy-2000-in-fallout-76%3F) explicitly states that Fallout 76's Pip-Boy does not pause the game and provides a View option for checking surroundings. The presence of a physical device does not decide simulation policy.

Bethesda's [2021 inventory update](https://fallout.bethesda.net/en-US/news/fallout-76-inventory-update-notes-january-26-2021) added inventory categories and both individual and stack weight. Its [2025 accessibility guide](https://fallout.bethesda.net/en-US/news/fallout-76-gameplay-and-accessibility-settings-guide) describes broader digital/analog navigation support and customizable Pip-Boy/Quick-boy colors. A strong fictional identity still benefits from ordinary improvements in sorting, quantities, controls, and readability.

**Transferable strengths:** one memorable interface object; consistent categories; recognizable controls and sound; stable information locations; direct shortcuts outside the full device interface.

**Design risks inferred for Arc & Dust:** a narrow display can encourage long lists and repeated tab changes; a large housing consumes reading space; simulated glow, scanlines, glare, and sway can weaken legibility. Copying green CRT styling or a wrist computer would import Fallout's visual identity without establishing why that technology belongs to Booter. Device form and technology remain proposals.

## Effectiveness evidence

[Iacovides et al.](https://eprints.whiterose.ac.uk/id/eprint/130581/) compared first-person shooter interfaces and found that removing non-diegetic elements could influence immersion in expert players through cognitive involvement and control. Their findings emphasize player expertise; they do not establish that removing HUD information helps every player.

[Peacocke et al.](https://research.monash.edu/en/publications/an-empirical-comparison-of-first-person-shooter-information-displ/) studied ammunition, health, weapon choice, and navigation. No display class performed best for all tasks: health monitoring favored a HUD, ammunition favored diegetic/spatial displays, and weapon display benefited from redundancy. These FPS results justify task-specific evaluation, not direct predictions about Arc & Dust's top-down survival gameplay.

For this game, immersion should be assessed alongside task success. A player who fails to notice a critical reserve warning because it exists only on a tiny prop has not received an effective interface. Minimal presentation can coexist with redundant critical cues and an accessible expanded view.

## Comparison and proposed synthesis

| Reference | Organizing principle | Contribution to Arc & Dust | Adaptation needed |
| --- | --- | --- | --- |
| Wilds | Clear browsing and selected detail within a larger menu family. | Lists/grids, visible focus, consequences, quick actions. | Reduce density and preserve the quieter survival tone. |
| Metro Exodus | A physical tool for a particular task. | Personal kit, route ledger, cargo inspection that feels situated. | Keep navigation coherent across tools and compensate for the elevated camera. |
| Fallout | A recognizable device containing many functions. | A consistent personal information hub with stable categories. | Avoid giving one device unexplained world knowledge or remote storage power. |

Recommended direction: **one coherent menu family with several believable physical owners**. Share controls and information hierarchy across those surfaces; do not make the player learn an unrelated interface for each prop.

| Function | Candidate physical owner | Proposed presentation | Boundary |
| --- | --- | --- | --- |
| Personal status | Booter's field instrument or kit-mounted gauge. | Compact critical cues in play; detailed readable inspection when opened. | Reflect existing five-vital state; invent no sensors or new survival mechanics. |
| Carried supplies | Booter's field kit. | Recognizable compartments or labeled mounts with selection and detail. | Preserve current slots, stack rules, and transactional services; avoid freeform packing chores. |
| Cargo | Legger's physical loadframe. | Mount diagram paired with Booter's kit when reachable. | Transfer and repack validate real range; callover remains physical traversal. |
| Map and notes | Folded route ledger or field instrument. | Survey-like markings and observations within the shared menu shell. | Requires approved map/knowledge services; no automatic revelation of unexplored terrain. |
| Frequent actions | Existing Compass Ring. | Immediate selection with binding prompts and availability reasons. | Avoid an instrument-opening ritual for every quick command. |
| Settings and accessibility | Player-facing system menu. | Same typography and focus language with direct settings access. | Available before play; never require a fictional tool, resource, or safe location. |

These are candidate owners, not approved items or lore. The [world basis](../../WORLD_BASIS.md) and [inventory specification](../../Design/Gameplay/INVENTORY_AND_ITEM_MANAGEMENT_SYSTEM.md) control the Legger's role and the game's tone. He remains a finite carrier and companion, never a crafting platform or mobile base.

## Three presentation options to compare

| Option | Experience | Cost and risk |
| --- | --- | --- |
| Personal instrument | Booter opens one rugged device with named categories. | Strong identity and consistency; device technology and housing can dominate the design. |
| Physical kit and ledger | Inventory is a kit, notes/map a ledger, cargo the loadframe. | Strong material ownership; switching objects and art/animation needs may become cumbersome. |
| Physical opening with readable inspection | A brief handling cue leads to a stable enlarged tool or kit view, with a clear reading plane. | Best initial candidate for the current camera; needs a deliberate transition and must avoid looking like a disconnected desktop window. |

The third option can retain uGUI. Physical framing can be art around the same screen model; literal in-world rendering or a render texture is an optional later presentation adapter. All variants should share domain commands, input contexts, preference state, focus, and validation. This keeps a later 3D presentation from becoming a gameplay rewrite.

## Interaction and time policy

Separate the **menu opening cue** from the **operation outcome**. Accept navigation as soon as the screen is usable, allow immediate cancellation, and offer reduced-motion or shortened transitions. Repeated inspection must not play a long animation. Decorative knobs need not require analog rotation; controller buttons and keyboard navigation can operate them consistently.

Time policy remains open. Live field inspection would make shelter and preparation meaningful but could punish reading speed and accessibility needs. Paused inspection offers time to understand details; the physical prop can still support immersion. A distinct system pause can remain available even if field interfaces are live. Do not introduce depletion, companion movement, or combat changes merely to imitate a reference game.

If live inspection is approved, cancellation and threat cues must remain reliable. Returning to play should not fire a held menu action as a gameplay action. If paused inspection is approved, centralize simulation pause and restore it correctly through nested dialogs; do not let individual screens overwrite `Time.timeScale` independently. Both policies need validation of the existing mouse/stick-neutral return behavior.

## Current architecture and world boundaries

Source inspection confirms existing inventory range checks and input modes. The current Inventory screen owns open/close directly and uses overlay uGUI. The [survival standard](../../Design/Gameplay/SURVIVAL_SYSTEM_STANDARD.md) specifies Health, Hunger, Thirst, Oxygen, and Reserve, with several consequences/replenishment systems deferred. A physical instrument should present that actual state without implying those deferred systems work.

- **Deterministic identity:** presentation does not change world seed or identity; existing domain IDs remain authoritative.
- **Streaming and unload/reload:** Greater Wasteland remains fixed and always loaded. Future inspection must cancel or invalidate missing targets and clear subscriptions safely; no streaming implementation is proposed.
- **Generated-object identity:** no generated menu props are needed. Any later persisted item/device needs a deliberate stable-ID contract, not a scene instance ID.
- **Authored constraints:** range, placement, capacity, and companion traversal rules remain enforced by services. The menu camera must never move Booter or the Legger to stage a shot.
- **Persisted deltas:** gameplay mutations belong to existing services. Last tab, reduced motion, scale, and presentation preference belong to UI settings, separate from inventory/world snapshots. A decorative device battery or damage mechanic is not authorized.

## Next research and review gate

Compare the three presentation options using the same existing Field Kit contents, readable cargo availability, and five-vital information. Include an elevated-camera view, enlarged inspection view, large-text state, and controller focus. Judge identity, reading space, navigation steps, information authority, and repeated-use friction before commissioning a device model or camera animation.

For a later authorized prototype, compare representative tasks: inspect a vital; locate an item; transfer a stack to reachable cargo; recognize unreachable cargo; cancel without a command; reopen at remembered selection; change device; read enlarged text. Record completion time, wrong actions, missed warnings, and subjective immersion separately. Hands-on gameplay smoke testing remains user-owned.

This continuation is documentation research only. It does not claim a current-release hands-on audit of the reference games, Unity visual validation, or a passing runtime test run.
