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
        public void NineFlatAreasHaveTheSameBaseAndExactlyOneEditableRockEach()
        {
            var scene = EditorSceneManager.OpenPreviewScene(NineAreaRockStudy.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects(); var areas = roots.Where(r => r.name.StartsWith("Area ")).OrderBy(r => r.name).ToArray();
                Assert.That(areas.Length, Is.EqualTo(9)); Assert.That(roots.Length, Is.EqualTo(11));
                var material = AssetDatabase.LoadAssetAtPath<Material>(NineAreaRockStudy.SourceMaterial);
                TerrainLayer shared = null;
                for (int i = 0; i < areas.Length; i++)
                {
                    Assert.That(areas[i].transform.childCount, Is.EqualTo(i == 4 ? 3 : 2));
                    if (i == 4)
                    {
                        var basalt = areas[i].transform.Find("SculptedBasalt01");
                        Assert.That(basalt, Is.Not.Null);
                        Assert.That(basalt.localPosition, Is.EqualTo(Vector3.zero));
                        Assert.That(basalt.localScale, Is.EqualTo(Vector3.one));
                        Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(basalt.gameObject), Is.EqualTo(SculptedBasaltAssetPipeline.PrefabPath));
                        Assert.That(basalt.GetComponent<LODGroup>().GetLODs().Length, Is.EqualTo(3));
                        Assert.That(basalt.GetComponentInChildren<MeshCollider>().convex, Is.True);
                    }
                    Assert.That(areas[i].GetComponentsInChildren<GroundTextureSticker>().Length, Is.Zero);
                    var terrain = areas[i].GetComponentsInChildren<Terrain>(); var rocks = areas[i].GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>();
                    Assert.That(terrain.Length, Is.EqualTo(1)); Assert.That(rocks.Length, Is.EqualTo(1));
                    Assert.That(terrain[0].materialTemplate, Is.SameAs(material)); Assert.That(terrain[0].terrainData.terrainLayers.Length, Is.EqualTo(1));
                    shared ??= terrain[0].terrainData.terrainLayers[0]; Assert.That(terrain[0].terrainData.terrainLayers[0], Is.SameAs(shared));
                    var heights = terrain[0].terrainData.GetHeights(0, 0, 65, 65);
                    foreach (float h in heights) Assert.That(h, Is.Zero);
                    Assert.That(rocks[0].IsFormationMember, Is.False); Assert.That(rocks[0].GetComponentsInChildren<TopDown3DRockVolumeNode>().Length, Is.GreaterThan(0));
                    Assert.That(rocks[0].RockMaterial, Is.Not.Null);
                    foreach (var transform in areas[i].GetComponentsInChildren<Transform>())
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero);
                }
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
