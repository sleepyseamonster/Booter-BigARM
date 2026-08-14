using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DSurvivalHud : MonoBehaviour
    {
        public const float ReferenceWidth = 270f;
        public const float ReferenceHeight = 80f;
        public const float ReferenceMargin = 28f;
        public const float ReferenceGap = 6f;

        private static readonly Color PanelColor = new(0.027f, 0.033f, 0.039f, 0.82f);
        private static readonly Color CellColor = new(0.075f, 0.09f, 0.106f, 0.94f);
        private static readonly Color BorderColor = new(0.77f, 0.68f, 0.5f, 0.82f);
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
            var cellWidth = (panel.width - gap) * 0.5f;
            var cellHeight = (panel.height - gap) * 0.5f;
            var column = meterIndex % 2;
            var row = meterIndex / 2;
            return new Rect(
                panel.x + (column * (cellWidth + gap)),
                panel.y + (row * (cellHeight + gap)),
                cellWidth,
                cellHeight);
        }

        private void OnGUI()
        {
            if (vitals == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var scale = Mathf.Clamp(
                Mathf.Min(Screen.width / 1920f, Screen.height / 1080f),
                0.65f,
                1.25f);
            var panel = GetPanelRect(Screen.safeArea, Screen.height, scale);
            EnsureStyles(scale);

            DrawSolid(Expand(panel, 2f * scale), BorderColor);
            DrawSolid(panel, PanelColor);
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

            var padding = 6f * scale;
            var labelWidth = 48f * scale;
            var labelRect = new Rect(
                cell.x + padding,
                cell.y,
                labelWidth,
                cell.height);
            GUI.Label(labelRect, label, labelStyle);

            var bar = new Rect(
                labelRect.xMax,
                cell.y + (cell.height * 0.34f),
                Mathf.Max(1f, cell.xMax - labelRect.xMax - padding),
                Mathf.Max(3f, cell.height * 0.32f));
            DrawSolid(bar, TrackColor);

            var inset = Mathf.Max(1f, 1.5f * scale);
            var inner = new Rect(
                bar.x + inset,
                bar.y + inset,
                Mathf.Max(0f, bar.width - (inset * 2f)),
                Mathf.Max(0f, bar.height - (inset * 2f)));
            inner.width *= vitals.GetNormalizedValue(vital);
            DrawSolid(inner, fillColor);
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

            labelStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(10f * scale));
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
