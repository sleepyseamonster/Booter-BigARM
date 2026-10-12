using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainCameraCoverageTests
    {
        [Test]
        public void LongDownwardFrustumKeepsEveryGroundRayWithoutFarClipInflation()
        {
            var owner = new GameObject("Downward frustum fixture");
            try
            {
                var camera = owner.AddComponent<Camera>(); camera.aspect = 16f / 9f; camera.fieldOfView = 60;
                camera.nearClipPlane = .1f; camera.farClipPlane = 8000;
                camera.transform.position = new Vector3(-1858, 20, -1511); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
                Assert.That(FixedTerrainCameraCoverage.TryHeightBounds(Corners(camera), -100, 1700, out var bounds), Is.True);
                Assert.That(bounds.size.x, Is.LessThan(250)); Assert.That(bounds.size.z, Is.LessThan(140));
                foreach (float height in new[] { -100f, 0f, 19f })
                    for (int y = 0; y <= 10; y++) for (int x = 0; x <= 10; x++)
                    {
                        var ray = camera.ViewportPointToRay(new Vector3(x / 10f, y / 10f, 0));
                        var plane = new Plane(Vector3.up, new Vector3(0, height, 0));
                        Assert.That(plane.Raycast(ray, out var distance), Is.True);
                        Assert.That(FixedTerrainRenderWindow.IntersectsXZ(bounds, new Bounds(ray.GetPoint(distance), Vector3.zero)), Is.True);
                    }
                Assert.That(camera.farClipPlane, Is.EqualTo(8000));
            }
            finally { Object.DestroyImmediate(owner); }
        }
        [Test]
        public void HorizonAndOrthographicViewsRetainDistantElevatedGround()
        {
            var owner = new GameObject("Horizon frustum fixture");
            try
            {
                var camera = owner.AddComponent<Camera>(); camera.aspect = 16f / 9f; camera.fieldOfView = 60;
                camera.nearClipPlane = .1f; camera.farClipPlane = 8000; camera.transform.position = new Vector3(0, 10, 0);
                Assert.That(FixedTerrainCameraCoverage.TryHeightBounds(Corners(camera), -100, 1700, out var bounds), Is.True);
                foreach (var point in new[] { new Vector3(0, 1000, 6000), new Vector3(1000, 1500, 7000), new Vector3(-1000, 500, 7000) })
                { var viewport = camera.WorldToViewportPoint(point); Assert.That(viewport.x, Is.InRange(0, 1)); Assert.That(viewport.y, Is.InRange(0, 1)); Assert.That(bounds.Contains(point), Is.True); }
                var orthoOwner = new GameObject("Orthographic fixture"); orthoOwner.transform.SetParent(owner.transform, false);
                var orthographic = orthoOwner.AddComponent<Camera>(); orthographic.aspect = 16f / 9f;
                orthographic.orthographic = true; orthographic.orthographicSize = 64; orthographic.nearClipPlane = .1f; orthographic.farClipPlane = 8000;
                camera = orthographic; camera.transform.position = new Vector3(0, 20, 0); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
                // Camera projection getters refresh Unity's lazily cached matrices in a non-rendering EditMode fixture.
                var projection = camera.projectionMatrix;
                Assert.That(FixedTerrainCameraCoverage.TryHeightBounds(Corners(camera), -100, 1700, out bounds), Is.True);
                Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(128 * camera.aspect));
                for (int y = 0; y <= 10; y++) for (int x = 0; x <= 10; x++)
                {
                    var ray = camera.ViewportPointToRay(new Vector3(x / 10f, y / 10f, 0));
                    Assert.That(new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var distance), Is.True);
                    Assert.That(FixedTerrainRenderWindow.IntersectsXZ(bounds, new Bounds(ray.GetPoint(distance), Vector3.zero)), Is.True);
                }
            }
            finally { Object.DestroyImmediate(owner); }
        }
        [Test]
        public void SlabEdgeIntersectionsAndInvalidInputsAreExplicit()
        {
            var box = new[] { new Vector3(-1, 10, -1), new Vector3(-1, 10, 1), new Vector3(1, 10, 1), new Vector3(1, 10, -1),
                new Vector3(-10, -10, -10), new Vector3(-10, -10, 10), new Vector3(10, -10, 10), new Vector3(10, -10, -10) };
            Assert.That(FixedTerrainCameraCoverage.TryHeightBounds(box, -1, 1, out var bounds), Is.True, "No original corner is inside this thin slab.");
            Assert.That(bounds.min.y, Is.EqualTo(-1)); Assert.That(bounds.max.y, Is.EqualTo(1));
            Assert.That(FixedTerrainCameraCoverage.TryHeightBounds(box, 20, 30, out _), Is.False);
            Assert.That(FixedTerrainCameraCoverage.TryHeightBounds(box, 2, 1, out _), Is.False);
            box[0] = new Vector3(float.NaN, 0, 0);
            Assert.That(FixedTerrainCameraCoverage.TryHeightBounds(box, -1, 1, out _), Is.False);
        }
        [Test]
        public void NativeVegetationUsesUnclippedFallback()
        {
            var scene = EditorSceneManager.NewPreviewScene(); var data = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 100, 256) };
            var texture = new Texture2D(1, 1);
            try
            {
                data.detailPrototypes = new[] { new DetailPrototype { prototypeTexture = texture, renderMode = DetailRenderMode.GrassBillboard, minHeight = 1, maxHeight = 2, minWidth = 1, maxWidth = 2 } };
                var tile = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>(); SceneManager.MoveGameObjectToScene(tile.gameObject, scene);
                using var window = new FixedTerrainRenderWindow(new[] { tile }); Assert.That(window.CanClipCameraHeight, Is.False);
                Assert.That(tile.GetComponent<TerrainCollider>().enabled, Is.True);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); Object.DestroyImmediate(data); Object.DestroyImmediate(texture); }
        }
        private static Vector3[] Corners(Camera camera)
        {
            var result = new Vector3[8];
            FixedTerrainCameraCoverage.FillFrustum(camera, camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, result);
            return result;
        }
    }
}
