using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DCompassHud : MonoBehaviour
    {
        private TopDown3DActionDpadHud pad;
        private TopDown3DPlayerMotor player;
        private TopDown3DWaypointCampController waypoint;
        private float heading, nextBinding;
        private bool hasHeading, arrived;
        private TopDown3DCampView lastSite;
        public TopDown3DCompassGraphic Graphic { get; private set; }
        public Vector2 LocalCoordinates { get; private set; }

        public static TopDown3DCompassHud TryInstallForScene(Scene scene)
        {
            if (scene.path != TopDown3DWaypointCampSettings.ProductionScene) return null;
            var pad = TopDown3DActionDpadHud.TryInstallForScene(scene);
            return pad != null ? Install(pad) : null;
        }

        public static TopDown3DCompassHud Install(TopDown3DActionDpadHud pad)
        {
            var owner = pad.GetComponent<TopDown3DCompassHud>() ?? pad.gameObject.AddComponent<TopDown3DCompassHud>();
            owner.pad = pad;
            if (owner.Graphic == null)
            {
                var root = new GameObject("Facing compass", typeof(RectTransform)); root.transform.SetParent(pad.transform, false);
                owner.Graphic = root.AddComponent<TopDown3DCompassGraphic>(); owner.Graphic.raycastTarget = false;
                root.transform.SetAsFirstSibling();
            }
            owner.RefreshLayout(); owner.ResolveSources(); owner.RefreshPresentation();
            return owner;
        }

        public void Bind(TopDown3DPlayerMotor motor, TopDown3DWaypointCampController destination)
        {
            if (player != motor) { hasHeading = false; arrived = false; }
            player = motor; waypoint = destination;
            RefreshPresentation();
        }

        private void ResolveSources()
        {
            var motor = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerMotor>(gameObject.scene);
            Bind(motor, motor != null ? motor.GetComponent<TopDown3DWaypointCampController>() : null);
            nextBinding = Time.unscaledTime + .5f;
        }

        private void LateUpdate()
        {
            // Initialization order and actor replacement can leave sources unresolved; never scan every frame.
            if ((player == null || waypoint == null) && Time.unscaledTime >= nextBinding) ResolveSources();
            RefreshLayout(); RefreshPresentation();
        }

        public void RefreshPresentation()
        {
            if (Graphic == null) return;
            if (player == null)
            {
                hasHeading = false; arrived = false; lastSite = null; LocalCoordinates = Vector2.zero;
                Graphic.SetState(heading, false, 0, false, false); return;
            }
            if (TopDown3DCompassNavigation.TryHeading(player.FacingDirection, out var current)) { heading = current; hasHeading = true; }
            var site = waypoint != null ? waypoint.Site : null;
            var definition = waypoint != null ? waypoint.Definition : null;
            var registry = waypoint != null ? waypoint.Registry : null;
            var valid = definition != null && registry != null && registry.Discovered && site != null && site.gameObject.activeInHierarchy
                && registry.WorldId == definition.worldId && registry.SiteId == definition.siteId && site.StableId == registry.SiteId
                && TopDown3DCompassNavigation.ValidHorizontalPosition(player.transform.position)
                && TopDown3DCompassNavigation.ValidHorizontalPosition(site.transform.position);
            if (site != lastSite) { arrived = false; lastSite = site; }
            if (!valid) { arrived = false; LocalCoordinates = Vector2.zero; Graphic.SetState(heading, hasHeading, 0, false, false); return; }
            LocalCoordinates = TopDown3DCompassNavigation.LocalCoordinates(player.transform.position, site.transform.position);
            arrived = TopDown3DCompassNavigation.IsArrived(LocalCoordinates.magnitude, arrived);
            Graphic.SetState(heading, hasHeading, arrived ? 0 : TopDown3DCompassNavigation.RelativeBearing(player.transform.position, site.transform.position, heading), hasHeading, arrived);
        }

        private void RefreshLayout()
        {
            if (pad == null || Graphic == null) return;
            pad.SetCompassLayout(true);
            var rect = Graphic.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * TopDown3DCompassLayout.OuterRadius(pad.Size) * 2;
        }
        private void OnEnable()
        {
            if (Graphic != null) Graphic.gameObject.SetActive(true);
            if (pad != null) RefreshLayout();
        }
        private void OnDisable()
        {
            if (pad != null) pad.SetCompassLayout(false);
            if (Graphic != null) Graphic.gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            if (Graphic == null) return;
            if (Application.isPlaying) Destroy(Graphic.gameObject);
            else DestroyImmediate(Graphic.gameObject);
        }
    }
}
