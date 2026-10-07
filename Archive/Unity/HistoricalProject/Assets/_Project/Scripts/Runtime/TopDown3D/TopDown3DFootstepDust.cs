using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(TopDown3DPlayerMotor),
        typeof(CapsuleCollider),
        typeof(TopDown3DPlayerAnimationDriver))]
    [DefaultExecutionOrder(120)]
    public sealed class TopDown3DFootstepDust : MonoBehaviour
    {
        public const float MinimumMovementSpeed = 0.35f;
        public const int DefaultMinimumParticlesPerStep = 10;
        public const int DefaultMaximumParticlesPerStep = 20;
        public const float DefaultClearAirDustStrength = 0.8f;

        [Header("Dust Burst")]
        [SerializeField, Range(2, 24)] private int minimumParticlesPerStep =
            DefaultMinimumParticlesPerStep;
        [SerializeField, Range(2, 24)] private int maximumParticlesPerStep =
            DefaultMaximumParticlesPerStep;
        [SerializeField, Range(0f, 1f)] private float clearAirDustStrength =
            DefaultClearAirDustStrength;
        [SerializeField, Min(0f)] private float radialSpeed = 0.34f;
        [SerializeField, Min(0f)] private float upwardSpeed = 0.52f;

        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver animationDriver;
        private ParticleSystem dustParticles;
        private Material dustMaterial;
        private Texture2D dustTexture;
        private uint burstSequence;

        public static int EvaluateBurstCount(
            float planarSpeed,
            float regionalDustIntensity,
            int minimumCount = DefaultMinimumParticlesPerStep,
            int maximumCount = DefaultMaximumParticlesPerStep)
        {
            var lower = Mathf.Max(1, Mathf.Min(minimumCount, maximumCount));
            var upper = Mathf.Max(lower, maximumCount);
            var speedWeight = Mathf.InverseLerp(
                MinimumMovementSpeed,
                7.4f,
                Mathf.Max(0f, planarSpeed));
            var dustWeight = Mathf.Clamp01(
                regionalDustIntensity / TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity);
            return Mathf.RoundToInt(Mathf.Lerp(lower, upper, speedWeight * 0.58f + dustWeight * 0.42f));
        }

        public static float EvaluateDustStrength(
            float regionalDustIntensity,
            float clearAirStrength = DefaultClearAirDustStrength)
        {
            var pocketWeight = Mathf.Clamp01(
                regionalDustIntensity / TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity);
            return Mathf.Lerp(Mathf.Clamp01(clearAirStrength), 1f, pocketWeight);
        }

        private void Awake()
        {
            motor = GetComponent<TopDown3DPlayerMotor>();
            animationDriver = GetComponent<TopDown3DPlayerAnimationDriver>();
        }

        private void OnEnable()
        {
            if (animationDriver != null)
            {
                animationDriver.FootContact -= HandleFootContact;
                animationDriver.FootContact += HandleFootContact;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            EnsureParticleSystem();
            if (dustParticles != null && !dustParticles.isPlaying)
            {
                dustParticles.Play();
            }
        }

        private void HandleFootContact(TopDown3DFootContact contact)
        {
            if (motor == null
                || !motor.IsGrounded
                || motor.ActiveTraversal != TopDown3DTraversalMove.None
                || contact.PlanarSpeed < MinimumMovementSpeed)
            {
                return;
            }

            EmitFootstep(contact);
        }

        private void OnDisable()
        {
            if (animationDriver != null)
            {
                animationDriver.FootContact -= HandleFootContact;
            }

            if (dustParticles != null)
            {
                dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnDestroy()
        {
            if (dustMaterial != null)
            {
                Destroy(dustMaterial);
            }

            if (dustTexture != null)
            {
                Destroy(dustTexture);
            }
        }

        private void EmitFootstep(TopDown3DFootContact contact)
        {
            EnsureParticleSystem();
            if (dustParticles == null)
            {
                return;
            }

            burstSequence++;
            var flattenedForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var contactPosition = contact.WorldPosition;

            var atmosphere = TopDown3DDustAtmosphere.Active;
            var regionalIntensity = 0f;
            var dustTint = TopDown3DDustAtmosphere.DefaultRustParticleDust;
            if (atmosphere != null)
            {
                var sample = atmosphere.SampleAtPosition(contactPosition);
                regionalIntensity = sample.Intensity;
                dustTint = Color.Lerp(dustTint, sample.Tint, 0.45f);
            }

            var strength = EvaluateDustStrength(regionalIntensity, clearAirDustStrength);
            var particleCount = EvaluateBurstCount(
                contact.PlanarSpeed,
                regionalIntensity,
                minimumParticlesPerStep,
                maximumParticlesPerStep);
            var planarVelocity = motor.LocomotionSnapshot.CurrentPlanarVelocity;
            var movementDirection = planarVelocity.sqrMagnitude > 0.0001f
                ? planarVelocity.normalized
                : flattenedForward;
            var seed = StableHash(
                Mathf.RoundToInt(contactPosition.x * 16f),
                Mathf.RoundToInt(contactPosition.z * 16f),
                (int)burstSequence);

            for (var particleIndex = 0; particleIndex < particleCount; particleIndex++)
            {
                var angle = Hash01(seed + particleIndex * 3571) * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var radiusWeight = Hash01(seed + particleIndex * 5741 + 17);
                var riseWeight = Hash01(seed + particleIndex * 7919 + 31);
                var lifetimeWeight = Hash01(seed + particleIndex * 10301 + 47);
                var sizeWeight = Hash01(seed + particleIndex * 12289 + 61);
                var alphaWeight = Hash01(seed + particleIndex * 14207 + 79);

                var emit = new ParticleSystem.EmitParams
                {
                    position = contactPosition + radial * Mathf.Lerp(0.015f, 0.13f, radiusWeight),
                    velocity = radial * radialSpeed * Mathf.Lerp(0.35f, 1f, radiusWeight)
                        + Vector3.up * upwardSpeed * Mathf.Lerp(0.42f, 1f, riseWeight)
                        - movementDirection * Mathf.Lerp(0.03f, 0.15f, strength),
                    startLifetime = Mathf.Lerp(0.58f, 1.18f, lifetimeWeight) * Mathf.Lerp(0.82f, 1.08f, strength),
                    startSize = Mathf.Lerp(0.055f, 0.24f, sizeWeight) * Mathf.Lerp(0.78f, 1.2f, strength),
                    randomSeed = (uint)StableHash(seed, particleIndex, 0x4D2A91),
                };
                dustTint.a = Mathf.Lerp(0.22f, 0.42f, alphaWeight) * strength;
                emit.startColor = dustTint;
                dustParticles.Emit(emit, 1);
            }
        }

        private void EnsureParticleSystem()
        {
            if (dustParticles != null || !Application.isPlaying)
            {
                return;
            }

            dustTexture = TopDown3DDustAtmosphere.CreateSoftDustTexture();
            dustMaterial = TopDown3DDustAtmosphere.CreateParticleMaterial(dustTexture, false);
            if (dustMaterial == null)
            {
                Debug.LogError(
                    $"Footstep dust requires shader '{TopDown3DDustAtmosphere.SoftVeilShaderName}'.",
                    this);
                Destroy(dustTexture);
                dustTexture = null;
                enabled = false;
                return;
            }

            dustMaterial.name = "Runtime Footstep Dust Material";
            var particleObject = new GameObject("Footstep Dust Particles")
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave,
            };
            particleObject.transform.SetParent(transform, false);
            dustParticles = particleObject.AddComponent<ParticleSystem>();

            var main = dustParticles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 192;
            main.startSpeed = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.58f, 1.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.24f);

            var emission = dustParticles.emission;
            emission.enabled = false;
            var shape = dustParticles.shape;
            shape.enabled = false;

            var colorOverLifetime = dustParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.08f),
                    new GradientAlphaKey(0.66f, 0.42f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = fade;

            var sizeOverLifetime = dustParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.42f),
                    new Keyframe(0.28f, 0.88f),
                    new Keyframe(1f, 1.3f)));

            var noise = dustParticles.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.strength = 0.11f;
            noise.frequency = 0.48f;
            noise.scrollSpeed = 0.22f;
            noise.damping = true;
            noise.octaveCount = 2;

            var renderer = dustParticles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = dustMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = -0.05f;

            dustParticles.Play();
        }

        private static int StableHash(int a, int b, int c)
        {
            unchecked
            {
                var hash = (uint)a;
                hash ^= (uint)b * 0x9E3779B9u;
                hash = (hash << 13) | (hash >> 19);
                hash ^= (uint)c * 0x85EBCA6Bu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return (int)hash;
            }
        }

        private static float Hash01(int hash)
        {
            unchecked
            {
                var value = (uint)hash;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                return (value & 0x00FFFFFFu) / 16777215f;
            }
        }

    }
}
