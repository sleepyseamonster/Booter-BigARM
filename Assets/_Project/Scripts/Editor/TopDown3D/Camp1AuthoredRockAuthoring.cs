using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class Camp1AuthoredRockAuthoring
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/TopDown3D/Environment/Camp1AuthoredRocks.prefab";
        public const string RootName = "Camp 1 · Authored Rocks";
        private const string ScenePath = TopDown3DWaypointCampSettings.ProductionScene;
        private static readonly string[] Families = { "ArtistVolcanicRock01", "ArtistFlatVolcanicRock02", "ArtistVolcanicRock03",
            "ArtistVolcanicRock04", "ArtistVolcanicRock05", "ArtistVolcanicRock06", "ArtistVolcanicRock07" };
        // Deliberate authored poses, never sampled randomly or regenerated at runtime.
        private static readonly Vector2[] Offsets = { new(-14, -10), new(8, -15), new(20, -4),
            new(-12, 19), new(-23, -2), new(24, 7), new(-6, -23) };
        private static readonly float[] Yaws = { 25, 112, -40, -15, 72, 150, 245 };

        public static string BuildForLoadedScene()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Author from the saved clean Greater Wasteland in Edit Mode.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("Preserve the existing authored arrangement; validate it instead of rebuilding.");
            var camp = TopDown3DWaypointCampSettings.Load();
            if (camp == null || !camp.TryValidate(out _) || camp.siteId != "camp.starting") throw new InvalidOperationException("Camp 1 definition is missing.");
            var index = new FixedTerrainSurfaceIndex(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Terrain>(true)), camp.worldId);
            var protectedAreas = ProtectedAreas(scene, camp);
            var previous = SceneManager.GetActiveScene();
            var staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var root = new GameObject(RootName);
            try
            {
                var members = new List<TopDown3DCampRockArrangement.Member>();
                for (var i = 0; i < Families.Length; i++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/TopDown3D/Environment/" + Families[i] + ".prefab");
                    if (prefab == null) throw new InvalidOperationException("Completed rock prefab missing: " + Families[i]);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, staging);
                    instance.name = $"Camp Rock {i + 1:00} · {Families[i]}"; instance.transform.SetParent(root.transform, false);
                    var xz = new Vector2(camp.position.x, camp.position.z) + Offsets[i];
                    if (!index.TrySample(xz, out var support) || support.SlopeDegrees > 18f)
                        throw new InvalidOperationException($"Authored rock {i + 1} lacks gentle native support; adjust its explicit pose.");
                    instance.transform.SetPositionAndRotation(support.Position,
                        Quaternion.FromToRotation(Vector3.up, support.Normal) * Quaternion.Euler(0, Yaws[i], 0));
                    FitFootprint(instance, index);
                    var bounds = CollisionBounds(instance);
                    RequireClear(bounds, protectedAreas);
                    foreach (var other in members)
                        if (ExpandedXZ(CollisionBounds(other.instance.gameObject), 1f).Intersects(bounds))
                            throw new InvalidOperationException("Authored rocks overlap or leave insufficient spacing.");
                    Physics.SyncTransforms();
                    foreach (var hit in Physics.OverlapBox(bounds.center, bounds.extents + Vector3.one * .15f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                        if (!(hit is TerrainCollider) && !hit.transform.IsChildOf(root.transform))
                            throw new InvalidOperationException("Rock placement overlaps existing production content: " + hit.name);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                    members.Add(new TopDown3DCampRockArrangement.Member { placementId = $"greater-wasteland:camp1:rock:v1:{i + 1:D2}",
                        assetId = Families[i], instance = instance.transform, groundAnchor = support.Position });
                }
                root.AddComponent<TopDown3DCampRockArrangement>().Configure(camp.position, members.ToArray());
                ValidateRoot(root, index, protectedAreas);
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null) throw new IOException("Arrangement prefab could not be saved.");
                Directory.CreateDirectory("Logs/Camp1Rocks");
                File.WriteAllText("Logs/Camp1Rocks/poses.json", JsonUtility.ToJson(new PoseReport { placements = members.Select(m => new Pose {
                    id=m.placementId, asset=m.assetId, position=m.instance.position, rotation=m.instance.rotation, ground=m.groundAnchor }).ToArray() }, true));
                return "Seven authored rocks fitted and validated around Camp 1; production scene not yet changed.";
            }
            finally { EditorSceneManager.CloseScene(staging, true); SceneManager.SetActiveScene(previous); }
        }

        public static string InstallForLoadedScene()
        {
            var scene=SceneManager.GetSceneByPath(ScenePath);
            if(!scene.IsValid() || !scene.isLoaded || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Install only into the clean saved production scene in Edit Mode.");
            if(scene.GetRootGameObjects().Any(r=>r.GetComponent<TopDown3DCampRockArrangement>()!=null)) return ValidateLoaded();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if(prefab==null) throw new InvalidOperationException("Author and validate the bounded arrangement first.");
            var before=File.ReadAllText(ScenePath);
            const string temporary="Assets/_Project/Scenes/Reference/__Camp1RockIntegration_20261012.unity";
            if(File.Exists(temporary)) throw new InvalidOperationException("Preserve existing integration scratch scene before retrying.");
            var previous=SceneManager.GetActiveScene();
            var staging=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,staging); instance.name=RootName;
                var camp=TopDown3DWaypointCampSettings.Load();
                ValidateRoot(instance,new FixedTerrainSurfaceIndex(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Terrain>(true)),camp.worldId),ProtectedAreas(scene,camp));
                // Serialize a miniature integration scene. Never resave the existing terrain/actors.
                if(!EditorSceneManager.SaveScene(staging,temporary)) throw new IOException("Could not serialize integration fragment.");
                Directory.CreateDirectory("Logs/Camp1Rocks"); File.Copy(temporary,"Logs/Camp1Rocks/fragment.unity",true);
                var patched=MergeNewRoot(before,File.ReadAllText(temporary),AssetDatabase.AssetPathToGUID(PrefabPath));
                if(File.ReadAllText(ScenePath)!=before) throw new InvalidOperationException("Production changed during authoring; preserve the other writer.");
                Directory.CreateDirectory("Logs/Camp1Rocks"); File.WriteAllText("Logs/Camp1Rocks/GreaterWasteland.before.unity",before,new UTF8Encoding(false));
                File.WriteAllText(ScenePath,patched,new UTF8Encoding(false));
            }
            catch(Exception exception) { Debug.LogError("CAMP1_INSTALL_FAILED: "+exception.Message); throw; }
            finally
            {
                EditorSceneManager.CloseScene(staging,true); SceneManager.SetActiveScene(previous);
                if(File.Exists(temporary)) AssetDatabase.DeleteAsset(temporary);
            }
            AssetDatabase.ImportAsset(ScenePath); EditorSceneManager.OpenScene(ScenePath);
            return ValidateLoaded();
        }
        internal static string MergeNewRoot(string original,string serialized,string prefabGuid)
        {
            var header=new Regex(@"^--- !u!(\d+) &(\d+)[^\r\n]*",RegexOptions.Multiline);
            Dictionary<string,string> Documents(string text)
            {
                var matches=header.Matches(text); var result=new Dictionary<string,string>();
                for(var i=0;i<matches.Count;i++) result.Add(matches[i].Groups[2].Value,
                    text.Substring(matches[i].Index,(i+1<matches.Count?matches[i+1].Index:text.Length)-matches[i].Index));
                return result;
            }
            var before=Documents(original); var after=Documents(serialized);
            const string rootsId="9223372036854775807";
            if(!before.ContainsKey(rootsId) || !after.ContainsKey(rootsId))
                throw new InvalidOperationException("Integration copy lost existing scene objects.");
            var ids=new Regex(@"^  - \{fileID: (\d+)\}",RegexOptions.Multiline);
            var oldRoots=ids.Matches(before[rootsId]).Cast<Match>().Select(m=>m.Groups[1].Value).ToArray();
            var newRoots=ids.Matches(after[rootsId]).Cast<Match>().Select(m=>m.Groups[1].Value).ToArray();
            var addedRoots=newRoots.Except(oldRoots).ToArray(); var added=after.Where(p=>!before.ContainsKey(p.Key))
                .Select(p=>p.Value).Where(doc=>doc.StartsWith("--- !u!1001") || doc.StartsWith("--- !u!4 ") || doc.StartsWith("--- !u!1 ") || doc.StartsWith("--- !u!114 ")).ToArray();
            if(addedRoots.Length!=1 || added.Length<1
                || added.Count(doc=>doc.StartsWith("--- !u!1001"))!=1
                || !added.Any(doc=>doc.StartsWith("--- !u!1001") && doc.Contains("guid: "+prefabGuid))
                || added.Any(doc=>!doc.StartsWith("--- !u!1001") && !doc.StartsWith("--- !u!4 ") && !doc.StartsWith("--- !u!1 ") && !doc.StartsWith("--- !u!114 ")))
                throw new InvalidOperationException("Integration must add exactly one linked prefab root and its reference documents.");
            var newline=original.Contains("\r\n")?"\r\n":"\n";
            var roots=before[rootsId].TrimEnd('\r','\n')+newline+"  - {fileID: "+addedRoots[0]+"}"+newline;
            return original.Replace(before[rootsId],roots).TrimEnd('\r','\n')+newline
                +string.Join("",added).Replace("\r\n","\n").Replace("\n",newline);
        }

        private static void FitFootprint(GameObject instance, FixedTerrainSurfaceIndex index)
        {
            var filter = instance.GetComponentsInChildren<MeshFilter>().First(f => f.gameObject.name == "LOD0");
            var vertices = filter.sharedMesh.vertices;
            var lowest = vertices.Min(v => v.y); var band = Mathf.Min(.06f, filter.sharedMesh.bounds.size.y * .08f);
            var contacts = vertices.Where(v => v.y <= lowest + band).Select(v => filter.transform.TransformPoint(v)).ToArray();
            if (contacts.Length < 3) throw new InvalidOperationException("Rock has no measurable contact footprint.");
            var gaps = new List<float>();
            foreach (var contact in contacts)
            {
                if (!index.TrySample(new Vector2(contact.x, contact.z), out var support) || support.SlopeDegrees > 22f)
                    throw new InvalidOperationException("Rock contact footprint crosses a terrain hole or steep edge.");
                gaps.Add(contact.y - support.Position.y);
            }
            // Fit the actual low contact vertices, keeping terrain penetration and edge gaps bounded.
            var shift = -.5f * (gaps.Max() + gaps.Min());
            instance.transform.position += Vector3.up * shift;
            var tolerance = Mathf.Min(.18f, filter.sharedMesh.bounds.size.y * .2f);
            if (gaps.Max() + shift > tolerance || gaps.Min() + shift < -tolerance)
                throw new InvalidOperationException(instance.name + " contact variation " + (gaps.Max()-gaps.Min()).ToString("F3")
                    + " exceeds " + (tolerance*2).ToString("F3") + "; choose a flatter authored position.");
        }
        internal static Bounds CollisionBounds(GameObject instance)
        {
            var collider = instance.GetComponentInChildren<MeshCollider>();
            if (collider == null || !collider.convex || collider.sharedMesh == null) throw new InvalidOperationException("Completed collision mesh is missing.");
            var mesh = collider.sharedMesh.bounds; var bounds = new Bounds(collider.transform.TransformPoint(mesh.center), Vector3.zero);
            foreach (var corner in Corners(mesh)) bounds.Encapsulate(collider.transform.TransformPoint(corner));
            return bounds;
        }
        private static IEnumerable<Vector3> Corners(Bounds bounds)
        { foreach (var x in new[] {-1,1}) foreach (var y in new[] {-1,1}) foreach (var z in new[] {-1,1}) yield return bounds.center + Vector3.Scale(bounds.extents, new Vector3(x,y,z)); }
        private static Bounds ExpandedXZ(Bounds b, float margin)
        { b.Expand(new Vector3(margin * 2, 0, margin * 2)); return b; }
        internal static void RequireClear(Bounds rock, IEnumerable<Bounds> exclusions)
        { foreach (var exclusion in exclusions) if (rock.Intersects(exclusion)) throw new InvalidOperationException(
            "Authored rock at " + rock.center.ToString("F2") + " obstructs protected gameplay or saved pose at " + exclusion.center.ToString("F2")); }
        private static Bounds Area(Vector3 point, float radius) => new(point + Vector3.up * 2, new Vector3(radius * 2, 16, radius * 2));
        private static List<Bounds> ProtectedAreas(Scene scene, TopDown3DWaypointCampSettings camp)
        {
            var areas = new List<Bounds> { Area(camp.position, 6f) };
            for (var i = 1; i < camp.returnRoute.Length; i++)
            {
                var a = camp.returnRoute[i-1]; var b = camp.returnRoute[i]; var count = Mathf.CeilToInt(Vector3.Distance(a,b) / .5f);
                for (var j=0;j<=count;j++) areas.Add(Area(Vector3.Lerp(a,b,(float)j/count), camp.corridorRadius + 2f));
            }
            foreach (var actor in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
                if (actor is TopDown3DPlayerMotor || actor is TopDown3DBigArmCargo) areas.Add(Area(actor.transform.position, 5f));
            var cache = AssetDatabase.LoadAssetAtPath<GameObject>(GreaterWastelandInventoryAuthoring.CachePath);
            if (cache != null) areas.Add(Area(cache.transform.position, 5f));
            var ore = AssetDatabase.LoadAssetAtPath<GameObject>(GreaterWastelandIronstoneAuthoring.AreaPrefabPath);
            if (ore != null) foreach (var node in ore.GetComponentsInChildren<TopDown3DAuthoredIronstoneNode>()) areas.Add(Area(node.transform.position, 4f));
            var savePath = Path.Combine(Application.persistentDataPath,"GreaterWasteland","inventory-v1.json");
            foreach (var path in new[] { savePath, savePath + ".backup", savePath + ".pending" })
            {
                if (!File.Exists(path)) continue;
                if (!GreaterWastelandInventoryFile.Read(path, out var saved) || saved.worldId != camp.worldId
                    || !GreaterWastelandInventorySnapshot.Finite(saved.booterPosition) || !GreaterWastelandInventorySnapshot.Finite(saved.leggerPosition))
                    throw new InvalidOperationException("Preserve and inspect incompatible checkpoint before adding obstacles: " + path);
                areas.Add(Area(saved.booterPosition, 5f)); areas.Add(Area(saved.leggerPosition, 5f));
                if (saved.campDeployed) areas.Add(Area(saved.camp.position, 5f));
                if (saved.harvesters != null) foreach (var record in saved.harvesters.Records)
                    areas.Add(Area(new Vector3((float)record.Position.HorizontalA,(float)record.Position.Vertical,(float)record.Position.HorizontalB),3f));
            }
            return areas;
        }
        public static string ValidateLoaded()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath); var camp=TopDown3DWaypointCampSettings.Load();
            var roots=scene.GetRootGameObjects(); var arrangements=roots.SelectMany(r=>r.GetComponentsInChildren<TopDown3DCampRockArrangement>(true)).ToArray();
            if(arrangements.Length!=1) throw new InvalidOperationException("Expected exactly one Camp 1 arrangement in production.");
            ValidateRoot(arrangements[0].gameObject,new FixedTerrainSurfaceIndex(roots.SelectMany(r=>r.GetComponentsInChildren<Terrain>(true)),camp.worldId),ProtectedAreas(scene,camp));
            TopDown3DWaypointCampValidator.ValidateLoadedProduction();
            return "CAMP1_ROCKS_VALIDATED: seven linked decorative rocks, native support, protected camp/route/resources/saved poses.";
        }
        private static void ValidateRoot(GameObject root, FixedTerrainSurfaceIndex index, List<Bounds> areas)
        {
            var layout=root.GetComponent<TopDown3DCampRockArrangement>(); var members=layout.Members;
            if(layout.WorldId!=GreaterWastelandInventorySnapshot.WorldIdentity || layout.SiteId!="camp.starting" || layout.LayoutVersion!=1
                || members.Length!=7 || members.Select(m=>m.placementId).Distinct().Count()!=7
                || root.GetComponentsInChildren<TopDown3DIronstoneNode>(true).Length!=0) throw new InvalidOperationException("Authored identity or decorative-only contract failed.");
            foreach(var member in members)
            {
                if(member.instance==null || !member.instance.IsChildOf(root.transform) || !Families.Contains(member.assetId)
                    || Vector3.ProjectOnPlane(member.instance.position-layout.CampAnchor,Vector3.up).magnitude>28f
                    || !index.TrySample(new Vector2(member.groundAnchor.x,member.groundAnchor.z),out var support)
                    || Mathf.Abs(support.Position.y-member.groundAnchor.y)>.02f) throw new InvalidOperationException("Authored rock pose or scope changed.");
                RequireClear(CollisionBounds(member.instance.gameObject),areas);
                if(member.instance.GetComponent<LODGroup>()?.GetLODs().Length!=3) throw new InvalidOperationException("Rock LOD chain missing.");
            }
        }
        [Serializable] private sealed class Pose { public string id,asset; public Vector3 position,ground; public Quaternion rotation; }
        [Serializable] private sealed class PoseReport { public Pose[] placements; }
    }
}
