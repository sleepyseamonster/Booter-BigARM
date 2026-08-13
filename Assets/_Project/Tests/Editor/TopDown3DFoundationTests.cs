using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DFoundationTests
    {
        private const string WorldSettingsPath = "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";

        [Test]
        public void HeightSampling_IsDeterministic()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var first = TopDown3DHeightSampler.SampleHeight(settings, 37.25f, -18.75f);
            var second = TopDown3DHeightSampler.SampleHeight(settings, 37.25f, -18.75f);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void AdjacentChunkMeshes_ShareExactBorderHeights()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var left = TopDown3DChunkMeshBuilder.BuildData(settings, Vector2Int.zero);
            var right = TopDown3DChunkMeshBuilder.BuildData(settings, Vector2Int.right);
            var verticesPerAxis = settings.QuadsPerAxis + 1;
            for (var z = 0; z < verticesPerAxis; z++)
            {
                var leftHeight = left.Vertices[z * verticesPerAxis + settings.QuadsPerAxis].y;
                var rightHeight = right.Vertices[z * verticesPerAxis].y;
                Assert.That(rightHeight, Is.EqualTo(leftHeight).Within(0.000001f), $"Seam mismatch at row {z}.");
            }
        }

        [Test]
        public void PerspectiveMovementBasis_IsCameraRelativeAndSpeedClamped()
        {
            var direction = TopDown3DMovementBasis.ToWorldDirection(
                new Vector2(1f, 1f),
                new Vector3(1f, -1f, 1f),
                new Vector3(1f, 0f, -1f));
            Assert.That(direction.y, Is.EqualTo(0f).Within(0.00001f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.00001f));
        }

        [Test]
        public void GameplayActions_ContainRequiredGamepadAndKeyboardBindings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.That(asset, Is.Not.Null);
            var gameplay = asset.FindActionMap("Gameplay", true);
            AssertBinding(gameplay.FindAction("Move", true), "<Gamepad>/leftStick", "Gamepad");
            AssertBinding(gameplay.FindAction("Sprint", true), "<Gamepad>/rightShoulder", "Gamepad");
            AssertBinding(gameplay.FindAction("RecallBigArm", true), "<Gamepad>/leftShoulder", "Gamepad");
            AssertBinding(gameplay.FindAction("Move", true), "<Keyboard>/w", "Keyboard&Mouse");
            AssertBinding(gameplay.FindAction("Sprint", true), "<Keyboard>/leftShift", "Keyboard&Mouse");
            AssertBinding(gameplay.FindAction("RecallBigArm", true), "<Keyboard>/f1", "Keyboard&Mouse");
        }

        [Test]
        public void PerspectivePrototypeValidator_ReportsNoErrors()
        {
            var errors = TopDown3DPrototypeValidator.CollectErrors();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        private static void AssertBinding(InputAction action, string path, string group)
        {
            Assert.That(
                action.bindings.Any(binding => binding.path == path && binding.groups.Contains(group)),
                Is.True,
                $"{action.name} is missing {path} for {group}.");
        }
    }
}
