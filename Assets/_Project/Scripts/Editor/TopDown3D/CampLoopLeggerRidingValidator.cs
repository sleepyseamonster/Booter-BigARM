using System;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class CampLoopLeggerRidingValidator
    {
        public static void ValidateFromCli() => Validate(EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath));
        public static void Validate(Scene scene)
        {
            CampLoopTestingSceneBuilder.Validate(scene);
            var controllers = CampLoopLeggerRidingAuthoring.All<TopDown3DLeggerRidingController>(scene);
            if (controllers.Length != 1 || CampLoopLeggerRidingAuthoring.All<TopDown3DLeggerMountTarget>(scene).Length != 1)
                throw new InvalidOperationException("One riding controller and one mount target required.");
            var serialized = new SerializedObject(controllers[0]);
            foreach (var name in new[] { "player", "follower", "input", "cameraRig", "laboratory", "seatFrame" })
            {
                var reference = serialized.FindProperty(name).objectReferenceValue as Component;
                if (reference == null || reference.gameObject.scene != scene) throw new InvalidOperationException("Invalid scene reference: " + name);
            }
            var profile = serialized.FindProperty("profile").objectReferenceValue as TopDown3DLeggerRideProfile;
            if (profile == null || !profile.Validate(out _) || AssetDatabase.GetAssetPath(profile) != CampLoopLeggerRidingAuthoring.ProfilePath)
                throw new InvalidOperationException("The private riding profile is invalid.");
            foreach (var name in new[] { "exitFrames", "resetReturns" })
            {
                var array = serialized.FindProperty(name);
                if (array.arraySize < (name == "exitFrames" ? 2 : 1)) throw new InvalidOperationException("Missing anchors: " + name);
                for (int i = 0; i < array.arraySize; i++)
                {
                    var anchor = array.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    if (anchor == null || anchor.gameObject.scene != scene) throw new InvalidOperationException("Invalid anchor: " + name);
                    if (!Finite(anchor.position)) throw new InvalidOperationException("Non-finite anchor: " + name);
                }
            }
            var target = new SerializedObject(CampLoopLeggerRidingAuthoring.All<TopDown3DLeggerMountTarget>(scene)[0]);
            if (target.FindProperty("controller").objectReferenceValue != controllers[0]) throw new InvalidOperationException("Mount target owner mismatch.");
            var player = (TopDown3DPlayerMotor)serialized.FindProperty("player").objectReferenceValue;
            var follower = (TopDown3DBigArmFollower)serialized.FindProperty("follower").objectReferenceValue;
            var seat = (Transform)serialized.FindProperty("seatFrame").objectReferenceValue;
            var box = follower.GetComponent<BoxCollider>();
            var capsule = player.GetComponent<CapsuleCollider>();
            if (box == null || capsule == null || box.isTrigger || capsule.isTrigger || capsule.direction != 1
                || !Finite(box.size) || !Finite(box.center) || !Finite(capsule.center)
                || box.size.x <= 0 || box.size.y <= 0 || box.size.z <= 0
                || !float.IsFinite(capsule.height) || !float.IsFinite(capsule.radius)
                || capsule.radius <= 0 || capsule.height < capsule.radius * 2
                || !Finite(seat.position) || !seat.IsChildOf(follower.transform)
                || follower.transform.InverseTransformPoint(seat.position).y + capsule.center.y - capsule.height * .5f
                    < box.center.y + box.size.y * .5f + .02f)
                throw new InvalidOperationException("Invalid combined mount and standing rider geometry.");
            if (!TopDown3DAnimationPlaygroundSession.HasUnitPhysicsHierarchy(player.transform)
                || (follower.transform.lossyScale - Vector3.one).sqrMagnitude > .000001f)
                throw new InvalidOperationException("Unit actor physics scale required.");
            if (!TopDown3DAnimationPlaygroundSession.IsSessionOnly(scene)) throw new InvalidOperationException("Riding trial must remain session-only.");
            Debug.Log("LEGGER_RIDING_VALIDATION_PASS: single owner, native grid, private profile, exact scene bindings and session-only policy.");
        }
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
