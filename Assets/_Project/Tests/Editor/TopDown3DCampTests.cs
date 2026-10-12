using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DCampTests
    {
        [Test] public void KitTransferConditionRejectionAndNotificationFailurePreserveOwnership()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            var state = new TopDown3DInventoryState(catalog, 1);
            state.TryAdd(new TopDown3DItemAmount(TopDown3DCampSettings.ItemId, 1));
            var worldOwnsKit = false;
            Assert.That(state.TryRemove(TopDown3DCampSettings.ItemId, 1, () => false).Succeeded, Is.False);
            Assert.That(state.Slots.Sum(slot => slot.Quantity), Is.EqualTo(1));
            state.Changed += () => { Assert.That(worldOwnsKit, Is.True); throw new InvalidOperationException("Observer failed after commit"); };
            var laterObserverRan = false;
            state.Changed += () => { laterObserverRan = true; Assert.That(state.TryAdd(new TopDown3DItemAmount(TopDown3DCampSettings.ItemId, 1)).Code, Is.EqualTo(TopDown3DInventoryResultCode.MutationBusy)); };
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Observer failed after commit"));
            Assert.That(state.TryRemove(TopDown3DCampSettings.ItemId, 1, () => { worldOwnsKit = true; return true; }).Succeeded, Is.True);
            Assert.That(laterObserverRan, Is.True);
            Assert.That(worldOwnsKit, Is.True); Assert.That(state.Slots.Sum(slot => slot.Quantity), Is.EqualTo(0));
        }
        [Test] public void FullInventoryRejectsCampReturnBeforeOwnershipCallback()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            var state = new TopDown3DInventoryState(catalog, 1); state.TryAdd(new TopDown3DItemAmount("tool.micro_dust_harvester", 1));
            var called = false;
            var result = state.TryAdd(new[] { new TopDown3DItemAmount(TopDown3DCampSettings.ItemId, 1) }, () => { called = true; return true; });
            Assert.That(result.Succeeded, Is.False); Assert.That(called, Is.False);
        }
        [Test] public void CampRecordIdentityDoesNotDependOnPosition()
        {
            var a = TopDown3DCampRecord.Create(Vector3.zero, Quaternion.identity); var b = TopDown3DCampRecord.Create(Vector3.zero, Quaternion.identity);
            Assert.That(a.id, Is.Not.EqualTo(b.id)); Assert.That(Guid.TryParse(a.id, out _), Is.True);
        }
        [Test] public void PlacementRejectsNewObstructionAndUnsupportedGround()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DCampSettings>();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                ground.transform.position = new Vector3(10000, -.25f, 10000); ground.transform.localScale = new Vector3(15, .5f, 15);
                ground.AddComponent<TopDown3DGroundSurface>(); obstacle.transform.position = new Vector3(10000, 1, 10001);
                obstacle.SetActive(false); Physics.SyncTransforms();
                Assert.That(TopDown3DCampPlacement.Validate(new Vector3(10000, 0, 10000), Quaternion.identity, settings, null, null, out _, out _), Is.True);
                obstacle.SetActive(true); Physics.SyncTransforms();
                Assert.That(TopDown3DCampPlacement.Validate(new Vector3(10000, 0, 10000), Quaternion.identity, settings, null, null, out _, out _), Is.False);
                Assert.That(TopDown3DCampPlacement.Validate(new Vector3(11000, 0, 11000), Quaternion.identity, settings, null, null, out _, out _), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(obstacle); UnityEngine.Object.DestroyImmediate(settings); }
        }
        [Test] public void AuthoredCampAssetsAndHumanoidClipsAreComplete()
        {
            var settings = TopDown3DCampSettings.Load(); Assert.That(settings.HasAssets, Is.True);
            Assert.That(settings.sleepClip.isHumanMotion && settings.restClip.isHumanMotion, Is.True);
            Assert.That(settings.deployedPrefab.GetComponent<TopDown3DCampView>(), Is.Not.Null);
            Assert.That(settings.wornCloakPrefab.GetComponentsInChildren<MeshFilter>().All(filter => filter.sharedMesh.isReadable), Is.True);
            foreach (var prefab in new[] { settings.deployedPrefab, settings.packedPrefab, settings.wornCloakPrefab, settings.blanketPrefab })
                Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one), "Gameplay roots must not inherit FBX unit conversion.");
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            Assert.That(catalog.TryGetDefinition(TopDown3DCampSettings.ItemId, out var item), Is.True);
            Assert.That(item.TryValidate(out _), Is.True); Assert.That(item.MaxStack, Is.EqualTo(1));
            Assert.That(settings.deployedPrefab.GetComponentsInChildren<MeshRenderer>().All(renderer => renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.TwoSided), Is.True);
        }
    }
}
