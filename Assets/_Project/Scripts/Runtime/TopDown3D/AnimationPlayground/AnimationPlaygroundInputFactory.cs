using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public static class AnimationPlaygroundInputFactory
    {
        public const string PalettePath="System/AnimationPalette";
        public static bool IsLabAsset(InputActionAsset asset) => asset != null && asset.FindAction(PalettePath,false) != null;
        public static InputActionAsset Create(InputActionAsset source)
        {
            if(source == null) throw new ArgumentNullException(nameof(source));
            var working=InputActionAsset.FromJson(source.ToJson());
            var overrides=source.SaveBindingOverridesAsJson();
            try
            {
                var gameplay=working.FindActionMap("Gameplay",true);
                var system=working.FindActionMap("System",true);
                var replacementIds=new System.Collections.Generic.Dictionary<string,string>();
                AddButton(gameplay,"Crouch","<Keyboard>/v","Keyboard&Mouse",replacementIds);
                AddButton(gameplay,"Crouch","<Gamepad>/leftStickPress","Gamepad",replacementIds);
                AddButton(gameplay,"Attack","<Gamepad>/rightTrigger","Gamepad",replacementIds);
                AddButton(gameplay,"AnimationSlowWalk","<Keyboard>/leftAlt","Keyboard&Mouse",replacementIds);
                AddButton(gameplay,"AnimationCancel","<Keyboard>/backspace","Keyboard&Mouse",replacementIds);
                AddButton(gameplay,"AnimationCancel","<Gamepad>/buttonEast","Gamepad",replacementIds);
                AddButton(system,"AnimationPalette","<Keyboard>/f9","Keyboard&Mouse",replacementIds);
                AddButton(system,"AnimationPalette","<Gamepad>/select","Gamepad",replacementIds);
                var json=working.ToJson();
                foreach(var replacement in replacementIds) json=json.Replace(replacement.Key,replacement.Value);
                var owned=InputActionAsset.FromJson(json);
                owned.name=source.name+" (Animation Playground runtime)";
                if(!string.IsNullOrEmpty(overrides)) owned.LoadBindingOverridesFromJson(overrides,false);
                return owned;
            }
            finally { if(Application.isPlaying) UnityEngine.Object.Destroy(working); else UnityEngine.Object.DestroyImmediate(working); }
        }
        private static void AddButton(InputActionMap map,string actionName,string path,string group,
            System.Collections.Generic.Dictionary<string,string> ids)
        {
            var action=map.FindAction(actionName,false);
            if(action == null)
            {
                action=map.AddAction(actionName,InputActionType.Button,expectedControlLayout:"Button");
                ids[action.id.ToString()]=StableId(map.name+"/"+actionName);
            }
            foreach(var binding in action.bindings) if(binding.path == path) return;
            action.AddBinding(path,groups:group);
            var added=action.bindings[action.bindings.Count-1];
            ids[added.id.ToString()]=StableId(map.name+"/"+actionName+"/"+group+"/"+path);
        }
        private static string StableId(string key)
        {
            using var digest=MD5.Create();
            return new Guid(digest.ComputeHash(Encoding.UTF8.GetBytes("BooterBigArm.AnimationPlayground.v1/"+key))).ToString();
        }
    }
}
