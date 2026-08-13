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
        public void AdjacentChunkMeshes_ShareExactBorderNormals()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var left = TopDown3DChunkMeshBuilder.BuildData(settings, Vector2Int.zero);
            var right = TopDown3DChunkMeshBuilder.BuildData(settings, Vector2Int.right);
            var verticesPerAxis = settings.QuadsPerAxis + 1;
            for (var z = 0; z < verticesPerAxis; z++)
            {
                var leftNormal = left.Normals[z * verticesPerAxis + settings.QuadsPerAxis];
                var rightNormal = right.Normals[z * verticesPerAxis];
                Assert.That(Vector3.Distance(leftNormal, rightNormal), Is.LessThan(0.000001f), $"Normal seam at row {z}.");
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
        public void CameraOrbit_IsFrameRateIndependentAndPitchClamped()
        {
            var oneStepYaw = TopDown3DCameraRig.CalculateYaw(350f, 1f, 120f, 0.25f);
            var twoStepYaw = TopDown3DCameraRig.CalculateYaw(
                TopDown3DCameraRig.CalculateYaw(350f, 1f, 120f, 0.125f),
                1f,
                120f,
                0.125f);
            Assert.That(oneStepYaw, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(twoStepYaw, Is.EqualTo(oneStepYaw).Within(0.0001f));
            Assert.That(
                TopDown3DCameraRig.CalculatePitch(40f, 1f, 70f, 1f, 38f, 65f),
                Is.EqualTo(38f).Within(0.0001f));
            Assert.That(
                TopDown3DCameraRig.CalculatePitch(60f, -1f, 70f, 1f, 38f, 65f),
                Is.EqualTo(65f).Within(0.0001f));
        }

        [Test]
        public void LandscapeCameraPullback_HasMatchingWorldCoverage()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            var cameraObject = new GameObject("Landscape camera coverage contract");
            try
            {
                cameraObject.AddComponent<Camera>();
                var rig = cameraObject.AddComponent<TopDown3DCameraRig>();

                Assert.That(settings, Is.Not.Null);
                Assert.That(rig.Distance, Is.EqualTo(25f).Within(0.0001f));
                Assert.That(settings.StreamingRadius, Is.GreaterThanOrEqualTo(7));
                Assert.That(settings.ImmediateLoadRadius, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void SafeSpawnSearch_ReturnsDeterministicWalkableGround()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var foundFirst = TopDown3DHeightSampler.TryFindWalkablePosition(
                settings,
                Vector2.zero,
                settings.SafeSpawnSearchRadius,
                settings.SafeSpawnSearchStep,
                settings.MaximumSafeSpawnSlope,
                out var first);
            var foundSecond = TopDown3DHeightSampler.TryFindWalkablePosition(
                settings,
                Vector2.zero,
                settings.SafeSpawnSearchRadius,
                settings.SafeSpawnSearchStep,
                settings.MaximumSafeSpawnSlope,
                out var second);
            Assert.That(foundFirst, Is.True);
            Assert.That(foundSecond, Is.True);
            Assert.That(second, Is.EqualTo(first));
            Assert.That(
                Vector3.Angle(
                    TopDown3DHeightSampler.SampleNormal(settings, first.x, first.z),
                    Vector3.up),
                Is.LessThanOrEqualTo(settings.MaximumSafeSpawnSlope));
        }

        [Test]
        public void BigArmSpeedProfile_StopsInsideFollowBandAndAcceleratesForCatchUp()
        {
            var stopped = TopDown3DBigArmFollower.CalculateDesiredSpeed(0.8f, 0.85f, 2.8f, 5.8f, 8.4f, false);
            var following = TopDown3DBigArmFollower.CalculateDesiredSpeed(5f, 0.85f, 2.8f, 5.8f, 8.4f, false);
            var catchingUp = TopDown3DBigArmFollower.CalculateDesiredSpeed(5f, 0.85f, 2.8f, 5.8f, 8.4f, true);

            Assert.That(stopped, Is.EqualTo(0f));
            Assert.That(following, Is.EqualTo(5.8f).Within(0.0001f));
            Assert.That(catchingUp, Is.EqualTo(8.4f).Within(0.0001f));
        }

        [Test]
        public void BigArmCall_DoesNotRelocateCompanion()
        {
            var bigArm = new GameObject("BigARM no-teleport contract");
            try
            {
                bigArm.transform.position = new Vector3(-40f, 2f, 17f);
                bigArm.AddComponent<BoxCollider>();
                bigArm.AddComponent<Rigidbody>();
                var follower = bigArm.AddComponent<TopDown3DBigArmFollower>();
                var before = bigArm.transform.position;

                follower.RequestRecall();

                Assert.That(bigArm.transform.position, Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(bigArm);
            }
        }

        [Test]
        public void PerspectiveCharacterMaterials_UseSwappedIdentityColors()
        {
            var booter = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Materials/TopDown3D/Greybox_Booter.mat");
            var bigArm = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Materials/TopDown3D/Greybox_BigARM.mat");

            Assert.That(booter, Is.Not.Null);
            Assert.That(bigArm, Is.Not.Null);
            Assert.That(booter.GetColor("_BaseColor"), Is.EqualTo(new Color(0.58f, 0.49f, 0.37f, 1f)));
            Assert.That(bigArm.GetColor("_BaseColor"), Is.EqualTo(new Color(0.08f, 0.74f, 0.76f, 1f)));
        }

        [Test]
        public void RegionalDustField_IsDeterministicAndBounded()
        {
            var position = new Vector3(84.25f, 3f, -117.5f);
            var first = TopDown3DDustAtmosphere.EvaluateRegionalIntensity(
                24681357,
                position,
                0.0065f,
                TopDown3DDustAtmosphere.DefaultMinimumRegionalIntensity,
                TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity);
            var second = TopDown3DDustAtmosphere.EvaluateRegionalIntensity(
                24681357,
                position,
                0.0065f,
                TopDown3DDustAtmosphere.DefaultMinimumRegionalIntensity,
                TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(
                first,
                Is.InRange(
                    TopDown3DDustAtmosphere.DefaultMinimumRegionalIntensity,
                    TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity));
        }

        [Test]
        public void AlwaysOnDust_KeepsHalfVisibilityNearLandscapeCameraRange()
        {
            var clearestDensity = TopDown3DDustAtmosphere.DefaultFogDensityAtIntensityOne
                * TopDown3DDustAtmosphere.DefaultMinimumRegionalIntensity;
            var dustiestDensity = TopDown3DDustAtmosphere.DefaultFogDensityAtIntensityOne
                * TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity;

            Assert.That(
                TopDown3DDustAtmosphere.EvaluateHalfVisibilityDistance(clearestDensity),
                Is.InRange(26f, 27f));
            Assert.That(
                TopDown3DDustAtmosphere.EvaluateHalfVisibilityDistance(dustiestDensity),
                Is.InRange(17f, 18f));
        }

        [Test]
        public void DustZoneWeight_HasFullStrengthCoreAndSmoothFalloff()
        {
            Assert.That(TopDown3DDustZone.EvaluateWeight(8f, 10f, 12f), Is.EqualTo(1f));
            Assert.That(TopDown3DDustZone.EvaluateWeight(22f, 10f, 12f), Is.EqualTo(0f));
            Assert.That(TopDown3DDustZone.EvaluateWeight(16f, 10f, 12f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void DustAtmosphere_CoreZoneProvidesGameplayReadableExposure()
        {
            var atmosphereObject = new GameObject("Dust atmosphere contract");
            var zoneObject = new GameObject("Dense dust contract zone");
            try
            {
                var atmosphere = atmosphereObject.AddComponent<TopDown3DDustAtmosphere>();
                atmosphere.Configure(null, null, 24681357);
                zoneObject.transform.position = new Vector3(30f, 0f, -12f);
                zoneObject.AddComponent<TopDown3DDustZone>().Configure(
                    10f,
                    12f,
                    1.8f,
                    new Color(0.72f, 0.32f, 0.12f));

                var sample = atmosphere.SampleAtPosition(zoneObject.transform.position);

                Assert.That(sample.Intensity, Is.EqualTo(1.8f).Within(0.0001f));
                Assert.That(sample.ZoneInfluence, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(zoneObject);
                Object.DestroyImmediate(atmosphereObject);
            }
        }

        [Test]
        public void GameplayActions_ContainRequiredGamepadAndKeyboardBindings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.That(asset, Is.Not.Null);
            var gameplay = asset.FindActionMap("Gameplay", true);
            AssertBinding(gameplay.FindAction("Move", true), "<Gamepad>/leftStick", "Gamepad");
            AssertBinding(gameplay.FindAction("Look", true), "<Gamepad>/rightStick", "Gamepad");
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
