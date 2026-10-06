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
        private TopDown3DBigArmCargo bigArmCargo;
        private bool subscribed;
        private GameObject placementPreview;
        private Material placementPreviewMaterial;
        private Vector3 proposedPlacement;
        private bool placementValid;
        private bool placingHarvester;
        private string pickupHarvesterId;
        private float harvesterActionElapsed;
        private const float HarvesterActionDuration = 0.75f;

        public bool IsGathering => gatherAction != null && gatherAction.IsActive
            || placingHarvester || pickupHarvesterId != null;
        public bool IsPlacingHarvester => placementPreview != null;
        public string PlacementPrompt => placementPreview != null && input != null
            ? $"[{input.GetBindingDisplayName("Gameplay/DeployCanister", "G")}] place  "
                + $"[{input.GetBindingDisplayName("Gameplay/PickupCanister", "C")}] cancel"
            : string.Empty;
        public event Action<string> FeedbackRequested;
        public event Action<bool> GatheringChanged;
        public event Action PlacementChanged;
        public event Action<TopDown3DBigArmCargo> CargoAccessRequested;

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
            if (placementPreview != null) CancelPlacementPreview();
            EnsureGatherAction();
            if (gatherAction == null || interaction == null || motor == null || animationDriver == null)
            {
                return TopDown3DGatherActionResult.Rejected;
            }

            var target = interaction.CurrentTarget;
            if (target is TopDown3DHarvesterView harvester)
                return TryBeginHarvesterPickup(harvester);
            if (target is TopDown3DBigArmCargoAccess)
            {
                CargoAccessRequested?.Invoke(((TopDown3DBigArmCargoAccess)target).Cargo);
                return TopDown3DGatherActionResult.Completed;
            }
            var result = gatherAction.TryBegin(target, motor.Position);
            if (result == TopDown3DGatherActionResult.InventoryFull)
            {
                FeedbackRequested?.Invoke("Inventory full");
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
            CancelPlacementPreview();
            if (placingHarvester || pickupHarvesterId != null)
            {
                placingHarvester = false;
                pickupHarvesterId = null;
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
                    FeedbackRequested?.Invoke($"+{reward.Quantity} {ResolveRewardName(reward.ItemId)}");
                    return;
                case TopDown3DGatherActionResult.InventoryFull:
                    EndPresentation();
                    FeedbackRequested?.Invoke("Inventory full");
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
            TryBeginCurrentTarget();
        }

        private void HandleDeployCanisterRequested()
        {
            if (input == null || input.Mode != TopDown3DInputMode.Gameplay
                || inventory == null || harvesterState == null || proceduralWorld == null
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
            if (placementPreview != null)
            {
                CancelPlacementPreview();
                return;
            }
            if (interaction?.CurrentTarget is TopDown3DHarvesterView view)
                TryBeginHarvesterPickup(view);
            else
                FeedbackRequested?.Invoke("No dust canister in reach");
        }

        private TopDown3DGatherActionResult TryBeginHarvesterPickup(TopDown3DHarvesterView view)
        {
            if (view == null || harvesterState == null || inventory == null
                || motor == null || animationDriver == null || IsGathering
                || !view.IsAvailable
                || Vector3.Distance(motor.Position, view.InteractionPoint) > view.InteractionRange)
                return TopDown3DGatherActionResult.Rejected;
            if (!motor.TryBeginActionConstraint(this)
                || !animationDriver.TryBeginGather(HarvesterActionDuration))
            {
                motor.EndActionConstraint(this);
                return TopDown3DGatherActionResult.Rejected;
            }
            pickupHarvesterId = view.StableId;
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
                var target = interaction != null ? interaction.CurrentTarget as TopDown3DHarvesterView : null;
                if (target != null && target.StableId == id
                    && Vector3.Distance(motor.Position, target.InteractionPoint) <= target.InteractionRange
                    && harvesterState.TryPickup(id, inventory.State, out var dust))
                    FeedbackRequested?.Invoke($"Picked up canister with {dust} dust");
                else
                    FeedbackRequested?.Invoke("Cannot pick up canister; check space and range");
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
            if (motor == null || proceduralWorld == null || harvesterState?.Settings == null)
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
                    || !proceduralWorld.TryGetLoadedChunkAt(hit.point, out _)) continue;
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

            var destinations = new TopDown3DItemDestinationService(
                inventory.State,
                bigArmCargo != null ? bigArmCargo.State : null,
                () => bigArmCargo != null
                    && bigArmCargo.IsInAccessRange(motor != null ? motor.Position : transform.position));
            gatherAction = new TopDown3DGatherActionState(destinations);
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
            if (mode != TopDown3DInputMode.Gameplay) CancelGather();
        }
    }
}
