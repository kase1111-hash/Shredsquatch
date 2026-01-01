#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Shredsquatch.Editor
{
    /// <summary>
    /// Editor utility for setting up Shredsquatch custom shaders and materials.
    /// </summary>
    public class ShaderSetupUtility : EditorWindow
    {
        private const string SHADERS_PATH = "Assets/Shaders";
        private const string MATERIALS_PATH = "Assets/Materials";

        [MenuItem("Shredsquatch/Shader Setup Utility")]
        public static void ShowWindow()
        {
            GetWindow<ShaderSetupUtility>("Shader Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Shredsquatch Shader Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Use this utility to assign custom Shredsquatch shaders to materials. " +
                "Custom shaders provide enhanced visual effects like snow sparkle, " +
                "Sasquatch fur with subsurface scattering, and animated trails.",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Assign Snow Shader to Snow Material"))
            {
                AssignShaderToMaterial("Snow", "Shredsquatch/Snow Sparkle");
            }

            if (GUILayout.Button("Assign Fur Shader to SasquatchFur Material"))
            {
                AssignShaderToMaterial("SasquatchFur", "Shredsquatch/Sasquatch Fur");
            }

            if (GUILayout.Button("Assign Coin Shader to Coin Material"))
            {
                AssignShaderToMaterial("Coin", "Shredsquatch/Coin Glow");
            }

            GUILayout.Space(10);
            GUILayout.Label("Trail Materials", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Fire Trail Material"))
            {
                AssignShaderToMaterial("TrailFire", "Shredsquatch/Trail Fire");
            }

            if (GUILayout.Button("Setup Rainbow Trail Material"))
            {
                AssignShaderToMaterial("TrailRainbow", "Shredsquatch/Trail Rainbow");
            }

            if (GUILayout.Button("Setup Lightning Trail Material"))
            {
                AssignShaderToMaterial("TrailLightning", "Shredsquatch/Trail Lightning");
            }

            GUILayout.Space(10);
            GUILayout.Label("Environment", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Aurora Material"))
            {
                AssignShaderToMaterial("Aurora", "Shredsquatch/Aurora Borealis");
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Setup All Materials", GUILayout.Height(30)))
            {
                SetupAllMaterials();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Validate Shader Compilation"))
            {
                ValidateShaders();
            }
        }

        private void AssignShaderToMaterial(string materialName, string shaderName)
        {
            string materialPath = $"{MATERIALS_PATH}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                EditorUtility.DisplayDialog("Material Not Found",
                    $"Could not find material at: {materialPath}",
                    "OK");
                return;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Shader Not Found",
                    $"Could not find shader: {shaderName}\n\n" +
                    "Make sure the shader file exists and has no compilation errors.",
                    "OK");
                return;
            }

            material.shader = shader;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            Debug.Log($"Assigned shader '{shaderName}' to material '{materialName}'");
        }

        private void SetupAllMaterials()
        {
            AssignShaderToMaterial("Snow", "Shredsquatch/Snow Sparkle");
            AssignShaderToMaterial("SasquatchFur", "Shredsquatch/Sasquatch Fur");
            AssignShaderToMaterial("Coin", "Shredsquatch/Coin Glow");
            AssignShaderToMaterial("TrailFire", "Shredsquatch/Trail Fire");
            AssignShaderToMaterial("TrailRainbow", "Shredsquatch/Trail Rainbow");
            AssignShaderToMaterial("TrailLightning", "Shredsquatch/Trail Lightning");
            AssignShaderToMaterial("Aurora", "Shredsquatch/Aurora Borealis");

            EditorUtility.DisplayDialog("Setup Complete",
                "All materials have been updated with custom shaders.",
                "OK");
        }

        private void ValidateShaders()
        {
            string[] shaderNames = new string[]
            {
                "Shredsquatch/Snow Sparkle",
                "Shredsquatch/Sasquatch Fur",
                "Shredsquatch/Coin Glow",
                "Shredsquatch/Trail Fire",
                "Shredsquatch/Trail Rainbow",
                "Shredsquatch/Trail Lightning",
                "Shredsquatch/Aurora Borealis",
                "Shredsquatch/Snow Tracks",
                "Shredsquatch/Powerup Glow"
            };

            int found = 0;
            int missing = 0;
            string missingList = "";

            foreach (string name in shaderNames)
            {
                Shader shader = Shader.Find(name);
                if (shader != null)
                {
                    found++;
                }
                else
                {
                    missing++;
                    missingList += $"- {name}\n";
                }
            }

            if (missing == 0)
            {
                EditorUtility.DisplayDialog("Shader Validation",
                    $"All {found} custom shaders compiled successfully!",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Shader Validation",
                    $"Found: {found}\nMissing/Error: {missing}\n\n{missingList}",
                    "OK");
            }
        }

        [MenuItem("Shredsquatch/Create Shader Manager")]
        public static void CreateShaderManager()
        {
            if (FindObjectOfType<Shredsquatch.Rendering.ShaderManager>() != null)
            {
                EditorUtility.DisplayDialog("Already Exists",
                    "A ShaderManager already exists in the scene.",
                    "OK");
                return;
            }

            GameObject go = new GameObject("ShaderManager");
            go.AddComponent<Shredsquatch.Rendering.ShaderManager>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Shader Manager");

            Debug.Log("Created ShaderManager GameObject");
        }
    }
}
#endif
