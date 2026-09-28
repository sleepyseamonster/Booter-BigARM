using System;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DLandscapeGroundLookTests
    {
        [Test]
        public void LandscapeSandSampler_IsDeterministicWithoutFormationInput()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);

            Assert.That(settings, Is.Not.Null);
            var generator = new TopDown3DWorldGenerator(settings);
            var position = new Vector2(3.25f, -7.5f);
            var first = TopDown3DLandscapeSandPreview.SampleTerrainAt(settings, generator, position);
            var second = TopDown3DLandscapeSandPreview.SampleTerrainAt(settings, generator, position);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void LandscapeClutterPreview_RespectsDisabledProductionDensities()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.TerrainMaterialPath);
            var root = new GameObject("Landscape Ground Look Test");

            try
            {
                Assert.That(settings, Is.Not.Null);
                Assert.That(settings.NaturalObjectCatalog, Is.Not.Null);
                Assert.That(settings.DarkRockMaterial, Is.Not.Null);

                var sandbox = root.AddComponent<TopDown3DLandscapeAuthoringSandbox>();
                sandbox.Configure(settings, terrainMaterial, settings.DarkRockMaterial, Vector2Int.zero);
                var serialized = new SerializedObject(sandbox);
                serialized.FindProperty("terrainRadiusInChunks").intValue = 0;
                serialized.FindProperty("landscapeClutter").floatValue = 1f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                TopDown3DLandscapeAuthoringSandboxEditor.BuildTerrainContext(sandbox);

                var context = root.transform.Find("__Generated Terrain Context");
                var ground = context.GetComponentsInChildren<MeshCollider>();
                Physics.SyncTransforms();
                var automaticPreview = context.Find("Natural Landscape Clutter");
                Assert.That(automaticPreview, Is.Null);
                var firstCount = TopDown3DLandscapeGroundClutterPreview.Apply(
                    sandbox,
                    context,
                    ground,
                    Array.Empty<TopDown3DRockWorkbenchAuthoring>());
                var secondCount = TopDown3DLandscapeGroundClutterPreview.Apply(
                    sandbox,
                    context,
                    ground,
                    Array.Empty<TopDown3DRockWorkbenchAuthoring>());
                Assert.That(firstCount, Is.Zero);
                Assert.That(secondCount, Is.EqualTo(firstCount));
                Assert.That(context.Find("Natural Landscape Clutter"), Is.Null);
            }
            finally
            {
                TopDown3DLandscapeAuthoringSandboxEditor.ClearTerrainContext(
                    root.GetComponent<TopDown3DLandscapeAuthoringSandbox>());
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LandscapeGroundControls_AreIndependentlyBounded()
        {
            var root = new GameObject("Landscape Ground Control Test");
            try
            {
                var sandbox = root.AddComponent<TopDown3DLandscapeAuthoringSandbox>();
                var serialized = new SerializedObject(sandbox);
                serialized.FindProperty("groundClutter").floatValue = 0.25f;
                serialized.FindProperty("landscapeSand").floatValue = 0.6f;
                serialized.FindProperty("landscapeSandRelief").floatValue = 0.34f;
                serialized.FindProperty("landscapeClutter").floatValue = 0.8f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(sandbox.GroundClutter, Is.EqualTo(0.25f));
                Assert.That(sandbox.LandscapeSand, Is.EqualTo(0.6f));
                Assert.That(sandbox.LandscapeSandRelief, Is.EqualTo(0.34f));
                Assert.That(sandbox.LandscapeClutter, Is.EqualTo(0.8f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FormationWorkbench_UsesSeparateLandscapeAuthorityForRaisedSand()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var landscapeObject = new GameObject("Landscape Authoring Sandbox");
            var formationObject = new GameObject("Mixed Formation Ground");
            SceneManager.MoveGameObjectToScene(landscapeObject, scene);
            SceneManager.MoveGameObjectToScene(formationObject, scene);

            try
            {
                var landscape = landscapeObject.AddComponent<TopDown3DLandscapeAuthoringSandbox>();
                var formation = formationObject.AddComponent<TopDown3DLandscapeAuthoringSandbox>();
                var rockReference = AssetDatabase.LoadAssetAtPath<GameObject>(
                    TopDown3DLandscapeAuthoringSandboxEditor.MixedReferencePath);
                formation.ConfigureRockReference(rockReference);

                Assert.That(
                    TopDown3DLandscapeAuthoringSandboxEditor.ResolveLandscapeAuthority(formation),
                    Is.SameAs(landscape));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(formationObject);
                UnityEngine.Object.DestroyImmediate(landscapeObject);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
