using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DSurvivalHud : MonoBehaviour
    {
        public const float ReferenceWidth = 300f;
        public const float ReferenceHeight = 152f;
        public const float ReferenceMargin = 34f;
        public const float ReferenceGap = 5f;
        public const float ReferencePadding = 8f;
        public const float MinimumUiScale = 0.85f;

        private static readonly Color ShadowColor = new(0.01f, 0.012f, 0.015f, 0.48f);
        private static readonly Color PanelColor = new(0.027f, 0.033f, 0.039f, 0.82f);
        private static readonly Color CellColor = new(0.075f, 0.09f, 0.106f, 0.94f);
        private static readonly Color BorderColor = new(0.77f, 0.68f, 0.5f, 0.82f);
        private static readonly Color HighlightColor = new(0.98f, 0.88f, 0.66f, 0.18f);
        private static readonly Color TrackColor = new(0.025f, 0.03f, 0.035f, 1f);
        private static readonly Color HealthColor = new(0.73f, 0.22f, 0.17f, 1f);
        private static readonly Color HungerColor = new(0.78f, 0.57f, 0.2f, 1f);
        private static readonly Color ThirstColor = new(0.22f, 0.65f, 0.67f, 1f);
        private static readonly Color OxygenColor = new(0.54f, 0.73f, 0.78f, 1f);

        [SerializeField] private TopDown3DSurvivalVitals vitals;

        private GUIStyle labelStyle;

        public TopDown3DSurvivalVitals Vitals => vitals;

        public void Configure(TopDown3DSurvivalVitals survivalVitals)
        {
            vitals = survivalVitals;
        }

        public static TopDown3DSurvivalHud TryInstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var player = FindInScene<TopDown3DPlayerMotor>(scene);
            if (player == null)
            {
                return null;
            }

            var survivalVitals = player.GetComponent<TopDown3DSurvivalVitals>();
            if (survivalVitals == null)
            {
                survivalVitals = player.gameObject.AddComponent<TopDown3DSurvivalVitals>();
            }

            var existing = FindInScene<TopDown3DSurvivalHud>(scene);
            if (existing != null)
            {
                existing.Configure(survivalVitals);
                return existing;
            }

            var hudObject = new GameObject("Survival HUD");
            SceneManager.MoveGameObjectToScene(hudObject, scene);
            var hud = hudObject.AddComponent<TopDown3DSurvivalHud>();
            hud.Configure(survivalVitals);
            return hud;
        }

        public static Rect GetPanelRect(Rect safeArea, int screenHeight, float scale)
        {
            var scaledMargin = ReferenceMargin * scale;
            return new Rect(
                safeArea.xMin + scaledMargin,
                screenHeight - safeArea.yMax + scaledMargin,
                ReferenceWidth * scale,
                ReferenceHeight * scale);
        }

        public static Rect GetMeterRect(Rect panel, int meterIndex, float scale)
        {
            var gap = ReferenceGap * scale;
            var padding = ReferencePadding * scale;
            var cellHeight = (panel.height - (padding * 2f) - (gap * 3f)) * 0.25f;
            return new Rect(
                panel.x + padding,
                panel.y + padding + (meterIndex * (cellHeight + gap)),
                panel.width - (padding * 2f),
                cellHeight);
        }

        public static float GetUiScale(int screenWidth, int screenHeight)
        {
            return Mathf.Clamp(
                Mathf.Min(screenWidth / 1920f, screenHeight / 1080f),
                MinimumUiScale,
                1.25f);
        }

        private void OnGUI()
        {
            if (vitals == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var scale = GetUiScale(Screen.width, Screen.height);
            var panel = GetPanelRect(Screen.safeArea, Screen.height, scale);
            EnsureStyles(scale);

            DrawSolid(new Rect(
                panel.x + (4f * scale),
                panel.y + (5f * scale),
                panel.width,
                panel.height), ShadowColor);
            DrawSolid(Expand(panel, 2f * scale), BorderColor);
            DrawSolid(panel, PanelColor);
            DrawSolid(
                new Rect(panel.x, panel.y, panel.width, Mathf.Max(1f, 2f * scale)),
                HighlightColor);
            DrawMeter(panel, 0, scale, "HEALTH", TopDown3DSurvivalVital.Health, HealthColor);
            DrawMeter(panel, 1, scale, "HUNGER", TopDown3DSurvivalVital.Hunger, HungerColor);
            DrawMeter(panel, 2, scale, "THIRST", TopDown3DSurvivalVital.Thirst, ThirstColor);
            DrawMeter(panel, 3, scale, "OXYGEN", TopDown3DSurvivalVital.Oxygen, OxygenColor);
        }

        private void DrawMeter(
            Rect panel,
            int meterIndex,
            float scale,
            string label,
            TopDown3DSurvivalVital vital,
            Color fillColor)
        {
            var cell = GetMeterRect(panel, meterIndex, scale);
            DrawSolid(cell, CellColor);

            var padding = 7f * scale;
            var pipWidth = Mathf.Max(3f, 4f * scale);
            var pipHeight = Mathf.Max(10f, cell.height * 0.48f);
            DrawSolid(
                new Rect(
                    cell.x + padding,
                    cell.center.y - (pipHeight * 0.5f),
                    pipWidth,
                    pipHeight),
                fillColor);

            var labelWidth = 61f * scale;
            var labelRect = new Rect(
                cell.x + padding + pipWidth + (7f * scale),
                cell.y,
                labelWidth,
                cell.height);
            GUI.Label(labelRect, label, labelStyle);

            var barX = labelRect.xMax + (4f * scale);
            var bar = new Rect(
                barX,
                cell.y + (cell.height * 0.31f),
                Mathf.Max(1f, cell.xMax - barX - padding),
                Mathf.Max(7f, cell.height * 0.38f));
            DrawSolid(bar, TrackColor);

            var inset = Mathf.Max(1f, 1.5f * scale);
            var inner = new Rect(
                bar.x + inset,
                bar.y + inset,
                Mathf.Max(0f, bar.width - (inset * 2f)),
                Mathf.Max(0f, bar.height - (inset * 2f)));
            inner.width *= vitals.GetNormalizedValue(vital);
            DrawSolid(inner, fillColor);

            if (inner.width > 0f)
            {
                var sheen = new Rect(inner.x, inner.y, inner.width, Mathf.Max(1f, inner.height * 0.34f));
                var sheenColor = Color.Lerp(fillColor, Color.white, 0.42f);
                sheenColor.a = 0.38f;
                DrawSolid(sheen, sheenColor);
            }
        }

        private void EnsureStyles(float scale)
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.9f, 0.86f, 0.76f, 0.96f) }
                };
            }

            labelStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(11f * scale));
        }

        private static Rect Expand(Rect rect, float amount)
        {
            return new Rect(
                rect.x - amount,
                rect.y - amount,
                rect.width + (amount * 2f),
                rect.height + (amount * 2f));
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }

    internal static class TopDown3DSurvivalHudBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterForSceneLoads()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCurrentSceneHasHud()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TopDown3DSurvivalHud.TryInstallForScene(scene);
        }
    }
}
