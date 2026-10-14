using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DefaultExecutionOrder(-8000), DisallowMultipleComponent]
    public sealed class TopDown3DCampLoopEconomySession : MonoBehaviour
    {
        [SerializeField] private TopDown3DPlayerInventory kit;
        [SerializeField] private TopDown3DBigArmCargo cargo;
        [SerializeField] private TopDown3DResourceCache cache;
        [SerializeField] private TopDown3DCampWorkbench workbench;
        [SerializeField] private TopDown3DDustRefinery refinery;
        [SerializeField, HideInInspector] private bool initialized;
        [SerializeField, HideInInspector] private uint epoch;
        [SerializeField, HideInInspector] private string starterCanisterId;
        private bool configured, locked, recovery;
        private TopDown3DPlacedHarvesterState devices;
        private TopDown3DInputRouter input;
        private TopDown3DPlayerActionController action;
        private TopDown3DCampLoopInventoryTrial trial;
        public TopDown3DPlayerInventory Kit => kit;
        public TopDown3DBigArmCargo Cargo => cargo;
        public TopDown3DResourceCache Cache => cache;
        public TopDown3DCampWorkbench Workbench => workbench;
        public TopDown3DDustRefinery Refinery => refinery;
        public TopDown3DPlacedHarvesterState Devices => devices;
        public TopDown3DInputRouter Input => input;
        public TopDown3DPlayerActionController Action => action;
        public TopDown3DCampWorkstationUiController Ui { get; private set; }
        public bool Ready => initialized && configured && !recovery && isActiveAndEnabled
            && TopDown3DAnimationPlaygroundSession.Find(gameObject.scene)?.PreviewReady == true;
        public uint Epoch => epoch;
        public string FailureReason { get; private set; }

        internal void Configure(TopDown3DPlayerInventory player, TopDown3DBigArmCargo carrier, TopDown3DResourceCache storage,
            TopDown3DCampWorkbench bench, TopDown3DDustRefinery station)
        { kit = player; cargo = carrier; cache = storage; workbench = bench; refinery = station; }
        private void Start() => TryInitializeWhenReady();
        private void Update() { if (!configured && !recovery) TryInitializeWhenReady(); }
        private void TryInitializeWhenReady()
        {
            var lab = TopDown3DAnimationPlaygroundSession.Find(gameObject.scene);
            if (lab == null || !lab.PreviewReady) return;
            try { Initialize(); } catch (Exception error) { Fail(error.Message); }
        }
        internal bool Initialize()
        {
            if (configured) return Ready;
            if (!GreaterWastelandSceneScope.IsCampLoop(gameObject.scene) || !TopDown3DAnimationPlaygroundSession.IsSessionOnly(gameObject.scene)
                || kit == null || cargo == null || cache == null || workbench == null || refinery == null
                || workbench.Recipe == null || new Component[] { kit, cargo, cache, workbench, refinery }.Any(c => c.gameObject.scene != gameObject.scene))
                return Fail("Camp workstation bindings are incomplete; inventories were preserved.");
            devices = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlacedHarvesterState>(gameObject.scene);
            input = kit.GetComponent<TopDown3DPlayerMotor>()?.InputRouter;
            if (input == null) input = TopDown3DGameHudCanvas.FindInScene<TopDown3DInputRouter>(gameObject.scene);
            action = kit.GetComponent<TopDown3DPlayerActionController>();
            // Resolve the shared private profile before adapting authorities initialized during early Awake.
            var catalog = kit.ItemCatalog;
            if (devices == null || input == null || action == null || !devices.IsAuthored || devices.HasRetainedRecovery
                || devices.Settings == null || devices.Settings.Capacity != 100 || devices.Settings.SecondsPerUnit != 10
                || workbench.Chest != cache || !workbench.Recipe.TryValidate(kit.ItemCatalog, out _)
                || TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene) == null
                || TopDown3DGameHudCanvas.FindInScene<TopDown3DInventoryUiController>(gameObject.scene)?.EventSystem == null)
                return Fail($"Camp contents configuration is incompatible; automatic grants are disabled. Devices: {devices != null}/{devices?.IsAuthored}, input: {input != null}, action: {action != null}, linked chest: {workbench.Chest == cache}, recipe: {workbench.Recipe.TryValidate(kit.ItemCatalog, out _)}, menu: {TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene) != null}, events: {TopDown3DGameHudCanvas.FindInScene<TopDown3DInventoryUiController>(gameObject.scene)?.EventSystem != null}.");
            var activation = new[] { kit.State.PrepareCanisterContentsAtStartup(), cargo.State.PrepareCanisterContentsAtStartup(),
                cache.State.PrepareCanisterContentsAtStartup(true) };
            if (activation.Any(a => a == null) || !TopDown3DInventoryTransaction.Commit(Array.Empty<TopDown3DInventoryTransaction.Mutation>(),
                () => isActiveAndEnabled, activation).Succeeded || !ValidateOwnership())
                return Fail("Camp contents could not activate safely; existing storage was preserved.");
            devices.ConfigureCanisterContents(true);
            var fresh = !initialized;
            if (!initialized)
            {
                var starter = kit.State.Slots.FirstOrDefault(s => s.ItemId == TopDown3DHarvesterSettings.CanisterItemId);
                if (starter == null || !starter.CanisterContents.IsValid() || starter.CanisterContents.dust != 0 || devices.DeployedCount != 0
                    || refinery.CaptureState().HasCanister || refinery.CaptureState().HasBattery)
                    return Fail("A new economy session requires one packed starter canister and empty refinery sockets.");
                var mutation = new TopDown3DInventoryTransaction.Mutation(cache.State);
                foreach (var ingredient in workbench.Recipe.Ingredients)
                {
                    var added = mutation.Draft.TryAdd(new TopDown3DItemAmount(ingredient.itemId, ingredient.quantity));
                    if (!added.Succeeded) return Fail(added.Message);
                }
                var id = starter.InstanceId;
                var grant = new TopDown3DPreparedWorldMutation(
                    () => { if (locked || initialized) return false; locked = true; return true; },
                    () => { starterCanisterId = id; initialized = true; }, () => { }, () => locked = false);
                var result = TopDown3DInventoryTransaction.Commit(new[] { mutation }, () => isActiveAndEnabled, new[] { grant });
                if (!result.Succeeded) return Fail(result.Message);
            }
            if (!Guid.TryParseExact(starterCanisterId, "N", out _) || !ValidateOwnership()) return Fail("Retained camp ownership is invalid; contents were preserved.");
            configured = true;
            if (Ui == null)
            {
                var uiObject = new GameObject("Camp Workstation UI", typeof(RectTransform), typeof(TopDown3DCampWorkstationCanvas));
                uiObject.transform.SetParent(transform, false); Ui = uiObject.AddComponent<TopDown3DCampWorkstationUiController>();
            }
            Ui.Configure(this);
            trial = kit.GetComponent<TopDown3DCampLoopInventoryTrial>();
            if (trial != null)
            {
                if (fresh) trial.AdoptEconomyBaseline(kit.CaptureSnapshot(), cargo.State.CaptureSnapshot(), cache.State.CaptureSnapshot());
                trial.ExternalResetBlockReason = () => !Ready ? "Camp economy requires recovery before resetting."
                    : refinery.CaptureState().HasCanister || refinery.CaptureState().HasBattery
                        ? "Retrieve refinery items before resetting the inventory trial." : null;
                trial.ExternalResetMutation = PrepareEpochAdvance;
            }
            return true;
        }

        public bool ValidateOwnership()
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var state in new[] { kit.State, cargo.State, cache.State })
                foreach (var slot in state.Slots)
                {
                    if (slot.IsEmpty) continue;
                    if (!string.IsNullOrEmpty(slot.InstanceId) && !identities.Add(slot.InstanceId)) return false;
                    if (slot.ItemId == TopDown3DHarvesterSettings.CanisterItemId && !slot.CanisterContents.IsValid()) return false;
                }
            if (devices != null) foreach (var placed in devices.CaptureSnapshot().Records)
                if (!string.IsNullOrEmpty(placed.EquipmentInstanceId) && !identities.Add(placed.EquipmentInstanceId)) return false;
            var station = refinery.CaptureState();
            if (!station.IsValid || station.HasCanister && !identities.Add(station.canisterInstanceId)) return false;
            return !initialized || identities.Contains(starterCanisterId);
        }

        public bool CanAccessOwner(int owner)
        {
            if (!Ready) return false;
            return owner == 0 || owner == 1 && TopDown3DCargoAccess.CanAccess(cargo, kit.transform)
                || owner == 2 && cache.CanAccess(kit.transform);
        }
        public TopDown3DInventoryState Owner(int owner) => owner switch { 0 => kit.State, 1 => cargo.State, 2 => cache.State, _ => null };
        public static string OwnerLabel(int owner) => owner switch { 0 => "BOOTER", 1 => "LEGGER", 2 => "CAMP CHEST", _ => "UNAVAILABLE" };

        public TopDown3DInventoryResult ResetEconomy(uint expectedEpoch)
        {
            if (!Ready || expectedEpoch != epoch || epoch == uint.MaxValue || input.Mode != TopDown3DInputMode.Inventory || locked
                || !ValidateOwnership()) return TopDown3DCraftingService.Failure("Camp reset is unavailable; current contents were preserved.");
            var camp = kit.GetComponent<TopDown3DCampController>();
            if (action.IsGathering || action.IsPlacingHarvester || camp != null && camp.Phase != TopDown3DCampPhase.Idle)
                return TopDown3DCraftingService.Failure("Finish the current activity before resetting.");
            var mutations = new[] { new TopDown3DInventoryTransaction.Mutation(kit.State),
                new TopDown3DInventoryTransaction.Mutation(cargo.State), new TopDown3DInventoryTransaction.Mutation(cache.State) };
            foreach (var mutation in mutations)
                for (var i = 0; i < mutation.Draft.Capacity; i++)
                {
                    var slot = mutation.Draft.Slots[i];
                    if (slot.IsEmpty || slot.ItemId != TopDown3DHarvesterSettings.CanisterItemId
                        && slot.ItemId != TopDown3DDustRefineryState.EmptyBatteryId && slot.ItemId != TopDown3DDustRefineryState.ChargedBatteryId) continue;
                    var removed = mutation.Draft.RemoveFromSlot(i, slot.Quantity); if (!removed.Succeeded) return removed;
                }
            var kitDraft = mutations[0].Draft; kitDraft.EquipmentAdmission = null;
            var starter = kitDraft.TryAdd(new TopDown3DItemAmount(TopDown3DHarvesterSettings.CanisterItemId, 1, starterCanisterId, TopDown3DCanisterContents.Create()));
            if (!starter.Succeeded) return starter;
            foreach (var ingredient in workbench.Recipe.Ingredients)
            {
                var count = mutations[2].Draft.Slots.Where(s => s.ItemId == ingredient.itemId).Sum(s => s.Quantity);
                if (count > 0 && !mutations[2].Draft.TryRemove(ingredient.itemId, count).Succeeded) return TopDown3DCraftingService.Failure("Chest contents changed.");
                var added = mutations[2].Draft.TryAdd(new TopDown3DItemAmount(ingredient.itemId, ingredient.quantity)); if (!added.Succeeded) return added;
            }
            var current = devices.CaptureSnapshot();
            var world = devices.PrepareTrialRestore(TopDown3DPlacedHarvesterSnapshot.Create(0, current.SimulationSeconds, current.NextSequence,
                Array.Empty<TopDown3DPlacedHarvesterRecord>(), current.AuthoredWorldId));
            if (world == null) return TopDown3DCraftingService.Failure("Device state cannot reset safely.");
            var nextEpoch = epoch + 1;
            var reset = new TopDown3DPreparedWorldMutation(
                () => { if (locked || epoch != expectedEpoch) return false; locked = true; return true; },
                () => epoch = nextEpoch, () => { }, () => locked = false);
            return TopDown3DInventoryTransaction.Commit(mutations, () => Ready && input.Mode == TopDown3DInputMode.Inventory,
                new[] { world, refinery.Prepare(new TopDown3DDustRefineryState()), reset });
        }
        private bool Fail(string reason) { recovery = true; FailureReason = reason; Debug.LogWarning(reason, this); return false; }
        private TopDown3DPreparedWorldMutation PrepareEpochAdvance()
        {
            if (epoch == uint.MaxValue) return null;
            var expected = epoch;
            return new TopDown3DPreparedWorldMutation(() => { if (locked || epoch != expected) return false; locked = true; return true; },
                () => epoch++, () => { }, () => locked = false);
        }
        private void OnDisable() { Ui?.Teardown(); configured = false; }
    }
}
