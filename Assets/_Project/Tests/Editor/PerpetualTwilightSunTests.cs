using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Tests
{
    public sealed class PerpetualTwilightSunTests
    {
        private const string PipelineAssetPath =
            "Assets/_Project/Settings/Rendering/URP/UniversalRP.asset";

        [Test]
        public void Cycle_StaysInsidePerpetualTwilightElevationBand()
        {
            const float centerElevation = 19f;
            const float elevationAmplitude = 7f;

            for (var sample = 0; sample <= 64; sample++)
            {
                var state = PerpetualTwilightSun.EvaluateCycle(
                    sample / 64f,
                    centerElevation,
                    elevationAmplitude,
                    -32f,
                    14f);

                Assert.That(state.ElevationDegrees, Is.InRange(12f, 26f));
                Assert.That(state.Brightness01, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void Cycle_WrapsWithoutAJump()
        {
            var start = PerpetualTwilightSun.EvaluateCycle(0f, 19f, 7f, -32f, 14f);
            var wrapped = PerpetualTwilightSun.EvaluateCycle(1f, 19f, 7f, -32f, 14f);

            Assert.That(wrapped.ElevationDegrees, Is.EqualTo(start.ElevationDegrees).Within(0.0001f));
            Assert.That(wrapped.AzimuthDegrees, Is.EqualTo(start.AzimuthDegrees).Within(0.0001f));
            Assert.That(wrapped.Brightness01, Is.EqualTo(start.Brightness01).Within(0.0001f));
        }

        [Test]
        public void Cycle_HasDistinctBrightAndDeepTwilightPhases()
        {
            var bright = PerpetualTwilightSun.EvaluateCycle(0.25f, 19f, 7f, -32f, 14f);
            var deep = PerpetualTwilightSun.EvaluateCycle(0.75f, 19f, 7f, -32f, 14f);

            Assert.That(bright.ElevationDegrees, Is.EqualTo(26f).Within(0.0001f));
            Assert.That(bright.Brightness01, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(deep.ElevationDegrees, Is.EqualTo(12f).Within(0.0001f));
            Assert.That(deep.Brightness01, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Pipeline_SupportsReadableMainLightShadows()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            Assert.That(pipeline, Is.Not.Null);

            var serialized = new SerializedObject(pipeline);
            Assert.That(serialized.FindProperty("m_MainLightShadowsSupported").boolValue, Is.True);
            Assert.That(serialized.FindProperty("m_SoftShadowsSupported").boolValue, Is.True);
            Assert.That(serialized.FindProperty("m_ShadowCascadeCount").intValue, Is.GreaterThanOrEqualTo(2));
            Assert.That(serialized.FindProperty("m_ShadowDistance").floatValue, Is.GreaterThanOrEqualTo(50f));
        }
    }
}
