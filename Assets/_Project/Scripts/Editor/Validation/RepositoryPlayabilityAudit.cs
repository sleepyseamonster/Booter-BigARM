using System;
using System.IO;
using BooterBigArm.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Editor
{
    // Audit entry points never change Build Settings or save inspected scenes.
    public static class RepositoryPlayabilityAudit
    {
        private const string World = "Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity";
        private const string Valley = "Assets/_Project/Scenes/TopDown3D/BadwaterFourSlices.unity";

        public static void ValidateBothFromCli()
        {
            TopDown3DPrototypeValidator.ValidateFromCli();
            BooterBigArm.Editor.WorldCreator.WorldCreatorProductionPathValidator.ValidateMenu();
            ValidateMissingScripts(World);
            BadwaterPlayableSceneBuilder.ValidateFromCli();
            ValidateMissingScripts(Valley);
            BadwaterTerrainReadbackAudit.ValidateFromCli();
            int missingGroundMarkers = 0;
            foreach (var terrain in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
                if (terrain.GetComponent<BooterBigArm.TopDown3D.TopDown3DGroundSurface>() == null) missingGroundMarkers++;
            if (missingGroundMarkers > 0)
                Debug.LogWarning($"AUDIT_GAMEPLAY_GAP: {missingGroundMarkers} Death Valley terrain objects lack TopDown3DGroundSurface; existing structural validation does not prove actor grounding.");
            Debug.Log("AUDIT_BOTH_SCENES_STRUCTURALLY_VALIDATED");
        }





        private static void ValidateMissingScripts(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidDataException("Missing script in " + path + ": " + transform.name);
        }
    }
}
