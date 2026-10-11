using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DCampPlacement
    {
        private static readonly RaycastHit[] ground = new RaycastHit[32];
        private static readonly Collider[] overlaps = new Collider[64];

        public static bool TryGround(Vector3 point, out Vector3 position, out Vector3 normal)
        {
            position = point; normal = Vector3.up;
            var count = Physics.RaycastNonAlloc(point + Vector3.up * 4, Vector3.down, ground, 12, ~0, QueryTriggerInteraction.Ignore);
            var nearest = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var hit = ground[i];
                if (!(hit.collider is TerrainCollider) && hit.collider.GetComponentInParent<TopDown3DGroundSurface>() == null) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance; position = hit.point; normal = hit.normal;
            }
            return nearest < float.PositiveInfinity;
        }

        public static bool Validate(Vector3 point, Quaternion rotation, TopDown3DCampSettings settings,
            Transform actor, Transform existingCamp, out Vector3 supported, out string reason)
        {
            reason = "No supported ground";
            if (!TryGround(point, out supported, out var normal)) return false;
            if (Vector3.Angle(normal, Vector3.up) > settings.maximumSlope) { reason = "Ground is too steep"; return false; }
            var half = new Vector3(settings.footprint.x * .5f, 0, settings.footprint.z * .5f);
            var centre = supported + rotation * new Vector3(0, 0, settings.footprint.z * .5f);
            for (var x = -1; x <= 1; x += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var corner = centre + rotation * new Vector3(half.x * x, 0, half.z * z);
                if (!TryGround(corner, out var support, out var slope) || Mathf.Abs(support.y - supported.y) > settings.maximumHeightVariation
                    || Vector3.Angle(slope, Vector3.up) > settings.maximumSlope)
                { reason = "Feet or sleeping space lack level support"; return false; }
            }
            var count = Physics.OverlapBoxNonAlloc(centre + Vector3.up * 1.3f,
                new Vector3(half.x, 1.22f, half.z), overlaps, rotation, ~0, QueryTriggerInteraction.Ignore);
            if (count >= overlaps.Length) { reason = "Space is too crowded"; return false; }
            for (var i = 0; i < count; i++)
            {
                var collider = overlaps[i];
                if (collider is TerrainCollider || collider.GetComponentInParent<TopDown3DGroundSurface>() != null
                    || (actor != null && collider.transform.IsChildOf(actor))
                    || (existingCamp != null && collider.transform.IsChildOf(existingCamp))) continue;
                reason = "Sleeping or standing space is blocked"; return false;
            }
            reason = "Supported · sleeping space clear"; return true;
        }

        public static Quaternion SunFacing()
        {
            var direction = PerpetualTwilightSun.Active != null ? PerpetualTwilightSun.Active.DirectionToSun : Vector3.back;
            direction = -Vector3.ProjectOnPlane(direction, Vector3.up);
            return direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
        }
    }
}
