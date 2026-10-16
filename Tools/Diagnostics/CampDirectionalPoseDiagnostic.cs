using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEngine;

public static class CampDirectionalPoseDiagnostic
{
    public static string Inspect(string output)
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(!Application.isBatchMode || Directory.Exists(Path.Combine(root,".git")) || File.Exists(output)
            || !File.Exists(Path.Combine(root,".animation-playground-validation-project")))throw new InvalidOperationException("Use the marked candidate and a fresh receipt.");
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BooterBigArm.Tests.AnimationPlaygroundNativeGroundTests+NativeFixture")).First(t=>t!=null);
        using var fixture=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public,null,new object[]{SkinWeights.FourBones},null);
        var inner=type.GetField("Inner").GetValue(fixture);var innerType=inner.GetType();
        var driver=(TopDown3DPlayerAnimationDriver)innerType.GetProperty("Driver").GetValue(inner);
        var visual=(GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver);
        var scene=driver.gameObject.scene;var terrains=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TerrainCollider>(true)).Select(t=>(collider:t,bounds:t.bounds)).ToArray();
        bool Hit(Vector3 p,out RaycastHit hit)
        {foreach(var t in terrains)if(p.x>=t.bounds.min.x && p.x<=t.bounds.max.x && p.z>=t.bounds.min.z && p.z<=t.bounds.max.z
                && t.collider.Raycast(new Ray(new Vector3(p.x,2000,p.z),Vector3.down),out hit,2500))return true;hit=default;return false;}
        var sites=new[]{new Vector2(-1848,-1498),new Vector2(-1848,-1546),new Vector2(-1840,-1394),new Vector2(-1976,-1370),new Vector2(-1880,-1506)};
        var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingDirectionalReview.asset");
        var rows=new List<object>();var mesh=new Mesh();
        try
        {
            foreach(var site in sites)foreach(var heading in new[]{0f,90,180,270})foreach(var requested in new[]{3f,6})
            {
                type.GetMethod("Place").Invoke(fixture,new object[]{site,0f});innerType.GetMethod("Lock").Invoke(inner,new object[]{true});
                var local=new Vector2(Mathf.Sin(heading*Mathf.Deg2Rad),Mathf.Cos(heading*Mathf.Deg2Rad));var speed=Mathf.Min(requested,AnimationPlaygroundDirectionalMath.SpeedLimit(profile,local));
                innerType.GetMethod("Frame").Invoke(inner,new object[]{speed,0d,local,1f});
                var minimum=float.PositiveInfinity;var skinName="";var point=Vector3.zero;
                foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.BakeMesh(mesh,true);
                    foreach(var v in mesh.vertices)
                    {
                        var p=skin.transform.TransformPoint(v);if(p.y-driver.transform.position.y>.0f || !Hit(p,out var hit))continue;
                        if(p.y-hit.point.y<minimum){minimum=p.y-hit.point.y;skinName=skin.name;point=p;}
                    }
                }
                rows.Add(new{site=new[]{site.x,site.y},heading,speed,minimum,skinName,point=new[]{point.x,point.y,point.z},driver.LaboratoryNativeGroundAdapted,driver.LaboratoryNativeGroundFailure,driver.LastGarmentFloorDisplacement,driver.GarmentFloorExceeded});
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(mesh);}
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return output;
    }
}
