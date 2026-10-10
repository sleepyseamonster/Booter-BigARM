using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class TopDown3DSceneRenderingOverride : MonoBehaviour
    {
        [SerializeField] private bool disableTiltShift = true;
        private static readonly HashSet<TopDown3DSceneRenderingOverride> active = new HashSet<TopDown3DSceneRenderingOverride>();
        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);
        public static bool TiltShiftDisabledIn(Scene scene)
        {
            if (!scene.IsValid()) return false;
            foreach (var item in active)
                if (item != null && item.isActiveAndEnabled && item.disableTiltShift && item.gameObject.scene == scene) return true;
            return false;
        }
    }
}
