using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DActionDpadHudTests
    {
        [Test]
        public void Initialize_BuildsPolishedSafeAreaAwareBottomLeftHud()
        {
            var hudObject = new GameObject("HUD Test");
            try
            {
                var hud = hudObject.AddComponent<TopDown3DActionDpadHud>();
                hud.Initialize();

                var canvas = hudObject.GetComponent<Canvas>();
                var scaler = hudObject.GetComponent<CanvasScaler>();
                var graphic = hud.Graphic;

                Assert.That(canvas, Is.Not.Null);
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.sortingOrder, Is.EqualTo(TopDown3DActionDpadHud.CanvasSortingOrder));
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                Assert.That(graphic, Is.Not.Null);
                Assert.That(graphic.raycastTarget, Is.False);
                Assert.That(graphic.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(graphic.rectTransform.anchorMax, Is.EqualTo(Vector2.zero));
                Assert.That(graphic.rectTransform.pivot, Is.EqualTo(Vector2.zero));
                var expectedPosition = TopDown3DActionDpadHud.GetBottomLeftAnchoredPosition(
                    Screen.safeArea,
                    canvas.scaleFactor);
                Assert.That(graphic.rectTransform.anchoredPosition, Is.EqualTo(expectedPosition));
                Assert.That(graphic.rectTransform.sizeDelta,
                    Is.EqualTo(Vector2.one * TopDown3DActionDpadHud.ReferenceSize));
                Assert.That(graphic.transform.parent, Is.EqualTo(hudObject.transform));
                Assert.That(hudObject.transform.Find("Safe Area"), Is.Null);
                Assert.That(hudObject.GetComponent<GraphicRaycaster>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
            }
        }

        [Test]
        public void BottomLeftPosition_AccountsForSafeAreaAndCanvasScale()
        {
            var safeArea = new Rect(80f, 40f, 1760f, 980f);

            var anchored = TopDown3DActionDpadHud.GetBottomLeftAnchoredPosition(safeArea, 2f);

            Assert.That(anchored.x, Is.EqualTo(TopDown3DActionDpadHud.ReferenceMargin + 40f));
            Assert.That(anchored.y, Is.EqualTo(TopDown3DActionDpadHud.ReferenceMargin + 20f));
        }

        [Test]
        public void DebugOverlay_IsHiddenUntilExplicitlyEnabled()
        {
            var overlayObject = new GameObject("Debug Overlay Test");
            try
            {
                var overlay = overlayObject.AddComponent<TopDown3DDebugOverlay>();

                Assert.That(overlay.IsVisible, Is.False);

                overlay.SetVisible(true);

                Assert.That(overlay.IsVisible, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
            }
        }

        [Test]
        public void DirectionContract_LeavesActionsUnmappedAndUsesVialForDown()
        {
            var hudObject = new GameObject("HUD Test");
            try
            {
                var hud = hudObject.AddComponent<TopDown3DActionDpadHud>();
                hud.Initialize();

                Assert.That(hud.GetIcon(TopDown3DDpadDirection.Up), Is.EqualTo(TopDown3DDpadIcon.DirectionArrow));
                Assert.That(hud.GetIcon(TopDown3DDpadDirection.Left), Is.EqualTo(TopDown3DDpadIcon.DirectionArrow));
                Assert.That(hud.GetIcon(TopDown3DDpadDirection.Right), Is.EqualTo(TopDown3DDpadIcon.DirectionArrow));
                Assert.That(hud.GetIcon(TopDown3DDpadDirection.Down), Is.EqualTo(TopDown3DDpadIcon.Vial));

                hud.SetDirectionPressed(TopDown3DDpadDirection.Down, true);

                Assert.That(hud.IsDirectionPressed(TopDown3DDpadDirection.Down), Is.True);
                Assert.That(hud.IsDirectionPressed(TopDown3DDpadDirection.Up), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
            }
        }

        [Test]
        public void TryInstallForScene_RequiresPerspectiveInputAndIsIdempotent()
        {
            var scene = SceneManager.CreateScene("DpadHudTestScene");
            try
            {
                Assert.That(TopDown3DActionDpadHud.TryInstallForScene(scene), Is.Null);

                var inputObject = new GameObject("Input Router");
                inputObject.SetActive(false);
                SceneManager.MoveGameObjectToScene(inputObject, scene);
                inputObject.AddComponent<TopDown3DInputRouter>();

                var first = TopDown3DActionDpadHud.TryInstallForScene(scene);
                var second = TopDown3DActionDpadHud.TryInstallForScene(scene);

                Assert.That(first, Is.Not.Null);
                Assert.That(second, Is.SameAs(first));
                Assert.That(scene.GetRootGameObjects().Length, Is.EqualTo(2));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
