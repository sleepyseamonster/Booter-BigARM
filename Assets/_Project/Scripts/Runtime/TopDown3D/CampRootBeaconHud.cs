using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Passive navigation/debrief surface; reuses input modes and never creates an EventSystem.</summary>
    public sealed class CampRootBeaconHud : MonoBehaviour
    {
        private CampRootBeaconSession session;
        private Canvas canvas;
        private Text status, marker;
        private Image backing;
        private RectTransform safe;
        private Camera view;
        private TopDown3DMenuController menu;
        public string StatusText => status != null ? status.text : string.Empty;

        public void Configure(CampRootBeaconSession owner)
        {
            session = owner;
            var root = new GameObject("Root beacon guidance", typeof(RectTransform)); root.transform.SetParent(transform, false);
            canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            safe = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>(); safe.SetParent(root.transform, false);
            backing = new GameObject("Expedition status", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            backing.transform.SetParent(safe, false); backing.raycastTarget = false; backing.color = new Color(.025f, .06f, .065f, .92f);
            var panel = backing.rectTransform; panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 1);
            panel.anchoredPosition = new Vector2(-18, -18); panel.sizeDelta = new Vector2(510, 165);
            status = Label("Return instructions", panel, new Vector2(480, 150));
            status.alignment = TextAnchor.MiddleLeft; status.rectTransform.anchoredPosition = new Vector2(-255, -82);
            marker = Label("Root beacon return marker", safe, new Vector2(310, 80));
            view = TopDown3DGameHudCanvas.FindInScene<TopDown3DCameraRig>(gameObject.scene)?.GetComponent<Camera>();
            if (view == null)
                foreach (var camera in Camera.allCameras) if (camera.gameObject.scene == gameObject.scene && camera.cameraType == CameraType.Game) { view = camera; break; }
            menu = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene);
        }

        private static Text Label(string name, Transform parent, Vector2 size)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow)).GetComponent<Text>();
            text.transform.SetParent(parent, false); text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(1, 1);
            text.rectTransform.sizeDelta = size; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 23; text.color = new Color(.65f, 1, 1); text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleCenter; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.GetComponent<Shadow>().effectColor = Color.black; return text;
        }

        private void LateUpdate()
        {
            if (canvas == null || session == null) return;
            if (menu == null) menu = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene);
            bool complete = session.Run?.Phase == RootBeaconPhase.Extracted;
            canvas.enabled = session.isActiveAndEnabled && gameObject.scene.IsValid() && gameObject.scene.isLoaded
                &&(session.Run==null||session.HasLiveBinding)
                && (session.Input == null || session.Input.Mode == TopDown3DInputMode.Gameplay || complete && (menu == null || !menu.IsOpen));
            if (!canvas.enabled) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1, Screen.width), area.yMin / Mathf.Max(1, Screen.height));
            safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1, Screen.width), area.yMax / Mathf.Max(1, Screen.height)); safe.offsetMin = safe.offsetMax = Vector2.zero;
            bool large = menu != null && menu.LargeText;
            status.fontSize = marker.fontSize = large ? 27 : 23;
            float panelHeight = large ? 245 : 210;
            backing.rectTransform.sizeDelta = new Vector2(510, panelHeight);
            status.rectTransform.sizeDelta = new Vector2(480, panelHeight - 20);
            status.rectTransform.anchoredPosition = new Vector2(-255, -panelHeight * .5f);
            marker.enabled = false;
            if (session.Run == null) { status.text = "EXPEDITION UNAVAILABLE\n" + session.Failure; return; }
            var run = session.Run;
            if (complete)
            {
                status.text = session.IsExpeditionOwned
                    ? "RETURN SAVE PENDING\nCargo is retained. Resolve the save before continuing."
                    : "EXTRACTION COMPLETE\nBooter and Legger returned.\nItems retained for this session.\nTown transfer is not part of this test.\nOpen the system menu to exit.";
                return;
            }
            var delta = run.Origin - session.Player.Position;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            var directions = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            string bearing = directions[Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 360) / 45) % 8];
            status.text = run.Phase == RootBeaconPhase.Departing
                ? $"RETURNING TO OUTPOST\n{Mathf.CeilToInt((RootBeaconRun.DepartureSeconds - run.DepartureElapsed) * 10) / 10f:0.0} seconds remaining\nYou can keep moving. [{session.Input.GetBindingDisplayName("Gameplay/PickupCanister", "C")}] cancels."
                : $"ROOT BEACON · {Mathf.CeilToInt(distance)} m {bearing}\nReturn here to end your expedition.\nAt the beacon: [{session.Input.GetBindingDisplayName("Gameplay/Interact", "E")}] activate. Bring Legger.";
            if(run.Phase==RootBeaconPhase.Exploring&&distance<=4)
            {
                var blocker=session.DepartureBlocker;
                status.text=$"ROOT BEACON · {Mathf.CeilToInt(distance)} m {bearing}\n"
                    +(blocker??$"[{session.Input.GetBindingDisplayName("Gameplay/Interact", "E")}] Activate · return in 3 seconds. You can keep moving.");
            }
            if (view == null) return;
            var p = view.WorldToScreenPoint(run.Origin + Vector3.up * 3.5f);
            bool outside = p.z <= 0 || !area.Contains(new Vector2(p.x, p.y));
            if (p.z < 0) { p.x = Screen.width - p.x; p.y = Screen.height - p.y; }
            // Reserve the top strip for the two instructional panels and the bottom strip for action prompts.
            float scale = canvas.scaleFactor;
            var bounds = new Rect(area.xMin + 165 * scale, area.yMin + 170 * scale,
                Mathf.Max(0, area.width - 330 * scale), Mathf.Max(0, area.height - 480 * scale));
            var clamped = new Vector2(Mathf.Clamp(p.x, bounds.xMin, bounds.xMax), Mathf.Clamp(p.y, bounds.yMin, bounds.yMax));
            outside |= Vector2.Distance(clamped, new Vector2(p.x, p.y)) > 1;
            marker.enabled = true; marker.rectTransform.position = clamped;
            marker.text = (outside ? "RETURN (OUT OF VIEW)" : "ROOT BEACON") + $"\n{Mathf.CeilToInt(distance)} m {bearing}";
        }
    }
}
