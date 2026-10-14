using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public interface ITopDown3DInteractable : ITopDown3DInteractionTarget
    {
        float ActionDuration { get; }
        TopDown3DItemAmount Reward { get; }
        bool TryConsume(out int remainingUses);
    }
}
