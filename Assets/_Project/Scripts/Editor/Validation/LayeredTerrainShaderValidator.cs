using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class LayeredTerrainShaderValidator
    {
        public const string ShaderPath =
            "Assets/_Project/Shaders/TopDown3D/BrokenWorldTerrainBlend.shader";

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
