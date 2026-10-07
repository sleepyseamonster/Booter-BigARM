# Menu system direction

Status: accepted product direction from the user on 2026-10-07. Implementation and visual details remain open.

Menus are a quiet supporting control layer for Arc & Dust. They let the player configure radial shortcuts, change settings, and leave the game, then return promptly to play. Immersion should come from the world, actions, sound, and restrained presentation. Menu complexity and repeated management are not part of the intended core experience.

## Product priorities

- Keep the system menu short, with clear access to Resume, Radial Setup, Settings, and Exit as the supporting services become available.
- Provide useful radial defaults so configuration is optional. The wheel is for fast actions; the setup screen is occasional maintenance.
- Keep existing inventory and cargo tasks efficient, consistent with the [inventory specification](./INVENTORY_AND_ITEM_MANAGEMENT_SYSTEM.md). The player makes expedition decisions; routine sorting should require little attention.
- Use consistent controls, visible focus, readable prompts, and predictable back behavior. Return to the previous screen and its selection.
- Add categories, tabs, or screens only when an approved gameplay need cannot be served clearly by the existing structure.
- Keep settings and accessibility directly available. Their use never depends on owning or operating an in-world device.

## Reference use

Monster Hunter Wilds informs visual hierarchy and quick-action clarity. Metro Exodus and Fallout inform material identity and the sense that interfaces belong to the world. Their menu breadth, device interactions, crafting, and management systems are not requirements here.

A physical object, instrument frame, or handling cue is optional presentation. It must earn its place by improving immersion without adding navigation, upkeep, mandatory configuration, or repeated delays. No wrist computer, separate ledger/kit menu family, or inspection camera is selected.

The [Wilds audit](../../Research/UI/MONSTER_HUNTER_WILDS_MENU_AUDIT.md) and [immersive menu research](../../Research/UI/IMMERSIVE_MENU_SYSTEMS_RESEARCH.md) remain provisional references. This direction supersedes their recommendations to prioritize comparisons among expanded physical menu families. Shared navigation and input reliability remain useful internal architecture; they should simplify the player's experience.

## Scope and boundaries

The first design review should show a compact supporting menu and an uncomplicated radial setup flow using existing commands. Inventory presentation can be reviewed when it serves a concrete existing task. Pause behavior, save/exit behavior, and final art remain unresolved; this direction does not select their implementation.

Existing Legger presence, cargo range, placement, and capacity rules remain authoritative. The menu does not grant remote cargo access or change companion traversal. The current survival values and their deferred mechanics remain as specified in the [survival standard](./SURVIVAL_SYSTEM_STANDARD.md).

World identity, generated-object identity, and persisted gameplay deltas are unaffected by this product-direction record. Greater Wasteland remains fixed and always loaded; no generation, streaming, or new persistence integration is proposed. UI preferences remain separate from gameplay state. Future views must safely handle invalid domain targets without taking ownership of world loading.

## Acceptance questions

Can a player begin playing with the defaults, find a frequent action quickly, change a shortcut without learning a complicated editor, and return to play without losing selection or triggering an unintended action? Does the visual treatment support the atmosphere while remaining readable? Does each screen remove friction from an actual task?

Menu breadth, the number of physical props, and configuration depth are not quality measures. The system succeeds when it needs little attention during ordinary play.
