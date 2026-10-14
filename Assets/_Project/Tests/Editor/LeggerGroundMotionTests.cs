using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class LeggerGroundMotionTests
    {
        private readonly List<GameObject> objects = new();
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
    }
}
