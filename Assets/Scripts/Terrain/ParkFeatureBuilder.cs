using System;
using System.Collections.Generic;
using UnityEngine;
using Shredsquatch.Core;
using Shredsquatch.Player;
using Shredsquatch.Tricks;
using Shredsquatch.Procedural;

namespace Shredsquatch.Terrain
{
    public enum ParkPiece
    {
        KickerSmall,
        KickerMedium,
        KickerLarge,
        FunBox,
        FlatBox,
        DownBox
    }

    /// <summary>Materials for park features. Any of them may be null (the renderer keeps its default).</summary>
    public sealed class ParkMaterials
    {
        public Material Kicker;
        public Material FunBox;
        public Material FlatBox;
        public Material DownBox;
        public Material Marker;
    }

    /// <summary>
    /// A chunk-local XZ trapezoid starting at Origin and running Length along Dir (unit length),
    /// widening linearly from HalfWidthStart to HalfWidthEnd. Covers a park feature's body or landing.
    /// </summary>
    public struct ParkReservation
    {
        public Vector2 Origin;
        public Vector2 Dir;
        public float Length;
        public float HalfWidthStart;
        public float HalfWidthEnd;

        public bool Contains(float x, float z)
        {
            float px = x - Origin.x;
            float pz = z - Origin.y;
            float along = px * Dir.x + pz * Dir.y;
            if (along < 0f || along > Length) return false;

            float lateral = Mathf.Abs(px * Dir.y - pz * Dir.x);
            return lateral <= Mathf.Lerp(HalfWidthStart, HalfWidthEnd, Length > 0f ? along / Length : 0f);
        }
    }

    /// <summary>
    /// Builds park features in code: snow kickers, grindable boxes and the trigger zones for
    /// drops and halfpipe walls. Rideable solids go on the Ground layer and triggers on Ignore
    /// Raycast; nothing is tagged, so CrashHandler never treats them as obstacles.
    /// Park lines are planned per chunk from a hash-seeded RNG, so they never touch
    /// TerrainGenerator's shared random stream.
    /// </summary>
    public static class ParkFeatureBuilder
    {
        // Kickers (index = (int)ParkPiece): the root is level and the deck rises at a fixed angle
        private static readonly float[] KickerWidth = { 4f, 5f, 6f };
        private static readonly float[] KickerAngle = { 16f, 20f, 24f };
        private static readonly float[] KickerSlopeLength = { 4.5f, 6.5f, 8.5f };
        private static readonly JumpController.RampType[] KickerRamp =
        {
            JumpController.RampType.SmallBump,
            JumpController.RampType.MediumRamp,
            JumpController.RampType.LargeKicker
        };
        private const float KickerEntryY = -0.10f;    // entry edge buried 0.10 m
        private const float KickerSideSlope = 30f;

        // Boxes: profile segments (z0, y0, z1, y1) in the root frame and the grindable deck range
        private struct BoxProfile
        {
            public RailType Type;
            public float DeckStartZ;
            public float DeckEndZ;
            public Vector4[] Segments;
        }

        private static readonly BoxProfile[] BoxProfiles =
        {
            new BoxProfile
            {
                Type = RailType.FunBox, DeckStartZ = 3f, DeckEndZ = 15f,
                Segments = new[] { new Vector4(0f, -0.10f, 3f, 0.60f), new Vector4(3f, 0.60f, 15f, 0.60f), new Vector4(15f, 0.60f, 18f, -0.10f) }
            },
            new BoxProfile
            {
                Type = RailType.FlatBox, DeckStartZ = 2.5f, DeckEndZ = 16.5f,
                Segments = new[] { new Vector4(0f, -0.10f, 2.5f, 0.80f), new Vector4(2.5f, 0.80f, 16.5f, 0.80f) }
            },
            new BoxProfile
            {
                Type = RailType.DownBox, DeckStartZ = 4f, DeckEndZ = 18f,
                Segments = new[] { new Vector4(0f, -0.10f, 4f, 1.10f), new Vector4(4f, 1.10f, 18f, 0.30f) }
            }
        };
        private const float BoxDeckHalfWidth = 0.8f;
        private const float BoxWingAngle = 25f;       // degrees below horizontal
        private const float MinBoxPitch = 3f;
        private const float MaxBoxPitch = 16f;

        // Line tables (index = (int)ParkPiece; declared after the kicker table they read)
        // Footprint: along-line length. After: room needed past it inside the chunk (a landing
        // at 25 m/s with a full charge). Gap: footprint end to the next entry. BodyHalfWidth
        // includes the kicker flare.
        private static readonly float[] Footprint =
        {
            KickerDeckLength(0), KickerDeckLength(1), KickerDeckLength(2), 18f, 16.5f, 18f
        };
        private static readonly float[] After = { 50f, 62f, 80f, 14f, 14f, 14f };
        private static readonly float[] Gap = { 46f, 58f, 74f, 22f, 22f, 22f };
        private static readonly float[] BodyHalfWidth =
        {
            KickerBodyHalfWidth(0), KickerBodyHalfWidth(1), KickerBodyHalfWidth(2), 4.5f, 4.5f, 4.5f
        };

        // Piece weights per zone tier (Tutorial, Forest, Extreme), index = (int)ParkPiece
        private static readonly float[][] PieceWeights =
        {
            new[] { 0.5f, 0f, 0f, 0.5f, 0f, 0f },
            new[] { 0.2f, 0.3f, 0f, 0.2f, 0.15f, 0.15f },
            new[] { 0f, 0.25f, 0.3f, 0f, 0.2f, 0.25f }
        };

        // Park line layout in chunk-local metres (tuned for 256 m chunks)
        private const float LineStartZ = -112f;
        private const float LineSpanZ = 236f;
        private const float LineSpreadX = 96f;
        private const float LineMaxEndX = 112f;
        private const float LineMaxYaw = 10f;          // degrees either side of the fall line
        private const float LineMinSeparation = 40f;
        private const int LineAttempts = 4;
        private const int LinePieceGuard = 24;
        private const float OnboardingFirstEntry = 70f;
        private const float RetryStep = 12f;
        private const float GapJitter = 8f;
        private const float ChunkEdgeMargin = 4f;

        // Shared, reused across chunks
        private static Mesh _cubeMesh;
        private static readonly Mesh[] _kickerMeshes = new Mesh[3];
        private static readonly List<Vector2> _acceptedLines = new List<Vector2>();
        private static readonly List<ParkReservation> _scratchReservations = new List<ParkReservation>();

        /// <summary>Everything one SpawnChunkFeatures call works with.</summary>
        private struct ChunkContext
        {
            public TerrainChunk Chunk;
            public int Seed;
            public TerrainFeatureSettings Settings;
            public ParkMaterials Materials;
            public List<ParkReservation> Reservations;
            public float Size;
            public double CentreX;     // world position of the chunk origin (its centre)
            public double CentreZ;
            public double MinX;        // world bounds, half-open: [Min, Max)
            public double MaxX;
            public double MinZ;
            public double MaxZ;
        }

        public static int GroundLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("Ground");
                return layer >= 0 ? layer : 8;
            }
        }

        public static int TriggerLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("Ignore Raycast");
                return layer >= 0 ? layer : 2;
            }
        }

        private static Mesh CubeMesh
        {
            get
            {
                if (_cubeMesh == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tmp.SetActive(false);
                    _cubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
                    if (Application.isPlaying) UnityEngine.Object.Destroy(tmp);
                    else UnityEngine.Object.DestroyImmediate(tmp);
                }
                return _cubeMesh;
            }
        }

        public static bool IsKicker(ParkPiece piece)
        {
            return piece <= ParkPiece.KickerLarge;
        }

        private static int KickerIndex(ParkPiece piece)
        {
            return Mathf.Clamp((int)piece, 0, 2);
        }

        private static int BoxIndex(ParkPiece piece)
        {
            return Mathf.Clamp((int)piece - (int)ParkPiece.FunBox, 0, 2);
        }

        public static void GetKickerDims(ParkPiece p, out float width, out float zLip, out float yLip, out float yBottom, out float flare)
        {
            int i = KickerIndex(p);
            float angle = KickerAngle[i] * Mathf.Deg2Rad;
            width = KickerWidth[i];
            zLip = KickerSlopeLength[i] * Mathf.Cos(angle);
            yLip = KickerEntryY + KickerSlopeLength[i] * Mathf.Sin(angle);
            yBottom = -(0.25f * zLip + 0.5f);
            flare = (yLip - yBottom) / Mathf.Tan(KickerSideSlope * Mathf.Deg2Rad);
        }

        private static float KickerDeckLength(int index)
        {
            GetKickerDims((ParkPiece)index, out _, out float zLip, out _, out _, out _);
            return zLip;
        }

        private static float KickerBodyHalfWidth(int index)
        {
            GetKickerDims((ParkPiece)index, out float width, out _, out _, out _, out float flare);
            return width * 0.5f + flare;
        }

        /// <summary>The shared kicker mesh, generated once per size.</summary>
        public static Mesh GetKickerMesh(ParkPiece piece)
        {
            int i = KickerIndex(piece);
            if (_kickerMeshes[i] == null)
            {
                GetKickerDims(piece, out float width, out float zLip, out float yLip, out float yBottom, out _);
                _kickerMeshes[i] = ProceduralMeshGenerator.GenerateKicker(width, zLip, KickerEntryY, yLip, yBottom, KickerSideSlope);
            }
            return _kickerMeshes[i];
        }

        #region Builders

        /// <summary>
        /// A kicker root at the origin, level and unscaled: a convex snow body, a lip stripe and an
        /// auto-launch zone whose origin sits on the lip.
        /// </summary>
        public static GameObject BuildKicker(ParkPiece p, ParkMaterials mats)
        {
            int i = KickerIndex(p);
            GetKickerDims(p, out float width, out float zLip, out float yLip, out _, out _);
            float angle = KickerAngle[i];
            float angleRad = angle * Mathf.Deg2Rad;

            var root = new GameObject("ParkKicker_" + p);
            root.layer = GroundLayer;

            Mesh mesh = GetKickerMesh(p);
            var body = new GameObject("KickerBody");
            body.layer = GroundLayer;
            body.transform.SetParent(root.transform, false);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            var bodyRenderer = body.AddComponent<MeshRenderer>();
            Material kickerMat = mats != null ? mats.Kicker : null;
            if (kickerMat != null) bodyRenderer.sharedMaterial = kickerMat;
            var bodyCollider = body.AddComponent<MeshCollider>();
            bodyCollider.convex = true;
            bodyCollider.sharedMesh = mesh;

            // Marker stripe on the last 0.3 m of the deck
            AddVisualCube(root.transform, "LipStripe",
                new Vector3(0f, yLip - 0.15f * Mathf.Sin(angleRad) + 0.03f, zLip - 0.15f * Mathf.Cos(angleRad)),
                Quaternion.Euler(-angle, 0f, 0f), new Vector3(width, 0.05f, 0.3f), mats != null ? mats.Marker : null);

            // Covers the last 5.5 m of the deck plus 0.5 m past the lip
            var zone = new GameObject("LaunchZone");
            zone.transform.SetParent(root.transform, false);
            zone.transform.localPosition = new Vector3(0f, yLip, zLip);
            zone.transform.localRotation = Quaternion.identity;
            ConfigureZone(zone, KickerRamp[i], true, Constants.Launch.KickerMaxEntryAngle,
                new Vector3(0f, 1.0f, -2.5f), new Vector3(width + 1f, 4.5f, 6f));

            return root;
        }

        /// <summary>
        /// A box root at the origin, unscaled, carrying the GrindSurface: one deck slab per profile
        /// segment, flanked by wings that slope down into the snow so the sides are rideable.
        /// The root must only be pitched and yawed when placed.
        /// </summary>
        public static GameObject BuildBox(ParkPiece p, ParkMaterials mats)
        {
            BoxProfile profile = BoxProfiles[BoxIndex(p)];

            var root = new GameObject("ParkBox_" + p);
            root.layer = GroundLayer;

            GrindSurface surface = root.AddComponent<GrindSurface>();
            surface.Type = profile.Type;
            surface.DeckStartZ = profile.DeckStartZ;
            surface.DeckEndZ = profile.DeckEndZ;
            surface.DeckHalfWidth = BoxDeckHalfWidth;

            Material deckMat = null;
            Material wingMat = null;
            if (mats != null)
            {
                deckMat = profile.Type == RailType.FunBox ? mats.FunBox
                    : profile.Type == RailType.FlatBox ? mats.FlatBox
                    : mats.DownBox;
                wingMat = mats.Kicker;
            }

            float wingCos = Mathf.Cos(BoxWingAngle * Mathf.Deg2Rad);
            float wingSin = Mathf.Sin(BoxWingAngle * Mathf.Deg2Rad);

            for (int i = 0; i < profile.Segments.Length; i++)
            {
                Vector4 segment = profile.Segments[i];
                Vector3 start = new Vector3(0f, segment.y, segment.x);
                Vector3 end = new Vector3(0f, segment.w, segment.z);
                Vector3 along = end - start;
                float length = along.magnitude;
                Vector3 alongDir = along / length;
                float top = Mathf.Max(segment.y, segment.w);

                AddSlab(root.transform, "Deck" + i, new Vector3(-BoxDeckHalfWidth, segment.y, segment.x),
                    along, length, Vector3.right, 2f * BoxDeckHalfWidth, top + 0.5f, deckMat);

                for (int side = 1; side >= -1; side -= 2)
                {
                    // Sized so the wing's outer edge ends below the snow
                    Vector3 down = new Vector3(side * wingCos, -wingSin, 0f);
                    Vector3 across = (down - Vector3.Dot(down, alongDir) * alongDir).normalized;
                    float acrossLength = (top + 0.35f) / Mathf.Abs(across.y) + 0.1f;
                    AddSlab(root.transform, (side > 0 ? "WingR" : "WingL") + i,
                        new Vector3(side * BoxDeckHalfWidth, segment.y, segment.x),
                        along, length, down, acrossLength, 0.3f, wingMat);
                }
            }

            return root;
        }

        private static GameObject AddVisualCube(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = new GameObject(name);
            go.layer = 0;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;

            go.AddComponent<MeshFilter>().sharedMesh = CubeMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            return go;
        }

        /// <summary>
        /// A solid box whose top face starts at topOrigin and spans alongLen by acrossLen
        /// (acrossDir is made perpendicular to alongDir), extending thickness below it.
        /// </summary>
        private static GameObject AddSlab(Transform parent, string name, Vector3 topOrigin, Vector3 alongDir, float alongLen,
            Vector3 acrossDir, float acrossLen, float thickness, Material mat)
        {
            Vector3 along = alongDir.normalized;
            Vector3 across = (acrossDir - Vector3.Dot(acrossDir, along) * along).normalized;
            Vector3 normal = Vector3.Cross(along, across);
            if (normal.y < 0f) normal = -normal;

            var go = new GameObject(name);
            go.layer = GroundLayer;
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.LookRotation(along, normal);
            go.transform.localScale = new Vector3(acrossLen, thickness, alongLen);
            go.transform.localPosition = topOrigin + along * (alongLen * 0.5f) + across * (acrossLen * 0.5f) - normal * (thickness * 0.5f);

            go.AddComponent<MeshFilter>().sharedMesh = CubeMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            go.AddComponent<BoxCollider>();
            return go;
        }

        private static void ConfigureZone(GameObject go, JumpController.RampType ramp, bool autoLaunch, float maxEntryAngle, Vector3 center, Vector3 size)
        {
            go.layer = TriggerLayer;

            RampZone zone = go.AddComponent<RampZone>();
            zone.Ramp = ramp;
            zone.AutoLaunch = autoLaunch;
            zone.LipLocalZ = 0f;
            zone.MaxEntryAngle = maxEntryAngle;
            zone.MinSpeed = Constants.Launch.DefaultMinSpeed;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = center;
            box.size = size;
        }

        #endregion

        #region Chunk Features

        /// <summary>
        /// Build the chunk's drop and halfpipe zones and plan its park lines. Call after the chunk
        /// mesh exists (heights come from SampleHeight). Each park feature adds its chunk-local
        /// footprint to reservations so scattered obstacles can stay clear of it.
        /// </summary>
        public static void SpawnChunkFeatures(TerrainChunk chunk, int seed, TerrainFeatureSettings s, int zoneTier,
            ParkMaterials mats, List<ParkReservation> reservations)
        {
            if (chunk == null || s == null || !s.Enabled) return;
            if (reservations == null)
            {
                reservations = _scratchReservations;
                reservations.Clear();
            }

            double size = chunk.Size;
            double centreX = chunk.ChunkCoord.x * size;
            double centreZ = chunk.ChunkCoord.y * size;
            var ctx = new ChunkContext
            {
                Chunk = chunk,
                Seed = seed,
                Settings = s,
                Materials = mats,
                Reservations = reservations,
                Size = chunk.Size,
                CentreX = centreX,
                CentreZ = centreZ,
                MinX = centreX - size * 0.5,
                MaxX = centreX + size * 0.5,
                MinZ = centreZ - size * 0.5,
                MaxZ = centreZ + size * 0.5
            };

            if (s.Drops) SpawnDropZones(in ctx);
            if (s.Chutes) SpawnHalfpipeZones(in ctx);
            if (s.ParkLines && chunk.ChunkCoord.y >= 1) SpawnParkLines(in ctx, Mathf.Clamp(zoneTier, 0, 2));
        }

        private static void SpawnDropZones(in ChunkContext ctx)
        {
            int iMin = (int)Math.Floor(ctx.MinX / TerrainFeatures.DropCellX);
            int iMax = (int)Math.Floor(ctx.MaxX / TerrainFeatures.DropCellX);
            int jMin = (int)Math.Floor(ctx.MinZ / TerrainFeatures.DropCellZ);
            int jMax = (int)Math.Floor(ctx.MaxZ / TerrainFeatures.DropCellZ);
            Material marker = ctx.Materials != null ? ctx.Materials.Marker : null;

            for (int i = iMin; i <= iMax; i++)
            {
                for (int j = jMin; j <= jMax; j++)
                {
                    if (!TerrainFeatures.TryGetDrop(ctx.Seed, ctx.Settings, i, j, out DropFeature drop)) continue;

                    // Half-open ownership: exactly one chunk builds each drop's zone
                    if (drop.LipX < ctx.MinX || drop.LipX >= ctx.MaxX || drop.LipZ < ctx.MinZ || drop.LipZ >= ctx.MaxZ) continue;

                    float lx = (float)(drop.LipX - ctx.CentreX);
                    float lz = (float)(drop.LipZ - ctx.CentreZ);
                    var go = new GameObject("DropZone");
                    ctx.Chunk.AdoptObject(go, new Vector3(lx, ctx.Chunk.SampleHeight(lx, lz), lz), Quaternion.identity);
                    ConfigureZone(go, JumpController.RampType.CliffJump, true, Constants.Launch.DropMaxEntryAngle,
                        new Vector3(0f, 1.5f, -3f), new Vector3(26f, 6f, 8f));

                    // Poles either side of the lip so the drop reads from the run-up
                    Vector3 poleScale = new Vector3(0.15f, 1.6f, 0.15f);
                    AddVisualCube(go.transform, "MarkerL", new Vector3(-13f, 0.8f, 0f), Quaternion.identity, poleScale, marker);
                    AddVisualCube(go.transform, "MarkerR", new Vector3(13f, 0.8f, 0f), Quaternion.identity, poleScale, marker);
                }
            }
        }

        /// <summary>Button-only HalfpipeLip zones along both upper walls of well-formed chute stretches.</summary>
        private static void SpawnHalfpipeZones(in ChunkContext ctx)
        {
            int slices = Mathf.Max(1, Mathf.FloorToInt(ctx.Size / 32f));
            float sliceLength = ctx.Size / slices;
            int laneMin = TerrainFeatures.LaneOf(ctx.MinX);
            int laneMax = TerrainFeatures.LaneOf(ctx.MaxX);

            for (int lane = laneMin; lane <= laneMax; lane++)
            {
                if (!TerrainFeatures.TryGetChute(ctx.Seed, ctx.Settings, lane, out double jitter, out double phase)) continue;

                for (int k = 0; k < slices; k++)
                {
                    double z = ctx.MinZ + (k + 0.5) * sliceLength;
                    if (TerrainFeatures.ChutePresence(ctx.Seed, lane, z) < 0.6) continue;

                    double centre = TerrainFeatures.ChuteCentre(lane, z, jitter, phase);
                    float yaw = (float)(Math.Atan(TerrainFeatures.ChuteCentreSlope(z, phase)) * 180.0 / Math.PI);

                    for (int side = 1; side >= -1; side -= 2)
                    {
                        double x = centre + side * TerrainFeatures.ChuteZoneOffset;
                        if (x < ctx.MinX || x >= ctx.MaxX) continue;

                        float lx = (float)(x - ctx.CentreX);
                        float lz = (float)(z - ctx.CentreZ);
                        var go = new GameObject("HalfpipeZone");
                        ctx.Chunk.AdoptObject(go, new Vector3(lx, ctx.Chunk.SampleHeight(lx, lz), lz), Quaternion.Euler(0f, yaw, 0f));
                        ConfigureZone(go, JumpController.RampType.HalfpipeLip, false, 90f,
                            new Vector3(0f, 0.5f, 0f), new Vector3(12f, 7f, sliceLength));
                    }
                }
            }
        }

        /// <summary>
        /// One to three line slots (by zone tier), each filled by chance with a line running within
        /// 10 degrees of the fall line through the chunk. Lines start and end at least 40 m apart.
        /// Chunk (0, 1) always gets the onboarding line straight ahead of the spawn.
        /// </summary>
        private static void SpawnParkLines(in ChunkContext ctx, int zoneTier)
        {
            Vector2Int coord = ctx.Chunk.ChunkCoord;
            var rng = new System.Random(unchecked((int)TerrainFeatures.Hash(ctx.Seed, coord.x, coord.y, TerrainFeatures.SaltPark)));
            int slots = zoneTier == 0 ? 1 : (zoneTier == 1 ? 2 : 3);
            _acceptedLines.Clear();

            for (int slot = 0; slot < slots; slot++)
            {
                bool forced = coord.x == 0 && coord.y == 1 && slot == 0;
                if (!forced && rng.NextDouble() >= ctx.Settings.ParkLineChance) continue;

                for (int attempt = 0; attempt < LineAttempts; attempt++)
                {
                    double r1 = rng.NextDouble();
                    double r2 = rng.NextDouble();
                    float x0 = forced ? 0f : (float)(-LineSpreadX + 2f * LineSpreadX * r1);
                    float yaw = forced ? 0f : (float)(-LineMaxYaw + 2f * LineMaxYaw * r2);
                    float yawRad = yaw * Mathf.Deg2Rad;

                    Vector2 dir = new Vector2(Mathf.Sin(yawRad), Mathf.Cos(yawRad));
                    Vector2 right = new Vector2(Mathf.Cos(yawRad), -Mathf.Sin(yawRad));
                    Vector2 start = new Vector2(x0, LineStartZ);
                    float length = LineSpanZ / Mathf.Cos(yawRad);
                    float xEnd = x0 + Mathf.Sin(yawRad) * length;
                    if (Mathf.Abs(xEnd) > LineMaxEndX || CrowdsAcceptedLine(x0, xEnd)) continue;

                    _acceptedLines.Add(new Vector2(x0, xEnd));
                    WalkLine(in ctx, rng, zoneTier, forced, start, dir, right, yaw, length);
                    break;
                }
            }

            _acceptedLines.Clear();
        }

        private static bool CrowdsAcceptedLine(float x0, float xEnd)
        {
            for (int i = 0; i < _acceptedLines.Count; i++)
            {
                Vector2 line = _acceptedLines[i];
                if (Mathf.Abs(x0 - line.x) < LineMinSeparation || Mathf.Abs(xEnd - line.y) < LineMinSeparation) return true;
            }
            return false;
        }

        /// <summary>Place pieces along one line, each clear of other lines and of terrain features.</summary>
        private static void WalkLine(in ChunkContext ctx, System.Random rng, int zoneTier, bool forced,
            Vector2 start, Vector2 dir, Vector2 right, float yaw, float length)
        {
            int lineStart = ctx.Reservations.Count;
            float distance = forced ? OnboardingFirstEntry : (float)(6.0 + 10.0 * rng.NextDouble());
            int forcedIndex = 0;

            for (int guard = 0; guard < LinePieceGuard; guard++)
            {
                // The onboarding line opens with a small kicker, then a fun box
                ParkPiece piece;
                if (forced && forcedIndex < 2)
                {
                    piece = forcedIndex == 0 ? ParkPiece.KickerSmall : ParkPiece.FunBox;
                    forcedIndex++;
                }
                else
                {
                    piece = Pick(zoneTier, rng.NextDouble());
                }

                float foot = Footprint[(int)piece];
                float after = After[(int)piece];
                if (distance + foot + after > length) break;

                Vector2 entry = start + dir * distance;
                bool built = FootprintClear(in ctx, lineStart, piece, entry, dir, right)
                    && (IsKicker(piece)
                        ? TryBuildKicker(in ctx, piece, entry, dir, right, yaw)
                        : TryBuildBox(in ctx, piece, entry, dir, right, yaw));

                if (built) distance += foot + Gap[(int)piece] + (float)(GapJitter * rng.NextDouble());
                else distance += RetryStep;
            }
        }

        private static ParkPiece Pick(int zoneTier, double u)
        {
            float[] weights = PieceWeights[zoneTier];
            double cumulative = 0;
            int last = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f) continue;
                cumulative += weights[i];
                last = i;
                if (u < cumulative) return (ParkPiece)i;
            }
            return (ParkPiece)last;   // only reached through float rounding of the weight sum
        }

        /// <summary>
        /// The piece's body (and, for kickers, its landing) stays inside the chunk, off rollers,
        /// chutes and drops, and out of earlier lines' reservations. Pieces of the same line may
        /// sit in each other's landing fan.
        /// </summary>
        private static bool FootprintClear(in ChunkContext ctx, int lineStart, ParkPiece piece, Vector2 entry, Vector2 dir, Vector2 right)
        {
            float foot = Footprint[(int)piece];
            float after = After[(int)piece];
            float halfWidth = BodyHalfWidth[(int)piece];
            float bound = ctx.Size * 0.5f - ChunkEdgeMargin;

            for (int ti = 0; ti < 6; ti++)
            {
                float t = ti switch
                {
                    0 => -8f,
                    1 => 0f,
                    2 => foot * 0.5f,
                    3 => foot,
                    4 => foot + after * 0.5f,
                    _ => foot + after
                };

                for (int li = -1; li <= 1; li++)
                {
                    Vector2 p = entry + dir * t + right * (li * halfWidth);
                    if (t <= foot && (Mathf.Abs(p.x) > bound || Mathf.Abs(p.y) > bound)) return false;
                    if (TerrainFeatures.BlocksPark(ctx.Seed, ctx.Settings, ctx.CentreX + p.x, ctx.CentreZ + p.y)) return false;

                    for (int r = 0; r < lineStart; r++)
                    {
                        if (ctx.Reservations[r].Contains(p.x, p.y)) return false;
                    }
                }
            }

            return true;
        }

        private static bool TryBuildKicker(in ChunkContext ctx, ParkPiece piece, Vector2 entry, Vector2 dir, Vector2 right, float yaw)
        {
            TerrainChunk chunk = ctx.Chunk;
            GetKickerDims(piece, out float width, out float zLip, out float yLip, out float yBottom, out float flare);
            float halfWidth = width * 0.5f;

            // Seat the entry edge just under the lowest snow across its width
            float h0 = Mathf.Min(Height(chunk, entry),
                Mathf.Min(Height(chunk, entry + right * halfWidth), Height(chunk, entry - right * halfWidth))) - 0.02f;

            // Snow falling away under the lip would leave the bottom floating
            Vector2 lip = entry + dir * zLip;
            float floorY = h0 + yBottom + 0.15f;
            float lipSnow = Height(chunk, lip);
            if (lipSnow < floorY
                || Height(chunk, lip + right * (halfWidth + flare)) < floorY
                || Height(chunk, lip - right * (halfWidth + flare)) < floorY)
            {
                return false;
            }

            // Snow rising toward the lip would swallow it
            if (lipSnow > h0 + yLip - 0.6f) return false;

            GameObject root = BuildKicker(piece, ctx.Materials);
            chunk.AdoptObject(root, new Vector3(entry.x, h0, entry.y), Quaternion.Euler(0f, yaw, 0f));

            int index = (int)piece;
            ctx.Reservations.Add(new ParkReservation
            {
                Origin = entry - dir * 8f,
                Dir = dir,
                Length = 8f + zLip,
                HalfWidthStart = BodyHalfWidth[index] + 2f,
                HalfWidthEnd = BodyHalfWidth[index] + 2f
            });
            ctx.Reservations.Add(new ParkReservation
            {
                Origin = lip,
                Dir = dir,
                Length = After[index],
                HalfWidthStart = halfWidth + 3f,
                HalfWidthEnd = halfWidth + 3f + 0.6f * After[index]
            });
            return true;
        }

        private static bool TryBuildBox(in ChunkContext ctx, ParkPiece piece, Vector2 entry, Vector2 dir, Vector2 right, float yaw)
        {
            TerrainChunk chunk = ctx.Chunk;
            float length = Footprint[(int)piece];
            Vector2 end = entry + dir * length;
            float hEntry = Height(chunk, entry);
            float hEnd = Height(chunk, end);

            // Boxes follow the fall line: downhill, but not steeply
            float pitch = Mathf.Atan2(hEntry - hEnd, length) * Mathf.Rad2Deg;
            if (pitch < MinBoxPitch || pitch > MaxBoxPitch) return false;

            // The snow must follow the chord along the box and be level across both ends
            for (int i = 1; i <= 3; i++)
            {
                float t = i * 0.25f;
                if (Mathf.Abs(Height(chunk, entry + dir * (length * t)) - (hEntry + (hEnd - hEntry) * t)) > 0.15f) return false;
            }
            if (Mathf.Abs(Height(chunk, entry + right * 1.6f) - Height(chunk, entry - right * 1.6f)) > 0.3f
                || Mathf.Abs(Height(chunk, end + right * 1.6f) - Height(chunk, end - right * 1.6f)) > 0.3f)
            {
                return false;
            }

            // Positive Euler X tips +Z down; the pitched axis lies on the sampled chord, so the
            // deck heights above the snow hold along the whole box
            GameObject root = BuildBox(piece, ctx.Materials);
            chunk.AdoptObject(root, new Vector3(entry.x, hEntry, entry.y), Quaternion.Euler(pitch, yaw, 0f));

            ctx.Reservations.Add(new ParkReservation
            {
                Origin = entry - dir * 8f,
                Dir = dir,
                Length = 8f + length + 14f,
                HalfWidthStart = 6f,
                HalfWidthEnd = 6f
            });
            return true;
        }

        private static float Height(TerrainChunk chunk, Vector2 local)
        {
            return chunk.SampleHeight(local.x, local.y);
        }

        #endregion
    }
}
