using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    [CreateAssetMenu(
        menuName = "Booter & BigARM/World Creator/Proof/Non-Canon Landscape Influence",
        fileName = "Proof_NonCanon_LandscapeInfluence")]
    public sealed class NonCanonProofInfluenceProfile : ScriptableObject, IWorldLandscapeInfluenceField
    {
        public const string RequiredIdPrefix = "proof.non-canon.";

        [SerializeField] private bool nonCanonProofOnly = true;
        [SerializeField] private string stableId = "proof.non-canon.influence.fractured-transect";
        [SerializeField, Min(1)] private int version = 1;
        [SerializeField] private string baselineProvinceId;
        [SerializeField] private string baselineStrataFamilyId;
        [SerializeField] private string influenceProvinceId;
        [SerializeField] private string influenceStrataFamilyId;
        [SerializeField] private double gradualStartHorizontalA = -768d;
        [SerializeField] private double gradualEndHorizontalA = 768d;
        [SerializeField] private double intentionalBoundaryHorizontalB = 384d;
        [SerializeField, Range(0.01f, 0.99f)] private float intentionalBoundaryWeight = 0.35f;

        public string StableId => stableId;
        public int Version => version;
        public bool NonCanonProofOnly => nonCanonProofOnly;
        public double GradualStartHorizontalA => gradualStartHorizontalA;
        public double GradualEndHorizontalA => gradualEndHorizontalA;
        public double IntentionalBoundaryHorizontalB => intentionalBoundaryHorizontalB;
        public float IntentionalBoundaryWeight => intentionalBoundaryWeight;

        public WorldLandscapeInfluenceSet Evaluate(AbsoluteWorldPosition absolutePosition)
        {
            if (!TryValidate(out var error))
            {
                throw new InvalidOperationException(error);
            }

            var linear = (absolutePosition.HorizontalA - gradualStartHorizontalA)
                / (gradualEndHorizontalA - gradualStartHorizontalA);
            linear = Math.Max(0d, Math.Min(1d, linear));
            var smooth = linear * linear * (3d - 2d * linear);
            var hardSide = absolutePosition.HorizontalB >= intentionalBoundaryHorizontalB ? 1f : 0f;
            var influenceWeight = (float)smooth * (1f - intentionalBoundaryWeight)
                + hardSide * intentionalBoundaryWeight;
            influenceWeight = Mathf.Clamp01(influenceWeight);
            return new WorldLandscapeInfluenceSet(
                new[]
                {
                    new WorldLandscapeInfluenceContribution(
                        RequiredIdPrefix + "influence.baseline",
                        baselineProvinceId,
                        baselineStrataFamilyId,
                        1f - influenceWeight),
                    new WorldLandscapeInfluenceContribution(
                        RequiredIdPrefix + "influence.fractured",
                        influenceProvinceId,
                        influenceStrataFamilyId,
                        influenceWeight)
                },
                hardSide);
        }

        public bool TryValidate(out string error)
        {
            if (!nonCanonProofOnly)
            {
                error = "The Batch 2 proof influence must remain explicitly non-canon.";
                return false;
            }

            if (!HasRequiredPrefix(stableId)
                || !HasRequiredPrefix(baselineProvinceId)
                || !HasRequiredPrefix(baselineStrataFamilyId)
                || !HasRequiredPrefix(influenceProvinceId)
                || !HasRequiredPrefix(influenceStrataFamilyId))
            {
                error = $"Every proof influence ID must begin with '{RequiredIdPrefix}'.";
                return false;
            }

            if (version < 1)
            {
                error = "The proof influence version must be positive.";
                return false;
            }

            if (!IsFinite(gradualStartHorizontalA)
                || !IsFinite(gradualEndHorizontalA)
                || gradualEndHorizontalA <= gradualStartHorizontalA)
            {
                error = "The proof influence gradual transition requires a finite increasing interval.";
                return false;
            }

            if (!IsFinite(intentionalBoundaryHorizontalB))
            {
                error = "The proof influence intentional boundary must be finite.";
                return false;
            }

            if (float.IsNaN(intentionalBoundaryWeight)
                || float.IsInfinity(intentionalBoundaryWeight)
                || intentionalBoundaryWeight <= 0f
                || intentionalBoundaryWeight >= 1f)
            {
                error = "The intentional boundary weight must remain inside (0, 1).";
                return false;
            }

            if (string.Equals(baselineProvinceId, influenceProvinceId, StringComparison.Ordinal)
                || string.Equals(baselineStrataFamilyId, influenceStrataFamilyId, StringComparison.Ordinal))
            {
                error = "The proof influence requires distinct baseline and influenced geology definitions.";
                return false;
            }

            error = null;
            return true;
        }

        public void ConfigureProofData(
            string authoredStableId,
            int authoredVersion,
            string authoredBaselineProvinceId,
            string authoredBaselineStrataFamilyId,
            string authoredInfluenceProvinceId,
            string authoredInfluenceStrataFamilyId,
            double authoredGradualStartHorizontalA,
            double authoredGradualEndHorizontalA,
            double authoredIntentionalBoundaryHorizontalB,
            float authoredIntentionalBoundaryWeight)
        {
            nonCanonProofOnly = true;
            stableId = authoredStableId;
            version = authoredVersion;
            baselineProvinceId = authoredBaselineProvinceId;
            baselineStrataFamilyId = authoredBaselineStrataFamilyId;
            influenceProvinceId = authoredInfluenceProvinceId;
            influenceStrataFamilyId = authoredInfluenceStrataFamilyId;
            gradualStartHorizontalA = authoredGradualStartHorizontalA;
            gradualEndHorizontalA = authoredGradualEndHorizontalA;
            intentionalBoundaryHorizontalB = authoredIntentionalBoundaryHorizontalB;
            intentionalBoundaryWeight = authoredIntentionalBoundaryWeight;
        }

        private static bool HasRequiredPrefix(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.StartsWith(RequiredIdPrefix, StringComparison.Ordinal);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
