using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Browser camera coordinates: top-left translation, screen-down Y, center-anchored zoom.</summary>
    internal static class CampTreeCameraMath
    {
        internal static float Axis(float value)=>Mathf.Abs(value)<=.22f?0:Mathf.Sign(value)*(Mathf.Clamp01(Mathf.Abs(value))-.22f)/.78f;
        internal static float Zoom(float scale,float factor)=>Mathf.Clamp(scale*factor,.1f,1);
        internal static Vector2 ZoomOffset(Vector2 offset,float before,float after,Vector2 center)=>center-(center-offset)*(after/before);
        internal static Vector2 Clamp(Vector2 offset,float scale,Rect bounds,Vector2 center)
        {var point=(center-offset)/scale;point.x=Mathf.Clamp(point.x,bounds.xMin,bounds.xMax);point.y=Mathf.Clamp(point.y,bounds.yMin,bounds.yMax);return center-point*scale;}
        internal static float Fit(Vector2 viewport,Vector2 stage)=>Mathf.Clamp(Mathf.Min((viewport.x-40)/stage.x,(viewport.y-40)/stage.y),.1f,1);
        internal static float Connections(float scale)=>Mathf.Round(Mathf.Clamp01((scale-.25f)/.25f)*10)/10;
    }
}
