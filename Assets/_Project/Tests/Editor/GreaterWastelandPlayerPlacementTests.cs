using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class GreaterWastelandPlayerPlacementTests
    {
        [Test]
        public void TerrainPlacementUsesCollisionHeightAndRejectsOutsideOrDisabledTerrain()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(32, 100, 32) };
            try
            {
                var heights = new float[33, 33];
                for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = .5f;
                data.SetHeights(0, 0, heights);
                GameObject terrain = Terrain.CreateTerrainGameObject(data);
                SceneManager.MoveGameObjectToScene(terrain, scene);
                terrain.transform.position = new Vector3(-32, -25, -32);
                Physics.SyncTransforms();
                Assert.That(GreaterWastelandPlayerPlacement.TryGroundPoint(scene, new Vector3(-16, 9999, -16), out var point), Is.True);
                Assert.That(point.y, Is.EqualTo(25f).Within(.01f));
                Assert.That(GreaterWastelandPlayerPlacement.TryGroundPoint(scene, new Vector3(50, 0, 50), out _), Is.False);
                var capsuleObject = new GameObject("Offset capsule placement proof");
                SceneManager.MoveGameObjectToScene(capsuleObject, scene);
                var capsule = capsuleObject.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.center = Vector3.up;
                capsuleObject.transform.localScale = new Vector3(1, 2, 1);
                Physics.SyncTransforms();
                float offset = capsuleObject.transform.position.y - capsule.bounds.min.y;
                Vector3 placed = GreaterWastelandPlayerPlacement.PositionAboveGround(point, offset);
                capsuleObject.transform.position = placed;
                Physics.SyncTransforms();
                Assert.That(capsule.bounds.min.y, Is.EqualTo(point.y + GreaterWastelandPlayerPlacement.GroundClearance).Within(.001f));
                terrain.GetComponent<TerrainCollider>().enabled = false;
                Assert.That(GreaterWastelandPlayerPlacement.TryGroundPoint(scene, point, out _), Is.False);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void TerrainHolesDoNotAcceptPlayerPlacement()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(32, 100, 32) };
            try
            {
                var holes = new bool[32, 32];
                // Every cell is a hole: there is no collision surface to stand on.
                data.SetHoles(0, 0, holes);
                var terrain = Terrain.CreateTerrainGameObject(data);
                SceneManager.MoveGameObjectToScene(terrain, scene);
                Physics.SyncTransforms();
                Assert.That(GreaterWastelandPlayerPlacement.TryGroundPoint(scene, new Vector3(16, 0, 16), out _), Is.False);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(data);
            }
        }
    }
}
