using UnityEngine;
using Shredsquatch.Configuration;
using Shredsquatch.Terrain;
using Shredsquatch.UI;
using Shredsquatch.Rendering;
using Shredsquatch.Progression;
using Shredsquatch.Sasquatch;

namespace Shredsquatch.Core
{
    /// <summary>
    /// Initializes the game scene by wiring up all system references.
    /// Place this on a GameObject in your scene and assign the configurations.
    /// </summary>
    public class SceneInitializer : MonoBehaviour
    {
        [Header("Configuration Assets")]
        [SerializeField] private PrefabRegistry _prefabRegistry;
        [SerializeField] private VisualAssetsConfig _visualAssets;
        [SerializeField] private GameAudioConfig _audioConfig;

        [Header("Scene References")]
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private TerrainGenerator _terrainGenerator;
        [SerializeField] private HUDController _hudController;

        [Header("Options")]
        [SerializeField] private bool _spawnPlayerOnStart = true;
        [SerializeField] private bool _initializeSasquatch = true;
        [SerializeField] private float _sasquatchSpawnDelay = 30f;

        // Runtime references
        private GameObject _playerInstance;
        private GameObject _sasquatchInstance;

        private void Awake()
        {
            ValidateReferences();
            InitializeSingletons();
        }

        private void Start()
        {
            WireTerrainGenerator();

            if (_spawnPlayerOnStart)
            {
                SpawnPlayer();
            }

            if (_initializeSasquatch)
            {
                Invoke(nameof(SpawnSasquatch), _sasquatchSpawnDelay);
            }

            WireHUD();
            WireManagers();

            Debug.Log("[SceneInitializer] Scene initialization complete");
        }

        private void ValidateReferences()
        {
            if (_prefabRegistry == null)
            {
                Debug.LogError("[SceneInitializer] PrefabRegistry not assigned!");
            }

            if (_terrainGenerator == null)
            {
                _terrainGenerator = FindObjectOfType<TerrainGenerator>();
            }

            if (_hudController == null)
            {
                _hudController = FindObjectOfType<HUDController>();
            }
        }

        private void InitializeSingletons()
        {
            // Ensure PrefabRegistry is accessible as singleton
            if (_prefabRegistry != null)
            {
                // The OnEnable of PrefabRegistry sets the Instance
            }
        }

        private void WireTerrainGenerator()
        {
            if (_terrainGenerator == null) return;

            // Set prefabs from registry
            if (_prefabRegistry != null)
            {
                _terrainGenerator.SetPrefabsFromRegistry(_prefabRegistry);
            }

            Debug.Log("[SceneInitializer] TerrainGenerator wired with prefabs");
        }

        private void SpawnPlayer()
        {
            if (_prefabRegistry == null || _prefabRegistry.PlayerPrefab == null)
            {
                Debug.LogWarning("[SceneInitializer] Cannot spawn player - prefab not assigned");
                return;
            }

            Vector3 spawnPos = _playerSpawnPoint != null
                ? _playerSpawnPoint.position
                : new Vector3(0, 5, 0);

            Quaternion spawnRot = _playerSpawnPoint != null
                ? _playerSpawnPoint.rotation
                : Quaternion.identity;

            _playerInstance = Instantiate(_prefabRegistry.PlayerPrefab, spawnPos, spawnRot);
            _playerInstance.name = "Player";

            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayer(_playerInstance.transform);
            }

            // Wire to terrain generator
            if (_terrainGenerator != null)
            {
                _terrainGenerator.SetPlayer(_playerInstance.transform);
            }

            Debug.Log("[SceneInitializer] Player spawned at " + spawnPos);
        }

        private void SpawnSasquatch()
        {
            if (_prefabRegistry == null || _prefabRegistry.SasquatchPrefab == null)
            {
                Debug.LogWarning("[SceneInitializer] Cannot spawn Sasquatch - prefab not assigned");
                return;
            }

            if (_playerInstance == null)
            {
                Debug.LogWarning("[SceneInitializer] Cannot spawn Sasquatch - no player");
                return;
            }

            // Spawn behind and above player
            Vector3 spawnPos = _playerInstance.transform.position + new Vector3(0, 10, -50);
            _sasquatchInstance = Instantiate(_prefabRegistry.SasquatchPrefab, spawnPos, Quaternion.identity);
            _sasquatchInstance.name = "Sasquatch";

            // Wire Sasquatch to target player
            var sasquatchAI = _sasquatchInstance.GetComponent<SasquatchAI>();
            if (sasquatchAI != null)
            {
                sasquatchAI.SetPlayerReference(_playerInstance.transform);
            }

            // Wire HUD to Sasquatch
            if (_hudController != null)
            {
                _hudController.SetSasquatch(sasquatchAI);
            }

            Debug.Log("[SceneInitializer] Sasquatch spawned");
        }

        private void WireHUD()
        {
            if (_hudController == null || _playerInstance == null) return;

            var playerController = _playerInstance.GetComponent<Player.PlayerController>();
            var trickController = _playerInstance.GetComponent<Tricks.TrickController>();

            if (playerController != null)
            {
                _hudController.SetPlayer(playerController);
            }

            if (trickController != null)
            {
                _hudController.SetTrickController(trickController);
            }

            Debug.Log("[SceneInitializer] HUD wired to player");
        }

        private void WireManagers()
        {
            // Wire AchievementManager
            var achievementManager = FindObjectOfType<AchievementManager>();
            if (achievementManager != null && _playerInstance != null)
            {
                var railController = _playerInstance.GetComponent<Tricks.RailGrindController>();
                if (railController != null)
                {
                    achievementManager.SetRailController(railController);
                }
            }

            // Wire ShaderManager
            var shaderManager = FindObjectOfType<ShaderManager>();
            if (shaderManager != null)
            {
                // ShaderManager is self-contained, just ensure it exists
            }

            Debug.Log("[SceneInitializer] Managers wired");
        }

        /// <summary>
        /// Get the current player instance.
        /// </summary>
        public GameObject GetPlayer()
        {
            return _playerInstance;
        }

        /// <summary>
        /// Get the current Sasquatch instance.
        /// </summary>
        public GameObject GetSasquatch()
        {
            return _sasquatchInstance;
        }

        /// <summary>
        /// Manually trigger Sasquatch spawn (for game mode changes).
        /// </summary>
        public void TriggerSasquatchSpawn()
        {
            if (_sasquatchInstance == null)
            {
                SpawnSasquatch();
            }
        }
    }
}
