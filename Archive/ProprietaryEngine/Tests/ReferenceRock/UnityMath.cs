// Test-only math adapters for running the unchanged project C# algorithms without Unity.
using System;
using System.Collections.Generic;
namespace UnityEngine {
public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
public struct Vector3 : IEquatable<Vector3> {
public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
public static Vector3 zero=>new(0,0,0);public static Vector3 one=>new(1,1,1);public static Vector3 up=>new(0,1,0);public static Vector3 right=>new(1,0,0);public static Vector3 forward=>new(0,0,1);
public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>MathF.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>1e-5f?this/magnitude:zero;public void Normalize(){this=normalized;}
public static Vector3 operator+(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float s)=>new(a.x*s,a.y*s,a.z*s);public static Vector3 operator/(Vector3 a,float s)=>new(a.x/s,a.y/s,a.z/s);
public static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);public static Vector3 Min(Vector3 a,Vector3 b)=>new(MathF.Min(a.x,b.x),MathF.Min(a.y,b.y),MathF.Min(a.z,b.z));public static Vector3 Max(Vector3 a,Vector3 b)=>new(MathF.Max(a.x,b.x),MathF.Max(a.y,b.y),MathF.Max(a.z,b.z));
public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static Vector3 Cross(Vector3 a,Vector3 b)=>new(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
public static Vector3 ProjectOnPlane(Vector3 v,Vector3 n)=>v-n*(Dot(v,n)/Dot(n,n));public static Vector3 LerpUnclamped(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
public static Vector3 Slerp(Vector3 a,Vector3 b,float t){if(t<=0)return a;if(t>=1)return b;float am=a.magnitude,bm=b.magnitude;var u=a/am;var v=b/bm;float d=Math.Clamp(Dot(u,v),-1,1);if(d>.9995f)return (a+(b-a)*t).normalized*Mathf.Lerp(am,bm,t);var relative=(v-u*d).normalized;if(relative.sqrMagnitude<1e-8f)relative=Cross(u,MathF.Abs(u.y)<.9f?up:right).normalized;float angle=MathF.Acos(d)*t;return (u*MathF.Cos(angle)+relative*MathF.Sin(angle))*Mathf.Lerp(am,bm,t);}
public bool Equals(Vector3 b)=>x.Equals(b.x)&&y.Equals(b.y)&&z.Equals(b.z);public override bool Equals(object o)=>o is Vector3 v&&Equals(v);public override int GetHashCode()=>HashCode.Combine(x,y,z);
}
public struct Vector3Int {public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}public static Vector3Int one=>new(1,1,1);public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);public static explicit operator Vector3(Vector3Int a)=>new(a.x,a.y,a.z);}
public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
public struct Quaternion {public float x,y,z,w;public Quaternion(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}
public static Quaternion identity=>new(0,0,0,1);
public static Quaternion operator*(Quaternion a,Quaternion b)=>new(a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,a.w*b.y+a.y*b.w+a.z*b.x-a.x*b.z,a.w*b.z+a.z*b.w+a.x*b.y-a.y*b.x,a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z);
public static Vector3 operator*(Quaternion q,Vector3 p){var v=new Vector3(q.x,q.y,q.z);var t=Vector3.Cross(v,p)*2;return p+t*q.w+Vector3.Cross(v,t);}
public static Quaternion Inverse(Quaternion q){float n=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;return new(-q.x/n,-q.y/n,-q.z/n,q.w/n);}
public static Quaternion AngleAxis(float deg,Vector3 v){v=v.normalized;float a=deg*Mathf.Deg2Rad*.5f,s=MathF.Sin(a);return new(v.x*s,v.y*s,v.z*s,MathF.Cos(a));}
public static Quaternion Euler(float x,float y,float z)=>AngleAxis(y,Vector3.up)*AngleAxis(x,Vector3.right)*AngleAxis(z,Vector3.forward);
public static Quaternion LookRotation(Vector3 f,Vector3 up)=>AngleAxis(MathF.Atan2(f.x,f.z)/Mathf.Deg2Rad,Vector3.up);
public static Quaternion FromToRotation(Vector3 a,Vector3 b){a=a.normalized;b=b.normalized;float d=Vector3.Dot(a,b);if(d>.999999f)return identity;if(d<-.999999f)return AngleAxis(180,Vector3.Cross(a,MathF.Abs(a.y)<.9f?Vector3.up:Vector3.right));var c=Vector3.Cross(a,b);float s=MathF.Sqrt((1+d)*2);return new(c.x/s,c.y/s,c.z/s,s*.5f);}
}
public static class Mathf {public const float PI=MathF.PI,Deg2Rad=PI/180;public static float Max(float a,float b)=>MathF.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>MathF.Min(a,b);public static float Min(float a,float b,float c)=>Min(a,Min(b,c));public static float Clamp(float a,float lo,float hi)=>Math.Clamp(a,lo,hi);public static int Clamp(int a,int lo,int hi)=>Math.Clamp(a,lo,hi);public static float Clamp01(float a)=>Clamp(a,0,1);public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public static float InverseLerp(float a,float b,float v)=>a!=b?Clamp01((v-a)/(b-a)):0;public static float SmoothStep(float a,float b,float t){t=Clamp01(t);t=-2*t*t*t+3*t*t;return b*t+a*(1-t);}public static float Repeat(float t,float length)=>Clamp(t-MathF.Floor(t/length)*length,0,length);public static float Abs(float x)=>MathF.Abs(x);public static float Sqrt(float x)=>MathF.Sqrt(x);public static float Sin(float x)=>MathF.Sin(x);public static float Cos(float x)=>MathF.Cos(x);public static float Atan2(float y,float x)=>MathF.Atan2(y,x);public static float Pow(float x,float y)=>MathF.Pow(x,y);public static int RoundToInt(float x)=>(int)MathF.Round(x);public static int CeilToInt(float x)=>(int)MathF.Ceiling(x);}
public struct Bounds {public Vector3 min,max;public Bounds(Vector3 c,Vector3 s){min=c-s*.5f;max=c+s*.5f;}public Vector3 center=>(min+max)*.5f;public Vector3 size=>max-min;public void SetMinMax(Vector3 a,Vector3 b){min=a;max=b;}public void Encapsulate(Vector3 p){min=Vector3.Min(min,p);max=Vector3.Max(max,p);}public void Encapsulate(Bounds b){Encapsulate(b.min);Encapsulate(b.max);}public void Expand(float f){min-=Vector3.one*f*.5f;max+=Vector3.one*f*.5f;}}
public struct Matrix4x4 {public System.Numerics.Matrix4x4 m;public Matrix4x4 inverse {get{System.Numerics.Matrix4x4.Invert(m,out var a);return new(){m=a};}}public static Matrix4x4 operator*(Matrix4x4 a,Matrix4x4 b)=>new(){m=b.m*a.m};public Vector3 MultiplyPoint3x4(Vector3 p){var v=System.Numerics.Vector3.Transform(new(p.x,p.y,p.z),m);return new(v.X,v.Y,v.Z);}public Vector3 MultiplyVector(Vector3 p){var v=System.Numerics.Vector3.TransformNormal(new(p.x,p.y,p.z),m);return new(v.X,v.Y,v.Z);}}
public class Transform {public Vector3 position,scale=Vector3.one;public Quaternion rotation=Quaternion.identity;public Matrix4x4 localToWorldMatrix=>new(){m=System.Numerics.Matrix4x4.CreateScale(scale.x,scale.y,scale.z)*System.Numerics.Matrix4x4.CreateFromQuaternion(new(rotation.x,rotation.y,rotation.z,rotation.w))*System.Numerics.Matrix4x4.CreateTranslation(position.x,position.y,position.z)};public Matrix4x4 worldToLocalMatrix=>localToWorldMatrix.inverse;}
public struct Color32 {public Color32(byte r,byte g,byte b,byte a){}}
public enum HideFlags {DontSaveInEditor=1,DontUnloadUnusedAsset=2}public class Mesh {public string name;public HideFlags hideFlags;public Rendering.IndexFormat indexFormat;public Vector3[] vertices;public int[] triangles;public Color32[] colors32;public void RecalculateNormals(){}public void RecalculateBounds(){}}
}
namespace UnityEngine.Rendering {public enum IndexFormat {UInt16,UInt32}}
namespace BooterBigArm.TopDown3D {
public enum TopDown3DRockSilhouetteProfile {Auto,Boulder,Slab,AngularChunk,SplitLobe,Shard,FracturedBoulder,BlockyMonolith,BrokenSlab}
public enum TopDown3DRockSourceShape {WeatheredBlock,Wedge,TaperedStone,FractureCut}public enum TopDown3DRockVolumeOperation {Additive,Subtractive}
public class TopDown3DRockWorkbenchAuthoring {public const int GoldenRockSeed=2126351350;public const float MinimumGeneratedRockDimension=.3f,MaximumGeneratedRockDimension=1.2f;public static int CalculateFractureCount(float a)=>a<=.001f?0:(a<.46f?1:(a<.84f?2:3));}
public class TopDown3DIndexedMeshData {public UnityEngine.Vector3[] Vertices;public int[] Triangles;public TopDown3DIndexedMeshData(UnityEngine.Vector3[] v,int[] t){Vertices=v;Triangles=t;}}
public class TopDown3DMeshTopologyReport {public bool IsValid;public string Error;public double SignedVolume;public TopDown3DMeshTopologyReport(bool ok,string e,int v,int t,double volume){IsValid=ok;Error=e;SignedVolume=volume;}}
}
