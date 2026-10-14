using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DAnimationPlaygroundStation : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        [SerializeField] private string stationId, title, missingGate;
        [SerializeField] private Vector2 footprint = new(28, 24);
        [SerializeField] private Transform entry, safeReturn;
        [SerializeField] private TextMesh sign;
        [SerializeField] private TopDown3DAnimationPlaygroundTarget[] targets = Array.Empty<TopDown3DAnimationPlaygroundTarget>();
        [SerializeField] private AnimationPlaygroundProp[] props = Array.Empty<AnimationPlaygroundProp>();
        private TopDown3DAnimationPlaygroundSession session;
        private AnimationPlaygroundResetRegistry registry;
        private Transform initialParent;
        private Vector3 initialPosition;
        private Vector3 initialScale;
        private Quaternion initialRotation;
        private float nextSignUpdate;
        private Camera viewCamera;
        public string StationId => stationId;
        public string Title => title;
        public string MissingGate => missingGate;
        public Vector2 Footprint => footprint;
        public Transform Entry => entry;
        public Transform SafeReturn => safeReturn;
        public TopDown3DAnimationPlaygroundTarget[] Targets => (TopDown3DAnimationPlaygroundTarget[])targets.Clone();
        public AnimationPlaygroundProp[] Props => (AnimationPlaygroundProp[])props.Clone();
        public string ResetId => "CampLoop.Station." + stationId;
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.StationState;
        public uint Epoch { get; private set; }
        public string Status { get; private set; }

        public void Configure(string id, string label, string gap, Vector2 size, Transform entrance, Transform returnAnchor,
            TextMesh statusSign, TopDown3DAnimationPlaygroundTarget[] ownedTargets, AnimationPlaygroundProp[] ownedProps)
        {
            if (registry != null || !IsStationId(id) || !float.IsFinite(size.x) || !float.IsFinite(size.y) || size.x <= 0 || size.y <= 0
                || entrance == null || returnAnchor == null || statusSign == null || ownedTargets == null || ownedProps == null)
                throw new ArgumentException("Author a bounded station with identity, anchors, sign and owned objects.");
            stationId = id; title = label; missingGate = gap; footprint = size; entry = entrance; safeReturn = returnAnchor;
            sign = statusSign; targets = (TopDown3DAnimationPlaygroundTarget[])ownedTargets.Clone(); props = (AnimationPlaygroundProp[])ownedProps.Clone();
        }
        public static bool IsStationId(string id) => id != null && id.Length == 4 && id.StartsWith("AP", StringComparison.Ordinal)
            && int.TryParse(id.Substring(2), out var number) && number >= 1 && number <= 12;
        public void Bind(TopDown3DAnimationPlaygroundSession owner, Camera camera)
        {
            if (owner == null || !owner.IsLab || owner.gameObject.scene != gameObject.scene) throw new ArgumentException("Bind stations only to their authored lab session.");
            if (session == owner) return;
            if (registry != null) throw new InvalidOperationException("A station cannot belong to two sessions.");
            registry = owner.ResetRegistry; session = owner; viewCamera = camera;
            initialParent = transform.parent; initialPosition = transform.localPosition; initialRotation = transform.localRotation; initialScale = transform.localScale;
            try
            {
                RefreshActionAvailability();
                foreach (var prop in props) prop.Bind(registry);
                registry.Register(this); owner.ReadinessChanged += RefreshStatus; RefreshStatus();
            }
            catch { Unbind(); throw; }
        }
        private void Update()
        {
            if (session == null || Time.unscaledTime < nextSignUpdate) return;
            nextSignUpdate = Time.unscaledTime + .1f; RefreshStatus();
        }
        private void LateUpdate()
        {
            if (viewCamera == null || sign == null) return;
            sign.transform.rotation = viewCamera.transform.rotation;
        }
        public void RefreshStatus()
        {
            RefreshActionAvailability();
            var preview = 0; var imported = 0; var restricted = 0;
            if (session != null && session.Catalog != null)
                foreach (var entry in session.Catalog.entries)
                {
                    if (entry == null || stationId != "AP01" && entry.stationId != stationId) continue;
                    if (entry.restrictedReference) { restricted++; continue; }
                    var binding = session.Bindings?.Find(entry.sourceId);
                    if (binding != null && binding.readiness >= AnimationPlaygroundMotionReadiness.ImportQualified) imported++;
                    if (binding?.CanPreview == true && session.PreviewReady) preview++;
                }
            var physical=stationId=="AP03" && session?.CrouchReady==true ? "Crouch ready | slide qualification pending" : missingGate;
            if(stationId=="AP04" && session?.JumpReady==true)physical="Jump, fall and landing ready | roll recovery pending";
            Status = session?.Readiness == AnimationPlaygroundReadiness.Failed ? session.UnavailableReason
                : $"Imported {imported} | Preview {preview} | Restricted {restricted}\nPhysical: {physical}";
            var text = stationId + "  " + title + "\n" + Status + "\nInspect terminal: variants | Reset terminal: session";
            if (sign != null && sign.text != text) sign.text = text;
        }
        private void RefreshActionAvailability()
        {
            foreach(var target in targets)
                if(stationId=="AP03" && target!=null && target.ActionKind==AnimationPlaygroundActionKind.Crouch)
                {
                    var ready=session?.CrouchReady==true;target.SetAvailable(ready);
                    target.SetPrompt(ready ? "Crouch or stand" : "Crouch unavailable");
                }
                else if(stationId=="AP04" && target!=null && (target.ActionKind==AnimationPlaygroundActionKind.Jump || target.ActionKind==AnimationPlaygroundActionKind.Fall))
                {
                    var ready=session?.JumpReady==true;target.SetAvailable(ready);
                    target.SetPrompt(ready ? target.ActionKind==AnimationPlaygroundActionKind.Jump ? "Jump" : "Falling test instructions" : "Jump and fall unavailable");
                }
        }
        public void InvalidateForReset(uint epoch)
        {
            Epoch = unchecked(Epoch + 1);
            foreach (var target in targets) if (target != null) target.Invalidate();
        }
        public bool TryRestoreForReset(out string reason)
        {
            if (initialParent == null) { reason = "The authored station parent is unavailable."; return false; }
            transform.SetParent(initialParent, false); transform.SetLocalPositionAndRotation(initialPosition, initialRotation); transform.localScale = initialScale;
            RefreshStatus(); reason = null; return true;
        }
        public bool Owns(TopDown3DAnimationPlaygroundTarget target)
        { foreach (var value in targets) if (value == target) return true; return false; }
        public void Unbind()
        {
            if (session != null) session.ReadinessChanged -= RefreshStatus;
            foreach (var prop in props) if (prop != null) prop.Unbind();
            registry?.Unregister(this); registry = null; session = null; viewCamera = null;
            RefreshActionAvailability();
        }
        private void OnDisable() => InvalidateForReset(0);
        private void OnDestroy() => Unbind();
    }
}
