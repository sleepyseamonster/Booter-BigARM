using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DPlacedHarvesterRecord
    {
        [SerializeField] private string stableId;
        [SerializeField] private long placementSequence;
        [SerializeField] private double horizontalA;
        [SerializeField] private double vertical;
        [SerializeField] private double horizontalB;
        [SerializeField] private int dust;
        [SerializeField] private double fractionalSeconds;
        [SerializeField] private double lastEvaluatedSeconds;

        public string StableId => stableId;
        public long PlacementSequence => placementSequence;
        public AbsoluteWorldPosition Position => new AbsoluteWorldPosition(horizontalA, vertical, horizontalB);
        public int Dust => dust;
        public double FractionalSeconds => fractionalSeconds;
        public double LastEvaluatedSeconds => lastEvaluatedSeconds;

        internal static TopDown3DPlacedHarvesterRecord Create(
            string id, long sequence, AbsoluteWorldPosition position, int amount,
            double fractional, double evaluatedAt)
        {
            return new TopDown3DPlacedHarvesterRecord
            {
                stableId = id,
                placementSequence = sequence,
                horizontalA = position.HorizontalA,
                vertical = position.Vertical,
                horizontalB = position.HorizontalB,
                dust = amount,
                fractionalSeconds = fractional,
                lastEvaluatedSeconds = evaluatedAt
            };
        }

        internal void Advance(double now, TopDown3DHarvesterSettings settings)
        {
            if (now <= lastEvaluatedSeconds) return;
            if (dust >= settings.Capacity)
            {
                lastEvaluatedSeconds = now;
                fractionalSeconds = 0d;
                return;
            }

            var total = fractionalSeconds + now - lastEvaluatedSeconds;
            var produced = (int)Math.Min(settings.Capacity - dust,
                Math.Floor(total / settings.SecondsPerUnit));
            dust += produced;
            fractionalSeconds = dust >= settings.Capacity
                ? 0d
                : total - produced * settings.SecondsPerUnit;
            lastEvaluatedSeconds = now;
        }

        internal TopDown3DPlacedHarvesterRecord Clone()
        {
            return Create(stableId, placementSequence, Position, dust,
                fractionalSeconds, lastEvaluatedSeconds);
        }

        internal bool IsValid(TopDown3DHarvesterSettings settings, double clock)
        {
            return WorldFeatureId.TryParse(stableId, out var parsedId) && !parsedId.IsEmpty
                && placementSequence >= 0
                && IsFinite(horizontalA) && IsFinite(vertical) && IsFinite(horizontalB)
                && dust >= 0 && dust <= settings.Capacity
                && IsFinite(fractionalSeconds) && fractionalSeconds >= 0d
                && fractionalSeconds < settings.SecondsPerUnit
                && IsFinite(lastEvaluatedSeconds) && lastEvaluatedSeconds >= 0d
                && lastEvaluatedSeconds <= clock;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    [Serializable]
    public sealed class TopDown3DPlacedHarvesterSnapshot
    {
        public const int CurrentVersion = 1;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private int worldSeed;
        [SerializeField] private string authoredWorldId;
        [SerializeField] private double simulationSeconds;
        [SerializeField] private long nextSequence;
        [SerializeField] private List<TopDown3DPlacedHarvesterRecord> records =
            new List<TopDown3DPlacedHarvesterRecord>();

        public int Version => version;
        public int WorldSeed => worldSeed;
        public string AuthoredWorldId => authoredWorldId;
        public double SimulationSeconds => simulationSeconds;
        public long NextSequence => nextSequence;
        public IReadOnlyList<TopDown3DPlacedHarvesterRecord> Records => records;

        internal static TopDown3DPlacedHarvesterSnapshot Create(
            int seed, double seconds, long sequence,
            IEnumerable<TopDown3DPlacedHarvesterRecord> source, string authoredId = null)
        {
            var ordered = new List<TopDown3DPlacedHarvesterRecord>();
            foreach (var record in source) ordered.Add(record.Clone());
            ordered.Sort((left, right) => string.CompareOrdinal(left.StableId, right.StableId));
            return new TopDown3DPlacedHarvesterSnapshot
            {
                worldSeed = seed,
                authoredWorldId = authoredId,
                simulationSeconds = seconds,
                nextSequence = sequence,
                records = ordered
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class TopDown3DPlacedHarvesterState : MonoBehaviour
    {
        private static readonly WorldSeedNamespace PlacementNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Site, "player-micro-dust-harvester");

        [SerializeField] private TopDown3DProceduralWorld world;
        [SerializeField] private TopDown3DHarvesterSettings settings;
        private string authoredWorldId;

        private readonly Dictionary<string, TopDown3DPlacedHarvesterRecord> records =
            new Dictionary<string, TopDown3DPlacedHarvesterRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, TopDown3DHarvesterView> views =
            new Dictionary<string, TopDown3DHarvesterView>(StringComparer.Ordinal);
        private readonly List<string> staleViews = new List<string>();
        private double simulationSeconds;
        private long nextSequence;
        private float viewRefreshElapsed;

        public TopDown3DHarvesterSettings Settings => settings;
        public int DeployedCount => records.Count;
        public bool IsAuthored => world == null && !string.IsNullOrEmpty(authoredWorldId);

        public void ConfigureAuthored(string worldId, TopDown3DHarvesterSettings authoredSettings)
        {
            if (string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("An authored world ID is required.");
            world = null;
            authoredWorldId = worldId;
            settings = authoredSettings;
        }

        public void Configure(TopDown3DProceduralWorld proceduralWorld,
            TopDown3DHarvesterSettings authoredSettings)
        {
            world = proceduralWorld;
            settings = authoredSettings;
        }

        private void Awake()
        {
            if (world == null) world = GetComponent<TopDown3DProceduralWorld>();
            if (settings == null) settings = TopDown3DHarvesterSettings.Load();
        }

        private void Update()
        {
            if (settings == null || (world == null && !IsAuthored)) return;
            simulationSeconds += Mathf.Max(0f, Time.deltaTime);
            viewRefreshElapsed += Time.deltaTime;
            if (viewRefreshElapsed < 0.2f) return;
            viewRefreshElapsed = 0f;
            RefreshViews();
        }

        public bool TryPlace(Vector3 localPosition, TopDown3DInventoryState inventory,
            out string stableId)
        {
            stableId = null;
            if (IsAuthored) return TryPlaceAuthored(localPosition, inventory, out stableId);
            if (settings == null || world == null || inventory == null
                || records.Count >= settings.MaximumDeployed
                || nextSequence == long.MaxValue
                || !world.TryGetLoadedChunkAt(localPosition, out _)
                || !world.TryLocalToAbsolute(localPosition, out var absolute)
                || world.ProductionRuntime == null
                || !inventory.ItemCatalog.TryGetDefinition(TopDown3DHarvesterSettings.CanisterItemId, out _))
                return false;

            var authority = world.ProductionRuntime;
            var sequence = nextSequence;
            var address = authority.CoordinateModel.Encode(absolute);
            var id = WorldFeatureId.Create(authority.Identity, PlacementNamespace,
                address, $"canister:{sequence}").ToString();
            if (records.ContainsKey(id)) return false;

            var record = TopDown3DPlacedHarvesterRecord.Create(id, sequence,
                absolute, 0, 0d, simulationSeconds);
            var result = inventory.TryRemove(TopDown3DHarvesterSettings.CanisterItemId, 1,
                () =>
                {
                    if (records.Count >= settings.MaximumDeployed || records.ContainsKey(id)) return false;
                    records.Add(id, record);
                    nextSequence++;
                    return true;
                });
            if (!result.Succeeded) return false;
            stableId = id;
            RefreshViews();
            return true;
        }

        public bool TryPickup(string stableId, TopDown3DInventoryState inventory,
            out int collectedDust)
        {
            collectedDust = 0;
            if (settings == null || inventory == null
                || !records.TryGetValue(stableId ?? string.Empty, out var record)) return false;
            record.Advance(simulationSeconds, settings);
            var reward = record.Dust > 0
                ? new[]
                {
                    new TopDown3DItemAmount(TopDown3DHarvesterSettings.CanisterItemId, 1),
                    new TopDown3DItemAmount(TopDown3DHarvesterSettings.DustItemId, record.Dust)
                }
                : new[] { new TopDown3DItemAmount(TopDown3DHarvesterSettings.CanisterItemId, 1) };
            var result = inventory.TryAdd(reward,
                () => records.TryGetValue(stableId, out var current)
                    && ReferenceEquals(current, record) && records.Remove(stableId));
            if (!result.Succeeded) return false;
            collectedDust = record.Dust;
            RemoveView(stableId);
            return true;
        }

        public bool TryGet(string stableId, out TopDown3DPlacedHarvesterRecord record)
        {
            if (!records.TryGetValue(stableId ?? string.Empty, out record)) return false;
            if (settings != null) record.Advance(simulationSeconds, settings);
            return true;
        }

        public TopDown3DPlacedHarvesterSnapshot CaptureSnapshot()
        {
            if (settings == null || (world == null && !IsAuthored)) throw new InvalidOperationException("Harvester state is not configured.");
            foreach (var record in records.Values) record.Advance(simulationSeconds, settings);
            return TopDown3DPlacedHarvesterSnapshot.Create(IsAuthored ? 0 : world.WorldSeed,
                simulationSeconds, nextSequence, records.Values, IsAuthored ? authoredWorldId : null);
        }

        public bool CanApplySnapshot(TopDown3DPlacedHarvesterSnapshot snapshot)
        {
            if (settings == null || (world == null && !IsAuthored) || snapshot == null
                || snapshot.Version != TopDown3DPlacedHarvesterSnapshot.CurrentVersion
                || (IsAuthored ? snapshot.WorldSeed != 0 || snapshot.AuthoredWorldId != authoredWorldId
                    : snapshot.WorldSeed != world.WorldSeed || !string.IsNullOrEmpty(snapshot.AuthoredWorldId))
                || double.IsNaN(snapshot.SimulationSeconds)
                || double.IsInfinity(snapshot.SimulationSeconds)
                || snapshot.SimulationSeconds < 0d || snapshot.NextSequence < 0
                || snapshot.Records == null || snapshot.Records.Count > settings.MaximumDeployed)
                return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var sequences = new HashSet<long>();
            foreach (var record in snapshot.Records)
            {
                if (record == null || !record.IsValid(settings, snapshot.SimulationSeconds)
                    || record.PlacementSequence >= snapshot.NextSequence
                    || !ids.Add(record.StableId) || !sequences.Add(record.PlacementSequence))
                    return false;
                if (IsAuthored && record.StableId != AuthoredPlacementId(record.PlacementSequence)) return false;
                if (world != null && world.ProductionRuntime != null)
                {
                    try
                    {
                        var authority = world.ProductionRuntime;
                        var address = authority.CoordinateModel.Encode(record.Position);
                        var expected = WorldFeatureId.Create(authority.Identity,
                            PlacementNamespace, address,
                            $"canister:{record.PlacementSequence}").ToString();
                        if (!string.Equals(expected, record.StableId, StringComparison.Ordinal))
                            return false;
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool ApplySnapshot(TopDown3DPlacedHarvesterSnapshot snapshot)
        {
            if (!CanApplySnapshot(snapshot)) return false;
            ClearViews();
            records.Clear();
            foreach (var record in snapshot.Records) records.Add(record.StableId, record.Clone());
            simulationSeconds = snapshot.SimulationSeconds;
            nextSequence = snapshot.NextSequence;
            RefreshViews();
            return true;
        }

        private void RefreshViews()
        {
            if (IsAuthored)
            {
                if (!Application.isPlaying) return; // EditMode state tests and validation must not build runtime effects.
                foreach (var pair in records)
                {
                    if (!views.TryGetValue(pair.Key, out var view) || view == null)
                    {
                        var p = pair.Value.Position;
                        view = TopDown3DHarvesterView.Create(pair.Key, this, transform,
                            new Vector3((float)p.HorizontalA, (float)p.Vertical, (float)p.HorizontalB));
                        views[pair.Key] = view;
                    }
                    pair.Value.Advance(simulationSeconds, settings);
                    view.Refresh(pair.Value.Dust, settings.Capacity);
                }
                return;
            }
            if (world == null || settings == null) return;
            staleViews.Clear();
            foreach (var pair in views)
            {
                if (!records.ContainsKey(pair.Key) || pair.Value == null
                    || !world.TryAbsoluteToLocal(records[pair.Key].Position, out var local)
                    || !world.TryGetLoadedChunkAt(local, out var chunk)
                    || pair.Value.transform.parent != chunk.transform)
                    staleViews.Add(pair.Key);
            }
            foreach (var id in staleViews) RemoveView(id);

            foreach (var pair in records)
            {
                if (!world.TryAbsoluteToLocal(pair.Value.Position, out var local)
                    || !world.TryGetLoadedChunkAt(local, out var chunk)) continue;
                if (!views.TryGetValue(pair.Key, out var view) || view == null)
                {
                    view = TopDown3DHarvesterView.Create(pair.Key, this, chunk.transform, local);
                    views[pair.Key] = view;
                }
                pair.Value.Advance(simulationSeconds, settings);
                view.Refresh(pair.Value.Dust, settings.Capacity);
            }
        }

        private void RemoveView(string id)
        {
            if (!views.TryGetValue(id, out var view)) return;
            views.Remove(id);
            if (view != null)
            {
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }
        }

        private string AuthoredPlacementId(long sequence) => Hash128.Compute(
            authoredWorldId + ":micro-dust-canister:" + sequence.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToString();

        private bool TryPlaceAuthored(Vector3 position, TopDown3DInventoryState inventory, out string id)
        {
            id = null;
            if (settings == null || inventory == null || records.Count >= settings.MaximumDeployed
                || nextSequence == long.MaxValue || !float.IsFinite(position.x)
                || !float.IsFinite(position.y) || !float.IsFinite(position.z)) return false;
            var sequence = nextSequence;
            var stable = AuthoredPlacementId(sequence);
            var record = TopDown3DPlacedHarvesterRecord.Create(stable, sequence,
                new AbsoluteWorldPosition(position.x, position.y, position.z), 0, 0d, simulationSeconds);
            var result = inventory.TryRemove(TopDown3DHarvesterSettings.CanisterItemId, 1, () =>
            {
                if (records.ContainsKey(stable) || records.Count >= settings.MaximumDeployed) return false;
                records.Add(stable, record); nextSequence++; return true;
            });
            if (!result.Succeeded) return false;
            id = stable;
            RefreshViews();
            return true;
        }

        private void ClearViews()
        {
            foreach (var view in views.Values)
            {
                if (view == null) continue;
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }
            views.Clear();
        }

        private void OnDestroy() => ClearViews();
    }
}
