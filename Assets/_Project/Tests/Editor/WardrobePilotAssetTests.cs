using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class WardrobePilotAssetTests
    {
        [Test] public void FittingPreviewInitializesAndDisposesWithoutOpeningAWindow()
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(BooterBigArm.Editor.WardrobePilotAuthoring.ModelPath))
                Assert.Ignore("Requires retained private animation character.");
            if (!AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeRig>("Assets/_Project/Art/Characters/Booter/Wardrobe/Pilot/BodyRig.asset"))
                Assert.Ignore("Requires the generated local wardrobe pilot.");
            var preview = ScriptableObject.CreateInstance<BooterBigArm.Editor.WardrobeFittingPreview>();
            try
            {
                var method = preview.GetType().GetMethod("Initialize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.DoesNotThrow(() => method.Invoke(preview, null));
                Assert.That(preview.GetType().GetField("wardrobe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(preview), Is.Not.Null);
            }
            finally { Object.DestroyImmediate(preview); }
        }
        [Test] public void ImportedCharacterAcceptsPilotAndPlateStaysRigidThroughChestMotion()
        {
            const string folder = "Assets/_Project/Art/Characters/Booter/Wardrobe/Pilot/";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(BooterBigArm.Editor.WardrobePilotAuthoring.ModelPath))
                Assert.Ignore("Requires the retained private animation character; source is not restored by a Git clone.");
            var rig = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeRig>(folder + "BodyRig.asset");
            if (!rig) Assert.Ignore("Requires the generated local wardrobe pilot.");
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BooterBigArm.Editor.WardrobePilotAuthoring.ModelPath));
            var before = new Mesh(); var after = new Mesh();
            try
            {
                var body = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s => s.sharedMesh == rig.bodyMesh);
                var jacket = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeDefinition>(folder + "Jacket.asset");
                var armor = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeDefinition>(folder + "Armor.asset");
                var skin = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeDefinition>(folder + "TextureSkin.asset");
                var wardrobe = root.AddComponent<TopDown3DWardrobePresentation>(); wardrobe.Configure(root.transform, body, rig);
                Assert.That(wardrobe.TryReplace(new[] { skin, jacket, armor }, out var error), Is.True, error);
                var renderer = root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s => s.sharedMesh == armor.parts[0].mesh);
                var chest = root.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Chest);
                var weights = renderer.sharedMesh.boneWeights;
                Assert.That(weights.All(w => w.weight0 == 1 && renderer.bones[w.boneIndex0] == chest), Is.True);
                renderer.BakeMesh(before); var points = before.vertices;
                chest.localRotation *= Quaternion.Euler(25, -20, 15);
                renderer.BakeMesh(after); var moved = after.vertices;
                Assert.That(Vector3.Distance(points[0], moved[0]), Is.GreaterThan(.01f));
                for (var i = 1; i < points.Length; i += 29)
                    Assert.That(Vector3.Distance(points[0], points[i]), Is.EqualTo(Vector3.Distance(moved[0], moved[i])).Within(.01f));
                Assert.That(body.sharedMesh.triangles.Length, Is.LessThan(rig.bodyMesh.triangles.Length));
                Assert.That(wardrobe.TryReplace(Array.Empty<TopDown3DWardrobeDefinition>(), out error), Is.True, error);
                Assert.That(body.sharedMesh, Is.SameAs(rig.bodyMesh));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(before); Object.DestroyImmediate(after); }
        }
    }
}
