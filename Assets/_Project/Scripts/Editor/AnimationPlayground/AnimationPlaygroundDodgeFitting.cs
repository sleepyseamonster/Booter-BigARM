using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundDodgeFitting
    {
        public const string ProjectionPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/DodgeBodyProjection.asset";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Call(object value, string method) => value.GetType().GetMethod(method, Private).Invoke(value, null);
        static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Private).GetValue(value);

        public static object Fit(float worldPerUnit)
        {
            if (!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".animation-playground-validation-project")
                || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || !float.IsFinite(worldPerUnit) || worldPerUnit < .1f || worldPerUnit > 2)
                throw new InvalidOperationException("Fit dodge art only in its marked background candidate.");
            if (File.Exists(ProjectionPath) || File.Exists(ProjectionPath + ".meta")) throw new IOException("Preserve an existing dodge projection.");
            var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
            GameObject actor = null; AnimationPlaygroundFootProjection dense = null; var mesh = new Mesh();
            var quality = QualitySettings.skinWeights;
            try
            {
                var session = TopDown3DAnimationPlaygroundSession.Find(scene); var b = session.Bindings; var profile = b.dodge;
                actor = new GameObject("Offline equipped dodge fitting"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, scene);
                actor.transform.position = Vector3.up * .91f;
                var motor = actor.AddComponent<TopDown3DPlayerMotor>(); var shape = actor.GetComponent<CapsuleCollider>();
                shape.height = b.capsuleHeight; shape.radius = b.capsuleRadius; shape.center = Vector3.zero;
                var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                EditorUtility.CopySerialized(session.Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
                driver.ConfigureLaboratory(b); Call(driver, "Awake"); actor.SetActive(true);
                var graph = Field<PlayableGraph>(driver, "playableGraph"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                if (!driver.ConfigureCamp(TopDown3DCampSettings.Load())) throw new InvalidDataException("The equipped fixture is unavailable.");
                driver.SetCampPresentation(-1, 1, false, true); Call(driver, "LateUpdate"); Call(driver, "NormalizeCampSkinWeights");
                var visual = Field<GameObject>(driver, "visualInstance");
                dense = AnimationPlaygroundFootProjectionAuthoring.BuildFixture(b.targetModel, visual, true, true);
                var sampler = dense.Bind(visual.transform); var selected = new HashSet<int>();
                var owner = new object(); var clip = profile.roll;
                var rootY = AnimationUtility.GetCurveBindings(clip).Single(k => k.propertyName == "RootT.y");
                var count = Mathf.CeilToInt(clip.length * 120); var maximumLift = 0f;
                float Minimum()
                {
                    var min = float.PositiveInfinity;
                    foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                        Vector3[] vertices;
                        if (renderer is SkinnedMeshRenderer skin)
                        {
                            if (TopDown3DPlayerAnimationDriver.IsSoftGarment(skin)) continue;
                            skin.BakeMesh(mesh, true); vertices = mesh.vertices;
                        }
                        else if (renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                            vertices = filter.sharedMesh.vertices;
                        else continue;
                        foreach (var vertex in vertices) min = Mathf.Min(min, renderer.transform.TransformPoint(vertex).y);
                    }
                    return min;
                }
                void Pose(float time)
                { driver.SetAnimationPresentationTime(owner, time); Call(driver, "PresentAnimationAction"); graph.Evaluate(0); }
                for (var pass = 0; pass < 3; pass++)
                {
                    var minima = Enumerable.Repeat(float.PositiveInfinity, count + 1).ToArray();
                    foreach (var mode in new[] { SkinWeights.OneBone, SkinWeights.TwoBones, SkinWeights.FourBones })
                    {
                        QualitySettings.skinWeights = mode; Call(driver, "NormalizeCampSkinWeights");
                        driver.EndAnimationPresentation(owner); driver.TryBeginAnimationPresentation(owner, clip);
                        for (var frame = 0; frame <= count; frame++)
                        { Pose(clip.length * frame / count); minima[frame] = Mathf.Min(minima[frame], Minimum()); }
                    }
                    var old = AnimationUtility.GetEditorCurve(clip, rootY); var keys = new Keyframe[count + 1];
                    for (var frame = 0; frame <= count; frame++)
                    {
                        var time = clip.length * frame / count; var lift = Mathf.Max(0, .006f - minima[frame]);
                        maximumLift = Mathf.Max(maximumLift, lift); keys[frame] = new Keyframe(time, old.Evaluate(time) + lift / worldPerUnit);
                    }
                    var curve = new AnimationCurve(keys);
                    for (var i = 0; i < keys.Length; i++)
                    { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
                    AnimationUtility.SetEditorCurve(clip, rootY, curve); EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
                }
                var minimumFinal = float.PositiveInfinity;
                var normals = new List<Vector3> { Vector3.up };
                foreach (var slope in new[] { 20f, 40f }) for (var yaw = 0; yaw < 360; yaw += 45)
                    normals.Add(Quaternion.Euler(0, yaw, 0) * Quaternion.AngleAxis(slope, Vector3.forward) * Vector3.up);
                foreach (var mode in new[] { SkinWeights.OneBone, SkinWeights.TwoBones, SkinWeights.FourBones })
                {
                    QualitySettings.skinWeights = mode; Call(driver, "NormalizeCampSkinWeights");
                    driver.EndAnimationPresentation(owner); driver.TryBeginAnimationPresentation(owner, clip);
                    for (var frame = 0; frame <= count; frame++)
                    {
                        Pose(clip.length * frame / count); minimumFinal = Mathf.Min(minimumFinal, Minimum());
                        foreach (var normal in normals)
                        { sampler.MinimumDistance(new Plane(normal, Vector3.zero), out var index); if (index >= 0) selected.Add(index); }
                    }
                }
                if (minimumFinal < -.001f) throw new InvalidDataException("The complete equipped source still penetrates the flat floor.");
                var oldProjection = profile.bodyProjection; var paths = dense.bonePaths.ToList();
                var verticesOut = selected.OrderBy(i => i).Select(i => dense.vertices[i]).ToList();
                foreach (var vertex in oldProjection.vertices)
                {
                    var points = new List<AnimationPlaygroundSkinPoint>();
                    foreach (var point in vertex.points)
                    {
                        var path = oldProjection.bonePaths[point.bone]; var index = paths.IndexOf(path);
                        if (index < 0) { index = paths.Count; paths.Add(path); }
                        points.Add(new AnimationPlaygroundSkinPoint { bone = index, weight = point.weight, position = point.position });
                    }
                    verticesOut.Add(new AnimationPlaygroundFootVertex { points = points.ToArray() });
                }
                var reduced = ScriptableObject.CreateInstance<AnimationPlaygroundFootProjection>();
                reduced.model = b.targetModel; reduced.bonePaths = paths.ToArray(); reduced.vertices = verticesOut.ToArray();
                if (!reduced.Validate(b.targetModel, out var reason)) throw new InvalidDataException(reason);
                AssetDatabase.CreateAsset(reduced, ProjectionPath); AssetDatabase.SaveAssetIfDirty(reduced);
                profile.bodyProjection = reduced; EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
                return new { minimumFinal, maximumLift, fullVertices = dense.vertices.Length, reducedVertices = reduced.vertices.Length, clip.length };
            }
            finally
            {
                if (actor != null) Object.DestroyImmediate(actor);
                Object.DestroyImmediate(dense); Object.DestroyImmediate(mesh); QualitySettings.skinWeights = quality;
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
