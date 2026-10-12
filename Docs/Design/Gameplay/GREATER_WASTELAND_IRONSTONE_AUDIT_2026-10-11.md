# Greater Wasteland ironstone harvesting audit

Date: October 11, 2026. Owner: Gottspan. Scope: repository and serialized-scene audit; implementation remains a subsequent task.

Greater Wasteland already carries the player-side harvesting foundation. It cannot currently offer ironstone harvesting because it has no resource nodes, placement owner, or resource delta state. Reuse the existing gather and inventory contracts, add a fixed-terrain resource authoring path, and resolve the collider and companion-reference defects before production placement. Start with a bounded area near Booter; whole-world distribution and application-restart persistence are separate slices.

## Verified scene and asset state

The inspected checkout is `main`, tracking `origin/main`, at `efccb41748e37df2e22334965efd2b43a4ee2eca`. It contained unrelated dirty survival, input, UI, material, and rock work. Those files are outside this audit's edit ownership; runtime findings describe the working tree, not a clean release candidate.

The [saved production scene](../../../Assets/_Project/Scenes/Production/GreaterWasteland.unity) contains 8,192 native Terrain components and no prefab source references. Its SHA-256 at inspection is `BD1E6687C586B6EB9B2AA191263B45FB33DF04898E47FA26EB0ABADB36B6E850`. Build Settings enable only this scene. The [previous generated-world playtest scene](../../../Assets/_Project/Scenes/Reference/GeneratedWorld/TopDown3DPrototype.unity) remains disabled.

| Surface | Greater Wasteland | Migration implication |
| --- | --- | --- |
| Interaction controller, player action controller, player inventory | One of each, enabled and wired to Booter | Retain the existing loop. |
| Gather animation | Referenced by the animation driver | Retain the existing in-place gather. |
| Inventory UI controller and interaction feedback HUD | One of each | Retain the existing prompt, receipt, and inventory UI. |
| Companion cargo | One, sharing Booter's item catalog; access range 3.2 m | Restore reliable runtime wiring before automatic harvest routing. |
| Ironstone node components | Zero | Author resource instances through a new fixed-terrain path. |
| Resource world state | Zero | Add one production resource delta owner. |
| Procedural terrain world and game save service | Zero | Preserve these exclusions. |

The generated-world reference has a procedural world, resource state, and game save service. Its ironstone nodes are instantiated at runtime, so it also has no serialized node instances. Copying the player roots cannot copy that resource population.

The [ironstone resource definition](../../../Assets/_Project/Settings/World/Resource_IronstoneNode.asset) currently grants **one ore from one use**, with a **1.1-second gather** and **2.2 m interaction range**. It resolves the `broken-world-outcrop-00` family in the [natural-object catalog](../../../Assets/_Project/Settings/World/TopDown3DNaturalObjectCatalog.asset), with three baked LOD meshes and active/depleted materials. [Ironstone Ore](../../../Assets/_Project/Settings/Items/Item_IronstoneOre.asset) stacks to 99 and has mass metadata 1.25. Booter's referenced item catalog includes this item; a second inventory or catalog is unnecessary.

## Existing gameplay worth retaining

[TopDown3DInteractionController](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DInteractionController.cs) selects nearby available targets by distance, facing, and stable ID, with line-of-sight admission. Current Interact bindings are keyboard E and gamepad west; the HUD resolves live binding labels.

[TopDown3DPlayerActionController](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DPlayerActionController.cs) starts the gather animation and an owner-bound motor constraint. [TopDown3DGatherActionState](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DGatherActionState.cs) checks availability, range, and capacity, then consumes a node as the condition of the inventory transaction. Ordinary consume rejection grants no ore. Cancellation or range exit grants nothing; a full destination leaves the node untouched. No ore pickup or ground-drop system is needed.

[TopDown3DResourceWorldState](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DResourceWorldState.cs) stores remaining uses by stable ID and exposes versioned snapshots. This is a useful mutable-state seam independent of terrain generation. Its snapshot alone is not a disk save or a complete fixed-world identity contract.

## Production gaps and defects

### Fixed terrain placement and authoring

[TopDown3DResourceNodePlanner](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DResourceNodePlanner.cs) requires generated-world settings and `TopDown3DWorldGenerator`. It samples generated height, lithology, bedrock/deposit weights, and traversal corridors. [TopDown3DResourceNodeDecorator](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DResourceNodeDecorator.cs) requires a generated chunk and its decoration root. Neither is a production drop-in for the native terrain.

Use [FixedTerrainSurfaceIndex](../../../Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainSurfaceIndex.cs) for native collision height, holes, seams, and smooth normals. It does not provide geology or route masks. Do not substitute synthetic geology from the old height generator; use explicit authored eligibility and exclusions until native terrain analysis exists.

[TopDown3DIronstoneNode](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DIronstoneNode.cs) stores placement, definition, state, renderers, and collider in nonserialized fields initialized by `Configure`. Adding this component in the Inspector or saving a decorator-created object does not produce a self-configuring scene asset. Add a serialized authored-node adapter or explicit scene installer that reconstructs these links on every load, rejects invalid/duplicate IDs, and creates nodes only after the state owner is ready. An unconfigured node must fail closed: the current `IsAvailable` can be true with a null definition, although zero range prevents normal selection.

### Depletion removes physical collision

The old decorator creates one BoxCollider, passes it as the interaction collider, and creates render-only LOD children. `RefreshPresentation` disables that sole collider when depleted. The rock remains visible but loses its physical obstacle. The existing resource test explicitly expects this disabling; it does not prove collision remains.

This contradicts the retained [system document](IRONSTONE_MINING_AND_INVENTORY_SYSTEM.md), which says depleted geometry keeps its collision envelope. Production nodes need a persistent obstacle collider and separately controlled interaction eligibility, or must retain the collider and filter depleted nodes through availability. Add a test that verifies collision persists while targeting stops.

### Companion routing does not survive scene reload

The action controller's `bigArmCargo` reference is private and nonserialized. `ConfigureCargo` is called only by the prototype scene builder in the inspected source. On reload, `OnEnable` rebuilds the gather state without resolving cargo, and the inventory UI's cargo lookup does not wire the action controller. Greater Wasteland therefore cannot rely on the intended nearby-companion harvest destination.

Resolve cargo explicitly from the same scene or serialize and validate the reference, with initialization that tolerates lifecycle ordering. The existing destination service chooses accessible companion cargo first, otherwise Booter. The gather action locks that destination at start and does not recheck companion access at completion; add completion-time access/capacity validation and deterministic Booter fallback. Preserve exactly-once consumption. The current success receipt omits its destination; retain destination information so the player knows where ore went.

### Restore and targeting need stronger contracts

`ResourceWorldState.ApplySnapshot` replaces deltas without notifying existing nodes. Recreated nodes read restored depletion correctly, but already-live nodes can retain the wrong material and collider state. Restore before constructing nodes or provide a reliable refresh/reset notification, including rollback, before save integration.

Line of sight and facing are checked at target selection. Gather completion rechecks availability and distance, but does not recheck occlusion. If an obstruction appears during the action, gathering can still complete. Define and test completion-time visibility for production.

The interaction scan uses a fixed 24-collider buffer and every layer. Nearby terrain, rocks, camp objects, and companion colliders compete for that buffer. Sorting returned candidates cannot guarantee the nearest valid resource when the buffer saturates. Add an overflow strategy or dedicated interaction filtering with focused dense-area proof before broader dressing. A large formation also needs an accessible interaction point; its bounds center may be out of reach despite proximity to its surface.

### Art experiments are not gameplay nodes

The rock workbench's ironstone seam shader is driven by editor preview material properties. It is separate from the resource node's active/depleted material swap and has no harvest state connection. Preserve the existing outcrop/material pair for the first gameplay slice. If the author selects seam-based formation art, add a runtime presentation adapter and accessible harvest points; do not turn every decorative formation into ore implicitly.

## Proposed implementation sequence

1. **Establish a bounded production resource area.** Add one resource-state root, a validated serialized node/placement path, and a small explicitly placed set of ironstone outcrops near the current Booter area. Ground them against native collision, preserve route and companion clearances, and retain stable obstacle collision. Reuse the existing animation, item catalog, input, inventory, and HUD. Make installation idempotent and additive; do not rebuild Greater Wasteland from the old prototype or template.
2. **Harden harvest completion and companion delivery.** Repair cargo lifecycle wiring, revalidate destination access/capacity and target visibility, expose the destination in receipts, and prove cancellation, saturation, reload, and depletion behavior. Shared input/HUD files currently have another owner's edits; integrate against their accepted state without overwriting them.
3. **Add bounded deterministic distribution on fixed terrain.** Introduce a separate fixed-world resource planner, authored masks and reservations, stable cell ownership, and an explicit resource density per area. Do not transfer `targetPerChunk: 0.35` blindly to native 256 m terrain tiles. Establish a bounded composition area and budget before whole-world population. The saved scene already contains 8,192 terrains; spawning all resource GameObjects synchronously or broadcasting each change to every node has scaling costs that need measurement.
4. **Implement saved-world integration as its own versioned slice.** Save inventory/cargo and resource deltas coherently under a fixed-world identity, reject incompatible or duplicate records before mutation, and refresh live presentation on restore. Do not install the generated-world save service in production. Until this slice exists, state survives node reconstruction only while the same in-memory state owner survives; restarting or reloading the scene resets it.

The first two steps deliver the smallest production harvesting loop. Step three distributes it through a larger world area. Step four makes depletion and earned ore survive application restart. New mining tools, repeated strikes, yield rebalance, respawn, and crafting recipes are outside this audit's proposed baseline.

## World compatibility requirements

Follow the controlling [fixed-terrain dressing plan](FIXED_TERRAIN_DRESSING_PLAN.md); older infinite generated-world standards do not override Greater Wasteland's current terrain authority.

| Concern | Required production contract |
| --- | --- |
| Deterministic world identity | Identify the fixed world using a versioned terrain manifest and seed, with independent resource/catalog versions. Keep the full scene hash as audit evidence rather than a save identity that changes whenever UI or lighting changes. Define terrain-expansion compatibility deliberately. |
| Terrain streaming and unload/reload | Not applicable: terrain is fixed and always loaded by explicit direction. Do not add terrain generation or streaming. If resource presentation is later culled/rebuilt by dressing cells, state must survive outside those objects. |
| Stable object identity | Authored nodes receive immutable unique IDs in the fixed-world namespace. Generated nodes derive IDs from fixed-world identity, resource version, resource definition and absolute cell/candidate. Never use instance IDs, hierarchy order, load order, or transient renderer identity. |
| Authored constraints | Respect terrain holes, slope and support, spawn and companion clearances, protected routes, landmarks, POI footprints, spacing, and physical formation envelopes. No terrain reshaping to fit ore. |
| Persisted runtime deltas | Store remaining uses in one world owner. Reconstruct from immutable placements plus deltas; save those deltas together with rewarded inventory/cargo. Retain depleted obstacle geometry and forbid implicit respawn. |

## Verification and acceptance

This audit used source inspection, serialized scene inventories, asset GUID/reference checks, current Build Settings, and the existing test source. It establishes repository structure and code paths, not fresh Unity compilation, executed tests, gameplay feel, or performance. No Editor launch, window activation, gameplay smoke test, runtime change, or terrain mutation was performed.

For implementation, reuse the existing `TopDown3DInteractionTests`, `TopDown3DResourceNodeTests`, `TopDown3DInventoryTests`, and native-query checks in `FixedTerrainDressingTests`. Add focused proof for serialized node initialization after scene load; duplicate IDs; companion routing after reload and range loss; depletion with retained physical collision; live snapshot restore; blocked completion; crowded scans; and fixed-terrain placement order, boundaries, exclusions, and reconstruction continuity. Existing generated-world determinism tests remain reference regression coverage, not proof of native placement.

Use background-safe Unity import/compilation and focused EditMode execution for the approved implementation. A production ironstone validator must assert resource count/state ownership, ID uniqueness, definition links, player wiring, terrain immutability, and absence of generated terrain/save services. The older prototype validator is not an acceptance gate for Greater Wasteland. Visual evidence belongs in an isolated background validation project; hands-on harvesting and animation acceptance remain author-owned.

The retained system document's production status and no-disk-save statement describe an older generated-world checkpoint. Current code contains a generated-world game save service, but Greater Wasteland has none. Reconcile that document with the accepted production implementation rather than treating its old status as proof that ironstone already works here.
