using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRockWorkbenchTests
    {
        private static readonly Vector3 GeneratedRockSize = new Vector3(4f, 3f, 3.5f);
        private const string WorkbenchMaterialPath =
            "Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat";
        private const string ProductionRendererPath =
            "Assets/_Project/Settings/Rendering/URP/IsometricRenderer.asset";
        private const string LayeredTextureRoot =
            "Assets/_Project/Art/Environment/Rocks/Workbench/Layered/";
        private const string SideAlbedoPath = LayeredTextureRoot + "RockWorkbenchSide_Albedo.png";
        private const string SideNormalPath = LayeredTextureRoot + "RockWorkbenchSide_Normal.png";
        private const string SideSurfacePath = LayeredTextureRoot + "RockWorkbenchSide_Surface.png";
        private const string TopAlbedoPath = LayeredTextureRoot + "RockWorkbenchTop_Albedo.png";
        private const string TopNormalPath = LayeredTextureRoot + "RockWorkbenchTop_Normal.png";
        private const string TopSurfacePath = LayeredTextureRoot + "RockWorkbenchTop_Surface.png";
        private const string CrackMaskPath = LayeredTextureRoot + "RockWorkbenchCrack_Mask.png";
        private const string GritAlbedoPath = LayeredTextureRoot + "RockWorkbenchGrit_Albedo.png";
        private const string GritNormalPath = LayeredTextureRoot + "RockWorkbenchGrit_Normal.png";
        private const string GritSurfacePath = LayeredTextureRoot + "RockWorkbenchGrit_Surface.png";
        private const string UndersideAlbedoPath = LayeredTextureRoot + "RockWorkbenchUnderside_Albedo.png";
        private const string UndersideNormalPath = LayeredTextureRoot + "RockWorkbenchUnderside_Normal.png";
        private const string UndersideSurfacePath = LayeredTextureRoot + "RockWorkbenchUnderside_Surface.png";

        [Test]
        public void WorkbenchMaterialUsesLayeredTopSideAndCrackPbrMaps()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath);

            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader, Is.Not.Null);
            Assert.That(
                material.shader.name,
                Is.EqualTo("BooterBigArm/TopDown3D/Broken World Rock Workbench PBR"));
            Assert.That(material.shader.isSupported, Is.True);
            Assert.That(ShaderUtil.GetShaderMessages(material.shader), Is.Empty);
            Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_NormalMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_SurfaceMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_TopBaseMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_TopNormalMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_TopSurfaceMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_CrackMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_GritBaseMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_GritNormalMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_GritSurfaceMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_BottomBaseMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_BottomNormalMap"), Is.Not.Null);
            Assert.That(material.GetTexture("_BottomSurfaceMap"), Is.Not.Null);
            Assert.That(material.GetFloat("_SurfacePatchStrength"), Is.GreaterThan(0f));
            Assert.That(material.GetFloat("_WornSmoothnessBoost"), Is.GreaterThan(0f));
            Assert.That(material.GetFloat("_CrackAmount"), Is.GreaterThan(0f));
            Assert.That(material.GetFloat("_SideGritAmount"), Is.GreaterThan(0f));
            Assert.That(material.GetFloat("_UndersideShaleAmount"), Is.GreaterThan(0f));
            Assert.That(material.GetFloat("_SideShalePatchAmount"), Is.GreaterThan(0f));
            Assert.That(material.GetFloat("_TopShalePatchAmount"), Is.GreaterThan(0f));
            Assert.That(material.HasProperty("_RockOriginWS"), Is.True);
            Assert.That(material.HasProperty("_FormationFractureAmount"), Is.True);
            Assert.That(material.HasProperty("_FormationFractureSpacing"), Is.True);
            Assert.That(material.HasProperty("_GeologicalSeamAmount"), Is.True);
        }

        [Test]
        public void FormationControlsClampToSafeAuthoringRanges()
        {
            var root = new GameObject("Rock Formation Control Test");
            try
            {
                var formation = root.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                var serialized = new SerializedObject(formation);
                serialized.FindProperty("generatedOverallSize").floatValue = 99f;
                serialized.FindProperty("generatedComplexity").floatValue = 4f;
                serialized.FindProperty("generatedVerticality").floatValue = -2f;
                serialized.FindProperty("longFractures").floatValue = 4f;
                serialized.FindProperty("fractureSpacing").floatValue = 99f;
                serialized.FindProperty("fusedVoxelSize").floatValue = -2f;
                serialized.FindProperty("fusedJoinSoftness").floatValue = 7f;
                serialized.FindProperty("geologicalSeamWidth").floatValue = -3f;
                serialized.FindProperty("geologicalSeamStrength").floatValue = 4f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(formation.GeneratedOverallSize, Is.EqualTo(30f));
                Assert.That(formation.GeneratedComplexity, Is.EqualTo(1f));
                Assert.That(formation.GeneratedVerticality, Is.EqualTo(0f));
                Assert.That(formation.GeneratedRockCount, Is.EqualTo(14));
                Assert.That(formation.LongFractures, Is.EqualTo(1f));
                Assert.That(formation.FractureSpacing, Is.EqualTo(16f));
                Assert.That(formation.FusedVoxelSize, Is.EqualTo(0.04f));
                Assert.That(formation.FusedJoinSoftness, Is.EqualTo(0.5f));
                Assert.That(formation.GeologicalSeamWidth, Is.EqualTo(0.08f));
                Assert.That(formation.GeologicalSeamStrength, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FormationPlanIsRepeatableAndUsesDistinctMemberSeeds()
        {
            var first = TopDown3DRockWorkbenchFormationGenerator.CreatePlan(
                24681357,
                7,
                14f,
                0.65f,
                0.55f);
            var repeat = TopDown3DRockWorkbenchFormationGenerator.CreatePlan(
                24681357,
                7,
                14f,
                0.65f,
                0.55f);
            var seeds = new HashSet<int>();

            Assert.That(first.Count, Is.EqualTo(7));
            Assert.That(repeat.Count, Is.EqualTo(first.Count));
            for (var index = 0; index < first.Count; index++)
            {
                Assert.That(repeat[index].LocalPosition, Is.EqualTo(first[index].LocalPosition));
                Assert.That(repeat[index].LocalRotation, Is.EqualTo(first[index].LocalRotation));
                Assert.That(repeat[index].LocalScale, Is.EqualTo(first[index].LocalScale));
                Assert.That(repeat[index].RockSize, Is.EqualTo(first[index].RockSize));
                Assert.That(repeat[index].Seed, Is.EqualTo(first[index].Seed));
                Assert.That(repeat[index].Role, Is.EqualTo(first[index].Role));
                Assert.That(seeds.Add(first[index].Seed), Is.True);
                Assert.That(first[index].RockSize, Is.InRange(0.75f, 10f));
                Assert.That(first[index].LocalScale.x, Is.GreaterThan(0f));
                Assert.That(first[index].LocalScale.y, Is.GreaterThan(0f));
                Assert.That(first[index].LocalScale.z, Is.GreaterThan(0f));
            }
        }

        [TestCase(97531)]
        [TestCase(24680)]
        [TestCase(13579)]
        public void FormationPlanUsesVolumetricRolesAndSurroundsTheCore(int seed)
        {
            var plan = TopDown3DRockWorkbenchFormationGenerator.CreatePlan(
                seed,
                14,
                24f,
                0.85f,
                0.72f);
            var roles = plan.Select(member => member.Role).ToHashSet();

            Assert.That(plan.Count, Is.EqualTo(14));
            Assert.That(plan[0].Role, Is.EqualTo(TopDown3DRockFormationMemberRole.Core));
            Assert.That(roles, Does.Contain(TopDown3DRockFormationMemberRole.Pillar));
            Assert.That(roles, Does.Contain(TopDown3DRockFormationMemberRole.Buttress));
            Assert.That(roles, Does.Contain(TopDown3DRockFormationMemberRole.Crown));
            Assert.That(roles, Does.Contain(TopDown3DRockFormationMemberRole.Talus));
            Assert.That(
                plan.Skip(1).Any(member =>
                    Mathf.Abs(member.LocalScale.x - member.LocalScale.y) > 0.2f
                    || Mathf.Abs(member.LocalScale.z - member.LocalScale.y) > 0.2f),
                Is.True);

            var directions = plan
                .Skip(1)
                .Where(member => member.Role != TopDown3DRockFormationMemberRole.Crown)
                .Select(member => Mathf.Atan2(member.LocalPosition.z, member.LocalPosition.x))
                .OrderBy(angle => angle)
                .ToArray();
            var maximumGap = 0f;
            for (var index = 0; index < directions.Length; index++)
            {
                var next = index + 1 < directions.Length
                    ? directions[index + 1]
                    : directions[0] + Mathf.PI * 2f;
                maximumGap = Mathf.Max(maximumGap, next - directions[index]);
            }

            Assert.That(maximumGap, Is.LessThan(Mathf.PI));
            Assert.That(plan.Any(member => member.LocalPosition.x < -0.1f), Is.True);
            Assert.That(plan.Any(member => member.LocalPosition.x > 0.1f), Is.True);
            Assert.That(plan.Any(member => member.LocalPosition.z < -0.1f), Is.True);
            Assert.That(plan.Any(member => member.LocalPosition.z > 0.1f), Is.True);
        }

        [Test]
        public void OneClickFormationGenerationCreatesEditableRockMembers()
        {
            var previousSelection = Selection.activeObject;
            var root = new GameObject("One Click Formation Test");
            try
            {
                var formation = root.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                formation.Configure(
                    AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath),
                    424242);
                var expectedCount = formation.GeneratedRockCount;

                TopDown3DRockWorkbenchFormationGenerator.GenerateIntoFormation(
                    formation,
                    424242);

                var members = root.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
                Assert.That(members.Length, Is.EqualTo(expectedCount));
                Assert.That(Selection.activeGameObject, Is.EqualTo(root));
                foreach (var member in members)
                {
                    Assert.That(
                        member.GetComponentsInChildren<TopDown3DRockVolumeNode>(true).Length,
                        Is.EqualTo(member.GeneratedCubeCount));
                    Assert.That(member.ShowSourceVolumes, Is.False);
                }
                Assert.That(members.Any(member => member.name.StartsWith("Core Rock")), Is.True);
                Assert.That(members.Any(member => member.name.StartsWith("Pillar Rock")), Is.True);
                Assert.That(members.Any(member => member.name.StartsWith("Buttress Rock")), Is.True);
                Assert.That(members.Any(member => member.name.StartsWith("Crown Rock")), Is.True);
                Assert.That(members.Any(member => member.name.StartsWith("Talus Rock")), Is.True);
            }
            finally
            {
                Selection.activeObject = previousSelection;
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(10101, 6f, 0.1f, 0.15f)]
        [TestCase(20202, 10f, 0.6f, 0.68f)]
        [TestCase(30303, 16f, 0.4f, 0.9f)]
        [TestCase(40404, 24f, 0.85f, 0.72f)]
        [TestCase(50505, 30f, 1f, 1f)]
        [TestCase(606060, 10f, 0.6f, 0.68f)]
        [TestCase(70707, 18f, 0.75f, 0.2f)]
        [TestCase(80808, 8f, 0.95f, 0.5f)]
        public void GeneratedVolumetricFormationBuildsOneGeologicalShell(
            int seed,
            float overallSize,
            float complexity,
            float verticality)
        {
            var previousSelection = Selection.activeObject;
            var root = new GameObject("Generated Volumetric Formation Test");
            try
            {
                var formation = root.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                formation.Configure(
                    AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath),
                    seed);
                var serialized = new SerializedObject(formation);
                serialized.FindProperty("generatedOverallSize").floatValue = overallSize;
                serialized.FindProperty("generatedComplexity").floatValue = complexity;
                serialized.FindProperty("generatedVerticality").floatValue = verticality;
                serialized.FindProperty("fusedVoxelSize").floatValue = 0.28f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                TopDown3DRockWorkbenchFormationGenerator.GenerateIntoFormation(
                    formation,
                    seed);

                Assert.That(
                    TopDown3DRockWorkbenchFormationPreview.TryBuildNow(formation, out var error),
                    Is.True,
                    error);
                Assert.That(formation.PreviewStatus, Does.StartWith("ONE GEOLOGICAL SHELL"));
                Assert.That(formation.GeneratedMesh, Is.Not.Null);
                Assert.That(formation.GeneratedMesh.colors32.Length,
                    Is.EqualTo(formation.GeneratedMesh.vertexCount));
            }
            finally
            {
                Selection.activeObject = previousSelection;
                var formation = root.GetComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                if (formation != null && formation.GeneratedMesh != null)
                    Object.DestroyImmediate(formation.GeneratedMesh);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SmoothFormationPreviewCombinesSeparateWorkbenchesIntoOneSurface()
        {
            var root = new GameObject("Rock Formation Preview Test");
            try
            {
                var formation = root.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                formation.Configure(
                    AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath),
                    424242);
                var serialized = new SerializedObject(formation);
                serialized.FindProperty("joinStyle").enumValueIndex =
                    (int)TopDown3DRockFormationJoinStyle.SmoothFusedPreview;
                serialized.FindProperty("fusedVoxelSize").floatValue = 0.22f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var left = CreateFormationMember(
                    root.transform,
                    "Left Rock",
                    new Vector3(-0.45f, 0f, 0f));
                left.transform.localScale = new Vector3(1.35f, 1.8f, 0.85f);
                var right = CreateFormationMember(
                    root.transform,
                    "Right Rock",
                    new Vector3(0.45f, 0.08f, 0.05f));
                right.transform.localScale = new Vector3(0.9f, 1.25f, 1.15f);
                right.transform.localRotation = Quaternion.Euler(0f, 18f, 5f);

                Assert.That(
                    TopDown3DRockWorkbenchFormationPreview.TryBuildNow(formation, out var error),
                    Is.True,
                    error);
                Assert.That(formation.GeneratedMesh, Is.Not.Null);
                Assert.That(formation.GeneratedMesh.vertexCount, Is.GreaterThan(0));
                Assert.That(formation.PreviewStatus, Does.StartWith("ONE FUSED SURFACE"));
                Assert.That(root.GetComponent<MeshRenderer>().enabled, Is.True);
                Assert.That(
                    root.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>()
                        .All(member => !member.GetComponent<MeshRenderer>().enabled),
                    Is.True);
            }
            finally
            {
                var formation = root.GetComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                if (formation != null && formation.GeneratedMesh != null)
                    Object.DestroyImmediate(formation.GeneratedMesh);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GeologicalFormationPreviewBuildsOneExteriorShellWithDeterministicSeams()
        {
            var root = new GameObject("Geological Formation Preview Test");
            try
            {
                var formation = root.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                formation.Configure(
                    AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath),
                    424242);
                var serialized = new SerializedObject(formation);
                serialized.FindProperty("joinStyle").enumValueIndex =
                    (int)TopDown3DRockFormationJoinStyle.FusedGeologicalSeams;
                serialized.FindProperty("fusedVoxelSize").floatValue = 0.18f;
                serialized.FindProperty("geologicalSeamWidth").floatValue = 0.5f;
                serialized.FindProperty("geologicalSeamStrength").floatValue = 0.9f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var left = CreateFormationMember(
                    root.transform,
                    "Left Rock",
                    new Vector3(-0.45f, 0f, 0f));
                left.transform.localScale = new Vector3(1.25f, 1.7f, 0.9f);
                var right = CreateFormationMember(
                    root.transform,
                    "Right Rock",
                    new Vector3(0.45f, 0.08f, 0.05f));
                right.transform.localScale = new Vector3(0.95f, 1.3f, 1.1f);
                right.transform.localRotation = Quaternion.Euler(0f, 16f, 4f);

                Assert.That(
                    TopDown3DRockWorkbenchFormationPreview.TryBuildNow(formation, out var error),
                    Is.True,
                    error);
                Assert.That(formation.GeneratedMesh, Is.Not.Null);
                Assert.That(formation.PreviewStatus, Does.StartWith("ONE GEOLOGICAL SHELL"));
                var firstColors = formation.GeneratedMesh.colors32;
                Assert.That(firstColors.Length, Is.EqualTo(formation.GeneratedMesh.vertexCount));
                Assert.That(firstColors.Any(color => color.r > 0), Is.True);
                Assert.That(firstColors.Any(color => color.r == 0), Is.True);

                var renderer = root.GetComponent<MeshRenderer>();
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(
                    properties.GetFloat(Shader.PropertyToID("_GeologicalSeamAmount")),
                    Is.EqualTo(0.9f).Within(0.0001f));
                Assert.That(
                    root.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>()
                        .All(member => !member.GetComponent<MeshRenderer>().enabled),
                    Is.True);

                Assert.That(
                    TopDown3DRockWorkbenchFormationPreview.TryBuildNow(formation, out error),
                    Is.True,
                    error);
                CollectionAssert.AreEqual(firstColors, formation.GeneratedMesh.colors32);
            }
            finally
            {
                var formation = root.GetComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                if (formation != null && formation.GeneratedMesh != null)
                    Object.DestroyImmediate(formation.GeneratedMesh);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProductionRendererUsesRestrainedRockContactOcclusion()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ProductionRendererPath);
            Assert.That(renderer, Is.Not.Null);
            var features = renderer.rendererFeatures
                .OfType<ScreenSpaceAmbientOcclusion>()
                .ToArray();
            Assert.That(features.Length, Is.EqualTo(1));
            Assert.That(features[0].isActive, Is.True);

            var serialized = new SerializedObject(features[0]);
            var settings = serialized.FindProperty("m_Settings");
            Assert.That(settings, Is.Not.Null);
            Assert.That(
                settings.FindPropertyRelative("Intensity").floatValue,
                Is.InRange(1f, 1.5f));
            Assert.That(
                settings.FindPropertyRelative("Radius").floatValue,
                Is.InRange(0.08f, 0.16f));
            Assert.That(settings.FindPropertyRelative("Downsample").boolValue, Is.True);
        }

        [Test]
        public void WorkbenchLayeredTexturesUsePbrImportSettings()
        {
            foreach (var path in new[] { SideAlbedoPath, TopAlbedoPath, GritAlbedoPath, UndersideAlbedoPath })
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.sRGBTexture, Is.True, path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat), path);
                Assert.That(importer.streamingMipmaps, Is.True, path);
            }

            foreach (var path in new[] { SideNormalPath, TopNormalPath, GritNormalPath, UndersideNormalPath })
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.NormalMap), path);
                Assert.That(importer.sRGBTexture, Is.False, path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat), path);
            }

            foreach (var path in new[]
                     {
                         SideSurfacePath,
                         TopSurfacePath,
                         CrackMaskPath,
                         GritSurfacePath,
                         UndersideSurfacePath
                     })
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.sRGBTexture, Is.False, path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat), path);
            }
        }

        [Test]
        public void WorkbenchSurfaceSeedMappingIsRepeatableAndDistinct()
        {
            var first = TopDown3DRockWorkbenchPreview.SeedToUnitFloat(24681357);
            var repeat = TopDown3DRockWorkbenchPreview.SeedToUnitFloat(24681357);
            var variation = TopDown3DRockWorkbenchPreview.SeedToUnitFloat(97531);

            Assert.That(first, Is.InRange(0f, 1f));
            Assert.That(repeat, Is.EqualTo(first));
            Assert.That(variation, Is.Not.EqualTo(first));
        }

        [Test]
        public void WorkbenchSurfaceLanguageControlsClampToAuthoredRanges()
        {
            var root = new GameObject("Rock Surface Language Test");
            try
            {
                var authoring = root.AddComponent<TopDown3DRockWorkbenchAuthoring>();
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("geologyScale").floatValue = 99f;
                serialized.FindProperty("surfaceVariation").floatValue = -3f;
                serialized.FindProperty("crackAmount").floatValue = 7f;
                serialized.FindProperty("sideGrit").floatValue = 3f;
                serialized.FindProperty("undersideShale").floatValue = -4f;
                serialized.FindProperty("sideShalePatches").floatValue = 6f;
                serialized.FindProperty("topShalePatches").floatValue = 8f;
                serialized.FindProperty("wornShine").floatValue = 4f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(authoring.GeologyScale, Is.EqualTo(3f));
                Assert.That(authoring.SurfaceVariation, Is.EqualTo(0f));
                Assert.That(authoring.CrackAmount, Is.EqualTo(1f));
                Assert.That(authoring.SideGrit, Is.EqualTo(1f));
                Assert.That(authoring.UndersideShale, Is.EqualTo(0f));
                Assert.That(authoring.SideShalePatches, Is.EqualTo(1f));
                Assert.That(authoring.TopShalePatches, Is.EqualTo(1f));
                Assert.That(authoring.WornShine, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BaseRockPlanRepeatsExactlyForTheSameSeed()
        {
            var first = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                24681357,
                6,
                GeneratedRockSize,
                0.55f,
                0.7f,
                0.65f);
            var second = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                24681357,
                6,
                GeneratedRockSize,
                0.55f,
                0.7f,
                0.65f);

            Assert.That(first.Count, Is.EqualTo(6));
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (var index = 0; index < first.Count; index++)
            {
                Assert.That(second[index].LocalPosition, Is.EqualTo(first[index].LocalPosition));
                Assert.That(second[index].LocalRotation, Is.EqualTo(first[index].LocalRotation));
                Assert.That(second[index].LocalScale, Is.EqualTo(first[index].LocalScale));
                Assert.That(second[index].Role, Is.EqualTo(first[index].Role));
            }
        }

        [Test]
        public void BaseRockPlanChangesWhenTheSeedChanges()
        {
            var first = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                13579,
                5,
                GeneratedRockSize,
                0.45f,
                0.65f,
                0.62f);
            var second = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                97531,
                5,
                GeneratedRockSize,
                0.45f,
                0.65f,
                0.62f);

            var differs = false;
            for (var index = 0; index < first.Count; index++)
            {
                if (first[index].LocalPosition != second[index].LocalPosition
                    || first[index].LocalRotation != second[index].LocalRotation
                    || first[index].LocalScale != second[index].LocalScale)
                {
                    differs = true;
                    break;
                }
            }
            Assert.That(differs, Is.True);
        }

        [Test]
        public void OverallSizeUniformlyScalesTheEnvelopeAndAutomaticCubeBudget()
        {
            var root = new GameObject("Overall Size Control Test");
            try
            {
                var authoring = root.AddComponent<TopDown3DRockWorkbenchAuthoring>();
                var serialized = new SerializedObject(authoring);
                var overallSize = serialized.FindProperty("generatedOverallScale");

                overallSize.floatValue = 1f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(authoring.GeneratedOverallSize, Is.EqualTo(Vector3.one));
                Assert.That(authoring.GeneratedCubeCount, Is.EqualTo(2));

                overallSize.floatValue = 4f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(authoring.GeneratedOverallSize, Is.EqualTo(Vector3.one * 4f));
                Assert.That(authoring.GeneratedCubeCount, Is.EqualTo(5));

                overallSize.floatValue = 10f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(authoring.GeneratedOverallSize, Is.EqualTo(Vector3.one * 10f));
                Assert.That(authoring.GeneratedCubeCount, Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void VariationGallerySeedsAreRepeatableAndDistinct()
        {
            const int baseSeed = 123456789;
            var seeds = new HashSet<int>();
            for (var index = 0;
                 index < TopDown3DRockWorkbenchVariationGallery.VariationCount;
                 index++)
            {
                var first = TopDown3DRockWorkbenchVariationGallery.DeriveSeed(baseSeed, index);
                var second = TopDown3DRockWorkbenchVariationGallery.DeriveSeed(baseSeed, index);
                Assert.That(second, Is.EqualTo(first));
                Assert.That(seeds.Add(first), Is.True, $"Duplicate gallery seed at index {index}");
            }

            Assert.That(
                TopDown3DRockWorkbenchVariationGallery.DeriveSeed(baseSeed, 0),
                Is.EqualTo(baseSeed));
        }

        [Test]
        public void VariationGalleryIsTemporarySeededAndDetachableForEditing()
        {
            var previousSelection = Selection.activeObject;
            var sourceObject = new GameObject("Gallery Source Workbench");
            TopDown3DRockWorkbenchAuthoring detached = null;
            try
            {
                TopDown3DRockWorkbenchVariationGallery.ClearGallery();
                var source = sourceObject.AddComponent<TopDown3DRockWorkbenchAuthoring>();
                source.Configure(AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath));
                const int baseSeed = 99887766;

                var gallery = TopDown3DRockWorkbenchVariationGallery.CreateOrReplaceGallery(
                    source,
                    baseSeed);

                Assert.That(gallery, Is.Not.Null);
                Assert.That(
                    gallery.hideFlags & HideFlags.DontSaveInEditor,
                    Is.EqualTo(HideFlags.DontSaveInEditor));
                Assert.That(
                    gallery.transform.childCount,
                    Is.EqualTo(TopDown3DRockWorkbenchVariationGallery.VariationCount));
                Assert.That(Selection.activeGameObject, Is.EqualTo(sourceObject));
                var positions = new HashSet<Vector3>();
                for (var index = 0; index < gallery.transform.childCount; index++)
                {
                    var item = gallery.transform.GetChild(index)
                        .GetComponent<TopDown3DRockWorkbenchAuthoring>();
                    Assert.That(item, Is.Not.Null);
                    Assert.That(
                        item.GenerationSeed,
                        Is.EqualTo(TopDown3DRockWorkbenchVariationGallery.DeriveSeed(baseSeed, index)));
                    Assert.That(item.AutoRebuild, Is.False);
                    Assert.That(item.ShowSourceVolumes, Is.False);
                    Assert.That(item.UpdateCollider, Is.False);
                    Assert.That(
                        item.gameObject.hideFlags & HideFlags.DontSaveInEditor,
                        Is.EqualTo(HideFlags.DontSaveInEditor));
                    Assert.That(positions.Add(item.transform.localPosition), Is.True);
                    Assert.That(
                        item.GetComponentsInChildren<TopDown3DRockVolumeNode>(true).Length,
                        Is.EqualTo(item.GeneratedCubeCount));
                    Assert.That(TopDown3DRockWorkbenchVariationGallery.IsGalleryItem(item), Is.True);
                }

                detached = gallery.transform.GetChild(0)
                    .GetComponent<TopDown3DRockWorkbenchAuthoring>();
                TopDown3DRockWorkbenchVariationGallery.DetachForEditing(detached);

                Assert.That(detached.transform.parent, Is.Null);
                Assert.That(detached.AutoRebuild, Is.True);
                Assert.That(detached.ShowSourceVolumes, Is.True);
                Assert.That(detached.UpdateCollider, Is.True);
                Assert.That(
                    detached.gameObject.hideFlags & HideFlags.DontSaveInEditor,
                    Is.EqualTo(HideFlags.None));
                Assert.That(TopDown3DRockWorkbenchVariationGallery.IsGalleryItem(detached), Is.False);
                Assert.That(gallery.transform.childCount, Is.EqualTo(
                    TopDown3DRockWorkbenchVariationGallery.VariationCount - 1));

                TopDown3DRockWorkbenchVariationGallery.ClearGallery();
                Assert.That(TopDown3DRockWorkbenchVariationGallery.FindGalleryRoot(), Is.Null);
            }
            finally
            {
                TopDown3DRockWorkbenchVariationGallery.ClearGallery();
                if (detached != null) Object.DestroyImmediate(detached.gameObject);
                Selection.activeObject = previousSelection;
                Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void VerticalityCreatesAVisiblyTallerSilhouette()
        {
            var horizontal = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                424242,
                7,
                GeneratedRockSize,
                0f,
                0.65f,
                0.62f);
            var vertical = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                424242,
                7,
                GeneratedRockSize,
                1f,
                0.65f,
                0.62f);

            var horizontalBounds = TopDown3DRockWorkbenchBaseRockGenerator.CalculateBounds(horizontal);
            var verticalBounds = TopDown3DRockWorkbenchBaseRockGenerator.CalculateBounds(vertical);
            var horizontalRatio = horizontalBounds.size.y
                / Mathf.Max(horizontalBounds.size.x, horizontalBounds.size.z);
            var verticalRatio = verticalBounds.size.y
                / Mathf.Max(verticalBounds.size.x, verticalBounds.size.z);

            Assert.That(verticalRatio, Is.GreaterThan(horizontalRatio * 1.35f));
            Assert.That(verticalBounds.size.y, Is.GreaterThan(horizontalBounds.size.y));
        }

        [Test]
        public void LopsidednessCreatesAVisiblyOneSidedMassDistribution()
        {
            var balanced = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                606060,
                10,
                Vector3.one * 8f,
                0.45f,
                0f,
                0.5f);
            var lopsided = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                606060,
                10,
                Vector3.one * 8f,
                0.45f,
                1f,
                0.5f);

            var balancedOffset = NormalizedHorizontalMassOffset(balanced);
            var lopsidedOffset = NormalizedHorizontalMassOffset(lopsided);

            Assert.That(lopsidedOffset, Is.GreaterThan(balancedOffset * 1.8f));
        }

        [Test]
        public void CompactionCreatesAVisiblyDenserMassArrangement()
        {
            var lobed = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                707070,
                9,
                Vector3.one * 8f,
                0.5f,
                0.65f,
                0f);
            var compact = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                707070,
                9,
                Vector3.one * 8f,
                0.5f,
                0.65f,
                1f);

            var lobedSpacing = NormalizedNearestMassSpacing(lobed);
            var compactSpacing = NormalizedNearestMassSpacing(compact);

            Assert.That(compactSpacing, Is.LessThan(lobedSpacing * 0.62f));
        }

        [Test]
        public void GeneratedPlansStayGroundedInsideTheirMaximumEnvelope()
        {
            foreach (var seed in new[] { 17, 1729, 8675309 })
            {
                foreach (var verticality in new[] { 0f, 0.5f, 1f })
                {
                    var plan = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                        seed,
                        8,
                        GeneratedRockSize,
                        verticality,
                        0.72f,
                        0.58f);
                    var bounds = TopDown3DRockWorkbenchBaseRockGenerator.CalculateBounds(plan);

                    Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.001f),
                        $"Seed {seed}, verticality {verticality}");
                    Assert.That(bounds.size.x, Is.LessThanOrEqualTo(GeneratedRockSize.x + 0.001f),
                        $"Seed {seed}, verticality {verticality}");
                    Assert.That(bounds.size.y, Is.LessThanOrEqualTo(GeneratedRockSize.y + 0.001f),
                        $"Seed {seed}, verticality {verticality}");
                    Assert.That(bounds.size.z, Is.LessThanOrEqualTo(GeneratedRockSize.z + 0.001f),
                        $"Seed {seed}, verticality {verticality}");
                }
            }
        }

        [Test]
        public void GeneratedPlanUsesDominantCoreSupportsAndSmallerDetails()
        {
            var plan = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                314159,
                8,
                GeneratedRockSize,
                0.55f,
                0.8f,
                0.62f);

            Assert.That(plan[0].Role, Is.EqualTo(TopDown3DRockWorkbenchMassRole.Core));
            var supportCount = 0;
            var detailCount = 0;
            var largestSupport = 0f;
            var largestDetail = 0f;
            for (var index = 1; index < plan.Count; index++)
            {
                var volume = Volume(plan[index].LocalScale);
                if (plan[index].Role == TopDown3DRockWorkbenchMassRole.Support)
                {
                    supportCount++;
                    largestSupport = Mathf.Max(largestSupport, volume);
                }
                else if (plan[index].Role == TopDown3DRockWorkbenchMassRole.Detail)
                {
                    detailCount++;
                    largestDetail = Mathf.Max(largestDetail, volume);
                }
            }

            Assert.That(supportCount, Is.GreaterThan(0));
            Assert.That(detailCount, Is.GreaterThan(0));
            Assert.That(Volume(plan[0].LocalScale), Is.GreaterThan(largestSupport));
            Assert.That(largestSupport, Is.GreaterThan(largestDetail));
        }

        [Test]
        public void GenerateIntoWorkbenchReplacesSourcesWithEditableSeededVolumes()
        {
            var previousSelection = Selection.activeObject;
            var root = new GameObject("Generated Base Rock Integration Test");
            try
            {
                var authoring = root.AddComponent<TopDown3DRockWorkbenchAuthoring>();
                var oldVolume = new GameObject("Old Cube Volume");
                oldVolume.transform.SetParent(root.transform, false);
                oldVolume.AddComponent<TopDown3DRockVolumeNode>();

                TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(authoring, 8675309);

                var generated = root.GetComponentsInChildren<TopDown3DRockVolumeNode>(true);
                Assert.That(authoring.GenerationSeed, Is.EqualTo(8675309));
                Assert.That(generated.Length, Is.EqualTo(authoring.GeneratedCubeCount));
                var shapeSeeds = new HashSet<int>();
                for (var index = 0; index < generated.Length; index++)
                {
                    var node = generated[index];
                    Assert.That(node, Is.Not.Null);
                    Assert.That(node.transform.parent, Is.EqualTo(root.transform));
                    Assert.That(node.name, Does.StartWith("Cube Volume "));
                    Assert.That(
                        node.ShapeSeed,
                        Is.EqualTo(TopDown3DRockWorkbenchBaseRockGenerator.DeriveVolumeShapeSeed(8675309, index)));
                    shapeSeeds.Add(node.ShapeSeed);
                }
                Assert.That(shapeSeeds.Count, Is.EqualTo(generated.Length));
            }
            finally
            {
                Selection.activeObject = previousSelection;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GeneratedBaseRockSeedsBuildOneClosedConnectedSurface()
        {
            var root = new GameObject("Generated Base Rock Test");
            try
            {
                foreach (var seed in new[] { 101, 202, 303 })
                {
                    foreach (var verticality in new[] { 0f, 0.5f, 1f })
                    {
                        foreach (var controls in new[]
                                 {
                                     new Vector2(0f, 0f),
                                     new Vector2(0f, 1f),
                                     new Vector2(1f, 0f),
                                     new Vector2(1f, 1f)
                                 })
                        {
                            var lopsidedness = controls.x;
                            var compaction = controls.y;
                            while (root.transform.childCount > 0)
                            {
                                Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                            }

                            var plan = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                                seed,
                                7,
                                GeneratedRockSize,
                                verticality,
                                lopsidedness,
                                compaction);
                            var boxes = new List<TopDown3DRockWorkbenchBox>(plan.Count);
                            for (var index = 0; index < plan.Count; index++)
                            {
                                var spec = plan[index];
                                var box = CreateBoxObject(root.transform, spec.LocalPosition, spec.LocalScale);
                                box.transform.localRotation = spec.LocalRotation;
                                boxes.Add(new TopDown3DRockWorkbenchBox(
                                    root.transform,
                                    box.transform,
                                    TopDown3DRockWorkbenchBaseRockGenerator.DeriveVolumeShapeSeed(seed, index)));
                            }

                            Assert.That(
                                TopDown3DRockWorkbenchMesher.TryBuild(
                                    boxes,
                                    0.2f,
                                    0.16f,
                                    out var result,
                                    out var error),
                                Is.True,
                                $"Seed {seed}, verticality {verticality}, lopsidedness {lopsidedness}, compaction {compaction}: {error}");
                            Assert.That(result.Topology.IsValid, Is.True,
                                $"Seed {seed}, verticality {verticality}, lopsidedness {lopsidedness}, compaction {compaction}: {result.Topology.Error}");
                            Assert.That(result.ConnectedComponents, Is.EqualTo(1),
                                $"Seed {seed}, verticality {verticality}, lopsidedness {lopsidedness}, compaction {compaction}");
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OverlappingBoxesBuildOneClosedConnectedSurface()
        {
            var root = new GameObject("Rock Workbench Test");
            try
            {
                var boxes = new List<TopDown3DRockWorkbenchBox>
                {
                    CreateBox(root.transform, Vector3.zero, new Vector3(2f, 2f, 2f)),
                    CreateBox(root.transform, new Vector3(1.1f, 0.25f, 0f), new Vector3(2f, 1.5f, 1.7f))
                };

                Assert.That(
                    TopDown3DRockWorkbenchMesher.TryBuild(boxes, 0.2f, 0.14f, out var result, out var error),
                    Is.True,
                    error);
                Assert.That(result.Topology.IsValid, Is.True, result.Topology.Error);
                Assert.That(result.Topology.SignedVolume, Is.GreaterThan(0d));
                Assert.That(result.ConnectedComponents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SeparatedBoxesReportTwoClosedSurfaceComponents()
        {
            var root = new GameObject("Rock Workbench Test");
            try
            {
                var boxes = new List<TopDown3DRockWorkbenchBox>
                {
                    CreateBox(root.transform, new Vector3(-2f, 0f, 0f), Vector3.one * 1.5f),
                    CreateBox(root.transform, new Vector3(2f, 0f, 0f), Vector3.one * 1.5f)
                };

                Assert.That(
                    TopDown3DRockWorkbenchMesher.TryBuild(boxes, 0.2f, 0.08f, out var result, out var error),
                    Is.True,
                    error);
                Assert.That(result.Topology.IsValid, Is.True, result.Topology.Error);
                Assert.That(result.ConnectedComponents, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RotatedTouchingBoxesSmoothIntoOneSurface()
        {
            var root = new GameObject("Rock Workbench Test");
            try
            {
                var first = CreateBoxObject(root.transform, Vector3.zero, new Vector3(2f, 2f, 2f));
                var second = CreateBoxObject(root.transform, new Vector3(1.8f, 0.15f, 0f), new Vector3(2f, 1.4f, 1.8f));
                second.transform.localRotation = Quaternion.Euler(0f, 18f, 8f);
                var boxes = new List<TopDown3DRockWorkbenchBox>
                {
                    new TopDown3DRockWorkbenchBox(root.transform, first.transform),
                    new TopDown3DRockWorkbenchBox(root.transform, second.transform)
                };

                Assert.That(
                    TopDown3DRockWorkbenchMesher.TryBuild(boxes, 0.18f, 0.2f, out var result, out var error),
                    Is.True,
                    error);
                Assert.That(result.Topology.IsValid, Is.True, result.Topology.Error);
                Assert.That(result.ConnectedComponents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FaceTouchingBoxesSmoothIntoOneSurface()
        {
            var root = new GameObject("Rock Workbench Test");
            try
            {
                var boxes = new List<TopDown3DRockWorkbenchBox>
                {
                    CreateBox(root.transform, Vector3.zero, Vector3.one * 2f),
                    CreateBox(root.transform, new Vector3(2f, 0f, 0f), Vector3.one * 2f)
                };

                Assert.That(
                    TopDown3DRockWorkbenchMesher.TryBuild(boxes, 0.18f, 0.16f, out var result, out var error),
                    Is.True,
                    error);
                Assert.That(result.Topology.IsValid, Is.True, result.Topology.Error);
                Assert.That(result.ConnectedComponents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RockMassPrimitivePreservesFaceContactAndRemovesBoxCorners()
        {
            var root = new GameObject("Rock Workbench Primitive Test");
            try
            {
                var source = CreateBoxObject(root.transform, Vector3.zero, Vector3.one);
                var rockMass = new TopDown3DRockWorkbenchBox(root.transform, source.transform, 24680);

                Assert.That(rockMass.Evaluate(Vector3.zero), Is.LessThan(-0.25f));
                foreach (var faceCenter in new[]
                         {
                             new Vector3(-0.5f, 0f, 0f),
                             new Vector3(0.5f, 0f, 0f),
                             new Vector3(0f, -0.5f, 0f),
                             new Vector3(0f, 0.5f, 0f),
                             new Vector3(0f, 0f, -0.5f),
                             new Vector3(0f, 0f, 0.5f)
                         })
                {
                    Assert.That(Mathf.Abs(rockMass.Evaluate(faceCenter)), Is.LessThan(0.0001f));
                }

                Assert.That(rockMass.Evaluate(new Vector3(0.5f, 0.5f, 0.5f)), Is.GreaterThan(0.04f));
                Assert.That(rockMass.Evaluate(new Vector3(0.5f, 0.5f, 0f)), Is.GreaterThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RockMassShapeSeedIsRepeatableAndChangesTheSurface()
        {
            var root = new GameObject("Rock Workbench Shape Seed Test");
            try
            {
                var source = CreateBoxObject(root.transform, Vector3.zero, Vector3.one);
                var first = new TopDown3DRockWorkbenchBox(root.transform, source.transform, 13579);
                var repeat = new TopDown3DRockWorkbenchBox(root.transform, source.transform, 13579);
                var variation = new TopDown3DRockWorkbenchBox(root.transform, source.transform, 97531);
                var totalVariation = 0f;

                foreach (var point in new[]
                         {
                             new Vector3(0.44f, 0.44f, 0.44f),
                             new Vector3(-0.44f, 0.44f, 0.44f),
                             new Vector3(0.44f, -0.44f, 0.44f),
                             new Vector3(0.44f, 0.44f, -0.44f),
                             new Vector3(-0.44f, -0.44f, -0.44f),
                             new Vector3(0.42f, 0.3f, 0.2f),
                             new Vector3(-0.3f, 0.42f, -0.25f)
                         })
                {
                    Assert.That(first.Evaluate(point), Is.EqualTo(repeat.Evaluate(point)).Within(0.000001f));
                    totalVariation += Mathf.Abs(first.Evaluate(point) - variation.Evaluate(point));
                }

                Assert.That(totalVariation, Is.GreaterThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static TopDown3DRockWorkbenchBox CreateBox(
            Transform root,
            Vector3 position,
            Vector3 scale)
        {
            return new TopDown3DRockWorkbenchBox(root, CreateBoxObject(root, position, scale).transform);
        }

        private static TopDown3DRockWorkbenchAuthoring CreateFormationMember(
            Transform formationRoot,
            string name,
            Vector3 localPosition)
        {
            var memberObject = new GameObject(name);
            memberObject.transform.SetParent(formationRoot, false);
            memberObject.transform.localPosition = localPosition;
            var member = memberObject.AddComponent<TopDown3DRockWorkbenchAuthoring>();
            member.Configure(AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath));

            var volume = new GameObject("Cube Volume");
            volume.transform.SetParent(memberObject.transform, false);
            volume.transform.localScale = Vector3.one * 2f;
            volume.AddComponent<TopDown3DRockVolumeNode>();
            return member;
        }

        private static GameObject CreateBoxObject(Transform root, Vector3 position, Vector3 scale)
        {
            var box = new GameObject("Box");
            box.transform.SetParent(root, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            return box;
        }

        private static float Volume(Vector3 scale)
        {
            return scale.x * scale.y * scale.z;
        }

        private static float NormalizedHorizontalMassOffset(
            IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> plan)
        {
            var centroid = Vector3.zero;
            for (var index = 1; index < plan.Count; index++)
            {
                centroid += plan[index].LocalPosition;
            }
            centroid /= plan.Count - 1;

            var offset = centroid - plan[0].LocalPosition;
            offset.y = 0f;
            var bounds = TopDown3DRockWorkbenchBaseRockGenerator.CalculateBounds(plan);
            return offset.magnitude / Mathf.Max(bounds.size.x, bounds.size.z);
        }

        private static float NormalizedNearestMassSpacing(
            IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> plan)
        {
            var total = 0f;
            for (var index = 1; index < plan.Count; index++)
            {
                var nearest = float.PositiveInfinity;
                for (var candidate = 0; candidate < index; candidate++)
                {
                    var distance = Vector3.Distance(
                        plan[index].LocalPosition,
                        plan[candidate].LocalPosition);
                    var scale = (plan[index].LocalScale.magnitude
                        + plan[candidate].LocalScale.magnitude) * 0.5f;
                    nearest = Mathf.Min(nearest, distance / Mathf.Max(scale, 0.001f));
                }
                total += nearest;
            }
            return total / (plan.Count - 1);
        }
    }
}
