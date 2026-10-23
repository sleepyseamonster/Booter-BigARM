using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DAnimationPlaygroundSession : ITopDown3DStationActionReceiver
    {
        [SerializeField] private TopDown3DPlayerMotor player;
        [SerializeField] private Transform[] safeReturns = Array.Empty<Transform>();
        private TopDown3DInputRouter playerInput;
        private TopDown3DPlayerAnimationDriver presentation;
        private TopDown3DPlayerActionController playerAction;
        private TopDown3DMenuController menu;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundCrouchController crouch;
        private bool servicesBound;
        private Func<bool> ownsInput;
        private string selectedSource;
        private DelegateResetParticipant legacyReset, vitalsReset, inputReset;
        public bool IsBusy => coordinator != null && coordinator.IsBusy || crouch!=null && crouch.Transitioning || airborne!=null && airborne.Active || lowMotion!=null && lowMotion.Active || dodge!=null && dodge.Active || sprintEntries!=null && sprintEntries.Active || locomotionTransitions!=null && locomotionTransitions.Active || ResourceCombatBusy?.Invoke() == true;
        public TopDown3DPlayerMotor Player => player;
        public AnimationPlaygroundActionCoordinator Coordinator => coordinator;
        // The aggregate busy flag includes this session and its own motor lease.
        // It gates admission, but cannot suspend an already owned turn/recovery.
        private bool TurnBlockedByPlayerActivity=>TurnActivityUnavailable(playerAction.Activity,turning!=null && turning.Active);
        private static bool TurnActivityUnavailable(TopDown3DActivitySnapshot activity,bool turningActive)
            =>activity.LegacyWork || activity.Placement || activity.CampActive || !turningActive && activity.IsBusy;
        public bool CrouchReady=>servicesBound && isActiveAndEnabled && PreviewReady && crouch!=null && bindings!=null
            && bindings.crouch!=null && bindings.crouch.contactsQualified && bindings.crouch.groundingQualified && bindings.footProjection!=null;

        public void ConfigurePlayer(TopDown3DPlayerMotor value, Transform[] returns)
        {
            if (servicesBound) throw new InvalidOperationException("Author the player and safe returns before startup.");
            player = value; safeReturns = returns == null ? Array.Empty<Transform>() : (Transform[])returns.Clone();
        }

        private void PreparePlayer()
        {
            if (player == null) { Fail("The authored playground player is missing."); return; }
            if (!bindings.ValidatePlayer(out var reason)) { Fail(reason); return; }
            RegisterStaticCampGround();
            var capsule = player.GetComponent<CapsuleCollider>();
            if (!HasUnitPhysicsHierarchy(player.transform) || capsule == null || capsule.direction != 1
                || (capsule.center - Vector3.zero).sqrMagnitude > .000001f
                || Mathf.Abs(capsule.height - bindings.capsuleHeight) > .0001f
                || Mathf.Abs(capsule.radius - bindings.capsuleRadius) > .0001f)
            { Fail("Restore the authored collider calibration with an unscaled physics hierarchy."); return; }
            presentation = player.GetComponent<TopDown3DPlayerAnimationDriver>();
            playerInput = player.InputRouter;
            playerAction = player.GetComponent<TopDown3DPlayerActionController>();
            if (presentation == null || playerInput == null || playerAction == null)
            { Fail("The existing player motor, animation, input and action services are required."); return; }
            if (safeReturns.Length == 0)
            { Fail("The lab requires an authored safe return anchor."); return; }
            foreach (var anchor in safeReturns)
                if (anchor == null || anchor.gameObject.scene != gameObject.scene)
                { Fail("Safe returns must be authored in the laboratory scene."); return; }
            try
            {
                presentation.ConfigureLaboratory(bindings);
                var review = ExperimentalForwardReview;
                if (review != null)
                {
                    var station = Array.Find(stations, value => value != null && value.StationId == "AP02");
                    presentation.ConfigureLaboratoryForwardReview(review, station);
                    if (bindings.standingStrafeReview != null) presentation.ConfigureLaboratoryStandingStrafeReview(bindings.standingStrafeReview);
                    if (bindings.continuationReview != null) presentation.ConfigureLaboratoryContinuation(bindings.continuationReview);
                    if (bindings.movingTurns != null) presentation.ConfigureLaboratoryMovingTurns(bindings.movingTurns);
                    if (Mathf.Abs(review.gaits[0].nominalSpeed - bindings.slowWalkSpeed) > .0001f
                        || Mathf.Abs(review.gaits[1].nominalSpeed - bindings.runSpeed) > .0001f
                        || Mathf.Abs(review.gaits[review.FastRunIndex].nominalSpeed - bindings.sprintSpeed) > .0001f)
                        throw new InvalidOperationException("Experimental gait calibration and laboratory motor speeds disagree.");
                }
            }
            catch (Exception error) { Fail(error.Message); }
        }

        public static bool HasUnitPhysicsHierarchy(Transform actor)
        {
            for (var item = actor; item != null; item = item.parent)
                if ((item.localScale - Vector3.one).sqrMagnitude > .000001f) return false;
            return actor != null;
        }

        private void Start()
        {
            if (Readiness != AnimationPlaygroundReadiness.DependenciesChecked) return;
            if (presentation.HumanoidAnimator == null || !presentation.HumanoidAnimator.isHuman)
            { Fail("The calibrated visual did not initialize its Humanoid graph."); return; }
            try
            {
                TopDown3DInventoryUiSceneInstaller.TryInstallForScene(gameObject.scene);
                menu = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene);
                if (menu == null || !menu.IsConfigured || !playerInput.HasAnimationCommands)
                { Fail("The existing menu and owned animation input must initialize first."); return; }
                if(playerInput.AnimationActionOwned != null) throw new InvalidOperationException("Animation input already has an owner.");
                var returns = new Vector3[safeReturns.Length];
                for (var i = 0; i < returns.Length; i++) returns[i] = safeReturns[i].position;
                coordinator = player.gameObject.AddComponent<AnimationPlaygroundActionCoordinator>();
                coordinator.Configure(player, presentation, ResetRegistry, returns,
                    () => !Application.isFocused || playerInput.Mode != TopDown3DInputMode.Gameplay);
                player.ConfigureLaboratorySpeeds(coordinator, bindings.slowWalkSpeed, bindings.runSpeed, bindings.sprintSpeed);
                if (ExperimentalForwardReview != null && ExperimentalForwardReview.ExpandedReview)
                    player.ConfigureLaboratoryGamepadGaits(coordinator, ExperimentalForwardReview.gaits[3].nominalSpeed,
                        ExperimentalForwardReview.gaits[4].nominalSpeed);
                player.BlockUnqualifiedLaboratoryTraversal(coordinator);
                if (bindings.standingStrafeReview != null && presentation.LaboratoryStandingStrafeConfigured)
                    player.ConfigureLaboratoryStandingStrafe(coordinator, bindings.standingStrafeReview, presentation.StandingStrafeReviewSupportAvailable);
                if(bindings.crouch!=null)
                {
                    crouch=player.gameObject.AddComponent<AnimationPlaygroundCrouchController>();
                    crouch.Configure(player,presentation,coordinator,bindings.crouch,ResetRegistry,
                        () => !Application.isFocused || playerInput.Mode != TopDown3DInputMode.Gameplay,() => playerAction.IsBusy);
                    playerInput.AnimationCrouchRequested+=ToggleCrouch;
                }
                BindLowMotion();BindAirborne();BindDodge();BindSprintEntries();
                if(bindings.continuationReview!=null && presentation.LaboratoryContinuationConfigured)
                {
                    turning=player.gameObject.AddComponent<AnimationPlaygroundTurnController>();
                    turning.Configure(player,presentation,coordinator,bindings.continuationReview,ResetRegistry,
                        ()=>!Application.isFocused || playerInput.Mode!=TopDown3DInputMode.Gameplay || TurnBlockedByPlayerActivity
                            || crouch!=null && crouch.Active || lowMotion!=null && lowMotion.Active || airborne!=null && airborne.Active || dodge!=null && dodge.Active || sprintEntries!=null && sprintEntries.Active);
                }
                BindLocomotionTransitions();
                ownsInput = () => IsBusy || crouch!=null && crouch.Active;
                playerInput.AnimationActionOwned = ownsInput;
                playerInput.AnimationCancelRequested += CancelOwnedAction;
                playerInput.AnimationJumpRequested += RequestJump;
                playerInput.AnimationDodgeRequested += RequestDodge;
                playerInput.AnimationActionRequested += UnavailableAction;
                menu.AnimationSelectionChanged += SelectMotion;
                menu.AnimationPreviewRequested += PreviewMotion;
                menu.AnimationResetRequested += ResetSession;
                playerAction.ConfigureStationActions(this);
                RegisterPlayerReset(); BindStations(); servicesBound = true;
                Advance(AnimationPlaygroundReadiness.ServicesBound);
                Advance(AnimationPlaygroundReadiness.VisualReady);
                // Physical station coverage and author acceptance gate PlayableReady separately.
            }
            catch (Exception error) { Fail("Player service binding failed: " + error.Message); DetachPlayerServices(); }
        }

        private void OnEnable()
        {
            if (!servicesBound) return;
            if (CheckDependencies())
            { Advance(AnimationPlaygroundReadiness.ServicesBound); Advance(AnimationPlaygroundReadiness.VisualReady); }
        }

        private void SelectMotion(string id) => selectedSource = id;
        private void PreviewMotion(string id)
        {
            if (!isActiveAndEnabled || !servicesBound || !PreviewReady || playerInput.Mode != TopDown3DInputMode.Gameplay)
            { playerAction?.ReportFeedback("The laboratory is unavailable for preview."); return; }
            var binding = bindings.Find(id);
            if(crouch!=null && crouch.Active){playerAction.ReportFeedback("Stand before previewing another motion.");return;}
            if(binding?.CanPreview != true) { playerAction.ReportFeedback("This motion is unavailable for preview."); return; }
            if(coordinator.IsPreview) coordinator.Cancel();
            if (playerAction.IsBusy)
            { playerAction?.ReportFeedback("Finish the current activity before previewing."); return; }
            if (!coordinator.TryBeginPreview(binding)) playerAction.ReportFeedback("This motion is unavailable for preview.");
        }
        private void ToggleCrouch()
        {
            if(!CrouchReady){playerAction?.ReportFeedback("Qualified crouch dependencies are unavailable.");return;}
            if(locomotionTransitions!=null && locomotionTransitions.Active)locomotionTransitions.TryYieldTo(crouch);
            if(!crouch.Active && player.SprintActive){RequestSlide();return;}
            if(!crouch.TryToggle()){presentation.EndPoseBlend(crouch);playerAction.ReportFeedback(crouch.LastFailure);}
        }
        private void UnavailableAction()
        {
            if (ResourceAttackRequested?.Invoke() == true) return;
            playerAction?.ReportFeedback("This action needs a qualified interaction at a test station.");
        }
        private void CancelOwnedAction()
        {
            if(locomotionTransitions!=null && locomotionTransitions.Active){locomotionTransitions.Cancel();return;}
            if(sprintEntries!=null && sprintEntries.Active){sprintEntries.Cancel();return;}
            if(dodge!=null && dodge.Active){dodge.Cancel();return;}
            if(turning!=null && turning.Active){turning.Cancel("Turn cancelled; release the stick to rearm.");return;}
            if(lowMotion!=null && lowMotion.Active){lowMotion.TryCancel();return;}
            if(airborne!=null && airborne.Active){airborne.TryCancel();return;}
            if(crouch!=null && crouch.Active)
            {if(!crouch.TryCancel())playerAction.ReportFeedback(crouch.LastFailure);return;}
            coordinator?.Cancel();
        }
        public bool TryBegin(TopDown3DInteractionTarget target)
        {
            if(TryDispatchStation(target,out var handled)) return true;
            if(handled)return false;
            playerAction?.ReportFeedback(string.IsNullOrEmpty(selectedSource)
                ? "Choose an animation variant in the lab palette."
                : "This station's physical action is awaiting contact and movement qualification.");
            return false;
        }
        public void ResetSession()
        {
            if(CampExpeditionFeature.Enabled(gameObject.scene)){playerAction?.ReportFeedback("The expedition checkpoint owns this session.");return;}
            if (!isActiveAndEnabled || !servicesBound) return;
            playerInput.SetLaboratoryResetBlocked(true);
            if (!ResetRegistry.TryReset()) playerAction.ReportFeedback(ResetRegistry.LastFailure);
            else { playerInput.SetLaboratoryResetBlocked(false); playerAction.ReportFeedback("Lab player and registered station state reset."); }
        }

        public void ResetSessionForForwardReview()
        {
            if (!IsLab || !isActiveAndEnabled || !servicesBound || !PreviewReady || ExperimentalForwardReview == null) return;
            var station = Array.Find(stations, value => value != null && value.StationId == "AP02");
            if (station?.SafeReturn == null || !coordinator.TrySelectResetReturn(station.SafeReturn.position))
            { playerAction.ReportFeedback("The authored gait-deck return is unavailable."); return; }
            try
            {
                ResetSession();
                if (ResetRegistry.LastFailure == null) playerAction.ReportFeedback("Experimental forward gait review: AP02 flat deck.");
            }
            finally { coordinator.ClearSelectedResetReturn(); }
        }

        private void RegisterPlayerReset()
        {
            var vitals = player.GetComponent<TopDown3DSurvivalVitals>();
            if (vitals == null) throw new InvalidOperationException("The existing survival vitals are required for lab reset.");
            var initialVitals = vitals.CaptureSnapshot();
            legacyReset = new DelegateResetParticipant("CampLoop.Booter.LegacyActions", AnimationPlaygroundResetStage.HeldAndSpawnedProps,
                () => { playerAction.CancelGather(); player.GetComponent<TopDown3DCampController>()?.InterruptForSystemMenu(); }, () => true);
            vitalsReset = new DelegateResetParticipant("CampLoop.Booter.Vitals", AnimationPlaygroundResetStage.Actor,
                () => { }, () => vitals.ApplySnapshot(initialVitals));
            inputReset = new DelegateResetParticipant("CampLoop.Booter.Input", AnimationPlaygroundResetStage.Input,
                () => playerInput.ClearLaboratoryIntent(),
                () => { playerInput.ClearLaboratoryIntent(); playerInput.EnterGameplayMode(); return true; });
            ResetRegistry.Register(legacyReset); ResetRegistry.Register(vitalsReset); ResetRegistry.Register(inputReset);
        }
        private void DetachPlayerServices()
        {
            if(turning!=null){turning.DisposeOwnedServices();if(Application.isPlaying)Destroy(turning);else DestroyImmediate(turning);turning=null;}
            DetachLocomotionTransitions();
            DetachSprintEntries();
            coordinator?.Cancel();
            DetachDodge();
            DetachAirborne();
            DetachLowMotion();
            if(crouch!=null)
            {
                crouch.DisposeOwnedServices();
                if(Application.isPlaying)Destroy(crouch);else DestroyImmediate(crouch);
                crouch=null;
            }
            UnbindStations();
            if (playerInput != null)
            {
                playerInput.AnimationCancelRequested -= CancelOwnedAction;
                playerInput.AnimationCrouchRequested -= ToggleCrouch;
                playerInput.AnimationJumpRequested -= RequestJump;
                playerInput.AnimationDodgeRequested -= RequestDodge;
                playerInput.AnimationActionRequested -= UnavailableAction;
                if (ReferenceEquals(playerInput.AnimationActionOwned, ownsInput)) playerInput.AnimationActionOwned = null;
            }
            if (menu != null)
            {
                menu.AnimationSelectionChanged -= SelectMotion; menu.AnimationPreviewRequested -= PreviewMotion;
                menu.AnimationResetRequested -= ResetSession;
            }
            if (playerAction != null) playerAction.ConfigureStationActions(null);
            foreach (var participant in new[] { legacyReset, vitalsReset, inputReset })
                if (participant != null) ResetRegistry.Unregister(participant);
            if(coordinator != null)
            {
                coordinator.DisposeOwnedServices();
                if(Application.isPlaying) Destroy(coordinator); else DestroyImmediate(coordinator);
                coordinator=null;
            }
            servicesBound = false;
        }
        private sealed class DelegateResetParticipant : IAnimationPlaygroundResetParticipant
        {
            private readonly Action invalidate;
            private readonly Func<bool> restore;
            public string ResetId { get; }
            public AnimationPlaygroundResetStage ResetStage { get; }
            public DelegateResetParticipant(string id, AnimationPlaygroundResetStage stage, Action cancel, Func<bool> reset)
            { ResetId = id; ResetStage = stage; invalidate = cancel; restore = reset; }
            public void InvalidateForReset(uint epoch) => invalidate();
            public bool TryRestoreForReset(out string reason)
            { var restored = restore(); reason = restored ? null : "The player service could not restore its initial state."; return restored; }
        }
    }
}
