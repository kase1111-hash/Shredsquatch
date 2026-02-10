using UnityEngine;
using System.Collections.Generic;

namespace Shredsquatch.Terrain
{
    public class TerrainChunk : MonoBehaviour
    {
        [Header("Mesh")]
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshCollider _meshCollider;
        [SerializeField] private MeshRenderer _meshRenderer;

        [Header("Settings")]
        public Vector2Int ChunkCoord;
        public float Size;

        private Mesh _mesh;
        private List<GameObject> _spawnedObjects = new List<GameObject>();
        private bool _isActive;

        public bool IsActive => _isActive;
        public Bounds Bounds => _meshRenderer != null ? _meshRenderer.bounds : new Bounds(transform.position, Vector3.one * Size);

        public void Initialize(Vector2Int coord, float size, Material terrainMaterial)
        {
            ChunkCoord = coord;
            Size = size;

            // Wire component references if not assigned (procedural chunks)
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshCollider == null) _meshCollider = GetComponent<MeshCollider>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            if (_meshRenderer != null && terrainMaterial != null)
            {
                _meshRenderer.material = terrainMaterial;
            }

            _mesh = new Mesh();
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            if (_meshFilter != null)
            {
                _meshFilter.mesh = _mesh;
            }
        }

        public void GenerateMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            // Scale factor to match chunk size (vertices to world units)
            float meshScale = Size / (width - 1);

            float topLeftX = Size / -2f;
            float topLeftZ = Size / 2f;

            Vector3[] vertices = new Vector3[width * height];
            Vector2[] uvs = new Vector2[width * height];
            int[] triangles = new int[(width - 1) * (height - 1) * 6];

            int vertexIndex = 0;
            int triangleIndex = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float heightValue = heightCurve != null
                        ? heightCurve.Evaluate(heightMap[x, y])
                        : heightMap[x, y];

                    // Scale vertices to match chunk size
                    vertices[vertexIndex] = new Vector3(
                        topLeftX + x * meshScale,
                        heightValue * heightMultiplier,
                        topLeftZ - y * meshScale
                    );

                    uvs[vertexIndex] = new Vector2(x / (float)(width - 1), y / (float)(height - 1));

                    if (x < width - 1 && y < height - 1)
                    {
                        // Triangle 1
                        triangles[triangleIndex] = vertexIndex;
                        triangles[triangleIndex + 1] = vertexIndex + width + 1;
                        triangles[triangleIndex + 2] = vertexIndex + width;

                        // Triangle 2
                        triangles[triangleIndex + 3] = vertexIndex + width + 1;
                        triangles[triangleIndex + 4] = vertexIndex;
                        triangles[triangleIndex + 5] = vertexIndex + 1;

                        triangleIndex += 6;
                    }

                    vertexIndex++;
                }
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            if (_meshCollider != null)
            {
                _meshCollider.sharedMesh = _mesh;
            }
        }

        public void SpawnObject(GameObject prefab, Vector3 localPosition, Quaternion rotation, Vector3 scale)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            _spawnedObjects.Add(obj);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            gameObject.SetActive(active);
        }

        public void Clear()
        {
            foreach (var obj in _spawnedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            _spawnedObjects.Clear();

            if (_mesh != null)
            {
                _mesh.Clear();
            }
        }

        private void OnDestroy()
        {
            Clear();
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
