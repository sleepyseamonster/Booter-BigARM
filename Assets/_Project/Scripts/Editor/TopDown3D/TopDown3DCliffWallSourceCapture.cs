using System;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>
    /// Captures the user's saved sample as an editable source recipe. The captured
    /// workbench prefab is not a runtime cliff mesh or a world-placement decision.
    /// </summary>
    public static class TopDown3DCliffWallSourceCapture
    {
        public const string ScenePath = "Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity";
        public const string PrefabPath =
            "Assets/_Project/Art/Environment/Rocks/Source/CliffWallSampleReference.prefab";
        private const string SourceRootName = "cliff wall";

        public static void CaptureFromCli()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Capture the cliff source outside Play Mode.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("The cliff source prefab already exists; inspect it before replacing it.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject source = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != SourceRootName) continue;
                if (source != null) throw new InvalidOperationException("The scene has more than one cliff wall root.");
                source = root;
            }
            if (source == null)
                throw new InvalidOperationException("The saved scene has no cliff wall root.");

            var rocks = source.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
            if (rocks.Length < 2)
                throw new InvalidOperationException("The cliff wall must contain multiple editable rock sources.");
            foreach (var rock in rocks)
            {
                if (rock.GetComponentsInChildren<TopDown3DRockVolumeNode>(true).Length == 0)
                    throw new InvalidOperationException($"{rock.name} has no saved source volumes.");
            }

            var copy = UnityEngine.Object.Instantiate(source);
            try
            {
                copy.name = "CliffWallSampleReference";
                copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                copy.transform.localScale = Vector3.one;
                foreach (var rock in copy.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true))
                    rock.SetSurfacePreset(TopDown3DRockSurfacePreset.LightCliffGray);
                var prefab = PrefabUtility.SaveAsPrefabAsset(copy, PrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("Unity did not save the cliff wall source prefab.");
                AssetDatabase.SaveAssets();
                var captured = prefab.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
                if (captured.Length != rocks.Length)
                    throw new InvalidOperationException("The captured cliff wall lost editable rock members.");
                Debug.Log($"Captured {captured.Length} cliff source rocks at {PrefabPath}. No gameplay mesh or world placement was changed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }
    }
}
