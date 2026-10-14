# Camp workstations and battery loop — implementation receipt

Implemented October 14, 2026 in the marked, session-only `Assets/_Project/Scenes/Reference/CampLoopTesting.unity` lab. The workbench and dust refinery sit beside the existing storage chest. These are functional primitive blockouts; final models, balance and hands-on acceptance remain separate.

## Player loop

1. The existing 24-slot chest receives exactly **4 Ironstone Ore** once at economy startup, from the battery recipe asset. Reopening UI or re-enabling services does not grant more.
2. Use the workbench beside the chest. **Craft Empty Battery** consumes those four ore atomically and puts one empty battery in a legal Booter supply pocket. A full destination or lost access consumes nothing. The recipe service also supports equipment outputs through the six named equipment slots.
3. Deploy the starter dust canister through the existing G/radial placement flow. Pick it up through the existing pickup flow. Filled canisters return to physically accessible Legger cargo; their dust is not delivered as a loose resource in this lab. Empty canisters may return to Booter's dust equipment slot.
4. Use the refinery, choose the physical source, select its canister or empty battery, and insert each into its socket. **Refine and Charge** converts **10 dust** to one charged Arc Energy battery. The canister, leftover dust and fractional collection remainder remain in the other socket.
5. Choose a legal destination and take the battery/canister. Filled canisters can return to cargo/chest and can redeploy from the exact selected slot. Collection pauses while carried or docked; deployment rebases the collection clock.
6. Use the chest's source/destination controls to transfer equipment, batteries and resources between legal owners. Both chest and cargo require physical range and line of sight. Inspection of an inaccessible source does not permit transfer.

The station modal closes the ordinary inventory, uses the existing EventSystem and Inventory input mode, blocks gameplay/animation actions, and explicitly suspends/resumes through the existing system menu. Held submit/click/cancel controls must release before another action. Lost station access, epoch changes or disabled input close it.

## Ownership, identity and reset

- Inventory slot `InstanceId` remains the canonical equipment identity from the sealed loadout implementation, commit `b086fcabd`. The same ID moves between one carried slot, the placed registry and the refinery socket; presentation has no item authority.
- Canister contents are one explicit versioned value in slot/amount/snapshot/packing records. Opted-in snapshots use version 3; ordinary production version 1 and equipment-only version 2 contracts remain supported. Filled dust contributes to cargo mass. Malformed payloads, wrong carriers, duplicate IDs, overflow and stale transactions reject without consuming items.
- The fixed terrain remains resident. No terrain generation, chunk streaming or generated-object population is involved. Placed devices retain the existing stable placement sequence and production scene GUID identity (`4de5dd018ee194314a00fd369e2d3eeb`). This differs intentionally from the authored checkpoint's `greater-wasteland.authored.v1` identifier. Runtime deltas are retained in lab/domain reload snapshots only; no production disk-save rollout is included.
- Startup prepares all three storage capabilities before installing them together, then seeds ingredients through the existing prepared-world/inventory transaction boundary. Retained missing starter ownership, incompatible stores or invalid device snapshots block automatic grants and preserve the recovery data.
- **Reset Camp Economy** requires explicit second confirmation. It removes trial canisters/batteries, clears refinery/deployed devices, returns the original starter ID empty, and replaces chest recipe ingredients with one recipe's quantities. Other item IDs, cargo ore, salvage state, actor positions and vitals remain. Epoch and placement sequence guards prevent stale commands/reused placement identities.
- The separate full inventory-trial reset adopts its economy baseline after seeding. It refuses while refinery sockets own items, preventing duplicate equipment. Its prepared reset also advances the economy epoch. Animation reset never replenishes the economy.
- Legacy production pickup still returns an ordinary stacked tool plus loose dust. A focused regression exposed JsonUtility's null-to-empty identity conversion; the legacy path now omits equipment identity for ordinary tools rather than rejecting the restored pickup.

## Verification

Pinned Unity **6000.4.0f1**, isolated candidate `D:/BooterBigArmValidation/CampWorkstations20261014-01`; all launches used hidden background processes. The author-owned Editor and foreground application were not controlled.

**177/177 focused EditMode tests pass**, with zero failures or skips, in [workstations-final.xml](workstations-final.xml). The selected fixtures cover recipe/output constraints, atomic socket ownership, 9/10/13-dust boundaries, contents transfer/snapshots/AutoPack/mass, dormant collection, actual authored session initialization/reset/recovery, held gamepad input, menu suspend/resume/access loss, loadout, ordinary inventory/menu/radial UI, legacy authored persistence/pickup, harvesting, salvage conservation and original lab/NPC scene contracts. EditMode fixtures use temporary settings directories and in-memory scene mutations; this is not gameplay smoke testing.

The exact installed additive scene was tested. Repeating `CampWorkstationSceneAuthoring.InstallFromCli` passed the structural validator without changing its saved hash. The source integration adds **12 scene documents**, adds capability overrides/root registration in **4 existing documents**, and preserves the other **1,284 existing documents exactly**, including all nine native terrain/collider pairs, twelve ore nodes, seven rocks, AP01–AP12, salvage and Lager NPC. See [scene-delta-proof.json](scene-delta-proof.json).

[source-proof.json](source-proof.json) matches the installed task files to the candidate. All seven protected production/catalog/cache/package/build/editor files and all nine native TerrainData assets match their captured hashes. No production scene, shared cache prefab, package pin, build routing, local Blender source or terrain source archive was changed.

Six static UI captures were rendered in background Edit Mode and inspected at 1920×1080 normal text and 1280×720 large text. Labels, all 24 storage slots, socket state, controls and notices fit without clipping:

- [Workbench normal](workbench-1920.png) / [large](workbench-1280-large.png)
- [Refinery normal](refinery-1920.png) / [large](refinery-1280-large.png)
- [Chest normal](chest-1920.png) / [large](chest-1280-large.png)

No PlayMode/gameplay smoke test, Player build, performance claim, production persistence migration, push or external publication was performed. Player feel, final scene appearance and gameplay acceptance remain user-owned.

## Audit resolution and authoring

F1 uses the sealed canonical loadout identity/admission and prepared transactions; batteries are supplies, not a seventh equipment slot. F2 adds separate camp dispatch while preserving the animation station receiver. F3 adds typed system-menu suspension and held-control guards. F4 carries exact source slot/revision/ID through placement preview. F5 uses explicit contents version/presence and rejects malformed JSON. F6 rebases deployment time and preserves only collected dust/remainder. F7 validates capabilities and binds session/reset ownership before allowing socket commands.

Owned editor entry points are `CampWorkstationAssetAuthoring.BuildAssetsFromCli`, `CampWorkstationSceneAuthoring.InstallFromCli` / `ValidateFromCli`, and `CampWorkstationVisualProof.CaptureFromCli`. Run them only in an isolated background candidate. The scene installer validates an existing installation without rebuilding it. Export an additive, source-hash-guarded delta for integration; do not replace the source scene with a full older candidate or run the old camp scene builder.

The private battery definitions extend the existing Resources/CampLoopLoadoutProfile, preserving the salvage catalog GUID and bytes. Prototype costs are data-driven assets, not settled production balance. Production promotion remains the separate migration slice in the implementation plan.
