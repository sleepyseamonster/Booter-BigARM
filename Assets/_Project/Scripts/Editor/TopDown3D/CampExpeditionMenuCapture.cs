using System;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace BooterBigArm.Editor
{
    /// <summary>Non-Play, offscreen presentation proof; never loads the author scene or simulates gameplay.</summary>
    public static partial class CampExpeditionMenuCapture
    {
        [Serializable] private sealed class Measurement {public string file;public int width,height;public bool large;public float minimumTextPixels,minimumNavigationTextPixels,treeScale,crosshairAlpha,inspectorTitleContentHeight,inspectorTitleViewportHeight;public bool completeGraphVisible;public double creationAndRenderMilliseconds;public string[] navigationClips,contentClips,noticeClips;public ContrastSample[] contrast;}
        [Serializable] private sealed class ContrastSample {public string text;public int corePixels;public float minimumRatio;}
        [Serializable] private sealed class Measurements {public string proof="Non-Play disposable presentation fixture; offscreen GPU render, no hardware input or gameplay acceptance.";public Measurement[] captures;}
        private static readonly List<Measurement> measurements=new();
        private static string promptFixture;
        private static bool inventoryFixture;
        private static bool treeBrowserFixture;
        private static bool trackedFixture;
        private static bool projectFixture;
        private static bool projectWorkshopFixture;
        private static bool resourceFixture;
        private static bool settingsFixture;
        private static bool inventoryCompletionFixture;
        [Serializable] private sealed class CostSample
        {public string operation;public int iterations,controlsBefore,controlsAfter,callbacksBefore,callbacksAfter;public double totalMilliseconds,medianMilliseconds,p95Milliseconds;public long threadAllocatedBytes;}
        [Serializable] private sealed class CostReport
        {public string proof="Isolated EditMode CPU paths, no GPU rendering or gameplay; Editor timings are not player frame-time acceptance. Allocation -1 means the host counter is unavailable.";public string editor;public bool catalogReferenceStable,threadAllocationCounterAvailable;public CostSample[] samples;}
        public static void MeasurePresentationCostsFromCli()
        {
            if(!Application.isBatchMode||Application.isPlaying||!File.Exists(".camp-expedition-validation"))throw new InvalidOperationException("Marked non-Play candidate required.");
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-campMenuCostOutput");
            if(index<0||index+1>=args.Length||Path.GetFileName(args[index+1])!=args[index+1]||args[index+1].StartsWith(".")||File.Exists(args[index+1]))throw new ArgumentException("Choose a fresh cost-report filename inside the candidate.");
            var samples=new List<CostSample>();GameObject prefab=null,instance=null,eventRoot=null;CampExpeditionMenuView view=null;CampMenuCatalog catalog=null;
            long probeStart=GC.GetAllocatedBytesForCurrentThread();var allocationProbe=new byte[4096];bool allocationCounter=GC.GetAllocatedBytesForCurrentThread()>probeStart;GC.KeepAlive(allocationProbe);
            int Controls()=>instance==null?0:instance.GetComponentsInChildren<CampMenuControl>(true).Length;
            int Callbacks()=>instance==null?0:instance.GetComponentsInChildren<CampMenuControl>(true).Sum(control=>new[]{"Activated","Browsed"}.Sum(name=>((Delegate)typeof(CampMenuControl).GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(control))?.GetInvocationList().Length??0));
            void Measure(string operation,int iterations,Action action)
            {
                var sample=new CostSample{operation=operation,iterations=iterations,controlsBefore=Controls(),callbacksBefore=Callbacks()};var times=new double[iterations];long allocated=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<iterations;i++){var watch=System.Diagnostics.Stopwatch.StartNew();action();watch.Stop();times[i]=watch.Elapsed.TotalMilliseconds;}
                sample.threadAllocatedBytes=allocationCounter?GC.GetAllocatedBytesForCurrentThread()-allocated:-1;sample.controlsAfter=Controls();sample.callbacksAfter=Callbacks();sample.totalMilliseconds=times.Sum();Array.Sort(times);sample.medianMilliseconds=times[times.Length/2];sample.p95Milliseconds=times[Math.Min(times.Length-1,(int)Math.Ceiling(times.Length*.95)-1)];samples.Add(sample);
            }
            try
            {
                Measure("cold asset and presentation catalog load",1,()=>{prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab");catalog=CampMenuCatalog.Load();});
                eventRoot=new GameObject("Menu cost fixture",typeof(UnityEngine.EventSystems.EventSystem));var focus=eventRoot.GetComponent<UnityEngine.EventSystems.EventSystem>();
                Measure("cold prefab instantiation",1,()=>{instance=UnityEngine.Object.Instantiate(prefab);view=instance.GetComponent<CampExpeditionMenuView>();});
                Measure("first native view initialization",1,()=>view.Initialize(focus));
                var model=new CampMenuPresentation{canPlay=true,townAccess=true,commons=50};
                Measure("first inventory presentation and canvas rebuild",1,()=>{view.SetVisible(true);view.ShowInventory("booter");view.Render(model);Canvas.ForceUpdateCanvases();});
                Measure("warm inventory reopen and canvas rebuild",40,()=>{view.SetVisible(false);view.SetVisible(true);view.ShowInventory("booter");view.Render(model);Canvas.ForceUpdateCanvases();});
                foreach(var page in new[]{"play","storage","tree","tracked","projects","workstations","start","guide","almanac","map"})Measure("warm page "+page+" and canvas rebuild",20,()=>{view.ShowPage(page);view.Render(model);Canvas.ForceUpdateCanvases();});
                Measure("repeated idempotent initialization",50,()=>view.Initialize(focus));
                Measure("measurement clock overhead",1000,()=>{});
                var tick=(Action)typeof(CampExpeditionMenuView).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).CreateDelegate(typeof(Action),view);
                view.ShowInventory("booter");Measure("visible equipment idle presentation tick",1000,tick);view.SetVisible(false);Measure("hidden equipment idle presentation tick",1000,tick);
                File.WriteAllText(args[index+1],JsonUtility.ToJson(new CostReport{editor=Application.unityVersion,catalogReferenceStable=ReferenceEquals(catalog,CampMenuCatalog.Load()),threadAllocationCounterAvailable=allocationCounter,samples=samples.ToArray()},true)+"\n");
            }
            finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);if(eventRoot!=null)UnityEngine.Object.DestroyImmediate(eventRoot);}
        }
        public static void CaptureFromCli()
        {
            if(!Application.isBatchMode || Application.isPlaying || !File.Exists(".camp-expedition-validation"))throw new InvalidOperationException("Marked non-Play candidate required.");
            Directory.CreateDirectory("MenuCaptures");
            measurements.Clear();
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Project/Settings/Rendering/URP/Renderer3D.asset");
            var features=renderer.rendererFeatures.Select(f=>f.isActive).ToArray();
            try
            {
            foreach(var feature in renderer.rendererFeatures)feature.SetActive(false);
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
                foreach(var page in new[]{"play","inventory","legger","storage","tree","tracked","projects","start"}) Capture(size,page,false);
            Capture(new Vector2Int(1280,720),"inventory",true);
            foreach(var page in new[]{"picker","tree-inspected","review","quote","workstations"})Capture(new Vector2Int(1920,1080),page,false);
            foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(2560,1440),new Vector2Int(1024,768)})
                foreach(var page in new[]{"inventory","tree","start"})Capture(size,page,false);
            }
            finally {for(int i=0;i<features.Length;i++)renderer.rendererFeatures[i].SetActive(features[i]);}
            File.WriteAllText("MenuCaptures/layout-measurements.json",JsonUtility.ToJson(new Measurements{captures=measurements.ToArray()},true));
            Debug.Log("Camp expedition menu: 31 non-Play offscreen captures complete.");
        }
        public static void CaptureInputReadinessFromCli()
        {
            if(!Application.isBatchMode||Application.isPlaying||!File.Exists(".camp-expedition-validation"))throw new InvalidOperationException("Marked non-Play candidate required.");
            bool secondPass=Environment.GetCommandLineArgs().Contains("-campInputSecondPass");
            bool completion=Environment.GetCommandLineArgs().Contains("-campInputCompletion");
            bool glyphs=Environment.GetCommandLineArgs().Contains("-campBindingGlyphs");
            bool treeCamera=Environment.GetCommandLineArgs().Contains("-campTreeCamera");
            bool antialiasing=Environment.GetCommandLineArgs().Contains("-campAntialiasing");
            bool workshops=Environment.GetCommandLineArgs().Contains("-campWorkshops");
            bool parity=Environment.GetCommandLineArgs().Contains("-campMenuParityFoundations");
            inventoryFixture=Environment.GetCommandLineArgs().Contains("-campMenuInventoryParity");
            treeBrowserFixture=Environment.GetCommandLineArgs().Contains("-campMenuTreeBrowser");
            trackedFixture=Environment.GetCommandLineArgs().Contains("-campMenuTrackedBrowser");
            projectFixture=Environment.GetCommandLineArgs().Contains("-campMenuProjects");
            projectWorkshopFixture=Environment.GetCommandLineArgs().Contains("-campMenuProjectWorkshops");projectFixture|=projectWorkshopFixture;
            bool facilities=Environment.GetCommandLineArgs().Contains("-campMenuFacilities");projectWorkshopFixture|=facilities;projectFixture|=facilities;
            bool closeout=Environment.GetCommandLineArgs().Contains("-campMenuCloseout");
            bool combinedProjects=Environment.GetCommandLineArgs().Contains("-campMenuProjectsAndWorkshops");projectFixture|=combinedProjects;
            resourceFixture=Environment.GetCommandLineArgs().Contains("-campMenuResources");
            settingsFixture=Environment.GetCommandLineArgs().Contains("-campMenuSettings");
            inventoryCompletionFixture=Environment.GetCommandLineArgs().Contains("-campMenuInventoryCompletion");
            string output=parity?"MenuParityFoundationCaptures":workshops?"MenuWorkshopCaptures":antialiasing?"MenuAntialiasingCaptures":treeCamera?"MenuTreeCameraCaptures":glyphs?"MenuBindingGlyphCaptures":completion?"MenuInputCompletionCaptures":secondPass?"MenuSecondInputPassCaptures":"MenuInputReadinessCaptures";
            var arguments=Environment.GetCommandLineArgs();int outputIndex=Array.IndexOf(arguments,"-campMenuCaptureOutput");
            if(outputIndex>=0){if(outputIndex+1>=arguments.Length||Path.GetFileName(arguments[outputIndex+1])!=arguments[outputIndex+1]||arguments[outputIndex+1].StartsWith("."))throw new ArgumentException("Choose one fresh capture folder inside the candidate.");output=arguments[outputIndex+1];}
            if(Directory.Exists(output))throw new InvalidOperationException("Preserve existing input-readiness captures; choose a fresh candidate output.");
            Directory.CreateDirectory(output);measurements.Clear();
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Project/Settings/Rendering/URP/Renderer3D.asset");
            var features=renderer.rendererFeatures.Select(f=>f.isActive).ToArray();
            try
            {
                foreach(var feature in renderer.rendererFeatures)feature.SetActive(false);
                if(closeout)
                {
                    promptFixture="XInputControllerWindows";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"play","inventory","legger","storage","workstations","tree","tracked","projects","start","guide","almanac","map","settings-home"})
                        {resourceFixture=page is "guide" or "almanac" or "map";settingsFixture=page=="settings-home";Capture(size,page,large,output);resourceFixture=settingsFixture=false;}
                    foreach(var size in new[]{new Vector2Int(1920,888),new Vector2Int(1600,900),new Vector2Int(2560,1440),new Vector2Int(1024,768)})foreach(var page in new[]{"inventory","tree","start"})Capture(size,page,true,output);
                    projectFixture=projectWorkshopFixture=true;foreach(var page in new[]{"workshop-plan","workshop-proposal","workshop-last"})Capture(new Vector2Int(1280,720),page,true,output);Capture(new Vector2Int(1024,768),"workshop-proposal",true,output);projectFixture=projectWorkshopFixture=false;
                    Capture(new Vector2Int(1280,720),"start-recovery",true,output);Capture(new Vector2Int(1024,768),"start-recovery",true,output);
                }
                else if(facilities)
                {
                    promptFixture="XInputControllerWindows";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"workshop-plan","workshop-field","workshop-dust","workshop-cooking","workshop-proposal","workshop-last"})Capture(size,page,large,output);
                    foreach(var size in new[]{new Vector2Int(1920,888),new Vector2Int(1600,900),new Vector2Int(2560,1440),new Vector2Int(1024,768)})Capture(size,"workshop-proposal",true,output);
                }
                else if(inventoryCompletionFixture)
                {
                    promptFixture="XInputControllerWindows";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"pockets-browse","pockets-last","pockets-unassigned","pockets-zero","pockets-huge","storage-filter","storage-sort","storage-no-results"})Capture(size,page,large,output);
                }
                else if(settingsFixture)
                {
                    promptFixture="XInputControllerWindows";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"settings-home","settings-draft","settings-discard","settings-radial","settings-radial-advanced","settings-controls","settings-listening","settings-binding-error","settings-camera","settings-readonly","settings-save-error","settings-long"})Capture(size,page,large,output);
                }
                else if(resourceFixture)
                {
                    promptFixture="XInputControllerWindows";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"guide","guide-search","guide-empty","almanac","almanac-known","almanac-empty","almanac-long","map","map-waypoint"})Capture(size,page,large,output);
                }
                else if(combinedProjects)
                {
                    promptFixture="XInputControllerWindows";projectWorkshopFixture=false;
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"projects-plans","projects-unqualified","projects-empty"})Capture(size,page,large,output);
                    foreach(var page in new[]{"projects-field","projects-accounted","projects-long","projects-invalid"})Capture(new Vector2Int(1280,720),page,true,output);
                    projectWorkshopFixture=true;
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"workshop-plan","workshop-field","workshop-unqualified","workshop-tracked"})Capture(size,page,large,output);
                }
                else if(projectWorkshopFixture)
                {
                    promptFixture="XInputControllerWindows";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"workshop-plan","workshop-field","workshop-unqualified","workshop-tracked"})Capture(size,page,large,output);
                }
                else if(projectFixture)
                {
                    promptFixture="XInputControllerWindows";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"projects-plans","projects-unqualified","projects-empty"})Capture(size,page,large,output);
                    foreach(var page in new[]{"projects-field","projects-accounted","projects-long","projects-invalid"})Capture(new Vector2Int(1280,720),page,true,output);
                }
                else if(trackedFixture)
                {
                    promptFixture="XInputControllerWindows";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"tracked-populated","tracked-filtered","tracked-empty"})Capture(size,page,large,output);
                    foreach(var page in new[]{"tracked-last-row","tracked-unknown"})Capture(new Vector2Int(1280,720),page,true,output);
                }
                else if(treeBrowserFixture)
                {
                    promptFixture="Keyboard";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})
                        foreach(var page in new[]{"tree-library","tree-catalog","tree-related"})Capture(size,page,large,output);
                    Capture(new Vector2Int(1280,720),"tree-empty",true,output);
                }
                else if(inventoryFixture)
                {
                    promptFixture="Keyboard";
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})foreach(bool large in new[]{false,true})Capture(size,"inventory",large,output);
                    foreach(var page in new[]{"carried","legger","storage","picker"})Capture(new Vector2Int(1280,720),page,true,output);
                }
                else if(parity)
                {
                    promptFixture="Keyboard";Capture(new Vector2Int(1920,1080),"picker",false,output);Capture(new Vector2Int(1280,720),"picker",true,output);
                    Capture(new Vector2Int(1280,720),"save-retry",true,output);Capture(new Vector2Int(1920,1080),"storage",false,output);
                }
                else if(workshops)
                {
                    foreach(var layout in new[]{"Keyboard","XInputControllerWindows"})
                    {promptFixture=layout;Capture(new Vector2Int(1280,720),"workstations",true,output);Capture(new Vector2Int(1920,1080),"workstations",false,output);}
                }
                else if(antialiasing)
                {
                    foreach(var layout in new[]{"Keyboard","DualSenseGamepadHID","XInputControllerWindows"})
                    {promptFixture=layout;Capture(new Vector2Int(1280,720),"storage",true,output);Capture(new Vector2Int(1920,1080),"inventory",false,output);}
                }
                else if(treeCamera)
                {
                    promptFixture="Keyboard";Capture(new Vector2Int(1280,720),"tree-fit",true,output);Capture(new Vector2Int(1280,720),"tree-camera",true,output);
                    promptFixture="DualSenseGamepadHID";Capture(new Vector2Int(1280,720),"tree-camera",true,output);Capture(new Vector2Int(1280,720),"tree-edge",true,output);
                    promptFixture="XInputControllerWindows";Capture(new Vector2Int(1920,1080),"tree-fit",false,output);Capture(new Vector2Int(1920,1080),"tree-camera",false,output);
                }
                else if(glyphs)
                {
                    foreach(var layout in new[]{"Keyboard","DualSenseGamepadHID","XInputControllerWindows"})
                    {promptFixture=layout;Capture(new Vector2Int(1280,720),"storage",true,output);Capture(new Vector2Int(1280,720),"tree",true,output);}
                    promptFixture="XInputControllerWindows";Capture(new Vector2Int(1920,1080),"inventory",false,output);Capture(new Vector2Int(1280,720),"play",false,output);
                }
                else if(completion)
                {
                    foreach(var layout in new[]{"Keyboard","DualSenseGamepadHID","XInputControllerWindows"})
                    {promptFixture=layout;Capture(new Vector2Int(1280,720),"tree",true,output);Capture(new Vector2Int(1280,720),"storage",true,output);}
                    promptFixture="DualSenseGamepadHID";Capture(new Vector2Int(1280,720),"picker",true,output);
                    promptFixture="Keyboard";Capture(new Vector2Int(1920,1080),"picker",false,output);
                }
                else if(secondPass)
                {
                    Capture(new Vector2Int(1280,720),"storage",false,output);
                    Capture(new Vector2Int(1280,720),"storage",true,output);
                    Capture(new Vector2Int(1280,720),"review",true,output);
                    Capture(new Vector2Int(1920,1080),"picker",false,output);
                }
                else
                {
                    Capture(new Vector2Int(1920,1080),"play",false,output);
                    Capture(new Vector2Int(1280,720),"play",false,output);
                    Capture(new Vector2Int(1920,1080),"picker",false,output);
                    Capture(new Vector2Int(1280,720),"inventory",true,output);
                }
            }
            finally{promptFixture=null;inventoryFixture=treeBrowserFixture=trackedFixture=projectFixture=projectWorkshopFixture=false;for(int i=0;i<features.Length;i++)renderer.rendererFeatures[i].SetActive(features[i]);}
            File.WriteAllText(output+"/layout-measurements.json",JsonUtility.ToJson(new Measurements{captures=measurements.ToArray()},true));
            Debug.Log("Camp expedition input readiness: "+measurements.Count+" non-Play offscreen captures complete.");
        }
        private static void Capture(Vector2Int size,string page,bool large,string output="MenuCaptures")
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var previous=RenderTexture.active;
            var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();
            Texture2D pixels=null;
            GameObject inputObject=null;InputActionAsset inputActions=null;Gamepad fixturePad=null,previousPad=Gamepad.current;Keyboard fixtureKeyboard=null,previousKeyboard=Keyboard.current;
            UnityEngine.EventSystems.EventSystem focus=null;
            SettingsCaptureHarness settingsHarness=null;
            CampExpeditionDefinition inventoryDefinition=null;
            try
            {
                var cameraObject=new GameObject("Offscreen menu camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var camera=cameraObject.GetComponent<Camera>();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=size.y/2f;camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.cullingMask=1<<5;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CampExpeditionAuthoring.PrefabPath),scene);
                // Match the mutable runtime clone; prefab-instance parenting is forbidden in Edit Mode.
                PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var transform in instance.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=5;
                var canvas=instance.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.pixelPerfect=true;
                if(promptFixture!=null)
                {
                    focus=new GameObject("Offscreen presentation focus").AddComponent<UnityEngine.EventSystems.EventSystem>();
                    typeof(UnityEngine.EventSystems.EventSystem).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(focus,null);
                    UnityEngine.EventSystems.EventSystem.current=focus;
                }
                var view=instance.GetComponent<CampExpeditionMenuView>();var inventoryCatalog=inventoryCompletionFixture?InventoryCompletionCatalog(view,page,out inventoryDefinition):null;view.Initialize(focus,inventoryCatalog);
                if(promptFixture!=null)
                {
                    fixtureKeyboard=InputSystem.AddDevice<Keyboard>();if(promptFixture!="Keyboard")fixturePad=(Gamepad)InputSystem.AddDevice(promptFixture);
                    inputActions=AnimationPlaygroundInputFactory.Create(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
                    inputActions.devices=fixturePad==null?new InputDevice[]{fixtureKeyboard}:new InputDevice[]{fixtureKeyboard,fixturePad};
                    inputObject=new GameObject("Inactive prompt fixture");inputObject.SetActive(false);var router=inputObject.AddComponent<TopDown3DInputRouter>();router.Configure(inputActions);
                    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    typeof(TopDown3DInputRouter).GetField("ownedAnimationActions",flags).SetValue(router,inputActions);router.PrepareExpeditionMenuActions();
                    typeof(TopDown3DInputRouter).GetMethod("SetPromptDevice",flags).Invoke(router,new object[]{fixturePad==null?TopDown3DPromptDevice.KeyboardMouse:TopDown3DPromptDevice.Gamepad});view.BindInputRouter(router);
                }
                view.Render(new CampMenuPresentation{canPlay=true,commons=50,storageCapacity=24,tracked=new[]{"author.bio.stone_bone"},stacks=new[]{
                    new CampMenuStackView{key="fixture-tool",itemId=CampExpeditionDefinition.ToolZeroId,owner="booter",quantity=1,equipmentAddress="tool"},
                    new CampMenuStackView{key="fixture-frame",itemId=CampExpeditionDefinition.HarnessId,owner="booter",quantity=1,equipmentAddress="frame"},
                    new CampMenuStackView{key="fixture-phagia",itemId=CampExpeditionDefinition.PhagiaId,owner="booter",quantity=1,equipmentAddress="attachment-0"}}});
                if(inventoryFixture)
                {
                    var stacks=new List<CampMenuStackView>{new(){key="0/8",slotIndex=8,owner="booter",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1,equipmentAddress="tool"},new(){key="0/1",slotIndex=1,owner="booter",itemId=CampExpeditionDefinition.HarnessId,quantity=1,equipmentAddress="frame"},new(){key="0/9",slotIndex=9,owner="booter",itemId=CampExpeditionDefinition.PhagiaId,quantity=1,equipmentAddress="attachment-0",contentsSummary="Phagia: 14.5 / 80.0"},new(){key="0/14",slotIndex=14,owner="booter",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}};
                    foreach(var carrier in new[]{"legger","storage"})for(int i=0;i<12;i++)stacks.Add(new CampMenuStackView{key=carrier+"/"+i,slotIndex=i,owner=carrier,itemId=i%2==0?"resource.ironstone_ore":CampExpeditionDefinition.ToolZeroId,quantity=i%2==0?i+1:1});
                    view.Render(new CampMenuPresentation{stacks=stacks.ToArray(),canPlay=true,commons=50,storageCapacity=24,leggerMass=37.5f,leggerMaximumMass=92,leggerAccessible=false});
                }
                if(inventoryCompletionFixture)ShowInventoryCompletion(view,inventoryCatalog,page);
                else if(settingsFixture)settingsHarness=new SettingsCaptureHarness(view,focus,inputObject.GetComponent<TopDown3DInputRouter>(),inputActions,output,page,large);
                else if(resourceFixture)
                {
                    var known=page=="almanac-long"?new[]{CampMenuCatalog.Load().Items.OrderByDescending(item=>item.name.Length).First().id}:new[]{CampExpeditionDefinition.ToolZeroId,CampExpeditionDefinition.HarnessId,CampExpeditionDefinition.PhagiaId};
                    view.Render(new CampMenuPresentation{discovered=known,canPlay=true,commons=50,stacks=known.Select((id,index)=>new CampMenuStackView{key="resource-fixture-"+index,itemId=id,owner="booter",quantity=1}).ToArray()});
                    view.ShowPage(page.StartsWith("guide",StringComparison.Ordinal)?"guide":page.StartsWith("almanac",StringComparison.Ordinal)?"almanac":"map");
                    if(page is "guide-search" or "guide-empty")instance.GetComponentsInChildren<UnityEngine.UI.InputField>().Single(f=>f.name=="Guide search").text=page=="guide-search"?"Phagia algae":"no_chapter_123";
                    if(page is "almanac-known" or "almanac-empty")instance.GetComponentsInChildren<UnityEngine.UI.InputField>().Single(f=>f.name=="Almanac search").text=page=="almanac-known"?CampMenuCatalog.Load().Find(CampExpeditionDefinition.ToolZeroId).name:"no_discovered_record_123";
                    if(page=="almanac-long")instance.GetComponentsInChildren<UnityEngine.UI.InputField>().Single(f=>f.name=="Almanac search").text=CampMenuCatalog.Load().Find(known[0]).name;
                    if(page=="map-waypoint")foreach(var key in new[]{"map-zoom-in","map-zoom-in","map-marker:1","map-waypoint"})instance.GetComponentsInChildren<CampMenuControl>().Single(c=>c.key==key).Button.onClick.Invoke();
                }
                else if(page=="legger")view.ShowInventory("legger");
                else if(projectFixture)
                {
                    var definition=view.definition;var catalog=definition.itemCatalog;
                    var booter=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,CampExpeditionLayout.Capacity,expeditionLayout:true));var cargo=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12));var storage=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,96,campStorage:true));
                    cargo.TryAdd(new TopDown3DItemAmount("author.bio.stone_bone",1));storage.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore",2));
                    var plans=page=="projects-empty"?Array.Empty<CampProjectRecord>():page=="projects-long"?CampMenuCatalog.Load().Trees.Select(tree=>new CampProjectRecord{rootId=tree.rootId,remaining=2}).ToArray():page=="projects-unqualified"?new[]{new CampProjectRecord{rootId="author.tool.arc_powered_rotary_percussion_mining_tool",remaining=3},new CampProjectRecord{rootId="retired.saved.recipe",remaining=2}}:new[]{new CampProjectRecord{rootId=definition.rootId,remaining=2},new CampProjectRecord{rootId=CampExpeditionDefinition.ToolZeroId,remaining=1}};
                    if(page=="projects-accounted")plans=new[]{new CampProjectRecord{rootId=definition.rootId,remaining=2}};
                    if(projectWorkshopFixture)plans=page=="workshop-tracked"?new[]{new CampProjectRecord{rootId=definition.rootId,remaining=2}}:Array.Empty<CampProjectRecord>();
                    if(page=="projects-accounted")
                    {var demand=CampExpeditionProject.Calculate(definition,booter,cargo,storage,plans);foreach(var row in demand.total){var result=storage.TryAdd(new TopDown3DItemAmount(row.Key,row.Value));if(!result.Succeeded)throw new InvalidOperationException("Project fixture stock: "+result.Message);}}
                    bool accessible=!page.EndsWith("-field",StringComparison.Ordinal);view.Render(new CampMenuPresentation{planning=CampMenuProjects.Read(definition,booter,cargo,storage,plans,accessible,accessible),commons=50,canPlay=true,townAccess=accessible});
                    if(projectWorkshopFixture)view.OpenWorkshopRecipe(page=="workshop-unqualified"?"author.tool.arc_powered_rotary_percussion_mining_tool":definition.rootId);else view.ShowPage("projects");
                    string facility=page switch{"workshop-dust"=>"author.station.dust_refinery","workshop-cooking"=>"camp-menu.cooking","workshop-proposal"=>"proposal.station.contained_chemical_processing_station","workshop-last"=>definition.stations.Last().id,_=>null};
                    if(facility!=null){var row=instance.GetComponentsInChildren<CampMenuControl>(true).Single(control=>control.key=="station:"+facility);row.Button.onClick.Invoke();focus.SetSelectedGameObject(row.gameObject);}
                }
                else if(trackedFixture)
                {
                    var catalog=CampMenuCatalog.Load();var ids=page=="tracked-empty"?Array.Empty<string>():page=="tracked-unknown"?new[]{"retired.item.saved.bookmark"}:page=="tracked-last-row"?catalog.Items.Select(item=>item.id).ToArray():catalog.Inputs(CampExpeditionDefinition.ToolOneId,CampExpeditionDefinition.ToolOneId).Concat(new[]{CampExpeditionDefinition.ToolOneId}).ToArray();
                    view.Render(new CampMenuPresentation{tracked=ids,canPlay=true,commons=50});view.ShowPage("tracked");if(page=="tracked-filtered")view.SelectTrackedFilter(CampExpeditionDefinition.ToolOneId);
                }
                else if(treeBrowserFixture)
                {
                    view.ShowPage("tree");var catalog=CampMenuCatalog.Load();var search=instance.GetComponentsInChildren<UnityEngine.UI.InputField>(true).Single(field=>field.name=="Tree search");
                    if(page=="tree-library")search.text=catalog.Tree(CampExpeditionDefinition.ToolOneId).family;
                    else if(page=="tree-empty")search.text="no_such_crafting_tree_123";
                    else{const string id="author.tool.arc_powered_rotary_percussion_mining_tool";view.OpenDisplayTree(id);search.text=catalog.Tree(id).family;}
                }
                else if(page=="start-recovery"){view.Render(new CampMenuPresentation{recoveryAvailable=true,status="Profile could not be loaded. Original and recovery files are preserved."});view.ShowPage("start");}
                else if(page=="carried")view.ShowInventory("booter");
                else if(page=="picker"){view.ShowInventory("booter");view.ShowPicker("tool",Enumerable.Range(0,promptFixture==null?1:40).Select(i=>new CampMenuStackView{key="fixture-tool-"+i,itemId=CampExpeditionDefinition.ToolZeroId,owner=promptFixture==null?"booter":"storage",quantity=1,equipmentAddress=promptFixture==null?"tool":null}).ToArray());}
                else if(page=="save-retry"){view.ShowPage("storage");view.ShowQuote("The save could not be acknowledged. Retry preserves the same operation and the expedition stays frozen.","No additional items or Commons are committed until this save succeeds.","SAVE WAITING","Retry save",false);}
                else if(page=="tree-inspected"){view.ShowPage("tree");view.Inspect("author.bio.stone_bone");}
                else if(page is "tree-fit" or "tree-camera" or "tree-edge")view.ShowPage("tree");
                else if(page=="quote"){view.ShowPage("review");view.ShowQuote("Booter · Utility harness\nBooter · Phagia Canister\nBooter · Bone salvage multi-tool\n\nAll three selected items will be sent to storage together.","0 additional slots · 0 Commons · balance 50");}
                else view.ShowPage(page);
                if(page=="tree"&&Environment.GetCommandLineArgs().Contains("-campMenuCloseout"))
                {instance.GetComponentsInChildren<UnityEngine.UI.InputField>(true).Single(field=>field.name=="Tree search").text="Iron-headed multi-tool";view.OpenDisplayTree(view.definition.rootId,true);focus.SetSelectedGameObject(instance.GetComponentsInChildren<CampMenuControl>(true).Single(control=>control.key=="node:"+view.definition.rootId).gameObject);}
                if(page=="tracked")view.Inspect("author.bio.stone_bone");view.SetLargeText(large);
                Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();
                if(page=="start-recovery"){focus.SetSelectedGameObject(instance.GetComponentsInChildren<CampMenuControl>().Single(control=>control.key=="recover-backup").gameObject);Canvas.ForceUpdateCanvases();}
                if(page is "workshop-dust" or "workshop-cooking" or "workshop-proposal" or "workshop-last")
                {string id=page switch{"workshop-dust"=>"author.station.dust_refinery","workshop-cooking"=>"camp-menu.cooking","workshop-proposal"=>"proposal.station.contained_chemical_processing_station",_=>view.definition.stations.Last().id};focus.SetSelectedGameObject(null);focus.SetSelectedGameObject(instance.GetComponentsInChildren<CampMenuControl>().Single(control=>control.key=="station:"+id).gameObject);Canvas.ForceUpdateCanvases();}
                if(inventoryCompletionFixture)FocusInventoryCompletion(view,focus,page);
                if(page=="tracked-last-row")
                {
                    var grid=instance.GetComponentsInChildren<CampMenuControl>().Single(control=>control.key=="tracked-grid").GetComponent<UnityEngine.UI.ScrollRect>();grid.content.anchoredPosition=new Vector2(0,grid.content.rect.height-grid.viewport.rect.height);grid.onValueChanged.Invoke(Vector2.zero);focus.SetSelectedGameObject(instance.GetComponentsInChildren<CampMenuControl>().Single(control=>control.key=="bookmark:"+(view.TrackedVisibleIds.Length-1)).gameObject);Canvas.ForceUpdateCanvases();
                }
                if(page=="projects-long")
                {var last=instance.GetComponentsInChildren<CampMenuControl>().Last(control=>control.key.StartsWith("project:remove:",StringComparison.Ordinal));focus.SetSelectedGameObject(last.gameObject);Canvas.ForceUpdateCanvases();}
                if(page=="projects-invalid")
                {instance.GetComponentsInChildren<UnityEngine.UI.InputField>().Single(field=>field.name=="Project quantity "+view.definition.rootId).onEndEdit.Invoke("0");Canvas.ForceUpdateCanvases();}
                if(page=="carried"){focus.SetSelectedGameObject(instance.GetComponentsInChildren<CampMenuControl>(true).Single(c=>c.key=="carried:3").gameObject);Canvas.ForceUpdateCanvases();}
                if(page=="tree-related")
                {
                    var tree=view.DisplayedTree;var selected=tree.nodes.FirstOrDefault(node=>node.id!=tree.rootId&&node.inputs.Length>0&&catalogArt(node.id))??tree.nodes.First(node=>node.id==tree.rootId);
                    bool catalogArt(string id)=>!string.IsNullOrEmpty(CampMenuCatalog.Load().Find(id)?.artResource);
                    var cameraTree=view.TreeCanvas;cameraTree.Zoom(1/cameraTree.Scale);var viewport=(RectTransform)cameraTree.transform;var center=(viewport.rect.size*.5f-cameraTree.Offset)/cameraTree.Scale;
                    cameraTree.Pan((center-new Vector2(selected.x+72.5f,selected.y+119))*cameraTree.Scale);cameraTree.OnSubmit(new UnityEngine.EventSystems.BaseEventData(focus));view.Render(new CampMenuPresentation{canPlay=true,commons=50,tracked=new[]{selected.id}});cameraTree.AdvanceVisual(.1f);Canvas.ForceUpdateCanvases();
                }
                if(page is "tree-camera" or "tree-edge")
                {
                    var tree=view.TreeCanvas;tree.Zoom(1/tree.Scale);var viewport=(RectTransform)tree.transform;
                    var item=view.definition.items.Single(i=>i.id==view.definition.rootId);
                    var center=(viewport.rect.size*.5f-tree.Offset)/tree.Scale;
                    tree.Pan((center-new Vector2(item.treeX+72.5f,item.treeY+119))*tree.Scale);
                    if(page=="tree-edge")tree.Pan(Vector2.one*100000);
                    else view.Inspect(item.id);
                    tree.AdvanceVisual(.1f);
                }
                foreach(var mask in instance.GetComponentsInChildren<UnityEngine.UI.RectMask2D>()){UnityEngine.UI.MaskUtilities.Notify2DMaskStateChanged(mask);mask.PerformClipping();}
                camera.Render();RenderTexture.active=target;
                pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                string filename=$"{page}-{size.x}x{size.y}{(large?"-large":"")}{(promptFixture==null?"":"-"+promptFixture)}.png";File.WriteAllBytes(output+"/"+filename,pixels.EncodeToPNG());
                var texts=instance.GetComponentsInChildren<UnityEngine.UI.Text>().Where(t=>t.enabled&&!string.IsNullOrWhiteSpace(t.text)).ToArray();
                var clips=new List<string>();foreach(var control in instance.GetComponentsInChildren<CampMenuControl>().Where(c=>c.tab||c.prompt))
                {var title=control.title;if(title.preferredHeight>title.rectTransform.rect.height+1)clips.Add(control.key+" vertical");var corners=new Vector3[4];title.rectTransform.GetWorldCorners(corners);if(corners.Any(p=>camera.WorldToScreenPoint(p).x < -1||camera.WorldToScreenPoint(p).x>size.x+1))clips.Add(control.key+" horizontal");}
                var treeCanvas=view.TreeCanvas;var stage=treeCanvas.transform.Find("Recipe canvas");
                var contentClips=instance.GetComponentsInChildren<CampMenuControl>().Where(c=>!c.tab&&!c.prompt&&!c.transform.IsChildOf(stage)).SelectMany(c=>new[]{(text:c.title,label:c.key+" title"),(text:c.subtitle,label:c.key+" subtitle"),(text:c.cornerLabel,label:c.key+" corner label")}.Where(row=>row.text!=null&&row.text.preferredHeight>row.text.rectTransform.rect.height+1).Select(row=>row.label)).ToArray();
                var headerClips=new List<string>();foreach(var text in texts.Where(text=>text.name is "Tree heading" or "Tree input counts")){var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds((RectTransform)treeCanvas.transform,text.transform);if(bounds.min.x<((RectTransform)treeCanvas.transform).rect.xMin-1||bounds.max.x>((RectTransform)treeCanvas.transform).rect.xMax+1||text.preferredHeight>text.rectTransform.rect.height+1)headerClips.Add(text.name+" column");}contentClips=contentClips.Concat(headerClips).ToArray();
                var noticeClips=new List<string>();if(view.RevealedSettingsNotice is UnityEngine.UI.Text notice){var scroll=notice.GetComponentInParent<UnityEngine.UI.ScrollRect>();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,notice.transform);var viewport=scroll.viewport.rect;if(bounds.size.y<=viewport.height-24&&(bounds.min.y<viewport.yMin+10||bounds.max.y>viewport.yMax-10))noticeClips.Add(notice.name+" inset");}
                var viewportRect=(RectTransform)treeCanvas.transform;
                bool graphVisible=instance.GetComponentsInChildren<CampMenuControl>().Where(c=>c.key.StartsWith("tree-node:",StringComparison.Ordinal)).All(c=>{var corners=new Vector3[4];((RectTransform)c.transform).GetWorldCorners(corners);return corners.All(p=>{var v=viewportRect.InverseTransformPoint(p);return v.x>=viewportRect.rect.xMin-.1f&&v.x<=viewportRect.rect.xMax+.1f&&v.y>=viewportRect.rect.yMin-.1f&&v.y<=viewportRect.rect.yMax+.1f;});});
                var contrast=Environment.GetCommandLineArgs().Contains("-campMenuContrast")?MeasureRenderedContrast(instance,camera,pixels,texts,size):null;
                watch.Stop();measurements.Add(new Measurement{file=filename,width=size.x,height=size.y,large=large,minimumTextPixels=texts.Min(t=>t.fontSize*t.rectTransform.lossyScale.y),minimumNavigationTextPixels=texts.Where(t=>!t.transform.IsChildOf(stage)).Min(t=>t.fontSize*t.rectTransform.lossyScale.y),treeScale=treeCanvas.Scale,crosshairAlpha=treeCanvas.CrosshairAlpha,completeGraphVisible=graphVisible,navigationClips=clips.ToArray(),contentClips=contentClips,noticeClips=noticeClips.ToArray(),contrast=contrast,creationAndRenderMilliseconds=watch.Elapsed.TotalMilliseconds,inspectorTitleContentHeight=view.inspectorTitle.rectTransform.rect.height,inspectorTitleViewportHeight=((RectTransform)view.inspectorTitle.transform.parent).rect.height});
            }
            finally
            {
                settingsHarness?.Dispose();
                if(focus!=null){typeof(UnityEngine.EventSystems.EventSystem).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(focus,null);UnityEngine.Object.DestroyImmediate(focus.gameObject);}
                if(inputObject!=null)UnityEngine.Object.DestroyImmediate(inputObject);if(inputActions!=null)UnityEngine.Object.DestroyImmediate(inputActions);
                if(fixturePad!=null)InputSystem.RemoveDevice(fixturePad);if(fixtureKeyboard!=null)InputSystem.RemoveDevice(fixtureKeyboard);
                if(previousPad!=null&&previousPad.added)previousPad.MakeCurrent();if(previousKeyboard!=null&&previousKeyboard.added)previousKeyboard.MakeCurrent();
                if(inventoryDefinition!=null)UnityEngine.Object.DestroyImmediate(inventoryDefinition);
                RenderTexture.active=previous;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);
            }
        }
        private static ContrastSample[] MeasureRenderedContrast(GameObject instance,Camera camera,Texture2D pixels,UnityEngine.UI.Text[] texts,Vector2Int size)
        {
            var original=pixels.GetPixels32();var graphics=instance.GetComponentsInChildren<UnityEngine.UI.Graphic>().Where(graphic=>graphic.enabled&&graphic is not UnityEngine.UI.Text).ToArray();var colors=texts.Select(text=>text.color).ToArray();var clear=camera.backgroundColor;
            Color32[] Read(){Canvas.ForceUpdateCanvases();camera.Render();pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();return pixels.GetPixels32();}
            Color32[] mask,background;
            try
            {
                foreach(var graphic in graphics)graphic.enabled=false;foreach(var text in texts)text.color=Color.white;camera.backgroundColor=Color.black;mask=Read();
                foreach(var graphic in graphics)graphic.enabled=true;foreach(var text in texts)text.color=Color.clear;camera.backgroundColor=clear;background=Read();
            }
            finally{foreach(var graphic in graphics)graphic.enabled=true;for(int i=0;i<texts.Length;i++)texts[i].color=colors[i];camera.backgroundColor=clear;Canvas.ForceUpdateCanvases();}
            float Linear(byte channel){float value=channel/255f;return value<=.04045f?value/12.92f:Mathf.Pow((value+.055f)/1.055f,2.4f);}
            float Luminance(Color32 color)=>.2126f*Linear(color.r)+.7152f*Linear(color.g)+.0722f*Linear(color.b);
            var results=new List<ContrastSample>();
            foreach(var text in texts)
            {
                // Disabled actions are unavailable controls; their visual affordance is
                // measured separately from the required reading-content contrast floor.
                var selectable=text.GetComponentInParent<UnityEngine.UI.Selectable>();if(selectable!=null&&!selectable.IsInteractable()&&selectable.GetComponent<CampMenuControl>()?.prompt!=true)continue;
                var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);var screen=corners.Select(camera.WorldToScreenPoint).ToArray();int x0=Mathf.Clamp(Mathf.FloorToInt(screen.Min(point=>point.x)),0,size.x),x1=Mathf.Clamp(Mathf.CeilToInt(screen.Max(point=>point.x)),0,size.x),y0=Mathf.Clamp(Mathf.FloorToInt(screen.Min(point=>point.y)),0,size.y),y1=Mathf.Clamp(Mathf.CeilToInt(screen.Max(point=>point.y)),0,size.y);int count=0;float minimum=float.PositiveInfinity;
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
                {int index=y*size.x+x;var coverage=mask[index];if(coverage.r<250||coverage.g<250||coverage.b<250)continue;var front=original[index];var back=background[index];if(front.r==back.r&&front.g==back.g&&front.b==back.b)continue;float a=Luminance(front),b=Luminance(back);minimum=Mathf.Min(minimum,(Mathf.Max(a,b)+.05f)/(Mathf.Min(a,b)+.05f));count++;}
                if(count>0)results.Add(new ContrastSample{text=text.text,corePixels=count,minimumRatio=minimum});
            }
            return results.ToArray();
        }
    }
}
