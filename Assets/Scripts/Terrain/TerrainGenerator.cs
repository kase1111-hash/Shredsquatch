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
        [Tooltip("Vertical drop per metre travelled downhill (+Z). 0.15 is roughly an 8.5 degree average grade.")]
        [SerializeField] private float _slopeBias = 0.15f;

        [Header("Prefabs")]
        [SerializeField] private GameObject _chunkPrefab;
        [SerializeField] private Material _terrainMaterial;

        [Header("Obstacle Prefabs")]
        [SerializeField] private GameObject[] _treePrefabs;
        [SerializeField] private GameObject[] _rockPrefabs;
        [SerializeField] private GameObject[] _rampPrefabs;
        [SerializeField] private GameObject[] _railPrefabs;
        [SerializeField] private GameObject _coinPrefab;

        [Header("Terrain Features")]
        [SerializeField] private TerrainFeatureSettings _features = new TerrainFeatureSettings();
        [Tooltip("Also spawn the registry ramp prefabs. Off: they are broken (Medium blocks, Large gives no bonus, Cliff is a Rock crash wall) and park kickers replace them.")]
        [SerializeField] private bool _spawnLegacyRamps = false;

        [Header("References")]
        [SerializeField] private Transform _player;

        // Chunk management
        private Dictionary<Vector2Int, TerrainChunk> _chunks = new Dictionary<Vector2Int, TerrainChunk>();
        private Queue<Vector2Int> _chunksToGenerate = new Queue<Vector2Int>();
        private HashSet<Vector2Int> _queuedChunks = new HashSet<Vector2Int>(); // For O(1) lookup
        private readonly List<Vector2Int> _chunksToRemove = new List<Vector2Int>();

        // Seeded random for deterministic generation
        private System.Random _seededRandom;

        // Park features of the chunk being generated (chunk-local) and their shared materials
        private readonly List<ParkReservation> _reservations = new List<ParkReservation>();
        private ParkMaterials _parkMaterials;

        // TODO: Implement object pooling for terrain obstacles (trees, rocks, ramps, coins, rails) — currently uses Instantiate/Destroy
        private Transform _chunkContainer;
        private bool _initialized;

        private float VertexSpacing => _chunkSize / Mathf.Max(1, _chunkResolution - 1);

        private void Start()
        {
            EnsureInitialized();

            // Register with error recovery system
            if (ErrorRecoveryManager.Instance != null)
            {
                ErrorRecoveryManager.Instance.RegisterRecoverable(this);
            }

            // Initial chunk generation around player
            SafeExecution.Try(UpdateChunks, "TerrainGenerator.InitialUpdate");
        }

        /// <summary>
        /// Set up the container, material and seeded random. Safe to call more than once;
        /// GenerateInitialChunks may run from SceneInitializer before this component's Start.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _chunkContainer = new GameObject("TerrainChunks").transform;

            // Create default snow material if none assigned
            if (_terrainMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                _terrainMaterial = new Material(shader);
                _terrainMaterial.color = new Color(0.95f, 0.97f, 1f);
            }

            // Use daily seed for leaderboards, or custom seed
            if (_seed == 0)
            {
                _seed = System.DateTime.Now.DayOfYear + System.DateTime.Now.Year * 1000;
            }

            // Initialize seeded random for deterministic procedural generation
            _seededRandom = new System.Random(_seed);
        }

        private void OnDestroy()
        {
            if (ErrorRecoveryManager.Instance != null)
            {
                ErrorRecoveryManager.Instance.UnregisterRecoverable(this);
            }

            if (_chunkContainer != null)
            {
                Destroy(_chunkContainer.gameObject);
            }

            if (_parkMaterials != null)
            {
                DestroyMaterial(_parkMaterials.Kicker);
                DestroyMaterial(_parkMaterials.FunBox);
                DestroyMaterial(_parkMaterials.FlatBox);
                DestroyMaterial(_parkMaterials.DownBox);
                DestroyMaterial(_parkMaterials.Marker);
                _parkMaterials = null;
            }
        }

        private static void DestroyMaterial(Material material)
        {
            if (material != null)
            {
                Destroy(material);
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
            float loadRadius = LoadRadius;

            // Calculate visible range in chunks
            int chunkRange = Mathf.CeilToInt(loadRadius / _chunkSize);

            // Find chunks to load. Loading uses the same (horizontal, circular) distance test as
            // unloading, with UnloadRadius > LoadRadius. Loading a square while unloading a circle
            // made the corner chunks load and unload every frame, regenerating meshes nonstop.
            for (int x = -chunkRange; x <= chunkRange; x++)
            {
                for (int z = -chunkRange; z <= chunkRange; z++)
                {
                    Vector2Int coord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);

                    // Only generate chunks ahead and around (not too far behind)
                    if (z < -2) continue; // Don't generate far behind player

                    if (HorizontalDistanceToChunk(coord) > loadRadius) continue;

                    if (!_chunks.ContainsKey(coord))
                    {
                        QueueChunk(coord);
                    }
                }
            }

            // Unload far chunks
            float unloadRadius = UnloadRadius;
            _chunksToRemove.Clear();
            foreach (var kvp in _chunks)
            {
                if (HorizontalDistanceToChunk(kvp.Key) > unloadRadius)
                {
                    _chunksToRemove.Add(kvp.Key);
                }
            }

            foreach (var coord in _chunksToRemove)
            {
                UnloadChunk(coord);
            }
        }

        // Always reach past the chunk under the rider, whatever the inspector says
        private float LoadRadius => Mathf.Max(_loadDistance, _chunkSize);

        // Keep a full chunk of hysteresis so chunks on the boundary don't thrash
        private float UnloadRadius => Mathf.Max(_unloadDistance, LoadRadius + _chunkSize);

        private float HorizontalDistanceToChunk(Vector2Int coord)
        {
            Vector3 center = GetChunkWorldPosition(coord);
            float dx = center.x - _player.position.x;
            float dz = center.z - _player.position.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
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

                // The rider may have moved on since this chunk was queued
                if (_player != null && HorizontalDistanceToChunk(coord) > UnloadRadius) continue;

                if (!_chunks.ContainsKey(coord))
                {
                    GenerateChunk(coord);
                    chunksPerFrame--;
                }
            }
        }

        private void GenerateChunk(Vector2Int coord)
        {
            EnsureInitialized();

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

            // Generate heightmap with downhill slope bias. NoiseGenerator samples by vertex
            // index, so the offset must advance by (resolution - 1) vertices per chunk for
            // neighbouring chunks to line up seamlessly.
            Vector2 noiseOffset = new Vector2(coord.x, coord.y) * (_chunkResolution - 1);
            float[,] heightMap = GenerateHeightMap(coord, noiseOffset);

            // Height curve is already applied in GenerateHeightMap (before the slope bias)
            chunk.GenerateMesh(heightMap, _heightMultiplier, null);

            // Spawn obstacles based on distance/zone
            SpawnObstacles(chunk, coord, heightMap);

            _chunks[coord] = chunk;
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

            // Shape the noise, then subtract a constant downhill grade so the mountain keeps
            // descending toward +Z. Values are in normalized units (multiplied by _heightMultiplier
            // in the chunk), so the grade is divided by the multiplier here.
            float gradePerMetre = _heightMultiplier > 0f ? _slopeBias / _heightMultiplier : 0f;
            float vertexSpacing = VertexSpacing;
            bool useCurve = _heightCurve != null && _heightCurve.length > 0;

            // Terrain features (metres) are pure functions of world XZ. Their positions come from
            // integer global vertex indices, so the two chunks sharing an edge evaluate bit-identical
            // coordinates (and heights) there. Same world positions as globalZ and the mesh vertices.
            bool useFeatures = _features != null && _features.Enabled && _heightMultiplier > 0f;
            int span = Mathf.Max(1, _chunkResolution - 1);
            double spacing = (double)_chunkSize / span;
            double half = _chunkSize * 0.5;
            int baseX = coord.x * span;
            int baseZ = coord.y * span;

            for (int y = 0; y < _chunkResolution; y++)
            {
                // Row y sits at chunk-local Z = -chunkSize/2 + y * spacing
                float globalZ = coord.y * _chunkSize - _chunkSize / 2f + y * vertexSpacing;
                double wz = (baseZ + y) * spacing - half;

                for (int x = 0; x < _chunkResolution; x++)
                {
                    float shaped = useCurve ? _heightCurve.Evaluate(heightMap[x, y]) : heightMap[x, y];
                    float feature = useFeatures
                        ? (float)(TerrainFeatures.HeightOffset(_seed, _features, (baseX + x) * spacing - half, wz) / _heightMultiplier)
                        : 0f;
                    heightMap[x, y] = shaped - globalZ * gradePerMetre + feature;
                }
            }

            return heightMap;
        }

        private void SpawnObstacles(TerrainChunk chunk, Vector2Int coord, float[,] heightMap)
        {
            _reservations.Clear();

            float distance = coord.y * _chunkSize / 1000f; // Approximate km

            // Determine zone
            TerrainZone zone = GetZone(distance);

            // Park features first, so the scattered obstacles below can keep clear of them
            SpawnParkFeatures(chunk, zone);

            // Tree density based on zone
            float treeDensity = GetTreeDensity(zone);
            SpawnTrees(chunk, heightMap, treeDensity, zone);

            // Rocks
            float rockDensity = GetRockDensity(zone);
            SpawnRocks(chunk, heightMap, rockDensity);

            // Ramps (less frequent)
            SpawnRamps(chunk, heightMap, zone);

            // Coin lines along downhill paths
            SpawnCoins(chunk, heightMap, zone);

            // Rails (sparse, increase with zone)
            SpawnRails(chunk, heightMap, zone);
        }

        private void SpawnParkFeatures(TerrainChunk chunk, TerrainZone zone)
        {
            if (_features == null || !_features.Enabled) return;

            // Plain try/catch rather than SafeExecution: repeated SafeExecution errors trip
            // ErrorRecoveryManager into AttemptRecovery, which rebuilds every chunk.
            try
            {
                ParkFeatureBuilder.SpawnChunkFeatures(chunk, _seed, _features, (int)zone, GetParkMaterials(), _reservations);
            }
            catch (System.Exception e)
            {
                // A throw here would orphan the chunk and regenerate it every frame
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// True where scattered obstacles must not spawn: this chunk's park features (bodies and
        /// landings) and the terrain features' reserved areas (chute floors, drop run-ups and landings).
        /// </summary>
        private bool IsReserved(TerrainChunk chunk, float localX, float localZ)
        {
            for (int i = 0; i < _reservations.Count; i++)
            {
                if (_reservations[i].Contains(localX, localZ)) return true;
            }

            return _features != null && TerrainFeatures.IsReserved(
                _seed,
                _features,
                chunk.ChunkCoord.x * (double)_chunkSize + localX,
                chunk.ChunkCoord.y * (double)_chunkSize + localZ);
        }

        /// <summary>Park materials are tinted copies of the terrain material, created once.</summary>
        private ParkMaterials GetParkMaterials()
        {
            if (_parkMaterials == null)
            {
                _parkMaterials = new ParkMaterials
                {
                    Kicker = MakeParkMaterial(0.80f, 0.88f, 0.97f),
                    FunBox = MakeParkMaterial(0.18f, 0.66f, 1f),
                    FlatBox = MakeParkMaterial(1f, 0.54f, 0.12f),
                    DownBox = MakeParkMaterial(1f, 0.82f, 0.12f),
                    Marker = MakeParkMaterial(1f, 0.35f, 0.05f)
                };
            }
            return _parkMaterials;
        }

        private Material MakeParkMaterial(float r, float g, float b)
        {
            return _terrainMaterial != null ? new Material(_terrainMaterial) { color = new Color(r, g, b) } : null;
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
                float height = chunk.SampleHeight(x - _chunkSize / 2, z - _chunkSize / 2);

                // Cluster trees using noise
                float clusterNoise = Mathf.PerlinNoise(x * 0.05f + _seed, z * 0.05f + _seed);
                if (clusterNoise < 0.4f) continue; // Skip for sparse areas

                Vector3 localPos = new Vector3(x - _chunkSize / 2, height, z - _chunkSize / 2);
                Quaternion rotation = Quaternion.Euler(0, SeededRandomRange(0, 360), 0);
                float scale = SeededRandomRange(0.8f, 1.5f);

                GameObject prefab = _treePrefabs[SeededRandomRange(0, _treePrefabs.Length)];

                // Skip only after the last draw so the seeded stream stays unchanged
                if (IsReserved(chunk, localPos.x, localPos.z)) continue;

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

                float height = chunk.SampleHeight(x - _chunkSize / 2, z - _chunkSize / 2);

                Vector3 localPos = new Vector3(x - _chunkSize / 2, height, z - _chunkSize / 2);
                Quaternion rotation = Quaternion.Euler(
                    SeededRandomRange(-10, 10),
                    SeededRandomRange(0, 360),
                    SeededRandomRange(-10, 10)
                );
                float scale = SeededRandomRange(0.5f, 2f);

                GameObject prefab = _rockPrefabs[SeededRandomRange(0, _rockPrefabs.Length)];
                if (IsReserved(chunk, localPos.x, localPos.z)) continue;

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

                float height = chunk.SampleHeight(x - _chunkSize / 2, z - _chunkSize / 2);

                Vector3 localPos = new Vector3(x - _chunkSize / 2, height, z - _chunkSize / 2);
                // Ramps face downhill (positive Z)
                Quaternion rotation = Quaternion.Euler(0, SeededRandomRange(-20, 20), 0);

                GameObject prefab = _rampPrefabs[SeededRandomRange(0, _rampPrefabs.Length)];

                // Legacy ramps still consume their draws when disabled, keeping later layouts stable
                if (!_spawnLegacyRamps || IsReserved(chunk, localPos.x, localPos.z)) continue;

                chunk.SpawnObject(prefab, localPos, rotation, Vector3.one);
            }
        }

        private void SpawnCoins(TerrainChunk chunk, float[,] heightMap, TerrainZone zone)
        {
            if (_coinPrefab == null) return;

            // Number of coin lines per chunk, increases with zone
            int lineCount = zone switch
            {
                TerrainZone.Tutorial => 2,
                TerrainZone.Forest => 3,
                TerrainZone.Extreme => 2,
                _ => 2
            };

            for (int line = 0; line < lineCount; line++)
            {
                if (_seededRandom.NextDouble() > 0.4) continue; // 40% chance per line

                // Pick a random X lane, coins run downhill (Z)
                float x = SeededRandomRange(_chunkSize * 0.15f, _chunkSize * 0.85f);
                float zStart = SeededRandomRange(0f, _chunkSize * 0.3f);
                int coinCount = SeededRandomRange(5, 10);
                float spacing = SeededRandomRange(8f, 14f);

                for (int i = 0; i < coinCount; i++)
                {
                    float z = zStart + i * spacing;
                    if (z >= _chunkSize) break;

                    float height = chunk.SampleHeight(x - _chunkSize / 2, z - _chunkSize / 2) + 1.5f; // Float above ground

                    Vector3 localPos = new Vector3(x - _chunkSize / 2, height, z - _chunkSize / 2);
                    if (IsReserved(chunk, localPos.x, localPos.z)) continue;

                    chunk.SpawnObject(_coinPrefab, localPos, Quaternion.identity, Vector3.one);
                }
            }
        }

        private void SpawnRails(TerrainChunk chunk, float[,] heightMap, TerrainZone zone)
        {
            if (_railPrefabs == null || _railPrefabs.Length == 0) return;

            // Rails are sparse, appear after tutorial
            int railCount = zone switch
            {
                TerrainZone.Tutorial => 0,
                TerrainZone.Forest => 2,
                TerrainZone.Extreme => 3,
                _ => 1
            };

            for (int i = 0; i < railCount; i++)
            {
                if (_seededRandom.NextDouble() > 0.25) continue; // 25% chance per rail

                float x = SeededRandomRange(_chunkSize * 0.2f, _chunkSize * 0.8f);
                float z = SeededRandomRange(_chunkSize * 0.2f, _chunkSize * 0.8f);

                float height = chunk.SampleHeight(x - _chunkSize / 2, z - _chunkSize / 2);

                Vector3 localPos = new Vector3(x - _chunkSize / 2, height, z - _chunkSize / 2);
                // Rails run roughly downhill with slight angle variation
                Quaternion rotation = Quaternion.Euler(0, SeededRandomRange(-30, 30), 0);

                GameObject prefab = _railPrefabs[SeededRandomRange(0, _railPrefabs.Length)];
                if (IsReserved(chunk, localPos.x, localPos.z)) continue;

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
            }
        }

        private Vector2Int GetChunkCoord(Vector3 position)
        {
            // Chunks are centred on coord * size, so round (not floor) to find the one underfoot
            return new Vector2Int(
                Mathf.RoundToInt(position.x / _chunkSize),
                Mathf.RoundToInt(position.z / _chunkSize)
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
        /// Set prefab arrays from PrefabRegistry.
        /// </summary>
        public void SetPrefabsFromRegistry(Configuration.PrefabRegistry registry)
        {
            if (registry == null) return;

            _treePrefabs = registry.GetAllTrees();
            _rockPrefabs = registry.GetAllRocks();
            _rampPrefabs = registry.GetAllRamps();
            _railPrefabs = registry.GetAllRails();
            _coinPrefab = registry.CoinPrefab;
        }

        /// <summary>
        /// Generate terrain chunks immediately around the player.
        /// Call after setting the player reference to ensure ground exists before gameplay starts.
        /// </summary>
        public void GenerateInitialChunks()
        {
            if (_player == null) return;

            EnsureInitialized();
            UpdateChunks();

            // Process all queued chunks immediately (not frame-limited)
            while (_chunksToGenerate.Count > 0)
            {
                Vector2Int coord = _chunksToGenerate.Dequeue();
                _queuedChunks.Remove(coord);

                if (!_chunks.ContainsKey(coord))
                {
                    GenerateChunk(coord);
                }
            }

            Debug.Log($"[TerrainGenerator] Generated {_chunks.Count} initial chunks");
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

            _chunks.Clear();

            // Rebuild the ground under the player immediately so they aren't left over a void
            if (_player != null)
            {
                SafeExecution.Try(GenerateInitialChunks, "RecoveryRegenerate");
            }

            Debug.Log("[TerrainGenerator] Recovery complete - terrain reset");
        }
    }
}
