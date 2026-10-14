using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundResetStage { HeldAndSpawnedProps, StationState, Actor, Input }

    public interface IAnimationPlaygroundResetParticipant
    {
        string ResetId { get; }
        AnimationPlaygroundResetStage ResetStage { get; }
        void InvalidateForReset(uint epoch);
        bool TryRestoreForReset(out string reason);
    }

    // Registration is explicit. The registry never discovers or deletes arbitrary scene bodies.
    public sealed class AnimationPlaygroundResetRegistry
    {
        private readonly List<IAnimationPlaygroundResetParticipant> participants = new();
        private bool resetting;
        public uint Epoch { get; private set; }
        public string LastFailure { get; private set; }

        public void Register(IAnimationPlaygroundResetParticipant participant)
        {
            if (resetting) throw new InvalidOperationException("Reset registration cannot change during reset.");
            if (!Alive(participant) || string.IsNullOrWhiteSpace(participant.ResetId))
                throw new ArgumentException("Reset participants require a stable authored ID.");
            participants.RemoveAll(value => !Alive(value));
            foreach (var value in participants)
            {
                if (ReferenceEquals(value, participant)) return;
                if (value.ResetId == participant.ResetId) throw new InvalidOperationException("Duplicate reset ID: " + participant.ResetId);
            }
            participants.Add(participant);
        }

        public void Unregister(IAnimationPlaygroundResetParticipant participant)
        {
            if (resetting) throw new InvalidOperationException("Reset registration cannot change during reset.");
            participants.Remove(participant);
        }

        public bool TryReset()
        {
            if (resetting) return false;
            resetting = true;
            try
            {
                InvalidateAll();
                if (LastFailure != null) return false;
                var ordered = participants.FindAll(Alive);
                ordered.Sort((a, b) =>
                {
                    var order = a.ResetStage.CompareTo(b.ResetStage);
                    return order != 0 ? order : string.CompareOrdinal(a.ResetId, b.ResetId);
                });
                foreach (var value in ordered)
                {
                    try
                    {
                        if (value.TryRestoreForReset(out var reason)) continue;
                        LastFailure = value.ResetId + ": " + (reason ?? "Reset is blocked.");
                    }
                    catch (Exception error) { LastFailure = value.ResetId + ": " + error.Message; }
                    return false; // Input remains disarmed when props or safe actor return fail.
                }
                return true;
            }
            finally { resetting = false; }
        }

        public void CancelAll()
        {
            if (resetting) return;
            resetting = true;
            try { InvalidateAll(); }
            finally { resetting = false; }
        }

        private void InvalidateAll()
        {
            Epoch = unchecked(Epoch + 1);
            LastFailure = null;
            // Invalidate every owner first, even if another cancellation failed.
            foreach (var value in participants)
            {
                if (!Alive(value)) continue;
                try { value.InvalidateForReset(Epoch); }
                catch (Exception error) { LastFailure ??= value.ResetId + ": " + error.Message; }
            }
        }
        private static bool Alive(IAnimationPlaygroundResetParticipant value)
            => value != null && (!(value is UnityEngine.Object unityObject) || unityObject != null);
    }
}
