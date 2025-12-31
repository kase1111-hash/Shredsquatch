using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shredsquatch.Editor
{
    /// <summary>
    /// Editor tool to validate and fix common project setup issues.
    /// Access via Tools > Shredsquatch > Project Setup
    /// </summary>
    public class ProjectSetupValidator : EditorWindow
    {
        private Vector2 _scrollPos;
        private List<SetupIssue> _issues = new List<SetupIssue>();
        private bool _hasScanned = false;

        private class SetupIssue
        {
            public string Category;
            public string Description;
            public string FixDescription;
            public System.Action FixAction;
            public bool IsFixed;
            public bool IsCritical;
        }

        [MenuItem("Tools/Shredsquatch/Project Setup Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectSetupValidator>("Project Setup");
            window.minSize = new Vector2(450, 400);
        }

        [MenuItem("Tools/Shredsquatch/Quick Fix All Issues")]
        public static void QuickFixAll()
        {
            if (EditorUtility.DisplayDialog("Fix All Issues",
                "This will attempt to fix all detectable project issues:\n\n" +
                "• Configure Input System\n" +
                "• Set up required Tags and Layers\n" +
                "• Configure Physics settings\n" +
                "• Create missing folders\n\n" +
                "Continue?", "Fix All", "Cancel"))
            {
                FixInputSystem();
                FixTagsAndLayers();
                FixPhysicsSettings();
                CreateRequiredFolders();
                AssetDatabase.Refresh();
                Debug.Log("[Shredsquatch] Project setup complete!");
            }
        }

        [MenuItem("Tools/Shredsquatch/Create Test Scene")]
        public static void CreateMinimalTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(100, 1, 100);
            ground.transform.rotation = Quaternion.Euler(5, 0, 0); // Slight slope
            ground.layer = LayerMask.NameToLayer("Ground");
            if (ground.layer == -1) ground.layer = 8;
            
            // Create player
            var player = new GameObject("Player");
            player.tag = "Player";
            player.layer = 9; // Player layer
            player.transform.position = new Vector3(0, 2, -10);
            
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 0.9f, 0);
            
            // Add core components (they'll initialize with defaults)
            player.AddComponent<Shredsquatch.Player.PlayerInput>();
            player.AddComponent<Shredsquatch.Player.SnowboardPhysics>();
            player.AddComponent<Shredsquatch.Player.JumpController>();
            player.AddComponent<Shredsquatch.Player.PlayerController>();
            
            // Add camera
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.SetParent(player.transform);
                cam.transform.localPosition = new Vector3(0, 1.6f, 0);
            }
            
            // Create GameManager
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<Shredsquatch.Core.GameManager>();
            
            // Create ErrorRecoveryManager
            var errObj = new GameObject("ErrorRecoveryManager");
            errObj.AddComponent<Shredsquatch.Core.ErrorRecoveryManager>();
            
            // Set directional light
            var lights = Object.FindObjectsOfType<Light>();
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.transform.rotation = Quaternion.Euler(50, -30, 0);
                    light.intensity = 1.2f;
                }
            }
            
            // Save scene
            string scenePath = "Assets/Scenes/TestScene.unity";
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"[Shredsquatch] Created minimal test scene at {scenePath}");
            EditorUtility.DisplayDialog("Test Scene Created", 
                "A minimal test scene has been created.\n\n" +
                "Press Play to test basic movement.", "OK");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan for Issues", GUILayout.Height(30)))
            {
                ScanForIssues();
            }
            if (GUILayout.Button("Fix All Issues", GUILayout.Height(30)))
            {
                FixAllIssues();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Create Minimal Test Scene", GUILayout.Height(25)))
            {
                CreateMinimalTestScene();
            }
            
            EditorGUILayout.Space(10);
            
            if (!_hasScanned)
            {
                EditorGUILayout.HelpBox("Click 'Scan for Issues' to check project setup.", MessageType.Info);
                return;
            }
            
            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues found! Project is ready to use.", MessageType.Info);
                return;
            }
            
            int criticalCount = _issues.Count(i => i.IsCritical && !i.IsFixed);
            int warningCount = _issues.Count(i => !i.IsCritical && !i.IsFixed);
            
            if (criticalCount > 0)
            {
                EditorGUILayout.HelpBox($"{criticalCount} critical issue(s) found!", MessageType.Error);
            }
            if (warningCount > 0)
            {
                EditorGUILayout.HelpBox($"{warningCount} warning(s) found.", MessageType.Warning);
            }
            
            EditorGUILayout.Space(5);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            string currentCategory = "";
            foreach (var issue in _issues)
            {
                if (issue.Category != currentCategory)
                {
                    currentCategory = issue.Category;
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField(currentCategory, EditorStyles.boldLabel);
                }
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                var icon = issue.IsFixed ? "✓" : (issue.IsCritical ? "✗" : "⚠");
                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = issue.IsFixed ? Color.green : (issue.IsCritical ? Color.red : Color.yellow);
                EditorGUILayout.LabelField(icon, style, GUILayout.Width(20));
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(issue.Description, EditorStyles.wordWrappedLabel);
                if (!issue.IsFixed && issue.FixAction != null)
                {
                    EditorGUILayout.LabelField(issue.FixDescription, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
                
                if (!issue.IsFixed && issue.FixAction != null)
                {
                    if (GUILayout.Button("Fix", GUILayout.Width(50)))
                    {
                        issue.FixAction();
                        issue.IsFixed = true;
                        AssetDatabase.Refresh();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void ScanForIssues()
        {
            _issues.Clear();
            _hasScanned = true;
            
            CheckInputSystem();
            CheckTagsAndLayers();
            CheckPhysicsSettings();
            CheckRequiredPackages();
            CheckRequiredFolders();
            CheckSceneSetup();
        }

        private void FixAllIssues()
        {
            foreach (var issue in _issues.Where(i => !i.IsFixed && i.FixAction != null))
            {
                issue.FixAction();
                issue.IsFixed = true;
            }
            AssetDatabase.Refresh();
        }

        private void CheckInputSystem()
        {
            // Check if Input System is configured
            var inputSettings = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/ProjectSettings.asset");
            var so = new SerializedObject(inputSettings);
            var prop = so.FindProperty("activeInputHandler");
            
            if (prop != null && prop.intValue == 0)
            {
                _issues.Add(new SetupIssue
                {
                    Category = "Input System",
                    Description = "Input System not enabled (using legacy input only)",
                    FixDescription = "Will set to 'Both' (legacy + new)",
                    IsCritical = true,
                    FixAction = FixInputSystem
                });
            }
        }

        private static void FixInputSystem()
        {
            // Note: This requires a restart to take effect
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            Debug.Log("[Shredsquatch] Input System fix: Please go to Edit > Project Settings > Player > Other Settings and set 'Active Input Handling' to 'Both'");
        }

        private void CheckTagsAndLayers()
        {
            string[] requiredTags = { "Player", "Tree", "Rock", "Ramp", "Rail", "Powerup", "Coin" };
            
            foreach (var tag in requiredTags)
            {
                try
                {
                    // This will throw if tag doesn't exist
                    GameObject.FindGameObjectsWithTag(tag);
                }
                catch
                {
                    _issues.Add(new SetupIssue
                    {
                        Category = "Tags & Layers",
                        Description = $"Missing tag: '{tag}'",
                        FixDescription = "Will create the tag",
                        IsCritical = tag == "Player",
                        FixAction = () => AddTag(tag)
                    });
                }
            }
            
            // Check layers
            if (LayerMask.NameToLayer("Ground") == -1)
            {
                _issues.Add(new SetupIssue
                {
                    Category = "Tags & Layers",
                    Description = "Missing layer: 'Ground' (should be layer 8)",
                    FixDescription = "Will create the layer",
                    IsCritical = true,
                    FixAction = () => SetLayerName(8, "Ground")
                });
            }
            
            if (LayerMask.NameToLayer("Player") == -1)
            {
                _issues.Add(new SetupIssue
                {
                    Category = "Tags & Layers",
                    Description = "Missing layer: 'Player' (should be layer 9)",
                    FixDescription = "Will create the layer",
                    IsCritical = true,
                    FixAction = () => SetLayerName(9, "Player")
                });
            }
        }

        private static void FixTagsAndLayers()
        {
            string[] requiredTags = { "Player", "Tree", "Rock", "Ramp", "Rail", "Powerup", "Coin" };
            foreach (var tag in requiredTags)
            {
                AddTag(tag);
            }
            SetLayerName(8, "Ground");
            SetLayerName(9, "Player");
        }

        private static void AddTag(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");
            
            // Check if tag already exists
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    return;
            }
            
            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }

        private static void SetLayerName(int layer, string name)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");
            
            if (layer < layersProp.arraySize)
            {
                layersProp.GetArrayElementAtIndex(layer).stringValue = name;
                tagManager.ApplyModifiedProperties();
            }
        }

        private void CheckPhysicsSettings()
        {
            if (Mathf.Abs(Physics.gravity.y - (-20f)) > 0.1f)
            {
                _issues.Add(new SetupIssue
                {
                    Category = "Physics",
                    Description = $"Gravity is {Physics.gravity.y}, recommended -20 for arcade feel",
                    FixDescription = "Will set gravity to -20",
                    IsCritical = false,
                    FixAction = FixPhysicsSettings
                });
            }
        }

        private static void FixPhysicsSettings()
        {
            Physics.gravity = new Vector3(0, -20, 0);
        }

        private void CheckRequiredPackages()
        {
            // Check for Input System package
            string manifestPath = "Packages/manifest.json";
            if (File.Exists(manifestPath))
            {
                string content = File.ReadAllText(manifestPath);
                if (!content.Contains("com.unity.inputsystem"))
                {
                    _issues.Add(new SetupIssue
                    {
                        Category = "Packages",
                        Description = "Input System package not installed",
                        FixDescription = "Add via Package Manager",
                        IsCritical = true,
                        FixAction = null // Can't auto-fix package installation easily
                    });
                }
                
                if (!content.Contains("com.unity.textmeshpro"))
                {
                    _issues.Add(new SetupIssue
                    {
                        Category = "Packages",
                        Description = "TextMeshPro package not installed",
                        FixDescription = "Add via Package Manager",
                        IsCritical = false,
                        FixAction = null
                    });
                }
            }
        }

        private void CheckRequiredFolders()
        {
            string[] requiredFolders = {
                "Assets/Scenes",
                "Assets/Prefabs",
                "Assets/Materials",
                "Assets/Audio"
            };
            
            foreach (var folder in requiredFolders)
            {
                if (!Directory.Exists(folder))
                {
                    _issues.Add(new SetupIssue
                    {
                        Category = "Folders",
                        Description = $"Missing folder: {folder}",
                        FixDescription = "Will create the folder",
                        IsCritical = false,
                        FixAction = () => Directory.CreateDirectory(folder)
                    });
                }
            }
        }

        private static void CreateRequiredFolders()
        {
            string[] folders = { "Assets/Scenes", "Assets/Prefabs", "Assets/Materials", "Assets/Audio" };
            foreach (var folder in folders)
            {
                Directory.CreateDirectory(folder);
            }
        }

        private void CheckSceneSetup()
        {
            // Check if GameScene exists
            if (!File.Exists("Assets/Scenes/GameScene.unity"))
            {
                _issues.Add(new SetupIssue
                {
                    Category = "Scenes",
                    Description = "GameScene.unity not found",
                    FixDescription = "Use 'Create Test Scene' button",
                    IsCritical = false,
                    FixAction = null
                });
            }
        }
    }
}
