using UnityEngine;

namespace Shredsquatch.Terrain
{
    public static class NoiseGenerator
    {
        public static float[,] GenerateNoiseMap(
            int width,
            int height,
            int seed,
            float scale,
            int octaves,
            float persistence,
            float lacunarity,
            Vector2 offset)
        {
            float[,] noiseMap = new float[width, height];

            System.Random prng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];

            for (int i = 0; i < octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + offset.x;
                float offsetY = prng.Next(-100000, 100000) + offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            if (scale <= 0) scale = 0.0001f;

            // Pre-calculate the theoretical max amplitude across all octaves so that
            // normalization uses a global range instead of per-chunk min/max.
            // Per-chunk normalization causes visible height seams at chunk borders because
            // adjacent chunks have different local min/max ranges.
            float theoreticalMaxHeight = 0f;
            {
                float amp = 1f;
                for (int i = 0; i < octaves; i++)
                {
                    theoreticalMaxHeight += amp;
                    amp *= persistence;
                }
            }

            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                        float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    noiseMap[x, y] = noiseHeight;
                }
            }

            // Normalize using theoretical amplitude range so all chunks share the same
            // mapping from noise values to [0,1]. This prevents height discontinuities
            // at chunk borders.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(-theoreticalMaxHeight, theoreticalMaxHeight, noiseMap[x, y]);
                }
            }

            return noiseMap;
        }

        public static float GetSimplexNoise(float x, float y, int seed)
        {
            // Offset by seed
            x += seed * 0.1f;
            y += seed * 0.1f;
            return Mathf.PerlinNoise(x, y);
        }

        public static float GetRidgedNoise(float x, float y, int seed, int octaves = 4)
        {
            float sum = 0;
            float amplitude = 1;
            float frequency = 1;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                float noise = 1 - Mathf.Abs(Mathf.PerlinNoise(
                    (x + seed) * frequency,
                    (y + seed) * frequency
                ) * 2 - 1);

                sum += noise * amplitude;
                maxValue += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return sum / maxValue;
        }
    }
}
