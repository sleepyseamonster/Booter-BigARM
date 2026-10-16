using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent selected-sole observations in the actual equipped graph. No Play
// Mode, motor replay, physics stepping, scene save, or formal qualification flag.
public static class CampDirectionalContactQualification
{
    public static string Inspect(string proposalPath,string output)
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root,".git")) || !File.Exists(Path.Combine(root,".animation-playground-validation-project")) || File.Exists(output))
            throw new InvalidOperationException("Use the marked compiled candidate and a fresh receipt.");
        var proposal=JObject.Parse(File.ReadAllText(proposalPath));var rows=new List<object>();
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BooterBigArm.Tests.AnimationPlaygroundDirectionalTests+Fixture")).First(t=>t!=null);
        var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingDirectionalReview.asset");
        string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        foreach(var quality in new[]{SkinWeights.OneBone,SkinWeights.TwoBones,SkinWeights.FourBones})
        {
            using var fixture=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{quality},null);
            var forward=type.GetField("Forward").GetValue(fixture);var ft=forward.GetType();
            T Field<T>(string n)=>(T)ft.GetField(n).GetValue(forward);
            var actor=Field<GameObject>("Actor");var body=Field<Rigidbody>("Player");var ground=Field<GameObject>("Ground");var scene=Field<Scene>("Scene");
            var driver=(TopDown3DPlayerAnimationDriver)type.GetProperty("Driver").GetValue(fixture);
            foreach(var collider in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Collider>(true)))
                if(collider.gameObject!=ground && !collider.transform.IsChildOf(actor.transform))collider.enabled=false;
            type.GetMethod("Lock").Invoke(fixture,new object[]{true});
            var visual=(GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver);
            var projection=profile.footProjection;var bones=projection.bonePaths.Select(p=>p.Length==0 ? visual.transform : visual.transform.Find(p)).ToArray();
            Vector3 Point(int vertex)
            {
                var value=Vector3.zero;var total=0f;
                foreach(var p in projection.vertices[vertex].points.OrderByDescending(p=>p.weight).Take((int)quality))
                {value+=bones[p.bone].TransformPoint(p.position)*p.weight;total+=p.weight;}return value/total;
            }
            foreach(var authored in new[]{false,true})
            {
                var origin=authored ? new Vector3(-2058,-72.4624f,-1718) : Vector3.zero;
                ground.transform.SetPositionAndRotation(origin-Vector3.up*.05f,Quaternion.identity);ground.GetComponent<BoxCollider>().size=new Vector3(24,.1f,24);
                actor.transform.rotation=body.rotation=Quaternion.identity;
                type.GetMethod("SetSupport").Invoke(fixture,new object[]{new Plane(Vector3.up,origin),ground.GetComponent<Collider>()});
                for(var gaitIndex=0;gaitIndex<6;gaitIndex++)
                {
                    var gait=profile.directionalGaits[gaitIndex];var definition=proposal["gaits"][gaitIndex];
                    var local=gaitIndex<2 ? Vector2.left : gaitIndex<4 ? Vector2.right : Vector2.down;
                    var direction=new Vector3(local.x,0,local.y);var map=gait.phaseMap;
                    double Semantic(double native,double centre)
                    {
                        while(native<map[0].native)native+=1;while(native>map[^1].native)native-=1;
                        var index=0;while(index+1<map.Length-1 && native>map[index+1].native)index++;
                        var a=map[index];var b=map[index+1];var semantic=a.semantic+(native-a.native)/(b.native-a.native)*(b.semantic-a.semantic);
                        return semantic+Math.Round(centre-semantic);
                    }
                    var ordinal=0;
                    foreach(var patch in definition["patches"])
                    {
                        var vertex=(int)patch["vertex"];var count=(int)patch["stop"]-(int)patch["first"];
                        var first=Vector3.zero;var previous=Vector3.zero;var excursion=0f;var travel=0f;var minimum=float.PositiveInfinity;var maximum=float.NegativeInfinity;
                        for(var frame=0;frame<=count;frame++)
                        {
                            var seconds=Mathf.Lerp((float)patch["start"],(float)patch["end"],frame/(float)count);
                            var semantic=Semantic(seconds/(double)definition["duration"],ordinal*.25);
                            var position=origin+Vector3.up*.91f+direction*(float)(semantic*gait.metresPerCycle);
                            actor.transform.position=body.position=position;Physics.SyncTransforms();
                            type.GetMethod("Frame").Invoke(fixture,new object[]{gait.nominalSpeed,semantic,local,frame==0 ? 1f : 0f});
                            var point=Point(vertex);if(frame==0)first=point;else travel+=Vector3.Distance(previous,point);
                            excursion=Mathf.Max(excursion,Vector3.Distance(first,point));previous=point;
                            minimum=Mathf.Min(minimum,point.y-origin.y);maximum=Mathf.Max(maximum,point.y-origin.y);
                        }
                        rows.Add(new{clip=gait.sourceClip.name,influences=(int)quality,authoredOrigin=authored,contactOrdinal=ordinal++,vertex,samples=count+1,
                            pointExcursion=excursion,cumulativeTravel=travel,minimumGroundGap=minimum,maximumGroundGap=maximum,
                            contactAimMet=excursion<=.02f && travel<=.02f && minimum>=-.001f && maximum<=.008f});
                    }
                }
            }
        }
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{state="observed-contact-review-not-formal-qualification",utc=DateTime.UtcNow,
            runtimeModule=typeof(TopDown3DPlayerAnimationDriver).Module.ModuleVersionId,proposalSha256=Hash(proposalPath),
            scope="Actual single-output equipped graph; complete calibrated pure channels, fixed selected sole identities, one/two/four influences and near/authored world origins. No blended directional transition or physics acceptance is implied.",rows},Newtonsoft.Json.Formatting.Indented));
        return output;
    }
}
