using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class CampLoopTestingSceneTests
    {
        private Scene scene;
        private string productionBefore;
        private string preferences;

        [SetUp] public void SetUp()
        {
            productionBefore = File.ReadAllText(TopDown3DWaypointCampSettings.ProductionScene);
            preferences = Path.Combine(Path.GetTempPath(), "CampLoopTests-" + System.Guid.NewGuid().ToString("N"));
            scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene);
        }
        [TearDown] public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(File.ReadAllText(TopDown3DWaypointCampSettings.ProductionScene), Is.EqualTo(productionBefore));
            if (Directory.Exists(preferences)) Directory.Delete(preferences, true);
        }
        private T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        [Test] public void SavedSliceRetainsCompleteCampContentAndNativeCollision()
        {
            CampLoopTestingSceneBuilder.Validate(scene);
            foreach (var terrain in All<Terrain>())
            {
                var guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(terrain.terrainData));
                Assert.That(productionBefore, Does.Contain("guid: " + guid));
                Assert.That(terrain.gameObject.activeInHierarchy && terrain.enabled, Is.True);
            }
            var camp = TopDown3DWaypointCampSettings.Load();
            foreach (var position in camp.returnRoute.Append(camp.position))
            {
                var support = All<Terrain>().Single(t => position.x >= t.transform.position.x && position.x < t.transform.position.x + 256
                    && position.z >= t.transform.position.z && position.z < t.transform.position.z + 256);
                Assert.That(support.SampleHeight(position) + support.transform.position.y, Is.EqualTo(position.y).Within(.15f));
            }
        }

        [Test] public void RuntimeInstallersProvideCampCompassHarvesterAndMenusOnce()
        {
            var ui = TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene, preferences);
            Assert.That(ui, Is.Not.Null);
            Assert.That(All<TopDown3DCampController>().Single().IsConfigured, Is.True);
            var waypoint = All<TopDown3DWaypointCampController>().Single();
            Assert.That(waypoint.Site, Is.Not.Null);
            Assert.That(waypoint.Definition.siteId, Is.EqualTo("camp.starting"));
            Assert.That(All<TopDown3DCompassHud>().Length, Is.EqualTo(1));
            Assert.That(All<TopDown3DPlacedHarvesterState>().Single().IsAuthored, Is.True);
            Assert.That(All<TopDown3DRadialMenuController>().Length, Is.EqualTo(1));
            Assert.That(All<TopDown3DMenuController>().Single().IsConfigured, Is.True);
            Assert.That(TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene, preferences), Is.SameAs(ui));
            GreaterWastelandIronstoneInstaller.TryInstall(scene);
            TopDown3DResourceCache.TryInstall(scene);
            Assert.That(All<TopDown3DIronstoneNode>().Length, Is.EqualTo(12));
            Assert.That(All<TopDown3DResourceCache>().Length, Is.EqualTo(1));
            Assert.That(All<TopDown3DWaypointCampController>().Length, Is.EqualTo(1));
        }

        [Test] public void TestingCheckpointCannotUseProductionSaveFolder()
        {
            var owner = new GameObject("Test checkpoint path");
            SceneManager.MoveGameObjectToScene(owner, scene);
            var service = owner.AddComponent<GreaterWastelandInventorySaveService>();
            Assert.That(service.SavePath, Is.EqualTo(Path.Combine(Application.persistentDataPath, "CampLoopTesting", "inventory-v1.json")));
            Assert.That(GreaterWastelandSceneScope.SupportsAuthoredLoop(scene), Is.True);
            Assert.That(GreaterWastelandSceneScope.CheckpointFolder(default), Is.EqualTo("GreaterWasteland"));
            Assert.That(GreaterWastelandSceneScope.SupportsAuthoredLoop(default), Is.False);
        }
    }
}
