using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DSurvivalTests
    {
        [Test]
        public void SettingsAsset_ProvidesAuthoredFourVitalTuning()
        {
            var settings = TopDown3DSurvivalSettings.Load();

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.MaximumHealth, Is.GreaterThan(0f));
            Assert.That(settings.MaximumHunger, Is.GreaterThan(0f));
            Assert.That(settings.MaximumThirst, Is.GreaterThan(0f));
            Assert.That(settings.MaximumOxygen, Is.GreaterThan(0f));
            Assert.That(settings.HungerDepletionPerSecond, Is.GreaterThan(0f));
            Assert.That(settings.ThirstDepletionPerSecond, Is.GreaterThan(0f));
        }

        [Test]
        public void Advance_DepletesOnlyHungerAndThirst()
        {
            var player = new GameObject("Survival Test Player");
            try
            {
                var vitals = player.AddComponent<TopDown3DSurvivalVitals>();
                vitals.ResetToFull();
                var settings = vitals.Settings;

                vitals.Advance(100f);

                Assert.That(vitals.Health, Is.EqualTo(settings.MaximumHealth));
                Assert.That(vitals.Oxygen, Is.EqualTo(settings.MaximumOxygen));
                Assert.That(
                    vitals.Hunger,
                    Is.EqualTo(settings.MaximumHunger - (settings.HungerDepletionPerSecond * 100f)).Within(0.001f));
                Assert.That(
                    vitals.Thirst,
                    Is.EqualTo(settings.MaximumThirst - (settings.ThirstDepletionPerSecond * 100f)).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Advance_ClampsDepletionAndIgnoresInvalidElapsedTime()
        {
            var player = new GameObject("Survival Test Player");
            try
            {
                var vitals = player.AddComponent<TopDown3DSurvivalVitals>();
                vitals.ResetToFull();

                vitals.Advance(float.NaN);
                vitals.Advance(float.PositiveInfinity);
                vitals.Advance(-1f);
                Assert.That(vitals.Hunger, Is.EqualTo(vitals.Settings.MaximumHunger));
                Assert.That(vitals.Thirst, Is.EqualTo(vitals.Settings.MaximumThirst));

                vitals.Advance(100000f);
                Assert.That(vitals.Hunger, Is.Zero);
                Assert.That(vitals.Thirst, Is.Zero);
                Assert.That(vitals.Health, Is.EqualTo(vitals.Settings.MaximumHealth));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Snapshot_RoundTripsAllFourPlayerOwnedVitals()
        {
            var sourceObject = new GameObject("Survival Snapshot Source");
            var targetObject = new GameObject("Survival Snapshot Target");
            try
            {
                var source = sourceObject.AddComponent<TopDown3DSurvivalVitals>();
                source.SetValue(TopDown3DSurvivalVital.Health, 72f);
                source.SetValue(TopDown3DSurvivalVital.Hunger, 61f);
                source.SetValue(TopDown3DSurvivalVital.Thirst, 54f);
                source.SetValue(TopDown3DSurvivalVital.Oxygen, 83f);

                var snapshot = source.CaptureSnapshot();
                var target = targetObject.AddComponent<TopDown3DSurvivalVitals>();

                Assert.That(target.ApplySnapshot(snapshot), Is.True);
                Assert.That(target.CaptureSnapshot().Version, Is.EqualTo(TopDown3DSurvivalVitals.CurrentSnapshotVersion));
                Assert.That(target.Health, Is.EqualTo(72f));
                Assert.That(target.Hunger, Is.EqualTo(61f));
                Assert.That(target.Thirst, Is.EqualTo(54f));
                Assert.That(target.Oxygen, Is.EqualTo(83f));
            }
            finally
            {
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void HudLayout_UsesCompactNonOverlappingTwoByTwoGridInsideSafeArea()
        {
            var safeArea = new Rect(80f, 40f, 1760f, 980f);
            var panel = TopDown3DSurvivalHud.GetPanelRect(safeArea, 1080, 1f);

            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(safeArea.xMin));
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(1080f - safeArea.yMax));
            Assert.That(panel.width, Is.EqualTo(TopDown3DSurvivalHud.ReferenceWidth));
            Assert.That(panel.height, Is.EqualTo(TopDown3DSurvivalHud.ReferenceHeight));

            var health = TopDown3DSurvivalHud.GetMeterRect(panel, 0, 1f);
            var hunger = TopDown3DSurvivalHud.GetMeterRect(panel, 1, 1f);
            var thirst = TopDown3DSurvivalHud.GetMeterRect(panel, 2, 1f);
            var oxygen = TopDown3DSurvivalHud.GetMeterRect(panel, 3, 1f);
            Assert.That(health.Overlaps(hunger), Is.False);
            Assert.That(health.Overlaps(thirst), Is.False);
            Assert.That(hunger.Overlaps(oxygen), Is.False);
            Assert.That(thirst.Overlaps(oxygen), Is.False);
        }

        [Test]
        public void TryInstallForScene_AttachesVitalsToPlayerAndIsIdempotent()
        {
            var scene = SceneManager.CreateScene("SurvivalHudTestScene");
            try
            {
                Assert.That(TopDown3DSurvivalHud.TryInstallForScene(scene), Is.Null);

                var player = new GameObject("Player");
                SceneManager.MoveGameObjectToScene(player, scene);
                player.AddComponent<TopDown3DPlayerMotor>();

                var first = TopDown3DSurvivalHud.TryInstallForScene(scene);
                var second = TopDown3DSurvivalHud.TryInstallForScene(scene);

                Assert.That(first, Is.Not.Null);
                Assert.That(second, Is.SameAs(first));
                Assert.That(first.Vitals, Is.SameAs(player.GetComponent<TopDown3DSurvivalVitals>()));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
