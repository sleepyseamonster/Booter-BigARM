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
