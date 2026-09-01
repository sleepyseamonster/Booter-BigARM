using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BooterBigArm.Editor;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRockWorkbenchTests
    {
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
