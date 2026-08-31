using Unity.Profiling;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DResourceNodeDecorator
    {
        private static readonly ProfilerMarker DecorateMarker =
            new ProfilerMarker("TopDown3D.World.DecorateResourceNodes");

        internal static void Decorate(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectChunkPlan plan,
            TopDown3DResourceWorldState worldState)
        {
            using (DecorateMarker.Auto())
            {
                if (chunk == null || settings == null || plan == null || worldState == null
                    || settings.ResourceCatalog == null || settings.NaturalObjectCatalog == null)
                {
                    return;
                }

                for (var i = 0; i < plan.InteractiveResourcePlacements.Count; i++)
                {
                    var placement = plan.InteractiveResourcePlacements[i];
                    if (!settings.ResourceCatalog.TryGetDefinition(placement.ResourceId, out var definition)
                        || definition == null
                        || !definition.TryValidate(settings.NaturalObjectCatalog, out _))
                    {
                        continue;
                    }

                    var family = settings.NaturalObjectCatalog.GetRequiredMeshFamily(
                        definition.MeshShape,
                        definition.MeshVariant);
                    var nodeObject = new GameObject($"Ironstone Node - {placement.StableId}");
                    nodeObject.transform.SetParent(chunk.DecorationRoot, true);
                    nodeObject.transform.SetPositionAndRotation(placement.Position, placement.Rotation);
                    nodeObject.transform.localScale = placement.Scale;
                    var interactionCollider = nodeObject.AddComponent<BoxCollider>();
                    interactionCollider.center = family.ColliderCenter;
                    interactionCollider.size = family.ColliderSize;
                    var renderers = TopDown3DNaturalObjectDecorator.CreateMemberLods(
                        nodeObject,
                        family,
                        definition.ActiveMaterial);
                    var node = nodeObject.AddComponent<TopDown3DIronstoneNode>();
                    node.Configure(placement, definition, worldState, renderers, interactionCollider);
                }
            }
        }
    }
}
