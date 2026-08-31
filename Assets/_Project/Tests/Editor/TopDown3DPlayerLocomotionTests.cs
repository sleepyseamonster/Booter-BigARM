using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DPlayerLocomotionTests
    {
        [Test]
        public void AccelerationProfile_UsesDistinctStartStopAndDirectionChangeRates()
        {
            Assert.That(
                TopDown3DLocomotionMath.SelectAccelerationRate(
                    Vector3.zero,
                    Vector3.forward * 4.2f,
                    8.5f,
                    12.5f,
                    20f,
                    13.5f),
                Is.EqualTo(8.5f).Within(0.0001f));
            Assert.That(
                TopDown3DLocomotionMath.SelectAccelerationRate(
                    Vector3.forward * 4.2f,
                    Vector3.zero,
                    8.5f,
                    12.5f,
                    20f,
                    13.5f),
                Is.EqualTo(20f).Within(0.0001f));
            Assert.That(
                TopDown3DLocomotionMath.SelectAccelerationRate(
                    Vector3.forward * 4.2f,
                    Vector3.back * 4.2f,
                    8.5f,
                    12.5f,
                    20f,
                    13.5f),
                Is.EqualTo(13.5f).Within(0.0001f));
        }

        [Test]
        public void MovementInput_UsesOneDeadzoneAuthorityAndOnlyClampsMagnitude()
        {
            var smallProcessedInput = new Vector2(0.06f, 0f);
            Assert.That(TopDown3DInputMath.ClampMove(smallProcessedInput), Is.EqualTo(smallProcessedInput));
            Assert.That(
                TopDown3DInputMath.ClampMove(new Vector2(2f, 0f)),
                Is.EqualTo(Vector2.right));
        }

        [Test]
        public void ResponseTiming_DefaultRatesStayInsideWeightyPlanBounds()
        {
            Assert.That(4.2f * 0.95f / 8.5f, Is.InRange(0.45f, 0.60f));
            Assert.That(7.4f * 0.95f / 8.5f, Is.InRange(0.80f, 1.00f));
            Assert.That(4.2f / 20f, Is.InRange(0.18f, 0.26f));
            Assert.That(7.4f / 20f, Is.InRange(0.32f, 0.42f));
            Assert.That(4.2f / 13.5f, Is.InRange(0.28f, 0.38f));
        }

        [Test]
        public void LocomotionSnapshot_PreservesAnimationFacingInputsWithoutOwningMotion()
        {
            var snapshot = new TopDown3DLocomotionSnapshot(
                Vector3.forward * 3f,
                Vector3.right * 4.2f,
                Vector3.right * 8.5f,
                Vector3.forward,
                120f,
                Vector3.up,
                true,
                false,
                false,
                0f,
                90f);

            Assert.That(snapshot.CurrentPlanarVelocity, Is.EqualTo(Vector3.forward * 3f));
            Assert.That(snapshot.DesiredPlanarVelocity, Is.EqualTo(Vector3.right * 4.2f));
            Assert.That(snapshot.FacingDirection, Is.EqualTo(Vector3.forward));
            Assert.That(snapshot.MeasuredYawRate, Is.EqualTo(120f).Within(0.0001f));
            Assert.That(snapshot.DirectionAlignment, Is.Zero.Within(0.0001f));
            Assert.That(snapshot.SignedHeadingError, Is.EqualTo(90f).Within(0.0001f));
            Assert.That(snapshot.TraversalOwnsMotion, Is.False);
        }

        [Test]
        public void TurnProfile_ReducesTurnRateOnlyAboveNormalRunSpeed()
        {
            Assert.That(
                TopDown3DLocomotionMath.SelectTurnSpeed(4.2f, 4.2f, 7.4f, 420f, 300f),
                Is.EqualTo(420f).Within(0.0001f));
            Assert.That(
                TopDown3DLocomotionMath.SelectTurnSpeed(7.4f, 4.2f, 7.4f, 420f, 300f),
                Is.EqualTo(300f).Within(0.0001f));
        }

        [Test]
        public void GaitWeights_AreContinuousAndNormalizedAcrossRunAndSprintBoundaries()
        {
            TopDown3DAnimationMath.CalculateGaitWeights(
                4.19f,
                1.8f,
                4.2f,
                7.4f,
                out var walkBeforeRun,
                out var runBeforeSprint,
                out var sprintBeforeRun);
            TopDown3DAnimationMath.CalculateGaitWeights(
                4.21f,
                1.8f,
                4.2f,
                7.4f,
                out var walkAfterRun,
                out var runAfterSprint,
                out var sprintAfterRun);

            Assert.That(walkBeforeRun + runBeforeSprint + sprintBeforeRun, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(walkAfterRun + runAfterSprint + sprintAfterRun, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Mathf.Abs(runBeforeSprint - runAfterSprint), Is.LessThan(0.01f));
            Assert.That(walkAfterRun, Is.Zero);
            Assert.That(sprintBeforeRun, Is.Zero);
        }

        [Test]
        public void GaitCadence_RemainsContinuousAtNominalRunSpeed()
        {
            TopDown3DAnimationMath.CalculateGaitWeights(
                4.19f,
                1.8f,
                4.2f,
                7.4f,
                out var walkBefore,
                out var runBefore,
                out var sprintBefore);
            TopDown3DAnimationMath.CalculateGaitWeights(
                4.21f,
                1.8f,
                4.2f,
                7.4f,
                out var walkAfter,
                out var runAfter,
                out var sprintAfter);
            var cadenceBefore = TopDown3DAnimationMath.CalculateGaitCyclesPerSecond(
                4.19f,
                1.8f,
                4.2f,
                7.4f,
                0.9f,
                1f,
                1f,
                1f,
                walkBefore,
                runBefore,
                sprintBefore);
            var cadenceAfter = TopDown3DAnimationMath.CalculateGaitCyclesPerSecond(
                4.21f,
                1.8f,
                4.2f,
                7.4f,
                0.9f,
                1f,
                1f,
                1f,
                walkAfter,
                runAfter,
                sprintAfter);

            Assert.That(Mathf.Abs(cadenceBefore - cadenceAfter), Is.LessThan(0.02f));
        }

        [Test]
        public void GaitPhaseContact_DetectsOrdinaryAndWrappedCrossingsOnce()
        {
            Assert.That(TopDown3DAnimationMath.DidPhaseCross(0.1f, 0.3f, 0.25f), Is.True);
            Assert.That(TopDown3DAnimationMath.DidPhaseCross(0.3f, 0.4f, 0.25f), Is.False);
            Assert.That(TopDown3DAnimationMath.DidPhaseCross(0.9f, 0.1f, 0f), Is.True);
            Assert.That(TopDown3DAnimationMath.DidPhaseCross(0.9f, 0.1f, 0.5f), Is.False);
        }

        [Test]
        public void StopCadenceAlignment_IsBoundedAndMovesTowardTheNextPlant()
        {
            var cadence = TopDown3DAnimationMath.AlignCadenceToStopContact(0.3f, 1f, 0.3f);
            Assert.That(cadence, Is.InRange(0.8f, 1.2f));
            Assert.That(cadence, Is.LessThan(1f));
        }

        [Test]
        public void TurnGaitWeight_IsSignedDirectionIndependentAndBounded()
        {
            var left = TopDown3DAnimationMath.CalculateTurnGaitWeight(-92f, 18f, 92f, 0.38f);
            var right = TopDown3DAnimationMath.CalculateTurnGaitWeight(92f, 18f, 92f, 0.38f);
            Assert.That(left, Is.EqualTo(0.38f).Within(0.0001f));
            Assert.That(right, Is.EqualTo(left).Within(0.0001f));
            Assert.That(
                TopDown3DAnimationMath.CalculateTurnGaitWeight(10f, 18f, 92f, 0.38f),
                Is.Zero);
        }

        [Test]
        public void DirectionalGait_UsesLocalTrajectoryAndMeasuredYawSymmetrically()
        {
            var localLeft = TopDown3DAnimationMath.ToLocalPlanarVelocity(
                Vector3.left + Vector3.forward,
                Vector3.forward);
            var localRight = TopDown3DAnimationMath.ToLocalPlanarVelocity(
                Vector3.right + Vector3.forward,
                Vector3.forward);
            var leftWeight = TopDown3DAnimationMath.CalculateDirectionalGaitWeight(
                localLeft,
                -180f,
                0.12f,
                0.65f,
                45f,
                240f,
                0.55f);
            var rightWeight = TopDown3DAnimationMath.CalculateDirectionalGaitWeight(
                localRight,
                180f,
                0.12f,
                0.65f,
                45f,
                240f,
                0.55f);

            Assert.That(leftWeight, Is.EqualTo(rightWeight).Within(0.0001f));
            Assert.That(leftWeight, Is.InRange(0f, 0.55f));
            Assert.That(
                TopDown3DAnimationMath.SelectDirectionalGaitSide(localLeft, 180f),
                Is.EqualTo(-1));
            Assert.That(
                TopDown3DAnimationMath.SelectDirectionalGaitSide(localRight, -180f),
                Is.EqualTo(1));
        }

        [Test]
        public void GroundedTravelState_HoldsStartAndStopUntilTheirReleaseThresholds()
        {
            Assert.That(
                TopDown3DAnimationMath.SelectGroundedTravelState(
                    TopDown3DLocomotionState.Idle,
                    0.1f,
                    4.2f,
                    0.35f,
                    1.2f,
                    0.15f,
                    0.75f,
                    0.15f),
                Is.EqualTo(TopDown3DLocomotionState.Starting));
            Assert.That(
                TopDown3DAnimationMath.SelectGroundedTravelState(
                    TopDown3DLocomotionState.Starting,
                    1.19f,
                    4.2f,
                    0.35f,
                    1.2f,
                    0.15f,
                    0.75f,
                    0.15f),
                Is.EqualTo(TopDown3DLocomotionState.Starting));
            Assert.That(
                TopDown3DAnimationMath.SelectGroundedTravelState(
                    TopDown3DLocomotionState.Sustained,
                    0.5f,
                    0.5f,
                    0.35f,
                    1.2f,
                    0.15f,
                    0.75f,
                    0.15f),
                Is.EqualTo(TopDown3DLocomotionState.Sustained));
            Assert.That(
                TopDown3DAnimationMath.SelectGroundedTravelState(
                    TopDown3DLocomotionState.Sustained,
                    4.2f,
                    0f,
                    0.35f,
                    1.2f,
                    0.15f,
                    0.75f,
                    0.15f),
                Is.EqualTo(TopDown3DLocomotionState.Stopping));
            Assert.That(
                TopDown3DAnimationMath.SelectGroundedTravelState(
                    TopDown3DLocomotionState.Stopping,
                    0.2f,
                    0f,
                    0.35f,
                    1.2f,
                    0.15f,
                    0.75f,
                    0.15f),
                Is.EqualTo(TopDown3DLocomotionState.Stopping));
            Assert.That(
                TopDown3DAnimationMath.SelectGroundedTravelState(
                    TopDown3DLocomotionState.Stopping,
                    0.14f,
                    0f,
                    0.35f,
                    1.2f,
                    0.15f,
                    0.75f,
                    0.15f),
                Is.EqualTo(TopDown3DLocomotionState.Idle));
        }

        [Test]
        public void SustainedCadence_ClampsTheDominantClipToProfilePlaybackBounds()
        {
            Assert.That(
                TopDown3DAnimationMath.ClampCadenceToPlaybackBounds(4f, 2f, 0.9f, 1.1f),
                Is.EqualTo(0.55f).Within(0.0001f));
            Assert.That(
                TopDown3DAnimationMath.ClampCadenceToPlaybackBounds(0.1f, 2f, 0.9f, 1.1f),
                Is.EqualTo(0.1f).Within(0.0001f));
        }

        [Test]
        public void HardTurnClassification_IsDirectionalLatchedAndTraversalSafe()
        {
            Assert.That(
                TopDown3DAnimationMath.SelectHardTurnDirection(-150f, 4.2f, true, false, false, 135f, 2.4f),
                Is.EqualTo(-1));
            Assert.That(
                TopDown3DAnimationMath.SelectHardTurnDirection(150f, 4.2f, true, false, false, 135f, 2.4f),
                Is.EqualTo(1));
            Assert.That(
                TopDown3DAnimationMath.SelectHardTurnDirection(150f, 4.2f, true, false, true, 135f, 2.4f),
                Is.Zero);
            Assert.That(
                TopDown3DAnimationMath.SelectHardTurnDirection(150f, 4.2f, true, true, false, 135f, 2.4f),
                Is.Zero);
            Assert.That(
                TopDown3DAnimationMath.ShouldReleaseHardTurnLatch(82f, 4.2f, 82f, 2.4f),
                Is.True);
        }

        [Test]
        public void LocomotionProfile_OwnsOneCompleteHumanoidNoRootMotionSet()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(
                TopDown3DPrototypeBuilder.LocomotionClipProfilePath);

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.TryValidate(out var error), Is.True, error);
            foreach (TopDown3DLocomotionClipRole role in System.Enum.GetValues(
                         typeof(TopDown3DLocomotionClipRole)))
            {
                var entry = profile.GetRequiredEntry(role);
                var path = AssetDatabase.GetAssetPath(entry.Clip);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                Assert.That(path, Does.StartWith("Assets/_Project/"), role.ToString());
                Assert.That(importer, Is.Not.Null, role.ToString());
                Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human), role.ToString());
                Assert.That(entry.Clip.isHumanMotion, Is.True, role.ToString());
            }
        }

        [Test]
        public void ProfilePhaseRemap_AlignsBothMeasuredContactsAcrossTwoStrideLoops()
        {
            var left = new[] { 0.44f, 0.93f };
            var right = new[] { 0.19f, 0.70f };

            Assert.That(
                TopDown3DAnimationMath.MapSemanticPhaseToSource(0f, left, right),
                Is.EqualTo(right[0]).Within(0.0001f));
            Assert.That(
                TopDown3DAnimationMath.MapSemanticPhaseToSource(0.25f, left, right),
                Is.EqualTo(left[0]).Within(0.0001f));
            Assert.That(
                TopDown3DAnimationMath.MapSemanticPhaseToSource(0.5f, left, right),
                Is.EqualTo(right[1]).Within(0.0001f));
            Assert.That(
                TopDown3DAnimationMath.MapSemanticPhaseToSource(0.75f, left, right),
                Is.EqualTo(left[1]).Within(0.0001f));
        }

        [Test]
        public void OneShotSideSelection_UsesTheNextMeasuredPlant()
        {
            var left = new[] { 0.44f, 0.93f };
            var right = new[] { 0.19f, 0.70f };

            Assert.That(
                TopDown3DAnimationMath.SelectNextPlantSide(0.05f, left, right),
                Is.EqualTo(TopDown3DFootSide.Left));
            Assert.That(
                TopDown3DAnimationMath.SelectNextPlantSide(0.35f, left, right),
                Is.EqualTo(TopDown3DFootSide.Right));
        }

        [Test]
        public void AnimationSpeedSmoothing_IsIndependentOfPhysicsStepSubdivision()
        {
            var oneLargeStep = TopDown3DAnimationMath.SmoothValue(0f, 4.2f, 10f, 0.1f);
            var fiveSmallSteps = 0f;
            for (var step = 0; step < 5; step++)
            {
                fiveSmallSteps = TopDown3DAnimationMath.SmoothValue(
                    fiveSmallSteps,
                    4.2f,
                    10f,
                    0.02f);
            }

            Assert.That(oneLargeStep, Is.EqualTo(fiveSmallSteps).Within(0.0001f));
        }
    }
}
