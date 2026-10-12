using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor.SceneManagement;
using UnityEngine;

// Inert outside Assets. Explains bounded visual demand without moving/simulating actors.
public static class IsolatedPopulationCoverageInspect
{
    [Serializable] private sealed class Report
    {
        public bool heightClipTrusted;
        public float visualRadius;
        public Bounds landscape, cameraCoverage, shadowCoverage;
        public Vector3 cameraPosition, lightDirection;
        public string[] shaders;
    }
    public static void Run()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        if (!Application.isBatchMode || !root.StartsWith("D:\\BooterBigArmValidation\\PerformancePlayer", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Owned background copy required.");
        string output = Path.Combine(root, "Logs/population-height-coverage-inspection.json");
        if (File.Exists(output)) throw new InvalidOperationException("Fresh inspection required.");
        var scene = EditorSceneManager.OpenScene(FixedTerrainPopulationSectorBuilder.ActorScenePath);
        var roots = scene.GetRootGameObjects();
        var study = roots.SelectMany(r => r.GetComponentsInChildren<FixedTerrainPopulationSectorStudy>()).Single();
        var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
        var light = roots.SelectMany(r => r.GetComponentsInChildren<Light>()).Single();
        object Field(string name) => typeof(FixedTerrainPopulationSectorStudy).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(study);
        try
        {
            study.Initialize(); var template = (GameObject)Field("prefab");
            var report = new Report { heightClipTrusted = (bool)Field("clipVisualHeight"), visualRadius = (float)Field("visualRadius"),
                landscape = (Bounds)Field("landscape"), cameraPosition = camera.transform.position, lightDirection = light.transform.forward,
                cameraCoverage = (Bounds)typeof(FixedTerrainPopulationSectorStudy).GetMethod("CameraBounds", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(study, null),
                shaders = template.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Select(m => m.shader.name).Distinct().ToArray() };
            report.shadowCoverage = GreaterWastelandTerrainRendering.ShadowCasterBounds(report.cameraCoverage, report.landscape, report.lightDirection);
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
        }
        finally { study.Shutdown(); }
    }
}
