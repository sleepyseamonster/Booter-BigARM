using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Creates a new reference scene only in a task-owned background validation copy.</summary>
    public static class FixedTerrainPopulationSectorBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Reference/Performance/GreaterWastelandPopulationSector20261012.unity";
        public const string ActorScenePath = "Assets/_Project/Scenes/Reference/Performance/GreaterWastelandPopulationActors20261012.unity";
        [Serializable] public sealed class SourceTile
        {
            public string path, guid, sha256, materialGuid;
            public Vector3 position;
            public float pixelError;
            public bool instanced;
            public int shadowCastingMode;
        }
        [Serializable] public sealed class SourceManifest
        {
            public string sourceScene, sourceSceneSha256, scope;
            public SourceTile[] tiles;
        }
        [Serializable] public sealed class BuildReceipt
        {
            public string sourceSceneSha256, scenePath, sceneSha256, sceneGuid, scope;
            public SourceTile[] tiles;
            public int seed, nativeTiles;
            public float candidateDensity;
            public string catalogRevision, constraintRevision;
        }

        public static void BuildFromCli() => Build(false);
        public static void RebuildVerifiedDraftFromCli() => Build(true);
        public static void RebuildActorsVerifiedDraftFromCli()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            if (!Application.isBatchMode || !root.StartsWith("D:\\BooterBigArmValidation\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Owned background draft only.");
            var prior = JsonUtility.FromJson<ActorBuildReceipt>(File.ReadAllText(Path.Combine(root, "Logs/population-actors-build-20261012.json")));
            if (Hash(ActorScenePath) != prior.sceneSha256 || Hash(ScenePath) != prior.referenceSha256
                || Hash(ProductionGameplayTemplateBuilder.TemplateScenePath) != prior.templateSha256
                || AssetDatabase.AssetPathToGUID(ActorScenePath) != prior.sceneGuid) throw new InvalidOperationException("Exact actor draft/source proof required before rebuilding.");
            File.WriteAllBytes(ActorScenePath, File.ReadAllBytes(ProductionGameplayTemplateBuilder.TemplateScenePath));
            AssetDatabase.ImportAsset(ActorScenePath, ImportAssetOptions.ForceSynchronousImport);
            BuildActorsFromCli();
        }
        public static void BuildActorsFromCli()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            if (!Application.isBatchMode || !root.StartsWith("D:\\BooterBigArmValidation\\", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Create an actor reference only in the owned background validation copy.");
            string template = ProductionGameplayTemplateBuilder.TemplateScenePath;
            string templateHash = Hash(template), referenceHash = Hash(ScenePath);
            if (File.Exists(ActorScenePath))
            {
                if (Hash(ActorScenePath) != templateHash) throw new InvalidOperationException("Only an exact unsaved template draft may be resumed; authored actor scenes are retained.");
            }
            else if (!AssetDatabase.CopyAsset(template, ActorScenePath)) throw new IOException("Gameplay template copy failed.");
            var scene = EditorSceneManager.OpenScene(ActorScenePath, OpenSceneMode.Single);
            ProductionGameplayTemplateBuilder.ValidateScene(scene);
            var reference = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var terrainRoot = reference.GetRootGameObjects().Single(r => r.GetComponentsInChildren<Terrain>(true).Length == 16);
                var studyRoot = reference.GetRootGameObjects().Single(r => r.GetComponent<FixedTerrainPopulationSectorStudy>() != null);
                var study = studyRoot.GetComponent<FixedTerrainPopulationSectorStudy>(); var configuration = new SerializedObject(study);
                var revisions = Enumerable.Range(0, 16).Select(i => configuration.FindProperty("terrainRevisions").GetArrayElementAtIndex(i).stringValue).ToArray();
                var start = configuration.FindProperty("reservedStart").vector2Value; var end = configuration.FindProperty("reservedEnd").vector2Value;
                var prefab = (GameObject)configuration.FindProperty("prefab").objectReferenceValue;
                int seed = configuration.FindProperty("seed").intValue;
                string catalog = configuration.FindProperty("catalogRevision").stringValue, constraints = configuration.FindProperty("constraintRevision").stringValue;
                var tiles = Enumerable.Range(0, 16).Select(i => (Terrain)configuration.FindProperty("terrain").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                SceneManager.MoveGameObjectToScene(terrainRoot, scene); SceneManager.MoveGameObjectToScene(studyRoot, scene);
                foreach (var tile in tiles) if (tile.GetComponent<TopDown3DGroundSurface>() == null) tile.gameObject.AddComponent<TopDown3DGroundSurface>();
                SceneManager.SetActiveScene(scene); Physics.SyncTransforms();
                var roots = scene.GetRootGameObjects();
                var booter = roots.Single(r => r.GetComponent<TopDown3DPlayerMotor>() != null).GetComponent<TopDown3DPlayerMotor>();
                var bigArm = roots.Single(r => r.GetComponent<TopDown3DBigArmFollower>() != null).GetComponent<TopDown3DBigArmFollower>();
                var camera = roots.Single(r => r.GetComponent<Camera>() != null).GetComponent<Camera>();
                var light = roots.Single(r => r.GetComponent<PerpetualTwilightSun>() != null).GetComponent<Light>();
                var surface = new FixedTerrainSurfaceIndex(tiles, FixedTerrainPopulationSectorStudy.StudyWorldId);
                if (!surface.TrySample(start, out var spawn) || !surface.TrySample(start + new Vector2(-6, -2), out var companion))
                    throw new InvalidOperationException("Protected actor spawns need native collision.");
                var capsule = booter.GetComponent<CapsuleCollider>();
                booter.transform.position = spawn.Position + Vector3.up * (capsule.height * .5f - capsule.center.y + .12f);
                bigArm.transform.position = companion.Position + Vector3.up * new SerializedObject(bigArm).FindProperty("groundClearance").floatValue;
                camera.GetComponent<TopDown3DCameraRig>().Configure(booter.transform, roots.Single(r => r.GetComponent<TopDown3DInputRouter>() != null).GetComponent<TopDown3DInputRouter>());
                study.Configure(tiles, revisions, camera, light, Array.Empty<Transform>(), prefab, seed, catalog, start, end, constraints);
                study.BindActors(booter, bigArm); RenderSettings.sun = light;
                ValidateActorScene(scene);
                if (!EditorSceneManager.SaveScene(scene, ActorScenePath)) throw new IOException("Actor reference scene save failed.");
                if (Hash(template) != templateHash || Hash(ScenePath) != referenceHash) throw new InvalidOperationException("Source scenes changed.");
                File.WriteAllText(Path.Combine(root, "Logs/population-actors-build-20261012.json"), JsonUtility.ToJson(new ActorBuildReceipt {
                    scenePath = ActorScenePath, sceneSha256 = Hash(ActorScenePath), sceneGuid = AssetDatabase.AssetPathToGUID(ActorScenePath),
                    templateSha256 = templateHash, referenceSha256 = referenceHash, booter = booter.transform.position, bigArm = bigArm.transform.position,
                    dependencies = AssetDatabase.GetDependencies(ActorScenePath, true),
                    scope = "Reference-only actors, input/camera/UI/atmosphere wiring and sixteen original native tiles; no save service, build enablement or gameplay acceptance." }, true));
                Debug.Log("POPULATION_ACTORS_REFERENCE_BUILT: real actor bindings with sixteen resident native tiles.");
            }
            finally { EditorSceneManager.CloseScene(reference, true); }
        }
        [Serializable] public sealed class ActorBuildReceipt
        {
            public string scenePath, sceneSha256, sceneGuid, templateSha256, referenceSha256, scope;
            public Vector3 booter, bigArm;
            public string[] dependencies;
        }
        public static void ValidateActorsFromCli()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Validate saved actors in Edit Mode.");
            var original = SceneManager.GetActiveScene(); var scene = SceneManager.GetSceneByPath(ActorScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ActorScenePath, OpenSceneMode.Additive);
            try { ValidateActorScene(scene); Debug.Log("POPULATION_ACTORS_REFERENCE_VALIDATED"); }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
        }
        public static void ValidateActorScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects(); var scripts = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            var booter = scripts.OfType<TopDown3DPlayerMotor>().Single(); var bigArm = scripts.OfType<TopDown3DBigArmFollower>().Single();
            var study = scripts.OfType<FixedTerrainPopulationSectorStudy>().Single(); var settings = new SerializedObject(study);
            if (scene.path != ActorScenePath || scripts.Any(s => s == null) || scripts.OfType<GreaterWastelandInventorySaveService>().Any()
                || scripts.OfType<TopDown3DGameStateSaveService>().Any() || scripts.OfType<TopDown3DProceduralWorld>().Any()
                || settings.FindProperty("studyBooter").objectReferenceValue != booter || settings.FindProperty("studyBigArm").objectReferenceValue != bigArm
                || settings.FindProperty("actorDemandMarkers").arraySize != 0 || settings.FindProperty("terrain").arraySize != 16
                || EditorBuildSettings.scenes.Any(s => s.path == ActorScenePath && s.enabled)) throw new InvalidOperationException("Actor reference ownership or build exclusion changed.");
            var input = scripts.OfType<TopDown3DInputRouter>().Single(); var rig = scripts.OfType<TopDown3DCameraRig>().Single();
            if (new SerializedObject(booter).FindProperty("input").objectReferenceValue != input
                || new SerializedObject(bigArm).FindProperty("followTarget").objectReferenceValue != booter.transform
                || new SerializedObject(bigArm).FindProperty("input").objectReferenceValue != input
                || new SerializedObject(rig).FindProperty("target").objectReferenceValue != booter.transform
                || settings.FindProperty("view").objectReferenceValue != rig.GetComponent<Camera>()) throw new InvalidOperationException("Actor/camera/input references changed.");
            var manifest = JsonUtility.FromJson<SourceManifest>(File.ReadAllText("Docs/Evidence/Performance/population-sector-native-inputs-2026-10-12.json"));
            var tiles = roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).ToArray();
            if (tiles.Length != 16) throw new InvalidOperationException("Actor terrain count changed.");
            foreach (var source in manifest.tiles)
            {
                var tile = tiles.Single(t => AssetDatabase.GetAssetPath(t.terrainData) == source.path);
                if (Hash(source.path) != source.sha256 || AssetDatabase.AssetPathToGUID(source.path) != source.guid
                    || tile.transform.position != source.position || !tile.enabled || !tile.gameObject.activeInHierarchy
                    || tile.heightmapPixelError != source.pixelError || tile.drawInstanced != source.instanced
                    || (int)tile.shadowCastingMode != source.shadowCastingMode
                    || AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tile.materialTemplate)) != source.materialGuid
                    || tile.GetComponent<TerrainCollider>()?.terrainData != tile.terrainData || !tile.GetComponent<TerrainCollider>().enabled
                    || tile.GetComponent<TopDown3DGroundSurface>() == null) throw new InvalidOperationException("Actor native terrain contract changed.");
            }
            var surface = new FixedTerrainSurfaceIndex(tiles, "actor-reference-validation");
            if (!surface.TrySample(new Vector2(booter.transform.position.x, booter.transform.position.z), out var spawn)
                || !surface.TrySample(new Vector2(bigArm.transform.position.x, bigArm.transform.position.z), out var companion)) throw new InvalidOperationException("Actors lack native ground.");
            var capsule = booter.GetComponent<CapsuleCollider>();
            if (Mathf.Abs(booter.transform.position.y - spawn.Position.y - capsule.height * .5f + capsule.center.y - .12f) > .01f
                || Mathf.Abs(bigArm.transform.position.y - companion.Position.y - new SerializedObject(bigArm).FindProperty("groundClearance").floatValue) > .01f)
                throw new InvalidOperationException("Reference actors are not grounded.");
        }
        public static void ValidateFromCli()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Validate the saved reference in Edit Mode.");
            var original = SceneManager.GetActiveScene(); var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                string root = Directory.GetParent(Application.dataPath).FullName;
                var source = JsonUtility.FromJson<SourceManifest>(File.ReadAllText(Path.Combine(root,
                    "Docs/Evidence/Performance/population-sector-native-inputs-2026-10-12.json")));
                var roots = scene.GetRootGameObjects();
                var native = roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).ToArray();
                if (source?.tiles == null || source.tiles.Length != 16 || native.Length != 16 || EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled))
                    throw new InvalidOperationException("Reference native count or build exclusion changed.");
                foreach (var record in source.tiles)
                {
                    var tile = native.Single(t => AssetDatabase.GetAssetPath(t.terrainData) == record.path);
                    var collision = tile.GetComponent<TerrainCollider>();
                    if (Hash(record.path) != record.sha256 || AssetDatabase.AssetPathToGUID(record.path) != record.guid
                        || tile.transform.position != record.position || !tile.enabled || !tile.gameObject.activeInHierarchy
                        || collision == null || !collision.enabled || collision.terrainData != tile.terrainData
                        || tile.heightmapPixelError != record.pixelError || tile.drawInstanced != record.instanced
                        || (int)tile.shadowCastingMode != record.shadowCastingMode
                        || AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tile.materialTemplate)) != record.materialGuid)
                        throw new InvalidOperationException("Native reference contract changed: " + record.path);
                }
                var scripts = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                if (scripts.Any(m => m == null) || scripts.OfType<GreaterWastelandInventorySaveService>().Any()
                    || scripts.OfType<FixedTerrainPopulationSectorStudy>().Count() != 1)
                    throw new InvalidOperationException("Reference script/save ownership changed.");
                var serialized = new SerializedObject(scripts.OfType<FixedTerrainPopulationSectorStudy>().Single());
                if (serialized.FindProperty("terrain").arraySize != 16 || serialized.FindProperty("terrainRevisions").arraySize != 16
                    || serialized.FindProperty("actorDemandMarkers").arraySize != 2 || serialized.FindProperty("view").objectReferenceValue == null
                    || AssetDatabase.GetAssetPath(serialized.FindProperty("prefab").objectReferenceValue) != "Assets/_Project/Prefabs/TopDown3D/Environment/SculptedBasalt01.prefab")
                    throw new InvalidOperationException("Reference configuration bindings changed.");
                for (int i = 0; i < 16; i++)
                    if (serialized.FindProperty("terrainRevisions").GetArrayElementAtIndex(i).stringValue != "native-sha256:" + source.tiles[i].sha256
                        || serialized.FindProperty("terrain").GetArrayElementAtIndex(i).objectReferenceValue != native.Single(t => AssetDatabase.GetAssetPath(t.terrainData) == source.tiles[i].path))
                        throw new InvalidOperationException("Reference native revision bindings changed.");
                Debug.Log("Saved authored population reference validation passed.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
        }
        private static void Build(bool replaceVerifiedDraft)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            if (!Application.isBatchMode || !root.StartsWith("D:\\BooterBigArmValidation\\", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Run only in a task-owned background validation project; production Editor state is preserved.");
            if (File.Exists(ScenePath))
            {
                string priorReceipt = Path.Combine(root, "Logs/population-sector-build-20261012.json");
                if (!replaceVerifiedDraft || !File.Exists(priorReceipt)
                    || JsonUtility.FromJson<BuildReceipt>(File.ReadAllText(priorReceipt)).sceneSha256 != Hash(ScenePath))
                    throw new InvalidOperationException("Existing scenes are retained unless an exact task-owned draft receipt authorizes replacement.");
            }
            var manifest = JsonUtility.FromJson<SourceManifest>(File.ReadAllText(Path.Combine(root, "Logs/population-sector-source-20261012.json")));
            if (manifest?.tiles == null || manifest.tiles.Length != 16) throw new InvalidOperationException("Exactly sixteen accepted native tiles are required.");
            foreach (var source in manifest.tiles)
                if (Hash(source.path) != source.sha256 || AssetDatabase.AssetPathToGUID(source.path) != source.guid)
                    throw new InvalidOperationException("Native source hash/GUID changed: " + source.path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var terrainRoot = new GameObject("Resident authored native sector");
            var tiles = new List<Terrain>(); var byCell = new Dictionary<Vector2Int, Terrain>();
            foreach (var source in manifest.tiles)
            {
                var data = AssetDatabase.LoadAssetAtPath<TerrainData>(source.path);
                if (data == null) throw new InvalidOperationException("Native TerrainData could not be imported.");
                var tile = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
                tile.name = Path.GetFileNameWithoutExtension(source.path); tile.transform.position = source.position; tile.transform.SetParent(terrainRoot.transform, true);
                tile.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(source.materialGuid));
                if (tile.materialTemplate == null) throw new InvalidOperationException("Missing accepted terrain material.");
                tile.heightmapPixelError = source.pixelError; tile.drawInstanced = source.instanced; tile.shadowCastingMode = (ShadowCastingMode)source.shadowCastingMode;
                var cell = FixedTerrainPopulationIdentity.CellAt(new Vector2(source.position.x, source.position.z)); byCell.Add(cell, tile); tiles.Add(tile);
            }
            foreach (var pair in byCell)
                pair.Value.SetNeighbors(Find(pair.Key + Vector2Int.left), Find(pair.Key + Vector2Int.up), Find(pair.Key + Vector2Int.right), Find(pair.Key + Vector2Int.down));
            Terrain Find(Vector2Int cell) => byCell.TryGetValue(cell, out var tile) ? tile : null;
            Physics.SyncTransforms(); var surface = new FixedTerrainSurfaceIndex(tiles, FixedTerrainPopulationSectorStudy.StudyWorldId);
            var center = new Vector2(tiles.Min(t => t.transform.position.x) + 512, tiles.Min(t => t.transform.position.z) + 512);
            if (!surface.TrySample(center, out var ground)) throw new InvalidOperationException("Study camera anchor must have native support.");
            var camera = new GameObject("Reference sector camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = ground.Position + new Vector3(0, 220, -120); camera.transform.LookAt(ground.Position);
            camera.fieldOfView = 55; camera.nearClipPlane = .1f; camera.farClipPlane = 1800; camera.aspect = 16f / 9f;
            camera.GetUniversalAdditionalCameraData().SetRenderer(0);
            var sun = new GameObject("Reference sector sun").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(.43f, -.40f, .81f)); sun.color = new Color(1, .72f, .5f); sun.intensity = .8f; sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.3079263f, .33095473f, .3954263f);
            RenderSettings.ambientEquatorColor = new Color(.44889024f, .2672181f, .2066469f);
            RenderSettings.ambientGroundColor = new Color(.16225578f, .09435347f, .07776821f); RenderSettings.fog = false;
            var actors = new Transform[2];
            for (int i = 0; i < actors.Length; i++)
            {
                var actor = new GameObject(i == 0 ? "Booter demand marker (no gameplay actor)" : "BigARM demand marker (no gameplay actor)").transform;
                var point = center + (i == 0 ? new Vector2(-256, -256) : new Vector2(384, 384));
                if (!surface.TrySample(point, out var contact)) throw new InvalidOperationException("Actor demand anchor must have native support.");
                actor.position = contact.Position; actors[i] = actor;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/TopDown3D/Environment/SculptedBasalt01.prefab");
            if (prefab == null) throw new InvalidOperationException("Missing accepted basalt prefab.");
            string catalog = "study.basalt:" + AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(prefab));
            string constraints = "study.spawn32.corridor12:" + center.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + center.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            var study = new GameObject("Bounded procedural population study").AddComponent<FixedTerrainPopulationSectorStudy>();
            study.Configure(tiles.ToArray(), manifest.tiles.Select(v => "native-sha256:" + v.sha256).ToArray(), camera, sun, actors,
                prefab, 20261012, catalog, center, center + new Vector2(384, 384), constraints);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)); AssetDatabase.Refresh();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Reference scene save failed.");
            foreach (var source in manifest.tiles) if (Hash(source.path) != source.sha256) throw new InvalidOperationException("Native source was modified during integration.");
            var receipt = new BuildReceipt { sourceSceneSha256 = manifest.sourceSceneSha256, scenePath = ScenePath,
                sceneSha256 = Hash(ScenePath), sceneGuid = AssetDatabase.AssetPathToGUID(ScenePath), tiles = manifest.tiles,
                nativeTiles = 16, seed = 20261012, candidateDensity = 1, catalogRevision = catalog, constraintRevision = constraints,
                scope = "Authored 4x4 patch and original native world poses; study-only seed/masks/demand markers. No production actors, save binding, density acceptance or Player FPS proof." };
            File.WriteAllText(Path.Combine(root, "Logs/population-sector-build-20261012.json"), JsonUtility.ToJson(receipt, true));
            Debug.Log("Created reference population sector with sixteen unchanged authored native tiles.");
        }
        private static string Hash(string path)
        {
            using var hash = SHA256.Create();
            return string.Concat(hash.ComputeHash(File.ReadAllBytes(path)).Select(v => v.ToString("x2")));
        }
    }
}
