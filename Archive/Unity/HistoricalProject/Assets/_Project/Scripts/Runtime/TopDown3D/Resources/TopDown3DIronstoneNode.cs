using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DIronstoneNode : MonoBehaviour, ITopDown3DInteractable
    {
        private TopDown3DResourceNodePlacement placement;
        private TopDown3DResourceDefinition definition;
        private TopDown3DResourceWorldState worldState;
        private Renderer[] renderers;
        private Collider interactionCollider;

        public string StableId => placement.StableId;
        public TopDown3DResourceDefinition Definition => definition;
        public Object UnityObject => this;
        public string Prompt => definition != null ? definition.Prompt : string.Empty;
        public Vector3 InteractionPoint => interactionCollider != null
            ? interactionCollider.bounds.center
            : transform.position;
        public float InteractionRange => definition != null ? definition.InteractionRange : 0f;
        public float ActionDuration => definition != null ? definition.ActionDuration : 0f;
        public TopDown3DItemAmount Reward => definition != null
            ? new TopDown3DItemAmount(definition.YieldedItem.ItemId, definition.YieldQuantity)
            : default;
        public bool IsAvailable => !IsDepleted && isActiveAndEnabled;
        public bool IsDepleted => definition != null
            && (worldState == null
                || worldState.GetRemainingUses(StableId, definition.MaximumUses) <= 0);

        internal void Configure(
            TopDown3DResourceNodePlacement authoredPlacement,
            TopDown3DResourceDefinition authoredDefinition,
            TopDown3DResourceWorldState state,
            Renderer[] authoredRenderers,
            Collider authoredInteractionCollider)
        {
            if (worldState != null)
            {
                worldState.Changed -= HandleWorldStateChanged;
            }

            placement = authoredPlacement;
            definition = authoredDefinition;
            worldState = state;
            renderers = authoredRenderers;
            interactionCollider = authoredInteractionCollider;
            if (worldState != null)
            {
                worldState.Changed += HandleWorldStateChanged;
            }

            RefreshPresentation();
        }

        public bool TryConsume(out int remainingUses)
        {
            remainingUses = definition != null && worldState != null
                ? worldState.GetRemainingUses(StableId, definition.MaximumUses)
                : 0;
            if (definition == null || worldState == null
                || !worldState.TryConsume(StableId, definition.MaximumUses, out remainingUses))
            {
                return false;
            }

            RefreshPresentation();
            return true;
        }

        private void HandleWorldStateChanged(string changedStableId, int remainingUses)
        {
            if (changedStableId == StableId)
            {
                RefreshPresentation();
            }
        }

        private void RefreshPresentation()
        {
            if (definition == null)
            {
                return;
            }

            var depleted = IsDepleted;
            var material = depleted ? definition.DepletedMaterial : definition.ActiveMaterial;
            if (renderers != null)
            {
                for (var i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].sharedMaterial = material;
                    }
                }
            }

            if (interactionCollider != null)
            {
                interactionCollider.enabled = !depleted;
            }
        }

        private void OnDestroy()
        {
            if (worldState != null)
            {
                worldState.Changed -= HandleWorldStateChanged;
            }
        }
    }
}
