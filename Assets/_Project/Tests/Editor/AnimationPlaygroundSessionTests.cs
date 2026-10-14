using System;
using System.IO;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundSessionTests
    {
        [Test]
        public void PolicyRequiresTheExactAuthoredSceneAndMarker()
        {
            Assert.That(TopDown3DAnimationPlaygroundSession.MatchesLabIdentity(TopDown3DAnimationPlaygroundSession.ScenePath, true), Is.True);
            Assert.That(TopDown3DAnimationPlaygroundSession.MatchesLabIdentity(TopDown3DAnimationPlaygroundSession.ScenePath, false), Is.False);
            Assert.That(TopDown3DAnimationPlaygroundSession.MatchesLabIdentity("Assets/_Project/Scenes/Production/GreaterWasteland.unity", true), Is.False);
            Assert.That(TopDown3DAnimationPlaygroundSession.MatchesLabIdentity("Assets/Other/CampLoopTesting.unity", true), Is.False);
        }

        [Test]
        public void SessionOutsideLabCannotAdvanceToPlayable()
        {
            var owner = new GameObject("Session fixture");
            try
            {
                var session = owner.AddComponent<TopDown3DAnimationPlaygroundSession>();
                Assert.That(session.CheckDependencies(), Is.False);
                Assert.That(session.Readiness, Is.EqualTo(AnimationPlaygroundReadiness.Failed));
                Assert.That(session.Advance(AnimationPlaygroundReadiness.PlayableReady), Is.False);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void ImportAndPreviewAreInsufficientForPhysicalEffects()
        {
            var clip = new AnimationClip();
            try
            {
                var binding = new AnimationPlaygroundMotionBinding {clip=clip, readiness=AnimationPlaygroundMotionReadiness.ImportQualified};
                Assert.That(binding.CanPreview, Is.True); Assert.That(binding.CanPerform, Is.False);
                binding.readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified;
                Assert.That(binding.CanPerform, Is.True);
                binding.restrictedReference=true;
                Assert.That(binding.CanPerform, Is.False); Assert.That(binding.CanPreview, Is.False);
            }
            finally { Object.DestroyImmediate(clip); }
        }

        // Scene-opening fixtures run only in the isolated validation candidate, never the author's loaded checkout.
        [Test]
        public void AuthoredLabBlocksRestoreAndAllCheckpointEntryPointsBeforeStartup()
        {
            var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
            GameObject owner=null, checkpoint=null;
            string path=null; byte[] previous=null; bool existed=false;
            try
            {
                owner = new GameObject("Session fixture"); SceneManager.MoveGameObjectToScene(owner, scene);
                var session=owner.AddComponent<TopDown3DAnimationPlaygroundSession>();
                Assert.That(session.CheckDependencies(),Is.False);
                Assert.That(session.UnavailableReason,Does.Contain("Private animation binding set is absent"));
                checkpoint=new GameObject("Checkpoint fixture"); SceneManager.MoveGameObjectToScene(checkpoint,scene);
                var service=checkpoint.AddComponent<GreaterWastelandInventorySaveService>();
                path=service.SavePath; existed=File.Exists(path); if(existed) previous=File.ReadAllBytes(path);
                Assert.That(service.IsSessionOnly, Is.True);
                typeof(GreaterWastelandInventorySaveService).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(service,null);
                Assert.That(service.Save(),Is.False);
                foreach(var method in new[]{"LateUpdate","OnApplicationQuit","OnDisable"})
                    typeof(GreaterWastelandInventorySaveService).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(service,null);
                var restore=typeof(GreaterWastelandInventorySaveService).GetMethod("Restore",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.That(restore.Invoke(service,new object[]{null}),Is.False);
                Assert.That(File.Exists(path),Is.EqualTo(existed));
                if(existed) Assert.That(File.ReadAllBytes(path),Is.EqualTo(previous));
                Assert.That(session.ControlsDirectory,Is.EqualTo(Path.Combine(Application.persistentDataPath,"AnimationPlayground")));
                Assert.That(TopDown3DAnimationPlaygroundSession.ResolveControlsDirectory(scene,"settings-only"),Is.EqualTo(session.ControlsDirectory));
            }
            finally
            {
                if(checkpoint!=null) Object.DestroyImmediate(checkpoint);
                if(owner!=null) Object.DestroyImmediate(owner);
                EditorSceneManager.CloseScene(scene,true);
            }
        }
    }
}
