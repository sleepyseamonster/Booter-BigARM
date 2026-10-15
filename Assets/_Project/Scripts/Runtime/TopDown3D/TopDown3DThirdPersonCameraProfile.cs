using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Third Person Camera Profile")]
    public sealed class TopDown3DThirdPersonCameraProfile : ScriptableObject
    {
        public string[] enabledScenePaths = { "Assets/_Project/Scenes/Reference/CampLoopTesting.unity" };
        public bool IsEnabledForScene(string path)
        {
            if (!IsValid || enabledScenePaths == null) return false;
            foreach (var enabledPath in enabledScenePaths) if (enabledPath == path) return true;
            return false;
        }
        [Min(1)] public float distance = 5;
        [Min(0)] public float pivotHeight = 1.6f;
        public float startingPitch = 12;
        public Vector2 pitchLimits = new(-35, 65);
        public Vector3 positionDamping = new(.12f, .25f, .12f);
        [Min(.1f)] public float clearanceRadius = .35f;
        [Min(.01f)] public float collisionReturnTime = .3f;
        public bool IsValid => float.IsFinite(distance) && distance >= 1 && float.IsFinite(pivotHeight) && pivotHeight >= 0
            && float.IsFinite(startingPitch) && float.IsFinite(pitchLimits.x) && float.IsFinite(pitchLimits.y)
            && pitchLimits.x >= -80 && pitchLimits.y <= 80 && pitchLimits.x <= startingPitch && startingPitch <= pitchLimits.y
            && float.IsFinite(clearanceRadius) && clearanceRadius >= .1f && float.IsFinite(collisionReturnTime) && collisionReturnTime > 0
            && float.IsFinite(positionDamping.x) && float.IsFinite(positionDamping.y) && float.IsFinite(positionDamping.z)
            && positionDamping.x >= 0 && positionDamping.y >= 0 && positionDamping.z >= 0;
    }
}
