using BooterBigArm.Runtime;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(PrototypeWorldSettings))]
    public sealed class PrototypeWorldSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty propSpawnChanceProperty;
        private SerializedProperty propCatalogProperty;

        private void OnEnable()
        {
            propSpawnChanceProperty = serializedObject.FindProperty("propSpawnChance");
            propCatalogProperty = serializedObject.FindProperty("propCatalog");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Prop Density", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                new GUIContent("Spawn Chance", "Chance per loaded chunk to spawn one prop instance."),
                new GUIContent(propSpawnChanceProperty.floatValue.ToString("0.000")));
            propSpawnChanceProperty.floatValue = EditorGUILayout.Slider(
                new GUIContent("Chunk Spawn Chance", "Higher values increase how many chunks can hold a prop."),
                propSpawnChanceProperty.floatValue,
                0f,
                0.5f);

            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(
                propCatalogProperty,
                new GUIContent("Prop Catalog", "Authoring catalog for categorized world prop spawning."));

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sparse"))
                {
                    SetDensity(0.08f);
                }

                if (GUILayout.Button("Balanced"))
                {
                    SetDensity(0.12f);
                }

                if (GUILayout.Button("Dense"))
                {
                    SetDensity(0.2f);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SetDensity(float density)
        {
            propSpawnChanceProperty.floatValue = density;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
