using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    public static class CampIronstoneVisualProof
    {
        private static object Call(object target,string name,params object[] args) => target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        private static void Field(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        public static void CaptureFromCli()
        {
            if(!Application.isBatchMode || Application.isPlaying) throw new InvalidOperationException("Background Edit Mode proof only.");
            var scene=EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene);
            var session=CampWorkstationSceneAuthoring.All<TopDown3DCampLoopEconomySession>(scene).Single();
            var motor=session.Kit.GetComponent<TopDown3DPlayerMotor>();
            motor.Configure(CampWorkstationSceneAuthoring.All<TopDown3DInputRouter>(scene).Single(),CampWorkstationSceneAuthoring.All<Camera>(scene).Single().transform);
            var lab=TopDown3DAnimationPlaygroundSession.Find(scene);
            if(!lab.CheckDependencies())throw new InvalidOperationException(lab.UnavailableReason);
            var preferences=Path.Combine(Path.GetTempPath(),"IronstoneCapture-"+Guid.NewGuid().ToString("N"));
            TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene,preferences);
            lab.Advance(AnimationPlaygroundReadiness.ServicesBound); lab.Advance(AnimationPlaygroundReadiness.VisualReady);
            if(!session.Initialize())throw new InvalidOperationException(session.FailureReason);
            TopDown3DProcessingService.Prepare(session.Site.Processor,session.Cache.State,()=>true,out var quote); quote.Commit();
            var pipeline=GraphicsSettings.defaultRenderPipeline; var quality=QualitySettings.renderPipeline;
            try
            {
                GraphicsSettings.defaultRenderPipeline=null; QualitySettings.renderPipeline=null;
                var cameraObject=new GameObject("Camp UI capture camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=540;camera.transform.position=new Vector3(0,0,-10);
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.09f,.06f);camera.farClipPlane=100;camera.cullingMask=1<<5;
                var ui=session.Ui;var view=ui.GetComponent<TopDown3DCampWorkstationCanvas>();var canvas=ui.GetComponent<Canvas>();
                foreach(var value in CampWorkstationSceneAuthoring.All<Canvas>(scene))value.enabled=false;
                foreach(var part in ui.GetComponentsInChildren<Transform>(true))part.gameObject.layer=5;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                foreach(var kind in new[]{"sorter","items","stations","companions","workbench","refinery","chest"})
                {
                    ui.Close();
                    ITopDown3DInteractable target=kind=="workbench"?session.Workbench:kind=="refinery"?session.Refinery:kind=="chest"?session.Cache:session.Site.Processor;
                    var position=kind=="workbench"?session.Cache.transform.position+new Vector3(-.8f,0,-1.2f)
                        :kind=="refinery"?session.Refinery.transform.position+Vector3.back*1.6f:(session.Cache.transform.position+session.Site.Processor.transform.position)*.5f;
                    session.Kit.transform.position=CampWorkstationSceneAuthoring.Supported(scene,position)+Vector3.up*.95f;Physics.SyncTransforms();
                    Call(ui,"OpenTarget",target);if(!ui.IsOpen)throw new InvalidOperationException("Capture target inaccessible: "+kind);
                    Call(ui,"Update");if(kind=="items"||kind=="stations"||kind=="companions")Call(ui,"Command","Overview"+char.ToUpperInvariant(kind[0])+kind.Substring(1));
                    if(kind=="items"||kind=="sorter")Field(ui,"source",3);
                    Call(ui,"Refresh");view.SetLargeText(false); Capture(camera,canvas,kind+"-1920.png",1920,1080);
                    view.SetLargeText(true); Capture(camera,canvas,kind+"-1280-large.png",1280,720);
                }
                Debug.Log("CAMP_IRONSTONE_CAPTURE_PASS: 14 actual controller view captures.");
            }
            finally{GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=quality;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                if(Directory.Exists(preferences))Directory.Delete(preferences,true);}
        }
        private static void Capture(Camera camera,Canvas canvas,string name,int width,int height)
        {
            var target=RenderTexture.GetTemporary(width,height,24);var prior=RenderTexture.active;
            try
            {
                camera.targetTexture=target;canvas.scaleFactor=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                var pixels=texture.GetPixels32();if(!pixels.Any(p=>Math.Abs(p.r-pixels[0].r)+Math.Abs(p.g-pixels[0].g)+Math.Abs(p.b-pixels[0].b)>100))throw new InvalidOperationException("Blank capture.");
                Directory.CreateDirectory("Logs/IronstoneCaptures");File.WriteAllBytes("Logs/IronstoneCaptures/"+name,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            }
            finally{camera.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);}
        }
    }
}
