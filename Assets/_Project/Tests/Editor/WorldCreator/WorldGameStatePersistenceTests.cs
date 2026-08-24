using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldGameStatePersistenceTests
    {
        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();
        private readonly List<string> ownedFiles = new List<string>();

        [TearDown]
        public void TearDown()
        {
            for (var i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null) UnityEngine.Object.DestroyImmediate(ownedObjects[i]);
            }
            for (var i = 0; i < ownedFiles.Count; i++)
            {
                if (File.Exists(ownedFiles[i])) File.Delete(ownedFiles[i]);
                if (File.Exists(ownedFiles[i] + ".tmp")) File.Delete(ownedFiles[i] + ".tmp");
            }
            ownedObjects.Clear();
            ownedFiles.Clear();
        }

        [Test]
        public void GameSave_RoundTripsCrossChunkPlaceResourcesAndDeltas_WithoutGeometry()
        {
            var fixture = CreateFixture();
            const string resourceId = "resource-node:far-transect:17";
            fixture.Player.transform.position = new Vector3(5120.25f, 37.5f, -4096.75f);
            Assert.That(fixture.Inventory.State.TryAdd(
                new TopDown3DItemAmount("resource.persistence_test", 2)).Succeeded, Is.True);
            Assert.That(fixture.Resources.TryConsume(resourceId, 3, out _), Is.True);
            Assert.That(fixture.Resources.TryConsume(resourceId, 3, out _), Is.True);
            Assert.That(fixture.Resources.TryConsume(resourceId, 3, out var remaining), Is.True);
            Assert.That(remaining, Is.Zero);

            var anchor = new WorldFeatureId(0x101UL, 0x202UL);
            Assert.That(fixture.SaveService.TryMarkPlace(
                "The Broken Shelf",
                new[] { new WorldFeatureReference("anchor", anchor) },
                new OptionalThematicCoordinatePayload(
                    true,
                    "future.user-authored-coordinate",
                    1,
                    "opaque-proof-value"),
                out var markedPlace,
                out var markError), Is.True, markError);
            Assert.That(fixture.SaveService.TryUpsertWorldDelta(
                anchor,
                WorldVersionDomain.Site,
                "player-observed",
                1,
                "{\"state\":\"known\"}",
                out var deltaError), Is.True, deltaError);

            Assert.That(fixture.SaveService.Save(), Is.True);
            var json = File.ReadAllText(fixture.SaveService.SavePath);
            Assert.That(json, Does.Contain("\"version\": 3"));
            Assert.That(json, Does.Contain("\"worldManifest\""));
            Assert.That(json, Does.Contain("\"resources\""));
            Assert.That(json, Does.Contain("\"savedPlacePayloads\""));
            Assert.That(json, Does.Contain("\"worldDeltas\""));
            Assert.That(json, Does.Not.Contain("vertices"));
            Assert.That(json, Does.Not.Contain("triangles"));
            Assert.That(json, Does.Not.Contain("meshData"));

            fixture.Player.transform.position = new Vector3(-12f, 0f, 9f);
            Assert.That(fixture.Inventory.State.TryRemove("resource.persistence_test", 2).Succeeded, Is.True);
            Assert.That(fixture.SaveService.RemoveSavedPlace(markedPlace.SavedPlaceId), Is.True);
            var replacementResourceObject = Own(new GameObject("Persistence Replacement Resources"));
            var replacementResources = replacementResourceObject.AddComponent<TopDown3DResourceWorldState>();
            replacementResources.Configure(1);
            fixture.SaveService.ConfigureWorldPersistence(null, replacementResources);

            Assert.That(fixture.SaveService.Load(), Is.True);
            Assert.That(fixture.Player.transform.position,
                Is.EqualTo(new Vector3(5120.25f, 37.5f, -4096.75f)));
            Assert.That(fixture.Inventory.State.Slots[0].Quantity, Is.EqualTo(2));
            Assert.That(replacementResources.GetRemainingUses(resourceId, 3), Is.Zero);
            Assert.That(fixture.SaveService.SavedPlaces.Count, Is.EqualTo(1));
            Assert.That(fixture.SaveService.WorldDeltaCount, Is.EqualTo(1));
            var reconstructed = fixture.SaveService.SavedPlaces[0];
            Assert.That(reconstructed, Is.EqualTo(markedPlace));
            Assert.That(reconstructed.PlayerName, Is.EqualTo("The Broken Shelf"));
            Assert.That(reconstructed.AbsolutePosition,
                Is.EqualTo(new AbsoluteWorldPosition(5120.25d, 37.5d, -4096.75d)));
            Assert.That(reconstructed.ThematicCoordinate.AuthorityId,
                Is.EqualTo("future.user-authored-coordinate"));
        }

        [Test]
        public void GameSave_WrongSchemaRejectsBeforeChangingRuntimeState()
        {
            var fixture = CreateFixture();
            fixture.Player.transform.position = new Vector3(3072f, 4f, -2048f);
            Assert.That(fixture.Inventory.State.TryAdd(
                new TopDown3DItemAmount("resource.persistence_test", 1)).Succeeded, Is.True);
            Assert.That(fixture.SaveService.Save(), Is.True);

            var incompatible = File.ReadAllText(fixture.SaveService.SavePath)
                .Replace("\"version\": 3", "\"version\": 2");
            File.WriteAllText(fixture.SaveService.SavePath, incompatible);
            fixture.Player.transform.position = new Vector3(19f, 2f, 23f);
            Assert.That(fixture.Inventory.State.TryRemove("resource.persistence_test", 1).Succeeded, Is.True);
            var positionBefore = fixture.Player.transform.position;
            var inventoryBefore = JsonUtility.ToJson(fixture.Inventory.CaptureSnapshot());
            var resourcesBefore = JsonUtility.ToJson(fixture.Resources.CaptureSnapshot());

            Assert.That(fixture.SaveService.Load(), Is.False);
            Assert.That(fixture.Player.transform.position, Is.EqualTo(positionBefore));
            Assert.That(JsonUtility.ToJson(fixture.Inventory.CaptureSnapshot()), Is.EqualTo(inventoryBefore));
            Assert.That(JsonUtility.ToJson(fixture.Resources.CaptureSnapshot()), Is.EqualTo(resourcesBefore));
            Assert.That(fixture.SaveService.SavedPlaces.Count, Is.Zero);
            Assert.That(fixture.SaveService.WorldDeltaCount, Is.Zero);
        }

        private PersistenceFixture CreateFixture()
        {
            var texture = Own(new Texture2D(2, 2));
            var icon = Own(Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f));
            var definition = Own(ScriptableObject.CreateInstance<TopDown3DItemDefinition>());
            SetSerialized(definition, "itemId", "resource.persistence_test");
            SetSerialized(definition, "displayName", "Persistence Test Resource");
            SetSerialized(definition, "description", "Persistence fixture only.");
            SetSerialized(definition, "category", "Resource");
            SetSerialized(definition, "icon", icon);
            SetSerialized(definition, "maxStack", 99);
            SetSerialized(definition, "mass", 0f);

            var catalog = Own(ScriptableObject.CreateInstance<TopDown3DItemCatalog>());
            var catalogObject = new SerializedObject(catalog);
            var definitions = catalogObject.FindProperty("definitions");
            definitions.arraySize = 1;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = definition;
            catalogObject.ApplyModifiedPropertiesWithoutUndo();

            var player = Own(new GameObject("Persistence Test Player"));
            player.SetActive(false);
            var inventory = player.AddComponent<TopDown3DPlayerInventory>();
            inventory.Configure(catalog, 2);
            var saveService = player.AddComponent<TopDown3DGameStateSaveService>();
            var resourceObject = Own(new GameObject("Persistence Test Resources"));
            var resources = resourceObject.AddComponent<TopDown3DResourceWorldState>();
            resources.Configure(1);
            saveService.Configure(inventory, player.transform, null, null, 3496479);
            saveService.ConfigureWorldPersistence(null, resources);
            SetSerialized(saveService, "fileName", $"world-creator-persistence-{Guid.NewGuid():N}.json");
            player.SetActive(true);
            ownedFiles.Add(saveService.SavePath);
            return new PersistenceFixture(player, inventory, resources, saveService);
        }

        private T Own<T>(T value) where T : UnityEngine.Object
        {
            ownedObjects.Add(value);
            return value;
        }

        private static void SetSerialized(UnityEngine.Object target, string propertyName, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, propertyName);
            switch (value)
            {
                case string text:
                    property.stringValue = text;
                    break;
                case int integer:
                    property.intValue = integer;
                    break;
                case float number:
                    property.floatValue = number;
                    break;
                case UnityEngine.Object reference:
                    property.objectReferenceValue = reference;
                    break;
                default:
                    throw new ArgumentException($"Unsupported serialized fixture value '{value}'.", nameof(value));
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct PersistenceFixture
        {
            public PersistenceFixture(
                GameObject player,
                TopDown3DPlayerInventory inventory,
                TopDown3DResourceWorldState resources,
                TopDown3DGameStateSaveService saveService)
            {
                Player = player;
                Inventory = inventory;
                Resources = resources;
                SaveService = saveService;
            }

            public GameObject Player { get; }
            public TopDown3DPlayerInventory Inventory { get; }
            public TopDown3DResourceWorldState Resources { get; }
            public TopDown3DGameStateSaveService SaveService { get; }
        }
    }
}
