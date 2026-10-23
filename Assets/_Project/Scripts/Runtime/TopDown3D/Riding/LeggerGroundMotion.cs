using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Query-only solver. The follower remains the one writer of the companion body.
    public sealed class LeggerGroundMotion
    {
        private const float SlopeToleranceDegrees = .05f;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly Collider[] overlaps = new Collider[32];
        private readonly Collider[] recoveryContacts = new Collider[32];
        private readonly Collider[] footprintSupports = new Collider[5];
        private readonly TopDown3DLeggerRideProfile profile;
        private readonly Transform mount, rider;
        private readonly BoxCollider box;
        private readonly CapsuleCollider capsule;
        private readonly Vector3 seat;
        public int Queries { get; private set; }
        public int Substeps { get; private set; }
        public LeggerRideBlock Block { get; private set; }
        private Vector3 stepStart, stepLift, stepAcross, stepEnd;
        private int stepLeg;
        private Vector3 collisionNormal;
        private Collider collisionCollider, brushingCollider;
        private float brushingSide = 1;
        private float contactSeparation;
        private float supportSpread;
        private float supportGradient;
        private float centerSupportHeight;
        public Vector3 SupportNormal { get; private set; } = Vector3.up;
        public bool RiderAttached { get; set; } = true;
        public bool AllowSteps { get; set; } = true;
        public bool Stepping => stepLeg != 0;
        public LeggerGroundMotion(TopDown3DLeggerRideProfile tuning, Transform mountRoot, Transform riderRoot,
            BoxCollider mountShape, CapsuleCollider riderShape, Vector3 seatOffset)
        { profile = tuning; mount = mountRoot; rider = riderRoot; box = mountShape; capsule = riderShape; seat = seatOffset; }
        public void BeginTick() { Queries = Substeps = 0; Block = LeggerRideBlock.None; }
        public void CancelStep() { stepLeg = 0; }
        private bool Query()
        {
            if (Queries >= profile.queryBudget) { Block = LeggerRideBlock.Budget; return false; }
            Queries++; return true;
        }
        public bool ReserveQueries(int count)
        {
            if (count < 0 || Queries + count > profile.queryBudget) { Block = LeggerRideBlock.Budget; return false; }
            Queries += count; return true;
        }
        private bool Relevant(Collider c) => c != null && c.gameObject.scene == mount.gameObject.scene
            && !c.transform.IsChildOf(mount) && (!RiderAttached || !c.transform.IsChildOf(rider));
        private bool Support(Collider c) => c is TerrainCollider || c.GetComponent<TopDown3DGroundSurface>() != null;
        private Vector3 Half => box.size * .5f;
        private Vector3 Center(Vector3 root, Quaternion rotation) => root + rotation * box.center;

        private void Contact(Collider other, Vector3 root, Quaternion rotation, Vector3 fallback)
        {
            collisionCollider = other;
            contactSeparation = profile.skin;
            if (Query() && Physics.ComputePenetration(box, root, rotation, other, other.transform.position,
                other.transform.rotation, out var normal, out var depth))
            { fallback = normal; contactSeparation = Mathf.Min(profile.substepDistance, depth + profile.skin); }
            // Low rocks can have an upward separation normal; brush around their side instead of climbing them.
            var lateral = Vector3.ProjectOnPlane(fallback, Vector3.up);
            if (lateral.sqrMagnitude < .01f)
                lateral = Vector3.ProjectOnPlane(Center(root, rotation) - other.bounds.center, Vector3.up);
            collisionNormal = lateral.normalized;
        }

        public bool TrySupport(Vector3 root, Quaternion rotation, float slope, out Vector3 grounded, bool broad = false)
        {
            grounded = root;
            supportGradient = 0;
            var half = Half;
            float highest = float.NegativeInfinity, lowest = float.PositiveInfinity;
            int supported = 0;
            bool centerSupported = false, frontSupported = false;
            Collider blockedSupport = null;
            for (int i = 0; i < 5; i++)
            {
                footprintSupports[i] = null;
                var offset = i == 0 ? Vector3.zero : i == 1 ? new Vector3(half.x, 0, half.z)
                    : i == 2 ? new Vector3(-half.x, 0, half.z) : i == 3 ? new Vector3(-half.x, 0, -half.z)
                    : new Vector3(half.x, 0, -half.z);
                var foot = root + rotation * (box.center + offset) + Vector3.down * half.y;
                var lift = broad ? 4 : profile.stepUp + .1f;
                var length = broad ? 8 : profile.stepUp + profile.stepDown + .3f
                    + new Vector2(half.x, half.z).magnitude * 2 * Mathf.Tan(slope * Mathf.Deg2Rad);
                if (!Query()) return false;
                int n = Physics.RaycastNonAlloc(foot + Vector3.up * lift, Vector3.down, hits, length, ~0, QueryTriggerInteraction.Ignore);
                if (n == hits.Length) { Block = LeggerRideBlock.Budget; return false; }
                float distance = float.PositiveInfinity, height = float.NaN, gradient = 0;
                Collider nearestCollider = null;
                for (int k = 0; k < n; k++)
                {
                    var hit = hits[k];
                    if (!Relevant(hit.collider) || hit.distance >= distance) continue;
                    // The first blocking surface occludes lower floors. Never stand on a floor through a ledge/roof.
                    distance = hit.distance;
                    nearestCollider = hit.collider;
                    height = Support(hit.collider) && Vector3.Angle(hit.normal, Vector3.up) <= slope + SlopeToleranceDegrees
                        ? hit.point.y : float.NaN;
                    gradient = new Vector2(hit.normal.x, hit.normal.z).magnitude / Mathf.Max(.0001f, hit.normal.y);
                    if (i == 0 && float.IsFinite(height)) { SupportNormal = hit.normal; centerSupportHeight = height; }
                }
                if (!float.IsFinite(height))
                {
                    if (nearestCollider != null && !Support(nearestCollider)) blockedSupport = nearestCollider;
                    continue;
                }
                supportGradient = Mathf.Max(supportGradient, gradient);
                footprintSupports[i] = nearestCollider;
                supported++; centerSupported |= i == 0; frontSupported |= i == 1;
                highest = Mathf.Max(highest, height); lowest = Mathf.Min(lowest, height);
            }
            if (blockedSupport != null || !centerSupported || !frontSupported || supported < 4
                || highest - lowest > new Vector2(half.x, half.z).magnitude * 2 * Mathf.Tan(slope * Mathf.Deg2Rad) + profile.skin)
            {
                if (blockedSupport != null) { Contact(blockedSupport, root, rotation, Vector3.zero); if (Block != LeggerRideBlock.Budget) Block = LeggerRideBlock.Obstacle; }
                else Block = LeggerRideBlock.MissingSupport;
                return false;
            }
            grounded.y = highest - box.center.y + half.y + profile.skin;
            supportSpread = highest - lowest;
            // Five rays establish a supported footprint, but can miss a crest between them.
            // Fit only the same sampled floor. On a ridge the highest contact can be
            // between the rays, farther above them than a single movement substep.
            float fitRange = Mathf.Max(profile.stepUp, new Vector2(half.x, half.z).magnitude * supportGradient);
            float fitLift = fitRange + profile.skin;
            if (!Query()) return false;
            int contacts = Physics.BoxCastNonAlloc(Center(grounded + Vector3.up * fitLift, rotation),
                Half - Vector3.one * .005f, Vector3.down, hits, rotation,
                fitLift + profile.skin + .005f, ~0, QueryTriggerInteraction.Ignore);
            if (contacts == hits.Length) { Block = LeggerRideBlock.Budget; return false; }
            float nearest = float.PositiveInfinity;
            Vector3 fittedNormal = Vector3.up;
            for (int i = 0; i < contacts; i++)
            {
                var hit = hits[i];
                if (!Relevant(hit.collider) || !Support(hit.collider) || hit.distance <= 0 || hit.distance >= nearest
                    || Vector3.Angle(hit.normal, Vector3.up) > slope + SlopeToleranceDegrees) continue;
                bool sampledFloor = false;
                for (int k = 0; k < footprintSupports.Length; k++) sampledFloor |= footprintSupports[k] == hit.collider;
                if (!sampledFloor) continue;
                float fittedY = grounded.y + fitLift - hit.distance + profile.skin + .005f;
                if (fittedY - grounded.y > fitRange) continue;
                nearest = hit.distance;
                fittedNormal = hit.normal;
            }
            if (float.IsFinite(nearest))
            {
                grounded.y = Mathf.Max(grounded.y, grounded.y + fitLift - nearest + profile.skin + .005f);
                supportGradient = Mathf.Max(supportGradient,
                    new Vector2(fittedNormal.x, fittedNormal.z).magnitude / Mathf.Max(.0001f, fittedNormal.y));
            }
            return true;
        }

        public bool Clear(Vector3 root, Quaternion rotation, bool includeRider)
        {
            if (!Query()) return false;
            int n = Physics.OverlapBoxNonAlloc(Center(root, rotation), Half - Vector3.one * .005f, overlaps,
                rotation, ~0, QueryTriggerInteraction.Ignore);
            if (n == overlaps.Length) { Block = LeggerRideBlock.Budget; return false; }
            for (int i = 0; i < n; i++) if (Relevant(overlaps[i]))
            { Contact(overlaps[i], root, rotation, Vector3.zero); if (Block != LeggerRideBlock.Budget) Block = LeggerRideBlock.Obstacle; return false; }
            return !includeRider || RiderClear(root + rotation * seat, rotation);
        }
        public bool TryPlanInitialFit(Vector3 root, Quaternion rotation, out Vector3 target)
        {
            target = root;
            if (Clear(root, rotation, false))
            {
                if (!TrySupport(root, rotation, profile.slopeDegrees, out _) || !Query()) return false;
                int contacts = Physics.OverlapBoxNonAlloc(Center(root, rotation) - Vector3.up * Half.y,
                    new Vector3(Half.x, .06f, Half.z), overlaps, rotation, ~0, QueryTriggerInteraction.Ignore);
                if (contacts == overlaps.Length) return false;
                for (int i = 0; i < contacts; i++) if (Relevant(overlaps[i]) && Support(overlaps[i])) return true;
                return false;
            }
            if (!Query()) return false;
            int n = Physics.OverlapBoxNonAlloc(Center(root, rotation), Half - Vector3.one * .005f, overlaps,
                rotation, ~0, QueryTriggerInteraction.Ignore);
            if (n == overlaps.Length) return false;
            for (int i = 0; i < n; i++) if (Relevant(overlaps[i]) && !Support(overlaps[i])) return false;
            if (!TrySupport(root, rotation, profile.slopeDegrees, out target)) return false;
            float centerFoot = root.y + box.center.y - Half.y;
            // Only correct the legacy center-grounded body. This cannot relocate an unsupported actor or switch floors.
            if (Mathf.Abs(centerFoot - centerSupportHeight) > .08f || target.y < root.y
                || target.y - root.y > profile.stepUp) return false;
            // Interior terrain vertices can be higher than the support samples; the actual body query must still fit.
            for (int attempt = 0; attempt < 8 && target.y - root.y <= profile.stepUp; attempt++, target.y += profile.skin)
                if (Clear(target, rotation, false)) return true;
            return false;
        }
        public bool AdvanceInitialFit(Vector3 root, Vector3 target, Quaternion rotation, float distance, out Vector3 result)
        {
            result = root;
            if (!Clear(target, rotation, false) || !Query()) return false;
            var next = Vector3.MoveTowards(root, target, Mathf.Min(distance, profile.substepDistance));
            var delta = next - root;
            int n = Physics.BoxCastNonAlloc(Center(root, rotation), Half - Vector3.one * .005f, Vector3.up, hits,
                rotation, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (n == hits.Length) return false;
            for (int i = 0; i < n; i++)
            {
                var hit = hits[i];
                if (!Relevant(hit.collider)) continue;
                if (Support(hit.collider) && Vector3.Angle(hit.normal, Vector3.up) <= profile.slopeDegrees + SlopeToleranceDegrees
                    && Vector3.Dot(delta, hit.normal) >= 0) continue;
                Block = LeggerRideBlock.Obstacle; return false;
            }
            result = next; Substeps++; return true;
        }
        private void CapsuleEnds(Vector3 position, Quaternion rotation, out Vector3 a, out Vector3 b)
        {
            var center = position + rotation * capsule.center;
            float height = Mathf.Max(0, capsule.height * .5f - capsule.radius);
            a = center + Vector3.up * height; b = center - Vector3.up * height;
        }
        public bool RiderClear(Vector3 position, Quaternion rotation)
        {
            CapsuleEnds(position, rotation, out var a, out var b);
            if (!Query()) return false;
            int n = Physics.OverlapCapsuleNonAlloc(a, b, capsule.radius - .005f, overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (n == overlaps.Length) { Block = LeggerRideBlock.Budget; return false; }
            for (int i = 0; i < n; i++) if (Relevant(overlaps[i])) { Block = LeggerRideBlock.Obstacle; return false; }
            return true;
        }
        public bool RiderPath(Vector3 from, Vector3 to, Quaternion rotation)
        {
            var attached = RiderAttached;
            RiderAttached = true;
            try { return RiderPathCore(from, to, rotation); }
            finally { RiderAttached = attached; }
        }
        private bool RiderPathCore(Vector3 from, Vector3 to, Quaternion rotation)
        {
            if (!RiderClear(from, rotation) || !RiderClear(to, rotation)) return false;
            var delta = to - from;
            if (delta.sqrMagnitude < .000001f) return true;
            CapsuleEnds(from, rotation, out var a, out var b);
            if (!Query()) return false;
            int n = Physics.CapsuleCastNonAlloc(a, b, capsule.radius - .005f, delta.normalized, hits,
                delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (n == hits.Length) { Block = LeggerRideBlock.Budget; return false; }
            for (int i = 0; i < n; i++) if (Relevant(hits[i].collider)) { Block = LeggerRideBlock.Obstacle; return false; }
            return true;
        }
        public bool TryExit(Vector3 seatPosition, Vector3 offsetPosition, Quaternion rotation, float slope, out Vector3 exit)
        {
            exit = offsetPosition;
            // Find Booter's own standing support, not the mount's footprint height.
            float bottom = capsule.center.y - capsule.height * .5f;
            if (!Query()) return false;
            int n = Physics.RaycastNonAlloc(offsetPosition + Vector3.up * 2, Vector3.down, hits, 5, ~0, QueryTriggerInteraction.Ignore);
            if (n == hits.Length) { Block = LeggerRideBlock.Budget; return false; }
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (!Relevant(h.collider) || !Support(h.collider) || Vector3.Angle(h.normal, Vector3.up) > slope || h.distance >= nearest) continue;
                nearest = h.distance; exit.y = h.point.y - bottom + profile.skin;
            }
            if (!float.IsFinite(nearest)) return false;
            var raisedExit = new Vector3(exit.x, seatPosition.y, exit.z);
            if (!RiderPath(seatPosition, raisedExit, rotation) || !RiderPath(raisedExit, exit, rotation)) return false;
            // Ignore mount only for entry; an exit must leave its full standing capsule outside the body.
            CapsuleEnds(exit, rotation, out var a, out var b);
            if (!Query()) return false;
            n = Physics.OverlapCapsuleNonAlloc(a, b, capsule.radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (n == overlaps.Length) return false;
            for (int i = 0; i < n; i++)
                if (overlaps[i] != null && overlaps[i].transform.IsChildOf(mount)) return false;
            return true;
        }

        private bool Sweep(Vector3 from, Vector3 to, Quaternion rotation, bool includeRider, float angularMargin = 0)
        {
            var delta = to - from;
            if (delta.sqrMagnitude < .000001f) return Clear(to, rotation, includeRider);
            if (!Query()) return false;
            // Inflate horizontal extents to enclose corner travel during the yaw interval.
            var half = Half + new Vector3(angularMargin, 0, angularMargin);
            int n = Physics.BoxCastNonAlloc(Center(from, rotation), half - Vector3.one * .005f, delta.normalized, hits,
                rotation, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (n == hits.Length) { Block = LeggerRideBlock.Budget; return false; }
            float nearest = float.PositiveInfinity;
            RaycastHit obstacle = default;
            for (int i = 0; i < n; i++)
                if (Relevant(hits[i].collider))
                {
                    var hit = hits[i];
                    // Allow only a starting skin contact while moving tangent to or away from legal support.
                    if (hit.distance <= profile.skin && Support(hit.collider)
                        && Vector3.Angle(hit.normal, Vector3.up) <= profile.slopeDegrees + SlopeToleranceDegrees
                        && Vector3.Dot(delta, hit.normal) >= -.0001f) continue;
                    // Mesh edges can report grazing hits while already travelling along a face.
                    // A tangent/separating hit cannot obstruct this sweep; the endpoint must still fit.
                    if (!Support(hit.collider) && Vector3.Dot(delta, hit.normal) >= -.0001f) continue;
                    if (hit.distance <= profile.skin && !Support(hit.collider))
                    {
                        if (!Query()) return false;
                        bool penetrating = Physics.ComputePenetration(box, from, rotation, hit.collider,
                            hit.collider.transform.position, hit.collider.transform.rotation, out var normal, out _);
                        if (Vector3.Dot(delta, penetrating ? normal : hit.normal) >= -.0001f) continue;
                    }
                    if (hit.distance < nearest) { nearest = hit.distance; obstacle = hit; }
                }
            if (float.IsFinite(nearest))
            { Contact(obstacle.collider, from, rotation, obstacle.normal); if (Block != LeggerRideBlock.Budget) Block = LeggerRideBlock.Obstacle; return false; }
            return Clear(to, rotation, includeRider)
                && (!includeRider || RiderPath(from + rotation * seat, to + rotation * seat, rotation));
        }

        public bool Resolve(Vector3 start, Quaternion rotation, Vector3 displacement, Quaternion desiredRotation,
            out Vector3 result, out Quaternion resultRotation)
        {
            collisionNormal = Vector3.zero;
            var origin = start;
            result = start; resultRotation = rotation;
            if (!Clear(origin, rotation, RiderAttached) && !RecoverContact(origin, rotation, out origin)) return false;
            bool moved = ResolveDirect(origin, rotation, displacement, desiredRotation, out result, out resultRotation) || origin != start;
            for (int contact = 0; contact < 2 && Block == LeggerRideBlock.Obstacle && collisionNormal.sqrMagnitude > .1f; contact++)
            {
                var remaining = Vector3.ProjectOnPlane(start + displacement - result, Vector3.up);
                var normal = Vector3.ProjectOnPlane(collisionNormal, Vector3.up).normalized;
                var slide = Vector3.Dot(remaining, normal) >= 0 ? remaining : Vector3.ProjectOnPlane(remaining, normal);
                // A near-head-on hit needs a stable choice of side instead of a zero tangent.
                // Retain the choice for this obstacle to avoid left/right oscillation.
                if (collisionCollider != brushingCollider)
                {
                    brushingCollider = collisionCollider;
                    float side = Vector3.Dot(remaining, Vector3.Cross(Vector3.up, normal));
                    brushingSide = Mathf.Abs(side) > .001f ? Mathf.Sign(side) : 1;
                }
                if (Vector3.Dot(remaining, normal) < 0 && slide.magnitude < remaining.magnitude * .35f)
                    slide = Vector3.Cross(Vector3.up, normal) * (brushingSide * remaining.magnitude * .35f);
                if (slide.sqrMagnitude < .00001f) break;
                var from = result; var facing = resultRotation;
                // Separate before advancing along the tangent. Blending the gap into a long
                // forward sweep leaves its first substep caught on the same triangle edge.
                if (Vector3.Dot(remaining, normal) <= 0)
                {
                    moved |= ResolveDirect(from, facing, normal * contactSeparation, facing, out result, out resultRotation);
                    float separation = Vector3.Distance(from, result);
                    slide = Vector3.ClampMagnitude(slide, Mathf.Max(0, remaining.magnitude - separation));
                    from = result; facing = resultRotation;
                    if (Block == LeggerRideBlock.Budget) break;
                }
                collisionNormal = Vector3.zero;
                moved |= ResolveDirect(from, facing, slide, facing, out result, out resultRotation);
            }
            return moved;
        }
        private bool RecoverContact(Vector3 start, Quaternion rotation, out Vector3 result)
        {
            result = start;
            if (Block == LeggerRideBlock.Budget || Substeps >= profile.substepBudget || !Query()) return false;
            int n = Physics.OverlapBoxNonAlloc(Center(start, rotation), Half, recoveryContacts, rotation, ~0, QueryTriggerInteraction.Ignore);
            if (n == recoveryContacts.Length) { Block = LeggerRideBlock.Budget; return false; }
            var correction = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                var other = recoveryContacts[i];
                if (!Relevant(other)) continue;
                if (Support(other) || !Query()) return false;
                if (!Physics.ComputePenetration(box, start, rotation, other, other.transform.position,
                    other.transform.rotation, out var normal, out var depth)) continue;
                // This is a shallow horizontal contact repair, never a floor switch or a large relocation.
                if (Mathf.Abs(normal.y) > .1f) return false;
                correction += normal * (depth + profile.skin);
            }
            if (correction.sqrMagnitude < .000001f || correction.magnitude > profile.substepDistance) return false;
            if (!TrySupport(start + correction, rotation, profile.slopeDegrees, out var supported)
                || Mathf.Abs(supported.y - start.y) > profile.skin * 2
                || !Sweep(start, supported, rotation, RiderAttached)) return false;
            result = supported; Substeps++; Block = LeggerRideBlock.None; return true;
        }
        private bool ResolveDirect(Vector3 start, Quaternion rotation, Vector3 displacement, Quaternion desiredRotation,
            out Vector3 result, out Quaternion resultRotation)
        {
            result = start; resultRotation = rotation;
            if (Stepping) return AdvanceStep(start, rotation, out result);
            if (!TrySupport(start, rotation, profile.slopeDegrees, out var supported) || !Clear(start, rotation, RiderAttached)) return false;
            float turnGradient = supportGradient;
            float corner = new Vector2(Half.x, Half.z).magnitude;
            float angle = Quaternion.Angle(rotation, desiredRotation) * Mathf.Deg2Rad;
            int count = Mathf.Max(1, Mathf.CeilToInt((displacement.magnitude + corner * angle) / profile.substepDistance));
            if (count > profile.substepBudget) { Block = LeggerRideBlock.Budget; return false; }
            for (int i = 1; i <= count; i++)
            {
                if (Substeps >= profile.substepBudget) { Block = LeggerRideBlock.Budget; return result != start; }
                Substeps++;
                var nextRotation = Quaternion.Slerp(rotation, desiredRotation, (float)i / count);
                var next = start + displacement * ((float)i / count);
                if (!TrySupport(next, nextRotation, profile.slopeDegrees, out var ground)) return result != start;
                turnGradient = Mathf.Max(turnGradient, supportGradient);
                // Continuous slopes have support variation proportional to distance; a larger discontinuity is a ledge.
                float yawMargin = corner * Quaternion.Angle(resultRotation, nextRotation) * Mathf.Deg2Rad;
                float allowedSlopeRise = (Vector3.ProjectOnPlane(next - result, Vector3.up).magnitude + yawMargin)
                    * turnGradient + .04f;
                float rise = ground.y - result.y;
                if (Mathf.Abs(rise) > allowedSlopeRise)
                {
                    if (!AllowSteps) { Block = LeggerRideBlock.MissingSupport; return result != start; }
                    if (rise > profile.stepUp || -rise > profile.stepDown) { Block = LeggerRideBlock.MissingSupport; return result != start; }
                    // Search forward for a full-footprint landing rather than perching across the ledge edge.
                    var forward = Vector3.ProjectOnPlane(displacement, Vector3.up).normalized;
                    var landingProbe = next + forward * (new Vector2(Half.x, Half.z).magnitude * 2 + profile.skin);
                    if (!TrySupport(landingProbe, nextRotation, profile.slopeDegrees, out var landing)
                        || landing.y - result.y > profile.stepUp || result.y - landing.y > profile.stepDown
                        || supportSpread > .12f || !BeginStep(result, landing, nextRotation)) return result != start;
                    resultRotation = nextRotation;
                    return AdvanceStep(result, nextRotation, out result);
                }
                if (!MoveAndTurnClear(result, ground, resultRotation, nextRotation, yawMargin, turnGradient)) return result != start;
                result = ground; resultRotation = nextRotation;
            }
            return true;
        }
        private bool MoveAndTurnClear(Vector3 from, Vector3 to, Quaternion rotation, Quaternion nextRotation, float margin, float gradient)
        {
            if (margin < .00001f) return Sweep(from, to, nextRotation, RiderAttached);
            // The yaw envelope extends uphill beyond the old footprint. Validate it at a
            // conservatively raised height, with swept ascent and descent, rather than
            // testing the new corners embedded in the slope at the old body's height.
            var raised = from;
            // Both horizontal half-extents grow by margin; their diagonal extends sqrt(2) farther.
            raised.y = Mathf.Max(from.y, to.y) + Mathf.Sqrt(2) * margin * gradient;
            return Sweep(from, raised, rotation, RiderAttached)
                && RotationClear(raised, rotation, nextRotation, margin)
                && Sweep(raised, to, nextRotation, RiderAttached);
        }
        private bool RotationClear(Vector3 position, Quaternion from, Quaternion to, float margin)
        {
            if (!Query()) return false;
            int n = Physics.OverlapBoxNonAlloc(Center(position, from), Half + new Vector3(margin, -.005f, margin),
                overlaps, from, ~0, QueryTriggerInteraction.Ignore);
            if (n == overlaps.Length) return false;
            for (int i = 0; i < n; i++) if (Relevant(overlaps[i]))
            { Contact(overlaps[i], position, from, Vector3.zero); if (Block != LeggerRideBlock.Budget) Block = LeggerRideBlock.Obstacle; return false; }
            return Clear(position, to, RiderAttached);
        }
        private bool BeginStep(Vector3 from, Vector3 end, Quaternion rotation)
        {
            var lift = new Vector3(from.x, Mathf.Max(from.y, end.y) + profile.skin, from.z);
            var across = new Vector3(end.x, lift.y, end.z);
            if (!Sweep(from, lift, rotation, true) || !Sweep(lift, across, rotation, true) || !Sweep(across, end, rotation, true)) return false;
            stepStart = from; stepLift = lift; stepAcross = across; stepEnd = end; stepLeg = 1; return true;
        }
        public bool AdvanceStep(Vector3 from, Quaternion rotation, out Vector3 result)
        {
            result = from;
            if (!Stepping) return true;
            if (Substeps >= profile.substepBudget) { Block = LeggerRideBlock.Budget; return false; }
            if (!TrySupport(stepEnd, rotation, profile.slopeDegrees, out var landing) || Vector3.Distance(landing, stepEnd) > .06f)
            { Block = LeggerRideBlock.MissingSupport; return false; }
            // Pending steps settle even with neutral/menu intent; never release rider gravity mid-lift.
            float distance = Mathf.Min(profile.stepSpeed * Time.fixedDeltaTime, profile.substepDistance);
            var target = stepLeg == 1 ? stepLift : stepLeg == 2 ? stepAcross : stepEnd;
            var next = Vector3.MoveTowards(from, target, distance);
            if (!Sweep(from, next, rotation, true)) return false;
            result = next; Substeps++;
            if (Vector3.Distance(next, target) < .001f) { stepLeg++; if (stepLeg > 3) stepLeg = 0; }
            return true;
        }
    }
}
