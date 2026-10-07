# Survival System Standard

This document defines the first TopDown3D survival-system slice and the seams that later survival work must preserve.

## Player Vitals

Booter has four capacity-style survival values. A full meter means the current need is satisfied.

- **Health** represents remaining life and begins full.
- **Hunger** represents remaining nourishment and depletes over time.
- **Thirst** represents remaining hydration and depletes over time.
- **Oxygen** represents breathable reserve and begins full.

The first implementation automatically depletes only hunger and thirst. Empty hunger or thirst does not yet damage health. Health loss, health recovery, eating, drinking, low-oxygen exposure, oxygen loss, and oxygen recovery are later slices.

## Tuning And Runtime State

- `TopDown3DSurvivalSettings` owns authored capacities and depletion rates.
- The canonical settings asset is `Assets/_Project/Settings/Survival/Resources/TopDown3DSurvivalSettings.asset`.
- `TopDown3DSurvivalVitals` owns mutable player state and deterministic elapsed-time advancement.
- The default hunger and thirst rates are provisional tuning values, not locked design decisions.
- External gameplay systems may set a vital explicitly, but this slice does not introduce item-consumption or environmental-exposure owners.

## Procedural World Contract

- **World identity:** not applicable to the vital values; they belong to Booter's save state rather than a seed or chunk.
- **Stable identity:** the single player-survival record is stable across chunk transitions.
- **Streaming lifecycle:** hunger and thirst continue independently of streamed chunk presence. Unloading a chunk must not reset them.
- **Persistence:** all four values are captured in a versioned snapshot. A future TopDown3D save owner must include that snapshot without serializing a scene object.
- **Authored constraints:** the Broken World's algae-based survival rules control future replenishment design. This slice does not invent water sources.
- **Deterministic proof:** direct elapsed-time advancement verifies rate behavior, clamping, and snapshot restore without depending on frame rate or chunk state.

Future low-oxygen regions should provide exposure to the player-owned oxygen system from a streamed zone or volume. The zone is generated or authored world content; the oxygen value itself remains player state and must survive zone unload.

## HUD Contract

The four meters use a compact 2x2 safe-area-aware panel. It occupies the upper-left corner and remains separate from the lower-left action D-pad indicator. All four meters remain visible in this foundation slice so their ownership and layout are stable; oxygen is not given extra visual priority while it is normally full.

The HUD installs at runtime when the perspective player is present, avoiding scene or prefab coupling during this additive foundation pass.
