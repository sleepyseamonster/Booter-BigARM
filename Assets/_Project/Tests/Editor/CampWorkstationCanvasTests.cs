using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.Tests
{
    public sealed class CampWorkstationCanvasTests
    {
        [Test] public void ReinitializationAndAccessibilityDoNotDuplicateInteractiveOwners()
        {
            var go = new GameObject("Camp UI", typeof(RectTransform));
            try
            {
                var view = go.AddComponent<TopDown3DCampWorkstationCanvas>();
                view.Initialize(_ => { }, _ => { }); view.Initialize(_ => { }, _ => { });
                Assert.That(go.GetComponents<Canvas>().Length, Is.EqualTo(1));
                Assert.That(go.GetComponents<GraphicRaycaster>().Length, Is.EqualTo(1));
                Assert.That(go.transform.childCount, Is.EqualTo(1));
                var labels = Enumerable.Range(0, 24).Select(i => "Resource " + i).ToArray();
                view.Present("Camp storage", "24 slots", "Bring the Legger closer.", labels, 0, false);
                Assert.That(go.GetComponentsInChildren<Button>(true).Count(b => b.name.StartsWith("Item ")), Is.EqualTo(24));
                Assert.That(go.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("Item ")).All(b => !b.interactable), Is.True);
                view.SetLargeText(true); var size = go.GetComponentsInChildren<Text>(true).Single(t => t.name == "Title").fontSize;
                view.SetLargeText(true);
                Assert.That(go.GetComponentsInChildren<Text>(true).Single(t => t.name == "Title").fontSize, Is.EqualTo(size));
                view.SetVisible(true); Assert.That(view.IsVisible, Is.True);
                view.SetVisible(false); Assert.That(view.IsVisible, Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
