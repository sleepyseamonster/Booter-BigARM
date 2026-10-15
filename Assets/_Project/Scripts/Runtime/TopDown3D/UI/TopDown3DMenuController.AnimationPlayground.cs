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
        private string animationStationFilter;
        public string SelectedAnimationId {get;private set;}
        public event Action<string> AnimationSelectionChanged;
        public event Action<string> AnimationPreviewRequested;
        public event Action AnimationResetRequested;
        private const int AnimationRows=8;
        private void ConfigureAnimationPalette()
        {
            animationSession=TopDown3DAnimationPlaygroundSession.Find(gameObject.scene);
            if(animationSession == null || animationSession.Catalog == null || animationSession.PalettePrefab == null || !input.HasAnimationCommands) return;
            animationPanel=Instantiate(animationSession.PalettePrefab);animationPanel.name="AnimationPalette";
            view.RegisterExtraPanel("AnimationPalette",animationPanel);
            FilterAnimationChoices(null);
            input.AnimationPaletteRequested+=OpenAnimationPalette;
        }
        public void OpenAnimationPalette()
            => OpenAnimationPaletteForStation(null);
        public void OpenAnimationPaletteForStation(string stationId)
        {
            if(!configured || animationPanel == null || input.Mode != TopDown3DInputMode.Gameplay) return;
            if(animationStationFilter != stationId) animationPage=0;
            FilterAnimationChoices(stationId);
            if(!IsOpen) Open();
            DrawAnimationPage();Show("AnimationPalette",animationChoices.Count==0 ? "LabPaletteBack" : "LabSelect0");
        }
        private void FilterAnimationChoices(string stationId)
        {
            animationStationFilter=stationId; animationChoices.Clear();
            foreach(var entry in animationSession.Catalog.entries)
                if(entry != null && !entry.restrictedReference && (stationId==null || entry.stationId==stationId)) animationChoices.Add(entry);
        }
        private void DrawAnimationPage()
        {
            var pages=Mathf.Max(1,Mathf.CeilToInt(animationChoices.Count/(float)AnimationRows));
            animationPage=Mathf.Clamp(animationPage,0,pages-1);
            view.SetText("LabPaletteTitle",(animationStationFilter ?? "Animation playground")+"  "+(animationPage+1)+" / "+pages);
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
            view.SetText("LabReset", animationSession.ExperimentalForwardReview != null ? "Reset to gait deck" : "Reset lab");
            var selected=animationSession.Bindings?.Find(SelectedAnimationId);
            var selectedInStation=animationChoices.Exists(value=>value.sourceId==SelectedAnimationId);
            view.SetButton("LabPreview",selectedInStation && animationSession.PreviewReady && selected?.CanPreview==true);
            var paletteStatus = animationSession.Readiness == AnimationPlaygroundReadiness.Failed
                ? "Unavailable: " + animationSession.UnavailableReason : string.IsNullOrEmpty(SelectedAnimationId)
                ? animationChoices.Count==0 ? "This station has no unarmed variants yet. F9 opens the full library." : "Select a variant. Physical actions require contact qualification."
                : "Selected: "+SelectedAnimationId+". Preview has no gameplay effects.";
            if (animationSession.Readiness != AnimationPlaygroundReadiness.Failed && animationSession.ForwardReviewStatus != null)
                paletteStatus = animationSession.ForwardReviewStatus + "\n" + paletteStatus;
            view.SetText("LabPaletteStatus", paletteStatus);
        }
        private bool HandleAnimationPaletteCommand(string command)
        {
            if(panel != "AnimationPalette") return false;
            if(command == "LabPrevious" || command == "LabNext")
            {animationPage+=command == "LabNext" ? 1 : -1;DrawAnimationPage();view.RefreshNavigation();return true;}
            if(command == "LabPaletteBack"){Close();return true;}
            if(command == "LabReset")
            {
                Close();
                if (animationSession.ExperimentalForwardReview != null) animationSession.ResetSessionForForwardReview();
                else AnimationResetRequested?.Invoke();
                return true;
            }
            if(command == "LabPreview")
            {
                if(animationChoices.Exists(value=>value.sourceId==SelectedAnimationId) && animationSession.PreviewReady && animationSession.Bindings?.Find(SelectedAnimationId)?.CanPreview==true)
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
