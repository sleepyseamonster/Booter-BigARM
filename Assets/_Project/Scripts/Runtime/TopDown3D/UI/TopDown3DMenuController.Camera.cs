using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DMenuController
    {
        private TopDown3DCameraOptions cameraOptions;
        private void ConfigureCameraOptions()
        {
            var rig = TopDown3DGameHudCanvas.FindInScene<TopDown3DCameraRig>(gameObject.scene);
            cameraOptions = rig != null ? rig.CameraOptions : null;
            view.SetActive("CameraOptions", cameraOptions != null && view.HasCameraPanel);
            cameraOptions?.ApplyPreferences(settings.Accepted);
        }

        private bool HandleCameraCommand(string command)
        {
            if (command == "CameraOptions")
            {
                if (cameraOptions != null && view.HasCameraPanel) Show("Camera", "CameraView");
                return true;
            }
            if (panel != "Camera") return false;
            switch (command)
            {
                case "CameraView": settingsDraft.cameraView = settingsDraft.cameraView == TopDown3DCameraView.Elevated
                    ? TopDown3DCameraView.ThirdPerson : TopDown3DCameraView.Elevated; break;
                case "CameraRecenter": settingsDraft.cameraAutoRecenter = !settingsDraft.cameraAutoRecenter; break;
                case "CameraInvert": settingsDraft.cameraInvertY = !settingsDraft.cameraInvertY; break;
                case "CameraSensX": settingsDraft.cameraSensitivityX = NextSensitivity(settingsDraft.cameraSensitivityX); break;
                case "CameraSensY": settingsDraft.cameraSensitivityY = NextSensitivity(settingsDraft.cameraSensitivityY); break;
                case "CameraFov": settingsDraft.cameraFieldOfView = settingsDraft.cameraFieldOfView >= 75 ? 55 : settingsDraft.cameraFieldOfView + 5; break;
                case "CameraBack": Show("Settings", "CameraOptions"); return true;
                case "CameraApply":
                    if (!settings.TrySave(settingsDraft, out var error)) { view.SetText("CameraNotice", error); return true; }
                    cameraOptions.ApplyPreferences(settings.Accepted);
                    cameraRange?.SetPreset(settings.Accepted.viewDistancePreset);
                    view.ApplySettings(settings.Accepted);
                    Show("Settings", "CameraOptions"); return true;
                default: return false;
            }
            RefreshCameraOptions(); return true;
        }

        private static float NextSensitivity(float value) => value >= 2 ? .25f : Mathf.Min(2, value + .25f);

        private void RefreshCameraOptions()
        {
            view.SetText("CameraView", "View: " + (settingsDraft.cameraView == TopDown3DCameraView.ThirdPerson ? "Third person" : "Elevated"));
            view.SetText("CameraRecenter", "Third person auto centre: " + (settingsDraft.cameraAutoRecenter ? "On" : "Off"));
            view.SetText("CameraInvert", "Third person invert Y: " + (settingsDraft.cameraInvertY ? "On" : "Off"));
            view.SetText("CameraSensX", "Third person horizontal sensitivity: " + settingsDraft.cameraSensitivityX.ToString("0.00"));
            view.SetText("CameraSensY", "Third person vertical sensitivity: " + settingsDraft.cameraSensitivityY.ToString("0.00"));
            view.SetText("CameraFov", "Third person vertical field of view: " + settingsDraft.cameraFieldOfView.ToString("0") + "°");
            view.SetText("CameraNotice", cameraOptions.Notice ?? "Apply saves your preference. The view changes on resume when space is clear.");
            view.SetButton("CameraApply", settings.CanSave);
        }
    }
}
