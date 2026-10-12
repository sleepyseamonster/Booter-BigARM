using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>One authored-world checkpoint, independent of the retired generated-world save.</summary>
    [Serializable]
    public sealed class GreaterWastelandInventorySnapshot
    {
        public const string WorldIdentity = "greater-wasteland.authored.v1";
        public int version;
        public string worldId, cacheId;
        public Vector3 booterPosition, leggerPosition;
        public Quaternion booterRotation, leggerRotation;
        public TopDown3DInventorySnapshot kit, cargo, cache;
        public TopDown3DResourceWorldSnapshot resources;
        public TopDown3DPlacedHarvesterSnapshot harvesters;
        public TopDown3DSurvivalSnapshot survival;
        public TopDown3DAlgaeCanisterSnapshot algae;
        public bool campDeployed;
        public TopDown3DCampRecord camp;

        public bool HasValidHeader(string expectedCache)
        {
            return version == 1 && worldId == WorldIdentity && cacheId == expectedCache
                && Finite(booterPosition) && Finite(leggerPosition)
                && ValidRotation(booterRotation) && ValidRotation(leggerRotation)
                && kit != null && cargo != null && cache != null && resources != null && harvesters != null
                && survival != null && algae != null
                && (!campDeployed || camp != null && Guid.TryParseExact(camp.id, "N", out _)
                    && Finite(camp.position) && ValidRotation(camp.rotation));
        }
        public static bool Finite(Vector3 point) => float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
        public static bool ValidRotation(Quaternion q)
        {
            var norm = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            return float.IsFinite(norm) && Mathf.Abs(norm - 1f) < .01f;
        }
    }

    public static class GreaterWastelandInventoryFile
    {
        private const int MaximumBytes = 1024 * 1024;
        [Serializable] private sealed class Envelope { public int version; public string payload, sha256; }
        private static string Hash(string value)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
        public static string Encode(GreaterWastelandInventorySnapshot snapshot)
        {
            var payload = JsonUtility.ToJson(snapshot);
            return JsonUtility.ToJson(new Envelope { version = 1, payload = payload, sha256 = Hash(payload) });
        }
        public static bool TryDecode(string text, out GreaterWastelandInventorySnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(text) || Encoding.UTF8.GetByteCount(text) > MaximumBytes) return false;
            try
            {
                var file = JsonUtility.FromJson<Envelope>(text);
                if (file == null || file.version != 1 || string.IsNullOrWhiteSpace(file.payload) || file.sha256 != Hash(file.payload)) return false;
                snapshot = JsonUtility.FromJson<GreaterWastelandInventorySnapshot>(file.payload);
                return snapshot != null;
            }
            catch (ArgumentException) { return false; }
        }
        public static void Write(string path, GreaterWastelandInventorySnapshot snapshot)
        {
            WriteEncoded(path, Encode(snapshot));
        }
        // Immutable encoding is captured on the main thread. This method uses only filesystem/BCL APIs.
        internal static void WriteEncoded(string path, string text)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentException("A captured checkpoint is required.", nameof(text));
            if (Encoding.UTF8.GetByteCount(text) > MaximumBytes) throw new InvalidDataException("Checkpoint exceeds its size limit.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".pending";
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".backup");
            else File.Move(temporary, path);
        }
        public static bool Read(string path, out GreaterWastelandInventorySnapshot snapshot)
        {
            snapshot = null;
            return new FileInfo(path).Length <= MaximumBytes && TryDecode(File.ReadAllText(path), out snapshot);
        }
    }
}
