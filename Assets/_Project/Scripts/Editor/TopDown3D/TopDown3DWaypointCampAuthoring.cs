using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.Editor
{
    public static class TopDown3DWaypointCampAuthoring
    {
        public const string DefinitionPath = "Assets/_Project/Settings/Camping/Resources/TopDown3DWaypointCampSettings.asset";
        public const string MenuPath = "Assets/_Project/Prefabs/UI/Resources/SystemMenu.prefab";
        // Mutates only the waypoint definition and the supporting menu prefab; never opens or saves a scene.
        public static void AuthorFromCli()
        {
            var definition = AssetDatabase.LoadAssetAtPath<TopDown3DWaypointCampSettings>(DefinitionPath);
            if (definition == null)
            { definition = ScriptableObject.CreateInstance<TopDown3DWaypointCampSettings>(); AssetDatabase.CreateAsset(definition, DefinitionPath); }
            if (!definition.TryValidate(out var reason)) throw new InvalidOperationException(reason);
            var root = PrefabUtility.LoadPrefabContents(MenuPath);
            try
            {
                var existing = root.GetComponentsInChildren<Button>(true).SingleOrDefault(button => button.name == "Camps");
                if (existing == null)
                {
                    var resume = root.GetComponentsInChildren<Button>(true).Single(button => button.name == "Resume");
                    var menu = resume.transform.parent;
                    var button = UnityEngine.Object.Instantiate(resume.gameObject, menu); button.name = "Camps";
                    button.transform.SetSiblingIndex(resume.transform.GetSiblingIndex() + 1);
                    button.GetComponent<Button>().onClick.RemoveAllListeners();
                    button.GetComponentInChildren<Text>().text = "Camps";
                    foreach (var label in button.GetComponentsInChildren<Text>(true)) label.name = "CampsLabel";
                }
                var resumeRow = root.GetComponentsInChildren<Button>(true).Single(value => value.name == "Resume");
                var menuRect = (RectTransform)resumeRow.transform.parent;
                menuRect.sizeDelta = new Vector2(menuRect.sizeDelta.x, 500);
                PrefabUtility.SaveAsPrefabAsset(root, MenuPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssetIfDirty(definition);
            System.IO.File.WriteAllText(MenuPath, string.Join("\n", System.IO.File.ReadAllLines(MenuPath).Select(line => line.TrimEnd())) + "\n", new System.Text.UTF8Encoding(false));
            Debug.Log("WAYPOINT CAMP AUTHORED: one definition, Camps menu entry, no scene saved");
        }
    }
}
