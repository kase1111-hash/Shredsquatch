using UnityEngine;

namespace Shredsquatch.Challenger
{
    /// <summary>
    /// Generates placeholder assets for challenger mode testing.
    /// Replace with real assets when permission is obtained.
    /// </summary>
    public static class PlaceholderAssets
    {
        /// <summary>
        /// Create a simple placeholder character mesh.
        /// </summary>
        public static Mesh CreatePlaceholderCharacter()
        {
            // Simple humanoid shape - capsule with sphere head
            var mesh = new Mesh();
            mesh.name = "PlaceholderCharacter";

            // Combine primitives conceptually
            // Body: elongated capsule
            // Head: sphere on top

            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // Body (simplified cylinder)
            int segments = 12;
            float bodyRadius = 0.4f;
            float bodyHeight = 1.2f;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(angle) * bodyRadius;
                float z = Mathf.Sin(angle) * bodyRadius;

                vertices.Add(new Vector3(x, 0, z));
                vertices.Add(new Vector3(x, bodyHeight, z));
            }

            // Body triangles
            for (int i = 0; i < segments; i++)
            {
                int curr = i * 2;
                int next = (i + 1) * 2;

                triangles.Add(curr);
                triangles.Add(curr + 1);
                triangles.Add(next);

                triangles.Add(next);
                triangles.Add(curr + 1);
                triangles.Add(next + 1);
            }

            // Head (simplified sphere octants)
            int headStart = vertices.Count;
            float headRadius = 0.3f;
            float headY = bodyHeight + headRadius;

            // Top
            vertices.Add(new Vector3(0, headY + headRadius, 0));

            // Middle ring
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8;
                vertices.Add(new Vector3(
                    Mathf.Cos(angle) * headRadius,
                    headY,
                    Mathf.Sin(angle) * headRadius
                ));
            }

            // Head triangles (top cap)
            for (int i = 0; i < 8; i++)
            {
                triangles.Add(headStart);
                triangles.Add(headStart + 1 + i);
                triangles.Add(headStart + 1 + ((i + 1) % 8));
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Create a placeholder waddle mount mesh (penguin-like).
        /// </summary>
        public static Mesh CreatePlaceholderMount()
        {
            var mesh = new Mesh();
            mesh.name = "PlaceholderMount";

            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // Egg-shaped body
            int rings = 8;
            int segments = 12;
            float width = 0.4f;
            float height = 0.6f;
            float length = 0.8f;

            for (int ring = 0; ring <= rings; ring++)
            {
                float v = (float)ring / rings;
                float ringY = Mathf.Lerp(-height / 2, height / 2, v);

                // Egg shape - wider at bottom
                float eggFactor = 1f - Mathf.Pow((v - 0.6f), 2) * 2f;
                eggFactor = Mathf.Clamp01(eggFactor);

                for (int seg = 0; seg <= segments; seg++)
                {
                    float u = (float)seg / segments;
                    float angle = u * Mathf.PI * 2f;

                    float x = Mathf.Cos(angle) * width * eggFactor;
                    float z = Mathf.Sin(angle) * length * eggFactor;

                    vertices.Add(new Vector3(x, ringY, z));
                }
            }

            // Triangles
            for (int ring = 0; ring < rings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int curr = ring * (segments + 1) + seg;
                    int next = curr + segments + 1;

                    triangles.Add(curr);
                    triangles.Add(next);
                    triangles.Add(curr + 1);

                    triangles.Add(curr + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            // Beak (simple cone)
            int beakStart = vertices.Count;
            Vector3 beakBase = new Vector3(0, 0.1f, length * 0.8f);
            Vector3 beakTip = beakBase + new Vector3(0, 0, 0.3f);

            vertices.Add(beakTip);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6;
                vertices.Add(beakBase + new Vector3(
                    Mathf.Cos(angle) * 0.1f,
                    Mathf.Sin(angle) * 0.05f,
                    0
                ));
            }

            for (int i = 0; i < 6; i++)
            {
                triangles.Add(beakStart);
                triangles.Add(beakStart + 1 + ((i + 1) % 6));
                triangles.Add(beakStart + 1 + i);
            }

            // Flippers (flat triangles on sides)
            int flipperStart = vertices.Count;

            // Left flipper
            vertices.Add(new Vector3(-width * 0.8f, 0.1f, 0));
            vertices.Add(new Vector3(-width * 1.5f, -0.1f, 0.2f));
            vertices.Add(new Vector3(-width * 0.8f, -0.2f, 0.1f));

            triangles.Add(flipperStart);
            triangles.Add(flipperStart + 1);
            triangles.Add(flipperStart + 2);

            // Right flipper
            vertices.Add(new Vector3(width * 0.8f, 0.1f, 0));
            vertices.Add(new Vector3(width * 1.5f, -0.1f, 0.2f));
            vertices.Add(new Vector3(width * 0.8f, -0.2f, 0.1f));

            triangles.Add(flipperStart + 3);
            triangles.Add(flipperStart + 5);
            triangles.Add(flipperStart + 4);

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Create placeholder materials.
        /// </summary>
        public static Material CreatePlaceholderMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        /// <summary>
        /// Create a complete placeholder character GameObject.
        /// </summary>
        public static GameObject CreatePlaceholderCharacterObject()
        {
            var go = new GameObject("PlaceholderChallenger");

            // Add mesh
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = CreatePlaceholderCharacter();

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = CreatePlaceholderMaterial(new Color(0.6f, 0.5f, 0.4f)); // Brown-ish

            // Add basic components
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            return go;
        }

        /// <summary>
        /// Create a complete placeholder mount GameObject.
        /// </summary>
        public static GameObject CreatePlaceholderMountObject()
        {
            var go = new GameObject("PlaceholderMount");

            // Add mesh
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = CreatePlaceholderMount();

            var mr = go.AddComponent<MeshRenderer>();

            // Black and white like a penguin
            var mats = new Material[]
            {
                CreatePlaceholderMaterial(new Color(0.1f, 0.1f, 0.1f)), // Black back
                CreatePlaceholderMaterial(new Color(0.95f, 0.95f, 0.95f)) // White front
            };
            mr.material = mats[0]; // Single material for now

            // Add mount component
            go.AddComponent<CreatureMount>();

            return go;
        }
    }
}
