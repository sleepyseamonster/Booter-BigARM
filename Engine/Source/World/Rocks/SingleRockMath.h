#pragma once
#include <algorithm>
#include <array>
#include <cmath>
#include <cstdint>
#include <limits>
#include <vector>
namespace engine::single_rock {
struct Mathf {
static constexpr float PI=3.14159265358979323846f,Deg2Rad=PI/180;
template<class T>static T Max(T a,T b){return std::max(a,b);} template<class T>static T Min(T a,T b){return std::min(a,b);} static float Min(float a,float b,float c){return Min(a,Min(b,c));}
template<class T>static T Clamp(T a,T lo,T hi){return std::clamp(a,lo,hi);} static float Clamp01(float x){return Clamp(x,0.f,1.f);} static float Abs(float x){return std::abs(x);} static float Sqrt(float x){return std::sqrt(x);} static float Sin(float x){return std::sin(x);} static float Cos(float x){return std::cos(x);} static float Atan2(float y,float x){return std::atan2(y,x);} static int CeilToInt(float x){return int(std::ceil(x));} static int RoundToInt(float x){return int(std::nearbyint(x));}
static float Repeat(float t,float length){return Clamp(t-std::floor(t/length)*length,0.f,length);} static float Lerp(float a,float b,float t){return a+(b-a)*Clamp01(t);} static float InverseLerp(float a,float b,float v){return a!=b?Clamp01((v-a)/(b-a)):0;} static float SmoothStep(float a,float b,float t){t=Clamp01(t);t=-2*t*t*t+3*t*t;return b*t+a*(1-t);}
};
struct Vector3 {
float x=0,y=0,z=0;
Vector3 operator+(Vector3 b)const{return {x+b.x,y+b.y,z+b.z};} Vector3 operator-(Vector3 b)const{return {x-b.x,y-b.y,z-b.z};} Vector3 operator*(float s)const{return {x*s,y*s,z*s};} Vector3 operator/(float s)const{return {x/s,y/s,z/s};}
float sqrMagnitude()const{return x*x+y*y+z*z;} Vector3 normalized()const{float m=std::sqrt(sqrMagnitude());return m>1e-5f?*this/m:Vector3{};}
static Vector3 Scale(Vector3 a,Vector3 b){return {a.x*b.x,a.y*b.y,a.z*b.z};} static Vector3 Min(Vector3 a,Vector3 b){return {std::min(a.x,b.x),std::min(a.y,b.y),std::min(a.z,b.z)};} static Vector3 Max(Vector3 a,Vector3 b){return {std::max(a.x,b.x),std::max(a.y,b.y),std::max(a.z,b.z)};}
static float Dot(Vector3 a,Vector3 b){return a.x*b.x+a.y*b.y+a.z*b.z;} static Vector3 Cross(Vector3 a,Vector3 b){return {a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x};}
static Vector3 ProjectOnPlane(Vector3 v,Vector3 n){return v-n*(Dot(v,n)/Dot(n,n));}
static Vector3 Slerp(Vector3 a,Vector3 b,float t){if(t<=0)return a;if(t>=1)return b;float am=std::sqrt(a.sqrMagnitude()),bm=std::sqrt(b.sqrMagnitude());auto u=a/am,v=b/bm;float d=std::clamp(Dot(u,v),-1.f,1.f);if(d>.9995f)return (a+(b-a)*t).normalized()*Mathf::Lerp(am,bm,t);auto relative=(v-u*d).normalized();if(relative.sqrMagnitude()<1e-8f)relative=Cross(u,std::abs(u.y)<.9f?Vector3{0,1,0}:Vector3{1,0,0}).normalized();float angle=std::acos(d)*t;return (u*std::cos(angle)+relative*std::sin(angle))*Mathf::Lerp(am,bm,t);}
static const Vector3 zero,one,up,right,forward;
};
inline const Vector3 Vector3::zero{},Vector3::one{1,1,1},Vector3::up{0,1,0},Vector3::right{1,0,0},Vector3::forward{0,0,1};
struct Vector2 {float x=0,y=0;};
struct Quaternion {
float x=0,y=0,z=0,w=1;
Quaternion operator*(Quaternion b)const{return {w*b.x+x*b.w+y*b.z-z*b.y,w*b.y+y*b.w+z*b.x-x*b.z,w*b.z+z*b.w+x*b.y-y*b.x,w*b.w-x*b.x-y*b.y-z*b.z};}
Vector3 operator*(Vector3 p)const{Vector3 q{x,y,z};auto t=Vector3::Cross(q,p)*2;return p+t*w+Vector3::Cross(q,t);}
static Quaternion Inverse(Quaternion q){float n=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;return {-q.x/n,-q.y/n,-q.z/n,q.w/n};}
static Quaternion AngleAxis(float degrees,Vector3 axis){axis=axis.normalized();float a=degrees*Mathf::Deg2Rad*.5f,s=std::sin(a);return {axis.x*s,axis.y*s,axis.z*s,std::cos(a)};}
static Quaternion Euler(Vector3 a){return AngleAxis(a.y,Vector3::up)*AngleAxis(a.x,Vector3::right)*AngleAxis(a.z,Vector3::forward);}
static Quaternion LookRotation(Vector3 f,Vector3){return AngleAxis(std::atan2(f.x,f.z)/Mathf::Deg2Rad,Vector3::up);}
static Quaternion FromToRotation(Vector3 a,Vector3 b){a=a.normalized();b=b.normalized();float d=Vector3::Dot(a,b);if(d>.999999f)return {};if(d<-.999999f)return AngleAxis(180,Vector3::Cross(a,std::abs(a.y)<.9f?Vector3::up:Vector3::right));auto c=Vector3::Cross(a,b);float s=std::sqrt((1+d)*2);return {c.x/s,c.y/s,c.z/s,s*.5f};}
};
struct Bounds {Vector3 min{},max{};Bounds()=default;Bounds(Vector3 c,Vector3 s):min(c-s*.5f),max(c+s*.5f){} Vector3 size()const{return max-min;} Vector3 center()const{return (min+max)*.5f;} void SetMinMax(Vector3 a,Vector3 b){min=a;max=b;}};
// System.Random's seeded subtractive algorithm. A reference C# runner verifies its sequence.
class Random {
std::array<int32_t,56> seeds{};int next=0,nextp=21;
public:
explicit Random(int32_t seed){int32_t subtraction=seed==INT32_MIN?INT32_MAX:std::abs(seed);int32_t mj=161803398-subtraction;seeds[55]=mj;int32_t mk=1;
for(int i=1;i<55;++i){int ii=(21*i)%55;seeds[ii]=mk;mk=int32_t(uint32_t(mj)-uint32_t(mk));if(mk<0)mk+=INT32_MAX;mj=seeds[ii];}
for(int k=1;k<5;++k)for(int i=1;i<56;++i){seeds[i]=int32_t(uint32_t(seeds[i])-uint32_t(seeds[1+(i+30)%55]));if(seeds[i]<0)seeds[i]+=INT32_MAX;}}
double NextDouble(){if(++next>=56)next=1;if(++nextp>=56)nextp=1;int ret=seeds[next]-seeds[nextp];if(ret==INT32_MAX)--ret;if(ret<0)ret+=INT32_MAX;seeds[next]=ret;return ret*(1.0/INT32_MAX);} int Next(int lo,int hi){return int(NextDouble()*(hi-lo))+lo;}
};
enum class Profile {Auto,Boulder,Slab,AngularChunk,SplitLobe,Shard,FracturedBoulder,BlockyMonolith,BrokenSlab};
enum class Role {Core,Support,Detail}; enum class Shape {WeatheredBlock,Wedge,TaperedStone,FractureCut}; enum class Operation {Additive,Subtractive};
struct Spec {Vector3 LocalPosition;Quaternion LocalRotation;Vector3 LocalScale;Role Role;Shape SourceShape=Shape::WeatheredBlock;Operation Operation=Operation::Additive;};
struct Defaults {
static constexpr int GoldenRockSeed=2126351350;static constexpr float MinimumGeneratedRockDimension=.3f,MaximumGeneratedRockDimension=1.2f;
static int CalculateFractureCount(float amount){return amount<=.001f?0:(amount<.46f?1:(amount<.84f?2:3));}
};
}
