using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Opt-in reference-sector integration. Never installed in Greater Wasteland or bound to its save path.</summary>
    public sealed class FixedTerrainPopulationSectorStudy : MonoBehaviour
    {
        public const string StudyWorldId = "greater-wasteland.population-sector.study.v1";
        public const string RecipeId = "study.sculpted-basalt.v1";
        [SerializeField] private Terrain[] terrain;
        [SerializeField] private string[] terrainRevisions;
        [SerializeField] private Camera view;
        [SerializeField] private Light sun;
        [SerializeField] private Transform[] actorDemandMarkers;
        [SerializeField] private TopDown3DPlayerMotor studyBooter;
        [SerializeField] private TopDown3DBigArmFollower studyBigArm;
        [SerializeField] private GameObject prefab;
        [SerializeField] private int seed;
        [SerializeField] private string catalogRevision;
        [SerializeField] private Vector2 reservedStart, reservedEnd;
        [SerializeField] private string constraintRevision;
        private readonly List<FixedTerrainPopulationDemand> demands = new();
        private readonly Vector3[] corners = new Vector3[8];
        private readonly Dictionary<Vector2Int, string> revisions = new();
        private FixedTerrainPopulationState state;
        private FixedTerrainPopulationScheduler scheduler;
        private FixedTerrainPopulationPresentation presentation;
        private Transform populationRoot;
        private Bounds landscape;
        private double nextDemand;
        private TopDown3DPlayerMotor boundBooter;
        private TopDown3DBigArmFollower boundBigArm;
        private Func<Bounds, bool> actorPreparation;
        private readonly List<Bounds> actorRequests = new(64);
        private ulong synchronizedCollisionRevision;
        public int PhysicsSynchronizations { get; private set; }
        public FixedTerrainPopulationState State => state;
        public int ActiveCount => presentation?.ActiveCount ?? 0;
        public int PendingBuilds => scheduler?.PendingBuilds ?? 0;
        public int PendingPresentations => presentation?.PendingCells ?? 0;
        public int FaultedCells => scheduler?.FaultedCells ?? 0;
        public int UnreadyCells => scheduler?.UnreadyCells ?? 0;

        /// <summary>Readiness of the same conservative region used for actor demand, independent of the study camera.
        /// This component never moves an actor or supplies physics/route validation.</summary>
        public bool IsActorCoverageReady(Transform actor) => scheduler != null && actor != null && actor.gameObject.activeInHierarchy
            && Array.IndexOf(actorDemandMarkers, actor) >= 0
            && scheduler.IsCollisionCoverageReady(new Bounds(actor.position, new Vector3(256, 0, 256)));

        public void BindActors(TopDown3DPlayerMotor booter, TopDown3DBigArmFollower bigArm)
        {
            if (scheduler != null || boundBooter != null || boundBigArm != null || booter == null || bigArm == null
                || booter.gameObject.scene != gameObject.scene || bigArm.gameObject.scene != gameObject.scene
                || gameObject.scene.path == GreaterWastelandTerrainRendering.ScenePath
                || booter.PopulationPreparation != null || bigArm.PopulationPreparation != null)
                throw new InvalidOperationException("Bind unowned real actors in this reference scene before initialization; production binding is deferred.");
            var spawn = new Vector2(booter.transform.position.x, booter.transform.position.z);
            var companionSpawn = new Vector2(bigArm.transform.position.x, bigArm.transform.position.z);
            if (Vector2.Distance(spawn, reservedStart) > 16 || Vector2.Distance(companionSpawn, reservedStart) > 16)
                throw new InvalidOperationException("Reference actors must begin inside the protected 32 m spawn reservation.");
            boundBooter = booter; boundBigArm = bigArm; actorPreparation = PrepareActorRegion;
            studyBooter = booter; studyBigArm = bigArm;
            booter.PopulationPreparation = actorPreparation; bigArm.PopulationPreparation = actorPreparation;
        }
        private bool PrepareActorRegion(Bounds region)
        {
            // This recipe's maximum scaled footprint is 4 m. The actor supplies body, motion and route extent.
            region.Expand(new Vector3(8, 0, 8));
            if (!actorRequests.Contains(region))
            {
                if (actorRequests.Count >= 64) return false;
                actorRequests.Add(region);
            }
            if (scheduler == null || !scheduler.IsCollisionCoverageReady(region)) return false;
            if (presentation.CollisionRevision != synchronizedCollisionRevision)
            {
                Physics.SyncTransforms(); synchronizedCollisionRevision = presentation.CollisionRevision; PhysicsSynchronizations++;
            }
            return true;
        }

        public void Configure(Terrain[] tiles, string[] tileRevisions, Camera camera, Light light, Transform[] actors,
            GameObject template, int studySeed, string catalog, Vector2 start, Vector2 end, string constraints)
        {
            if (state != null) throw new InvalidOperationException("Configure the reference study before initialization.");
            terrain = tiles; terrainRevisions = tileRevisions; view = camera; sun = light; actorDemandMarkers = actors;
            prefab = template; seed = studySeed; catalogRevision = catalog; reservedStart = start; reservedEnd = end; constraintRevision = constraints;
        }
        private void OnEnable() { if (Application.isPlaying) Initialize(); }
        public void Initialize()
        {
            if (scheduler != null) return;
            if (terrain == null || terrain.Length != 16 || terrainRevisions == null || terrainRevisions.Length != terrain.Length
                || view == null || prefab == null || string.IsNullOrWhiteSpace(catalogRevision) || string.IsNullOrWhiteSpace(constraintRevision)
                || actorDemandMarkers == null || actorDemandMarkers.Length > 8) throw new InvalidOperationException("Use an explicit 16-tile reference sector and versioned study inputs.");
            if (boundBooter == null && boundBigArm == null && (studyBooter != null || studyBigArm != null)) BindActors(studyBooter, studyBigArm);
            revisions.Clear();
            for (int i = 0; i < terrain.Length; i++)
            {
                var tile = terrain[i];
                if (tile == null || tile.terrainData == null || string.IsNullOrWhiteSpace(terrainRevisions[i])) throw new InvalidOperationException("Missing study native data/revision.");
                var cell = FixedTerrainPopulationIdentity.CellAt(new Vector2(tile.transform.position.x, tile.transform.position.z));
                if (!revisions.TryAdd(cell, terrainRevisions[i])) throw new InvalidOperationException("Duplicate study terrain ownership.");
                var bounds = new Bounds(tile.transform.position + tile.terrainData.size * .5f, tile.terrainData.size);
                if (i == 0) landscape = bounds; else landscape.Encapsulate(bounds);
            }
            var surface = new FixedTerrainSurfaceIndex(terrain, StudyWorldId);
            state ??= new FixedTerrainPopulationState(StudyWorldId, seed, 1, catalogRevision,
                new[] { new FixedTerrainPopulationRecipeVersion { recipeId = RecipeId, version = 1, candidateCount = 64 } },
                cell => revisions.TryGetValue(cell, out var revision) ? revision : null,
                cell => revisions.ContainsKey(cell) ? constraintRevision : null);
            var planner = new FixedTerrainPopulationPlanner(new FixedTerrainPopulationIdentity(StudyWorldId, seed, 1, RecipeId, 1),
                FixedTerrainPopulationPlanner.NativeSurface(surface), new[] {
                    new FixedTerrainPopulationExclusion(reservedStart, reservedStart, 32),
                    new FixedTerrainPopulationExclusion(reservedStart, reservedEnd, 12) }, 12, 4, 38, 1, 1);
            var owner = new GameObject("Reference sector population") { hideFlags = HideFlags.DontSave };
            owner.transform.SetParent(transform, false); populationRoot = owner.transform;
            try
            {
                presentation = new FixedTerrainPopulationPresentation(prefab, populationRoot, state, RecipeId, 1024, 128, 16);
                scheduler = new FixedTerrainPopulationScheduler(planner, presentation, state, revisions.Keys, 2);
                Physics.SyncTransforms(); synchronizedCollisionRevision = presentation.CollisionRevision; UpdateDemand(Time.realtimeSinceStartupAsDouble);
            }
            catch { Shutdown(); throw; }
        }
        private void Update()
        {
            AdvanceRuntime(Time.realtimeSinceStartupAsDouble);
        }
        /// <summary>Same demand cadence and work limits as the normal component loop; supports isolated timing without duplicate updates.</summary>
        public int AdvanceRuntime(double now)
        {
            if (scheduler == null) return 0;
            if (now >= nextDemand) UpdateDemand(now);
            return scheduler.Advance(64, 1, now);
        }
        public int AdvanceStudy(int operations, double milliseconds, double time)
        {
            if (scheduler == null) throw new InvalidOperationException("Initialize the study first.");
            UpdateDemand(time); return scheduler.Advance(operations, milliseconds, time);
        }
        public void UpdateDemand(double time)
        {
            demands.Clear();
            bool viewing = view != null && view.isActiveAndEnabled;
            var coverage = viewing ? CameraBounds() : default;
            if (viewing && sun != null && sun.isActiveAndEnabled && sun.shadows != LightShadows.None)
                coverage.Encapsulate(GreaterWastelandTerrainRendering.ShadowCasterBounds(coverage, landscape, sun.transform.forward));
            foreach (var cell in revisions.Keys)
            {
                var bounds = new Bounds(new Vector3((cell.x + .5f) * 256, 0, (cell.y + .5f) * 256), new Vector3(256, 0, 256));
                bool visual = viewing && FixedTerrainRenderWindow.IntersectsXZ(coverage, bounds), collision = false;
                float priority = visual ? -Vector2.Distance(new Vector2(view.transform.position.x, view.transform.position.z), new Vector2(bounds.center.x, bounds.center.z)) / 256 : 0;
                foreach (var actor in actorDemandMarkers)
                {
                    if (actor == null || !actor.gameObject.activeInHierarchy) continue;
                    // Study markers supply demand only; no actor is moved or simulated by this component.
                    var actorBounds = new Bounds(actor.position, new Vector3(256, 0, 256));
                    if (FixedTerrainRenderWindow.IntersectsXZ(actorBounds, bounds)) collision = true;
                }
                if (boundBooter != null && boundBooter.gameObject.activeInHierarchy
                    && FixedTerrainRenderWindow.IntersectsXZ(PaddedActorBounds(boundBooter.PopulationPreparationBounds()), bounds)) collision = true;
                if (boundBigArm != null && boundBigArm.gameObject.activeInHierarchy
                    && FixedTerrainRenderWindow.IntersectsXZ(PaddedActorBounds(boundBigArm.PopulationPreparationBounds()), bounds)) collision = true;
                foreach (var request in actorRequests) if (FixedTerrainRenderWindow.IntersectsXZ(request, bounds)) collision = true;
                if (visual || collision) demands.Add(new FixedTerrainPopulationDemand(cell, visual, collision, priority));
            }
            scheduler.SetDemand(demands, time); actorRequests.Clear(); nextDemand = time + .2;
        }
        private static Bounds PaddedActorBounds(Bounds region) { region.Expand(new Vector3(8, 0, 8)); return region; }
        private Bounds CameraBounds()
        {
            var bounds = new Bounds(view.transform.position, Vector3.zero);
            FixedTerrainCameraCoverage.FillFrustum(view, view.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, corners);
            foreach (var corner in corners) bounds.Encapsulate(corner);
            bounds.Expand(new Vector3(128, 0, 128)); return bounds;
        }
        public string CapturePopulationJson() => state == null ? null : JsonUtility.ToJson(state.CaptureSnapshot());
        public bool RestorePopulationJson(string json)
        {
            if (state == null || string.IsNullOrWhiteSpace(json)) return false;
            try { return state.ApplySnapshot(JsonUtility.FromJson<FixedTerrainPopulationSnapshot>(json)); }
            catch (ArgumentException) { return false; }
        }
        private void OnDisable() => Shutdown();
        private void OnDestroy()
        {
            if (boundBooter != null && boundBooter.PopulationPreparation == actorPreparation) boundBooter.PopulationPreparation = null;
            if (boundBigArm != null && boundBigArm.PopulationPreparation == actorPreparation) boundBigArm.PopulationPreparation = null;
        }
        public void Shutdown()
        {
            scheduler?.Dispose(); presentation?.Dispose(); scheduler = null; presentation = null;
            if (populationRoot != null)
            {
                if (Application.isPlaying) Destroy(populationRoot.gameObject); else DestroyImmediate(populationRoot.gameObject);
            }
            populationRoot = null;
            actorRequests.Clear();
        }
    }
}
