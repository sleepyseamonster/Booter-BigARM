using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DGameStateSaveService : MonoBehaviour
    {
        private static readonly WorldSeedNamespace SavedPlaceNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Site, "player-saved-place");

        [SerializeField] private TopDown3DPlayerInventory booterInventory;
        [SerializeField] private TopDown3DBigArmCargo cargo;
        [SerializeField] private TopDown3DBigArmState bigArmState;
        [SerializeField] private Transform booterTransform;
        [SerializeField] private TopDown3DProceduralWorld proceduralWorld;
        [SerializeField] private TopDown3DResourceWorldState resourceWorldState;
        [SerializeField] private int worldSeed;
        [SerializeField] private string fileName = "booter-bigarm-save.json";

        private readonly List<SavedPlaceRecord> savedPlaces = new List<SavedPlaceRecord>();
        private readonly Dictionary<string, TopDown3DWorldDeltaSnapshot> worldDeltas =
            new Dictionary<string, TopDown3DWorldDeltaSnapshot>(StringComparer.Ordinal);
        private long nextSavedPlaceSequence;
        private bool worldPersistenceExplicitlyConfigured;

        public string SavePath => Path.Combine(Application.persistentDataPath, fileName);
        public IReadOnlyList<SavedPlaceRecord> SavedPlaces => savedPlaces;
        public int WorldDeltaCount => worldDeltas.Count;

        public void Configure(
            TopDown3DPlayerInventory player,
            Transform playerTransform,
            TopDown3DBigArmCargo bigArmCargo,
            TopDown3DBigArmState companionState,
            int seed)
        {
            booterInventory = player;
            booterTransform = playerTransform;
            cargo = bigArmCargo;
            bigArmState = companionState;
            worldSeed = seed;
            ResolveWorldServices();
        }

        public void ConfigureWorldPersistence(
            TopDown3DProceduralWorld world,
            TopDown3DResourceWorldState resources)
        {
            proceduralWorld = world;
            resourceWorldState = resources;
            worldPersistenceExplicitlyConfigured = true;
        }

        public bool TryMarkPlace(
            string playerSuppliedName,
            IEnumerable<WorldFeatureReference> featureReferences,
            OptionalThematicCoordinatePayload thematicCoordinate,
            out SavedPlaceRecord record,
            out string error)
        {
            record = default;
            if (booterTransform == null)
            {
                error = "The save service has no Booter transform.";
                return false;
            }
            if (savedPlaces.Count >= TopDown3DGameStateSnapshot.MaximumSavedPlaces)
            {
                error = "The saved-place limit has been reached.";
                return false;
            }

            try
            {
                var authority = ResolveAuthority();
                var absolute = ToAbsolute(booterTransform.position);
                var address = authority.CoordinateModel.Encode(absolute);
                WorldFeatureId id;
                do
                {
                    id = WorldFeatureId.Create(
                        authority.Identity,
                        SavedPlaceNamespace,
                        address,
                        $"marker:{nextSavedPlaceSequence}");
                    nextSavedPlaceSequence = checked(nextSavedPlaceSequence + 1);
                }
                while (ContainsSavedPlace(id));
                record = new SavedPlaceRecord(
                    id,
                    WorldPersistenceManifest.CreateCurrent(
                        authority.Identity,
                        authority.CoordinateModel),
                    playerSuppliedName,
                    address,
                    absolute,
                    thematicCoordinate,
                    featureReferences ?? Array.Empty<WorldFeatureReference>());
                savedPlaces.Add(record);
                savedPlaces.Sort((left, right) => left.SavedPlaceId.CompareTo(right.SavedPlaceId));
                error = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is ArgumentOutOfRangeException
                || exception is InvalidOperationException)
            {
                error = exception.Message;
                record = default;
                return false;
            }
        }

        public bool RemoveSavedPlace(WorldFeatureId id)
        {
            for (var i = 0; i < savedPlaces.Count; i++)
            {
                if (!savedPlaces[i].SavedPlaceId.Equals(id)) continue;
                savedPlaces.RemoveAt(i);
                return true;
            }
            return false;
        }

        public bool TryUpsertWorldDelta(
            WorldFeatureId featureId,
            WorldVersionDomain domain,
            string operationId,
            int revision,
            string payload,
            out string error)
        {
            try
            {
                var delta = TopDown3DWorldDeltaSnapshot.Create(
                    featureId,
                    domain,
                    operationId,
                    revision,
                    payload);
                var key = featureId.ToString();
                if (!worldDeltas.ContainsKey(key)
                    && worldDeltas.Count >= TopDown3DGameStateSnapshot.MaximumWorldDeltas)
                {
                    error = "The runtime-delta limit has been reached.";
                    return false;
                }
                if (worldDeltas.TryGetValue(key, out var current) && revision <= current.Revision)
                {
                    error = "Runtime-delta revisions must increase monotonically.";
                    return false;
                }
                worldDeltas[key] = delta;
                error = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is ArgumentOutOfRangeException)
            {
                error = exception.Message;
                return false;
            }
        }

        public TopDown3DGameStateSnapshot CaptureSnapshot()
        {
            if (booterInventory == null || booterTransform == null)
                throw new InvalidOperationException("Save service is not configured.");
            ResolveWorldServices();
            var authority = ResolveAuthority();
            var manifest = WorldPersistenceManifest.CreateCurrent(
                authority.Identity,
                authority.CoordinateModel);
            var companion = bigArmState != null ? bigArmState.CaptureSnapshot(cargo) : null;
            AbsoluteWorldPosition? bigArmAbsolute = companion != null
                ? ToAbsolute(companion.AuthoritativePosition)
                : (AbsoluteWorldPosition?)null;
            return TopDown3DGameStateSnapshot.Create(
                manifest,
                authority.CoordinateModel,
                ToAbsolute(booterTransform.position),
                booterInventory.CaptureSnapshot(),
                companion,
                bigArmAbsolute,
                resourceWorldState != null ? resourceWorldState.CaptureSnapshot() : null,
                savedPlaces,
                worldDeltas.Values,
                nextSavedPlaceSequence);
        }

        public bool Save()
        {
            try
            {
                var temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(CaptureSnapshot(), true));
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, null);
                else File.Move(temp, SavePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not save BigARM state: {exception.Message}");
                return false;
            }
        }

        public bool Load()
        {
            if (!File.Exists(SavePath) || booterInventory == null || booterTransform == null)
                return false;
            try
            {
                var snapshot = JsonUtility.FromJson<TopDown3DGameStateSnapshot>(
                    File.ReadAllText(SavePath));
                if (!TryPrepareSnapshot(snapshot, out var prepared, out var error))
                {
                    Debug.LogWarning($"Rejected incompatible World Creator save: {error}", this);
                    return false;
                }
                return ApplyPreparedSnapshot(snapshot, prepared);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not load BigARM state: {exception.Message}");
                return false;
            }
        }

        internal bool TryPrepareSnapshot(
            TopDown3DGameStateSnapshot snapshot,
            out PreparedWorldState prepared,
            out string error)
        {
            prepared = default;
            error = null;
            ResolveWorldServices();
            var authority = ResolveAuthority();
            if (snapshot == null
                || !snapshot.TryCheckCompatibility(
                    authority.Identity,
                    authority.CoordinateModel,
                    out _,
                    out error))
                return false;
            if (snapshot.BooterInventory == null
                || !CanApplyInventorySnapshot(booterInventory.State, snapshot.BooterInventory)
                || snapshot.BooterAbsolutePosition == null
                || !snapshot.BooterAbsolutePosition.TryResolve(
                    authority.CoordinateModel,
                    out var booterAbsolute,
                    out error))
                return false;

            AbsoluteWorldPosition? bigArmAbsolute = null;
            if (snapshot.HasBigArm != (bigArmState != null))
            {
                error = "The save and runtime disagree about BigARM state availability.";
                return false;
            }
            if (snapshot.HasBigArm)
            {
                if (snapshot.BigArm == null
                    || snapshot.BigArm.Version != 1
                    || string.IsNullOrWhiteSpace(snapshot.BigArm.CompanionId)
                    || snapshot.BigArmAbsolutePosition == null
                    || !snapshot.BigArmAbsolutePosition.TryResolve(
                        authority.CoordinateModel,
                        out var resolvedBigArm,
                        out error))
                    return false;
                bigArmAbsolute = resolvedBigArm;
            }
            var savedCargo = snapshot.HasBigArmCargo ? snapshot.BigArm?.Cargo : null;
            if (snapshot.HasBigArmCargo != (cargo != null))
            {
                error = "The save and runtime disagree about BigARM cargo availability.";
                return false;
            }
            if (savedCargo != null
                && (savedCargo.Version != 1
                    || savedCargo.LoadframeVersion != 1
                    || savedCargo.Inventory == null
                    || !CanApplyInventorySnapshot(cargo.State, savedCargo.Inventory)))
            {
                error = "The BigARM cargo snapshot is invalid.";
                return false;
            }

            if (snapshot.SavedPlacePayloads == null
                || snapshot.SavedPlacePayloads.Count > TopDown3DGameStateSnapshot.MaximumSavedPlaces)
            {
                error = "The saved-place collection is missing or exceeds its bound.";
                return false;
            }
            var decodedPlaces = new List<SavedPlaceRecord>(snapshot.SavedPlacePayloads.Count);
            var placeIds = new HashSet<WorldFeatureId>();
            for (var i = 0; i < snapshot.SavedPlacePayloads.Count; i++)
            {
                byte[] payload;
                try { payload = Convert.FromBase64String(snapshot.SavedPlacePayloads[i]); }
                catch (FormatException)
                {
                    error = "A saved-place payload is not valid base64.";
                    return false;
                }
                if (!SavedPlaceRecordCodec.TryDecode(payload, out var place, out error)
                    || !place.Manifest.CheckCompatibility(
                        authority.Identity,
                        authority.CoordinateModel).IsCompatible
                    || !place.Address.IsCompatibleWith(authority.CoordinateModel)
                    || !authority.CoordinateModel.TryResolve(place.Address, out var resolvedPlace)
                    || !resolvedPlace.Equals(place.AbsolutePosition)
                    || !placeIds.Add(place.SavedPlaceId))
                {
                    error ??= "A saved place has incompatible identity, address, or duplicate ID.";
                    return false;
                }
                decodedPlaces.Add(place);
            }
            decodedPlaces.Sort((left, right) => left.SavedPlaceId.CompareTo(right.SavedPlaceId));

            if (snapshot.WorldDeltas == null
                || snapshot.WorldDeltas.Count > TopDown3DGameStateSnapshot.MaximumWorldDeltas)
            {
                error = "The runtime-delta collection is missing or exceeds its bound.";
                return false;
            }
            var decodedDeltas = new Dictionary<string, TopDown3DWorldDeltaSnapshot>(StringComparer.Ordinal);
            for (var i = 0; i < snapshot.WorldDeltas.Count; i++)
            {
                var delta = snapshot.WorldDeltas[i];
                if (delta == null || !delta.TryValidate(out var id, out error)
                    || !decodedDeltas.TryAdd(id.ToString(), delta))
                {
                    error ??= "A runtime delta is invalid or duplicated.";
                    return false;
                }
            }

            if (snapshot.NextSavedPlaceSequence < 0)
            {
                error = "The saved-place sequence is invalid.";
                return false;
            }
            if (resourceWorldState != null
                && (!snapshot.HasResources
                    || snapshot.Resources == null
                    || !resourceWorldState.CanApplySnapshot(snapshot.Resources)))
            {
                error = "The resource delta snapshot is incompatible.";
                return false;
            }
            if (resourceWorldState == null && snapshot.HasResources)
            {
                error = "The save contains resource deltas but no resource state is available.";
                return false;
            }

            prepared = new PreparedWorldState(
                booterAbsolute,
                bigArmAbsolute,
                decodedPlaces,
                decodedDeltas,
                snapshot.NextSavedPlaceSequence);
            error = null;
            return true;
        }

        private bool ApplyPreparedSnapshot(
            TopDown3DGameStateSnapshot snapshot,
            PreparedWorldState prepared)
        {
            if (proceduralWorld != null
                && !proceduralWorld.TryPrepareLoadFrame(prepared.BooterAbsolute))
                return false;
            if (!TryToLocal(prepared.BooterAbsolute, out var booterLocal)) return false;
            Vector3 bigArmLocal = default;
            if (prepared.BigArmAbsolute.HasValue
                && !TryToLocal(prepared.BigArmAbsolute.Value, out bigArmLocal))
                return false;

            var rollbackInventory = booterInventory.CaptureSnapshot();
            var rollbackPosition = booterTransform.position;
            var rollbackBigArm = bigArmState != null ? bigArmState.CaptureSnapshot(cargo) : null;
            var rollbackCargo = cargo != null ? cargo.CaptureSnapshot() : null;
            var rollbackResources = resourceWorldState != null ? resourceWorldState.CaptureSnapshot() : null;
            var rollbackPlaces = new List<SavedPlaceRecord>(savedPlaces);
            var rollbackDeltas = new Dictionary<string, TopDown3DWorldDeltaSnapshot>(worldDeltas);
            var rollbackSequence = nextSavedPlaceSequence;

            try
            {
                if (!booterInventory.ApplySnapshot(snapshot.BooterInventory).Succeeded)
                    throw new InvalidOperationException("Booter inventory snapshot was rejected.");
                booterTransform.position = booterLocal;
                if (bigArmState != null && snapshot.HasBigArm)
                {
                    var localizedCompanion = TopDown3DBigArmCompanionSnapshot.Create(
                        snapshot.BigArm.CompanionId,
                        bigArmLocal,
                        snapshot.BigArm.Task,
                        snapshot.BigArm.DetailSimulationLoaded,
                        snapshot.BigArm.Cargo);
                    if (!bigArmState.ApplySnapshot(localizedCompanion))
                        throw new InvalidOperationException("BigARM state snapshot was rejected.");
                }
                if (cargo != null && snapshot.HasBigArmCargo
                    && !cargo.ApplySnapshot(snapshot.BigArm.Cargo).Succeeded)
                    throw new InvalidOperationException("BigARM cargo snapshot was rejected.");
                if (resourceWorldState != null && !resourceWorldState.ApplySnapshot(snapshot.Resources))
                    throw new InvalidOperationException("Resource delta snapshot was rejected.");

                savedPlaces.Clear();
                savedPlaces.AddRange(prepared.SavedPlaces);
                worldDeltas.Clear();
                foreach (var pair in prepared.WorldDeltas) worldDeltas.Add(pair.Key, pair.Value);
                nextSavedPlaceSequence = prepared.NextSavedPlaceSequence;
                return true;
            }
            catch (Exception exception)
            {
                booterInventory.ApplySnapshot(rollbackInventory);
                booterTransform.position = rollbackPosition;
                if (bigArmState != null && rollbackBigArm != null) bigArmState.ApplySnapshot(rollbackBigArm);
                if (cargo != null && rollbackCargo != null) cargo.ApplySnapshot(rollbackCargo);
                if (resourceWorldState != null && rollbackResources != null)
                    resourceWorldState.ApplySnapshot(rollbackResources);
                savedPlaces.Clear();
                savedPlaces.AddRange(rollbackPlaces);
                worldDeltas.Clear();
                foreach (var pair in rollbackDeltas) worldDeltas.Add(pair.Key, pair.Value);
                nextSavedPlaceSequence = rollbackSequence;
                Debug.LogError($"Rejected game-state application atomically: {exception.Message}", this);
                return false;
            }
        }

        private WorldCreatorProductionAuthority ResolveAuthority()
        {
            ResolveWorldServices();
            return proceduralWorld?.ProductionRuntime?.Authority
                ?? WorldCreatorProductionRuntime.GetSharedAuthority(worldSeed);
        }

        private AbsoluteWorldPosition ToAbsolute(Vector3 local)
        {
            if (proceduralWorld != null
                && proceduralWorld.TryLocalToAbsolute(local, out var absolute))
                return absolute;
            return new AbsoluteWorldPosition(local.x, local.y, local.z);
        }

        private bool TryToLocal(AbsoluteWorldPosition absolute, out Vector3 local)
        {
            if (proceduralWorld != null) return proceduralWorld.TryAbsoluteToLocal(absolute, out local);
            try
            {
                local = new Vector3(
                    checked((float)absolute.HorizontalA),
                    checked((float)absolute.Vertical),
                    checked((float)absolute.HorizontalB));
                return true;
            }
            catch (OverflowException)
            {
                local = default;
                return false;
            }
        }

        private void ResolveWorldServices()
        {
            if (proceduralWorld == null && !worldPersistenceExplicitlyConfigured)
                proceduralWorld = FindFirstObjectByType<TopDown3DProceduralWorld>();
            if (resourceWorldState == null && !worldPersistenceExplicitlyConfigured)
                resourceWorldState = proceduralWorld != null
                    ? proceduralWorld.ResourceWorldState ?? proceduralWorld.GetComponent<TopDown3DResourceWorldState>()
                    : FindFirstObjectByType<TopDown3DResourceWorldState>();
        }

        private bool ContainsSavedPlace(WorldFeatureId id)
        {
            for (var i = 0; i < savedPlaces.Count; i++)
            {
                if (savedPlaces[i].SavedPlaceId.Equals(id)) return true;
            }
            return false;
        }

        private static bool CanApplyInventorySnapshot(
            TopDown3DInventoryState target,
            TopDown3DInventorySnapshot snapshot)
        {
            if (target == null || snapshot == null) return false;
            var validationState = new TopDown3DInventoryState(
                target.ItemCatalog,
                target.Policy);
            return validationState.ApplySnapshot(snapshot).Succeeded;
        }

        internal readonly struct PreparedWorldState
        {
            public PreparedWorldState(
                AbsoluteWorldPosition booterAbsolute,
                AbsoluteWorldPosition? bigArmAbsolute,
                List<SavedPlaceRecord> savedPlaces,
                Dictionary<string, TopDown3DWorldDeltaSnapshot> worldDeltas,
                long nextSavedPlaceSequence)
            {
                BooterAbsolute = booterAbsolute;
                BigArmAbsolute = bigArmAbsolute;
                SavedPlaces = savedPlaces;
                WorldDeltas = worldDeltas;
                NextSavedPlaceSequence = nextSavedPlaceSequence;
            }

            public AbsoluteWorldPosition BooterAbsolute { get; }
            public AbsoluteWorldPosition? BigArmAbsolute { get; }
            public List<SavedPlaceRecord> SavedPlaces { get; }
            public Dictionary<string, TopDown3DWorldDeltaSnapshot> WorldDeltas { get; }
            public long NextSavedPlaceSequence { get; }
        }
    }
}
