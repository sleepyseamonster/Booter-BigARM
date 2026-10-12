# Greater Wasteland ironstone implementation

Date: October 11, 2026. Owner: Gottspan, with the Babineaux Unity bridge workflow and Gear Ball commit boundary. Scope: the first bounded production harvesting area and the shared harvest correctness fixes identified by the [audit](../../Design/Gameplay/GREATER_WASTELAND_IRONSTONE_AUDIT_2026-10-11.md).

## Player behavior

Greater Wasteland now installs twelve harvestable Ironstone outcrops near the saved Booter position when the scene loads. Approach a visible outcrop and press Interact (keyboard E or gamepad west). Booter faces it and performs the existing in-place 1.1-second gather. Each node grants one Ironstone Ore once; ore stacks to 99. Nearby active Legger cargo receives the ore when it has room, otherwise Booter receives it. The success receipt names the destination.

The action rechecks availability, distance, visibility, cargo access and capacity before committing. Cancellation, a new obstruction, loss of target, or both inventories filling cannot consume the node or award ore. A depleted outcrop changes material, remains a physical obstacle, and stops offering interaction. Direct cargo delivery refreshes the Legger's load profile and change notification once.

## Production integration

- [GreaterWastelandIronstoneInstaller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/GreaterWastelandIronstoneInstaller.cs) installs the Resources area prefab only into a scene named `GreaterWasteland`. Scene-load and startup callbacks are idempotent. GameplaySetup and the generated-world reference receive no overlay.
- [GreaterWastelandIronstone.prefab](../../../Assets/_Project/Prefabs/TopDown3D/Resources/GreaterWastelandIronstone.prefab) contains one resource-state owner and twelve nested instances of the reusable [IronstoneNode prefab](../../../Assets/_Project/Prefabs/TopDown3D/IronstoneNode.prefab). Each uses the existing outcrop mesh family, three LODs, materials, item and animation contracts.
- [TopDown3DAuthoredIronstoneNode](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DAuthoredIronstoneNode.cs) retains serialized identity, definition, state, renderer and obstacle links and configures the gameplay node on enable. Invalid links and duplicate live IDs fail closed.
- [GreaterWastelandIronstoneAuthoring](../../../Assets/_Project/Scripts/Editor/TopDown3D/GreaterWastelandIronstoneAuthoring.cs) samples the saved native collision surface and authors the area through a temporary additive scene. It never saves Greater Wasteland or writes TerrainData. Repeated authoring validates accepted assets without overwriting them.

The authored candidates lie roughly 9 to 62 metres from the saved Booter center, with at most 6 metres of deterministic fitting adjustment. Admission checks native holes and slope, four footprint support samples, physical overlaps, at least 7 metres between nodes, a central traversal corridor, and player/companion clearance. This is an explicitly authored starter area, not a geology-based world scatter or whole-world population system. The ironstone seam art experiment remains separate.

## Identity and persistence

The immutable IDs are `greater-wasteland:ironstone:v1:authored:01` through `12`; the area resource-generation version is 1. IDs are independent of hierarchy order, renderer lifetime, terrain expansion order, Unity instance IDs and scene-load timing. These are authored placements, so a random seed does not select or reshuffle this slice. A future generated distribution requires the fixed-world identity and independent resource-version contract from the audit.

Terrain streaming is not applicable: Greater Wasteland remains fixed and always loaded. Node views can be reconstructed from their authored links plus the same resource-state owner. Versioned resource snapshot application now refreshes already-live presentation in both pristine and depleted directions.

There is **no application-restart persistence in this slice**. Scene reload or application restart creates a fresh resource owner and fresh inventory. Future fixed-world save integration must persist rewarded inventory/cargo and remaining node uses together. No generated-world terrain or save service was installed into production, and no respawn policy was introduced.

## Verification

Pinned Unity `6000.4.0f1` compiled and imported the implementation in a task-owned background candidate at `D:\BooterBigArmValidation\Ironstone20261011-01`. The candidate copies scripts, definitions, art and small reference scenes, and omits production terrain source assets. It does not share writable source files, caches or project locks with the production Editor.

The final focused EditMode run passed **72/72**, with zero failures or skips, across `GreaterWastelandIronstoneTests`, `TopDown3DInteractionTests`, `TopDown3DResourceNodeTests`, `TopDown3DInventoryTests`, `TopDown3DPackingTests`, `TopDown3DInventoryUiTests`, `TopDown3DPlayerLocomotionTests`, `TopDown3DSmartTraversalTests`, and `FixedTerrainDressingTests`. The local NUnit XML and Editor log are `Logs/ironstone-acceptance.xml` and `Logs/ironstone-acceptance.log` inside that candidate; the Editor exited 0. Earlier runs found an additive-scene test-host setup error, corrected before the accepted run without weakening any gameplay assertions.

New proof covers serialized scene links, duplicate identity rejection, production-only/idempotent installation of all twelve prefab nodes, retained collision after depletion, view reconstruction, live snapshot presentation restoration, cargo access/capacity changes, exactly-once completion, both destinations filling, crowded target scanning, new obstruction cancellation, cargo resolution after enabling, and direct cargo load-profile updates. Existing tests regress native queries, inventory/UI, generated reference resources, locomotion and traversal. The old depletion assertions now require the outcrop's physical collider to remain enabled.

Background validation in the existing production Editor accepted the area prefab's twelve unique identities, one state owner, native grounding against **8,192 terrains**, colliders, LODs, definitions and existing player wiring. A second authoring call returned `Ironstone already installed and validated; no changes.`

The saved production scene retained SHA-256 `BD1E6687C586B6EB9B2AA191263B45FB33DF04898E47FA26EB0ABADB36B6E850`, matching the audit baseline. The task changed no native terrain assets, scene, input bindings, package pins, renderer settings, humanoid clip or existing art. Concurrent algae, waypoint, UI, material and terrain work stays separately owned.

No Play Mode replay, hands-on smoke test, foreground activation, new Player build, or rendered appearance/performance acceptance was performed. The author owns the first hands-on harvest and animation review. The resource overlay appears on the next production scene load/Play session; it is not saved into the terrain scene's Edit Mode hierarchy.
