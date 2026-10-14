using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DAnimationPlaygroundSession
    {
        [SerializeField] private TopDown3DAnimationPlaygroundStation[] stations = Array.Empty<TopDown3DAnimationPlaygroundStation>();
        public TopDown3DAnimationPlaygroundStation[] Stations => (TopDown3DAnimationPlaygroundStation[])stations.Clone();
        public void ConfigureStations(TopDown3DAnimationPlaygroundStation[] values)
        {
            if (servicesBound) throw new InvalidOperationException("Author stations before player startup.");
            stations = values == null ? Array.Empty<TopDown3DAnimationPlaygroundStation>() : (TopDown3DAnimationPlaygroundStation[])values.Clone();
        }
        private void BindStations()
        {
            if (stations.Length == 0) return; // A player-only checkpoint remains usable before LAB1.
            if (stations.Length != 12) throw new InvalidOperationException("Install all twelve authored station shells together.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var station in stations)
            {
                if (station == null || !station.transform.IsChildOf(transform) || !ids.Add(station.StationId))
                    throw new InvalidOperationException("Stations require unique owned IDs in the session hierarchy.");
                foreach (var prop in station.Props)
                    if (prop == null || !ids.Add("Prop." + prop.StableId)) throw new InvalidOperationException("Station props require unique authored identities.");
            }
            var camera = TopDown3DGameHudCanvas.FindInScene<Camera>(gameObject.scene);
            foreach (var station in stations) station.Bind(this, camera);
        }
        private void UnbindStations() { foreach (var station in stations) if (station != null) station.Unbind(); }
        private bool TryDispatchStation(TopDown3DInteractionTarget capture,out bool handled)
        {
            handled=false;
            if (!servicesBound || !PreviewReady || !capture.IsValid || capture.Object is not TopDown3DAnimationPlaygroundTarget target) return false;
            TopDown3DAnimationPlaygroundStation station = null;
            foreach (var value in stations) if (value != null && value.Owns(target)) { station = value; break; }
            if (station == null) return false;
            handled=true;
            if (target.ActionKind == AnimationPlaygroundActionKind.Preview)
            { menu.OpenAnimationPaletteForStation(station.StationId == "AP01" ? null : station.StationId); return menu.IsOpen; }
            if (target.ActionKind == AnimationPlaygroundActionKind.Reset) { ResetSession(); return ResetRegistry.LastFailure == null; }
            if(station.StationId=="AP03" && target.ActionKind==AnimationPlaygroundActionKind.Crouch && CrouchReady)
            {
                if(crouch.TryToggle())return true;
                playerAction.ReportFeedback(crouch.LastFailure);return false;
            }
            if(station.StationId=="AP03" && target.ActionKind==AnimationPlaygroundActionKind.Slide && SlideReady)
            {
                if(lowMotion.TrySlide())return true;
                playerAction.ReportFeedback(lowMotion.LastFailure);return false;
            }
            if(station.StationId=="AP04" && JumpReady)
            {
                if(target.ActionKind==AnimationPlaygroundActionKind.Jump)
                {
                    if(airborne.TryJump())return true;
                    playerAction.ReportFeedback(airborne.LastFailure);return false;
                }
                if(target.ActionKind==AnimationPlaygroundActionKind.Fall)
                {playerAction.ReportFeedback("Walk off the drop platform to test falling and landing. Use the reset terminal to return.");return true;}
            }
            playerAction.ReportFeedback(station.Title + " unavailable: " + station.MissingGate);
            return false;
        }
    }
}
