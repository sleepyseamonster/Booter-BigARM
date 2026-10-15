using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DCameraOptionsTests
    {
        private readonly List<GameObject> objects = new();
        private TopDown3DCameraRig rig;
        private TopDown3DCameraOptions options;
        private Camera camera;
        private Transform target;
        private TopDown3DThirdPersonCameraProfile profile;
        private readonly Vector3 origin = new(15600, 15600, 15600);

        private GameObject Make(string name)
        { var item = new GameObject(name); item.transform.position = origin; item.layer = 31; objects.Add(item); return item; }

        [SetUp] public void SetUp()
        {
            target = Make("Target").transform;
            var item = Make("Camera"); camera = item.AddComponent<Camera>(); rig = item.AddComponent<TopDown3DCameraRig>();
            var serialized = new SerializedObject(rig); serialized.FindProperty("obstructionMask").intValue = 1 << 31;
            serialized.ApplyModifiedPropertiesWithoutUndo(); rig.Configure(target);
            options = rig.EnableCameraOptions();
            profile = ScriptableObject.CreateInstance<TopDown3DThirdPersonCameraProfile>(); options.Configure(rig, profile);
        }
        [TearDown] public void TearDown()
        {
            // These runtime behaviours do not execute lifecycle callbacks automatically in Edit Mode.
            if (options != null) { Lifecycle("OnDisable"); Lifecycle("OnDestroy"); }
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear(); Object.DestroyImmediate(profile); Physics.SyncTransforms();
        }
        private void Lifecycle(string name) => typeof(TopDown3DCameraOptions).GetMethod(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(options, null);

        [Test] public void RoundTripPreservesOutputIdentityAnglesAndRidingAvailability()
        {
            var identity = camera.GetInstanceID(); var yaw = rig.YawDegrees; var pitch = rig.PitchDegrees;
            Assert.That(options.TrySetView(TopDown3DCameraView.ThirdPerson), Is.True);
            Assert.That(rig.isActiveAndEnabled, Is.True);
            Assert.That(camera.fieldOfView, Is.EqualTo(60));
            Assert.That(options.OwnsOutput, Is.True);
            Assert.That(options.TrySetView(TopDown3DCameraView.Elevated), Is.True);
            Assert.That(options.OwnsOutput, Is.False);
            Assert.That(camera.GetInstanceID(), Is.EqualTo(identity));
            Assert.That(camera.fieldOfView, Is.EqualTo(48));
            Assert.That(rig.YawDegrees, Is.EqualTo(yaw)); Assert.That(rig.PitchDegrees, Is.EqualTo(pitch));
        }

        [Test] public void InactiveElevatedWriterCannotSnapOrApplyLens()
        {
            Assert.That(options.TrySetView(TopDown3DCameraView.ThirdPerson), Is.True);
            var position = camera.transform.position; var rotation = camera.transform.rotation;
            rig.SetViewAngles(140, 60); rig.Configure(target);
            Assert.That(camera.transform.position, Is.EqualTo(position)); Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
            Assert.That(camera.fieldOfView, Is.EqualTo(60));
        }

        [Test] public void BlockedSwitchRetainsPoseLensAndOwner()
        {
            var blocker = Make("Enclosure"); blocker.transform.position = origin + Vector3.up;
            blocker.AddComponent<BoxCollider>().size = Vector3.one * 30; Physics.SyncTransforms();
            var position = camera.transform.position; var rotation = camera.transform.rotation;
            Assert.That(options.TrySetView(TopDown3DCameraView.ThirdPerson), Is.False);
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.Elevated));
            Assert.That(camera.transform.position, Is.EqualTo(position)); Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
            Assert.That(camera.fieldOfView, Is.EqualTo(48)); Assert.That(options.Notice, Is.Not.Empty);
        }

        [Test] public void ThirdPersonIgnoresTargetHierarchyAndRiddenMount()
        {
            var targetShape = target.gameObject.AddComponent<BoxCollider>(); targetShape.size = Vector3.one * 3;
            var mount = Make("Mount"); mount.AddComponent<BoxCollider>().size = Vector3.one * 8;
            var seat = Make("Seat"); seat.transform.SetParent(mount.transform, true);
            var ride = Make("Ride owner").AddComponent<TopDown3DLeggerRidingController>();
            rig.SetRidingFrame(ride, seat.transform, mount.transform); Physics.SyncTransforms();
            Assert.That(options.TrySetView(TopDown3DCameraView.ThirdPerson), Is.True);
            Assert.That(Vector3.Distance(camera.transform.position, origin + Vector3.up * 1.6f), Is.EqualTo(5).Within(.02f));
        }

        [Test] public void ThirdPersonCompressionOverridesPreferredDistance()
        {
            var blocker = Make("Rear wall"); blocker.transform.position = origin + new Vector3(0, 1, -3);
            blocker.AddComponent<BoxCollider>().size = new Vector3(5, 6, .5f); Physics.SyncTransforms();
            Assert.That(options.TrySetView(TopDown3DCameraView.ThirdPerson), Is.True);
            Assert.That(Vector3.Distance(camera.transform.position, origin + Vector3.up * 1.6f), Is.LessThan(3));
            Assert.That(rig.HasBlockingOverlap(camera.transform.position), Is.False);
        }

        [Test] public void CameraRangeRemainsFinalOwner()
        {
            var range = camera.gameObject.AddComponent<BadwaterCameraRange>(); range.SetPreset("Low");
            Assert.That(options.TrySetView(TopDown3DCameraView.ThirdPerson), Is.True);
            Assert.That(camera.farClipPlane, Is.EqualTo(500));
            Assert.That(options.TrySetView(TopDown3DCameraView.Elevated), Is.True);
            Assert.That(camera.farClipPlane, Is.EqualTo(500));
        }

        [TestCase(0, 1, 2, true, true)]
        [TestCase(1, 0, 2, true, false)]
        [TestCase(0, -1, 2, true, false)]
        [TestCase(0, 1, 1, true, false)]
        [TestCase(0, 1, 2, false, false)]
        public void RecenterOnlyDuringForwardTravelAfterIdle(float x, float y, float idle, bool enabled, bool turns)
        { Assert.That(TopDown3DCameraOptions.RecenterYaw(0, 90, new Vector2(x, y), idle, enabled, .1f) > 0, Is.EqualTo(turns)); }

        [Test] public void PreferencesAreDesiredStateUntilSafeRuntimeCommit()
        {
            options.ApplyPreferences(new TopDown3DMenuSettings { cameraView = TopDown3DCameraView.ThirdPerson });
            Assert.That(options.PreferredView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.Elevated));
            Assert.That(options.Notice, Is.Not.Empty);
        }

        private TopDown3DInputRouter AttachRouter(TopDown3DInputMode mode)
        {
            var router = Make("Router").AddComponent<TopDown3DInputRouter>();
            typeof(TopDown3DInputRouter).GetProperty("Mode").SetValue(router, mode);
            rig.Configure(target, router); return router;
        }

        [Test] public void ModalUiFreezesAutomaticMotionAndDefersSelectedView()
        {
            var router = AttachRouter(TopDown3DInputMode.SystemMenu);
            options.ApplyPreferences(new TopDown3DMenuSettings { cameraView = TopDown3DCameraView.ThirdPerson });
            var before = camera.transform.position; options.Tick(.1f);
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.Elevated));
            Assert.That(camera.transform.position, Is.EqualTo(before));
            typeof(TopDown3DInputRouter).GetProperty("Mode").SetValue(router, TopDown3DInputMode.Gameplay);
            options.Tick(.1f); Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            typeof(TopDown3DInputRouter).GetProperty("Mode").SetValue(router, TopDown3DInputMode.Inventory);
            before = camera.transform.position; target.position += Vector3.forward * 10; options.Tick(.1f);
            Assert.That(camera.transform.position, Is.EqualTo(before));
        }

        [Test] public void PlacementOverrideRestoresPreferenceWithoutSavingAnOverride()
        {
            var router = AttachRouter(TopDown3DInputMode.Gameplay);
            options.ApplyPreferences(new TopDown3DMenuSettings { cameraView = TopDown3DCameraView.ThirdPerson }); options.Tick(.1f);
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            typeof(TopDown3DInputRouter).GetProperty("Mode").SetValue(router, TopDown3DInputMode.CampPlacement);
            options.Tick(.1f); Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.Elevated));
            Assert.That(options.PreferredView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            typeof(TopDown3DInputRouter).GetProperty("Mode").SetValue(router, TopDown3DInputMode.Gameplay);
            options.Tick(.1f); Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
        }

        [Test] public void DisablingOptionsReleasesCameraWithoutDisablingRidingFacade()
        {
            Assert.That(options.TrySetView(TopDown3DCameraView.ThirdPerson), Is.True);
            options.enabled = false;
            Lifecycle("OnDisable");
            Assert.That(rig.isActiveAndEnabled, Is.True); Assert.That(options.OwnsOutput, Is.False);
            Assert.That(camera.fieldOfView, Is.EqualTo(48));
        }

        private TopDown3DCameraSelection AddAuthoredSelection()
        {
            var selection = Make("Camera Options").AddComponent<TopDown3DCameraSelection>();
            var elevated = Make("Elevated Camera Rig").AddComponent<TopDown3DElevatedCameraSettings>();
            elevated.transform.SetParent(selection.transform, true);
            elevated.distance = 9; elevated.yawDegrees = 23; elevated.obstructionMask = 1 << 31;
            var third = Make("Third Person Camera Rig").AddComponent<TopDown3DThirdPersonCameraSettings>();
            third.transform.SetParent(selection.transform, true); third.profile = profile; third.fieldOfView = 65;
            selection.elevated = elevated; selection.thirdPerson = third;
            var setup = new SerializedObject(selection); setup.FindProperty("outputRig").objectReferenceValue = rig; setup.ApplyModifiedProperties();
            var owner = new SerializedObject(rig); owner.FindProperty("cameraSelection").objectReferenceValue = selection; owner.ApplyModifiedProperties();
            options.Configure(rig, profile);
            return selection;
        }

        [Test] public void AuthoredStartingViewAndLensOverrideOldLocalChoiceOnlyOnStartup()
        {
            var selection = AddAuthoredSelection();
            var saved = new TopDown3DMenuSettings { cameraView = TopDown3DCameraView.Elevated, cameraFieldOfView = 55 };
            options.ApplyPreferences(saved);
            Assert.That(options.PreferredView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            Assert.That(options.ThirdPersonFieldOfView, Is.EqualTo(65));
            Assert.That(saved.cameraView, Is.EqualTo(TopDown3DCameraView.Elevated), "Caller data must not be mutated.");
            options.ApplyPreferences(saved);
            Assert.That(options.PreferredView, Is.EqualTo(TopDown3DCameraView.Elevated), "Subsequent menu Apply must work.");
            Assert.That(selection.startingView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
        }

        [Test] public void SceneSelectorRequestsSwitchWithoutChangingStartingChoiceOrOutputIdentity()
        {
            var selection = AddAuthoredSelection(); AttachRouter(TopDown3DInputMode.Gameplay);
            int identity = camera.GetInstanceID(); options.Tick(.1f);
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            selection.RequestView(TopDown3DCameraView.Elevated); options.Tick(.1f);
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.Elevated));
            Assert.That(selection.startingView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            Assert.That(camera.GetInstanceID(), Is.EqualTo(identity)); Assert.That(rig.isActiveAndEnabled, Is.True);
        }

        [Test] public void SeparateElevatedSettingsPreserveOrbitUntilEditedAndDoNotChangeThirdPersonProfile()
        {
            var selection = AddAuthoredSelection();
            Assert.That(rig.Distance, Is.EqualTo(9)); Assert.That(rig.YawDegrees, Is.EqualTo(23));
            rig.SetViewAngles(110, 45); rig.RefreshAuthoring();
            Assert.That(rig.YawDegrees, Is.EqualTo(110), "Unchanged authored settings must not overwrite player orbit each frame.");
            var settings = new SerializedObject(selection.elevated);
            settings.FindProperty("distance").floatValue = 7; settings.ApplyModifiedProperties(); rig.RefreshAuthoring();
            Assert.That(rig.Distance, Is.EqualTo(7)); Assert.That(profile.distance, Is.EqualTo(5));
        }

        [Test] public void AuthoredThirdPersonSolverIsVisibleUnderItsOwnRigAndAddsNoRenderingCamera()
        {
            var selection = AddAuthoredSelection();
            Assert.That(options.TrySetView(TopDown3DCameraView.ThirdPerson), Is.True);
            Assert.That(selection.thirdPerson.transform.Find("Pose Solver (runtime)"), Is.Not.Null);
            Assert.That(selection.thirdPerson.transform.Find("Follow Pivot (runtime)"), Is.Not.Null);
            Assert.That(selection.thirdPerson.transform.Find("Pose Solver (runtime)").gameObject.hideFlags & HideFlags.HideInHierarchy, Is.EqualTo(HideFlags.None));
            Assert.That(selection.GetComponentsInChildren<Camera>(true), Is.Empty);
        }

        [Test] public void OldSettingsAndUnknownCameraValuesRemainCompatible()
        {
            var legacy = JsonUtility.FromJson<TopDown3DMenuSettings>("{\"version\":1,\"largeText\":true}");
            TopDown3DMenuSettingsService.NormalizeCamera(legacy);
            Assert.That(legacy.cameraView, Is.EqualTo(TopDown3DCameraView.Elevated));
            Assert.That(legacy.cameraSensitivityX, Is.EqualTo(1)); Assert.That(legacy.cameraFieldOfView, Is.EqualTo(60));
            Assert.That(legacy.cameraAutoRecenter, Is.True);
            legacy.cameraView = (TopDown3DCameraView)99; legacy.cameraFieldOfView = float.NaN;
            TopDown3DMenuSettingsService.NormalizeCamera(legacy);
            Assert.That(legacy.cameraView, Is.EqualTo(TopDown3DCameraView.Elevated)); Assert.That(legacy.cameraFieldOfView, Is.EqualTo(60));
        }

        [Test] public void ProfileEnablesOnlyTheApprovedStudyAndRejectsInvalidTuning()
        {
            Assert.That(profile.IsEnabledForScene("Assets/_Project/Scenes/Reference/CampLoopTesting.unity"), Is.True);
            Assert.That(profile.IsEnabledForScene("Assets/_Project/Scenes/Production/GreaterWasteland.unity"), Is.False);
            profile.distance = float.NaN;
            Assert.That(profile.IsValid, Is.False);
            Assert.That(options.TrySetView(TopDown3DCameraView.ThirdPerson), Is.False);
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.Elevated));
        }

        [Test] public void CameraPreferencesRoundTripAndFailedSaveDoesNotChangeAcceptedState()
        {
            string directory = Path.Combine(Path.GetTempPath(), "CameraOptions-" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new TopDown3DMenuSettingsService(directory);
                var draft = service.Accepted.Clone(); draft.cameraView = TopDown3DCameraView.ThirdPerson;
                draft.cameraAutoRecenter = false; draft.cameraSensitivityY = 1.5f;
                Assert.That(service.TrySave(draft, out _), Is.True);
                var restored = new TopDown3DMenuSettingsService(directory);
                Assert.That(restored.Accepted.cameraView, Is.EqualTo(draft.cameraView));
                Assert.That(restored.Accepted.cameraAutoRecenter, Is.False); Assert.That(restored.Accepted.cameraSensitivityY, Is.EqualTo(1.5f));
                draft.version = 99; Assert.That(restored.TrySave(draft, out _), Is.False);
                Assert.That(restored.Accepted.version, Is.EqualTo(1));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
