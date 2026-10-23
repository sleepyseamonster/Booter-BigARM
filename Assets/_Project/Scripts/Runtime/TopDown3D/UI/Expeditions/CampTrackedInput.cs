using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    internal sealed class CampTrackedStickRepeat
    {
        private int previous;
        private double next;
        internal void Reset(){previous=0;next=0;}
        internal int Sample(float axis,double now)
        {
            int direction=float.IsFinite(axis)&&Mathf.Abs(axis)>.55f?Math.Sign(axis):0;
            if(direction==0){Reset();return 0;}
            if(direction!=previous){previous=direction;next=now+.36;return direction;}
            if(now<next)return 0;next=now+.125;return direction;
        }
    }
}
