using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundUiAuthoring
    {
        public const string CatalogPath="Assets/_Project/Settings/AnimationPlayground/AnimationCatalog.asset";
        public const string PalettePath="Assets/_Project/Prefabs/AnimationPlayground/UI/AnimationPalette.prefab";
        [Serializable] private sealed class Transfer {public Entry[] assets;}
        [Serializable] private sealed class Entry {public string asset_id,mixamo_source_title,category,primary_station,kind;public bool eligible_unarmed_candidate;}
        public static void CreateAndBind()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Author lab UI in Edit Mode.");
            var scene=EditorSceneManager.GetActiveScene();var session=TopDown3DAnimationPlaygroundSession.Find(scene);
            if(session == null || scene.isDirty) throw new InvalidOperationException("A saved owned lab session is required.");
            if(File.Exists(CatalogPath) || File.Exists(PalettePath)) throw new InvalidOperationException("Preserve existing authored UI; update explicitly.");
            EnsureFolder("Assets/_Project/Settings/AnimationPlayground");EnsureFolder("Assets/_Project/Prefabs/AnimationPlayground/UI");
            var source=JsonUtility.FromJson<Transfer>(File.ReadAllText(AnimationPlaygroundAssetImporter.TransferPlanPath));
            var catalog=ScriptableObject.CreateInstance<AnimationPlaygroundCatalog>();
            catalog.entries=source.assets.Where(e=>e.kind != "character" && e.kind != "retained-version")
                .Select(e=>new AnimationPlaygroundCatalogEntry{sourceId=e.asset_id,title=e.mixamo_source_title,category=e.category,stationId=e.primary_station,restrictedReference=!e.eligible_unarmed_candidate}).ToArray();
            AssetDatabase.CreateAsset(catalog,CatalogPath);
            var root=new GameObject("AnimationPalette",typeof(RectTransform),typeof(Image),typeof(VerticalLayoutGroup));
            try
            {
                var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(860,620);
                root.GetComponent<Image>().color=new Color32(28,31,29,248);
                var layout=root.GetComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(24,24,18,18);layout.spacing=6;
                layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
                Label(root.transform,"LabPaletteTitle","Animation playground",30,42);
                Label(root.transform,"LabPaletteStatus","Select a variant",19,46);
                for(var i=0;i<8;i++) Button(root.transform,"LabSelect"+i,"Animation variant",22,43);
                var footer=new GameObject("LabFooter",typeof(RectTransform),typeof(HorizontalLayoutGroup),typeof(LayoutElement));
                footer.transform.SetParent(root.transform,false);footer.GetComponent<LayoutElement>().preferredHeight=48;
                footer.GetComponent<HorizontalLayoutGroup>().spacing=8;
                Button(footer.transform,"LabPrevious","Previous",21,48);Button(footer.transform,"LabNext","Next",21,48);
                Button(footer.transform,"LabPreview","Preview",21,48);Button(footer.transform,"LabPaletteBack","Back",21,48);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,PalettePath);
                Undo.RecordObject(session,"Bind authored animation palette");session.ConfigurePresentation(catalog,prefab);
                EditorUtility.SetDirty(session);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            var parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        private static Text Label(Transform parent,string name,string text,int size,float height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(LayoutElement));go.transform.SetParent(parent,false);
            var label=go.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=size;
            label.color=new Color32(232,221,194,255);label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
            go.GetComponent<LayoutElement>().preferredHeight=height;return label;
        }
        private static void Button(Transform parent,string name,string text,int size,float height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement));go.transform.SetParent(parent,false);
            go.GetComponent<Image>().color=new Color32(55,61,57,255);go.GetComponent<LayoutElement>().preferredHeight=height;
            var label=Label(go.transform,name+"Label",text,size,height);var rect=(RectTransform)label.transform;
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(12,0);rect.offsetMax=new Vector2(-12,0);
        }
    }
}
