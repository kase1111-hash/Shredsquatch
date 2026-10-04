using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Shredsquatch.Player;
using Shredsquatch.Terrain;
using Shredsquatch.Tricks;

namespace Shredsquatch.Tests.Editor
{
    /// <summary>
    /// Park pieces are built entirely in code, so their meshes, colliders and layers can be
    /// checked in EditMode without a scene or prefabs.
    /// </summary>
    [TestFixture]
    public class ParkFeatureBuilderTests
    {
        private const int GroundLayer = 8;
        private const int IgnoreRaycastLayer = 2;

        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }

        [Test]
        public void KickerMesh_FacesPointOutward()
        {
            ParkPiece[] kickers = { ParkPiece.KickerSmall, ParkPiece.KickerMedium, ParkPiece.KickerLarge };
            foreach (ParkPiece piece in kickers)
            {
                Mesh mesh = ParkFeatureBuilder.GetKickerMesh(piece);
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Assert.Greater(triangles.Length, 0, $"{piece} has no triangles");

                Vector3 centroid = Vector3.zero;
                foreach (Vector3 v in vertices) centroid += v;
                centroid /= vertices.Length;

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]];
                    Vector3 b = vertices[triangles[i + 1]];
                    Vector3 c = vertices[triangles[i + 2]];
                    Vector3 triangleCentroid = (a + b + c) / 3f;
                    float facing = Vector3.Dot(Vector3.Cross(b - a, c - a), triangleCentroid - centroid);
                    Assert.Greater(facing, 0f, $"{piece} triangle {i / 3} faces inward");
                }
            }
        }

        [Test]
        public void Kicker_HasGroundBodyAndAutoZone()
        {
            // Null materials must be tolerated
            GameObject root = Track(ParkFeatureBuilder.BuildKicker(ParkPiece.KickerSmall, null));
            ParkFeatureBuilder.GetKickerDims(ParkPiece.KickerSmall, out _, out float zLip, out _, out _, out _);

            Assert.AreEqual(Vector3.one, root.transform.localScale);

            int solids = 0;
            int triggers = 0;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Assert.AreNotEqual("Tree", collider.tag, collider.name);
                Assert.AreNotEqual("Rock", collider.tag, collider.name);

                if (collider.isTrigger)
                {
                    triggers++;
                    Assert.IsInstanceOf<BoxCollider>(collider, collider.name);
                    Assert.AreEqual(IgnoreRaycastLayer, collider.gameObject.layer, collider.name);

                    RampZone zone = collider.GetComponent<RampZone>();
                    Assert.IsNotNull(zone, collider.name);
                    Assert.AreEqual(JumpController.RampType.SmallBump, zone.Ramp);
                    Assert.IsTrue(zone.AutoLaunch);
                    Assert.AreEqual(zLip, collider.transform.localPosition.z, 1e-4f);
                }
                else
                {
                    solids++;
                    MeshCollider body = collider as MeshCollider;
                    Assert.IsNotNull(body, $"{collider.name} should be the kicker's MeshCollider");
                    Assert.IsTrue(body.convex, collider.name);
                    Assert.AreEqual(GroundLayer, collider.gameObject.layer, collider.name);
                }
            }

            Assert.AreEqual(1, solids, "solid colliders");
            Assert.AreEqual(1, triggers, "trigger colliders");
        }

        [Test]
        public void Box_IsGroundBoxesWithGrindSurface()
        {
            // Materials holder with every slot null must be tolerated too
            GameObject root = Track(ParkFeatureBuilder.BuildBox(ParkPiece.FunBox, new ParkMaterials()));

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            Assert.Greater(colliders.Length, 0);
            foreach (Collider collider in colliders)
            {
                Assert.IsInstanceOf<BoxCollider>(collider, collider.name);
                Assert.IsFalse(collider.isTrigger, collider.name);
                Assert.AreEqual(GroundLayer, collider.gameObject.layer, collider.name);
            }

            GrindSurface surface = root.GetComponent<GrindSurface>();
            Assert.IsNotNull(surface);
            Assert.AreEqual(RailType.FunBox, surface.Type);
            Assert.AreEqual(3f, surface.DeckStartZ);
            Assert.AreEqual(15f, surface.DeckEndZ);
        }

        [Test]
        public void GrindSurface_Math()
        {
            GameObject root = Track(ParkFeatureBuilder.BuildBox(ParkPiece.FunBox, null));
            root.transform.SetPositionAndRotation(new Vector3(10f, 5f, 20f), Quaternion.Euler(8f, 10f, 0f));
            GrindSurface surface = root.GetComponent<GrindSurface>();

            Vector3 onDeck = root.transform.TransformPoint(new Vector3(0.3f, 0.6f, 9f));
            Assert.IsTrue(surface.IsOnDeck(onDeck));
            Assert.AreEqual(0.5f, surface.GetProgress(onDeck), 1e-3f);
            Assert.AreEqual(0.3f, surface.LateralOffset(onDeck), 1e-3f);

            Vector3 pastEnd = root.transform.TransformPoint(new Vector3(0f, 0.6f, 20f));
            Assert.IsFalse(surface.IsOnDeck(pastEnd));
            Assert.AreEqual(1f, surface.GetProgress(pastEnd), 1e-6f);
        }

        [Test]
        public void Reservation_Contains()
        {
            var reservation = new ParkReservation
            {
                Origin = Vector2.zero,
                Dir = new Vector2(0f, 1f),
                Length = 10f,
                HalfWidthStart = 2f,
                HalfWidthEnd = 6f
            };

            Assert.IsTrue(reservation.Contains(0f, 5f));
            Assert.IsTrue(reservation.Contains(3.9f, 5f));
            Assert.IsFalse(reservation.Contains(4.5f, 5f));
            Assert.IsFalse(reservation.Contains(0f, -1f));
            Assert.IsFalse(reservation.Contains(0f, 11f));
        }
    }
}
