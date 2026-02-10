using UnityEngine;
using System.Collections.Generic;
using Shredsquatch.Core;

namespace Shredsquatch.Terrain
{
    public class TerrainGenerator : MonoBehaviour, IRecoverable
    {
        [Header("Chunk Settings")]
        [SerializeField] private float _chunkSize = 256f;
        [SerializeField] private int _chunkResolution = 129;
        [SerializeField] private float _loadDistance = 2000f;
        [SerializeField] private float _unloadDistance = 2500f;

        [Header("Height Settings")]
        [SerializeField] private float _heightMultiplier = 100f;
        [SerializeField] private AnimationCurve _heightCurve;

        [Header("Noise Settings")]
        [SerializeField] private int _seed = 42;
        [SerializeField] private float _noiseScale = 100f;
        [SerializeField] private int _octaves = 4;
        [SerializeField] private float _persistence = 0.5f;
        [SerializeField] private float _lacunarity = 2f;
        [SerializeField] private float _slopeBias = 0.3f;

        [Header("Prefabs")]
        [SerializeField] private GameObject _chunkPrefab;
        [SerializeField] private Material _terrainMaterial;

        [Header("Obstacle Prefabs")]
        [SerializeField] private GameObject[] _treePrefabs;
        [SerializeField] private GameObject[] _rockPrefabs;
        [SerializeField] private GameObject[] _rampPrefabs;

        [Header("References")]
        [SerializeField] private Transform _player;

        // Chunk management
        private Dictionary<Vector2Int, TerrainChunk> _chunks = new Dictionary<Vector2Int, TerrainChunk>();
        private Queue<Vector2Int> _chunksToGenerate = new Queue<Vector2Int>();
        private HashSet<Vector2Int> _queuedChunks = new HashSet<Vector2Int>(); // For O(1) lookup
        private List<Vector2Int> _activeChunks = new List<Vector2Int>();

        // Seeded random for deterministic generation
        private System.Random _seededRandom;

        // Object pooling
        private Transform _chunkContainer;

        private void Start()
        {
            _chunkContainer = new GameObject("TerrainChunks").transform;

            // Use daily seed for leaderboards, or custom seed
            if (_seed == 0)
            {
                _seed = System.DateTime.Now.DayOfYear + System.DateTime.Now.Year * 1000;
            }

            // Initialize seeded random for deterministic procedural generation
            _seededRandom = new System.Random(_seed);

            // Register with error recovery system
            if (ErrorRecoveryManager.Instance != null)
            {
                ErrorRecoveryManager.Instance.RegisterRecoverable(this);
            }

            // Initial chunk generation around player
            SafeExecution.Try(UpdateChunks, "TerrainGenerator.InitialUpdate");
        }

        private void OnDestroy()
        {
            if (ErrorRecoveryManager.Instance != null)
            {
                ErrorRecoveryManager.Instance.UnregisterRecoverable(this);
            }
        }

        private void Update()
        {
            if (GameManager.Instance?.CurrentState == GameState.Playing)
            {
                UpdateChunks();
                ProcessChunkQueue();
            }
        }

        private void UpdateChunks()
        {
            if (_player == null) return;

            Vector2Int playerChunk = GetChunkCoord(_player.position);

            // Calculate visible range in chunks
            int chunkRange = Mathf.CeilToInt(_loadDistance / _chunkSize);

            // Find chunks to load
            for (int x = -chunkRange; x <= chunkRange; x++)
            {
                for (int z = -chunkRange; z <= chunkRange; z++)
                {
                    Vector2Int coord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);

                    // Only generate chunks ahead and around (not too far behind)
                    if (z < -2) continue; // Don't generate far behind player

                    if (!_chunks.ContainsKey(coord))
                    {
                        QueueChunk(coord);
                    }
                    else
                    {
                        _chunks[coord].SetActive(true);
                    }
                }
            }

            // Unload far chunks
            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (var kvp in _chunks)
            {
                Vector3 chunkCenter = GetChunkWorldPosition(kvp.Key);
                float distance = Vector3.Distance(_player.position, chunkCenter);

                if (distance > _unloadDistance)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var coord in toRemove)
            {
                UnloadChunk(coord);
            }
        }

        private void QueueChunk(Vector2Int coord)
        {
            // Use HashSet for O(1) lookup instead of O(n) Queue.Contains
            if (_queuedChunks.Add(coord))
            {
                _chunksToGenerate.Enqueue(coord);
            }
        }

        private void ProcessChunkQueue()
        {
            // Generate 1-2 chunks per frame to avoid hitching
            int chunksPerFrame = 2;

            while (_chunksToGenerate.Count > 0 && chunksPerFrame > 0)
            {
                Vector2Int coord = _chunksToGenerate.Dequeue();
                _queuedChunks.Remove(coord); // Keep HashSet in sync

                if (!_chunks.ContainsKey(coord))
                {
                    GenerateChunk(coord);
                    chunksPerFrame--;
                }
            }
        }

        private void GenerateChunk(Vector2Int coord)
        {
            // Instantiate chunk
            GameObject chunkObj;
            if (_chunkPrefab != null)
            {
                chunkObj = Instantiate(_chunkPrefab, _chunkContainer);
            }
            else
            {
                chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}");
                chunkObj.transform.parent = _chunkContainer;
                chunkObj.AddComponent<MeshFilter>();
                chunkObj.AddComponent<MeshRenderer>();
                chunkObj.AddComponent<MeshCollider>();
            }

            TerrainChunk chunk = chunkObj.GetComponent<TerrainChunk>();
            if (chunk == null)
            {
                chunk = chunkObj.AddComponent<TerrainChunk>();
            }

            chunk.Initialize(coord, _chunkSize, _terrainMaterial);

            // Position chunk
            Vector3 worldPos = GetChunkWorldPosition(coord);
            chunkObj.transform.position = worldPos;

            // Generate heightmap with downhill slope bias
            Vector2 noiseOffset = new Vector2(coord.x * _chunkSize, coord.y * _chunkSize);
            float[,] heightMap = GenerateHeightMap(coord, noiseOffset);

            chunk.GenerateMesh(heightMap, _heightMultiplier, _heightCurve);

            // Spawn obstacles based on distance/zone
            SpawnObstacles(chunk, coord, heightMap);

            _chunks[coord] = chunk;
            _activeChunks.Add(coord);
        }

        private float[,] GenerateHeightMap(Vector2Int coord, Vector2 offset)
        {
            float[,] heightMap = NoiseGenerator.GenerateNoiseMap(
                _chunkResolution,
                _chunkResolution,
                _seed,
                _noiseScale,
                _octaves,
                _persistence,
                _lacunarity,
                offset
            );

            // Apply downhill slope bias
            for (int y = 0; y < _chunkResolution; y++)
            {
                for (int x = 0; x < _chunkResolution; x++)
                {
                    // Slope increases with Z (further down mountain)
                    float globalZ = coord.y * _chunkSize + y * (_chunkSize / _chunkResolution);
                    float slopeFactor = globalZ * _slopeBias * 0.001f;

                    heightMap[x, y] -= slopeFactor;
                    heightMap[x, y] = Mathf.Max(0, heightMap[x, y]);
                }
            }

            return heightMap;
        }

        private void SpawnObstacles(TerrainChunk chunk, Vector2Int coord, float[,] heightMap)
        {
            float distance = coord.y * _chunkSize / 1000f; // Approximate km

            // Determine zone
            TerrainZone zone = GetZone(distance);

            // Tree density based on zone
            float treeDensity = GetTreeDensity(zone);
            SpawnTrees(chunk, heightMap, treeDensity, zone);

            // Rocks
            float rockDensity = GetRockDensity(zone);
            SpawnRocks(chunk, heightMap, rockDensity);

            // Ramps (less frequent)
            SpawnRamps(chunk, heightMap, zone);
        }

        // Helper methods for seeded random
        private float SeededRandomRange(float min, float max)
        {
            return (float)(_seededRandom.NextDouble() * (max - min) + min);
        }

        private int SeededRandomRange(int min, int maxExclusive)
        {
            return _seededRandom.Next(min, maxExclusive);
        }

        private void SpawnTrees(TerrainChunk chunk, float[,] heightMap, float density, TerrainZone zone)
        {
            if (_treePrefabs == null || _treePrefabs.Length == 0) return;

            // Poisson disk sampling approximation
            int treeCount = Mathf.RoundToInt(density * _chunkSize * _chunkSize * 0.0001f);

            for (int i = 0; i < treeCount; i++)
            {
                float x = SeededRandomRange(0f, _chunkSize);
                float z = SeededRandomRange(0f, _chunkSize);

                // Sample height at this position
                int mapX = Mathf.Clamp(Mathf.RoundToInt(x / _chunkSize * _chunkResolution), 0, _chunkResolution - 1);
                int mapZ = Mathf.Clamp(Mathf.RoundToInt(z / _chunkSize * _chunkResolution), 0, _chunkResolution - 1);
                float height = heightMap[mapX, mapZ] * _heightMultiplier;

                // Cluster trees using noise
                float clusterNoise = Mathf.PerlinNoise(x * 0.05f + _seed, z * 0.05f + _seed);
                if (clusterNoise < 0.4f) continue; // Skip for sparse areas

                Vector3 localPos = new Vector3(x - _chunkSize / 2, height, z - _chunkSize / 2);
                Quaternion rotation = Quaternion.Euler(0, SeededRandomRange(0, 360), 0);
                float scale = SeededRandomRange(0.8f, 1.5f);

                GameObject prefab = _treePrefabs[SeededRandomRange(0, _treePrefabs.Length)];
                chunk.SpawnObject(prefab, localPos, rotation, Vector3.one * scale);
            }
        }

        private void SpawnRocks(TerrainChunk chunk, float[,] heightMap, float density)
        {
            if (_rockPrefabs == null || _rockPrefabs.Length == 0) return;

            int rockCount = Mathf.RoundToInt(density * _chunkSize * _chunkSize * 0.00002f);

            for (int i = 0; i < rockCount; i++)
            {
                float x = SeededRandomRange(0f, _chunkSize);
                float z = SeededRandomRange(0f, _chunkSize);

                int mapX = Mathf.Clamp(Mathf.RoundToInt(x / _chunkSize * _chunkResolution), 0, _chunkResolution - 1);
                int mapZ = Mathf.Clamp(Mathf.RoundToInt(z / _chunkSize * _chunkResolution), 0, _chunkResolution - 1);
                float height = heightMap[mapX, mapZ] * _heightMultiplier;

                Vector3 localPos = new Vector3(x - _chunkSize / 2, height, z - _chunkSize / 2);
                Quaternion rotation = Quaternion.Euler(
                    SeededRandomRange(-10, 10),
                    SeededRandomRange(0, 360),
                    SeededRandomRange(-10, 10)
                );
                float scale = SeededRandomRange(0.5f, 2f);

                GameObject prefab = _rockPrefabs[SeededRandomRange(0, _rockPrefabs.Length)];
                chunk.SpawnObject(prefab, localPos, rotation, Vector3.one * scale);
            }
        }

        private void SpawnRamps(TerrainChunk chunk, float[,] heightMap, TerrainZone zone)
        {
            if (_rampPrefabs == null || _rampPrefabs.Length == 0) return;

            // Ramps are sparse
            int rampCount = zone switch
            {
                TerrainZone.Tutorial => 1,
                TerrainZone.Forest => 2,
                TerrainZone.Extreme => 3,
                _ => 1
            };

            for (int i = 0; i < rampCount; i++)
            {
                if (_seededRandom.NextDouble() > 0.3) continue; // 30% chance per potential ramp

                float x = SeededRandomRange(_chunkSize * 0.2f, _chunkSize * 0.8f);
                float z = SeededRandomRange(_chunkSize * 0.2f, _chunkSize * 0.8f);

                int mapX = Mathf.Clamp(Mathf.RoundToInt(x / _chunkSize * _chunkResolution), 0, _chunkResolution - 1);
                int mapZ = Mathf.Clamp(Mathf.RoundToInt(z / _chunkSize * _chunkResolution), 0, _chunkResolution - 1);
                float height = heightMap[mapX, mapZ] * _heightMultiplier;

                Vector3 localPos = new Vector3(x - _chunkSize / 2, height, z - _chunkSize / 2);
                // Ramps face downhill (positive Z)
                Quaternion rotation = Quaternion.Euler(0, SeededRandomRange(-20, 20), 0);

                GameObject prefab = _rampPrefabs[SeededRandomRange(0, _rampPrefabs.Length)];
                chunk.SpawnObject(prefab, localPos, rotation, Vector3.one);
            }
        }

        private void UnloadChunk(Vector2Int coord)
        {
            if (_chunks.TryGetValue(coord, out TerrainChunk chunk))
            {
                chunk.Clear();
                Destroy(chunk.gameObject);
                _chunks.Remove(coord);
                _activeChunks.Remove(coord);
            }
        }

        private Vector2Int GetChunkCoord(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / _chunkSize),
                Mathf.FloorToInt(position.z / _chunkSize)
            );
        }

        private Vector3 GetChunkWorldPosition(Vector2Int coord)
        {
            return new Vector3(
                coord.x * _chunkSize,
                0,
                coord.y * _chunkSize
            );
        }

        private enum TerrainZone
        {
            Tutorial,  // 0-2km
            Forest,    // 2-5km
            Extreme    // 5km+
        }

        private TerrainZone GetZone(float distanceKm)
        {
            if (distanceKm < 2f) return TerrainZone.Tutorial;
            if (distanceKm < 5f) return TerrainZone.Forest;
            return TerrainZone.Extreme;
        }

        private float GetTreeDensity(TerrainZone zone)
        {
            return zone switch
            {
                TerrainZone.Tutorial => 0.3f,
                TerrainZone.Forest => 1.0f,
                TerrainZone.Extreme => 0.6f,
                _ => 0.5f
            };
        }

        private float GetRockDensity(TerrainZone zone)
        {
            return zone switch
            {
                TerrainZone.Tutorial => 0.2f,
                TerrainZone.Forest => 0.5f,
                TerrainZone.Extreme => 1.0f,
                _ => 0.4f
            };
        }

        public void SetSeed(int seed)
        {
            _seed = seed;
            _seededRandom = new System.Random(_seed);
        }

        public void SetPlayerReference(Transform player)
        {
            _player = player;
        }

        /// <summary>
        /// Alias for SetPlayerReference for SceneInitializer compatibility.
        /// </summary>
        public void SetPlayer(Transform player)
        {
            SetPlayerReference(player);
        }

        /// <summary>
        /// Set prefab arrays from PrefabRegistry.
        /// </summary>
        public void SetPrefabsFromRegistry(Configuration.PrefabRegistry registry)
        {
            if (registry == null) return;

            _treePrefabs = registry.GetAllTrees();
            _rockPrefabs = registry.GetAllRocks();
            _rampPrefabs = registry.GetAllRamps();
        }

        /// <summary>
        /// IRecoverable implementation - clear bad chunks and reset generation state.
        /// </summary>
        public void AttemptRecovery()
        {
            // Clear the generation queue
            _chunksToGenerate.Clear();
            _queuedChunks.Clear();

            // Try to clear all chunks safely
            var chunksToRemove = new List<Vector2Int>(_chunks.Keys);
            foreach (var coord in chunksToRemove)
            {
                SafeExecution.Try(() => UnloadChunk(coord), "RecoveryUnloadChunk");
            }

            // Re-initialize seeded random
            _seededRandom = new System.Random(_seed);

            // Clear tracking lists
            _chunks.Clear();
            _activeChunks.Clear();

            Debug.Log("[TerrainGenerator] Recovery complete - terrain reset");
        }
    }
}
