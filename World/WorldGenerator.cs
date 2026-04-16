using System;
using Microsoft.Xna.Framework;

namespace NewProject.World;

public static class WorldGenerator
{
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

                for (int y = 0; y < terrainHeight; y++)
                {
                    BlockType block = y switch
                    {
                        var _ when y == terrainHeight - 1 => BlockType.Grass,
                        var _ when y >= terrainHeight - 4 => BlockType.Dirt,
                        _ => BlockType.Stone
                    };

                    world.SetBlock(x, y, z, block);
                }

                if (ShouldPlaceTree(worldX, worldZ, terrainHeight, seed))
                {
                    PlaceTree(world, x, terrainHeight, z, seed);
                }
            }
        }

        return world;
    }

    private static int GetTerrainHeight(int x, int z, int worldHeight, int seed)
    {
        float continental = FractalNoise(x * 0.045f, z * 0.045f, seed, 4, 0.55f);
        float detail = FractalNoise(x * 0.12f, z * 0.12f, seed + 17, 3, 0.5f);
        float ridges = 1f - MathF.Abs(FractalNoise(x * 0.07f, z * 0.07f, seed + 93, 3, 0.5f) * 2f - 1f);

        float shaped = continental * 0.65f + detail * 0.2f + ridges * 0.15f;
        int height = (int)MathF.Round(MathHelper.Lerp(6f, worldHeight - 9f, shaped));
        return Math.Clamp(height, 4, worldHeight - 6);
    }

    private static bool ShouldPlaceTree(int x, int z, int terrainHeight, int seed)
    {
        if (terrainHeight < 8)
        {
            return false;
        }

        float chance = FractalNoise(x * 0.21f, z * 0.21f, seed + 211, 2, 0.55f);
        float spacing = FractalNoise(x * 0.08f, z * 0.08f, seed + 503, 1, 0.5f);
        return chance > 0.72f && spacing > 0.48f;
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
