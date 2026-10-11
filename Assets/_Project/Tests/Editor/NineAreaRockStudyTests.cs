using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class NineAreaRockStudyTests
    {
        [Test]
        public void SavedStudyHasOneTerrainAndEditableVolcanicCubes()
        {
            var scene = EditorSceneManager.OpenPreviewScene(NineAreaRockStudy.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                var terrains = roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).ToArray();
                Assert.That(terrains.Length, Is.EqualTo(1));
                Assert.That(terrains[0].terrainData, Is.Not.Null);
                Assert.That(terrains[0].materialTemplate,
                    Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>(NineAreaRockStudy.SourceMaterial)));
                Assert.That(terrains[0].terrainData.terrainLayers,
                    Is.EqualTo(new[] { AssetDatabase.LoadAssetAtPath<TerrainLayer>(NineAreaRockStudy.Folder + "/StudyBase.terrainlayer") }));
                var cubes = roots.SelectMany(r => r.GetComponentsInChildren<SmoothRockCube>(true)).ToArray();
                Assert.That(cubes.Length, Is.GreaterThan(0));
                var volcanic = AssetDatabase.LoadAssetAtPath<Material>(SmoothRockCubePreview.MaterialPath);
                Assert.That(volcanic, Is.Not.Null);
                foreach (var cube in cubes)
                {
                    Assert.That(cube.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
                    Assert.That(cube.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(volcanic));
                }
                // Author-owned poses, paint, heights and comparison rocks may continue to evolve.
                foreach (var transform in roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero);
                Assert.That(roots.Any(r => r.name == "Smooth cube rock exterior"), Is.False,
                    "The generated preview must not be serialized into the study.");
                Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == scene.path), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        [Test]
        public void SharedStudyLayerSamplesOnlyAPadSizedRegionAtNativeScale()
        {
            var source = AssetDatabase.LoadAssetAtPath<TerrainLayer>(NineAreaRockStudy.SourceLayer);
            var study = AssetDatabase.LoadAssetAtPath<TerrainLayer>(NineAreaRockStudy.Folder + "/StudyBase.terrainlayer");
            Assert.That(study, Is.Not.SameAs(source)); Assert.That(study.diffuseTexture, Is.SameAs(source.diffuseTexture));
            Assert.That(study.tileSize, Is.EqualTo(source.tileSize));
            Assert.That(study.tileOffset, Is.EqualTo(source.tileOffset + Vector2.one * ((256f - NineAreaRockStudy.AreaSize) / 2f)));
        }
        [Test]
        public void StudySunAndEnvironmentMatchTheSavedWorldLightingReference()
        {
            var reference = EditorSceneManager.OpenPreviewScene(NineAreaLightingAndGround.LightingReference);
            var study = EditorSceneManager.OpenPreviewScene(NineAreaRockStudy.ScenePath);
            try
            {
                var sourceSun = reference.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PerpetualTwilightSun>()).Single();
                var targetSun = study.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PerpetualTwilightSun>()).Single();
                Assert.That(targetSun.Cycle01, Is.EqualTo(sourceSun.Cycle01));
                Assert.That(targetSun.CycleDurationSeconds, Is.EqualTo(sourceSun.CycleDurationSeconds));
                Assert.That(targetSun.CycleEnabled, Is.EqualTo(sourceSun.CycleEnabled));
                var source = sourceSun.GetComponent<Light>(); var target = targetSun.GetComponent<Light>();
                Assert.That(target.intensity, Is.EqualTo(source.intensity).Within(.00001));
                Assert.That(target.color, Is.EqualTo(source.color));
                Assert.That(target.shadowStrength, Is.EqualTo(source.shadowStrength));
                Assert.That(target.shadowBias, Is.EqualTo(source.shadowBias));
                Assert.That(target.shadowNormalBias, Is.EqualTo(source.shadowNormalBias));
                Assert.That(Quaternion.Angle(target.transform.rotation, source.transform.rotation), Is.LessThan(.001f));
                // Verify serialized scene environment values; loaded preview callbacks do not establish persisted settings.
                string renderBlock(string path) => System.Text.RegularExpressions.Regex.Match(System.IO.File.ReadAllText(path),
                    @"(?ms)^--- !u!104.*?(?=^--- !u!|\z)").Value;
                var a = renderBlock(NineAreaLightingAndGround.LightingReference);
                var b = renderBlock(NineAreaRockStudy.ScenePath);
                foreach (string key in new[] { "m_AmbientSkyColor", "m_AmbientEquatorColor", "m_AmbientGroundColor", "m_AmbientMode",
                    "m_SkyboxMaterial", "m_ReflectionIntensity", "m_FogColor", "m_FogDensity", "m_Fog" })
                {
                    string value(string text) => System.Text.RegularExpressions.Regex.Match(text, @"(?m)^  " + key + @":.*$").Value.Trim();
                    Assert.That(value(b), Is.EqualTo(value(a)), key);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(study); EditorSceneManager.ClosePreviewScene(reference); }
        }
    }
}
