using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Copies a bounded native-terrain slice without loading or resaving the production scene.</summary>
    public static class CampLoopTestingSceneBuilder
    {
        private sealed class Document
        {
            public string Id, Text;
            public int Type;
        }
        private static readonly Regex Header = new(@"^--- !u!(\d+) &(-?\d+)[^\r\n]*", RegexOptions.Multiline);
        private static readonly Regex LocalReference = new(@"\{fileID: (-?\d+)\}");
        private static string Field(string text, string name) => Regex.Match(text, name + @": \{fileID: (-?\d+)\}").Groups[1].Value;
        private static Vector3 Position(string text)
        {
            var match = Regex.Match(text, @"m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)");
            return new Vector3(float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
        }

        public static void BuildFromCli()
        {
            if (!Application.isBatchMode || File.Exists(GreaterWastelandSceneScope.CampLoopScene))
                throw new InvalidOperationException("Author once in background batchmode; preserve existing testing scenes.");
            var source = File.ReadAllText(TopDown3DWaypointCampSettings.ProductionScene);
            var matches = Header.Matches(source);
            var documents = matches.Cast<Match>().Select((match, i) => new Document {
                Id = match.Groups[2].Value, Type = int.Parse(match.Groups[1].Value),
                Text = source.Substring(match.Index, (i + 1 < matches.Count ? matches[i + 1].Index : source.Length) - match.Index)
            }).ToArray();
            var transforms = documents.Where(d => d.Type == 4).ToDictionary(d => Field(d.Text, "m_GameObject"));
            var terrain = documents.Where(d => d.Type == 218).ToArray();
            var camp = TopDown3DWaypointCampSettings.Load();
            if (camp == null || !camp.TryValidate(out _)) throw new InvalidOperationException("Missing authored starting camp.");
            const float tileSize = 256;
            var center = new Vector2(Mathf.Floor(camp.position.x / tileSize) * tileSize, Mathf.Floor(camp.position.z / tileSize) * tileSize);
            var retained = terrain.Where(d => {
                var p = Position(transforms[Field(d.Text, "m_GameObject")].Text);
                return Mathf.Abs(p.x - center.x) <= tileSize && Mathf.Abs(p.z - center.y) <= tileSize;
            }).ToArray();
            if (retained.Length != 9) throw new InvalidOperationException("Starting camp must have a complete native nine-tile grid.");
            var keptObjects = retained.Select(d => Field(d.Text, "m_GameObject")).ToHashSet();
            // Keep only terrain hierarchy ancestors needed by the nine selected tiles.
            var keptTransforms = new HashSet<string>();
            var byId = documents.ToDictionary(d => d.Id);
            foreach (var go in keptObjects)
            {
                var transform = transforms[go];
                while (transform != null)
                {
                    keptTransforms.Add(transform.Id);
                    var parent = Field(transform.Text, "m_Father");
                    transform = parent != "0" && byId.TryGetValue(parent, out var found) ? found : null;
                }
            }
            var removedObjects = terrain.Where(d => !keptObjects.Contains(Field(d.Text, "m_GameObject")))
                .Select(d => Field(d.Text, "m_GameObject")).ToHashSet();
            foreach (var doc in documents.Where(d => d.Type == 1))
            {
                var name = Regex.Match(doc.Text, @"m_Name: ([^\r\n]+)").Groups[1].Value;
                if ((name.StartsWith("Terrain Sector |") || name.StartsWith("Terrain Section |")
                    || name == "badwater_core" || Regex.IsMatch(name, @"^(west|north|northwest)(_|$)"))
                    && transforms.TryGetValue(doc.Id, out var transform) && !keptTransforms.Contains(transform.Id))
                    removedObjects.Add(doc.Id);
            }
            var removed = documents.Where(d => removedObjects.Contains(d.Id) || removedObjects.Contains(Field(d.Text, "m_GameObject")))
                .Select(d => d.Id).ToHashSet();
            var result = new StringBuilder(source.Substring(0, matches[0].Index));
            foreach (var doc in documents.Where(d => !removed.Contains(d.Id)))
            {
                // Drop removed children/root entries. Null any remaining local terrain references.
                var text = Regex.Replace(doc.Text, @"^  - \{fileID: (-?\d+)\}[^\r\n]*\r?\n", m => removed.Contains(m.Groups[1].Value) ? "" : m.Value, RegexOptions.Multiline);
                text = LocalReference.Replace(text, m => removed.Contains(m.Groups[1].Value) ? "{fileID: 0}" : m.Value);
                result.Append(text);
            }
            File.WriteAllText(GreaterWastelandSceneScope.CampLoopScene, result.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(GreaterWastelandSceneScope.CampLoopScene, ImportAssetOptions.ForceSynchronousImport);
            CompleteFromCli();
            if (File.ReadAllText(TopDown3DWaypointCampSettings.ProductionScene) != source)
                throw new InvalidOperationException("Production changed during slice creation.");
            Debug.Log("CAMP_LOOP_BUILD_PASS: nine native 256m tiles; authored actors, rocks, resources and lighting retained.");
        }

        // Idempotent completion after extraction/import; never replaces terrain or accepted content.
        public static void CompleteFromCli()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use background batchmode.");
            var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene, OpenSceneMode.Single);
            if (!scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<TopDown3DAuthoredIronstoneNode>(true) != null))
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GreaterWastelandIronstoneAuthoring.AreaPrefabPath), scene);
            if (!scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<TopDown3DResourceCache>(true) != null))
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/TopDown3D/Resources/GreaterWastelandResourceCache.prefab"), scene);
            var ironstone = scene.GetRootGameObjects().Single(r => r.GetComponentInChildren<TopDown3DAuthoredIronstoneNode>(true) != null);
            ironstone.name = GreaterWastelandIronstoneInstaller.RootName;
            Validate(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Testing scene save failed.");
        }

        public static void ValidateFromCli() => Validate(EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene));

        public static void Validate(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            T[] All<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
            void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
            var terrain = All<Terrain>();
            Require(terrain.Length == 9 && All<TerrainCollider>().Length == 9, "Exactly nine terrain/collider pairs required.");
            var positions = terrain.Select(t => new Vector2(t.transform.position.x, t.transform.position.z)).ToArray();
            Require(positions.Distinct().Count() == 9 && positions.Select(p => p.x).Distinct().Count() == 3
                && positions.Select(p => p.y).Distinct().Count() == 3, "Terrain must form a complete 3x3 grid.");
            foreach (var t in terrain)
                Require(t.terrainData != null && t.terrainData.size.x == 256 && t.terrainData.size.z == 256
                    && t.GetComponent<TerrainCollider>().terrainData == t.terrainData, "Native terrain/collision mismatch.");
            bool Supported(Vector3 p) => terrain.Any(t => p.x >= t.transform.position.x && p.x < t.transform.position.x + 256
                && p.z >= t.transform.position.z && p.z < t.transform.position.z + 256);
            var camp = TopDown3DWaypointCampSettings.Load();
            Require(Supported(camp.position) && camp.returnRoute.All(Supported), "Camp/return route outside retained terrain.");
            Require(All<TopDown3DPlayerMotor>().Length == 1 && All<TopDown3DBigArmFollower>().Length == 1, "Actor ownership mismatch.");
            Require(Supported(All<TopDown3DPlayerMotor>()[0].transform.position)
                && Supported(All<TopDown3DBigArmFollower>()[0].transform.position), "Actors outside testing grid.");
            Require(All<TopDown3DInputRouter>().Length == 1 && All<Camera>().Any(c => c.CompareTag("MainCamera"))
                && All<Light>().Length > 0, "Controls, camera or lighting missing.");
            var rocks = All<TopDown3DCampRockArrangement>();
            Require(rocks.Length == 1 && rocks[0].Members.Length == 7 && rocks[0].Members.All(m => m.instance != null && Supported(m.groundAnchor)), "Authored camp rocks missing.");
            var nodes = All<TopDown3DAuthoredIronstoneNode>();
            Require(nodes.Length == 12 && nodes.Select(n => n.StableId).Distinct().Count() == 12
                && nodes.All(n => Supported(n.transform.position)), "Twelve grounded ironstone identities required.");
            Require(All<TopDown3DResourceCache>().Length == 1 && Supported(All<TopDown3DResourceCache>()[0].transform.position), "Resource cache missing.");
            Require(All<TopDown3DProceduralWorld>().Length == 0 && All<TopDown3DGameStateSaveService>().Length == 0, "Generated-world services prohibited.");
            Require(!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == scene.path), "Testing scene must not replace production build routing.");
            foreach (var root in roots)
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + transform.name);
            Debug.Log("CAMP_LOOP_VALIDATION_PASS: complete native grid, camp route, actors, seven rocks, twelve ironstone nodes, cache, controls and lighting.");
        }
    }
}
