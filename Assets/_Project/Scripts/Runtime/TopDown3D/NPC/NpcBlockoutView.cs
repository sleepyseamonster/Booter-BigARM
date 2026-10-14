using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed class NpcBlockoutView : MonoBehaviour
    {
        [SerializeField] private Transform[] legPivots;
        private Vector3 previous;
        private float phase;
        public void Configure(Transform[] pivots) => legPivots = pivots;
        private void OnEnable() => previous = transform.position;
        private void LateUpdate()
        {
            var distance = NpcMath.Distance(transform.position, previous); previous = transform.position;
            phase += distance * 6;
            if (legPivots == null) return;
            for (var i = 0; i < legPivots.Length; i++)
                if (legPivots[i] != null) legPivots[i].localRotation = Quaternion.Euler(Mathf.Sin(phase + i % 2 * Mathf.PI) * Mathf.Min(distance * 200, 20), 0, 0);
        }
    }
}
