// Run through Unity Pipeline run_script, outside Assets; never saves scenes or assets.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CharacterScaleAudit
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var dirty = active.isDirty;
        var preview = EditorSceneManager.NewPreviewScene();
        var reports = new List<object>();
        try
        {
            var paths = new[] {
                "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/defaultmale_rig.fbx",
                "Assets/_Project/Art/Characters/AnimationPlayground/Qualification/MaleBaseMeshOriginalRig.fbx"
            };
            foreach (var path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) throw new FileNotFoundException(path);
                foreach (var scale in new[] { 1f, path.Contains("Prototype/") ? 1.4f : .123861514f })
                {
                    var instance = UnityEngine.Object.Instantiate(prefab);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, preview);
                    var graph = PlayableGraph.Create("Character scale audit");
                    try
                    {
                        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        instance.transform.localScale = Vector3.one * scale;
                        var animator = instance.GetComponentInChildren<Animator>();
                        animator.applyRootMotion = false;
                        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        var profile = AssetDatabase.LoadAssetAtPath<BooterBigArm.TopDown3D.TopDown3DLocomotionClipProfile>(
                            "Assets/_Project/Settings/Player/TopDown3DLocomotionClipProfile.asset");
                        var clip = profile.GetRequiredEntry(BooterBigArm.TopDown3D.TopDown3DLocomotionClipRole.Idle).Clip;
                        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        var playable = AnimationClipPlayable.Create(graph, clip);
                        playable.SetApplyFootIK(false);
                        var output = AnimationPlayableOutput.Create(graph, "Standing", animator);
                        output.SetSourcePlayable(playable);
                        graph.Play(); graph.Evaluate(.1f);
                        var meshes = new List<object>();
                        foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var source = renderer.sharedMesh;
                            // Independent linear skinning in world space: each weight applies bone * bindpose once.
                            var matrices = renderer.bones.Select((b, i) => b.localToWorldMatrix * source.bindposes[i]).ToArray();
                            var vertices = source.vertices; var weights = source.boneWeights;
                            var skinned = new Vector3[vertices.Length];
                            for (int i = 0; i < vertices.Length; i++)
                            {
                                var w = weights[i]; var v = vertices[i];
                                skinned[i] = matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0
                                    + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1
                                    + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2
                                    + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                            }
                            var baked = new Mesh();
                            try
                            {
                                renderer.BakeMesh(baked, false);
                                var noScale = BoundsOf(baked.vertices.Select(renderer.transform.TransformPoint));
                                renderer.BakeMesh(baked, true);
                                var withScale = BoundsOf(baked.vertices.Select(renderer.transform.TransformPoint));
                                var manualHeight = skinned.Max(v => v.y) - skinned.Min(v => v.y);
                                var bakedWorld = baked.vertices.Select(renderer.transform.TransformPoint).ToArray();
                                var bakedHeight = bakedWorld.Max(v => v.y) - bakedWorld.Min(v => v.y);
                                if (!float.IsFinite(manualHeight) || manualHeight <= 0 || Mathf.Abs(manualHeight - bakedHeight) > .001f)
                                    throw new InvalidOperationException("Independent skinning and compensated BakeMesh disagree: " + path);
                                meshes.Add(new { renderer = renderer.name, rendererScale = Components(renderer.transform.lossyScale),
                                    independentWorldSkinning = BoundsOf(skinned), bakeFalseThenTransform = noScale,
                                    bakeTrueThenTransform = withScale });
                            }
                            finally { UnityEngine.Object.DestroyImmediate(baked); }
                        }
                        reports.Add(new { path, guid = AssetDatabase.AssetPathToGUID(path), scale, pose = clip.name,
                            sampleSeconds = .1f, meshes });
                    }
                    finally { if (graph.IsValid()) graph.Destroy(); UnityEngine.Object.DestroyImmediate(instance); }
                }
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            if (active.isDirty != dirty || active != UnityEngine.SceneManagement.SceneManager.GetActiveScene())
                throw new InvalidOperationException("Active scene state changed.");
        }
        var root = Directory.GetParent(Application.dataPath).FullName;
        var destination = Path.Combine(root, "Docs/Evidence/Art/CharacterScaleAudit20261013/measurements.json");
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(new {
            measuredUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
            activeScene = active.path, activeSceneDirty = dirty, targetBodyHeightMetres = 1.8f, reports
        }, Newtonsoft.Json.Formatting.Indented));
        return destination;
    }

    private static object BoundsOf(IEnumerable<Vector3> points)
    {
        var values = points.ToArray();
        var min = values.Aggregate(Vector3.Min); var max = values.Aggregate(Vector3.Max);
        return new { min = Components(min), max = Components(max), size = Components(max - min) };
    }
    private static object Components(Vector3 v) => new { x = v.x, y = v.y, z = v.z };
}
