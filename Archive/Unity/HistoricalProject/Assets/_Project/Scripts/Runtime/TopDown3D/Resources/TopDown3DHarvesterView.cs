using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DHarvesterView : MonoBehaviour, ITopDown3DInteractable
    {
        private static Material bodyMaterial;
        private static Material coilMaterial;
        private static Material dustMaterial;
        private static Material particleMaterial;

        private TopDown3DPlacedHarvesterState state;
        private string stableId;
        private Transform fill;
        private Transform coil;
        private ParticleSystem motes;
        private int displayedDust = -1;
        private int capacity = 1;

        public Object UnityObject => this;
        public string StableId => stableId;
        public string Prompt => $"Pick up dust canister ({displayedDust}/{capacity})";
        public Vector3 InteractionPoint => transform.position + Vector3.up * 0.35f;
        public float InteractionRange => state != null && state.Settings != null
            ? state.Settings.PickupRange : 2.2f;
        public float ActionDuration => 0.75f;
        public TopDown3DItemAmount Reward => default;
        public bool IsAvailable => state != null && state.TryGet(stableId, out _);
        public bool TryConsume(out int remainingUses)
        {
            remainingUses = IsAvailable ? 1 : 0;
            return false;
        }

        public static TopDown3DHarvesterView Create(string id,
            TopDown3DPlacedHarvesterState owner, Transform chunk, Vector3 localPosition)
        {
            var root = new GameObject("Micro Dust Harvester");
            root.transform.SetParent(chunk, false);
            root.transform.position = localPosition;
            var collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            collider.center = Vector3.up * 0.34f;
            var view = root.AddComponent<TopDown3DHarvesterView>();
            view.stableId = id;
            view.state = owner;
            view.BuildVisuals();
            return view;
        }

        public void Refresh(int amount, int maximum)
        {
            displayedDust = Mathf.Clamp(amount, 0, Mathf.Max(1, maximum));
            capacity = Mathf.Max(1, maximum);
            if (fill == null) return;
            var ratio = (float)displayedDust / capacity;
            fill.localScale = new Vector3(0.25f, Mathf.Max(0.012f, ratio * 0.19f), 0.25f);
            fill.localPosition = new Vector3(0f, 0.15f + ratio * 0.19f, 0f);
            fill.gameObject.SetActive(displayedDust > 0);
            if (motes != null)
            {
                var emission = motes.emission;
                emission.enabled = displayedDust < capacity;
            }
        }

        private void Update()
        {
            if (coil != null && displayedDust < capacity)
                coil.Rotate(Vector3.up, 65f * Time.deltaTime, Space.Self);
        }

        private void BuildVisuals()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (bodyMaterial == null) bodyMaterial = MakeMaterial(shader, new Color(0.17f, 0.19f, 0.19f));
            if (coilMaterial == null) coilMaterial = MakeMaterial(shader, new Color(0.54f, 0.28f, 0.11f));
            if (dustMaterial == null) dustMaterial = MakeMaterial(shader, new Color(0.77f, 0.43f, 0.18f));
            if (particleMaterial == null)
                particleMaterial = MakeMaterial(
                    Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? shader,
                    new Color(0.88f, 0.52f, 0.25f));

            MakePart("Base", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f),
                new Vector3(0.42f, 0.08f, 0.42f), bodyMaterial);
            MakePart("Lower Chamber Rim", PrimitiveType.Cylinder,
                new Vector3(0f, 0.16f, 0f), new Vector3(0.30f, 0.025f, 0.30f), coilMaterial);
            MakePart("Upper Chamber Rim", PrimitiveType.Cylinder,
                new Vector3(0f, 0.52f, 0f), new Vector3(0.30f, 0.025f, 0.30f), coilMaterial);
            for (var i = 0; i < 4; i++)
            {
                var angle = i * Mathf.PI * 0.5f;
                MakePart($"Chamber Strut {i + 1}", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(angle) * 0.23f, 0.34f, Mathf.Sin(angle) * 0.23f),
                    new Vector3(0.045f, 0.34f, 0.045f), bodyMaterial);
            }
            fill = MakePart("Visible Dust Fill", PrimitiveType.Cylinder,
                new Vector3(0f, 0.15f, 0f), new Vector3(0.25f, 0.012f, 0.25f), dustMaterial);
            coil = MakePart("Electromagnetic Coil", PrimitiveType.Cylinder,
                new Vector3(0f, 0.56f, 0f), new Vector3(0.34f, 0.035f, 0.34f), coilMaterial);
            var emitter = new GameObject("Inward Dust Motes");
            emitter.transform.SetParent(transform, false);
            emitter.transform.localPosition = Vector3.up * 0.5f;
            motes = emitter.AddComponent<ParticleSystem>();
            var main = motes.main;
            main.loop = true;
            main.startLifetime = 0.75f;
            main.startSpeed = 0f;
            main.startSize = 0.055f;
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new Color(0.82f, 0.45f, 0.19f, 0.75f);
            var shape = motes.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;
            var velocity = motes.velocityOverLifetime;
            velocity.enabled = true;
            velocity.radial = new ParticleSystem.MinMaxCurve(-0.8f);
            var emission = motes.emission;
            emission.rateOverTime = 12f;
            var renderer = emitter.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            motes.Play();
        }

        private Transform MakePart(string partName, PrimitiveType primitive,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            var collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part.transform;
        }

        private static Material MakeMaterial(Shader shader, Color color)
        {
            var material = new Material(shader);
            material.color = color;
            return material;
        }
    }
}
