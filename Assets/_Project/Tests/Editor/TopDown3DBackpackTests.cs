using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DBackpackTests
    {
        [Test]
        public void PackIsRigidThroughChestAndHipMotionAndUsesWornPresentation()
        {
            var scene = EditorSceneManager.OpenPreviewScene(BooterBigArm.Editor.TopDown3DCampAssetBuilder.StudyPath);
            TopDown3DPlayerAnimationDriver driver = null;
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var before = new Mesh(); var after = new Mesh();
            try
            {
                driver = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TopDown3DPlayerAnimationDriver>()).Single();
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("Awake", flags).Invoke(driver, null);
                Assert.That(driver.ConfigureCamp(TopDown3DCampSettings.Load()), Is.True);
                var graph = (PlayableGraph)typeof(TopDown3DPlayerAnimationDriver).GetField("playableGraph", flags).GetValue(driver);
                graph.Evaluate(0);
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("LateUpdate", flags).Invoke(driver, null);
                var pack = driver.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == "Rigid backpack skin");
                var cloth = driver.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.name == "Cloak cloth skin").ToArray();
                Assert.That(cloth.Length, Is.EqualTo(2));
                Assert.That(cloth.Any(r => r.sharedMesh.boneWeights.Any(w => w.weight0 > 0 && w.weight0 < 1)), Is.True,
                    "The cloak must retain its flexible chest/hips drape");
                Assert.That(pack.sharedMaterials.Length, Is.EqualTo(3));
                Assert.That(pack.GetComponents<Collider>(), Is.Empty);
                pack.BakeMesh(before);
                var animator = driver.HumanoidAnimator;
                var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                hips.localRotation *= Quaternion.Euler(12, 18, 0);
                chest.localRotation *= Quaternion.Euler(28, -20, 10);
                pack.BakeMesh(after);
                var a = before.vertices; var b = after.vertices;
                Assert.That(b.Length, Is.EqualTo(a.Length));
                Assert.That((b[0] - a[0]).magnitude, Is.GreaterThan(.01f), "Pack must follow the animated torso");
                for (var i = 1; i < a.Length; i += 31)
                    Assert.That(Vector3.Distance(b[0], b[i]), Is.EqualTo(Vector3.Distance(a[0], a[i])).Within(.0001f), "Frame must not bend");
                driver.SetCampPresentation(0, 2, true, true);
                Assert.That(pack.gameObject.activeInHierarchy, Is.False, "Pack cannot remain beneath reclining Booter");
                driver.SetCampPresentation(-1, 1, false, false);
                Assert.That(pack.gameObject.activeInHierarchy, Is.True, "Deploying the separate shade cannot remove the pack");
            }
            finally
            {
                if (driver) typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDestroy", flags).Invoke(driver, null);
                Object.DestroyImmediate(before); Object.DestroyImmediate(after);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
