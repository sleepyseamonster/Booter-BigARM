using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>
    /// One-time, guarded transfer of the production play setup into the measured
    /// Badwater terrain scene. Run in an isolated project while the main editor is open.
    /// </summary>
    public static class BadwaterPlayableSceneBuilder
    {
        private const string TerrainScenePath = "Assets/_Project/Scenes/TopDown3D/GreaterWasteland.unity";
        private const string PrototypeScenePath = "Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity";
        private const float SpawnX = 520600f - 522448f;
        private const float SpawnZ = 4006750f - 4008248f;

        private static readonly string[] PlayRoots =
        {
            "Top Down 3D Input",
            "Booter Perspective 3D Controller",
            "Legger",
            "Main Camera",
            "Directional Key Light",
            "Dust Atmosphere",
            "EventSystem",
            "Inventory Canvas",
            "Game HUD Canvas"
        };

        public static void BuildFromCli()
        {
            if (!File.Exists(TerrainScenePath) || !File.Exists(PrototypeScenePath))
                throw new FileNotFoundException("Badwater or production scene is missing.");
            Scene destination = EditorSceneManager.OpenScene(TerrainScenePath, OpenSceneMode.Single);
            if (FindRoot(destination, "Booter Perspective 3D Controller") != null)
                throw new InvalidOperationException("Badwater scene is already playable; refusing duplicate transfer.");
            if (CountTerrain(destination) != 256)
                throw new InvalidDataException("Badwater scene must contain all 256 source terrain tiles.");
            foreach (GameObject root in destination.GetRootGameObjects())
            foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
            {
                if (terrain.terrainData.heightmapResolution != BadwaterHeightmapStitching.RenderResolution || !terrain.allowAutoConnect)
                    throw new InvalidDataException("Rebuild terrain with compatible grids and persistent neighbor stitching first.");
                if (terrain.GetComponent<TopDown3DGroundSurface>() == null)
                    terrain.gameObject.AddComponent<TopDown3DGroundSurface>();
            }
            foreach (GameObject root in destination.GetRootGameObjects())
                if (root.GetComponentsInChildren<Terrain>(true).Length == 256 && root.GetComponent<BadwaterTerrainConnectivity>() == null)
                    root.AddComponent<BadwaterTerrainConnectivity>();

            Scene source = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Additive);
            var moved = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (string name in PlayRoots)
            {
                GameObject root = FindRoot(source, name);
                if (root == null) throw new InvalidDataException("Missing production root: " + name);
                SceneManager.MoveGameObjectToScene(root, destination);
                moved.Add(name, root);
            }
            EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(destination);

            var player = moved["Booter Perspective 3D Controller"];
            var companion = moved["Legger"];
            var cameraObject = moved["Main Camera"];
            var input = moved["Top Down 3D Input"].GetComponent<TopDown3DInputRouter>();
            var motor = player.GetComponent<TopDown3DPlayerMotor>();
            var follower = companion.GetComponent<TopDown3DBigArmFollower>();
            var rig = cameraObject.GetComponent<TopDown3DCameraRig>();
            var camera = cameraObject.GetComponent<Camera>();
            if (input == null || input.InputActions == null || motor == null || follower == null || rig == null || camera == null)
                throw new InvalidDataException("Transferred play setup has a missing core component.");

            float playerGround = SampleWorldHeight(destination, SpawnX, SpawnZ);
            var capsule = player.GetComponent<CapsuleCollider>();
            if (capsule == null) throw new InvalidDataException("Booter capsule is missing.");
            player.transform.position = new Vector3(SpawnX, playerGround + capsule.height * 0.5f + 0.12f, SpawnZ);

            float leggerX = SpawnX - 4.2f;
            float leggerZ = SpawnZ - 2.5f;
            float leggerGround = SampleWorldHeight(destination, leggerX, leggerZ);
            companion.transform.position = new Vector3(leggerX, leggerGround + 0.82f, leggerZ);

            motor.Configure(input, rig.transform);
            rig.Configure(player.transform, input);
            follower.Configure(player.transform, rig.transform, input);
            player.GetComponent<TopDown3DPlayerActionController>()?.ConfigureHarvester(null, null);
            cameraObject.AddComponent<BadwaterCameraRange>().Configure(8000f);

            // The imported scene had a temporary review sun. Its production
            // twilight light, fog and post-processing now own the presentation.
            GameObject reviewSun = FindRoot(destination, "Review Sun");
            if (reviewSun != null) UnityEngine.Object.DestroyImmediate(reviewSun);
            RenderSettings.fog = false;
            RenderSettings.fogColor = new Color(0.42f, 0.19f, 0.10f);
            RenderSettings.fogDensity = TopDown3DDustAtmosphere.DefaultFogDensityAtIntensityOne;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientLight = new Color(0.36f, 0.39f, 0.43f);
            RenderSettings.sun = moved["Directional Key Light"].GetComponent<Light>();

            if (!EditorSceneManager.SaveScene(destination, TerrainScenePath))
                throw new IOException("Could not save playable Badwater scene.");
            Debug.Log($"Playable Badwater scene saved: Booter at {player.transform.position}, Legger at {companion.transform.position}.");
        }

        public static void ValidateFromCli()
        {
            Scene scene = EditorSceneManager.OpenScene(TerrainScenePath, OpenSceneMode.Single);
            if (CountTerrain(scene) != 256) throw new InvalidDataException("Terrain count changed.");
            BadwaterTerrainSeamRepair.Validate(scene);
            if (UnityEngine.Object.FindObjectsByType<TopDown3DProceduralWorld>(FindObjectsSortMode.None).Length != 0)
                throw new InvalidDataException("Procedural terrain generator would cover the measured terrain.");
            if (UnityEngine.Object.FindObjectsByType<TopDown3DGameStateSaveService>(FindObjectsSortMode.None).Length != 0)
                throw new InvalidDataException("Production save service must not write from this bounded study scene.");
            foreach (string name in PlayRoots)
                if (FindRoot(scene, name) == null) throw new InvalidDataException("Missing play root: " + name);
            var player = FindRoot(scene, "Booter Perspective 3D Controller");
            var companion = FindRoot(scene, "Legger");
            var cameraObject = FindRoot(scene, "Main Camera");
            var input = FindRoot(scene, "Top Down 3D Input").GetComponent<TopDown3DInputRouter>();
            var motor = player.GetComponent<TopDown3DPlayerMotor>();
            var follower = companion.GetComponent<TopDown3DBigArmFollower>();
            var rig = cameraObject.GetComponent<TopDown3DCameraRig>();
            var camera = cameraObject.GetComponent<Camera>();
            var range = cameraObject.GetComponent<BadwaterCameraRange>();
            if (input?.InputActions == null || motor == null || follower == null || rig == null || camera == null
                || range == null || range.FarClipPlane < 7000f
                || cameraObject.GetComponent<AudioListener>() == null)
                throw new InvalidDataException("Missing configured input, locomotion, long range camera or audio.");
            if (FindRoot(scene, "Review Sun") != null || FindRoot(scene, "Directional Key Light").GetComponent<PerpetualTwilightSun>() == null
                || FindRoot(scene, "Dust Atmosphere").GetComponent<TopDown3DDustAtmosphere>() == null)
                throw new InvalidDataException("Lighting and atmosphere ownership mismatch.");
            if (Mathf.Abs(player.transform.position.y - SampleWorldHeight(scene, SpawnX, SpawnZ)
                - player.GetComponent<CapsuleCollider>().height * 0.5f - 0.12f) > 0.04f)
                throw new InvalidDataException("Booter is not grounded at the measured spawn.");
            if (Mathf.Abs(companion.transform.position.y - SampleWorldHeight(scene, SpawnX - 4.2f, SpawnZ - 2.5f) - 0.82f) > 0.04f)
                throw new InvalidDataException("Legger is not grounded at the measured spawn.");
            foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour == null) throw new InvalidDataException("A scene component has a missing script.");
            Debug.Log("Playable Badwater scene validated: 256 terrains, Booter, Legger, input, camera, UI, twilight and dust.");
        }

        private static int CountTerrain(Scene scene)
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<Terrain>(true).Length;
            return count;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static float SampleWorldHeight(Scene scene, float x, float z)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
            {
                Vector3 origin = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (x < origin.x || x > origin.x + size.x || z < origin.z || z > origin.z + size.z)
                    continue;
                return origin.y + terrain.terrainData.GetInterpolatedHeight(
                    (x - origin.x) / size.x, (z - origin.z) / size.z);
            }
            throw new InvalidDataException($"No measured terrain under X={x}, Z={z}.");
        }
    }
}
