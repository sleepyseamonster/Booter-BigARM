using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEditor;

namespace BooterBigArm.Tests
{
    public sealed class SceneRenderingOverrideTests
    {
        [Test]
        public void SavedStudyCameraOwnsTheEnabledOverride()
        {
            var scene=EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/Reference/FixedTerrainDressingStudy.unity");
            try
            {
                var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();
                var setting=camera.GetComponent<TopDown3DSceneRenderingOverride>();
                Assert.That(setting,Is.Not.Null); Assert.That(setting.enabled,Is.True);
                Assert.That(new SerializedObject(setting).FindProperty("disableTiltShift").boolValue,Is.True);
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]
        public void TiltShiftSuppressionAppliesOnlyToItsSceneAndCanBeDisabled()
        {
            var study = EditorSceneManager.NewPreviewScene();
            var other = EditorSceneManager.NewPreviewScene();
            try
            {
                var owner = new GameObject("Study rendering override"); SceneManager.MoveGameObjectToScene(owner, study);
                var setting = owner.AddComponent<TopDown3DSceneRenderingOverride>();
                Assert.That(TopDown3DSceneRenderingOverride.TiltShiftDisabledIn(study), Is.True);
                Assert.That(TopDown3DSceneRenderingOverride.TiltShiftDisabledIn(other), Is.False);
                setting.enabled = false;
                Assert.That(TopDown3DSceneRenderingOverride.TiltShiftDisabledIn(study), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(study); EditorSceneManager.ClosePreviewScene(other); }
        }
    }
}
