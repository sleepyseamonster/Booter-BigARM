using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Approved authored compositions. Selection is derived only from stable world identity;
    /// the catalog owns no geographic density or placement decisions.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Booter & BigARM/Top Down 3D/Authored Formation Catalog",
        fileName = "TopDown3DAuthoredFormationCatalog")]
    public sealed class TopDown3DAuthoredFormationCatalog : ScriptableObject
    {
        [SerializeField] private TopDown3DAuthoredFormationAsset[] templates =
            Array.Empty<TopDown3DAuthoredFormationAsset>();

        public IReadOnlyList<TopDown3DAuthoredFormationAsset> Templates =>
            Array.AsReadOnly(templates);

        public bool IsComplete
        {
            get
            {
                if (templates.Length == 0) return false;
                var sourceGuids = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < templates.Length; i++)
                {
                    var template = templates[i];
                    if (template == null || !template.HasBakedVariants || !template.HasApprovedStage
                        || string.IsNullOrWhiteSpace(template.SourceGuid)
                        || !sourceGuids.Add(template.SourceGuid))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public TopDown3DAuthoredFormationAsset Select(string stableReservationId)
        {
            if (string.IsNullOrWhiteSpace(stableReservationId))
                throw new ArgumentException("Formation selection requires a stable reservation identity.",
                    nameof(stableReservationId));
            if (!IsComplete)
                throw new InvalidOperationException("The authored formation catalog is incomplete.");

            unchecked
            {
                uint hash = 2166136261u;
                for (var i = 0; i < stableReservationId.Length; i++)
                    hash = (hash ^ stableReservationId[i]) * 16777619u;
                return templates[(int)(hash % (uint)templates.Length)];
            }
        }

        internal void Configure(TopDown3DAuthoredFormationAsset[] approvedTemplates)
        {
            if (approvedTemplates == null) throw new ArgumentNullException(nameof(approvedTemplates));
            templates = (TopDown3DAuthoredFormationAsset[])approvedTemplates.Clone();
        }
    }
}
