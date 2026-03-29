using BooterBigArm.Runtime;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(PrototypeWorldGenerator))]
    public sealed class PrototypeWorldGeneratorEditor : UnityEditor.Editor
    {
        private SerializedProperty targetProperty;
        private SerializedProperty propPrefabProperty;
        private SerializedProperty propPrefabsProperty;
        private SerializedProperty propParentProperty;
        private SerializedProperty propSpawnChanceProperty;
        private SerializedProperty pebbleTilemapProperty;
        private SerializedProperty pebbleTileSpritesProperty;
        private SerializedProperty rockTilemapProperty;
        private SerializedProperty rockTileSpritesProperty;
        private SerializedProperty ruleGroundTilemapProperty;
        private SerializedProperty ruleGroundTileProperty;
        private SerializedProperty sandOverlayTilemapProperty;
        private SerializedProperty sandOverlayTileSpritesProperty;
        private SerializedProperty sandPatchRegionSizeWorldProperty;
        private SerializedProperty sandPatchMinRadiusWorldProperty;
        private SerializedProperty sandPatchMaxRadiusWorldProperty;
        private SerializedProperty sandPatchRegionChanceProperty;
        private SerializedProperty sandPatchEdgeNoiseProperty;
        private SerializedProperty sandPatchWobbleStrengthProperty;
        private SerializedProperty sandPatchWobbleScaleWorldProperty;
        private SerializedProperty sandPatchErosionProperty;
        private SerializedProperty sandPatchInteriorCutoutProperty;
        private SerializedProperty sandPatchRibbonBiasProperty;
        private SerializedProperty sandPatchRibbonScaleWorldProperty;
        private SerializedProperty smoothTilemapProperty;
        private SerializedProperty smoothTileSpritesProperty;
        private SerializedProperty tileSpritesProperty;
        private SerializedProperty seedProperty;
        private SerializedProperty chunkSizeProperty;
        private SerializedProperty chunkRadiusProperty;
        private SerializedProperty chunkOperationsPerFrameProperty;

        private void OnEnable()
        {
            targetProperty = serializedObject.FindProperty("target");
            propPrefabProperty = serializedObject.FindProperty("propPrefab");
            propPrefabsProperty = serializedObject.FindProperty("propPrefabs");
            propParentProperty = serializedObject.FindProperty("propParent");
            propSpawnChanceProperty = serializedObject.FindProperty("propSpawnChance");
            pebbleTilemapProperty = serializedObject.FindProperty("pebbleTilemap");
            pebbleTileSpritesProperty = serializedObject.FindProperty("pebbleTileSprites");
            rockTilemapProperty = serializedObject.FindProperty("rockTilemap");
            rockTileSpritesProperty = serializedObject.FindProperty("rockTileSprites");
            ruleGroundTilemapProperty = serializedObject.FindProperty("ruleGroundTilemap");
            ruleGroundTileProperty = serializedObject.FindProperty("ruleGroundTile");
            sandOverlayTilemapProperty = serializedObject.FindProperty("sandOverlayTilemap");
            sandOverlayTileSpritesProperty = serializedObject.FindProperty("sandOverlayTileSprites");
            sandPatchRegionSizeWorldProperty = serializedObject.FindProperty("sandPatchRegionSizeWorld");
            sandPatchMinRadiusWorldProperty = serializedObject.FindProperty("sandPatchMinRadiusWorld");
            sandPatchMaxRadiusWorldProperty = serializedObject.FindProperty("sandPatchMaxRadiusWorld");
            sandPatchRegionChanceProperty = serializedObject.FindProperty("sandPatchRegionChance");
            sandPatchEdgeNoiseProperty = serializedObject.FindProperty("sandPatchEdgeNoise");
            sandPatchWobbleStrengthProperty = serializedObject.FindProperty("sandPatchWobbleStrength");
            sandPatchWobbleScaleWorldProperty = serializedObject.FindProperty("sandPatchWobbleScaleWorld");
            sandPatchErosionProperty = serializedObject.FindProperty("sandPatchErosion");
            sandPatchInteriorCutoutProperty = serializedObject.FindProperty("sandPatchInteriorCutout");
            sandPatchRibbonBiasProperty = serializedObject.FindProperty("sandPatchRibbonBias");
            sandPatchRibbonScaleWorldProperty = serializedObject.FindProperty("sandPatchRibbonScaleWorld");
            smoothTilemapProperty = serializedObject.FindProperty("smoothTilemap");
            smoothTileSpritesProperty = serializedObject.FindProperty("smoothTileSprites");
            tileSpritesProperty = serializedObject.FindProperty("tileSprites");
            seedProperty = serializedObject.FindProperty("seed");
            chunkSizeProperty = serializedObject.FindProperty("chunkSize");
            chunkRadiusProperty = serializedObject.FindProperty("chunkRadius");
            chunkOperationsPerFrameProperty = serializedObject.FindProperty("chunkOperationsPerFrame");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(targetProperty);
            EditorGUILayout.PropertyField(propPrefabProperty);
            EditorGUILayout.PropertyField(propPrefabsProperty);
            EditorGUILayout.PropertyField(propParentProperty);

            DrawSpawnDensitySection();

            EditorGUILayout.Space(8f);
            EditorGUILayout.PropertyField(tileSpritesProperty, true);
            EditorGUILayout.PropertyField(ruleGroundTilemapProperty, new GUIContent("Sand Patch Tilemap"));
            EditorGUILayout.PropertyField(ruleGroundTileProperty, new GUIContent("Sand Patch Rule Tile"));
            EditorGUILayout.PropertyField(sandPatchRegionSizeWorldProperty, new GUIContent("Sand Patch Region Size"));
            EditorGUILayout.PropertyField(sandPatchMinRadiusWorldProperty, new GUIContent("Sand Patch Min Radius"));
            EditorGUILayout.PropertyField(sandPatchMaxRadiusWorldProperty, new GUIContent("Sand Patch Max Radius"));
            EditorGUILayout.PropertyField(sandPatchRegionChanceProperty, new GUIContent("Sand Patch Region Chance"));
            EditorGUILayout.PropertyField(sandPatchEdgeNoiseProperty, new GUIContent("Sand Patch Edge Noise"));
            EditorGUILayout.PropertyField(sandPatchWobbleStrengthProperty, new GUIContent("Sand Patch Wobble Strength"));
            EditorGUILayout.PropertyField(sandPatchWobbleScaleWorldProperty, new GUIContent("Sand Patch Wobble Scale"));
            EditorGUILayout.PropertyField(sandPatchErosionProperty, new GUIContent("Sand Patch Erosion"));
            EditorGUILayout.PropertyField(sandPatchInteriorCutoutProperty, new GUIContent("Sand Patch Interior Cutout"));
            EditorGUILayout.PropertyField(sandPatchRibbonBiasProperty, new GUIContent("Sand Patch Ribbon Bias"));
            EditorGUILayout.PropertyField(sandPatchRibbonScaleWorldProperty, new GUIContent("Sand Patch Ribbon Scale"));
            EditorGUILayout.PropertyField(sandOverlayTilemapProperty);
            EditorGUILayout.PropertyField(sandOverlayTileSpritesProperty, true);
            EditorGUILayout.PropertyField(pebbleTilemapProperty);
            EditorGUILayout.PropertyField(pebbleTileSpritesProperty, true);
            EditorGUILayout.PropertyField(rockTilemapProperty);
            EditorGUILayout.PropertyField(rockTileSpritesProperty, true);
            EditorGUILayout.PropertyField(smoothTilemapProperty);
            EditorGUILayout.PropertyField(smoothTileSpritesProperty, true);
            EditorGUILayout.PropertyField(seedProperty);
            EditorGUILayout.PropertyField(chunkSizeProperty);
            EditorGUILayout.PropertyField(chunkRadiusProperty);
            EditorGUILayout.PropertyField(chunkOperationsPerFrameProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSpawnDensitySection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Prop Density", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                new GUIContent("Spawn Chance", "Chance per loaded chunk to spawn one prop instance."),
                new GUIContent(propSpawnChanceProperty.floatValue.ToString("0.000")));
            propSpawnChanceProperty.floatValue = EditorGUILayout.Slider(
                new GUIContent("Chunk Spawn Chance", "Higher values increase how many chunks can hold a prop."),
                propSpawnChanceProperty.floatValue,
                0f,
                0.5f);

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sparse"))
                {
                    SetDensityAndRebuild(0.08f);
                }

                if (GUILayout.Button("Balanced"))
                {
                    SetDensityAndRebuild(0.12f);
                }

                if (GUILayout.Button("Dense"))
                {
                    SetDensityAndRebuild(0.2f);
                }
            }

            if (GUILayout.Button("Rebuild Visible World"))
            {
                serializedObject.ApplyModifiedProperties();
                var generator = (PrototypeWorldGenerator)target;
                generator.ResetWorld(generator.Seed);
            }
        }

        private void SetDensityAndRebuild(float density)
        {
            propSpawnChanceProperty.floatValue = density;
            serializedObject.ApplyModifiedProperties();
            var generator = (PrototypeWorldGenerator)target;
            generator.ResetWorld(generator.Seed);
            EditorUtility.SetDirty(generator);
        }
    }
}
