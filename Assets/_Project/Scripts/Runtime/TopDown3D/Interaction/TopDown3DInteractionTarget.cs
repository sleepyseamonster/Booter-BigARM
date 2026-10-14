using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public interface ITopDown3DInteractionTarget
    {
        UnityEngine.Object UnityObject { get; }
        string StableId { get; }
        string Prompt { get; }
        Vector3 InteractionPoint { get; }
        float InteractionRange { get; }
        bool IsAvailable { get; }
    }
    public interface ITopDown3DTargetEpoch { uint Epoch { get; } }
    public interface ITopDown3DTargetColliderOwner { bool OwnsCollider(Collider collider); }
    public interface ITopDown3DStationInteraction : ITopDown3DInteractionTarget
    {
        AnimationPlaygroundActionKind ActionKind { get; }
    }
    public interface ITopDown3DStationActionReceiver
    {
        bool IsBusy { get; }
        bool TryBegin(TopDown3DInteractionTarget target);
    }

    public readonly struct TopDown3DInteractionTarget
    {
        public readonly ITopDown3DInteractionTarget Target;
        public readonly UnityEngine.Object Object;
        public readonly string StableId;
        public readonly uint Epoch;
        public TopDown3DInteractionTarget(ITopDown3DInteractionTarget target)
        {
            Target=target; Object=target?.UnityObject; StableId=target?.StableId;
            Epoch=target is ITopDown3DTargetEpoch versioned ? versioned.Epoch : 0;
        }
        public bool IsValid => Target != null && Object != null && Target.UnityObject == Object
            && string.Equals(Target.StableId,StableId,StringComparison.Ordinal) && Target.IsAvailable
            && (!(Object is Behaviour behaviour) || behaviour.isActiveAndEnabled)
            && (!(Target is ITopDown3DTargetEpoch versioned) || versioned.Epoch == Epoch);
        public static bool OwnsCollider(ITopDown3DInteractionTarget target, Collider collider)
        {
            if(target?.UnityObject == null || collider == null) return false;
            if(target is ITopDown3DTargetColliderOwner owner) return owner.OwnsCollider(collider);
            return target.UnityObject is Component component && collider.transform.IsChildOf(component.transform);
        }
    }

    public readonly struct TopDown3DActivitySnapshot
    {
        public readonly bool LegacyWork, Placement, MotorOwned, CampActive, StationActive;
        public TopDown3DActivitySnapshot(bool legacyWork, bool placement, bool motorOwned, bool campActive, bool stationActive)
        { LegacyWork=legacyWork; Placement=placement; MotorOwned=motorOwned; CampActive=campActive; StationActive=stationActive; }
        public bool IsBusy => LegacyWork || Placement || MotorOwned || CampActive || StationActive;
    }
}
