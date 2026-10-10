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
                    Assert.That(areas[i].transform.childCount, Is.EqualTo(2));
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
        public void SharedStudyLayerRetainsTheChosenSourceTextureAndRelativeUVRegion()
        {
            var source = AssetDatabase.LoadAssetAtPath<TerrainLayer>(NineAreaRockStudy.SourceLayer);
            var study = AssetDatabase.LoadAssetAtPath<TerrainLayer>(NineAreaRockStudy.Folder + "/StudyBase.terrainlayer");
            Assert.That(study, Is.Not.SameAs(source)); Assert.That(study.diffuseTexture, Is.SameAs(source.diffuseTexture));
            Assert.That(study.tileSize, Is.EqualTo(source.tileSize * (NineAreaRockStudy.AreaSize / 256f)));
            Assert.That(study.tileOffset, Is.EqualTo(source.tileOffset * (NineAreaRockStudy.AreaSize / 256f)));
        }
    }
}
