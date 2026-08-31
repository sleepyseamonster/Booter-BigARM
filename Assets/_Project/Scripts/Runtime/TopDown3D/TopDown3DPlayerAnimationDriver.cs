using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DFootSide
    {
        Left,
        Right,
    }

    public enum TopDown3DLocomotionState
    {
        Idle,
        Starting,
        Sustained,
        Stopping,
        Pivoting,
        Traversal,
        ConstrainedAction,
    }

    public readonly struct TopDown3DFootContact
    {
        public TopDown3DFootContact(
            TopDown3DFootSide side,
            Vector3 worldPosition,
            float planarSpeed,
            float normalizedImpact)
        {
            Side = side;
            WorldPosition = worldPosition;
            PlanarSpeed = planarSpeed;
            NormalizedImpact = Mathf.Clamp01(normalizedImpact);
        }

        public TopDown3DFootSide Side { get; }
        public Vector3 WorldPosition { get; }
        public float PlanarSpeed { get; }
        public float NormalizedImpact { get; }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(TopDown3DPlayerMotor))]
    public sealed class TopDown3DPlayerAnimationDriver : MonoBehaviour
    {
        public const float PrototypeVisualScale = 1.4f;
        public const float PrototypeVisualGroundOffset = -1.24f;

        private const int IdleIndex = 0;
        private const int WalkIndex = 1;
        private const int RunIndex = 2;
        private const int SprintIndex = 3;
        private const int TurnGaitLeftIndex = 4;
        private const int TurnGaitRightIndex = 5;
        private const int StartLeftIndex = 6;
        private const int StartRightIndex = 7;
        private const int StopLeftIndex = 8;
        private const int StopRightIndex = 9;
        private const int HardTurnLeftIndex = 10;
        private const int HardTurnRightIndex = 11;
        private const int SideStepLeftIndex = 12;
        private const int SideStepRightIndex = 13;
        private const int VaultIndex = 14;
        private const int GatherIndex = 15;
        private const int ClipCount = 16;
        private const float LocomotionStopSpeed = 0.08f;
        private const int FootGroundHitCapacity = 4;

        [Header("Prototype Humanoid")]
        [SerializeField] private GameObject humanoidPrefab;
        [SerializeField] private Vector3 visualLocalPosition =
            new Vector3(0f, PrototypeVisualGroundOffset, 0f);
        [SerializeField] private Vector3 visualLocalEulerAngles;
        [SerializeField, Min(0.01f)] private float visualScale = PrototypeVisualScale;

        [Header("Animation Authority")]
        [SerializeField] private TopDown3DLocomotionClipProfile locomotionProfile;
        [SerializeField] private AnimationClip sideStepLeftClip;
        [SerializeField] private AnimationClip sideStepRightClip;
        [SerializeField] private AnimationClip vaultClip;
        [SerializeField] private AnimationClip gatherClip;

        [Header("Locomotion Blend")]
        [SerializeField, Min(0.01f)] private float animationSpeedSharpness = 10f;
        [SerializeField, Min(0.01f)] private float traversalCrossFadeDuration = 0.08f;
        [SerializeField, Range(0f, 1f)] private float maximumTurnGaitWeight = 0.55f;
        [SerializeField, Range(0f, 1f)] private float turnGaitStartLateralRatio = 0.12f;
        [SerializeField, Range(0f, 1f)] private float turnGaitFullLateralRatio = 0.65f;
        [SerializeField, Min(0f)] private float turnGaitStartYawRate = 45f;
        [SerializeField, Min(0f)] private float turnGaitFullYawRate = 240f;

        [Header("Foot Contacts")]
        [SerializeField, Min(0.05f)] private float footProbeLift = 0.28f;
        [SerializeField, Min(0.05f)] private float footProbeDistance = 0.62f;
        [SerializeField, Min(0f)] private float footGroundClearance = 0.018f;

        private readonly AnimationClipPlayable[] clipPlayables = new AnimationClipPlayable[ClipCount];
        private readonly float[] targetWeights = new float[ClipCount];
        private readonly TopDown3DLocomotionClipEntry[] locomotionEntries =
            new TopDown3DLocomotionClipEntry[12];
        private readonly RaycastHit[] footGroundHits = new RaycastHit[FootGroundHitCapacity];
        private TopDown3DPlayerMotor motor;
        private Animator humanoidAnimator;
        private Transform leftFoot;
        private Transform rightFoot;
        private GameObject visualInstance;
        private PlayableGraph playableGraph;
        private AnimationMixerPlayable mixer;
        private int lastActionIndex = -1;
        private int activeHardTurnIndex = -1;
        private float activeHardTurnRemaining;
        private bool hardTurnLatched;
        private int activeTransitionIndex = -1;
        private bool oneShotContactEmitted;
        private float locomotionPhase;
        private float smoothedPlanarSpeed;
        private float activeGatherDuration;

        public bool HasCoreAnimationSet => TryGetAnimationSetError(false, out _);
        public bool HasCompleteAnimationSet => TryGetAnimationSetError(true, out _);
        public bool IsGathering { get; private set; }
        public TopDown3DLocomotionState CurrentLocomotionState { get; private set; }
        public TopDown3DLocomotionClipProfile LocomotionProfile => locomotionProfile;
        public AnimationClip GatherClip => gatherClip;
        public Vector3 VisualLocalPosition => visualLocalPosition;
        public float VisualScale => visualScale;
        public event Action<TopDown3DFootContact> FootContact;

        public void Configure(
            GameObject modelPrefab,
            TopDown3DLocomotionClipProfile profile,
            AnimationClip sideStepLeft,
            AnimationClip sideStepRight,
            AnimationClip vault,
            AnimationClip gather)
        {
            humanoidPrefab = modelPrefab;
            locomotionProfile = profile;
            sideStepLeftClip = sideStepLeft;
            sideStepRightClip = sideStepRight;
            vaultClip = vault;
            gatherClip = gather;
        }

        public bool TryBeginGather(float duration)
        {
            if (gatherClip == null || !playableGraph.IsValid() || duration <= 0f)
            {
                return false;
            }

            activeGatherDuration = Mathf.Max(0.05f, duration);
            IsGathering = true;
            return true;
        }

        public void EndGather()
        {
            IsGathering = false;
            activeGatherDuration = 0f;
        }

        private void Awake()
        {
            motor = GetComponent<TopDown3DPlayerMotor>();
            if (!TryGetAnimationSetError(true, out var animationSetError))
            {
                Debug.LogError(
                    $"Booter's required animation set is incomplete: {animationSetError} Locomotion has no fallback clips.",
                    this);
                enabled = false;
                return;
            }

            CreateVisual();
        }

        private bool TryGetAnimationSetError(bool includeActions, out string error)
        {
            if (humanoidPrefab == null)
            {
                error = "the prototype Humanoid model reference is missing.";
                return false;
            }

            if (locomotionProfile == null)
            {
                error = "the canonical locomotion profile reference is missing.";
                return false;
            }

            if (!locomotionProfile.TryValidate(out var profileError))
            {
                error = $"the canonical locomotion profile is invalid ({profileError}).";
                return false;
            }

            if (includeActions && sideStepLeftClip == null)
            {
                error = "the left sidestep clip reference is missing.";
                return false;
            }

            if (includeActions && sideStepRightClip == null)
            {
                error = "the right sidestep clip reference is missing.";
                return false;
            }

            if (includeActions && vaultClip == null)
            {
                error = "the vault clip reference is missing.";
                return false;
            }

            if (includeActions && gatherClip == null)
            {
                error = "the gather clip reference is missing.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void CreateVisual()
        {
            visualInstance = Instantiate(humanoidPrefab, transform, false);
            visualInstance.name = "Booter Prototype Humanoid Visual";
            visualInstance.transform.SetLocalPositionAndRotation(
                visualLocalPosition,
                Quaternion.Euler(visualLocalEulerAngles));
            visualInstance.transform.localScale = Vector3.one * visualScale;

            humanoidAnimator = visualInstance.GetComponentInChildren<Animator>();
            if (humanoidAnimator == null || !humanoidAnimator.isHuman)
            {
                Debug.LogError("Booter's prototype visual requires a valid Humanoid Animator.", this);
                Destroy(visualInstance);
                enabled = false;
                return;
            }

            humanoidAnimator.applyRootMotion = false;
            humanoidAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            humanoidAnimator.updateMode = AnimatorUpdateMode.Normal;
            humanoidAnimator.stabilizeFeet = false;
            leftFoot = humanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = humanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot == null || rightFoot == null)
            {
                Debug.LogError("Booter's Humanoid avatar must expose both foot bones.", this);
                Destroy(visualInstance);
                enabled = false;
                return;
            }

            DisableGreyboxVisuals();
            CreatePlayableGraph(humanoidAnimator);
        }

        private void DisableGreyboxVisuals()
        {
            var capsuleRenderer = GetComponent<MeshRenderer>();
            if (capsuleRenderer != null)
            {
                capsuleRenderer.enabled = false;
            }

            var facingMarker = transform.Find("Facing Marker");
            if (facingMarker != null)
            {
                facingMarker.gameObject.SetActive(false);
            }
        }

        private void CreatePlayableGraph(Animator animator)
        {
            for (var roleIndex = 0; roleIndex < locomotionEntries.Length; roleIndex++)
            {
                locomotionEntries[roleIndex] = locomotionProfile.GetRequiredEntry(
                    (TopDown3DLocomotionClipRole)roleIndex);
            }

            var clips = new[]
            {
                GetLocomotionClip(TopDown3DLocomotionClipRole.Idle),
                GetLocomotionClip(TopDown3DLocomotionClipRole.Walk),
                GetLocomotionClip(TopDown3DLocomotionClipRole.Run),
                GetLocomotionClip(TopDown3DLocomotionClipRole.Sprint),
                GetLocomotionClip(TopDown3DLocomotionClipRole.DirectionalLeft),
                GetLocomotionClip(TopDown3DLocomotionClipRole.DirectionalRight),
                GetLocomotionClip(TopDown3DLocomotionClipRole.StartLeft),
                GetLocomotionClip(TopDown3DLocomotionClipRole.StartRight),
                GetLocomotionClip(TopDown3DLocomotionClipRole.StopLeft),
                GetLocomotionClip(TopDown3DLocomotionClipRole.StopRight),
                GetLocomotionClip(TopDown3DLocomotionClipRole.PivotLeft),
                GetLocomotionClip(TopDown3DLocomotionClipRole.PivotRight),
                sideStepLeftClip,
                sideStepRightClip,
                vaultClip,
                gatherClip
            };
            playableGraph = PlayableGraph.Create("Booter Prototype Humanoid Animation");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(playableGraph, ClipCount);

            for (var i = 0; i < clips.Length; i++)
            {
                var playable = AnimationClipPlayable.Create(playableGraph, clips[i]);
                // Preserve the authored Humanoid pose during baseline recovery.
                // Foot IK can be reconsidered only after complete strides are proven.
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                playableGraph.Connect(playable, 0, mixer, i);
                mixer.SetInputWeight(i, i == IdleIndex ? 1f : 0f);
                clipPlayables[i] = playable;
            }

            var output = AnimationPlayableOutput.Create(playableGraph, "Booter Humanoid", animator);
            // The mixer is the sole pose authority. Procedural IK and body/foot
            // rewriting previously collapsed full locomotion clips into a small
            // leg wiggle even while their playhead advanced normally.
            output.SetSourcePlayable(mixer);
            playableGraph.Play();
        }

        private AnimationClip GetLocomotionClip(TopDown3DLocomotionClipRole role)
        {
            return locomotionEntries[(int)role].Clip;
        }

        private void Update()
        {
            if (!playableGraph.IsValid() || motor == null)
            {
                return;
            }

            var snapshot = motor.LocomotionSnapshot;
            var planarSpeed = snapshot.CurrentPlanarVelocity.magnitude;
            smoothedPlanarSpeed = TopDown3DAnimationMath.SmoothValue(
                smoothedPlanarSpeed,
                planarSpeed,
                animationSpeedSharpness,
                Time.deltaTime);

            var actionIndex = ResolveActionIndex(snapshot, planarSpeed);
            System.Array.Clear(targetWeights, 0, targetWeights.Length);
            if (actionIndex >= 0)
            {
                targetWeights[actionIndex] = 1f;
                BeginActionIfNeeded(actionIndex, snapshot);
                UpdateActionPlayback(actionIndex, snapshot);
            }
            else
            {
                lastActionIndex = -1;
                UpdateLocomotionTargets(snapshot, smoothedPlanarSpeed);
            }

            var isLocomotionOneShot = actionIndex >= StartLeftIndex
                && actionIndex <= HardTurnRightIndex;
            var blendDuration = actionIndex >= 0 && !isLocomotionOneShot
                ? traversalCrossFadeDuration
                : isLocomotionOneShot
                    ? locomotionProfile.OneShotBlendDuration
                    : locomotionProfile.SustainedBlendDuration;
            BlendToTargetWeights(blendDuration);
        }

        private int ResolveActionIndex(TopDown3DLocomotionSnapshot snapshot, float planarSpeed)
        {
            if (IsGathering)
            {
                ResetTransitionState(TopDown3DLocomotionState.ConstrainedAction);
                return GatherIndex;
            }

            if (motor.IsActionConstrained)
            {
                ResetTransitionState(TopDown3DLocomotionState.ConstrainedAction);
                return -1;
            }

            switch (motor.ActiveTraversal)
            {
                case TopDown3DTraversalMove.SideStep:
                    ResetTransitionState(TopDown3DLocomotionState.Traversal);
                    return motor.ActiveTraversalSide < 0f
                        ? SideStepLeftIndex
                        : SideStepRightIndex;
                case TopDown3DTraversalMove.Vault:
                    ResetTransitionState(TopDown3DLocomotionState.Traversal);
                    return VaultIndex;
            }

            var absoluteHeadingError = Mathf.Abs(snapshot.SignedHeadingError);
            var releaseHardTurn = TopDown3DAnimationMath.ShouldReleaseHardTurnLatch(
                absoluteHeadingError,
                planarSpeed,
                locomotionProfile.PivotReleaseAngle,
                locomotionProfile.PivotEntrySpeed);
            if (activeHardTurnIndex >= 0)
            {
                activeHardTurnRemaining -= Time.deltaTime;
                if (activeHardTurnRemaining > 0f && !releaseHardTurn)
                {
                    CurrentLocomotionState = TopDown3DLocomotionState.Pivoting;
                    return activeHardTurnIndex;
                }

                activeHardTurnIndex = -1;
            }

            if (releaseHardTurn)
            {
                hardTurnLatched = false;
            }

            var hardTurnDirection = TopDown3DAnimationMath.SelectHardTurnDirection(
                snapshot.SignedHeadingError,
                planarSpeed,
                snapshot.IsGrounded,
                snapshot.TraversalOwnsMotion,
                hardTurnLatched,
                locomotionProfile.PivotEntryAngle,
                locomotionProfile.PivotEntrySpeed);
            if (hardTurnDirection != 0)
            {
                hardTurnLatched = true;
                activeHardTurnIndex = hardTurnDirection < 0
                    ? HardTurnLeftIndex
                    : HardTurnRightIndex;
                activeHardTurnRemaining = Mathf.Max(
                    0.01f,
                    clipPlayables[activeHardTurnIndex].GetAnimationClip().length);
                activeTransitionIndex = -1;
                CurrentLocomotionState = TopDown3DLocomotionState.Pivoting;
                return activeHardTurnIndex;
            }

            var desiredSpeed = snapshot.DesiredPlanarVelocity.magnitude;
            if (CurrentLocomotionState == TopDown3DLocomotionState.Starting
                && IsStartIndex(activeTransitionIndex)
                && HasReachedTransitionRelease(activeTransitionIndex))
            {
                activeTransitionIndex = -1;
                CurrentLocomotionState = TopDown3DLocomotionState.Sustained;
            }

            var groundedTravelState = TopDown3DAnimationMath.SelectGroundedTravelState(
                CurrentLocomotionState,
                planarSpeed,
                desiredSpeed,
                locomotionProfile.IdleExitSpeed,
                locomotionProfile.StartCompletionSpeed,
                locomotionProfile.StopIntentSpeed,
                locomotionProfile.StopEntrySpeed,
                locomotionProfile.IdleEntrySpeed);
            if (groundedTravelState == TopDown3DLocomotionState.Stopping)
            {
                if (CurrentLocomotionState == TopDown3DLocomotionState.Stopping
                    && IsStopIndex(activeTransitionIndex))
                {
                    return activeTransitionIndex;
                }

                return BeginTransition(
                    TopDown3DLocomotionState.Stopping,
                    StopLeftIndex,
                    StopRightIndex);
            }

            if (groundedTravelState == TopDown3DLocomotionState.Starting)
            {
                if (CurrentLocomotionState == TopDown3DLocomotionState.Starting
                    && IsStartIndex(activeTransitionIndex))
                {
                    return activeTransitionIndex;
                }

                return BeginTransition(
                    TopDown3DLocomotionState.Starting,
                    StartLeftIndex,
                    StartRightIndex);
            }

            if (groundedTravelState == TopDown3DLocomotionState.Idle
                && CurrentLocomotionState == TopDown3DLocomotionState.Stopping
                && IsStopIndex(activeTransitionIndex))
            {
                EmitOneShotPlantNow(activeTransitionIndex, snapshot);
            }

            activeTransitionIndex = -1;
            CurrentLocomotionState = groundedTravelState;
            return -1;
        }

        private int BeginTransition(
            TopDown3DLocomotionState state,
            int leftIndex,
            int rightIndex)
        {
            activeTransitionIndex = SelectOneShotIndex(leftIndex, rightIndex);
            CurrentLocomotionState = state;
            return activeTransitionIndex;
        }

        private bool HasReachedTransitionRelease(int actionIndex)
        {
            var entry = locomotionEntries[actionIndex];
            var clipLength = Mathf.Max(0.01f, entry.Clip.length);
            return clipPlayables[actionIndex].GetTime() / clipLength >= entry.ReleaseTime;
        }

        private static bool IsStartIndex(int index)
        {
            return index == StartLeftIndex || index == StartRightIndex;
        }

        private static bool IsStopIndex(int index)
        {
            return index == StopLeftIndex || index == StopRightIndex;
        }

        private int SelectOneShotIndex(int leftIndex, int rightIndex)
        {
            var runEntry = locomotionEntries[(int)TopDown3DLocomotionClipRole.Run];
            return TopDown3DAnimationMath.SelectNextPlantSide(
                    locomotionPhase,
                    runEntry.LeftContactPhases,
                    runEntry.RightContactPhases)
                == TopDown3DFootSide.Left
                    ? leftIndex
                    : rightIndex;
        }

        private void CancelHardTurn()
        {
            activeHardTurnIndex = -1;
            activeHardTurnRemaining = 0f;
            hardTurnLatched = false;
        }

        private void ResetTransitionState(TopDown3DLocomotionState state)
        {
            CancelHardTurn();
            activeTransitionIndex = -1;
            CurrentLocomotionState = state;
        }

        private void BeginActionIfNeeded(int actionIndex, TopDown3DLocomotionSnapshot snapshot)
        {
            if (lastActionIndex == actionIndex)
            {
                return;
            }

            var startPhase = 0f;
            if (IsStartIndex(actionIndex))
            {
                var entry = locomotionEntries[actionIndex];
                var accelerationMagnitude = Mathf.Max(0.1f, snapshot.PlanarAcceleration.magnitude);
                var timeToSustained = Mathf.Max(
                    0f,
                    locomotionProfile.StartCompletionSpeed
                        - snapshot.CurrentPlanarVelocity.magnitude)
                    / accelerationMagnitude;
                startPhase = Mathf.Clamp01(
                    entry.ReleaseTime - timeToSustained / Mathf.Max(0.01f, entry.Clip.length));
            }
            else if (IsStopIndex(actionIndex))
            {
                var brakingAcceleration = Mathf.Max(0.1f, snapshot.PlanarAcceleration.magnitude);
                var timeToIdle = Mathf.Max(
                    0f,
                    snapshot.CurrentPlanarVelocity.magnitude - locomotionProfile.IdleEntrySpeed)
                    / brakingAcceleration;
                var entry = locomotionEntries[actionIndex];
                var clipLength = Mathf.Max(0.01f, entry.Clip.length);
                startPhase = Mathf.Clamp01(entry.PlantTime - timeToIdle / clipLength);
            }
            else if (actionIndex == HardTurnLeftIndex
                || actionIndex == HardTurnRightIndex)
            {
                var brakingAcceleration = Mathf.Max(0.1f, snapshot.PlanarAcceleration.magnitude);
                var timeToZero = snapshot.CurrentPlanarVelocity.magnitude / brakingAcceleration;
                var entry = locomotionEntries[actionIndex];
                var clipLength = Mathf.Max(0.01f, entry.Clip.length);
                startPhase = Mathf.Clamp01(entry.PlantTime - timeToZero / clipLength);
            }

            var clip = clipPlayables[actionIndex].GetAnimationClip();
            clipPlayables[actionIndex].SetTime(startPhase * Mathf.Max(0f, clip.length));
            oneShotContactEmitted = false;
            lastActionIndex = actionIndex;
        }

        private void UpdateActionPlayback(
            int actionIndex,
            TopDown3DLocomotionSnapshot snapshot)
        {
            if (actionIndex >= StartLeftIndex && actionIndex <= HardTurnRightIndex)
            {
                clipPlayables[actionIndex].SetSpeed(1d);
                EmitOneShotContactIfDue(actionIndex, snapshot);
                return;
            }

            var clipLength = Mathf.Max(0.01f, clipPlayables[actionIndex].GetAnimationClip().length);
            float duration;
            if (actionIndex == GatherIndex)
            {
                duration = Mathf.Max(0.05f, activeGatherDuration);
            }
            else
            {
                duration = Mathf.Max(0.05f, motor.ActiveTraversalDuration);
            }

            clipPlayables[actionIndex].SetSpeed(clipLength / duration);
        }

        private void EmitOneShotContactIfDue(
            int actionIndex,
            TopDown3DLocomotionSnapshot snapshot)
        {
            if (oneShotContactEmitted || !snapshot.IsGrounded || snapshot.TraversalOwnsMotion)
            {
                return;
            }

            var entry = locomotionEntries[actionIndex];
            var clipLength = Mathf.Max(0.01f, entry.Clip.length);
            var normalizedTime = (float)(clipPlayables[actionIndex].GetTime() / clipLength);
            if (normalizedTime < entry.PlantTime)
            {
                return;
            }

            oneShotContactEmitted = true;
            var foot = entry.PlantedFoot == TopDown3DFootSide.Left ? leftFoot : rightFoot;
            EmitFootContact(entry.PlantedFoot, foot, snapshot.CurrentPlanarVelocity.magnitude);
        }

        private void EmitOneShotPlantNow(
            int actionIndex,
            TopDown3DLocomotionSnapshot snapshot)
        {
            if (oneShotContactEmitted || !snapshot.IsGrounded || snapshot.TraversalOwnsMotion)
            {
                return;
            }

            oneShotContactEmitted = true;
            var entry = locomotionEntries[actionIndex];
            var foot = entry.PlantedFoot == TopDown3DFootSide.Left ? leftFoot : rightFoot;
            EmitFootContact(entry.PlantedFoot, foot, snapshot.CurrentPlanarVelocity.magnitude);
        }

        private void UpdateLocomotionTargets(
            TopDown3DLocomotionSnapshot snapshot,
            float planarSpeed)
        {
            if (planarSpeed <= LocomotionStopSpeed)
            {
                targetWeights[IdleIndex] = 1f;
                clipPlayables[IdleIndex].SetSpeed(1f);
                return;
            }

            TopDown3DAnimationMath.CalculateGaitWeights(
                planarSpeed,
                locomotionEntries[(int)TopDown3DLocomotionClipRole.Walk].NominalForwardSpeed,
                locomotionEntries[(int)TopDown3DLocomotionClipRole.Run].NominalForwardSpeed,
                locomotionEntries[(int)TopDown3DLocomotionClipRole.Sprint].NominalForwardSpeed,
                out var walkWeight,
                out var runWeight,
                out var sprintWeight);
            var localDesiredVelocity = TopDown3DAnimationMath.ToLocalPlanarVelocity(
                snapshot.DesiredPlanarVelocity,
                snapshot.FacingDirection);
            var turnWeight = TopDown3DAnimationMath.CalculateDirectionalGaitWeight(
                localDesiredVelocity,
                snapshot.MeasuredYawRate,
                turnGaitStartLateralRatio,
                turnGaitFullLateralRatio,
                turnGaitStartYawRate,
                turnGaitFullYawRate,
                maximumTurnGaitWeight);
            var forwardWeight = 1f - turnWeight;
            targetWeights[WalkIndex] = walkWeight * forwardWeight;
            targetWeights[RunIndex] = runWeight * forwardWeight;
            targetWeights[SprintIndex] = sprintWeight * forwardWeight;
            if (TopDown3DAnimationMath.SelectDirectionalGaitSide(
                    localDesiredVelocity,
                    snapshot.MeasuredYawRate) < 0)
            {
                targetWeights[TurnGaitLeftIndex] = turnWeight;
            }
            else
            {
                targetWeights[TurnGaitRightIndex] = turnWeight;
            }

            var cyclesPerSecond = TopDown3DAnimationMath.CalculateGaitCyclesPerSecond(
                planarSpeed,
                locomotionEntries[(int)TopDown3DLocomotionClipRole.Walk].NominalForwardSpeed,
                locomotionEntries[(int)TopDown3DLocomotionClipRole.Run].NominalForwardSpeed,
                locomotionEntries[(int)TopDown3DLocomotionClipRole.Sprint].NominalForwardSpeed,
                1f,
                GetLocomotionClip(TopDown3DLocomotionClipRole.Walk).length,
                GetLocomotionClip(TopDown3DLocomotionClipRole.Run).length,
                GetLocomotionClip(TopDown3DLocomotionClipRole.Sprint).length,
                walkWeight,
                runWeight,
                sprintWeight);
            var dominantRole = TopDown3DAnimationMath.SelectDominantGaitRole(
                walkWeight,
                runWeight,
                sprintWeight);
            var dominantEntry = locomotionEntries[(int)dominantRole];
            cyclesPerSecond = TopDown3DAnimationMath.ClampCadenceToPlaybackBounds(
                cyclesPerSecond,
                dominantEntry.Clip.length,
                dominantEntry.MinimumPlaybackRate,
                dominantEntry.MaximumPlaybackRate);
            if (snapshot.DesiredPlanarVelocity.sqrMagnitude <= 0.0001f
                && snapshot.PlanarAcceleration.sqrMagnitude > 0.0001f)
            {
                var timeToStop = planarSpeed / snapshot.PlanarAcceleration.magnitude;
                cyclesPerSecond = TopDown3DAnimationMath.AlignCadenceToStopContact(
                    locomotionPhase,
                    cyclesPerSecond,
                    timeToStop);
            }

            var previousPhase = locomotionPhase;
            var phaseAdvance = Mathf.Min(0.95f, cyclesPerSecond * Time.deltaTime);
            locomotionPhase = Mathf.Repeat(locomotionPhase + phaseAdvance, 1f);
            SetPhaseDrivenGaitTimes(locomotionPhase);
            EmitCrossedFootContacts(previousPhase, locomotionPhase, planarSpeed, snapshot);
        }

        private void SetPhaseDrivenGaitTimes(float phase)
        {
            for (var i = WalkIndex; i <= TurnGaitRightIndex; i++)
            {
                var clip = clipPlayables[i].GetAnimationClip();
                var sourcePhase = TopDown3DAnimationMath.MapSemanticPhaseToSource(
                    phase,
                    locomotionEntries[i].LeftContactPhases,
                    locomotionEntries[i].RightContactPhases);
                clipPlayables[i].SetSpeed(0d);
                clipPlayables[i].SetTime(sourcePhase * Mathf.Max(0.01f, clip.length));
            }
        }

        private void BlendToTargetWeights(float duration)
        {
            var blend = 1f - Mathf.Exp(-4.6f * Time.deltaTime / Mathf.Max(0.01f, duration));
            var totalWeight = 0f;
            for (var i = 0; i < ClipCount; i++)
            {
                var weight = Mathf.Lerp(mixer.GetInputWeight(i), targetWeights[i], blend);
                mixer.SetInputWeight(i, weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0.0001f)
            {
                mixer.SetInputWeight(IdleIndex, 1f);
                return;
            }

            for (var i = 0; i < ClipCount; i++)
            {
                mixer.SetInputWeight(i, mixer.GetInputWeight(i) / totalWeight);
            }
        }

        private void EmitCrossedFootContacts(
            float previousPhase,
            float currentPhase,
            float planarSpeed,
            TopDown3DLocomotionSnapshot snapshot)
        {
            if (!snapshot.IsGrounded || snapshot.TraversalOwnsMotion || planarSpeed < TopDown3DFootstepDust.MinimumMovementSpeed)
            {
                return;
            }

            if (TopDown3DAnimationMath.DidPhaseCross(previousPhase, currentPhase, 0.25f)
                || TopDown3DAnimationMath.DidPhaseCross(previousPhase, currentPhase, 0.75f))
            {
                EmitFootContact(TopDown3DFootSide.Left, leftFoot, planarSpeed);
            }

            if (TopDown3DAnimationMath.DidPhaseCross(previousPhase, currentPhase, 0f)
                || TopDown3DAnimationMath.DidPhaseCross(previousPhase, currentPhase, 0.5f))
            {
                EmitFootContact(TopDown3DFootSide.Right, rightFoot, planarSpeed);
            }
        }

        private void EmitFootContact(TopDown3DFootSide side, Transform foot, float planarSpeed)
        {
            if (foot == null || !TryResolveFootGround(foot.position, out var position))
            {
                return;
            }

            FootContact?.Invoke(new TopDown3DFootContact(
                side,
                position,
                planarSpeed,
                Mathf.InverseLerp(
                    TopDown3DFootstepDust.MinimumMovementSpeed,
                    locomotionEntries[(int)TopDown3DLocomotionClipRole.Sprint].NominalForwardSpeed,
                    planarSpeed)));
        }

        private bool TryResolveFootGround(
            Vector3 animatedFootPosition,
            out Vector3 targetPosition)
        {
            targetPosition = animatedFootPosition;
            var origin = animatedFootPosition + Vector3.up * footProbeLift;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                footGroundHits,
                footProbeLift + footProbeDistance,
                motor.GroundMask,
                QueryTriggerInteraction.Ignore);
            var nearestDistance = float.PositiveInfinity;
            RaycastHit nearestHit = default;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = footGroundHits[i];
                if (hit.collider == null
                    || hit.collider.transform.IsChildOf(transform)
                    || hit.collider.GetComponentInParent<TopDown3DGroundSurface>() == null
                    || !TopDown3DSlopeMath.IsWalkable(hit.normal, motor.MaxWalkableSlope)
                    || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                nearestHit = hit;
            }

            if (nearestDistance == float.PositiveInfinity)
            {
                return false;
            }

            targetPosition = nearestHit.point + nearestHit.normal * footGroundClearance;
            return true;
        }

        private void OnDestroy()
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
        }

        private void OnDisable()
        {
            EndGather();
            ResetTransitionState(TopDown3DLocomotionState.Idle);
            lastActionIndex = -1;
            oneShotContactEmitted = false;
            locomotionPhase = 0f;
            smoothedPlanarSpeed = 0f;
        }

        private void OnValidate()
        {
            visualScale = Mathf.Max(0.01f, visualScale);
            animationSpeedSharpness = Mathf.Max(0.01f, animationSpeedSharpness);
            traversalCrossFadeDuration = Mathf.Max(0.01f, traversalCrossFadeDuration);
            maximumTurnGaitWeight = Mathf.Clamp01(maximumTurnGaitWeight);
            turnGaitStartLateralRatio = Mathf.Clamp01(turnGaitStartLateralRatio);
            turnGaitFullLateralRatio = Mathf.Clamp(
                turnGaitFullLateralRatio,
                turnGaitStartLateralRatio + 0.01f,
                1f);
            turnGaitStartYawRate = Mathf.Max(0f, turnGaitStartYawRate);
            turnGaitFullYawRate = Mathf.Max(turnGaitStartYawRate + 0.01f, turnGaitFullYawRate);
            footProbeLift = Mathf.Max(0.05f, footProbeLift);
            footProbeDistance = Mathf.Max(0.05f, footProbeDistance);
            footGroundClearance = Mathf.Max(0f, footGroundClearance);
        }
    }

    public static class TopDown3DAnimationMath
    {
        public static float SmoothValue(
            float current,
            float target,
            float sharpness,
            float deltaTime)
        {
            if (sharpness <= 0f || deltaTime <= 0f)
            {
                return target;
            }

            var blend = 1f - Mathf.Exp(-sharpness * deltaTime);
            return Mathf.Lerp(current, target, blend);
        }

        public static TopDown3DLocomotionState SelectGroundedTravelState(
            TopDown3DLocomotionState previousState,
            float currentSpeed,
            float desiredSpeed,
            float idleExitSpeed,
            float startCompletionSpeed,
            float stopIntentSpeed,
            float stopEntrySpeed,
            float idleEntrySpeed)
        {
            currentSpeed = Mathf.Max(0f, currentSpeed);
            desiredSpeed = Mathf.Max(0f, desiredSpeed);
            if (previousState == TopDown3DLocomotionState.Sustained)
            {
                if (desiredSpeed < stopIntentSpeed && currentSpeed > stopEntrySpeed)
                {
                    return TopDown3DLocomotionState.Stopping;
                }

                return currentSpeed > idleEntrySpeed || desiredSpeed >= idleExitSpeed
                    ? TopDown3DLocomotionState.Sustained
                    : TopDown3DLocomotionState.Idle;
            }

            if (previousState == TopDown3DLocomotionState.Stopping)
            {
                if (desiredSpeed >= idleExitSpeed)
                {
                    return currentSpeed < startCompletionSpeed
                        ? TopDown3DLocomotionState.Starting
                        : TopDown3DLocomotionState.Sustained;
                }

                return currentSpeed > idleEntrySpeed
                    ? TopDown3DLocomotionState.Stopping
                    : TopDown3DLocomotionState.Idle;
            }

            if (previousState == TopDown3DLocomotionState.Starting)
            {
                if (desiredSpeed < stopIntentSpeed)
                {
                    return currentSpeed > stopEntrySpeed
                        ? TopDown3DLocomotionState.Stopping
                        : TopDown3DLocomotionState.Idle;
                }

                return currentSpeed < startCompletionSpeed
                    ? TopDown3DLocomotionState.Starting
                    : TopDown3DLocomotionState.Sustained;
            }

            if (desiredSpeed < stopIntentSpeed && currentSpeed > stopEntrySpeed)
            {
                return TopDown3DLocomotionState.Stopping;
            }

            if (desiredSpeed >= idleExitSpeed && currentSpeed < startCompletionSpeed)
            {
                return TopDown3DLocomotionState.Starting;
            }

            return currentSpeed > idleEntrySpeed || desiredSpeed >= idleExitSpeed
                ? TopDown3DLocomotionState.Sustained
                : TopDown3DLocomotionState.Idle;
        }

        public static void CalculateGaitWeights(
            float planarSpeed,
            float walkSpeed,
            float runSpeed,
            float sprintSpeed,
            out float walkWeight,
            out float runWeight,
            out float sprintWeight)
        {
            planarSpeed = Mathf.Max(0f, planarSpeed);
            walkSpeed = Mathf.Max(0.01f, walkSpeed);
            runSpeed = Mathf.Max(walkSpeed + 0.01f, runSpeed);
            sprintSpeed = Mathf.Max(runSpeed + 0.01f, sprintSpeed);
            if (planarSpeed <= walkSpeed)
            {
                walkWeight = 1f;
                runWeight = 0f;
                sprintWeight = 0f;
                return;
            }

            if (planarSpeed <= runSpeed)
            {
                runWeight = Mathf.InverseLerp(walkSpeed, runSpeed, planarSpeed);
                walkWeight = 1f - runWeight;
                sprintWeight = 0f;
                return;
            }

            sprintWeight = Mathf.InverseLerp(runSpeed, sprintSpeed, planarSpeed);
            runWeight = 1f - sprintWeight;
            walkWeight = 0f;
        }

        public static float CalculateTurnGaitWeight(
            float signedHeadingError,
            float startAngle,
            float fullAngle,
            float maximumWeight)
        {
            var weight = Mathf.InverseLerp(
                Mathf.Max(0f, startAngle),
                Mathf.Max(startAngle + 0.01f, fullAngle),
                Mathf.Abs(signedHeadingError));
            return weight * Mathf.Clamp01(maximumWeight);
        }

        public static Vector2 ToLocalPlanarVelocity(
            Vector3 worldVelocity,
            Vector3 worldFacing)
        {
            var forward = Vector3.ProjectOnPlane(worldFacing, Vector3.up);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);
            return new Vector2(
                Vector3.Dot(worldVelocity, right),
                Vector3.Dot(worldVelocity, forward));
        }

        public static float CalculateDirectionalGaitWeight(
            Vector2 localDesiredVelocity,
            float measuredYawRate,
            float startLateralRatio,
            float fullLateralRatio,
            float startYawRate,
            float fullYawRate,
            float maximumWeight)
        {
            var desiredMagnitude = localDesiredVelocity.magnitude;
            var lateralRatio = desiredMagnitude > 0.0001f
                ? Mathf.Abs(localDesiredVelocity.x) / desiredMagnitude
                : 0f;
            var trajectoryWeight = Mathf.InverseLerp(
                Mathf.Clamp01(startLateralRatio),
                Mathf.Clamp(fullLateralRatio, startLateralRatio + 0.0001f, 1f),
                lateralRatio);
            var yawWeight = Mathf.InverseLerp(
                Mathf.Max(0f, startYawRate),
                Mathf.Max(startYawRate + 0.0001f, fullYawRate),
                Mathf.Abs(measuredYawRate));
            return Mathf.Max(trajectoryWeight, yawWeight) * Mathf.Clamp01(maximumWeight);
        }

        public static int SelectDirectionalGaitSide(
            Vector2 localDesiredVelocity,
            float measuredYawRate)
        {
            if (Mathf.Abs(localDesiredVelocity.x) > 0.01f)
            {
                return localDesiredVelocity.x < 0f ? -1 : 1;
            }

            return measuredYawRate < 0f ? -1 : 1;
        }

        public static TopDown3DLocomotionClipRole SelectDominantGaitRole(
            float walkWeight,
            float runWeight,
            float sprintWeight)
        {
            if (walkWeight >= runWeight && walkWeight >= sprintWeight)
            {
                return TopDown3DLocomotionClipRole.Walk;
            }

            return runWeight >= sprintWeight
                ? TopDown3DLocomotionClipRole.Run
                : TopDown3DLocomotionClipRole.Sprint;
        }

        public static float ClampCadenceToPlaybackBounds(
            float cyclesPerSecond,
            float clipLength,
            float minimumPlaybackRate,
            float maximumPlaybackRate)
        {
            clipLength = Mathf.Max(0.01f, clipLength);
            minimumPlaybackRate = Mathf.Max(0.01f, minimumPlaybackRate);
            maximumPlaybackRate = Mathf.Max(minimumPlaybackRate, maximumPlaybackRate);
            cyclesPerSecond = Mathf.Max(0f, cyclesPerSecond);
            var minimumCadence = minimumPlaybackRate / clipLength;
            if (cyclesPerSecond < minimumCadence)
            {
                // Partial analog travel is intentional motion below the authored
                // nominal gait, not a clip-matching correction to force upward.
                return cyclesPerSecond;
            }

            return Mathf.Min(cyclesPerSecond, maximumPlaybackRate / clipLength);
        }

        public static float CalculateGaitCyclesPerSecond(
            float planarSpeed,
            float walkSpeed,
            float runSpeed,
            float sprintSpeed,
            float runPlaybackScale,
            float walkClipLength,
            float runClipLength,
            float sprintClipLength,
            float walkWeight,
            float runWeight,
            float sprintWeight)
        {
            var walkCycles = planarSpeed / Mathf.Max(0.01f, walkSpeed)
                / Mathf.Max(0.01f, walkClipLength);
            var runCycles = planarSpeed / Mathf.Max(0.01f, runSpeed)
                * Mathf.Max(0f, runPlaybackScale)
                / Mathf.Max(0.01f, runClipLength);
            var sprintCycles = planarSpeed / Mathf.Max(0.01f, sprintSpeed)
                / Mathf.Max(0.01f, sprintClipLength);
            return Mathf.Max(
                0f,
                walkCycles * Mathf.Max(0f, walkWeight)
                    + runCycles * Mathf.Max(0f, runWeight)
                    + sprintCycles * Mathf.Max(0f, sprintWeight));
        }

        public static bool DidPhaseCross(float previousPhase, float currentPhase, float contactPhase)
        {
            previousPhase = Mathf.Repeat(previousPhase, 1f);
            currentPhase = Mathf.Repeat(currentPhase, 1f);
            contactPhase = Mathf.Repeat(contactPhase, 1f);
            return currentPhase >= previousPhase
                ? contactPhase > previousPhase && contactPhase <= currentPhase
                : contactPhase > previousPhase || contactPhase <= currentPhase;
        }

        public static float MapSemanticPhaseToSource(
            float semanticPhase,
            float[] leftContactPhases,
            float[] rightContactPhases)
        {
            if (leftContactPhases == null
                || rightContactPhases == null
                || leftContactPhases.Length != 2
                || rightContactPhases.Length != 2)
            {
                return Mathf.Repeat(semanticPhase, 1f);
            }

            semanticPhase = Mathf.Repeat(semanticPhase, 1f);
            var segment = Mathf.Min(3, Mathf.FloorToInt(semanticPhase * 4f));
            var segmentProgress = semanticPhase * 4f - segment;
            var sourceStart = GetAlternatingContact(segment, leftContactPhases, rightContactPhases);
            var sourceEnd = segment == 3
                ? GetAlternatingContact(0, leftContactPhases, rightContactPhases) + 1f
                : GetAlternatingContact(segment + 1, leftContactPhases, rightContactPhases);
            if (sourceEnd <= sourceStart)
            {
                sourceEnd += 1f;
            }

            return Mathf.Repeat(Mathf.Lerp(sourceStart, sourceEnd, segmentProgress), 1f);
        }

        private static float GetAlternatingContact(
            int index,
            float[] leftContactPhases,
            float[] rightContactPhases)
        {
            return index switch
            {
                0 => rightContactPhases[0],
                1 => leftContactPhases[0],
                2 => rightContactPhases[1],
                _ => leftContactPhases[1],
            };
        }

        public static TopDown3DFootSide SelectNextPlantSide(
            float semanticPhase,
            float[] leftContactPhases,
            float[] rightContactPhases)
        {
            if (leftContactPhases == null
                || rightContactPhases == null
                || leftContactPhases.Length == 0
                || rightContactPhases.Length == 0)
            {
                return TopDown3DFootSide.Left;
            }

            var sourcePhase = MapSemanticPhaseToSource(
                semanticPhase,
                leftContactPhases,
                rightContactPhases);
            var leftDistance = NextCircularDistance(sourcePhase, leftContactPhases);
            var rightDistance = NextCircularDistance(sourcePhase, rightContactPhases);
            return leftDistance <= rightDistance
                ? TopDown3DFootSide.Left
                : TopDown3DFootSide.Right;
        }

        private static float NextCircularDistance(float phase, float[] contacts)
        {
            var nearest = 1f;
            for (var i = 0; i < contacts.Length; i++)
            {
                var distance = Mathf.Repeat(contacts[i] - phase, 1f);
                if (distance <= 0.0001f)
                {
                    distance = 1f;
                }

                nearest = Mathf.Min(nearest, distance);
            }

            return nearest;
        }

        public static int SelectHardTurnDirection(
            float signedHeadingError,
            float planarSpeed,
            bool isGrounded,
            bool traversalOwnsMotion,
            bool hardTurnLatched,
            float enterAngle,
            float minimumSpeed)
        {
            if (!isGrounded
                || traversalOwnsMotion
                || hardTurnLatched
                || planarSpeed < Mathf.Max(0f, minimumSpeed)
                || Mathf.Abs(signedHeadingError) < Mathf.Clamp(enterAngle, 0f, 180f))
            {
                return 0;
            }

            return signedHeadingError < 0f ? -1 : 1;
        }

        public static bool ShouldReleaseHardTurnLatch(
            float absoluteHeadingError,
            float planarSpeed,
            float exitAngle,
            float minimumSpeed)
        {
            return Mathf.Abs(absoluteHeadingError) <= Mathf.Clamp(exitAngle, 0f, 180f)
                || planarSpeed < Mathf.Max(0f, minimumSpeed) * 0.6f;
        }

        public static float AlignCadenceToStopContact(
            float currentPhase,
            float cyclesPerSecond,
            float timeToStop)
        {
            cyclesPerSecond = Mathf.Max(0f, cyclesPerSecond);
            if (cyclesPerSecond <= 0f || timeToStop <= 0.0001f)
            {
                return cyclesPerSecond;
            }

            currentPhase = Mathf.Repeat(currentPhase, 1f);
            var phaseToLeft = Mathf.Repeat(1f - currentPhase, 1f);
            if (phaseToLeft <= 0.0001f)
            {
                phaseToLeft = 1f;
            }

            var phaseToRight = Mathf.Repeat(0.5f - currentPhase, 1f);
            if (phaseToRight <= 0.0001f)
            {
                phaseToRight = 1f;
            }

            var phaseToNextContact = Mathf.Min(phaseToLeft, phaseToRight);
            var alignedCadence = phaseToNextContact / timeToStop;
            return Mathf.Clamp(alignedCadence, cyclesPerSecond * 0.8f, cyclesPerSecond * 1.2f);
        }
    }
}
