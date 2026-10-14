using System;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Tests
{
    public sealed class CampOpticsPrerequisiteTests
    {
        [Test]
        public void CameraAndActiveBridgeAreRequired()
        {
            var owner = new GameObject("Prerequisite camera");
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            var bridge = ScriptableObject.CreateInstance<TopDown3DSunBloomFeature>();
            var flare = ScriptableObject.CreateInstance<TopDown3DRoundLensFlareFeature>();
            try
            {
                var camera = owner.AddComponent<Camera>();
                Assert.Throws<InvalidOperationException>(() => CampOpticsAuthoring.ValidateCameraPrerequisites(camera, renderer));
                var data = owner.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                StringAssert.Contains("bridge", Assert.Throws<InvalidOperationException>(() => CampOpticsAuthoring.ValidateCameraPrerequisites(camera, renderer)).Message);
                renderer.rendererFeatures.Add(bridge); renderer.rendererFeatures.Add(flare);
                bridge.SetActive(false);
                StringAssert.Contains("bridge", Assert.Throws<InvalidOperationException>(() => CampOpticsAuthoring.ValidateCameraPrerequisites(camera, renderer)).Message);
                bridge.SetActive(true); flare.SetActive(false);
                StringAssert.Contains("Flare", Assert.Throws<InvalidOperationException>(() => CampOpticsAuthoring.ValidateCameraPrerequisites(camera, renderer)).Message);
                data.renderType = CameraRenderType.Overlay;
                StringAssert.Contains("base camera", Assert.Throws<InvalidOperationException>(() => CampOpticsAuthoring.ValidateCameraPrerequisites(camera, renderer)).Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(renderer);
                UnityEngine.Object.DestroyImmediate(bridge);
                UnityEngine.Object.DestroyImmediate(flare);
            }
        }
    }
}
