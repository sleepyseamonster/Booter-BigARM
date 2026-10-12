using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Serialized authoring links; the world owner retains depletion when this view is recreated.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TopDown3DIronstoneNode))]
    public sealed class TopDown3DAuthoredIronstoneNode : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private TopDown3DResourceDefinition definition;
        [SerializeField] private TopDown3DResourceWorldState worldState;
        [SerializeField] private Collider obstacle;
        [SerializeField] private Renderer[] renderers;

        public string StableId => stableId;
        public TopDown3DResourceWorldState WorldState => worldState;
        public TopDown3DResourceDefinition Definition => definition;
        public Collider Obstacle => obstacle;

        public void Configure(string id, TopDown3DResourceDefinition resource,
            TopDown3DResourceWorldState state, Collider physicalObstacle, Renderer[] lodRenderers)
        {
            stableId = id;
            definition = resource;
            worldState = state;
            obstacle = physicalObstacle;
            renderers = lodRenderers;
            if (!TryInitialize(out var error)) throw new ArgumentException(error);
        }

        public bool TryInitialize(out string error)
        {
            var node = GetComponent<TopDown3DIronstoneNode>();
            node.enabled = false;
            if (string.IsNullOrWhiteSpace(stableId) || definition == null
                || definition.YieldedItem == null || worldState == null
                || worldState.gameObject.scene != gameObject.scene
                || obstacle == null || obstacle.isTrigger || !obstacle.enabled
                || renderers == null || renderers.Length == 0
                || Array.Exists(renderers, renderer => renderer == null))
            {
                error = "Authored ironstone needs a stable ID, resource, same-scene state, obstacle and renderers.";
                return false;
            }

            try
            {
                var placement = new TopDown3DResourceNodePlacement(stableId, definition.ResourceId,
                    Vector2Int.zero, transform.position, transform.rotation, transform.localScale, 0, 0);
                node.Configure(placement, definition, worldState, renderers, obstacle);
                node.enabled = isActiveAndEnabled;
                error = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private void OnEnable()
        {
            if (!TryInitialize(out var error)) Debug.LogWarning(error, this);
        }

        private void OnDisable() => GetComponent<TopDown3DIronstoneNode>().enabled = false;
    }
}
