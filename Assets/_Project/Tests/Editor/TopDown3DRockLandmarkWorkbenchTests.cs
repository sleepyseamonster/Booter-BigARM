using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRockLandmarkWorkbenchTests
    {
        private const string WorkbenchMaterialPath =
            "Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat";

        [Test]
        public void LandmarkControlsClampAndScaleFormationCount()
        {
            var root = new GameObject("Rock Landmark Control Test");
            try
            {
                var landmark = root.AddComponent<TopDown3DRockLandmarkAuthoring>();
                var serialized = new SerializedObject(landmark);
                serialized.FindProperty("generatedWidth").floatValue = -50f;
                serialized.FindProperty("generatedHeight").floatValue = 100f;
                serialized.FindProperty("generatedAsymmetry").floatValue = 4f;
                serialized.FindProperty("approachOpening").floatValue = -2f;
                serialized.FindProperty("memberVoxelSize").floatValue = -1f;
                serialized.FindProperty("memberFusionSmoothness").floatValue = 3f;
                serialized.FindProperty("memberSurfaceRelaxation").floatValue = 3f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(landmark.GeneratedWidth, Is.EqualTo(12f));
                Assert.That(landmark.GeneratedHeight, Is.EqualTo(30f));
                Assert.That(landmark.GeneratedAsymmetry, Is.EqualTo(1f));
                Assert.That(landmark.ApproachOpening, Is.EqualTo(0f));
                Assert.That(landmark.MemberVoxelSize, Is.EqualTo(0.025f));
                Assert.That(landmark.MemberFusionSmoothness, Is.EqualTo(0.35f));
                Assert.That(landmark.MemberSurfaceRelaxation, Is.EqualTo(1f));
                Assert.That(TopDown3DRockLandmarkAuthoring.CalculateFormationCount(12f, 4f),
                    Is.EqualTo(12));
                Assert.That(TopDown3DRockLandmarkAuthoring.CalculateFormationCount(60f, 30f),
                    Is.EqualTo(29));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SpireComplexPlanIsDeterministicLayeredAndUsesUniqueFormationSeeds()
        {
            var first = TopDown3DRockLandmarkGenerator.CreatePlan(
                TopDown3DRockLandmarkArchetype.SpireComplex,
                8675309,
                29,
                36f,
                14f,
                0.68f,
                0.34f);
            var repeat = TopDown3DRockLandmarkGenerator.CreatePlan(
                TopDown3DRockLandmarkArchetype.SpireComplex,
                8675309,
                29,
                36f,
                14f,
                0.68f,
                0.34f);
            var seeds = new HashSet<int>();

            Assert.That(first.Count, Is.EqualTo(29));
            Assert.That(repeat.Count, Is.EqualTo(first.Count));
            Assert.That(first.Count(member => member.Role
                == TopDown3DRockLandmarkFormationRole.Crown), Is.EqualTo(3));
            Assert.That(first.Count(member => member.Role
                == TopDown3DRockLandmarkFormationRole.Shoulder), Is.EqualTo(7));
            Assert.That(first.Count(member => member.Role
                == TopDown3DRockLandmarkFormationRole.Outlier), Is.EqualTo(3));
            Assert.That(first.Any(member => member.Archetype
                == TopDown3DRockFormationArchetype.ScatteredRocks), Is.True);
            Assert.That(first.Any(member => member.Archetype
                == TopDown3DRockFormationArchetype.PileOfRocks), Is.True);

            for (var index = 0; index < first.Count; index++)
            {
                Assert.That(repeat[index].Index, Is.EqualTo(first[index].Index));
                Assert.That(repeat[index].Role, Is.EqualTo(first[index].Role));
                Assert.That(repeat[index].Archetype, Is.EqualTo(first[index].Archetype));
                Assert.That(repeat[index].LocalPosition, Is.EqualTo(first[index].LocalPosition));
                Assert.That(repeat[index].LocalRotation, Is.EqualTo(first[index].LocalRotation));
                Assert.That(repeat[index].Width, Is.EqualTo(first[index].Width));
                Assert.That(repeat[index].Height, Is.EqualTo(first[index].Height));
                Assert.That(repeat[index].Seed, Is.EqualTo(first[index].Seed));
                Assert.That(seeds.Add(first[index].Seed), Is.True);
                Assert.That(first[index].Archetype, Is.Not.EqualTo(
                    TopDown3DRockFormationArchetype.ConnectedOutcrop));
            }

            var crowns = first.Where(member => member.Role
                == TopDown3DRockLandmarkFormationRole.Crown).ToArray();
            var aprons = first.Where(member => member.Role
                == TopDown3DRockLandmarkFormationRole.Apron).ToArray();
            var shoulders = first.Where(member => member.Role
                == TopDown3DRockLandmarkFormationRole.Shoulder).ToArray();
            var outliers = first.Where(member => member.Role
                == TopDown3DRockLandmarkFormationRole.Outlier).ToArray();
            Assert.That(crowns.All(member => member.Archetype
                == TopDown3DRockFormationArchetype.PileOfRocks), Is.True);
            Assert.That(crowns.Min(member => member.Height),
                Is.GreaterThan(aprons.Max(member => member.Height)));
            Assert.That(outliers.Min(member => HorizontalRadius(member.LocalPosition)),
                Is.GreaterThan(shoulders.Max(member => HorizontalRadius(member.LocalPosition))));
            Assert.That(aprons.Any(member => member.LocalPosition.x < 0f), Is.True);
            Assert.That(aprons.Any(member => member.LocalPosition.x > 0f), Is.True);
            Assert.That(aprons.Any(member => member.LocalPosition.z < 0f), Is.True);
            Assert.That(aprons.Any(member => member.LocalPosition.z > 0f), Is.True);
        }

        [Test]
        public void LandmarkSharedFieldDoesNotReplaceChildGeometrySeeds()
        {
            var landmarkObject = new GameObject("Shared Geological Field Landmark");
            var formationObject = new GameObject("Child Formation");
            try
            {
                var landmark = landmarkObject.AddComponent<TopDown3DRockLandmarkAuthoring>();
                landmark.Configure(null, 123456);
                formationObject.transform.SetParent(landmarkObject.transform, false);
                var formation = formationObject.AddComponent<
                    TopDown3DRockWorkbenchFormationAuthoring>();
                formation.Configure(null, 654321);

                Assert.That(formation.FormationSeed, Is.EqualTo(654321));
                Assert.That(formation.SurfaceFieldSeed, Is.EqualTo(123456));
                Assert.That(formation.SurfaceFieldOrigin, Is.EqualTo(landmarkObject.transform.position));

                landmark.ConfigureCaptured(null, 123456, 20f, 8f);
                Assert.That(landmark.ShareGeologicalField, Is.False);
                Assert.That(formation.FormationSeed, Is.EqualTo(654321));
                Assert.That(formation.SurfaceFieldSeed, Is.EqualTo(654321));
                Assert.That(formation.SurfaceFieldOrigin, Is.EqualTo(formationObject.transform.position));
            }
            finally
            {
                Object.DestroyImmediate(landmarkObject);
            }
        }

        [Test]
        public void CaptureSelectedFormationsPreservesTheirWorldPosesAndChildren()
        {
            var firstObject = new GameObject("First Formation");
            var secondObject = new GameObject("Second Formation");
            TopDown3DRockLandmarkAuthoring landmark = null;
            try
            {
                firstObject.transform.SetPositionAndRotation(
                    new Vector3(-7f, 2f, 4f),
                    Quaternion.Euler(0f, 23f, 0f));
                secondObject.transform.SetPositionAndRotation(
                    new Vector3(9f, 3f, -6f),
                    Quaternion.Euler(0f, 141f, 0f));
                var first = firstObject.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                var second = secondObject.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                var firstPosition = firstObject.transform.position;
                var firstRotation = firstObject.transform.rotation;
                var secondPosition = secondObject.transform.position;
                var secondRotation = secondObject.transform.rotation;

                landmark = TopDown3DRockLandmarkEditor.CreateLandmarkFromFormations(
                    new[] { first, second });

                Assert.That(landmark, Is.Not.Null);
                Assert.That(landmark.ShareGeologicalField, Is.False);
                Assert.That(firstObject.transform.parent, Is.EqualTo(landmark.transform));
                Assert.That(secondObject.transform.parent, Is.EqualTo(landmark.transform));
                Assert.That(firstObject.transform.position, Is.EqualTo(firstPosition));
                Assert.That(firstObject.transform.rotation, Is.EqualTo(firstRotation));
                Assert.That(secondObject.transform.position, Is.EqualTo(secondPosition));
                Assert.That(secondObject.transform.rotation, Is.EqualTo(secondRotation));
            }
            finally
            {
                if (landmark != null) Object.DestroyImmediate(landmark.gameObject);
                else
                {
                    Object.DestroyImmediate(firstObject);
                    Object.DestroyImmediate(secondObject);
                }
            }
        }

        [Test]
        public void LandmarkGenerationBuildsEditableChildFormations()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath);
            var root = new GameObject("Generated Rock Landmark Integration Test");
            try
            {
                Assert.That(material, Is.Not.Null);
                var landmark = root.AddComponent<TopDown3DRockLandmarkAuthoring>();
                landmark.Configure(material, 24681357);
                var serialized = new SerializedObject(landmark);
                serialized.FindProperty("generatedWidth").floatValue = 12f;
                serialized.FindProperty("generatedHeight").floatValue = 4f;
                serialized.FindProperty("conformToTerrain").boolValue = false;
                serialized.FindProperty("memberVoxelSize").floatValue = 0.18f;
                serialized.FindProperty("memberSurfaceRelaxation").floatValue = 0f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                TopDown3DRockLandmarkGenerator.GenerateIntoLandmark(
                    landmark,
                    landmark.LandmarkSeed);

                var formations = root.GetComponentsInChildren<
                    TopDown3DRockWorkbenchFormationAuthoring>(true);
                Assert.That(formations.Length, Is.EqualTo(12));
                Assert.That(formations.Select(item => item.FormationSeed).Distinct().Count(),
                    Is.EqualTo(formations.Length));
                Assert.That(formations.All(item => item.SurfaceFieldSeed
                    == landmark.LandmarkSeed), Is.True);
                Assert.That(formations.All(item => item.GetComponentsInChildren<
                    TopDown3DRockWorkbenchAuthoring>(true).Length >= 5), Is.True);
                Assert.That(formations.Any(item => item.FormationArchetype
                    == TopDown3DRockFormationArchetype.ScatteredRocks), Is.True);
                Assert.That(formations.Any(item => item.FormationArchetype
                    == TopDown3DRockFormationArchetype.PileOfRocks), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static float HorizontalRadius(Vector3 position)
        {
            return new Vector2(position.x, position.z).magnitude;
        }
    }
}
