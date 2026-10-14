using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Omnidirectional sensing prevents a fleeing body turn from making a visible pursuer disappear.
    public sealed class NpcThreatPerception
    {
        private readonly RaycastHit[] hits = new RaycastHit[32];
        public NpcThreatObservation Observation { get; private set; }
        public int Probes { get; private set; }
        public void Clear() { Observation = default; Probes = 0; }
        public bool Sample(Transform self, Transform target, NpcTuning tuning, double now)
        {
            Probes = 0;
            if (target == null || !target.gameObject.activeInHierarchy || target.gameObject.scene != self.gameObject.scene)
            { Clear(); return false; }
            if (!NpcMath.Finite(self.position) || !NpcMath.Finite(target.position)) { Clear(); return false; }
            if (NpcMath.Distance(self.position, target.position) > tuning.detection
                || Mathf.Abs(self.position.y - target.position.y) > tuning.maximumVerticalDistance) return false;
            var collider = target.GetComponent<CapsuleCollider>();
            var center = collider != null ? target.TransformPoint(collider.center) : target.position;
            for (var sample = 0; sample < 2; sample++)
            {
                var point = center + Vector3.up * (sample == 0 ? 0 : .4f);
                var origin = self.position + Vector3.up * .8f; var delta = point - origin;
                if (delta.magnitude < .001f) { Observation = new(target.position, now); return true; }
                Probes++;
                var count = Physics.RaycastNonAlloc(origin, delta.normalized, hits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
                if (count == hits.Length) continue;
                var occluded = false;
                for (var i = 0; i < count; i++)
                {
                    var c = hits[i].collider;
                    if (c != null && c.gameObject.scene == self.gameObject.scene && !c.transform.IsChildOf(self)
                        && !c.transform.IsChildOf(target)) { occluded = true; break; }
                }
                if (!occluded) { Observation = new(target.position, now); return true; }
            }
            return false; // Retain last observed position and original timestamp only.
        }
    }
}
