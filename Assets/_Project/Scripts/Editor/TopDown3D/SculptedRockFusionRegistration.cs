using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Registration is limited to authored sculpted-rock prefabs, never general scene meshes.</summary>
    public static class SculptedRockFusionRegistration
    {
        public static int RegisterAll()
        {
            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/TopDown3D/Environment", SculptedRockFusionTool.OutputFolder }
                .Where(AssetDatabase.IsValidFolder).ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Eligible(path) && Register(path)) changed++;
            }
            return changed;
        }
        internal static bool Eligible(string path) => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
            && (path.StartsWith("Assets/_Project/Prefabs/TopDown3D/Environment/Sculpted", StringComparison.Ordinal)
                || path.StartsWith(SculptedRockFusionTool.OutputFolder + "/", StringComparison.Ordinal));
        internal static bool Register(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponent<SculptedRockFusionParticipant>() != null) return false;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { root.AddComponent<SculptedRockFusionParticipant>(); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return true;
        }
    }
    public sealed class SculptedRockFusionPrefabImporter : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] old)
        {
            var eligible = imported.Concat(moved).Where(SculptedRockFusionRegistration.Eligible).ToArray();
            if (eligible.Length > 0) EditorApplication.delayCall += () =>
            { foreach (var path in eligible) if (File.Exists(path)) SculptedRockFusionRegistration.Register(path); };
        }
    }
}
