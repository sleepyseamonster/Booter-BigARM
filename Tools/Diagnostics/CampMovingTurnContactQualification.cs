using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Virtual curved trajectories through held EditMode poses, without physics stepping.
public static class CampMovingTurnContactQualification
{
    const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
    public static string Run(string receipt)
    {
        if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".animation-playground-validation-project")
            || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || File.Exists(receipt))throw new InvalidOperationException("Fresh offline candidate qualification required.");
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BooterBigArm.Tests.AnimationPlaygroundNativeGroundTests+NativeFixture")).First(t=>t!=null);
        using var fixture=(IDisposable)Activator.CreateInstance(type,new object[]{SkinWeights.FourBones});
        var inner=type.GetField("Inner").GetValue(fixture);var it=inner.GetType();var forward=it.GetField("Forward").GetValue(inner);var ft=forward.GetType();
        var driver=(TopDown3DPlayerAnimationDriver)it.GetProperty("Driver").GetValue(inner);var motor=(TopDown3DPlayerMotor)it.GetProperty("Motor").GetValue(inner);
        T F<T>(string n)=>(T)ft.GetField(n).GetValue(forward);
        void Set(string n,object v)=>typeof(TopDown3DPlayerAnimationDriver).GetField(n,P).SetValue(driver,v);
        void Call(string n,params object[] args)=>typeof(TopDown3DPlayerAnimationDriver).GetMethod(n,P).Invoke(driver,args);
        var policy=F<AnimationPlaygroundForwardLocomotionProfile>("Policy");var profile=Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationPlaygroundMovingTurnProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/MovingTurnReview.asset"));profile.forwardProfile=policy;
        if(!profile.Validate(policy,out var reason))throw new InvalidDataException(reason);
        Set("movingTurnProfile",profile);Call("InitializeMovingTurnInputs");it.GetMethod("Lock").Invoke(inner,new object[]{false});
        var visual=(GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance",P).GetValue(driver);var projection=policy.footProjection;
        var bones=projection.bonePaths.Select(p=>p.Length==0?visual.transform:visual.transform.Find(p)).ToArray();
        Vector3 Point(AnimationPlaygroundFootVertex v){var p=Vector3.zero;foreach(var b in v.points)p+=bones[b.bone].TransformPoint(b.position)*b.weight;return p;}
        var sides=projection.vertices.Select(v=>projection.bonePaths[v.points[0].bone].Contains(".L")?0:1).ToArray();var rows=new List<object>();var failures=0;var maximumExcursion=0f;var maximumGap=0f;
        try
        {
            foreach(var speed in new[]{1.4f,2.3f,3,3.5f,4,4.45f,4.9f,5.5f,6.15f})
            foreach(var sign in new[]{-1f,0,1f})foreach(var control in new[]{false,true})for(var plant=0;plant<4;plant++)
            {
                var curvature=sign*12f;var half=speed<2?.025:.0167;var samples=new List<Vector3[]>();var gaps=new List<float[]>();var phase=1+plant*.25;var center=policy.DistanceAtCycles(speed,phase);
                for(var frame=0;frame<9;frame++)
                {
                    var seconds=half*(frame/4d-1);var cycles=policy.AdvanceCycles(speed,phase,seconds*speed);var distance=policy.DistanceAtCycles(speed,cycles)-center;var k=curvature*Mathf.Deg2Rad;
                    var x=sign==0?0:(1-Math.Cos(k*distance))/k;var z=sign==0?distance:Math.Sin(k*distance)/k;
                    type.GetMethod("Place").Invoke(fixture,new object[]{new Vector2(-1848+(float)x,-1498+(float)z),(float)(curvature*distance)});
                    Call("PrepareForwardGraphUpdate");Set("movingTurnPoseCurvature",control?0:curvature);Set("forwardSpeed",speed);Call("PresentForwardFrame",speed,cycles,.2f);Call("LateUpdate");
                    var points=projection.vertices.Select(Point).ToArray();var ground=new float[points.Length];
                    for(var i=0;i<points.Length;i++){var args=new object[]{points[i],null};if(!(bool)type.GetMethod("Hit").Invoke(fixture,args))throw new InvalidDataException("Native support missing.");ground[i]=points[i].y-((RaycastHit)args[1]).point.y;}
                    samples.Add(points);gaps.Add(ground);
                }
                var foot=plant%2==0?1:0;var best=-1;var excursion=float.PositiveInfinity;var gap=float.PositiveInfinity;
                for(var i=0;i<projection.vertices.Length;i++)
                {
                    if(sides[i]!=foot)continue;var g=gaps.Max(a=>a[i]);if(g>.008f)continue;var start=samples[0][i];var e=samples.Max(a=>Vector2.Distance(new Vector2(start.x,start.z),new Vector2(a[i].x,a[i].z)));
                    if(e<excursion){excursion=e;gap=g;best=i;}
                }
                var qualified=best>=0;
                if(!qualified){best=Enumerable.Range(0,projection.vertices.Length).Where(i=>sides[i]==foot).OrderBy(i=>gaps.Max(a=>a[i])).First();gap=gaps.Max(a=>a[best]);var start=samples[0][best];excursion=samples.Max(a=>Vector2.Distance(new Vector2(start.x,start.z),new Vector2(a[best].x,a[best].z)));}
                var passed=qualified && excursion<=.02f;if(!passed)failures++;maximumExcursion=Mathf.Max(maximumExcursion,excursion);maximumGap=Mathf.Max(maximumGap,gap);
                rows.Add(new{speed,curvature,control,plant,window=half*2,vertex=best,excursion,gap,qualified,passed});
            }
            File.WriteAllText(receipt,Newtonsoft.Json.JsonConvert.SerializeObject(new{scope="Offline completed-distance poses; no gameplay or physics simulation",windows=rows.Count,failures,maximumExcursion,maximumGap,rows},Newtonsoft.Json.Formatting.Indented));return receipt;
        }
        finally{Object.DestroyImmediate(profile);}
    }
}
