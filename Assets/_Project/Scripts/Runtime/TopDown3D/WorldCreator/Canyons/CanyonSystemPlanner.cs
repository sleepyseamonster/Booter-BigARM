using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public sealed class CanyonSystemPlanner
    {
        public const string PlanKind = "canyon-system";
        private static readonly WorldSeedNamespace CanyonNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.canyon-system");

        private static readonly SideDescriptor[] Sides =
        {
            new SideDescriptor(CanyonBoundarySide.West, -1, 0),
            new SideDescriptor(CanyonBoundarySide.East, 1, 0),
            new SideDescriptor(CanyonBoundarySide.South, 0, -1),
            new SideDescriptor(CanyonBoundarySide.North, 0, 1)
        };

        private readonly IWorldCoordinateContextProvider contextProvider;
        private readonly CanyonPlannerProfile profile;

        public CanyonSystemPlanner(
            IWorldCoordinateContextProvider contextProvider,
            CanyonPlannerProfile profile)
        {
            this.contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            this.profile = profile;
        }

        public CanyonPlannerProfile Profile => profile;

        public bool TryBuild(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            CanyonSystemCellIndex cell,
            out CanyonSystemPlan plan,
            out string error)
        {
            plan = null;
            if (coordinateModel == null)
            {
                error = "A coordinate model is required to build a canyon plan.";
                return false;
            }

            if (!TryCreateCellInput(world, coordinateModel, cell, out var centerInput, out error))
            {
                return false;
            }

            var neighborInputs = new Dictionary<CanyonBoundarySide, CellInput>();
            for (var i = 0; i < Sides.Length; i++)
            {
                CanyonSystemCellIndex neighborCell;
                try
                {
                    neighborCell = cell.Offset(Sides[i].DeltaHorizontalA, Sides[i].DeltaHorizontalB);
                }
                catch (OverflowException)
                {
                    error = "Canyon planning cannot address a neighbor beyond the supported technical cell range.";
                    return false;
                }

                if (!TryCreateCellInput(world, coordinateModel, neighborCell, out var neighborInput, out error))
                {
                    return false;
                }

                if (neighborInput.Key.Equals(centerInput.Key))
                {
                    error = "The coordinate model cannot distinguish adjacent technical planning cells at this absolute scale.";
                    return false;
                }

                neighborInputs.Add(Sides[i].Side, neighborInput);
            }

            var junctionIds = new[]
            {
                CreateFeatureId(world, centerInput.Key, "node.junction:0"),
                CreateFeatureId(world, centerInput.Key, "node.junction:1")
            };
            var junctionDrafts = new[]
            {
                new LocalNodeDraft(
                    junctionIds[0],
                    BuildJunctionPosition(centerInput, 0),
                    BuildCrossSection(
                        centerInput.Context.Landscape.CanyonWidth,
                        centerInput.Context.Landscape.CanyonDepth,
                        Seed(centerInput, 1UL),
                        profile.BigArmClearWidth * 1.5f)),
                new LocalNodeDraft(
                    junctionIds[1],
                    BuildJunctionPosition(centerInput, 1),
                    BuildCrossSection(
                        centerInput.Context.Landscape.CanyonWidth * 0.9f,
                        centerInput.Context.Landscape.CanyonDepth * 0.92f,
                        Seed(centerInput, 2UL),
                        profile.BigArmClearWidth * 1.5f))
            };
            var nodes = new List<CanyonNodePlan>();
            var boundaries = new List<CanyonBoundaryPortReference>(4);
            var portIds = new Dictionary<CanyonBoundarySide, WorldFeatureId>();
            for (var i = 0; i < Sides.Length; i++)
            {
                var descriptor = Sides[i];
                var neighbor = neighborInputs[descriptor.Side];
                var boundary = new WorldPlanBoundaryKey(centerInput.Key, neighbor.Key);
                var portId = WorldFeatureId.Create(
                    world,
                    CanyonNamespace,
                    boundary.Owner.OwnerAddress,
                    "boundary-port:" + boundary.StableId);
                var portSection = BuildBoundaryCrossSection(centerInput, neighbor, boundary);
                var portPosition = BuildBoundaryPosition(cell, descriptor.Side, boundary);
                var localJunctionId = junctionIds[GetJunctionIndex(centerInput, descriptor.Side)];
                var neighborJunctionId = CreateFeatureId(
                    world,
                    neighbor.Key,
                    "node.junction:" + GetJunctionIndex(neighbor, Opposite(descriptor.Side)));
                var portRole = CanyonNodeRole.BoundaryPort | CanyonNodeRole.Spine;
                portRole |= ClassifyRole(portId, localJunctionId, neighborJunctionId);
                nodes.Add(new CanyonNodePlan(
                    portId,
                    boundary.Owner,
                    portPosition,
                    portSection,
                    portRole));
                boundaries.Add(new CanyonBoundaryPortReference(descriptor.Side, boundary, portId));
                portIds.Add(descriptor.Side, portId);
            }

            var activeLocalBranchCount = GetActiveLocalBranchCount(centerInput.Context.Landscape);
            var draftLocalNodes = new List<LocalNodeDraft>(activeLocalBranchCount);
            for (var branchIndex = 0; branchIndex < activeLocalBranchCount; branchIndex++)
            {
                var branchSeed = Seed(centerInput, (ulong)(100 + branchIndex));
                var branchId = CreateFeatureId(world, centerInput.Key, "node.local-branch:" + branchIndex);
                var branchPosition = BuildLocalBranchPosition(centerInput.Center, branchSeed, branchIndex);
                var widthParameter = centerInput.Context.Landscape.CanyonWidth * (0.35f + 0.2f * UnitFloat(branchSeed >> 11));
                var depthParameter = centerInput.Context.Landscape.CanyonDepth * (0.3f + 0.25f * UnitFloat(branchSeed >> 23));
                var branchSection = BuildCrossSection(
                    widthParameter,
                    depthParameter,
                    branchSeed,
                    profile.BooterClearWidth * 1.25f);
                draftLocalNodes.Add(new LocalNodeDraft(branchId, branchPosition, branchSection));
            }

            var segments = new List<CanyonSegmentPlan>(profile.MaximumSegments);
            var coreSegmentIds = new List<WorldFeatureId>(5);
            var mainSegmentIds = new List<WorldFeatureId>(3);
            var branchSegmentIds = new List<WorldFeatureId>(activeLocalBranchCount);
            var horizontalSpine = centerInput.Context.Landscape.StructuralDirection < 0.5f;
            var primarySides = SelectPrimarySides(centerInput, portIds, horizontalSpine);
            for (var i = 0; i < Sides.Length; i++)
            {
                var side = Sides[i].Side;
                var hierarchy = primarySides.Contains(side)
                    ? CanyonHierarchyTier.MainSpine
                    : CanyonHierarchyTier.Tributary;
                var junctionId = junctionIds[GetJunctionIndex(centerInput, side)];
                var segment = CreateSegment(
                    world,
                    centerInput,
                    junctionId,
                    portIds[side],
                    hierarchy,
                    CanyonTraversalAccess.BooterAndBigArm,
                    profile.BigArmClearWidth,
                    profile.BigArmClearHeight,
                    (ulong)(200 + i));
                segments.Add(segment);
                coreSegmentIds.Add(segment.Id);
                if (hierarchy == CanyonHierarchyTier.MainSpine)
                {
                    mainSegmentIds.Add(segment.Id);
                }
            }

            var junctionSegment = CreateSegment(
                world,
                centerInput,
                junctionIds[0],
                junctionIds[1],
                CanyonHierarchyTier.MainSpine,
                CanyonTraversalAccess.BooterAndBigArm,
                profile.BigArmClearWidth,
                profile.BigArmClearHeight,
                250UL);
            segments.Add(junctionSegment);
            coreSegmentIds.Add(junctionSegment.Id);
            mainSegmentIds.Add(junctionSegment.Id);

            for (var i = 0; i < draftLocalNodes.Count; i++)
            {
                var attachmentIndex = (int)(Seed(centerInput, (ulong)(400 + i)) & 1UL);
                var segment = CreateSegment(
                    world,
                    centerInput,
                    junctionIds[attachmentIndex],
                    draftLocalNodes[i].Id,
                    CanyonHierarchyTier.MinorBranch,
                    CanyonTraversalAccess.BooterOnly,
                    profile.BooterClearWidth,
                    profile.BooterClearHeight,
                    (ulong)(300 + i));
                segments.Add(segment);
                branchSegmentIds.Add(segment.Id);
            }

            for (var junctionIndex = 0; junctionIndex < junctionDrafts.Length; junctionIndex++)
            {
                var junction = junctionDrafts[junctionIndex];
                var role = CanyonNodeRole.Spine;
                if (junctionIndex == 0)
                {
                    role |= CanyonNodeRole.RegroupAnchor;
                }

                role |= ClassifyRole(junction.Id, segments);
                nodes.Add(new CanyonNodePlan(
                    junction.Id,
                    centerInput.Key,
                    junction.Position,
                    junction.CrossSection,
                    role));
            }

            for (var i = 0; i < draftLocalNodes.Count; i++)
            {
                var draft = draftLocalNodes[i];
                var role = CanyonNodeRole.LocalOpportunity | CanyonNodeRole.Branch;
                role |= ClassifyRole(draft.Id, segments);
                nodes.Add(new CanyonNodePlan(
                    draft.Id,
                    centerInput.Key,
                    draft.Position,
                    draft.CrossSection,
                    role));
            }

            var routes = new[]
            {
                new CanyonMacroRoutePlan(
                    CreateFeatureId(world, centerInput.Key, "route.primary-spine"),
                    CanyonMacroRouteKind.PrimarySpine,
                    CanyonTraversalAccess.BooterAndBigArm,
                    mainSegmentIds),
                new CanyonMacroRoutePlan(
                    CreateFeatureId(world, centerInput.Key, "route.bigarm-regroup"),
                    CanyonMacroRouteKind.BigArmRegroupNetwork,
                    CanyonTraversalAccess.BooterAndBigArm,
                    coreSegmentIds),
                new CanyonMacroRoutePlan(
                    CreateFeatureId(world, centerInput.Key, "route.booter-opportunity"),
                    CanyonMacroRouteKind.BooterOpportunity,
                    CanyonTraversalAccess.BooterOnly,
                    branchSegmentIds)
            };
            var planningCost = new CanyonPlanningCost(
                1 + neighborInputs.Count,
                boundaries.Count,
                nodes.Count,
                segments.Count);
            var fingerprint = BuildContentFingerprint(
                centerInput.Key,
                nodes,
                segments,
                boundaries,
                routes,
                junctionIds[0],
                planningCost);
            var header = new WorldPlanHeader(centerInput.Key, profile.FormatVersion, fingerprint);
            var candidate = new CanyonSystemPlan(
                header,
                cell,
                profile,
                nodes,
                segments,
                boundaries,
                routes,
                junctionIds[0],
                planningCost);
            if (!CanyonGraphValidator.TryValidate(candidate, out error))
            {
                return false;
            }

            plan = candidate;
            error = null;
            return true;
        }

        private bool TryCreateCellInput(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            CanyonSystemCellIndex cell,
            out CellInput input,
            out string error)
        {
            var center = cell.GetCenter(profile.CellSpan);
            var address = coordinateModel.Encode(center);
            if (!contextProvider.TrySample(world, coordinateModel, address, out var context, out error))
            {
                input = default;
                return false;
            }

            var key = new WorldPlanKey(world, CanyonNamespace, context.Address, PlanKind);
            input = new CellInput(cell, center, key, context, world);
            return true;
        }

        private CanyonNodeRole ClassifyRole(
            WorldFeatureId nodeId,
            WorldFeatureId firstNeighbor,
            WorldFeatureId secondNeighbor)
        {
            var incoming = 0;
            var outgoing = 0;
            CountDirection(nodeId, firstNeighbor, ref incoming, ref outgoing);
            CountDirection(nodeId, secondNeighbor, ref incoming, ref outgoing);
            return BuildDirectionalRole(incoming, outgoing);
        }

        private CanyonNodeRole ClassifyRole(
            WorldFeatureId nodeId,
            IReadOnlyList<CanyonSegmentPlan> segments)
        {
            var incoming = 0;
            var outgoing = 0;
            for (var i = 0; i < segments.Count; i++)
            {
                if (segments[i].ToNodeId.Equals(nodeId))
                {
                    incoming++;
                }

                if (segments[i].FromNodeId.Equals(nodeId))
                {
                    outgoing++;
                }
            }

            return BuildDirectionalRole(incoming, outgoing);
        }

        private static CanyonNodeRole BuildDirectionalRole(int incoming, int outgoing)
        {
            var role = CanyonNodeRole.None;
            if (incoming == 0)
            {
                role |= CanyonNodeRole.Source;
            }

            if (outgoing == 0)
            {
                role |= CanyonNodeRole.Termination;
            }

            if (incoming > 1)
            {
                role |= CanyonNodeRole.Confluence;
            }

            if (outgoing > 1)
            {
                role |= CanyonNodeRole.Branch;
            }

            if (incoming == 1 && outgoing == 1)
            {
                role |= CanyonNodeRole.Spine;
            }

            return role;
        }

        private static void CountDirection(
            WorldFeatureId node,
            WorldFeatureId neighbor,
            ref int incoming,
            ref int outgoing)
        {
            if (neighbor.CompareTo(node) > 0)
            {
                incoming++;
            }
            else
            {
                outgoing++;
            }
        }

        private CanyonSegmentPlan CreateSegment(
            WorldIdentity world,
            CellInput owner,
            WorldFeatureId firstNodeId,
            WorldFeatureId secondNodeId,
            CanyonHierarchyTier hierarchy,
            CanyonTraversalAccess access,
            float clearanceWidth,
            float clearanceHeight,
            ulong stream)
        {
            var from = firstNodeId.CompareTo(secondNodeId) > 0 ? firstNodeId : secondNodeId;
            var to = firstNodeId.CompareTo(secondNodeId) > 0 ? secondNodeId : firstNodeId;
            var segmentId = CreateFeatureId(
                world,
                owner.Key,
                "segment:" + from + ":" + to);
            var traversalId = CreateFeatureId(
                world,
                owner.Key,
                "traversal:" + segmentId);
            var variationSeed = Seed(owner, stream);
            var routeCost = 1f + 3f * UnitFloat(variationSeed >> 17);
            return new CanyonSegmentPlan(
                segmentId,
                owner.Key,
                from,
                to,
                hierarchy,
                UnitFloat(variationSeed),
                new CanyonTraversalReservation(
                    traversalId,
                    access,
                    clearanceWidth,
                    clearanceHeight,
                    routeCost));
        }

        private CanyonCrossSection BuildBoundaryCrossSection(
            CellInput first,
            CellInput second,
            WorldPlanBoundaryKey boundary)
        {
            var width = (first.Context.Landscape.CanyonWidth + second.Context.Landscape.CanyonWidth) * 0.5f;
            var depth = (first.Context.Landscape.CanyonDepth + second.Context.Landscape.CanyonDepth) * 0.5f;
            var seed = boundary.StableId.High ^ RotateLeft(boundary.StableId.Low, 23);
            return BuildCrossSection(width, depth, seed, profile.BigArmClearWidth * 1.5f);
        }

        private static CanyonCrossSection BuildCrossSection(
            float widthParameter,
            float depthParameter,
            ulong seed,
            float minimumWidth)
        {
            widthParameter = Clamp01(widthParameter);
            depthParameter = Clamp01(depthParameter);
            var width = Lerp(18f, 112f, widthParameter) * Lerp(0.86f, 1.14f, UnitFloat(seed));
            width = Math.Max(minimumWidth, width);
            var depth = Lerp(16f, 138f, depthParameter) * Lerp(0.88f, 1.12f, UnitFloat(seed >> 19));
            var lower = Lerp(0.16f, 0.27f, UnitFloat(seed >> 7));
            var middle = Lerp(0.43f, 0.57f, UnitFloat(seed >> 29));
            var upper = Lerp(0.73f, 0.87f, UnitFloat(seed >> 43));
            return new CanyonCrossSection(width, depth, new CanyonShelfProfile(lower, middle, upper));
        }

        private AbsoluteWorldPosition BuildBoundaryPosition(
            CanyonSystemCellIndex cell,
            CanyonBoundarySide side,
            WorldPlanBoundaryKey boundary)
        {
            var along = 0.15d + 0.7d * UnitDouble(boundary.StableId.Low);
            var minimumA = cell.HorizontalA * profile.CellSpan;
            var minimumB = cell.HorizontalB * profile.CellSpan;
            return side switch
            {
                CanyonBoundarySide.West => new AbsoluteWorldPosition(
                    minimumA,
                    0d,
                    minimumB + along * profile.CellSpan),
                CanyonBoundarySide.East => new AbsoluteWorldPosition(
                    minimumA + profile.CellSpan,
                    0d,
                    minimumB + along * profile.CellSpan),
                CanyonBoundarySide.South => new AbsoluteWorldPosition(
                    minimumA + along * profile.CellSpan,
                    0d,
                    minimumB),
                CanyonBoundarySide.North => new AbsoluteWorldPosition(
                    minimumA + along * profile.CellSpan,
                    0d,
                    minimumB + profile.CellSpan),
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };
        }

        private AbsoluteWorldPosition BuildLocalBranchPosition(
            AbsoluteWorldPosition center,
            ulong seed,
            int branchIndex)
        {
            var angle = UnitDouble(seed) * Math.PI * 2d + branchIndex * Math.PI;
            var radius = profile.CellSpan * (0.2d + 0.1d * UnitDouble(seed >> 13));
            return new AbsoluteWorldPosition(
                center.HorizontalA + Math.Cos(angle) * radius,
                center.Vertical,
                center.HorizontalB + Math.Sin(angle) * radius);
        }

        private AbsoluteWorldPosition BuildJunctionPosition(CellInput input, int junctionIndex)
        {
            var seed = Seed(input, 20UL);
            var angle = input.Context.Landscape.StructuralDirection * Math.PI * 2d
                + UnitDouble(seed) * 0.7d - 0.35d;
            var distance = profile.CellSpan * (0.08d + 0.07d * UnitDouble(seed >> 17));
            var direction = junctionIndex == 0 ? -1d : 1d;
            return new AbsoluteWorldPosition(
                input.Center.HorizontalA + Math.Cos(angle) * distance * direction,
                input.Center.Vertical,
                input.Center.HorizontalB + Math.Sin(angle) * distance * direction);
        }

        private HashSet<CanyonBoundarySide> SelectPrimarySides(
            CellInput input,
            IReadOnlyDictionary<CanyonBoundarySide, WorldFeatureId> portIds,
            bool horizontalSpine)
        {
            var selected = new HashSet<CanyonBoundarySide>();
            for (var junctionIndex = 0; junctionIndex < 2; junctionIndex++)
            {
                var candidates = new List<CanyonBoundarySide>();
                var preferred = new List<CanyonBoundarySide>();
                for (var sideIndex = 0; sideIndex < Sides.Length; sideIndex++)
                {
                    var side = Sides[sideIndex].Side;
                    if (GetJunctionIndex(input, side) != junctionIndex)
                    {
                        continue;
                    }

                    candidates.Add(side);
                    if (IsMainSide(side, horizontalSpine))
                    {
                        preferred.Add(side);
                    }
                }

                var source = preferred.Count > 0 ? preferred : candidates;
                source.Sort((left, right) => portIds[left].CompareTo(portIds[right]));
                selected.Add(source[0]);
            }

            return selected;
        }

        private static int GetJunctionIndex(CellInput input, CanyonBoundarySide side)
        {
            var pairing = (int)(Seed(input, 50UL) % 3UL);
            return pairing switch
            {
                0 => side == CanyonBoundarySide.West || side == CanyonBoundarySide.North ? 0 : 1,
                1 => side == CanyonBoundarySide.West || side == CanyonBoundarySide.South ? 0 : 1,
                _ => side == CanyonBoundarySide.West || side == CanyonBoundarySide.East ? 0 : 1
            };
        }

        private static CanyonBoundarySide Opposite(CanyonBoundarySide side)
        {
            return side switch
            {
                CanyonBoundarySide.West => CanyonBoundarySide.East,
                CanyonBoundarySide.East => CanyonBoundarySide.West,
                CanyonBoundarySide.South => CanyonBoundarySide.North,
                CanyonBoundarySide.North => CanyonBoundarySide.South,
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };
        }

        private WorldFeatureId BuildContentFingerprint(
            WorldPlanKey key,
            List<CanyonNodePlan> nodes,
            List<CanyonSegmentPlan> segments,
            List<CanyonBoundaryPortReference> boundaries,
            IReadOnlyList<CanyonMacroRoutePlan> routes,
            WorldFeatureId regroupAnchor,
            CanyonPlanningCost cost)
        {
            nodes.Sort((left, right) => left.Id.CompareTo(right.Id));
            segments.Sort((left, right) => left.Id.CompareTo(right.Id));
            boundaries.Sort((left, right) => left.PortNodeId.CompareTo(right.PortNodeId));
            var hash = new WorldStableHashBuilder("canyon-system-plan-content-v1");
            key.AppendTo(ref hash);
            hash.Append(BitConverter.DoubleToInt64Bits(profile.CellSpan));
            hash.Append(profile.MaximumLocalBranchCount);
            hash.Append(profile.FormatVersion);
            hash.Append(profile.AllowDeclaredCycles ? (byte)1 : (byte)0);
            for (var i = 0; i < nodes.Count; i++)
            {
                AppendNode(ref hash, nodes[i]);
            }

            for (var i = 0; i < segments.Count; i++)
            {
                AppendSegment(ref hash, segments[i]);
            }

            for (var i = 0; i < boundaries.Count; i++)
            {
                hash.Append((byte)boundaries[i].Side);
                hash.Append(boundaries[i].Boundary.StableId.High);
                hash.Append(boundaries[i].Boundary.StableId.Low);
                hash.Append(boundaries[i].PortNodeId.High);
                hash.Append(boundaries[i].PortNodeId.Low);
            }

            for (var i = 0; i < routes.Count; i++)
            {
                hash.Append(routes[i].Id.High);
                hash.Append(routes[i].Id.Low);
                hash.Append((byte)routes[i].Kind);
                hash.Append((byte)routes[i].Access);
                for (var segmentIndex = 0; segmentIndex < routes[i].SegmentIds.Count; segmentIndex++)
                {
                    hash.Append(routes[i].SegmentIds[segmentIndex].High);
                    hash.Append(routes[i].SegmentIds[segmentIndex].Low);
                }
            }

            hash.Append(regroupAnchor.High);
            hash.Append(regroupAnchor.Low);
            hash.Append(cost.ContextSamples);
            hash.Append(cost.BoundaryEvaluations);
            hash.Append(cost.NodeCount);
            hash.Append(cost.SegmentCount);
            hash.Finish128(out var high, out var low);
            return new WorldFeatureId(high, low);
        }

        private static void AppendNode(ref WorldStableHashBuilder hash, CanyonNodePlan node)
        {
            hash.Append(node.Id.High);
            hash.Append(node.Id.Low);
            hash.Append(node.Owner.StableId.High);
            hash.Append(node.Owner.StableId.Low);
            hash.Append(BitConverter.DoubleToInt64Bits(node.Position.HorizontalA));
            hash.Append(BitConverter.DoubleToInt64Bits(node.Position.Vertical));
            hash.Append(BitConverter.DoubleToInt64Bits(node.Position.HorizontalB));
            hash.Append(BitConverter.SingleToInt32Bits(node.CrossSection.Width));
            hash.Append(BitConverter.SingleToInt32Bits(node.CrossSection.Depth));
            hash.Append(BitConverter.SingleToInt32Bits(node.CrossSection.Shelves.Lower));
            hash.Append(BitConverter.SingleToInt32Bits(node.CrossSection.Shelves.Middle));
            hash.Append(BitConverter.SingleToInt32Bits(node.CrossSection.Shelves.Upper));
            hash.Append((int)node.Role);
        }

        private static void AppendSegment(ref WorldStableHashBuilder hash, CanyonSegmentPlan segment)
        {
            hash.Append(segment.Id.High);
            hash.Append(segment.Id.Low);
            hash.Append(segment.FromNodeId.High);
            hash.Append(segment.FromNodeId.Low);
            hash.Append(segment.ToNodeId.High);
            hash.Append(segment.ToNodeId.Low);
            hash.Append((byte)segment.Hierarchy);
            hash.Append(BitConverter.SingleToInt32Bits(segment.ProfileVariation));
            hash.Append((byte)segment.Traversal.Access);
            hash.Append(BitConverter.SingleToInt32Bits(segment.Traversal.MinimumClearWidth));
            hash.Append(BitConverter.SingleToInt32Bits(segment.Traversal.MinimumClearHeight));
            hash.Append(BitConverter.SingleToInt32Bits(segment.Traversal.RouteCost));
            hash.Append(segment.DeclaredCycleId);
        }

        private static WorldFeatureId CreateFeatureId(
            WorldIdentity world,
            WorldPlanKey owner,
            string localKey)
        {
            return WorldFeatureId.Create(world, CanyonNamespace, owner.OwnerAddress, localKey);
        }

        private static ulong Seed(CellInput input, ulong stream)
        {
            return CanyonNamespace.DeriveSeed(input.World, input.Context.Address, stream);
        }

        private static bool IsMainSide(CanyonBoundarySide side, bool horizontal)
        {
            return horizontal
                ? side == CanyonBoundarySide.West || side == CanyonBoundarySide.East
                : side == CanyonBoundarySide.South || side == CanyonBoundarySide.North;
        }

        private int GetActiveLocalBranchCount(WorldLandscapeParameters landscape)
        {
            var branchSignal = Clamp01((landscape.CanyonDensity + landscape.CanyonBranching) * 0.5f);
            return 1 + (int)Math.Floor(
                branchSignal * (profile.MaximumLocalBranchCount - 1) + 0.5f);
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }

        private static float Lerp(float minimum, float maximum, float value)
        {
            return minimum + (maximum - minimum) * Clamp01(value);
        }

        private static float UnitFloat(ulong value)
        {
            return (float)UnitDouble(value);
        }

        private static double UnitDouble(ulong value)
        {
            return (value >> 11) * (1d / 9007199254740992d);
        }

        private static ulong RotateLeft(ulong value, int distance)
        {
            return (value << distance) | (value >> (64 - distance));
        }

        private readonly struct SideDescriptor
        {
            public SideDescriptor(CanyonBoundarySide side, int deltaHorizontalA, int deltaHorizontalB)
            {
                Side = side;
                DeltaHorizontalA = deltaHorizontalA;
                DeltaHorizontalB = deltaHorizontalB;
            }

            public CanyonBoundarySide Side { get; }
            public int DeltaHorizontalA { get; }
            public int DeltaHorizontalB { get; }
        }

        private readonly struct CellInput
        {
            public CellInput(
                CanyonSystemCellIndex cell,
                AbsoluteWorldPosition center,
                WorldPlanKey key,
                WorldCoordinateContext context,
                WorldIdentity world)
            {
                Cell = cell;
                Center = center;
                Key = key;
                Context = context;
                World = world;
            }

            public CanyonSystemCellIndex Cell { get; }
            public AbsoluteWorldPosition Center { get; }
            public WorldPlanKey Key { get; }
            public WorldCoordinateContext Context { get; }
            public WorldIdentity World { get; }
        }

        private readonly struct LocalNodeDraft
        {
            public LocalNodeDraft(
                WorldFeatureId id,
                AbsoluteWorldPosition position,
                CanyonCrossSection crossSection)
            {
                Id = id;
                Position = position;
                CrossSection = crossSection;
            }

            public WorldFeatureId Id { get; }
            public AbsoluteWorldPosition Position { get; }
            public CanyonCrossSection CrossSection { get; }
        }
    }
}
