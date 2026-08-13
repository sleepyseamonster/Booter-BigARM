using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.Runtime;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class ConversionAssetStructureTests
    {
        [Test]
        public void ProtectedBaselineValidator_ReportsNoErrors()
        {
            var errors = ConversionBaselineValidator.CollectErrors();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void ConversionLab_ContainsRequiredSpikeProofsAndExplicitRenderer()
        {
            var scene = EditorSceneManager.OpenScene(
                ConversionBaselineValidator.ConversionScenePath,
                OpenSceneMode.Additive);

            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots, Is.Not.Empty);
                Assert.That(FindComponents<IsometricPlayerMotor3D>(roots).Count(), Is.EqualTo(1));
                Assert.That(FindComponents<IsometricHarvestNode3D>(roots).Count(), Is.EqualTo(1));
                Assert.That(FindComponents<IsometricWorldItemPickup3D>(roots).Count(), Is.EqualTo(1));
                Assert.That(FindComponents<IsometricBigArmFollower3D>(roots).Count(), Is.EqualTo(1));
                Assert.That(FindComponents<IsometricOccluder>(roots).Any(), Is.True);
                Assert.That(FindComponents<CinemachineCamera>(roots).Count(), Is.EqualTo(1));

                var cameraData = FindComponents<UniversalAdditionalCameraData>(roots).Single();
                var serializedCameraData = new SerializedObject(cameraData);
                Assert.That(serializedCameraData.FindProperty("m_RendererIndex").intValue, Is.EqualTo(1));
                Assert.That(cameraData.GetComponent<Camera>().orthographic, Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void HarvestNode_DeliversItsConfiguredYieldOnlyOnceUntilRespawn()
        {
            var nodeObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var node = nodeObject.AddComponent<IsometricHarvestNode3D>();
            node.Configure("test.node", "Test Node", "ironstone", 3, 0.25f, 5f, nodeObject.GetComponent<Renderer>());
            var receiver = new RecordingReceiver();

            try
            {
                Assert.That(node.TryHarvest(receiver), Is.True);
                Assert.That(receiver.LastItems, Has.Length.EqualTo(1));
                Assert.That(receiver.LastItems[0].ItemId, Is.EqualTo("ironstone"));
                Assert.That(receiver.LastItems[0].Amount, Is.EqualTo(3));
                Assert.That(node.IsAvailable, Is.False);
                Assert.That(node.TryHarvest(receiver), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(nodeObject);
            }
        }

        [Test]
        public void BigArmRecall_UsesCameraRelativeScreenLeftPosition()
        {
            var target = new GameObject("Target");
            var camera = new GameObject("Camera Basis");
            var bigArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bigArm.AddComponent<Rigidbody>();
            var follower = bigArm.AddComponent<IsometricBigArmFollower3D>();
            target.transform.position = new Vector3(2f, 1f, 3f);
            camera.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
            follower.Configure(target.transform, camera.transform, null);

            try
            {
                var desired = follower.GetDesiredPosition();
                var screenRight = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
                Assert.That(Vector3.Dot(desired - target.transform.position, screenRight), Is.LessThan(-5f));

                follower.RequestRecall();
                Assert.That(
                    Vector3.Distance(bigArm.GetComponent<Rigidbody>().position, desired),
                    Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(bigArm);
                Object.DestroyImmediate(camera);
                Object.DestroyImmediate(target);
            }
        }

        private static T[] FindComponents<T>(GameObject[] roots) where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private sealed class RecordingReceiver : IPrototypeItemReceiver
        {
            public PrototypeItemAmount[] LastItems { get; private set; }

            public bool TryAddItems(PrototypeItemAmount[] items)
            {
                LastItems = items;
                return true;
            }
        }
    }
}
