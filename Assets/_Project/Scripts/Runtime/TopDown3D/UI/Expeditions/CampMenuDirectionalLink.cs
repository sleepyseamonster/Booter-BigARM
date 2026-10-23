using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Moves between native UI regions through the existing EventSystem stream.</summary>
    public sealed class CampMenuDirectionalLink : MonoBehaviour,IMoveHandler
    {
        private Action<AxisEventData> move;
        internal void Bind(Action<AxisEventData> handler){move=handler;}
        public void OnMove(AxisEventData data){move?.Invoke(data);}
    }
}
