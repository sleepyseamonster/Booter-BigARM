using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DefaultExecutionOrder(900), DisallowMultipleComponent]
    public sealed class TopDown3DLoadoutPresentation : MonoBehaviour
    {
        private TopDown3DPlayerInventory inventory;
        private TopDown3DPlayerAnimationDriver animation;
        public void Configure(TopDown3DPlayerInventory owner)
        { inventory = owner; animation = owner.GetComponent<TopDown3DPlayerAnimationDriver>(); }
        private void LateUpdate()
        { if (inventory != null && animation != null && inventory.State.Policy.IsLoadout) animation.SynchronizeLoadoutCloak(!inventory.State.Slots[4].IsEmpty); }
    }
}
