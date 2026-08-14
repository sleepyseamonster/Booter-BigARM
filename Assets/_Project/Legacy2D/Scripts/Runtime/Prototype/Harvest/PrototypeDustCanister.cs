using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class PrototypeDustCanister : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float storedDust;
        [SerializeField, Min(1f)] private float maxDust = 100f;
        [SerializeField, Min(0f)] private float dustPerSecond = 2f;
        [SerializeField] private Sprite dustFillSprite;

        private SpriteRenderer bodyRenderer;
        private SpriteRenderer fillRenderer;
        private CircleCollider2D pickupCollider;

        public float StoredDust => storedDust;
        public float MaxDust => maxDust;
        public bool IsFull => storedDust >= maxDust - 0.01f;

        public void Initialize(Sprite bodySprite, Sprite fillSprite, float startingDust)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            bodyRenderer.sprite = bodySprite != null ? bodySprite : bodyRenderer.sprite;
            bodyRenderer.sortingOrder = 8;

            dustFillSprite = fillSprite;
            EnsureFillRenderer();
            SetDustAmount(startingDust);
        }

        public PrototypeDustCanisterSaveData CaptureSaveData()
        {
            return PrototypeDustCanisterSaveData.Create(true, transform.position, storedDust);
        }

        public void ApplySaveData(PrototypeDustCanisterSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            transform.position = saveData.Position;
            SetDustAmount(saveData.StoredDust);
        }

        public void SetDustAmount(float value)
        {
            storedDust = Mathf.Clamp(value, 0f, maxDust);
            RefreshFillVisual();
        }

        public float CollectAllDust()
        {
            var dust = storedDust;
            SetDustAmount(0f);
            return dust;
        }

        private void Awake()
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            pickupCollider = GetComponent<CircleCollider2D>();
            pickupCollider.isTrigger = true;
            pickupCollider.radius = 0.38f;
            EnsureFillRenderer();
            RefreshFillVisual();
        }

        private void Update()
        {
            if (storedDust >= maxDust)
            {
                return;
            }

            SetDustAmount(storedDust + (dustPerSecond * Time.deltaTime));
        }

        private void OnValidate()
        {
            maxDust = Mathf.Max(1f, maxDust);
            dustPerSecond = Mathf.Max(0f, dustPerSecond);
            storedDust = Mathf.Clamp(storedDust, 0f, maxDust);
        }

        private void EnsureFillRenderer()
        {
            if (fillRenderer != null)
            {
                fillRenderer.sprite = dustFillSprite;
                return;
            }

            var fillObject = new GameObject("Dust Fill");
            fillObject.transform.SetParent(transform, false);
            fillObject.transform.localPosition = new Vector3(0f, -0.1f, -0.02f);

            fillRenderer = fillObject.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = dustFillSprite;
            fillRenderer.sortingOrder = 7;
            fillRenderer.color = new Color(0.79f, 0.71f, 0.39f, 0.95f);
        }

        private void RefreshFillVisual()
        {
            if (fillRenderer == null)
            {
                return;
            }

            var fill01 = Mathf.Clamp01(storedDust / Mathf.Max(1f, maxDust));
            fillRenderer.enabled = fill01 > 0.01f;
            fillRenderer.transform.localPosition = new Vector3(0f, Mathf.Lerp(-0.14f, 0.04f, fill01), -0.02f);
            fillRenderer.transform.localScale = new Vector3(
                Mathf.Lerp(0.28f, 0.42f, fill01),
                Mathf.Lerp(0.12f, 0.42f, fill01),
                1f);
            fillRenderer.color = Color.Lerp(
                new Color(0.72f, 0.63f, 0.34f, 0.65f),
                new Color(0.95f, 0.88f, 0.49f, 0.95f),
                fill01);
        }
    }
}
