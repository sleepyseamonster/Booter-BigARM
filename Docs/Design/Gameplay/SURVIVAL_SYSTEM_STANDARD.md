# Survival System Standard

This document defines the first TopDown3D survival-system slice and the seams that later survival work must preserve.

## Player Vitals

Booter has five capacity-style survival values. A full meter means the current need is satisfied.

- **Health** represents remaining life and begins full.
- **Hunger** represents remaining nourishment and depletes over time.
- **Thirst** represents remaining hydration and depletes over time.
- **Oxygen** represents breathable reserve and begins full.
- **Reserve** represents long-term physical readiness. It drains slowly during activity and a little faster while Sprint is held during running, climbing, or traversal moves. It is not a short stamina bar.

Hunger, thirst, and Reserve deplete automatically. Empty Reserve does not force a fall or disable movement. Standing still or waiting never restores Reserve. The camp prototype adds explicit awake Rest and Sleep actions. By user direction on October 11, 2026, either action can start at any Reserve level, including full Reserve, subject to the existing camp and supply checks. Rest lasts up to 20 simulation seconds and recovers Reserve only below 40 percent. Sleep accelerates the continuously simulated world, recovers Reserve only below 80 percent, and continues until manual wake, low supplies, or interruption. Reaching a recovery cap does not wake Booter or reduce an already higher Reserve value. Both actions keep spending nourishment, hydration, and oxygen at their authored rates, including while Reserve recovery is capped. The ordinary depletion tuning still gives roughly 167 minutes from full to empty and roughly 67 minutes of continuous exertion. These values are provisional. Empty hunger or thirst does not yet damage health. General health loss/recovery, eating, drinking, low-oxygen exposure, and ordinary oxygen depletion/recovery remain later slices; explicit health reduction now interrupts camp recovery.

## Tuning And Runtime State

- `TopDown3DSurvivalSettings` owns authored capacities and depletion rates.
- The canonical settings asset is `Assets/_Project/Settings/Survival/Resources/TopDown3DSurvivalSettings.asset`.
- `TopDown3DSurvivalVitals` owns mutable player state and deterministic elapsed-time advancement.
- The default hunger, thirst, and Reserve rates are provisional tuning values, not locked design decisions. No automatic idle recovery is active. Camp rates and assets live in `Assets/_Project/Settings/Camping/Resources/TopDown3DCampSettings.asset`; the existing vitals owner selects the activity mode.
- Runtime advancement reconciles timestamped activity transitions once per simulation interval. Internal double accumulators preserve fractional depletion at high frame rates; the public values and versioned snapshots remain floats. Snapshot restore resets transient recovery and its runtime clock.
- External gameplay systems may set a vital explicitly, but this slice does not introduce item-consumption or environmental-exposure owners.

## Procedural World Contract

- **World identity:** not applicable to the vital values; they belong to Booter's save state rather than a seed or chunk.
- **Stable identity:** the single player-survival record is stable across chunk transitions.
- **Streaming lifecycle:** hunger, thirst, and Reserve continue independently of streamed chunk presence. Unloading a chunk must not reset them.
- **Persistence:** all five values are captured in a versioned snapshot. Version 1 snapshots restore Reserve full; version 2 preserves its value. A future TopDown3D save owner must include that snapshot without serializing a scene object. The current camp prototype assigns stable session instance IDs, but deployed camps and sleep progress are not restored after restarting a session. Saved-world integration and establishment sleeping remain separate slices.
- **Authored constraints:** the Broken World's algae-based survival rules control future replenishment design. This slice does not invent water sources.
- **Deterministic proof:** direct elapsed-time advancement verifies rate behavior, clamping, and snapshot restore without depending on frame rate or chunk state.

Future low-oxygen regions should provide exposure to the player-owned oxygen system from a streamed zone or volume. The zone is generated or authored world content; the oxygen value itself remains player state and must survive zone unload.

See the [camp implementation plan](./CAMP_CLOAK_AND_SLEEP_IMPLEMENTATION_PLAN.md) for scope, controls, tuning, interruption rules, and proof boundaries. The starter cloak provides worn and blanket presentations without creating a second equipment inventory or a healing bonus.

## HUD Contract

The five meters use a compact, vertically stacked safe-area-aware panel. It occupies the upper-left corner and remains separate from the lower-left action D-pad indicator.

The HUD installs at runtime when the perspective player is present, avoiding scene or prefab coupling during this additive foundation pass.
