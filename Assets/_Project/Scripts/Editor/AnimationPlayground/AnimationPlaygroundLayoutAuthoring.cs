using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundLayoutAuthoring
    {
        public const string CandidateMarker = ".animation-playground-validation-project";
        [Serializable] public sealed class TerrainIdentity { public string path, guid, sha256; }
        [Serializable] public sealed class Placement
        {
            public string id, prefab, supportGuid;
            public Vector3 position;
            public float minimumGround, maximumGround, maximumSlope, rampLength, rampRise, rampYaw;
            public Vector3 rampToe;
        }
        [Serializable] public sealed class Layout
        {
            public int schema = 1;
            public string scene = TopDown3DAnimationPlaygroundSession.ScenePath;
            public string state, sourceSceneHash;
            public Vector3 campOrigin, translation;
            public Placement[] stations;
            public string[] rejectedCandidates;
            public TerrainIdentity[] terrains;
        }
        // Row order follows the planned four-row/three-column playground diagram.
        private static readonly int[] Grid = { 2, 3, 4, 5, 6, 12, 7, 1, 11, 8, 9, 10 };

        public static Layout SampleLayout(Scene scene)
        {
            var session = TopDown3DAnimationPlaygroundSession.Find(scene);
            if (session == null || session.Player == null) throw new InvalidDataException("Restore the calibrated authored lab first.");
            var terrains = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Terrain>(true)).ToArray();
            if (terrains.Length != 9 || terrains.Any(value => value.terrainData == null || !value.isActiveAndEnabled
                || value.GetComponent<TerrainCollider>()?.terrainData != value.terrainData || value.transform.lossyScale != Vector3.one))
                throw new InvalidDataException("Preserve all nine native, active, unscaled terrain/collider pairs.");
            var colliders = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Collider>(true))
                .Where(value => value.enabled && !value.isTrigger && value is not TerrainCollider
                    && value.GetComponentInParent<Rigidbody>() == null && !value.transform.IsChildOf(session.transform)).ToArray();
            var camp = session.Player.transform.position;
            var rejected = new List<string>();
            foreach (var translation in CandidateTranslations())
            {
                try
                {
                    var entries = new List<Placement>();
                    for (var cell = 0; cell < Grid.Length; cell++)
                    {
                        var number = Grid[cell]; var centre = camp + new Vector3(90 + cell % 3 * 40, 0, 80 + cell / 3 * 40) + translation;
                        var min = float.PositiveInfinity; var max = float.NegativeInfinity; var slope = 0f;
                        for (var x = -14; x <= 14; x++) for (var z = -12; z <= 12; z++)
                        {
                            Ground(terrains, centre + new Vector3(x, 0, z), out var height, out var normal, out _);
                            min = Mathf.Min(min, height); max = Mathf.Max(max, height); slope = Mathf.Max(slope, Vector3.Angle(Vector3.up, normal));
                        }
                        // The walking surface is a flat deck. Native relief controls leg height; approach slope is checked separately.
                        if (max - min > 3.5f) throw new InvalidDataException("AP" + number.ToString("00") + $" relief {max-min:F3}m exceeds the bounded support-leg height.");
                        centre.y = max + .18f;
                        var pad = new Bounds(centre + Vector3.up * 2.5f, new Vector3(29, 5.4f, 25));
                        foreach (var collider in colliders)
                            if (collider.bounds.Intersects(pad)) throw new InvalidDataException("AP" + number.ToString("00") + " conflicts with " + collider.name);
                        if (new Vector2(centre.x - camp.x, centre.z - camp.z).magnitude < 42)
                            throw new InvalidDataException("Preserve the camp reserve.");
                        var approach = FindApproach(terrains, centre);
                        Ground(terrains, centre, out _, out _, out var support);
                        entries.Add(new Placement { id = "AP" + number.ToString("00"), prefab = AnimationPlaygroundStationAuthoring.StationPath(number),
                            position = centre, minimumGround = min, maximumGround = max, maximumSlope = slope, rampLength = approach.rampLength,
                            rampRise = approach.rampRise, rampToe = approach.rampToe, rampYaw = approach.rampYaw,
                            supportGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(support.terrainData)) });
                    }
                    return new Layout { state = "sampled-native-ground", campOrigin = camp, translation = translation, stations = entries.OrderBy(value => value.id).ToArray(),
                        rejectedCandidates = rejected.ToArray(), sourceSceneHash = Hash(scene.path), terrains = terrains.Select(value =>
                        { var path=AssetDatabase.GetAssetPath(value.terrainData); return new TerrainIdentity{path=path,guid=AssetDatabase.AssetPathToGUID(path),sha256=Hash(path)}; }).ToArray() };
                }
                catch (InvalidDataException error) { rejected.Add(translation + ": " + error.Message); }
            }
            throw new InvalidDataException("No bounded layout fits the reviewed search. " + string.Join(" | ", rejected));
        }

        private static IEnumerable<Vector3> CandidateTranslations()
        {
            yield return Vector3.zero;
            for (var ring = 1; ring <= 15; ring++)
                foreach (var direction in new[] { Vector3.right, Vector3.forward, Vector3.left, Vector3.back, new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1) })
                    yield return direction * ring * 20;
        }
        private static Placement FindApproach(Terrain[] terrains, Vector3 centre)
        {
            foreach(var length in new[]{4f,6f,8f,10f})
                foreach(var outward in new[]{Vector3.back,Vector3.right,Vector3.forward,Vector3.left})
                {
                    var edge = outward.x != 0 ? 14f : 12f;
                    var toe = outward * (edge + length); var across = Vector3.Cross(Vector3.up,outward);
                    try
                    {
                        var h=float.NegativeInfinity;
                        foreach(var lateral in new[]{-1.5f,0,1.5f})
                        {Ground(terrains,centre+toe+across*lateral,out var toeHeight,out _,out _);h=Mathf.Max(h,toeHeight);}
                        var rise=centre.y-h-.05f;
                        if(Mathf.Abs(Mathf.Atan2(rise,length)*Mathf.Rad2Deg)>14)continue;
                        var clear=true;
                        for(var step=0;step<=Mathf.CeilToInt(length);step++)
                        {
                            var t=step/Mathf.Ceil(length); var p=centre+toe-outward*length*t;
                            foreach(var lateral in new[]{-1.5f,0,1.5f})
                            {Ground(terrains,p+across*lateral,out var ground,out _,out _);if(ground>Mathf.Lerp(h+.05f,centre.y,t)+.01f)clear=false;}
                        }
                        if(!clear)continue;
                        toe.y=-rise;
                        return new Placement{rampLength=length,rampRise=rise,rampToe=toe,rampYaw=Mathf.Atan2(-outward.x,-outward.z)*Mathf.Rad2Deg};
                    }
                    catch(InvalidDataException){}
                }
            throw new InvalidDataException("No 3m-wide approach ramp clears the native ground within 4–10m length and 14 degrees.");
        }
        public static void Ground(Terrain[] terrains, Vector3 point, out float height, out Vector3 normal, out Terrain support)
        {
            foreach (var terrain in terrains)
            {
                var origin = terrain.transform.position; var size = terrain.terrainData.size;
                if (point.x < origin.x || point.x > origin.x + size.x || point.z < origin.z || point.z > origin.z + size.z) continue;
                var x = (point.x - origin.x) / size.x; var z = (point.z - origin.z) / size.z;
                var holes = terrain.terrainData.holesResolution;
                if (holes > 0 && terrain.terrainData.IsHole(Mathf.Min(holes - 1, Mathf.FloorToInt(x * holes)), Mathf.Min(holes - 1, Mathf.FloorToInt(z * holes))))
                    throw new InvalidDataException("The authored footprint crosses a terrain hole.");
                height = origin.y + terrain.SampleHeight(point); normal = terrain.transform.TransformDirection(terrain.terrainData.GetInterpolatedNormal(x, z)); support = terrain; return;
            }
            throw new InvalidDataException("The authored footprint leaves the nine-tile boundary.");
        }

        public static void Install(Layout layout)
        {
            var scene = SceneManager.GetActiveScene(); var session = TopDown3DAnimationPlaygroundSession.Find(scene);
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || session == null
                || layout == null || layout.schema != 1 || layout.scene != scene.path || layout.sourceSceneHash != Hash(scene.path))
                throw new InvalidOperationException("Install an explicit sampled recipe into its unchanged, saved, already-loaded lab.");
            if (session.Stations.Length != 0 || session.transform.Find("Stations") != null) throw new InvalidOperationException("Preserve an existing owned station installation.");
            if (layout.stations == null || layout.stations.Length != 12 || layout.stations.Select(value => value.id).Distinct().Count() != 12)
                throw new InvalidDataException("Supply all twelve distinct station placements.");
            if(!TerrainIdentitiesMatch(layout))
                throw new InvalidDataException("Preserve the sampled native TerrainData identities and exact bytes before installation.");
            foreach (var entry in layout.stations)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefab)?.GetComponent<TopDown3DAnimationPlaygroundStation>()?.StationId != entry.id)
                    throw new InvalidDataException("Restore the matching authored station prefab: " + entry.id);
            var root = new GameObject("Stations"); root.transform.SetParent(session.transform, false); Undo.RegisterCreatedObjectUndo(root, "Install animation station shells");
            var stations = new List<TopDown3DAnimationPlaygroundStation>(); var returns = new List<Transform>();
            var initialReturn = session.transform.Find("Booter Safe Return");
            if (initialReturn == null) { Object.DestroyImmediate(root); throw new InvalidDataException("Preserve the original player return anchor."); }
            returns.Add(initialReturn);
            try
            {
                foreach (var entry in layout.stations)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefab), root.transform);
                    instance.transform.position = entry.position; instance.transform.localRotation = Quaternion.identity; instance.transform.localScale = Vector3.one;
                    var station = instance.GetComponent<TopDown3DAnimationPlaygroundStation>(); stations.Add(station); returns.Add(station.SafeReturn);
                    var approach=new GameObject("Native-ground approach");approach.transform.SetParent(instance.transform,false);
                    approach.transform.localPosition=entry.rampToe; approach.transform.localRotation=Quaternion.Euler(0,entry.rampYaw,0);
                    AnimationPlaygroundStationAuthoring.Ramp(approach.transform, "Ramp", Vector3.zero, 3, entry.rampLength + .2f, entry.rampRise);
                    AddSupportsAndGuards(instance.transform,entry);
                }
                Undo.RecordObject(session, "Bind twelve owned stations"); session.ConfigureStations(stations.ToArray()); session.ConfigurePlayer(session.Player, returns.ToArray());
                EditorUtility.SetDirty(session); Physics.SyncTransforms(); EditorSceneManager.MarkSceneDirty(scene);
            }
            catch
            {
                Object.DestroyImmediate(root); session.ConfigureStations(Array.Empty<TopDown3DAnimationPlaygroundStation>()); session.ConfigurePlayer(session.Player, new[] { initialReturn }); throw;
            }
        }
        public static bool TerrainIdentitiesMatch(Layout layout)
            => layout?.terrains != null && layout.terrains.Length==9 && layout.terrains.All(value=>value!=null
                && value.path!=null && value.path.StartsWith("Assets/_Project/Art/Terrain/",StringComparison.Ordinal)
                && File.Exists(value.path) && AssetDatabase.AssetPathToGUID(value.path)==value.guid && Hash(value.path)==value.sha256);
        private static void AddSupportsAndGuards(Transform root,Placement entry)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AnimationPlayground/Deck.mat");
            var terrains=root.gameObject.scene.GetRootGameObjects().SelectMany(value=>value.GetComponentsInChildren<Terrain>(true)).ToArray();
            foreach(var x in new[]{-12f,12f})foreach(var z in new[]{-10f,10f})
            {
                Ground(terrains,root.position+new Vector3(x,0,z),out var ground,out _,out _); var height=root.position.y-ground;
                Box(root,"Support leg",new Vector3(x,-height*.5f,z),new Vector3(.3f,height,.3f),material);
            }
            var outward=entry.rampToe;outward.y=0;outward.Normalize();
            foreach(var side in new[]{Vector3.back,Vector3.right,Vector3.forward,Vector3.left})
            {
                var half=side.x!=0 ? 12f : 14f;var edge=side.x!=0 ? 13.85f : 11.85f;
                var across=Vector3.Cross(Vector3.up,side);
                if(Vector3.Dot(side,outward)>.99f)
                    foreach(var sign in new[]{-1f,1f})Box(root,"Approach guard",side*edge+across*sign*(half*.5f+.9f)+Vector3.up*.5f,
                        side.x!=0 ? new Vector3(.12f,1,half-1.8f) : new Vector3(half-1.8f,1,.12f),material);
                else Box(root,"Deck perimeter guard",side*edge+Vector3.up*.5f,
                    side.x!=0 ? new Vector3(.12f,1,24) : new Vector3(28,1,.12f),material);
            }
        }
        private static void Box(Transform root,string name,Vector3 position,Vector3 scale,Material material)
        {var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.transform.SetParent(root,false);box.transform.localPosition=position;box.transform.localScale=scale;box.GetComponent<Renderer>().sharedMaterial=material;}

        public static void PrepareCandidateFromCli()
        {
            if (!Application.isBatchMode || Directory.Exists(".git") || File.Exists(".git") || !File.Exists(CandidateMarker))
                throw new InvalidOperationException("This entry point only authors the explicitly marked disposable validation copy.");
            var evidence = Path.GetFullPath("StationBuild"); Directory.CreateDirectory(evidence);
            try
            {
                var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Single);
                var existing = Enumerable.Range(1, 12).Count(number => File.Exists(AnimationPlaygroundStationAuthoring.StationPath(number)));
                if(existing == 0) AnimationPlaygroundStationAuthoring.CreatePrefabs();
                else if(existing != 12) throw new InvalidDataException("Preserve and inspect the incomplete owned prefab preparation before retrying.");
                AnimationPlaygroundStationAuthoring.RepairPreparedActionMarkers();
                var layout = SampleLayout(scene); File.WriteAllText(Path.Combine(evidence, "layout.json"), JsonUtility.ToJson(layout, true) + "\n");
                Install(layout);
                var report = AnimationPlaygroundSceneValidator.Validate(scene);
                File.WriteAllText(Path.Combine(evidence, "scene-validation.json"), JsonUtility.ToJson(report, true) + "\n");
                if (!report.valid) throw new InvalidDataException(string.Join("; ", report.errors));
                // Preserve the baseline cargo reference that legacy OnValidate resolves while loading.
                var action = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerActionController>(scene); var serialized = new SerializedObject(action);
                serialized.FindProperty("bigArmCargo").objectReferenceValue = null; serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(evidence, "completed.json"), "{\"state\":\"candidate-stations-saved-and-validated\"}\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(Path.Combine(evidence, "failure.txt"), error.ToString()); Debug.LogException(error); EditorApplication.Exit(1); }
        }

        public static void InstallPreparedAuthorSceneFromCli()
        {
            if(!Application.isBatchMode || !Directory.Exists(".git") || File.Exists(CandidateMarker)
                || File.ReadAllText(".git/HEAD").Trim()!="ref: refs/heads/main")
                throw new InvalidOperationException("Install the prepared author scene only through background batchmode on repository main.");
            var evidence=Path.GetFullPath("Logs/animation-station-main-install-20261014");Directory.CreateDirectory(evidence);
            try
            {
                var recipePath="Docs/Evidence/Gameplay/AnimationPlaygroundImplementation20261013/Stations/layout.json";
                var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(recipePath));
                if(layout==null || layout.sourceSceneHash!=Hash(TopDown3DAnimationPlaygroundSession.ScenePath) || !TerrainIdentitiesMatch(layout))
                    throw new InvalidDataException("The sealed author scene or terrain baseline changed.");
                var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath,OpenSceneMode.Single);
                Install(layout);
                var report=AnimationPlaygroundSceneValidator.Validate(scene);
                File.WriteAllText(Path.Combine(evidence,"validation.json"),JsonUtility.ToJson(report,true)+"\n");
                if(!report.valid)throw new InvalidDataException(string.Join("; ",report.errors));
                var action=TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerActionController>(scene);
                var serialized=new SerializedObject(action);
                serialized.FindProperty("bigArmCargo").objectReferenceValue=null;serialized.ApplyModifiedPropertiesWithoutUndo();
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("The author scene save failed.");
                if(!TerrainIdentitiesMatch(layout))throw new InvalidDataException("A native terrain changed during installation.");
                File.WriteAllText(Path.Combine(evidence,"completed.json"),"{\"state\":\"twelve-author-scene-stations-installed\",\"sceneSha256\":\""+Hash(scene.path)+"\"}\n");
                EditorApplication.Exit(0);
            }
            catch(Exception error){File.WriteAllText(Path.Combine(evidence,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }
        public static string Hash(string path) { using var stream = File.OpenRead(path); using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        [Serializable] private sealed class Survey { public Placement[] candidates; }
        public static void SurveyCandidateFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(CandidateMarker))throw new InvalidOperationException("Use the marked validation copy.");
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath,OpenSceneMode.Single);
            var terrains=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Terrain>(true)).ToArray();
            var origin=TopDown3DAnimationPlaygroundSession.Find(scene).Player.transform.position;
            var entries=new List<Placement>();
            for(var x=-200;x<=400;x+=20)for(var z=-200;z<=400;z+=20)
            {
                var centre=origin+new Vector3(x,0,z);var min=float.PositiveInfinity;var max=float.NegativeInfinity;var slope=0f;
                try
                {
                    for(var dx=-14;dx<=14;dx+=2)for(var dz=-12;dz<=12;dz+=2)
                    {Ground(terrains,centre+new Vector3(dx,0,dz),out var h,out var normal,out _);min=Mathf.Min(min,h);max=Mathf.Max(max,h);slope=Mathf.Max(slope,Vector3.Angle(Vector3.up,normal));}
                    entries.Add(new Placement{position=centre,minimumGround=min,maximumGround=max,maximumSlope=slope});
                }
                catch(InvalidDataException){}
            }
            Directory.CreateDirectory("StationBuild");
            File.WriteAllText("StationBuild/ground-survey.json",JsonUtility.ToJson(new Survey{candidates=entries.ToArray()},true)+"\n"); EditorApplication.Exit(0);
        }
    }
}
