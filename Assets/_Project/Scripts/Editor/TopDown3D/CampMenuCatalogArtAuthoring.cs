using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Import the exact browser-selected art only in a marked, disposable validation project.</summary>
    public static class CampMenuCatalogArtAuthoring
    {
        private const string Folder="Assets/_Project/Settings/UI/CampExpedition/Resources/CampMenuArt";
        [Serializable] private sealed class Report {public int sprites;public string[] paths;public int maximumTextureSize=1024;public string scope="Native sprite import settings only; exact source PNG bytes retained.";}
        public static void ImportFromCli()
        {
            if(!Application.isBatchMode||Application.isPlaying||!File.Exists(".camp-expedition-validation"))
                throw new InvalidOperationException("Import only in the marked, non-Play candidate.");
            if(!Directory.Exists(Folder))throw new InvalidOperationException("Prepare the browser artwork in the candidate first.");
            AssetDatabase.Refresh();var files=Directory.GetFiles(Folder,"*.png").Select(p=>p.Replace('\\','/')).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach(var file in files)
                {
                    var importer=AssetImporter.GetAtPath(file) as TextureImporter;
                    if(importer==null)throw new InvalidOperationException("Artwork importer unavailable: "+file);
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                    importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;
                    importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;
                    importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                    AssetDatabase.WriteImportSettingsIfDirty(file);AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceUpdate);
                }
            }
            finally{AssetDatabase.StopAssetEditing();}
            foreach(var file in files)if(AssetDatabase.LoadAssetAtPath<Sprite>(file)==null)throw new InvalidOperationException("Native sprite failed to import: "+file);
            File.WriteAllText("menu-catalog-art-import.json",JsonUtility.ToJson(new Report{sprites=files.Length,paths=files},true));
            Debug.Log("Camp menu catalog artwork imported: "+files.Length+" exact source images.");
        }
    }
}
