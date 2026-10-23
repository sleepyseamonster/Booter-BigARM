using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DLeggerRidingController : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        [SerializeField] private TopDown3DPlayerMotor player;
        [SerializeField] private TopDown3DBigArmFollower follower;
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private TopDown3DCameraRig cameraRig;
        [SerializeField] private TopDown3DAnimationPlaygroundSession laboratory;
        [SerializeField] private TopDown3DLeggerRideProfile profile;
        [SerializeField] private Transform seatFrame;
        [SerializeField] private Transform[] exitFrames = Array.Empty<Transform>();
        [SerializeField] private Transform[] resetReturns = Array.Empty<Transform>();
        private TopDown3DPlayerAnimationDriver presentation;
        private TopDown3DPlayerActionController actions;
        private TopDown3DBigArmCargo cargo;
        private LeggerGroundMotion ground;
        private Vector3 velocity, seatOffset;
        private bool bound, registered, resetting, deferredUnregister;
        private float releaseHold;
        private bool focusSuspended;
        private RidingResetParticipant resetParticipant;
        private int standaloneValidationQueries;
        private bool initialFitPlanned;
        private Vector3 initialFitTarget;
        private TopDown3DLeggerRideProfile runtimeProfile;
        public LeggerRideState State { get; } = new();
        public bool IsConfigured => bound;
        public bool HasRider => State.HasRider;
        public string LastFailure { get; private set; }
        public float ActualSpeed { get; private set; }
        public int QueryCount => ground?.Queries ?? 0;
        public int SubstepCount => ground?.Substeps ?? 0;
        public bool Stepping => ground?.Stepping == true;
        public LeggerRideBlock MotionBlock => ground?.Block ?? LeggerRideBlock.None;
        public TopDown3DBigArmFollower Follower => follower;
        public TopDown3DPlayerMotor Player => player;
        public string ResetId => "CampLoop.Booter.Riding";
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.HeldAndSpawnedProps;
        internal IAnimationPlaygroundResetParticipant ResetParticipant => resetParticipant ??= new RidingResetParticipant(this);
        // A managed reset participant survives removal of the controller's native component.
        // It retains only this relationship until the scene registry itself is disposed.
        private sealed class RidingResetParticipant : IAnimationPlaygroundResetParticipant
        {
            private readonly TopDown3DLeggerRidingController owner;
            public RidingResetParticipant(TopDown3DLeggerRidingController value) { owner = value; }
            public string ResetId => owner.ResetId;
            public AnimationPlaygroundResetStage ResetStage => owner.ResetStage;
            public void InvalidateForReset(uint epoch) => owner.InvalidateForReset(epoch);
            public bool TryRestoreForReset(out string reason) => owner.TryRestoreForReset(out reason);
        }
        public bool CanMount => bound && isActiveAndEnabled && State.Phase == LeggerRidePhase.OnFoot
            && !resetting && laboratory != null && laboratory.PreviewReady && follower.isActiveAndEnabled
            && follower.GroundReadyForRide && player.isActiveAndEnabled && player.CanBeginCampAction
            && presentation != null && presentation.MountedPresentationAvailable
            && (laboratory.Coordinator == null || player.IsAnimationPostureOwner(laboratory.Coordinator))
            && actions != null && !actions.IsBusy && !input.LaboratoryResetBlocked && input.Mode == TopDown3DInputMode.Gameplay;
        public bool CargoEligible => HasRider && State.Phase == LeggerRidePhase.Mounted && !Stepping
            && input != null && AllowedInput;
        public void Configure(TopDown3DPlayerMotor booter, TopDown3DBigArmFollower legger, TopDown3DInputRouter router,
            TopDown3DCameraRig camera, TopDown3DAnimationPlaygroundSession lab, TopDown3DLeggerRideProfile tuning,
            Transform seat, Transform[] exits, Transform[] returns)
        {
            if (bound) throw new InvalidOperationException("Configure riding before startup.");
            player = booter; follower = legger; input = router; cameraRig = camera; laboratory = lab;
            profile = tuning; seatFrame = seat; exitFrames = exits; resetReturns = returns;
        }
        private void Start() { Bind(); }
        internal bool Bind()
        {
            if (bound) return true;
            if (player == null || follower == null || input == null || cameraRig == null || laboratory == null
                || profile == null || !profile.Validate(out _) || seatFrame == null || exitFrames == null || exitFrames.Length < 2
                || resetReturns == null || resetReturns.Length == 0 || !SameScene(player) || !SameScene(follower)
                || !SameScene(input) || !SameScene(cameraRig) || !SameScene(laboratory) || !SameScene(seatFrame))
            { Fail("Riding references or profile are unavailable."); return false; }
            foreach (var anchor in exitFrames) if (anchor == null || !SameScene(anchor) || !Finite(anchor.position)) return false;
            foreach (var anchor in resetReturns) if (anchor == null || !SameScene(anchor) || !Finite(anchor.position)) return false;
            var box = follower.GetComponent<BoxCollider>(); var capsule = player.GetComponent<CapsuleCollider>();
            if (box == null || capsule == null || capsule.direction != 1 || box.isTrigger || capsule.isTrigger
                || !Finite(box.size) || !Finite(box.center) || !Finite(capsule.center) || !Finite(seatFrame.position)
                || box.size.x <= 0 || box.size.y <= 0 || box.size.z <= 0
                || !float.IsFinite(capsule.height) || !float.IsFinite(capsule.radius) || capsule.radius <= 0
                || capsule.height < capsule.radius * 2 || !seatFrame.IsChildOf(follower.transform)
                || !TopDown3DAnimationPlaygroundSession.HasUnitPhysicsHierarchy(player.transform)
                || (follower.transform.lossyScale - Vector3.one).sqrMagnitude > .000001f) return false;
            seatOffset = follower.transform.InverseTransformPoint(seatFrame.position);
            if (seatOffset.y + capsule.center.y - capsule.height * .5f < box.center.y + box.size.y * .5f + .02f)
            { Fail("The placeholder rider needs clearance above the Legger body."); return false; }
            actions = player.GetComponent<TopDown3DPlayerActionController>();
            presentation = player.GetComponent<TopDown3DPlayerAnimationDriver>(); cargo = follower.GetComponent<TopDown3DBigArmCargo>();
            // Cargo is lazy. Capture its real load before bypassing autonomous follow startup.
            try { if (cargo != null) _ = cargo.State; }
            catch (Exception error) { Fail("Legger cargo is unavailable: " + error.Message); return false; }
            if (actions == null || presentation == null || !follower.BindRiding(this) || !input.BindRidingInput(this)) return false;
            runtimeProfile = Instantiate(profile);
            runtimeProfile.hideFlags = HideFlags.HideAndDontSave;
            ground = new LeggerGroundMotion(runtimeProfile, follower.transform, player.transform, box, capsule, seatOffset);
            bound = true; return true;
        }
        private bool SameScene(Component component) => component.gameObject.scene == gameObject.scene;
        private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        private void Update()
        {
            if (!bound) return;
            if (deferredUnregister && laboratory != null) { laboratory.ResetRegistry.Unregister(ResetParticipant); registered = false; deferredUnregister = false; }
            if (!registered && isActiveAndEnabled && laboratory != null && laboratory.PreviewReady)
            { laboratory.ResetRegistry.Register(ResetParticipant); registered = true; }
            if (State.OwnsMount && (follower == null || !follower.isActiveAndEnabled || player == null || !player.isActiveAndEnabled
                || input == null || !input.isActiveAndEnabled || cameraRig == null || !cameraRig.isActiveAndEnabled
                || laboratory == null || !laboratory.isActiveAndEnabled || presentation == null || !presentation.isActiveAndEnabled))
                NotifyActorUnavailable();
        }
        public bool RequestMount()
        {
            if (!CanMount) { Fail("Finish the current activity before mounting."); return false; }
            if (Vector3.Distance(player.Position, follower.transform.position) > runtimeProfile.reach
                || !TopDown3DCargoAccess.HasLineOfSight(player.transform, follower.transform))
            { Fail("Move within reach of the Legger."); return false; }
            if (!State.BeginMount()) return false;
            initialFitPlanned = false;
            velocity = follower.transform.forward * follower.CurrentSpeed; LastFailure = null; return true;
        }
        public bool RequestDismount()
        {
            if (!bound || resetting || Stepping || !State.RequestExit()) return false;
            LastFailure = null; return true;
        }
        private bool AllowedInput => !focusSuspended && (!Application.isPlaying || Application.isFocused) && input.Mode == TopDown3DInputMode.Gameplay
            && !input.LaboratoryResetBlocked && !resetting && State.Phase != LeggerRidePhase.Faulted;
        internal void TickRiding(float dt)
        {
            if (!bound || !State.OwnsMount || dt <= 0 || !float.IsFinite(dt)) return;
            ground.BeginTick();
            if (player == null || input == null || cameraRig == null || runtimeProfile == null || follower == null
                || laboratory == null || presentation == null)
            { NotifyActorUnavailable(); return; }
            ground.RiderAttached = HasRider;
            ground.AllowSteps = HasRider;
            var body = follower.RideBody; if (body == null) { NotifyActorUnavailable(); return; }
            var start = body.position; var rotation = body.rotation;
            if (State.Phase == LeggerRidePhase.Faulted || resetting) return;
            if (State.Phase == LeggerRidePhase.Mounting && State.AdvanceEntry(dt, runtimeProfile.entryTimeout))
            { follower.ResetFollowAfterRide(); Fail("Mounting timed out."); return; }
            if (!CollisionReady(start)) { velocity = Vector3.zero; Fail("Waiting for terrain dressing collision."); return; }
            if (State.Phase == LeggerRidePhase.Mounting && velocity.magnitude <= runtimeProfile.exitSpeed)
            {
                if (!initialFitPlanned)
                {
                    if (!ground.TryPlanInitialFit(start, rotation, out initialFitTarget))
                    { State.Clear(); follower.ResetFollowAfterRide(); Fail("The Legger needs clear standing room to mount."); return; }
                    initialFitPlanned = true;
                }
                if (initialFitTarget.y - start.y > .002f)
                {
                    if (!ground.AdvanceInitialFit(start, initialFitTarget, rotation, runtimeProfile.stepSpeed * dt, out var fit))
                    { State.Clear(); follower.ResetFollowAfterRide(); Fail("The Legger's standing room changed."); return; }
                    follower.CommitRidePose(fit, rotation, Vector3.Distance(start, fit) / dt); return;
                }
            }
            if (State.Phase == LeggerRidePhase.Dismounting && AllowedInput) State.ObserveExitInput(input.MoveValue);
            var move = AllowedInput && State.Phase == LeggerRidePhase.Mounted ? input.MoveValue : Vector2.zero;
            var direction = TopDown3DMovementBasis.ToWorldDirection(move, cameraRig.transform.forward, cameraRig.transform.right);
            float slopeSpeed = 1;
            if (direction.sqrMagnitude > .0001f && ground.TrySupport(start, rotation, runtimeProfile.slopeDegrees, out _))
            {
                direction = Vector3.ProjectOnPlane(direction, ground.SupportNormal).normalized * move.magnitude;
                slopeSpeed = runtimeProfile.SpeedMultiplierForSlope(ground.SupportNormal);
            }
            float loadSpeed = cargo != null ? cargo.LoadProfile.SpeedMultiplier : 1;
            float loadAcceleration = cargo != null ? cargo.LoadProfile.AccelerationMultiplier : 1;
            var speed = (AllowedInput && input.SprintHeld ? runtimeProfile.fastSpeed : runtimeProfile.normalSpeed) * loadSpeed * slopeSpeed;
            var target = direction * speed;
            bool reversal = velocity.sqrMagnitude > .01f && Vector3.Dot(velocity, target) < 0;
            if (reversal) target = Vector3.zero;
            var rate = target.sqrMagnitude < velocity.sqrMagnitude || reversal ? runtimeProfile.braking : runtimeProfile.acceleration * loadAcceleration;
            velocity = Vector3.MoveTowards(velocity, target, rate * dt);
            var facing = target.sqrMagnitude > .001f ? target.normalized : rotation * Vector3.forward;
            facing.y = 0;
            var turn = Mathf.Lerp(runtimeProfile.slowTurn, runtimeProfile.fastTurn, Mathf.Clamp01(velocity.magnitude / runtimeProfile.fastSpeed));
            var nextRotation = Quaternion.RotateTowards(rotation, Quaternion.LookRotation(facing, Vector3.up), turn * dt);
            Vector3 end = start; Quaternion endRotation = rotation;
            if (velocity.sqrMagnitude > .000001f || ground.Stepping)
                ground.Resolve(start, rotation, velocity * dt, nextRotation, out end, out endRotation);
            ActualSpeed = Vector3.Distance(start, end) / dt;
            if (end == start) velocity = Vector3.zero;
            follower.CommitRidePose(end, endRotation, ActualSpeed);
            if (HasRider) player.SetRiderPose(this, end + endRotation * seatOffset, endRotation);
            if (ground.Stepping && end == start && (ground.Block == LeggerRideBlock.MissingSupport || ground.Block == LeggerRideBlock.Obstacle))
            {
                State.Fault(); input.SetLaboratoryResetBlocked(true);
                Fail("Step interrupted by changed terrain. Use lab reset for a safe return."); return;
            }
            if (ground.Stepping) return;
            if (State.Phase == LeggerRidePhase.Mounting && velocity.magnitude <= runtimeProfile.exitSpeed) CommitMount(end, endRotation);
            else if (State.Phase == LeggerRidePhase.Dismounting && ActualSpeed <= runtimeProfile.exitSpeed) TryExit(end, endRotation);
        }
        private void CommitMount(Vector3 position, Quaternion rotation)
        {
            if (!AllowedInput || Vector3.Distance(player.Position, position) > runtimeProfile.reach || !player.CanBeginCampAction)
            { State.Clear(); follower.ResetFollowAfterRide(); Fail("Mounting interrupted."); return; }
            var seat = position + rotation * seatOffset;
            ground.RiderAttached = true;
            var raised = new Vector3(player.Position.x, seat.y, player.Position.z);
            if (!ground.Clear(position, rotation, true) || !ground.RiderPath(player.Position, raised, rotation)
                || !ground.RiderPath(raised, seat, rotation))
            { State.Clear(); follower.ResetFollowAfterRide(); Fail("Mounting space is blocked."); return; }
            if (!presentation.TryBeginMountedPresentation(this)) { State.Clear(); follower.ResetFollowAfterRide(); return; }
            if (!player.TryAcquireRider(this))
            { presentation.EndMountedPresentation(this); State.Clear(); follower.ResetFollowAfterRide(); return; }
            State.CommitMount(State.Epoch); player.SetRiderPose(this, seat, rotation); velocity = Vector3.zero;
            input.FenceRidingInteraction();
            cameraRig.SetRidingFrame(this, seatFrame, follower.transform);
        }
        // Rebind the saved relationship only inside the frozen expedition restoration barrier.
        internal bool RestoreExpeditionCheckpoint()
        {
            if(!CampExpeditionFeature.Enabled(gameObject.scene)||CampExpeditionSession.Find(gameObject.scene)?.RestoringRiderCheckpoint!=true||!Bind()||State.OwnsMount)return false;
            var position=follower.transform.position;var rotation=follower.transform.rotation;
            ground.BeginTick();ground.RiderAttached=true;
            if(!ground.Clear(position,rotation,true)||!State.BeginMount())return false;
            if(!presentation.TryBeginMountedPresentation(this)){State.Clear();return false;}
            if(!player.TryAcquireRider(this,true)){presentation.EndMountedPresentation(this);State.Clear();return false;}
            State.CommitMount(State.Epoch);player.SetRiderPose(this,position+rotation*seatOffset,rotation);velocity=Vector3.zero;
            input.FenceRidingInteraction();cameraRig.SetRidingFrame(this,seatFrame,follower.transform);return true;
        }
        internal void ReleaseExpeditionTownRider()
        {
            if(!CampExpeditionFeature.Enabled(gameObject.scene)||CampExpeditionSession.Find(gameObject.scene)?.IsSettledTown!=true||!HasRider)return;
            if(player.ReleaseExpeditionTownRider(this))CompleteRelease();
        }
        private bool TryExit(Vector3 position, Quaternion rotation)
        {
            var seat = position + rotation * seatOffset;
            foreach (var anchor in exitFrames)
            {
                var local = follower.transform.InverseTransformPoint(anchor.position);
                if (!ground.TryExit(seat, position + rotation * local, rotation, player.MaxWalkableSlope, out var exit)
                    || !player.TryReleaseRider(this, exit, rotation)) continue;
                CompleteRelease(); return true;
            }
            State.RejectExit(); Fail("No safe exit. Reposition the Legger."); return false;
        }
        private void CompleteRelease()
        {
            input?.ClearLaboratoryIntent();
            if (presentation != null) presentation.EndMountedPresentation(this);
            ground.CancelStep(); velocity = Vector3.zero;
            if (cameraRig != null) cameraRig.ReleaseRidingFrame(this);
            State.Clear(); if (follower != null) follower.ResetFollowAfterRide(); releaseHold = .35f;
        }
        private bool CollisionReady(Vector3 position)
        {
            var region = new Bounds(position, Vector3.zero);
            float margin = runtimeProfile.fastSpeed * runtimeProfile.fastSpeed / (2 * runtimeProfile.braking) + runtimeProfile.fastSpeed * 2 + 2;
            region.Expand(new Vector3(margin * 2, 0, margin * 2));
            return follower.PopulationPreparation == null || follower.PopulationPreparation(region);
        }
        internal bool HoldAfterRelease(float dt)
        {
            if (releaseHold <= 0) return false;
            releaseHold = Mathf.Max(0, releaseHold - dt); return true;
        }
        private void LateUpdate()
        {
            if (HasRider && presentation != null && follower != null)
                presentation.AlignMountedVisual(this, follower.transform.position + follower.transform.rotation * seatOffset, follower.transform.rotation);
        }
        private void Fail(string reason) { LastFailure = reason; if (actions != null) actions.ReportFeedback(reason); }
        internal void ReportUnavailableAction() => Fail("Dismount before using ground actions.");
        internal bool ReserveStandingValidation()
        {
            if (ground != null) return ground.ReserveQueries(4);
            if (standaloneValidationQueries + 4 > 256) return false;
            standaloneValidationQueries += 4; return true;
        }
        private void OnApplicationFocus(bool focused)
        {
            focusSuspended = !focused;
            if (!focused) InterruptInput();
        }
        internal void InterruptInput()
        {
            input?.ClearLaboratoryIntent();
            if (State.Phase == LeggerRidePhase.Mounting) { State.Clear(); follower?.ResetFollowAfterRide(); }
            else if (State.Phase == LeggerRidePhase.Dismounting) State.RejectExit();
        }
        public void NotifyActorUnavailable()
        {
            if (!State.OwnsMount || State.Phase == LeggerRidePhase.Faulted) return;
            State.InvalidateCommands(); velocity = Vector3.zero;
            if (!HasRider) { State.Clear(); return; }
            ground.BeginTick();
            if (follower != null && player != null && TryExit(follower.transform.position, follower.transform.rotation)) return;
            State.Fault(); input?.SetLaboratoryResetBlocked(true); Fail("Riding interrupted. Use lab reset for a safe return.");
        }
        public void InvalidateForReset(uint epoch)
        { resetting = true; State.InvalidateCommands(); velocity = Vector3.zero; ground?.BeginTick(); standaloneValidationQueries = 0; }
        public bool TryRestoreForReset(out string reason)
        {
            reason = null;
            if (HasRider)
            {
                foreach (var anchor in resetReturns)
                {
                    if (anchor != null && player.TryResetRider(this, laboratory.Coordinator, State.Epoch, anchor.position))
                    { CompleteRelease(); resetting = false; return true; }
                }
                reason = "No supported rider return is clear."; return false;
            }
            State.Clear(); ground?.CancelStep(); follower?.ResetFollowAfterRide(); resetting = false; return true;
        }
        private void OnDisable()
        {
            if (State.OwnsMount) NotifyActorUnavailable();
            if (registered && resetting) deferredUnregister = true;
            else if (registered && !HasRider) { laboratory.ResetRegistry.Unregister(ResetParticipant); registered = false; }
        }
        private void OnDestroy()
        {
            if (runtimeProfile != null)
            {
                if (Application.isPlaying) Destroy(runtimeProfile); else DestroyImmediate(runtimeProfile);
            }
        }
    }
}
