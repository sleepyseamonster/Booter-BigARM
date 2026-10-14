using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class CampLoopSalvageAuthoring
    {
        public const string RootName = "Ghost Machine Salvage Study";
        public const string Settings = "Assets/_Project/Settings/Studies/CampLoopSalvage";
        public const string Art = "Assets/_Project/Art/Environment/Machines/Studies/CampLoopSalvage";
        public const string Prefabs = "Assets/_Project/Prefabs/TopDown3D/Studies/CampLoopSalvage";
        public const string Icons = "Assets/_Project/UI/Items/Studies/CampLoopSalvage";
        public const string CatalogPath = Settings + "/LabItemCatalog.asset";
        public static void InstallFromCli()
        {
            if (!Application.isBatchMode || Application.isPlaying) throw new InvalidOperationException("Author in background Edit Mode only.");
            var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene, OpenSceneMode.Single);
            if (All<CampLoopSalvageSession>(scene).Length > 0) { Validate(scene); ExportCandidate(scene); return; }
            if (scene.GetRootGameObjects().Any(r => r.name == RootName) || All<TopDown3DAnimationPlaygroundSession>(scene).Length != 1)
                throw new InvalidOperationException("Ambiguous wreck ownership or missing laboratory marker.");
            if (new[] { Settings, Art, Prefabs, Icons }.Any(Directory.Exists))
                throw new InvalidOperationException("Uninstalled study assets already exist; preserve them and reconcile ownership before authoring.");
            CampLoopTestingSceneBuilder.Validate(scene);
            Physics.SyncTransforms();
            var position = FindPlacement(scene);
            foreach (var folder in new[] { Settings, Art, Prefabs, Icons }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var stock = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            var names = new[] { "Metal Plate", "Wire Bundle", "Drive Assembly" };
            var masses = new[] { 2f, .5f, 6f }; var stacks = new[] { 20, 10, 1 };
            var items = new TopDown3DItemDefinition[3];
            for (var i = 0; i < 3; i++)
            {
                var iconPath = Icons + "/Part" + i + ".png";
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++)
                {
                    var dx = x - 32; var dy = y - 32;
                    var fill = i == 0 ? x > 12 && x < 52 && y > 18 && y < 46
                        : i == 1 ? dx * dx + dy * dy > 210 && dx * dx + dy * dy < 420
                        : Mathf.Abs(dx) < 12 && Mathf.Abs(dy) < 22 || Mathf.Abs(dx) < 23 && Mathf.Abs(dy) < 7;
                    texture.SetPixel(x, y, fill ? new Color(.78f, .65f, .40f) : Color.clear);
                }
                texture.Apply(); File.WriteAllBytes(iconPath, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(iconPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
                importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 64; importer.alphaIsTransparency = true; importer.SaveAndReimport();
                items[i] = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
                items[i].Configure(TopDown3DSalvageWreckState.ItemIds[i], names[i], "Trial salvage from the dead camp-loop machine.", "Resource",
                    AssetDatabase.LoadAssetAtPath<Sprite>(iconPath), stacks[i], masses[i]);
                AssetDatabase.CreateAsset(items[i], Settings + "/Part" + i + ".asset");
            }
            var catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>();
            catalog.Configure(stock.Definitions.Concat(items)); AssetDatabase.CreateAsset(catalog, CatalogPath);
            var metal = Material("WornMetal", new Color(.19f, .16f, .14f), .65f);
            var cut = Material("ExposedMetal", new Color(.48f, .39f, .26f), .6f);
            var ceramic = Material("Ceramic", new Color(.72f, .64f, .47f), .1f);
            var root = new GameObject(RootName); SceneManager.MoveGameObjectToScene(root, scene); root.transform.position = position;
            var wreck = root.AddComponent<TopDown3DSalvageWreckState>(); var session = root.AddComponent<CampLoopSalvageSession>();
            var geometry = new GameObject("Structural carcass"); geometry.transform.SetParent(root.transform, false);
            Block(geometry.transform, "Collapsed spine", new Vector3(0, .6f, 0), new Vector3(3.8f, .55f, .7f), metal, true);
            for (var i = 0; i < 6; i++)
            {
                var x = -1.6f + i * .64f;
                Block(geometry.transform, "Hull rib " + i, new Vector3(x, .9f, .25f), new Vector3(.12f, .95f, 1.5f), metal, false);
            }
            for (var i = 0; i < 3; i++) for (var side = -1; side <= 1; side += 2)
            {
                var leg = Block(geometry.transform, "Folded leg", new Vector3(-1.4f + i * 1.4f, .3f, side * .85f), new Vector3(.25f, .25f, 1.6f), metal, false);
                leg.transform.localRotation = Quaternion.Euler(0, side * 25, 12);
            }
            Block(geometry.transform, "Empty sensor head", new Vector3(2f, .4f, .1f), new Vector3(.45f, .55f, .5f), metal, true);
            Block(geometry.transform, "Broken sensor aperture", new Vector3(2.24f, .4f, .1f), new Vector3(.05f, .18f, .23f), ceramic, false);
            Block(geometry.transform, "Detached leg", new Vector3(-1.7f, .15f, 1.5f), new Vector3(1.1f, .22f, .22f), metal, false);
            var durations = new[] { 2.5f, 2f, 3.5f };
            var prompts = new[] { "Remove plating — 2 Metal Plates", "Strip wiring — 1 Wire Bundle", "Extract joint — 1 Drive Assembly" };
            for (var i = 0; i < 3; i++)
            {
                var socket = new GameObject("Salvage " + names[i]); socket.transform.SetParent(root.transform, false);
                socket.transform.localPosition = new Vector3(-1.35f + i * 1.35f, .65f, -1.05f);
                var collider = socket.AddComponent<BoxCollider>(); collider.size = new Vector3(.65f, .6f, .3f); collider.isTrigger = true;
                var housing = Block(geometry.transform, "Empty housing " + i, socket.transform.localPosition + Vector3.forward * .25f, new Vector3(.65f, .45f, .15f), metal, false);
                var removable = Block(socket.transform, names[i], Vector3.zero, i == 0 ? new Vector3(.7f, .5f, .12f)
                    : i == 1 ? new Vector3(.5f, .12f, .16f) : new Vector3(.4f, .3f, .3f), cut, false);
                if (i == 1)
                {
                    for (var wire = 0; wire < 3; wire++) Block(removable.transform, "Cable loop", new Vector3(-.6f + wire * .6f, -1.4f, 0), new Vector3(.09f, 3f, .7f), ceramic, false);
                }
                socket.AddComponent<TopDown3DSalvagePoint>().Configure(wreck, session, i, durations[i], prompts[i], collider, removable);
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/DeadGhostMachine.prefab");
            Object.DestroyImmediate(root);
            root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene); root.transform.position = position;
            var kit = All<TopDown3DPlayerInventory>(scene).Single(); var cargo = All<TopDown3DBigArmCargo>(scene).Single(); var cache = All<TopDown3DResourceCache>(scene).Single();
            foreach (var owner in new Object[] { kit, cargo, cache })
            {
                var edit = new SerializedObject(owner); edit.FindProperty("itemCatalog").objectReferenceValue = catalog;
                edit.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
            }
            root.GetComponent<CampLoopSalvageSession>().Configure(catalog, kit, cargo, cache, root.GetComponent<TopDown3DSalvageWreckState>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.GetComponent<CampLoopSalvageSession>());
            AssetDatabase.SaveAssets(); Validate(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Camp salvage scene save failed.");
            ExportCandidate(scene);
            Debug.Log("CAMP_SALVAGE_INSTALLED: " + position);
        }
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        private static Material Material(string name, Color color, float metallic)
        {
            var value = new Material(Shader.Find("Universal Render Pipeline/Lit")); value.SetColor("_BaseColor", color);
            value.SetFloat("_Metallic", metallic); value.SetFloat("_Smoothness", .25f);
            AssetDatabase.CreateAsset(value, Art + "/" + name + ".mat"); return value;
        }
        private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collision)
        {
            var value = GameObject.CreatePrimitive(PrimitiveType.Cube); value.name = name; value.transform.SetParent(parent, false);
            value.transform.localPosition = position; value.transform.localScale = size; value.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(value.GetComponent<Collider>()); return value;
        }
        public static bool Ground(Scene scene, Vector3 position, out Vector3 ground)
        {
            foreach (var terrain in All<TerrainCollider>(scene))
                if (terrain.Raycast(new Ray(position + Vector3.up * 1000, Vector3.down), out var hit, 2000)) { ground = hit.point; return hit.normal.y > .85f; }
            ground = default; return false;
        }
        private static Vector3 FindPlacement(Scene scene)
        {
            var camp = TopDown3DWaypointCampSettings.Load();
            for (var candidate = 0; candidate < 32; candidate++)
            {
                var angle = candidate * Mathf.PI / 16; var p = camp.position + new Vector3(Mathf.Cos(angle) * 28, 0, Mathf.Sin(angle) * 28);
                if (!Ground(scene, p, out var ground) || camp.returnRoute.Any(r => Vector3.ProjectOnPlane(r - ground, Vector3.up).magnitude < 6)) continue;
                var bounds = new Bounds(ground + Vector3.up, new Vector3(9, 4, 7));
                if (All<Collider>(scene).Any(c => !(c is TerrainCollider) && c.enabled && c.bounds.Intersects(bounds))) continue;
                if (!Ground(scene, ground + new Vector3(-1.35f, 0, -2), out _) || !Ground(scene, ground + new Vector3(1.35f, 0, -2), out _)) continue;
                return ground;
            }
            throw new InvalidOperationException("No clear, supported salvage site near camp; scene remains unchanged.");
        }
        public static void ValidateFromCli() => Validate(EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene));
        public static void Validate(Scene scene)
        {
            CampLoopTestingSceneBuilder.Validate(scene);
            var wreck = All<TopDown3DSalvageWreckState>(scene).Single(); var session = All<CampLoopSalvageSession>(scene).Single();
            var points = All<TopDown3DSalvagePoint>(scene);
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(CatalogPath);
            if (All<TopDown3DAnimationPlaygroundSession>(scene).Length != 1 || points.Length != 3 || !wreck.IsValid
                || points.Select(p => p.StableId).Distinct().Count() != 3 || catalog == null || !catalog.TryValidate(out var error)
                || All<TopDown3DPlayerInventory>(scene).Single().ItemCatalog != catalog || All<TopDown3DBigArmCargo>(scene).Single().ItemCatalog != catalog
                || All<TopDown3DResourceCache>(scene).Single().ItemCatalog != catalog) throw new InvalidOperationException("Salvage scene/catalog/identity validation failed.");
            foreach (var point in points)
                if (!Ground(scene, point.InteractionPoint + Vector3.back * 1.2f, out _)) throw new InvalidOperationException("Unsupported salvage approach.");
            Debug.Log("CAMP_SALVAGE_VALIDATED: one dead wreck, three unique points, private inventory catalog, native support, lab marker.");
        }
        private static void ExportCandidate(Scene scene)
        {
            var paths = AssetDatabase.GetDependencies(new[] { scene.path, CatalogPath, "Assets/_Project/Settings/World/TopDown3DResourceCatalog.asset",
                "Assets/_Project/Settings/Camping/Resources/TopDown3DWaypointCampSettings.asset" }, true)
                .Where(p => p.StartsWith("Assets/")).Concat(Directory.GetFiles("Assets/_Project/Scripts", "*", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles("Assets/_Project/Tests", "*", SearchOption.AllDirectories)).Select(p => p.Replace('\\', '/')).Distinct().ToArray();
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/camp-salvage-candidate-files.txt", paths);
        }
    }
}
