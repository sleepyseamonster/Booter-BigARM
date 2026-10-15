using System.Collections;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;

namespace BooterBigArm.Tests
{
    // Synthetic camera lifecycle fixture, with no production scene, traversal or gameplay smoke test.
    public sealed class TopDown3DCameraRuntimeTests
    {
        private GameObject target, cameraObject, routerObject, selectionObject;
        private TopDown3DCameraRig rig;
        private TopDown3DCameraOptions options;
        private Camera camera;
        private InputActionAsset actions;
        [UnitySetUp] public IEnumerator SetUp()
        {
            if (!Application.isBatchMode || !System.IO.File.Exists(".camera-options-validation"))
                Assert.Ignore("Run only in the marked isolated camera candidate.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            target = new GameObject("Synthetic follow target");
            actions = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            actions.devices = System.Array.Empty<InputDevice>();
            routerObject = new GameObject("Synthetic input"); routerObject.SetActive(false);
            var input = routerObject.AddComponent<TopDown3DInputRouter>(); input.Configure(actions); routerObject.SetActive(true);
            cameraObject = new GameObject("Synthetic output", typeof(Camera)); camera = cameraObject.GetComponent<Camera>();
            rig = cameraObject.AddComponent<TopDown3DCameraRig>(); rig.Configure(target.transform, input);
            options = rig.EnableCameraOptions();
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(target); Object.DestroyImmediate(routerObject);
            if (selectionObject != null) Object.DestroyImmediate(selectionObject);
            Object.DestroyImmediate(actions);
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator RuntimeCallbacksAcquireThirdPersonAndRestoreElevatedOnDisable()
        {
            options.ApplyPreferences(new TopDown3DMenuSettings { cameraView = TopDown3DCameraView.ThirdPerson });
            yield return null; yield return null;
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            Assert.That(camera.fieldOfView, Is.EqualTo(60)); Assert.That(rig.isActiveAndEnabled, Is.True);
            Assert.That(camera.transform.position.z, Is.LessThan(target.transform.position.z));
            options.enabled = false;
            yield return null;
            Assert.That(camera.fieldOfView, Is.EqualTo(48)); Assert.That(rig.isActiveAndEnabled, Is.True);
        }
        [UnityTest] public IEnumerator ThirdPersonFollowsRootTranslationWithoutMovingTheTarget()
        {
            options.ApplyPreferences(new TopDown3DMenuSettings { cameraView = TopDown3DCameraView.ThirdPerson });
            yield return null; yield return null;
            var before = camera.transform.position;
            var moved = target.transform.position + Vector3.right * 2; target.transform.position = moved;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(target.transform.position, Is.EqualTo(moved));
            Assert.That(camera.transform.position.x, Is.GreaterThan(before.x + .5f));
            Assert.That(options.HasUnresolvedClearance, Is.False);
        }

        [UnityTest] public IEnumerator AuthoredHierarchyStartsSelectedViewAndSupportsSessionSwitch()
        {
            selectionObject = new GameObject("Camera Options"); selectionObject.SetActive(false);
            var selection = selectionObject.AddComponent<TopDown3DCameraSelection>();
            var elevated = new GameObject("Elevated Camera Rig").AddComponent<TopDown3DElevatedCameraSettings>();
            elevated.transform.SetParent(selectionObject.transform, false); elevated.distance = 8;
            var third = new GameObject("Third Person Camera Rig").AddComponent<TopDown3DThirdPersonCameraSettings>();
            third.transform.SetParent(selectionObject.transform, false);
            third.profile = Resources.Load<TopDown3DThirdPersonCameraProfile>("ThirdPersonCameraProfile"); third.fieldOfView = 65;
            selection.elevated = elevated; selection.thirdPerson = third;
            var setup = new SerializedObject(selection); setup.FindProperty("outputRig").objectReferenceValue = rig; setup.ApplyModifiedProperties();
            var owner = new SerializedObject(rig); owner.FindProperty("cameraSelection").objectReferenceValue = selection; owner.ApplyModifiedProperties();
            options.Configure(rig);
            options.ApplyPreferences(new TopDown3DMenuSettings { cameraView = TopDown3DCameraView.Elevated });
            selectionObject.SetActive(true);
            yield return null; yield return null;
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            Assert.That(camera.fieldOfView, Is.EqualTo(65)); Assert.That(rig.Distance, Is.EqualTo(8));
            selection.RequestView(TopDown3DCameraView.Elevated);
            yield return null; yield return null;
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.Elevated));
            Assert.That(camera.fieldOfView, Is.EqualTo(48)); Assert.That(rig.isActiveAndEnabled, Is.True);
            Assert.That(selection.startingView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
        }
    }
}
