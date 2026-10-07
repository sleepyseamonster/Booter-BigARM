using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public interface ITopDown3DInteractable
    {
        Object UnityObject { get; }
        string StableId { get; }
        string Prompt { get; }
        Vector3 InteractionPoint { get; }
        float InteractionRange { get; }
        float ActionDuration { get; }
        TopDown3DItemAmount Reward { get; }
        bool IsAvailable { get; }
        bool TryConsume(out int remainingUses);
    }
}
