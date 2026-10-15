# Camp Loop ghost machine salvage implementation plan

Status: implemented as a bounded playground blockout October 14, 2026; see the [implementation receipt](../../Evidence/Gameplay/CampLoopSalvage20261014/README.md). The audit corrections below govern the build. Its ghost-machine lore remains provisional.

Build one dead, collapsed six-legged machine in `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`. Booter can remove visible metal plates, wiring and a drive assembly into the physically accessible Legger cargo. Three independent salvage points leave visible empty housings after use. The existing playground reset restores the wreck and clears its trial rewards together, allowing repeated testing without accumulating free salvage.

Gottspan coordinates scope and integration. Babineaux owns Unity authoring and validation. A Blender art pass is optional after the blockout works; native Blender sources remain local-only. The author owns hands-on testing, appearance and creative acceptance.

## Scene baseline and controlling sources

The target is the existing CampLoopTesting scene, not a newly extracted scene or the production Greater Wasteland scene. Preserve its nine native terrain/collider pairs, camp and return route, seven authored rocks, twelve Ironstone nodes, resource cache, Booter, Legger, camera, input, lighting and animation stations.

The [original scene receipt](../../Evidence/Gameplay/CAMP_LOOP_TESTING_SCENE_2026-10-13.md) describes its initial separate disk checkpoint folder. The current [playground session](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.cs) supersedes that behavior when its scene marker is present: `IsSessionOnly` prevents checkpoint startup, restoration and writes through the current save service. Keep that policy. A marker-free or incorrectly configured scene must not install salvage.

The [playground scene plan](CAMP_LOOP_ANIMATION_PLAYGROUND_SCENE_PLAN_2026-10-13.md), [inventory standard](INVENTORY_AND_ITEM_MANAGEMENT_SYSTEM.md), [world basis](../../WORLD_BASIS.md) and [fixed terrain dressing plan](FIXED_TERRAIN_DRESSING_PLAN.md) control their respective lanes. Ghost-machine consciousness, history and power-source lore remain open; the first wreck has no live core, awakening or liquid coolant.

Live implementation seams inspected for this plan:

- [Interaction contract](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/ITopDown3DInteractable.cs): one reward type per action, duration and consume callback.
- [Gather state](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DGatherActionState.cs): range/reachability revalidation and inventory transaction with resource consumption as its commit condition.
- [Target selection](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DInteractionController.cs): collider ownership, facing, line of sight and target retention.
- [Item destination](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DItemDestinationService.cs): Resource items require accessible Legger cargo; no Booter fallback.
- [Reset registry](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundResetRegistry.cs): invalidate all owners, restore ordered participants, keep input blocked if reset fails.
- [Checkpoint service](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandInventorySaveService.cs): current fixed-world persistence, disabled for the marked playground. Resource restoration currently validates Ironstone-specific records.

The old Ironstone system document's Booter fallback and deferred disk-save descriptions do not describe current live behavior. This task uses current code and the later inventory/playground contracts; general documentation reconciliation is a separate concern.

## First playable result

Place one wreck roughly 20 to 35 metres from the camp on a clear side excursion. Choose the exact transform from native terrain queries and current station footprints during implementation; distance alone does not establish safe placement. The carcass is approximately 4 m long, 2.5 m wide and 1.5 m high. Lie it diagonally on one side with an exposed flank, folded legs and one detached leg. These dimensions are starting art targets.

Give it a narrow ribbed hull, angular jointed legs and a recessed, empty sensor head. Use dark worn metal, pale ceramic details and dust in cavities. The exposed armour, hanging cable loom and open leg joint should remain identifiable at the normal elevated camera distance. No alive/dead colour toggle substitutes for the missing-part geometry.

Use a static structural hull and simple colliders. Removable pieces are presentation children with separate interaction ownership; successful extraction hides them rather than spawning loose physics pickups. Keep the main obstacle envelope unchanged after stripping. Detached decorative debris is visually subordinate and does not advertise a collectible reward.

| Salvage point | Trial reward | Initial duration | Depleted appearance |
| --- | --- | --- | --- |
| Loose outer armour | 2 Metal Plates | 2.5 seconds | Loose plates absent; structural ribs retained |
| Exposed cable loom | 1 Wire Bundle | 2 seconds | Long loops absent; cut ends retained |
| Broken leg joint | 1 Drive Assembly | 3.5 seconds | Empty joint housing |

All three points begin available and can be worked in any order. Each has one use. There is no opening prerequisite, random reward roll, quality grade, tool gate, crafting recipe or sale value in this slice. The reward is known from the prompt; use the existing device-aware binding hint with short text such as `Strip wiring`. Successful receipts name quantity, item and destination.

The player stands, faces the part and commits to an in-place extraction action. Retain cancel behavior and modal/action ownership. Full or inaccessible cargo, blocked reach and cancelled work leave reward and wreck state unchanged. The wreck remains in place when exhausted.

## Trial item definitions and catalog isolation

Create three private lab item assets with stable IDs such as `lab.salvage.metal_plate`, `lab.salvage.wire_bundle` and `lab.salvage.drive_assembly`. The `lab` prefix distinguishes trial items from future production items; player-facing display names remain plain.

| Item | Max stack | Trial unit mass | Carry and packing |
| --- | --- | --- | --- |
| Metal Plate | 20 | 2 kg | Resource, Legger only, normal packing |
| Wire Bundle | 10 | 0.5 kg | Resource, Legger only, normal packing |
| Drive Assembly | 1 | 6 kg | Resource, Legger only, normal packing |

The complete wreck yields 10.5 kg. These are test values, not accepted economy tuning. Use distinct plate, loop and joint icons with the existing inventory presentation. Every item must pass the current required-icon, finite-mass and Resource carry-preference validation.

Author one lab item catalog containing the current production definitions plus these three. Assign it through scene-local serialized overrides on Booter inventory, Legger cargo and cache before their storage states initialize. Preserve capacities, packing mounts, starting contents and cache identity. The runtime session validates these assignments; it never calls inventory `Configure`, which recreates storage. UI and transfer destinations must resolve the same definitions. A cache catalog override is sufficient; retain its original prefab link.

Do not edit the production item catalog or add salvage to production starting inventories. Validate final catalog bindings after all lab service initialization, since serialized assignments alone cannot prove installers preserve them.

## Runtime and animation integration

Add small salvage components inside `BooterBigArm.TopDown3D.Runtime`, under `Runtime/TopDown3D/Salvage/`:

- `TopDown3DSalvagePoint`: implements `ITopDown3DInteractable`, explicit collider ownership and target epoch. Holds a serialized point ID, one reward, duration and reachable interaction anchor.
- `TopDown3DSalvageWreckState`: owns the three finite part states and restores presentation from that state. State remains independent of renderer visibility or point view reconstruction.
- `CampLoopSalvageSession`: validates the exact scene, active lab readiness and private catalog assignments, registers the reset participant, and enables the wreck only after dependencies are ready.

Names are proposed; keep the responsibilities even if the final file names differ. Avoid extending the natural-rock resource definition or registering a machine as an Ironstone node. The existing world resource registry is typed to Ironstone; this lab slice needs no production registry or save-format migration.

Reuse `TopDown3DGatherActionState` and the existing player action route for one-item-type rewards. The shared gather state captures `TopDown3DInteractionTarget` at begin and checks identity/epoch during advancement and commit; authoring epochs on a point alone is insufficient. A consume callback changes only the retained part bit, with no Unity or observer calls. Presentation reconciles after the transaction through the point's update. Quantity and item appear in the supplied prompt, such as `Strip wiring — 1 Wire Bundle`.

The October 15 interaction repair adds the session component as a whole-hull target alongside the three sockets. It uses the nearest solid wreck surface as its approach point and selects the next available part. Resource-reward cargo preflight now includes salvage, so a nearby Legger is called over before work starts. Distant/inaccessible or full cargo produces feedback without silently awarding Booter resources. Individual socket work, mining, retained state and reset remain supported; see the implementation receipt for repair proof.

Start with the current in-place gather motion through the existing animation driver. It already scales clip speed to the requested action duration. Its use here is a functional placeholder; author evaluation must assess the stretched motion before approving dedicated extraction animation. Inventory mutation timing remains gameplay-owned.

Keep the original mining station and its reward loop. This wreck is a separate gameplay test object, not a duplicate station demonstration. Dedicated pry, wire-pull and joint-extraction clips follow only after the loop works. No animation download, package change or edit of user-owned crouch/foot-projection files belongs to the initial salvage batch.

## Identity and lifecycle

The test scene retains its fixed-world content coordinates and identity; the new wreck belongs to a lab-only feature namespace such as `camp-loop:salvage:v1:authored:01`. Append immutable socket keys such as `armour`, `wiring` and `drive`. Do not derive identity from transform, hierarchy order, Unity instance ID or load order. Reject duplicate wreck/point IDs before enabling interaction.

Terrain streaming is not applicable because the nine native tiles stay loaded. There is no procedural placement in this slice. If a point view is disabled or reconstructed, it reads the retained session owner and cannot replenish its reward. The wreck retains a version, stable ID, removed-part bitmask and epoch as serialized runtime fields across domain reload, alongside existing inventory snapshots. Before rearming, check that aggregate trial quantities in all three inventories match the removed bits. An incompatible record or mismatch disables extraction and preserves storage; it does not silently replenish the machine.

Native terrain, materials and many prefabs are shared with production. Preserve TerrainData bytes and shared asset settings. Ground the wreck through native terrain/collision authority without sculpting the surface. Reserve reachable work positions at all three points and a usable Legger approach outside the hull, camp footprint, return route, rocks, Ironstone and existing station access volumes. Collision must prevent gathering through the opposite hull while allowing each intended exposed approach.

Persisted runtime deltas are intentionally session-only here because the marked playground suppresses disk checkpoints. Leaving Play Mode starts a fresh trial; repeated resets follow the paired reward/state rule below. Later production adoption needs a versioned authored-wreck registry, joint inventory/depletion checkpoint integration and old-save compatibility. Do not graft salvage deltas into the current Ironstone-only checkpoint validation.

## Reset without reward accumulation

Extend the existing playground reset through explicit registration; do not create a second competing reset menu. Cancel pending gather actions and invalidate point epochs before restoring any state. Release movement/animation ownership and suppress input until restoration completes.

Use one salvage reset participant at the station-state stage to prevalidate and transactionally remove all three lab-only item IDs from the explicitly bound kit, cargo and cache, regardless of physical distance or access. Missing or duplicate storage owners block reset. Preserve other items, slot capacity, packing configuration, Ironstone depletion and unrelated station state. Scan actual contents because the player can move, merge or transfer the rewards.

Prepare inventory drafts and validate every owner before commit. The commit condition assigns the pristine bitmask without callbacks or fallible presentation; the existing transaction installs every inventory draft before publishing observers. It does not offer arbitrary world-state rollback. Restore visible parts only after successful commit; a later presentation retry must never replay inventory cleanup or rewards. Failure keeps input blocked with a clear reason. Cancellation during reset yields nothing. Repeated reset, transfer-to-cache then reset, view reconstruction and scene unload must not duplicate rewards or leave action constraints active.

Use the reset registry's existing safe actor-return behavior. The salvage participant never teleports the Legger or globally deletes props. Disposal only unregisters its own participants and subscriptions. Do not write, delete, rename or import any checkpoint file to reset this lab.

## File ownership and authoring

Proposed new content boundaries:

| Surface | Destination |
| --- | --- |
| Runtime components | `Assets/_Project/Scripts/Runtime/TopDown3D/Salvage/` |
| Idempotent authoring and validation | `Assets/_Project/Scripts/Editor/TopDown3D/CampLoopSalvageAuthoring.cs` |
| Wreck and optional private cache prefabs | `Assets/_Project/Prefabs/TopDown3D/Studies/CampLoopSalvage/` |
| Blockout meshes and materials | `Assets/_Project/Art/Environment/Machines/Studies/CampLoopSalvage/` |
| Private items and catalog | `Assets/_Project/Settings/Studies/CampLoopSalvage/` |
| Trial icons | `Assets/_Project/UI/Items/Studies/CampLoopSalvage/` |
| Focused EditMode fixtures | `Assets/_Project/Tests/Editor/CampLoopSalvageTests.cs` |
| Implementation receipt | `Docs/Evidence/Gameplay/CampLoopSalvage20261014/` |

The scene edit adds one owned `Ghost Machine Salvage Study` root and necessary lab-local catalog bindings. Preserve all existing GUIDs and create paired metadata for intentional new assets. No scene recreation or production overlay installer is needed. In particular, do not run `CampLoopTestingSceneBuilder.BuildFromCli` against the existing playground.

The new authoring entry point must inspect existing ownership, refuse ambiguous/mismatched IDs, and avoid overwriting hand edits on rerun. Its validator is read-only. Register new commands in the Unity automation document when they exist. Optional Blender refinement follows the Blender agent's source/handoff contract, retaining `.blend` files locally and importing only Unity runtime outputs.

## Ordered implementation batches

1. **Freeze the candidate and dependencies.** Reinspect current scene/session/animation state, exact catalog initialization and reset transaction APIs. Record hashes of production scene, native TerrainData, shared catalogs/prefabs and packages. Confirm scene ownership before modifying it; preserve concurrent animation work. Resolve required shared-source edits with their owner instead of overwriting dirty files.
2. **Build the wreck blockout.** Author one linked prefab and three exposed anchors. Place it in the existing scene using native queries and exclusion footprints. Gate: all work positions and Legger approach are supported; existing scene validator still passes; protected shared assets remain byte-identical.
3. **Bind the trial economy.** Add three validated items/icons and the private catalog. Bind all lab inventory owners before initialization and preserve starting contents. Gate: all seven item definitions resolve consistently, original items still work, Resource items reject Booter storage and cargo honors stack/mass limits.
4. **Implement extraction.** Connect salvage points to the existing gather transaction, motion and feedback, retaining finite session state and collider ownership. Gate: each socket awards its authored reward once, correct part disappears only on commit, interruptions leave it intact and stripped hull collision remains stable.
5. **Implement paired reset.** Register cancellation/epochs and coordinated trial-item removal plus wreck restoration. Gate: repeated reset, moved/merged/cache-transferred rewards, mid-action reset and reconstruction cannot duplicate items or leak ownership. Disk checkpoint files remain untouched.
6. **Seal the playable test slice.** Run relevant compilation, structural validation and focused EditMode checks in a safely isolated hydrated candidate. Record exact scene/prefab/catalog changes and animation limitations. Commit only verified task-owned files. The author then evaluates the wreck in the playground; polish follows that evidence.

## Verification and acceptance

Automated checks directly relevant to implementation:

- Unique immutable IDs, correct scene/marker gate, idempotent authoring, no missing scripts, required icon/catalog references and production build routing unchanged.
- Three socket approaches with correct line of sight and explicit collider ownership; no collection through the hull or unavailable-point prompt retention.
- Exactly-once completion, repeated input, cancellation, out-of-range, lost visibility, full/heavy cargo and Legger leaving access before completion.
- Target destruction, reset epochs, modal interruption and presentation failure without extra awards or stuck motor/animation ownership.
- Paired inventory/reset restoration across cargo/cache transfers and view/domain reconstruction; failure leaves input blocked and resources unreplenished.
- Marked playground performs zero checkpoint reads/writes through startup, manual/timed save, disable and quit paths.
- Existing Ironstone/cargo transaction regressions and the camp-loop validator retain twelve nodes, one cache, nine terrain pairs, seven camp rocks, one Booter and one Legger.

Scene-opening tests and offscreen captures run only in an isolated background candidate, not the author's active Editor. Follow the [Unity automation workflow](../../Engineering/UNITY_AUTOMATION.md), verify the pinned editor and current target session, and preserve the foreground application. No gameplay smoke test is created or run by the agent under this planning request.

Author-owned acceptance asks whether the machine is recognizable from the normal camera, the three salvage points are easy to approach and distinguish, extraction feels different enough from Ironstone, receipts explain the cargo destination, and stripping/resetting provides useful iteration. Visual approval and Player performance are separate from structural/EditMode proof.

The first build is complete when one wreck supports all three finite extraction actions and safe repeated reset inside the existing playground, with original camp/mining/animation functions preserved and production content unchanged. Wider wreck distribution, richer parts, recipes, specialised tools, companion lifting, ghost lore and production persistence remain later design slices.
