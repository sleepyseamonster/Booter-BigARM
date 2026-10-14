using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class CampLoopNpcValidator
    {
        public static void ValidateFromCli() => Validate(EditorSceneManager.OpenScene(CampLoopNpcAuthoring.ScenePath, OpenSceneMode.Single));
        public static void Validate(Scene scene)
        {
            if (scene.path != CampLoopNpcAuthoring.ScenePath) throw new InvalidOperationException("NPC validator is lab-specific.");
            var actors = CampLoopNpcAuthoring.All<NpcActor>(scene); var trials = CampLoopNpcAuthoring.All<CampLoopNpcTrial>(scene);
            if (actors.Length != 1 || trials.Length != 1 || actors[0].StableId != "lab.npc.skittish_legger.001"
                || actors[0].ThreatTarget != CampLoopNpcAuthoring.All<TopDown3DPlayerMotor>(scene).Single().transform
                || CampLoopNpcAuthoring.All<TopDown3DAnimationPlaygroundSession>(scene).Length != 1
                || actors[0].Definition == null || !actors[0].Definition.Validate(out _)
                || !actors[0].Motor.ValidateShape(out _)) throw new InvalidOperationException("NPC identity, target or ownership mismatch.");
            Physics.SyncTransforms();
            if (!actors[0].Initialize(out var reason)) throw new InvalidOperationException(reason);
            CampLoopTestingSceneBuilder.Validate(scene);
            Debug.Log("CAMP_NPC_VALIDATED: one grounded Lager, distinct companion, immutable profile and explicit laboratory binding.");
        }
    }
}
