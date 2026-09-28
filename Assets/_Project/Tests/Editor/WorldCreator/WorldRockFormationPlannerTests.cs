using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldRockFormationPlannerTests
    {
        private const string SettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        private const string IronstonePath =
            "Assets/_Project/Settings/World/Resource_IronstoneNode.asset";

        [Test]
        public void RealizedAuthoredFormations_MatchIronstoneCadence()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(SettingsPath);
            var ironstone = AssetDatabase.LoadAssetAtPath<TopDown3DResourceDefinition>(IronstonePath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(ironstone, Is.Not.Null);
            var formationCount = 0;
            var ironstoneCount = 0;
            var generator = new TopDown3DWorldGenerator(settings);
            var disabledSpawnExclusion = new Vector2(10000000f, 10000000f);
            for (var z = -4; z <= 4; z++)
            {
                for (var x = -4; x <= 4; x++)
                {
                    var chunk = new Vector2Int(x, z);
                    var formations = TopDown3DGeologicalRockAdapter.BuildPhysicalFormations(
                        settings,
                        generator,
                        settings.NaturalObjectCatalog,
                        chunk,
                        disabledSpawnExclusion);
                    formationCount += formations.Count;
                    var resources = TopDown3DResourceNodePlanner.BuildChunkPlacements(
                        settings,
                        generator,
                        settings.ResourceCatalog,
                        chunk,
                        disabledSpawnExclusion,
                        formations);
                    for (var resourceIndex = 0; resourceIndex < resources.Count; resourceIndex++)
                    {
                        if (resources[resourceIndex].ResourceId == ironstone.ResourceId)
                            ironstoneCount++;
                    }
                }
            }

            Assert.That(formationCount, Is.GreaterThan(0));
            Assert.That(ironstoneCount, Is.GreaterThan(0));
            Assert.That(formationCount / (float)ironstoneCount,
                Is.InRange(0.75f, 1.25f),
                $"Realized Scatter/Spire count {formationCount} should track realized "
                + $"ironstone count {ironstoneCount} in the same streamed terrain sample.");
        }

        [Test]
        public void AbsolutePlans_AreDeterministicGenealogicalAndDeltaAddressable()
        {
            using var runtime = WorldCreatorProductionRuntime.Create(3496479);
            var planner = CreatePlanner(runtime);
            var first = planner.PlanOwnerArea(
                -256d, -256d, 512d, 512d,
                new AbsoluteWorldPosition(0d, 0d, 0d), 18d);
            var second = planner.PlanOwnerArea(
                -256d, -256d, 512d, 512d,
                new AbsoluteWorldPosition(0d, 0d, 0d), 18d);

            Assert.That(first.Count, Is.GreaterThan(8));
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Id, Is.EqualTo(first[i].Id));
                Assert.That(second[i].NoveltyFingerprint, Is.EqualTo(first[i].NoveltyFingerprint));
                Assert.That(second[i].Members.Count, Is.EqualTo(first[i].Members.Count));
                Assert.That(first[i].Id.IsEmpty, Is.False);
                for (var memberIndex = 0; memberIndex < first[i].Members.Count; memberIndex++)
                {
                    var member = first[i].Members[memberIndex];
                    Assert.That(member.Id.IsEmpty, Is.False);
                    Assert.That(member.MemberIndex, Is.EqualTo(memberIndex));
                    Assert.That(member.ParentIndex, Is.LessThan(memberIndex));
                    Assert.That(member.Generation, Is.EqualTo(
                        member.ParentIndex < 0
                            ? 0
                            : first[i].Members[member.ParentIndex].Generation + 1));
                    Assert.That(second[i].Members[memberIndex].Id, Is.EqualTo(member.Id));
                }
            }
        }

        [Test]
        public void AdjacentOwnerAreas_HaveExclusiveBoundaryOwnership()
        {
            using var runtime = WorldCreatorProductionRuntime.Create(3496479);
            var planner = CreatePlanner(runtime);
            var protectedCenter = new AbsoluteWorldPosition(-10000d, 0d, -10000d);
            var left = planner.PlanOwnerArea(0d, 0d, 256d, 256d, protectedCenter, 0d);
            var right = planner.PlanOwnerArea(256d, 0d, 256d, 256d, protectedCenter, 0d);
            var ids = new HashSet<WorldFeatureId>();
            for (var i = 0; i < left.Count; i++)
            {
                Assert.That(left[i].Center.HorizontalA, Is.LessThan(256d));
                Assert.That(ids.Add(left[i].Id), Is.True);
            }
            for (var i = 0; i < right.Count; i++)
            {
                Assert.That(right[i].Center.HorizontalA, Is.GreaterThanOrEqualTo(256d));
                Assert.That(ids.Add(right[i].Id), Is.True, "Adjacent owner areas duplicated a formation ID.");
            }
        }

        [Test]
        public void GeologicalReservations_RespectRoutesAndCreateVariedSilhouettes()
        {
            using var runtime = WorldCreatorProductionRuntime.Create(3496479);
            var plans = CreatePlanner(runtime).PlanOwnerArea(
                -320d, -320d, 640d, 640d,
                new AbsoluteWorldPosition(0d, 0d, 0d), 18d);
            var fingerprints = new HashSet<ulong>();
            var goals = new HashSet<WorldRockCompositionGoal>();
            var scales = new HashSet<WorldRockReservationScale>();
            for (var i = 0; i < plans.Count; i++)
            {
                Assert.That(runtime.Query.TrySampleAffordance(
                    plans[i].Center,
                    WorldAgentProfile.BigArmProof,
                    out var affordance,
                    out var error), Is.True, error);
                Assert.That(affordance.ReservedRoute, Is.False);
                Assert.That(runtime.Query.TrySampleSurface(
                    plans[i].Center,
                    out var surface,
                    out error), Is.True, error);
                Assert.That(surface.Semantic
                    & (WorldSurfaceSemantic.SiteReservation | WorldSurfaceSemantic.Approach),
                    Is.EqualTo(WorldSurfaceSemantic.None));
                fingerprints.Add(plans[i].NoveltyFingerprint);
                goals.Add(plans[i].CompositionGoal);
                scales.Add(plans[i].ReservationScale);
            }

            Assert.That(plans.Count, Is.GreaterThan(12));
            Assert.That((float)fingerprints.Count / plans.Count, Is.GreaterThan(0.92f));
            Assert.That(goals.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(scales, Does.Contain(WorldRockReservationScale.Formation));
            Assert.That(scales, Does.Contain(WorldRockReservationScale.LandformAnchor));
        }

        [Test]
        public void BakedRealization_PreservesIdsAcrossReloadAndRebase()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(SettingsPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.NaturalObjectCatalog, Is.Not.Null);
            var absoluteChunk = FindPopulatedChunk(settings, new TopDown3DWorldGenerator(settings));
            var firstGenerator = new TopDown3DWorldGenerator(settings);
            var first = TopDown3DGeologicalRockAdapter.BuildPhysicalFormations(
                settings,
                firstGenerator,
                settings.NaturalObjectCatalog,
                absoluteChunk,
                Vector2.zero);
            Assert.That(first.Count, Is.GreaterThan(0));

            using var runtime = WorldCreatorProductionRuntime.Create(settings.WorldSeed);
            Assert.That(runtime.TryRebase(new AbsoluteWorldPosition(512d, 0d, -384d)), Is.True);
            var rebasedGenerator = new TopDown3DWorldGenerator(settings, runtime);
            var rebased = TopDown3DGeologicalRockAdapter.BuildPhysicalFormations(
                settings,
                rebasedGenerator,
                settings.NaturalObjectCatalog,
                absoluteChunk,
                Vector2.zero);
            Assert.That(rebased.Count, Is.EqualTo(first.Count));
            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(rebased[i].StableId, Is.EqualTo(first[i].StableId));
                Assert.That(rebased[i].Members.Count, Is.EqualTo(first[i].Members.Count));
                for (var memberIndex = 0; memberIndex < first[i].Members.Count; memberIndex++)
                {
                    Assert.That(rebased[i].Members[memberIndex].StableId,
                        Is.EqualTo(first[i].Members[memberIndex].StableId));
                    Assert.That(settings.NaturalObjectCatalog.GetRequiredMeshFamily(
                        first[i].Members[memberIndex].Shape,
                        first[i].Members[memberIndex].Variant).IsComplete, Is.True);
                }
            }
        }

        [Test]
        public void RealizedFormation_UsesCompleteAuthoredMembersWithStableIds()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(SettingsPath);
            var generator = new TopDown3DWorldGenerator(settings);
            var coordinate = FindPopulatedChunk(settings, generator);
            var formations = TopDown3DGeologicalRockAdapter.BuildPhysicalFormations(
                settings,
                generator,
                settings.NaturalObjectCatalog,
                coordinate,
                Vector2.zero);
            for (var formationIndex = 0; formationIndex < formations.Count; formationIndex++)
            {
                var formation = formations[formationIndex];
                var members = formation.Members;
                Assert.That(formation.AuthoredTemplate, Is.Not.Null);
                Assert.That(members.Count, Is.GreaterThan(0));
                Assert.That(formation.EnvelopeRadius, Is.GreaterThan(0f));
                var ids = new HashSet<string>();
                for (var i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    Assert.That(ids.Add(member.StableId), Is.True);
                    Assert.That(member.AuthoredFamily, Is.Not.Null);
                    Assert.That(member.AuthoredFamily.IsComplete, Is.True);
                    Assert.That(member.WorldBounds.size.sqrMagnitude, Is.GreaterThan(0f));
                }
            }
        }

        private static WorldRockFormationPlanner CreatePlanner(WorldCreatorProductionRuntime runtime)
        {
            return new WorldRockFormationPlanner(
                runtime.Identity,
                runtime.CoordinateModel,
                runtime.ContextProvider,
                runtime.Query);
        }

        private static Vector2Int FindPopulatedChunk(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator)
        {
            for (var radius = 0; radius <= 8; radius++)
            {
                for (var z = -radius; z <= radius; z++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(z) != radius) continue;
                        var coordinate = new Vector2Int(x, z);
                        if (TopDown3DGeologicalRockAdapter.BuildPhysicalFormations(
                            settings,
                            generator,
                            settings.NaturalObjectCatalog,
                            coordinate,
                            Vector2.zero).Count > 0)
                            return coordinate;
                    }
                }
            }
            Assert.Fail("No populated geological rock chunk was found inside the bounded proof search.");
            return default;
        }
    }
}
