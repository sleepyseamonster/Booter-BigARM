namespace BooterBigArm.TopDown3D
{
    /// <summary>Optional presentation host. The existing menu controller and preference services retain authority.</summary>
    public interface ITopDown3DMenuPresentation
    {
        void Show(string panel,string focus);
        void SetInputReady(bool ready);
        void SetText(string key,string value);
        void SetButton(string key,bool enabled);
        void SetActive(string key,bool enabled);
        void SetPageName(string value);
        void EndTextEdit();
        string FocusKey {get;}
        bool IsEditingText {get;}
        void ApplySettings(TopDown3DMenuSettings settings);
        void DrawSlots(TopDown3DRadialPage page,int selected);
        void DrawRadialSummary(TopDown3DRadialPreferences preferences);
        void RefreshNavigation();
        void RevealNotice(string key);
    }
}
