using UnityEngine;
using UnityEditor;
using BooterBigArm.TopDown3D;

public static class AlgaeCanisterAuthoring
{
    public static string CreateSettings()
    {
        const string path="Assets/_Project/Settings/Survival/Resources/TopDown3DAlgaeCanisterSettings.asset";
        var settings=AssetDatabase.LoadAssetAtPath<TopDown3DAlgaeCanisterSettings>(path);
        if(settings==null){settings=ScriptableObject.CreateInstance<TopDown3DAlgaeCanisterSettings>();AssetDatabase.CreateAsset(settings,path);}
        if(!settings.IsValid)throw new System.Exception("Invalid algae settings");
        return "Valid algae settings asset ready";
    }
}
