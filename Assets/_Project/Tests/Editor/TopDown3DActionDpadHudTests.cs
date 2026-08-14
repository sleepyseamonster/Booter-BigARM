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
        public void Install_BuildsPolishedDpadUnderSharedCanvas()
        {
            var scene = SceneManager.CreateScene("DpadLayoutTestScene");
            try
            {
                AddInactiveInputRouter(scene);
                var gameHud = TopDown3DGameHudCanvas.TryInstallForScene(scene);
                var hud = TopDown3DActionDpadHud.TryInstallForScene(scene);

                var canvas = gameHud.Canvas;
                var scaler = gameHud.Scaler;
                var graphic = hud.Graphic;

                Assert.That(canvas, Is.Not.Null);
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.sortingOrder, Is.EqualTo(TopDown3DActionDpadHud.CanvasSortingOrder));
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                Assert.That(graphic, Is.Not.Null);
                Assert.That(graphic.raycastTarget, Is.False);
                Assert.That(hud.transform.parent, Is.EqualTo(gameHud.transform));
                var hudRect = (RectTransform)hud.transform;
                Assert.That(hudRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(hudRect.anchorMax, Is.EqualTo(Vector2.zero));
                Assert.That(hudRect.pivot, Is.EqualTo(Vector2.zero));
                var expectedPosition = TopDown3DActionDpadHud.GetBottomLeftAnchoredPosition(
                    Screen.safeArea,
                    canvas.scaleFactor);
                Assert.That(hudRect.anchoredPosition, Is.EqualTo(expectedPosition));
                Assert.That(hudRect.sizeDelta,
                    Is.EqualTo(Vector2.one * TopDown3DActionDpadHud.ReferenceSize));
                Assert.That(graphic.transform.parent, Is.EqualTo(hud.transform));
                Assert.That(graphic.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(graphic.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(gameHud.GetComponent<GraphicRaycaster>(), Is.Null);
                Assert.That(CountSceneCanvases(scene), Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
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
            var scene = SceneManager.CreateScene("DpadDirectionTestScene");
            try
            {
                AddInactiveInputRouter(scene);
                var hud = TopDown3DActionDpadHud.TryInstallForScene(scene);

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
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TryInstallForScene_RequiresGameplayMarkerAndIsIdempotent()
        {
            var scene = SceneManager.CreateScene("DpadHudTestScene");
            try
            {
                Assert.That(TopDown3DActionDpadHud.TryInstallForScene(scene), Is.Null);

                AddInactiveInputRouter(scene);

                var first = TopDown3DActionDpadHud.TryInstallForScene(scene);
                var second = TopDown3DActionDpadHud.TryInstallForScene(scene);

                Assert.That(first, Is.Not.Null);
                Assert.That(second, Is.SameAs(first));
                Assert.That(scene.GetRootGameObjects().Length, Is.EqualTo(2));
                Assert.That(CountSceneCanvases(scene), Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TryInstallForScene_PlayerMarkerDoesNotRequireInputRouter()
        {
            var scene = SceneManager.CreateScene("DpadPlayerMarkerTestScene");
            try
            {
                var player = new GameObject("Player");
                SceneManager.MoveGameObjectToScene(player, scene);
                player.AddComponent<TopDown3DPlayerMotor>();

                var hud = TopDown3DActionDpadHud.TryInstallForScene(scene);

                Assert.That(hud, Is.Not.Null);
                Assert.That(hud.GetComponentInParent<TopDown3DGameHudCanvas>(), Is.Not.Null);
                Assert.That(CountSceneCanvases(scene), Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GameHudCanvas_IsSharedByDpadAndSurvivalHud()
        {
            var scene = SceneManager.CreateScene("SharedGameHudTestScene");
            try
            {
                AddInactiveInputRouter(scene);
                var player = new GameObject("Player");
                SceneManager.MoveGameObjectToScene(player, scene);
                player.AddComponent<TopDown3DPlayerMotor>();

                var dpad = TopDown3DActionDpadHud.TryInstallForScene(scene);
                var survival = TopDown3DSurvivalHud.TryInstallForScene(scene);

                Assert.That(dpad, Is.Not.Null);
                Assert.That(survival, Is.Not.Null);
                Assert.That(dpad.GetComponentInParent<TopDown3DGameHudCanvas>(), Is.SameAs(survival.GameHud));
                Assert.That(CountSceneCanvases(scene), Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AddInactiveInputRouter(Scene scene)
        {
            var inputObject = new GameObject("Input Router");
            inputObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(inputObject, scene);
            inputObject.AddComponent<TopDown3DInputRouter>();
        }

        private static int CountSceneCanvases(Scene scene)
        {
            var count = 0;
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                count += roots[i].GetComponentsInChildren<Canvas>(true).Length;
            }

            return count;
        }
    }
}
