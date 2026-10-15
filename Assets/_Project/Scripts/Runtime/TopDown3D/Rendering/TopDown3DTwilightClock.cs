using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-550)]
    public sealed class TopDown3DTwilightClock : MonoBehaviour
    {
        [SerializeField] private bool cycleEnabled = true;
        [SerializeField, Min(1f)] private float durationSeconds = 1200f;
        [SerializeField, Range(0f, 1f)] private float phase = .12f;
        [SerializeField] private float centerElevation = 19f, elevationAmplitude = 7f;
        [SerializeField] private float centerAzimuth = -32f, azimuthAmplitude = 14f;
        public float Phase => phase;
        public float DurationSeconds => durationSeconds;
        public bool CycleEnabled => cycleEnabled;
        public PerpetualTwilightSun.SunState State => PerpetualTwilightSun.EvaluateCycle(phase,
            centerElevation, elevationAmplitude, centerAzimuth, azimuthAmplitude);
        private void Update() { if (cycleEnabled) Advance(Time.deltaTime); }
        public void Advance(float scaledSeconds)
        {
            if (!isActiveAndEnabled || !cycleEnabled || !float.IsFinite(scaledSeconds)) return;
            phase = Mathf.Repeat(phase + scaledSeconds / Mathf.Max(1f, durationSeconds), 1f);
        }
        public void SetPhase(float value) { if (float.IsFinite(value)) phase = Mathf.Repeat(value, 1f); }
        public void SetRunning(bool value) => cycleEnabled = value;
        public void Configure(bool running, float duration, float value, float elevation, float amplitude, float azimuth, float sideAmplitude)
        {
            cycleEnabled = running; durationSeconds = Mathf.Max(1f, duration); SetPhase(value);
            centerElevation = elevation; elevationAmplitude = amplitude;
            centerAzimuth = azimuth; azimuthAmplitude = sideAmplitude;
        }
    }
}
