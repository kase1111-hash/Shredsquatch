using System;

namespace Shredsquatch.Terrain
{
    /// <summary>
    /// Which terrain features TerrainGenerator shapes into the mountain. Geometry is fixed in
    /// TerrainFeatures (the slope limits depend on it); only presence and frequency are tunable.
    /// </summary>
    [Serializable]
    public class TerrainFeatureSettings
    {
        public bool Enabled = true;
        public bool Rollers = true;
        public bool Chutes = true;
        public bool Drops = true;
        public bool ParkLines = true;
        public float ChuteLaneChance = 0.55f;
        public float DropChanceForest = 0.35f;
        public float DropChanceExtreme = 0.5f;
        public float ParkLineChance = 0.7f;
    }

    /// <summary>A cornice drop: run-up, flat table, knuckle at the lip, steep landing.</summary>
    public struct DropFeature
    {
        public double LipX;
        public double LipZ;
        public double Height;
        public double RunUp;
        public double Table;
        public double Landing;
    }

    /// <summary>
    /// Terrain shape features as pure functions of world XZ (plus seed and settings), so every
    /// chunk computes identical heights along shared edges and regenerated chunks match exactly.
    /// No UnityEngine dependency: nothing here reads chunk indices, RNG state or scene objects.
    ///
    /// Features:
    ///  - Rollers: ~1 m whoops across the fall line in noise-selected fields.
    ///  - Chutes: meandering halfpipe channels in some 288 m lanes, 4.5-6.5 m deep.
    ///  - Drops: cornice tables (2 km+) that end in a knuckle and a steep landing.
    /// Everything fades in between StartZ and FullZ, so the spawn area is untouched.
    /// </summary>
    public static class TerrainFeatures
    {
        // Fade-in from the spawn
        public const double StartZ = 320;
        public const double FullZ = 512;

        // Rollers
        public const double RollerWavelength = 60;
        public const double RollerNoiseScale = 420;
        public const double RollerBend = 12;
        public const double RollerBendWavelength = 230;

        // Chutes
        public const double LanePitch = 288;
        public const double ChuteHalfWidth = 16;
        public const double ChuteMeander = 10;
        public const double ChuteMeanderWavelength = 640;
        public const double ChuteJitter = 90;
        public const double ChutePresenceScale = 448;
        public const double ChuteZoneOffset = 11;

        // Drops
        public const double DropCellX = 224;
        public const double DropCellZ = 320;
        public const double DropCore = 12;
        public const double DropShoulder = 22;
        public const double DropRunout = 70;

        // Hash salts
        public const int SaltRoller = 0xB0;
        public const int SaltLaneOn = 21;
        public const int SaltLaneJitter = 22;
        public const int SaltLanePhase = 23;
        public const int SaltLanePresence = 24;
        public const int SaltDropOn = 31;
        public const int SaltDropX = 32;
        public const int SaltDropZ = 33;
        public const int SaltPark = 0x5A17;

        private const double ForestStartZ = 2000;
        private const double ExtremeStartZ = 5000;

        public static uint Hash(int seed, int a, int b, int salt)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)a * 0x85EBCA77u;
                h = ((h << 13) | (h >> 19)) * 0xC2B2AE3Du;
                h ^= (uint)b * 0x27D4EB2Fu;
                h = ((h << 13) | (h >> 19)) * 0x165667B1u;
                h ^= (uint)salt * 0x9E3779B1u;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                h *= 0xC2B2AE35u;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>Uniform value in [0, 1) from the hash.</summary>
        public static double U01(int seed, int a, int b, int salt)
        {
            return (Hash(seed, a, b, salt) >> 8) * (1.0 / 16777216.0);
        }

        public static double Smooth(double e0, double e1, double t)
        {
            double k = (t - e0) / (e1 - e0);
            k = k < 0 ? 0 : (k > 1 ? 1 : k);
            return k * k * (3 - 2 * k);
        }

        /// <summary>Smoothstep-interpolated value noise, C1, in [0, 1).</summary>
        public static double ValueNoise(int seed, double x, double z, int salt)
        {
            int ix = (int)Math.Floor(x);
            int iz = (int)Math.Floor(z);
            double fx = x - ix;
            double fz = z - iz;
            double sx = fx * fx * (3 - 2 * fx);
            double sz = fz * fz * (3 - 2 * fz);

            double a = U01(seed, ix, iz, salt);
            double b = U01(seed, ix + 1, iz, salt);
            double c = U01(seed, ix, iz + 1, salt);
            double d = U01(seed, ix + 1, iz + 1, salt);

            double near = a + (b - a) * sx;
            double far = c + (d - c) * sx;
            return near + (far - near) * sz;
        }

        public static double Fade(double z)
        {
            return Smooth(StartZ, FullZ, z);
        }

        private static double ForestBlend(double z)
        {
            return Smooth(1800, 2200, z);
        }

        private static double ExtremeBlend(double z)
        {
            return Smooth(4800, 5200, z);
        }

        public static int LaneOf(double wx)
        {
            return (int)Math.Floor((wx + LanePitch * 0.5) / LanePitch);
        }

        public static bool TryGetChute(int seed, TerrainFeatureSettings s, int lane, out double jitter, out double phase)
        {
            jitter = (2 * U01(seed, lane, 0, SaltLaneJitter) - 1) * ChuteJitter;
            phase = 2 * Math.PI * U01(seed, lane, 0, SaltLanePhase);
            return s != null && s.Enabled && s.Chutes && U01(seed, lane, 0, SaltLaneOn) < s.ChuteLaneChance;
        }

        public static double ChuteCentre(int lane, double wz, double jitter, double phase)
        {
            return lane * LanePitch + jitter + ChuteMeander * Math.Sin(2 * Math.PI * wz / ChuteMeanderWavelength + phase);
        }

        /// <summary>d(ChuteCentre)/dz: the chute's sideways drift per metre downhill.</summary>
        public static double ChuteCentreSlope(double wz, double phase)
        {
            return ChuteMeander * 2 * Math.PI / ChuteMeanderWavelength * Math.Cos(2 * Math.PI * wz / ChuteMeanderWavelength + phase);
        }

        /// <summary>How strongly the lane's chute exists at this Z (0-1, includes the spawn fade).</summary>
        public static double ChutePresence(int seed, int lane, double wz)
        {
            double t = wz / ChutePresenceScale;
            int k = (int)Math.Floor(t);
            double v0 = U01(seed, lane, k, SaltLanePresence);
            double v1 = U01(seed, lane, k + 1, SaltLanePresence);
            return Fade(wz) * Smooth(0.35, 0.65, v0 + (v1 - v0) * Smooth(0, 1, t - k));
        }

        public static double RollerWeight(int seed, double wx, double wz)
        {
            return Fade(wz) * Smooth(0.55, 0.72, ValueNoise(seed, wx / RollerNoiseScale + 0.37, wz / RollerNoiseScale + 0.61, SaltRoller));
        }

        /// <summary>
        /// The drop owned by cell (i, j), if any. Its whole footprint (and mask) lies inside the cell.
        /// </summary>
        public static bool TryGetDrop(int seed, TerrainFeatureSettings s, int i, int j, out DropFeature d)
        {
            d = default;
            if (s == null || !s.Enabled || !s.Drops) return false;

            double cz = (j + 0.5) * DropCellZ;
            double p = cz < ForestStartZ ? 0 : (cz < ExtremeStartZ ? s.DropChanceForest : s.DropChanceExtreme);
            if (p <= 0 || U01(seed, i, j, SaltDropOn) >= p) return false;

            bool big = cz >= ExtremeStartZ;
            d.Height = big ? 7 : 5;
            d.RunUp = big ? 100 : 72;
            d.Table = 10;
            d.Landing = big ? 28 : 22;

            d.LipX = i * DropCellX + DropCellX * 0.5 + (2 * U01(seed, i, j, SaltDropX) - 1) * 70;

            double zMin = j * DropCellZ + 8 + d.RunUp + d.Table + 16;
            double zMax = (j + 1) * DropCellZ - 8 - d.Landing - 16;
            d.LipZ = zMin + U01(seed, i, j, SaltDropZ) * (zMax - zMin);

            if (d.LipZ - d.RunUp - d.Table < FullZ) return false;

            // Keep drops clear of chutes in either neighbouring lane
            for (int side = -1; side <= 1; side += 2)
            {
                int lane = LaneOf(d.LipX + side * 60);
                if (TryGetChute(seed, s, lane, out double jitter, out _)
                    && Math.Abs(d.LipX - (lane * LanePitch + jitter)) < 84)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Feature height in metres to add to the base terrain at world (wx, wz).</summary>
        public static double HeightOffset(int seed, TerrainFeatureSettings s, double wx, double wz)
        {
            if (s == null || !s.Enabled) return 0;
            double g = Fade(wz);
            if (g <= 0) return 0;

            double chute = 0;
            double chuteMask = 0;
            if (s.Chutes)
            {
                int lane = LaneOf(wx);
                if (TryGetChute(seed, s, lane, out double jitter, out double phase))
                {
                    double d = Math.Abs(wx - ChuteCentre(lane, wz, jitter, phase));
                    double presence = ChutePresence(seed, lane, wz);
                    if (presence > 0)
                    {
                        double depth = 4.5 + 1.5 * ForestBlend(wz) + 0.5 * ExtremeBlend(wz);
                        double profile = d < ChuteHalfWidth ? 0.5 * (1 + Math.Cos(Math.PI * d / ChuteHalfWidth)) : 0;
                        chute = -depth * presence * profile;
                        chuteMask = presence * (1 - Smooth(ChuteHalfWidth, 2 * ChuteHalfWidth, d));
                    }
                }
            }

            double drop = 0;
            double dropMask = 0;
            if (s.Drops)
            {
                int i = (int)Math.Floor(wx / DropCellX);
                int j = (int)Math.Floor(wz / DropCellZ);
                if (TryGetDrop(seed, s, i, j, out DropFeature f))
                {
                    double u = wz - f.LipZ;
                    double dx = Math.Abs(wx - f.LipX);

                    double along;
                    if (u < -(f.RunUp + f.Table)) along = 0;
                    else if (u < -f.Table) along = Smooth(0, 1, (u + f.RunUp + f.Table) / f.RunUp);
                    else if (u < 0) along = 1;
                    else if (u < f.Landing) along = 1 - Smooth(0, 1, u / f.Landing);
                    else along = 0;

                    double across;
                    if (dx <= DropCore) across = 1;
                    else if (dx < DropCore + DropShoulder) across = 0.5 * (1 + Math.Cos(Math.PI * (dx - DropCore) / DropShoulder));
                    else across = 0;

                    drop = f.Height * along * across;
                    dropMask = across
                        * Smooth(-(f.RunUp + f.Table + 16), -(f.RunUp + f.Table), u)
                        * (1 - Smooth(f.Landing, f.Landing + 16, u));
                }
            }

            double roll = 0;
            if (s.Rollers)
            {
                double amplitude = 1.0 + 0.25 * ForestBlend(wz) + 0.35 * ExtremeBlend(wz);
                double weight = Smooth(0.55, 0.72, ValueNoise(seed, wx / RollerNoiseScale + 0.37, wz / RollerNoiseScale + 0.61, SaltRoller));
                double phaseZ = wz + RollerBend * Math.Sin(2 * Math.PI * wx / RollerBendWavelength);
                roll = amplitude * g * weight * (1 - chuteMask) * (1 - dropMask) * Math.Sin(2 * Math.PI * phaseZ / RollerWavelength);
            }

            return roll + chute + drop;
        }

        /// <summary>
        /// True where scattered obstacles (trees, rocks, coins, rails) must not spawn:
        /// chute floors and drop run-ups/landings.
        /// </summary>
        public static bool IsReserved(int seed, TerrainFeatureSettings s, double wx, double wz)
        {
            if (s == null || !s.Enabled) return false;

            int lane = LaneOf(wx);
            if (TryGetChute(seed, s, lane, out double jitter, out double phase)
                && ChutePresence(seed, lane, wz) > 0.05
                && Math.Abs(wx - ChuteCentre(lane, wz, jitter, phase)) < ChuteHalfWidth + 4)
            {
                return true;
            }

            int i = (int)Math.Floor(wx / DropCellX);
            int j = (int)Math.Floor(wz / DropCellZ);
            for (int jj = j; jj >= j - 1; jj--)
            {
                if (!TryGetDrop(seed, s, i, jj, out DropFeature f)) continue;
                double u = wz - f.LipZ;
                double dx = Math.Abs(wx - f.LipX);
                if (dx < DropCore + DropShoulder && u >= -(f.RunUp + f.Table) && u <= f.Landing + DropRunout)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True where park lines must not go: rollers, chutes and drops (with margins).</summary>
        public static bool BlocksPark(int seed, TerrainFeatureSettings s, double wx, double wz)
        {
            if (s == null || !s.Enabled) return false;

            if (s.Rollers && RollerWeight(seed, wx, wz) > 0.05) return true;

            int lane = LaneOf(wx);
            if (TryGetChute(seed, s, lane, out double jitter, out double phase)
                && ChutePresence(seed, lane, wz) > 0.05
                && Math.Abs(wx - ChuteCentre(lane, wz, jitter, phase)) < ChuteHalfWidth + 24)
            {
                return true;
            }

            int i = (int)Math.Floor(wx / DropCellX);
            int j = (int)Math.Floor(wz / DropCellZ);
            for (int jj = j; jj >= j - 1; jj--)
            {
                if (!TryGetDrop(seed, s, i, jj, out DropFeature f)) continue;
                double u = wz - f.LipZ;
                double dx = Math.Abs(wx - f.LipX);
                if (dx < DropCore + DropShoulder + 8 && u > -(f.RunUp + f.Table + 16) && u < f.Landing + DropRunout)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
