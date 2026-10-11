using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DCampView : MonoBehaviour, ITopDown3DInteractable
    {
        private TopDown3DCampController controller;
        private TopDown3DCampSettings settings;
        public TopDown3DCampRecord Record { get; private set; }
        public Object UnityObject => this;
        public string StableId => Record?.id ?? string.Empty;
        public string Prompt => "Camp · rest, sleep, or pack up";
        public Vector3 InteractionPoint => transform.TransformPoint(new Vector3(0, .8f, .7f));
        public float InteractionRange => settings != null ? settings.interactionRange : 0;
        public float ActionDuration => 0;
        public TopDown3DItemAmount Reward => default;
        public bool IsAvailable => controller != null && controller.Phase == TopDown3DCampPhase.Idle;
        public bool TryConsume(out int remainingUses) { remainingUses = 1; return false; }
        public void Configure(TopDown3DCampController owner, TopDown3DCampSettings config, TopDown3DCampRecord record)
        { controller = owner; settings = config; Record = record; }
        public bool Interact() => controller != null && controller.OpenActions(this);
    }
}
