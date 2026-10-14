using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundPlayerAuthoring
    {
        public const string BindingPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset";
        public const string MaterialPath = "Assets/_Project/Materials/AnimationPlayground/BodyNeutral.mat";
        [Serializable] private sealed class Transfer { public Entry[] assets; }
        [Serializable] private sealed class Entry { public string asset_id, destination, kind; public bool eligible_unarmed_candidate; }
        [Serializable] public sealed class CalibrationReceipt
        {
            public string modelPath, bindingGuid, profileGuid, state = "integrated-preview-and-locomotion-draft";
            public float bodyHeight, visualScale, capsuleHeight, capsuleRadius, torsoMaximumRadius, torsoRadialMargin;
            public Vector3 visualOffset, torsoBoundsSize, actorPosition;
            public int torsoVertices, motions, unarmedPreviews, restrictedReferences;
        }

        // Authoring touches the existing Booter and owned session only; it never launches Play Mode.
        public static CalibrationReceipt CreateAndBindPlayer()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var session = TopDown3DAnimationPlaygroundSession.Find(scene);
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || session == null || scene.isDirty)
                throw new InvalidOperationException("Author the saved, already loaded Camp Loop Testing scene in Edit Mode.");
            if (File.Exists(BindingPath)) throw new InvalidOperationException("Preserve the private binding; restore or update it explicitly.");
            var player = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerMotor>(scene);
            var driver = player != null ? player.GetComponent<TopDown3DPlayerAnimationDriver>() : null;
            var capsule = player != null ? player.GetComponent<CapsuleCollider>() : null;
            if (player == null || driver == null || capsule == null || !TopDown3DAnimationPlaygroundSession.HasUnitPhysicsHierarchy(player.transform))
                throw new InvalidDataException("The existing unscaled player with its motor, visual driver and capsule is required.");
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(AnimationPlaygroundLocomotionAuthoring.ProfilePath);
            if (profile == null || !profile.TryValidate(out _)) throw new InvalidDataException("Restore the calibrated private locomotion draft first.");
            var modelPath = AnimationPlaygroundAssetImporter.RigPaths[1];
            var report = AnimationPlaygroundAssetValidator.MeasureRig(modelPath, 1.8f, -.91f);
            var source = AvatarAt(AnimationPlaygroundAssetImporter.RigPaths[0]); var target = AvatarAt(modelPath);
            var transfer = JsonUtility.FromJson<Transfer>(File.ReadAllText(AnimationPlaygroundAssetImporter.TransferPlanPath));
            var motions = new List<AnimationPlaygroundMotionBinding>();
            foreach (var item in transfer.assets)
            {
                if (!item.destination.StartsWith("Assets/_Project/Art/Animations/Mixamo/", StringComparison.Ordinal)) continue;
                var clips = AssetDatabase.LoadAllAssetsAtPath(item.destination).OfType<AnimationClip>()
                    .Where(value => !value.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
                if (clips.Length != 1 || !clips[0].isHumanMotion) throw new InvalidDataException("Restore the qualified canonical clip: " + item.destination);
                motions.Add(new AnimationPlaygroundMotionBinding { sourceId = item.asset_id, clip = clips[0],
                    readiness = AnimationPlaygroundMotionReadiness.ImportQualified, restrictedReference = !item.eligible_unarmed_candidate });
            }
            if (motions.Count != 71) throw new InvalidDataException("The canonical library must contain all 71 source motions.");
            var receipt = MeasureTorso(modelPath, report.visualScale);
            if (receipt.torsoMaximumRadius > .18f) throw new InvalidDataException("Revisit capsule fit: the measured torso exceeds its reviewed margin.");
            EnsureFolder("Assets/_Project/Materials/AnimationPlayground");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidDataException("The pinned URP Lit shader is required.");
                material = new Material(shader) { name = "BodyNeutral" };
                material.SetColor("_BaseColor", new Color(.42f, .34f, .26f)); material.SetFloat("_Smoothness", .2f);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            var bindings = ScriptableObject.CreateInstance<TopDown3DAnimationPlaygroundBindings>();
            bindings.targetModel = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            bindings.sourceAvatar = source; bindings.targetAvatar = target; bindings.locomotion = profile;
            bindings.visualScale = report.visualScale; bindings.visualOffset = report.visualOffset;
            bindings.bodyMaterial = material; bindings.motions = motions.ToArray();
            if (!bindings.ValidatePlayer(out var error)) { Object.DestroyImmediate(bindings); throw new InvalidDataException(error); }
            AssetDatabase.CreateAsset(bindings, BindingPath);
            Undo.RecordObjects(new Object[] { player.transform, capsule, driver, session }, "Bind calibrated lab player");
            // Keep the former capsule's world foot plane; body height is a visual measurement, not a root scale.
            var feetY = player.transform.position.y + capsule.center.y - capsule.height * .5f;
            var position = player.transform.position; position.y = feetY + bindings.capsuleHeight * .5f;
            player.transform.position = position;
            capsule.height = bindings.capsuleHeight; capsule.radius = bindings.capsuleRadius; capsule.center = Vector3.zero;
            driver.ConfigureLaboratory(bindings);
            var anchor = new GameObject("Booter Safe Return"); anchor.transform.SetParent(session.transform, false);
            anchor.transform.SetPositionAndRotation(position, player.transform.rotation);
            Undo.RegisterCreatedObjectUndo(anchor, "Author player safe return");
            session.Configure(bindings); session.ConfigurePlayer(player, new[] { anchor.transform });
            foreach (var value in new Object[] { player.transform, capsule, driver, session }) EditorUtility.SetDirty(value);
            AnimationPlaygroundUiAuthoring.AddResetControl();
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            receipt.modelPath = modelPath; receipt.bindingGuid = AssetDatabase.AssetPathToGUID(BindingPath);
            receipt.profileGuid = AssetDatabase.AssetPathToGUID(AnimationPlaygroundLocomotionAuthoring.ProfilePath);
            receipt.bodyHeight = 1.8f; receipt.visualScale = bindings.visualScale; receipt.visualOffset = bindings.visualOffset;
            receipt.capsuleHeight = capsule.height; receipt.capsuleRadius = capsule.radius;
            receipt.torsoRadialMargin = capsule.radius - receipt.torsoMaximumRadius; receipt.actorPosition = position;
            receipt.motions = motions.Count; receipt.unarmedPreviews = motions.Count(value => value.CanPreview);
            receipt.restrictedReferences = motions.Count(value => value.restrictedReference);
            return receipt;
        }

        private static Avatar AvatarAt(string path)
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().SingleOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman) throw new InvalidDataException("Restore the independently qualified Avatar: " + path);
            return avatar;
        }
        public static CalibrationReceipt MeasureTorso(string path, float scale)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); instance.transform.localScale = Vector3.one * scale;
                var animator = instance.GetComponentInChildren<Animator>(); var torso = new HashSet<Transform>();
                foreach (var bone in new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest })
                    if (animator.GetBoneTransform(bone) != null) torso.Add(animator.GetBoneTransform(bone));
                var receipt = new CalibrationReceipt(); var bounds = new Bounds();
                foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var baked = new Mesh();
                    try
                    {
                        skin.BakeMesh(baked, true); var vertices = baked.vertices;
                        using var counts = skin.sharedMesh.GetBonesPerVertex(); using var weights = skin.sharedMesh.GetAllBoneWeights(); var cursor = 0;
                        for (var i = 0; i < counts.Length; i++)
                        {
                            var torsoWeight = 0f;
                            for (var j = 0; j < counts[i]; j++) { var item = weights[cursor++]; if (torso.Contains(skin.bones[item.boneIndex])) torsoWeight += item.weight; }
                            if (torsoWeight < .5f) continue;
                            var point = skin.transform.TransformPoint(vertices[i]);
                            receipt.torsoMaximumRadius = Mathf.Max(receipt.torsoMaximumRadius, new Vector2(point.x, point.z).magnitude);
                            if (receipt.torsoVertices++ == 0) bounds = new Bounds(point, Vector3.zero); else bounds.Encapsulate(point);
                        }
                    }
                    finally { Object.DestroyImmediate(baked); }
                }
                if (receipt.torsoVertices == 0) throw new InvalidDataException("The target has no weighted torso vertices.");
                receipt.torsoBoundsSize = bounds.size; return receipt;
            }
            finally { Object.DestroyImmediate(instance); EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
