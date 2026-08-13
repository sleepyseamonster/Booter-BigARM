using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DChromaticAberrationTests
    {
        [Test]
        public void DustAtmosphere_RuntimeProfileIncludesSubtleChromaticAberration()
        {
            var gameObject = new GameObject("Chromatic aberration contract");
            var profile = default(VolumeProfile);
            try
            {
                var volume = gameObject.AddComponent<Volume>();
                var atmosphere = gameObject.AddComponent<TopDown3DDustAtmosphere>();
                var ensurePostProcessing = typeof(TopDown3DDustAtmosphere).GetMethod(
                    "EnsurePostProcessing",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(ensurePostProcessing, Is.Not.Null);
                ensurePostProcessing.Invoke(atmosphere, null);
                profile = volume.sharedProfile;

                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.TryGet(out ChromaticAberration aberration), Is.True);
                Assert.That(aberration.active, Is.True);
                Assert.That(aberration.intensity.overrideState, Is.True);
                Assert.That(
                    aberration.intensity.value,
                    Is.EqualTo(TopDown3DDustAtmosphere.DefaultChromaticAberrationIntensity)
                        .Within(0.0001f));
            }
            finally
            {
                if (profile != null)
                {
                    Object.DestroyImmediate(profile);
                }

                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
