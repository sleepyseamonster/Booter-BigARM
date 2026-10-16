// Compile through Pipeline run_script; no assembly reload or scene changes.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class SableOrganicLibrary
{
    const string Art = "Assets/_Project/Art/Textures/OrganicLibrary20261015";
    const string Mats = "Assets/_Project/Materials/SurfaceLibrary/OrganicLibrary20261015";
    const string Docs = "Docs/Agents/Sable/work/2026-10-15-organic-materials";
    static readonly string[] Families = { "WornLeather", "Cloth", "Bone", "Chitin", "AgedWood" };
    static readonly string[] Styles = { "R", "E", "P" };
    static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new Exception("Requires stable Edit Mode; no state change attempted.");
    }
    static string Root => Path.GetDirectoryName(Application.dataPath);
    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        Folder(Path.GetDirectoryName(path).Replace('\\','/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'), Path.GetFileName(path));
    }
    public static object Import(string family)
    {
        Guard(); if (!Families.Contains(family)) throw new Exception("Unknown family");
        string source = Path.Combine(Root,"outputs/Sable/MaterialDioramas20261015/OrganicLibrary/Textures");
        string[] names = { "BaseColor_R", "BaseColor_E", "BaseColor_P", "Normal", "Mask" };
        foreach (var name in names)
        {
            string input = Path.Combine(source,family+"_"+name+".png");
            if (!File.Exists(input)) throw new Exception("Missing baked map: "+input);
            string target = Art+"/"+family+"_"+name+".png";
            if (File.Exists(target) && !File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(input)))
                throw new Exception("Refusing to overwrite differing existing texture: "+target);
        }
        Folder(Art);Folder(Mats);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader || !shader.isSupported) throw new Exception("URP/Lit unavailable");
        AssetDatabase.DisallowAutoRefresh();
        try
        {
            foreach(var name in names)
            {
                string target=Art+"/"+family+"_"+name+".png";
                if(!File.Exists(target)) File.Copy(Path.Combine(source,family+"_"+name+".png"),target,false);
                AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);
                var ti=(TextureImporter)AssetImporter.GetAtPath(target);
                ti.textureType=name=="Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture=name.StartsWith("BaseColor");ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Repeat;
                ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=8;ti.maxTextureSize=2048;ti.isReadable=false;
                ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=false;
                ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.compressionQuality=100;
                ti.convertToNormalmap=false;ti.flipGreenChannel=false;
                var platform=ti.GetPlatformTextureSettings("Standalone");platform.overridden=true;platform.maxTextureSize=2048;
                platform.format=name=="Normal" ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7;
                platform.compressionQuality=100;ti.SetPlatformTextureSettings(platform);ti.SaveAndReimport();
            }
            foreach(string style in Styles)
            {
                string path=Mats+"/"+family+"_"+style+".mat";
                Material mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null)
                {
                    mat=new Material(shader);mat.name=family+"_"+style;AssetDatabase.CreateAsset(mat,path);
                }
                else if(mat.shader!=shader)throw new Exception("Existing material shader differs");
                mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+family+"_BaseColor_"+style+".png"));
                mat.SetColor("_BaseColor",Color.white);
                mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+family+"_Normal.png"));
                var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+family+"_Mask.png");
                mat.SetTexture("_MetallicGlossMap",mask);mat.SetTexture("_OcclusionMap",mask);
                mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",1);mat.SetFloat("_SmoothnessTextureChannel",0);
                mat.SetFloat("_BumpScale",1);mat.SetFloat("_OcclusionStrength",1);
                mat.SetFloat("_WorkflowMode",1);mat.SetFloat("_Surface",0);mat.SetFloat("_AlphaClip",0);mat.SetFloat("_Cull",2);
                mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.EnableKeyword("_OCCLUSIONMAP");
                mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");mat.DisableKeyword("_SPECULAR_SETUP");
                mat.enableInstancing=true;EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
            }
        }
        finally{AssetDatabase.AllowAutoRefresh();}
        return ValidateFamily(family);
    }
    static object ValidateFamily(string family)
    {
        var textures=new List<object>();var materials=new List<object>();
        foreach(string name in new[]{"BaseColor_R","BaseColor_E","BaseColor_P","Normal","Mask"})
        {
            string path=Art+"/"+family+"_"+name+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!tex||tex.width!=2048||tex.height!=2048||tex.mipmapCount!=12)throw new Exception("Invalid texture "+path);
            if(ti.sRGBTexture!=name.StartsWith("BaseColor")||!ti.mipmapEnabled||ti.wrapMode!=TextureWrapMode.Repeat||ti.isReadable||ti.filterMode!=FilterMode.Trilinear||ti.anisoLevel!=8)throw new Exception("Importer mismatch "+path);
            if(name=="Normal"&&(ti.textureType!=TextureImporterType.NormalMap||ti.flipGreenChannel))throw new Exception("Normal importer mismatch");
            var ps=ti.GetPlatformTextureSettings("Standalone");if(!ps.overridden||ps.format!=(name=="Normal"?TextureImporterFormat.BC5:TextureImporterFormat.BC7))throw new Exception("Compression mismatch");
            textures.Add(new{path,guid=AssetDatabase.AssetPathToGUID(path),width=tex.width,height=tex.height,mips=tex.mipmapCount,format=tex.format.ToString(),runtimeBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex),sRGB=ti.sRGBTexture});
        }
        foreach(string style in Styles)
        {
            string path=Mats+"/"+family+"_"+style+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m||m.shader.name!="Universal Render Pipeline/Lit"||!m.shader.isSupported||ShaderUtil.ShaderHasError(m.shader))throw new Exception("Shader failure "+path);
            foreach(string slot in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap"})if(!m.GetTexture(slot))throw new Exception("Missing slot "+path+slot);
            if(m.GetTexture("_MetallicGlossMap")!=m.GetTexture("_OcclusionMap")||m.GetFloat("_Smoothness")!=1||m.GetFloat("_SmoothnessTextureChannel")!=0||m.GetFloat("_WorkflowMode")!=1)throw new Exception("Packed-map contract mismatch");
            foreach(string key in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP"})if(!m.IsKeywordEnabled(key))throw new Exception("Keyword missing "+key);
            materials.Add(new{path,guid=AssetDatabase.AssetPathToGUID(path),shader=m.shader.name,normal=m.GetTexture("_BumpMap").name,mask=m.GetTexture("_MetallicGlossMap").name});
        }
        return new{family,textures,materials};
    }
    public static object Validate()
    {
        Guard();var result=Families.Select(ValidateFamily).ToArray();
        var scenes=Enumerable.Range(0,EditorSceneManager.sceneCount).Select(i=>new{path=EditorSceneManager.GetSceneAt(i).path,dirty=EditorSceneManager.GetSceneAt(i).isDirty}).ToArray();
        var report=new{unity=Application.unityVersion,activeBuildTarget=EditorUserBuildSettings.activeBuildTarget.ToString(),materials=15,textures=25,families=result,scenes,playModeEntered=false};
        File.WriteAllText(Path.Combine(Root,Docs,"unity_verification.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    public static object FinalizeFolder()
    {
        Guard();const string previous="Assets/_Project/Materials/Library";const string final="Assets/_Project/Materials/SurfaceLibrary";
        if(AssetDatabase.IsValidFolder(final))throw new Exception("Final folder already exists");
        if(!Directory.GetDirectories(previous).SequenceEqual(new[]{previous+"/OrganicLibrary20261015"},StringComparer.OrdinalIgnoreCase))
        {
            var dirs=Directory.GetDirectories(previous).Select(x=>x.Replace('\\','/')).ToArray();
            if(dirs.Length!=1||dirs[0]!=previous+"/OrganicLibrary20261015")throw new Exception("Unexpected sibling material folder");
        }
        if(Directory.GetFiles(previous).Any(x=>!x.EndsWith("OrganicLibrary20261015.meta")))throw new Exception("Unexpected sibling files");
        string error=AssetDatabase.MoveAsset(previous,final);if(!string.IsNullOrEmpty(error))throw new Exception(error);
        return new{path=final,assetGuidsPreserved=true,reason="Avoid repository's recursive Library/ cache exclusion"};
    }
    public static object UpdateWood()
    {
        Guard();var rows=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(Path.Combine(Root,Docs,"wood_revision.json")));
        if(rows.Count!=5)throw new Exception("Expected five owned previous wood maps");
        foreach(var row in rows)
        {
            string relative=(string)row["file"];
            if(!relative.StartsWith(Art+"/AgedWood_")||!relative.EndsWith(".png"))throw new Exception("Out of scope update");
            using(var sha=System.Security.Cryptography.SHA256.Create())
            {
                string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(Root,relative)))).Replace("-","").ToLowerInvariant();
                if(hash!=(string)row["previous_sha256"])throw new Exception("Previous owned bytes changed: "+relative);
            }
        }
        AssetDatabase.DisallowAutoRefresh();
        try{foreach(var row in rows){string relative=(string)row["file"];File.Copy(Path.Combine(Root,"outputs/Sable/MaterialDioramas20261015/OrganicLibrary/Textures",Path.GetFileName(relative)),Path.Combine(Root,relative),true);}}
        finally{AssetDatabase.AllowAutoRefresh();}
        return Import("AgedWood");
    }
    static Mesh Sphere()
    {
        const int U=64,V=32;var verts=new Vector3[(U+1)*(V+1)];var norms=new Vector3[verts.Length];var uv=new Vector2[verts.Length];var tangents=new Vector4[verts.Length];var tris=new List<int>();
        for(int y=0;y<=V;y++)for(int x=0;x<=U;x++)
        {
            float p=2*Mathf.PI*x/U,t=Mathf.PI*y/V;int i=y*(U+1)+x;
            verts[i]=new Vector3(Mathf.Sin(t)*Mathf.Cos(p),Mathf.Cos(t),Mathf.Sin(t)*Mathf.Sin(p));norms[i]=verts[i];uv[i]=new Vector2((float)x/U,1f-(float)y/V);tangents[i]=new Vector4(-Mathf.Sin(p),0,Mathf.Cos(p),-1);
            if(y<V&&x<U){int b=i+U+1;tris.AddRange(new[]{i,i+1,b,i+1,b+1,b});}
        }
        var m=new Mesh{name="Sable private preview sphere",hideFlags=HideFlags.HideAndDontSave};m.vertices=verts;m.normals=norms;m.uv=uv;m.tangents=tangents;m.triangles=tris.ToArray();m.RecalculateBounds();return m;
    }
    public static object Capture(string family)
    {
        Guard();if(!Families.Contains(family))throw new Exception("Unknown family");
        string output=Path.Combine(Root,"outputs/Sable/MaterialDioramas20261015/OrganicLibrary/Unity");Directory.CreateDirectory(output);
        var mesh=Sphere();var preview=new PreviewRenderUtility();var files=new List<string>();
        try
        {
            preview.camera.transform.position=new Vector3(0,0,-4);preview.camera.transform.rotation=Quaternion.identity;
            preview.camera.orthographic=true;preview.camera.orthographicSize=1.28f;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=20;
            preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.12f,.14f,.15f,1);
            preview.lights[0].intensity=1.3f;preview.lights[0].color=Color.white;preview.lights[0].transform.rotation=Quaternion.Euler(35,30,0);
            preview.lights[1].intensity=.65f;preview.lights[1].color=new Color(.8f,.87f,1);preview.lights[1].transform.rotation=Quaternion.Euler(330,210,0);
            preview.ambientColor=new Color(.12f,.12f,.12f,1);
            foreach(string style in Styles)
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(Mats+"/"+family+"_"+style+".mat");
                preview.BeginStaticPreview(new Rect(0,0,1024,1024));preview.DrawMesh(mesh,Matrix4x4.identity,mat,0);preview.Render(true,true);
                Texture2D image=preview.EndStaticPreview();string file=Path.Combine(output,family+"_"+style+".png");File.WriteAllBytes(file,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);files.Add(file);
            }
        }
        finally{preview.Cleanup();UnityEngine.Object.DestroyImmediate(mesh);}
        return new{family,files,pipeline="Actual imported stock URP/Lit material in private Editor preview scene",sceneSaved=false};
    }
}
