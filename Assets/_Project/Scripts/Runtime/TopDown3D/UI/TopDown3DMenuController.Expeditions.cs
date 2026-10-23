using System;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DMenuController
    {
        private bool expeditionHost;
        private bool radialReturnSettings;
        internal bool UsesExpeditionPresentation=>expeditionPresentation!=null;
        private CampExpeditionMenuView expeditionPresentation;
        internal bool LostExpeditionServices=>expeditionHost&&settings==null;
        internal void RetireLostExpeditionServices()
        {
            if(!LostExpeditionServices)return;
            DisposeAnimationPalette();
            UnityEngine.InputSystem.InputSystem.onDeviceChange-=ControlDeviceChanged;
            if(input!=null){input.MenuToggleRequested-=Toggle;input.UiCancelRequested-=Back;input.PromptDeviceChanged-=PromptChanged;input.ModeChanged-=InputModeChanged;}
            if(view!=null){view.Command-=OnCommand;view.Rename-=Rename;}
            // The replacement takes these shared services. Deferred destruction of this
            // presentation must not close the replacement or clear its radial callbacks.
            ExpeditionSettingsClosed=null;configured=false;panel=null;expeditionPresentation=null;
            radial=null;input=null;view=null;module=null;
            gameObject.SetActive(false);Destroy(gameObject);
        }
        internal void UseExpeditionPresentation(CampExpeditionMenuView host,Action inventoryRequested=null)
        {
            if(!expeditionHost||IsOpen)throw new InvalidOperationException("A closed expedition settings controller is required.");
            expeditionPresentation=host;
            radial.InventoryRequested=host==null?null:inventoryRequested;
            if(host!=null)host.BindSettingsPresentation(view.DispatchCommand,view.DispatchRename,Back);
            view.AttachPresentation(host);
        }
        public event Action ExpeditionSettingsClosed;
        internal void UseExpeditionHost()
        {
            if(expeditionHost)return;expeditionHost=true;
            input.MenuToggleRequested-=Toggle;input.UiCancelRequested-=Back;
            ExitRequested=null;
        }
        public void OpenExpeditionSettings()
        {
            if(!expeditionHost || IsOpen)return;Open();settingsDraft=settings.Accepted.Clone();Show("Settings","TextSize");if(!settings.CanSave)view.RevealNotice("SettingsNotice");
        }
    }
}
