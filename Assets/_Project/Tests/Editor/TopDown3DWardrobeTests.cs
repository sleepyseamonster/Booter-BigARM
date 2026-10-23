using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DWardrobeTests
    {
        private GameObject root;
        private SkinnedMeshRenderer body;
        private TopDown3DWardrobeRig rig;
        private TopDown3DWardrobePresentation wardrobe;
        private Material material;
        private readonly List<Object> assets = new();
        [SetUp] public void Setup()
        {
            root = new GameObject("Wardrobe fixture");
            var hips = new GameObject("Hips").transform; hips.SetParent(root.transform, false);
            var chest = new GameObject("Chest").transform; chest.SetParent(hips, false); chest.localPosition = Vector3.up;
            body = root.AddComponent<SkinnedMeshRenderer>();
            var mesh = Own(new Mesh { name = "Fixture body" });
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.one };
            mesh.triangles = new[] { 0, 1, 2, 1, 3, 2 };
            mesh.bindposes = new[] { hips.worldToLocalMatrix, chest.worldToLocalMatrix };
            mesh.boneWeights = new[] { Weight(0), Weight(0), Weight(1), Weight(1) };
            mesh.RecalculateBounds(); body.sharedMesh = mesh; body.bones = new[] { hips, chest }; body.rootBone = hips;
            material = Own(new Material(Shader.Find("Hidden/InternalErrorShader")));
            body.sharedMaterials = new[] { material };
            rig = Own(ScriptableObject.CreateInstance<TopDown3DWardrobeRig>());
            rig.bodyId = "test.body.v1"; rig.bodyMesh = mesh; rig.bonePaths = new[] { "Hips", "Hips/Chest" };
            rig.triangleRegions = new[] { (int)WardrobeRegion.Torso, (int)WardrobeRegion.Arms };
            wardrobe = root.AddComponent<TopDown3DWardrobePresentation>(); wardrobe.Configure(root.transform, body, rig);
        }
        [TearDown] public void Cleanup()
        { if (root) Object.DestroyImmediate(root); foreach (var asset in assets) if (asset) Object.DestroyImmediate(asset); assets.Clear(); }
        private T Own<T>(T asset) where T : Object { assets.Add(asset); return asset; }
        private static BoneWeight Weight(int i) => new() { boneIndex0 = i, weight0 = 1 };
        private TopDown3DWardrobeDefinition Wearable(string id, WardrobeRegion hides = WardrobeRegion.None)
        {
            var d = Own(ScriptableObject.CreateInstance<TopDown3DWardrobeDefinition>());
            d.rig = rig; d.visualId = id; d.hides = hides;
            d.parts = new[] { new WardrobePart { mesh = rig.bodyMesh, bonePaths = (string[])rig.bonePaths.Clone(),
                rootBonePath = "Hips", materials = new[] { material }, bounds = new Bounds(Vector3.zero, Vector3.one * 4) } };
            return d;
        }
        [Test] public void CoverageIsUnionAndRemovalRestoresOriginalMesh()
        {
            var torso = Wearable("torso", WardrobeRegion.Torso); var arms = Wearable("arms", WardrobeRegion.Arms);
            Assert.That(wardrobe.TryReplace(new[] { torso, arms }, out var error), Is.True, error);
            Assert.That(body.sharedMesh.triangles, Is.Empty);
            Assert.That(wardrobe.TryReplace(new[] { arms, torso }, out error), Is.True, error);
            Assert.That(body.sharedMesh.triangles, Is.Empty);
            Assert.That(wardrobe.TryReplace(new[] { arms }, out error), Is.True, error);
            Assert.That(body.sharedMesh.triangles, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(wardrobe.TryReplace(Array.Empty<TopDown3DWardrobeDefinition>(), out error), Is.True, error);
            Assert.That(body.sharedMesh, Is.SameAs(rig.bodyMesh));
        }
        [Test] public void ReorderedBoneArraysPreserveMeshIndexMapping()
        {
            var d = Wearable("reordered"); var part = d.parts[0];
            part.mesh = Own(Object.Instantiate(rig.bodyMesh));
            part.bonePaths = new[] { "Hips/Chest", "Hips" };
            part.mesh.bindposes = new[] { rig.bodyMesh.bindposes[1], rig.bodyMesh.bindposes[0] };
            part.mesh.boneWeights = new[] { Weight(1), Weight(1), Weight(0), Weight(0) };
            Assert.That(wardrobe.TryReplace(new[] { d }, out var error), Is.True, error);
            var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            Assert.That(skins.Length, Is.EqualTo(2));
            Assert.That(skins[1].bones[0], Is.SameAs(body.bones[1]));
            var baked = Own(new Mesh()); skins[1].BakeMesh(baked); var before = baked.vertices;
            body.bones[1].localRotation = Quaternion.Euler(0, 0, 50);
            skins[1].BakeMesh(baked); var after = baked.vertices;
            Assert.That(Vector3.Distance(before[3], after[3]), Is.GreaterThan(.01f));
            Assert.That(Vector3.Distance(before[2], before[3]), Is.EqualTo(Vector3.Distance(after[2], after[3])).Within(.0001f));
        }
        [TestCase("bind")][TestCase("bone")][TestCase("weight")][TestCase("bounds")][TestCase("material")][TestCase("schema")]
        public void InvalidCandidateLeavesEquippedAssemblyAndCoverageUntouched(string failure)
        {
            var good = Wearable("good", WardrobeRegion.Torso);
            Assert.That(wardrobe.TryReplace(new[] { good }, out _), Is.True);
            var priorMesh = body.sharedMesh; var revision = wardrobe.Revision;
            var d = Wearable("bad"); var p = d.parts[0]; p.mesh = Own(Object.Instantiate(p.mesh));
            if (failure == "bind") { var b = p.mesh.bindposes; b[0] = Matrix4x4.Scale(Vector3.one * 2); p.mesh.bindposes = b; }
            if (failure == "bone") p.bonePaths[0] = "Absent";
            if (failure == "weight") p.mesh.boneWeights = new[] { Weight(99), Weight(0), Weight(1), Weight(1) };
            if (failure == "bounds") p.bounds = new Bounds();
            if (failure == "material") p.materials = Array.Empty<Material>();
            if (failure == "schema") d.schemaVersion = 2;
            Assert.That(wardrobe.TryReplace(new[] { d }, out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty); Assert.That(wardrobe.Revision, Is.EqualTo(revision));
            Assert.That(body.sharedMesh, Is.SameAs(priorMesh)); Assert.That(wardrobe.EquippedVisualIds, Is.EqualTo(new[] { "good" }));
            Assert.That(root.GetComponentsInChildren<SkinnedMeshRenderer>().Length, Is.EqualTo(2));
        }
        [Test] public void ConflictsRejectButDifferentLayersCanShareRegions()
        {
            var a = Wearable("a"); var b = Wearable("b"); a.occupies = b.occupies = WardrobeRegion.Torso;
            Assert.That(wardrobe.CanReplace(new[] { a, b }, out _), Is.False);
            b.layer = WardrobeLayer.Armor; Assert.That(wardrobe.TryReplace(new[] { a, b }, out _), Is.True);
            Assert.That(wardrobe.CanReplace(new[] { a, a }, out _), Is.False);
        }
        [Test] public void MaterialVariantRestoresOnRemovalAndRejectsCompetingOwners()
        {
            var a = Wearable("skin"); a.parts = Array.Empty<WardrobePart>();
            var replacement = Own(new Material(material)); a.bodyMaterials = new[] { replacement };
            Assert.That(wardrobe.TryReplace(new[] { a }, out _), Is.True); Assert.That(body.sharedMaterials[0], Is.SameAs(replacement));
            var b = Wearable("other"); b.bodyMaterials = new[] { material };
            Assert.That(wardrobe.CanReplace(new[] { a, b }, out _), Is.False);
            Assert.That(wardrobe.TryReplace(Array.Empty<TopDown3DWardrobeDefinition>(), out _), Is.True);
            Assert.That(body.sharedMaterials[0], Is.SameAs(material));
        }
        [Test] public void RepeatedReplacementDoesNotAccumulateObjectsAndDestroyRestoresBody()
        {
            var d = Wearable("jacket", WardrobeRegion.Torso);
            for (var i = 0; i < 25; i++) Assert.That(wardrobe.TryReplace(new[] { d }, out _), Is.True);
            Assert.That(root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.EqualTo(2));
            Assert.That(root.GetComponentsInChildren<Animator>(true), Is.Empty);
            Object.DestroyImmediate(wardrobe); Assert.That(body.sharedMesh, Is.SameAs(rig.bodyMesh));
            Assert.That(root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.EqualTo(1));
        }
        [Test] public void ChangedBodyOrRigIsRejectedBeforeMutation()
        {
            var d = Wearable("jacket"); d.rig = Own(ScriptableObject.CreateInstance<TopDown3DWardrobeRig>());
            Assert.That(wardrobe.CanReplace(new[] { d }, out _), Is.False);
            body.sharedMesh = Own(Object.Instantiate(rig.bodyMesh));
            Assert.That(wardrobe.TryReplace(Array.Empty<TopDown3DWardrobeDefinition>(), out _), Is.False);
        }
        [Test] public void OfflinePoseBakeUsesCurrentBonesAndPreservesSource()
        {
            var original = rig.bodyMesh.vertices;
            body.bones[1].localRotation = Quaternion.Euler(0, 0, 90);
            var baked = Own(BooterBigArm.Editor.WardrobePoseBake.Bake(body));
            var expected = body.bones[1].localToWorldMatrix.MultiplyPoint3x4(rig.bodyMesh.bindposes[1].MultiplyPoint3x4(original[3]));
            Assert.That(Vector3.Distance(baked.vertices[3], expected), Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(baked.vertices[3], original[3]), Is.GreaterThan(.1f));
            Assert.That(rig.bodyMesh.vertices, Is.EqualTo(original));
        }
        [Test] public void PreparationFailureReleasesTemporaryMeshAndPreservesPresentation()
        {
            var d = Wearable("covered", WardrobeRegion.Torso);
            Assert.That(wardrobe.TryReplace(new[] { d }, out _), Is.True);
            var previous = body.sharedMesh; var revision = wardrobe.Revision;
            var count = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "Wardrobe covered body");
            rig.triangleRegions = Array.Empty<int>();
            Assert.That(wardrobe.TryReplace(new[] { d }, out _), Is.False);
            Assert.That(body.sharedMesh, Is.SameAs(previous)); Assert.That(wardrobe.Revision, Is.EqualTo(revision));
            Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "Wardrobe covered body"), Is.EqualTo(count));
        }
    }
}
