using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent, RequireComponent(typeof(NpcGroundMotor))]
    public sealed class CampHuskBeetle : MonoBehaviour
    {
        [SerializeField] private CampResourceStudy study;
        [SerializeField] private Vector3 home;
        [SerializeField] private Transform visual;
        [SerializeField] private Transform[] legs;
        private NpcGroundMotor motor;
        private readonly NpcTuning tuning = new() { slopeDegrees = 25, stepUp = .22f, stepDown = .3f };
        private float windup = -1, recovery, wanderTime;
        private Vector3 strikeFacing, wanderTarget;
        public string State { get; private set; } = "Idle";
        public Vector3 Home => home;
        public void Configure(CampResourceStudy owner, Transform model, Transform[] limbs)
        { study = owner; home = transform.position; visual = model; legs = limbs; }
        private NpcGroundMotor Motor => motor != null ? motor : (motor = GetComponent<NpcGroundMotor>());
        public bool CanRestore() => Motor.TryGround(home, tuning, false, out var grounded) && Vector3.Distance(home, grounded) < .12f;
        public void RestoreAuthored()
        { var body = GetComponent<Rigidbody>(); body.position = home; body.rotation = Quaternion.identity; CancelIntent(); Physics.SyncTransforms(); }
        public void CancelIntent() { windup = -1; recovery = 0; wanderTime = 0; Motor.Stop(); }
        public void TakeHit(float amount)
        {
            if (study == null || !study.SimulationAllowed) return;
            if (study.Ledger.Damage(amount))
            { CancelIntent(); recovery = .35f; State = study.Ledger.Dead ? "Dead" : "Hurt"; }
        }
        public static bool CanHit(Vector3 origin, Vector3 facing, Vector3 target, float range, float minimumDot)
        {
            if (!NpcMath.Finite(origin) || !NpcMath.Finite(target) || !NpcMath.Finite(facing)) return false;
            var d = Vector3.ProjectOnPlane(target - origin, Vector3.up);
            return Mathf.Abs(target.y - origin.y) <= 1.8f && d.magnitude <= range
                && (d.sqrMagnitude < .001f || Vector3.Dot(facing.normalized, d.normalized) >= minimumDot);
        }
        public bool ClearToPlayer()
        {
            if (study == null || study.Player == null) return false;
            var from = transform.position + Vector3.up * .45f; var to = study.Player.Position + Vector3.up * .65f;
            var hits = Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
                if (!hit.collider.transform.IsChildOf(transform) && !hit.collider.transform.IsChildOf(study.Player.transform)) return false;
            return true;
        }
        private void FixedUpdate()
        {
            if (study == null) return;
            bool dead = study.Ledger.Dead;
            if (visual != null) visual.localRotation = Quaternion.Euler(0, 0, dead ? 18 : windup >= 0 ? -8 : 0);
            if (dead) { CancelIntent(); State = study.Ledger.Used(4) ? "Depleted carcass" : "Harvestable carcass"; return; }
            if (!study.SimulationAllowed) { CancelIntent(); State = "Suspended"; return; }
            var dt = Time.fixedDeltaTime;
            if (recovery > 0) { recovery -= dt; Motor.Stop(); State = "Recovery"; return; }
            if (windup >= 0)
            {
                windup += dt; State = "Attack windup"; Motor.Stop();
                if (windup >= .65f)
                {
                    if (CanHit(transform.position, strikeFacing, study.Player.Position, 1.6f, .5f) && ClearToPlayer())
                        study.Vitals.SetValue(TopDown3DSurvivalVital.Health, study.Vitals.Health - 6);
                    windup = -1; recovery = 1.1f;
                }
                return;
            }
            var p = study.Player.Position; float distance = NpcMath.Distance(transform.position, p);
            bool threatened = distance < 6 && NpcMath.Distance(home, p) < 5.5f && ClearToPlayer();
            if (threatened && distance < 1.5f)
            { Motor.FaceKnownPosition(p, tuning, dt); strikeFacing = transform.forward; windup = 0; State = "Alert"; return; }
            Vector3 goal;
            if (threatened) { goal = p; State = "Approach"; }
            else if (NpcMath.Distance(home, transform.position) > 3.5f) { goal = home; State = "Return home"; }
            else
            {
                wanderTime -= dt;
                if (wanderTime <= 0)
                { wanderTime = 4; var angle = (study.Ledger.Revision * 47 + (int)(Time.time / 4) * 113) * Mathf.Deg2Rad;
                    wanderTarget = home + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2; }
                goal = wanderTarget; State = "Wander";
            }
            if (NpcMath.Distance(transform.position, goal) < .25f) { Motor.Stop(); State = "Idle"; return; }
            if (!Motor.Step(goal, threatened ? 1.4f : .55f, tuning, dt, out _)) State = "Blocked";
            if (legs != null) for (int i = 0; i < legs.Length; i++)
                if (legs[i] != null) legs[i].localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 8 + i * Mathf.PI) * Motor.Speed * 18, i < 3 ? -45 : 45);
        }
        private void OnDisable() => CancelIntent();
    }
}
