using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class InventoryHaulingStudyTests
    {
        [Test] public void AuthoredStudyHasIndependentTuningAndNoGeneratedOrSaveServices()
        {
            var scene = EditorSceneManager.OpenPreviewScene(InventoryHaulingStudyAuthoring.ScenePath);
            try
            {
                InventoryHaulingStudyAuthoring.Validate(scene);
                Assert.That(TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerInventory>(scene).Capacity, Is.EqualTo(8));
                Assert.That(TopDown3DGameHudCanvas.FindInScene<TopDown3DResourceCache>(scene).State.Capacity, Is.EqualTo(2));
                typeof(GreaterWastelandInventorySaveService).GetMethod("Install", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, new object[] { scene });
                Assert.That(TopDown3DGameHudCanvas.FindInScene<GreaterWastelandInventorySaveService>(scene), Is.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>(InventoryHaulingStudyAuthoring.SettingsPath);
            var state = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 12, settings.HardMass));
            Assert.That(state.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 9)).Succeeded, Is.True);
            Assert.That(state.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 1)).Code, Is.EqualTo(TopDown3DInventoryResultCode.TooHeavy));
            var production = AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>("Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset");
            Assert.That(production.HardMass, Is.EqualTo(92f).Within(.001f));
        }
    }
}
