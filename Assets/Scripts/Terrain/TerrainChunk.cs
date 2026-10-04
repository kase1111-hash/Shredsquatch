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

        // Final (world-unit) vertex heights, indexed [x, z-row]; used to place objects on the surface
        private float[,] _heights;
        private float _vertexSpacing = 1f;

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

        /// <summary>
        /// Build the chunk surface. heightMap[x, y] is indexed so that y grows toward +Z
        /// (downhill), matching TerrainGenerator's slope bias and object placement.
        /// </summary>
        /// <param name="apronHeights">
        /// Optional (width + 2) x (height + 2) map with one extra ring of samples around heightMap
        /// (apronHeights[x + 1, y + 1] == heightMap[x, y]). When given, normals come from central
        /// differences over it, so border vertices shade the same as the neighbouring chunk's;
        /// otherwise Unity recalculates them from this chunk's triangles only.
        /// </param>
        public void GenerateMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, float[,] apronHeights = null)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            // Scale factor to match chunk size (vertices to world units)
            _vertexSpacing = Size / (width - 1);
            float originX = Size / -2f;
            float originZ = Size / -2f;

            Vector3[] vertices = new Vector3[width * height];
            Vector2[] uvs = new Vector2[width * height];
            int[] triangles = new int[(width - 1) * (height - 1) * 6];
            _heights = new float[width, height];

            int vertexIndex = 0;
            int triangleIndex = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float heightValue = heightCurve != null && heightCurve.length > 0
                        ? heightCurve.Evaluate(heightMap[x, y])
                        : heightMap[x, y];

                    float worldHeight = heightValue * heightMultiplier;
                    _heights[x, y] = worldHeight;

                    vertices[vertexIndex] = new Vector3(
                        originX + x * _vertexSpacing,
                        worldHeight,
                        originZ + y * _vertexSpacing
                    );

                    uvs[vertexIndex] = new Vector2(x / (float)(width - 1), y / (float)(height - 1));

                    if (x < width - 1 && y < height - 1)
                    {
                        // Rows advance toward +Z, so wind (v, v+width, v+width+1) to keep normals facing up
                        triangles[triangleIndex] = vertexIndex;
                        triangles[triangleIndex + 1] = vertexIndex + width;
                        triangles[triangleIndex + 2] = vertexIndex + width + 1;

                        triangles[triangleIndex + 3] = vertexIndex + width + 1;
                        triangles[triangleIndex + 4] = vertexIndex + 1;
                        triangles[triangleIndex + 5] = vertexIndex;

                        triangleIndex += 6;
                    }

                    vertexIndex++;
                }
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;

            bool hasApron = apronHeights != null
                && apronHeights.GetLength(0) == width + 2
                && apronHeights.GetLength(1) == height + 2;
            if (hasApron)
            {
                _mesh.normals = ComputeApronNormals(apronHeights, width, height, heightMultiplier, heightCurve);
            }
            else
            {
                _mesh.RecalculateNormals();
            }

            _mesh.RecalculateBounds();

            if (_meshCollider != null)
            {
                _meshCollider.sharedMesh = _mesh;
            }
        }

        private Vector3[] ComputeApronNormals(float[,] apron, int width, int height, float heightMultiplier, AnimationCurve heightCurve)
        {
            bool useCurve = heightCurve != null && heightCurve.length > 0;
            Vector3[] normals = new Vector3[width * height];
            float twoSpacing = 2f * _vertexSpacing;

            int i = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float left = apron[x, y + 1];
                    float right = apron[x + 2, y + 1];
                    float back = apron[x + 1, y];
                    float front = apron[x + 1, y + 2];

                    if (useCurve)
                    {
                        left = heightCurve.Evaluate(left);
                        right = heightCurve.Evaluate(right);
                        back = heightCurve.Evaluate(back);
                        front = heightCurve.Evaluate(front);
                    }

                    // Surface y = h(x, z): normal is (-dh/dx, 1, -dh/dz), scaled by 2 * spacing
                    normals[i++] = new Vector3(
                        (left - right) * heightMultiplier,
                        twoSpacing,
                        (back - front) * heightMultiplier).normalized;
                }
            }

            return normals;
        }

        /// <summary>
        /// Surface height (local Y) at a chunk-local XZ position, bilinearly interpolated
        /// from the generated vertices. Returns 0 before the mesh has been generated.
        /// </summary>
        public float SampleHeight(float localX, float localZ)
        {
            if (_heights == null) return 0f;

            int width = _heights.GetLength(0);
            int height = _heights.GetLength(1);

            float fx = Mathf.Clamp((localX + Size / 2f) / _vertexSpacing, 0f, width - 1);
            float fz = Mathf.Clamp((localZ + Size / 2f) / _vertexSpacing, 0f, height - 1);

            int x0 = Mathf.FloorToInt(fx);
            int z0 = Mathf.FloorToInt(fz);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int z1 = Mathf.Min(z0 + 1, height - 1);
            float tx = fx - x0;
            float tz = fz - z0;

            float near = Mathf.Lerp(_heights[x0, z0], _heights[x1, z0], tx);
            float far = Mathf.Lerp(_heights[x0, z1], _heights[x1, z1], tx);
            return Mathf.Lerp(near, far, tz);
        }

        public void SpawnObject(GameObject prefab, Vector3 localPosition, Quaternion rotation, Vector3 scale)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            _spawnedObjects.Add(obj);
        }

        /// <summary>
        /// Parent a code-built object to this chunk without touching its scale (unlike SpawnObject).
        /// Clear() destroys it with the rest of the chunk's objects.
        /// </summary>
        public void AdoptObject(GameObject obj, Vector3 localPosition, Quaternion localRotation)
        {
            if (obj == null) return;
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
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
