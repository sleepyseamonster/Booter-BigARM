using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DWorldSpeedController : MonoBehaviour
    {
        private object owner;
        private float baseline, requested;
        public bool HasRequest => owner != null;
        public event Action Invalidated;

        public bool TryAcquire(object requester, float speed)
        {
            if (requester == null || owner != null || !Mathf.Approximately(Time.timeScale, 1)
                || !float.IsFinite(speed) || speed < 1 || speed > 4) return false;
            baseline = Time.timeScale; requested = speed; owner = requester;
            Time.timeScale = speed;
            return true;
        }

        public void Release(object requester)
        {
            if (!ReferenceEquals(owner, requester) || owner == null) return;
            owner = null;
            if (Mathf.Approximately(Time.timeScale, requested)) Time.timeScale = baseline;
        }

        private void Update()
        {
            if (owner == null || Mathf.Approximately(Time.timeScale, requested)) return;
            owner = null; Invalidated?.Invoke();
        }
        private void OnApplicationFocus(bool focused) { if (!focused) Invalidate(); }
        private void OnDisable() => Invalidate();
        private void Invalidate()
        {
            if (owner == null) return;
            var previous = owner; Release(previous); Invalidated?.Invoke();
        }
    }
}
