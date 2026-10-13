using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DCompassNavigationTests
    {
        [TestCase(-20, 0, 0, 90)] [TestCase(-20, 0, 90, 0)]
        [TestCase(0, 20, 0, 180)] [TestCase(0, 20, 90, 90)]
        [TestCase(20, 0, 0, 270)] [TestCase(0, -20, 0, 0)] [TestCase(0, -20, 359, 1)]
        public void CampBearingIsRelativeToFacing(float east, float north, float heading, float expected)
        {
            var camp = new Vector3(-1842, -48, -1498);
            var player = camp + new Vector3(east, 50, north);
            Assert.That(TopDown3DCompassNavigation.RelativeBearing(player, camp, heading), Is.EqualTo(expected).Within(.001));
            Assert.That(TopDown3DCompassNavigation.LocalCoordinates(player, camp), Is.EqualTo(new Vector2(east, north)));
        }
        [TestCase(0, 1, 0)] [TestCase(1, 0, 90)] [TestCase(0, -1, 180)] [TestCase(-1, 0, 270)]
        public void HeadingUsesPhysicalHorizontalFacing(float x, float z, float expected)
        {
            Assert.That(TopDown3DCompassNavigation.TryHeading(new Vector3(x, 42, z), out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected).Within(.001));
        }
        [Test] public void InvalidHeadingAndPositionAreRejected()
        {
            Assert.That(TopDown3DCompassNavigation.TryHeading(Vector3.up, out _), Is.False);
            Assert.That(TopDown3DCompassNavigation.TryHeading(new Vector3(float.NaN, 0, 1), out _), Is.False);
            Assert.That(TopDown3DCompassNavigation.ValidHorizontalPosition(new Vector3(float.PositiveInfinity, 0, 0)), Is.False);
        }
        [Test] public void ArrivalHasHysteresisAndDoesNotEvaluateZeroBearing()
        {
            Assert.That(TopDown3DCompassNavigation.IsArrived(2, false), Is.True);
            Assert.That(TopDown3DCompassNavigation.IsArrived(2.5f, false), Is.False);
            Assert.That(TopDown3DCompassNavigation.IsArrived(2.5f, true), Is.True);
            Assert.That(TopDown3DCompassNavigation.IsArrived(3.01f, true), Is.False);
        }
        [Test] public void DarkwardAndLightwardStayOppositeAsBooterTurns()
        {
            for (var heading = 0; heading < 360; heading += 7)
                Assert.That((TopDown3DCompassNavigation.OnRing(-heading, 112) + TopDown3DCompassNavigation.OnRing(180 - heading, 112)).magnitude, Is.LessThan(.001));
            Assert.That(TopDown3DCompassNavigation.OnRing(-90, 112).x, Is.EqualTo(-112).Within(.001));
        }
        [TestCase(168)] [TestCase(128)] [TestCase(224)]
        public void SharedEnvelopeSeparatesMarkersDustAndCanister(float size)
        {
            var center = size * .5f;
            var radius = TopDown3DCompassLayout.OuterRadius(size);
            var insets = TopDown3DCompassLayout.ExtraInset(size);
            Assert.That(center - radius + insets.x, Is.EqualTo(0).Within(.001));
            Assert.That(TopDown3DCompassLayout.DustBottom(size) + insets.y, Is.EqualTo(0).Within(.001));
            Assert.That(TopDown3DCompassLayout.DustBottom(size) + 20, Is.LessThan(center - radius));
            Assert.That(TopDown3DCompassLayout.DetailsLeft(size) - 10, Is.GreaterThan(center + radius));
            Assert.That(TopDown3DCompassLayout.RingRadius(size) - 14 * TopDown3DCompassLayout.Scale(size),
                Is.GreaterThan(100 * TopDown3DCompassLayout.Scale(size)), "Sun rays must clear a coincident destination arrow or arrival cue");
        }
    }

    public sealed class TopDown3DCompassHudTests
    {
        private TopDown3DPlayerMotor player;
        private TopDown3DActionDpadHud pad;
        private TopDown3DCompassHud hud;
        private TopDown3DWaypointCampController waypoint;
        private TopDown3DCampView site;
        private TopDown3DWaypointCampSettings definition;
        [SetUp] public void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var actor = new GameObject("Booter"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, scene);
            player = actor.AddComponent<TopDown3DPlayerMotor>();
            waypoint = actor.AddComponent<TopDown3DWaypointCampController>();
            definition = ScriptableObject.CreateInstance<TopDown3DWaypointCampSettings>();
            site = new GameObject("Established camp").AddComponent<TopDown3DCampView>();
            site.ConfigureEstablished(null, null, new TopDown3DCampRecord {id = definition.siteId});
            site.transform.position = definition.position;
            Property("Definition", definition); Property("Registry", new TopDown3DWaypointCampRegistry(definition.worldId, definition.siteId, true)); Property("Site", site);
            Facing(Vector3.forward); player.transform.position = definition.position + Vector3.left * 20;
            pad = TopDown3DActionDpadHud.TryInstallForScene(scene); hud = TopDown3DCompassHud.Install(pad); hud.Bind(player, waypoint);
        }
        [TearDown] public void Cleanup()
        {
            // The controller was a read-only test double, not configured with simulation subscriptions.
            Property("Definition", null);
            Object.DestroyImmediate(definition);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        private void Property(string name, object value) => typeof(TopDown3DWaypointCampController).GetProperty(name).SetValue(waypoint, value);
        private void Facing(Vector3 direction) => typeof(TopDown3DPlayerMotor).GetField("facingDirection", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, direction);
        private void SetCompassEnabled(bool enabled)
        {
            hud.enabled = enabled;
            // Runtime-only MonoBehaviour messages do not run on toggles in EditMode.
            typeof(TopDown3DCompassHud).GetMethod(enabled ? "OnEnable" : "OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
        }
        [Test] public void InstallationIsPassiveAndIdempotent()
        {
            Assert.That(TopDown3DCompassHud.Install(pad), Is.SameAs(hud));
            Assert.That(pad.GetComponentsInChildren<TopDown3DCompassGraphic>().Length, Is.EqualTo(1));
            Assert.That(hud.Graphic.raycastTarget, Is.False);
            Assert.That(hud.Graphic.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That(pad.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(hud.Graphic.CampBearing, Is.EqualTo(90).Within(.001));
        }
        [Test] public void MissingOrForeignCampDoesNotPointToWorldZero()
        {
            Property("Registry", new TopDown3DWaypointCampRegistry("other-world", definition.siteId, true)); hud.RefreshPresentation();
            Assert.That(hud.Graphic.HasDestination, Is.False);
            Property("Registry", new TopDown3DWaypointCampRegistry(definition.worldId, definition.siteId, true));
            site.gameObject.SetActive(false); hud.RefreshPresentation(); Assert.That(hud.Graphic.HasDestination, Is.False);
            site.gameObject.SetActive(true); hud.RefreshPresentation(); Assert.That(hud.Graphic.HasDestination, Is.True);
            hud.Bind(player, null); Assert.That(hud.Graphic.HasDestination, Is.False);
            hud.Bind(player, waypoint); Assert.That(hud.Graphic.HasDestination, Is.True);
        }
        [Test] public void HeadingChangesButCameraOrbitDoesNot()
        {
            var camera = new GameObject("Orbit camera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(60, 235, 0); hud.RefreshPresentation();
            Assert.That(hud.Graphic.CampBearing, Is.EqualTo(90).Within(.001));
            Facing(Vector3.right); hud.RefreshPresentation(); Assert.That(hud.Graphic.CampBearing, Is.EqualTo(0).Within(.001));
            Facing(Vector3.zero); hud.RefreshPresentation(); Assert.That(hud.Graphic.Heading, Is.EqualTo(90).Within(.001));
        }
        [Test] public void ArrivalAndDepartureUpdateImmediately()
        {
            player.transform.position = site.transform.position; hud.RefreshPresentation(); Assert.That(hud.Graphic.Arrived, Is.True);
            player.transform.position += Vector3.right * 2.5f; hud.RefreshPresentation(); Assert.That(hud.Graphic.Arrived, Is.True);
            player.transform.position += Vector3.right; hud.RefreshPresentation(); Assert.That(hud.Graphic.Arrived, Is.False);
            Assert.That(hud.Graphic.CampBearing, Is.EqualTo(270).Within(.001));
        }
        [Test] public void DisablingRestoresLayoutAndReenablingRestoresGraphic()
        {
            Assert.That(pad.CompassLayout, Is.True);
            SetCompassEnabled(false); Assert.That(pad.CompassLayout, Is.False); Assert.That(hud.Graphic.gameObject.activeSelf, Is.False);
            SetCompassEnabled(true); Assert.That(pad.CompassLayout, Is.True); Assert.That(hud.Graphic.gameObject.activeSelf, Is.True);
            Assert.That(pad.DetailsLeft, Is.GreaterThan(168 + 16));
        }
        [Test] public void PlayerReplacementClearsStaleArrivalAndHeading()
        {
            player.transform.position = site.transform.position; hud.RefreshPresentation();
            var replacement = new GameObject("Replacement"); replacement.SetActive(false);
            var motor = replacement.AddComponent<TopDown3DPlayerMotor>();
            replacement.transform.position = site.transform.position + Vector3.back * 20;
            hud.Bind(motor, waypoint); Assert.That(hud.Graphic.Arrived, Is.False);
            hud.Bind(null, null); Assert.That(hud.Graphic.HasDestination, Is.False);
        }
        [Test] public void ReferenceScenesDoNotInstallProductionCompass()
        {
            Assert.That(TopDown3DCompassHud.TryInstallForScene(player.gameObject.scene), Is.Null);
        }
        [Test] public void AlgaePanelAndDustFollowCompassLayoutWithoutChangingPrompts()
        {
            var input = player.gameObject.AddComponent<TopDown3DInputRouter>();
            var vitals = player.gameObject.AddComponent<TopDown3DSurvivalVitals>();
            var state = player.gameObject.AddComponent<TopDown3DAlgaeCanisterState>(); state.Configure(TopDown3DAlgaeCanisterSettings.Load());
            var consumption = player.gameObject.AddComponent<TopDown3DAlgaeConsumptionController>(); consumption.Configure(input, vitals, state);
            var algae = TopDown3DAlgaeCanisterHud.TryInstallForScene(player.gameObject.scene, consumption);
            var dust = pad.transform.Find("Dust control").GetComponent<RectTransform>();
            var contents = algae.ContentsText;
            Assert.That(algae.DetailsRect.anchoredPosition.x, Is.EqualTo(pad.DetailsLeft));
            Assert.That(dust.anchoredPosition.y, Is.EqualTo(pad.DustBottom));
            SetCompassEnabled(false);
            Assert.That(algae.DetailsRect.anchoredPosition.x, Is.EqualTo(184)); Assert.That(dust.anchoredPosition.y, Is.EqualTo(-23));
            SetCompassEnabled(true);
            Assert.That(algae.DetailsRect.anchoredPosition.x, Is.EqualTo(pad.DetailsLeft));
            Assert.That(dust.anchoredPosition.y, Is.EqualTo(pad.DustBottom));
            Assert.That(algae.ContentsText, Is.EqualTo(contents));
        }
    }
}
