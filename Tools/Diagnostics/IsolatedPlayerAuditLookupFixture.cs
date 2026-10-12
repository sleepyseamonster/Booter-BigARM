using System;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Copy into an isolated validation Editor assembly; never production Assets.
public static class IsolatedPlayerAuditLookupFixture
{
    public static void Verify()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        if (!Application.isBatchMode || !root.StartsWith("D:\\BooterBigArmValidation\\PerformanceImplementation", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the owned small background validation copy.");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            var owner = new GameObject("Transient renderer fixture");
            SceneManager.MoveGameObjectToScene(owner, scene);
            var controller = owner.AddComponent<GreaterWastelandTerrainRendering>();
            if (UnityEngine.Object.FindAnyObjectByType<GreaterWastelandTerrainRendering>() != controller)
                throw new InvalidOperationException("Normal scene ownership must be visible to general lookup before applying DontSave.");
            owner.hideFlags = HideFlags.DontSave;
            if (UnityEngine.Object.FindAnyObjectByType<GreaterWastelandTerrainRendering>() == controller)
                throw new InvalidOperationException("Fixture must demonstrate the general lookup exclusion.");
            if (GreaterWastelandPlayerAudit.FindTerrainRendering(scene) != controller)
                throw new InvalidOperationException("Explicit scene-root lookup missed transient ownership.");
            controller.enabled = false;
            if (GreaterWastelandPlayerAudit.FindTerrainRendering(scene) != null)
                throw new InvalidOperationException("Disabled ownership must not be selected.");
            controller.enabled = true; UnityEngine.Object.DestroyImmediate(owner);
            if (GreaterWastelandPlayerAudit.FindTerrainRendering(scene) != null)
                throw new InvalidOperationException("Destroyed ownership must not be retained.");
            Debug.Log("PLAYER_AUDIT_LOOKUP_VERIFIED: DontSave root resolved; disabled and destroyed roots rejected.");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
