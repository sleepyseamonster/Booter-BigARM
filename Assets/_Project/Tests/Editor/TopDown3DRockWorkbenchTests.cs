using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRockWorkbenchTests
    {
        private static readonly Vector3 GeneratedRockSize = new Vector3(4f, 3f, 3.5f);
        private const string WorkbenchMaterialPath =
            "Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat";
        private const string WorkbenchNormalPath =
            "Assets/_Project/Art/Environment/Rocks/Workbench/RockWorkbenchNeutral_Normal.png";
        private const string WorkbenchSurfacePath =
            "Assets/_Project/Art/Environment/Rocks/Workbench/RockWorkbenchNeutral_Surface.png";
        private const string WorkbenchAlbedoPath =
            "Assets/_Project/Art/Environment/Rocks/Workbench/RockWorkbenchNeutral_Albedo.png";

        [Test]
        public void WorkbenchMaterialUsesDedicatedTriplanarPbrSurfaceMaps()
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
        }

        [Test]
        public void WorkbenchTexturesUsePbrColorSpaceAndNormalImportSettings()
        {
            var albedo = AssetImporter.GetAtPath(WorkbenchAlbedoPath) as TextureImporter;
            var normal = AssetImporter.GetAtPath(WorkbenchNormalPath) as TextureImporter;
            var surface = AssetImporter.GetAtPath(WorkbenchSurfacePath) as TextureImporter;

            Assert.That(albedo, Is.Not.Null);
            Assert.That(normal, Is.Not.Null);
            Assert.That(surface, Is.Not.Null);
            Assert.That(albedo.sRGBTexture, Is.True);
            Assert.That(normal.textureType, Is.EqualTo(TextureImporterType.NormalMap));
            Assert.That(normal.sRGBTexture, Is.False);
            Assert.That(surface.sRGBTexture, Is.False);
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
                foreach (var node in generated)
                {
                    Assert.That(node, Is.Not.Null);
                    Assert.That(node.transform.parent, Is.EqualTo(root.transform));
                    Assert.That(node.name, Does.StartWith("Cube Volume "));
                }
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
                foreach (var seed in new[] { 101, 202, 303, 404 })
                {
                    while (root.transform.childCount > 0)
                    {
                        Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                    }

                    var plan = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                        seed,
                        5,
                        GeneratedRockSize,
                        0.45f,
                        0.65f,
                        0.62f);
                    var boxes = new List<TopDown3DRockWorkbenchBox>(plan.Count);
                    foreach (var spec in plan)
                    {
                        var box = CreateBoxObject(root.transform, spec.LocalPosition, spec.LocalScale);
                        box.transform.localRotation = spec.LocalRotation;
                        boxes.Add(new TopDown3DRockWorkbenchBox(root.transform, box.transform));
                    }

                    Assert.That(
                        TopDown3DRockWorkbenchMesher.TryBuild(
                            boxes,
                            0.2f,
                            0.16f,
                            out var result,
                            out var error),
                        Is.True,
                        $"Seed {seed}: {error}");
                    Assert.That(result.Topology.IsValid, Is.True, $"Seed {seed}: {result.Topology.Error}");
                    Assert.That(result.ConnectedComponents, Is.EqualTo(1), $"Seed {seed}");
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

        private static TopDown3DRockWorkbenchBox CreateBox(
            Transform root,
            Vector3 position,
            Vector3 scale)
        {
            return new TopDown3DRockWorkbenchBox(root, CreateBoxObject(root, position, scale).transform);
        }

        private static GameObject CreateBoxObject(Transform root, Vector3 position, Vector3 scale)
        {
            var box = new GameObject("Box");
            box.transform.SetParent(root, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            return box;
        }
    }
}
