using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    /// <summary>CampLoopTesting-only expedition study. Runtime objects never serialize into the author scene.</summary>
    [DefaultExecutionOrder(500), DisallowMultipleComponent]
    public sealed class CampRootBeaconSession : MonoBehaviour, ITopDown3DInteractionTarget
    {
        public const string ScenePath = "Assets/_Project/Scenes/Reference/CampLoopTesting.unity";
        [field: SerializeReference] public RootBeaconRun Run { get; private set; }
        public TopDown3DPlayerMotor Player { get; private set; }
        public TopDown3DInputRouter Input { get; private set; }
        public string Failure { get; private set; }
        public event Action<RootBeaconRun> ExtractionCompleted;
        private TopDown3DPlayerActionController actions;
        private TopDown3DInteractionController interaction;
        private TopDown3DBigArmFollower legger;
        private TopDown3DSurvivalVitals vitals;
        private Material markerMaterial;
        private float healthAtDeparture, previousTimeScale;
        private bool leased, paused;
        [SerializeField] private bool expeditionOwned;
        [SerializeField] private CampExpeditionSession expeditionOwner;
        private bool retired;
        private TopDown3DInputRouter subscribedInput;
        private TopDown3DSurvivalVitals subscribedVitals;
        private bool HasBindings=>Player!=null&&Input!=null&&actions!=null&&interaction!=null&&legger!=null;
        private bool HasAuthority=>!retired&&gameObject.scene.IsValid()&&gameObject.scene.isLoaded
            &&(!expeditionOwned||expeditionOwner!=null&&expeditionOwner.OwnsBeacon(this));

        public UnityEngine.Object UnityObject => this;
        public string StableId => Run?.BeaconId ?? "camp.root-beacon.unavailable";
        public string Prompt => "Activate root beacon · extract (3 seconds)";
        public Vector3 InteractionPoint => transform.position + Vector3.up * .8f;
        public float InteractionRange => 3f;
        public bool IsAvailable => HasAuthority&&HasBindings&&Run?.Phase==RootBeaconPhase.Exploring&&isActiveAndEnabled;
        internal bool IsDeparting=>HasAuthority&&HasBindings&&Run?.Phase==RootBeaconPhase.Departing;
        internal bool HasLiveBinding=>isActiveAndEnabled&&HasAuthority&&HasBindings&&Run!=null;
        internal bool IsExpeditionOwned=>expeditionOwned;
        public static bool SupportsScene(string path) => string.Equals(path, ScenePath, StringComparison.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallCurrent() => OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        private static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Application.isPlaying || !scene.isLoaded || !SupportsScene(scene.path)
                || CampExpeditionFeature.Enabled(scene)
                || TopDown3DGameHudCanvas.FindInScene<CampRootBeaconSession>(scene) != null) return;
            CampExpeditionSession.ReconcileOrphans();
            var owner = new GameObject("Root Beacon · Expedition Arrival");
            SceneManager.MoveGameObjectToScene(owner, scene);
            var session = owner.AddComponent<CampRootBeaconSession>();
            session.Initialize(scene);
        }

        private void Initialize(Scene scene)
        {
            Player = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerMotor>(scene);
            Input = TopDown3DGameHudCanvas.FindInScene<TopDown3DInputRouter>(scene);
            actions = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerActionController>(scene);
            interaction = TopDown3DGameHudCanvas.FindInScene<TopDown3DInteractionController>(scene);
            legger = TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmFollower>(scene);
            vitals = TopDown3DGameHudCanvas.FindInScene<TopDown3DSurvivalVitals>(scene);
            if (Player == null || Input == null || actions == null || interaction == null || legger == null)
            { Fail("Root beacon requires Booter, Legger and the existing interaction/input systems."); return; }
            var terrains = new List<Terrain>();
            foreach (var root in scene.GetRootGameObjects()) terrains.AddRange(root.GetComponentsInChildren<Terrain>(false));
            int seed = Guid.NewGuid().GetHashCode();
            Physics.SyncTransforms();
            if (!RootBeaconArrival.TryChoose(terrains, seed, Player, legger, out var site))
            { Fail("No clear supported arrival site was found. Existing actor positions are preserved; extraction is unavailable."); return; }
            transform.position = site.Beacon;
            Run = new RootBeaconRun(scene.path, Guid.NewGuid().ToString("N"), seed, site.Beacon);
            // Initial expedition deployment only; no return/recall path moves the companion.
            Player.Teleport(RootBeaconArrival.StandingPosition(Player.GetComponent<Collider>(), site.Player));
            legger.PrepareInitialWorldStartPosition(site.Companion);
            BuildMarker();
            gameObject.AddComponent<CampRootBeaconHud>().Configure(this);
            BindEvents();
        }

        internal void ConfigureExpedition(CampExpeditionRunRecord record)
        {
            RestoreExpeditionState(record);
            BuildMarker();
            gameObject.AddComponent<CampRootBeaconHud>().Configure(this);
        }

        internal void RestoreExpeditionState(CampExpeditionRunRecord record)
        {
            UnbindEvents();
            expeditionOwned=true;var scene=gameObject.scene;
            expeditionOwner=CampExpeditionSession.Find(scene);
            Player=TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerMotor>(scene);Input=TopDown3DGameHudCanvas.FindInScene<TopDown3DInputRouter>(scene);
            actions=TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerActionController>(scene);interaction=TopDown3DGameHudCanvas.FindInScene<TopDown3DInteractionController>(scene);
            legger=TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmFollower>(scene);vitals=TopDown3DGameHudCanvas.FindInScene<TopDown3DSurvivalVitals>(scene);
            transform.position=record.origin;Run=new RootBeaconRun(ScenePath,record.id,record.seed,record.origin);Failure=null;BindEvents();
        }

        private void BindEvents()
        {
            if(!HasBindings||retired)return;
            Input.PickupCanisterRequested -= Cancel;
            Input.PickupCanisterRequested += Cancel;
            if (vitals != null) { vitals.HealthReduced -= Cancel; vitals.HealthReduced += Cancel; }
            subscribedInput=Input;subscribedVitals=vitals;
        }
        private void UnbindEvents()
        {
            if(subscribedInput!=null)subscribedInput.PickupCanisterRequested-=Cancel;
            if(subscribedVitals!=null)subscribedVitals.HealthReduced-=Cancel;
            subscribedInput=null;subscribedVitals=null;
        }

        private void OnEnable()
        {
            if (Run == null||!HasBindings||!HasAuthority) return;
            BindEvents();
            if (!expeditionOwned&&Run.Phase == RootBeaconPhase.Extracted)
            { leased = Player.TryBeginActionConstraint(this); PauseCompletedRun(); }
        }

        private void Fail(string message)
        {
            Failure = message;
            Debug.LogWarning(message, this);
            gameObject.AddComponent<CampRootBeaconHud>().Configure(this);
        }

        // Structural availability never calls reachability: CanReachTarget itself reads IsAvailable.
        public string DepartureBlocker
        {
            get
            {
                if(!HasAuthority||!HasBindings||Run==null)return "The root beacon is waiting for its active expedition binding.";
                if(Run.Phase==RootBeaconPhase.Departing)return "Return countdown is already running.";
                if(Run.Phase==RootBeaconPhase.Extracted)return "Return settlement is pending.";
                if(Player.IsMounted)return "Dismount Legger before activating the root beacon.";
                if(Input.Mode!=TopDown3DInputMode.Gameplay)return "Close the menu to activate the root beacon.";
                if(!Player.isActiveAndEnabled||vitals!=null&&vitals.Health<=0)return "Booter must be alive to return.";
                if(actions.IsBusy)return "Finish or cancel the current activity before returning.";
                if(!Player.IsGrounded)return "Land before activating the root beacon.";
                if(!Run.AtBeacon(Player.Position)||Vector3.Distance(Player.Position,InteractionPoint)>InteractionRange)return "Move closer to the root beacon on its ground level.";
                if(!interaction.CanReachTarget(this))return "The root beacon is obstructed. Approach from a clear side.";
                if(!CompanionPresent())return "Bring Legger back to the root beacon before extracting.";
                return null;
            }
        }
        public bool TryBeginDeparture()
        {
            var blocker=DepartureBlocker;
            if(!IsAvailable||blocker!=null){actions?.ReportFeedback(blocker??"The root beacon is unavailable.");return false;}
            if (!Run.TryDepart(Player.Position)) return false;
            healthAtDeparture = vitals != null ? vitals.Health : 1;
            return true;
        }

        private bool CompanionPresent() => legger != null && legger.isActiveAndEnabled
            && Vector3.Distance(legger.transform.position, Run.Origin) <= 10f;

        private void Update() => AdvanceDeparture(Time.deltaTime);

        internal void AdvanceDeparture(float seconds)
        {
            if(retired)return;
            if(Run==null||!HasBindings||expeditionOwned&&!HasAuthority)
            {
                if(CampExpeditionSession.Find(gameObject.scene)?.TryRestoreBeaconState(this)!=true)
                {
                    const string reason="The root beacon has no active expedition binding. Open the expedition menu for recovery; no cargo was transferred.";
                    if(string.IsNullOrEmpty(Failure)){Failure=reason;Debug.LogWarning(reason,this);}
                    return;
                }
            }
            if(!HasAuthority)return;
            if (Run == null || Run.Phase != RootBeaconPhase.Departing) return;
            bool valid = Player != null && Player.isActiveAndEnabled && Input != null
                && Input.Mode == TopDown3DInputMode.Gameplay
                && (vitals == null || vitals.Health > 0 && vitals.Health >= healthAtDeparture);
            Run.Advance(seconds, Player != null ? Player.Position : Vector3.positiveInfinity, valid);
            if (Run.Phase == RootBeaconPhase.Exploring) { Release(); actions.ReportFeedback("Departure interrupted. Activate the beacon again when ready."); }
            else if (Run.Phase == RootBeaconPhase.Extracted)
            {
                if(!expeditionOwned)leased=Player.TryBeginActionConstraint(this);
                PauseCompletedRun();
                ExtractionCompleted?.Invoke(Run);
            }
        }

        private void PauseCompletedRun()
        {
            if(expeditionOwned)return;
            if (!paused) { previousTimeScale = Time.timeScale; Time.timeScale = 0; paused = true; }
            Input.EnterCampMode(TopDown3DInputMode.CampActions);
        }

        private void LateUpdate()
        {
            if(expeditionOwned)return;
            if (Run?.Phase != RootBeaconPhase.Extracted || Input == null) return;
            var menu = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene);
            if (menu == null || !menu.IsOpen) Input.EnterCampMode(TopDown3DInputMode.CampActions);
        }

        public void Cancel()
        {
            if (Run?.Phase != RootBeaconPhase.Departing) return;
            Run.Cancel(); Release(); actions?.ReportFeedback("Departure cancelled.");
        }

        private void Release() { if (leased && Player != null) Player.EndActionConstraint(this); leased = false; }
        private void OnDisable()
        {
            if(retired){UnbindEvents();return;}
            Cancel(); Release();
            UnbindEvents();
            if (Input != null) Input.PickupCanisterRequested -= Cancel;
            if (vitals != null) vitals.HealthReduced -= Cancel;
            if (paused) { if (Mathf.Approximately(Time.timeScale, 0)) Time.timeScale = previousTimeScale; paused = false; }
            if (Run?.Phase == RootBeaconPhase.Extracted && Input != null && Input.Mode == TopDown3DInputMode.CampActions) Input.EnterGameplayMode();
        }
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        internal void Retire()
        {
            Release();retired=true;paused=false;
            UnbindEvents();
        }
        private void OnDestroy() { if (markerMaterial != null) Destroy(markerMaterial); }

        private void BuildMarker()
        {
            var trigger = gameObject.AddComponent<SphereCollider>(); trigger.radius = .65f;
            trigger.center = Vector3.up * .8f; trigger.isTrigger = true;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                markerMaterial = new Material(shader) { name = "Root beacon session marker" };
                markerMaterial.SetColor("_BaseColor", new Color(.12f, .8f, .8f));
                markerMaterial.EnableKeyword("_EMISSION"); markerMaterial.SetColor("_EmissionColor", new Color(.1f, 1.3f, 1.3f));
            }
            Part("Beacon footing", PrimitiveType.Cylinder, new Vector3(0, .12f, 0), new Vector3(1.4f, .12f, 1.4f));
            Part("Beacon mast", PrimitiveType.Cylinder, new Vector3(0, 1.6f, 0), new Vector3(.15f, 1.5f, .15f));
            Part("Beacon signal", PrimitiveType.Sphere, new Vector3(0, 3.2f, 0), Vector3.one * .55f);
        }

        private void Part(string label, PrimitiveType type, Vector3 position, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(type); part.name = label; part.transform.SetParent(transform, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            if (markerMaterial != null) part.GetComponent<Renderer>().sharedMaterial = markerMaterial;
        }
    }
}
