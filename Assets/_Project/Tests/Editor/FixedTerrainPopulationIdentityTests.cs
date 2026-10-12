using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainPopulationIdentityTests
    {
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
