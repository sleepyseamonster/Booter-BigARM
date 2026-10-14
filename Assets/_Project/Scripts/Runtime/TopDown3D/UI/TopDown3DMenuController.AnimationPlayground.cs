using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DMenuController
    {
        private TopDown3DAnimationPlaygroundSession animationSession;
        private GameObject animationPanel;
        private readonly List<AnimationPlaygroundCatalogEntry> animationChoices=new();
        private int animationPage;
        public string SelectedAnimationId {get;private set;}
        public event Action<string> AnimationSelectionChanged;
        public event Action<string> AnimationPreviewRequested;
        private const int AnimationRows=8;
        private void ConfigureAnimationPalette()
        {
            animationSession=TopDown3DAnimationPlaygroundSession.Find(gameObject.scene);
            if(animationSession == null || animationSession.Catalog == null || animationSession.PalettePrefab == null || !input.HasAnimationCommands) return;
            animationPanel=Instantiate(animationSession.PalettePrefab);animationPanel.name="AnimationPalette";
            view.RegisterExtraPanel("AnimationPalette",animationPanel);
            foreach(var entry in animationSession.Catalog.entries)
                if(entry != null && !entry.restrictedReference) animationChoices.Add(entry);
            input.AnimationPaletteRequested+=OpenAnimationPalette;
        }
        public void OpenAnimationPalette()
        {
            if(!configured || animationPanel == null || input.Mode != TopDown3DInputMode.Gameplay) return;
            if(!IsOpen) Open();
            DrawAnimationPage();Show("AnimationPalette","LabSelect0");
        }
        private void DrawAnimationPage()
        {
            var pages=Mathf.Max(1,Mathf.CeilToInt(animationChoices.Count/(float)AnimationRows));
            animationPage=Mathf.Clamp(animationPage,0,pages-1);
            view.SetText("LabPaletteTitle","Animation playground  "+(animationPage+1)+" / "+pages);
            for(var row=0;row<AnimationRows;row++)
            {
                var index=animationPage*AnimationRows+row;var exists=index<animationChoices.Count;
                view.SetActive("LabSelect"+row,exists);
                if(!exists) continue;
                var entry=animationChoices[index];var binding=animationSession.Bindings?.Find(entry.sourceId);
                var status=binding == null ? "Source only" : binding.readiness.ToString();
                view.SetText("LabSelect"+row,entry.category+": "+entry.title+"  ["+status+"]");
            }
            view.SetButton("LabPrevious",animationPage>0);view.SetButton("LabNext",animationPage+1<pages);
            var selected=animationSession.Bindings?.Find(SelectedAnimationId);
            view.SetButton("LabPreview",selected?.CanPreview==true);
            view.SetText("LabPaletteStatus",string.IsNullOrEmpty(SelectedAnimationId)
                ? "Select a variant. A fresh gameplay press performs an available action."
                : "Selected: "+SelectedAnimationId+". Preview has no gameplay effects.");
        }
        private bool HandleAnimationPaletteCommand(string command)
        {
            if(panel != "AnimationPalette") return false;
            if(command == "LabPrevious" || command == "LabNext")
            {animationPage+=command == "LabNext" ? 1 : -1;DrawAnimationPage();view.RefreshNavigation();return true;}
            if(command == "LabPaletteBack"){Close();return true;}
            if(command == "LabPreview")
            {
                if(animationSession.Bindings?.Find(SelectedAnimationId)?.CanPreview==true)
                {var selected=SelectedAnimationId;Close();AnimationPreviewRequested?.Invoke(selected);}
                return true;
            }
            if(command.StartsWith("LabSelect",StringComparison.Ordinal) && int.TryParse(command.Substring(9),out var row))
            {
                var index=animationPage*AnimationRows+row;
                if(row>=0 && row<AnimationRows && index<animationChoices.Count)
                {SelectedAnimationId=animationChoices[index].sourceId;Close();AnimationSelectionChanged?.Invoke(SelectedAnimationId);}
                return true;
            }
            return false;
        }
        private void DisposeAnimationPalette()
        {
            if(input != null) input.AnimationPaletteRequested-=OpenAnimationPalette;
            if(animationPanel != null) Destroy(animationPanel);
            animationPanel=null;animationChoices.Clear();animationSession=null;
        }
    }
}
