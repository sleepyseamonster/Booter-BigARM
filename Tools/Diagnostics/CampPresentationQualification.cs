using System;
using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CampPresentationQualification
{
    public static string ValidateSaved()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || !File.Exists(Path.Combine(root, ".modularity-validation-project")) || Directory.Exists(Path.Combine(root, ".git")))
            throw new InvalidOperationException("Requires isolated modularity candidate");
        var scene = EditorSceneManager.OpenScene(CampPresentationMigration.ScenePath);
        var roots = scene.GetRootGameObjects();
        int missing = roots.Sum(g => g.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
        var rig = roots.SelectMany(g => g.GetComponentsInChildren<TopDown3DEnvironmentRig>(true)).Single();
        CampLookEditorPreview.Refresh();
        string repeat = CampPresentationMigration.DryRun();
        if (missing != 0 || !rig.IsOwner || rig.PreviewSky == null || !repeat.StartsWith("Version 1 already"))
            throw new InvalidOperationException("Persisted migration failed reference/lifecycle validation");
        return Newtonsoft.Json.JsonConvert.SerializeObject(new { scene = scene.path, missingScripts = missing, environmentOwner = rig.IsOwner,
            roots = roots.Select(g => g.name).ToArray(), phase = rig.clock.Phase, idempotent = repeat,
            optics = AssetDatabase.GetAssetPath(TopDown3DSceneRenderingOverride.CameraOpticsFor(rig.sky.outputCamera)), skyClone = rig.PreviewSky.name });
    }
    public static string Run()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".modularity-validation-project")))
            throw new InvalidOperationException("Requires the isolated modularity candidate.");
        var scene = EditorSceneManager.OpenScene(CampPresentationMigration.ScenePath);
        var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Single();
        var sun = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PerpetualTwilightSun>(true)).Single();
        var directory = Path.Combine(root, "Logs/Qualification"); Directory.CreateDirectory(directory);
        float authoredPhase = sun.Cycle01; var phases = new[] { authoredPhase, .25f, .75f };
        CampPresentationMigration.RegisterRendererFeature();
        var before = phases.Select((phase, i) => Capture(camera, sun, phase, Path.Combine(directory, "before-" + i + ".png"))).ToArray();
        var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToDictionary(t => t, t => t.localToWorldMatrix);
        var terrain = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).ToDictionary(t => t, t => t.terrainData);
        TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out var legacy);
        string protectedProfile = EditorJsonUtility.ToJson(legacy), protectedNative = EditorJsonUtility.ToJson(legacy.nativeOptics), protectedSky = EditorJsonUtility.ToJson(legacy.skyMaterial);
        string migration = CampPresentationMigration.InstallCurrentScene();
        foreach (var pair in transforms)
        {
            if (pair.Key == null) throw new InvalidOperationException("Migration destroyed an existing object.");
            for (int i = 0; i < 16; i++) if (Mathf.Abs(pair.Key.localToWorldMatrix[i] - pair.Value[i]) > .001f)
                throw new InvalidOperationException("Migration changed world pose: " + pair.Key.name);
        }
        foreach (var pair in terrain) if (pair.Key == null || pair.Key.terrainData != pair.Value) throw new InvalidOperationException("Native terrain reference changed.");
        bool protectedSourcesUnchanged = protectedProfile == EditorJsonUtility.ToJson(legacy)
            && protectedNative == EditorJsonUtility.ToJson(legacy.nativeOptics) && protectedSky == EditorJsonUtility.ToJson(legacy.skyMaterial);
        if (!protectedSourcesUnchanged) throw new InvalidOperationException("Migration changed a protected source asset.");
        var after = phases.Select((phase, i) => Capture(camera, sun, phase, Path.Combine(directory, "after-" + i + ".png"))).ToArray();
        var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>("Assets/_Project/Settings/Rendering/URP/Renderer3D.asset");
        var bloom = renderer.rendererFeatures.OfType<TopDown3DSunBloomFeature>().Single();
        bool wasActive = bloom.isActive; byte[] withoutBloomHost;
        try { bloom.SetActive(false); withoutBloomHost = Capture(camera, sun, phases[0], Path.Combine(directory, "without-old-bloom-host.png")); }
        finally { bloom.SetActive(wasActive); }
        bool opticsIndependentOfBloomHost = after[0].SequenceEqual(withoutBloomHost);
        if (!opticsIndependentOfBloomHost) throw new InvalidOperationException("Disabling the old bloom feature changed modular camera optics.");
        var results = before.Select((a, i) => {
            long error = 0; int maximum = 0, count = 0;
            for (int j = 0; j < a.Length; j++) { int delta = Math.Abs(a[j] - after[i][j]); error += delta; maximum = Math.Max(maximum, delta); if (delta != 0) count++; }
            return new { phase = phases[i], meanByteDifference = (double)error / a.Length, maximumByteDifference = maximum, changedChannels = count, channels = a.Length };
        }).ToArray();
        if (results.Any(r => r.maximumByteDifference != 0)) throw new InvalidOperationException("Matched rendering equivalence failed; inspect retained captures.");
        sun.SetCycle01(authoredPhase); CampLookEditorPreview.Release();
        var binding = camera.GetComponent<TopDown3DSceneRenderingOverride>();
        if (!TopDown3DSceneRenderingOverride.TryGetOpticsProfile(camera, out _) || binding.ModularOpticsConfigured == false)
            throw new InvalidOperationException("Missing independent optics binding");
        string repeat = CampPresentationMigration.InstallCurrentScene();
        EditorSceneManager.SaveScene(scene);
        var report = Newtonsoft.Json.JsonConvert.SerializeObject(new { migration, repeat, resolution = "960x540", preservedTransforms = transforms.Count, preservedTerrainReferences = terrain.Count, protectedSourcesUnchanged, opticsIndependentOfBloomHost, results, limitation = "Static EditMode candidate captures; no gameplay, Player or performance acceptance." }, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Path.Combine(directory, "qualification.json"), report);
        return report;
    }
    private static byte[] Capture(Camera camera, PerpetualTwilightSun sun, float phase, string path)
    {
        sun.SetCycle01(phase); sun.RefreshEditorPresentation();
        var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active; float aspect = camera.aspect;
        var target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGBHalf); target.Create();
        var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target; camera.aspect = 16f / 9f;
            camera.Render(); TopDown3DCampOpticsStack.Restore(camera);
            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG()); return image.GetRawTextureData<byte>().ToArray();
        }
        finally
        {
            TopDown3DCampOpticsStack.Restore(camera);
            camera.targetTexture = previousTarget; camera.aspect = aspect; RenderTexture.active = previousActive;
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
