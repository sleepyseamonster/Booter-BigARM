using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using BooterBigArm.TopDown3D;

public static class AlgaeCanisterCapture
{
    public static string Capture()
    {
        if(EditorApplication.isPlaying)throw new Exception("Capture requires Edit Mode; never stop the user's session.");
        var production=SceneManager.GetActiveScene();
        if(production.path!="Assets/_Project/Scenes/Production/GreaterWasteland.unity")throw new Exception("Expected loaded production scene");
        var source=production.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TopDown3DInputRouter>(true)).Single();
        var input=UnityEngine.Object.Instantiate(source.InputActions);
        var scene=EditorSceneManager.NewPreviewScene();
        var player=new GameObject("UI inspection player");player.SetActive(false);SceneManager.MoveGameObjectToScene(player,scene);
        TopDown3DInputRouter router=null;
        var priorPipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
        var priorQualityPipeline=QualitySettings.renderPipeline;
        UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset reviewPipeline=null;
        UnityEngine.Rendering.Universal.UniversalRendererData reviewRenderer=null;
        try
        {
            var motor=player.AddComponent<TopDown3DPlayerMotor>();router=player.AddComponent<TopDown3DInputRouter>();router.Configure(input);router.Initialize();motor.Configure(router,null);
            var vitals=player.AddComponent<TopDown3DSurvivalVitals>();vitals.ResetToFull();
            vitals.SetValue(TopDown3DSurvivalVital.Health,72);vitals.SetValue(TopDown3DSurvivalVital.Hunger,48);vitals.SetValue(TopDown3DSurvivalVital.Thirst,35);
            TopDown3DSurvivalHud.TryInstallForScene(scene);
            var controller=TopDown3DAlgaeConsumptionController.TryInstallForScene(scene);
            controller.State.ApplySnapshot(new TopDown3DAlgaeCanisterSnapshot{remaining=60,capacity=100});
            var hud=TopDown3DAlgaeCanisterHud.TryInstallForScene(scene,controller);
            var pad=hud.GetComponent<TopDown3DActionDpadHud>();
            var gameHud=TopDown3DGameHudCanvas.FindInScene<TopDown3DGameHudCanvas>(scene);
            // UI is an overlay in production; use a temporary feature-free pipeline for camera-based capture.
            reviewRenderer=ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
            reviewPipeline=UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.Create(reviewRenderer);
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=reviewPipeline;QualitySettings.renderPipeline=reviewPipeline;
            var cameraObject=new GameObject("UI inspection camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.19f,.10f,.065f);
            cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
            gameHud.Canvas.renderMode=RenderMode.ScreenSpaceCamera;gameHud.Canvas.worldCamera=camera;gameHud.Canvas.planeDistance=10;
            Directory.CreateDirectory("Docs/Evidence/UI/AlgaeCanister");
            foreach(var view in new[]{"1920x1080","1280x720","2560x1080","safe-area","after-dose","empty"})
            {
                if(view=="empty")controller.State.ApplySnapshot(new TopDown3DAlgaeCanisterSnapshot{remaining=0,capacity=100});
                if(view=="after-dose")
                {
                    typeof(TopDown3DAlgaeConsumptionController).GetMethod("TryUse",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{TopDown3DAlgaeUse.Drink,1d});
                    typeof(TopDown3DAlgaeCanisterHud).GetField("gaugeStarted",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hud,Time.unscaledTime-.3f);
                    var meters=TopDown3DGameHudCanvas.FindInScene<TopDown3DSurvivalHud>(scene);
                    typeof(TopDown3DSurvivalHud).GetField("restorationStarted",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(meters,Time.unscaledTime-.3f);
                    typeof(TopDown3DSurvivalHud).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(meters,null);
                }
                var width=view=="1280x720"?1280:view=="2560x1080"?2560:1920;
                var height=view=="1280x720"?720:1080;
                var rt=RenderTexture.GetTemporary(width,height,24);var previous=RenderTexture.active;
                try
                {
                    camera.targetTexture=rt;
                    typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gameHud.Scaler,null);
                    Canvas.ForceUpdateCanvases();
                    var safe=view=="safe-area"?new Rect(80,40,width-160,height-80):new Rect(0,0,width,height);
                    var scale=gameHud.Canvas.scaleFactor;
                    ((RectTransform)pad.transform).anchoredPosition=TopDown3DActionDpadHud.GetBottomLeftAnchoredPosition(safe,scale);
                    TopDown3DGameHudCanvas.FindInScene<TopDown3DSurvivalHud>(scene).PanelRect.anchoredPosition=TopDown3DSurvivalHud.GetTopLeftAnchoredPosition(safe,height,scale);
                    // Initialize the gauge's geometry in Edit Mode without a gameplay simulation.
                    typeof(TopDown3DAlgaeCanisterHud).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);
                    pad.Graphic.SetAllDirty();Canvas.ForceUpdateCanvases();pad.Graphic.Rebuild(CanvasUpdate.PreRender);
                    camera.Render();RenderTexture.active=rt;
                    var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                    File.WriteAllBytes("Docs/Evidence/UI/AlgaeCanister/"+view+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                }
                finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
            }
            return "Six HUD views captured from the production input asset in a disposable preview scene.";
        }
        finally
        {
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=priorPipeline;QualitySettings.renderPipeline=priorQualityPipeline;
            if(reviewPipeline!=null)UnityEngine.Object.DestroyImmediate(reviewPipeline);
            if(reviewRenderer!=null)UnityEngine.Object.DestroyImmediate(reviewRenderer);
            if(router!=null)typeof(TopDown3DInputRouter).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(router,null);
            EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(input);
        }
    }
}
