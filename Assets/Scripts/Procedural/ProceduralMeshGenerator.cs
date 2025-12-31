using UnityEngine;
using System.Collections.Generic;

namespace Shredsquatch.Procedural
{
    /// <summary>
    /// Generates procedural meshes for environment objects.
    /// Use these as placeholders until real 3D assets are available.
    /// </summary>
    public static class ProceduralMeshGenerator
    {
        #region Trees

        /// <summary>
        /// Generate a simple pine tree mesh (cone on cylinder trunk).
        /// </summary>
        public static Mesh GeneratePineTree(float trunkHeight = 2f, float trunkRadius = 0.15f,
            float coneHeight = 4f, float coneRadius = 1.5f, int segments = 8)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralPineTree";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // Generate trunk (cylinder)
            int trunkBaseIndex = 0;
            GenerateCylinder(vertices, triangles, uvs, Vector3.zero, trunkRadius, trunkHeight, segments);

            // Generate foliage (cone)
            int coneBaseIndex = vertices.Count;
            GenerateCone(vertices, triangles, uvs, new Vector3(0, trunkHeight, 0), coneRadius, coneHeight, segments);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generate a dead/bare tree (trunk with branches).
        /// </summary>
        public static Mesh GenerateDeadTree(float trunkHeight = 5f, float trunkRadius = 0.2f, int segments = 6)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralDeadTree";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // Main trunk
            GenerateCylinder(vertices, triangles, uvs, Vector3.zero, trunkRadius, trunkHeight, segments);

            // Add some branches
            float branchHeight = trunkHeight * 0.6f;
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 branchDir = new Vector3(Mathf.Cos(angle), 0.5f, Mathf.Sin(angle)).normalized;
                Vector3 branchStart = new Vector3(0, branchHeight + i * 0.3f, 0);

                GenerateBranch(vertices, triangles, uvs, branchStart, branchDir,
                    trunkRadius * 0.4f, 1.5f, segments);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generate a fallen log.
        /// </summary>
        public static Mesh GenerateFallenLog(float length = 8f, float radius = 0.3f, int segments = 8)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralFallenLog";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // Rotated cylinder (lying on side)
            GenerateCylinderHorizontal(vertices, triangles, uvs, Vector3.zero, radius, length, segments);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        #endregion

        #region Rocks

        /// <summary>
        /// Generate a boulder mesh (deformed sphere).
        /// </summary>
        public static Mesh GenerateBoulder(float radius = 1f, float deformation = 0.3f, int subdivisions = 2)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralBoulder";

            // Start with icosphere
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            CreateIcosphere(vertices, triangles, subdivisions);

            // Deform vertices for rocky look
            System.Random rng = new System.Random(42);
            for (int i = 0; i < vertices.Count; i++)
            {
                float noise = 1f + (float)(rng.NextDouble() * 2 - 1) * deformation;
                vertices[i] = vertices[i].normalized * radius * noise;
            }

            // Generate UVs
            var uvs = new List<Vector2>();
            foreach (var v in vertices)
            {
                float u = Mathf.Atan2(v.x, v.z) / (2f * Mathf.PI) + 0.5f;
                float vCoord = v.y / (2f * radius) + 0.5f;
                uvs.Add(new Vector2(u, vCoord));
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generate a rock outcrop (flat-ish boulder).
        /// </summary>
        public static Mesh GenerateRockOutcrop(float width = 2f, float height = 1f, float depth = 1.5f)
        {
            var mesh = GenerateBoulder(1f, 0.4f, 2);
            mesh.name = "ProceduralRockOutcrop";

            // Scale vertices
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(
                    vertices[i].x * width,
                    vertices[i].y * height,
                    vertices[i].z * depth
                );
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            return mesh;
        }

        #endregion

        #region Ramps

        /// <summary>
        /// Generate a snow ramp/kicker.
        /// </summary>
        public static Mesh GenerateRamp(float width = 3f, float length = 4f, float height = 2f)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralRamp";

            // Simple wedge shape
            var vertices = new Vector3[]
            {
                // Bottom face
                new Vector3(-width/2, 0, 0),
                new Vector3(width/2, 0, 0),
                new Vector3(width/2, 0, length),
                new Vector3(-width/2, 0, length),
                // Top edge
                new Vector3(-width/2, height, length),
                new Vector3(width/2, height, length),
            };

            var triangles = new int[]
            {
                // Bottom
                0, 2, 1,
                0, 3, 2,
                // Ramp surface
                0, 4, 3,
                0, 1, 5,
                0, 5, 4,
                1, 2, 5,
                // Back
                3, 4, 5,
                3, 5, 2,
                // Sides
                0, 4, 3,
                1, 2, 5,
            };

            var uvs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(1, 1),
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generate a half-pipe section.
        /// </summary>
        public static Mesh GenerateHalfPipe(float width = 6f, float length = 20f, float height = 3f, int segments = 12)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralHalfPipe";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            float radius = width / 2f;

            // Generate curved surface
            for (int z = 0; z <= 1; z++)
            {
                float zPos = z * length;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = Mathf.PI * i / segments; // 0 to PI (half circle)
                    float x = Mathf.Cos(angle) * radius;
                    float y = Mathf.Sin(angle) * height / radius * radius;

                    vertices.Add(new Vector3(x, y, zPos));
                    uvs.Add(new Vector2((float)i / segments, z));
                }
            }

            // Generate triangles
            for (int i = 0; i < segments; i++)
            {
                int row1 = i;
                int row2 = i + segments + 1;

                triangles.Add(row1);
                triangles.Add(row2);
                triangles.Add(row1 + 1);

                triangles.Add(row1 + 1);
                triangles.Add(row2);
                triangles.Add(row2 + 1);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        #endregion

        #region Rails

        /// <summary>
        /// Generate a fence rail.
        /// </summary>
        public static Mesh GenerateFenceRail(float length = 10f, float postSpacing = 3f,
            float railHeight = 0.8f, float railRadius = 0.05f, int segments = 6)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralFenceRail";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // Main rail (horizontal cylinder)
            GenerateCylinderHorizontal(vertices, triangles, uvs,
                new Vector3(0, railHeight, 0), railRadius, length, segments);

            // Posts
            int numPosts = Mathf.CeilToInt(length / postSpacing) + 1;
            for (int i = 0; i < numPosts; i++)
            {
                float z = i * postSpacing;
                if (z > length) z = length;

                int baseIndex = vertices.Count;
                GenerateCylinder(vertices, triangles, uvs,
                    new Vector3(0, 0, z), 0.08f, railHeight + 0.1f, 4);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generate a metal pipe rail.
        /// </summary>
        public static Mesh GeneratePipeRail(float length = 15f, float radius = 0.1f, int segments = 8)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralPipeRail";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            GenerateCylinderHorizontal(vertices, triangles, uvs,
                new Vector3(0, 0.5f, 0), radius, length, segments);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        #endregion

        #region Chairlift

        /// <summary>
        /// Generate a chairlift tower.
        /// </summary>
        public static Mesh GenerateChairliftTower(float height = 15f, float baseWidth = 2f)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralChairliftTower";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // Main pole
            GenerateCylinder(vertices, triangles, uvs, Vector3.zero, 0.3f, height, 6);

            // Cross arm at top
            int armBase = vertices.Count;
            GenerateCylinderHorizontal(vertices, triangles, uvs,
                new Vector3(0, height - 0.5f, 0), 0.15f, baseWidth, 6);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generate a simple chairlift chair.
        /// </summary>
        public static Mesh GenerateChairliftChair(float width = 1.5f, float depth = 0.6f)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralChairliftChair";

            // Simple box seat with back
            var vertices = new Vector3[]
            {
                // Seat
                new Vector3(-width/2, 0, 0),
                new Vector3(width/2, 0, 0),
                new Vector3(width/2, 0, depth),
                new Vector3(-width/2, 0, depth),
                new Vector3(-width/2, -0.1f, 0),
                new Vector3(width/2, -0.1f, 0),
                new Vector3(width/2, -0.1f, depth),
                new Vector3(-width/2, -0.1f, depth),
                // Back
                new Vector3(-width/2, 0, 0),
                new Vector3(width/2, 0, 0),
                new Vector3(width/2, 0.8f, -0.1f),
                new Vector3(-width/2, 0.8f, -0.1f),
            };

            var triangles = new int[]
            {
                // Seat top
                0, 1, 2, 0, 2, 3,
                // Seat bottom
                4, 6, 5, 4, 7, 6,
                // Seat sides
                0, 4, 1, 1, 4, 5,
                2, 6, 3, 3, 6, 7,
                0, 3, 4, 4, 3, 7,
                1, 5, 2, 2, 5, 6,
                // Back
                8, 10, 9, 8, 11, 10,
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        #endregion

        #region Powerups

        /// <summary>
        /// Generate a coin mesh.
        /// </summary>
        public static Mesh GenerateCoin(float radius = 0.3f, float thickness = 0.05f, int segments = 16)
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralCoin";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // Front face
            vertices.Add(new Vector3(0, 0, thickness / 2));
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, thickness / 2));
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            for (int i = 0; i < segments; i++)
            {
                triangles.Add(0);
                triangles.Add(1 + i);
                triangles.Add(1 + (i + 1) % segments);
            }

            // Back face
            int backCenter = vertices.Count;
            vertices.Add(new Vector3(0, 0, -thickness / 2));
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -thickness / 2));
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            for (int i = 0; i < segments; i++)
            {
                triangles.Add(backCenter);
                triangles.Add(backCenter + 1 + (i + 1) % segments);
                triangles.Add(backCenter + 1 + i);
            }

            // Edge
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int f1 = 1 + i;
                int f2 = 1 + next;
                int b1 = backCenter + 1 + i;
                int b2 = backCenter + 1 + next;

                triangles.Add(f1);
                triangles.Add(b1);
                triangles.Add(f2);

                triangles.Add(f2);
                triangles.Add(b1);
                triangles.Add(b2);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        #endregion

        #region Helper Methods

        private static void GenerateCylinder(List<Vector3> vertices, List<int> triangles,
            List<Vector2> uvs, Vector3 baseCenter, float radius, float height, int segments)
        {
            int baseIndex = vertices.Count;

            // Bottom center
            vertices.Add(baseCenter);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Bottom ring
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                vertices.Add(baseCenter + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            // Bottom face
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1 + (i + 1) % segments);
                triangles.Add(baseIndex + 1 + i);
            }

            // Top center
            int topCenterIndex = vertices.Count;
            vertices.Add(baseCenter + Vector3.up * height);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Top ring
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                vertices.Add(baseCenter + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            // Top face
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(topCenterIndex);
                triangles.Add(topCenterIndex + 1 + i);
                triangles.Add(topCenterIndex + 1 + (i + 1) % segments);
            }

            // Side faces
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int bottom1 = baseIndex + 1 + i;
                int bottom2 = baseIndex + 1 + next;
                int top1 = topCenterIndex + 1 + i;
                int top2 = topCenterIndex + 1 + next;

                triangles.Add(bottom1);
                triangles.Add(top1);
                triangles.Add(bottom2);

                triangles.Add(bottom2);
                triangles.Add(top1);
                triangles.Add(top2);
            }
        }

        private static void GenerateCylinderHorizontal(List<Vector3> vertices, List<int> triangles,
            List<Vector2> uvs, Vector3 center, float radius, float length, int segments)
        {
            int baseIndex = vertices.Count;

            // Front cap center
            vertices.Add(center);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Front ring
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                vertices.Add(center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            // Front cap
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1 + (i + 1) % segments);
                triangles.Add(baseIndex + 1 + i);
            }

            // Back cap center
            int backCenterIndex = vertices.Count;
            vertices.Add(center + Vector3.forward * length);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Back ring
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                vertices.Add(center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, length));
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            // Back cap
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(backCenterIndex);
                triangles.Add(backCenterIndex + 1 + i);
                triangles.Add(backCenterIndex + 1 + (i + 1) % segments);
            }

            // Side faces
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int front1 = baseIndex + 1 + i;
                int front2 = baseIndex + 1 + next;
                int back1 = backCenterIndex + 1 + i;
                int back2 = backCenterIndex + 1 + next;

                triangles.Add(front1);
                triangles.Add(back1);
                triangles.Add(front2);

                triangles.Add(front2);
                triangles.Add(back1);
                triangles.Add(back2);
            }
        }

        private static void GenerateCone(List<Vector3> vertices, List<int> triangles,
            List<Vector2> uvs, Vector3 baseCenter, float radius, float height, int segments)
        {
            int baseIndex = vertices.Count;

            // Base center
            vertices.Add(baseCenter);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Base ring
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                vertices.Add(baseCenter + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            // Base face
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1 + (i + 1) % segments);
                triangles.Add(baseIndex + 1 + i);
            }

            // Tip
            int tipIndex = vertices.Count;
            vertices.Add(baseCenter + Vector3.up * height);
            uvs.Add(new Vector2(0.5f, 1f));

            // Cone sides
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(baseIndex + 1 + i);
                triangles.Add(baseIndex + 1 + (i + 1) % segments);
                triangles.Add(tipIndex);
            }
        }

        private static void GenerateBranch(List<Vector3> vertices, List<int> triangles,
            List<Vector2> uvs, Vector3 start, Vector3 direction, float radius, float length, int segments)
        {
            Vector3 end = start + direction * length;

            int baseIndex = vertices.Count;

            // Create simple tapered cylinder for branch
            Quaternion rotation = Quaternion.LookRotation(direction);

            for (int ring = 0; ring <= 1; ring++)
            {
                Vector3 center = Vector3.Lerp(start, end, ring);
                float r = Mathf.Lerp(radius, radius * 0.3f, ring);

                for (int i = 0; i < segments; i++)
                {
                    float angle = 2f * Mathf.PI * i / segments;
                    Vector3 offset = rotation * new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
                    vertices.Add(center + offset);
                    uvs.Add(new Vector2((float)i / segments, ring));
                }
            }

            // Triangles
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int ring1 = baseIndex + i;
                int ring1Next = baseIndex + next;
                int ring2 = baseIndex + segments + i;
                int ring2Next = baseIndex + segments + next;

                triangles.Add(ring1);
                triangles.Add(ring2);
                triangles.Add(ring1Next);

                triangles.Add(ring1Next);
                triangles.Add(ring2);
                triangles.Add(ring2Next);
            }
        }

        private static void CreateIcosphere(List<Vector3> vertices, List<int> triangles, int subdivisions)
        {
            // Golden ratio
            float t = (1f + Mathf.Sqrt(5f)) / 2f;

            // Initial icosahedron vertices
            vertices.Add(new Vector3(-1, t, 0).normalized);
            vertices.Add(new Vector3(1, t, 0).normalized);
            vertices.Add(new Vector3(-1, -t, 0).normalized);
            vertices.Add(new Vector3(1, -t, 0).normalized);
            vertices.Add(new Vector3(0, -1, t).normalized);
            vertices.Add(new Vector3(0, 1, t).normalized);
            vertices.Add(new Vector3(0, -1, -t).normalized);
            vertices.Add(new Vector3(0, 1, -t).normalized);
            vertices.Add(new Vector3(t, 0, -1).normalized);
            vertices.Add(new Vector3(t, 0, 1).normalized);
            vertices.Add(new Vector3(-t, 0, -1).normalized);
            vertices.Add(new Vector3(-t, 0, 1).normalized);

            // Initial triangles
            int[] initialTris = new int[]
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };

            triangles.AddRange(initialTris);

            // Subdivide
            for (int s = 0; s < subdivisions; s++)
            {
                var newTriangles = new List<int>();
                var midpointCache = new Dictionary<long, int>();

                for (int i = 0; i < triangles.Count; i += 3)
                {
                    int v1 = triangles[i];
                    int v2 = triangles[i + 1];
                    int v3 = triangles[i + 2];

                    int a = GetMidpoint(v1, v2, vertices, midpointCache);
                    int b = GetMidpoint(v2, v3, vertices, midpointCache);
                    int c = GetMidpoint(v3, v1, vertices, midpointCache);

                    newTriangles.AddRange(new[] { v1, a, c });
                    newTriangles.AddRange(new[] { v2, b, a });
                    newTriangles.AddRange(new[] { v3, c, b });
                    newTriangles.AddRange(new[] { a, b, c });
                }

                triangles.Clear();
                triangles.AddRange(newTriangles);
            }
        }

        private static int GetMidpoint(int v1, int v2, List<Vector3> vertices, Dictionary<long, int> cache)
        {
            long key = ((long)Mathf.Min(v1, v2) << 32) + Mathf.Max(v1, v2);

            if (cache.TryGetValue(key, out int midpoint))
                return midpoint;

            Vector3 p1 = vertices[v1];
            Vector3 p2 = vertices[v2];
            Vector3 mid = ((p1 + p2) / 2f).normalized;

            midpoint = vertices.Count;
            vertices.Add(mid);
            cache[key] = midpoint;

            return midpoint;
        }

        #endregion
    }
}
