// Inert here. Copy into the task-owned candidate's asmdef-isolated Editor folder.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Profiling;
using Stopwatch = System.Diagnostics.Stopwatch;

[InitializeOnLoad]
public static class GreaterWastelandInputAudit
{
    const string Active = "Booter.InputAudit.Active", Started = "Booter.InputAudit.Started";
    static double warmStart, deadline;
    static double approachStart;
    static bool sink;
    static Gamepad replay;
    static TopDown3DInputRouter router;
    static TopDown3DInteractionController interaction;
    static TopDown3DBigArmCargo cargo;
    static TopDown3DInventoryUiController inventory;
    static TopDown3DMenuController menu;
    static TopDown3DInteractionFeedbackHud hud;
    static GameObject proxy;
    static readonly List<GameObject> fixtures = new();
    static readonly List<Row> rows = new();
    [Serializable] public sealed class Row
    {
        public string name;
        public int repeat, samples, operationsPerSample;
        public double medianUs, p95Us, allocationEventsPerOperation;
        public bool allocationObserverValid;
    }
    [Serializable] public sealed class Report
    {
        public string unityVersion, scene, label, persistentDataPath, evidenceKind;
        public int residentTerrains, terrainColliders, ironstoneNodes;
        public bool focused, headless, cargoProxyClear, cargoProxyBlocked, byteCounterAvailable, inventoryCargoAccessible, actionCargoAccessible;
        public Vector3 proxyPosition;
        public int ignoredChildRayHits, denseOverlapHitCount;
        public float actorCargoDistance;
        public long positiveControlEvents, negativeControlEvents;
        public Row[] rows;
    }
    static GreaterWastelandInputAudit()
    {
        if (!SessionState.GetBool(Active, false)) return;
        if (SessionState.GetBool(Started, false)) { EditorApplication.delayCall += () => EditorApplication.Exit(1); return; }
        deadline = EditorApplication.timeSinceStartup + 300;
        EditorApplication.update += Tick;
    }
    public static void Launch()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').Contains("/BooterBigArmValidation/InputInteraction")
            || !Application.productName.StartsWith("InputInteraction", StringComparison.Ordinal))
            throw new InvalidOperationException("Use an isolated background InputInteraction candidate with a separate product/save profile.");
        SessionState.SetBool(Active, true); SessionState.SetBool(Started, false);
        var label = Argument("-inputProfileLabel", "baseline");
        if (!System.Text.RegularExpressions.Regex.IsMatch(label, "^[A-Za-z0-9-]+$")) throw new ArgumentException("Use an alphanumeric capture label.");
        // Each launch starts from the saved scene, not the previous replay's checkpoint.
        PlayerSettings.productName = new DirectoryInfo(Path.GetDirectoryName(Application.dataPath)).Name + "-" + label;
        deadline = EditorApplication.timeSinceStartup + 300;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Application.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Production/GreaterWasteland.unity");
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Input audit startup exceeded 300 seconds.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            EditorApplication.QueuePlayerLoopUpdate();
            router = UnityEngine.Object.FindFirstObjectByType<TopDown3DInputRouter>();
            interaction = UnityEngine.Object.FindFirstObjectByType<TopDown3DInteractionController>();
            cargo = UnityEngine.Object.FindFirstObjectByType<TopDown3DBigArmCargo>();
            inventory = UnityEngine.Object.FindFirstObjectByType<TopDown3DInventoryUiController>();
            menu = UnityEngine.Object.FindFirstObjectByType<TopDown3DMenuController>();
            hud = UnityEngine.Object.FindFirstObjectByType<TopDown3DInteractionFeedbackHud>();
            if (router == null || interaction == null || cargo == null || inventory == null || menu == null || hud == null || !menu.IsConfigured) return;
            if (warmStart == 0)
            {
                replay = InputSystem.AddDevice<Gamepad>("Input audit isolated replay");
                router.InputActions.devices = new InputDevice[] { replay };
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.QueueStateEvent(replay, new GamepadState());
                warmStart = EditorApplication.timeSinceStartup; return;
            }
            if (EditorApplication.timeSinceStartup - warmStart < 10) return;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-inputProfileApproach") >= 0)
            {
                var actor = UnityEngine.Object.FindFirstObjectByType<TopDown3DPlayerInventory>();
                if (approachStart == 0)
                {
                    var follower = UnityEngine.Object.FindFirstObjectByType<TopDown3DBigArmFollower>();
                    if (follower == null) throw new InvalidOperationException("Physical companion approach required.");
                    follower.RequestCallOver(); approachStart = EditorApplication.timeSinceStartup; return;
                }
                if (EditorApplication.timeSinceStartup - approachStart < 3) return;
                if (!TopDown3DCargoAccess.CanAccess(cargo, actor.transform))
                {
                    if (EditorApplication.timeSinceStartup - approachStart > 20) throw new TimeoutException("Companion could not physically approach cargo range.");
                    return;
                }
            }
            SessionState.SetBool(Started, true); EditorApplication.update -= Tick;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-inputProfileInspect") >= 0)
            {
                var action = UnityEngine.Object.FindFirstObjectByType<TopDown3DPlayerActionController>();
                var actor = UnityEngine.Object.FindFirstObjectByType<TopDown3DPlayerInventory>();
                var actionCargo = typeof(TopDown3DPlayerActionController).GetField("bigArmCargo", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(action);
                var hudInput = typeof(TopDown3DInteractionFeedbackHud).GetField("input", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
                Directory.CreateDirectory("Logs/InputAudit");
                File.WriteAllText("Logs/InputAudit/" + Argument("-inputProfileLabel", "inspection") + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    inventoryCargoAccessible = TopDown3DCargoAccess.CanAccess(cargo, actor.transform),
                    actionCargoAccessible = action.CanOpenCargo, actionCargoAssigned = actionCargo != null,
                    actionCargoMatchesSceneCargo = ReferenceEquals(actionCargo, cargo), hudInputMatchesRouter = ReferenceEquals(hudInput, router),
                    gathering = action.IsGathering, placement = action.IsPlacingHarvester, inputMode = router.Mode.ToString(),
                    actorMatchesAction = actor.transform == action.transform, actorCargoDistance = Vector3.Distance(actor.transform.position, cargo.transform.position)
                }, Newtonsoft.Json.Formatting.Indented));
                Cleanup(); SessionState.SetBool(Active, false); SessionState.SetBool(Started, false); EditorApplication.Exit(0); return;
            }
            var report = RunMeasurements();
            Directory.CreateDirectory("Logs/InputAudit");
            File.WriteAllText("Logs/InputAudit/" + report.label + ".json", JsonUtility.ToJson(report, true));
            Cleanup(); SessionState.SetBool(Active, false); SessionState.SetBool(Started, false);
            Debug.Log("INPUT INTERACTION AUDIT COMPLETE: " + report.residentTerrains + " resident terrains, " + report.rows.Length + " measured rows; CPU/allocation probes, not Player FPS.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Directory.CreateDirectory("Logs/InputAudit"); File.WriteAllText("Logs/InputAudit/failure.txt", e.ToString());
            Debug.LogException(e); Cleanup(); SessionState.SetBool(Active, false); SessionState.SetBool(Started, false);
            EditorApplication.Exit(1);
        }
    }
    static Report RunMeasurements()
    {
        var actor = UnityEngine.Object.FindFirstObjectByType<TopDown3DPlayerInventory>();
        var motor = UnityEngine.Object.FindFirstObjectByType<TopDown3DPlayerMotor>();
        if (actor == null || motor == null || actor.transform != motor.transform) throw new InvalidOperationException("Actual Booter inventory/motor origin required.");
        var report = new Report { unityVersion = Application.unityVersion, scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
            label = Argument("-inputProfileLabel", "baseline"), focused = Application.isFocused, headless = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null,
            persistentDataPath = Application.persistentDataPath, evidenceKind = "Warmed synchronous main-thread CPU/allocation probes in the complete native production scene. No rendering or physical-device latency/FPS proof.",
            residentTerrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
            terrainColliders = UnityEngine.Object.FindObjectsByType<TerrainCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
            ironstoneNodes = UnityEngine.Object.FindObjectsByType<TopDown3DIronstoneNode>(FindObjectsSortMode.None).Length,
            actorCargoDistance = Vector3.Distance(actor.transform.position, cargo.transform.position) };
        if (report.residentTerrains != 9216 || report.terrainColliders != 9216) throw new InvalidOperationException("The current complete 9,216-terrain scene is required.");
        if (!report.persistentDataPath.Contains(Application.productName)) throw new InvalidOperationException("Save profile isolation failed.");
        report.positiveControlEvents = Allocations(() => GC.KeepAlive(new byte[4096]), 64);
        if (report.positiveControlEvents < 64) throw new InvalidOperationException("Allocation observer failed its positive control.");
        report.negativeControlEvents = Allocations(() => sink = !sink, 64);
        if (report.negativeControlEvents != 0) throw new InvalidOperationException("Allocation observer failed its nonallocating control.");
        var before = GC.GetAllocatedBytesForCurrentThread(); GC.KeepAlive(new byte[4096]);
        report.byteCounterAvailable = GC.GetAllocatedBytesForCurrentThread() - before >= 4096;
        proxy = new GameObject("Input audit cargo probe origin");
        for (int direction = 0; direction < 8; direction++)
        {
            var angle = direction * Mathf.PI / 4;
            proxy.transform.position = cargo.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 1.5f;
            Physics.SyncTransforms();
            if (TopDown3DCargoAccess.HasLineOfSight(proxy.transform, cargo.transform)) break;
        }
        report.proxyPosition = proxy.transform.position;
        if (!TopDown3DCargoAccess.HasLineOfSight(proxy.transform, cargo.transform)) throw new InvalidOperationException("Cannot establish clear cargo LOS fixture in native world.");
        report.inventoryCargoAccessible = TopDown3DCargoAccess.CanAccess(cargo, actor.transform);
        var sceneAction = UnityEngine.Object.FindFirstObjectByType<TopDown3DPlayerActionController>();
        report.actionCargoAccessible = sceneAction != null && sceneAction.CanOpenCargo;
        if (report.inventoryCargoAccessible && !report.actionCargoAccessible)
            throw new InvalidOperationException("Reachable cargo must also be bound to the gameplay action controller.");
        var updateRouter = Bound(router, "AfterInputUpdate");
        var updateInventory = Bound(inventory, "Update");
        var promptMethod = typeof(TopDown3DInteractionFeedbackHud).GetMethod("RefreshPrompt", BindingFlags.Instance | BindingFlags.NonPublic);
        var prompt = (Action<ITopDown3DInteractable>)Delegate.CreateDelegate(typeof(Action<ITopDown3DInteractable>), hud, promptMethod);
        for (int repeat = 0; repeat < 2; repeat++)
        {
            menu.Close(); inventory.Close(); router.EnterGameplayMode(); interaction.RefreshTarget();
            var target = interaction.CurrentTarget;
            if (target == null) target = UnityEngine.Object.FindFirstObjectByType<TopDown3DIronstoneNode>();
            Measure("observer.empty_delegate", repeat, () => sink = !sink);
            Measure("input.neutral_end_update", repeat, updateRouter);
            Measure("interaction.current_target_scan", repeat, interaction.RefreshTarget);
            Measure("interaction.canister_scan", repeat, () => GC.KeepAlive(interaction.FindCanisterTarget()));
            Measure("cargo.los_near", repeat, () => sink = TopDown3DCargoAccess.HasLineOfSight(proxy.transform, cargo.transform));
            report.cargoProxyClear = TopDown3DCargoAccess.HasLineOfSight(proxy.transform, cargo.transform);
            Measure("hud.unchanged_prompt", repeat, () => prompt(target));
            inventory.Open(true);
            Measure(report.inventoryCargoAccessible ? "inventory.open_access_poll_near" : "inventory.open_access_poll_inaccessible", repeat, updateInventory);
            inventory.Close();
            Measure("input.inventory_mode_roundtrip", repeat, () => { router.EnterInventoryMode(); router.EnterGameplayMode(); }, 32, 1);
            Measure("ui.inventory_open_close", repeat, () => { inventory.Open(true); inventory.Close(); }, 32, 1);
            menu.Open();
            var menuView = menu.GetComponent<TopDown3DMenuView>();
            Measure("ui.menu_navigation_rebuild", repeat, menuView.RefreshNavigation, 32, 1);
            menu.Close();
            // Fixtures exist only in this synchronous probe; no simulation step runs with them present.
            for (int i = 0; i < 64; i++)
            {
                var box = new GameObject("Input audit dense overlap", typeof(BoxCollider)); fixtures.Add(box);
                box.transform.position = motor.Position + Vector3.up * 2 + new Vector3((i % 8) * .025f, 0, (i / 8) * .025f);
                box.GetComponent<BoxCollider>().size = Vector3.one * .01f;
            }
            Physics.SyncTransforms();
            report.denseOverlapHitCount = Physics.OverlapSphere(motor.Position, 4f, ~0, QueryTriggerInteraction.Collide).Length;
            if (report.denseOverlapHitCount < 64) throw new InvalidOperationException("Dense target fixture did not saturate the actor's actual query.");
            Measure("interaction.dense_64_hit_scan", repeat, interaction.RefreshTarget); ClearFixtures();
            var from = proxy.transform.position + Vector3.up * .5f;
            var to = cargo.transform.position + Vector3.up * .5f;
            for (int i = 0; i < 64; i++)
            {
                var box = new GameObject("Input audit ignored actor child", typeof(BoxCollider)); fixtures.Add(box);
                box.transform.SetParent(proxy.transform); box.transform.position = Vector3.Lerp(from, to, (i + 1f) / 66f);
                box.GetComponent<BoxCollider>().size = Vector3.one * .008f;
            }
            Physics.SyncTransforms();
            if (!TopDown3DCargoAccess.HasLineOfSight(proxy.transform, cargo.transform)) throw new InvalidOperationException("Ignored-child fixture must have clear cargo LOS.");
            foreach (var hit in Physics.RaycastAll(from, (to-from).normalized, (to-from).magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (hit.transform.IsChildOf(proxy.transform)) report.ignoredChildRayHits++;
            if (report.ignoredChildRayHits < 64 * (repeat + 1)) throw new InvalidOperationException("Dense ray fixture did not hit 64 actor children.");
            Measure("cargo.los_64_ignored_children", repeat, () => sink = TopDown3DCargoAccess.HasLineOfSight(proxy.transform, cargo.transform));
            var blocker = new GameObject("Input audit wall", typeof(BoxCollider)); fixtures.Add(blocker);
            blocker.transform.position = Vector3.Lerp(from, to, .97f); blocker.GetComponent<BoxCollider>().size = Vector3.one * .02f;
            Physics.SyncTransforms(); report.cargoProxyBlocked = !TopDown3DCargoAccess.HasLineOfSight(proxy.transform, cargo.transform);
            if (!report.cargoProxyBlocked) throw new InvalidOperationException("A wall must still block cargo access behind ignored actor-child colliders.");
            Measure("cargo.los_dense_with_wall", repeat, () => sink = TopDown3DCargoAccess.HasLineOfSight(proxy.transform, cargo.transform));
            ClearFixtures();
        }
        report.rows = rows.ToArray(); return report;
    }
    static void Measure(string name, int repeat, Action action, int samples = 64, int operations = 16)
    {
        for (int i = 0; i < 16; i++) action();
        var times = new double[samples];
        for (int sample = 0; sample < samples; sample++)
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < operations; i++) action();
            times[sample] = (Stopwatch.GetTimestamp() - start) * 1000000d / Stopwatch.Frequency / operations;
        }
        Array.Sort(times);
        long allocationEvents = Allocations(action, samples * operations);
        rows.Add(new Row { name = name, repeat = repeat, samples = samples, operationsPerSample = operations,
            medianUs = times[samples / 2], p95Us = times[(int)(samples * .95)], allocationEventsPerOperation = (double)allocationEvents / (samples * operations), allocationObserverValid = true });
    }
    static long Allocations(Action action, int count)
    {
        using var recorder = new ProfilerRecorder(ProfilerCategory.Memory, "GC.Alloc", 1,
            ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.SumAllSamplesInFrame);
        if (!recorder.Valid) throw new InvalidOperationException("GC.Alloc observer unavailable.");
        recorder.Start(); for (int i = 0; i < count; i++) action(); recorder.Stop();
        return recorder.Count == 0 ? 0 : (long)recorder.GetSample(recorder.Count - 1).Count;
    }
    static Action Bound(object target, string method) => (Action)Delegate.CreateDelegate(typeof(Action), target,
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic));
    static void ClearFixtures() { foreach (var fixture in fixtures) UnityEngine.Object.DestroyImmediate(fixture); fixtures.Clear(); Physics.SyncTransforms(); }
    static void Cleanup() { ClearFixtures(); if (proxy != null) UnityEngine.Object.DestroyImmediate(proxy); if (replay != null) InputSystem.RemoveDevice(replay); }
    static string Argument(string name, string fallback) { var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
}
