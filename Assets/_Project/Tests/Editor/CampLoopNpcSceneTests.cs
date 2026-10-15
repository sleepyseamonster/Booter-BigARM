using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class CampLoopNpcSceneTests
    {
        [Test] public void SavedTrialHasIndependentIdentityAndNoCompanionFollower()
        {
            var scene = EditorSceneManager.OpenScene(CampLoopNpcAuthoring.ScenePath, OpenSceneMode.Single);
            CampLoopNpcValidator.Validate(scene);
            var actor = CampLoopNpcAuthoring.All<NpcActor>(scene).Single();
            Assert.That(actor.GetComponent<TopDown3DBigArmFollower>(), Is.Null);
            Assert.That(actor.GetComponent<TopDown3DBigArmCargo>(), Is.Null);
            Assert.That(actor.Definition.DisplayName, Is.EqualTo("Lager"));
            Assert.That(CampLoopNpcAuthoring.All<TopDown3DBigArmFollower>(scene).Length, Is.EqualTo(1));
            Assert.That(TopDown3DAnimationPlaygroundSession.IsSessionOnly(scene), Is.True);
        }
        [Test] public void PrefabHasNoSceneTargetAndProfileIsImmutableAtRuntime()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(CampLoopNpcAuthoring.PrefabPath);
            Assert.That(prefab, Is.Not.Null); var actor = prefab.GetComponent<NpcActor>();
            Assert.That(actor.ThreatTarget, Is.Null); var tuning = actor.Definition.Profile.Snapshot();
            tuning.comfort = 9; Assert.That(actor.Definition.Profile.Snapshot().comfort, Is.EqualTo(10));
        }
        [Test] public void InstalledTerrainOffersEscapeWhenBooterApproaches()
        {
            var scene = EditorSceneManager.OpenScene(CampLoopNpcAuthoring.ScenePath, OpenSceneMode.Single);
            var actor = CampLoopNpcAuthoring.All<NpcActor>(scene).Single();
            UnityEngine.Physics.SyncTransforms();
            Assert.That(actor.Initialize(out var reason), Is.True, reason);
            var tuning = actor.Tuning;
            // Inspect actual authored terrain/physics in Edit Mode, without running gameplay.
            var threat = actor.transform.position - UnityEngine.Vector3.forward * 3;
            actor.Navigation.Plan(actor.Motor, new NpcIntent(threat, tuning.fleeSpeed), tuning, actor.transform.position, 35, 0);
            Assert.That(actor.Navigation.HasRoute, Is.True, actor.Navigation.Reason + " / " + string.Join("; ", actor.Navigation.Candidates.Select(c => c.Reason)));
        }
        [TestCase(0)] [TestCase(90)] [TestCase(180)] [TestCase(270)]
        public void InstalledNpcDetectsNearbyBooterAndCommandsMovement(int angle)
        {
            var scene = EditorSceneManager.OpenScene(CampLoopNpcAuthoring.ScenePath, OpenSceneMode.Single);
            var actor = CampLoopNpcAuthoring.All<NpcActor>(scene).Single();
            var session = CampLoopNpcAuthoring.All<TopDown3DAnimationPlaygroundSession>(scene).Single();
            Assert.That(session.CheckDependencies(), Is.True, session.UnavailableReason);
            session.Advance(AnimationPlaygroundReadiness.ServicesBound); session.Advance(AnimationPlaygroundReadiness.VisualReady);
            var point = actor.transform.position + UnityEngine.Quaternion.Euler(0, angle, 0) * UnityEngine.Vector3.forward * 3;
            Assert.That(actor.Motor.TryGround(point, actor.Definition.Profile.Snapshot(), true, out var ground), Is.True);
            var target = actor.ThreatTarget;
            target.position = ground + UnityEngine.Vector3.up * .9f;
            target.GetComponent<UnityEngine.Rigidbody>().position = target.position;
            UnityEngine.Physics.SyncTransforms();
            var trial = CampLoopNpcAuthoring.All<CampLoopNpcTrial>(scene).Single(); trial.RefreshGate();
            Assert.That(actor.IsArmed, Is.True, trial.Failure);
            actor.Tick(10, .02f);
            Assert.That(actor.Observation.Valid, Is.True, "Nearby Booter was not perceived at angle " + angle);
            Assert.That(actor.Behavior, Is.EqualTo(NpcBehavior.Flee));
            Assert.That(actor.Motor.Speed, Is.GreaterThan(0), actor.Failure + " / " + actor.Navigation.Reason);
        }
        [Test] public void TemporaryStartupObstructionCanRecoverWithoutReenablingTrial()
        {
            var scene = EditorSceneManager.OpenScene(CampLoopNpcAuthoring.ScenePath, OpenSceneMode.Single);
            var actor = CampLoopNpcAuthoring.All<NpcActor>(scene).Single();
            var session = CampLoopNpcAuthoring.All<TopDown3DAnimationPlaygroundSession>(scene).Single();
            Assert.That(session.CheckDependencies(), Is.True, session.UnavailableReason);
            session.Advance(AnimationPlaygroundReadiness.ServicesBound); session.Advance(AnimationPlaygroundReadiness.VisualReady);
            var obstruction = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
            obstruction.transform.position = actor.transform.position + UnityEngine.Vector3.up * .7f;
            UnityEngine.Physics.SyncTransforms();
            var trial = CampLoopNpcAuthoring.All<CampLoopNpcTrial>(scene).Single(); trial.RefreshGate();
            Assert.That(actor.IsArmed, Is.False); Assert.That(trial.Failure, Does.Contain("support"));
            UnityEngine.Object.DestroyImmediate(obstruction); UnityEngine.Physics.SyncTransforms();
            trial.RefreshGate();
            Assert.That(actor.IsArmed, Is.True, trial.Failure); Assert.That(trial.Failure, Is.Null);
        }
        [Test] public void LagerPrefabHasOneVisibleRedCubeAndNoDecorativeColliders()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(CampLoopNpcAuthoring.PrefabPath);
            var visual = prefab.transform.Find("Visual");
            var renderers = visual.GetComponentsInChildren<UnityEngine.Renderer>();
            Assert.That(renderers.Length, Is.EqualTo(1));
            Assert.That(renderers[0].name, Is.EqualTo("Body"));
            Assert.That(renderers[0].transform.localScale, Is.EqualTo(UnityEngine.Vector3.one * 1.25f));
            Assert.That(renderers[0].sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Unlit"));
            Assert.That(renderers[0].sharedMaterial.GetColor("_BaseColor"), Is.EqualTo(UnityEngine.Color.red));
            Assert.That(visual.GetComponentsInChildren<UnityEngine.Collider>(true), Is.Empty);
        }
        [TestCase("CampLoop.Aaa")] [TestCase("CampLoop.Zzz")]
        public void FailedResetBeforeOrAfterNpcCannotRearmUntilSuccessfulRetry(string failureId)
        {
            var scene = EditorSceneManager.OpenScene(CampLoopNpcAuthoring.ScenePath, OpenSceneMode.Single);
            var session = CampLoopNpcAuthoring.All<TopDown3DAnimationPlaygroundSession>(scene).Single();
            Assert.That(session.CheckDependencies(), Is.True, session.UnavailableReason);
            Assert.That(session.Advance(AnimationPlaygroundReadiness.ServicesBound), Is.True);
            Assert.That(session.Advance(AnimationPlaygroundReadiness.VisualReady), Is.True);
            var actor = CampLoopNpcAuthoring.All<NpcActor>(scene).Single();
            var trial = CampLoopNpcAuthoring.All<CampLoopNpcTrial>(scene).Single();
            var input = CampLoopNpcAuthoring.All<TopDown3DInputRouter>(scene).Single();
            trial.RefreshGate(); Assert.That(actor.IsArmed, Is.True, trial.Failure);
            var failure = new FailingReset(failureId); session.ResetRegistry.Register(failure);
            input.SetLaboratoryResetBlocked(true); Assert.That(session.ResetRegistry.TryReset(), Is.False);
            input.SetLaboratoryResetBlocked(false); trial.RefreshGate(); Assert.That(actor.IsArmed, Is.False);
            actor.ApplyTuning(new NpcTuning { panic = 4 }, out _); actor.enabled = false; actor.enabled = true;
            trial.RefreshGate(); Assert.That(actor.IsArmed, Is.False);
            session.ResetRegistry.Unregister(failure);
            input.SetLaboratoryResetBlocked(true); Assert.That(session.ResetRegistry.TryReset(), Is.True);
            trial.RefreshGate(); Assert.That(actor.IsArmed, Is.False);
            input.SetLaboratoryResetBlocked(false); trial.RefreshGate(); Assert.That(actor.IsArmed, Is.True, trial.Failure);
            Assert.That(actor.Tuning.panic, Is.EqualTo(4));
        }
        private sealed class FailingReset : IAnimationPlaygroundResetParticipant
        {
            public string ResetId { get; }
            public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.Actor;
            public FailingReset(string id) => ResetId = id;
            public void InvalidateForReset(uint epoch) { }
            public bool TryRestoreForReset(out string reason) { reason = "Injected later/earlier participant failure."; return false; }
        }
        [Test] public void DisabledOptionalAdapterCannotRearmByDirectRefresh()
        {
            var scene = EditorSceneManager.OpenScene(CampLoopNpcAuthoring.ScenePath, OpenSceneMode.Single);
            var trial = CampLoopNpcAuthoring.All<CampLoopNpcTrial>(scene).Single();
            var actor = CampLoopNpcAuthoring.All<NpcActor>(scene).Single();
            trial.enabled = false; trial.RefreshGate();
            Assert.That(actor.IsArmed, Is.False); Assert.That(trial.TryRestoreForReset(out _), Is.False);
        }
        [Test] public void ActorLifetimeChangeRechecksSupportBeforeRearming()
        {
            var scene = EditorSceneManager.OpenScene(CampLoopNpcAuthoring.ScenePath, OpenSceneMode.Single);
            var session = CampLoopNpcAuthoring.All<TopDown3DAnimationPlaygroundSession>(scene).Single();
            Assert.That(session.CheckDependencies(), Is.True); session.Advance(AnimationPlaygroundReadiness.ServicesBound); session.Advance(AnimationPlaygroundReadiness.VisualReady);
            var actor = CampLoopNpcAuthoring.All<NpcActor>(scene).Single();
            var trial = CampLoopNpcAuthoring.All<CampLoopNpcTrial>(scene).Single(); trial.RefreshGate();
            Assert.That(actor.IsArmed, Is.True);
            actor.Invalidate(); // Explicit lifecycle notification in this non-Play-Mode fixture.
            var p = actor.transform.position;
            var tile = CampLoopNpcAuthoring.All<UnityEngine.Terrain>(scene).Single(t => p.x >= t.transform.position.x && p.z >= t.transform.position.z
                && p.x < t.transform.position.x + t.terrainData.size.x && p.z < t.transform.position.z + t.terrainData.size.z);
            tile.GetComponent<UnityEngine.TerrainCollider>().enabled = false; UnityEngine.Physics.SyncTransforms();
            trial.RefreshGate(); Assert.That(actor.IsArmed, Is.False); Assert.That(trial.Failure, Does.Contain("support"));
        }
    }
}
