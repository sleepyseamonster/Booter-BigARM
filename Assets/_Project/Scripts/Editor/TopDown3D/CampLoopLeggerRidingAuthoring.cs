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
    public static class CampLoopLeggerRidingAuthoring
    {
        public const string RootName = "Legger Riding Study";
        public const string ProfilePath = "Assets/_Project/Settings/Studies/LeggerRiding/CampLoopLeggerRideProfile.asset";
        public static void InstallFromCli()
        {
            if (!Application.isBatchMode || Application.isPlaying) throw new InvalidOperationException("Background Edit Mode authoring only.");
            var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Single);
            if (All<TopDown3DLeggerRidingController>(scene).Length != 0)
            { CampLoopLeggerRidingValidator.Validate(scene); return; }
            CampLoopTestingSceneBuilder.Validate(scene);
            if (scene.GetRootGameObjects().Any(r => r.name == RootName)) throw new InvalidOperationException("Ambiguous riding study root.");
            var player = All<TopDown3DPlayerMotor>(scene).Single();
            var follower = All<TopDown3DBigArmFollower>(scene).Single();
            var input = All<TopDown3DInputRouter>(scene).Single();
            var camera = All<TopDown3DCameraRig>(scene).Single();
            var lab = All<TopDown3DAnimationPlaygroundSession>(scene).Single();
            var returnsProperty = new SerializedObject(lab).FindProperty("safeReturns");
            var returns = Enumerable.Range(0, returnsProperty.arraySize)
                .Select(i => (Transform)returnsProperty.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            if (returns.Length == 0 || returns.Any(r => r == null)) throw new InvalidOperationException("Missing authored safe return anchors.");
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DLeggerRideProfile>(ProfilePath);
            if (profile == null)
            {
                if (File.Exists(ProfilePath)) throw new InvalidOperationException("Preserve invalid existing riding profile.");
                Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath)); AssetDatabase.Refresh();
                profile = ScriptableObject.CreateInstance<TopDown3DLeggerRideProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            if (!profile.Validate(out var reason)) throw new InvalidOperationException(reason);
            var root = new GameObject(RootName); SceneManager.MoveGameObjectToScene(root, scene);
            var controller = root.AddComponent<TopDown3DLeggerRidingController>();
            var box = follower.GetComponent<BoxCollider>(); var capsule = player.GetComponent<CapsuleCollider>();
            float seatHeight = box.center.y + box.size.y * .5f - capsule.center.y + capsule.height * .5f + .08f;
            var seat = Anchor("Legger ride seat", follower.transform, new Vector3(0, seatHeight, 0));
            var exits = new[] { Anchor("Legger exit left", follower.transform, Vector3.left * 1.5f),
                Anchor("Legger exit right", follower.transform, Vector3.right * 1.5f),
                Anchor("Legger exit rear", follower.transform, Vector3.back * 1.65f) };
            controller.Configure(player, follower, input, camera, lab, profile, seat, exits, returns);
            follower.gameObject.AddComponent<TopDown3DLeggerMountTarget>().Configure(controller);
            var native = All<Terrain>(scene);
            var center = player.transform.position + new Vector3(34, 0, 32);
            float Ground(Vector3 p)
            {
                var terrain = native.SingleOrDefault(t => p.x >= t.transform.position.x && p.x < t.transform.position.x + t.terrainData.size.x
                    && p.z >= t.transform.position.z && p.z < t.transform.position.z + t.terrainData.size.z);
                if (terrain == null) throw new InvalidOperationException("Riding corridor outside native terrain.");
                return terrain.SampleHeight(p) + terrain.transform.position.y;
            }
            center.y = Ground(center);
            var trial = new GameObject("Riding terrain corridor"); trial.transform.SetParent(root.transform, false); trial.transform.position = center;
            // Authored solids on unchanged native terrain. Only intentional landings are marked as support.
            var deck = Solid("Step landing 0.65m", trial.transform, new Vector3(-7, 0, 0), new Vector3(4, .65f, 4), true);
            deck.position = new Vector3(deck.position.x, Ground(deck.position) + .325f, deck.position.z);
            var ramp = Solid("Supported 52 degree incline", trial.transform, new Vector3(0, 2.5f, 0), new Vector3(3, .18f, 6), true);
            ramp.localRotation = Quaternion.Euler(-52, 0, 0);
            for (int i = 0; i < 3; i++)
            {
                var obstacle = Solid("Slalom blocker " + (i + 1), trial.transform, new Vector3(7 + (i % 2) * 2, 0, i * 4 - 4), new Vector3(.6f, 1.4f, .6f), false);
                obstacle.position = new Vector3(obstacle.position.x, Ground(obstacle.position) + .7f, obstacle.position.z);
            }
            Solid("Rider headroom boundary", trial.transform, new Vector3(14, 3.0f, 0), new Vector3(3, .2f, 3), false);
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
            CampLoopLeggerRidingValidator.Validate(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Riding candidate scene save failed.");
            Debug.Log("LEGGER_RIDING_AUTHORING_PASS: additive seat/exits, private tuning and bounded terrain corridor.");
        }
        private static Transform Anchor(string name, Transform parent, Vector3 local)
        {
            var anchor = new GameObject(name).transform; anchor.SetParent(parent, false); anchor.localPosition = local; return anchor;
        }
        private static Transform Solid(string name, Transform parent, Vector3 local, Vector3 size, bool support)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = local; go.transform.localScale = size;
            if (support) go.AddComponent<TopDown3DGroundSurface>(); return go.transform;
        }
        internal static T[] All<T>(Scene scene) where T : Component
            => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    }
}
