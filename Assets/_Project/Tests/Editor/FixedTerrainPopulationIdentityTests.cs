using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainPopulationIdentityTests
    {
        [Test]
        public void VersionOneHexEncodingMatchesOriginalAndReducesAllocation()
        {
            var identity = Context(); byte[] prefix;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write("greater-wasteland.population-key.v1"); writer.Write(identity.WorldId); writer.Write(identity.Seed);
                    writer.Write(identity.GenerationVersion); writer.Write(identity.RecipeId); writer.Write(identity.RecipeVersion);
                }
                prefix = stream.ToArray();
            }
            string Original(Vector2Int cell, int ordinal)
            {
                var input = new byte[prefix.Length + 16]; Buffer.BlockCopy(prefix, 0, input, 0, prefix.Length);
                var tail = input.AsSpan(prefix.Length);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(tail, cell.x);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(tail.Slice(4), cell.y);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(tail.Slice(8), ordinal);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(tail.Slice(12), 0);
                using var sha = SHA256.Create(); var bytes = sha.ComputeHash(input);
                var text = new StringBuilder("gw-pop-v1-", 74);
                foreach (var value in bytes) text.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                return text.ToString();
            }
            var cell = new Vector2Int(-7, 11);
            for (int i = 0; i < 128; i++) Assert.That(identity.CandidateId(cell, i), Is.EqualTo(Original(cell, i)));
            Assert.That(MeasureAllocationCount(() => GC.KeepAlive(new byte[4096])), Is.GreaterThan(0), "Allocation observer must detect a positive control.");
            long optimized = MeasureAllocationCount(() => { for (int i = 0; i < 100; i++) identity.CandidateId(cell, i); });
            long original = MeasureAllocationCount(() => { for (int i = 0; i < 100; i++) Original(cell, i); });
            TestContext.WriteLine($"HEX_ALLOCATION_EVENTS_100_IDS original={original} optimized={optimized}");
            Assert.That(optimized, Is.LessThan(original - 16 * 100), "Remove the per-byte formatting allocations, including ordinals beyond the study slots.");
        }

        internal static long MeasureAllocationCount(Action action)
        {
            using var recorder = new ProfilerRecorder(ProfilerCategory.Memory, "GC.Alloc", 1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.SumAllSamplesInFrame);
            Assert.That(recorder.Valid, Is.True, "A supported Unity allocation marker is required.");
            recorder.Start(); action(); recorder.Stop();
            // GC.Alloc is a marker: Count is the number of allocation events, not byte size.
            var sample = recorder.Count == 0 ? default : recorder.GetSample(recorder.Count - 1);
            TestContext.WriteLine($"GC_ALLOC_OBSERVER count={sample.Count} value={sample.Value}");
            return (long)sample.Count;
        }

        [Test]
        public void CandidateIdentityAndPositionsSurviveReverseOrderAndContextReconstruction()
        {
            var first = Context(); var recreated = Context(); var ids = new HashSet<string>();
            var expected = new Dictionary<string, Vector2>();
            for (int z = -1; z <= 1; z++) for (int x = -1; x <= 1; x++)
                for (int ordinal = 0; ordinal < FixedTerrainPopulationIdentity.CandidatesPerCell; ordinal++)
                {
                    var cell = new Vector2Int(x, z); var id = first.CandidateId(cell, ordinal);
                    Assert.That(ids.Add(id), Is.True);
                    var point = first.CandidatePosition(cell, ordinal);
                    Assert.That(FixedTerrainPopulationIdentity.CellAt(point), Is.EqualTo(cell));
                    expected.Add(id, point);
                }
            for (int z = 1; z >= -1; z--) for (int x = 1; x >= -1; x--)
                for (int ordinal = FixedTerrainPopulationIdentity.CandidatesPerCell - 1; ordinal >= 0; ordinal--)
                {
                    var cell = new Vector2Int(x, z); var id = recreated.CandidateId(cell, ordinal);
                    Assert.That(recreated.CandidatePosition(cell, ordinal), Is.EqualTo(expected[id]));
                    recreated.Sample01(cell, ordinal, 99); // Another feature stream cannot advance placement.
                    Assert.That(recreated.CandidatePosition(cell, ordinal), Is.EqualTo(expected[id]));
                }
        }

        [Test]
        public void NegativeBordersIndependentStreamsAndRecipeVersionsAreExplicit()
        {
            Assert.That(FixedTerrainPopulationIdentity.CellAt(new Vector2(-.001f, -256)), Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(FixedTerrainPopulationIdentity.CellAt(new Vector2(256, 0)), Is.EqualTo(new Vector2Int(1, 0)));
            var first = Context(); var cell = new Vector2Int(-1, 0);
            var versioned = new FixedTerrainPopulationIdentity(first.WorldId, first.Seed, 1, first.RecipeId, 2);
            Assert.That(first.CandidateId(cell, 3), Is.Not.EqualTo(versioned.CandidateId(cell, 3)));
            Assert.That(first.CandidateId(cell, 3), Is.Not.EqualTo(first.CandidateId(cell, 4)));
            Assert.That(first.Sample01(cell, 3, 1), Is.InRange(0d, .999999999999d));
            Assert.That(first.CandidateId(cell, 64), Is.Not.EqualTo(first.CandidateId(cell, 0)), "Identity is not restricted to the study's 64 slots.");
            Assert.Throws<ArgumentOutOfRangeException>(() => first.CandidatePosition(cell, 64));
            Assert.Throws<ArgumentOutOfRangeException>(() => first.CandidateId(cell, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => first.Sample01(cell, 3, 0));
            Assert.Throws<ArgumentException>(() => FixedTerrainPopulationIdentity.CellAt(new Vector2(float.NaN, 0)));
        }
        [Test]
        public void VersionOneEncodingMatchesIndependentReferenceVector()
        {
            // Python hashlib + explicit UTF-8 length prefixes / struct.pack('<iiii') produced this vector.
            var context = Context(); var cell = new Vector2Int(-1, 0);
            Assert.That(context.CandidateId(cell, 3), Is.EqualTo("gw-pop-v1-835403681839ccb74725b91a18a757c5e36b4965835b61c17dc565c4df2eba06"));
            var point = context.CandidatePosition(cell, 3);
            Assert.That(point.x, Is.EqualTo(-138.6273433148861).Within(.0001));
            Assert.That(point.y, Is.EqualTo(18.243134635686875).Within(.0001));
        }
        private static FixedTerrainPopulationIdentity Context() => new(GreaterWastelandInventorySnapshot.WorldIdentity, 17, 1, "fixture.clutter", 1);
    }
}
