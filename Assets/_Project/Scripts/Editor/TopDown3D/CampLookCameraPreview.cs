using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public sealed class CampLookCameraPreview : EditorWindow
    {
        private TopDown3DCampLook look;
        private RenderTexture image;
        private double nextRender;
        public static void Open(TopDown3DCampLook look)
        {
            var window = GetWindow<CampLookCameraPreview>(false, "Camp Camera Preview");
            window.look = look;
        }
        private void Update() { if (EditorApplication.timeSinceStartup >= nextRender) Repaint(); }
        private void OnGUI()
        {
            look = (TopDown3DCampLook)EditorGUILayout.ObjectField("Camp Look", look, typeof(TopDown3DCampLook), true);
            EditorGUILayout.HelpBox("Frozen Edit Mode gameplay-camera rendering, including native Bokeh. The overlay HUD and gameplay simulation are not rendered here.", MessageType.Info);
            if (look == null || look.gameCamera == null || Application.isPlaying) return;
            if (Event.current.type == EventType.Repaint && EditorApplication.timeSinceStartup >= nextRender)
            {
                nextRender = EditorApplication.timeSinceStartup + .15;
                CampLookEditorPreview.Refresh();
                if (image == null) { image = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGBHalf) { hideFlags = HideFlags.HideAndDontSave }; image.Create(); }
                var camera = look.gameCamera;
                var target = camera.targetTexture; var active = RenderTexture.active; float aspect = camera.aspect;
                try { camera.targetTexture = image; camera.aspect = 16f / 9f; camera.Render(); }
                finally { TopDown3DCampOpticsStack.Restore(camera); camera.targetTexture = target; camera.aspect = aspect; RenderTexture.active = active; }
            }
            var rect = GUILayoutUtility.GetAspectRect(16f / 9f);
            if (image != null) EditorGUI.DrawPreviewTexture(rect, image);
        }
        private void OnDisable()
        {
            if (image == null) return;
            image.Release(); DestroyImmediate(image); image = null;
        }
    }
}
