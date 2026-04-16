using System;
using Microsoft.Xna.Framework;

namespace NewProject.World;

public static class WorldGenerator
{
    private const int SeaLevel = 11;

    public static VoxelWorld GenerateChunk(int chunkX, int chunkZ, int chunkSize, int height, int seed)
    {
        VoxelWorld world = new(chunkSize, height, chunkSize);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = chunkX * chunkSize + x;
                int worldZ = chunkZ * chunkSize + z;
                int terrainHeight = GetTerrainHeight(worldX, worldZ, height, seed);
                bool supportsTrees = terrainHeight > SeaLevel + 1;

                for (int y = 0; y < terrainHeight; y++)
                {
                    BlockType block = GetTerrainBlock(y, terrainHeight);

                    world.SetBlock(x, y, z, block);
                }

                for (int y = Math.Max(terrainHeight, 0); y <= SeaLevel && y < height; y++)
                {
                    world.SetBlock(x, y, z, BlockType.Water);
                }

                if (supportsTrees && ShouldPlaceTree(worldX, worldZ, terrainHeight, seed))
                {
                    PlaceTree(world, x, terrainHeight, z, seed);
                }
            }
        }

        return world;
    }

    private static int GetTerrainHeight(int x, int z, int worldHeight, int seed)
    {
        float distance = MathF.Sqrt(x * x + z * z);
        float islandRadius = 120f;
        float falloff = 1f - MathHelper.Clamp(distance / islandRadius, 0f, 1f);
        falloff = falloff * falloff * (3f - 2f * falloff);

        float continental = FractalNoise(x * 0.035f, z * 0.035f, seed, 4, 0.55f);
        float hills = FractalNoise(x * 0.085f, z * 0.085f, seed + 17, 3, 0.5f);
        float ridges = 1f - MathF.Abs(FractalNoise(x * 0.06f, z * 0.06f, seed + 93, 3, 0.5f) * 2f - 1f);
        float shapedNoise = continental * 0.55f + hills * 0.25f + ridges * 0.20f;

        float landShape = MathHelper.Clamp(falloff * (0.75f + shapedNoise * 0.45f), 0f, 1f);
        float baseHeight = MathHelper.Lerp(SeaLevel - 5f, worldHeight - 8f, landShape);
        float height = MathHelper.Lerp(SeaLevel - 3f, baseHeight, falloff);

        if (falloff < 0.22f)
        {
            height = MathHelper.Lerp(height, SeaLevel, 1f - falloff / 0.22f);
        }

        return Math.Clamp((int)MathF.Round(height), 3, worldHeight - 6);
    }

    private static BlockType GetTerrainBlock(int y, int terrainHeight)
    {
        bool beachOrSeafloor = terrainHeight <= SeaLevel + 1;

        if (y == terrainHeight - 1)
        {
            return beachOrSeafloor ? BlockType.Sand : BlockType.Grass;
        }

        if (y >= terrainHeight - 3)
        {
            return beachOrSeafloor ? BlockType.Sand : BlockType.Dirt;
        }

        if (beachOrSeafloor && y >= terrainHeight - 5)
        {
            return BlockType.Sand;
        }

        return BlockType.Stone;
    }

    private static bool ShouldPlaceTree(int x, int z, int terrainHeight, int seed)
    {
        if (terrainHeight <= SeaLevel + 1)
        {
            return false;
        }

        float chance = FractalNoise(x * 0.21f, z * 0.21f, seed + 211, 2, 0.55f);
        float spacing = FractalNoise(x * 0.08f, z * 0.08f, seed + 503, 1, 0.5f);
        return chance > 0.7f && spacing > 0.5f;
    }

    private static void PlaceTree(VoxelWorld world, int baseX, int baseY, int baseZ, int seed)
    {
        int trunkHeight = 4 + (Hash(baseX, baseZ, seed) % 3);

        for (int y = 0; y < trunkHeight && baseY + y < world.Height; y++)
        {
            world.SetBlock(baseX, baseY + y, baseZ, BlockType.Trunk);
        }

        int leafBaseY = baseY + trunkHeight - 2;
        for (int x = -2; x <= 2; x++)
        {
            for (int y = 0; y <= 3; y++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    int distance = Math.Abs(x) + Math.Abs(z) + Math.Abs(y - 1);
                    if (distance > 4)
                    {
                        continue;
                    }

                    int worldX = baseX + x;
                    int worldY = leafBaseY + y;
                    int worldZ = baseZ + z;

                    if (world.GetBlock(worldX, worldY, worldZ) == BlockType.Air)
                    {
                        world.SetBlock(worldX, worldY, worldZ, BlockType.Leaves);
                    }
                }
            }
        }
    }

    private static float FractalNoise(float x, float z, int seed, int octaves, float persistence)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float value = 0f;
        float sum = 0f;

        for (int i = 0; i < octaves; i++)
        {
            value += ValueNoise(x * frequency, z * frequency, seed + i * 31) * amplitude;
            sum += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }

        return value / sum;
    }

    private static float ValueNoise(float x, float z, int seed)
    {
        int x0 = (int)MathF.Floor(x);
        int z0 = (int)MathF.Floor(z);
        int x1 = x0 + 1;
        int z1 = z0 + 1;

        float tx = x - x0;
        float tz = z - z0;
        float sx = tx * tx * (3f - 2f * tx);
        float sz = tz * tz * (3f - 2f * tz);

        float n00 = Hash01(x0, z0, seed);
        float n10 = Hash01(x1, z0, seed);
        float n01 = Hash01(x0, z1, seed);
        float n11 = Hash01(x1, z1, seed);

        float ix0 = MathHelper.Lerp(n00, n10, sx);
        float ix1 = MathHelper.Lerp(n01, n11, sx);
        return MathHelper.Lerp(ix0, ix1, sz);
    }

    private static int Hash(int x, int z, int seed)
    {
        unchecked
        {
            int value = seed;
            value = (value * 397) ^ x;
            value = (value * 397) ^ z;
            value ^= value >> 13;
            value *= 1274126177;
            value ^= value >> 16;
            return value & int.MaxValue;
        }
    }

    private static float Hash01(int x, int z, int seed)
    {
        return (Hash(x, z, seed) % 10000) / 10000f;
    }
}
