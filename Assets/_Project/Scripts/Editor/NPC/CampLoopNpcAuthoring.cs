using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class CampLoopNpcAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/Reference/CampLoopTesting.unity";
        public const string PrefabPath = "Assets/_Project/Prefabs/TopDown3D/NPC/LagerSkittish.prefab";
        public const string DefinitionPath = "Assets/_Project/Settings/NPC/LagerDefinition.asset";
        public const string ProfilePath = "Assets/_Project/Settings/NPC/LagerSkittishProfile.asset";
        public static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        public static void InstallFromCli()
        {
            if (!Application.isBatchMode || Application.isPlaying) throw new InvalidOperationException("Use isolated background Edit Mode authoring.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (All<NpcActor>(scene).Length > 0) { CampLoopNpcValidator.Validate(scene); return; }
            if (All<TopDown3DAnimationPlaygroundSession>(scene).Length != 1) throw new InvalidOperationException("Missing unique session marker.");
            CampLoopTestingSceneBuilder.Validate(scene);
            foreach (var folder in new[] { "Assets/_Project/Settings/NPC", "Assets/_Project/Materials/NPC", "Assets/_Project/Prefabs/TopDown3D/NPC" })
            { if (Directory.Exists(folder)) throw new InvalidOperationException("Uninstalled NPC assets exist; preserve them."); Directory.CreateDirectory(folder); }
            AssetDatabase.Refresh();
            var profile = ScriptableObject.CreateInstance<SkittishNpcProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath);
            var definition = ScriptableObject.CreateInstance<NpcDefinition>(); definition.Configure(profile); AssetDatabase.CreateAsset(definition, DefinitionPath);
            var metal = NewMaterial("LagerMetal", new Color(.28f, .16f, .10f));
            var sensor = NewMaterial("LagerSensor", new Color(.85f, .76f, .53f));
            var root = new GameObject("Lager"); SceneManager.MoveGameObjectToScene(root, scene);
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false; body.interpolation = RigidbodyInterpolation.Interpolate;
            var shape = root.AddComponent<CapsuleCollider>(); shape.radius = .67f; shape.height = 1.4f; shape.center = Vector3.up * .7f;
            var actor = root.AddComponent<NpcActor>();
            var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform, false);
            Block(visual.transform, "Body", new Vector3(0, .75f, 0), new Vector3(.75f, .45f, 1.05f), metal);
            Block(visual.transform, "Front sensor", new Vector3(0, .84f, .57f), new Vector3(.3f, .16f, .1f), sensor);
            var pivots = new Transform[4];
            for (var n = 0; n < 4; n++)
            {
                var side = n < 2 ? -1 : 1; var z = n % 2 == 0 ? -.35f : .35f;
                var pivot = new GameObject("Leg " + (n + 1)); pivot.transform.SetParent(visual.transform, false);
                pivot.transform.localPosition = new Vector3(side * .3f, .7f, z); pivots[n] = pivot.transform;
                var upper = Block(pivot.transform, "Upper", new Vector3(side * .12f, -.17f, 0), new Vector3(.1f, .4f, .1f), metal);
                upper.transform.localRotation = Quaternion.Euler(0, 0, side * 25);
                Block(pivot.transform, "Lower", new Vector3(side * .2f, -.5f, 0), new Vector3(.08f, .3f, .08f), metal);
            }
            root.AddComponent<NpcBlockoutView>().Configure(pivots);
            var motor = root.GetComponent<NpcGroundMotor>();
            var camp = TopDown3DWaypointCampSettings.Load(); var target = All<TopDown3DPlayerMotor>(scene).Single().transform;
            Physics.SyncTransforms();
            var position = FindPlacement(scene, motor, camp.position, target.position, profile.Snapshot());
            root.transform.position = position; body.position = position;
            actor.Configure(definition, target, position, 35);
            Physics.SyncTransforms();
            if (!actor.Initialize(out var reason)) throw new InvalidOperationException(reason);
            actor.SetSimulationAllowed(false);
            // Prefab owns reusable actor/presentation; scene-only references are authored after saving it.
            actor.Configure(definition, null, Vector3.zero, 35);
            root.transform.position = Vector3.zero; body.position = Vector3.zero;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene); root.name = "Camp Loop Lager Trial"; root.transform.position = position;
            var exclusions = All<TopDown3DAnimationPlaygroundStation>(scene).Select(s => new NpcReservation(s.transform.position, 6))
                .Concat(new[] { new NpcReservation(camp.position, 10), new NpcReservation(target.position, 3) }).ToArray();
            actor = root.GetComponent<NpcActor>(); actor.Configure(definition, target, position, 35, exclusions);
            root.AddComponent<CampLoopNpcTrial>().Configure(All<TopDown3DAnimationPlaygroundSession>(scene).Single(), All<TopDown3DInputRouter>(scene).Single(), actor, position);
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("NPC candidate scene save failed.");
            Physics.SyncTransforms(); CampLoopNpcValidator.Validate(scene);
            Debug.Log("CAMP_NPC_INSTALLED: one separate Lager, omnidirectional perception, tunable skittish policy and safe reset.");
        }
        private static Vector3 FindPlacement(Scene scene, NpcGroundMotor motor, Vector3 camp, Vector3 player, NpcTuning t)
        {
            // Native scene tiles are surveyed directly; no terrain edits, generated-world builder or guessed height.
            foreach (var distance in new[] { 28f, 35f }) for (var n = 0; n < 16; n++)
            {
                var direction = Quaternion.Euler(0, n * 22.5f, 0) * Vector3.forward;
                var probe = camp + direction * distance;
                var terrain = All<Terrain>(scene).FirstOrDefault(v => probe.x >= v.transform.position.x && probe.z >= v.transform.position.z
                    && probe.x < v.transform.position.x + v.terrainData.size.x && probe.z < v.transform.position.z + v.terrainData.size.z);
                if (terrain == null) continue;
                probe.y = terrain.SampleHeight(probe) + terrain.transform.position.y + NpcGroundMotor.Skin;
                if (!motor.TryGround(probe, t, false, out var ground) || NpcMath.Distance(ground, player) < 20) continue;
                // Protect actor return/camp areas, station entrances and nearby fixtures with a generous obstacle exclusion.
                var bounds = new Bounds(ground + Vector3.up, new Vector3(12, 4, 12));
                if (All<Collider>(scene).Any(c => !(c is TerrainCollider) && !c.transform.IsChildOf(motor.transform)
                    && c.enabled && c.gameObject.activeInHierarchy && c.bounds.Intersects(bounds))) continue;
                var safe = 0;
                for (var axis = 0; axis < 4; axis++)
                {
                    var previous = ground; var valid = true;
                    for (var step = 1; step <= 6 && valid; step++)
                    {
                        var point = ground + Quaternion.Euler(0, axis * 90, 0) * Vector3.forward * (step * 1.25f);
                        point.y = previous.y;
                        valid = motor.TryGround(point, t, false, out var next) && motor.SegmentClear(previous, next); previous = next;
                    }
                    if (valid) safe++;
                }
                if (safe >= 2) return ground;
            }
            throw new InvalidOperationException("No supported clear NPC anchor with two escape directions; preserve scene.");
        }
        private static Material NewMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) throw new InvalidOperationException("Missing pinned URP shader.");
            var material = new Material(shader) { name = name }; material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", .5f); material.SetFloat("_Smoothness", .25f);
            AssetDatabase.CreateAsset(material, "Assets/_Project/Materials/NPC/" + name + ".mat"); return material;
        }
        private static GameObject Block(Transform parent, string name, Vector3 point, Vector3 size, Material material)
        {
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube); child.name = name; child.transform.SetParent(parent, false);
            child.transform.localPosition = point; child.transform.localScale = size;
            UnityEngine.Object.DestroyImmediate(child.GetComponent<Collider>()); child.GetComponent<Renderer>().sharedMaterial = material; return child;
        }
    }
}
