using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundPlayerIntegrationTests
    {
        private static void Invoke(object value, string method) => value.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, null);
        private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
        internal static void CloseFixtureScene(Scene scene)
        {
            // These fixtures call Awake manually in Edit Mode; Unity does not own their runtime graph lifecycle.
            foreach(var root in scene.GetRootGameObjects())
                foreach(var driver in root.GetComponentsInChildren<TopDown3DPlayerAnimationDriver>(true))
                    if(Field<PlayableGraph>(driver,"playableGraph").IsValid())Invoke(driver,"OnDestroy");
            EditorSceneManager.CloseScene(scene,true);
        }
        private static TopDown3DAnimationPlaygroundBindings PrivateBindings()
        {
            var result = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
            if (result == null) Assert.Ignore("Restore the paired private player binding before running this integration gate.");
            return result;
        }

        [Test] public void PhysicsHierarchyRejectsScaledAncestorsAndMirrors()
        {
            var root = new GameObject("Root fixture"); var child = new GameObject("Actor fixture"); child.transform.SetParent(root.transform);
            try
            {
                Assert.That(TopDown3DAnimationPlaygroundSession.HasUnitPhysicsHierarchy(child.transform), Is.True);
                root.transform.localScale = new Vector3(1, 2, 1);
                Assert.That(TopDown3DAnimationPlaygroundSession.HasUnitPhysicsHierarchy(child.transform), Is.False);
                root.transform.localScale = Vector3.one; child.transform.localScale = new Vector3(-1, 1, 1);
                Assert.That(TopDown3DAnimationPlaygroundSession.HasUnitPhysicsHierarchy(child.transform), Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test, Category("PrivateAnimationQualification")] public void PrivateBindingCoversEverySourceWithoutPromotingPhysicalReadiness()
        {
            var bindings = PrivateBindings(); Assert.That(bindings.ValidatePlayer(out var error), Is.True, error);
            Assert.That(bindings.motions.Length, Is.EqualTo(71)); Assert.That(bindings.motions.Count(value => value.CanPreview), Is.EqualTo(68));
            Assert.That(bindings.motions.Count(value => value.restrictedReference), Is.EqualTo(3));
            Assert.That(bindings.motions.Any(value => value.CanPerform), Is.False);
            Assert.That(bindings.motions.Select(value => value.sourceId).Distinct().Count(), Is.EqualTo(71));
            Assert.That(bindings.visualScale, Is.EqualTo(.08989948f).Within(.0000001f));
            Assert.That(bindings.capsuleHeight, Is.EqualTo(1.82f)); Assert.That(bindings.capsuleRadius, Is.EqualTo(.2f));
            var measured = AnimationPlaygroundAssetValidator.MeasureRig(AssetDatabase.GetAssetPath(bindings.targetModel), bindings.bodyHeight, -bindings.capsuleHeight * .5f);
            Assert.That(measured.importedHeight * bindings.visualScale, Is.EqualTo(1.8f).Within(.00001f));
            Assert.That(measured.visualOffset.y, Is.EqualTo(bindings.visualOffset.y).Within(.00001f));
        }

        [Test, Category("PrivateAnimationQualification")] public void BindingRejectsBrokenColliderProfileAndSpeedContracts()
        {
            var clone = Object.Instantiate(PrivateBindings());
            try
            {
                clone.capsuleRadius = 2; Assert.That(clone.ValidatePlayer(out _), Is.False);
                clone.capsuleRadius = .2f; clone.sprintSpeed = float.NaN; Assert.That(clone.ValidatePlayer(out _), Is.False);
                clone.sprintSpeed = 3.8f; clone.locomotion = null; Assert.That(clone.ValidatePlayer(out _), Is.False);
            }
            finally { Object.DestroyImmediate(clone); }
        }

        [Test] public void ReturnAnchorsRequireNearbyWalkableSupport()
        {
            var actor = new GameObject("Return fixture"); actor.SetActive(false); actor.transform.position = new Vector3(60000, 1, 60000);
            var motor = actor.AddComponent<TopDown3DPlayerMotor>(); var capsule = actor.GetComponent<CapsuleCollider>(); capsule.height = 2; capsule.center = Vector3.zero;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.position = new Vector3(60000, -.1f, 60000); floor.transform.localScale = new Vector3(4, .2f, 4);
            try
            {
                Physics.SyncTransforms(); Assert.That(motor.IsAnimationReturnSupported(actor.transform.position), Is.True);
                Assert.That(motor.IsAnimationReturnSupported(actor.transform.position + Vector3.right * 10), Is.False);
                floor.transform.position += Vector3.down; Physics.SyncTransforms(); Assert.That(motor.IsAnimationReturnSupported(actor.transform.position), Is.False);
            }
            finally { Object.DestroyImmediate(actor); Object.DestroyImmediate(floor); }
        }

        [Test] public void FailedResetBlocksMovementAndActionsWhilePaletteRemainsAvailable()
        {
            var oldSettings = InputSystem.settings; var settings = ScriptableObject.CreateInstance<InputSettings>();
            settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true); InputSystem.settings = settings;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var actions = AnimationPlaygroundInputFactory.Create(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            var root = new GameObject("Blocked lab input fixture"); root.SetActive(false);
            try
            {
                var input = root.AddComponent<TopDown3DInputRouter>(); input.Configure(actions); input.Initialize();
                var jumps = 0; var palettes = 0; input.AnimationJumpRequested += () => jumps++; input.AnimationPaletteRequested += () => palettes++;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); input.SetLaboratoryResetBlocked(true);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space, Key.F9)); InputSystem.Update();
                Assert.That(input.MoveValue, Is.EqualTo(Vector2.zero)); Assert.That(jumps, Is.Zero); Assert.That(palettes, Is.EqualTo(1));
                input.SetLaboratoryResetBlocked(false); InputSystem.Update(); Assert.That(jumps, Is.Zero);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space)); InputSystem.Update();
                Assert.That(jumps, Is.EqualTo(1)); Assert.That(input.MoveValue.sqrMagnitude, Is.GreaterThan(.5f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                input.SetLaboratoryResetBlocked(true); input.SetLaboratoryResetBlocked(false); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
                Assert.That(input.MoveValue.sqrMagnitude, Is.GreaterThan(.5f), "A reset while neutral must accept the next fresh movement press.");
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(actions); InputSystem.RemoveDevice(keyboard);
                InputSystem.settings = oldSettings; Object.DestroyImmediate(settings);
            }
        }

        // This deterministic Edit Mode fixture runs only in the isolated validation project.
        [Test, Category("PrivateAnimationQualification")] public void AuthoredPlayerUsesOneGraphAndEquipmentHasIndependentMetreScale()
        {
            var bindings = PrivateBindings(); var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
            try
            {
                var session = TopDown3DAnimationPlaygroundSession.Find(scene); var player = session.Player;
                Assert.That(player, Is.Not.Null); Assert.That(session.Bindings, Is.SameAs(bindings));
                Invoke(session, "Awake"); Assert.That(session.Readiness, Is.EqualTo(AnimationPlaygroundReadiness.DependenciesChecked), session.UnavailableReason);
                var driver = player.GetComponent<TopDown3DPlayerAnimationDriver>(); Invoke(driver, "Awake");
                Assert.That(driver.HumanoidAnimator.avatar, Is.SameAs(bindings.targetAvatar)); Assert.That(driver.HumanoidAnimator.applyRootMotion, Is.False);
                Assert.That(player.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
                var graph = Field<PlayableGraph>(driver, "playableGraph"); Assert.That(graph.GetOutputCount(), Is.EqualTo(1));
                Assert.Throws<InvalidOperationException>(() => driver.ConfigureLaboratory(bindings));
                var visual = Field<GameObject>(driver, "visualInstance");
                foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>()) Assert.That(skin.sharedMaterials.All(value => value == bindings.bodyMaterial), Is.True);
                var camp = TopDown3DCampSettings.Load(); Assert.That(driver.ConfigureCamp(camp), Is.True);
                var cloak = Field<GameObject>(driver, "wornCloak"); var blanket = Field<GameObject>(driver, "sleepingBlanket");
                var equipmentScale = bindings.bodyHeight / bindings.equipmentModelHeight;
                Assert.That(cloak.transform.lossyScale.x, Is.EqualTo(equipmentScale).Within(.00001f));
                Assert.That(blanket.transform.lossyScale.x, Is.EqualTo(equipmentScale).Within(.00001f));
                Assert.That(player.transform.lossyScale, Is.EqualTo(Vector3.one));
                Assert.That(player.GetComponent<CapsuleCollider>().height, Is.EqualTo(bindings.capsuleHeight));
            }
            finally { CloseFixtureScene(scene); }
        }

        [Test, Category("PrivateAnimationQualification")] public void SessionWiresExistingInputMenuPreviewAndOrderedPlayerReset()
        {
            PrivateBindings(); var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive); InputActionAsset actions = null;
            try
            {
                var session = TopDown3DAnimationPlaygroundSession.Find(scene); Invoke(session, "Awake");
                var player = session.Player; var driver = player.GetComponent<TopDown3DPlayerAnimationDriver>(); Invoke(driver, "Awake");
                var input = player.InputRouter; actions = AnimationPlaygroundInputFactory.Create(input.InputActions);
                input.Configure(actions); input.Initialize();
                Invoke(session, "Start"); Assert.That(session.Readiness, Is.EqualTo(AnimationPlaygroundReadiness.VisualReady), session.UnavailableReason);
                Assert.That(Field<float>(player, "animationWalkSpeed"), Is.EqualTo(1.4f).Within(.0001f));
                Assert.That(Field<float>(player, "walkSpeed"), Is.EqualTo(2.7f).Within(.0001f));
                Assert.That(Field<float>(player, "sprintSpeed"), Is.EqualTo(3.8f).Within(.0001f));
                Assert.That(driver.LaboratorySustainedDiagnostic, Is.False, "Source selection must not opt the author into a diagnostic.");
                Assert.That(session.Coordinator, Is.Not.Null); Assert.That(input.AnimationActionOwned, Is.Not.Null);
                var binding = session.Bindings.motions.First(value => value.CanPreview);
                Assert.That(session.Coordinator.TryBeginPreview(binding), Is.True); Assert.That(input.AnimationActionOwned(), Is.True);
                var replacement=session.Bindings.motions.First(value=>value.CanPreview && value!=binding);
                typeof(TopDown3DAnimationPlaygroundSession).GetMethod("PreviewMotion",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session,new object[]{replacement.sourceId});
                Assert.That(Field<AnimationPlaygroundMotionBinding>(session.Coordinator,"preview"),Is.SameAs(replacement));
                var vitals = player.GetComponent<TopDown3DSurvivalVitals>(); var initial = vitals.Health; vitals.SetValue(TopDown3DSurvivalVital.Health, 1);
                Physics.SyncTransforms(); session.ResetSession();
                Assert.That(session.ResetRegistry.LastFailure, Is.Null); Assert.That(input.LaboratoryResetBlocked, Is.False);
                Assert.That(session.Coordinator.IsBusy, Is.False); Assert.That(driver.AnimationPresentationOwned, Is.False);
                Assert.That(vitals.Health, Is.EqualTo(initial));
                Assert.That(session.JumpReady,Is.True);
                Assert.That(session.Bindings.lowMotion,Is.Not.Null);Assert.That(session.SlideReady,Is.True);
                Assert.That(player.GetComponent<AnimationPlaygroundLowMotionController>(),Is.Not.Null);
                typeof(TopDown3DAnimationPlaygroundSession).GetMethod("RequestSlide",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session,null);
                Assert.That(session.Coordinator.IsBusy,Is.False);
                var airborne=player.GetComponent<AnimationPlaygroundAirborneController>();
                typeof(AnimationPlaygroundAirborneController).GetField("paused",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(airborne,(Func<bool>)(()=>false));
                typeof(TopDown3DPlayerMotor).GetField("<IsGrounded>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,true);
                ((Action)typeof(TopDown3DInputRouter).GetField("AnimationJumpRequested",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input)).Invoke();
                Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Takeoff));
                Assert.That(session.IsBusy,Is.True);Assert.That(session.Coordinator.IsSequenceOwner(airborne),Is.True);
                session.ResetSession();Assert.That(airborne.Active,Is.False);Assert.That(input.LaboratoryResetBlocked,Is.False);
                var beforeDamage=vitals.Health;
                typeof(TopDown3DAnimationPlaygroundSession).GetMethod("CommitLandingDamage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session,new object[]{8f});
                Assert.That(vitals.Health,Is.LessThan(beforeDamage));
                session.ResetSession();Assert.That(vitals.Health,Is.EqualTo(initial));
                session.Fail("Fixture dependency is absent.");
                Assert.That(session.JumpReady,Is.False);
                Assert.That(session.PreviewReady,Is.False);
                var menu=TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(scene); menu.OpenAnimationPalette();
                var panel=Field<GameObject>(menu,"animationPanel");
                Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(value=>value.name=="LabPaletteStatus").text,
                    Does.Contain("Fixture dependency is absent."));
            }
            finally { CloseFixtureScene(scene); if (actions != null) Object.DestroyImmediate(actions); }
        }
    }
}
