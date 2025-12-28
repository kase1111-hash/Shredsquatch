using UnityEngine;
using UnityEditor;
using Shredsquatch.Terrain;

namespace Shredsquatch.Editor
{
    [CustomEditor(typeof(TerrainGenerator))]
    public class TerrainGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TerrainGenerator generator = (TerrainGenerator)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Preview Chunk"))
            {
                Debug.Log("Preview chunk generation would happen here in play mode");
            }

            if (GUILayout.Button("Randomize Seed"))
            {
                generator.SetSeed(Random.Range(1, 999999));
                EditorUtility.SetDirty(generator);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Terrain generates procedurally at runtime. " +
                "Use the seed to create reproducible runs for leaderboards.",
                MessageType.Info
            );
        }
    }
}
