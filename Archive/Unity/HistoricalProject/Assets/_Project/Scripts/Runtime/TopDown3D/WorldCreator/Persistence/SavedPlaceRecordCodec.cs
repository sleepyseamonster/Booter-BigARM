using System;
using System.IO;
using System.Text;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public static class SavedPlaceRecordCodec
    {
        private const uint Magic = 0x50534357U;
        private const int CodecVersion = 2;
        private const int MaximumPayloadBytes = 64 * 1024;

        public static byte[] Encode(SavedPlaceRecord record)
        {
            using var stream = new MemoryStream(512);
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true))
            {
                writer.Write(Magic);
                writer.Write(CodecVersion);
                WriteFeatureId(writer, record.SavedPlaceId);
                WriteManifest(writer, record.Manifest);
                writer.Write(record.PlayerName);
                WriteAddress(writer, record.Address);
                WriteAbsolutePosition(writer, record.AbsolutePosition);
                writer.Write(record.ThematicCoordinate.HasValue);
                if (record.ThematicCoordinate.HasValue)
                {
                    writer.Write(record.ThematicCoordinate.AuthorityId);
                    writer.Write(record.ThematicCoordinate.Version);
                    writer.Write(record.ThematicCoordinate.Payload);
                }
                writer.Write(record.FeatureReferences.Count);
                for (var i = 0; i < record.FeatureReferences.Count; i++)
                {
                    writer.Write(record.FeatureReferences[i].Role);
                    WriteFeatureId(writer, record.FeatureReferences[i].FeatureId);
                }
            }

            return stream.ToArray();
        }

        internal static byte[] EncodeLegacyV1ForTests(SavedPlaceRecord record)
        {
            using var stream = new MemoryStream(512);
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true))
            {
                writer.Write(Magic);
                writer.Write(1);
                WriteFeatureId(writer, record.SavedPlaceId);
                WriteManifest(writer, record.Manifest);
                WriteAddress(writer, record.Address);
                writer.Write(record.HasAnchorFeature);
                WriteFeatureId(writer, record.AnchorFeatureId);
            }
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] payload, out SavedPlaceRecord record, out string error)
        {
            record = default;
            error = string.Empty;
            if (payload == null || payload.Length == 0 || payload.Length > MaximumPayloadBytes)
            {
                error = "Saved-place payload size is invalid.";
                return false;
            }

            try
            {
                using var stream = new MemoryStream(payload, false);
                using var reader = new BinaryReader(stream, new UTF8Encoding(false, true), true);
                if (reader.ReadUInt32() != Magic)
                {
                    error = "Saved-place payload header is invalid.";
                    return false;
                }

                var codecVersion = reader.ReadInt32();
                if (codecVersion != 1 && codecVersion != CodecVersion)
                {
                    error = $"Saved-place codec version {codecVersion} is not supported.";
                    return false;
                }

                var savedPlaceId = ReadFeatureId(reader);
                var manifest = ReadManifest(reader);
                if (codecVersion == 1)
                {
                    var legacyAddress = ReadAddress(reader);
                    var hasAnchor = reader.ReadBoolean();
                    var anchor = ReadFeatureId(reader);
                    if (stream.Position != stream.Length)
                    {
                        error = "Saved-place payload has trailing data.";
                        return false;
                    }
                    record = new SavedPlaceRecord(
                        savedPlaceId,
                        manifest,
                        "Saved place",
                        legacyAddress,
                        default,
                        OptionalThematicCoordinatePayload.None,
                        hasAnchor
                            ? new[] { new WorldFeatureReference("anchor", anchor) }
                            : Array.Empty<WorldFeatureReference>());
                    return true;
                }

                var playerName = ReadBoundedString(reader, 64, "player name");
                var address = ReadAddress(reader);
                var absolutePosition = ReadAbsolutePosition(reader);
                OptionalThematicCoordinatePayload thematic = default;
                if (reader.ReadBoolean())
                {
                    thematic = new OptionalThematicCoordinatePayload(
                        true,
                        ReadBoundedString(reader, 128, "thematic authority id"),
                        reader.ReadInt32(),
                        ReadBoundedString(reader, 4096, "thematic coordinate payload"));
                }
                var referenceCount = reader.ReadInt32();
                if (referenceCount < 0 || referenceCount > 64)
                    throw new InvalidDataException("Saved-place feature reference count is invalid.");
                var references = new WorldFeatureReference[referenceCount];
                for (var i = 0; i < referenceCount; i++)
                {
                    references[i] = new WorldFeatureReference(
                        ReadBoundedString(reader, 64, "feature role"),
                        ReadFeatureId(reader));
                }
                if (stream.Position != stream.Length)
                {
                    error = "Saved-place payload has trailing data.";
                    return false;
                }

                record = new SavedPlaceRecord(
                    savedPlaceId,
                    manifest,
                    playerName,
                    address,
                    absolutePosition,
                    thematic,
                    references);
                return true;
            }
            catch (Exception exception) when (
                exception is EndOfStreamException
                || exception is IOException
                || exception is ArgumentException
                || exception is OverflowException
                || exception is DecoderFallbackException)
            {
                error = $"Saved-place payload is invalid: {exception.Message}";
                record = default;
                return false;
            }
        }

        private static void WriteManifest(BinaryWriter writer, WorldPersistenceManifest manifest)
        {
            writer.Write(manifest.SchemaVersion);
            writer.Write(manifest.World.Seed);
            var versions = manifest.World.Versions;
            writer.Write(versions.Topology);
            writer.Write(versions.Coordinate);
            writer.Write(versions.Landform);
            writer.Write(versions.Material);
            writer.Write(versions.Decoration);
            writer.Write(versions.Resource);
            writer.Write(versions.Site);
            writer.Write(manifest.CoordinateModelId);
            writer.Write(manifest.CoordinateModelVersion);
        }

        private static WorldPersistenceManifest ReadManifest(BinaryReader reader)
        {
            var schemaVersion = reader.ReadInt32();
            var seed = reader.ReadInt64();
            var versions = new WorldVersionManifest(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32());
            var coordinateModelId = ReadBoundedString(reader, 128, "coordinate model id");
            var coordinateModelVersion = reader.ReadInt32();
            return new WorldPersistenceManifest(
                schemaVersion,
                new WorldIdentity(seed, versions),
                coordinateModelId,
                coordinateModelVersion);
        }

        private static void WriteAddress(BinaryWriter writer, WorldCoordinateAddress address)
        {
            writer.Write(address.ModelId);
            writer.Write(address.ModelVersion);
            writer.Write(address.CanonicalValue);
        }

        private static WorldCoordinateAddress ReadAddress(BinaryReader reader)
        {
            return new WorldCoordinateAddress(
                ReadBoundedString(reader, 128, "coordinate model id"),
                reader.ReadInt32(),
                ReadBoundedString(reader, 4096, "canonical coordinate"));
        }

        private static void WriteAbsolutePosition(BinaryWriter writer, AbsoluteWorldPosition position)
        {
            writer.Write(position.HorizontalA);
            writer.Write(position.Vertical);
            writer.Write(position.HorizontalB);
        }

        private static AbsoluteWorldPosition ReadAbsolutePosition(BinaryReader reader)
        {
            return new AbsoluteWorldPosition(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
        }

        private static void WriteFeatureId(BinaryWriter writer, WorldFeatureId featureId)
        {
            writer.Write(featureId.High);
            writer.Write(featureId.Low);
        }

        private static WorldFeatureId ReadFeatureId(BinaryReader reader)
        {
            return new WorldFeatureId(reader.ReadUInt64(), reader.ReadUInt64());
        }

        private static string ReadBoundedString(BinaryReader reader, int maximumLength, string fieldName)
        {
            var value = reader.ReadString();
            if (value.Length > maximumLength)
            {
                throw new InvalidDataException($"Saved-place {fieldName} is too long.");
            }

            return value;
        }
    }
}
