using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DefaultExecutionOrder(1000), DisallowMultipleComponent]
    public sealed class GreaterWastelandInventorySaveService : MonoBehaviour
    {
        private const string ProductionScene = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        private TopDown3DPlayerInventory kit;
        private TopDown3DBigArmCargo cargo;
        private TopDown3DResourceCache cache;
        private TopDown3DResourceWorldState resources;
        private TopDown3DPlacedHarvesterState harvesters;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DAlgaeCanisterState algae;
        private TopDown3DCampController camp;
        private TopDown3DPlayerMotor motor;
        private bool ready, recoveryRequired, applying;
        private float nextSave;
        private long kitRevision, cargoRevision, cacheRevision;
        private long submittedKitRevision, submittedCargoRevision, submittedCacheRevision;
        private GreaterWastelandCheckpointWriter writer;
        private FixedTerrainPopulationState population;
        private long submittedPopulationRevision, completedPopulationRevision;
        public enum CheckpointWriteStatus { Idle, Queued, Completed, Failed }
        public CheckpointWriteStatus WriteStatus { get; private set; }
        public long CompletedKitRevision => kitRevision;
        public long CompletedCargoRevision => cargoRevision;
        public long CompletedCacheRevision => cacheRevision;
        public long CompletedPopulationRevision => completedPopulationRevision;
        public bool RecoveryRequired => recoveryRequired;
        public string SavePath => Path.Combine(Application.persistentDataPath, "GreaterWasteland", "inventory-v1.json");

        // Bind explicitly before startup. No seed inference or silent version-1 population migration.
        public void ConfigurePopulation(FixedTerrainPopulationState state)
        {
            if (ready || writer != null || population != null || state == null || state.WorldId != GreaterWastelandInventorySnapshot.WorldIdentity)
                throw new InvalidOperationException("Bind one compatible population state before checkpoint startup.");
            population = state;
        }

        private void Start()
        {
            var scene = gameObject.scene;
            kit = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerInventory>(scene);
            cargo = TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmCargo>(scene);
            cache = TopDown3DGameHudCanvas.FindInScene<TopDown3DResourceCache>(scene);
            resources = TopDown3DGameHudCanvas.FindInScene<TopDown3DResourceWorldState>(scene);
            harvesters = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlacedHarvesterState>(scene);
            if (kit == null || cargo == null || cache == null || resources == null || harvesters == null) return;
            motor = kit.GetComponent<TopDown3DPlayerMotor>(); vitals = kit.GetComponent<TopDown3DSurvivalVitals>();
            algae = kit.GetComponent<TopDown3DAlgaeCanisterState>(); camp = kit.GetComponent<TopDown3DCampController>();
            if (motor == null || vitals == null || algae == null || camp == null || !camp.IsConfigured || !harvesters.IsAuthored) return;
            ready = true;
            if (File.Exists(SavePath + ".pending") || !File.Exists(SavePath) && File.Exists(SavePath + ".backup"))
                RequireRecovery("An interrupted inventory checkpoint is preserved. Automatic saving is disabled until recovery.");
            if (File.Exists(SavePath))
            {
                try
                {
                    if (!GreaterWastelandInventoryFile.Read(SavePath, out var snapshot) || !Restore(snapshot))
                        RequireRecovery("Saved inventory is incompatible. Its file and backup are preserved; automatic saving is disabled.");
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
                { RequireRecovery("Saved inventory could not be read. It is preserved; automatic saving is disabled."); }
            }
            RememberRevisions(); RememberSubmittedRevisions(); nextSave = Time.unscaledTime + 30f;
            writer = new GreaterWastelandCheckpointWriter(SavePath);
        }

        private bool SafeCheckpoint => ready && !applying && !recoveryRequired
            && !kit.State.IsCommitting && !cargo.State.IsCommitting && !cache.State.IsCommitting
            && camp.Phase == TopDown3DCampPhase.Idle
            && motor.IsGrounded
            && !kit.GetComponent<TopDown3DPlayerActionController>().IsGathering;
        private GreaterWastelandInventorySnapshot Capture()
        {
            var booterBody = kit.GetComponent<Rigidbody>(); var leggerBody = cargo.GetComponent<Rigidbody>();
            return new GreaterWastelandInventorySnapshot
            {
                version = population == null ? 1 : 2, worldId = GreaterWastelandInventorySnapshot.WorldIdentity, cacheId = cache.StableId,
                booterPosition = motor.Position, booterRotation = booterBody != null ? booterBody.rotation : kit.transform.rotation,
                leggerPosition = leggerBody != null ? leggerBody.position : cargo.transform.position,
                leggerRotation = leggerBody != null ? leggerBody.rotation : cargo.transform.rotation,
                kit = kit.CaptureSnapshot(), cargo = cargo.State.CaptureSnapshot(), cache = cache.State.CaptureSnapshot(),
                resources = resources.CaptureSnapshot(), harvesters = harvesters.CaptureSnapshot(),
                survival = vitals.CaptureSnapshot(), algae = algae.CaptureSnapshot(),
                campDeployed = camp.Record != null, camp = camp.Record,
                populationJson = population == null ? null : JsonUtility.ToJson(population.CaptureSnapshot())
            };
        }
        private void LateUpdate()
        {
            ObserveWriter();
            if (!SafeCheckpoint) return;
            if (Time.unscaledTime >= nextSave || submittedKitRevision != kit.State.Revision
                || submittedCargoRevision != cargo.State.Revision || submittedCacheRevision != cache.State.Revision
                || submittedPopulationRevision != (population?.Revision ?? 0)) QueueCheckpoint();
        }
        private void QueueCheckpoint()
        {
            var snapshot = Capture();
            if (!Supported(snapshot.booterPosition) || !Supported(snapshot.leggerPosition)) return;
            // Encoding detaches every reference from mutable live state before handing work to another thread.
            var request = new GreaterWastelandCheckpointWriter.Checkpoint(GreaterWastelandInventoryFile.Encode(snapshot),
                kit.State.Revision, cargo.State.Revision, cache.State.Revision, population?.Revision ?? 0);
            if (writer == null || !writer.Queue(request)) return;
            RememberSubmittedRevisions(); nextSave = Time.unscaledTime + 30f;
            WriteStatus = CheckpointWriteStatus.Queued;
        }
        private void ObserveWriter()
        {
            if (writer == null || !writer.Poll(out var completed, out var error)) return;
            if (error != null) { RequireRecovery("Inventory checkpoint could not be written. Previous files are preserved; automatic saving is disabled."); return; }
            kitRevision = completed.KitRevision; cargoRevision = completed.CargoRevision; cacheRevision = completed.CacheRevision;
            completedPopulationRevision = completed.PopulationRevision;
            WriteStatus = writer.Busy ? CheckpointWriteStatus.Queued : CheckpointWriteStatus.Completed;
        }
        private bool DrainWriter()
        {
            if (writer == null) return true;
            if (writer.Failed && recoveryRequired) return false;
            if (!writer.Drain(out var completed, out var error))
            {
                RequireRecovery("Inventory checkpoint could not be completed. Previous files are preserved; automatic saving is disabled.");
                return false;
            }
            if (completed != null)
            {
                kitRevision = completed.KitRevision; cargoRevision = completed.CargoRevision; cacheRevision = completed.CacheRevision;
                completedPopulationRevision = completed.PopulationRevision;
                WriteStatus = CheckpointWriteStatus.Completed;
            }
            return true;
        }
        public bool Save()
        {
            if (!SafeCheckpoint) return false;
            if (!DrainWriter()) return false;
            try
            {
                var snapshot = Capture();
                if (!Supported(snapshot.booterPosition) || !Supported(snapshot.leggerPosition)) return false;
                GreaterWastelandInventoryFile.Write(SavePath, snapshot);
                RememberRevisions(); RememberSubmittedRevisions(); nextSave = Time.unscaledTime + 30f;
                WriteStatus = CheckpointWriteStatus.Completed; return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            { RequireRecovery("Inventory checkpoint could not be written. Previous files are preserved; automatic saving is disabled."); return false; }
        }
        private void RememberRevisions()
        { kitRevision = kit.State.Revision; cargoRevision = cargo.State.Revision; cacheRevision = cache.State.Revision; completedPopulationRevision = population?.Revision ?? 0; }
        private void RememberSubmittedRevisions()
        { submittedKitRevision = kit.State.Revision; submittedCargoRevision = cargo.State.Revision; submittedCacheRevision = cache.State.Revision; submittedPopulationRevision = population?.Revision ?? 0; }
        private void RequireRecovery(string message)
        {
            recoveryRequired = true; WriteStatus = CheckpointWriteStatus.Failed; Debug.LogWarning(message, this);
            if (kit != null) kit.GetComponent<TopDown3DPlayerActionController>()?.ReportFeedback(message);
        }

        internal bool Restore(GreaterWastelandInventorySnapshot snapshot)
        {
            if (snapshot == null || !snapshot.HasValidHeader(cache.StableId) || !snapshot.TryReadPopulation(out var savedPopulation)
                || !resources.CanApplySnapshot(snapshot.resources)
                || (population == null ? savedPopulation != null : !population.CanApplySnapshot(savedPopulation))
                || !harvesters.CanApplySnapshot(snapshot.harvesters) || !camp.CanRestoreCamp(snapshot.camp, snapshot.campDeployed)
                || !Supported(snapshot.booterPosition) || !Supported(snapshot.leggerPosition)
                || !ValidVitals(snapshot.survival, vitals.Settings) || !ValidAlgae(snapshot.algae, algae.Settings)) return false;
            foreach (var record in snapshot.harvesters.Records)
            {
                var position = new Vector3((float)record.Position.HorizontalA, (float)record.Position.Vertical, (float)record.Position.HorizontalB);
                if (!GreaterWastelandInventorySnapshot.Finite(position) || !Supported(position)) return false;
            }
            var nodes = gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DIronstoneNode>(true))
                .ToDictionary(node => node.StableId, node => node.Definition.MaximumUses);
            foreach (var delta in snapshot.resources.Deltas)
                if (!nodes.TryGetValue(delta.StableId, out var maximum) || delta.RemainingUses > maximum) return false;
            var mutations = new[] { new TopDown3DInventoryTransaction.Mutation(kit.State),
                new TopDown3DInventoryTransaction.Mutation(cargo.State), new TopDown3DInventoryTransaction.Mutation(cache.State) };
            var inventories = new[] { snapshot.kit, snapshot.cargo, snapshot.cache };
            for (var i = 0; i < mutations.Length; i++) if (!mutations[i].Draft.ApplySnapshot(inventories[i]).Succeeded) return false;
            var previous = Capture();
            applying = true;
            try
            {
                var result = TopDown3DInventoryTransaction.Commit(mutations, () =>
                {
                    try { return ApplyWorld(snapshot); }
                    catch (Exception exception) { Debug.LogException(exception); return false; }
                });
                if (result.Succeeded) return true;
                try { ApplyWorld(previous); }
                catch (Exception exception) { Debug.LogException(exception); RequireRecovery("Checkpoint restoration needs recovery; its file is preserved."); }
                return false;
            }
            finally { applying = false; }
        }
        private bool ApplyWorld(GreaterWastelandInventorySnapshot snapshot)
        {
            if (!snapshot.TryReadPopulation(out var savedPopulation) || (population != null && !population.ApplySnapshot(savedPopulation))
                || !camp.RestoreCamp(snapshot.camp, snapshot.campDeployed)
                || !ApplyObserved(() => resources.ApplySnapshot(snapshot.resources), () => JsonUtility.ToJson(resources.CaptureSnapshot()) == JsonUtility.ToJson(snapshot.resources))
                || !ApplyObserved(() => harvesters.ApplySnapshot(snapshot.harvesters), () => JsonUtility.ToJson(harvesters.CaptureSnapshot()) == JsonUtility.ToJson(snapshot.harvesters))
                || !ApplyObserved(() => vitals.ApplySnapshot(snapshot.survival), () => JsonUtility.ToJson(vitals.CaptureSnapshot()) == JsonUtility.ToJson(snapshot.survival))
                || !ApplyObserved(() => algae.ApplySnapshot(snapshot.algae), () => JsonUtility.ToJson(algae.CaptureSnapshot()) == JsonUtility.ToJson(snapshot.algae))) return false;
            motor.Teleport(snapshot.booterPosition); kit.transform.rotation = snapshot.booterRotation;
            kit.GetComponent<Rigidbody>().rotation = snapshot.booterRotation;
            var follower = cargo.GetComponent<TopDown3DBigArmFollower>();
            if (follower != null) follower.RestoreSavedWorldPosition(snapshot.leggerPosition, snapshot.leggerRotation);
            else cargo.transform.SetPositionAndRotation(snapshot.leggerPosition, snapshot.leggerRotation);
            Physics.SyncTransforms(); return true;
        }
        private static bool ApplyObserved(Func<bool> apply, Func<bool> committed)
        {
            try { return apply(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                // A failed presentation callback must not replay an already applied checkpoint.
                return committed();
            }
        }
        private static bool Supported(Vector3 position) => Physics.RaycastAll(position + Vector3.up * 3f, Vector3.down, 8f,
            ~0, QueryTriggerInteraction.Ignore).Any(hit => hit.collider is TerrainCollider);
        internal static bool ValidVitals(TopDown3DSurvivalSnapshot value, TopDown3DSurvivalSettings settings)
        {
            if (value == null || settings == null || value.Version != TopDown3DSurvivalVitals.CurrentSnapshotVersion) return false;
            var amounts = new[] { value.Health, value.Hunger, value.Thirst, value.Oxygen, value.Reserve };
            var maximums = new[] { settings.MaximumHealth, settings.MaximumHunger, settings.MaximumThirst, settings.MaximumOxygen, settings.MaximumReserve };
            for (var i = 0; i < amounts.Length; i++) if (!float.IsFinite(amounts[i]) || amounts[i] < 0 || amounts[i] > maximums[i]) return false;
            return true;
        }
        internal static bool ValidAlgae(TopDown3DAlgaeCanisterSnapshot value, TopDown3DAlgaeCanisterSettings settings)
            => value != null && settings != null && value.version == 1 && double.IsFinite(value.remaining)
                && value.capacity == settings.capacity && value.remaining >= 0 && value.remaining <= value.capacity;
        private void OnApplicationQuit() { if (SafeCheckpoint) Save(); }
        private void OnDisable() => DrainWriter();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallLoaded()
        {
            SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
            for (var i = 0; i < SceneManager.sceneCount; i++) Install(SceneManager.GetSceneAt(i));
        }
        private static void Loaded(Scene scene, LoadSceneMode _) => Install(scene);
        private static void Install(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ProductionScene
                || TopDown3DGameHudCanvas.FindInScene<GreaterWastelandInventorySaveService>(scene) != null) return;
            GreaterWastelandIronstoneInstaller.TryInstall(scene); TopDown3DResourceCache.TryInstall(scene);
            var owner = new GameObject("Greater Wasteland inventory checkpoint");
            SceneManager.MoveGameObjectToScene(owner, scene); owner.AddComponent<GreaterWastelandInventorySaveService>();
        }
    }
}
