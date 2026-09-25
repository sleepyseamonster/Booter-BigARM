using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DCameraRig))]
    [CanEditMultipleObjects]
    public sealed class TopDown3DCameraRigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10f);
            DrawSharedTiltShiftControls();
            EditorGUILayout.Space(10f);
            DrawSharedAnamorphicStreakControls();
            EditorGUILayout.Space(10f);
            DrawSharedRoundLensFlareControls();
            EditorGUILayout.Space(10f);
            DrawSharedSunBloomControls();
        }

        private static void DrawSharedSunBloomControls()
        {
            EditorGUILayout.LabelField("Shared Sun Bloom (Game + Scene View)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Bloom runs after the sun lens profile. A multi-scale HDR pyramid separates tight glow, medium spread, and broad haze. The atmospheric aureole is sourced directly from the directional light, so geometry may hide the disc without switching off the surrounding light.",
                MessageType.Info);

            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var feature = renderer != null
                ? renderer.rendererFeatures.OfType<TopDown3DSunBloomFeature>().FirstOrDefault()
                : null;
            if (renderer == null || feature == null)
            {
                EditorGUILayout.HelpBox("The shared TopDown3D sun-bloom renderer feature could not be found.", MessageType.Warning);
                return;
            }

            var featureObject = new SerializedObject(feature);
            featureObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(featureObject.FindProperty("bloomEnabled"), new GUIContent("Enabled"));
            EditorGUILayout.PropertyField(featureObject.FindProperty("previewInSceneView"), new GUIContent("Preview In Scene View"));
            EditorGUILayout.IntSlider(featureObject.FindProperty("downsample"), 1, 4,
                new GUIContent("Downsample", "Two is the high-quality baseline; larger values are faster and softer."));
            EditorGUILayout.IntSlider(featureObject.FindProperty("pyramidLevels"), 3, 7,
                new GUIContent("Bloom Scales", "More scales create a broader, more photographic light spread."));
            EditorGUILayout.Slider(featureObject.FindProperty("intensity"), 0f, 2f, new GUIContent("Bloom Intensity"));
            EditorGUILayout.Slider(featureObject.FindProperty("threshold"), 0f, 4f,
                new GUIContent("Highlight Threshold", "Only HDR highlights above this brightness feed bloom."));
            EditorGUILayout.Slider(featureObject.FindProperty("softKnee"), 0f, 1f,
                new GUIContent("Threshold Feather", "Softens the transition around the highlight threshold."));
            EditorGUILayout.Slider(featureObject.FindProperty("scatter"), 0f, 1f,
                new GUIContent("Scatter", "Controls the radius of the light glow."));
            EditorGUILayout.Slider(featureObject.FindProperty("clamp"), 1f, 32f,
                new GUIContent("HDR Clamp", "Limits extreme bloom energy without flattening the source."));
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Scale Balance", EditorStyles.miniBoldLabel);
            EditorGUILayout.Slider(featureObject.FindProperty("tightWeight"), 0f, 2f,
                new GUIContent("Tight Glow", "Weight of the sharp bloom nearest bright highlights."));
            EditorGUILayout.Slider(featureObject.FindProperty("mediumWeight"), 0f, 2f,
                new GUIContent("Medium Glow", "Weight of the middle bloom scales."));
            EditorGUILayout.Slider(featureObject.FindProperty("broadWeight"), 0f, 2f,
                new GUIContent("Broad Haze", "Weight of the widest, lowest-frequency light spread."));
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Persistent Sun Aureole", EditorStyles.miniBoldLabel);
            EditorGUILayout.Slider(featureObject.FindProperty("sunAureoleIntensity"), 0f, 2f,
                new GUIContent("Aureole Intensity", "Atmospheric light around the sun that is not switched off by foreground geometry."));
            EditorGUILayout.Slider(featureObject.FindProperty("sunAureoleRadius"), 0.02f, 0.5f,
                new GUIContent("Aureole Radius", "Screen-space radius of the broad atmospheric sun glow."));
            EditorGUILayout.PropertyField(featureObject.FindProperty("tint"), new GUIContent("Bloom Tint"));

            if (EditorGUI.EndChangeCheck())
            {
                featureObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Reset Sun Bloom"))
            {
                featureObject.FindProperty("bloomEnabled").boolValue = true;
                featureObject.FindProperty("previewInSceneView").boolValue = true;
                featureObject.FindProperty("downsample").intValue = TopDown3DSunBloomFeature.DefaultDownsample;
                featureObject.FindProperty("pyramidLevels").intValue = TopDown3DSunBloomFeature.DefaultPyramidLevels;
                featureObject.FindProperty("intensity").floatValue = TopDown3DSunBloomFeature.DefaultIntensity;
                featureObject.FindProperty("threshold").floatValue = TopDown3DSunBloomFeature.DefaultThreshold;
                featureObject.FindProperty("softKnee").floatValue = TopDown3DSunBloomFeature.DefaultSoftKnee;
                featureObject.FindProperty("scatter").floatValue = TopDown3DSunBloomFeature.DefaultScatter;
                featureObject.FindProperty("clamp").floatValue = TopDown3DSunBloomFeature.DefaultClamp;
                featureObject.FindProperty("tightWeight").floatValue = TopDown3DSunBloomFeature.DefaultTightWeight;
                featureObject.FindProperty("mediumWeight").floatValue = TopDown3DSunBloomFeature.DefaultMediumWeight;
                featureObject.FindProperty("broadWeight").floatValue = TopDown3DSunBloomFeature.DefaultBroadWeight;
                featureObject.FindProperty("sunAureoleIntensity").floatValue = TopDown3DSunBloomFeature.DefaultSunAureoleIntensity;
                featureObject.FindProperty("sunAureoleRadius").floatValue = TopDown3DSunBloomFeature.DefaultSunGlowRadius;
                featureObject.FindProperty("tint").colorValue = TopDown3DSunBloomFeature.DefaultTint;
                featureObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
                SceneView.RepaintAll();
            }
        }

        private static void DrawSharedRoundLensFlareControls()
        {
            EditorGUILayout.LabelField("Shared Sun Lens Profile (Game + Scene View)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This lens profile follows the physical sun, uses temporally stabilized depth visibility, and gives the sun core, atmospheric aureole, and lens ghosts separate occlusion responses. The three optical sprites remain artist-replaceable.",
                MessageType.Info);

            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var feature = renderer != null
                ? renderer.rendererFeatures.OfType<TopDown3DRoundLensFlareFeature>().FirstOrDefault()
                : null;
            if (renderer == null || feature == null)
            {
                EditorGUILayout.HelpBox(
                    "The shared TopDown3D round lens-flare renderer feature could not be found.",
                    MessageType.Warning);
                return;
            }

            var featureObject = new SerializedObject(feature);
            featureObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("flareEnabled"),
                new GUIContent("Enabled"));
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("previewInSceneView"),
                new GUIContent("Preview In Scene View"));
            EditorGUILayout.Slider(
                featureObject.FindProperty("intensity"),
                0f,
                2f,
                new GUIContent("Flare Intensity"));
            EditorGUILayout.Slider(
                featureObject.FindProperty("radius"),
                0.02f,
                0.75f,
                new GUIContent("Round Radius", "Controls the primary flare radius relative to screen height."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("anisotropy"),
                0.4f,
                2.5f,
                new GUIContent("Round Anisotropy", "Stretches or compresses the round flare horizontally."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("ghostReach"),
                0f,
                3f,
                new GUIContent("Ghost Reach", "Controls how far the circular lens ghosts travel across the frame."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("edgeReach"),
                0f,
                1f,
                new GUIContent("Edge Reach", "Keeps the flare visible while its physical light source moves beyond the screen edge."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("haloThickness"),
                0.03f,
                0.5f,
                new GUIContent("Halo Thickness"));
            EditorGUILayout.Slider(
                featureObject.FindProperty("hdrEnergy"),
                0.5f,
                6f,
                new GUIContent("HDR Sun Energy", "Controls how strongly the physical sun and flare feed the bloom pass."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("occlusionRadius"),
                0.005f,
                0.25f,
                new GUIContent("Source Mask Softness", "Controls how progressively geometry masks the visible sun disc. It does not reduce halo or bloom energy."));
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Occlusion Response", EditorStyles.miniBoldLabel);
            EditorGUILayout.Slider(
                featureObject.FindProperty("coreOcclusion"),
                0f,
                1f,
                new GUIContent("Sun Core", "One fully hides the direct sun behind geometry; zero keeps it visible."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("aureoleOcclusion"),
                0f,
                1f,
                new GUIContent("Atmospheric Aureole", "Keep this low so atmospheric light remains around silhouettes."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("ghostOcclusion"),
                0f,
                1f,
                new GUIContent("Lens Ghosts", "Partial response keeps ghosts subtle instead of switching the entire lens stack off."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("occlusionStability"),
                0f,
                0.95f,
                new GUIContent("Occlusion Stability", "Smooths depth visibility over several frames to prevent flare popping along silhouettes."));
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("tint"),
                new GUIContent("Flare Tint"));
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("primaryFlareSprite"),
                new GUIContent("Primary Flare Sprite", "Transparent warm source orb."));
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("ghostRingSprite"),
                new GUIContent("Ghost Ring Sprite", "Transparent circular coating reflection."));
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("apertureGhostSprite"),
                new GUIContent("Aperture Ghost Sprite", "Transparent chromatic glass ghost."));

            if (EditorGUI.EndChangeCheck())
            {
                featureObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Reset Round Lens Flare"))
            {
                featureObject.FindProperty("flareEnabled").boolValue = true;
                featureObject.FindProperty("previewInSceneView").boolValue = true;
                featureObject.FindProperty("intensity").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultIntensity;
                featureObject.FindProperty("radius").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultRadius;
                featureObject.FindProperty("anisotropy").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultAnisotropy;
                featureObject.FindProperty("ghostReach").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultGhostReach;
                featureObject.FindProperty("edgeReach").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultEdgeReach;
                featureObject.FindProperty("haloThickness").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultHaloThickness;
                featureObject.FindProperty("hdrEnergy").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultHdrEnergy;
                featureObject.FindProperty("occlusionRadius").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultOcclusionRadius;
                featureObject.FindProperty("coreOcclusion").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultCoreOcclusion;
                featureObject.FindProperty("aureoleOcclusion").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultAureoleOcclusion;
                featureObject.FindProperty("ghostOcclusion").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultGhostOcclusion;
                featureObject.FindProperty("occlusionStability").floatValue =
                    TopDown3DRoundLensFlareFeature.DefaultOcclusionStability;
                featureObject.FindProperty("tint").colorValue =
                    TopDown3DRoundLensFlareFeature.DefaultTint;
                featureObject.FindProperty("primaryFlareSprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        TopDown3DPrototypeBuilder.RoundLensFlarePrimarySpritePath);
                featureObject.FindProperty("ghostRingSprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        TopDown3DPrototypeBuilder.RoundLensFlareGhostRingSpritePath);
                featureObject.FindProperty("apertureGhostSprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        TopDown3DPrototypeBuilder.RoundLensFlareApertureGhostSpritePath);
                featureObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
                SceneView.RepaintAll();
            }
        }

        private static void DrawSharedAnamorphicStreakControls()
        {
            EditorGUILayout.LabelField("Shared Anamorphic Streak (Game + Scene View)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This is the one shared bright-highlight streak used by both the game camera and Scene View.",
                MessageType.Info);

            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var feature = renderer != null
                ? renderer.rendererFeatures.OfType<TopDown3DAnamorphicStreakFeature>().FirstOrDefault()
                : null;
            if (renderer == null || feature == null)
            {
                EditorGUILayout.HelpBox(
                    "The shared TopDown3D anamorphic streak renderer feature could not be found.",
                    MessageType.Warning);
                return;
            }

            var featureObject = new SerializedObject(feature);
            featureObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("streakEnabled"),
                new GUIContent("Enabled"));
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("previewInSceneView"),
                new GUIContent("Preview In Scene View"));
            EditorGUILayout.Slider(
                featureObject.FindProperty("intensity"),
                0f,
                2f,
                new GUIContent("Streak Intensity", "Controls the brightness of the horizontal light streak."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("length"),
                0f,
                1f,
                new GUIContent("Streak Length", "Controls how far bright highlights stretch across the picture."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("threshold"),
                0f,
                4f,
                new GUIContent("Highlight Threshold", "Lower values allow dimmer surfaces to create streaks."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("orientation"),
                -180f,
                180f,
                new GUIContent("Orientation", "Rotates the streak direction in screen space. Zero is horizontal."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("chromaticSeparation"),
                0f,
                0.15f,
                new GUIContent("Color Separation", "Offsets red and blue slightly along the streak."));
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("tint"),
                new GUIContent("Streak Tint"));

            if (EditorGUI.EndChangeCheck())
            {
                featureObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Reset Anamorphic Streak"))
            {
                featureObject.FindProperty("streakEnabled").boolValue = true;
                featureObject.FindProperty("previewInSceneView").boolValue = true;
                featureObject.FindProperty("intensity").floatValue =
                    TopDown3DAnamorphicStreakFeature.DefaultIntensity;
                featureObject.FindProperty("length").floatValue =
                    TopDown3DAnamorphicStreakFeature.DefaultLength;
                featureObject.FindProperty("threshold").floatValue =
                    TopDown3DAnamorphicStreakFeature.DefaultThreshold;
                featureObject.FindProperty("orientation").floatValue =
                    TopDown3DAnamorphicStreakFeature.DefaultOrientation;
                featureObject.FindProperty("chromaticSeparation").floatValue =
                    TopDown3DAnamorphicStreakFeature.DefaultChromaticSeparation;
                featureObject.FindProperty("tint").colorValue =
                    TopDown3DAnamorphicStreakFeature.DefaultTint;
                featureObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
                SceneView.RepaintAll();
            }
        }

        private static void DrawSharedTiltShiftControls()
        {
            EditorGUILayout.LabelField("Shared Tilt Shift (Game + Scene View)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These controls edit the one shared tilt-shift effect used by both the game camera and Scene View.",
                MessageType.Info);

            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var feature = renderer != null
                ? renderer.rendererFeatures.OfType<TopDown3DTiltShiftFeature>().FirstOrDefault()
                : null;
            if (renderer == null || feature == null)
            {
                EditorGUILayout.HelpBox(
                    "The shared TopDown3D tilt-shift renderer feature could not be found.",
                    MessageType.Warning);
                return;
            }

            var featureObject = new SerializedObject(feature);
            featureObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                featureObject.FindProperty("previewInSceneView"),
                new GUIContent("Preview In Scene View"));
            EditorGUILayout.Slider(
                featureObject.FindProperty("focusCenter"),
                0f,
                1f,
                new GUIContent("Focus Center", "Moves the sharp band vertically through the picture."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("sharpBandWidth"),
                0.05f,
                0.8f,
                new GUIContent("Sharp Band Width", "Controls how much of the middle of the picture stays sharp."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("featherWidth"),
                0.01f,
                0.5f,
                new GUIContent("Feather Width", "Controls how gradually the sharp band fades into blur."));
            EditorGUILayout.Slider(
                featureObject.FindProperty("blurRadius"),
                0.5f,
                12f,
                new GUIContent("Blur Strength", "Controls the maximum blur at the top and bottom of the picture."));

            if (EditorGUI.EndChangeCheck())
            {
                featureObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
                SceneView.RepaintAll();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Tilt Shift"))
                {
                    featureObject.FindProperty("previewInSceneView").boolValue = true;
                    featureObject.FindProperty("focusCenter").floatValue =
                        TopDown3DTiltShiftFeature.DefaultFocusCenter;
                    featureObject.FindProperty("sharpBandWidth").floatValue =
                        TopDown3DTiltShiftFeature.DefaultSharpBandWidth;
                    featureObject.FindProperty("featherWidth").floatValue =
                        TopDown3DTiltShiftFeature.DefaultFeatherWidth;
                    featureObject.FindProperty("blurRadius").floatValue =
                        TopDown3DTiltShiftFeature.DefaultBlurRadius;
                    featureObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(feature);
                    renderer.SetDirty();
                    EditorUtility.SetDirty(renderer);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Select Renderer Asset"))
                {
                    Selection.activeObject = renderer;
                    EditorGUIUtility.PingObject(renderer);
                }
            }
        }
    }
}
