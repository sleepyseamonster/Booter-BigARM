using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundStationAuthoring
    {
        public const string StationFolder = "Assets/_Project/Prefabs/AnimationPlayground/Stations";
        public const string PropFolder = "Assets/_Project/Prefabs/AnimationPlayground/Props";
        private const string MaterialFolder = "Assets/_Project/Materials/AnimationPlayground";
        public static readonly string[] Titles = { "Hub", "Locomotion", "Crouch and slide", "Jump and fall", "Pickup throw and button", "Push", "Mining", "Cover", "Crowd and wall", "Ladder", "Rest and mount", "Combat and recovery" };
        public static readonly string[] Gates = {
            "preview only; physical families qualify separately", "cadence, transitions and backpedal qualification pending",
            "posture, slide phases and ceiling recovery pending", "takeoff, air, landing and roll qualification pending",
            "hold, drop, release and switch contacts pending", "crate start, loop, stop and collision ownership pending",
            "blunt-tool adaptation, grip and impact pending", "cover entry, tangent, ends and occlusion qualification pending",
            "wall-hug or crowd-squeeze motion missing; ordinary walking tests spacing", "entry, rungs and top exit contacts pending; descent motion missing",
            "complete rest transitions and actual mount/dismount motion pending", "strike, reaction, terminal and recovery contacts pending" };
        private static Material deck, wall, accent, marking, soft;
        private static Font font;

        public static string StationPath(int number) => StationFolder + "/AP" + number.ToString("00") + ".prefab";
        public static void RepairCrouchClearanceFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Repair the fixture in the marked candidate before transferring its verified prefab.");
            var root=PrefabUtility.LoadPrefabContents(StationPath(3));
            try
            {
                foreach(var name in new[]{"Tunnel left","Tunnel right"})
                {
                    var item=root.transform.Find(name);if(item==null)throw new InvalidDataException("Crouch fixture wall is missing.");
                    var position=item.localPosition;position.y=.825f;item.localPosition=position;
                    var scale=item.localScale;scale.y=1.65f;item.localScale=scale;
                }
                var ceiling=root.transform.Find("Tunnel ceiling");if(ceiling==null)throw new InvalidDataException("Crouch fixture roof is missing.");
                var height=ceiling.localPosition;height.y=1.65f;ceiling.localPosition=height;
                PrefabUtility.SaveAsPrefabAsset(root,StationPath(3));AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            EditorApplication.Exit(0);
        }
        public static void RepairPreparedActionMarkers()
        {
            for(var number=1;number<=12;number++)
            {
                var root=PrefabUtility.LoadPrefabContents(StationPath(number));
                try
                {
                    foreach(var target in root.GetComponentsInChildren<TopDown3DAnimationPlaygroundTarget>(true))
                    {
                        if(target.ActionKind==AnimationPlaygroundActionKind.Preview || target.ActionKind==AnimationPlaygroundActionKind.Reset)continue;
                        var pedestal=target.transform.Find("Terminal pedestal");
                        if(pedestal==null)continue;
                        var collider=pedestal.GetComponent<Collider>(); if(collider!=null)Object.DestroyImmediate(collider);
                        pedestal.localPosition=new Vector3(0,.025f,0);pedestal.localScale=new Vector3(.25f,.025f,.25f);
                        target.Configure(target.StableId,target.ActionKind,target.Prompt,target.transform.Find("Interaction"),target.InteractionRange,target.GetComponentsInChildren<Collider>());
                        target.SetAvailable(false);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,StationPath(number));
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        public static void CreatePrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Author stationary fixtures only in Edit Mode.");
            for (var number = 1; number <= 12; number++)
                if (File.Exists(StationPath(number))) throw new InvalidOperationException("Preserve existing station assets; update their owned hierarchy explicitly.");
            var previous = SceneManager.GetActiveScene();
            var staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(staging);
            try
            {
                EnsureFolder(StationFolder); EnsureFolder(PropFolder); EnsureFolder(MaterialFolder);
                deck = Material("Deck", new Color(.19f, .23f, .22f)); wall = Material("FixtureWall", new Color(.36f, .39f, .34f));
                accent = Material("FixtureAccent", new Color(.78f, .48f, .12f)); marking = Material("FixtureMarking", new Color(.12f, .55f, .61f));
                soft = Material("FixturePadding", new Color(.38f, .27f, .19f)); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                CreateReusableProps();
                for (var number = 1; number <= 12; number++)
                {
                    var root = new GameObject("AP" + number.ToString("00") + " " + Titles[number - 1]);
                    try
                    {
                        var id = "AP" + number.ToString("00");
                        Cube(root.transform, "Flat deck", new Vector3(0, -.1f, 0), new Vector3(28, .2f, 24), deck);
                        var entry = Anchor(root.transform, "Entry", new Vector3(0, .93f, -9));
                        var safe = Anchor(root.transform, "Safe Return", new Vector3(0, .93f, -9));
                        var targets = new List<TopDown3DAnimationPlaygroundTarget>();
                        targets.Add(Terminal(root.transform, id + ".Inspect", "Inspect " + Titles[number - 1], new Vector3(-11, 0, -9), AnimationPlaygroundActionKind.Preview));
                        targets.Add(Terminal(root.transform, id + ".Reset", "Reset lab session", new Vector3(-8.5f, 0, -9), AnimationPlaygroundActionKind.Reset));
                        var sign = Sign(root.transform, id + " " + Titles[number - 1], new Vector3(-9.8f, 2.5f, -9));
                        Furnish(number, root.transform, id, targets);
                        root.AddComponent<TopDown3DAnimationPlaygroundStation>().Configure(id, Titles[number - 1], Gates[number - 1], new Vector2(28, 24), entry, safe, sign,
                            targets.ToArray(), root.GetComponentsInChildren<AnimationPlaygroundProp>(true));
                        PrefabUtility.SaveAsPrefabAsset(root, StationPath(number));
                    }
                    finally { Object.DestroyImmediate(root); }
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(staging, true);
            }
        }

        private static void Furnish(int number, Transform root, string id, List<TopDown3DAnimationPlaygroundTarget> targets)
        {
            switch (number)
            {
                case 1:
                    Cube(root, "One metre gauge", new Vector3(-5, .5f, 0), new Vector3(.12f, 1, .12f), accent);
                    Cube(root, "Feet plane", new Vector3(0, .004f, 0), new Vector3(5, .008f, 5), marking, false);
                    Ring(root, "Turn circle", new Vector3(0, .01f, 0), 2.5f);
                    Sign(root, "1 Unity unit = 1 metre\nBody 1.80m | capsule 1.82m x 0.40m", new Vector3(-5, 1.8f, 0));
                    break;
                case 2:
                    Lane(root, "20 metre gait lane", -5, 20); Ring(root, "Turning circle", new Vector3(6, .01f, 2), 3);
                    Cube(root, "Start zone", new Vector3(-5, .008f, -9), new Vector3(3, .01f, 1), accent, false);
                    Cube(root, "Stop zone", new Vector3(-5, .008f, 9), new Vector3(3, .01f, 1), accent, false);
                    Ramp(root, "Mild ramp", new Vector3(7, 0, -4), 3, 8, 1);
                    Anchor(root, "Strafe left", new Vector3(3, .93f, 3)); Anchor(root, "Strafe right", new Vector3(10, .93f, 3));
                    break;
                case 3:
                    Lane(root, "16 metre slide lane", -5, 16);
                    Cube(root, "Tunnel left", new Vector3(2.8f, .825f, 0), new Vector3(.2f, 1.65f, 8), wall);
                    Cube(root, "Tunnel right", new Vector3(5.2f, .825f, 0), new Vector3(.2f, 1.65f, 8), wall);
                    Cube(root, "Tunnel ceiling", new Vector3(4, 1.65f, 0), new Vector3(2.6f, .15f, 8), wall);
                    Anchor(root, "Blocked stand clearance", new Vector3(4, .93f, 0));
                    DisabledAction(root, id, "Crouch entry", new Vector3(4, 1, -5), AnimationPlaygroundActionKind.Crouch, targets);
                    DisabledAction(root, id, "Slide entry", new Vector3(-5, 1, -7), AnimationPlaygroundActionKind.Slide, targets);
                    break;
                case 4:
                    Cube(root, "Low vault", new Vector3(-7, .35f, -1), new Vector3(3, .7f, .5f), wall);
                    Cube(root, "Half metre step", new Vector3(-7, .25f, 3), new Vector3(3, .5f, 3), wall);
                    Cube(root, "One metre step", new Vector3(-7, .5f, 7), new Vector3(3, 1, 3), wall);
                    Platform(root, "Two metre fall", new Vector3(2, 0, -1), 2, 3);
                    Platform(root, "Four metre fall", new Vector3(6, 0, 6), 4, 4);
                    Stairs(root, "Platform return stairs", new Vector3(9, 0, -3), 4, 16, .5f);
                    Cube(root, "Landing zone", new Vector3(2, .009f, -5), new Vector3(5, .012f, 4), marking, false);
                    DisabledAction(root, id, "Jump takeoff", new Vector3(2, 1, -6), AnimationPlaygroundActionKind.Jump, targets);
                    DisabledAction(root, id, "Fall release", new Vector3(6, 5, 5), AnimationPlaygroundActionKind.Fall, targets);
                    break;
                case 5:
                    Prop(root, "Table", id + ".Table", new Vector3(-6, 0, 1));
                    for (var i = 0; i < 3; i++) Prop(root, "Scrap", id + ".Scrap" + (i + 1), new Vector3(-6 + i * .45f, .9f, 1));
                    Cube(root, "Throw backstop", new Vector3(6, 1.5f, 8), new Vector3(9, 3, .3f), wall);
                    Cube(root, "Catch floor", new Vector3(6, .02f, 5), new Vector3(8, .04f, 5), soft);
                    Cube(root, "Throw target", new Vector3(6, 1.2f, 7.7f), new Vector3(1.2f, 1.2f, .08f), accent);
                    Anchor(root, "Throw release", new Vector3(6, 1.4f, -1)); Anchor(root, "Catch centre", new Vector3(6, 1, 5));
                    DisabledAction(root, id, "Pickup", new Vector3(-6, 1.1f, -.5f), AnimationPlaygroundActionKind.Pickup, targets);
                    DisabledAction(root, id, "Throw", new Vector3(6, 1, -2), AnimationPlaygroundActionKind.Throw, targets);
                    DisabledAction(root, id, "Switch", new Vector3(0, 1, 3), AnimationPlaygroundActionKind.Button, targets);
                    break;
                case 6:
                    Lane(root, "12 metre push lane", 0, 12); Prop(root, "HeavyCrate", id + ".Crate", new Vector3(0, 0, -3));
                    Cube(root, "Crate obstruction", new Vector3(0, .55f, 4), new Vector3(3, 1.1f, .35f), wall);
                    Cube(root, "Push stop marker", new Vector3(0, .01f, 2), new Vector3(3, .015f, .25f), accent, false);
                    Anchor(root, "Push left grip", new Vector3(-.4f, .8f, -3.7f)); Anchor(root, "Push right grip", new Vector3(.4f, .8f, -3.7f));
                    DisabledAction(root, id, "Push handles", new Vector3(0, 1, -4), AnimationPlaygroundActionKind.Push, targets);
                    break;
                case 7:
                    var node = Shape(root, "Lab mining node", PrimitiveType.Sphere, new Vector3(-3, .55f, 2), new Vector3(1.3f, 1.1f, 1.3f), wall);
                    node.AddComponent<AnimationPlaygroundProp>().Configure(id + ".Node", null, Anchor(node.transform, "Impact", new Vector3(0, .5f, 0)));
                    Prop(root, "BluntMiningTool", id + ".Tool", new Vector3(2, .05f, 2));
                    DisabledAction(root, id, "Mine", new Vector3(-3, 1, .5f), AnimationPlaygroundActionKind.Mining, targets);
                    Sign(root, "Lab-only reward sink\nBlunt tool; no production resource node", new Vector3(-3, 2.5f, 4));
                    break;
                case 8:
                    Cube(root, "Low cover 1.10m", new Vector3(-4, .55f, 2), new Vector3(10, 1.1f, .3f), wall);
                    Cube(root, "High cover 2.20m", new Vector3(6, 1.1f, 2), new Vector3(8, 2.2f, .3f), wall);
                    Cube(root, "Cover L corner", new Vector3(9.85f, 1.1f, 5), new Vector3(.3f, 2.2f, 6), wall);
                    Anchor(root, "Cover tangent start", new Vector3(-8, .93f, 1.5f)); Anchor(root, "Cover tangent end", new Vector3(0, .93f, 1.5f));
                    for (var i = 0; i < 3; i++) Shape(root, "Observer eye " + i, PrimitiveType.Sphere, new Vector3(-7 + i * 7, 1.8f, 9), Vector3.one * .18f, marking, false);
                    DisabledAction(root, id, "Cover entry", new Vector3(-4, 1, .5f), AnimationPlaygroundActionKind.Crouch, targets);
                    break;
                case 9:
                    Cube(root, "Corridor left", new Vector3(-.7f, 1.1f, 1), new Vector3(.2f, 2.2f, 16), wall);
                    Cube(root, "Corridor right", new Vector3(.7f, 1.1f, 1), new Vector3(.2f, 2.2f, 16), wall);
                    Anchor(root, "Corridor 1.20m gap", new Vector3(0, .93f, 0));
                    for (var i = 0; i < 6; i++) Prop(root, "TrainingDummy", id + ".Crowd" + i, new Vector3(i % 2 == 0 ? -4 : 4, 0, -3 + i / 2 * 4));
                    DisabledAction(root, id, "Wall squeeze", new Vector3(0, 1, -7), AnimationPlaygroundActionKind.Corridor, targets);
                    break;
                case 10:
                    Cube(root, "Ladder left rail", new Vector3(-.4f, 2, 1.7f), new Vector3(.07f, 4, .07f), accent);
                    Cube(root, "Ladder right rail", new Vector3(.4f, 2, 1.7f), new Vector3(.07f, 4, .07f), accent);
                    for (var i = 1; i <= 13; i++)
                    {
                        Cube(root, "Rung " + i, new Vector3(0, i * .3f, 1.7f), new Vector3(.8f, .06f, .08f), accent);
                        Anchor(root, "Rung contact " + i, new Vector3(0, i * .3f, 1.65f));
                    }
                    Cube(root, "Ladder top deck", new Vector3(0, 3.9f, 3.7f), new Vector3(4, .2f, 4), deck);
                    Stairs(root, "Ladder return stairs", new Vector3(3, 0, -3), 4, 16, .5f);
                    Anchor(root, "Ladder entry", new Vector3(0, .93f, 1.2f)); Anchor(root, "Ladder top exit", new Vector3(0, 4.93f, 2.5f));
                    var blocker = Cube(root, "Optional blocked exit", new Vector3(0, 4.9f, 2.5f), new Vector3(1, 1.8f, .3f), wall);
                    blocker.GetComponent<Collider>().enabled = false; blocker.GetComponent<Renderer>().enabled = false;
                    blocker.AddComponent<AnimationPlaygroundProp>().Configure(id + ".ExitBlocker");
                    DisabledAction(root, id, "Ladder climb", new Vector3(0, 1, .5f), AnimationPlaygroundActionKind.Ladder, targets);
                    break;
                case 11:
                    Prop(root, "Chair", id + ".Chair", new Vector3(-6, 0, 2));
                    Cube(root, "Ground rest mat", new Vector3(3, .025f, -3), new Vector3(1, .05f, 2), soft);
                    Cube(root, "Side sleep bedroll", new Vector3(-3, .07f, -3), new Vector3(.9f, .14f, 2.3f), soft);
                    Cube(root, "Sleep pillow", new Vector3(-3, .18f, -2.1f), new Vector3(.7f, .22f, .4f), soft);
                    Cube(root, "Mechanical saddle fixture", new Vector3(7, .5f, 3), new Vector3(1, 1, 1.8f), wall);
                    Cube(root, "Mount seat", new Vector3(7, 1.05f, 3), new Vector3(.65f, .12f, 1.1f), soft);
                    Anchor(root, "Saddle mount entry", new Vector3(5.8f, .93f, 3)); Anchor(root, "Saddle rider seat", new Vector3(7, 1.2f, 3)); Anchor(root, "Saddle dismount exit", new Vector3(8.2f, .93f, 3));
                    DisabledAction(root, id, "Chair sit", new Vector3(-6, 1, 1), AnimationPlaygroundActionKind.Chair, targets);
                    DisabledAction(root, id, "Ground rest", new Vector3(3, 1, -4), AnimationPlaygroundActionKind.GroundRest, targets);
                    DisabledAction(root, id, "Sleep", new Vector3(-3, 1, -4), AnimationPlaygroundActionKind.Sleep, targets);
                    DisabledAction(root, id, "Mount", new Vector3(5.8f, 1, 3), AnimationPlaygroundActionKind.Mount, targets);
                    break;
                case 12:
                    Prop(root, "TrainingDummy", id + ".Target", new Vector3(-5, 0, 2));
                    Anchor(root, "Strike torso contact", new Vector3(-5, 1.1f, 1.8f)); Anchor(root, "Strike head contact", new Vector3(-5, 1.6f, 1.8f));
                    Cube(root, "Recovery clear floor", new Vector3(5, .01f, 2), new Vector3(6, .015f, 5), marking, false);
                    DisabledAction(root, id, "Punch", new Vector3(-5, 1, .5f), AnimationPlaygroundActionKind.Punch, targets);
                    DisabledAction(root, id, "Kick", new Vector3(-3.5f, 1, .5f), AnimationPlaygroundActionKind.Kick, targets);
                    DisabledAction(root, id, "Directional hurt", new Vector3(5, 1, -1), AnimationPlaygroundActionKind.Hurt, targets);
                    DisabledAction(root, id, "Death and recovery", new Vector3(7, 1, -1), AnimationPlaygroundActionKind.Death, targets);
                    break;
            }
        }

        private static void CreateReusableProps()
        {
            foreach (var name in new[] { "Table", "Scrap", "HeavyCrate", "BluntMiningTool", "TrainingDummy", "Chair" })
            {
                var path = PropFolder + "/" + name + ".prefab";
                if (File.Exists(path)) throw new InvalidOperationException("Preserve existing authored prop: " + path);
                var root = new GameObject(name);
                try
                {
                    switch (name)
                    {
                        case "Table":
                            Cube(root.transform, "Top", new Vector3(0, .8f, 0), new Vector3(2, .12f, 1), wall);
                            foreach (var x in new[] { -.8f, .8f }) foreach (var z in new[] { -.35f, .35f }) Cube(root.transform, "Leg", new Vector3(x, .4f, z), new Vector3(.12f, .8f, .12f), wall);
                            break;
                        case "Scrap": Cube(root.transform, "Body", new Vector3(0, .1f, 0), new Vector3(.22f, .2f, .2f), accent); root.AddComponent<Rigidbody>().mass = .3f; break;
                        case "HeavyCrate": Cube(root.transform, "Body", new Vector3(0, .55f, 0), new Vector3(1.2f, 1.1f, 1.2f), wall); root.AddComponent<Rigidbody>().mass = 60; break;
                        case "BluntMiningTool":
                            Cube(root.transform, "Handle", new Vector3(0, .4f, 0), new Vector3(.07f, .8f, .07f), soft);
                            Cube(root.transform, "Blunt head", new Vector3(0, .8f, 0), new Vector3(.6f, .17f, .17f), wall); root.AddComponent<Rigidbody>().mass = 2; break;
                        case "TrainingDummy": Shape(root.transform, "Padding", PrimitiveType.Capsule, new Vector3(0, .9f, 0), new Vector3(.4f, .9f, .4f), soft); break;
                        case "Chair":
                            Cube(root.transform, "Seat", new Vector3(0, .46f, 0), new Vector3(.65f, .12f, .65f), soft);
                            Cube(root.transform, "Back", new Vector3(0, .8f, .3f), new Vector3(.65f, .65f, .12f), wall);
                            foreach (var x in new[] { -.25f, .25f }) foreach (var z in new[] { -.25f, .25f }) Cube(root.transform, "Leg", new Vector3(x, .2f, z), new Vector3(.08f, .4f, .08f), wall);
                            Anchor(root.transform, "Seat contact", new Vector3(0, .52f, 0)); break;
                    }
                    var grip = Anchor(root.transform, "Grip", name == "BluntMiningTool" ? new Vector3(0, .3f, 0) : new Vector3(0, .1f, 0));
                    var contact = Anchor(root.transform, "Contact", name == "BluntMiningTool" ? new Vector3(.3f, .8f, 0) : new Vector3(0, .5f, 0));
                    root.AddComponent<AnimationPlaygroundProp>().Configure("Template." + name, grip, contact);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { Object.DestroyImmediate(root); }
            }
        }
        private static GameObject Prop(Transform parent, string name, string id, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PropFolder + "/" + name + ".prefab"), parent);
            instance.name = id; instance.transform.localPosition = position;
            var prop = instance.GetComponent<AnimationPlaygroundProp>(); prop.Configure(id, prop.Grip, prop.Contact); return instance;
        }
        private static TopDown3DAnimationPlaygroundTarget Terminal(Transform parent, string id, string label, Vector3 position, AnimationPlaygroundActionKind kind)
        {
            var root = new GameObject(id); root.transform.SetParent(parent, false); root.transform.localPosition = position;
            Cube(root.transform, "Terminal pedestal", new Vector3(0, .5f, 0), new Vector3(.4f, 1, .4f), accent);
            var trigger = root.AddComponent<SphereCollider>(); trigger.center = Vector3.up; trigger.radius = .65f; trigger.isTrigger = true;
            var point = Anchor(root.transform, "Interaction", Vector3.up);
            var target = root.AddComponent<TopDown3DAnimationPlaygroundTarget>(); target.Configure(id, kind, label, point, 3, root.GetComponentsInChildren<Collider>()); return target;
        }
        private static void DisabledAction(Transform parent, string station, string name, Vector3 position, AnimationPlaygroundActionKind kind, List<TopDown3DAnimationPlaygroundTarget> targets)
        {
            var target = Terminal(parent, station + ".Action." + kind, name + " (qualification pending)", position - Vector3.up, kind);
            var pedestal=target.transform.Find("Terminal pedestal"); Object.DestroyImmediate(pedestal.GetComponent<Collider>());
            pedestal.localPosition=new Vector3(0,.025f,0);pedestal.localScale=new Vector3(.25f,.025f,.25f);
            target.Configure(target.StableId,kind,target.Prompt,target.transform.Find("Interaction"),3,target.GetComponentsInChildren<Collider>());
            target.SetAvailable(false); targets.Add(target);
        }
        private static void Platform(Transform parent, string name, Vector3 position, float height, float width)
        {
            Cube(parent, name, position + Vector3.up * height * .5f, new Vector3(width, height, width), wall);
            Anchor(parent, name + " standing anchor", position + Vector3.up * (height + .93f));
        }
        private static void Stairs(Transform parent, string name, Vector3 start, float height, int count, float run)
        { for (var i = 0; i < count; i++) Cube(parent, name + " " + i, start + new Vector3(0, height * (i + 1) / count * .5f, run * i), new Vector3(1.8f, height * (i + 1) / count, run), deck); }
        private static void Lane(Transform parent, string name, float x, float length)
        { Cube(parent, name, new Vector3(x, .008f, 0), new Vector3(2.5f, .01f, length), marking, false); }
        private static void Ring(Transform parent, string name, Vector3 centre, float radius)
        {
            for (var i = 0; i < 32; i++)
            {
                var angle = i * Mathf.PI * 2 / 32; var go = Cube(parent, name + " " + i, centre + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius, new Vector3(.08f, .01f, .32f), marking, false);
                go.transform.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
            }
        }
        public static GameObject Ramp(Transform parent, string name, Vector3 lowEnd, float width, float length, float rise)
        {
            var angle = Mathf.Atan2(rise, length); var centre = lowEnd + new Vector3(0, rise * .5f - .1f * Mathf.Cos(angle), length * .5f);
            var ramp = Cube(parent, name, centre, new Vector3(width, .2f, Mathf.Sqrt(length * length + rise * rise)), deck ?? AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Deck.mat"));
            ramp.transform.localRotation = Quaternion.Euler(-angle * Mathf.Rad2Deg, 0, 0); return ramp;
        }
        private static Transform Anchor(Transform parent, string name, Vector3 point)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = point; return go.transform; }
        private static TextMesh Sign(Transform parent, string text, Vector3 position)
        {
            var root = new GameObject("Status sign"); root.transform.SetParent(parent, false); root.transform.localPosition = position;
            var sign = root.AddComponent<TextMesh>(); sign.font = font; sign.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            sign.fontSize = 64; sign.characterSize = .11f; sign.anchor = TextAnchor.MiddleCenter; sign.alignment = TextAlignment.Center;
            sign.color = new Color(.95f, .86f, .63f); sign.text = text; root.transform.localRotation = Quaternion.Euler(0, 180, 0); return sign;
        }
        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collision = true)
            => Shape(parent, name, PrimitiveType.Cube, position, size, material, collision);
        private static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private static Material Material(string name, Color colour)
        {
            var path = MaterialFolder + "/" + name + ".mat"; var value = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (value != null) return value;
            var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) throw new InvalidDataException("Restore the pinned URP shader.");
            value = new Material(shader) { name = name }; value.SetColor("_BaseColor", colour); value.SetFloat("_Smoothness", .15f); AssetDatabase.CreateAsset(value, path); return value;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return; var parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
