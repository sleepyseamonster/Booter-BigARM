using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
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
        private const int SideStepLeftIndex = 4;
        private const int SideStepRightIndex = 5;
        private const int VaultIndex = 6;
        private const int ClipCount = 7;

        [Header("Prototype Humanoid")]
        [SerializeField] private GameObject humanoidPrefab;
        [SerializeField] private Vector3 visualLocalPosition =
            new Vector3(0f, PrototypeVisualGroundOffset, 0f);
        [SerializeField] private Vector3 visualLocalEulerAngles;
        [SerializeField, Min(0.01f)] private float visualScale = PrototypeVisualScale;

        [Header("Animation Clips")]
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip walkClip;
        [SerializeField] private AnimationClip runClip;
        [SerializeField] private AnimationClip sprintClip;
        [SerializeField] private AnimationClip sideStepLeftClip;
        [SerializeField] private AnimationClip sideStepRightClip;
        [SerializeField] private AnimationClip vaultClip;

        [Header("Locomotion Blend")]
        [SerializeField, Min(0.01f)] private float walkToRunThreshold = 2.4f;
        [SerializeField, Min(0.01f)] private float nominalWalkSpeed = 1.8f;
        [SerializeField, Min(0.01f)] private float nominalRunSpeed = 4.2f;
        [SerializeField, Min(0.01f)] private float nominalSprintSpeed = 7.4f;
        [SerializeField, Range(0.5f, 1.25f)] private float runPlaybackScale = 0.9f;
        [SerializeField, Min(0.01f)] private float crossFadeDuration = 0.1f;

        private readonly AnimationClipPlayable[] clipPlayables = new AnimationClipPlayable[ClipCount];
        private TopDown3DPlayerMotor motor;
        private GameObject visualInstance;
        private PlayableGraph playableGraph;
        private AnimationMixerPlayable mixer;
        private int activeClipIndex = -1;

        public bool HasCoreAnimationSet => humanoidPrefab != null
            && idleClip != null
            && walkClip != null
            && runClip != null
            && sprintClip != null;
        public bool HasCompleteAnimationSet => HasCoreAnimationSet
            && sideStepLeftClip != null
            && sideStepRightClip != null
            && vaultClip != null;
        public Vector3 VisualLocalPosition => visualLocalPosition;
        public float VisualScale => visualScale;

        public void Configure(
            GameObject modelPrefab,
            AnimationClip idle,
            AnimationClip walk,
            AnimationClip run,
            AnimationClip sprint,
            AnimationClip sideStepLeft,
            AnimationClip sideStepRight,
            AnimationClip vault)
        {
            humanoidPrefab = modelPrefab;
            idleClip = idle;
            walkClip = walk;
            runClip = run;
            sprintClip = sprint;
            sideStepLeftClip = sideStepLeft;
            sideStepRightClip = sideStepRight;
            vaultClip = vault;
        }

        private void Awake()
        {
            motor = GetComponent<TopDown3DPlayerMotor>();
            if (!HasCoreAnimationSet)
            {
                Debug.LogError(
                    "Booter's core prototype Humanoid model or locomotion set is incomplete.",
                    this);
                ShowGreyboxFallback();
                enabled = false;
                return;
            }

            ResolveTraversalAnimationFallbacks();
            CreateVisual();
        }

        private void ResolveTraversalAnimationFallbacks()
        {
            if (sideStepLeftClip != null && sideStepRightClip != null && vaultClip != null)
            {
                return;
            }

            Debug.LogWarning(
                "Booter's traversal animation references are incomplete in the loaded scene. "
                + "Using the run clip as a temporary visual fallback until the scene reloads.",
                this);
            if (sideStepLeftClip == null)
            {
                sideStepLeftClip = runClip;
            }

            if (sideStepRightClip == null)
            {
                sideStepRightClip = runClip;
            }

            if (vaultClip == null)
            {
                vaultClip = runClip;
            }
        }

        private void CreateVisual()
        {
            visualInstance = Instantiate(humanoidPrefab, transform, false);

            visualInstance.name = "Booter Prototype Humanoid Visual";
            visualInstance.transform.SetLocalPositionAndRotation(
                visualLocalPosition,
                Quaternion.Euler(visualLocalEulerAngles));
            visualInstance.transform.localScale = Vector3.one * visualScale;

            var animator = visualInstance.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("Booter's prototype Humanoid prefab does not contain an Animator.", this);
                Destroy(visualInstance);
                ShowGreyboxFallback();
                enabled = false;
                return;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            DisableGreyboxVisuals();
            CreatePlayableGraph(animator);
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

        private void ShowGreyboxFallback()
        {
            var capsuleRenderer = GetComponent<MeshRenderer>();
            if (capsuleRenderer != null)
            {
                capsuleRenderer.enabled = true;
            }

            var facingMarker = transform.Find("Facing Marker");
            if (facingMarker != null)
            {
                facingMarker.gameObject.SetActive(true);
            }
        }

        private void CreatePlayableGraph(Animator animator)
        {
            var clips = new[]
            {
                idleClip,
                walkClip,
                runClip,
                sprintClip,
                sideStepLeftClip,
                sideStepRightClip,
                vaultClip
            };
            playableGraph = PlayableGraph.Create("Booter Prototype Humanoid Animation");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(playableGraph, ClipCount);

            for (var i = 0; i < clips.Length; i++)
            {
                var playable = AnimationClipPlayable.Create(playableGraph, clips[i]);
                playable.SetApplyFootIK(i <= SprintIndex);
                playable.SetApplyPlayableIK(false);
                playableGraph.Connect(playable, 0, mixer, i);
                mixer.SetInputWeight(i, i == IdleIndex ? 1f : 0f);
                clipPlayables[i] = playable;
            }

            var output = AnimationPlayableOutput.Create(playableGraph, "Booter Humanoid", animator);
            output.SetSourcePlayable(mixer);
            activeClipIndex = IdleIndex;
            playableGraph.Play();
        }

        private void Update()
        {
            if (!playableGraph.IsValid() || motor == null)
            {
                return;
            }

            var desiredClipIndex = SelectClipIndex();
            if (desiredClipIndex != activeClipIndex)
            {
                activeClipIndex = desiredClipIndex;
                clipPlayables[desiredClipIndex].SetTime(0d);
            }

            UpdatePlaybackSpeed(desiredClipIndex);
            var blendStep = Time.deltaTime / Mathf.Max(0.01f, crossFadeDuration);
            for (var i = 0; i < ClipCount; i++)
            {
                var targetWeight = i == desiredClipIndex ? 1f : 0f;
                mixer.SetInputWeight(
                    i,
                    Mathf.MoveTowards(mixer.GetInputWeight(i), targetWeight, blendStep));
            }
        }

        private int SelectClipIndex()
        {
            switch (motor.ActiveTraversal)
            {
                case TopDown3DTraversalMove.SideStep:
                    return motor.ActiveTraversalSide < 0f
                        ? SideStepLeftIndex
                        : SideStepRightIndex;
                case TopDown3DTraversalMove.Vault:
                    return VaultIndex;
            }

            var velocity = motor.Velocity;
            var planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            if (planarSpeed <= 0.08f)
            {
                return IdleIndex;
            }

            if (motor.SprintActive)
            {
                return SprintIndex;
            }

            return planarSpeed >= walkToRunThreshold ? RunIndex : WalkIndex;
        }

        private void UpdatePlaybackSpeed(int clipIndex)
        {
            var velocity = motor.Velocity;
            var planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            float playbackSpeed;
            switch (clipIndex)
            {
                case WalkIndex:
                    playbackSpeed = planarSpeed / nominalWalkSpeed;
                    break;
                case RunIndex:
                    playbackSpeed = planarSpeed / nominalRunSpeed * runPlaybackScale;
                    break;
                case SprintIndex:
                    playbackSpeed = planarSpeed / nominalSprintSpeed;
                    break;
                case SideStepLeftIndex:
                case SideStepRightIndex:
                case VaultIndex:
                    var duration = Mathf.Max(0.05f, motor.ActiveTraversalDuration);
                    playbackSpeed = clipPlayables[clipIndex].GetAnimationClip().length / duration;
                    break;
                default:
                    playbackSpeed = 1f;
                    break;
            }

            clipPlayables[clipIndex].SetSpeed(Mathf.Clamp(playbackSpeed, 0.65f, 6f));
        }

        private void OnDestroy()
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
        }

        private void OnValidate()
        {
            visualScale = Mathf.Max(0.01f, visualScale);
            walkToRunThreshold = Mathf.Max(0.01f, walkToRunThreshold);
            nominalWalkSpeed = Mathf.Max(0.01f, nominalWalkSpeed);
            nominalRunSpeed = Mathf.Max(nominalWalkSpeed, nominalRunSpeed);
            nominalSprintSpeed = Mathf.Max(nominalRunSpeed, nominalSprintSpeed);
            runPlaybackScale = Mathf.Clamp(runPlaybackScale, 0.5f, 1.25f);
            crossFadeDuration = Mathf.Max(0.01f, crossFadeDuration);
        }
    }
}
