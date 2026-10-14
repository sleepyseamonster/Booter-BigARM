# Camp Loop inventory implementation

October 14, 2026. The session-only Camp Loop playground now uses six equipment slots and four supply pockets, with physically accessible Legger cargo kept separate. The [inventory audit](../../../Design/Gameplay/CAMP_LOOP_INVENTORY_REDESIGN_BRAINSTORM_2026-10-14.md) records the design origin.

## Equipment and supplies

The six named equipment addresses are Dust canister, Algae canister, Grappling hook, Digging tool, Cloak and Jacket. Each accepts only its matching equipment type and at most one instance. Four separate pockets accept explicitly classified field supplies; the packed popup camp occupies the first pocket. Resources are rejected in every Booter address. Equipment spares use permitted Legger cargo.

Equipment and supplies are regions of one transaction authority rather than independent copies of the kit. Slot policy enforces eligibility during acquisition, movement, transfer and restore. Equipment instances retain identity through drafts, snapshots, selected-slot transfers, swaps and Auto-Pack; they cannot merge. Cargo replacements exchange with the fitted piece atomically, including capacity/mass and physical-access checks.

The algae tile reads the existing living reserve. It does not create a stack, refill on UI rebuild or restore Reserve. The prototype attachment cannot be removed; absence or inventory recovery blocks consumption. Cloak presentation follows the fitted item, using the existing worn and blanket assets without duplicating ownership. Camp recovery tuning is unchanged.

Grapple, jacket and digging-tool definitions establish inventory ownership and original code-drawn glyphs. They do not add grapple traversal, clothing protection/durability, a new mining prerequisite or combat actions. The digging tool is the starter combined shovel/axe. Those behaviors and dedicated 3D equipment art remain separate features.

## Activation and initialization

The [private loadout profile](../../../../Assets/_Project/Settings/Items/CampLoopLoadout/Resources/CampLoopLoadoutProfile.asset) requires the existing Camp Loop animation-playground marker and targets the private salvage catalog. It supplies transient definition overrides and five additional starter equipment records, preserving the serialized catalog reference and its existing resources. No source scene or serialized base catalog was changed by this task.

The scene's serialized kit capacity remains 16; its effective marked-playground capacity is 10. Production retains its generic kit, original tool stacking and version-1 snapshot contract. Loadout equipment records use version 2, and deployed records carrying equipment identity use a corresponding version-2 format restricted to the lab. This does not migrate production disk checkpoints.

Unity can request inventory state before the scene marker is discoverable. Unconfigured UI now waits for its explicit configuration before subscribing. A startup barrier can normalize only an untouched legacy starter grant, preserving the authority object, subscribers and any existing instance IDs. Changed, used or incompatible retained inventories require recovery; the barrier never truncates them or remotely moves overflow to cargo. Rebuilding UI reconnects both slot regions by their stable authored names.

## Dust canister ownership

Deployment removes the equipped instance and records its identity with the deployed device. The named slot stays reserved; a different canister cannot fill it. Recovery returns the same instance and delivers collected dust directly to physically accessible cargo. Failed space/access checks preserve the device and its contents.

This slice retains the existing direct dust delivery behavior. The separately owned workstation/refinery extension will subsequently add carried canister contents to these same records. It must preserve the inventory hooks and perform its own integrated validation; this receipt does not claim that filled-canister docking is installed.

## Explicit inventory trial reset

Inventory offers RESET INVENTORY TRIAL, followed by a second confirmation. Back cancels confirmation before leaving the screen. This action is separate from Animation Playground reset.

The baseline includes kit, cargo, cache, resource depletion, deployed devices, algae and salvage removal state. Reset prepares all inventory and world records, rechecks state under locks, installs logical ownership before notifications and guards observer reentry. Failed preparation or stale state leaves contents unchanged. Actor positions, vitals, authored Starting Camp and animation fixtures are preserved. Restoring a resource source also restores its trial rewards together; there is no accumulating salvage reward loop.

The baseline is retained through reconstruction rather than captured again from an already-used trial. Device placement sequences remain monotonic across resets so old placement addresses cannot name a newly deployed device. `ExternalResetBlockReason` lets a later socket/economy owner block the inventory reset before enabling external equipment ownership; docked gear must be included in that owner's coordinated reset.

## Verification

Pinned Unity 6000.4.0f1 imported and compiled the isolated candidate at `D:/BooterBigArmValidation/CampLoadout20261014`. The final combined model and scene run passed **113/113 EditMode checks**, zero failures or skips. [Test results](loadout-release.xml) preserve the executed proof.

The run covers the new loadout fixture plus existing inventory state, inventory loop/UI, micro dust harvester, algae, salvage, Camp Loop scene, camp and radial fixtures. It includes wrong-slot/resource rejection, exact equipment identity, full/access-lost transfers, coherent swaps, duplicate snapshot rejection, fixed algae, world reset locking, source-scene activation, same-device deployment/recovery, replacement rejection while deployed, two-step trial reset, UI reconstruction/navigation and legacy production snapshots. The candidate includes the independently committed additive NPC scene integration; no NPC files are owned by this inventory batch.

The [loadout validator](../../../../Assets/_Project/Scripts/Editor/TopDown3D/CampLoopLoadoutValidator.cs) passed private-profile, six-slot, supply-placement and unchanged production-contract checks. Background-rendered UI fixtures were inspected at [1920 by 1080](loadout-1920.png) and [1280 by 720](loadout-1280.png). Their sample algae/cargo values demonstrate the view, not a live gameplay capture. Graphics pipeline changes used for capture existed only in the isolated process and were restored without saving settings.

Early candidate runs identified missing fixture assets, premature UI subscription and capture setup defects. Those runs are not passing evidence. The final results and captures use the corrected candidate.

No gameplay smoke test, Play Mode trial, Player build, subjective controller acceptance or production save migration is claimed. No app was focused, no package version or shared terrain was changed, and no push is authorized. Unrelated scene, animation, atmosphere, NPC, riding and workstation work remains separately owned.
