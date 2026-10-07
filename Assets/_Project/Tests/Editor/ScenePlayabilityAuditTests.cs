using System.Collections;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BooterBigArm.Tests
{
    // Explicit audit requested by the user. Never part of the default test run.
    [Explicit("Load and exercise actual scenes only for an authorized playability audit.")]
    public sealed class ScenePlayabilityAuditTests
    {
        private Keyboard keyboard;
        private Gamepad gamepad;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        private InputSettings.BackgroundBehavior previousBackgroundInput;
        private bool changedInputSettings;
        private bool previousRunInBackground;

        [UnitySetUp]
        public IEnumerator EnterRuntime()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            // Route simulated devices as a focused Game view without changing desktop focus.
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousBackgroundInput = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            changedInputSettings = true;
        }

        [UnityTearDown]
        public IEnumerator LeaveRuntime()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (gamepad != null && gamepad.added) InputSystem.RemoveDevice(gamepad);
            if (changedInputSettings)
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
                InputSystem.settings.backgroundBehavior = previousBackgroundInput;
                Application.runInBackground = previousRunInBackground;
                changedInputSettings = false;
            }
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator GeneratedWorld_StartsAndAcceptsMovementCameraAndInventory()
        {
            var routine = Exercise(TopDown3DPrototypeBuilder.ScenePath, true);
            while (routine.MoveNext()) yield return routine.Current;
        }

        [UnityTest]
        public IEnumerator DeathValley_StartsAndAcceptsMovementCameraAndInventory()
        {
            var routine = Exercise("Assets/_Project/Scenes/Production/GreaterWasteland.unity", false);
            while (routine.MoveNext()) yield return routine.Current;
        }

        [UnityTest]
        public IEnumerator DeathValley_RuntimeOnlyGroundMarker_ConfirmsWiringDiagnosis()
        {
            var routine = Exercise("Assets/_Project/Scenes/Production/GreaterWasteland.unity", false, true);
            while (routine.MoveNext()) yield return routine.Current;
        }

        private IEnumerator Exercise(string path, bool generated, bool diagnosticGroundMarker = false)
        {
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            keyboard = InputSystem.AddDevice<Keyboard>();
            gamepad = InputSystem.AddDevice<Gamepad>();
            yield return null;
            if (diagnosticGroundMarker)
            {
                var terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
                Assert.That(terrains.Length, Is.EqualTo(256));
                foreach (var terrain in terrains)
                {
                    if (terrain.GetComponent<TopDown3DGroundSurface>() == null)
                        terrain.gameObject.AddComponent<TopDown3DGroundSurface>();
                }
                Debug.Log("AUDIT_RUNTIME_ONLY_GROUND_MARKERS: temporary missing terrain components; original scene is not saved.");
            }
            var motor = Object.FindFirstObjectByType<TopDown3DPlayerMotor>();
            var follower = Object.FindFirstObjectByType<TopDown3DBigArmFollower>();
            var input = Object.FindFirstObjectByType<TopDown3DInputRouter>();
            var cameraRig = Object.FindFirstObjectByType<TopDown3DCameraRig>();
            var inventory = Object.FindFirstObjectByType<TopDown3DInventoryUiController>();
            var world = Object.FindFirstObjectByType<TopDown3DProceduralWorld>();
            Assert.That(motor, Is.Not.Null, path + ": Booter missing");
            Assert.That(follower, Is.Not.Null, path + ": Legger missing");
            Assert.That(input, Is.Not.Null, path + ": input owner missing");
            Assert.That(cameraRig, Is.Not.Null, path + ": camera rig missing");
            Assert.That(inventory, Is.Not.Null, path + ": inventory UI missing");
            Assert.That(Object.FindObjectsByType<TopDown3DInputRouter>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            if (generated)
            {
                Assert.That(world, Is.Not.Null);
                double deadline = Time.realtimeSinceStartupAsDouble + 120.0;
                while (!world.InitialPresentationReady && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                Assert.That(world.InitialPresentationReady, Is.True, "Generated world startup never became ready");
                Assert.That(world.LoadedChunkCount, Is.GreaterThan(0));
                Assert.That(world.TerrainColliderCount, Is.GreaterThan(0));
            }
            else
            {
                Assert.That(world, Is.Null, "Generated terrain contaminated Death Valley");
                Assert.That(Object.FindFirstObjectByType<TopDown3DGameStateSaveService>(), Is.Null);
                Assert.That(Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length, Is.EqualTo(256));
            }
            for (int frame = 0; frame < 30; frame++) yield return null;
            var physicsAdvance = AdvancePhysics(2f);
            while (physicsAdvance.MoveNext()) yield return physicsAdvance.Current;
            var body = motor.GetComponent<Rigidbody>();
            var capsule = motor.GetComponent<CapsuleCollider>();
            var supports = Physics.RaycastAll(motor.Position + Vector3.up * 5f, Vector3.down, 1000f, motor.GroundMask, QueryTriggerInteraction.Ignore)
                .Where(hit => hit.collider.GetComponentInParent<TopDown3DPlayerMotor>() == null)
                .OrderBy(hit => hit.distance).ToArray();
            Debug.Log($"AUDIT_GROUND {path}: position={motor.Position}; velocity={body.linearVelocity}; motorEnabled={motor.enabled}; kinematic={body.isKinematic}; capsule={capsule.bounds}; colliderEnabled={capsule.enabled}; scale={Time.timeScale}; physics={Physics.simulationMode}; grounded={motor.IsGrounded}; focused={Application.isFocused}; support={(supports.Length > 0 ? supports[0].collider.name + " slope=" + Vector3.Angle(supports[0].normal, Vector3.up) : "none")}");
            Assert.That(input.Mode, Is.EqualTo(TopDown3DInputMode.Gameplay));
            Assert.That(motor.IsGrounded, Is.True, "Booter failed to settle on terrain");
            Vector3 start = motor.Position;
            Vector3 companionStart = follower.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.Update();
            yield return null;
            Assert.That(input.MoveValue.sqrMagnitude, Is.GreaterThan(0.1f), "Keyboard input did not reach the router");
            physicsAdvance = AdvancePhysics(1.2f);
            while (physicsAdvance.MoveNext()) yield return physicsAdvance.Current;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return null;
            float keyboardDistance = PlanarDistance(start, motor.Position);
            Debug.Log($"AUDIT_MOVEMENT {path}: distance={keyboardDistance}; input={input.MoveValue}; mode={input.Mode}; velocity={body.linearVelocity}; desired={motor.LocomotionSnapshot.DesiredPlanarVelocity}; constrained={motor.IsActionConstrained}; grounded={motor.IsGrounded}; cameraForward={cameraRig.transform.forward}; collider={capsule.bounds}");
            Assert.That(keyboardDistance, Is.GreaterThan(0.2f), "Booter did not move with keyboard input");
            Vector3 padStart = motor.Position;
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f) });
            InputSystem.Update();
            yield return null;
            physicsAdvance = AdvancePhysics(1.2f);
            while (physicsAdvance.MoveNext()) yield return physicsAdvance.Current;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.Update();
            yield return null;
            float gamepadDistance = PlanarDistance(padStart, motor.Position);
            Assert.That(gamepadDistance, Is.GreaterThan(0.2f), "Booter did not move with gamepad input");
            float yaw = cameraRig.transform.eulerAngles.y;
            InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = new Vector2(0.7f, 0f) });
            InputSystem.Update();
            yield return null;
            physicsAdvance = AdvancePhysics(0.6f);
            while (physicsAdvance.MoveNext()) yield return physicsAdvance.Current;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.Update();
            yield return null;
            float yawChange = Mathf.Abs(Mathf.DeltaAngle(yaw, cameraRig.transform.eulerAngles.y));
            Assert.That(yawChange, Is.GreaterThan(1f), "Right stick did not rotate the camera");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            InputSystem.Update();
            yield return null;
            yield return null;
            Assert.That(inventory.IsOpen, Is.True, "Inventory did not open from Tab");
            Assert.That(input.Mode, Is.EqualTo(TopDown3DInputMode.Inventory));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            InputSystem.Update();
            yield return null;
            yield return null;
            Assert.That(inventory.IsOpen, Is.False, "Inventory did not close from Tab");
            Assert.That(input.Mode, Is.EqualTo(TopDown3DInputMode.Gameplay));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return null;
            physicsAdvance = AdvancePhysics(0.4f);
            while (physicsAdvance.MoveNext()) yield return physicsAdvance.Current;
            Assert.That(motor.IsGrounded, Is.True, "Booter lost terrain after movement");
            Assert.That(IsFinite(follower.transform.position), Is.True, "Legger position became invalid");
            float companionDistance = PlanarDistance(companionStart, follower.transform.position);
            Assert.That(companionDistance, Is.GreaterThan(0.2f), "Legger did not follow the moving player");
            Debug.Log($"PLAYABILITY_AUDIT {path}: keyboard={keyboardDistance:F2}m; gamepad={gamepadDistance:F2}m; cameraYaw={yawChange:F1}deg; grounded={motor.IsGrounded}; Legger={follower.State}; companion={companionDistance:F2}m; inventory toggle passed.");
        }

        private static IEnumerator AdvancePhysics(float seconds)
        {
            double end = Time.fixedTimeAsDouble + seconds;
            double deadline = Time.realtimeSinceStartupAsDouble + 15.0;
            while (Time.fixedTimeAsDouble < end && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert.That(Time.fixedTimeAsDouble, Is.GreaterThanOrEqualTo(end), "Runtime physics did not advance in the background audit");
        }

        private static float PlanarDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
        private static bool IsFinite(Vector3 p) => !(float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.z));
    }
}
