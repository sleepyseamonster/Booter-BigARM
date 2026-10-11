using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Immutable world-meter box data; safe to evaluate without Unity objects on a worker.</summary>
    internal readonly struct SmoothRockBox
    {
        internal readonly Vector3 Center, X, Y, Z, HalfSize;
        internal readonly Bounds Bounds;

        internal SmoothRockBox(Vector3 center, Quaternion rotation, Vector3 size)
        {
            Center = center;
            X = rotation * Vector3.right; Y = rotation * Vector3.up; Z = rotation * Vector3.forward;
            HalfSize = size * 0.5f;
            var extent = Abs(X) * HalfSize.x + Abs(Y) * HalfSize.y + Abs(Z) * HalfSize.z;
            Bounds = new Bounds(center, extent * 2f);
        }

        internal static SmoothRockBox Capture(Transform transform)
        {
            var matrix = transform.localToWorldMatrix;
            var x = matrix.MultiplyVector(Vector3.right);
            var y = matrix.MultiplyVector(Vector3.up);
            var z = matrix.MultiplyVector(Vector3.forward);
            var size = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (!Finite(transform.position) || !Finite(size) || size.x < .001f || size.y < .001f || size.z < .001f
                || Vector3.Dot(Vector3.Cross(x, y), z) <= 0f)
                throw new InvalidOperationException("Rock cube requires a finite, positive size (at least 1 mm) and no mirrored transform.");
            x /= size.x; y /= size.y; z /= size.z;
            if (Mathf.Abs(Vector3.Dot(x,y)) > .0001f || Mathf.Abs(Vector3.Dot(x,z)) > .0001f || Mathf.Abs(Vector3.Dot(y,z)) > .0001f)
                throw new InvalidOperationException("Rock cube has a sheared parent transform; use an unscaled parent.");
            return new SmoothRockBox(transform.position, Quaternion.LookRotation(z,y), size);
        }

        internal float Distance(Vector3 point)
        {
            var p = point - Center;
            var q = new Vector3(Mathf.Abs(Vector3.Dot(p,X)), Mathf.Abs(Vector3.Dot(p,Y)), Mathf.Abs(Vector3.Dot(p,Z))) - HalfSize;
            var outside = Vector3.Max(q, Vector3.zero);
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y,q.z)),0f);
        }

        internal bool SameGeometry(SmoothRockBox other) => Center.Equals(other.Center) && HalfSize.Equals(other.HalfSize)
            && X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        internal bool Raycast(Ray ray,out float distance)
        {
            distance=0f; float far=float.PositiveInfinity;
            var origin=ray.origin-Center;
            var axes=new[]{X,Y,Z}; var sizes=new[]{HalfSize.x,HalfSize.y,HalfSize.z};
            for(int i=0;i<3;i++)
            {
                float p=Vector3.Dot(origin,axes[i]),d=Vector3.Dot(ray.direction,axes[i]);
                if(Mathf.Abs(d)<.000001f) { if(Mathf.Abs(p)>sizes[i]) return false; continue; }
                float a=(-sizes[i]-p)/d,b=(sizes[i]-p)/d;
                distance=Mathf.Max(distance,Mathf.Min(a,b)); far=Mathf.Min(far,Mathf.Max(a,b));
                if(distance>far) return false;
            }
            return far>=0f;
        }

        internal static bool Touches(SmoothRockBox a, SmoothRockBox b)
        {
            var delta = b.Center - a.Center;
            var tolerance = .00001f * Mathf.Max(1f, Mathf.Max(a.HalfSize.magnitude,b.HalfSize.magnitude));
            if (Separated(a,b,delta,a.X,tolerance) || Separated(a,b,delta,a.Y,tolerance) || Separated(a,b,delta,a.Z,tolerance)
                || Separated(a,b,delta,b.X,tolerance) || Separated(a,b,delta,b.Y,tolerance) || Separated(a,b,delta,b.Z,tolerance)) return false;
            var aa = new[] { a.X,a.Y,a.Z }; var bb = new[] { b.X,b.Y,b.Z };
            foreach (var x in aa) foreach (var y in bb)
                if (Separated(a,b,delta,Vector3.Cross(x,y),tolerance)) return false;
            return true;
        }

        private static bool Separated(SmoothRockBox a, SmoothRockBox b, Vector3 delta, Vector3 axis, float tolerance)
        {
            var length = axis.magnitude;
            if (length < .000001f) return false;
            axis /= length;
            float Radius(SmoothRockBox box) => Mathf.Abs(Vector3.Dot(axis,box.X))*box.HalfSize.x
                + Mathf.Abs(Vector3.Dot(axis,box.Y))*box.HalfSize.y + Mathf.Abs(Vector3.Dot(axis,box.Z))*box.HalfSize.z;
            return Mathf.Abs(Vector3.Dot(delta,axis)) > Radius(a)+Radius(b)+tolerance;
        }

        private static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x),Mathf.Abs(value.y),Mathf.Abs(value.z));
        private static bool Finite(Vector3 value) => !float.IsNaN(value.x+value.y+value.z) && !float.IsInfinity(value.x+value.y+value.z);
    }

    internal sealed class SmoothRockField
    {
        internal readonly Vector3 Origin;
        internal readonly Bounds Bounds;
        internal readonly Vector3Int Cells;
        internal readonly float Spacing;
        internal readonly float[] Values;
        internal SmoothRockField(Vector3 origin, Bounds bounds, Vector3Int cells, float spacing, float[] values)
        { Origin=origin; Bounds=bounds; Cells=cells; Spacing=spacing; Values=values; }

        internal float Sample(Vector3 point)
        {
            var q=(point-Origin-Bounds.min)/Spacing;
            int x=Mathf.Clamp(Mathf.FloorToInt(q.x),0,Cells.x-1), y=Mathf.Clamp(Mathf.FloorToInt(q.y),0,Cells.y-1), z=Mathf.Clamp(Mathf.FloorToInt(q.z),0,Cells.z-1);
            float tx=Mathf.Clamp01(q.x-x),ty=Mathf.Clamp01(q.y-y),tz=Mathf.Clamp01(q.z-z);
            float V(int dx,int dy,int dz) => Values[((z+dz)*(Cells.y+1)+y+dy)*(Cells.x+1)+x+dx];
            return Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(V(0,0,0),V(1,0,0),tx),Mathf.Lerp(V(0,1,0),V(1,1,0),tx),ty),
                Mathf.Lerp(Mathf.Lerp(V(0,0,1),V(1,0,1),tx),Mathf.Lerp(V(0,1,1),V(1,1,1),tx),ty),tz);
        }
    }

    internal static class SmoothRockCubeGeometry
    {
        // Fixed implementation constants, deliberately not exposed as authoring settings.
        internal const float Spacing = .04f;
        internal const float JoinRadius = .2f;
        internal const int RadiusCells = 5;
        private const int MaximumSamples = 1000000;

        internal static float UnionDistance(IReadOnlyList<SmoothRockBox> boxes, Vector3 point)
        {
            float value=float.PositiveInfinity;
            for(int i=0;i<boxes.Count;i++) value=Mathf.Min(value,boxes[i].Distance(point));
            return value;
        }

        /// <summary>
        /// Sampled spherical closing of the exact box union: dilate by r, then erode by a
        /// lattice ball. Min/max make this permutation-independent and duplicate-neutral.
        /// Unlike ordered smooth minima, no primitive multiplicity enters the geometry.
        /// </summary>
        internal static SmoothRockField BuildField(IReadOnlyList<SmoothRockBox> boxes, CancellationToken cancellation=default)
        {
            if(boxes==null || boxes.Count==0 || boxes.Count>32) throw new InvalidOperationException("Rock cube group must contain 1–32 sources.");
            var bounds=boxes[0].Bounds;
            for(int i=1;i<boxes.Count;i++) bounds.Encapsulate(boxes[i].Bounds);
            // Keep float mesh coordinates near zero, preserving a fixed world lattice.
            var origin=new Vector3(Mathf.Round(bounds.center.x/Spacing),Mathf.Round(bounds.center.y/Spacing),Mathf.Round(bounds.center.z/Spacing))*Spacing;
            var localBoxes=new SmoothRockBox[boxes.Count];
            for(int i=0;i<boxes.Count;i++) localBoxes[i]=new SmoothRockBox(boxes[i].Center-origin,Quaternion.LookRotation(boxes[i].Z,boxes[i].Y),boxes[i].HalfSize*2f);
            bounds.center-=origin;
            bounds.Expand((JoinRadius+Spacing*2f)*2f);
            var low=new Vector3(Mathf.Floor(bounds.min.x/Spacing),Mathf.Floor(bounds.min.y/Spacing),Mathf.Floor(bounds.min.z/Spacing))*Spacing;
            var cells=new Vector3Int(Mathf.CeilToInt((bounds.max.x-low.x)/Spacing),Mathf.CeilToInt((bounds.max.y-low.y)/Spacing),Mathf.CeilToInt((bounds.max.z-low.z)/Spacing));
            if(cells.x<2 || cells.y<2 || cells.z<2 || cells.x>4096 || cells.y>4096 || cells.z>4096)
                throw new InvalidOperationException("Rock cube bounds exceed the bounded study grid.");
            var points=cells+Vector3Int.one;
            long count=(long)points.x*points.y*points.z;
            var extended=points+Vector3Int.one*(RadiusCells*2);
            long extendedCount=(long)extended.x*extended.y*extended.z;
            if(count>MaximumSamples || extendedCount>1600000) throw new InvalidOperationException("Rock cube arrangement exceeds the bounded study grid; sources remain visible.");
            var union=new float[(int)extendedCount];
            for(int z=0;z<extended.z;z++)
            {
                cancellation.ThrowIfCancellationRequested();
                for(int y=0;y<extended.y;y++) for(int x=0;x<extended.x;x++)
                    union[(z*extended.y+y)*extended.x+x]=UnionDistance(localBoxes,low+new Vector3(x-RadiusCells,y-RadiusCells,z-RadiusCells)*Spacing);
            }
            // Face directions resolve most unchanged planar samples immediately.
            var offsets=new List<int> { RadiusCells,-RadiusCells,RadiusCells*extended.x,-RadiusCells*extended.x,
                RadiusCells*extended.x*extended.y,-RadiusCells*extended.x*extended.y };
            for(int z=-RadiusCells;z<=RadiusCells;z++) for(int y=-RadiusCells;y<=RadiusCells;y++) for(int x=-RadiusCells;x<=RadiusCells;x++)
                if(x*x+y*y+z*z<=RadiusCells*RadiusCells && !(Mathf.Abs(x)+Mathf.Abs(y)+Mathf.Abs(z)==RadiusCells && (x==0&&y==0 || x==0&&z==0 || y==0&&z==0)))
                    offsets.Add((z*extended.y+y)*extended.x+x);
            var values=new float[(int)count];
            for(int z=0;z<points.z;z++)
            {
                cancellation.ThrowIfCancellationRequested();
                for(int y=0;y<points.y;y++) for(int x=0;x<points.x;x++)
                {
                    int center=((z+RadiusCells)*extended.y+y+RadiusCells)*extended.x+x+RadiusCells;
                    float closed=float.NegativeInfinity;
                    for(int k=0;k<offsets.Count;k++)
                    {
                        closed=Mathf.Max(closed,union[center+offsets[k]]-JoinRadius);
                        if(closed>=union[center]) break;
                    }
                    // Retain the original union against lattice-ball approximation error.
                    float value=Mathf.Min(union[center],closed);
                    if(Mathf.Abs(value)<Spacing*.00001f) value=Spacing*.00001f;
                    values[(z*points.y+y)*points.x+x]=value;
                }
            }
            return new SmoothRockField(origin,new Bounds(low+(Vector3)cells*Spacing*.5f,(Vector3)cells*Spacing),cells,Spacing,values);
        }
    }
}
