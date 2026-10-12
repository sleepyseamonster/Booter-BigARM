using System;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class Camp1AuthoredRockTests
    {
        [Test] public void SceneMergePreservesExistingDocumentsAndAddsOnlyThePrefabRoot()
        {
            const string original="%YAML 1.1\n--- !u!1 &10\nGameObject:\n  m_Name: Existing Actor\n--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_Roots:\n  - {fileID: 10}\n";
            const string serialized="%YAML 1.1\n--- !u!1 &10\nGameObject:\n  m_Name: Serializer Noise\n--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_Roots:\n  - {fileID: 10}\n  - {fileID: 21}\n--- !u!1001 &20\nPrefabInstance:\n  m_SourcePrefab: {fileID: 100100000, guid: abc, type: 3}\n--- !u!4 &21 stripped\nTransform:\n  m_PrefabInstance: {fileID: 20}\n";
            var result=Camp1AuthoredRockAuthoring.MergeNewRoot(original,serialized,"abc");
            Assert.That(result, Does.Contain("m_Name: Existing Actor")); Assert.That(result, Does.Not.Contain("Serializer Noise"));
            Assert.That(result, Does.Contain("  - {fileID: 21}")); Assert.That(result, Does.Contain("--- !u!1001 &20"));
            Assert.Throws<InvalidOperationException>(()=>Camp1AuthoredRockAuthoring.MergeNewRoot(original,serialized.Replace("  - {fileID: 21}\n","  - {fileID: 21}\n  - {fileID: 22}\n"),"abc"));
        }
        [Test] public void ProtectedGameplayVolumesRejectAnObstructingRock()
        {
            var rock = new Bounds(new Vector3(10, 1, 0), new Vector3(3, 2, 3));
            Assert.Throws<InvalidOperationException>(() => Camp1AuthoredRockAuthoring.RequireClear(rock,
                new[] { new Bounds(new Vector3(10, 2, 0), new Vector3(10, 16, 10)) }));
            Assert.DoesNotThrow(() => Camp1AuthoredRockAuthoring.RequireClear(rock,
                new[] { new Bounds(new Vector3(-10, 2, 0), new Vector3(10, 16, 10)) }));
        }
        [Test] public void AuthoredPrefabUsesSevenDistinctReadyFamiliesAndFixedIdentities()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Camp1AuthoredRockAuthoring.PrefabPath);
            Assert.That(root, Is.Not.Null);
            var layout = root.GetComponent<TopDown3DCampRockArrangement>();
            Assert.That(layout.WorldId, Is.EqualTo(GreaterWastelandInventorySnapshot.WorldIdentity));
            Assert.That(layout.SiteId, Is.EqualTo("camp.starting")); Assert.That(layout.LayoutVersion, Is.EqualTo(1));
            Assert.That(layout.Members.Length, Is.EqualTo(7));
            Assert.That(layout.Members.Select(m => m.placementId).Distinct().Count(), Is.EqualTo(7));
            Assert.That(layout.Members.Select(m => m.assetId).Distinct().Count(), Is.EqualTo(7));
            foreach (var member in layout.Members)
            {
                Assert.That(member.placementId, Does.StartWith("greater-wasteland:camp1:rock:v1:"));
                Assert.That(Vector3.ProjectOnPlane(member.instance.position - layout.CampAnchor, Vector3.up).magnitude, Is.LessThanOrEqualTo(28f));
                Assert.That(member.instance.localScale, Is.EqualTo(Vector3.one));
                Assert.That(member.instance.GetComponent<LODGroup>().GetLODs().Length, Is.EqualTo(3));
                Assert.That(member.instance.GetComponentInChildren<MeshCollider>().convex, Is.True);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(member.instance.gameObject), Is.Not.Null);
            }
            Assert.That(root.GetComponentsInChildren<TopDown3DIronstoneNode>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<TopDown3DProceduralWorld>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
        }
    }
}
