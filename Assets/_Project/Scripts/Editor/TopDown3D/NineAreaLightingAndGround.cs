using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Study-only ground UV and lighting alignment; preserves terrain heights and authored rocks.</summary>
    public static class NineAreaLightingAndGround
    {
        public const string LightingReference = "Assets/_Project/Scenes/Reference/GreaterWastelandLightingReference_20261010.unity";
        public static void Apply()
        {
            var study = SceneManager.GetActiveScene();
            if (study.path != NineAreaRockStudy.ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open the study in Edit Mode.");
            if (study.isDirty) throw new InvalidOperationException("Preserve unsaved study edits before alignment.");
            var sourceLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(NineAreaRockStudy.SourceLayer);
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(NineAreaRockStudy.Folder + "/StudyBase.terrainlayer");
            if (sourceLayer == null || layer == null) throw new InvalidOperationException("Missing base layers.");
            Undo.RecordObject(layer, "Sample a small ground patch");
            layer.diffuseTexture = sourceLayer.diffuseTexture;
            layer.tileSize = sourceLayer.tileSize;
            // Sample only a pad-sized region around the original source tile's centre.
            layer.tileOffset = sourceLayer.tileOffset + Vector2.one * ((256f - NineAreaRockStudy.AreaSize) / 2f);
            EditorUtility.SetDirty(layer); AssetDatabase.SaveAssetIfDirty(layer);
            var getSettings = typeof(RenderSettings).GetMethod("GetRenderSettings", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("The pinned Editor's RenderSettings accessor is unavailable.");
            var reference = EditorSceneManager.OpenScene(LightingReference, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(reference);
                var sourceSettings = (UnityEngine.Object)getSettings.Invoke(null, null);
                var sourceSun = reference.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PerpetualTwilightSun>()).Single();
                var sourceLight = sourceSun.GetComponent<Light>();
                SceneManager.SetActiveScene(study);
                var targetSettings = (UnityEngine.Object)getSettings.Invoke(null, null);
                Undo.RecordObject(targetSettings, "Match world environment lighting");
                EditorUtility.CopySerialized(sourceSettings, targetSettings);
                foreach (var old in study.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Light>()).Where(l => l.type == LightType.Directional).ToArray())
                    Undo.DestroyObjectImmediate(old.gameObject);
                var cloned = UnityEngine.Object.Instantiate(sourceSun.gameObject);
                SceneManager.MoveGameObjectToScene(cloned, study);
                cloned.name = "Greater Wasteland Sun";
                Undo.RegisterCreatedObjectUndo(cloned, "Match world sun");
                var light = cloned.GetComponent<Light>();
                // Instantiate remaps the controller's internal Light reference. Apply its exact saved cycle.
                cloned.GetComponent<PerpetualTwilightSun>().Configure(light);
                cloned.transform.rotation = sourceLight.transform.rotation;
                RenderSettings.sun = light;
                var camera = study.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
                Undo.RecordObject(camera, "Use world sky lighting"); camera.clearFlags = CameraClearFlags.Skybox;
                EditorSceneManager.MarkSceneDirty(study);
                if (!EditorSceneManager.SaveScene(study)) throw new InvalidOperationException("Could not save aligned study.");
            }
            finally { SceneManager.SetActiveScene(study); EditorSceneManager.CloseScene(reference, true); }
            SceneView.RepaintAll();
        }
    }
}
