using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DefaultExecutionOrder(-12000)]
    public sealed partial class CampExpeditionSession:MonoBehaviour
    {
        private static readonly Dictionary<int,CampExpeditionSession> sessions=new();
        private CampExpeditionLedger ledger;
        private CampExpeditionFile saveFile;
        private CampExpeditionMenuView view;
        private TopDown3DPlayerInventory kit;
        private TopDown3DBigArmCargo cargo;
        private TopDown3DPlayerMotor player;
        private TopDown3DBigArmFollower legger;
        private TopDown3DInputRouter input;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DAlgaeCanisterState phagia;
        private TopDown3DMenuController settings;
        private EventSystem events;
        private InputSystemUIInputModule module;
        private InputActionReference submit,click,cancel;
        private CampExpeditionSnapshot snapshot;
        private CampPreparedOperation pending;
        private CampMenuStackView[] candidates=Array.Empty<CampMenuStackView>();
        private readonly Dictionary<string,CampExpeditionNodeRecord> nodes=new(StringComparer.Ordinal);
        private readonly List<CampExpeditionSource> sourceViews=new();
        private GameObject population;
        private CampRootBeaconSession beacon;
        private bool frozen=true,menuOpen,waitingRelease,quitting,restoring;
        private bool saveRecovery,quitAfterRecovery;
        private bool restoringRider,fatalRecovery;
        private float riderRestoreSeconds;
        internal bool RestoringRiderCheckpoint=>restoringRider;
        internal bool IsSettledTown=>snapshot?.phase is CampExpeditionPhase.Town or CampExpeditionPhase.CargoReview;
        internal InputSystemUIInputModule OwnedInputModule=>view!=null?module:null;
        private float initialTimeScale=1,autosave;
        private double gameSeconds;
        private string message;
        private string boundRunId;
        [SerializeField, HideInInspector] private string reloadCheckpoint;
        [SerializeField, HideInInspector] private bool reloadSaveWaiting;
        private bool resumeAfterReload;
        private bool retired;
        internal CampRootBeaconSession CurrentBeacon=>OwnsBeacon(beacon)?beacon:null;
        internal bool OwnsBeacon(CampRootBeaconSession target)=>!retired && isActiveAndEnabled
            && gameObject.scene.IsValid() && gameObject.scene.isLoaded && ledger!=null && !fatalRecovery
            && snapshot?.phase==CampExpeditionPhase.Exploring && snapshot.run!=null
            && target!=null && target==beacon && target.gameObject.scene==gameObject.scene
            && boundRunId==snapshot.run.id && target.Run?.RunId==snapshot.run.id;
        public CampExpeditionConfiguration Configuration{get;private set;}
        private bool Settling=>pending?.Description=="expedition.extract";
        public bool CanSimulateField=>ledger!=null&&!frozen&&!restoringRider&&!fatalRecovery&&!Settling&&!ledger.Busy&&snapshot?.phase==CampExpeditionPhase.Exploring;
        public bool HasDiggingTool=>ledger!=null&&ledger.Booter.Slots[8].ItemId is CampExpeditionDefinition.ToolZeroId or CampExpeditionDefinition.ToolOneId;
        public bool HasMountedPhagia=>ledger!=null&&(ledger.Booter.Slots[9].ItemId==CampExpeditionDefinition.PhagiaId||ledger.Booter.Slots[10].ItemId==CampExpeditionDefinition.PhagiaId);
        public static CampExpeditionSession Find(Scene scene)
        {
            if(!scene.IsValid()||!scene.isLoaded)return null;
            if(sessions.TryGetValue(scene.handle,out var found)&&found!=null&&!found.retired
                &&found.isActiveAndEnabled&&found.gameObject.scene==scene)return found;
            // A runtime registry is not durable authority. Recover its existing scene owner
            // without creating a second session or reading/replacing the player's save.
            found=TopDown3DGameHudCanvas.FindInScene<CampExpeditionSession>(scene);
            if(found==null||found.retired||!found.isActiveAndEnabled)return null;
            sessions[scene.handle]=found;
            return found;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetSessions()=>sessions.Clear();
        internal static void Install(Scene scene)
        {
            ReconcileOrphans();
            if(Find(scene)!=null)return;var root=new GameObject("Camp Expedition · Persistent Loop");SceneManager.MoveGameObjectToScene(root,scene);root.AddComponent<CampExpeditionSession>();
        }
        private void Awake(){sessions[gameObject.scene.handle]=this;initialTimeScale=Time.timeScale>0?Time.timeScale:1;Time.timeScale=0;}
        private void OnEnable()
        {
            if(!Application.isPlaying||retired||!gameObject.scene.IsValid()||!gameObject.scene.isLoaded)return;
            sessions[gameObject.scene.handle]=this;
            resumeAfterReload=ledger==null&&!string.IsNullOrEmpty(reloadCheckpoint);
        }
        private void OnDisable()
        {
            // Main-thread, memory-only handoff. Do not roll the player back to the last autosave
            // or perform a disk write as a side effect of script recompilation.
            if(retired||ledger==null||quitting)return;
            reloadCheckpoint=JsonUtility.ToJson(ledger.State);
            reloadSaveWaiting=saveRecovery||Settling||fatalRecovery;
        }
        private void Start()
        {
            if(retired)return;
            ReconcileOrphans();
            // Adopt an already running pre-fix owner without replacing its checkpoint.
            gameObject.hideFlags=HideFlags.None;
            if(ledger!=null)return;
            try
            {
                bool reloading=!string.IsNullOrEmpty(reloadCheckpoint);
                Configuration=CampExpeditionFeature.Configuration;
                if(Configuration==null||Configuration.definition==null||Configuration.menuPrefab==null||!Configuration.definition.ValidatePresentation(out _))throw new InvalidOperationException("Expedition configuration requires qualified assets.");
                var scene=gameObject.scene;kit=FindIn<TopDown3DPlayerInventory>();cargo=FindIn<TopDown3DBigArmCargo>();player=FindIn<TopDown3DPlayerMotor>();legger=FindIn<TopDown3DBigArmFollower>();input=FindIn<TopDown3DInputRouter>();vitals=FindIn<TopDown3DSurvivalVitals>();
                if(kit==null||cargo==null||player==null||legger==null||input==null||vitals==null)throw new InvalidOperationException("Camp expedition actor bindings are incomplete.");
                var oldUi=TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene);if(oldUi==null)throw new InvalidOperationException("The existing input/settings bridge is unavailable.");
                events=oldUi.EventSystem;module=events.GetComponent<InputSystemUIInputModule>();
                if(!reloading){submit=module.submit;click=module.leftClick;cancel=module.cancel;}
                settings=TopDown3DMenuSceneInstaller.TryInstall(scene,input,oldUi,events);
                if(reloading){module.move=originalMove;if(settings!=null&&settings.IsOpen)settings.Close();}
                oldUi.enabled=false;settings.UseExpeditionHost();settings.ExpeditionSettingsClosed+=SettingsClosed;
                // The old loadout trial installed this service. Its grant/reset path is intentionally
                // bypassed here, so initialize the shared consumption/state service explicitly.
                phagia=TopDown3DAlgaeConsumptionController.TryInstallForScene(scene)?.State;
                if(phagia==null||phagia.Settings==null||!phagia.Settings.IsValid)throw new InvalidOperationException("The existing Phagia state must initialize before expedition ownership.");
                // Managed UI callbacks/dictionaries do not survive a domain reload. Rebuild the
                // presentation while retaining the captured run, cargo and generated-node records.
                if(view!=null){view.gameObject.SetActive(false);Destroy(view.gameObject);view=null;}
                var menu=Instantiate(Configuration.menuPrefab);SceneManager.MoveGameObjectToScene(menu,scene);menu.name="Expedition Game Menu";
                var replacement=menu.GetComponent<CampExpeditionMenuView>();
                try{replacement.Initialize(events);replacement.SetLargeText(settings.LargeText);}
                catch{menu.SetActive(false);Destroy(menu);throw;}
                view=replacement;view.Command+=Command;
                settings.UseExpeditionPresentation(view,ToggleInventory);
                CutOverOldFieldProcesses();
                var file=new CampExpeditionFile(Path.Combine(Application.persistentDataPath,"CampExpeditions","profile-v1.json"),Configuration.ContentFingerprint);
                saveFile=file;
                snapshot=reloading?CampExpeditionSnapshot.FromJson(reloadCheckpoint):file.Read();
                if(reloading&&(snapshot==null||snapshot.contentFingerprint!=Configuration.ContentFingerprint))throw new InvalidOperationException("The in-memory expedition checkpoint requires recovery; no starter items or replacement run were created.");
                var storage=new TopDown3DInventoryState(Configuration.definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,snapshot?.storageCapacity??Configuration.startingStorageSlots,campStorage:true));
                if(snapshot==null)
                {
                    if(kit.State.OccupiedSlotCount!=0||cargo.State.OccupiedSlotCount!=0)throw new InvalidOperationException("Unexpected pre-bootstrap inventory was preserved; automatic starter grants stopped.");
                    var prepared=new TopDown3DInventoryState(kit.ItemCatalog,kit.State.Policy);
                    var grant=prepared.TryAdd(new[]{new TopDown3DItemAmount(CampExpeditionDefinition.HarnessId,1),new TopDown3DItemAmount(CampExpeditionDefinition.ToolZeroId,1),new TopDown3DItemAmount(CampExpeditionDefinition.PhagiaId,1)});
                    if(!grant.Succeeded)throw new InvalidOperationException(grant.Message);
                    snapshot=new CampExpeditionSnapshot{sequence=1,contentFingerprint=Configuration.ContentFingerprint,profileId=Guid.NewGuid().ToString("N"),commons=Configuration.startingCommons,storageCapacity=storage.Capacity,
                        booter=prepared.CaptureSnapshot(),legger=cargo.CaptureSnapshot().Inventory,storage=storage.CaptureSnapshot(),vitals=vitals.CaptureSnapshot()};
                    if(phagia!=null&&phagia.Settings!=null)snapshot.equipmentStates=new[]{new CampEquipmentStateRecord{instanceId=prepared.Slots[9].InstanceId,phagia=phagia.CaptureSnapshot()}};
                    file.Write(snapshot); // Grant becomes visible only after the initial town checkpoint is durable.
                }
                ledger=new CampExpeditionLedger(file,snapshot,kit.State,cargo.State,storage);gameSeconds=snapshot.inGameSeconds;
                ledger.CaptureRuntime=CaptureRuntime;ledger.Changed+=Committed;
                if(reloading&&reloadSaveWaiting)throw new InvalidOperationException("Scripts reloaded while save recovery was pending. The durable checkpoint and pending save were preserved; restart Play and use save recovery before continuing.");
                fatalRecovery=false;message=null; // A validated checkpoint may retry transient service initialization.
                restoring=true;
                if(snapshot.vitals!=null&&!vitals.ApplySnapshot(snapshot.vitals))throw new InvalidOperationException("Saved survival state requires recovery.");
                RestorePhagia();restoring=false;RefreshCache();
                input.MenuToggleRequested+=ToggleStart;input.InventoryToggleRequested+=ToggleInventory;input.UiCancelRequested+=Back;input.CargoOpenRequested+=OpenCargo;
                BindMenuCommands();
                Application.wantsToQuit+=WantsQuit;
                if(snapshot.phase==CampExpeditionPhase.Exploring)
                {RestoreField();if(snapshot.run.riding){restoringRider=true;OpenMenu("play");message="Restoring the saved riding relationship…";}else CloseMenu();}
                else{OpenMenu(snapshot.phase==CampExpeditionPhase.CargoReview?"review":"play");}
            }
            catch(Exception error){Debug.LogException(error,this);Fail(error.Message);}
        }
        private T FindIn<T>()where T:Component=>TopDown3DGameHudCanvas.FindInScene<T>(gameObject.scene);
        private void CutOverOldFieldProcesses()
        {
            foreach(var root in gameObject.scene.GetRootGameObjects())
            {
                foreach(var source in root.GetComponentsInChildren<CampFirstToolSource>(true))source.enabled=false;
                foreach(var source in root.GetComponentsInChildren<CampResourceSource>(true))source.enabled=false;
                foreach(var devices in root.GetComponentsInChildren<TopDown3DPlacedHarvesterState>(true))devices.enabled=false;
                foreach(var work in root.GetComponentsInChildren<TopDown3DCampWorkbench>(true))work.enabled=false;
                foreach(var refinery in root.GetComponentsInChildren<TopDown3DDustRefinery>(true))refinery.enabled=false;
            }
        }
        private void CaptureRuntime(CampExpeditionSnapshot next)
        {
            // A newly durable candidate must be published before old live actors can contribute another snapshot.
            if(snapshot!=null&&next.sequence!=snapshot.sequence)return;
            next.inGameSeconds=gameSeconds;if(vitals!=null)next.vitals=vitals.CaptureSnapshot();
            if(next.run!=null&&next.run.id==boundRunId&&player!=null){next.run.playerPosition=player.Position;next.run.leggerPosition=legger.transform.position;next.run.playerRotation=player.transform.rotation;next.run.leggerRotation=legger.transform.rotation;next.run.riding=player.IsMounted;}
            if(phagia!=null&&phagia.Settings!=null)
                foreach(var record in next.equipmentStates)if(record.phagia!=null)record.phagia=phagia.CaptureSnapshot();
        }
        private void RestorePhagia()
        {if(phagia==null)return;foreach(var record in snapshot.equipmentStates)if(record.phagia!=null&&!phagia.ApplySnapshot(record.phagia))throw new InvalidOperationException("Saved Phagia contents require recovery.");}
        private void Committed()
        {
            try
            {
                var next=ledger.State;
                if(next.receipts.LastOrDefault()?.kind=="expedition.death"&&next.vitals!=null&&!vitals.ApplySnapshot(next.vitals))throw new InvalidOperationException("Committed respawn vitals require recovery.");
                snapshot=next;RefreshCache();
                foreach(var source in sourceViews)if(source!=null)source.RefreshPresentation();
                Render();
            }
            catch(Exception error){Fail(error.Message);}
        }
        private void RefreshCache(){nodes.Clear();if(snapshot?.run!=null)foreach(var node in snapshot.run.nodes)nodes.Add(node.key,node);}
        public CampExpeditionNodeRecord Node(string key)=>nodes.TryGetValue(key,out var node)?node:null;
        private void Update()
        {
            if(retired)return;
            if(ledger==null&&!fatalRecovery&&!string.IsNullOrEmpty(reloadCheckpoint))resumeAfterReload=true;
            if(resumeAfterReload){resumeAfterReload=false;Start();return;}
            if(waitingRelease&&input!=null&&settings?.IsOpen!=true&&!MenuSubmissionHeld()&&(!waitingNavigationRelease&&!view.StorageChoiceOpen||MenuNavigationNeutral()))
            {waitingRelease=false;RestoreMenuNavigation();module.submit=submit;module.leftClick=click;view.SetStorageChoiceReady();}
            if(ledger==null||fatalRecovery)return;
            AdvanceMenuScroll();
            if(restoringRider)
            {
                var riding=FindIn<TopDown3DLeggerRidingController>();var laboratory=FindIn<TopDown3DAnimationPlaygroundSession>();
                riderRestoreSeconds+=Time.unscaledDeltaTime;
                if(laboratory?.Readiness==AnimationPlaygroundReadiness.Failed||riderRestoreSeconds>30)
                {restoringRider=false;Fail("Saved riding could not initialize: "+(laboratory?.UnavailableReason??"actor services timed out")+". The checkpoint was preserved.");return;}
                if(riding!=null&&riding.IsConfigured&&laboratory?.PreviewReady==true)
                {if(!riding.RestoreExpeditionCheckpoint()){restoringRider=false;Fail("The saved mounted relationship could not be safely restored. The checkpoint was preserved.");return;}restoringRider=false;message=null;CloseMenu();}
            }
            if(CanSimulateField)
            {
                gameSeconds+=Time.deltaTime;autosave+=Time.unscaledDeltaTime;
                if(vitals.Health<=0){if(Apply(ledger.PrepareDeath(vitals.GetMaximum(TopDown3DSurvivalVital.Health)))){FindIn<TopDown3DLeggerRidingController>()?.ReleaseExpeditionTownRider();ClearField();OpenMenu("play");message="Returned to town at full health. Lost equipment is sealed; recovery services are unavailable.";Render();}return;}
                if(autosave>=Configuration.autosaveSeconds){autosave=0;Save();}
            }
            if(settings!=null&&settings.IsOpen&&!settings.UsesExpeditionPresentation&&view!=null)view.SetVisible(false);
        }
        private bool Apply(CampPreparedOperation operation)
        {
            bool prior=frozen;frozen=true;Time.timeScale=0;
            bool success=ledger.Commit(operation,out var failure);frozen=prior;
            if(!success)
            {
                message=failure;
                if(ledger.LastCommitRequiresRecovery)
                {saveRecovery=true;pending=operation;frozen=true;if(!menuOpen)OpenMenu("start");view.ShowQuote(failure+"\n\nRetry saves the same operation. The expedition remains frozen.","","SAVE WAITING","Retry save",false);ArmRelease();}
            }
            SetSimulation();Render();return success;
        }
        private void SetSimulation(){Time.timeScale=snapshot?.phase==CampExpeditionPhase.Exploring&&!frozen&&!Settling&&!restoringRider&&!fatalRecovery?initialTimeScale:0;}
        private void BeginRun()
        {
            if(snapshot.phase==CampExpeditionPhase.Exploring){CloseMenu();return;}
            if(snapshot.phase!=CampExpeditionPhase.Town)return;
            try
            {
                var terrains=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(false)).ToArray();
                int seed=Guid.NewGuid().GetHashCode();Physics.SyncTransforms();
                if(!RootBeaconArrival.TryChoose(terrains,seed,player,legger,out var site))throw new InvalidOperationException("No supported arrival was found; town state was retained.");
                var record=new CampExpeditionRunRecord{id=Guid.NewGuid().ToString("N"),seed=seed,terrainId=Configuration.terrainIdentity,origin=site.Beacon,
                    playerPosition=RootBeaconArrival.StandingPosition(player.GetComponent<Collider>(),site.Player),leggerPosition=RootBeaconArrival.StandingPosition(legger.GetComponent<Collider>(),site.Companion),playerRotation=player.transform.rotation,leggerRotation=legger.transform.rotation};
                var support=new CampExpeditionTerrain(terrains,player.transform,legger.transform);
                if(!CampExpeditionPopulation.TryPrepare(Configuration.definition,record.id,seed,record.origin,support.Support,support.Route,out record.nodes))throw new InvalidOperationException("This arrival has no complete supported material route; town state was retained. Try Play again.");
                var operation=PrepareDeparture(ledger,record);
                if(!Apply(operation))return;RestoreField();CloseMenu();
            }
            catch(Exception error){if(snapshot.phase==CampExpeditionPhase.Exploring&&snapshot.run?.id!=boundRunId)Fail(error.Message);else{message=error.Message;Render();}}
        }
        internal static CampPreparedOperation PrepareDeparture(CampExpeditionLedger ledger,CampExpeditionRunRecord record)
        {
            if(ledger.State.phase is not (CampExpeditionPhase.Town or CampExpeditionPhase.CargoReview))
                throw new InvalidOperationException("An expedition can only depart from town or retained cargo review.");
            // Play acknowledges keeping the reviewed cargo. The normal begin receipt
            // persists that choice and the new run together without moving any items.
            return ledger.Prepare("expedition.begin",(next,_)=>{next.run=record;next.phase=CampExpeditionPhase.Exploring;});
        }
        private void RestoreField()
        {
            if(snapshot.run==null||snapshot.run.terrainId!=Configuration.terrainIdentity)throw new InvalidOperationException("Saved terrain identity requires recovery.");
            var sourceLibrary=CampExpeditionSourceLibrary.Load(Configuration.definition);
            ClearField();player.RestoreExpeditionPose(snapshot.run.playerPosition,snapshot.run.playerRotation);legger.RestoreSavedWorldPosition(snapshot.run.leggerPosition,snapshot.run.leggerRotation);
            population=new GameObject("Expedition population · "+snapshot.run.id);SceneManager.MoveGameObjectToScene(population,gameObject.scene);
            var beaconObject=new GameObject("Root beacon");beaconObject.transform.SetParent(population.transform,false);beacon=beaconObject.AddComponent<CampRootBeaconSession>();beacon.ConfigureExpedition(snapshot.run);beacon.ExtractionCompleted+=Extract;
            RestoreSources(sourceLibrary);
            boundRunId=snapshot.run.id;
        }
        private void RestoreSources(CampExpeditionSourceLibrary sourceLibrary)
        {
            foreach(var node in snapshot.run.nodes)
            {
                var visual=Instantiate(sourceLibrary.Find(node.itemId),population.transform);
                visual.name=visual.SourceName+" · "+Configuration.definition.Find(node.itemId).DisplayName;
                visual.transform.SetPositionAndRotation(node.position,Quaternion.identity);
                sourceViews.Add(visual.Bind(this,node));
            }
        }
        private void ClearField(){boundRunId=null;sourceViews.Clear();if(beacon!=null)beacon.ExtractionCompleted-=Extract;if(population!=null){population.SetActive(false);if(Application.isPlaying)Destroy(population);else DestroyImmediate(population);}population=null;beacon=null;}
        internal bool TryRestoreBeaconState(CampRootBeaconSession target)
        {
            if(ledger==null||fatalRecovery||snapshot?.phase!=CampExpeditionPhase.Exploring||snapshot.run==null
                ||target==null||target!=beacon||target.gameObject.scene!=gameObject.scene
                ||boundRunId!=snapshot.run.id)return false;
            target.RestoreExpeditionState(snapshot.run);
            target.ExtractionCompleted-=Extract;target.ExtractionCompleted+=Extract;
            return true;
        }
        private void Extract(RootBeaconRun run)
        {
            frozen=true;Time.timeScale=0;
            try
            {
                var operation=PrepareExtraction(ledger,run);
                pending=operation;
                if(Apply(operation)){pending=null;FindIn<TopDown3DLeggerRidingController>()?.ReleaseExpeditionTownRider();ClearField();OpenMenu("review");}
                else{view.ShowQuote("Extraction is waiting for a durable save.\n"+message+"\nConfirm retries this same return operation.");OpenMenu("review");}
            }
            catch(Exception error){Fail(error.Message);}
        }
        internal static CampPreparedOperation PrepareExtraction(CampExpeditionLedger ledger,RootBeaconRun run)
        {
            if(run==null||run.Phase!=RootBeaconPhase.Extracted)throw new InvalidOperationException("Complete the beacon departure before returning to outpost.");
            return ledger.Prepare("expedition.extract",(next,_)=>
            {
                if(next.phase!=CampExpeditionPhase.Exploring||next.run?.id!=run.RunId)throw new InvalidOperationException("Beacon run identity changed.");
                next.phase=CampExpeditionPhase.CargoReview;next.run=null;
            });
        }
        public bool TryHarvest(string nodeKey,TopDown3DInventoryState destination,out string failure)
        {
            failure=null;
            if(!CanSimulateField||!nodes.TryGetValue(nodeKey,out var node)||node.remaining<=0||node.requiresTool&&!HasDiggingTool){failure="This source is unavailable or requires a fitted multi-tool.";return false;}
            int carrier=ReferenceEquals(destination,ledger.Booter)?0:ReferenceEquals(destination,ledger.Legger)?1:-1;if(carrier<0){failure="The selected carrier changed.";return false;}
            try
            {
                var operation=ledger.Prepare("source.harvest",(next,drafts)=>
                {var record=next.run.nodes.Single(n=>n.key==nodeKey);if(record.remaining<=0)throw new InvalidOperationException("Source depleted.");var result=drafts[carrier].TryAdd(new TopDown3DItemAmount(record.itemId,1));if(!result.Succeeded)throw new InvalidOperationException(result.Message);record.remaining--;});
                bool success=Apply(operation);failure=success?null:message;return success;
            }
            catch(Exception error){failure=error.Message;return false;}
        }
        private void Command(string key)
        {
            if(waitingRelease||restoring||restoringRider)return;
            if(fatalRecovery && key!="quit" && key!="recover-pending" && key!="recover-backup")return;
            try
            {
                if(key.StartsWith("inspect:",StringComparison.Ordinal))return;
                if(key=="shortcut:west"){PerformQuickTransfer();return;}
                if(key=="shortcut:primary"){CyclePrimary(1);return;}
                if(key=="shortcut:context"){CycleContext(1);return;}
                if(key=="recover-pending"||key=="recover-backup")
                {
                    if(ledger!=null&&!fatalRecovery||saveFile==null)throw new InvalidOperationException("Recovery is only available while the profile is stopped.");
                    if(key=="recover-pending")saveFile.FinishPending();else saveFile.RestoreBackup();
                    if(settings!=null)settings.ExpeditionSettingsClosed-=SettingsClosed;
                    Application.wantsToQuit-=WantsQuit;UnbindMenuCommands();
                    if(input!=null){input.MenuToggleRequested-=ToggleStart;input.InventoryToggleRequested-=ToggleInventory;input.UiCancelRequested-=Back;input.CargoOpenRequested-=OpenCargo;}
                    if(ledger!=null){ledger.Changed-=Committed;ledger.CaptureRuntime=null;}ledger=null;reloadCheckpoint=null;reloadSaveWaiting=false;resumeAfterReload=false;ClearField();fatalRecovery=false;saveRecovery=false;pending=null;
                    if(view!=null){view.Command-=Command;view.gameObject.SetActive(false);Destroy(view.gameObject);view=null;}
                    message=null;Start();return;
                }
                if(key=="play"){BeginRun();return;}
                if(key=="send-all")
                {
                    bool returning=snapshot.phase==CampExpeditionPhase.CargoReview;
                    var manifest=ledger.AllCarriedManifest();
                    if(returning&&manifest.Count==0){Command("continue");return;}
                    var operation=ledger.PrepareStorage(manifest,Configuration.overflowFeePerSlot,returning);
                    if(returning&&operation.CommonsDebit==0){if(Apply(operation))OpenMenu("play");}
                    else Quote(operation);
                    return;
                }
                if(key=="quote-cancel"){if(Settling||saveRecovery){message="A durable save is pending; retry before continuing.";Render();return;}pending=null;view.HideQuote();return;}
                if(key=="quote-confirm")
                {
                    if(pending!=null&&Apply(pending))
                    {bool recovered=saveRecovery;saveRecovery=false;pending=null;view.HideQuote();message=null;
                        if(quitAfterRecovery){quitting=true;Application.Quit();return;}
                        var kind=snapshot.receipts.LastOrDefault()?.kind;
                        if(kind=="expedition.death"){FindIn<TopDown3DLeggerRidingController>()?.ReleaseExpeditionTownRider();ClearField();OpenMenu("play");}
                        else if(kind=="expedition.begin")
                        {try{RestoreField();CloseMenu();}catch(Exception error){Fail(error.Message);}}
                        else if(kind=="equipment.fit"){view.Back();ArmRelease();Render();}
                        else if(kind=="review.store-all"&&snapshot.phase==CampExpeditionPhase.Town){OpenMenu("play");}
                        else if(kind is "project.plan" or "project.bookmark")
                        {frozen=snapshot.phase!=CampExpeditionPhase.Exploring||Settling;SetSimulation();ArmRelease();Render();}
                        else if(snapshot.phase==CampExpeditionPhase.CargoReview){FindIn<TopDown3DLeggerRidingController>()?.ReleaseExpeditionTownRider();ClearField();OpenMenu("review");}
                        else if(recovered&&snapshot.phase==CampExpeditionPhase.Exploring)CloseMenu();else Render();}
                    return;
                }
                if(key=="continue"){if(Apply(ledger.Prepare("review.continue",(next,_)=>next.phase=CampExpeditionPhase.Town)))OpenMenu("play");return;}
                if(key=="transfer"){TransferSelected();return;}
                if(key.StartsWith("equip:",StringComparison.Ordinal)){OpenPicker(key.Substring(6));return;}
                if(key.StartsWith("candidate:",StringComparison.Ordinal)){FitCandidate(int.Parse(key.Substring(10)));return;}
                if(key is "track" or "track-center")
                {ToggleTrackedItem(key=="track-center"?view.TreeTrackingTarget:view.InspectedItem);return;}
                if(key.StartsWith("project:",StringComparison.Ordinal)){ChangeProject(key);return;}
                if(key=="settings"){settings.OpenExpeditionSettings();return;}
                if(key=="quit"){SaveAndQuit();return;}
                if(key.StartsWith("back:",StringComparison.Ordinal)){view.ShowPage("play");return;}
                message="This service is reserved for a later prototype milestone.";Render();
            }
            catch(Exception error){message=error.Message;Render();}
        }
        private void Quote(CampPreparedOperation operation){pending=operation;view.ShowQuote(operation.Description,$"{operation.AdditionalSlots} permanent slots · {operation.CommonsDebit} Commons · balance {checked(snapshot.commons-operation.CommonsDebit)}");ArmRelease();}
        private void ToggleTrackedItem(string id)
        {if(id!=null)Apply(ledger.Prepare("project.bookmark",(next,_)=>next.tracked=next.tracked.Contains(id)?next.tracked.Where(v=>v!=id).ToArray():next.tracked.Concat(new[]{id}).ToArray()));}
        private void TransferSelected()
        {
            view.RefreshFocusedDetails();
            var selected=view.SelectedStack;if(selected==null)throw new InvalidOperationException("Select a cargo item first.");var row=ParseCurrent(selected);
            if(snapshot.phase!=CampExpeditionPhase.Exploring&&row.owner<2){Quote(ledger.PrepareStorage(new[]{row},Configuration.overflowFeePerSlot));return;}
            if(row.owner==2&&snapshot.phase==CampExpeditionPhase.Exploring)throw new InvalidOperationException("Town storage is unavailable in the field.");
            if(snapshot.phase==CampExpeditionPhase.Exploring&&!TopDown3DCargoAccess.CanAccess(cargo,kit.transform))throw new InvalidOperationException("Bring Legger closer to transfer cargo.");
            int target=row.owner==2?1:row.owner==1?0:1;
            var operation=ledger.Prepare("inventory.transfer",(_,drafts)=>{var result=TopDown3DInventoryTransferService.TransferSlot(drafts[row.owner],drafts[target],row.slot);if(!result.Succeeded)throw new InvalidOperationException(result.Message);});Apply(operation);
        }
        private void OpenPicker(string address)
        {
            int target=CampExpeditionLayout.Index(address);if(target<0)return;
            var accessible=ReadStacks().Where(s=>s.owner=="booter"||s.owner=="legger"&&(snapshot.phase!=CampExpeditionPhase.Exploring||TopDown3DCargoAccess.CanAccess(cargo,kit.transform))||s.owner=="storage"&&snapshot.phase!=CampExpeditionPhase.Exploring);
            candidates=accessible.Where(s=>(s.owner!="booter"||string.IsNullOrEmpty(s.equipmentAddress))&&CampExpeditionLayout.Allows(target,Configuration.definition.Find(s.itemId)?.runtimeDefinition)).ToArray();view.ShowPicker(address,candidates);ArmRelease();
        }
        private void FitCandidate(int index)
        {
            if(index<0||index>=candidates.Length)return;int target=CampExpeditionLayout.Index(view.PickerTarget);if(target<0)return;var row=ParseCurrent(candidates[index]);
            if(row.owner==0&&row.slot==target){view.Back();return;}
            if(snapshot.phase==CampExpeditionPhase.Exploring&&(row.owner==2||row.owner==1&&!TopDown3DCargoAccess.CanAccess(cargo,kit.transform)))throw new InvalidOperationException("The selected gear is no longer accessible.");
            var operation=ledger.Prepare("equipment.fit",(_,drafts)=>
            {
                if(row.owner==0){var result=drafts[0].TryMoveOrSwap(row.slot,target);if(!result.Succeeded)throw new InvalidOperationException(result.Message);return;}
                var source=drafts[row.owner];var selected=source.Slots[row.slot].Clone();var outgoing=drafts[0].Slots[target].Clone();
                if(!drafts[0].Slots[target].IsEmpty)
                {var remove=drafts[0].RemoveFromSlot(target,1);if(!remove.Succeeded)throw new InvalidOperationException(remove.Message);}
                var removed=source.RemoveFromSlot(row.slot,1);if(!removed.Succeeded)throw new InvalidOperationException(removed.Message);
                if(!outgoing.IsEmpty){var result=source.TryAdd(new TopDown3DItemAmount(outgoing.ItemId,1,outgoing.InstanceId,outgoing.CanisterContents));if(!result.Succeeded)throw new InvalidOperationException(result.Message);}
                drafts[0].Slots[target].Set(selected.ItemId,1,selected.InstanceId,selected.CanisterContents);
            });
            if(Apply(operation)){view.Back();ArmRelease();}
        }
        private static (int owner,int slot) Parse(CampMenuStackView stack){var parts=stack.key.Split('/');return(int.Parse(parts[0]),int.Parse(parts[1]));}
        private (int owner,int slot) ParseCurrent(CampMenuStackView stack)
        {var row=Parse(stack);if(!ledger.SelectionMatches(row.owner,row.slot,stack.itemId,stack.quantity,stack.instanceId,stack.storeRevision))throw new InvalidOperationException("This item changed. Close the picker and select it again.");return row;}
        private CampMenuStackView[] ReadStacks()
        {
            var rows=new List<CampMenuStackView>();var stores=new[]{ledger.Booter,ledger.Legger,ledger.Storage};var owners=new[]{"booter","legger","storage"};
            for(int o=0;o<3;o++)for(int i=0;i<stores[o].Capacity;i++)
            {
                var slot=stores[o].Slots[i];if(slot.IsEmpty)continue;
                string contents=slot.CanisterContents.IsPresent?$"Contained dust: {slot.CanisterContents.dust}":null;
                var algae=snapshot.equipmentStates.FirstOrDefault(record=>record.instanceId==slot.InstanceId)?.phagia;
                if(algae!=null)
                {if(o==0&&(i is 9 or 10)&&phagia!=null)algae=phagia.CaptureSnapshot();contents=$"Phagia: {algae.remaining:0.0} / {algae.capacity:0.0}";}
                rows.Add(new CampMenuStackView{key=o+"/"+i,slotIndex=i,itemId=slot.ItemId,quantity=slot.Quantity,instanceId=slot.InstanceId,storeRevision=stores[o].Revision,owner=owners[o],contentsSummary=contents,equipmentAddress=o==0?CampExpeditionLayout.Address(i):null});
            }
            return rows.ToArray();
        }
        private void Render()
        {
            if(view==null)return;if(ledger==null){view.Render(new CampMenuPresentation{status=message,recoveryAvailable=true});return;}
            view.SetReducedMotion(settings!=null&&settings.ReducedMotion);
            var demand=CampExpeditionProject.Calculate(Configuration.definition,ledger.Booter,ledger.Legger,ledger.Storage);
            bool town=snapshot.phase!=CampExpeditionPhase.Exploring||Settling,leggerAccessible=town||TopDown3DCargoAccess.CanAccess(cargo,kit.transform);
            var planning=CampMenuProjects.Read(Configuration.definition,ledger.Booter,ledger.Legger,ledger.Storage,snapshot.projects,leggerAccessible,town);
            view.Render(new CampMenuPresentation{cargoReview=snapshot.phase==CampExpeditionPhase.CargoReview,canPlay=!Settling&&!restoringRider&&!fatalRecovery&&vitals.Health>0,recoveryAvailable=fatalRecovery,playLabel=snapshot.phase==CampExpeditionPhase.Exploring?"RESUME":"PLAY",commons=snapshot.commons,storageCapacity=ledger.Storage.Capacity,stacks=ReadStacks(),tracked=snapshot.tracked,discovered=snapshot.discovered,status=message,planning=planning,
                projectOverview=demand.Ready?"T1 materials ready":$"T1 materials · {demand.missing.Values.Sum()} missing",
                townAccess=town,leggerCapacity=ledger.Legger.Capacity,leggerMass=ledger.Legger.CalculateMass(),leggerMaximumMass=ledger.Legger.Policy.MaximumMass,leggerAccessible=leggerAccessible,attachmentMounts=ledger.Booter.Slots[1].ItemId==CampExpeditionDefinition.HarnessId?2:0,occupiedMounts=(ledger.Booter.Slots[9].IsEmpty?0:1)+(ledger.Booter.Slots[10].IsEmpty?0:1)});
        }
        private void OpenMenu(string page){menuOpen=true;view.SetVisible(true);view.ShowPage(page);input.EnterInventoryMode();module.cancel=null;ArmRelease();frozen=snapshot.phase!=CampExpeditionPhase.Exploring||Settling;SetSimulation();Render();}
        private void ArmRelease(){waitingRelease=true;awaitPanNeutral=awaitTreeZoomNeutral=awaitScrollNeutral=true;module.submit=null;module.leftClick=null;input.InputActions.FindAction("UI/Submit",true).Enable();input.InputActions.FindAction("UI/Click",true).Enable();}
        private void CloseMenu(){if(saveRecovery||Settling||fatalRecovery||restoringRider)return;menuOpen=false;RestoreMenuNavigation();view.SetVisible(false);module.submit=submit;module.leftClick=click;module.cancel=cancel;frozen=false;SetSimulation();input.EnterGameplayMode();}
        private bool CanChangeMenu=>snapshot?.phase!=CampExpeditionPhase.CargoReview&&!fatalRecovery&&!restoring&&!restoringRider&&pending==null&&view?.PickerTarget==null;
        private void ToggleStart(){if(settings.IsOpen){settings.Back();return;}if(!CanChangeMenu)return;if(menuOpen&&view.Page=="start"&&snapshot.phase==CampExpeditionPhase.Exploring)CloseMenu();else OpenMenu("start");}
        private void ToggleInventory(){if(settings.IsOpen||!CanChangeMenu||view.IsEditingText)return;if(menuOpen&&view.Page=="inventory"&&snapshot.phase==CampExpeditionPhase.Exploring)CloseMenu();else OpenMenu("inventory");}
        private void OpenCargo(){if(!CanChangeMenu||snapshot.phase==CampExpeditionPhase.Exploring&&!TopDown3DCargoAccess.CanAccess(cargo,kit.transform))return;OpenMenu("inventory");view.ShowInventory("legger");}
        private void Back(){if(settings.IsOpen){settings.Back();return;}if(menuOpen){view.Back();if(menuOpen&&MenuSubmissionHeld())ArmRelease();}}
        private void SettingsClosed(){if(this==null||view==null)return;view.SetLargeText(settings.LargeText);OpenMenu("start");}
        private bool Save(){if(ledger==null)return false;try{return Apply(ledger.Prepare("checkpoint",(_,_)=>{}));}catch(Exception error){message=error.Message;Render();return false;}}
        private void SaveAndQuit(){if(ledger==null||fatalRecovery){quitting=true;Application.Quit();return;}bool old=frozen;frozen=true;Time.timeScale=0;if(!Save()){quitAfterRecovery=saveRecovery;if(!saveRecovery)frozen=old;SetSimulation();return;}quitting=true;Application.Quit();}
        private bool WantsQuit(){if(retired||quitting||ledger==null||fatalRecovery)return true;frozen=true;Time.timeScale=0;if(!Save()){OpenMenu("start");return false;}quitting=true;return true;}
        private void Fail(string error){fatalRecovery=true;message="Expedition recovery required: "+error;frozen=true;Time.timeScale=0;Render();if(view!=null){view.SetVisible(true);view.ShowPage("start");}input?.EnterInventoryMode();Debug.LogError(message,this);}
        private void OnApplicationQuit(){if(!retired&&!quitting&&!fatalRecovery&&ledger!=null)Save();}
        // Run only at lifecycle boundaries. Scene-root enumeration cannot see legacy DontSave
        // populations left outside loaded scenes. Component identity plus the private runtime
        // hierarchy distinguishes these from assets, authored fixtures and unrelated engine roots.
        internal static int ReconcileOrphans()
        {
            var roots=new HashSet<GameObject>();
            foreach(var owner in Resources.FindObjectsOfTypeAll<CampExpeditionSession>())
                if(IsOrphan(owner.gameObject)&&owner.transform.parent==null)
                {
                    owner.retired=true;
                    foreach(var key in sessions.Where(pair=>ReferenceEquals(pair.Value,owner)).Select(pair=>pair.Key).ToArray())sessions.Remove(key);
                    roots.Add(owner.gameObject);
                }
            foreach(var target in Resources.FindObjectsOfTypeAll<CampRootBeaconSession>())
            {
                var root=target.transform.root.gameObject;
                bool standalone=root==target.gameObject;
                bool populationRoot=target.transform.parent==root.transform
                    &&root.name.StartsWith("Expedition population · ",StringComparison.Ordinal)
                    &&root.GetComponentInChildren<TopDown3DPlayerMotor>(true)==null
                    &&root.GetComponentInChildren<TopDown3DBigArmFollower>(true)==null;
                if(!standalone&&!populationRoot)continue;
                bool inactiveLegacyPopulation=populationRoot&&!root.activeSelf
                    &&root.scene.IsValid()&&root.scene.path==CampRootBeaconSession.ScenePath
                    &&(root.hideFlags&HideFlags.DontSave)==HideFlags.DontSave;
                if(!IsOrphan(root)&&!inactiveLegacyPopulation)continue;
                target.Retire();roots.Add(root);
            }
            foreach(var root in roots)
            {
                foreach(var shape in root.GetComponentsInChildren<Collider>(true))shape.enabled=false;
                foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))canvas.enabled=false;
                root.SetActive(false);
#if UNITY_EDITOR
                // These roots are already retired and outside active ownership. Destroy now:
                // the author Editor may not advance its player loop while working in another app.
                DestroyImmediate(root);
#else
                Destroy(root);
#endif
            }
            return roots.Count;
        }
        private static bool IsOrphan(GameObject root)=>root!=null
            &&(root.hideFlags&HideFlags.DontSave)==HideFlags.DontSave
            &&(!root.scene.IsValid()||!root.scene.isLoaded);
        private void OnDestroy()
        {
            bool ownsServices=!retired&&sessions.TryGetValue(gameObject.scene.handle,out var current)&&ReferenceEquals(current,this);
            UnbindMenuCommands(ownsServices);
            Application.wantsToQuit-=WantsQuit;if(input!=null){input.MenuToggleRequested-=ToggleStart;input.InventoryToggleRequested-=ToggleInventory;input.UiCancelRequested-=Back;input.CargoOpenRequested-=OpenCargo;}if(settings!=null)settings.ExpeditionSettingsClosed-=SettingsClosed;
            if(ownsServices&&settings!=null&&settings.UsesExpeditionPresentation){settings.Close();settings.UseExpeditionPresentation(null);}
            if(ledger!=null){ledger.Changed-=Committed;ledger.CaptureRuntime=null;}
            if(!retired){if(view!=null){view.Command-=Command;Destroy(view.gameObject);}ClearField();}
            if(ownsServices){sessions.Remove(gameObject.scene.handle);Time.timeScale=initialTimeScale;}
        }
    }
}
