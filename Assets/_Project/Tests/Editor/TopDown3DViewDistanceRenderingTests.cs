using System.IO;
using System.Collections;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace BooterBigArm.Tests
{
    // Deterministic offscreen rendering fixture, never enters Play Mode or exercises gameplay.
    public sealed class TopDown3DViewDistanceRenderingTests
    {
        [UnityTest]
        public IEnumerator DeepTwilightCutoffMatchesSky() => RenderFixture(0.65f, 0f);

        [UnityTest]
        public IEnumerator BrightTwilightCutoffMatchesSky() => RenderFixture(1.05f, 26f);

        private IEnumerator RenderFixture(float exposure, float pitch)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("A graphics device is required for render evidence.");
            var scene = EditorSceneManager.NewPreviewScene();
            var oldDefault = GraphicsSettings.defaultRenderPipeline;
            var oldQuality = QualitySettings.renderPipeline;
            var oldSky = RenderSettings.skybox;
            var skyMaterial = new Material(Resources.Load<Material>("TopDown3D/MartianPanoramaSky"));
            skyMaterial.SetFloat("_Exposure", exposure);
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            var feature = ScriptableObject.CreateInstance<TopDown3DVolumetricDustFeature>();
            feature.Configure(Shader.Find("BooterBigArm/TopDown3D/Broken World Volumetric Dust"));
            renderer.rendererFeatures.Add(feature);
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            var cameraRoot = new GameObject("Distance render fixture", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraRoot, scene);
            var camera = cameraRoot.GetComponent<Camera>();
            camera.scene = scene; camera.enabled = false; camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.1f;
            // Exercise the production Game-camera path without touching the user's loaded scenes.
            camera.cameraType = CameraType.Game;
            camera.transform.rotation = Quaternion.Euler(pitch, 40f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.2f, 0.1f, 0.05f);
            camera.GetUniversalAdditionalCameraData().requiresDepthTexture = true;
            var range = cameraRoot.AddComponent<BadwaterCameraRange>();
            // Preview scenes park ordinary behaviours; enable only this owner for offscreen EditMode proof.
            range.runInEditMode = true;
            var target = new RenderTexture(256, 144, 24, RenderTextureFormat.ARGBHalf);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", Color.green);
            GameObject surface = null;
            try
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                RenderSettings.skybox = skyMaterial;
                Assert.That(RenderSettings.skybox, Is.Not.Null, "Use the production directional panorama");
                camera.targetTexture = target;
                range.SetPreset("Maximum");
                yield return null;
                var sky = Capture(camera, target, $"sky-{exposure}");
                surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(surface, scene);
                surface.transform.localScale = new Vector3(850f, 450f, 1f);
                surface.GetComponent<MeshRenderer>().sharedMaterial = material;
                surface.transform.rotation = camera.transform.rotation;
                surface.transform.position = camera.transform.forward * 499.9f;
                range.SetPreset("Low");
                Assert.That(range.DistanceHazeEnabled, Is.True, "The fixture must exercise the production haze path");
                yield return null;
                var edge = Capture(camera, target, $"low-edge-{exposure}");
                AssertRgb(sky, edge, 0.02f, "Terrain must converge to the actual sky, including off-axis pixels");
                range.enabled = false;
                yield return null;
                var edgeWithoutFade = Capture(camera, target, "edge-without-fade");
                camera.clearFlags = CameraClearFlags.SolidColor;
                var solidEdge = Capture(camera, target, "edge-solid-control");
                Assert.That(solidEdge[72 * 256 + 128].g, Is.GreaterThan(0.9f), "Geometry must remain inside the low far clip");
                camera.clearFlags = CameraClearFlags.Skybox;
                Assert.That(edgeWithoutFade[72 * 256 + 128].g, Is.GreaterThan(edge[72 * 256 + 128].g + 0.1f),
                    "The edge fixture must contain visible geometry rather than pass through clipping alone");
                range.enabled = true;
                range.SetPreset("Maximum");
                yield return null;
                var unfaded = Capture(camera, target, "maximum-surface");
                Assert.That(unfaded[unfaded.Length / 2].g, Is.GreaterThan(edge[edge.Length / 2].g + 0.1f));
                surface.transform.position = camera.transform.forward * 485f;
                range.SetPreset("Low");
                yield return null;
                var nearEdge = Capture(camera, target, "low-near-edge");
                AssertRgb(sky, nearEdge, 0.05f, "Far geometry with small nonzero depth must still receive the fade");
                surface.transform.position = camera.transform.forward * 437.5f;
                yield return null;
                var midBand = Capture(camera, target, "low-mid-band");
                var expectedMidBand = (Color[])sky.Clone();
                foreach (int x in new[] { 32, 128, 224 })
                {
                    int index = 72 * 256 + x;
                    // Front face depth437: smoothstep(375,500,437)=0.494000128 at every viewing ray.
                    expectedMidBand[index] = Color.Lerp(Color.green, sky[index], 0.494000128f);
                }
                AssertRgb(expectedMidBand, midBand, 0.02f, "Fade must use camera eye depth, not radial distance");
                surface.transform.position = camera.transform.forward * 100f;
                range.SetPreset("Maximum");
                yield return null;
                var nearMaximum = Capture(camera, target, "near-maximum");
                range.SetPreset("Low");
                yield return null;
                var nearLow = Capture(camera, target, "near-low");
                AssertRgb(nearMaximum, nearLow, 0.005f, "Near geometry must retain its rendering");
            }
            finally
            {
                camera.targetTexture = null;
                GraphicsSettings.defaultRenderPipeline = oldDefault;
                QualitySettings.renderPipeline = oldQuality;
                RenderSettings.skybox = oldSky;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(material);
                Object.DestroyImmediate(skyMaterial);
                Object.DestroyImmediate(pipeline); Object.DestroyImmediate(feature); Object.DestroyImmediate(renderer);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static Color[] Capture(Camera camera, RenderTexture target, string label)
        {
            camera.cameraType = CameraType.Game;
            camera.Render();
            var previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBAFloat, false, true);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
                var directory = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "ViewDistanceCaptures");
                Directory.CreateDirectory(directory);
                var pixels = texture.GetPixels();
                var png = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, true);
                try
                {
                    png.SetPixels(pixels); png.Apply();
                    File.WriteAllBytes(Path.Combine(directory, label + ".png"), png.EncodeToPNG());
                }
                finally { Object.DestroyImmediate(png); }
                return pixels;
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(texture); }
        }

        private static void AssertRgb(Color[] expected, Color[] actual, float tolerance, string message)
        {
            // Sample the center and both sides; a radial distance fade would fail the off-axis checks.
            foreach (int x in new[] { 32, 128, 224 })
            {
                int index = 72 * 256 + x;
                Assert.That(Mathf.Abs(expected[index].r - actual[index].r), Is.LessThan(tolerance), message);
                Assert.That(Mathf.Abs(expected[index].g - actual[index].g), Is.LessThan(tolerance), message);
                Assert.That(Mathf.Abs(expected[index].b - actual[index].b), Is.LessThan(tolerance), message);
            }
        }
    }
}
