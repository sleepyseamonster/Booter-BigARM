using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;

namespace BooterBigArm.TopDown3D
{
    [DefaultExecutionOrder(200)]
    public sealed class TopDown3DCampController : MonoBehaviour
    {
        public TopDown3DCampPhase Phase { get; private set; }
        public TopDown3DCampRecord Record { get; private set; }
        public TopDown3DCampView Camp { get; private set; }
        public bool IsConfigured => settings != null;
        public TopDown3DCampSettings Settings => settings;
        private TopDown3DCampSettings settings;
        private TopDown3DInputRouter input;
        private TopDown3DPlayerInventory inventory;
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver animation;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DPlayerActionController action;
        private TopDown3DWorldSpeedController speed;
        private TopDown3DCampCanvas canvas;
        private InputSystemUIInputModule module;
        private InputActionReference priorCancel, priorSubmit, priorClick;
        private EventSystem eventSystem;
        private TopDown3DMenuController menu;
        private GameObject preview;
        private Transform worldRoot;
        private LineRenderer footprint;
        private Vector3 placement;
        private Quaternion rotation;
        private float elapsed;
        private bool armed, leased, committing, pendingSleep, startingActivity;
        private string wakeReason;
        private TopDown3DSurvivalSnapshot sleepStart;

        public bool KitCarried
        {
            get
            {
                if (inventory == null) return false;
                foreach (var slot in inventory.State.Slots)
                    if (slot.ItemId == TopDown3DCampSettings.ItemId && slot.Quantity > 0) return true;
                return false;
            }
        }
        public bool CanCallUp => IsConfigured && input != null && input.HasCampActions && Phase == TopDown3DCampPhase.Idle
            && vitals.Health > 0 && motor.CanBeginCampAction && animation.HasCampAnimationSet && !action.IsGathering && !action.IsPlacingHarvester
            && (Camp != null || Record == null && KitCarried);

        public void Configure(TopDown3DCampSettings config, TopDown3DInputRouter router, EventSystem system)
        {
            if (IsConfigured) return;
            settings = config; input = router; eventSystem = system;
            inventory = GetComponent<TopDown3DPlayerInventory>(); motor = GetComponent<TopDown3DPlayerMotor>();
            animation = GetComponent<TopDown3DPlayerAnimationDriver>(); vitals = GetComponent<TopDown3DSurvivalVitals>();
            action = GetComponent<TopDown3DPlayerActionController>();
            var root = new GameObject("Camp World"); SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            worldRoot = root.transform; speed = root.AddComponent<TopDown3DWorldSpeedController>();
            var ui = new GameObject("Camp Canvas", typeof(RectTransform)); SceneManager.MoveGameObjectToScene(ui, gameObject.scene);
            canvas = ui.AddComponent<TopDown3DCampCanvas>(); canvas.Configure(() => BeginRecovery(true), () => BeginRecovery(false), BeginPack);
            module = system.GetComponent<InputSystemUIInputModule>();
            menu = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene);
            vitals.HealthReduced += OnDamage; vitals.ActivityCompleted += OnRecoveryComplete;
            vitals.ResetPerformed += AbortLifecycle;
            input.ModeChanged += ModeChanged; motor.CampApproachFinished += ApproachFinished;
            speed.Invalidated += OnSpeedLost; InputSystem.onDeviceChange += DeviceChanged;
        }

        private void Start()
        {
            if (settings != null && !animation.ConfigureCamp(settings))
                action.ReportFeedback("Camping unavailable: required camp animation or mesh is missing");
        }

        public bool CallUp()
        {
            if (!CanCallUp) { action?.ReportFeedback("Camp unavailable during this action, or no kit carried"); return false; }
            if (Camp != null)
            {
                if (Vector3.Distance(motor.Position, Camp.InteractionPoint) <= settings.interactionRange) return OpenActions(Camp);
                action.ReportFeedback("Camp is already deployed · return to pack it up"); return false;
            }
            if (!motor.TryBeginActionConstraint(this)) return false;
            rotation = TopDown3DCampPlacement.SunFacing(); placement = motor.Position - rotation * settings.sleepOffset;
            preview = Instantiate(settings.deployedPrefab, worldRoot); preview.name = "Camp placement preview";
            foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var outline = new GameObject("Sleeping and exit footprint"); outline.transform.SetParent(preview.transform, false);
            footprint = outline.AddComponent<LineRenderer>(); footprint.useWorldSpace = true; footprint.loop = true;
            footprint.positionCount = 4; footprint.widthMultiplier = .035f;
            footprint.sharedMaterial = settings.previewMaterial;
            Enter(TopDown3DCampPhase.Placement, TopDown3DInputMode.CampPlacement); return true;
        }

        private void Enter(TopDown3DCampPhase phase, TopDown3DInputMode mode)
        { Phase = phase; elapsed = 0; armed = false; input.EnterCampMode(mode); }

        private bool ConfirmIsHeld()
        {
            foreach (var path in new[] { "Camp/Confirm", "UI/Submit", "UI/Click", "Gameplay/Interact" })
            {
                var binding = input.InputActions.FindAction(path, false); if (binding == null) continue;
                foreach (var control in binding.controls) if (control is ButtonControl button && button.isPressed) return true;
            }
            return false;
        }

        private void Update()
        {
            if (settings == null) return;
            canvas.ApplyTextScale(menu != null && menu.LargeText);
            if (!animation.HasCampAnimationSet) animation.ConfigureCamp(settings);
            if (!armed && !ConfirmIsHeld()) armed = true;
            if (Phase == TopDown3DCampPhase.Idle)
            { animation.SetCampPresentation(-1, 1, false, KitCarried); return; }
            if (input.CampCancel && armed) { Cancel("You woke early"); return; }
            elapsed += Time.deltaTime;
            if (Phase == TopDown3DCampPhase.Placement) UpdatePlacement();
            else if (Phase == TopDown3DCampPhase.SettingUp && elapsed >= settings.setupDuration) CommitDeployment();
            else if (Phase == TopDown3DCampPhase.Packing && elapsed >= settings.setupDuration) CommitPacking();
            else if (Phase == TopDown3DCampPhase.Actions) ShowActions();
            else if (Phase == TopDown3DCampPhase.Reclining && elapsed >= settings.poseDuration) StartRecovery();
            else if (Phase == TopDown3DCampPhase.Waking && elapsed >= settings.wakeDuration) FinishWake();
            else if (Phase == TopDown3DCampPhase.Sleeping || Phase == TopDown3DCampPhase.Resting)
            {
                if (Camp == null || Vector3.ProjectOnPlane(motor.Position - Camp.transform.TransformPoint(settings.sleepOffset), Vector3.up).magnitude > 1)
                { Wake("Sleeping space changed"); return; }
                canvas.Show($"{(pendingSleep ? "Sleeping" : "Resting")} {(pendingSleep ? settings.sleepSpeed : 1):0.#}x · Reserve {vitals.Reserve:0}\n"
                    + $"[{input.GetBindingDisplayName("Camp/Cancel", "Wake")}] Wake · the world remains active", false);
            }
        }

        private void UpdatePlacement()
        {
            var cam = Camera.main;
            var move = input.CampMove;
            var forward = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            var delta = (right * move.x + forward * move.y) * (Time.unscaledDeltaTime * 1.8f);
            placement += delta; placement = motor.Position + Vector3.ClampMagnitude(Vector3.ProjectOnPlane(placement - motor.Position, Vector3.up), settings.placementReach);
            if (armed && input.CampRotate != 0) rotation *= Quaternion.Euler(0, input.CampRotate * 15, 0);
            if (armed && input.CampSun) rotation = TopDown3DCampPlacement.SunFacing();
            var valid = TopDown3DCampPlacement.Validate(placement, rotation, settings, transform, null, out var supported, out var reason);
            placement.y = supported.y; preview.transform.SetPositionAndRotation(supported, rotation);
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", valid ? new Color(.4f, .65f, .45f) : new Color(.8f, .35f, .25f));
            foreach (var renderer in preview.GetComponentsInChildren<MeshRenderer>()) renderer.SetPropertyBlock(block);
            var halfWidth = settings.footprint.x * .5f; var depth = settings.footprint.z;
            var points = new[] { new Vector3(-halfWidth, .06f, 0), new Vector3(halfWidth, .06f, 0), new Vector3(halfWidth, .06f, depth), new Vector3(-halfWidth, .06f, depth) };
            for (var i = 0; i < points.Length; i++) footprint.SetPosition(i, supported + rotation * points[i]);
            canvas.Show($"SET UP CAMP · {reason}\n[{input.GetBindingDisplayName("Camp/Confirm", "Enter")}] Place   "
                + $"[{input.GetBindingDisplayName("Camp/Cancel", "Escape")}] Cancel\n"
                + $"Rotate [{input.GetBindingDisplayName("Camp/RotateLeft", "Z")}] / [{input.GetBindingDisplayName("Camp/RotateRight", "C")}] · "
                + $"Face sun [{input.GetBindingDisplayName("Camp/FaceSun", "R")}]", false);
            if (!armed || !input.CampConfirm || !valid) return;
            Enter(TopDown3DCampPhase.SettingUp, TopDown3DInputMode.CampBusy);
            animation.SetCampPresentation(2, settings.setupDuration, false, true);
            canvas.Show("Unfolding the shade…", false);
        }

        private void CommitDeployment()
        {
            if (committing || Record != null) return;
            committing = true; GameObject candidate = null; TopDown3DCampRecord next = null;
            try
            {
                if (!KitCarried || !TopDown3DCampPlacement.Validate(placement, rotation, settings, transform, null, out var ground, out var reason))
                { action.ReportFeedback("Camp placement is no longer valid"); return; }
                next = TopDown3DCampRecord.Create(ground, rotation);
                candidate = Instantiate(settings.deployedPrefab, worldRoot); candidate.SetActive(false);
                candidate.transform.SetPositionAndRotation(ground, rotation);
                var view = candidate.GetComponent<TopDown3DCampView>(); view.Configure(this, settings, next);
                var result = inventory.State.TryRemove(TopDown3DCampSettings.ItemId, 1, () =>
                { if (Record != null) return false; Record = next; Camp = view; return true; });
                if (!result.Succeeded) { Record = null; Camp = null; return; }
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                var committed = next != null && ReferenceEquals(Record, next);
                if (candidate != null) { if (committed) candidate.SetActive(true); else DestroyOwned(candidate); }
                committing = false; DestroyPreview(); EndAction();
                if (committed) action.ReportFeedback("Camp ready · interact to sleep");
            }
        }

        public bool OpenActions(TopDown3DCampView target)
        {
            if (!CanCallUp || target != Camp || Vector3.Distance(motor.Position, target.InteractionPoint) > settings.interactionRange) return false;
            if (!CanReachCamp()) { action.ReportFeedback("Camp is blocked from here"); return false; }
            if (!motor.TryBeginActionConstraint(this)) return false;
            LeaseUi(); Enter(TopDown3DCampPhase.Actions, TopDown3DInputMode.CampActions); ShowActions(); return true;
        }

        private void LeaseUi()
        {
            if (leased) return; leased = true;
            priorCancel = module.cancel; priorSubmit = module.submit; priorClick = module.leftClick;
            module.cancel = null; module.submit = null; module.leftClick = null;
        }
        private void RestoreUi()
        {
            if (!leased) return; leased = false;
            module.cancel = priorCancel; module.submit = priorSubmit; module.leftClick = priorClick;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }
        private void ShowActions()
        {
            var sleep = TopDown3DSleepRules.Quote(vitals, settings, TopDown3DSurvivalActivity.Sleep);
            var rest = TopDown3DSleepRules.Quote(vitals, settings, TopDown3DSurvivalActivity.Rest);
            var canPack = inventory.State.CanAdd(new TopDown3DItemAmount(TopDown3DCampSettings.ItemId, 1));
            var notice = !canPack ? "Pack up unavailable: inventory full" : !sleep.IsUseful
                ? vitals.Reserve >= settings.sleepTarget * vitals.Settings.MaximumReserve ? "Already ready" : "Supplies too low to sleep safely"
                : $"Rest: up to +{rest.Gain:0.#} Reserve";
            if (armed) { module.submit = priorSubmit; module.leftClick = priorClick; }
            canvas.Show($"CAMP · Reserve {vitals.Reserve:0}\nSleep: +{sleep.Gain:0.#} Reserve · ~{sleep.Seconds / settings.sleepSpeed:0}s · "
                + $"Food {sleep.Food:0.#} / Water {sleep.Water:0.#} / Oxygen {sleep.Oxygen:0.#}\n"
                + $"{notice} · [{input.GetBindingDisplayName("Camp/Cancel", "Back")}] Back", true, armed);
            canvas.SetAvailability(sleep.IsUseful, rest.IsUseful, canPack);
            canvas.Focus(eventSystem);
        }

        private void BeginRecovery(bool sleep)
        {
            if (!armed || committing || Phase != TopDown3DCampPhase.Actions || Camp == null) return;
            var quote = TopDown3DSleepRules.Quote(vitals, settings, sleep ? TopDown3DSurvivalActivity.Sleep : TopDown3DSurvivalActivity.Rest);
            if (!quote.IsUseful) return;
            pendingSleep = sleep; RestoreUi(); motor.EndActionConstraint(this);
            var point = Camp.transform.TransformPoint(settings.sleepOffset);
            if (!TopDown3DCampPlacement.TryGround(point, out var grounded, out _)) { EndAction(); return; }
            var capsule = GetComponent<CapsuleCollider>(); point = grounded + Vector3.up * (capsule.height * .5f - capsule.center.y);
            Enter(TopDown3DCampPhase.Approaching, TopDown3DInputMode.CampBusy);
            canvas.Show("Moving into the shade…", false);
            if (!motor.TryBeginCampApproach(this, point, settings.approachTimeout)) { action.ReportFeedback("Cannot reach sleeping space"); EndAction(); }
        }

        private void ApproachFinished(bool success)
        {
            if (Phase != TopDown3DCampPhase.Approaching) return;
            if (!success || !TopDown3DCampPlacement.Validate(Record.position, Record.rotation, settings, transform, Camp.transform, out _, out _))
            { action.ReportFeedback("Sleeping space or approach is blocked"); EndAction(); return; }
            motor.FacePlanarDirection(Camp.transform.forward);
            Enter(TopDown3DCampPhase.Reclining, TopDown3DInputMode.CampBusy);
            animation.SetCampPresentation(pendingSleep ? 0 : 1, settings.poseDuration, pendingSleep, false);
            canvas.Show(pendingSleep ? "Settling beneath the cloak…" : "Taking a breather…", false);
        }

        private void StartRecovery()
        {
            if (Camp == null || !TopDown3DCampPlacement.Validate(Record.position, Record.rotation, settings, transform, Camp.transform, out _, out _))
            { Wake("Camp space changed"); return; }
            var mode = pendingSleep ? TopDown3DSurvivalActivity.Sleep : TopDown3DSurvivalActivity.Rest;
            if (!TopDown3DSleepRules.Quote(vitals, settings, mode).IsUseful) { Wake("Supplies getting low"); return; }
            if (pendingSleep && !speed.TryAcquire(this, settings.sleepSpeed)) { Wake("World speed is unavailable"); return; }
            sleepStart = vitals.CaptureSnapshot();
            Enter(pendingSleep ? TopDown3DCampPhase.Sleeping : TopDown3DCampPhase.Resting, TopDown3DInputMode.CampBusy);
            startingActivity = true; vitals.QueueActivity(mode, settings, Time.timeAsDouble); startingActivity = false;
        }

        private void BeginPack()
        {
            if (!armed || committing || Phase != TopDown3DCampPhase.Actions || Camp == null) return;
            if (!CanReachCamp()) { action.ReportFeedback("Camp is blocked from here"); return; }
            RestoreUi(); Enter(TopDown3DCampPhase.Packing, TopDown3DInputMode.CampBusy);
            animation.SetCampPresentation(2, settings.setupDuration, false, false); canvas.Show("Folding the shade…", false);
        }
        private void CommitPacking()
        {
            if (committing || Camp == null) return; committing = true;
            var previous = Camp; var oldRecord = Record;
            try
            {
                var result = inventory.State.TryAdd(new[] { new TopDown3DItemAmount(TopDown3DCampSettings.ItemId, 1) }, () =>
                {
                    if (Record != oldRecord || Vector3.Distance(motor.Position, previous.InteractionPoint) > settings.interactionRange || !CanReachCamp()) return false;
                    Record = null; Camp = null; return true;
                });
                if (!result.Succeeded) action.ReportFeedback("Cannot pack camp · inventory full or camp out of reach");
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { if (Record == null) DestroyOwned(previous.gameObject); committing = false; EndAction(); }
        }

        public void NotifyDanger(string reason) => Wake(reason);
        private bool CanReachCamp()
        {
            if (Camp == null) return false;
            if (!Physics.Linecast(motor.Position + Vector3.up * .3f, Camp.InteractionPoint, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform.IsChildOf(Camp.transform);
        }
        public void InterruptForSystemMenu() => AbortLifecycle();
        private void OnDamage()
        {
            // An input-side wake may precede this frame's earlier fixed damage tick.
            // Preserve that earlier accounting boundary even after presentation has left sleep.
            if (vitals.Activity != TopDown3DSurvivalActivity.Ordinary)
                vitals.QueueActivity(TopDown3DSurvivalActivity.Ordinary, null, Time.timeAsDouble);
            if (Phase != TopDown3DCampPhase.Idle) Cancel(vitals.Health <= 0 ? "Health depleted" : "Damage taken");
        }
        private void OnRecoveryComplete(string reason) { if (!startingActivity) Wake(reason); }
        private void OnSpeedLost() => Wake("Sleep interrupted");
        private void Wake(string reason)
        {
            if (settings == null || Phase == TopDown3DCampPhase.Idle || Phase == TopDown3DCampPhase.Waking) return;
            speed.Release(this); vitals.QueueActivity(TopDown3DSurvivalActivity.Ordinary, null, Time.timeAsDouble);
            RestoreUi(); DestroyPreview(); wakeReason = reason;
            Enter(TopDown3DCampPhase.Waking, TopDown3DInputMode.CampBusy);
            animation.SetCampPresentation(3, settings.wakeDuration, false, KitCarried);
        }
        private void FinishWake()
        {
            var start = sleepStart; sleepStart = null; EndAction();
            canvas.Summary(start == null ? wakeReason : $"{wakeReason} · +{Mathf.Max(0, vitals.Reserve - start.Reserve):0.#} Reserve\n"
                + $"Food {Mathf.Max(0, start.Hunger - vitals.Hunger):0.#} · Water {Mathf.Max(0, start.Thirst - vitals.Thirst):0.#} · Oxygen {Mathf.Max(0, start.Oxygen - vitals.Oxygen):0.#}");
        }
        public void Cancel(string reason)
        {
            if (Phase == TopDown3DCampPhase.Sleeping || Phase == TopDown3DCampPhase.Resting || Phase == TopDown3DCampPhase.Reclining)
                Wake(reason);
            else { speed?.Release(this); DestroyPreview(); EndAction(); }
        }
        private static void DestroyOwned(UnityEngine.Object value)
        { if (value == null) return; if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        private void DestroyPreview() { if (preview != null) DestroyOwned(preview); preview = null; footprint = null; }
        private void EndAction(bool returnGameplay = true)
        {
            Phase = TopDown3DCampPhase.Idle; RestoreUi(); motor?.EndActionConstraint(this); canvas?.Hide();
            animation?.SetCampPresentation(-1, 1, false, KitCarried);
            if (returnGameplay && input != null && input.IsCampMode) input.EnterGameplayMode();
        }
        private void ModeChanged(TopDown3DInputMode mode)
        {
            if (!IsConfigured || input.IsCampMode || Phase == TopDown3DCampPhase.Idle) return;
            speed.Release(this); vitals.QueueActivity(TopDown3DSurvivalActivity.Ordinary, null, Time.timeAsDouble);
            DestroyPreview(); EndAction(false); sleepStart = null;
        }
        private void DeviceChanged(InputDevice device, InputDeviceChange change)
        { if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected) AbortLifecycle(); }
        private void OnApplicationFocus(bool focused) { if (!focused) AbortLifecycle(); }
        private void AbortLifecycle()
        {
            if (!IsConfigured || Phase == TopDown3DCampPhase.Idle) return;
            speed.Release(this); vitals.QueueActivity(TopDown3DSurvivalActivity.Ordinary, null, Time.timeAsDouble);
            DestroyPreview(); EndAction(); sleepStart = null;
        }
        private void OnDisable() => AbortLifecycle();
        private void OnDestroy()
        {
            if (!IsConfigured) return;
            AbortLifecycle(); vitals.HealthReduced -= OnDamage; vitals.ActivityCompleted -= OnRecoveryComplete;
            vitals.ResetPerformed -= AbortLifecycle;
            input.ModeChanged -= ModeChanged; motor.CampApproachFinished -= ApproachFinished;
            speed.Invalidated -= OnSpeedLost; InputSystem.onDeviceChange -= DeviceChanged;
            if (worldRoot != null) DestroyOwned(worldRoot.gameObject); if (canvas != null) DestroyOwned(canvas.gameObject);
        }
    }
}
