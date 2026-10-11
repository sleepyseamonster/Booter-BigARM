using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    [InitializeOnLoad]
    public static class TopDown3DCampPerformanceCapture
    {
        private const string ActiveKey = "Booter.Camp.CaptureActive";
        private static double deadline, stageStart;
        private static int stage, scenario;
        private static Gamepad replay;
        private static TopDown3DCampController camp;
        private static TopDown3DSurvivalVitals vitals;
        private static TopDown3DCampFrameProbe probe;
        private static float originalSpeed, startReserve, startWater, startPhase;
        private static double startTime;
        private static readonly List<Sample> results = new();
        [Serializable] private sealed class Sample
        {
            public float speed, p50Ms, p95Ms, simulationSeconds, sunSeconds, reserveGain, waterSpent, droppedSimulationSeconds, wakeReleaseMs;
            public int frames, maximumFixedStepsPerFrame;
            public bool focused, remainedSleeping, releasedSpeed;
        }
        static TopDown3DCampPerformanceCapture()
        {
            if (SessionState.GetBool(ActiveKey, false))
            { deadline = EditorApplication.timeSinceStartup + 400; EditorApplication.update += Tick; }
        }
        public static void RunFromCli()
        {
            if (!Application.dataPath.Replace('\\', '/').Contains("/BooterBigArmValidation/"))
                throw new InvalidOperationException("Capture is restricted to an isolated candidate.");
            SessionState.SetBool(ActiveKey, true);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Production/GreaterWasteland.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new InvalidOperationException("Camp performance capture timed out in stage " + stage);
                switch (stage)
                {
                    case 0:
                        camp = UnityEngine.Object.FindFirstObjectByType<TopDown3DCampController>();
                        if (camp == null || !camp.CanCallUp) { stageStart = 0; return; }
                        if (stageStart == 0) stageStart = EditorApplication.timeSinceStartup;
                        if (EditorApplication.timeSinceStartup - stageStart < 2) return;
                        originalSpeed = camp.Settings.sleepSpeed; camp.Settings.sleepSpeed = scenario == 0 ? 1 : 4;
                        vitals = camp.GetComponent<TopDown3DSurvivalVitals>(); vitals.SetValue(TopDown3DSurvivalVital.Reserve, 0);
                        replay = InputSystem.AddDevice<Gamepad>("Camp capture replay");
                        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                        InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
                        Application.runInBackground = true;
                        Debug.Log("CAMP_CAPTURE_FOCUS: " + Application.isFocused);
                        // The replay device is local to this hidden validation process. Production
                        // focus-loss callbacks remain active; no real window is focused or activated.
                        if (!camp.CallUp()) throw new InvalidOperationException("Camp replay could not begin placement");
                        stage = 1; stageStart = EditorApplication.timeSinceStartup; break;
                    case 1:
                        if (EditorApplication.timeSinceStartup - stageStart < .4) return;
                        InputSystem.QueueStateEvent(replay, new GamepadState().WithButton(GamepadButton.South)); stage = 2; break;
                    case 2:
                        if (camp.Phase == TopDown3DCampPhase.Placement) return;
                        InputSystem.QueueStateEvent(replay, new GamepadState()); stage = 3; break;
                    case 3:
                        if (camp.Phase != TopDown3DCampPhase.Idle || camp.Record == null) return;
                        if (!camp.CallUp()) throw new InvalidOperationException("Camp replay could not open local actions");
                        stage = 4; stageStart = EditorApplication.timeSinceStartup; break;
                    case 4:
                        if (EditorApplication.timeSinceStartup - stageStart < .4) return;
                        InputSystem.QueueStateEvent(replay, new GamepadState().WithButton(GamepadButton.South)); stage = 5; break;
                    case 5:
                        if (camp.Phase == TopDown3DCampPhase.Actions) return;
                        InputSystem.QueueStateEvent(replay, new GamepadState()); stage = 6; break;
                    case 6:
                        if (camp.Phase != TopDown3DCampPhase.Sleeping) return;
                        if (Camera.main != null) TopDown3DCampCapture.Render(Camera.main, scenario == 0 ? "live-camp-1x.png" : "live-camp-4x.png");
                        stage = 7; stageStart = EditorApplication.timeSinceStartup; deadline = stageStart + 90; break;
                    case 7:
                        if (camp.Phase != TopDown3DCampPhase.Sleeping) throw new InvalidOperationException("Sleep ended during warmup");
                        if (EditorApplication.timeSinceStartup - stageStart < 10) return;
                        probe = camp.gameObject.AddComponent<TopDown3DCampFrameProbe>();
                        probe.speed = scenario == 0 ? 1 : 4;
                        startTime = Time.timeAsDouble; startReserve = vitals.Reserve; startWater = vitals.Thirst;
                        startPhase = PerpetualTwilightSun.Active.Cycle01; stage = 8; stageStart = EditorApplication.timeSinceStartup; break;
                    case 8:
                        if (EditorApplication.timeSinceStartup - stageStart < 30) return;
                        var values = probe.frames.OrderBy(value => value).ToArray();
                        if (values.Length == 0) throw new InvalidOperationException("No rendered frames captured");
                        var sample = new Sample
                        {
                            speed = scenario == 0 ? 1 : 4, focused = Application.isFocused, remainedSleeping = camp.Phase == TopDown3DCampPhase.Sleeping,
                            frames = values.Length, p50Ms = values[values.Length / 2], p95Ms = values[Mathf.Min(values.Length - 1, (int)(values.Length * .95f))],
                            simulationSeconds = (float)(Time.timeAsDouble - startTime), sunSeconds = Mathf.Repeat(PerpetualTwilightSun.Active.Cycle01 - startPhase, 1) * 1200,
                            reserveGain = vitals.Reserve - startReserve, waterSpent = startWater - vitals.Thirst,
                            droppedSimulationSeconds = (float)(probe.expectedSeconds - probe.scaledSeconds), maximumFixedStepsPerFrame = probe.maximumFixedSteps
                        };
                        var before = System.Diagnostics.Stopwatch.StartNew(); camp.NotifyDanger("Capture wake request");
                        sample.wakeReleaseMs = (float)before.Elapsed.TotalMilliseconds; sample.releasedSpeed = Mathf.Approximately(Time.timeScale, 1);
                        results.Add(sample); UnityEngine.Object.Destroy(probe); probe = null;
                        camp.Settings.sleepSpeed = originalSpeed; InputSystem.RemoveDevice(replay); replay = null;
                        if (scenario == 0)
                        {
                            scenario = 1; stage = 0; stageStart = 0; deadline = EditorApplication.timeSinceStartup + 400;
                            SceneManager.LoadScene("Assets/_Project/Scenes/Production/GreaterWasteland.unity");
                        }
                        else Complete();
                        break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception); Write("capture-failure.txt", exception.ToString());
                SessionState.SetBool(ActiveKey, false); EditorApplication.update -= Tick;
                if (camp != null) { camp.InterruptForSystemMenu(); camp.Settings.sleepSpeed = originalSpeed; }
                EditorApplication.Exit(1);
            }
        }
        private static void Complete()
        {
            Write("performance-1x.json", JsonUtility.ToJson(results[0], true));
            Write("performance-4x.json", JsonUtility.ToJson(results[1], true));
            var threshold = Mathf.Max(33.3f, 1.5f * results[0].p95Ms);
            var passed = results.All(value => value.remainedSleeping && value.releasedSpeed && Mathf.Abs(value.droppedSimulationSeconds) < .03f
                && Mathf.Abs(value.sunSeconds - value.simulationSeconds) < .1f)
                && results[1].p95Ms <= threshold;
            Write("performance-result.json", "{\"passed\":" + passed.ToString().ToLowerInvariant() + ",\"p95BudgetMs\":" + threshold.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
            SessionState.SetBool(ActiveKey, false); EditorApplication.update -= Tick;
            Debug.Log("CAMP PERFORMANCE CAPTURE " + (passed ? "PASSED" : "FAILED") + ": 10s warmup + 30s capture per speed, real camp replay, native terrain and companion.");
            EditorApplication.Exit(passed ? 0 : 1);
        }
        private static void Write(string name, string content)
        { Directory.CreateDirectory("Logs/CampCapture"); File.WriteAllText("Logs/CampCapture/" + name, content); }
    }

    public sealed class TopDown3DCampFrameProbe : MonoBehaviour
    {
        public readonly List<float> frames = new();
        public double expectedSeconds, scaledSeconds;
        public float speed;
        public int maximumFixedSteps;
        private int fixedSteps;
        private double previousReal;
        private double previousSimulation;
        private bool started;
        private void FixedUpdate() => fixedSteps++;
        private void Update()
        {
            var now = Time.realtimeSinceStartupAsDouble;
            var simulation = Time.timeAsDouble;
            if (!started) { started = true; previousReal = now; previousSimulation = simulation; return; }
            var realElapsed = now - previousReal; previousReal = now;
            frames.Add((float)(realElapsed * 1000)); expectedSeconds += realElapsed * speed; scaledSeconds += simulation - previousSimulation; previousSimulation = simulation;
            maximumFixedSteps = Mathf.Max(maximumFixedSteps, fixedSteps); fixedSteps = 0;
        }
    }
}
