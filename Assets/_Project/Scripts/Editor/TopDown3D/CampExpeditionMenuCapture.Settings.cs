using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static partial class CampExpeditionMenuCapture
    {
        // Presentation-only services use an explicit candidate-owned directory; no author profile is read or written.
        private sealed class SettingsCaptureHarness : IDisposable
        {
            private readonly List<GameObject> roots=new();
            private readonly List<InputActionReference> references=new();
            private readonly TopDown3DMenuController controller;
            private readonly InputSystemUIInputModule module;
            private readonly CampExpeditionMenuView native;
            public SettingsCaptureHarness(CampExpeditionMenuView native,EventSystem events,TopDown3DInputRouter input,InputActionAsset actions,string output,string page,bool large)
            {
                if(!Application.isBatchMode||Application.isPlaying||!File.Exists(".camp-expedition-validation"))throw new InvalidOperationException("An isolated, marked non-Play candidate is required.");
                this.native=native;string directory=Path.GetFullPath(Path.Combine(output,"SettingsFixturePreferences",Guid.NewGuid().ToString("N")));Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,"controls-v1.json"),JsonUtility.ToJson(new TopDown3DControlPreferences()));
                File.WriteAllText(Path.Combine(directory,"menu-settings-v1.json"),page=="settings-readonly"?"{\"version\":99}":JsonUtility.ToJson(new TopDown3DMenuSettings{largeText=large}));
                if(page=="settings-save-error"){File.Delete(Path.Combine(directory,"menu-settings-v1.json"));Directory.CreateDirectory(Path.Combine(directory,"menu-settings-v1.json"));}
                module=events.gameObject.AddComponent<InputSystemUIInputModule>();module.actionsAsset=actions;module.move=Reference("UI/Navigate");module.submit=Reference("UI/Submit");module.cancel=Reference("UI/Cancel");module.leftClick=Reference("UI/Click");
                var kit=input.GetComponent<TopDown3DPlayerInventory>()??input.gameObject.AddComponent<TopDown3DPlayerInventory>();kit.Configure(native.definition.itemCatalog);
                var inventoryRoot=Root("Settings inventory service fixture");var inventoryCanvas=inventoryRoot.AddComponent<TopDown3DInventoryCanvas>();var inventory=inventoryRoot.AddComponent<TopDown3DInventoryUiController>();inventory.Configure(input,kit,null,inventoryCanvas,events);
                var radialRoot=Root("Settings radial service fixture");var radialCanvas=radialRoot.AddComponent<TopDown3DRadialCanvas>();radialCanvas.Build();var radial=radialRoot.AddComponent<TopDown3DRadialMenuController>();radial.Configure(input,kit,inventory,null,null,null,radialCanvas,Path.Combine(directory,"radial-layout-v1.json"));
                var cameraRoot=Root("Settings camera service fixture");cameraRoot.AddComponent<Camera>().enabled=false;cameraRoot.SetActive(true);var rig=cameraRoot.AddComponent<TopDown3DCameraRig>();rig.Configure(input.transform);rig.EnableCameraOptions();
                var legacy=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/Resources/SystemMenu.prefab"));roots.Add(legacy);controller=legacy.AddComponent<TopDown3DMenuController>();controller.Configure(input,inventory,radial,legacy.GetComponent<TopDown3DMenuView>(),module,directory,null,directory);controller.UseExpeditionHost();controller.UseExpeditionPresentation(native);controller.OpenExpeditionSettings();Tick();
                if((page is "settings-readonly" or "settings-save-error")&&large)Click("TextSize");
                if(page=="settings-readonly")((ITopDown3DMenuPresentation)native).RevealNotice("SettingsNotice");
                if(page=="settings-save-error"){Click("Motion");Click("SettingsApply");}
                if(page is "settings-draft" or "settings-discard"){Click("Motion");if(page=="settings-discard"){controller.Back();Tick();}}
                if(page is "settings-radial" or "settings-radial-advanced")
                {Click("RadialSetup");if(page=="settings-radial-advanced"){Click("More");Click("SectorCount");Click("DialogAccept");}}
                if(page is "settings-controls" or "settings-listening" or "settings-binding-error" or "settings-long")
                {
                    Click("Controls");if(page=="settings-listening")Click("ControlBinding");
                    if(page=="settings-binding-error"){Click("ControlDevice");Click("ControlBinding");InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W));InputSystem.Update();Tick();InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.Update();Tick();}
                    if(page=="settings-long"){((ITopDown3DMenuPresentation)native).SetText("ControlNotice",string.Join("\n\n",Enumerable.Repeat("This binding cannot be accepted. Keep the current controls or choose another button, then Apply to save the new draft.",8)));((ITopDown3DMenuPresentation)native).RefreshNavigation();}
                }
                if(page=="settings-camera")Click("CameraOptions");
                InputActionReference Reference(string key){var reference=InputActionReference.Create(actions.FindAction(key,true));references.Add(reference);return reference;}
            }
            private GameObject Root(string name){var root=new GameObject(name,typeof(RectTransform));root.SetActive(false);roots.Add(root);return root;}
            private void Tick()=>typeof(TopDown3DMenuController).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
            private void Click(string key){native.GetComponentsInChildren<CampMenuControl>(true).Single(control=>control.key=="settings:"+key).Button.onClick.Invoke();Tick();}
            public void Dispose()
            {
                if(controller!=null){controller.Close();controller.UseExpeditionPresentation(null);}
                if(module!=null)module.enabled=false;
                for(int i=roots.Count-1;i>=0;i--)if(roots[i]!=null)Object.DestroyImmediate(roots[i]);foreach(var reference in references)if(reference!=null)Object.DestroyImmediate(reference);
            }
        }
    }
}
