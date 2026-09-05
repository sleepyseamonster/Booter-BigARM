using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator.GroundedGeology;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator.GroundedGeology
{
    internal sealed class GroundedGeologyFixtureSnapshot
    {
        public GroundedGeologyFixtureSnapshot(
            GameObject source,
            GroundedGeologyRecipe recipe,
            WorldCoordinateAddress ownerAddress,
            WorldFeatureId sourceSignatureBefore,
            WorldFeatureId sourceSignatureAfter,
            Vector3 sourceRootScale,
            float physicalBakeFactor,
            string warning)
        {
            Source = source;
            Recipe = recipe;
            OwnerAddress = ownerAddress;
            SourceSignatureBefore = sourceSignatureBefore;
            SourceSignatureAfter = sourceSignatureAfter;
            SourceRootScale = sourceRootScale;
            PhysicalBakeFactor = physicalBakeFactor;
            Warning = warning ?? string.Empty;
        }

        public GameObject Source { get; }
        public GroundedGeologyRecipe Recipe { get; }
        public WorldCoordinateAddress OwnerAddress { get; }
        public WorldFeatureId SourceSignatureBefore { get; }
        public WorldFeatureId SourceSignatureAfter { get; }
        public Vector3 SourceRootScale { get; }
        public float PhysicalBakeFactor { get; }
        public string Warning { get; }
        public bool SourceWasPreserved => SourceSignatureBefore.Equals(SourceSignatureAfter);
    }

    /// <summary>
    /// Read-only bridge from the existing editor fixtures into the runtime-safe recipe contract.
    /// It never normalizes, reparents, rebuilds, or serializes the source hierarchy.
    /// </summary>
    internal static class ExistingWorkbenchFixtureAdapter
    {
        internal const float ApprovedIndividualRockPhysicalBake = 0.1f;
        private const double MinimumDimensionMeters = 0.000001d;

        public static bool TryCapture(
            GameObject source,
            int seed,
            bool seedLocked,
            out GroundedGeologyFixtureSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = null;
            if (source == null)
            {
                error = "Select an existing Rock Workbench or Formation Workbench fixture.";
                return false;
            }

            var formation = source.GetComponent<TopDown3DRockWorkbenchFormationAuthoring>();
            var rock = source.GetComponent<TopDown3DRockWorkbenchAuthoring>();
            if (formation == null && rock == null)
            {
                error = "The selected root does not own a Rock Workbench or Formation Workbench component.";
                return false;
            }

            var sourceSignatureBefore = CaptureSourceHierarchySignature(source);
            var fixtureKind = rock != null
                ? GroundedGeologyFixtureKind.TemporaryExistingRock
                : GroundedGeologyFixtureKind.TemporaryExistingFormation;
            var bakeFactor = rock != null ? ApprovedIndividualRockPhysicalBake : 1f;
            var elements = CaptureElements(source.transform, bakeFactor, rock, formation);
            var bounds = CalculateBounds(elements);
            var sourceSignatureAfter = CaptureSourceHierarchySignature(source);
            if (!sourceSignatureBefore.Equals(sourceSignatureAfter))
            {
                error = "Fixture capture changed the source hierarchy; the snapshot was rejected.";
                return false;
            }

            var stableId = "temporary.existing-workbench."
                + (rock != null ? "rock." : "formation.")
                + sourceSignatureBefore;
            var anchor = source.transform.position;
            var rotation = source.transform.rotation;
            var sampleSpacing = rock != null
                ? rock.VoxelSize * bakeFactor
                : formation.FusedVoxelSize;
            var blendWidth = rock != null
                ? rock.FusionSmoothness * bakeFactor
                : formation.FusedJoinSoftness;
            var seamWidth = rock != null ? 0d : formation.GeologicalSeamWidth;
            var materialWavelength = rock != null
                ? rock.GeologyScale * bakeFactor
                : Math.Max(formation.FractureSpacing, formation.GeologicalSeamWidth);
            var recipe = new GroundedGeologyRecipe(
                stableId,
                GroundedGeologyRecipe.CurrentSchemaVersion,
                fixtureKind,
                true,
                new AbsoluteWorldPosition(anchor.x, anchor.y, anchor.z),
                new GroundedGeologyRotation(rotation.x, rotation.y, rotation.z, rotation.w),
                bounds,
                GroundedGeologyAlgorithmVersions.Initial,
                unchecked((ulong)(uint)seed),
                seedLocked,
                sampleSpacing,
                blendWidth,
                seamWidth,
                materialWavelength,
                sourceSignatureBefore,
                elements);
            var ownerAddress = new WorldCoordinateAddress(
                "grounded-geology.fixture-local",
                1,
                stableId);
            var warning = ApproximatelyUnit(source.transform.localScale)
                ? string.Empty
                : rock != null
                    ? "Source root is non-unit. Reference A uses the approved 0.1 physical scale and target B uses a unit root; the source remains untouched."
                    : "Formation source root is non-unit. The adapter reads its authored meter values without normalizing the source.";

            snapshot = new GroundedGeologyFixtureSnapshot(
                source,
                recipe,
                ownerAddress,
                sourceSignatureBefore,
                sourceSignatureAfter,
                source.transform.localScale,
                bakeFactor,
                warning);
            return true;
        }

        internal static WorldFeatureId CaptureSourceHierarchySignature(GameObject source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var hash = new WorldStableHashBuilder("grounded-geology-fixture-source-v1");
            hash.Append(source.scene.path ?? string.Empty);
            var transforms = source.GetComponentsInChildren<Transform>(true);
            Array.Sort(transforms, (left, right) => string.CompareOrdinal(
                GetRelativePath(source.transform, left),
                GetRelativePath(source.transform, right)));
            hash.Append(transforms.Length);
            for (var i = 0; i < transforms.Length; i++)
            {
                var item = transforms[i];
                hash.Append(GetRelativePath(source.transform, item));
                hash.Append((byte)(item.gameObject.activeSelf ? 1 : 0));
                AppendVector(item.localPosition, ref hash);
                AppendQuaternion(item.localRotation, ref hash);
                AppendVector(item.localScale, ref hash);
                var node = item.GetComponent<TopDown3DRockVolumeNode>();
                hash.Append((byte)(node != null ? 1 : 0));
                if (node != null)
                {
                    hash.Append((byte)(node.ContributesToRock ? 1 : 0));
                    hash.Append((int)node.SourceShape);
                    hash.Append((int)node.Operation);
                    hash.Append(node.ShapeSeed);
                }
            }

            var rock = source.GetComponent<TopDown3DRockWorkbenchAuthoring>();
            hash.Append((byte)(rock != null ? 1 : 0));
            if (rock != null)
            {
                hash.Append(rock.GenerationSeed);
                AppendFloat(rock.VoxelSize, ref hash);
                AppendFloat(rock.FusionSmoothness, ref hash);
                AppendFloat(rock.SurfaceRelaxation, ref hash);
                AppendFloat(rock.GeologyScale, ref hash);
                AppendFloat(rock.GeneratedWidth, ref hash);
                AppendFloat(rock.GeneratedHeight, ref hash);
            }

            var formation = source.GetComponent<TopDown3DRockWorkbenchFormationAuthoring>();
            hash.Append((byte)(formation != null ? 1 : 0));
            if (formation != null)
            {
                hash.Append(formation.FormationSeed);
                hash.Append((int)formation.FormationArchetype);
                hash.Append((int)formation.JoinStyle);
                AppendFloat(formation.GeneratedWidth, ref hash);
                AppendFloat(formation.GeneratedHeight, ref hash);
                AppendFloat(formation.MemberVoxelSize, ref hash);
                AppendFloat(formation.FusedVoxelSize, ref hash);
                AppendFloat(formation.FusedJoinSoftness, ref hash);
                AppendFloat(formation.GeologicalSeamWidth, ref hash);
                AppendFloat(formation.FractureSpacing, ref hash);
            }

            hash.Finish128(out var high, out var low);
            return new WorldFeatureId(high, low);
        }

        private static GroundedGeologyStructuralElement[] CaptureElements(
            Transform root,
            float bakeFactor,
            TopDown3DRockWorkbenchAuthoring rock,
            TopDown3DRockWorkbenchFormationAuthoring formation)
        {
            var nodes = root.GetComponentsInChildren<TopDown3DRockVolumeNode>(true);
            Array.Sort(nodes, (left, right) => string.CompareOrdinal(
                GetRelativePath(root, left.transform),
                GetRelativePath(root, right.transform)));
            var elements = new List<GroundedGeologyStructuralElement>(Math.Max(1, nodes.Length));
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                var rootLocal = root.worldToLocalMatrix * node.transform.localToWorldMatrix;
                var localPosition = rootLocal.MultiplyPoint3x4(Vector3.zero) * bakeFactor;
                var localSize = new Vector3(
                    rootLocal.MultiplyVector(Vector3.right).magnitude,
                    rootLocal.MultiplyVector(Vector3.up).magnitude,
                    rootLocal.MultiplyVector(Vector3.forward).magnitude) * bakeFactor;
                localSize.x = Mathf.Max(localSize.x, (float)MinimumDimensionMeters);
                localSize.y = Mathf.Max(localSize.y, (float)MinimumDimensionMeters);
                localSize.z = Mathf.Max(localSize.z, (float)MinimumDimensionMeters);
                var localRotation = Quaternion.Inverse(root.rotation) * node.transform.rotation;
                elements.Add(new GroundedGeologyStructuralElement(
                    GetRelativePath(root, node.transform),
                    node.Operation == TopDown3DRockVolumeOperation.Subtractive
                        ? GroundedGeologyElementKind.SubtractiveVoid
                        : GroundedGeologyElementKind.AdditiveMass,
                    ToMeters(localPosition),
                    ToRotation(localRotation),
                    ToMeters(localSize),
                    (int)node.SourceShape,
                    node.ShapeSeed,
                    node.ContributesToRock && node.gameObject.activeInHierarchy));
            }

            if (elements.Count == 0)
            {
                var filter = root.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    var meshBounds = filter.sharedMesh.bounds;
                    elements.Add(new GroundedGeologyStructuralElement(
                        "generated-reference-surface",
                        GroundedGeologyElementKind.ReferenceSurface,
                        ToMeters(meshBounds.center * bakeFactor),
                        GroundedGeologyRotation.Identity,
                        ToMeters(ClampPositive(meshBounds.size * bakeFactor)),
                        0,
                        rock != null ? rock.GenerationSeed : formation.FormationSeed,
                        true));
                }
                else
                {
                    var fallbackSize = rock != null
                        ? new Vector3(rock.GeneratedWidth, rock.GeneratedHeight, rock.GeneratedWidth) * bakeFactor
                        : new Vector3(formation.GeneratedWidth, formation.GeneratedHeight, formation.GeneratedWidth);
                    elements.Add(new GroundedGeologyStructuralElement(
                        "authoring-dimension-reference",
                        GroundedGeologyElementKind.ReferenceSurface,
                        new GroundedGeologyVector3Meters(0d, fallbackSize.y * 0.5d, 0d),
                        GroundedGeologyRotation.Identity,
                        ToMeters(ClampPositive(fallbackSize)),
                        0,
                        rock != null ? rock.GenerationSeed : formation.FormationSeed,
                        true));
                }
            }

            return elements.ToArray();
        }

        private static GroundedGeologyBoundsMeters CalculateBounds(
            IReadOnlyList<GroundedGeologyStructuralElement> elements)
        {
            var hasBounds = false;
            var minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (var i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (!element.Contributes || element.Kind == GroundedGeologyElementKind.SubtractiveVoid)
                    continue;
                var center = ToUnity(element.LocalPositionMeters);
                var size = ToUnity(element.SizeMeters);
                var rotation = ToUnity(element.LocalRotation);
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var corner = center + rotation * Vector3.Scale(
                                size * 0.5f,
                                new Vector3(x, y, z));
                            minimum = Vector3.Min(minimum, corner);
                            maximum = Vector3.Max(maximum, corner);
                            hasBounds = true;
                        }
                    }
                }
            }

            if (!hasBounds)
                throw new InvalidOperationException("Fixture has no contributing additive or reference surface.");
            var sizeMeters = ClampPositive(maximum - minimum);
            return new GroundedGeologyBoundsMeters(
                ToMeters((minimum + maximum) * 0.5f),
                ToMeters(sizeMeters));
        }

        private static string GetRelativePath(Transform root, Transform item)
        {
            if (item == root) return root.name;
            var names = new Stack<string>();
            var current = item;
            while (current != null && current != root)
            {
                names.Push(current.name + "[" + current.GetSiblingIndex() + "]");
                current = current.parent;
            }
            return root.name + "/" + string.Join("/", names.ToArray());
        }

        private static bool ApproximatelyUnit(Vector3 scale)
        {
            return Mathf.Abs(scale.x - 1f) <= 0.0001f
                && Mathf.Abs(scale.y - 1f) <= 0.0001f
                && Mathf.Abs(scale.z - 1f) <= 0.0001f;
        }

        private static GroundedGeologyVector3Meters ToMeters(Vector3 value)
        {
            return new GroundedGeologyVector3Meters(value.x, value.y, value.z);
        }

        private static GroundedGeologyRotation ToRotation(Quaternion value)
        {
            return new GroundedGeologyRotation(value.x, value.y, value.z, value.w);
        }

        private static Vector3 ToUnity(GroundedGeologyVector3Meters value)
        {
            return new Vector3((float)value.X, (float)value.Y, (float)value.Z);
        }

        private static Quaternion ToUnity(GroundedGeologyRotation value)
        {
            return new Quaternion((float)value.X, (float)value.Y, (float)value.Z, (float)value.W);
        }

        private static Vector3 ClampPositive(Vector3 value)
        {
            return new Vector3(
                Mathf.Max((float)MinimumDimensionMeters, Mathf.Abs(value.x)),
                Mathf.Max((float)MinimumDimensionMeters, Mathf.Abs(value.y)),
                Mathf.Max((float)MinimumDimensionMeters, Mathf.Abs(value.z)));
        }

        private static void AppendVector(Vector3 value, ref WorldStableHashBuilder hash)
        {
            AppendFloat(value.x, ref hash);
            AppendFloat(value.y, ref hash);
            AppendFloat(value.z, ref hash);
        }

        private static void AppendQuaternion(Quaternion value, ref WorldStableHashBuilder hash)
        {
            AppendFloat(value.x, ref hash);
            AppendFloat(value.y, ref hash);
            AppendFloat(value.z, ref hash);
            AppendFloat(value.w, ref hash);
        }

        private static void AppendFloat(float value, ref WorldStableHashBuilder hash)
        {
            hash.Append(BitConverter.SingleToInt32Bits(value == 0f ? 0f : value));
        }
    }
}
