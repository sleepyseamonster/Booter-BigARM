using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DCampSite : MonoBehaviour
    {
        [SerializeField] private TopDown3DWaypointCampSettings definition;
        [SerializeField] private TopDown3DIronstoneProcessor processor;
        public TopDown3DIronstoneProcessor Processor => processor;
        public TopDown3DCampRegistry Registry { get; private set; }
        public string WorldId => definition != null ? definition.worldId : null;
        public string SiteId => definition != null ? definition.siteId : null;
        public string FailureReason { get; private set; }
        internal void Configure(TopDown3DWaypointCampSettings value, TopDown3DIronstoneProcessor station) { definition = value; processor = station; }
        internal bool Bind(TopDown3DCampLoopEconomySession session)
        {
            if (Registry?.Current == true) return true;
            Registry?.Invalidate();
            if (!isActiveAndEnabled || definition == null || !definition.TryValidate(out _) || processor == null || !processor.isActiveAndEnabled || !processor.Output.isActiveAndEnabled
                || processor.gameObject.scene != gameObject.scene || !GreaterWastelandSceneScope.IsCampLoop(gameObject.scene)
                || processor.Recipe == null || !processor.Recipe.TryFingerprint(session.Kit.ItemCatalog, out _, out _))
            { FailureReason = "Camp site or sorting recipe binding is invalid."; return false; }
            var waypoint = session.Kit.GetComponent<TopDown3DWaypointCampController>();
            if (waypoint != null && waypoint.Definition != null && (waypoint.Registry.WorldId != WorldId || waypoint.Registry.SiteId != SiteId))
            { FailureReason = "Camp site does not match waypoint identity."; return false; }
            if (!processor.Output.Bind(session, processor, WorldId, SiteId)) { FailureReason = processor.Output.FailureReason; return false; }
            var companion = session.Cargo.GetComponent<TopDown3DBigArmState>();
            if (companion == null || string.IsNullOrWhiteSpace(companion.CompanionId)) { FailureReason = "Camp cargo has no companion identity."; return false; }
            try
            {
                var registry = new TopDown3DCampRegistry(gameObject.scene, WorldId, SiteId);
                var actor = session.Kit.transform;
                var output = processor.Output;
                registry.Add(new TopDown3DCampStoreBinding("booter.kit", "BOOTER", session.Kit, session.Kit.State, true, () => session.Kit.isActiveAndEnabled, () => session.Kit.State));
                registry.Add(new TopDown3DCampStoreBinding(companion.CompanionId, "LEGGER", session.Cargo, session.Cargo.State, true, () => TopDown3DCargoAccess.CanAccess(session.Cargo, actor), () => session.Cargo.State));
                registry.Add(new TopDown3DCampStoreBinding(session.Cache.StableId, "CAMP CHEST", session.Cache, session.Cache.State, true, () => session.Cache.CanAccess(actor), () => session.Cache.State));
                registry.Add(new TopDown3DCampStoreBinding(output.StableId, "SORTER OUTPUT", output, output.State, false,
                    () => processor != null && processor.CanAccess(actor), () => output.State));
                registry.Add(new TopDown3DCampStationAdapter(session.Workbench.StableId, "WORKBENCH", session.Workbench, session.Workbench.CanAccess));
                registry.Add(new TopDown3DCampStationAdapter(session.Refinery.StableId, "ARC DUST REFINERY", session.Refinery, session.Refinery.CanAccess));
                registry.Add(new TopDown3DCampStationAdapter(processor.StableId, "IRONSTONE SORTER", processor, processor.CanAccess));
                Registry = registry; FailureReason = null; return true;
            }
            catch (ArgumentException error) { FailureReason = error.Message; return false; }
        }
        private void OnDisable() => Registry?.Invalidate();
        internal void Invalidate() => Registry?.Invalidate();
    }
}
