using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSpriteDepthSorter : MonoBehaviour
    {
        [SerializeField] private Transform sortAnchor;
        [SerializeField] private int sortBaseOrder = 5000;
        [SerializeField] private int unitsPerOrderStep = 100;
        [SerializeField] private SpriteRenderer[] targetRenderers;
        [SerializeField] private Collider2D[] targetColliders;
        [SerializeField] private bool forceDefaultSortingLayer = true;

        private int[] orderOffsets;

        private void Awake()
        {
            if (sortAnchor == null)
            {
                sortAnchor = transform;
            }

            CacheRenderers();
            ApplySortOrder();
        }

        private void LateUpdate()
        {
            ApplySortOrder();
        }

        public void Configure(Transform anchor, SpriteRenderer[] renderers, bool useDefaultSortingLayer)
        {
            sortAnchor = anchor != null ? anchor : transform;
            targetRenderers = renderers;
            forceDefaultSortingLayer = useDefaultSortingLayer;
            CacheRenderers();
            ApplySortOrder();
        }

        private void CacheRenderers()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<SpriteRenderer>();
            }

            if (targetColliders == null || targetColliders.Length == 0)
            {
                targetColliders = GetComponentsInChildren<Collider2D>();
            }

            var maxOrder = int.MinValue;
            for (var i = 0; i < targetRenderers.Length; i++)
            {
                var renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                maxOrder = Mathf.Max(maxOrder, renderer.sortingOrder);
            }

            if (maxOrder == int.MinValue)
            {
                maxOrder = 0;
            }

            orderOffsets = new int[targetRenderers.Length];
            for (var i = 0; i < targetRenderers.Length; i++)
            {
                var renderer = targetRenderers[i];
                orderOffsets[i] = renderer != null ? renderer.sortingOrder - maxOrder : 0;
            }
        }

        private void ApplySortOrder()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                return;
            }

            var baseOrder = sortBaseOrder - Mathf.RoundToInt(ResolveSortY() * unitsPerOrderStep);
            for (var i = 0; i < targetRenderers.Length; i++)
            {
                var renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (forceDefaultSortingLayer)
                {
                    renderer.sortingLayerID = 0;
                }

                renderer.sortingOrder = baseOrder + orderOffsets[i];
            }
        }

        private float ResolveSortY()
        {
            var hasBounds = false;
            var minY = float.PositiveInfinity;

            if (targetColliders != null)
            {
                for (var i = 0; i < targetColliders.Length; i++)
                {
                    var collider = targetColliders[i];
                    if (collider == null || !collider.enabled)
                    {
                        continue;
                    }

                    minY = Mathf.Min(minY, collider.bounds.min.y);
                    hasBounds = true;
                }
            }

            if (!hasBounds && targetRenderers != null)
            {
                for (var i = 0; i < targetRenderers.Length; i++)
                {
                    var renderer = targetRenderers[i];
                    if (renderer == null || !renderer.enabled)
                    {
                        continue;
                    }

                    minY = Mathf.Min(minY, renderer.bounds.min.y);
                    hasBounds = true;
                }
            }

            if (hasBounds)
            {
                return minY;
            }

            var anchor = sortAnchor != null ? sortAnchor : transform;
            return anchor.position.y;
        }
    }
}
