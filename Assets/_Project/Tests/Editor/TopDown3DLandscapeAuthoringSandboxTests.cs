using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DLandscapeAuthoringSandboxTests
    {
        [Test]
        public void TerrainContextBuildsPlayableGroundFromProductionGenerator()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.TerrainMaterialPath);
            var sandboxObject = new GameObject("Landscape Authoring Sandbox Test");

            try
            {
                Assert.That(settings, Is.Not.Null);
                Assert.That(terrainMaterial, Is.Not.Null);

                var sandbox = sandboxObject.AddComponent<TopDown3DLandscapeAuthoringSandbox>();
                sandbox.Configure(settings, terrainMaterial, null, Vector2Int.zero);
                var serialized = new SerializedObject(sandbox);
                serialized.FindProperty("terrainRadiusInChunks").intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                TopDown3DLandscapeAuthoringSandboxEditor.BuildTerrainContext(sandbox);

                Assert.That(sandbox.transform.childCount, Is.EqualTo(1));
                var contextRoot = sandbox.transform.GetChild(0);
                Assert.That(contextRoot.name, Is.EqualTo("__Generated Terrain Context"));
                Assert.That(contextRoot.childCount, Is.EqualTo(1));

                var chunk = contextRoot.GetChild(0).gameObject;
                var filter = chunk.GetComponent<MeshFilter>();
                var collider = chunk.GetComponent<MeshCollider>();
                Assert.That(filter, Is.Not.Null);
                Assert.That(filter.sharedMesh, Is.Not.Null);
                Assert.That(chunk.GetComponent<MeshRenderer>(), Is.Not.Null);
                Assert.That(chunk.GetComponent<TopDown3DGroundSurface>(), Is.Not.Null);
                Assert.That(collider, Is.Not.Null);
                Assert.That(collider.sharedMesh, Is.SameAs(filter.sharedMesh));
            }
            finally
            {
                var sandbox = sandboxObject.GetComponent<TopDown3DLandscapeAuthoringSandbox>();
                TopDown3DLandscapeAuthoringSandboxEditor.ClearTerrainContext(sandbox);
                Object.DestroyImmediate(sandboxObject);
            }
        }

        [Test]
        public void SavedSandboxContainsWiredPlayerRigAndOneActiveCamera()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(
                    LandscapeAuthoringSandboxBuilder.ScenePath,
                    OpenSceneMode.Single);
                var motors = Object.FindObjectsByType<TopDown3DPlayerMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var inputs = Object.FindObjectsByType<TopDown3DInputRouter>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var cameraRigs = Object.FindObjectsByType<TopDown3DCameraRig>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                Assert.That(motors, Has.Length.EqualTo(1));
                Assert.That(inputs, Has.Length.EqualTo(1));
                Assert.That(cameraRigs, Has.Length.EqualTo(1));
                Assert.That(inputs[0].InputActions, Is.Not.Null);

                var motor = new SerializedObject(motors[0]);
                Assert.That(motor.FindProperty("input").objectReferenceValue, Is.SameAs(inputs[0]));
                Assert.That(
                    motor.FindProperty("cameraBasis").objectReferenceValue,
                    Is.SameAs(cameraRigs[0].transform));

                var cameraRig = new SerializedObject(cameraRigs[0]);
                Assert.That(
                    cameraRig.FindProperty("target").objectReferenceValue,
                    Is.SameAs(motors[0].transform));
                Assert.That(cameraRig.FindProperty("input").objectReferenceValue, Is.SameAs(inputs[0]));

                var activeCameras = Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var activeCameraCount = 0;
                Camera activeCamera = null;
                for (var i = 0; i < activeCameras.Length; i++)
                {
                    var candidate = activeCameras[i];
                    if (candidate.gameObject.scene != scene
                        || !candidate.isActiveAndEnabled)
                    {
                        continue;
                    }

                    activeCameraCount++;
                    activeCamera = candidate;
                }

                Assert.That(activeCameraCount, Is.EqualTo(1));
                Assert.That(activeCamera, Is.SameAs(cameraRigs[0].GetComponent<Camera>()));
                Assert.That(activeCamera.CompareTag("MainCamera"), Is.True);
                Assert.That(activeCamera.GetComponent<AudioListener>(), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
