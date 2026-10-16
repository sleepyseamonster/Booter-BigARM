// Unity CLI eval script. Run only in the marked, hidden animation candidate.
// Command/state and pose audit: no Play Mode, physics stepping, actor travel,
// scene save, source asset editing or production Editor mutation.
if (!UnityEngine.Application.isBatchMode || !System.IO.File.Exists(".animation-playground-validation-project")
    || System.IO.Directory.Exists(".git") || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
    || UnityEditor.EditorApplication.isCompiling)
    throw new System.InvalidOperationException("Use the stable marked background candidate.");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var types = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToArray();
var nativeType = types.Single(t => t.FullName == "BooterBigArm.Tests.AnimationPlaygroundNativeGroundTests+NativeFixture");
var inputType = types.Single(t => t.FullName == "BooterBigArm.Tests.AnimationPlaygroundStandingStrafeTests+InputFixture");
System.Func<object, string, object> Get = (o, n) => o.GetType().GetField(n, flags)?.GetValue(o) ?? o.GetType().GetProperty(n, flags)?.GetValue(o);
System.Action<object, string, object> Set = (o, n, v) => o.GetType().GetField(n, flags).SetValue(o, v);
System.Action<object, string> Call = (o, n) => o.GetType().GetMethod(n, flags).Invoke(o, null);
var cases = new System.Collections.Generic.List<object>();
foreach (var speed in new[] { 4.9f, 6.15f })
foreach (var heading in new[] { 0f, 90f, 180f })
foreach (var locked in heading == 0 ? new[] { false, true } : new[] { true })
{
    var input = System.Activator.CreateInstance(inputType);
    var fixture = System.Activator.CreateInstance(nativeType, new object[] { UnityEngine.SkinWeights.FourBones });
    UnityEngine.GameObject basis = null;
    try
    {
        nativeType.GetMethod("Place").Invoke(fixture, new object[] { new UnityEngine.Vector2(-1848, -1498), 0f });
        var inner = Get(fixture, "Inner"); var forward = Get(inner, "Forward");
        var motor = (BooterBigArm.TopDown3D.TopDown3DPlayerMotor)Get(inner, "Motor");
        var driver = (BooterBigArm.TopDown3D.TopDown3DPlayerAnimationDriver)Get(inner, "Driver");
        var body = (UnityEngine.Rigidbody)Get(forward, "Player");
        var router = (BooterBigArm.TopDown3D.TopDown3DInputRouter)Get(input, "Router");
        var session = (BooterBigArm.TopDown3D.TopDown3DAnimationPlaygroundSession)Get(forward, "Session");
        var b = session.Bindings; var registry = new BooterBigArm.TopDown3D.AnimationPlaygroundResetRegistry();
        var actions = motor.gameObject.AddComponent<BooterBigArm.TopDown3D.AnimationPlaygroundActionCoordinator>();
        actions.Configure(motor, driver, registry, new[] { body.position });
        motor.ConfigureLaboratorySpeeds(actions, b.slowWalkSpeed, b.runSpeed, b.sprintSpeed);
        motor.ConfigureLaboratoryGamepadGaits(actions, b.forwardReviewOverride.gaits[3].nominalSpeed, b.forwardReviewOverride.gaits[4].nominalSpeed);
        motor.BlockUnqualifiedLaboratoryTraversal(actions);
        // Binding a posture owner clears observed support; restore the fixture's
        // explicit native observation before checking the real support callback.
        nativeType.GetMethod("Place").Invoke(fixture, new object[] { new UnityEngine.Vector2(-1848, -1498), 0f });
        // Use the actual production callback, rather than the earlier test's () => true.
        motor.ConfigureLaboratoryStandingStrafe(actions, b.standingStrafeReview, driver.StandingStrafeReviewSupportAvailable);
        basis = new UnityEngine.GameObject("Offline dodge audit basis");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(basis, motor.gameObject.scene);
        Set(motor, "input", router); Set(motor, "cameraBasis", basis.transform);
        motor.FacePlanarDirection(UnityEngine.Vector3.forward);
        motor.GetType().GetMethod("ApplyStandingStrafeIntent", flags).Invoke(motor, new object[] { UnityEngine.Vector3.zero });
        var radians = heading * UnityEngine.Mathf.Deg2Rad;
        var stick = new UnityEngine.Vector2(UnityEngine.Mathf.Sin(radians), UnityEngine.Mathf.Cos(radians));
        var state = new UnityEngine.InputSystem.LowLevel.GamepadState { leftStick = stick, leftTrigger = locked ? 1 : 0 };
        if (speed > 5) state = state.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.RightShoulder);
        inputType.GetMethod("PadState").Invoke(input, new object[] { state });
        motor.GetType().GetMethod("ApplyStandingStrafeIntent", flags).Invoke(motor, new object[] { new UnityEngine.Vector3(stick.x, 0, stick.y) });
        inner.GetType().GetMethod("Frame").Invoke(inner, new object[] { speed, .25d, UnityEngine.Vector2.up, 1f });
        body.linearVelocity = UnityEngine.Vector3.forward * speed;
        var dodge = motor.gameObject.AddComponent<BooterBigArm.TopDown3D.AnimationPlaygroundDodgeController>();
        dodge.Configure(motor, driver, actions, b.dodge, registry, () => false, () => false);
        var supportBefore = driver.StandingStrafeReviewSupportAvailable();
        var lockBefore = motor.LaboratoryStandingFacingLocked;
        if (!supportBefore || locked && !lockBefore)
            throw new System.InvalidOperationException("The native-ground baseline must be ready before measuring a dodge.");
        if (!dodge.TryDodge()) throw new System.InvalidOperationException(dodge.LastFailure);
        var rows = new System.Collections.Generic.List<object>(); var firstLostLock = -1;
        var minimumEntrySpeed = speed; var releaseYaw = 0f; var firstFreeSpeed = -1f;
        for (var tick = 1; tick <= 65; tick++)
        {
            var wasRoll = motor.DodgeLocomotionActive; var before = body.rotation;
            var supportCallbackBefore = driver.StandingStrafeReviewSupportAvailable();
            Call(motor, "FixedUpdate"); Call(driver, "Update"); Call(driver, "LateUpdate");
            var planar = UnityEngine.Vector3.ProjectOnPlane(body.linearVelocity, UnityEngine.Vector3.up).magnitude;
            if (tick <= 15) minimumEntrySpeed = UnityEngine.Mathf.Min(minimumEntrySpeed, planar);
            if (wasRoll && !motor.DodgeLocomotionActive)
            { releaseYaw = UnityEngine.Quaternion.Angle(before, body.rotation); firstFreeSpeed = planar; }
            if (locked && firstLostLock < 0 && !motor.LaboratoryStandingFacingLocked) firstLostLock = tick;
            rows.Add(new { tick, seconds = tick * UnityEngine.Time.fixedDeltaTime, planarSpeed = planar,
                roll = motor.DodgeLocomotionActive, blend = driver.PoseBlendOwned, active = dodge.Active,
                locked = motor.LaboratoryStandingFacingLocked, awaitLTRelease = (bool)Get(motor, "standingStrafeAwaitRelease"),
                supportCallbackBefore, yaw = body.rotation.eulerAngles.y,
                renderedCycles = driver.LaboratoryRenderedCycles, poseSpeed = driver.LaboratoryForwardPoseSpeed });
        }
        cases.Add(new { speed, heading, ltHeld = locked, supportBefore, lockBefore, minimumEntrySpeed, firstFreeSpeed,
            releaseYaw, firstLostLock, finalLock = motor.LaboratoryStandingFacingLocked,
            lookAheadStillHeld = router.CameraLookAheadHeld, rows });
        dodge.DisposeOwnedServices(); actions.DisposeOwnedServices();
    }
    finally
    {
        if (basis != null) UnityEngine.Object.DestroyImmediate(basis);
        ((System.IDisposable)fixture).Dispose(); ((System.IDisposable)input).Dispose();
    }
}
return new { scope = "Offline actual motor/driver callbacks with fixed actor position; no physics stepping or traversal/feel proof", cases };
