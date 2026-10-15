# Camp Loop five resource sources plan audit

October 14, 2026. Reviewed the [creative plan](CAMP_LOOP_FIVE_RESOURCE_SOURCES_CREATIVE_PLAN.md) against current repository sources. Scope: document and code inspection only. The original plan, Unity scene, prefabs, runtime code and spreadsheet are unchanged by this audit.

The five-source selection, separate study hierarchy and authored placement approach are coherent creative directions. The plan needs the following six corrections or clarifications before it can guide a reliable build. Two are high priority contradictions; four concern integration contracts. Proposed dimensions and exact placement remain appropriately provisional rather than verified scene results.

## Findings

### 1. P1 Separate animation reset from inventory trial reset

**Plan locations:** Identity and repeatable Camp Loop trials, especially lines 152–156 at the reviewed version.

The plan combines the ordered animation reset, captured inventory baseline and restoration of all source stock/beetle state into one unspecified reset. Those are separate existing owners. [AnimationPlaygroundResetRegistry](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundResetRegistry.cs) invalidates and restores participants sequentially; it is not an atomic inventory/world transaction. [TopDown3DCampLoopInventoryTrial](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DCampLoopInventoryTrial.cs), particularly `RequestReset` and `RegisterResetParticipant`, owns the confirmed inventory baseline restore and prepared world mutations. Its confirmation expressly says actors and vitals stay unchanged.

**Failure:** placing stock restoration on the animation registry could replenish sources while leaving their rewards in cargo. Replacing that with a broad inventory snapshot could instead discard other trial progress through a reset control that currently promises different behavior. Restoring a live beetle through inventory reset would also contradict the current actor-preservation message unless that new exception is explicit.

**Required correction:** define both paths. Animation reset cancels combat/gather captures and preserves study inventory, health/death and depletion. The confirmed inventory-trial reset must prepare study stock and any chosen beetle lifecycle restoration within the same transaction as inventory restoration. Register through its existing participant seam; do not supply duplicate mutations for inventories already owned by the reset. Explicitly decide whether that confirmed reset restores the beetle or preserves its living state. If it restores the beetle, extend the existing confirmation to name that exception, validate the return anchor and preserve Booter, Legger and unrelated actors. No new reset menu or companion teleport is needed.

**Proof required later:** animation reset after harvesting cannot grant another reward; confirmed trial reset restores paired stock/inventory; a failed later animation participant cannot duplicate a previously restored reward; repeated confirmation/retry and invalid beetle return remain coherent.

### 2. P1 Fix the death and reward acceptance contradiction

**Plan location:** completion criteria, line 168 at the reviewed version.

The condition that “the beetle cannot damage or reward after death” contradicts the intended death → carcass → scrape → fibers loop. An implementer could satisfy the acceptance sentence by disabling the only intended fiber reward.

**Required correction:** use: “The beetle cannot move or attack after death. Death grants no automatic inventory reward. Its unharvested carcass can award the finite scrape reward once; a depleted carcass cannot award it again until the explicitly defined trial reset.” Also prevent health changing from reviving the actor incidentally.

**Proof required later:** dead/unharvested is harvestable, dead/depleted is unavailable, alive is not scrapeable, and duplicate death callbacks add no reward.

### 3. P2 Keep the winch independent of the existing wreck owner

**Plan locations:** RS04 dead winch and the shared-interaction/build sections.

The plan distinguishes the winch visually from the ghost machine but does not identify the unsafe reuse boundary. [TopDown3DSalvageWreckState](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Salvage/TopDown3DSalvageWreckState.cs) is hard-coded to one wreck ID, three sockets and three existing salvage rewards. [CampLoopSalvageSession.TryBindSession](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Salvage/CampLoopSalvageSession.cs) requires exactly one such wreck in the entire scene. Attaching that owner to a second winch would disable the original salvage session or produce wrong rewards/IDs.

**Required correction:** give the winch its own generic finite-source state within the new study. Reuse the interaction, targeting and inventory transaction contracts, not the ghost machine's concrete session/wreck component. Keep one original ghost-machine owner and preserve its existing three points. Do not expand its hard-coded item array or copy its stable ID.

**Proof required later:** one existing ghost-machine wreck owner remains; its three original rewards work; one independent winch yields cable once and has a distinct ID/reset state.

### 4. P2 Complete the design-to-runtime item mapping and initialization contract

**Plan locations:** source output descriptions and shared catalog paragraph, especially lines 114–144 at the reviewed version.

Four outputs currently refer only to workbook `proposal.*` IDs, while Ironstone names a real runtime ID. The plan says to extend the current catalog but does not state the chosen runtime IDs or when every consumer must receive that catalog. [TopDown3DItemCatalog](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Items/TopDown3DItemCatalog.cs) builds a cached lookup and applies a separate loadout definition list. [CampLoopSalvageSession](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Salvage/CampLoopSalvageSession.cs) also checks exact catalog-object agreement with kit, cargo and cache. Equivalent contents in a different catalog object are insufficient for that owner.

**Failure:** unknown rewards, stale runtime lookup or disabled original salvage despite apparently correct new inventory entries. The display/workbook category `Resources` also does not substitute for the runtime `Resource` literal that enforces Legger cargo.

**Required correction:** add one explicit mapping table. Retain `resource.ironstone_ore`; proposed private IDs for the other four could be `lab.resource.chitin_fibers`, `lab.resource.dry_mineral_salts`, `lab.resource.coarse_sensory_bristles` and `lab.resource.salvaged_metal_cable`. These are suggestions, not newly created items. Specify validated icons, Resource category and Legger-only carry metadata. Bind one deliberately selected scene catalog consistently to inventories and existing sessions before storage/loadout initialization. Check all camp storage/processor owners that resolve or accept these items. Preserve existing definitions, resolve variants, and do not mutate shared production assets or reinitialize live inventory to apply a catalog change.

The existing automatic call-over branch is specific to `TopDown3DIronstoneNode`; the new generic study sources will not inherit it merely by yielding ore. Retain manual companion regroup/access unless a later task explicitly expands that behavior.

**Proof required later:** fresh startup and domain reload resolve all five rewards and old items; existing salvage remains ready; inaccessible/full/heavy cargo preserves the source; no implicit Booter fallback.

### 5. P2 Specify retained source state across domain reload

**Plan locations:** identity/lifecycle paragraphs and the proposed Source Registry.

The plan correctly says disable/re-enable does not renew stock, but “session-only” leaves domain reload behavior unstated. Existing [player inventory](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DPlayerInventory.cs) retains runtime snapshots, and the [salvage wreck](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Salvage/TopDown3DSalvageWreckState.cs) retains serialized consumption state. Recreating the new source ledger as pristine after a reload would restore stock while existing cargo still contains its reward.

**Required correction:** distinguish fresh Play session, domain reload, presentation reconstruction and explicit reset. Retain a versioned five-source record with immutable source ID, stock/phase, beetle health/death and required anchor/transform state. Validate retained records before enabling actions; incompatible records preserve inventory and expose an unavailable reason. Retained baseline acquisition must occur after the intended catalog and existing trial services are ready. Increment epochs to reject stale captures without treating that increment as stock renewal. Declare that the registry owns only study records and never resets arbitrary objects found by type/name.

**Proof required later:** gather → reload/reconstruct → no second reward; death before harvest survives reload; depleted carcass remains depleted; invalid/duplicate IDs stop availability; a new Play session starts fresh without touching disk checkpoints.

### 6. P2 Define the minimum combat and interruption contract

**Plan location:** RS05 husk beetle and later build step four.

The plan requires a qualified damage interaction but does not spell out how to distinguish it from animation playback, repeated collision events or a command that remains active across menus/reset. Existing [NPC actor](../../../Assets/_Project/Scripts/Runtime/TopDown3D/NPC/NpcActor.cs) is a skittish movement owner; the [animation station dispatcher](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.Stations.cs) is not a combat damage implementation. Creative telegraph/strike/recovery language needs a small observable contract before the enemy can be called complete.

**Required correction:** one attack instance and one bounded hit window, with range/facing/obstruction checks and at most one hit per intended target per attack, regardless of child colliders. Reuse current input/motor/animation ownership; attacks cannot overlap gathering, camp/station actions, riding or modal UI. Cancel or suspend the owned action safely on reset, death, pause, disable or lost readiness; do not execute a queued strike after returning from a menu. Keep the beetle's sensing, approach and strike envelope out of protected areas. Retain the plan's no-interaction-to-kill completion rule.

**Proof required later:** no through-wall hit, no duplicate collider damage, no post-death attack, no preview damage, no menu/reset delayed strike and no competing player action owner. Hands-on combat feel remains author-owned.

## What passes the creative audit

- The five selected sources map to established item concepts and create distinct gathering activities.
- One owned root, independent prefabs, explicit source identity and replaceable visual children give a clear organization direction.
- Fixed, always-loaded terrain, no procedural rollout and no production-save installation fit the requested Camp Loop scope.
- Terrain support, source/cargo approach, station/riding/Lager exclusions and source readability are explicitly required before choosing exact positions.
- Native terrain, existing nodes, original wreck, shared metadata and companion ownership are protected.

The proposed “12 m clear encounter area” still needs an unambiguous diameter/radius and a full activity-envelope survey during layout. This is a normal unresolved creative dimension, not evidence that the current terrain can support that footprint. Do not promote the dated Booter/Lager anchors into approved spawn coordinates.

## Audit boundary and handoff

No Unity process was launched or focused, no gameplay smoke test was created, and no implementation was validated. Findings come from current source contracts and the reviewed document. The worktree contains concurrent scene/NPC/rendering edits; implementation must refresh its baseline rather than use this audit as a sealed scene snapshot. Older salvage documentation describes a historical reset path; current inventory-trial code controls the live baseline-reset integration.

Recommended disposition: retain the creative layout and source briefs; revise the six contracts above before starting the build. The original plan is deliberately left unchanged so this review remains distinct from an accepted design revision.

## Implementation follow-through

The author subsequently requested implementation. The revised creative plan and [implementation receipt](../../Evidence/Gameplay/CampResources20261014/README.md) incorporate all six findings: separate animation and inventory resets, finite post-death harvesting, an independent winch ledger, explicit item/catalog mappings, retained versioned source state and owned melee hit/cancellation rules. Eighty-seven focused checks pass in the isolated candidate against the latest saved scene. Live synchronization into the author's dirty Editor remains subject to its external-scene-change dialog; this does not authorize discarding or reloading unsaved work.
