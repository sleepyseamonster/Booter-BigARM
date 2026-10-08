using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Owns the authored world's camera range; local graphics preferences never alter terrain state.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(500)]
    public sealed class BadwaterCameraRange : MonoBehaviour
    {
        public const float MaximumDistance = 8000f;
        public const float FadeStartFraction = 0.75f;
        [SerializeField, Min(500f)] private float farClipPlane = MaximumDistance;
        private Camera outputCamera;
        private string preset = "Maximum";
        private bool preferenceApplied;

        public float FarClipPlane => farClipPlane;
        public string Preset => preset;
        public float EffectiveDistance => preferenceApplied ? GetDistance(preset) : farClipPlane;
        public bool DistanceHazeEnabled => preferenceApplied && preset != "Maximum" && isActiveAndEnabled;

        public static string NormalizePreset(string value) => value == "Low" || value == "Medium"
            || value == "High" ? value : "Maximum";

        public static float GetDistance(string value) => NormalizePreset(value) switch
        { "Low" => 500f, "Medium" => 1000f, "High" => 2000f, _ => MaximumDistance };

        public void SetPreset(string value)
        {
            preset = NormalizePreset(value);
            preferenceApplied = true;
            Apply();
        }

        public static float EvaluateFade(float eyeDepth, float distance)
        {
            float t = Mathf.Clamp01((eyeDepth - distance * FadeStartFraction)
                / Mathf.Max(0.001f, distance * (1f - FadeStartFraction)));
            return t * t * (3f - 2f * t);
        }

        public void Configure(float distance)
        {
            farClipPlane = Mathf.Clamp(distance, 500f, MaximumDistance);
            Apply();
        }

        private void OnEnable() => Apply();
        private void LateUpdate() => Apply();

        public void Apply()
        {
            if (outputCamera == null) outputCamera = GetComponent<Camera>();
            if (outputCamera != null && !Mathf.Approximately(outputCamera.farClipPlane, EffectiveDistance))
                outputCamera.farClipPlane = EffectiveDistance;
        }
    }
}
