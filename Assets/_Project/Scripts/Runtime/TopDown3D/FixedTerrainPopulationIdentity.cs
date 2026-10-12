using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Immutable keyed candidate identity, independent of rendering/work partition and visitation order.</summary>
    public sealed class FixedTerrainPopulationIdentity
    {
        public const int CellSize = 256;
        public const int CandidateGridSize = 8;
        public const int CandidatesPerCell = CandidateGridSize * CandidateGridSize;
        private readonly byte[] prefix;
        public string WorldId { get; }
        public int Seed { get; }
        public int GenerationVersion { get; }
        public string RecipeId { get; }
        public int RecipeVersion { get; }

        public FixedTerrainPopulationIdentity(string worldId, int seed, int generationVersion, string recipeId, int recipeVersion)
        {
            if (string.IsNullOrWhiteSpace(worldId) || string.IsNullOrWhiteSpace(recipeId))
                throw new ArgumentException("Explicit world and recipe identities are required.");
            if (generationVersion < 1 || recipeVersion < 1) throw new ArgumentOutOfRangeException(nameof(generationVersion));
            WorldId = worldId; Seed = seed; GenerationVersion = generationVersion; RecipeId = recipeId; RecipeVersion = recipeVersion;
            // Protocol v1: BinaryWriter length-prefixed UTF-8 strings and little-endian signed 32-bit integers.
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write("greater-wasteland.population-key.v1"); writer.Write(worldId); writer.Write(seed);
                writer.Write(generationVersion); writer.Write(recipeId); writer.Write(recipeVersion);
            }
            prefix = stream.ToArray();
        }

        public string CandidateId(Vector2Int cell, int ordinal)
        {
            return EncodeHex(Digest(cell, ordinal, 0), "gw-pop-v1-");
        }

        // Same lowercase v1 encoding, without one formatting string per digest byte.
        internal static string EncodeHex(byte[] bytes, string prefix = "")
        {
            const string digits = "0123456789abcdef";
            var chars = new char[prefix.Length + bytes.Length * 2];
            prefix.CopyTo(0, chars, 0, prefix.Length);
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[prefix.Length + i * 2] = digits[bytes[i] >> 4];
                chars[prefix.Length + i * 2 + 1] = digits[bytes[i] & 15];
            }
            return new string(chars);
        }

        /// <summary>Bounded study lattice only. Recipes can use other positions with the same ordinal identity protocol.</summary>
        public Vector2 CandidatePosition(Vector2Int cell, int ordinal)
        {
            if (ordinal < 0 || ordinal >= CandidatesPerCell) throw new ArgumentOutOfRangeException(nameof(ordinal));
            float slot = (float)CellSize / CandidateGridSize;
            double x = (double)cell.x * CellSize + (ordinal % CandidateGridSize + .1 + .8 * Sample01(cell, ordinal, 1)) * slot;
            double z = (double)cell.y * CellSize + (ordinal / CandidateGridSize + .1 + .8 * Sample01(cell, ordinal, 2)) * slot;
            return new Vector2((float)x, (float)z);
        }

        public double Sample01(Vector2Int cell, int ordinal, int stream)
        {
            if (stream < 1) throw new ArgumentOutOfRangeException(nameof(stream), "Stream zero belongs to identity.");
            var bytes = Digest(cell, ordinal, stream);
            uint value = (uint)bytes[0] | (uint)bytes[1] << 8 | (uint)bytes[2] << 16 | (uint)bytes[3] << 24;
            return value / 4294967296.0;
        }

        public static Vector2Int CellAt(Vector2 position)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y)) throw new ArgumentException("Finite population coordinates are required.");
            double x = Math.Floor((double)position.x / CellSize), z = Math.Floor((double)position.y / CellSize);
            if (x < int.MinValue || x > int.MaxValue || z < int.MinValue || z > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(position));
            return new Vector2Int((int)x, (int)z);
        }

        private byte[] Digest(Vector2Int cell, int ordinal, int stream)
        {
            ValidateOrdinal(ordinal);
            var input = new byte[prefix.Length + 16];
            Buffer.BlockCopy(prefix, 0, input, 0, prefix.Length);
            WriteInt(input, prefix.Length, cell.x); WriteInt(input, prefix.Length + 4, cell.y);
            WriteInt(input, prefix.Length + 8, ordinal); WriteInt(input, prefix.Length + 12, stream);
            using var sha = SHA256.Create();
            return sha.ComputeHash(input);
        }
        private static void ValidateOrdinal(int ordinal)
        { if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal)); }
        private static void WriteInt(byte[] bytes, int offset, int value)
        { uint bits = unchecked((uint)value); for (int i = 0; i < 4; i++) bytes[offset + i] = (byte)(bits >> (i * 8)); }
    }
}
