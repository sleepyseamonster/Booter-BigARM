using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundActionKind
    { Preview, Mining, Crouch, Slide, Strafe, Ladder, Throw, Punch, Kick, Jump, Fall, Hurt, Death, Pickup, Push, Button, Chair, GroundRest, Sleep, Mount, Corridor, Reset }

    [DisallowMultipleComponent]
    public sealed class TopDown3DAnimationPlaygroundTarget : MonoBehaviour, ITopDown3DStationInteraction, ITopDown3DTargetEpoch, ITopDown3DTargetColliderOwner
    {
        [SerializeField] private string stableId;
        [SerializeField] private AnimationPlaygroundActionKind kind;
        [SerializeField] private string prompt = "Test animation";
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(.1f)] private float interactionRange=3f;
        [SerializeField] private Collider[] ownedColliders=Array.Empty<Collider>();
        [SerializeField] private bool available=true;
        public UnityEngine.Object UnityObject => this;
        public string StableId => stableId;
        public AnimationPlaygroundActionKind ActionKind => kind;
        public string Prompt => prompt;
        public Vector3 InteractionPoint => interactionPoint != null ? interactionPoint.position : transform.position;
        public float InteractionRange => interactionRange;
        public bool IsAvailable => isActiveAndEnabled && available && !string.IsNullOrWhiteSpace(stableId);
        public uint Epoch { get; private set; }
        public void Configure(string id, AnimationPlaygroundActionKind action, string label, Transform point, float range, Collider[] colliders)
        {
            if(string.IsNullOrWhiteSpace(id) || !float.IsFinite(range) || range <= 0) throw new ArgumentException("Station identity/range is required.");
            Invalidate(); stableId=id;kind=action;prompt=label;interactionPoint=point;interactionRange=range;ownedColliders=colliders ?? Array.Empty<Collider>();
        }
        public bool OwnsCollider(Collider collider)
        {
            foreach(var owned in ownedColliders) if(owned != null && owned == collider) return true;
            return false;
        }
        public void SetAvailable(bool value) { if(available != value){Invalidate();available=value;} }
        public void SetPrompt(string value)
        {if(string.IsNullOrWhiteSpace(value))throw new ArgumentException("A target prompt is required.");prompt=value;}
        public void Invalidate() => Epoch=unchecked(Epoch+1);
        private void OnDisable() => Invalidate();
    }
}
