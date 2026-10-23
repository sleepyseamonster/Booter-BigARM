using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class LeggerGroundMotionTests
    {
        [System.Serializable] private sealed class NativeSprintPose
        {
            public float[] root;
            public float[] rotation;
            public NativeSprintTile[] tiles;
        }
        [System.Serializable] private sealed class NativeSprintTile
        {
            public int resolution;
            public float[] origin;
            public float[] size;
            public float[] heights;
        }
        private readonly List<GameObject> objects = new();
        private readonly List<Mesh> meshes = new();
        private TopDown3DLeggerRideProfile profile;
        private LeggerGroundMotion motion;
        private Transform mount, rider;
        private Vector3 start;
        private GameObject Box(string name, Vector3 position, Vector3 size, bool ground = false)
        {
            var go = new GameObject(name); objects.Add(go); go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
            if (ground) go.AddComponent<TopDown3DGroundSurface>(); return go;
        }
        [SetUp] public void SetUp()
        {
            profile = ScriptableObject.CreateInstance<TopDown3DLeggerRideProfile>();
            // Keep fixtures away from other loaded EditMode scene colliders.
            start = new Vector3(25000, .825f, 25000);
            mount = Box("Legger fixture", start, new Vector3(1.1f, 2.2f, 1.3f)).transform;
            var shape = mount.GetComponent<BoxCollider>(); shape.center = Vector3.up * .3f;
            rider = new GameObject("Passive rider fixture").transform; objects.Add(rider.gameObject);
            var capsule = rider.gameObject.AddComponent<CapsuleCollider>(); capsule.height = 1.82f; capsule.radius = .2f;
            rider.position = start + Vector3.up * 2.35f; capsule.enabled = false;
            Box("Native support fixture", start + new Vector3(0, -.925f, 0), new Vector3(30, .2f, 30), true);
            motion = new LeggerGroundMotion(profile, mount, rider, shape, capsule, Vector3.up * 2.35f);
            Physics.SyncTransforms(); motion.BeginTick();
        }
        [TearDown] public void TearDown()
        {
            foreach (var go in objects) Object.DestroyImmediate(go); objects.Clear(); Object.DestroyImmediate(profile);
            foreach (var mesh in meshes) Object.DestroyImmediate(mesh); meshes.Clear();
        }
        [Test] public void FlatGroundSupportsBoundedMovementAndTracksActualQueries()
        {
            Assert.That(motion.Resolve(start, Quaternion.identity, Vector3.forward * .2f, Quaternion.identity, out var end, out _), Is.True);
            Assert.That(end.z, Is.EqualTo(start.z + .2f).Within(.002f));
            Assert.That(motion.Queries, Is.InRange(1, profile.queryBudget));
            Assert.That(motion.Substeps, Is.InRange(1, profile.substepBudget));
        }
        [Test] public void SupportedNativeTerrainDoesNotExemptItsBlockingBank()
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(4, 4, 4) };
            var heights = new float[33, 33];
            for (int z = 23; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = .8f;
            data.SetHeights(0, 0, heights);
            var terrain = Terrain.CreateTerrainGameObject(data); objects.Add(terrain);
            terrain.transform.position = new Vector3(start.x - 2, 0, start.z - 2);
            try
            {
                Physics.SyncTransforms(); motion.BeginTick();
                Assert.That(motion.TrySupport(start, Quaternion.identity, 55, out _), Is.True);
                motion.Resolve(start, Quaternion.identity, Vector3.forward * .3f, Quaternion.identity, out var end, out _);
                Assert.That(end.z, Is.LessThan(start.z + .2f));
                Assert.That(end.y, Is.LessThan(start.y + profile.stepUp));
            }
            finally { Object.DestroyImmediate(terrain); Object.DestroyImmediate(data); }
        }
        [Test] public void ThinWallBlocksWholePathEvenWithClearEndpoint()
        {
            Box("Thin blocker", start + new Vector3(0, .3f, 1), new Vector3(4, 4, .025f)); Physics.SyncTransforms();
            motion.Resolve(start, Quaternion.identity, Vector3.forward, Quaternion.identity, out var end, out _);
            Assert.That(end.z - start.z, Is.LessThan(.4f));
            Assert.That(motion.Block, Is.EqualTo(LeggerRideBlock.Obstacle));
        }
        [Test] public void RiderHeadroomBlocksTravelEvenWhenMountFits()
        {
            Box("Low overhead", start + new Vector3(0, 3.2f, .9f), new Vector3(3, .2f, .2f)); Physics.SyncTransforms();
            motion.Resolve(start, Quaternion.identity, Vector3.forward * .6f, Quaternion.identity, out var end, out _);
            Assert.That(end.z - start.z, Is.LessThan(.6f));
        }
        [Test] public void NoSupportAndBudgetExhaustionCannotCommitMotion()
        {
            Assert.That(motion.TrySupport(start + Vector3.right * 50, Quaternion.identity, 55, out _), Is.False);
            profile.queryBudget = 1; motion.BeginTick();
            Assert.That(motion.Resolve(start, Quaternion.identity, Vector3.forward * .2f, Quaternion.identity, out var end, out _), Is.False);
            Assert.That(end, Is.EqualTo(start)); Assert.That(motion.Queries, Is.EqualTo(1));
        }
        [Test] public void ExitRequiresClearDescentAndStandingSpace()
        {
            var offset = start + Vector3.right * 1.5f;
            Assert.That(motion.TryExit(start + Vector3.up * 2.35f, offset, Quaternion.identity, 48, out var exit), Is.True);
            Box("Blocked exit", exit, new Vector3(.6f, 2, .6f)); Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.TryExit(start + Vector3.up * 2.35f, offset, Quaternion.identity, 48, out _), Is.False);
        }
        [TestCase(52f, true)]
        [TestCase(55.5f, false)]
        public void RoughSlopeAboveOldLimitsUsesFullBodySupport(float degrees, bool allowed)
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            var slope = Box("Slope fixture", new Vector3(start.x, -.1f / Mathf.Cos(degrees * Mathf.Deg2Rad), start.z), new Vector3(30, .2f, 30), true);
            slope.transform.rotation = Quaternion.Euler(-degrees, 0, 0); Physics.SyncTransforms(); motion.BeginTick();
            bool supported = motion.TrySupport(start, Quaternion.identity, 55, out var root, true);
            Assert.That(supported, Is.EqualTo(allowed));
            if (!allowed) return;
            mount.position = root; rider.position = root + Vector3.up * 2.35f; Physics.SyncTransforms(); motion.BeginTick();
            var delta = new Vector3(0, Mathf.Sin(degrees * Mathf.Deg2Rad), Mathf.Cos(degrees * Mathf.Deg2Rad)) * .12f;
            Assert.That(motion.Resolve(root, Quaternion.identity, delta, Quaternion.identity, out var end, out _), Is.True);
            Assert.That(end.z, Is.GreaterThan(root.z)); Assert.That(end.y, Is.GreaterThan(root.y));
        }
        [Test] public void StepAboveOldLimitContinuesAcrossMultipleTicksAndSettles()
        {
            Box("Wide step landing", new Vector3(start.x, .3f, start.z + 3), new Vector3(6, .6f, 4), true);
            Physics.SyncTransforms(); bool entered = false; var root = start;
            for (int tick = 0; tick < 90; tick++)
            {
                motion.BeginTick(); motion.Resolve(root, Quaternion.identity, Vector3.forward * .15f, Quaternion.identity, out var next, out _);
                entered |= motion.Stepping; root = next; mount.position = root; rider.position = root + Vector3.up * 2.35f; Physics.SyncTransforms();
                Assert.That(motion.Queries, Is.LessThanOrEqualTo(profile.queryBudget));
                if (entered && !motion.Stepping) break;
            }
            Assert.That(entered, Is.True); Assert.That(motion.Stepping, Is.False);
            Assert.That(root.y, Is.EqualTo(start.y + .6f).Within(.01f));
            Assert.That(root.z - start.z, Is.GreaterThan(1.6f));
        }
        [Test] public void ChangedLandingCannotContinueAnAirborneStep()
        {
            var platform = Box("Removable landing", new Vector3(start.x, .3f, start.z + 3), new Vector3(6, .6f, 4), true);
            Physics.SyncTransforms(); var root = start;
            for (int tick = 0; tick < 10 && !motion.Stepping; tick++)
            {
                motion.BeginTick(); motion.Resolve(root, Quaternion.identity, Vector3.forward * .15f, Quaternion.identity, out root, out _);
                mount.position = root; rider.position = root + Vector3.up * 2.35f; Physics.SyncTransforms();
            }
            Assert.That(motion.Stepping, Is.True);
            Object.DestroyImmediate(platform); Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.Resolve(root, Quaternion.identity, Vector3.zero, Quaternion.identity, out var end, out _), Is.False);
            Assert.That(end, Is.EqualTo(root)); Assert.That(motion.Block, Is.EqualTo(LeggerRideBlock.MissingSupport));
        }
        [Test] public void MountPreparationCannotRelocateAnUnsupportedFloatingActor()
        {
            Assert.That(motion.TryPlanInitialFit(start + Vector3.up * 3, Quaternion.identity, out _), Is.False);
        }

        [TestCase(25f, 0f, 8f)]
        [TestCase(40f, 45f, 8f)]
        [TestCase(52f, 90f, -8f)]
        public void SupportedSlopeAllowsSteeringAndRetreat(float degrees, float yaw, float turn)
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            var slope = Box("Steering slope", new Vector3(start.x, -.1f / Mathf.Cos(degrees * Mathf.Deg2Rad), start.z),
                new Vector3(30, .2f, 30), true);
            slope.transform.rotation = Quaternion.Euler(-degrees, 0, 0);
            var rotation = Quaternion.Euler(0, yaw, 0);
            Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.TrySupport(start, rotation, profile.slopeDegrees, out var root, true), Is.True);
            mount.SetPositionAndRotation(root, rotation); Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.Clear(root, rotation, true), Is.True);
            motion.BeginTick();
            Assert.That(motion.Resolve(root, rotation, Vector3.forward * .04f,
                Quaternion.Euler(0, yaw + turn, 0), out var end, out var facing), Is.True, motion.Block.ToString());
            Assert.That(end.z, Is.GreaterThan(root.z));
            Assert.That(Quaternion.Angle(rotation, facing), Is.GreaterThan(1));
            mount.SetPositionAndRotation(end, facing); Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.Resolve(end, facing, Vector3.back * .04f, facing, out var retreat, out _), Is.True,
                motion.Block.ToString());
            Assert.That(retreat.z, Is.LessThan(end.z));
            Assert.That(motion.Clear(retreat, facing, true), Is.True);
        }

        [TestCase(25f, 2f)]
        [TestCase(40f, 2f)]
        [TestCase(52f, 2f)]
        [TestCase(25f, 10f)]
        [TestCase(40f, 10f)]
        [TestCase(52f, 10f)]
        [TestCase(25f, 24f)]
        [TestCase(40f, 24f)]
        [TestCase(52f, 24f)]
        [TestCase(45f, 24f)]
        [TestCase(60f, 24f)]
        [TestCase(70f, 24f)]
        [TestCase(75f, 24f)]
        [TestCase(75f, 8.4f)]
        public void NativeTerrainSlopeKeepsSteeringAcrossTicksAndCanReverse(float degrees, float speed)
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            float gradient = Mathf.Tan(degrees * Mathf.Deg2Rad);
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(16, 80, 16) };
            var heights = new float[33, 33];
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++)
                heights[z, x] = .05f + z * .5f * gradient / 80;
            data.SetHeights(0, 0, heights);
            var terrain = Terrain.CreateTerrainGameObject(data); objects.Add(terrain);
            terrain.transform.position = new Vector3(start.x - 8, -4 - 8 * gradient, start.z - 8);
            try
            {
                Physics.SyncTransforms(); motion.BeginTick();
                var rotation = Quaternion.identity;
                Assert.That(motion.TrySupport(start, rotation, profile.slopeDegrees, out var root, true), Is.True);
                float originZ = root.z;
                // Keep the high-speed path inside the finite fixture terrain.
                int halfTicks = speed > 2 ? 10 : 20;
                for (int tick = 0; tick < halfTicks * 2; tick++)
                {
                    mount.SetPositionAndRotation(root, rotation); Physics.SyncTransforms(); motion.BeginTick();
                    var intent = tick < halfTicks ? Vector3.forward : Vector3.back;
                    var desired = Quaternion.Euler(0, (tick % 4 - 1) * 8, 0);
                    var displacement = Vector3.ProjectOnPlane(intent, motion.SupportNormal).normalized * speed * .02f;
                    Assert.That(motion.Resolve(root, rotation, displacement, desired, out var end, out var facing),
                        Is.True, $"tick={tick}, block={motion.Block}");
                    Assert.That(Vector3.Dot(end - root, intent), Is.GreaterThan(.02f));
                    Assert.That(Quaternion.Angle(facing, desired), Is.LessThan(.1f), $"tick={tick}, block={motion.Block}, queries={motion.Queries}");
                    Assert.That(motion.Stepping, Is.False);
                    Assert.That(motion.Queries, Is.LessThanOrEqualTo(profile.queryBudget));
                    Assert.That(motion.Substeps, Is.LessThanOrEqualTo(profile.substepBudget));
                    Assert.That(motion.Clear(end, facing, true), Is.True);
                    root = end; rotation = facing;
                }
                Assert.That(root.z, Is.EqualTo(originZ).Within(.01f));
            }
            finally { Object.DestroyImmediate(terrain); Object.DestroyImmediate(data); }
        }

        [Test] public void SteeringEnvelopeStillBlocksWalls()
        {
            Box("Wall beside turning corner", start + new Vector3(.66f, .3f, .5f), new Vector3(.04f, 6, .15f));
            Physics.SyncTransforms(); motion.BeginTick();
            motion.Resolve(start, Quaternion.identity, Vector3.forward * .04f, Quaternion.Euler(0, 8, 0), out var end, out var facing);
            // A clear first substep may commit, but the turn into the wall must stop.
            Assert.That(Quaternion.Angle(Quaternion.identity, facing), Is.LessThan(7.9f));
            Assert.That(motion.Clear(end, facing, true), Is.True);
        }

        [Test] public void SteeringAscentCannotPassThroughRiderCeiling()
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            var slope = Box("Ceiling slope", new Vector3(start.x, -.1f / Mathf.Cos(40 * Mathf.Deg2Rad), start.z),
                new Vector3(30, .2f, 30), true);
            slope.transform.rotation = Quaternion.Euler(-40, 0, 0);
            Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.TrySupport(start, Quaternion.identity, profile.slopeDegrees, out var root, true), Is.True);
            mount.position = root;
            Box("Ceiling above rider", root + new Vector3(0, 3.38f, 0), new Vector3(5, .2f, 5));
            Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.Clear(root, Quaternion.identity, true), Is.True);
            motion.BeginTick();
            motion.Resolve(root, Quaternion.identity, Vector3.forward * .04f, Quaternion.Euler(0, 8, 0), out var end, out var facing);
            Assert.That(Quaternion.Angle(Quaternion.identity, facing), Is.LessThan(.1f));
            Assert.That(motion.Clear(end, facing, true), Is.True);
        }

        [Test] public void FlatGroundSteeringDoesNotRequireExtraHeadroom()
        {
            Box("Close flat ceiling", start + new Vector3(0, 3.38f, 0), new Vector3(5, .2f, 5));
            Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.Resolve(start, Quaternion.identity, Vector3.forward * .04f, Quaternion.Euler(0, 8, 0),
                out var end, out var facing), Is.True);
            Assert.That(Quaternion.Angle(facing, Quaternion.Euler(0, 8, 0)), Is.LessThan(.1f));
            Assert.That(end.y, Is.EqualTo(start.y).Within(.002f));
        }

        [Test] public void GlancingWallContactPreservesTravelAlongTheWall()
        {
            Box("Long brushing wall", start + new Vector3(.9f, .3f, 4), new Vector3(.2f, 5, 20));
            Physics.SyncTransforms(); var root = start;
            for (int tick = 0; tick < 20; tick++)
            {
                motion.BeginTick();
                motion.Resolve(root, Quaternion.identity, new Vector3(1, 0, 1).normalized * .48f,
                    Quaternion.identity, out var end, out _);
                Assert.That(end.z - root.z, Is.GreaterThan(.2f), $"tick={tick}, block={motion.Block}");
                Assert.That(motion.Clear(end, Quaternion.identity, true), Is.True);
                Assert.That(end.y, Is.EqualTo(start.y).Within(.002f));
                root = end; mount.position = root; Physics.SyncTransforms();
            }
            Assert.That(root.z - start.z, Is.GreaterThan(6));
        }

        [TestCase(false, 0f, 0f)]
        [TestCase(true, 0f, 0f)]
        [TestCase(true, 25f, 0f)]
        [TestCase(true, 40f, 0f)]
        [TestCase(true, 52f, 0f)]
        [TestCase(true, 52f, 45f)]
        [TestCase(true, 52f, 90f)]
        [TestCase(true, 52f, 180f)]
        [TestCase(true, 70f, 45f)]
        [TestCase(true, 70f, 90f)]
        public void HeadOnRockBrushesAsideAndContinuesWithoutLosingSupport(bool faceted, float slopeDegrees, float heading)
        {
            var root = start;
            var rotation = Quaternion.Euler(0, heading, 0);
            var forward = rotation * Vector3.forward;
            if (slopeDegrees > 0)
            {
                Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
                var floor = Box("Rock brushing slope", new Vector3(start.x, -.1f / Mathf.Cos(slopeDegrees * Mathf.Deg2Rad), start.z),
                    new Vector3(80, .2f, 80), true);
                floor.transform.rotation = Quaternion.Euler(-slopeDegrees, 0, 0);
                Physics.SyncTransforms(); motion.BeginTick();
                Assert.That(motion.TrySupport(start, rotation, profile.slopeDegrees, out root, true), Is.True);
            }
            var rock = new GameObject("Rounded rock"); objects.Add(rock);
            rock.transform.position = start + forward * 1.6f + Vector3.up * .1f;
            rock.transform.rotation = rotation;
            if (faceted)
            {
                var mesh = new Mesh(); meshes.Add(mesh);
                mesh.vertices = new[] { new Vector3(-.8f,0,-.8f),new Vector3(.8f,0,-.8f),new Vector3(.8f,0,.8f),new Vector3(-.8f,0,.8f),new Vector3(0,1.5f,0) };
                mesh.triangles = new[] { 0,4,1,1,4,2,2,4,3,3,4,0,0,1,2,0,2,3 };
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); rock.AddComponent<MeshCollider>().sharedMesh = mesh;
                rock.transform.position = new Vector3(start.x + forward.x * 1.6f,
                    forward.z * 1.6f * Mathf.Tan(slopeDegrees * Mathf.Deg2Rad), start.z + forward.z * 1.6f);
            }
            else rock.AddComponent<SphereCollider>().radius = .8f;
            mount.SetPositionAndRotation(root, rotation); Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.TrySupport(root, rotation, profile.slopeDegrees, out _), Is.True);
            float horizontalTravel = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(forward, motion.SupportNormal).normalized, Vector3.up).magnitude;
            // Steep ground reduces horizontal speed; allow enough time to verify continued travel beyond the rock.
            int ticks = slopeDegrees > 40 ? 40 : 30;
            for (int tick = 0; tick < ticks; tick++)
            {
                motion.BeginTick();
                var displacement = Vector3.ProjectOnPlane(forward, motion.SupportNormal).normalized * .48f;
                motion.Resolve(root, rotation, displacement, rotation, out var end, out _);
                Assert.That(Vector3.Distance(root, end), Is.GreaterThan(.01f), $"tick={tick}, block={motion.Block}");
                Assert.That(motion.Stepping, Is.False);
                Assert.That(motion.Queries, Is.LessThanOrEqualTo(profile.queryBudget));
                Assert.That(motion.Substeps, Is.LessThanOrEqualTo(profile.substepBudget));
                Assert.That(motion.Clear(end, rotation, true), Is.True);
                Assert.That(motion.TrySupport(end, rotation, profile.slopeDegrees, out var supported), Is.True);
                Assert.That(end.y, Is.EqualTo(supported.y).Within(.003f));
                root = end; mount.position = root; Physics.SyncTransforms();
            }
            Assert.That(Vector3.Dot(root - start, forward), Is.GreaterThan(6 * horizontalTravel));
        }

        [Test] public void BlockedTurnBesideWallCannotPreventRetreat()
        {
            Box("Wall beside retreat", start + new Vector3(.75f, .3f, 0), new Vector3(.2f, 5, 10));
            Physics.SyncTransforms(); motion.BeginTick();
            motion.Resolve(start, Quaternion.identity, Vector3.back * .48f, Quaternion.Euler(0, 8, 0), out var end, out var facing);
            Assert.That(start.z - end.z, Is.GreaterThan(.3f));
            Assert.That(motion.Clear(end, facing, true), Is.True);
        }

        [Test] public void ShallowRockOverlapRecoversLocallyAndAllowsRetreat()
        {
            Box("Shallow overlapping rock", start + new Vector3(.62f, .3f, 0), new Vector3(.2f, 5, 4));
            Physics.SyncTransforms(); motion.BeginTick();
            motion.Resolve(start, Quaternion.identity, Vector3.back * .2f, Quaternion.identity, out var end, out var facing);
            Assert.That(start.z - end.z, Is.GreaterThan(.15f));
            Assert.That(start.x - end.x, Is.InRange(.02f, profile.substepDistance));
            Assert.That(motion.Clear(end, facing, true), Is.True);
            Assert.That(end.y, Is.EqualTo(start.y).Within(.002f));
        }

        [Test] public void BlockedTurnCannotCancelAnOutwardEscape()
        {
            Box("Wall beside outward escape", start + new Vector3(.68f, .3f, 0), new Vector3(.2f, 5, 10));
            Physics.SyncTransforms(); motion.BeginTick();
            motion.Resolve(start, Quaternion.identity, Vector3.left * .02f, Quaternion.Euler(0, 8, 0), out var end, out var facing);
            Assert.That(start.x - end.x, Is.GreaterThan(.015f));
            Assert.That(motion.Clear(end, facing, true), Is.True);
        }

        [TestCase(.3f, false)]
        [TestCase(.62f, true)]
        public void OverlapRecoveryCannotTeleportOrPushThroughAnotherWall(float rockX, bool trapped)
        {
            Box("Overlap beyond recoverable space", start + new Vector3(rockX, .3f, 0), new Vector3(.2f, 5, 4));
            if (trapped) Box("Wall behind recovery", start + new Vector3(-.68f, .3f, 0), new Vector3(.2f, 5, 4));
            Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.Resolve(start, Quaternion.identity, Vector3.back * .2f, Quaternion.identity, out var end, out _), Is.False);
            Assert.That(end, Is.EqualTo(start));
        }

        [Test] public void BrushingCannotLeaveSupportedGroundToEscapeARock()
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            Box("Narrow supported approach", new Vector3(start.x, -.1f, start.z), new Vector3(1.2f, .2f, 20), true);
            Box("Rock across supported approach", start + new Vector3(0, .3f, 1.5f), new Vector3(2, 4, .4f));
            Physics.SyncTransforms(); var root = start;
            for (int tick = 0; tick < 10; tick++)
            {
                motion.BeginTick();
                motion.Resolve(root, Quaternion.identity, Vector3.forward * .48f, Quaternion.identity, out var end, out var facing);
                Assert.That(Mathf.Abs(end.x - start.x), Is.LessThan(.055f));
                Assert.That(motion.Clear(end, facing, true), Is.True);
                Assert.That(motion.TrySupport(end, facing, profile.slopeDegrees, out _), Is.True);
                root = end; mount.position = root; Physics.SyncTransforms();
            }
            Assert.That(root.z - start.z, Is.LessThan(.7f));
        }

        [TestCase(74.9f, true)]
        [TestCase(75f, true)]
        [TestCase(75.5f, false)]
        public void SteepSupportHonorsThe75DegreeBoundary(float degrees, bool allowed)
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            var floor = Box("Steep support boundary", new Vector3(start.x, -.1f / Mathf.Cos(degrees * Mathf.Deg2Rad), start.z),
                new Vector3(40, .2f, 40), true);
            floor.transform.rotation = Quaternion.Euler(-degrees, 0, 0);
            Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.TrySupport(start, Quaternion.identity, profile.slopeDegrees, out var root, true), Is.EqualTo(allowed));
            if (!allowed) return;
            mount.position = root; Physics.SyncTransforms(); motion.BeginTick();
            var delta = Vector3.ProjectOnPlane(Vector3.forward, motion.SupportNormal).normalized
                * profile.fastSpeed * profile.SpeedMultiplierForSlope(motion.SupportNormal) * .02f;
            Assert.That(motion.Resolve(root, Quaternion.identity, delta, Quaternion.Euler(0, 4, 0), out var end, out var facing), Is.True);
            Assert.That(end.z, Is.GreaterThan(root.z)); Assert.That(end.y, Is.GreaterThan(root.y));
            Assert.That(motion.Clear(end, facing, true), Is.True);
        }

        [TestCase(0f)]
        [TestCase(37f)]
        [TestCase(45f)]
        public void StraightClimbRetainsFullSurfaceSpeedThrough45Degrees(float degrees)
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            var floor = Box("Full-speed incline", new Vector3(start.x, -.1f / Mathf.Cos(degrees * Mathf.Deg2Rad), start.z),
                new Vector3(30, .2f, 30), true);
            floor.transform.rotation = Quaternion.Euler(-degrees, 0, 0);
            Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.TrySupport(start, Quaternion.identity, profile.slopeDegrees, out var root, true), Is.True);
            mount.position = root; Physics.SyncTransforms(); motion.BeginTick();
            float speed = profile.fastSpeed * profile.SpeedMultiplierForSlope(motion.SupportNormal);
            var delta = Vector3.ProjectOnPlane(Vector3.forward, motion.SupportNormal).normalized * speed * .02f;
            Assert.That(motion.Resolve(root, Quaternion.identity, delta, Quaternion.identity, out var end, out _), Is.True);
            Assert.That(Vector3.Distance(root, end), Is.EqualTo(profile.fastSpeed * .02f).Within(.003f));
        }

        [TestCase(0f, 0f)]
        [TestCase(40f, 0f)]
        [TestCase(70f, 0f)]
        [TestCase(0f, 45f)]
        [TestCase(40f, 45f)]
        [TestCase(70f, 45f)]
        public void UnevenNativeTerrainMaintainsGroundContactWhileTurningAndReversing(float degrees, float heading)
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            var data = new TerrainData { heightmapResolution = 257, size = new Vector3(32, 128, 32) };
            var heights = new float[257, 257];
            float gradient = Mathf.Tan(degrees * Mathf.Deg2Rad);
            for (int z = 0; z < 257; z++) for (int x = 0; x < 257; x++)
                heights[z,x] = (64 + (z * .125f - 16) * gradient
                    + .06f * Mathf.Sin(x * .5f) * Mathf.Sin(z * .5f)) / 128;
            data.SetHeights(0, 0, heights);
            var terrain = Terrain.CreateTerrainGameObject(data); objects.Add(terrain);
            terrain.transform.position = new Vector3(start.x - 16, -64, start.z - 16);
            try
            {
                var rotation = Quaternion.Euler(0, heading, 0);
                Physics.SyncTransforms(); motion.BeginTick();
                Assert.That(motion.TrySupport(start, rotation, profile.slopeDegrees, out var root, true), Is.True);
                var shape = mount.GetComponent<BoxCollider>();
                for (int tick = 0; tick < 36; tick++)
                {
                    mount.SetPositionAndRotation(root, rotation); Physics.SyncTransforms(); motion.BeginTick();
                    var intent = Quaternion.Euler(0, heading, 0) * (tick < 18 ? Vector3.forward : Vector3.back);
                    var direction = Vector3.ProjectOnPlane(intent, motion.SupportNormal).normalized;
                    var desired = Quaternion.Euler(0, heading + Mathf.Sin(tick * .5f) * 12, 0);
                    float speed = profile.fastSpeed * profile.SpeedMultiplierForSlope(motion.SupportNormal);
                    motion.Resolve(root, rotation, direction * speed * .02f, desired, out var end, out var facing);
                    Assert.That(Vector3.Distance(root, end), Is.GreaterThan(.005f), $"tick={tick}, block={motion.Block}");
                    Assert.That(motion.Stepping, Is.False, $"tick={tick}");
                    Assert.That(motion.Queries, Is.LessThanOrEqualTo(profile.queryBudget));
                    Assert.That(motion.Substeps, Is.LessThanOrEqualTo(profile.substepBudget));
                    Assert.That(motion.Clear(end, facing, true), Is.True);
                    bool grounded = false;
                    foreach (var hit in Physics.BoxCastAll(end + facing * shape.center + Vector3.up * .1f,
                        shape.size * .5f - Vector3.one * .005f, Vector3.down, facing, .2f, ~0, QueryTriggerInteraction.Ignore))
                        if (hit.collider == terrain.GetComponent<TerrainCollider>() && hit.distance <= .16f) grounded = true;
                    Assert.That(grounded, Is.True, $"tick={tick}, ground gap");
                    root = end; rotation = facing;
                }
            }
            finally { Object.DestroyImmediate(terrain); Object.DestroyImmediate(data); }
        }

        [TestCase(40f)]
        [TestCase(65f)]
        [TestCase(72f)]
        public void NativeRidgeTransitionKeepsMovingFromAscentToDescent(float degrees)
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            var data = new TerrainData { heightmapResolution = 129, size = new Vector3(8, 20, 8) };
            var heights = new float[129,129]; float gradient = Mathf.Tan(degrees * Mathf.Deg2Rad);
            for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++)
                heights[z,x] = Mathf.Min(z * .0625f, 8 - z * .0625f) * gradient / 20;
            data.SetHeights(0,0,heights);
            var terrain = Terrain.CreateTerrainGameObject(data); objects.Add(terrain);
            terrain.transform.position = new Vector3(start.x - 4, 0, start.z - 1);
            try
            {
                Physics.SyncTransforms(); motion.BeginTick();
                var rotation = Quaternion.identity;
                Assert.That(motion.TrySupport(start + Vector3.up * gradient, rotation, profile.slopeDegrees, out var root, true), Is.True);
                for (int tick = 0; tick < 120 && root.z < start.z + 4.5f; tick++)
                {
                    mount.SetPositionAndRotation(root, rotation); Physics.SyncTransforms(); motion.BeginTick();
                    var desired = Quaternion.Euler(0, Mathf.Sin(tick * .4f) * 8, 0);
                    var delta = Vector3.ProjectOnPlane(Vector3.forward, motion.SupportNormal).normalized * .16f;
                    motion.Resolve(root, rotation, delta, desired, out var end, out var facing);
                    Assert.That(end.z - root.z, Is.GreaterThan(.005f), $"tick={tick}, block={motion.Block}");
                    Assert.That(motion.Stepping, Is.False);
                    Assert.That(motion.Clear(end, facing, true), Is.True);
                    Assert.That(motion.Queries, Is.LessThanOrEqualTo(profile.queryBudget));
                    root = end; rotation = facing;
                }
                Assert.That(root.z, Is.GreaterThanOrEqualTo(start.z + 4.5f));
            }
            finally { Object.DestroyImmediate(terrain); Object.DestroyImmediate(data); }
        }

        [TestCase(.12f)]
        [TestCase(.3f)]
        public void InteriorTerrainCrestIsCrossedWithoutDriftingOrTakingAnAirborneStep(float height)
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            var data = new TerrainData { heightmapResolution = 129, size = new Vector3(8, 2, 8) };
            var heights = new float[129,129];
            for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++)
                heights[z,x] = height * Mathf.Max(0, 1 - Mathf.Abs(x * .0625f - 4.25f) / .25f)
                    * Mathf.Max(0, 1 - Mathf.Abs(z * .0625f - 4.9f) / .25f) / 2;
            data.SetHeights(0,0,heights);
            var terrain = Terrain.CreateTerrainGameObject(data); objects.Add(terrain);
            terrain.transform.position = new Vector3(start.x - 4, 0, start.z - 4);
            try
            {
                Physics.SyncTransforms(); motion.BeginTick();
                Assert.That(motion.TrySupport(start, Quaternion.identity, profile.slopeDegrees, out var root, true), Is.True);
                for (int tick = 0; tick < 24; tick++)
                {
                    mount.position = root; Physics.SyncTransforms(); motion.BeginTick();
                    motion.Resolve(root, Quaternion.identity, Vector3.forward * .08f, Quaternion.identity, out var end, out _);
                    Assert.That(end.z - root.z, Is.GreaterThan(.06f), $"tick={tick}, block={motion.Block}");
                    Assert.That(end.x, Is.EqualTo(start.x).Within(.002f));
                    Assert.That(motion.Stepping, Is.False);
                    Assert.That(motion.Clear(end, Quaternion.identity, true), Is.True);
                    root = end;
                }
                Assert.That(root.y, Is.EqualTo(start.y).Within(.03f));
            }
            finally { Object.DestroyImmediate(terrain); Object.DestroyImmediate(data); }
        }

        [Test] public void WholeFootprintFitCannotChooseAnUnprobedRaisedPlatform()
        {
            Box("Small raised platform", start + new Vector3(.25f, -.925f, .9f), new Vector3(.15f, 1, .3f), true);
            Physics.SyncTransforms(); motion.BeginTick();
            Assert.That(motion.TrySupport(start + Vector3.forward * .4f, Quaternion.identity, profile.slopeDegrees, out var support), Is.True);
            Assert.That(support.y, Is.EqualTo(start.y).Within(.003f));
            var root = start;
            for (int tick = 0; tick < 12; tick++)
            {
                mount.position = root; Physics.SyncTransforms(); motion.BeginTick();
                motion.Resolve(root, Quaternion.identity, Vector3.forward * .16f, Quaternion.identity, out var end, out var facing);
                Assert.That(end.y, Is.LessThan(start.y + .05f));
                Assert.That(motion.Clear(end, facing, true), Is.True);
                root = end;
            }
        }

        [TestCase(0f)]
        [TestCase(45f)]
        [TestCase(90f)]
        [TestCase(135f)]
        [TestCase(180f)]
        [TestCase(225f)]
        [TestCase(270f)]
        [TestCase(315f)]
        public void ReportedNativeOpenSpacePoseAllowsSprintingAndEscape(float heading)
        {
            Object.DestroyImmediate(objects.Find(o => o.name == "Native support fixture"));
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath),
                "Docs/Evidence/Gameplay/LeggerOpenSprintStop20261021/native-pose.json");
            var snapshot = JsonUtility.FromJson<NativeSprintPose>(System.IO.File.ReadAllText(path));
            // Preserve the captured shape and heightfield, away from any open author scene colliders.
            var offset = new Vector3(40000, 0, 40000);
            var terrains = new List<GameObject>(); var data = new List<TerrainData>();
            try
            {
                foreach (var tile in snapshot.tiles)
                {
                    var d = new TerrainData { heightmapResolution = tile.resolution,
                        size = new Vector3(tile.size[0], tile.size[1], tile.size[2]) }; data.Add(d);
                    var heights = new float[tile.resolution,tile.resolution];
                    for(int z=0;z<tile.resolution;z++)for(int x=0;x<tile.resolution;x++)
                        heights[z,x]=tile.heights[z*tile.resolution+x];
                    d.SetHeights(0,0,heights);
                    var terrain=Terrain.CreateTerrainGameObject(d);terrains.Add(terrain);objects.Add(terrain);
                    terrain.transform.position=new Vector3(tile.origin[0],tile.origin[1],tile.origin[2])+offset;
                }
                var root=new Vector3(snapshot.root[0],snapshot.root[1],snapshot.root[2])+offset;
                var rotation=new Quaternion(snapshot.rotation[0],snapshot.rotation[1],snapshot.rotation[2],snapshot.rotation[3]);
                var intent=Quaternion.Euler(0,heading,0)*(rotation*Vector3.forward);
                motion=new LeggerGroundMotion(profile,mount,rider,mount.GetComponent<BoxCollider>(),
                    rider.GetComponent<CapsuleCollider>(),Vector3.up*2.39f);
                for(int tick=0;tick<12;tick++)
                {
                    mount.SetPositionAndRotation(root,rotation);Physics.SyncTransforms();motion.BeginTick();
                    Assert.That(motion.TrySupport(root,rotation,profile.slopeDegrees,out _),Is.True);
                    var delta=Vector3.ProjectOnPlane(intent,motion.SupportNormal).normalized*profile.fastSpeed*.02f;
                    var desired=Quaternion.RotateTowards(rotation,Quaternion.LookRotation(intent),7.2f);
                    motion.Resolve(root,rotation,delta,desired,out var end,out var facing);
                    Assert.That(Vector3.Dot(end-root,intent),Is.GreaterThan(.3f),$"tick={tick}, block={motion.Block}");
                    Assert.That(motion.Stepping,Is.False);
                    Assert.That(motion.Queries,Is.LessThanOrEqualTo(profile.queryBudget));
                    Assert.That(motion.Substeps,Is.LessThanOrEqualTo(profile.substepBudget));
                    Assert.That(motion.Clear(end,facing,true),Is.True);
                    root=end;rotation=facing;
                }
            }
            finally
            {
                foreach(var terrain in terrains)Object.DestroyImmediate(terrain);
                foreach(var d in data)Object.DestroyImmediate(d);
            }
        }
    }
}
