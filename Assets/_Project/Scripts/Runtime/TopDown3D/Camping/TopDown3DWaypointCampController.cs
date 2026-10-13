using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DefaultExecutionOrder(210)]
    public sealed class TopDown3DWaypointCampController : MonoBehaviour
    {
        public TopDown3DWaypointCampRegistry Registry { get; private set; }
        public TopDown3DCampView Site { get; private set; }
        public bool IsOpen { get; private set; }
        public bool Traveling { get; private set; }
        public string LastTravelResult { get; private set; }
        public TopDown3DWaypointCampSettings Definition { get; private set; }
        private TopDown3DInputRouter input;
        private TopDown3DPlayerMotor motor;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DCampController recovery;
        private TopDown3DBigArmFollower legger;
        private TopDown3DBigArmCargo cargo;
        private TopDown3DPlayerActionController actions;
        private TopDown3DWorldSpeedController speed;
        private TopDown3DCampCanvas view;
        private EventSystem eventSystem;
        private InputSystemUIInputModule module;
        private InputActionReference submit, click, cancel;
        private bool leased, armed;
        public bool RegroupRequested { get; private set; }
        private Vector3 lastPosition;
        private float stallSeconds, journeySeconds;
        private float refreshAt;
        private TopDown3DMenuController menu;

        public static TopDown3DWaypointCampController TryInstall(Scene scene, TopDown3DInputRouter router,
            TopDown3DPlayerInventory inventory, EventSystem system, TopDown3DCampController recovery)
        {
            var definition = TopDown3DWaypointCampSettings.Load();
            if (definition == null || !definition.TryValidate(out _) || !GreaterWastelandSceneScope.SupportsAuthoredLoop(scene)) return null;
            var owner = inventory.GetComponent<TopDown3DWaypointCampController>() ?? inventory.gameObject.AddComponent<TopDown3DWaypointCampController>();
            owner.Configure(definition, router, system, recovery);
            return owner;
        }

        public void Configure(TopDown3DWaypointCampSettings definition, TopDown3DInputRouter router, EventSystem system, TopDown3DCampController camp)
        {
            if (Definition != null) return;
            if (!definition.TryValidate(out var reason)) throw new ArgumentException(reason);
            Definition = definition; input = router; eventSystem = system; recovery = camp;
            motor = GetComponent<TopDown3DPlayerMotor>(); vitals = GetComponent<TopDown3DSurvivalVitals>(); actions = GetComponent<TopDown3DPlayerActionController>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<TopDown3DBigArmFollower>();
                if (found != null) { if (legger != null) throw new InvalidOperationException("Multiple Legger actors"); legger = found; }
            }
            cargo = legger != null ? legger.GetComponent<TopDown3DBigArmCargo>() : null;
            Registry = new TopDown3DWaypointCampRegistry(definition.worldId, definition.siteId, true);
            speed = TopDown3DWorldSpeedController.ForScene(gameObject.scene);
            var ui = new GameObject("Camps Destination Canvas", typeof(RectTransform)); SceneManager.MoveGameObjectToScene(ui, gameObject.scene);
            view = ui.AddComponent<TopDown3DCampCanvas>(); view.Configure(BeginReturn, Regroup, Close); view.SetLabels("Return", "Regroup", "Back");
            module = system.GetComponent<InputSystemUIInputModule>();
            menu = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene);
            motor.WaypointTravelFinished += Arrival;
            vitals.HealthReduced += Damage; vitals.ResetPerformed += Reset;
            speed.Invalidated += SpeedLost; input.ModeChanged += ModeChanged; InputSystem.onDeviceChange += DeviceChanged;
            EnsureSite();
            TopDown3DCompassHud.TryInstallForScene(gameObject.scene)?.Bind(motor, this);
        }

        private void EnsureSite()
        {
            if (Site != null || recovery.Settings == null) return;
            var settings = recovery.Settings;
            if (!TopDown3DCampPlacement.Validate(Definition.position, Quaternion.identity, settings, transform, null, out var ground, out var reason))
            { actions?.ReportFeedback("Starting Camp unavailable: " + reason); return; }
            var root = Instantiate(settings.deployedPrefab); root.name = Definition.displayName;
            SceneManager.MoveGameObjectToScene(root, gameObject.scene); root.transform.SetPositionAndRotation(ground, Quaternion.identity);
            Site = root.GetComponent<TopDown3DCampView>();
            Site.ConfigureEstablished(recovery, settings, new TopDown3DCampRecord { id = Registry.SiteId, position = ground, rotation = Quaternion.identity });
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube); marker.name = "Camp marker";
            marker.transform.SetParent(root.transform, false); marker.transform.localPosition = new Vector3(2, 1.6f, -.4f);
            marker.transform.localScale = new Vector3(.12f, 3.2f, .12f);
            var source = root.GetComponentInChildren<Renderer>();
            if (source != null) marker.GetComponent<Renderer>().sharedMaterial = source.sharedMaterial;
            for (var i = 1; i < Definition.returnRoute.Length; i++)
            {
                var p = Definition.returnRoute[i] + Vector3.left * (Definition.corridorRadius + 2f);
                if (!TopDown3DCampPlacement.TryGround(p, out var support, out _)) continue;
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube); post.name = "Return corridor marker " + i;
                post.transform.SetParent(root.transform); post.transform.position = support + Vector3.up * .4f;
                post.transform.localScale = new Vector3(.12f, .8f, .12f);
                if (source != null) post.GetComponent<Renderer>().sharedMaterial = source.sharedMaterial;
            }
        }

        public bool Open()
        {
            if (Definition == null || view == null || IsOpen || Traveling || input.Mode != TopDown3DInputMode.Gameplay || !recovery.CanUseCamp) return false;
            EnsureSite();
            if (!motor.TryBeginActionConstraint(this)) return false;
            leased = true; submit = module.submit; click = module.leftClick; cancel = module.cancel;
            module.submit = null; module.leftClick = null; module.cancel = null;
            IsOpen = true; armed = false; input.EnterCampMode(TopDown3DInputMode.CampActions);
            Refresh(); return true;
        }

        public bool TryQuote(out Vector3[] path, out float seconds, out string reason)
        {
            path = null; seconds = 0; reason = "Camp has not been discovered";
            if (Registry == null || !Registry.Discovered) return false;
            if (Site == null) { reason = "Starting Camp is unavailable"; return false; }
            if (!TopDown3DWaypointRoute.TryReturn(motor.Position, Definition.returnRoute, Definition.corridorRadius, out path, out var distance))
            { reason = "Outside the starting return corridor"; return false; }
            if (distance < 2) { reason = "Already at the camp approach"; return false; }
            seconds = distance / Definition.walkingSpeed + 1;
            reason = "Legger is unavailable";
            if (legger == null || !legger.isActiveAndEnabled || cargo == null) return false;
            reason = "Legger needs supported ground before travel";
            if (legger.State == TopDown3DBigArmFollower.FollowState.WaitingForTerrain) return false;
            reason = "Legger cannot reach this shared route · Back and move to reconnect";
            if (legger.State == TopDown3DBigArmFollower.FollowState.WaitingForRoute) return false;
            reason = "Regroup with the Legger first"; if (!cargo.IsInAccessRange(motor.Position)) return false;
            reason = "World speed is in use"; if (speed.HasRequest || !Mathf.Approximately(Time.timeScale, 1)) return false;
            var prior = motor.Position;
            foreach (var point in path) { if (!motor.CanWalkWaypointSegment(prior, point)) { reason = "Return route is blocked"; return false; } prior = point; }
            var tuning = vitals.Settings;
            if (vitals.Hunger - seconds * tuning.HungerDepletionPerSecond <= tuning.MaximumHunger * Definition.resourceFloor
                || vitals.Thirst - seconds * tuning.ThirstDepletionPerSecond <= tuning.MaximumThirst * Definition.resourceFloor
                || vitals.Reserve - seconds * tuning.ReserveDepletionPerSecond <= tuning.MaximumReserve * Definition.resourceFloor)
            { reason = "Supplies too low for this return"; return false; }
            reason = "Shared return route ready"; return true;
        }

        private void Refresh()
        {
            var ready = TryQuote(out _, out var seconds, out var reason);
            if (RegroupRequested)
            {
                if (cargo != null && cargo.IsInAccessRange(motor.Position)) RegroupRequested = false;
                else if (legger == null || !legger.isActiveAndEnabled) { RegroupRequested = false; reason = "Legger is unavailable"; }
                else if (legger.State == TopDown3DBigArmFollower.FollowState.WaitingForRoute)
                    reason = "Legger cannot reach you · Back and move to a shared route";
                else if (legger.State == TopDown3DBigArmFollower.FollowState.WaitingForTerrain)
                    reason = "Legger is waiting for supported ground";
                else reason = "Legger approaching · choose Return when ready";
            }
            if (!ready && reason == "Outside the starting return corridor") reason = RouteGuidance();
            var delta = Definition.position - motor.Position;
            var bearing = Mathf.Repeat(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 360);
            var costs = vitals.Settings;
            var detail = seconds > 0 ? $"~{seconds:0.#} world seconds at {Definition.travelSpeed:0.#}x · shared return\nFood -{seconds * costs.HungerDepletionPerSecond:0.##} · Hydration -{seconds * costs.ThirstDepletionPerSecond:0.##} · Reserve -{seconds * costs.ReserveDepletionPerSecond:0.##}" : "Return follows the short marked starting corridor";
            view.ApplyTextScale(menu != null && menu.LargeText);
            view.Show($"{Definition.displayName} · known · {Vector3.ProjectOnPlane(delta, Vector3.up).magnitude:0} m straight-line · bearing {bearing:0}°\n{detail}\n{reason} · session progress is not saved", true, armed);
            view.SetAvailability(ready, legger != null && legger.isActiveAndEnabled && cargo != null && !cargo.IsInAccessRange(motor.Position), true); view.Focus(eventSystem);
        }

        public string RouteGuidance()
        {
            if (!TopDown3DWaypointRoute.TryClosestPoint(motor.Position, Definition.returnRoute, out var point, out _, out var distance)) return "Return route is unavailable";
            if (Mathf.Abs(motor.Position.y - point.y) > 2.5f) return "Reach the marked route on its ground level";
            var direction = point - motor.Position;
            var bearing = Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 360);
            var compass = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            return $"Marked return route: {distance:0} m {compass[Mathf.RoundToInt(bearing / 45) % 8]}";
        }

        private bool Held()
        {
            foreach (var name in new[] { "UI/Submit", "UI/Click", "Camp/Confirm", "Gameplay/Interact" })
            {
                var action = input.InputActions.FindAction(name, false);
                if (action == null) continue;
                foreach (var control in action.controls) if (control is ButtonControl button && button.isPressed) return true;
            }
            return false;
        }

        private void Update()
        {
            if (Definition == null) return;
            if (view == null) { Close(); Stop("Camp interface is unavailable"); return; }
            if (IsOpen)
            {
                if (!armed && !Held()) { armed = true; module.submit = submit; module.leftClick = click; }
                if (input.CampCancel) { Close(); return; }
                if (Time.unscaledTime >= refreshAt) { refreshAt = Time.unscaledTime + .4f; Refresh(); }
            }
            if (!Traveling) return;
            if (Site == null || !motor.isActiveAndEnabled || !motor.IsActionConstrained)
            { Stop("Camp or movement authority is unavailable"); return; }
            journeySeconds += Time.deltaTime;
            stallSeconds += Time.deltaTime;
            if (HasMeaningfulProgress(lastPosition, motor.Position)) { stallSeconds = 0; lastPosition = motor.Position; }
            if (input.CampCancel) Stop("Return canceled");
            else if (journeySeconds > 90 || stallSeconds > 3) Stop("Return movement is blocked");
            else if (legger == null || !legger.isActiveAndEnabled || legger.DistanceToBooter > 9
                || legger.State == TopDown3DBigArmFollower.FollowState.WaitingForRoute || legger.State == TopDown3DBigArmFollower.FollowState.WaitingForTerrain) Stop("Legger cannot continue this route");
            else if (vitals.Hunger <= vitals.Settings.MaximumHunger * Definition.resourceFloor
                || vitals.Thirst <= vitals.Settings.MaximumThirst * Definition.resourceFloor
                || vitals.Reserve <= vitals.Settings.MaximumReserve * Definition.resourceFloor) Stop("Supplies getting low");
            else view.Show($"Returning to {Definition.displayName} · {Definition.travelSpeed:0.#}x · {Vector3.ProjectOnPlane(motor.Position - Definition.returnRoute[0], Vector3.up).magnitude:0} m to approach\n[{input.GetBindingDisplayName("Camp/Cancel", "Cancel")}] Cancel · the Legger follows physically", false);
        }

        public void BeginReturn()
        {
            if (!IsOpen || !armed || vitals.Health <= 0) return;
            if (!TryQuote(out var path, out _, out _)) { Refresh(); return; }
            RestoreUi(); IsOpen = false; motor.EndActionConstraint(this);
            if (!motor.TryBeginWaypointTravel(this, path, Definition.walkingSpeed)) { input.EnterGameplayMode(); view.Summary("Return route is no longer valid"); return; }
            if (!speed.TryAcquire(this, Definition.travelSpeed)) { motor.EndWaypointTravel(this); input.EnterGameplayMode(); view.Summary("World speed is unavailable"); return; }
            Traveling = true; journeySeconds = stallSeconds = 0; lastPosition = motor.Position;
            input.EnterCampMode(TopDown3DInputMode.CampBusy);
        }

        public void RequestRegroup()
        {
            if (!IsOpen || !armed || legger == null || cargo == null || cargo.IsInAccessRange(motor.Position)) return;
            if (!RegroupRequested) { RegroupRequested = true; legger.RequestCallOver(); }
            armed = false; module.submit = null; module.leftClick = null; Refresh();
        }
        private void Regroup() => RequestRegroup();
        public static bool HasMeaningfulProgress(Vector3 previous, Vector3 current)
            => Vector3.ProjectOnPlane(current - previous, Vector3.up).sqrMagnitude >= .01f;
        private void RestoreUi()
        { if (!leased) return; leased = false; if (module != null) { module.submit = submit; module.leftClick = click; module.cancel = cancel; } if (eventSystem != null) eventSystem.SetSelectedGameObject(null); }
        public void Close()
        { if (!IsOpen) return; IsOpen = false; RestoreUi(); motor.EndActionConstraint(this); if (view != null) view.Hide(); if (input.IsCampMode) input.EnterGameplayMode(); }
        private void Stop(string reason)
        {
            if (!Traveling) return; Traveling = false; LastTravelResult = reason; speed.Release(this); motor.EndWaypointTravel(this);
            if (input.IsCampMode) input.EnterGameplayMode(); if (view != null) view.Summary(reason);
        }
        private void Arrival(bool success) => Stop(success ? "Starting Camp approach reached · services require local interaction" : "Return route became blocked");
        private void Damage() { Close(); Stop("Damage interrupted return"); }
        private void Reset() { Close(); Stop("Return interrupted"); }
        private void SpeedLost() => Stop("World speed interrupted return");
        private void ModeChanged(TopDown3DInputMode mode)
        { if (input.IsCampMode) return; if (IsOpen) { IsOpen = false; RestoreUi(); motor.EndActionConstraint(this); if (view != null) view.Hide(); } Stop("Return interrupted by menu"); }
        private void OnApplicationFocus(bool focused) { if (!focused) Reset(); }
        private void DeviceChanged(InputDevice device, InputDeviceChange change)
        { if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected) Reset(); }
        private void OnDisable() { if (Definition != null) Reset(); }
        private void OnDestroy()
        {
            if (Definition == null) return; Reset();
            motor.WaypointTravelFinished -= Arrival; vitals.HealthReduced -= Damage; vitals.ResetPerformed -= Reset;
            speed.Invalidated -= SpeedLost; input.ModeChanged -= ModeChanged; InputSystem.onDeviceChange -= DeviceChanged;
            if (Site != null) DestroyOwned(Site.gameObject); if (view != null) DestroyOwned(view.gameObject);
        }
        private static void DestroyOwned(UnityEngine.Object target)
        { if (Application.isPlaying) Destroy(target); else DestroyImmediate(target); }
    }
}
