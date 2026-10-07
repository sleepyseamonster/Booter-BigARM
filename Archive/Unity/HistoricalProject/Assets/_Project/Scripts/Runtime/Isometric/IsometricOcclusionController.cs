using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class IsometricOcclusionController : MonoBehaviour
    {
        private const int RaycastCapacity = 32;

        [SerializeField] private Camera sourceCamera;
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.8f, 0f);
        [SerializeField] private LayerMask occluderMask = ~0;

        private readonly RaycastHit[] hits = new RaycastHit[RaycastCapacity];
        private readonly HashSet<IsometricOccluder> hiddenLastFrame = new HashSet<IsometricOccluder>();
        private readonly HashSet<IsometricOccluder> hiddenThisFrame = new HashSet<IsometricOccluder>();

        public void Configure(Camera camera, Transform visibilityTarget)
        {
            sourceCamera = camera;
            target = visibilityTarget;
        }

        private void LateUpdate()
        {
            hiddenThisFrame.Clear();
            if (sourceCamera != null && target != null)
            {
                var origin = sourceCamera.transform.position;
                var destination = target.position + targetOffset;
                var direction = destination - origin;
                var distance = direction.magnitude;
                if (distance > 0.01f)
                {
                    var hitCount = Physics.RaycastNonAlloc(
                        origin,
                        direction / distance,
                        hits,
                        distance,
                        occluderMask,
                        QueryTriggerInteraction.Ignore);
                    for (var i = 0; i < hitCount; i++)
                    {
                        var marker = hits[i].collider != null
                            ? hits[i].collider.GetComponentInParent<IsometricOccluder>()
                            : null;
                        if (marker != null)
                        {
                            hiddenThisFrame.Add(marker);
                        }
                    }
                }
            }

            foreach (var previous in hiddenLastFrame)
            {
                if (previous != null && !hiddenThisFrame.Contains(previous))
                {
                    previous.SetOccluded(false);
                }
            }

            foreach (var current in hiddenThisFrame)
            {
                if (current != null)
                {
                    current.SetOccluded(true);
                }
            }

            hiddenLastFrame.Clear();
            foreach (var current in hiddenThisFrame)
            {
                hiddenLastFrame.Add(current);
            }
        }

        private void OnDisable()
        {
            foreach (var marker in hiddenLastFrame)
            {
                if (marker != null)
                {
                    marker.SetOccluded(false);
                }
            }

            hiddenLastFrame.Clear();
            hiddenThisFrame.Clear();
        }
    }
}
