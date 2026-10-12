# Algae canister control brainstorm

October 11, 2026. The author requested a lower-left algae canister control alongside the existing upper-left status HUD. D-pad left spends algae to restore thirst, right spends algae to restore hunger, and up spends algae to restore health. Each action restores an amount rather than automatically filling its entire meter.

Those directions and effects are the requested design. The presentation, amounts, costs and timing below are proposals for implementation review.

## The canister as a survival decision

Booter carries one finite algae reserve serving three needs. Drinking and eating draw from the same store; treating an injury consumes a more expensive dose. Spending algae now leaves less available for the next stretch of travel. This fits the [world basis](../../WORLD_BASIS.md): personal algae reserves sustain life on a world without natural water.

The control can imply a drinking nozzle, ration outlet and treatment applicator without committing to medical lore, new crafting recipes or a backpack animation. Health restoration is the author's requested game effect. The first UI slice need not establish how that treatment works biologically.

## Recommended lower-left layout

Use the existing lower-left D-pad panel as the foundation. Give its three action cells a droplet marked **Drink**, a ration marked **Eat**, and a cross marked **Treat**. Match the existing thirst, hunger and health meter colours, while retaining distinct shapes and text. Put an algae canister silhouette and a readable fill bar immediately beside the control. Display **ALGAE 60 / 100**, using the actual authored capacity rather than a baked-in 100.

The layout reads as a cross: Treat above, Drink left, Eat right. Its centre is a quiet control hub. D-pad down remains the existing dust-canister deployment action and retains a separate dust icon and label. Do not make down refill algae or give it a new survival function.

After a successful press, briefly illuminate the relevant cell and matching upper-left meter, animate the fill changes, and show feedback such as **Thirst +20 · Algae −5**. The underlying values commit immediately; the animation only presents them. A full meter says **Already hydrated**, **Already nourished**, or **Health full** and spends nothing. An empty store says **Canister empty**. An inactive cell explains the reason when activated instead of silently doing nothing. Avoid continuous flashing or repeated low-fill sounds.

Keep the panel legible against orange terrain and usable without colour recognition. Reuse safe-area margins and HUD scaling. During menus and camp actions, dim the consumption controls to communicate that the D-pad currently has another owner. Derive binding prompts from the existing input router so rebinding and device changes remain accurate.

## Consumption options

| Approach | Experience | Tradeoff |
| --- | --- | --- |
| One dose per fresh press | Immediate, predictable resource decision | Repeated doses require deliberate taps |
| Hold to consume continuously | Convenient for large deficits | Easier accidental depletion; adds repeat timing and interruption complexity |
| Open a dose selector | Precise amount selection | Adds menu friction to three common survival actions |

Recommend one dose per fresh press. Holding a direction consumes once. Require release before another dose, including after closing a menu. Reject diagonal D-pad presses instead of spending twice or choosing an invisible priority. Use a short shared cooldown for all three actions to prevent rapid accidental multi-dose input. Continue allowing ordinary movement; the first slice does not lock locomotion for a decorative use animation. Consumption is unavailable while camp, climbing, traversal, gathering or another constrained action owns the player.

## Starting balance proposal

These are editable trial values, not final game balance. On the current 100-point vital capacities:

| Direction | Action | Maximum gain per press | Algae cost for that gain |
| --- | --- | --- | --- |
| Left | Drink | 20 thirst points | 5 algae units |
| Right | Eat | 20 hunger points | 5 algae units |
| Up | Treat | 10 health points | 10 algae units |

Start the prototype with a 100-unit canister containing 100 units. Health is intentionally more expensive in this proposal. Costs and gains belong in a dedicated settings asset and may be revised independently of input or UI.

Spend only for useful restoration. At 96 thirst, Drink restores 4 and spends 1 algae unit. With only 2 algae units left, Drink restores up to 8 thirst. Empty/full cases spend nothing. This removes waste near the cap while preserving the shared resource decision. A roughly 0.35-second cooldown, measured in unscaled time, is a provisional accidental-repeat guard.

Oxygen and Reserve are outside this control. Rest and Sleep continue restoring Reserve under their existing rules. Refill sources, refill stations, replacement cartridges, inventory transfers, physical canister art, use animations and long-term save integration belong to later slices. A fresh-session starter fill is a prototype initialization rule, never a refill on menu close, HUD rebuild or player presentation change.

See the [implementation plan](ALGAE_CANISTER_IMPLEMENTATION_PLAN.md) for ownership, transaction rules, input conflicts, persistence boundaries and verification gates.
