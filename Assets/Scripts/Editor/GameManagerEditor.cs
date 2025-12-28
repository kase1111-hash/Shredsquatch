using UnityEngine;
using UnityEditor;
using Shredsquatch.Core;

namespace Shredsquatch.Editor
{
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GameManager manager = (GameManager)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Info", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Current State", manager.CurrentState.ToString());
                EditorGUILayout.LabelField("Current Mode", manager.CurrentMode.ToString());

                if (manager.CurrentRun != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Run Stats", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Distance", $"{manager.CurrentRun.Distance:F2} km");
                    EditorGUILayout.LabelField("Trick Score", manager.CurrentRun.TrickScore.ToString());
                    EditorGUILayout.LabelField("Max Speed", $"{manager.CurrentRun.MaxSpeed:F0} km/h");
                    EditorGUILayout.LabelField("Max Combo", $"{manager.CurrentRun.MaxCombo}x");
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug Actions", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Start Run"))
                {
                    manager.StartRun();
                }
                if (GUILayout.Button("End Run"))
                {
                    manager.EndRun();
                }
                EditorGUILayout.EndHorizontal();

                if (manager.CurrentState == GameState.Playing)
                {
                    if (GUILayout.Button("Pause"))
                    {
                        manager.PauseGame();
                    }
                }
                else if (manager.CurrentState == GameState.Paused)
                {
                    if (GUILayout.Button("Resume"))
                    {
                        manager.ResumeGame();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see debug info", MessageType.Info);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(manager);
            }
        }
    }
}
