using System.Text.Json;
using UnityEngine;
using BooterBigArm.TopDown3D;
using BooterBigArm.Editor;
using Planner=BooterBigArm.Editor.TopDown3DRockWorkbenchBaseRockGenerator;
class Program {
static float[] A(Vector3 v)=>new[]{v.x,v.y,v.z};static float[] Q(Quaternion q)=>new[]{q.x,q.y,q.z,q.w};
static float F(JsonElement e,string name)=>e.GetProperty(name).GetSingle();static Vector3 V(JsonElement e)=>new(e[0].GetSingle(),e[1].GetSingle(),e[2].GetSingle());
static void Main(string[] args){
var r=JsonDocument.Parse(File.ReadAllText(args[0])).RootElement.GetProperty("payload");var single=r.GetProperty("single");int seed=unchecked((int)r.GetProperty("seed").GetUInt32());
float width=F(single,"width"),length=F(single,"body_length");if(single.GetProperty("random_dimensions").GetBoolean()){var d=Planner.CreateBellCurvedStandaloneDimensions(seed);width=d.x;length=d.y;}
var profile=(TopDown3DRockSilhouetteProfile)r.GetProperty("profile").GetInt32();if(profile==TopDown3DRockSilhouetteProfile.Auto&&single.GetProperty("dark_auto_profile").GetBoolean())profile=Planner.ResolveDarkDesertSilhouetteProfile(seed);
int count=Math.Clamp(Mathf.RoundToInt(MathF.Sqrt(width*width*.55f+length*length*.45f)*.22f)+1,2,4);
var shape=r.GetProperty("shape");var plan=Planner.ApplyGoldenRockRestingPose(Planner.CreatePlan(seed,count,new(width,length,width),Mathf.InverseLerp(.35f,2.5f,length/width),F(shape,"asymmetry")*.001f,F(shape,"compaction")*.001f,profile,F(shape,"fractures")*.001f),seed);
var boxes=new List<TopDown3DRockWorkbenchBox>();var poses=new List<object>();var root=new Transform();
if(r.GetProperty("volumes").GetArrayLength()>0){foreach(var v in r.GetProperty("volumes").EnumerateArray()){var q=v.GetProperty("orientation");var transform=new Transform{position=V(v.GetProperty("center")),scale=V(v.GetProperty("half_size"))*2,rotation=new(q[0].GetSingle(),q[1].GetSingle(),q[2].GetSingle(),q[3].GetSingle())};int p=v.GetProperty("primitive").GetInt32();poses.Add(new{center=A(transform.position),scale=A(transform.scale),orientation=Q(transform.rotation),primitive=p,seed=v.GetProperty("shape_seed").GetUInt32()});var source=p==1?TopDown3DRockSourceShape.TaperedStone:(p==2?TopDown3DRockSourceShape.Wedge:(p==3?TopDown3DRockSourceShape.FractureCut:TopDown3DRockSourceShape.WeatheredBlock));boxes.Add(new(root,transform,source,unchecked((int)v.GetProperty("shape_seed").GetUInt32()),v.GetProperty("subtractive").GetBoolean()?TopDown3DRockVolumeOperation.Subtractive:TopDown3DRockVolumeOperation.Additive,F(shape,"edge_damage")*.001f));}}
else for(int i=0;i<plan.Count;i++){var s=plan[i];poses.Add(new{center=A(s.LocalPosition),scale=A(s.LocalScale),orientation=Q(s.LocalRotation),primitive=s.SourceShape==TopDown3DRockSourceShape.Wedge?2:(s.SourceShape==TopDown3DRockSourceShape.TaperedStone?1:(s.SourceShape==TopDown3DRockSourceShape.FractureCut?3:0)),seed=unchecked((uint)Planner.DeriveVolumeShapeSeed(seed,i))});boxes.Add(new(root,new Transform{position=s.LocalPosition,rotation=s.LocalRotation,scale=s.LocalScale},s.SourceShape,Planner.DeriveVolumeShapeSeed(seed,i),s.Operation,F(shape,"edge_damage")*.001f));}
var calibration=r.GetProperty("calibration");float voxel=F(calibration,"sampling_mm")*.001f*MathF.Pow(2,2-r.GetProperty("subdivisions").GetInt32());
if(!TopDown3DRockWorkbenchMesher.TryBuild(boxes,voxel,F(calibration,"fusion"),F(calibration,"relaxation"),out var result,out var error))throw new Exception(error);
float scale=F(calibration,"scale");var vertices=result.MeshData.Vertices.Select(v=>A(v*scale)).ToArray();
File.WriteAllText(args[1],JsonSerializer.Serialize(new{width,length,poses,vertices,triangles=result.MeshData.Triangles,cells=new[]{result.GridCells.x,result.GridCells.y,result.GridCells.z},voxel=result.EffectiveVoxelSize,components=result.ConnectedComponents}));
Console.WriteLine($"C# reference: {vertices.Length} vertices, {result.MeshData.Triangles.Length/3} triangles");
}}
