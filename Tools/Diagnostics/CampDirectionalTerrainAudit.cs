using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Read-only native collision samples. No player movement, scene writes or simulation.
public static class CampDirectionalTerrainAudit
{
    public static string InspectCandidateNative(string output)
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(!Application.isBatchMode || Directory.Exists(Path.Combine(root,".git"))
            || !File.Exists(Path.Combine(root,".animation-playground-validation-project")))
            throw new InvalidOperationException("Open disposable scene copies only in the marked batchmode candidate.");
        var previous=SceneManager.GetActiveScene();
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/_Project/Scenes/Reference/CampLoopTesting.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try { SceneManager.SetActiveScene(scene);return InspectNative(output); }
        finally
        {
            SceneManager.SetActiveScene(previous);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
        }
    }

    public static string InspectNative(string output)
    {
        var scene=SceneManager.GetActiveScene();var dirty=scene.isDirty;
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || scene.path!="Assets/_Project/Scenes/Reference/CampLoopTesting.unity" || File.Exists(output))
            throw new InvalidOperationException("Inspect the already loaded Camp Loop in Edit Mode into a fresh receipt.");
        var terrains=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene).ToArray();
        if(terrains.Length!=9)throw new InvalidDataException("Expected nine retained native terrains.");
        var colliders=terrains.Select(t=>t.GetComponent<TerrainCollider>()).ToArray();
        string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        bool Hit(float x,float z,out RaycastHit result)
        {
            var ray=new Ray(new Vector3(x,2000,z),Vector3.down);
            foreach(var collider in colliders)if(collider.Raycast(ray,out result,2500))return true;
            result=default;return false;
        }
        var rows=new List<object>();var offsets=new[]{new Vector2(-.3f,-.45f),new Vector2(.3f,-.45f),new Vector2(-.3f,.45f),new Vector2(.3f,.45f)};
        var player=UnityEngine.Object.FindObjectsByType<TopDown3DPlayerMotor>(FindObjectsSortMode.None).Single(p=>p.gameObject.scene==scene);
        var centre=player.transform.position;
        for(var x=-160;x<=160;x+=8)for(var z=-160;z<=160;z+=8)
        {
            if(!Hit(centre.x+x,centre.z+z,out var hit))continue;
            var plane=new Plane(hit.normal,hit.point);var residual=0f;var normalDelta=0f;var complete=true;
            foreach(var offset in offsets)
            {
                if(!Hit(hit.point.x+offset.x,hit.point.z+offset.y,out var probe)){complete=false;break;}
                residual=Mathf.Max(residual,Mathf.Abs(plane.GetDistanceToPoint(probe.point)));normalDelta=Mathf.Max(normalDelta,Vector3.Angle(hit.normal,probe.normal));
            }
            rows.Add(new{point=new[]{hit.point.x,hit.point.y,hit.point.z},normal=new[]{hit.normal.x,hit.normal.y,hit.normal.z},slope=Vector3.Angle(hit.normal,Vector3.up),
                residual,normalDelta,complete,terrain=hit.collider.name});
        }
        if(scene.isDirty!=dirty)throw new InvalidOperationException("Read-only terrain audit changed scene state.");
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{scope="Native collision geometry only; no gameplay or scene writes.",centre=new[]{centre.x,centre.y,centre.z},sceneDirty=dirty,
            terrains=terrains.Select(t=>new{name=t.name,data=AssetDatabase.GetAssetPath(t.terrainData),ground=t.GetComponentInParent<TopDown3DGroundSurface>()!=null,
                sha256=Hash(AssetDatabase.GetAssetPath(t.terrainData)),metaSha256=Hash(AssetDatabase.GetAssetPath(t.terrainData)+".meta"),
                position=new[]{t.transform.position.x,t.transform.position.y,t.transform.position.z},rotation=new[]{t.transform.rotation.x,t.transform.rotation.y,t.transform.rotation.z,t.transform.rotation.w},
                scale=new[]{t.transform.lossyScale.x,t.transform.lossyScale.y,t.transform.lossyScale.z}}).ToArray(),rows}));return output;
    }
}
