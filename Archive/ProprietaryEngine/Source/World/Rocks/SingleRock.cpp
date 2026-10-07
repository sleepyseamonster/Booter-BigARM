// Port of the preserved Unity single-rock planner. See SINGLE_ROCK_PARITY.md.
#include "World/Rocks/SingleRock.h"
#include "World/Rocks/SingleRockMath.h"
namespace engine { namespace single_rock {
struct Planner {
static inline const Quaternion GoldenRockRestingRotation=Quaternion::Euler(Vector3{-18.374f,147.048f,-85.817f});
static constexpr float MinimumGoldenRockBurialFraction=.04f,MaximumGoldenRockBurialFraction=.12f;
static constexpr int32_t GoldenRockYawSalt=int32_t(0xD1B54A35u),GoldenRockBurialSalt=int32_t(0x94D049BBu),StandaloneWidthSalt=int32_t(0xA24BAED4u),StandaloneBodyLengthSalt=int32_t(0x9FB21C65u);
static constexpr int DimensionBellSampleCount=3;
        static std::vector<Spec> CreatePlan(
            int seed,
            int cubeCount,
            Vector3 overallSize,
            float verticality,
            float asymmetry,
            float overlap,
            Profile silhouetteProfile =
                Profile::Auto,
            float majorFractures = 0.f)
        {
            cubeCount = Mathf::Clamp(cubeCount, 2, 4);
            overallSize = Vector3{
                Mathf::Max(0.05f, overallSize.x),
                Mathf::Max(0.05f, overallSize.y),
                Mathf::Max(0.05f, overallSize.z)};
            verticality = Mathf::Clamp01(verticality);
            asymmetry = Mathf::Clamp01(asymmetry);
            overlap = Mathf::Clamp01(overlap);
            silhouetteProfile = ResolveSilhouetteProfile(seed, silhouetteProfile);
            if (silhouetteProfile == Profile::Shard
                && overallSize.y / Mathf::Max(overallSize.x, overallSize.z) >= 5.f)
            {
                // An already extreme physical aspect supplies the shard silhouette by itself.
                // Keeping the weathered profile here prevents tapered tips from becoming
                // thinner than the workbench voxel grid.
                silhouetteProfile = Profile::Boulder;
            }
            auto profileVerticality = GetProfileVerticality(verticality, silhouetteProfile);

            auto random = Random(seed);
            auto raw = std::vector<Spec>{};
            auto preferredAngle = NextRange(random, 0.f, Mathf::PI * 2.f);
            auto preferredDirection = Vector3{
                Mathf::Cos(preferredAngle),
                0.f,
                Mathf::Sin(preferredAngle)};
            auto sharedRotation = CreateSharedRotation(random, asymmetry);

            auto firstScale = CreateProfileCoreScale(
                random,
                verticality,
                asymmetry,
                silhouetteProfile);
            raw.push_back(Spec{
                Vector3::zero,
                CreateRoleRotation(random, sharedRotation, asymmetry, Role::Core),
                firstScale,
                Role::Core,
                GetProfileCoreShape(silhouetteProfile)});

            auto supportCount = cubeCount <= 2
                ? 1
                : Mathf::Clamp(Mathf::CeilToInt((cubeCount - 1) * 0.6f), 1, cubeCount - 2);

            for (auto index = 1; index < cubeCount; index++)
            {
                auto role = index <= supportCount
                    ? Role::Support
                    : Role::Detail;
                auto roleIndex = role == Role::Support
                    ? index - 1
                    : index - supportCount - 1;
                auto roleCount = role == Role::Support
                    ? supportCount
                    : cubeCount - supportCount - 1;
                auto parentIndex = ChooseParentIndex(
                    random,
                    index,
                    supportCount,
                    profileVerticality,
                    asymmetry,
                    role);
                auto parent = raw[parentIndex];
                auto childScale = CreateRoleScale(
                    random,
                    firstScale,
                    role,
                    roleIndex,
                    roleCount,
                    profileVerticality,
                    asymmetry);
                childScale = ApplyProfileScale(
                    random,
                    childScale,
                    firstScale,
                    index,
                    role,
                    silhouetteProfile);
                auto childRotation = CreateRoleRotation(random, sharedRotation, asymmetry, role);
                auto direction = CreateGrowthDirection(
                    random,
                    preferredDirection,
                    index,
                    profileVerticality,
                    asymmetry,
                    role);
                auto sourceShape = ChooseSourceShape(
                    seed,
                    index,
                    role,
                    asymmetry,
                    silhouetteProfile);
                if (silhouetteProfile == Profile::Boulder
                    && cubeCount >= 3
                    && index == 1)
                {
                    sourceShape = Shape::Wedge;
                }
                else if (silhouetteProfile == Profile::Boulder
                         && cubeCount >= 3
                         && index == supportCount + 1)
                {
                    sourceShape = Shape::TaperedStone;
                }
                childRotation = OrientSourceShapeTowardGrowth(
                    childRotation,
                    direction,
                    sourceShape);

                auto parentRadius = DirectionalRadius(parent.LocalScale, parent.LocalRotation, direction);
                auto childRadius = DirectionalRadius(childScale, childRotation, direction);
                auto overlapDepth = Mathf::Lerp(
                    0.18f,
                    0.8f,
                    Mathf::SmoothStep(
                        0.f,
                        1.f,
                        GetProfileOverlap(overlap, silhouetteProfile)));
                if (silhouetteProfile != Profile::Boulder)
                {
                    // Wedges and tapered stones remove part of their nominal box. Preserve
                    // enough overlap for a connected surface without burying their entire
                    // silhouette inside the dominant core mass.
                    auto safeLowCompactionOverlap = silhouetteProfile
                        == Profile::Slab
                        ? 0.76f
                        : 0.72f;
                    auto minimumShapedOverlap = Mathf::Lerp(
                        safeLowCompactionOverlap,
                        0.64f,
                        Mathf::InverseLerp(0.f, 0.5f, overlap));
                    overlapDepth = Mathf::Max(overlapDepth, minimumShapedOverlap);
                }
                overlapDepth = Mathf::Clamp01(
                    overlapDepth
                    + GetShapeOverlapAllowance(sourceShape)
                    + GetShapeOverlapAllowance(parent.SourceShape) * 0.65f);
                auto centerDistance = (parentRadius + childRadius) * (1.f - overlapDepth);
                auto childPosition = parent.LocalPosition + direction * centerDistance;
                raw.push_back(Spec{
                    childPosition,
                    childRotation,
                    childScale,
                    role,
                    sourceShape});
            }

            auto fitted = FitPlanToSizeAndGround(raw, overallSize);
            return AddFractureCuts(
                fitted,
                seed,
                overallSize,
                silhouetteProfile,
                majorFractures);
        }
        static int DeriveVolumeShapeSeed(int generationSeed, int volumeIndex)
        {
            {
                auto value = (uint32_t)generationSeed + 0x9E3779B9u * (uint32_t)(volumeIndex + 1);
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (int)value;
            }
        }
        static Vector2 CreateBellCurvedStandaloneDimensions(int seed)
        {
            return Vector2{
                SampleBellCurvedDimension(seed, StandaloneWidthSalt),
                SampleBellCurvedDimension(seed, StandaloneBodyLengthSalt)};
        }
        static float SampleBellCurvedDimension(int seed, int salt)
        {
            auto normalized = 0.f;
            for (auto sample = 0; sample < DimensionBellSampleCount; sample++)
            {
                normalized += HashToUnitFloat(
                    DeriveVolumeShapeSeed(seed ^ salt, sample));
            }

            normalized /= DimensionBellSampleCount;
            return Mathf::Lerp(
                Defaults::MinimumGeneratedRockDimension,
                Defaults::MaximumGeneratedRockDimension,
                normalized);
        }
        static std::vector<Spec> ApplyGoldenRockRestingPose(
            const std::vector<Spec>& plan,
            int seed)
        {
            if (plan.size() == 0) return plan;

            auto yaw = CalculateGoldenRockYaw(seed);
            auto restingRotation = Quaternion::AngleAxis(yaw, Vector3::up)
                * GoldenRockRestingRotation;
            auto rotated = std::vector<Spec>{};
            for (const auto& spec : plan)
            {
                rotated.push_back(Spec{
                    restingRotation * spec.LocalPosition,
                    restingRotation * spec.LocalRotation,
                    spec.LocalScale,
                    spec.Role,
                    spec.SourceShape,
                    spec.Operation});
            }

            auto bounds = CalculateBounds(rotated);
            auto burialFraction = Mathf::Lerp(
                MinimumGoldenRockBurialFraction,
                MaximumGoldenRockBurialFraction,
                HashToUnitFloat(DeriveVolumeShapeSeed(seed ^ GoldenRockBurialSalt, 0)));
            auto groundOffset = -bounds.min.y - bounds.size().y * burialFraction;
            for (auto index = 0; index < rotated.size(); index++)
            {
                auto spec = rotated[index];
                rotated[index] = Spec{
                    spec.LocalPosition + Vector3::up * groundOffset,
                    spec.LocalRotation,
                    spec.LocalScale,
                    spec.Role,
                    spec.SourceShape,
                    spec.Operation};
            }
            return rotated;
        }
        static float CalculateGoldenRockYaw(int seed)
        {
            auto seedTurn = HashToUnitFloat(
                DeriveVolumeShapeSeed(seed ^ GoldenRockYawSalt, 0));
            auto referenceTurn = HashToUnitFloat(
                DeriveVolumeShapeSeed(
                    Defaults::GoldenRockSeed ^ GoldenRockYawSalt,
                    0));
            return Mathf::Repeat((seedTurn - referenceTurn) * 360.f, 360.f);
        }
        static std::vector<Spec> AddFractureCuts(
            const std::vector<Spec>& stone,
            int seed,
            Vector3 overallSize,
            Profile silhouetteProfile,
            float majorFractures)
        {
            auto fractureCount = Defaults::CalculateFractureCount(
                majorFractures);
            if (fractureCount == 0) return stone;

            auto result = std::vector<Spec>{};
            result=stone;
            auto random = Random(seed ^ int32_t(0x6A09E667u));
            auto horizontalSize = Mathf::Max(
                0.05f,
                Mathf::Min(overallSize.x, overallSize.z));
            auto height = Mathf::Max(0.05f, overallSize.y);
            if (height / horizontalSize > 4.f) return stone;
            auto thickness = Mathf::Max(
                0.011f,
                horizontalSize * Mathf::Lerp(0.028f, 0.06f, Mathf::Clamp01(majorFractures)));

            for (auto index = 0; index < fractureCount; index++)
            {
                auto heading = NextRange(random, 0.f, 360.f);
                auto radians = heading * Mathf::Deg2Rad;
                auto outward = Vector3{Mathf::Sin(radians), 0.f, Mathf::Cos(radians)};
                auto topCut = index == fractureCount - 1
                    && (fractureCount >= 3
                        || silhouetteProfile == Profile::BrokenSlab);
                Vector3 position;
                Vector3 scale;
                Quaternion rotation;
                if (topCut)
                {
                    position = outward * NextRange(
                        random,
                        -horizontalSize * 0.08f,
                        horizontalSize * 0.08f);
                    position.y = height * NextRange(random, 0.83f, 0.91f);
                    scale = Vector3{
                        thickness * NextRange(random, 0.78f, 1.18f),
                        height * NextRange(random, 0.28f, 0.42f),
                        horizontalSize * NextRange(random, 0.46f, 0.68f)};
                    rotation = Quaternion::Euler(Vector3{
                        NextRange(random, -8.f, 8.f),
                        heading,
                        NextRange(random, -12.f, 12.f)});
                }
                else
                {
                    position = outward * horizontalSize * NextRange(random, 0.31f, 0.38f);
                    position.y = height * NextRange(random, 0.44f, 0.58f);
                    scale = Vector3{
                        thickness * NextRange(random, 0.82f, 1.22f),
                        height * NextRange(random, 0.58f, 0.88f),
                        horizontalSize * NextRange(random, 0.42f, 0.56f)};
                    rotation = Quaternion::Euler(Vector3{
                        NextRange(random, -7.f, 7.f),
                        heading,
                        NextRange(random, -20.f, 20.f)});
                }

                result.push_back(Spec{
                    position,
                    rotation,
                    scale,
                    Role::Detail,
                    Shape::FractureCut,
                    Operation::Subtractive});
            }

            return result;
        }
        static Bounds CalculateBounds(const std::vector<Spec>& plan)
        {
            if (plan.size() == 0) return Bounds{Vector3::zero, Vector3::zero};

            auto firstIndex = -1;
            for (auto index = 0; index < plan.size(); index++)
            {
                if (plan[index].Operation != Operation::Additive) continue;
                firstIndex = index;
                break;
            }
            if (firstIndex < 0) return Bounds{Vector3::zero, Vector3::zero};

            auto first = plan[firstIndex];
            auto firstExtents = GetRotatedExtents(first.LocalRotation, first.LocalScale);
            auto min = first.LocalPosition - firstExtents;
            auto max = first.LocalPosition + firstExtents;
            for (auto index = firstIndex + 1; index < plan.size(); index++)
            {
                auto spec = plan[index];
                if (spec.Operation != Operation::Additive) continue;
                auto extents = GetRotatedExtents(spec.LocalRotation, spec.LocalScale);
                min = Vector3::Min(min, spec.LocalPosition - extents);
                max = Vector3::Max(max, spec.LocalPosition + extents);
            }

            auto bounds = Bounds{};
            bounds.SetMinMax(min, max);
            return bounds;
        }
        static std::vector<Spec> FitPlanToSizeAndGround(
            const std::vector<Spec>& raw,
            Vector3 overallSize)
        {
            auto rawBounds = CalculateBounds(raw);
            auto fitted = std::vector<Spec>{};
            auto independentWidthAndHeight = Mathf::Abs(overallSize.x - overallSize.z) <= 0.001f;
            for (const auto& spec : raw)
            {
                fitted.push_back(Spec{
                    spec.LocalPosition - rawBounds.center(),
                    independentWidthAndHeight
                        ? KeepYawOnly(spec.LocalRotation)
                        : spec.LocalRotation,
                    spec.LocalScale,
                    spec.Role,
                    spec.SourceShape,
                    spec.Operation});
            }

            if (independentWidthAndHeight)
            {
                // Upright sources allow one exact horizontal/vertical affine scale without
                // shearing their editable transforms or weakening their overlaps.
                auto bounds = CalculateBounds(fitted);
                ApplyAxisCorrection(fitted, Vector3{
                    overallSize.x / Mathf::Max(bounds.size().x, 0.001f),
                    overallSize.y / Mathf::Max(bounds.size().y, 0.001f),
                    overallSize.z / Mathf::Max(bounds.size().z, 0.001f)});
            }
            else
            {
                // Compatibility path for earlier callers that supplied three unrelated axes.
                // Uniform fitting preserves their previous connected-volume behavior.
                auto size = rawBounds.size();
                auto fit = Mathf::Min(
                    overallSize.x / Mathf::Max(size.x, 0.001f),
                    overallSize.y / Mathf::Max(size.y, 0.001f),
                    overallSize.z / Mathf::Max(size.z, 0.001f));
                ApplyAxisCorrection(fitted, Vector3::one * fit);
            }

            auto fittedMinimumY = CalculateBounds(fitted).min.y;
            for (auto index = 0; index < fitted.size(); index++)
            {
                auto spec = fitted[index];
                fitted[index] = Spec{
                    spec.LocalPosition + Vector3::up * -fittedMinimumY,
                    spec.LocalRotation,
                    spec.LocalScale,
                    spec.Role,
                    spec.SourceShape,
                    spec.Operation};
            }
            return fitted;
        }
        static void ApplyAxisCorrection(
            std::vector<Spec>& plan,
            Vector3 correction)
        {
            for (auto index = 0; index < plan.size(); index++)
            {
                auto spec = plan[index];
                plan[index] = Spec{
                    Vector3::Scale(spec.LocalPosition, correction),
                    spec.LocalRotation,
                    Vector3::Scale(spec.LocalScale, correction),
                    spec.Role,
                    spec.SourceShape,
                    spec.Operation};
            }
        }
        static Quaternion KeepYawOnly(Quaternion rotation)
        {
            auto forward = Vector3::ProjectOnPlane(rotation * Vector3::forward, Vector3::up);
            if (forward.sqrMagnitude() <= 0.000001f) forward = Vector3::forward;
            return Quaternion::LookRotation(forward.normalized(), Vector3::up);
        }
        static Shape ChooseSourceShape(
            int seed,
            int index,
            Role role,
            float asymmetry,
            Profile silhouetteProfile)
        {
            if (role == Role::Core)
                return GetProfileCoreShape(silhouetteProfile);

            switch (silhouetteProfile)
            {
                case Profile::Slab:
                    return index % 3 == 2
                        ? Shape::WeatheredBlock
                        : Shape::Wedge;
                case Profile::AngularChunk:
                    return index % 2 == 0
                        ? Shape::Wedge
                        : Shape::TaperedStone;
                case Profile::SplitLobe:
                    if (index == 1) return Shape::WeatheredBlock;
                    return index % 2 == 0
                        ? Shape::TaperedStone
                        : Shape::Wedge;
                case Profile::Shard:
                    return role == Role::Support
                        ? Shape::TaperedStone
                        : Shape::Wedge;
                case Profile::FracturedBoulder:
                    return index % 3 == 0
                        ? Shape::WeatheredBlock
                        : index % 2 == 0
                            ? Shape::Wedge
                            : Shape::TaperedStone;
                case Profile::BlockyMonolith:
                    return index % 3 == 2
                        ? Shape::Wedge
                        : Shape::WeatheredBlock;
                case Profile::BrokenSlab:
                    return index % 3 == 1
                        ? Shape::WeatheredBlock
                        : Shape::Wedge;
            }

            auto shapeSeed = DeriveVolumeShapeSeed(seed ^ int32_t(0x5F356495u), index);
            auto roll = HashToUnitFloat(shapeSeed);
            auto wedgeChance = role == Role::Support
                ? Mathf::Lerp(0.3f, 0.44f, asymmetry)
                : Mathf::Lerp(0.42f, 0.56f, asymmetry);
            auto taperedChance = role == Role::Support
                ? Mathf::Lerp(0.12f, 0.2f, asymmetry)
                : Mathf::Lerp(0.18f, 0.26f, asymmetry);
            if (roll < wedgeChance) return Shape::Wedge;
            if (roll < wedgeChance + taperedChance)
                return Shape::TaperedStone;
            return Shape::WeatheredBlock;
        }
        static Profile ResolveSilhouetteProfile(
            int seed,
            Profile requested)
        {
            if (requested != Profile::Auto) return requested;

            auto profileSeed = DeriveVolumeShapeSeed(
                seed ^ int32_t(0x2C1B3C6Du),
                0);
            return (Profile)(1
                + uint32_t(profileSeed) % 5u);
        }
        static Profile ResolveDarkDesertSilhouetteProfile(
            int seed)
        {
            auto profileSeed = DeriveVolumeShapeSeed(
                seed ^ int32_t(0x510E527Fu),
                0);
            return (Profile)(1
                + uint32_t(profileSeed) % 8u);
        }
        static float GetProfileVerticality(
            float verticality,
            Profile silhouetteProfile)
        {
            switch (silhouetteProfile)
            {
                case Profile::Slab:
                    return verticality * 0.28f;
                case Profile::AngularChunk:
                    return Mathf::Lerp(verticality, 0.58f, 0.25f);
                case Profile::SplitLobe:
                    return Mathf::Lerp(verticality, 0.3f, 0.5f);
                case Profile::Shard:
                    return Mathf::Lerp(0.35f, 1.f, verticality);
                case Profile::FracturedBoulder:
                    return Mathf::Lerp(verticality, 0.48f, 0.32f);
                case Profile::BlockyMonolith:
                    return Mathf::Lerp(0.62f, 1.f, verticality);
                case Profile::BrokenSlab:
                    return verticality * 0.22f;
                default:
                    return verticality;
            }
        }
        static Vector3 CreateProfileCoreScale(
            Random& random,
            float verticality,
            float asymmetry,
            Profile silhouetteProfile)
        {
            Vector3 center;
            switch (silhouetteProfile)
            {
                case Profile::Slab:
                    center = Vector3{1.85f, Mathf::Lerp(0.48f, 0.78f, verticality), 1.16f};
                    break;
                case Profile::AngularChunk:
                    center = Vector3{1.18f, Mathf::Lerp(0.82f, 1.42f, verticality), 1.02f};
                    break;
                case Profile::SplitLobe:
                    center = Vector3{1.12f, Mathf::Lerp(0.72f, 1.22f, verticality), 1.04f};
                    break;
                case Profile::Shard:
                    center = Vector3{0.86f, Mathf::Lerp(0.72f, 1.95f, verticality), 0.76f};
                    break;
                case Profile::FracturedBoulder:
                    center = Vector3{1.5f, Mathf::Lerp(0.9f, 1.48f, verticality), 1.34f};
                    break;
                case Profile::BlockyMonolith:
                    center = Vector3{1.04f, Mathf::Lerp(1.38f, 2.12f, verticality), 0.96f};
                    break;
                case Profile::BrokenSlab:
                    center = Vector3{1.92f, Mathf::Lerp(0.42f, 0.68f, verticality), 1.24f};
                    break;
                default:
                    center = Vector3{1.42f, Mathf::Lerp(0.72f, 1.58f, verticality), 1.28f};
                    break;
            }

            return Vector3{
                VariedFraction(random, center.x, center.x * 0.1f, asymmetry),
                VariedFraction(random, center.y, center.y * 0.08f, asymmetry),
                VariedFraction(random, center.z, center.z * 0.1f, asymmetry)};
        }
        static Shape GetProfileCoreShape(
            Profile silhouetteProfile)
        {
            switch (silhouetteProfile)
            {
                case Profile::Slab:
                    return Shape::Wedge;
                case Profile::AngularChunk:
                case Profile::Shard:
                    return Shape::TaperedStone;
                case Profile::BrokenSlab:
                    return Shape::Wedge;
                case Profile::FracturedBoulder:
                case Profile::BlockyMonolith:
                    return Shape::WeatheredBlock;
                default:
                    return Shape::WeatheredBlock;
            }
        }
        static Vector3 ApplyProfileScale(
            Random& random,
            Vector3 childScale,
            Vector3 coreScale,
            int index,
            Role role,
            Profile silhouetteProfile)
        {
            auto support = role == Role::Support;
            switch (silhouetteProfile)
            {
                case Profile::Slab:
                    return Vector3::Scale(childScale, Vector3{
                        support ? NextRange(random, 1.05f, 1.35f) : 0.9f,
                        support ? 0.62f : 0.48f,
                        support ? NextRange(random, 0.82f, 1.18f) : 0.72f});
                case Profile::AngularChunk:
                    return Vector3::Scale(childScale, Vector3{
                        support ? 1.2f : 0.88f,
                        support ? 1.08f : 0.9f,
                        support ? 1.08f : 0.78f});
                case Profile::SplitLobe:
                    if (index == 1)
                    {
                        return Vector3::Scale(coreScale, Vector3{
                            NextRange(random, 0.82f, 1.02f),
                            NextRange(random, 0.72f, 0.94f),
                            NextRange(random, 0.8f, 1.f)});
                    }
                    return Vector3::Scale(childScale, support
                        ? Vector3{1.15f, 0.92f, 1.08f}
                        : Vector3{0.9f, 0.82f, 0.84f});
                case Profile::Shard:
                    return Vector3::Scale(childScale, Vector3{
                        support ? 0.9f : 0.78f,
                        support ? 1.28f : 1.04f,
                        support ? 0.86f : 0.74f});
                case Profile::FracturedBoulder:
                    return Vector3::Scale(childScale, support
                        ? Vector3{1.16f, 1.02f, 1.12f}
                        : Vector3{0.82f, 0.78f, 0.8f});
                case Profile::BlockyMonolith:
                    return Vector3::Scale(childScale, Vector3{
                        support ? 0.92f : 0.72f,
                        support ? 1.3f : 0.92f,
                        support ? 0.88f : 0.7f});
                case Profile::BrokenSlab:
                    return Vector3::Scale(childScale, Vector3{
                        support ? NextRange(random, 1.16f, 1.48f) : 0.84f,
                        support ? 0.52f : 0.4f,
                        support ? NextRange(random, 0.9f, 1.24f) : 0.7f});
                default:
                    return Vector3::Scale(childScale, support
                        ? Vector3{1.08f, 1.f, 1.04f}
                        : Vector3::one);
            }
        }
        static float GetProfileOverlap(
            float overlap,
            Profile silhouetteProfile)
        {
            switch (silhouetteProfile)
            {
                case Profile::Slab:
                    return Mathf::Lerp(overlap, 0.82f, 0.75f);
                case Profile::AngularChunk:
                    return Mathf::Lerp(overlap, 0.58f, 0.35f);
                case Profile::SplitLobe:
                    return Mathf::Lerp(overlap, 0.52f, 0.35f);
                case Profile::Shard:
                    return Mathf::Lerp(overlap, 0.68f, 0.55f);
                case Profile::FracturedBoulder:
                    return Mathf::Lerp(overlap, 0.56f, 0.4f);
                case Profile::BlockyMonolith:
                    return Mathf::Lerp(overlap, 0.64f, 0.42f);
                case Profile::BrokenSlab:
                    return Mathf::Lerp(overlap, 0.76f, 0.62f);
                default:
                    return overlap;
            }
        }
        static float GetShapeOverlapAllowance(Shape sourceShape)
        {
            switch (sourceShape)
            {
                case Shape::Wedge:
                    return 0.09f;
                case Shape::TaperedStone:
                    return 0.07f;
                default:
                    return 0.f;
            }
        }
        static Quaternion OrientSourceShapeTowardGrowth(
            Quaternion rotation,
            Vector3 growthDirection,
            Shape sourceShape)
        {
            if (sourceShape != Shape::Wedge) return rotation;

            auto localUp = rotation * Vector3::up;
            auto currentSlopeHeading = Vector3::ProjectOnPlane(
                rotation * Vector3::right,
                localUp);
            auto desiredSlopeHeading = Vector3::ProjectOnPlane(
                growthDirection,
                localUp);
            if (currentSlopeHeading.sqrMagnitude() <= 0.000001f
                || desiredSlopeHeading.sqrMagnitude() <= 0.000001f)
            {
                return rotation;
            }

            return Quaternion::FromToRotation(
                       currentSlopeHeading.normalized(),
                       desiredSlopeHeading.normalized())
                   * rotation;
        }
        static float HashToUnitFloat(int seed)
        {
            return (uint32_t(seed) & 0x00FFFFFFu) / 16777215.f;
        }
        static int ChooseParentIndex(
            Random& random,
            int index,
            int supportCount,
            float verticality,
            float asymmetry,
            Role role)
        {
            if (index == 1) return 0;

            if (role == Role::Detail)
            {
                return random.Next(0, supportCount + 1);
            }

            auto continueSpineChance = Mathf::Lerp(0.18f, 0.82f, verticality);
            continueSpineChance = Mathf::Lerp(
                continueSpineChance,
                0.92f,
                Mathf::SmoothStep(0.f, 1.f, asymmetry) * 0.72f);
            if (random.NextDouble() < continueSpineChance) return index - 1;
            return random.NextDouble() < 0.72 ? 0 : random.Next(1, index);
        }
        static Vector3 CreateRoleScale(
            Random& random,
            Vector3 coreScale,
            Role role,
            int roleIndex,
            int roleCount,
            float verticality,
            float asymmetry)
        {
            auto progress = roleCount <= 1 ? 0.f : roleIndex / (float)(roleCount - 1);
            auto ratio = role == Role::Support
                ? Mathf::Lerp(0.78f, 0.64f, progress)
                : Mathf::Lerp(0.68f, 0.58f, progress);
            auto horizontalVariation = Mathf::Lerp(0.025f, 0.28f, asymmetry);
            auto verticalVariation = Mathf::Lerp(0.02f, 0.22f, asymmetry);
            auto verticalRatio = ratio * (role == Role::Support
                ? Mathf::Lerp(0.78f, 1.1f, verticality)
                : Mathf::Lerp(0.7f, 0.92f, verticality));
            return Vector3{
                coreScale.x * VariedFraction(random, ratio, horizontalVariation, 1.f),
                coreScale.y * VariedFraction(random, verticalRatio, verticalVariation, 1.f),
                coreScale.z * VariedFraction(random, ratio, horizontalVariation, 1.f)};
        }
        static Vector3 CreateGrowthDirection(
            Random& random,
            Vector3 preferredDirection,
            int index,
            float verticality,
            float asymmetry,
            Role role)
        {
            const float goldenAngle = 2.39996323f;
            auto preferredAngle = Mathf::Atan2(preferredDirection.z, preferredDirection.x);
            auto balancedAngle = preferredAngle + index * goldenAngle;
            auto balancedDirection = Vector3{
                Mathf::Cos(balancedAngle),
                0.f,
                Mathf::Sin(balancedAngle)};
            auto randomAngle = NextRange(random, 0.f, Mathf::PI * 2.f);
            auto randomDirection = Vector3{
                Mathf::Cos(randomAngle),
                0.f,
                Mathf::Sin(randomAngle)};
            auto irregularity = Mathf::SmoothStep(0.f, 1.f, asymmetry) * 0.35f;
            auto horizontal = Vector3::Slerp(balancedDirection, randomDirection, irregularity);
            auto directionalBias = Mathf::SmoothStep(0.f, 1.f, asymmetry) * 0.88f;
            horizontal = Vector3::Slerp(horizontal, preferredDirection, directionalBias).normalized();

            auto detail = role == Role::Detail;
            auto minimumY = detail
                ? Mathf::Lerp(-0.14f, 0.06f, verticality)
                : Mathf::Lerp(-0.08f, 0.38f, verticality);
            auto maximumY = detail
                ? Mathf::Lerp(0.18f, 0.62f, verticality)
                : Mathf::Lerp(0.14f, 1.12f, verticality);
            auto horizontalWeight = detail
                ? Mathf::Lerp(1.f, 0.58f, verticality)
                : Mathf::Lerp(1.f, 0.34f, verticality);
            return (horizontal * horizontalWeight
                + Vector3::up * NextRange(random, minimumY, maximumY)).normalized();
        }
        static Quaternion CreateSharedRotation(Random& random, float asymmetry)
        {
            auto baseTilt = Mathf::Lerp(1.5f, 14.f, asymmetry);
            return Quaternion::Euler(Vector3{
                NextRange(random, -baseTilt, baseTilt),
                NextRange(random, 0.f, 360.f),
                NextRange(random, -baseTilt, baseTilt)});
        }
        static Quaternion CreateRoleRotation(
            Random& random,
            Quaternion sharedRotation,
            float asymmetry,
            Role role)
        {
            auto roleVariation = role == Role::Core
                ? 0.45f
                : role == Role::Support ? 0.75f : 1.f;
            auto maximumTilt = Mathf::Lerp(2.f, 26.f, asymmetry) * roleVariation;
            auto maximumYaw = Mathf::Lerp(4.f, 42.f, asymmetry) * roleVariation;
            return sharedRotation * Quaternion::Euler(Vector3{
                NextRange(random, -maximumTilt, maximumTilt),
                NextRange(random, -maximumYaw, maximumYaw),
                NextRange(random, -maximumTilt, maximumTilt)});
        }
        static float DirectionalRadius(
            Vector3 scale,
            Quaternion rotation,
            Vector3 worldDirection)
        {
            auto direction = Quaternion::Inverse(rotation) * worldDirection.normalized();
            auto half = scale * 0.5f;
            auto radius = std::numeric_limits<float>::infinity();
            if (Mathf::Abs(direction.x) > 0.0001f)
            {
                radius = Mathf::Min(radius, half.x / Mathf::Abs(direction.x));
            }
            if (Mathf::Abs(direction.y) > 0.0001f)
            {
                radius = Mathf::Min(radius, half.y / Mathf::Abs(direction.y));
            }
            if (Mathf::Abs(direction.z) > 0.0001f)
            {
                radius = Mathf::Min(radius, half.z / Mathf::Abs(direction.z));
            }
            return radius;
        }
        static Vector3 GetRotatedExtents(Quaternion rotation, Vector3 scale)
        {
            auto half = scale * 0.5f;
            auto right = rotation * Vector3::right;
            auto up = rotation * Vector3::up;
            auto forward = rotation * Vector3::forward;
            return Vector3{
                Mathf::Abs(right.x) * half.x + Mathf::Abs(up.x) * half.y + Mathf::Abs(forward.x) * half.z,
                Mathf::Abs(right.y) * half.x + Mathf::Abs(up.y) * half.y + Mathf::Abs(forward.y) * half.z,
                Mathf::Abs(right.z) * half.x + Mathf::Abs(up.z) * half.y + Mathf::Abs(forward.z) * half.z};
        }
        static float VariedFraction(
            Random& random,
            float center,
            float halfRange,
            float amount)
        {
            return Mathf::Max(0.15f, center + NextRange(random, -halfRange, halfRange) * amount);
        }
        static float NextRange(Random& random, float minimum, float maximum)
        {
            return Mathf::Lerp(minimum, maximum, (float)random.NextDouble());
        }
};
}
std::array<float,2> singleRockDimensions(uint32_t seed){auto d=single_rock::Planner::CreateBellCurvedStandaloneDimensions(int32_t(seed));return {d.x,d.y};}
std::vector<RockVolume> planSingleRock(const RockRecipe& r){
    using namespace single_rock;
    auto width=r.single.width,length=r.single.bodyLength;
    if(r.single.randomDimensions){auto d=singleRockDimensions(uint32_t(r.seed));width=d[0];length=d[1];}
    auto profile=Profile(r.profile);if(profile==Profile::Auto&&r.single.darkAutoProfile)profile=Planner::ResolveDarkDesertSilhouetteProfile(int32_t(r.seed));
    const int count=std::clamp(Mathf::RoundToInt(std::sqrt(width*width*.55f+length*length*.45f)*.22f)+1,2,4);
    const float verticality=Mathf::InverseLerp(.35f,2.5f,length/width);
    auto plan=Planner::ApplyGoldenRockRestingPose(Planner::CreatePlan(int32_t(r.seed),count,{width,length,width},verticality,r.asymmetry*.001f,r.compaction*.001f,profile,r.fractures*.001f),int32_t(r.seed));
    std::vector<RockVolume> result;
    for(size_t i=0;i<plan.size();++i){const auto& s=plan[i];RockVolume v;v.id=uint32_t(i);v.center={s.LocalPosition.x,s.LocalPosition.y,s.LocalPosition.z};v.halfSize={s.LocalScale.x*.5f,s.LocalScale.y*.5f,s.LocalScale.z*.5f};v.orientation={s.LocalRotation.x,s.LocalRotation.y,s.LocalRotation.z,s.LocalRotation.w};v.shapeSeed=uint32_t(Planner::DeriveVolumeShapeSeed(int32_t(r.seed),int(i)));v.primitive=s.SourceShape==Shape::Wedge?2:(s.SourceShape==Shape::TaperedStone?1:(s.SourceShape==Shape::FractureCut?3:0));v.subtractive=s.Operation==Operation::Subtractive;result.push_back(v);}
    return result;
}
}
