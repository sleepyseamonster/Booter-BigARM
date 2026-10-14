using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundPaletteTests
    {
        [Test]
        public void AuthoredCatalogRetainsRestrictedSourcesWithoutOfferingThemAsActions()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCatalog>(BooterBigArm.Editor.AnimationPlaygroundUiAuthoring.CatalogPath);
            Assert.That(catalog,Is.Not.Null);Assert.That(catalog.entries.Length,Is.EqualTo(71));
            Assert.That(catalog.entries.Count(e=>!e.restrictedReference),Is.EqualTo(68));
            Assert.That(catalog.entries.Select(e=>e.sourceId).Distinct().Count(),Is.EqualTo(71));
        }
        [TestCase(false)] [TestCase(true)]
        public void PaletteRegistersInExistingViewWithOneEventSystemAndNavigation(bool large)
        {
            var menu=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/Resources/SystemMenu.prefab"));
            var events=new GameObject("Palette fixture EventSystem",typeof(EventSystem));GameObject panel=null;
            try
            {
                var view=menu.GetComponent<TopDown3DMenuView>();view.Initialize(events.GetComponent<EventSystem>());
                panel=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BooterBigArm.Editor.AnimationPlaygroundUiAuthoring.PalettePath));
                Assert.That(panel.GetComponentsInChildren<EventSystem>(true).Length,Is.EqualTo(0));
                view.RegisterExtraPanel("AnimationPalette",panel);view.ApplySettings(new TopDown3DMenuSettings{largeText=large,reducedMotion=true});
                view.Show("AnimationPalette","LabSelect0");
                Assert.That(events.GetComponent<EventSystem>().currentSelectedGameObject.name,Is.EqualTo("LabSelect0"));
                var rows=panel.GetComponentsInChildren<Button>(true).Where(b=>b.name.StartsWith("LabSelect")).ToArray();
                Assert.That(rows.Length,Is.EqualTo(8));
                foreach(var row in rows)
                {
                    Assert.That(row.navigation.mode,Is.EqualTo(Navigation.Mode.Explicit));
                    Assert.That(row.GetComponentInChildren<Text>().fontSize,Is.EqualTo(Mathf.RoundToInt(22*(large?1.25f:1f))));
                }
                view.Show("Root","Resume");Assert.That(panel.activeSelf,Is.False);
            }
            finally{Object.DestroyImmediate(menu);Object.DestroyImmediate(events);if(panel!=null)Object.DestroyImmediate(panel);}
        }
    }
}
