using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DPlayerActionController : MonoBehaviour
    {
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private TopDown3DInteractionController interaction;
        [SerializeField] private TopDown3DPlayerInventory inventory;
        [SerializeField] private TopDown3DPlayerMotor motor;
        [SerializeField] private TopDown3DPlayerAnimationDriver animationDriver;
        [SerializeField] private TopDown3DPlacedHarvesterState harvesterState;
        [SerializeField] private TopDown3DProceduralWorld proceduralWorld;

        private TopDown3DGatherActionState gatherAction;
        [SerializeField] private TopDown3DBigArmCargo bigArmCargo;
        private bool subscribed;
        private GameObject placementPreview;
        private Material placementPreviewMaterial;
        private Vector3 proposedPlacement;
        private bool placementValid;
        private bool placingHarvester;
        private string pickupHarvesterId;
        private TopDown3DHarvesterView pickupHarvesterView;
        private float harvesterActionElapsed;
        private const float HarvesterActionDuration = 0.75f;
        private ITopDown3DInteractable pendingResource;
        private ITopDown3DInteractable pressedTarget;
        private bool hasPressedTarget;
        private float approachSeconds;
        public bool IsAwaitingCargo => pendingResource != null;

        public bool IsGathering => gatherAction != null && gatherAction.IsActive
            || placingHarvester || pickupHarvesterId != null || IsAwaitingCargo;
        public bool IsPlacingHarvester => placementPreview != null;
        public bool CanOpenCargo => input != null && input.Mode == TopDown3DInputMode.Gameplay
            && !IsGathering && !IsPlacingHarvester && TopDown3DCargoAccess.CanAccess(bigArmCargo, transform);
        public string BusyPrompt => IsGathering && input != null
            ? (IsAwaitingCargo ? "Waiting for Legger" : "Working")
                + $"  [{input.GetBindingDisplayName("Gameplay/PickupCanister", "C")}] cancel"
            : string.Empty;
        public string PlacementPrompt => placementPreview != null && input != null
            ? $"[{input.GetBindingDisplayName("Gameplay/DeployCanister", "G")}] place  "
                + $"[{input.GetBindingDisplayName("Gameplay/PickupCanister", "C")}] cancel"
            : string.Empty;
        public event Action<string> FeedbackRequested;
        public event Action<bool> GatheringChanged;
        public event Action PlacementChanged;
        public event Action<TopDown3DBigArmCargo> CargoAccessRequested;
        public event Action<TopDown3DResourceCache> CacheAccessRequested;
        public void ReportFeedback(string message) => FeedbackRequested?.Invoke(message);

        public bool CanSelectCanister => harvesterState != null && harvesterState.Settings != null
            && inventory != null && !IsGathering && HasCanister();

        private bool HasCanister()
        {
            foreach (var slot in inventory.State.Slots)
                if (slot.ItemId == TopDown3DHarvesterSettings.CanisterItemId && slot.Quantity > 0) return true;
            return false;
        }

        public bool SelectCanister()
        {
            if (!CanSelectCanister || input == null || input.Mode != TopDown3DInputMode.Gameplay) return false;
            // Selection begins a preview only. Repeated radial selection must not confirm placement.
            if (placementPreview == null) HandleDeployCanisterRequested();
            return placementPreview != null;
        }

        public void Configure(
            TopDown3DInputRouter inputRouter,
            TopDown3DInteractionController interactionController,
            TopDown3DPlayerInventory playerInventory,
            TopDown3DPlayerMotor playerMotor,
            TopDown3DPlayerAnimationDriver playerAnimation)
        {
            Unsubscribe();
            input = inputRouter;
            interaction = interactionController;
            inventory = playerInventory;
            motor = playerMotor;
            animationDriver = playerAnimation;
            RebuildGatherAction();
            Subscribe();
        }

        public void ConfigureCargo(TopDown3DBigArmCargo cargo)
        {
            bigArmCargo = cargo;
            RebuildGatherAction();
        }

        public void ConfigureHarvester(
            TopDown3DPlacedHarvesterState placedState,
            TopDown3DProceduralWorld world)
        {
            harvesterState = placedState;
            proceduralWorld = world;
        }

        public TopDown3DGatherActionResult TryBeginCurrentTarget()
        {
            if (pendingResource != null) return TopDown3DGatherActionResult.AwaitingCargo;
            if (placementPreview != null) CancelPlacementPreview();
            EnsureGatherAction();
            if (gatherAction == null || interaction == null || motor == null || animationDriver == null)
            {
                return TopDown3DGatherActionResult.Rejected;
            }

            var target = interaction.CurrentTarget;
            return TryBeginTarget(target);
        }

        private TopDown3DGatherActionResult TryBeginTarget(ITopDown3DInteractable target)
        {
            if (target == null || !target.IsAvailable) return TopDown3DGatherActionResult.Rejected;
            if (target is TopDown3DCampView camp)
                return camp.Interact() ? TopDown3DGatherActionResult.Completed : TopDown3DGatherActionResult.Rejected;
            if (target is TopDown3DHarvesterView harvester)
                return TryBeginHarvesterPickup(harvester);
            if (target is TopDown3DResourceCache cache)
            {
                CacheAccessRequested?.Invoke(cache);
                return TopDown3DGatherActionResult.Completed;
            }
            if (target is TopDown3DIronstoneNode ironstone && ironstone.Definition.YieldedItem.IsResource)
            {
                if (bigArmCargo == null || !bigArmCargo.isActiveAndEnabled)
                {
                    ReportFeedback("Legger cargo is unavailable.");
                    return TopDown3DGatherActionResult.Rejected;
                }
                var space = bigArmCargo.State.CheckAdd(target.Reward);
                if (!space.Succeeded)
                {
                    ReportFeedback(space.Code == TopDown3DInventoryResultCode.TooHeavy
                        ? "Legger cannot carry this deposit's weight." : "Legger cargo is full.");
                    return TopDown3DGatherActionResult.InventoryFull;
                }
                if (!TopDown3DCargoAccess.CanAccess(bigArmCargo, transform))
                {
                    var follower = bigArmCargo.GetComponent<TopDown3DBigArmFollower>();
                    if (follower == null || !follower.isActiveAndEnabled
                        || Vector3.Distance(transform.position, bigArmCargo.transform.position) > 24f)
                    {
                        ReportFeedback("Call the Legger closer before mining.");
                        return TopDown3DGatherActionResult.Rejected;
                    }
                    follower.RequestCallOver();
                    pendingResource = target;
                    approachSeconds = 0f;
                    ReportFeedback("Legger approaching — cancel to stop waiting.");
                    return TopDown3DGatherActionResult.AwaitingCargo;
                }
            }
            var result = gatherAction.TryBegin(target, motor.Position);
            if (result == TopDown3DGatherActionResult.InventoryFull || result == TopDown3DGatherActionResult.DestinationUnavailable)
            {
                FeedbackRequested?.Invoke(gatherAction.FailureMessage ?? "Inventory full");
                return result;
            }

            if (result != TopDown3DGatherActionResult.Started)
            {
                return result;
            }

            if (!motor.TryBeginActionConstraint(this)
                || !animationDriver.TryBeginGather(target.ActionDuration))
            {
                gatherAction.Cancel();
                motor.EndActionConstraint(this);
                animationDriver.EndGather();
                return TopDown3DGatherActionResult.Rejected;
            }

            motor.FacePlanarDirection(target.InteractionPoint - motor.Position);
            GatheringChanged?.Invoke(true);
            return result;
        }

        public void CancelGather()
        {
            pressedTarget = null; hasPressedTarget = false;
            pendingResource = null;
            CancelPlacementPreview();
            if (placingHarvester || pickupHarvesterId != null)
            {
                placingHarvester = false;
                pickupHarvesterId = null;
                pickupHarvesterView = null;
                EndPresentation();
            }
            if (gatherAction == null || !gatherAction.IsActive)
            {
                return;
            }

            gatherAction.Cancel();
            EndPresentation();
        }

        private void Update()
        {
            if (pendingResource != null)
            {
                approachSeconds += Time.unscaledDeltaTime;
                var follower = bigArmCargo != null ? bigArmCargo.GetComponent<TopDown3DBigArmFollower>() : null;
                if (approachSeconds > 12f || pendingResource.UnityObject == null || !pendingResource.IsAvailable
                    || interaction == null || !interaction.CanReachTarget(pendingResource)
                    || follower == null || (approachSeconds > 1f && follower.State == TopDown3DBigArmFollower.FollowState.WaitingForRoute))
                {
                    pendingResource = null;
                    ReportFeedback("Mining cancelled — the Legger or deposit cannot be reached.");
                    return;
                }
                if (TopDown3DCargoAccess.CanAccess(bigArmCargo, transform))
                {
                    var preparedTarget = pendingResource;
                    pendingResource = null;
                    TryBeginTarget(preparedTarget);
                }
                return;
            }
            if (placementPreview != null) RefreshPlacementPreview();
            if (placingHarvester || pickupHarvesterId != null)
            {
                AdvanceHarvesterAction();
                return;
            }
            if (gatherAction == null || !gatherAction.IsActive || motor == null)
            {
                return;
            }

            var target = gatherAction.Target;
            var reward = target != null ? target.Reward : default;
            var result = gatherAction.Advance(Time.deltaTime, motor.Position);
            switch (result)
            {
                case TopDown3DGatherActionResult.Running:
                    return;
                case TopDown3DGatherActionResult.Completed:
                    EndPresentation();
                    FeedbackRequested?.Invoke($"+{reward.Quantity} {ResolveRewardName(reward.ItemId)}"
                        + (gatherAction.LastCompletionWasBigArm ? " -> Legger" : " -> Booter"));
                    return;
                case TopDown3DGatherActionResult.InventoryFull:
                case TopDown3DGatherActionResult.DestinationUnavailable:
                    EndPresentation();
                    FeedbackRequested?.Invoke(gatherAction.FailureMessage ?? "Inventory full");
                    return;
                case TopDown3DGatherActionResult.Cancelled:
                case TopDown3DGatherActionResult.CommitFailed:
                case TopDown3DGatherActionResult.Rejected:
                    EndPresentation();
                    return;
            }
        }

        private string ResolveRewardName(string itemId)
        {
            return inventory != null && inventory.ItemCatalog != null
                && inventory.ItemCatalog.TryGetDefinition(itemId, out var definition)
                ? definition.DisplayName
                : itemId;
        }

        private void HandleInteractRequested()
        {
            if (input == null || input.Mode != TopDown3DInputMode.Gameplay || IsGathering) return;
            var target = pressedTarget;
            var captured = hasPressedTarget;
            pressedTarget = null; hasPressedTarget = false;
            if (!captured) { interaction?.RefreshTarget(); TryBeginCurrentTarget(); return; }
            if (target == null || interaction == null || !interaction.CanReachTarget(target)) return;
            if (placementPreview != null) CancelPlacementPreview();
            EnsureGatherAction();
            if (gatherAction != null && motor != null && animationDriver != null) TryBeginTarget(target);
        }

        private void HandleInteractionPressStarted()
        {
            interaction?.RefreshTarget();
            pressedTarget = interaction?.CurrentTarget;
            hasPressedTarget = true;
        }

        private void HandleCargoOpenRequested()
        {
            if (CanOpenCargo) CargoAccessRequested?.Invoke(bigArmCargo);
        }

        private void HandleDeployCanisterRequested()
        {
            if (input == null || input.Mode != TopDown3DInputMode.Gameplay
                || inventory == null || harvesterState == null || (proceduralWorld == null && !harvesterState.IsAuthored)
                || IsGathering) return;
            if (placementPreview == null)
            {
                var hasCanister = false;
                foreach (var slot in inventory.State.Slots)
                    hasCanister |= slot.ItemId == TopDown3DHarvesterSettings.CanisterItemId
                        && slot.Quantity > 0;
                if (!hasCanister)
                {
                    FeedbackRequested?.Invoke("No dust canister in inventory");
                    return;
                }
                placementPreview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                placementPreview.name = "Micro Dust Harvester Placement Preview";
                placementPreview.transform.localScale = new Vector3(0.42f, 0.3f, 0.42f);
                Destroy(placementPreview.GetComponent<Collider>());
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                placementPreviewMaterial = new Material(shader);
                placementPreview.GetComponent<Renderer>().sharedMaterial = placementPreviewMaterial;
                RefreshPlacementPreview();
                PlacementChanged?.Invoke();
                FeedbackRequested?.Invoke(PlacementPrompt);
                return;
            }

            RefreshPlacementPreview();
            if (!placementValid)
            {
                FeedbackRequested?.Invoke("Choose clear, walkable ground");
                return;
            }
            if (!motor.TryBeginActionConstraint(this)
                || !animationDriver.TryBeginGather(HarvesterActionDuration))
            {
                motor.EndActionConstraint(this);
                return;
            }
            placingHarvester = true;
            harvesterActionElapsed = 0f;
            motor.FacePlanarDirection(proposedPlacement - motor.Position);
            CancelPlacementPreview();
            GatheringChanged?.Invoke(true);
        }

        private void HandlePickupCanisterRequested()
        {
            if (input == null || input.Mode != TopDown3DInputMode.Gameplay) return;
            if (IsGathering) { CancelGather(); return; }
            if (placementPreview != null)
            {
                CancelPlacementPreview();
                return;
            }
            var view = interaction?.FindCanisterTarget();
            if (view != null)
                TryBeginHarvesterPickup(view);
            else
                FeedbackRequested?.Invoke("No dust canister in reach");
        }

        private TopDown3DGatherActionResult TryBeginHarvesterPickup(TopDown3DHarvesterView view)
        {
            if (view == null || harvesterState == null || inventory == null
                || motor == null || animationDriver == null || IsGathering
                || !view.IsAvailable
                || Vector3.Distance(motor.Position, view.InteractionPoint) > view.InteractionRange
                || interaction == null || !interaction.CanReachTarget(view))
                return TopDown3DGatherActionResult.Rejected;
            var toolSpace = inventory.State.CheckAdd(new TopDown3DItemAmount(TopDown3DHarvesterSettings.CanisterItemId, 1));
            if (!toolSpace.Succeeded) { ReportFeedback(toolSpace.Message); return TopDown3DGatherActionResult.InventoryFull; }
            if (!harvesterState.TryGet(view.StableId, out var record)) return TopDown3DGatherActionResult.Rejected;
            if (record.Dust > 0)
            {
                if (bigArmCargo == null || !bigArmCargo.isActiveAndEnabled)
                { ReportFeedback("Legger cargo is unavailable; the canister keeps its dust."); return TopDown3DGatherActionResult.DestinationUnavailable; }
                var space = bigArmCargo.State.CheckAdd(new TopDown3DItemAmount(TopDown3DHarvesterSettings.DustItemId, record.Dust));
                if (!space.Succeeded) { ReportFeedback(space.Message); return TopDown3DGatherActionResult.InventoryFull; }
                if (!TopDown3DCargoAccess.CanAccess(bigArmCargo, transform))
                {
                    var follower = bigArmCargo.GetComponent<TopDown3DBigArmFollower>();
                    if (follower == null || !follower.isActiveAndEnabled || Vector3.Distance(transform.position, bigArmCargo.transform.position) > 24f)
                    { ReportFeedback("Call the Legger closer to collect the dust."); return TopDown3DGatherActionResult.DestinationUnavailable; }
                    follower.RequestCallOver(); pendingResource = view; approachSeconds = 0f;
                    ReportFeedback("Legger approaching — cancel to stop waiting.");
                    return TopDown3DGatherActionResult.AwaitingCargo;
                }
            }
            if (!motor.TryBeginActionConstraint(this)
                || !animationDriver.TryBeginGather(HarvesterActionDuration))
            {
                motor.EndActionConstraint(this);
                return TopDown3DGatherActionResult.Rejected;
            }
            pickupHarvesterId = view.StableId;
            pickupHarvesterView = view;
            harvesterActionElapsed = 0f;
            motor.FacePlanarDirection(view.InteractionPoint - motor.Position);
            GatheringChanged?.Invoke(true);
            return TopDown3DGatherActionResult.Started;
        }

        private void AdvanceHarvesterAction()
        {
            harvesterActionElapsed += Mathf.Max(0f, Time.deltaTime);
            if (harvesterActionElapsed < HarvesterActionDuration) return;
            if (placingHarvester)
            {
                placingHarvester = false;
                if (TryFindPlacement(out var current)
                    && Vector3.Distance(current, proposedPlacement) < 0.35f
                    && harvesterState.TryPlace(current, inventory.State, out _))
                    FeedbackRequested?.Invoke("Dust canister deployed");
                else
                    FeedbackRequested?.Invoke("Could not place dust canister");
            }
            else if (pickupHarvesterId != null)
            {
                var id = pickupHarvesterId;
                pickupHarvesterId = null;
                var target = pickupHarvesterView;
                pickupHarvesterView = null;
                if (target != null && target.StableId == id
                    && Vector3.Distance(motor.Position, target.InteractionPoint) <= target.InteractionRange
                    && interaction != null && interaction.CanReachTarget(target)
                    && harvesterState.TryPickup(id, inventory.State, bigArmCargo != null ? bigArmCargo.State : null,
                        out var dust, () => TopDown3DCargoAccess.CanAccess(bigArmCargo, transform)))
                    FeedbackRequested?.Invoke($"Picked up canister with {dust} dust");
                else
                    FeedbackRequested?.Invoke(harvesterState.LastPickupFailure ?? "Cannot pick up canister; check space and range");
            }
            EndPresentation();
        }

        private void RefreshPlacementPreview()
        {
            if (placementPreview == null || motor == null) return;
            placementValid = TryFindPlacement(out proposedPlacement);
            placementPreview.transform.position = placementValid
                ? proposedPlacement + Vector3.up * 0.26f
                : motor.Position + motor.FacingDirection * 1.2f;
            if (placementPreviewMaterial != null)
                placementPreviewMaterial.color = placementValid
                    ? new Color(0.3f, 0.7f, 0.45f, 0.75f)
                    : new Color(0.8f, 0.2f, 0.16f, 0.75f);
        }

        private bool TryFindPlacement(out Vector3 position)
        {
            position = default;
            if (motor == null || harvesterState?.Settings == null
                || (proceduralWorld == null && !harvesterState.IsAuthored))
                return false;
            var facing = Vector3.ProjectOnPlane(motor.FacingDirection, Vector3.up).normalized;
            if (facing.sqrMagnitude < 0.01f) return false;
            var center = motor.Position + facing * harvesterState.Settings.PlacementDistance;
            var hits = Physics.RaycastAll(center + Vector3.up * 3f, Vector3.down, 7f,
                ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponent<TopDown3DGroundSurface>() == null
                    || Vector3.Angle(hit.normal, Vector3.up) > harvesterState.Settings.MaximumSlopeDegrees
                    || (proceduralWorld != null && !proceduralWorld.TryGetLoadedChunkAt(hit.point, out _))) continue;
                var overlaps = Physics.OverlapSphere(hit.point + Vector3.up * 0.25f,
                    0.43f, ~0, QueryTriggerInteraction.Collide);
                var clear = true;
                foreach (var overlap in overlaps)
                {
                    if (overlap.GetComponent<TopDown3DGroundSurface>() != null
                        || overlap.transform.IsChildOf(transform)) continue;
                    clear = false;
                    break;
                }
                if (!clear) continue;
                position = hit.point;
                return true;
            }
            return false;
        }

        private void CancelPlacementPreview()
        {
            if (placementPreview == null) return;
            Destroy(placementPreview);
            if (placementPreviewMaterial != null) Destroy(placementPreviewMaterial);
            placementPreview = null;
            placementPreviewMaterial = null;
            placementValid = false;
            PlacementChanged?.Invoke();
        }

        private void EnsureGatherAction()
        {
            if (gatherAction == null && inventory != null)
            {
                RebuildGatherAction();
            }
        }

        private void RebuildGatherAction()
        {
            if (inventory == null)
            {
                gatherAction = null;
                return;
            }

            if (bigArmCargo == null || bigArmCargo.gameObject.scene != gameObject.scene)
                bigArmCargo = TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmCargo>(gameObject.scene);
            var destinations = new TopDown3DItemDestinationService(
                inventory.State,
                bigArmCargo != null ? bigArmCargo.State : null,
                () => TopDown3DCargoAccess.CanAccess(bigArmCargo, transform));
            gatherAction = new TopDown3DGatherActionState(destinations,
                target => interaction != null && interaction.CanReachTarget(target));
        }

        private void EndPresentation()
        {
            animationDriver?.EndGather();
            motor?.EndActionConstraint(this);
            GatheringChanged?.Invoke(false);
        }

        private void Subscribe()
        {
            if (subscribed || !isActiveAndEnabled || input == null)
            {
                return;
            }

            input.InteractRequested += HandleInteractRequested;
            input.InteractionPressStarted += HandleInteractionPressStarted;
            input.CargoOpenRequested += HandleCargoOpenRequested;
            input.ItemActionCancelRequested += CancelGather;
            input.DeployCanisterRequested += HandleDeployCanisterRequested;
            input.PickupCanisterRequested += HandlePickupCanisterRequested;
            input.ModeChanged += HandleInputModeChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (subscribed && input != null)
            {
                input.InteractRequested -= HandleInteractRequested;
                input.InteractionPressStarted -= HandleInteractionPressStarted;
                input.CargoOpenRequested -= HandleCargoOpenRequested;
                input.ItemActionCancelRequested -= CancelGather;
                input.DeployCanisterRequested -= HandleDeployCanisterRequested;
                input.PickupCanisterRequested -= HandlePickupCanisterRequested;
                input.ModeChanged -= HandleInputModeChanged;
            }

            subscribed = false;
        }

        private void OnEnable()
        {
            EnsureGatherAction();
            Subscribe();
        }

        private void Start()
        {
            // OnEnable/OnValidate can run before the scene's cargo is discoverable.
            // Rebuild once after scene loading so actions share the live cargo owner.
            if (gatherAction == null || !gatherAction.IsActive) RebuildGatherAction();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureGatherAction();
        }
#endif

        private void OnDisable()
        {
            Unsubscribe();
            CancelGather();
            motor?.EndActionConstraint(this);
            animationDriver?.EndGather();
        }

        private void HandleInputModeChanged(TopDown3DInputMode mode)
        {
            if (mode != TopDown3DInputMode.Gameplay)
            {
                pressedTarget = null; hasPressedTarget = false;
                CancelGather();
            }
        }
    }
}
