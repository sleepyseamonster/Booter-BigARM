using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class LayeredTerrainShaderValidator
    {
        public const string ShaderPath =
            "Assets/_Project/Shaders/TopDown3D/BrokenWorldTerrainBlend.shader";

        // Compiler-only check: no scene, gameplay, or authoring state is changed.
        public static void CompileLightingVariants()
        {
            AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
                throw new System.InvalidOperationException($"Missing shader at {ShaderPath}.");

            var variants = new ShaderVariantCollection();
            try
            {
                foreach (var fast in new[] { false, true })
                {
                    foreach (var lighting in new[] { 0, 1, 2 })
                    {
                        var keywords = new System.Collections.Generic.List<string>();
                        if (lighting > 0)
                            keywords.AddRange(new[] { "INSTANCING_ON", "_ADDITIONAL_LIGHTS",
                                "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT" });
                        if (lighting > 1)
                            keywords.AddRange(new[] { "_ADDITIONAL_LIGHT_SHADOWS", "FOG_LINEAR" });
                        if (fast)
                            keywords.Add("TOPDOWN3D_PLAYTEST_FAST_TERRAIN");
                        if (!variants.Add(new ShaderVariantCollection.ShaderVariant(shader,
                                UnityEngine.Rendering.PassType.ScriptableRenderPipeline, keywords.ToArray())))
                            throw new System.InvalidOperationException("Could not add terrain shader variant.");
                        Debug.Log($"Terrain compiler variant: {string.Join(", ", keywords)}");
                    }
                }

                variants.WarmUp();
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                        throw new System.InvalidOperationException(
                            $"{message.file}:{message.line}: {message.message}");
                if (!variants.isWarmedUp || !shader.isSupported)
                    throw new System.InvalidOperationException("Terrain shader warmup failed or shader unsupported.");
                Debug.Log($"Terrain lighting compilation completed: {variants.variantCount} variants; " +
                          $"{SystemInfo.graphicsDeviceType}. No scene or gameplay checks performed.");
            }
            finally
            {
                Object.DestroyImmediate(variants);
            }
        }

        [MenuItem("Booter & BigARM/Top Down 3D/Validate Layered Terrain Shader")]
        public static void ValidateFromMenu()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"Missing layered terrain shader at {ShaderPath}.");
                return;
            }

            var messages = ShaderUtil.GetShaderMessages(shader);
            for (var i = 0; i < messages.Length; i++)
            {
                var message = messages[i];
                Debug.LogError(
                    $"Layered terrain shader {message.severity}: {message.file}:{message.line}: {message.message}");
            }

            if (!shader.isSupported || messages.Length > 0)
            {
                Debug.LogError(
                    $"Layered terrain shader validation failed. Supported={shader.isSupported}; messages={messages.Length}.");
                return;
            }

            Debug.Log("Layered terrain shader validation passed.");
        }
    }
}
