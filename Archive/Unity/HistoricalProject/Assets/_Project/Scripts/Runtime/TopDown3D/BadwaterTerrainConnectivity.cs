using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Restores native terrain LOD links for the bounded, authored Badwater grid.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class BadwaterTerrainConnectivity : MonoBehaviour
    {
        private void OnEnable()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            Reconnect();
        }

        private void OnDisable() => SceneManager.sceneLoaded -= SceneLoaded;
        private void Start() => Reconnect();

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene == gameObject.scene) Reconnect();
        }

        public void Reconnect()
        {
            // This scene is a fixed 256 m grid, separate from the procedural world.
            // Restrict ownership to this parent so another loaded scene is unaffected.
            var grid = new Dictionary<Vector2Int, Terrain>();
            foreach (Terrain tile in GetComponentsInChildren<Terrain>())
            {
                var cell = new Vector2Int(Mathf.RoundToInt(tile.transform.position.x / 256f),
                    Mathf.RoundToInt(tile.transform.position.z / 256f));
                grid.Add(cell, tile);
            }
            foreach (var pair in grid)
            {
                grid.TryGetValue(pair.Key + Vector2Int.left, out Terrain left);
                grid.TryGetValue(pair.Key + Vector2Int.up, out Terrain top);
                grid.TryGetValue(pair.Key + Vector2Int.right, out Terrain right);
                grid.TryGetValue(pair.Key + Vector2Int.down, out Terrain bottom);
                pair.Value.SetNeighbors(left, top, right, bottom);
            }
        }
    }
}
