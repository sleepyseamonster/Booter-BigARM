using System;
using System.Linq;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.Editor
{
    /// <summary>Disposable preview scene. Samples existing Unity clips; never edits a gameplay scene or clip.</summary>
    public sealed class WardrobeFittingPreview : EditorWindow
    {
        private PreviewRenderUtility preview;
        private GameObject character;
        private TopDown3DWardrobePresentation wardrobe;
        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        private AnimationClip clip, boundClip;
        private float time, angle = 20;
        private bool skin, jacket = true, armor = true;
        private string error;
        private Bounds bounds;

        [MenuItem("Booter & BigARM/Wardrobe/Fitting Preview")]
        private static void Open() => GetWindow<WardrobeFittingPreview>("Wardrobe fitting");
        private void OnDisable() => Dispose();
        private void Dispose()
        {
            if (graph.IsValid()) graph.Destroy();
            if (character) DestroyImmediate(character);
            preview?.Cleanup(); preview = null; character = null; wardrobe = null; boundClip = null;
        }
        private void Initialize()
        {
            Dispose();
            var rig = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeRig>(WardrobePilotAuthoring.Output + "/BodyRig.asset");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(WardrobePilotAuthoring.ModelPath);
            if (!rig || !model) { error = "Pilot assets are missing. See the wardrobe implementation receipt."; return; }
            preview = new PreviewRenderUtility();
            character = Instantiate(model); preview.AddSingleGO(character);
            var body = character.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s => s.sharedMesh == rig.bodyMesh);
            var neutral = AssetDatabase.LoadAssetAtPath<Material>(WardrobePilotAuthoring.Output + "/DiagnosticNeutral.mat");
            body.sharedMaterials = Enumerable.Repeat(neutral, body.sharedMesh.subMeshCount).ToArray();
            wardrobe = character.AddComponent<TopDown3DWardrobePresentation>(); wardrobe.Configure(character.transform, body, rig);
            bounds = body.bounds;
            var animator = character.GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Disposable wardrobe clip preview"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, "Character pose", animator);
            playable = AnimationClipPlayable.Create(graph, null); output.SetSourcePlayable(playable); graph.Play();
            Apply();
        }
        private void Apply()
        {
            if (!wardrobe) return;
            var names = new[] { skin ? "TextureSkin" : null, jacket ? "Jacket" : null, armor ? "Armor" : null };
            var definitions = names.Where(n => n != null).Select(n => AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeDefinition>(
                WardrobePilotAuthoring.Output + "/" + n + ".asset")).ToArray();
            wardrobe.TryReplace(definitions, out error);
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Technical fitting samples — appearance awaits review", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This isolated preview uses the animation-test character. Existing Unity clips stay in Unity. No gameplay scene or inventory is changed.", MessageType.Info);
            if (preview == null && GUILayout.Button("Load fitting character")) Initialize();
            EditorGUI.BeginChangeCheck();
            skin = EditorGUILayout.Toggle("Texture grid", skin); jacket = EditorGUILayout.Toggle("Fitted jacket shell", jacket);
            armor = EditorGUILayout.Toggle("Rigid chest plate", armor);
            if (EditorGUI.EndChangeCheck()) Apply();
            clip = (AnimationClip)EditorGUILayout.ObjectField("Existing Unity clip", clip, typeof(AnimationClip), false);
            time = EditorGUILayout.Slider("Clip time (seconds)", time, 0, clip ? clip.length : 1);
            angle = EditorGUILayout.Slider("View angle", angle, -180, 180);
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
            if (preview == null) return;
            var rect = GUILayoutUtility.GetRect(64, 10000, 100, 10000);
            if (Event.current.type != EventType.Repaint) return;
            if (clip != boundClip)
            {
                var output = graph.GetOutput(0);
                graph.DestroyPlayable(playable);
                playable = AnimationClipPlayable.Create(graph, clip); playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false);
                output.SetSourcePlayable(playable); boundClip = clip;
            }
            if (clip) { playable.SetTime(time); playable.SetSpeed(0); graph.Evaluate(0); }
            preview.BeginPreview(rect, GUIStyle.none);
            var direction = Quaternion.Euler(0, angle, 0) * new Vector3(0, .12f, 1.8f);
            preview.camera.transform.position = bounds.center + direction * bounds.size.y;
            preview.camera.transform.LookAt(bounds.center); preview.camera.orthographic = true;
            preview.camera.orthographicSize = bounds.size.y * .63f;
            preview.camera.nearClipPlane = .01f; preview.camera.farClipPlane = bounds.size.y * 10;
            preview.lights[0].intensity = 1.3f; preview.lights[0].transform.rotation = Quaternion.Euler(25, 160, 0);
            preview.ambientColor = new Color(.4f, .4f, .4f);
            var nodes = new List<GameObject>(); var meshes = new List<Mesh>(); var hidden = new List<SkinnedMeshRenderer>();
            try
            {
                foreach (var source in character.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s => s.enabled))
                {
                    var mesh = WardrobePoseBake.Bake(source); meshes.Add(mesh);
                    var node = new GameObject("Baked fitting pose"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(node, character.scene);
                    node.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation); node.transform.localScale = source.transform.lossyScale;
                    node.AddComponent<MeshFilter>().sharedMesh = mesh; node.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
                    source.enabled = false; hidden.Add(source); nodes.Add(node);
                }
                preview.camera.Render(); GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.ScaleToFit, false);
            }
            finally
            {
                foreach (var node in nodes) DestroyImmediate(node);
                foreach (var mesh in meshes) DestroyImmediate(mesh);
                foreach (var source in hidden) if (source) source.enabled = true;
            }
        }
    }
}
