// Unity CLI eval script. Marked hidden candidate only; fixed actor command
// observations, explicit completed-motion units, no Play Mode or physics steps.
if (!UnityEngine.Application.isBatchMode || !System.IO.File.Exists(".animation-playground-validation-project")
    || System.IO.Directory.Exists(".git") || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
    || UnityEditor.EditorApplication.isCompiling)
    throw new System.InvalidOperationException("Use the stable marked background candidate.");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var fixtureType = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
    .Single(t => t.FullName == "BooterBigArm.Tests.AnimationPlaygroundDodgeTransitionTests+Fixture");
System.Func<object, string, object> Get = (o, n) => o.GetType().GetField(n, flags)?.GetValue(o) ?? o.GetType().GetProperty(n, flags)?.GetValue(o);
var cases = new System.Collections.Generic.List<object>();
foreach (var speed in new[] { 4.9f, 6.15f })
foreach (var heading in new[] { 0f, 45f, 90f, 180f })
foreach (var locked in heading == 0 ? new[] { false, true } : new[] { true })
{
    var fixture = System.Activator.CreateInstance(fixtureType, flags, null,
        new object[] { speed, heading, locked, UnityEngine.SkinWeights.FourBones }, null);
    try
    {
        var motor = (BooterBigArm.TopDown3D.TopDown3DPlayerMotor)Get(fixture, "Motor");
        var driver = (BooterBigArm.TopDown3D.TopDown3DPlayerAnimationDriver)Get(fixture, "Driver");
        var dodge = (BooterBigArm.TopDown3D.AnimationPlaygroundDodgeController)Get(fixture, "Dodge");
        var body = (UnityEngine.Rigidbody)Get(fixture, "Body");
        var normal = (UnityEngine.Vector3)Get(fixture, "Normal"); var origin = body.position;
        var rows = new System.Collections.Generic.List<object>();
        var previous = speed; var maximumEntryBraking = 0f; var releaseYaw = 0f; var releaseVisualYaw = 0f;
        var scalarCommandDistance = 0f; var firstLostLock = -1;
        if (!dodge.TryDodge()) throw new System.InvalidOperationException(dodge.LastFailure);
        for (var tick = 1; tick <= 65; tick++)
        {
            var wasRoll = motor.DodgeLocomotionActive; var before = body.rotation;
            var visualBefore = (UnityEngine.Vector3)Get(fixture, "VisualForward");
            fixtureType.GetMethod("Step").Invoke(fixture, new object[] { true });
            var tangentSpeed = UnityEngine.Vector3.ProjectOnPlane(body.linearVelocity, normal).magnitude;
            if (tick <= 15) maximumEntryBraking = UnityEngine.Mathf.Max(maximumEntryBraking, (previous - tangentSpeed) / UnityEngine.Time.fixedDeltaTime);
            if (wasRoll) scalarCommandDistance += tangentSpeed * UnityEngine.Time.fixedDeltaTime;
            if (wasRoll && !motor.DodgeLocomotionActive)
            {
                releaseYaw = UnityEngine.Quaternion.Angle(before, body.rotation);
                releaseVisualYaw = UnityEngine.Vector3.Angle(visualBefore, (UnityEngine.Vector3)Get(fixture, "VisualForward"));
            }
            if (locked && firstLostLock < 0 && !motor.LaboratoryStandingFacingLocked) firstLostLock = tick;
            rows.Add(new { tick, seconds = tick * UnityEngine.Time.fixedDeltaTime, tangentSpeed,
                roll = motor.DodgeLocomotionActive, blend = driver.PoseBlendOwned,
                locked = motor.LaboratoryStandingFacingLocked, yaw = body.rotation.eulerAngles.y,
                visualYawDegrees = UnityEngine.Vector3.SignedAngle(UnityEngine.Vector3.forward, (UnityEngine.Vector3)Get(fixture, "VisualForward"), UnityEngine.Vector3.up) });
            previous = tangentSpeed;
        }
        if (body.position != origin || firstLostLock >= 0 || releaseVisualYaw > 1 || maximumEntryBraking > 8.1f)
            throw new System.InvalidOperationException("A fixed-position transition invariant failed.");
        cases.Add(new { speed, heading, ltHeld = locked, maximumEntryBraking, releaseYaw, releaseVisualYaw,
            firstLostLock, scalarCommandDistance, finalLock = motor.LaboratoryStandingFacingLocked, rows });
    }
    finally { ((System.IDisposable)fixture).Dispose(); }
}
return Newtonsoft.Json.JsonConvert.SerializeObject(new { scope = "Offline command/state proof on native camp support; completed-motion units are explicit, actor held fixed, no physical distance/cadence/feel claim", cases });
