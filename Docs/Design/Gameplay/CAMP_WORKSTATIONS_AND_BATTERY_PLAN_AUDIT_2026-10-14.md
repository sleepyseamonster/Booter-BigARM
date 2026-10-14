# Camp workstation implementation plan audit

Date: October 14, 2026. Reviewed [implementation plan](CAMP_WORKSTATIONS_AND_BATTERY_IMPLEMENTATION_PLAN_2026-10-14.md) at `dfb2219dd`, together with live working files and concurrent diffs. Verdict: the architecture is suitable, but the plan needs revision before autonomous implementation. Four high-priority integration gaps and three medium-priority specification gaps remain. This review leaves the implementation plan and gameplay assets unchanged.

## Findings

### F1 Reconcile the new inventory contract before designing payloads and battery destinations

Priority: P1. Plan locations: Revalidated repository facts, Prototype defaults, Items and recipes, Typed canister record, and Batches 0–2.

Concurrent working changes now add `InstanceId` to [inventory slots](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventorySlot.cs), [snapshots](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventorySnapshot.cs) and [item amounts](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Items/TopDown3DItemAmount.cs). They also add slot admission through [inventory policy](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventoryPolicy.cs) and a [loadout layout](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DLoadoutLayout.cs) with six named equipment slots and four supply pockets. Its equipment addresses have no battery slot. Supply pockets require `IsFieldSupply`; a category string of Equipment does not satisfy that rule.

The plan was grounded in the older slot shape and kit capacity, proposes instance identity inside the payload and sends the battery directly into Booter's kit without defining loadout metadata. Implementing against those assumptions could duplicate identity authority or make crafting/retrieval permanently fail even with empty supply pockets. These live changes are unfinished owner work, not proof that a new production contract is accepted or complete.

Required correction: make the inventory owner's sealed handoff a Batch 0 dependency. Extend its canonical slot instance ID with canister contents instead of adding a competing payload ID. Decide the battery's legal destination against the settled layout: preferably a supply pocket if the owner approves that classification, or an explicitly selected local chest/cargo output. If the fixed DustCanister equipment slot is retained, reconcile the proposed filled-canister-in-cargo behavior with that slot and its action availability. Update capability/snapshot versions, item metadata, capacities, tests and affected source boundaries from that single contract.

Proof gate: craft and retrieve a battery into a legal empty address; reject a full or wrong slot without consuming ore/dust; preserve canister identity through the settled equipment/cargo transfer rules; run the inventory owner's focused layout/identity tests alongside workstation tests. Do not implement these overlapping files while their current owner is writing them.

### F2 Choose a camp-station dispatch path that preserves the playground receiver

Priority: P1. Plan locations: Commands, UI, Stations and session boundary, and Batch 5.

The [player action controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DPlayerActionController.cs) holds one `ITopDown3DStationActionReceiver`. `ConfigureStationActions` replaces that owner when it is idle, and rejects replacement while busy. The [playground player binding](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.Player.cs) assigns itself during startup and clears the receiver during detach. Its `TryBegin` handles animation targets and reports unrelated targets as unavailable. The plan calls for explicit dispatch but does not select how camp services coexist with this exclusive owner.

A new receiver installed by the workstations could disconnect animation stations; passing new camp station targets into the current playground receiver could reject them. Startup/detach order must not decide which service works.

Required correction: select one route before implementation. The smallest extension is to expose camp workstations through a dedicated interaction branch/event analogous to cache access, keeping the existing animation receiver intact. Include the station modal in the player's activity/busy checks without replacing the receiver. If the settled interaction owner instead selects a dispatcher, it must be the single receiver with explicit target ownership and safe unregister semantics. Name the exact interface/event and its registration/lifecycle in the plan.

Proof gate: camp services and AP01–AP12 remain addressable in either initialization order; idle/active cancellation and disabling either host do not clear the other host's ownership or allow simultaneous actions.

### F3 Specify the workstation modal and system-menu handoff

Priority: P1. Plan locations: UI, Interface boundary, and Batch 5.

The [system menu](../../../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DMenuController.cs) records only `inventory.IsOpen`, invokes that controller's suspend/resume methods, and otherwise returns to Gameplay when closing. The [input router](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DInputRouter.cs) enables the UI map through existing Inventory/SystemMenu modes. A separate workstation canvas does not automatically participate in this contract.

The plan requires menu suspension and stale-submit protection but lists the menu/input owners only as systems to inspect. Without an explicit integration, opening the system menu over a refinery can leave station buttons active underneath it, lose their focus, or restore Gameplay while the refinery panel remains visible. Using Inventory mode alone does not teach the system menu how to resume the station.

Required correction: choose the existing inventory controller as the host for workstation contexts, or explicitly extend `TopDown3DMenuController` to retain and suspend the typed workstation modal owner. Define opening, closing, suspension, resume validation, focus restoration, binding-release guards and ToggleInventory behavior. Require one visible interactive owner and one InputSystemUIInputModule. If access is lost while suspended, close the station and restore Gameplay rather than reopening an invalid context. Include the precise menu/router/controller files in the modification boundary.

Proof gate: open workbench/refinery -> system menu -> resume, cancel, Camps, inventory toggle, death and disable/unload. A held Submit/Click must never craft, insert, refine or retrieve on resume; closing one modal must not change the mode owned by another.

### F4 Expose an actual way to select and redeploy a partial cargo canister

Priority: P1. Plan locations: Canister carry default, Commands, and Batch 3.

The plan correctly keeps nonempty canisters out of Booter's kit and requires redeployment from accessible cargo. Current `HasCanister`, `CanSelectCanister` and placement-preview startup in the [action controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DPlayerActionController.cs) inspect Booter's kit only. The [radial controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DRadialMenuController.cs) relies on `CanSelectCanister`. The plan specifies a service command but no player-facing source selector or mapping to those existing entry points.

After collecting the sole starter canister or refining only part of its contents, the player may have a valid canister in cargo and no way to start its placement preview. Selecting an arbitrary matching canister would also violate the plan's exact-instance command rule.

Required correction: add a Deploy action for a selected canister in physically accessible cargo/chest inventory, or a small source selection context reached through the existing deployment action. Name the chosen path. Capture owner, instance ID, revision and epoch when entering preview, recheck access at confirmation, and cancel safely if the selected slot changes. Extend radial availability and direct deployment consistently; preserve all current production behavior with the lab feature absent.

Proof gate: with Booter's kit containing no canister and cargo holding two different partial canisters, select one, preview, cancel, deploy and pick it up without altering the other. Moving cargo away or repacking during preview must invalidate safely. Keyboard and gamepad must reach the same path.

### F5 Define payload absence in serialized snapshots

Priority: P2. Plan locations: Typed canister record and Snapshots.

The plan proposes an optional serializable payload while preserving ordinary v1 saves, but does not define its discriminator or representation. The repository already documents a relevant hazard in [the authored-world snapshot](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandInventorySnapshot.cs): `JsonUtility` can materialize null inline classes on round trip, so a nullable string is used to preserve population-data absence.

Relying only on `payload == null` could mistake a default materialized object for a real canister payload and reject ordinary items/legacy snapshots, or accept malformed empty payloads. This is a representation gap, not a confirmed implementation bug; the payload has not been written yet.

Required correction: choose an explicit payload kind/presence field or flat versioned fields. Use the settled canonical `InstanceId` from F1. Define empty-slot, ordinary-item, empty-canister, filled-canister and legacy-v1 representations and validation. Require a valid discriminator, not object nullability alone. Decide how older state is handled without silently minting instances.

Proof gate: serialize and deserialize empty/ordinary slots, populated canisters and legacy inventories with `JsonUtility`; compare quantity, identity and payload semantics; reject a forged presence flag, unrelated item payload and missing instance ID before any owner changes.

### F6 Rebase the collection clock during ownership transitions

Priority: P2. Plan locations: Collection default, Commands, and Batch 3.

The plan states that carried/docked canisters do not collect and that redeployment preserves fractional seconds. Existing [placed records](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DPlacedHarvesterState.cs) calculate production from `fractionalSeconds + now - lastEvaluatedSeconds`. Copying a record's old evaluation timestamp into a later deployment would count time spent in cargo or a socket. The plan does not specify the timestamp transition or test that interval.

Required correction: on pickup, advance the placed record once to the authoritative simulation clock before preparing the transfer. Store dust and fractional remainder as dormant carried state. On redeployment, initialize `lastEvaluatedSeconds` to the current registry clock while preserving that remainder; do not carry the old timestamp across dormancy. Clock/fill changes must participate in the registry's revision and lock rules so drafts cannot overwrite collection that occurred after preparation. At capacity, preserve the existing zero-remainder rule.

Proof gate: collect for 13 seconds at the current 10-second rate -> 1 dust plus 3 seconds remainder; remain carried/docked for 1,000 simulation seconds -> unchanged; redeploy for 7 seconds -> exactly 2 dust, not 102 or a full canister. Repeat across retained snapshot restore and paused simulation.

### F7 Move prerequisite station state and initialization before dependent gates

Priority: P2. Plan locations: Batches 1–5.

Batch 2 requires refinery transaction participants, while refinery state is assigned to Batch 5. Batch 4 requires validating every owner, establishing a baseline with refinery sockets, and clearing those sockets during reset before Batch 5 creates station state. Fixtures can use drafts, but the plan does not state when these shared prerequisites become real, compilable types.

Required correction: introduce plain refinery/socket state, prepared participants, recipe state and pure storage policies before inventory/reset integration. Keep MonoBehaviour station wiring, visuals and UI in the later batch. State which batch creates each type and which fixtures can run before scene installation. Require every commit boundary to compile; do not depend on temporary fake owners that survive into the final architecture.

Proof gate: each batch has a complete compilable source boundary and tests available at that stage. Batch 4 reset tests can instantiate the actual refinery state without a future dependency, and Batch 5 only adds interaction/modal presentation to already-qualified state.

## What should remain in the plan

Keep the lab-first rollout, fixed resident terrain, separate production save migration, independent recipe-driven starter grant, one existing chest, exact dust leftovers, physical cargo access and guarded inventory transactions. Preserve the salvage catalog and reward conservation until that owner deliberately extends its consumption ledger. Keep ordinary animation reset separate from economy reset, and preserve unrelated inventory contents during the latter.

Four ore and ten dust remain reasonable proposed test values. This audit does not treat them as approved economy or introduce new materials, fuel, queues, battery consumers or terrain work. No redesign of the overall architecture is required to resolve these findings.

## Revision order and implementation gate

1. Obtain the settled inventory/interaction owner handoff and update the plan's data, slot and destination assumptions (F1).
2. Specify station dispatch, modal lifecycle and source selection against those contracts (F2–F4).
3. Define snapshot presence and simulation-time transitions, then reorder prerequisite batches (F5–F7).
4. Map each finding to a focused regression fixture and rerun the document's preservation/ownership review. Mark the plan implementation-ready only when every correction is concrete.

The plan is ready for revision, not autonomous implementation as written. These are technical planning gaps that can be resolved without requesting routine user decisions. Any actual change to the author's desired carry rules, recipe or refinery timing must remain explicit in the implementation brief.

## Evidence boundary

This audit inspected source, plan text and working diffs. It did not run Unity, compile, execute tests, alter gameplay, edit the reviewed plan or focus an application. Live inventory and animation work changed during inspection; the new loadout/identity details are integration dependencies, not accepted final behavior. The report's documentation links and whitespace are checked separately from implementation proof. Only this report belongs to the audit commit.
