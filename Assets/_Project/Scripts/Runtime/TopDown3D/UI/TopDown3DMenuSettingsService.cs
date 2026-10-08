using System;
using System.IO;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DMenuSettings
    {
        public int version = 1;
        public bool largeText;
        public bool reducedMotion;
        public string viewDistancePreset = "Maximum";
        public TopDown3DMenuSettings Clone() => JsonUtility.FromJson<TopDown3DMenuSettings>(JsonUtility.ToJson(this));
    }

    public sealed class TopDown3DMenuSettingsService
    {
        private readonly string path;
        public TopDown3DMenuSettings Accepted { get; private set; } = new();
        public bool CanSave { get; private set; } = true;
        public string Notice { get; private set; }

        public TopDown3DMenuSettingsService(string directory)
        {
            path = Path.Combine(directory, "menu-settings-v1.json");
            try
            {
                if (!File.Exists(path)) return;
                var saved = JsonUtility.FromJson<TopDown3DMenuSettings>(File.ReadAllText(path));
                if (saved != null && saved.version > 1)
                { CanSave = false; Notice = "These settings are from a newer version. The file will be preserved."; return; }
                if (saved == null || saved.version != 1) throw new ArgumentException("Invalid settings version");
                saved.viewDistancePreset = BadwaterCameraRange.NormalizePreset(saved.viewDistancePreset);
                Accepted = saved;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { Notice = "Settings could not be read. Defaults are in use."; }
        }

        public bool TrySave(TopDown3DMenuSettings draft, out string error)
        {
            error = null;
            if (!CanSave || draft == null || draft.version != 1)
            { error = Notice ?? "Settings are invalid."; return false; }
            try
            {
                var normalized = draft.Clone();
                normalized.viewDistancePreset = BadwaterCameraRange.NormalizePreset(normalized.viewDistancePreset);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(normalized, true));
                if (File.Exists(path)) File.Replace(path + ".tmp", path, null);
                else File.Move(path + ".tmp", path);
                Accepted = normalized; Notice = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { error = "Could not save settings. Your edits are retained; try Apply again."; return false; }
        }
    }
}
