using BooterBigArm.Editor.WorldCreator.GroundedGeology;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator.GroundedGeology;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests.WorldCreator.GroundedGeology
{
    public sealed class GroundedGeologyWorkbenchTests
    {
        [Test]
        public void RockAdapter_BakesApprovedFactorIntoAllMeterValuesWithoutChangingSource()
        {
            var source = CreateRockFixture("Grounded Geology Adapter Test");
            try
            {
                source.transform.localScale = new Vector3(0.37f, 0.37f, 0.37f);
                var node = source.GetComponentInChildren<TopDown3DRockVolumeNode>();
                node.transform.localPosition = new Vector3(2f, 3f, -4f);
                node.transform.localScale = new Vector3(4f, 5f, 6f);
                var originalPosition = node.transform.localPosition;
                var originalScale = node.transform.localScale;
                var originalRootScale = source.transform.localScale;
                var before = ExistingWorkbenchFixtureAdapter.CaptureSourceHierarchySignature(source);

                Assert.That(ExistingWorkbenchFixtureAdapter.TryCapture(
                    source,
                    1729,
                    true,
                    out var snapshot,
                    out var error), Is.True, error);

                var element = snapshot.Recipe.StructuralElements[0];
                Assert.That(snapshot.PhysicalBakeFactor, Is.EqualTo(0.1f));
                Assert.That(element.LocalPositionMeters.X, Is.EqualTo(0.2d).Within(0.000001d));
                Assert.That(element.LocalPositionMeters.Y, Is.EqualTo(0.3d).Within(0.000001d));
                Assert.That(element.LocalPositionMeters.Z, Is.EqualTo(-0.4d).Within(0.000001d));
                Assert.That(element.SizeMeters.X, Is.EqualTo(0.4d).Within(0.000001d));
                Assert.That(element.SizeMeters.Y, Is.EqualTo(0.5d).Within(0.000001d));
                Assert.That(element.SizeMeters.Z, Is.EqualTo(0.6d).Within(0.000001d));
                Assert.That(snapshot.Recipe.SampleSpacingMeters, Is.EqualTo(0.01d).Within(0.000001d));
                Assert.That(snapshot.Recipe.BlendWidthMeters, Is.EqualTo(0.016d).Within(0.000001d));
                Assert.That(snapshot.Recipe.MaterialWavelengthMeters, Is.EqualTo(0.11d).Within(0.000001d));
                Assert.That(snapshot.SourceSignatureBefore, Is.EqualTo(before));
                Assert.That(snapshot.SourceWasPreserved, Is.True);
                Assert.That(node.transform.localPosition, Is.EqualTo(originalPosition));
                Assert.That(node.transform.localScale, Is.EqualTo(originalScale));
                Assert.That(source.transform.localScale, Is.EqualTo(originalRootScale));
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void FormationAdapter_ProducesTemporaryReferenceSnapshotWithoutRockScaleConversion()
        {
            var source = new GameObject("Grounded Geology Formation Adapter Test");
            try
            {
                var formation = source.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>();
                var child = new GameObject("Member Volume");
                child.transform.SetParent(source.transform, false);
                child.transform.localPosition = new Vector3(3f, 1f, -2f);
                child.transform.localScale = new Vector3(2f, 4f, 2f);
                child.AddComponent<TopDown3DRockVolumeNode>();

                Assert.That(ExistingWorkbenchFixtureAdapter.TryCapture(
                    source,
                    formation.FormationSeed,
                    true,
                    out var snapshot,
                    out var error), Is.True, error);

                Assert.That(snapshot.Recipe.FixtureKind,
                    Is.EqualTo(GroundedGeologyFixtureKind.TemporaryExistingFormation));
                Assert.That(snapshot.PhysicalBakeFactor, Is.EqualTo(1f));
                Assert.That(snapshot.Recipe.StructuralElements[0].LocalPositionMeters.X,
                    Is.EqualTo(3d).Within(0.000001d));
                Assert.That(snapshot.Recipe.SeamWidthMeters,
                    Is.EqualTo(formation.GeologicalSeamWidth).Within(0.000001d));
                Assert.That(snapshot.SourceWasPreserved, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void Comparison_BRootIsUnitAndMatchesReferenceBoundsColliderAndSamples()
        {
            var source = CreateRockFixture("Grounded Geology Comparison Test");
            GroundedGeologyComparisonPreview preview = null;
            try
            {
                source.transform.localScale = Vector3.one * 0.43f;
                var sourceMesh = source.GetComponent<MeshFilter>().sharedMesh;
                var sourceVertices = sourceMesh.vertices;
                var sourceSignature = ExistingWorkbenchFixtureAdapter.CaptureSourceHierarchySignature(source);

                Assert.That(GroundedGeologyComparisonBuilder.TryBuild(
                    source,
                    0.1f,
                    null,
                    out preview,
                    out var error), Is.True, error);

                Assert.That(preview.TargetB.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(preview.ReferenceA.transform.localScale, Is.EqualTo(Vector3.one * 0.1f));
                Assert.That(preview.Metrics.IsEquivalent(0.00001f), Is.True);
                Assert.That(preview.Metrics.MaximumRenderSampleDelta, Is.LessThanOrEqualTo(0.00001f));
                Assert.That(preview.Metrics.MaximumColliderSampleDelta, Is.LessThanOrEqualTo(0.00001f));
                Assert.That(ExistingWorkbenchFixtureAdapter.CaptureSourceHierarchySignature(source),
                    Is.EqualTo(sourceSignature));
                Assert.That(sourceMesh.vertices, Is.EqualTo(sourceVertices));
            }
            finally
            {
                if (preview != null && preview.Root != null)
                    Object.DestroyImmediate(preview.Root);
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void Comparison_AcceptsGeneratedMeshWhenAnEarlierFilterIsEmpty()
        {
            var source = new GameObject("Grounded Geology Nested Mesh Test");
            GroundedGeologyComparisonPreview preview = null;
            try
            {
                source.AddComponent<TopDown3DRockWorkbenchAuthoring>();
                var emptyFilter = new GameObject("Empty Preview Slot");
                emptyFilter.transform.SetParent(source.transform, false);
                emptyFilter.AddComponent<MeshFilter>();
                var generatedSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                generatedSurface.name = "Generated Preview Surface";
                generatedSurface.transform.SetParent(source.transform, false);

                Assert.That(GroundedGeologyComparisonBuilder.TryBuild(
                    source,
                    0.1f,
                    null,
                    out preview,
                    out var error), Is.True, error);
                Assert.That(preview.Metrics.IsEquivalent(0.00001f), Is.True);
            }
            finally
            {
                if (preview != null && preview.Root != null)
                    Object.DestroyImmediate(preview.Root);
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void TemporaryComparison_CancelRemovesPreviewAndPreservesSource()
        {
            var source = CreateRockFixture("Grounded Geology Cancel Test");
            try
            {
                var sourceSignature = ExistingWorkbenchFixtureAdapter.CaptureSourceHierarchySignature(source);
                Assert.That(GroundedGeologyComparisonBuilder.TryBuild(
                    source,
                    0.1f,
                    null,
                    out var preview,
                    out var error), Is.True, error);
                var root = preview.Root;
                Assert.That(GroundedGeologyComparisonBuilder.IsTemporary(root), Is.True);
                Assert.That(preview.IsAlive, Is.True);

                GroundedGeologyComparisonBuilder.Cancel(root);

                Assert.That(root == null, Is.True);
                Assert.That(preview.IsAlive, Is.False);
                Assert.That(ExistingWorkbenchFixtureAdapter.CaptureSourceHierarchySignature(source),
                    Is.EqualTo(sourceSignature));
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void TemporaryComparison_DestroyedChildMakesPreviewSafelyInactive()
        {
            var source = CreateRockFixture("Grounded Geology Destroyed Child Test");
            GroundedGeologyComparisonPreview preview = null;
            try
            {
                Assert.That(GroundedGeologyComparisonBuilder.TryBuild(
                    source,
                    0.1f,
                    null,
                    out preview,
                    out var error), Is.True, error);

                Object.DestroyImmediate(preview.TargetB);

                Assert.DoesNotThrow(() => _ = preview.IsAlive);
                Assert.That(preview.IsAlive, Is.False);
            }
            finally
            {
                if (preview != null && preview.Root != null)
                    Object.DestroyImmediate(preview.Root);
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void TemporaryComparison_CreationParticipatesInUndo()
        {
            var source = CreateRockFixture("Grounded Geology Undo Test");
            try
            {
                Undo.IncrementCurrentGroup();
                Assert.That(GroundedGeologyComparisonBuilder.TryBuild(
                    source,
                    0.1f,
                    null,
                    out var preview,
                    out var error), Is.True, error);
                var root = preview.Root;

                Undo.PerformUndo();

                Assert.That(root == null, Is.True);
                Assert.That(source, Is.Not.Null);

                Undo.PerformRedo();

                Assert.That(root != null, Is.True);
                Assert.That(root.GetComponentInChildren<MeshFilter>(true).sharedMesh, Is.Not.Null);
                GroundedGeologyComparisonBuilder.Cancel(root);
            }
            finally
            {
                Object.DestroyImmediate(source);
                Undo.ClearAll();
            }
        }

        [Test]
        public void SeedLockAndFixedReviewEnvironmentAreDeterministic()
        {
            var locked = new GroundedGeologyWorkbenchSeedState(73, true);
            var unlocked = new GroundedGeologyWorkbenchSeedState(73, false);

            Assert.That(locked.TryAdvance(), Is.False);
            Assert.That(locked.Seed, Is.EqualTo(73));
            Assert.That(unlocked.TryAdvance(), Is.True);
            Assert.That(unlocked.Seed, Is.EqualTo(unchecked(73 * 1103515245 + 12345)));
            Assert.That(GroundedGeologyReviewEnvironment.Cameras.Count, Is.EqualTo(9));
            Assert.That(GroundedGeologyReviewEnvironment.Cameras[0],
                Is.EqualTo(new GroundedGeologyReviewCameraPreset("Gameplay Baseline", 315f, 34f, 2.4f)));
            Assert.That(GroundedGeologyReviewEnvironment.LockedLightEuler,
                Is.EqualTo(new Vector3(42f, -34f, 0f)));
        }

        [Test]
        public void EditorAdapterAndRuntimeContractRemainInSeparateAssemblies()
        {
            Assert.That(typeof(ExistingWorkbenchFixtureAdapter).Assembly.GetName().Name,
                Is.EqualTo("BooterBigArm.Editor"));
            Assert.That(typeof(GroundedGeologyRecipe).Assembly.GetName().Name,
                Is.EqualTo("BooterBigArm.TopDown3D.Runtime"));
            Assert.That(typeof(ExistingWorkbenchFixtureAdapter).Assembly,
                Is.Not.EqualTo(typeof(GroundedGeologyRecipe).Assembly));
        }

        private static GameObject CreateRockFixture(string name)
        {
            var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
            source.name = name;
            var meshCollider = source.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            source.AddComponent<TopDown3DRockWorkbenchAuthoring>();
            var nodeObject = new GameObject("Source Volume");
            nodeObject.transform.SetParent(source.transform, false);
            nodeObject.transform.localPosition = new Vector3(0.5f, 0.5f, -0.25f);
            nodeObject.transform.localScale = new Vector3(2f, 1f, 1.5f);
            var node = nodeObject.AddComponent<TopDown3DRockVolumeNode>();
            node.SetShapeSeed(991);
            return source;
        }
    }
}
