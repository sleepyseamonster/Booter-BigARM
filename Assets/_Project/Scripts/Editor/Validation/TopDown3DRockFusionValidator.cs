using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class TopDown3DRockFusionValidator
    {
        private const string ManifoldPath =
            "Assets/_Project/Plugins/Manifold/macOS/libmanifold.dylib";
        private const string ManifoldCPath =
            "Assets/_Project/Plugins/Manifold/macOS/libmanifoldc.dylib";
        private const string ManifoldSha256 =
            "d11e68c985dee63b57dbf54d18f6ec030caf348cd893ee76dad225a40fa1dc70";
        private const string ManifoldCSha256 =
            "e7f3f0fea0482e89d16e5490ae68c2d2e47b4872153501cf764a6b9d87847dbb";

        [MenuItem("Booter & BigARM/Top Down 3D/Validate Editor Rock Formation Fusion")]
        private static void ValidateFromMenu()
        {
            var errors = new List<string>();
            CollectErrors(errors);
            if (errors.Count == 0)
            {
                Debug.Log("Editor rock formation fusion dependency validation passed.");
                return;
            }

            Debug.LogError(string.Join("\n", errors));
        }

        public static void ValidateFromCli()
        {
            var errors = new List<string>();
            CollectErrors(errors);
            if (errors.Count > 0)
            {
                throw new BuildFailedException(string.Join("\n", errors));
            }

            Debug.Log("Editor rock formation fusion dependency validation passed.");
        }

        internal static void CollectErrors(ICollection<string> errors)
        {
            ValidatePlugin(ManifoldPath, ManifoldSha256, errors);
            ValidatePlugin(ManifoldCPath, ManifoldCSha256, errors);
        }

        private static void ValidatePlugin(
            string assetPath,
            string expectedSha256,
            ICollection<string> errors)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
            {
                errors.Add($"Rock fusion native plugin is missing or not imported: {assetPath}");
                return;
            }

            if (importer.GetCompatibleWithAnyPlatform()
                || !importer.GetCompatibleWithEditor()
                || importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX))
            {
                errors.Add(
                    $"Rock fusion plugin {assetPath} must target only the macOS Editor and remain excluded from players.");
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            var fullPath = projectRoot != null
                ? Path.Combine(projectRoot, assetPath)
                : Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                errors.Add($"Rock fusion native binary does not exist: {assetPath}");
                return;
            }

            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(fullPath))
            {
                var hash = ToLowerHex(sha256.ComputeHash(stream));
                if (hash != expectedSha256)
                {
                    errors.Add(
                        $"Rock fusion plugin hash drifted for {assetPath}. " +
                        $"Expected {expectedSha256}, found {hash}.");
                }
            }
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var characters = new char[bytes.Length * 2];
            const string alphabet = "0123456789abcdef";
            for (var i = 0; i < bytes.Length; i++)
            {
                characters[i * 2] = alphabet[bytes[i] >> 4];
                characters[i * 2 + 1] = alphabet[bytes[i] & 0x0F];
            }

            return new string(characters);
        }
    }
}
