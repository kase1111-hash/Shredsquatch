using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shredsquatch.Procedural
{
    /// <summary>
    /// Builds and places the primitive body parts of the procedural character rigs.
    /// Parts have no colliders, so they never interfere with gameplay physics or triggers.
    /// </summary>
    public static class ProceduralRig
    {
        private static readonly Dictionary<PrimitiveType, Mesh> _meshes = new Dictionary<PrimitiveType, Mesh>();

        /// <summary>
        /// Create a rendered primitive under parent. A part that is not visible to the camera
        /// still casts its shadow (used for the first-person rider's own torso and head).
        /// </summary>
        public static Transform CreatePart(string name, PrimitiveType shape, Transform parent, Material material,
            bool visibleToCamera = true)
        {
            var part = new GameObject(name);
            part.layer = parent.gameObject.layer;
            part.transform.SetParent(parent, false);

            part.AddComponent<MeshFilter>().sharedMesh = GetMesh(shape);

            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = visibleToCamera ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;

            return part.transform;
        }

        /// <summary>
        /// Stretch a capsule part between two points given in its parent's space.
        /// </summary>
        public static void PlaceCapsule(Transform part, Vector3 from, Vector3 to, float thickness)
        {
            PlaceCapsule(part, from, to, thickness, thickness);
        }

        /// <summary>
        /// Stretch a capsule between two points with a different width (along the parent's X)
        /// and depth, e.g. a torso that is broader than it is deep.
        /// </summary>
        public static void PlaceCapsule(Transform part, Vector3 from, Vector3 to, float width, float depth)
        {
            Vector3 axis = to - from;
            float length = axis.magnitude;

            part.localPosition = (from + to) * 0.5f;
            part.localRotation = length > 0.0001f
                ? Quaternion.FromToRotation(Vector3.up, axis / length)
                : Quaternion.identity;

            // The primitive capsule is 2 units tall; let its rounded ends overlap the joints
            float thickness = Mathf.Max(width, depth);
            part.localScale = new Vector3(width, (length + thickness * 0.5f) * 0.5f, depth);
        }

        /// <summary>
        /// Solid-colour material. Copying a template that is already in use keeps its shader
        /// in WebGL builds, where Shader.Find only sees shaders some asset references.
        /// </summary>
        public static Material CreateMaterial(Material template, Color color)
        {
            Material material;
            if (template != null)
            {
                material = new Material(template);
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
            }

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", null);
            material.color = color;
            return material;
        }

        public static void SetEmission(Material material, Color emission)
        {
            if (!material.HasProperty("_EmissionColor")) return;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        private static Mesh GetMesh(PrimitiveType shape)
        {
            if (_meshes.TryGetValue(shape, out Mesh mesh) && mesh != null) return mesh;

            // Borrow Unity's built-in primitive mesh; destroy the temporary object (and its
            // collider) immediately so it never takes part in a physics step
            var temp = GameObject.CreatePrimitive(shape);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);

            _meshes[shape] = mesh;
            return mesh;
        }
    }
}
