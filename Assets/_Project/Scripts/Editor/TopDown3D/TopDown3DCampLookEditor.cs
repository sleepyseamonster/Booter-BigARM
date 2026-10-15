using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DCampLook))]
    public sealed class TopDown3DCampLookEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor opticsEditor, skyEditor;
        private bool showSky;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gameCamera"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sun"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previewInEditor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previewSceneViewEffects"));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Edit camp look preview");
                serializedObject.ApplyModifiedProperties();
                CampLookEditorPreview.Refresh();
            }
            var look = (TopDown3DCampLook)target;
            EditorGUILayout.HelpBox("Editor preview uses these scene references. Preview toggles affect the editing view; the selected atmosphere and optics assets hold the saved look.", MessageType.Info);
            if ((look.gameCamera != null && look.gameCamera.gameObject.scene != look.gameObject.scene)
                || (look.sun != null && look.sun.gameObject.scene != look.gameObject.scene))
                EditorGUILayout.HelpBox("Choose a camera and sun from this Camp Look object's scene.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(look.gameCamera == null || look.sun == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("View Sun")) CampLookEditorPreview.LookAtSun(look);
                if (GUILayout.Button("Match Gameplay Camera")) CampLookEditorPreview.MatchGameCamera(look);
            }
            if (GUILayout.Button("Open Edit Mode Camera Preview")) CampLookCameraPreview.Open(look);

            DrawSun(look.sun);
            EditorGUILayout.Space(8f);
            if (look.gameCamera == null)
            {
                EditorGUILayout.HelpBox("Assign the gameplay camera to edit its selected atmosphere.", MessageType.Info);
                return;
            }
            DrawBindingSelection(look.gameCamera);
            if (!TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(look.gameCamera, out var profile))
            {
                EditorGUILayout.HelpBox("The camera needs one enabled, valid atmosphere binding in its scene. Resolve duplicate owners or missing sky references before previewing.", MessageType.Warning);
                return;
            }
            DrawProfile(profile, ref opticsEditor, ref skyEditor, ref showSky);
        }

        private void OnDisable()
        {
            DestroyImmediate(opticsEditor);
            DestroyImmediate(skyEditor);
        }

        internal static void DrawSun(PerpetualTwilightSun sun)
        {
            if (sun == null) return;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Sun Cycle and Direction", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The sun component owns direction, timing and direct intensity. The selected atmosphere owns sun colors and ambient fill. Light inspector values are computed from these controls.", MessageType.Info);
            var source = new SerializedObject(sun);
            source.Update();
            EditorGUI.BeginChangeCheck();
            foreach (var name in new[] { "cycleEnabled", "cycleDurationSeconds", "cycle01", "centerElevationDegrees",
                "elevationAmplitudeDegrees", "centerAzimuthDegrees", "azimuthAmplitudeDegrees",
                "deepTwilightIntensity", "brightTwilightIntensity" })
                DrawProperty(source, name);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sun, "Edit camp sun cycle");
                source.ApplyModifiedProperties();
                CampLookEditorPreview.Refresh();
            }
            var light = sun.GetComponent<Light>();
            if (light == null) return;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ColorField("Computed Light Color", light.color);
                EditorGUILayout.FloatField("Computed Light Intensity", light.intensity);
            }
        }

        internal static void DrawBindingSelection(Camera camera)
        {
            var binding = camera.GetComponent<TopDown3DSceneRenderingOverride>();
            if (binding == null) return;
            EditorGUILayout.LabelField("Camp Atmosphere Selection", EditorStyles.boldLabel);
            var source = new SerializedObject(binding);
            source.Update();
            EditorGUI.BeginChangeCheck();
            DrawProperty(source, "atmosphereProfile");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(binding, "Select camp atmosphere");
                source.ApplyModifiedProperties();
                TopDown3DCampOpticsStack.Release(camera);
                CampLookEditorPreview.Refresh();
            }
        }

        internal static void DrawProfile(TopDown3DAtmosphereStudyProfile profile,
            ref UnityEditor.Editor opticsEditor, ref UnityEditor.Editor skyEditor, ref bool showSky)
        {
            EditorGUILayout.HelpBox("Edits below change the named assets and every look that references them. Undo is supported. Camp overrides control this camera's look.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Atmosphere Asset", profile, typeof(TopDown3DAtmosphereStudyProfile), false);
            var source = new SerializedObject(profile);
            source.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Sun and Ambient Colors", EditorStyles.boldLabel);
            foreach (var name in new[] { "deepSunColor", "brightSunColor", "deepSkyFill", "brightSkyFill",
                "deepEquatorFill", "brightEquatorFill", "deepGroundFill", "brightGroundFill", "deepSkyExposure", "brightSkyExposure" })
                DrawProperty(source, name);
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Camera Optics", EditorStyles.boldLabel);
            foreach (var name in new[] { "nativeOptics", "flareEnabled", "flareIntensity", "tiltShiftEnabled",
                "useReferenceResolutionTiltShift" }) DrawProperty(source, name);
            if (source.FindProperty("tiltShiftEnabled").boolValue)
                foreach (var name in new[] { "tiltShiftDownsample", "tiltShiftReferenceHeight", "tiltShiftBlurRadiusPixels",
                    "tiltShiftBandAngleDegrees", "tiltShiftFocusCenter", "tiltShiftSharpBandWidth", "tiltShiftFeatherWidth" }) DrawProperty(source, name);
            if (source.FindProperty("nativeOptics").objectReferenceValue == null)
                foreach (var name in new[] { "bloomEnabled", "bloomIntensity", "bloomTint", "sunAureoleIntensity",
                    "bloomLevels", "bloomDownsample", "streakEnabled" }) DrawProperty(source, name);
            DrawProperty(source, "skyMaterial");
            DrawProperty(source, "sunlessReflection");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(profile, "Edit camp atmosphere asset");
                source.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                CampLookEditorPreview.Refresh();
            }

            if (profile.nativeOptics != null)
            {
                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField("Native Optics Asset", EditorStyles.boldLabel);
                DrawAssetEditor(profile.nativeOptics, ref opticsEditor, "Edit camp optics asset");
                if (!profile.nativeOptics.IsValid)
                    EditorGUILayout.HelpBox("The optics profile is invalid. Correct its version, ranges and supported effect modes before previewing.", MessageType.Warning);
                if (profile.nativeOptics.depthMode != UnityEngine.Rendering.Universal.DepthOfFieldMode.Off && !profile.nativeOptics.gaussianBeforeLens)
                    EditorGUILayout.HelpBox("Unity disables this native DOF mode in Scene view. Use Edit Mode Camera Preview to tune it with the exact gameplay-camera framing.", MessageType.Info);
                if (profile.tiltShiftEnabled && profile.nativeOptics.depthMode != UnityEngine.Rendering.Universal.DepthOfFieldMode.Off)
                    EditorGUILayout.HelpBox("Choose one world blur: artistic band or native depth blur.", MessageType.Warning);
            }
            else if (opticsEditor != null) { DestroyImmediate(opticsEditor); opticsEditor = null; }

            if (profile.skyMaterial == null) return;
            if (GUILayout.Button("Ping Sky Image")) EditorGUIUtility.PingObject(profile.skyMaterial.mainTexture);
            showSky = EditorGUILayout.Foldout(showSky, "Source Sky Material Asset", true);
            if (showSky)
            {
                EditorGUILayout.HelpBox("Sun-disc color and panorama tint live in this material. Deep/Bright Sun Color controls illumination and flare. The atmosphere owns exposure and the sun supplies direction/phase.", MessageType.Info);
                CreateCachedEditor(profile.skyMaterial, typeof(MaterialEditor), ref skyEditor);
                Undo.RecordObject(profile.skyMaterial, "Edit camp sky material asset");
                EditorGUI.BeginChangeCheck();
                foreach (var property in MaterialEditor.GetMaterialProperties(new Object[] { profile.skyMaterial }))
                {
                    if ((property.flags & MaterialProperty.PropFlags.HideInInspector) != 0
                        || property.name == "_Exposure" || property.name == "_TwilightBrightness" || property.name == "_SunDirection"
                        || (property.name == "_SunAngularRadius" && profile.nativeOptics != null)) continue;
                    ((MaterialEditor)skyEditor).ShaderProperty(property, property.displayName);
                }
                if (EditorGUI.EndChangeCheck()) { EditorUtility.SetDirty(profile.skyMaterial); CampLookEditorPreview.Refresh(); }
            }
        }

        private static void DrawAssetEditor(Object asset, ref UnityEditor.Editor cached, string undoLabel)
        {
            CreateCachedEditor(asset, null, ref cached);
            Undo.RecordObject(asset, undoLabel);
            EditorGUI.BeginChangeCheck();
            cached.OnInspectorGUI();
            if (!EditorGUI.EndChangeCheck()) return;
            EditorUtility.SetDirty(asset);
            CampLookEditorPreview.Refresh();
        }

        private static void DrawProperty(SerializedObject source, string name)
        {
            var property = source.FindProperty(name);
            if (property != null) EditorGUILayout.PropertyField(property, true);
        }
    }

    [CustomEditor(typeof(PerpetualTwilightSun))]
    public sealed class PerpetualTwilightSunLookEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor opticsEditor, skyEditor;
        private bool showSky;
        public override void OnInspectorGUI()
        {
            var sun = (PerpetualTwilightSun)target;
            var camera = TopDown3DSceneRenderingOverride.AtmosphereCamera(sun.gameObject.scene);
            if (camera == null || !TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out var profile))
            { DrawDefaultInspector(); return; }
            TopDown3DCampLookEditor.DrawSun(sun);
            var look = sun.GetComponentInParent<TopDown3DCampLook>();
            if (look != null && GUILayout.Button("View Sun")) CampLookEditorPreview.LookAtSun(look);
            TopDown3DCampLookEditor.DrawProfile(profile, ref opticsEditor, ref skyEditor, ref showSky);
        }
        private void OnDisable()
        {
            if (opticsEditor != null) DestroyImmediate(opticsEditor);
            if (skyEditor != null) DestroyImmediate(skyEditor);
        }
    }
}
