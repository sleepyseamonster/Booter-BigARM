using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class RockSurfaceShaderValidator
    {
        [MenuItem("Booter & BigARM/Top Down 3D/Validate Rock Surface Shader")]
        public static void ValidateFromMenu()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(TopDown3DPrototypeBuilder.RockShaderPath);
            if (shader == null)
            {
                Debug.LogError($"Missing rock surface shader at {TopDown3DPrototypeBuilder.RockShaderPath}.");
                return;
            }

            var messages = ShaderUtil.GetShaderMessages(shader);
            for (var i = 0; i < messages.Length; i++)
            {
                var message = messages[i];
                Debug.LogError(
                    $"Rock surface shader {message.severity}: {message.file}:{message.line}: {message.message}");
            }

            if (!shader.isSupported || messages.Length > 0)
            {
                Debug.LogError(
                    $"Rock surface shader validation failed. Supported={shader.isSupported}; messages={messages.Length}.");
                return;
            }

            Debug.Log("Rock surface shader validation passed.");
        }
    }
}
