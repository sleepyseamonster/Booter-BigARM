using System;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainRenderWindowTests
    {
        [Test]
        public void DistantEditorPreviewCamerasDoNotUndoGameRenderingWindow()
        {
            Assert.That(GreaterWastelandTerrainRendering.ContributesCoverage(CameraType.Game), Is.True);
            Assert.That(GreaterWastelandTerrainRendering.ContributesCoverage(CameraType.Reflection), Is.True);
            Assert.That(GreaterWastelandTerrainRendering.ContributesCoverage(CameraType.SceneView), Is.False);
            Assert.That(GreaterWastelandTerrainRendering.ContributesCoverage(CameraType.Preview), Is.False);
        }
        [Test]
        public void WindowPreservesCollisionNeighboursDataAndOriginalFlags()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 100, 256) };
            FixedTerrainRenderWindow window = null;
            try
            {
                var a = Create(scene, data, -256); var b = Create(scene, data, 0); var c = Create(scene, data, 256);
                a.SetNeighbors(null, null, b, null); b.SetNeighbors(a, null, c, null);
                c.enabled = false;
                var heights = data.GetHeights(0, 0, 33, 33);
                window = new FixedTerrainRenderWindow(new[] { c, b, a });
                window.Update(new[] { Area(-128) }, 0);
                Assert.That(a.enabled, Is.True); Assert.That(b.enabled, Is.False); Assert.That(c.enabled, Is.False);
                Assert.That(b.GetComponent<TerrainCollider>().enabled, Is.True);
                Assert.That(b.gameObject.activeSelf, Is.True);
                Assert.That(b.terrainData, Is.SameAs(data));
                Assert.That(a.rightNeighbor, Is.SameAs(b));
                window.Update(new[] { Area(128) }, 0);
                Assert.That(a.enabled, Is.False); Assert.That(b.enabled, Is.True);
                window.Update(new[] { Area(384) }, 0);
                Assert.That(c.enabled, Is.False, "Authored disabled tiles are not activated by visibility.");
                window.Dispose();
                Assert.That(a.enabled, Is.True); Assert.That(b.enabled, Is.True); Assert.That(c.enabled, Is.False);
                Assert.That(data.GetHeights(0, 0, 33, 33), Is.EqualTo(heights));
                Assert.Throws<ObjectDisposedException>(() => window.Update(new[] { Area(0) }));
            }
            finally { window?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); }
        }

        [Test]
        public void CameraUnionImmediateArrivalsRetentionAndFailOpenAreExplicit()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 100, 256) };
            FixedTerrainRenderWindow window = null;
            try
            {
                var a = Create(scene, data, -256); var b = Create(scene, data, 0); var c = Create(scene, data, 256);
                window = new FixedTerrainRenderWindow(new[] { a, b, c });
                window.Update(new[] { Area(-128), Area(384) }, 0);
                Assert.That(a.enabled && c.enabled && !b.enabled, Is.True);
                window.Update(new[] { Area(128) }, 0, 1);
                Assert.That(b.enabled, Is.True, "A camera jump enables arrivals before rendering.");
                Assert.That(window.EnabledCount, Is.EqualTo(2), "Departure work is bounded after initial selection.");
                window.Update(new[] { Area(128) }, 0, 1);
                Assert.That(window.EnabledCount, Is.EqualTo(1));
                window.Update(new[] { Area(-128) }, 0, 0);
                Assert.That(a.enabled && b.enabled, Is.True, "A render callback enables arrivals without multiplying the frame exit budget.");
                window.Update(new[] { Area(384) }, 256);
                Assert.That(b.enabled && c.enabled, Is.True, "Retention prevents boundary oscillation.");
                window.Update(Array.Empty<Bounds>());
                Assert.That(a.enabled && b.enabled && c.enabled, Is.True);
                window.Update(new[] { new Bounds(Vector3.zero, new Vector3(1e30f, 1, 1e30f)) });
                Assert.That(a.enabled && b.enabled && c.enabled, Is.True, "Huge finite camera bounds are clipped to the actual terrain grid.");
                window.Update(new[] { new Bounds(new Vector3(float.NaN, 0, 0), Vector3.one) });
                Assert.That(a.enabled && b.enabled && c.enabled, Is.True);
            }
            finally { window?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); }
        }

        [Test]
        public void LowSunIncludesRemoteCastersAndHorizonFailsOpen()
        {
            var receivers = new Bounds(Vector3.zero, new Vector3(20, 20, 20));
            var landscape = new Bounds(new Vector3(0, 200, 0), new Vector3(10000, 400, 10000));
            var casters = GreaterWastelandTerrainRendering.ShadowCasterBounds(receivers, landscape, new Vector3(1, -.1f, 0));
            Assert.That(casters.min.x, Is.LessThan(-4000));
            Assert.That(FixedTerrainRenderWindow.IntersectsXZ(casters, receivers), Is.True);
            Assert.That(GreaterWastelandTerrainRendering.ShadowCasterBounds(receivers, landscape, Vector3.right), Is.EqualTo(landscape));
        }

        [Test]
        public void ExplicitCameraPreparationIncludesRegisteredLightWithoutSunAssignment()
        {
            if (RenderSettings.sun != null) Assert.Ignore("Run this fixture in an empty scene with no global sun assignment.");
            var scene = EditorSceneManager.NewPreviewScene();
            var low = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 100, 256) };
            var high = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 100, 256) };
            FixedTerrainRenderWindow window = null;
            try
            {
                var heights = new float[33, 33];
                for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = .1f;
                low.SetHeights(0, 0, heights);
                for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = .9f;
                high.SetHeights(0, 0, heights);
                var nearby = Create(scene, low, 0); var remote = Create(scene, high, -512);
                window = new FixedTerrainRenderWindow(new[] { nearby, remote });
                var owner = new GameObject("Rendering fixture"); SceneManager.MoveGameObjectToScene(owner, scene);
                var controller = owner.AddComponent<GreaterWastelandTerrainRendering>();
                typeof(GreaterWastelandTerrainRendering).GetField("window", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(controller, window);
                var cameraOwner = new GameObject("Explicit offscreen fixture"); SceneManager.MoveGameObjectToScene(cameraOwner, scene);
                cameraOwner.SetActive(false);
                var camera = cameraOwner.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4;
                camera.aspect = 1; camera.nearClipPlane = .1f; camera.farClipPlane = 10;
                camera.transform.position = new Vector3(128, 20, 128);
                controller.PrepareCamera(camera);
                Assert.That(nearby.enabled, Is.True); Assert.That(remote.enabled, Is.False);
                var lightOwner = new GameObject("Unassigned shadow light"); SceneManager.MoveGameObjectToScene(lightOwner, scene);
                var light = lightOwner.AddComponent<Light>(); light.type = LightType.Directional; light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.LookRotation(new Vector3(1, -.1f, 0));
                controller.RegisterShadowLight(light); controller.PrepareCamera(camera);
                Assert.That(remote.enabled, Is.True, "Explicit requests must prepare the registered low-sun caster set before culling.");
                window.Update(new[] { Area(128) }, 0);
                Assert.That(remote.enabled, Is.False);
                controller.enabled = false;
                // Normal runtime MonoBehaviours do not receive lifecycle callbacks in EditMode.
                typeof(GreaterWastelandTerrainRendering).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(controller, null);
                Assert.That(nearby.enabled && remote.enabled, Is.True, "Component teardown restores the initial flags.");
            }
            finally
            {
                window?.Dispose(); EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(low); UnityEngine.Object.DestroyImmediate(high);
            }
        }

        private static Bounds Area(float x) => new(new Vector3(x, 0, 128), new Vector3(8, 1, 8));
        private static Terrain Create(Scene scene, TerrainData data, float x)
        {
            var terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
            SceneManager.MoveGameObjectToScene(terrain.gameObject, scene);
            terrain.transform.position = new Vector3(x, 0, 0);
            return terrain;
        }
    }
}
