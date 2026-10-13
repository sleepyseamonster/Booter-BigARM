using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DInteractionQueryTests
    {
        private readonly List<GameObject> objects = new();
        private readonly List<Scene> scenes = new();
        private readonly Vector3 origin = new(12345f, 12345f, 12345f);
        private Scene previousActive, scene;
        private TopDown3DItemCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            previousActive = SceneManager.GetActiveScene();
            // Owned objects at isolated coordinates use the existing default
            // physics scene, without loading saved gameplay geometry.
            scene = previousActive;
            catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(
                "Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            for (var i = scenes.Count - 1; i >= 0; i--)
                if (scenes[i].IsValid() && scenes[i].isLoaded) EditorSceneManager.ClosePreviewScene(scenes[i]);
            scenes.Clear();
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            Physics.SyncTransforms();
        }

        [Test]
        public void CargoLineOfSight_IgnoresDenseActorAndTargetChildrenButRejectsWallBehindThem()
        {
            var actor = Create("Query actor", Vector3.zero);
            var target = Create("Query cargo target", Vector3.right * 3f);
            var from = actor.transform.position + Vector3.up * .5f;
            var to = target.transform.position + Vector3.up * .5f;
            for (var i = 0; i < 64; i++)
                CreateBox("Ignored actor child " + i, Vector3.Lerp(from, to, (i + 1f) / 66f),
                    .012f, actor.transform);
            for (var i = 0; i < 4; i++)
                CreateBox("Ignored target child " + i, Vector3.Lerp(from, to, .15f + .15f * i),
                    .008f, target.transform);
            Physics.SyncTransforms();
            var actorHits = 0;
            foreach (var hit in Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to),
                         ~0, QueryTriggerInteraction.Ignore))
                if (hit.transform.IsChildOf(actor.transform)) actorHits++;
            Assert.That(actorHits, Is.EqualTo(64), "Fixture must actually hit all ignored actor children.");
            Assert.That(TopDown3DCargoAccess.HasLineOfSight(actor.transform, target.transform), Is.True);

            var wall = CreateBox("Wall behind ignored children", Vector3.Lerp(from, to, .99f), .012f);
            Physics.SyncTransforms();
            Assert.That(TopDown3DCargoAccess.HasLineOfSight(actor.transform, target.transform), Is.False,
                "A blocker after ignored child colliders still rejects cargo access.");
            wall.enabled = false;
            Physics.SyncTransforms();
            Assert.That(TopDown3DCargoAccess.HasLineOfSight(actor.transform, target.transform), Is.True);
            AssertWarmedNoAllocations(() =>
            {
                if (!TopDown3DCargoAccess.HasLineOfSight(actor.transform, target.transform))
                    throw new System.InvalidOperationException("Warmed clear LOS changed.");
            });
        }

        [Test]
        public void CargoLineOfSight_IgnoresTriggerButRejectsSameColliderWhenSolid()
        {
            var actor = Create("Trigger query actor", Vector3.zero);
            var target = Create("Trigger query target", Vector3.right * 3f);
            var collider = CreateBox("Trigger obstruction", origin + new Vector3(1.5f, .5f, 0), .2f);
            collider.isTrigger = true;
            Physics.SyncTransforms();
            Assert.That(TopDown3DCargoAccess.HasLineOfSight(actor.transform, target.transform), Is.True);
            collider.isTrigger = false;
            Physics.SyncTransforms();
            Assert.That(TopDown3DCargoAccess.HasLineOfSight(actor.transform, target.transform), Is.False);
        }

        [Test]
        public void CargoAccess_PreservesEnabledSameSceneAndRangeGuards()
        {
            var actor = Create("Access guard actor", Vector3.zero);
            var target = Create("Access guard cargo", Vector3.right * 2f);
            var cargo = target.AddComponent<TopDown3DBigArmCargo>();
            var packing = AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>(
                "Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset");
            Assert.That(packing, Is.Not.Null);
            cargo.Configure(catalog, packing);
            Physics.SyncTransforms();
            Assert.That(TopDown3DCargoAccess.CanAccess(cargo, actor.transform), Is.True);
            cargo.enabled = false;
            Assert.That(TopDown3DCargoAccess.CanAccess(cargo, actor.transform), Is.False);
            cargo.enabled = true;
            target.SetActive(false);
            Assert.That(TopDown3DCargoAccess.CanAccess(cargo, actor.transform), Is.False);
            target.SetActive(true);
            target.transform.position = origin + Vector3.right * (cargo.AccessRange + .25f);
            Physics.SyncTransforms();
            Assert.That(TopDown3DCargoAccess.CanAccess(cargo, actor.transform), Is.False);
            target.transform.position = origin + Vector3.right * 2f;
            var other = EditorSceneManager.NewPreviewScene();
            scenes.Add(other);
            SceneManager.MoveGameObjectToScene(target, other);
            Physics.SyncTransforms();
            Assert.That(TopDown3DCargoAccess.CanAccess(cargo, actor.transform), Is.False);
            SceneManager.MoveGameObjectToScene(target, scene);
            Physics.SyncTransforms();
            Assert.That(TopDown3DCargoAccess.CanAccess(cargo, actor.transform), Is.True);
            Assert.That(TopDown3DCargoAccess.CanAccess(null, actor.transform), Is.False);
            Assert.That(TopDown3DCargoAccess.CanAccess(cargo, null), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActionStartup_ResolvesLateCargoAndPreservesExplicitSameSceneOwner(bool explicitOwner)
        {
            scene = EditorSceneManager.NewPreviewScene(); scenes.Add(scene);
            var actor = Create("Startup actor", Vector3.zero); actor.SetActive(false);
            var actions = Object.Instantiate(AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            try
            {
                var router = actor.AddComponent<TopDown3DInputRouter>(); router.Configure(actions);
                var kit = actor.AddComponent<TopDown3DPlayerInventory>(); kit.Configure(catalog, 8);
                var action = actor.AddComponent<TopDown3DPlayerActionController>();
                action.Configure(router, null, kit, null, null);
                actor.SetActive(true); Assert.That(router.Initialize(), Is.True);
                if (explicitOwner) CreateStartupCargo("Other cargo", Vector3.right * 10f);
                var cargo = CreateStartupCargo("Late nearby cargo", Vector3.right * 2f);
                if (explicitOwner) action.ConfigureCargo(cargo);
                else Assert.That(action.CanOpenCargo, Is.False,
                    "An action initialized before cargo exists reproduces the missing startup owner.");
                Physics.SyncTransforms();
                typeof(TopDown3DPlayerActionController).GetMethod("Start",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(action, null);
                Assert.That(action.CanOpenCargo, Is.True);
                TopDown3DBigArmCargo requested = null;
                action.CargoAccessRequested += value => requested = value;
                typeof(TopDown3DPlayerActionController).GetMethod("HandleCargoOpenRequested",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(action, null);
                Assert.That(requested, Is.SameAs(cargo), "Cargo command must reach the live intended owner.");
                Assert.That(kit.State.OccupiedSlotCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(actor); Object.DestroyImmediate(actions); }
        }

        private TopDown3DBigArmCargo CreateStartupCargo(string name, Vector3 offset)
        {
            var value = Create(name, offset).AddComponent<TopDown3DBigArmCargo>();
            value.Configure(catalog, AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>(
                "Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset"));
            return value;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DenseTargeting_FindsLastCreatedNearestOrStableIdTieBeyond64Colliders(bool equalDistance)
        {
            var actor = Create("Dense targeting actor", Vector3.zero);
            var body = actor.AddComponent<Rigidbody>(); body.isKinematic = true;
            var motor = actor.AddComponent<TopDown3DPlayerMotor>();
            motor.FacePlanarDirection(Vector3.forward);
            var controller = actor.AddComponent<TopDown3DInteractionController>();
            controller.Configure(motor);
            var first = CreateCache("cache:z", equalDistance
                ? new Vector3(.35f, .8f, 1.5f) : new Vector3(.35f, .8f, 2.3f));
            // Dense irrelevant geometry belongs above the line to each target so
            // saturation cannot accidentally turn into an obstruction test.
            for (var i = 0; i < 96; i++)
                CreateBox("Dense irrelevant collider " + i,
                    origin + new Vector3((i % 12) * .025f, 2f, (i / 12) * .025f), .01f);
            var last = CreateCache("cache:a", equalDistance
                ? new Vector3(-.35f, .8f, 1.5f) : new Vector3(-.35f, .8f, 1.1f));
            Physics.SyncTransforms();
            Assert.That(Physics.OverlapSphere(actor.transform.position, 4f, ~0,
                QueryTriggerInteraction.Collide).Length, Is.GreaterThan(64));
            controller.RefreshTarget();
            Assert.That(controller.CurrentTarget, Is.SameAs(last),
                equalDistance ? "Equal eligible distances/facing use stable ID ordering."
                    : "Dense query must include the nearest eligible target created after the clutter.");
            last.enabled = false;
            controller.RefreshTarget();
            Assert.That(controller.CurrentTarget, Is.SameAs(first), "Disabled retained target must drop immediately.");
            AssertWarmedNoAllocations(controller.RefreshTarget);
        }

        private static void AssertWarmedNoAllocations(System.Action action)
        {
            // Windows' CLR byte counter is not reliable here. Validate the
            // stopped current-thread allocation-event observer before use.
            Assert.That(AllocationEvents(() => System.GC.KeepAlive(new byte[4096]), 64), Is.GreaterThanOrEqualTo(64));
            Assert.That(AllocationEvents(() => { }, 64), Is.Zero);
            for (var i = 0; i < 16; i++) action();
            Assert.That(AllocationEvents(action, 256), Is.Zero,
                "After capacity growth, a complete unchanged query must reuse its buffer.");
        }

        private static long AllocationEvents(System.Action action, int count)
        {
            using var recorder = new ProfilerRecorder(ProfilerCategory.Memory, "GC.Alloc", 1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.SumAllSamplesInFrame);
            Assert.That(recorder.Valid, Is.True);
            recorder.Start();
            for (var i = 0; i < count; i++) action();
            recorder.Stop();
            return recorder.Count == 0 ? 0 : (long)recorder.GetSample(recorder.Count - 1).Count;
        }

        private GameObject Create(string name, Vector3 offset)
        {
            var value = new GameObject(name);
            objects.Add(value);
            SceneManager.MoveGameObjectToScene(value, scene);
            value.transform.position = origin + offset;
            return value;
        }

        private BoxCollider CreateBox(string name, Vector3 worldPosition, float size, Transform parent = null)
        {
            var value = Create(name, worldPosition - origin);
            if (parent != null) value.transform.SetParent(parent, true);
            var collider = value.AddComponent<BoxCollider>(); collider.size = Vector3.one * size;
            return collider;
        }

        private TopDown3DResourceCache CreateCache(string id, Vector3 offset)
        {
            var value = Create(id, offset);
            value.AddComponent<SphereCollider>().radius = .1f;
            var cache = value.AddComponent<TopDown3DResourceCache>();
            cache.Configure(catalog, 2, id);
            return cache;
        }
    }
}
