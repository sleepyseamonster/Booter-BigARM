using System;
using System.IO;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    public static class PrototypeSaveService
    {
        public static string GetDefaultSavePath()
        {
            return Path.Combine(
                Application.persistentDataPath,
                PrototypeSaveSchema.DefaultSaveFolderName,
                PrototypeSaveSchema.DefaultSaveFileName);
        }

        public static void Save(PrototypeSaveData saveData, string savePath = null)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            var resolvedPath = ResolveSavePath(savePath);
            CreateDirectoryForPath(resolvedPath);

            var json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(resolvedPath, json);
        }

        public static bool TryLoad(out PrototypeSaveData saveData, out string error, string savePath = null)
        {
            var resolvedPath = ResolveSavePath(savePath);
            if (!File.Exists(resolvedPath))
            {
                saveData = null;
                error = $"No save file exists at '{resolvedPath}'.";
                return false;
            }

            try
            {
                var json = File.ReadAllText(resolvedPath);
                saveData = JsonUtility.FromJson<PrototypeSaveData>(json);
                if (saveData == null)
                {
                    error = $"Save file '{resolvedPath}' could not be deserialized.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                saveData = null;
                error = $"Failed to load save file '{resolvedPath}': {exception.Message}";
                return false;
            }

            error = null;
            return true;
        }

        public static bool Delete(string savePath = null)
        {
            var resolvedPath = ResolveSavePath(savePath);
            if (!File.Exists(resolvedPath))
            {
                return false;
            }

            File.Delete(resolvedPath);
            return true;
        }

        private static string ResolveSavePath(string savePath)
        {
            return string.IsNullOrWhiteSpace(savePath) ? GetDefaultSavePath() : savePath;
        }

        private static void CreateDirectoryForPath(string savePath)
        {
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
