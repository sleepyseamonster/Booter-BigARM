using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRockWorkbenchPlayModePreviewTests
    {
        private const string WorkbenchMaterialPath =
            "Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat";

        [Test]
        public void RebuildLoadedPreviewsRestoresStandaloneRockAndFusedLandmarkFormation()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath);
            var standaloneRoot = new GameObject("Standalone Rock Workbench");
            var landmarkRoot = new GameObject("Rock Landmark Workbench");
            var generatedMeshes = new List<Mesh>();

            try
            {
                Assert.That(material, Is.Not.Null);
                var standalone = CreateWorkbench(
                    standaloneRoot.transform,
                    "Playable Standalone Rock",
                    Vector3.zero,
                    material);

                landmarkRoot.AddComponent<TopDown3DRockLandmarkAuthoring>();
                var formationObject = new GameObject("Playable Fused Formation");
                formationObject.transform.SetParent(landmarkRoot.transform, false);
                var formation = formationObject.AddComponent<
                    TopDown3DRockWorkbenchFormationAuthoring>();
                formation.Configure(material, 24681357);
                var formationSettings = new SerializedObject(formation);
                formationSettings.FindProperty("fusedVoxelSize").floatValue = 0.3f;
                formationSettings.ApplyModifiedPropertiesWithoutUndo();
                var left = CreateWorkbench(
                    formationObject.transform,
                    "Left Member",
                    new Vector3(-0.45f, 0f, 0f),
                    material);
                var right = CreateWorkbench(
                    formationObject.transform,
                    "Right Member",
                    new Vector3(0.45f, 0.08f, 0.05f),
                    material);

                TopDown3DRockWorkbenchPlayModePreview.RebuildLoadedPreviews();

                AssertPlayableMesh(standalone);
                AssertPlayableMesh(formation);
                Assert.That(left.GeneratedMesh, Is.Null);
                Assert.That(right.GeneratedMesh, Is.Null);
                Assert.That(left.GetComponent<MeshRenderer>().enabled, Is.False);
                Assert.That(right.GetComponent<MeshRenderer>().enabled, Is.False);
                Assert.That(left.GetComponent<MeshCollider>().enabled, Is.False);
                Assert.That(right.GetComponent<MeshCollider>().enabled, Is.False);

                generatedMeshes.Add(standalone.GeneratedMesh);
                generatedMeshes.Add(formation.GeneratedMesh);
            }
            finally
            {
                Object.DestroyImmediate(standaloneRoot);
                Object.DestroyImmediate(landmarkRoot);
                DestroyMeshes(generatedMeshes);
            }
        }

        [Test]
        public void RebuildLoadedPreviewsRestoresSeparateRocksInsideLandmark()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath);
            var landmarkRoot = new GameObject("Scattered Rock Landmark Workbench");
            var generatedMeshes = new List<Mesh>();

            try
            {
                Assert.That(material, Is.Not.Null);
                landmarkRoot.AddComponent<TopDown3DRockLandmarkAuthoring>();
                var formationObject = new GameObject("Playable Scattered Formation");
                formationObject.transform.SetParent(landmarkRoot.transform, false);
                var formation = formationObject.AddComponent<
                    TopDown3DRockWorkbenchFormationAuthoring>();
                formation.Configure(material, 97531);
                var formationSettings = new SerializedObject(formation);
                formationSettings.FindProperty("formationArchetype").enumValueIndex =
                    (int)TopDown3DRockFormationArchetype.ScatteredRocks;
                formationSettings.ApplyModifiedPropertiesWithoutUndo();
                var left = CreateWorkbench(
                    formationObject.transform,
                    "Left Scattered Rock",
                    new Vector3(-1.2f, 0f, 0f),
                    material);
                var right = CreateWorkbench(
                    formationObject.transform,
                    "Right Scattered Rock",
                    new Vector3(1.2f, 0f, 0f),
                    material);

                TopDown3DRockWorkbenchPlayModePreview.RebuildLoadedPreviews();

                Assert.That(formation.GeneratedMesh, Is.Null);
                Assert.That(formation.GetComponent<MeshRenderer>().enabled, Is.False);
                AssertPlayableMesh(left);
                AssertPlayableMesh(right);
                generatedMeshes.Add(left.GeneratedMesh);
                generatedMeshes.Add(right.GeneratedMesh);
            }
            finally
            {
                Object.DestroyImmediate(landmarkRoot);
                DestroyMeshes(generatedMeshes);
            }
        }

        private static TopDown3DRockWorkbenchAuthoring CreateWorkbench(
            Transform parent,
            string name,
            Vector3 localPosition,
            Material material)
        {
            var workbenchObject = new GameObject(name);
            workbenchObject.transform.SetParent(parent, false);
            workbenchObject.transform.localPosition = localPosition;
            var workbench = workbenchObject.AddComponent<TopDown3DRockWorkbenchAuthoring>();
            workbench.Configure(material);
            var settings = new SerializedObject(workbench);
            settings.FindProperty("voxelSize").floatValue = 0.18f;
            settings.ApplyModifiedPropertiesWithoutUndo();

            var volume = new GameObject("Weathered Block Volume");
            volume.transform.SetParent(workbenchObject.transform, false);
            volume.transform.localScale = Vector3.one * 2f;
            volume.AddComponent<TopDown3DRockVolumeNode>();
            return workbench;
        }

        private static void AssertPlayableMesh(TopDown3DRockWorkbenchAuthoring workbench)
        {
            Assert.That(workbench.GeneratedMesh, Is.Not.Null);
            Assert.That(workbench.GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(workbench.GeneratedMesh));
            Assert.That(workbench.GetComponent<MeshRenderer>().enabled, Is.True);
            var collider = workbench.GetComponent<MeshCollider>();
            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.sharedMesh, Is.SameAs(workbench.GeneratedMesh));
        }

        private static void AssertPlayableMesh(
            TopDown3DRockWorkbenchFormationAuthoring formation)
        {
            Assert.That(formation.GeneratedMesh, Is.Not.Null);
            Assert.That(formation.GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(formation.GeneratedMesh));
            Assert.That(formation.GetComponent<MeshRenderer>().enabled, Is.True);
            var collider = formation.GetComponent<MeshCollider>();
            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.sharedMesh, Is.SameAs(formation.GeneratedMesh));
        }

        private static void DestroyMeshes(IReadOnlyList<Mesh> meshes)
        {
            for (var i = 0; i < meshes.Count; i++)
            {
                if (meshes[i] != null) Object.DestroyImmediate(meshes[i]);
            }
        }
    }
}
