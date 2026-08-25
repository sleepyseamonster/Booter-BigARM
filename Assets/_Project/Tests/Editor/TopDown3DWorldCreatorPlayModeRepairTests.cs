using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DWorldCreatorPlayModeRepairTests
    {
        [Test]
        public void BigArmStartupGrounding_UsesBootersGeneratedElevationAsProbeReference()
        {
            var ground = new GameObject("High generated ground contract");
            var player = new GameObject("Booter generated elevation contract");
            var bigArm = new GameObject("BigARM legacy elevation contract");
            try
            {
                ground.transform.position = new Vector3(0f, 50f, 0f);
                ground.AddComponent<BoxCollider>().size = new Vector3(20f, 1f, 20f);
                ground.AddComponent<TopDown3DGroundSurface>();

                player.transform.position = new Vector3(4f, 51f, 0f);
                bigArm.transform.position = new Vector3(0f, 0f, 0f);
                bigArm.AddComponent<BoxCollider>();
                var body = bigArm.AddComponent<Rigidbody>();
                var follower = bigArm.AddComponent<TopDown3DBigArmFollower>();
                follower.Configure(player.transform, null, null);
                Physics.SyncTransforms();

                Assert.That(follower.TryInitializeOnGround(), Is.True);
                Assert.That(body.position.y, Is.EqualTo(51.32f).Within(0.001f));
                Assert.That(follower.State, Is.EqualTo(TopDown3DBigArmFollower.FollowState.Idle));
            }
            finally
            {
                Object.DestroyImmediate(ground);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(bigArm);
            }
        }

        [Test]
        public void FullContentEditorOptIn_EnablesProfileWithoutCommandLineArgument()
        {
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.ShouldUseFullContentProfile(
                    new[] { "BooterBigArm" },
                    true),
                Is.True);
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.ShouldUseFullContentProfile(
                    new[] { "BooterBigArm" },
                    false),
                Is.False);
        }
    }
}
