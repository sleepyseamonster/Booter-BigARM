using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DSceneHierarchyTests
    {
        private Scene first, second;
        [SetUp] public void SetUp() { first = EditorSceneManager.NewPreviewScene(); second = EditorSceneManager.NewPreviewScene(); }
        [TearDown] public void TearDown()
        {
            if (first.IsValid()) EditorSceneManager.ClosePreviewScene(first);
            if (second.IsValid()) EditorSceneManager.ClosePreviewScene(second);
        }
        private static GameObject New(string name, Scene scene)
        {
            var value = new GameObject(name); SceneManager.MoveGameObjectToScene(value, scene); return value;
        }

        [Test]
        public void SameNamedGroupsRemainSceneLocalAndPreserveWorldPose()
        {
            var foreign = New("World", first);
            var group = New("World", second); group.transform.SetPositionAndRotation(new Vector3(8, 2, 3), Quaternion.Euler(0, 40, 0));
            group.transform.localScale = Vector3.one * 2;
            var instance = New("Starting Camp", first);
            instance.transform.SetPositionAndRotation(new Vector3(2, 4, 6), Quaternion.Euler(0, 70, 0));
            var before = instance.transform.localToWorldMatrix;
            TopDown3DSceneHierarchy.Place(instance, second, TopDown3DSceneGroup.World);
            Assert.That(instance.scene, Is.EqualTo(second));
            Assert.That(instance.transform.parent, Is.SameAs(group.transform));
            Assert.That(foreign.transform.childCount, Is.Zero);
            for (int i = 0; i < 16; i++) Assert.That(instance.transform.localToWorldMatrix[i], Is.EqualTo(before[i]).Within(.0001f));
        }

        [Test]
        public void SceneUnloadDoesNotRetainAStaleGroupOrDuplicateService()
        {
            var service = TopDown3DWorldSpeedController.ForScene(first);
            Assert.That(service.transform.parent.name, Is.EqualTo("Systems"));
            Assert.That(TopDown3DWorldSpeedController.ForScene(first), Is.SameAs(service));
            EditorSceneManager.ClosePreviewScene(first); first = EditorSceneManager.NewPreviewScene();
            var replacement = TopDown3DWorldSpeedController.ForScene(first);
            Assert.That(replacement.transform.parent.gameObject.scene, Is.EqualTo(first));
            Assert.That(first.GetRootGameObjects().Length, Is.EqualTo(1));
        }

        [Test]
        public void AmbiguousOrInactiveGroupsRejectPlacementWithoutMovingTheObject()
        {
            var firstGroup = New("UI", first); var duplicate = New("UI", first); var instance = New("Canvas", second);
            Assert.Throws<System.InvalidOperationException>(() => TopDown3DSceneHierarchy.Place(instance, first, TopDown3DSceneGroup.UI));
            Assert.That(instance.scene, Is.EqualTo(second)); Assert.That(instance.transform.parent, Is.Null);
            Object.DestroyImmediate(duplicate); firstGroup.SetActive(false);
            Assert.Throws<System.InvalidOperationException>(() => TopDown3DSceneHierarchy.Place(instance, first, TopDown3DSceneGroup.UI));
            Assert.That(instance.scene, Is.EqualTo(second));
        }

        [TestCase(DepthOfFieldMode.Gaussian, true)]
        [TestCase(DepthOfFieldMode.Bokeh, true)]
        [TestCase(DepthOfFieldMode.Bokeh, false)]
        public void InspectorDepthGuardKeepsBokehValidWithoutChangingOtherOptics(DepthOfFieldMode mode, bool early)
        {
            var profile = ScriptableObject.CreateInstance<TopDown3DCampOpticsProfile>();
            try
            {
                profile.depthMode = mode; profile.gaussianBeforeLens = early; profile.bloomIntensity = .83f;
                var source = new SerializedObject(profile); source.Update();
                bool changed = TopDown3DNativeOpticsProfileEditor.EnforceDepthCompatibility(source);
                source.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(changed, Is.EqualTo(mode == DepthOfFieldMode.Bokeh && early));
                Assert.That(profile.IsValid, Is.True);
                Assert.That(profile.depthMode, Is.EqualTo(mode));
                Assert.That(profile.gaussianBeforeLens, Is.EqualTo(mode != DepthOfFieldMode.Bokeh && early));
                Assert.That(profile.bloomIntensity, Is.EqualTo(.83f));
                var editor = UnityEditor.Editor.CreateEditor(profile);
                try { Assert.That(editor, Is.TypeOf<TopDown3DNativeOpticsProfileEditor>()); }
                finally { Object.DestroyImmediate(editor); }
            }
            finally { Object.DestroyImmediate(profile); }
        }
    }
}
