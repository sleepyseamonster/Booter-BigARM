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
        private GameObject target, cameraObject, routerObject;
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
    }
}
