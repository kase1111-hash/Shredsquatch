using System;
using NUnit.Framework;
using Shredsquatch.Terrain;

namespace Shredsquatch.Tests.Editor
{
    /// <summary>
    /// TerrainFeatures is pure math over world XZ, so these run without a scene.
    /// </summary>
    [TestFixture]
    public class TerrainFeaturesTests
    {
        private const double BaseGrade = 0.15;   // TerrainGenerator._slopeBias in the scene

        private static TerrainFeatureSettings DefaultSettings() => new TerrainFeatureSettings();

        [Test]
        public void HeightOffset_IsZeroNearSpawn()
        {
            var s = DefaultSettings();
            for (double x = -2000; x <= 2000; x += 25)
            {
                for (double z = -500; z <= 319; z += 7)
                {
                    Assert.AreEqual(0.0, TerrainFeatures.HeightOffset(42, s, x, z), $"height at ({x}, {z})");
                    Assert.IsFalse(TerrainFeatures.IsReserved(42, s, x, z), $"reserved at ({x}, {z})");
                    Assert.IsFalse(TerrainFeatures.BlocksPark(42, s, x, z), $"blocks park at ({x}, {z})");
                }
            }
        }

        [Test]
        public void HeightOffset_IsDeterministic()
        {
            var s = DefaultSettings();
            for (int i = 0; i < 1000; i++)
            {
                double x = (TerrainFeatures.U01(7, i, 0, 1) - 0.5) * 4000;
                double z = TerrainFeatures.U01(7, i, 0, 2) * 9000;
                double a = TerrainFeatures.HeightOffset(42, s, x, z);
                double b = TerrainFeatures.HeightOffset(42, s, x, z);
                Assert.AreEqual(BitConverter.DoubleToInt64Bits(a), BitConverter.DoubleToInt64Bits(b), $"at ({x}, {z})");
            }
        }

        [Test]
        public void Disabled_GivesZero()
        {
            var s = DefaultSettings();
            s.Enabled = false;
            for (double z = 0; z < 8000; z += 37)
            {
                for (double x = -1000; x < 1000; x += 41)
                {
                    Assert.AreEqual(0.0, TerrainFeatures.HeightOffset(42, s, x, z));
                    Assert.IsFalse(TerrainFeatures.IsReserved(42, s, x, z));
                    Assert.IsFalse(TerrainFeatures.BlocksPark(42, s, x, z));
                }
            }
        }

        [Test]
        public void Slopes_StayRideable()
        {
            // The rider's CharacterController treats anything over 45 degrees as a wall;
            // features (on top of the base grade) must stay well under that and never climb much.
            var s = DefaultSettings();
            int[] seeds = { 1, 42, 2026 };
            foreach (int seed in seeds)
            {
                for (double x = -1200; x < 1200; x += 10)
                {
                    for (double z = 0; z < 8000; z += 10)
                    {
                        double gx = (TerrainFeatures.HeightOffset(seed, s, x + 0.5, z) - TerrainFeatures.HeightOffset(seed, s, x - 0.5, z));
                        double gz = (TerrainFeatures.HeightOffset(seed, s, x, z + 0.5) - TerrainFeatures.HeightOffset(seed, s, x, z - 0.5));
                        double downhillGz = gz - BaseGrade;
                        double slope = Math.Sqrt(gx * gx + downhillGz * downhillGz);
                        Assert.Less(slope, 0.75, $"seed {seed} slope at ({x}, {z})");
                        Assert.Less(downhillGz, 0.06, $"seed {seed} uphill grade at ({x}, {z})");
                    }
                }
            }
        }

        [Test]
        public void Drops_StayInsideTheirCell()
        {
            var s = DefaultSettings();
            int found = 0;
            for (int i = -20; i <= 20; i++)
            {
                for (int j = 0; j <= 40; j++)
                {
                    if (!TerrainFeatures.TryGetDrop(42, s, i, j, out DropFeature d)) continue;
                    found++;
                    double cellX = TerrainFeatures.DropCellX;
                    double cellZ = TerrainFeatures.DropCellZ;
                    Assert.GreaterOrEqual(d.LipX - 34, i * cellX + 8 - 1e-6);
                    Assert.LessOrEqual(d.LipX + 34, (i + 1) * cellX - 8 + 1e-6);
                    Assert.GreaterOrEqual(d.LipZ - (d.RunUp + d.Table + 16), j * cellZ + 8 - 1e-6);
                    Assert.LessOrEqual(d.LipZ + d.Landing + 16, (j + 1) * cellZ - 8 + 1e-6);
                }
            }
            Assert.Greater(found, 0, "no drops generated at all");
        }

        [Test]
        public void Chutes_StayInsideTheirLane()
        {
            var s = DefaultSettings();
            for (int lane = -50; lane <= 50; lane++)
            {
                TerrainFeatures.TryGetChute(42, s, lane, out double jitter, out double phase);
                for (double z = 0; z < 8000; z += 50)
                {
                    double centre = TerrainFeatures.ChuteCentre(lane, z, jitter, phase);
                    Assert.LessOrEqual(Math.Abs(centre - lane * TerrainFeatures.LanePitch), 100.0, $"lane {lane} at z {z}");
                }
            }
        }
    }
}
