#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using Shredsquatch.Configuration;
using Shredsquatch.Core;
using Shredsquatch.Terrain;
using Shredsquatch.UI;

namespace Shredsquatch.Editor
{
    /// <summary>
    /// Editor utility for wiring prefabs to scenes and creating configuration assets.
    /// </summary>
    public class SceneWiringUtility : EditorWindow
    {
        private PrefabRegistry _prefabRegistry;
        private VisualAssetsConfig _visualAssets;
        private GameAudioConfig _audioConfig;

        private Vector2 _scrollPosition;

        [MenuItem("Shredsquatch/Scene Wiring Utility")]
        public static void ShowWindow()
        {
            GetWindow<SceneWiringUtility>("Scene Wiring");
        }

        private void OnEnable()
        {
            LoadConfigurations();
        }

        private void LoadConfigurations()
        {
            // Try to find existing configurations
            string[] prefabRegistryGuids = AssetDatabase.FindAssets("t:PrefabRegistry");
            if (prefabRegistryGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabRegistryGuids[0]);
                _prefabRegistry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(path);
            }

            string[] visualAssetsGuids = AssetDatabase.FindAssets("t:VisualAssetsConfig");
            if (visualAssetsGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(visualAssetsGuids[0]);
                _visualAssets = AssetDatabase.LoadAssetAtPath<VisualAssetsConfig>(path);
            }

            string[] audioConfigGuids = AssetDatabase.FindAssets("t:GameAudioConfig");
            if (audioConfigGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(audioConfigGuids[0]);
                _audioConfig = AssetDatabase.LoadAssetAtPath<GameAudioConfig>(path);
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("Scene Wiring Utility", EditorStyles.boldLabel);
            GUILayout.Space(10);

            DrawConfigurationSection();
            GUILayout.Space(10);

            DrawPrefabAutoAssignSection();
            GUILayout.Space(10);

            DrawSceneSetupSection();
            GUILayout.Space(10);

            DrawValidationSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigurationSection()
        {
            EditorGUILayout.LabelField("Configuration Assets", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _prefabRegistry = (PrefabRegistry)EditorGUILayout.ObjectField(
                "Prefab Registry", _prefabRegistry, typeof(PrefabRegistry), false);

            _visualAssets = (VisualAssetsConfig)EditorGUILayout.ObjectField(
                "Visual Assets", _visualAssets, typeof(VisualAssetsConfig), false);

            _audioConfig = (GameAudioConfig)EditorGUILayout.ObjectField(
                "Audio Config", _audioConfig, typeof(GameAudioConfig), false);

            if (EditorGUI.EndChangeCheck())
            {
                // Save references
            }

            EditorGUILayout.BeginHorizontal();

            if (_prefabRegistry == null && GUILayout.Button("Create Prefab Registry"))
            {
                CreatePrefabRegistry();
            }

            if (_visualAssets == null && GUILayout.Button("Create Visual Assets"))
            {
                CreateVisualAssetsConfig();
            }

            if (_audioConfig == null && GUILayout.Button("Create Audio Config"))
            {
                CreateAudioConfig();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPrefabAutoAssignSection()
        {
            EditorGUILayout.LabelField("Auto-Assign Prefabs", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Click to automatically find and assign prefabs from the Prefabs folder.",
                MessageType.Info);

            if (_prefabRegistry == null)
            {
                EditorGUILayout.HelpBox("Create or assign a Prefab Registry first.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Auto-Assign All Prefabs"))
            {
                AutoAssignPrefabs();
            }
        }

        private void DrawSceneSetupSection()
        {
            EditorGUILayout.LabelField("Scene Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Add SceneInitializer to Scene"))
            {
                AddSceneInitializer();
            }

            if (GUILayout.Button("Wire Existing Scene Objects"))
            {
                WireExistingSceneObjects();
            }

            if (GUILayout.Button("Create Complete Game Scene"))
            {
                CreateCompleteGameScene();
            }
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate All References"))
            {
                ValidateAllReferences();
            }
        }

        private void CreatePrefabRegistry()
        {
            EnsureDirectoryExists("Assets/Settings");

            _prefabRegistry = ScriptableObject.CreateInstance<PrefabRegistry>();
            AssetDatabase.CreateAsset(_prefabRegistry, "Assets/Settings/PrefabRegistry.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = _prefabRegistry;

            Debug.Log("Created PrefabRegistry at Assets/Settings/PrefabRegistry.asset");
        }

        private void CreateVisualAssetsConfig()
        {
            EnsureDirectoryExists("Assets/Settings");

            _visualAssets = ScriptableObject.CreateInstance<VisualAssetsConfig>();
            AssetDatabase.CreateAsset(_visualAssets, "Assets/Settings/VisualAssetsConfig.asset");
            AssetDatabase.SaveAssets();

            Debug.Log("Created VisualAssetsConfig at Assets/Settings/VisualAssetsConfig.asset");
        }

        private void CreateAudioConfig()
        {
            EnsureDirectoryExists("Assets/Settings");

            _audioConfig = ScriptableObject.CreateInstance<GameAudioConfig>();
            AssetDatabase.CreateAsset(_audioConfig, "Assets/Settings/GameAudioConfig.asset");
            AssetDatabase.SaveAssets();

            Debug.Log("Created GameAudioConfig at Assets/Settings/GameAudioConfig.asset");
        }

        private void AutoAssignPrefabs()
        {
            if (_prefabRegistry == null) return;

            Undo.RecordObject(_prefabRegistry, "Auto-Assign Prefabs");

            // Player
            _prefabRegistry.PlayerPrefab = FindPrefab("Assets/Prefabs/Player", "Player");

            // Sasquatch
            _prefabRegistry.SasquatchPrefab = FindPrefab("Assets/Prefabs/Sasquatch", "Sasquatch");

            // Terrain
            _prefabRegistry.TerrainChunkPrefab = FindPrefab("Assets/Prefabs/Terrain", "TerrainChunk");

            // Trees
            _prefabRegistry.PineTrees = FindPrefabsContaining("Assets/Prefabs/Obstacles", "Pine");
            _prefabRegistry.BirchTrees = FindPrefabsContaining("Assets/Prefabs/Obstacles", "Birch");
            _prefabRegistry.DeadTrees = FindPrefabsContaining("Assets/Prefabs/Obstacles", "Dead");
            _prefabRegistry.FallenTrees = FindPrefabsContaining("Assets/Prefabs/Obstacles", "Fallen");

            // Rocks
            _prefabRegistry.LargeRocks = FindPrefabsContaining("Assets/Prefabs/Obstacles", "Rock");

            // Ramps
            _prefabRegistry.SmallRamp = FindPrefab("Assets/Prefabs/Ramps", "Small");
            _prefabRegistry.MediumRamp = FindPrefab("Assets/Prefabs/Ramps", "Medium") ??
                                         FindPrefab("Assets/Prefabs/Obstacles", "Ramp_Medium");
            _prefabRegistry.LargeRamp = FindPrefab("Assets/Prefabs/Ramps", "Large");
            _prefabRegistry.CliffRamp = FindPrefab("Assets/Prefabs/Ramps", "Cliff");

            // Rails
            _prefabRegistry.FenceRail = FindPrefab("Assets/Prefabs/Rails", "Fence");
            _prefabRegistry.PipeRail = FindPrefab("Assets/Prefabs/Rails", "Pipe");
            _prefabRegistry.MetalRail = FindPrefab("Assets/Prefabs/Rails", "Metal");

            // Collectibles
            _prefabRegistry.CoinPrefab = FindPrefab("Assets/Prefabs/Collectibles", "Coin");

            // Powerups
            _prefabRegistry.NitroPowerup = FindPrefab("Assets/Prefabs/Powerups", "Nitro");
            _prefabRegistry.GoldenBoardPowerup = FindPrefab("Assets/Prefabs/Powerups", "Golden");
            _prefabRegistry.RepellentPowerup = FindPrefab("Assets/Prefabs/Powerups", "Repellent");

            // Environment
            _prefabRegistry.ChairliftTower = FindPrefab("Assets/Prefabs/Environment", "Tower");
            _prefabRegistry.ChairliftChair = FindPrefab("Assets/Prefabs/Environment", "Chair");

            EditorUtility.SetDirty(_prefabRegistry);
            AssetDatabase.SaveAssets();

            Debug.Log("Auto-assigned prefabs to PrefabRegistry");
            EditorUtility.DisplayDialog("Auto-Assign Complete",
                "Prefabs have been automatically assigned. Check the PrefabRegistry for results.",
                "OK");
        }

        private GameObject FindPrefab(string folder, string nameContains)
        {
            if (!Directory.Exists(folder)) return null;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Contains(nameContains))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            return null;
        }

        private GameObject[] FindPrefabsContaining(string folder, string nameContains)
        {
            if (!Directory.Exists(folder)) return new GameObject[0];

            var results = new System.Collections.Generic.List<GameObject>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Contains(nameContains))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        results.Add(prefab);
                }
            }

            return results.ToArray();
        }

        private void AddSceneInitializer()
        {
            // Check if one already exists
            SceneInitializer existing = FindObjectOfType<SceneInitializer>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Already Exists",
                    "A SceneInitializer already exists in the scene.",
                    "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create new
            GameObject go = new GameObject("SceneInitializer");
            SceneInitializer init = go.AddComponent<SceneInitializer>();

            // Try to assign configurations
            if (_prefabRegistry != null)
            {
                var field = typeof(SceneInitializer).GetField("_prefabRegistry",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(init, _prefabRegistry);
            }

            if (_visualAssets != null)
            {
                var field = typeof(SceneInitializer).GetField("_visualAssets",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(init, _visualAssets);
            }

            if (_audioConfig != null)
            {
                var field = typeof(SceneInitializer).GetField("_audioConfig",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(init, _audioConfig);
            }

            // Try to find and assign scene references
            TerrainGenerator terrainGen = FindObjectOfType<TerrainGenerator>();
            if (terrainGen != null)
            {
                var field = typeof(SceneInitializer).GetField("_terrainGenerator",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(init, terrainGen);
            }

            HUDController hud = FindObjectOfType<HUDController>();
            if (hud != null)
            {
                var field = typeof(SceneInitializer).GetField("_hudController",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(init, hud);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create SceneInitializer");
            Selection.activeGameObject = go;

            Debug.Log("Created SceneInitializer and wired references");
        }

        private void WireExistingSceneObjects()
        {
            // Wire TerrainGenerator prefabs
            TerrainGenerator terrainGen = FindObjectOfType<TerrainGenerator>();
            if (terrainGen != null && _prefabRegistry != null)
            {
                Undo.RecordObject(terrainGen, "Wire TerrainGenerator");

                var treesField = typeof(TerrainGenerator).GetField("_treePrefabs",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (treesField != null) treesField.SetValue(terrainGen, _prefabRegistry.GetAllTrees());

                var rocksField = typeof(TerrainGenerator).GetField("_rockPrefabs",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (rocksField != null) rocksField.SetValue(terrainGen, _prefabRegistry.GetAllRocks());

                var rampsField = typeof(TerrainGenerator).GetField("_rampPrefabs",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (rampsField != null) rampsField.SetValue(terrainGen, _prefabRegistry.GetAllRamps());

                EditorUtility.SetDirty(terrainGen);
            }

            Debug.Log("Wired existing scene objects");
            EditorUtility.DisplayDialog("Wiring Complete",
                "Scene objects have been wired with prefab references.",
                "OK");
        }

        private void CreateCompleteGameScene()
        {
            if (EditorUtility.DisplayDialog("Create Complete Scene",
                "This will add missing game objects to the current scene. Continue?",
                "Yes", "Cancel"))
            {
                CreateMissingGameObjects();
            }
        }

        private void CreateMissingGameObjects()
        {
            // GameManager
            if (FindObjectOfType<GameManager>() == null)
            {
                GameObject go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
            }

            // TerrainGenerator
            if (FindObjectOfType<TerrainGenerator>() == null)
            {
                GameObject go = new GameObject("TerrainGenerator");
                go.AddComponent<TerrainGenerator>();
                Undo.RegisterCreatedObjectUndo(go, "Create TerrainGenerator");
            }

            // SceneInitializer
            if (FindObjectOfType<SceneInitializer>() == null)
            {
                AddSceneInitializer();
            }

            // ShaderManager
            if (FindObjectOfType<Shredsquatch.Rendering.ShaderManager>() == null)
            {
                GameObject go = new GameObject("ShaderManager");
                go.AddComponent<Shredsquatch.Rendering.ShaderManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create ShaderManager");
            }

            Debug.Log("Created missing game objects");
        }

        private void ValidateAllReferences()
        {
            int issues = 0;

            // Validate PrefabRegistry
            if (_prefabRegistry != null)
            {
                if (_prefabRegistry.Validate(out string[] missing))
                {
                    Debug.Log("PrefabRegistry: All required prefabs assigned");
                }
                else
                {
                    Debug.LogWarning($"PrefabRegistry: Missing {missing.Length} prefabs: {string.Join(", ", missing)}");
                    issues += missing.Length;
                }
            }
            else
            {
                Debug.LogWarning("No PrefabRegistry assigned");
                issues++;
            }

            // Validate VisualAssets
            if (_visualAssets != null)
            {
                if (_visualAssets.ValidateMinimumAssets(out string[] missing))
                {
                    Debug.Log("VisualAssetsConfig: All minimum assets assigned");
                }
                else
                {
                    Debug.LogWarning($"VisualAssetsConfig: Missing {missing.Length} assets");
                    issues += missing.Length;
                }
            }

            // Validate AudioConfig
            if (_audioConfig != null)
            {
                if (_audioConfig.ValidateConfig(out string[] missing))
                {
                    Debug.Log("GameAudioConfig: All required clips assigned");
                }
                else
                {
                    Debug.LogWarning($"GameAudioConfig: Missing {missing.Length} clips");
                    issues += missing.Length;
                }
            }

            // Summary
            if (issues == 0)
            {
                EditorUtility.DisplayDialog("Validation Passed",
                    "All references are properly assigned!",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Issues",
                    $"Found {issues} missing references. Check the Console for details.",
                    "OK");
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
#endif
