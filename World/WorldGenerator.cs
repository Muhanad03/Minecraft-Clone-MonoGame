using System;
using Microsoft.Xna.Framework;

namespace NewProject.World;

public static class WorldGenerator
{
    private const int SeaLevel = 13;
    private const int IslandCellSize = 300;

    public static VoxelWorld GenerateChunk(int chunkX, int chunkZ, int chunkSize, int height, int seed)
    {
        VoxelWorld world = new(chunkSize, height, chunkSize);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = chunkX * chunkSize + x;
                int worldZ = chunkZ * chunkSize + z;
                TerrainColumn column = GetTerrainColumn(worldX, worldZ, height, seed);
                int terrainHeight = column.TerrainHeight;
                int waterLevel = column.WaterLevel;
                bool supportsTrees = terrainHeight > waterLevel + 2 && !column.IsLake;

                for (int y = 0; y < terrainHeight; y++)
                {
                    BlockType block = GetTerrainBlock(y, column);

                    world.SetBlock(x, y, z, block);
                }

                for (int y = Math.Max(terrainHeight, 0); y <= waterLevel && y < height; y++)
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

    private static TerrainColumn GetTerrainColumn(int x, int z, int worldHeight, int seed)
    {
        IslandSample island = SampleIsland(x, z, seed);
        float edge = island.Influence;
        float smoothEdge = edge * edge * (3f - 2f * edge);
        float continental = FractalNoise(x * 0.012f, z * 0.012f, seed + 11, 4, 0.55f);
        float hills = FractalNoise(x * 0.046f, z * 0.046f, seed + 37, 4, 0.52f);
        float roughness = FractalNoise(x * 0.095f, z * 0.095f, seed + 79, 3, 0.48f);
        float ridges = 1f - MathF.Abs(FractalNoise(x * 0.028f, z * 0.028f, seed + 131, 4, 0.5f) * 2f - 1f);
        ridges *= ridges;

        float shoreline = MathHelper.Clamp(edge / 0.26f, 0f, 1f);
        float baseHeight = MathHelper.Lerp(SeaLevel - 5.5f, SeaLevel + 7.5f, smoothEdge);
        float hillHeight = (hills - 0.38f) * 12.5f * smoothEdge;
        float mountainMask = MathHelper.Clamp((smoothEdge - 0.54f) / 0.40f, 0f, 1f);
        float mountainHeight = ridges * 23f * mountainMask;
        float detail = (roughness - 0.5f) * 4.5f * shoreline;
        float height = baseHeight + hillHeight + mountainHeight + detail + continental * 3f;

        float lakeNoise = FractalNoise(x * 0.018f, z * 0.018f, seed + 503, 3, 0.55f);
        float lakeShape = FractalNoise(x * 0.035f, z * 0.035f, seed + 911, 2, 0.5f);
        bool lakeCandidate = edge > 0.52f && edge < 0.88f && mountainMask < 0.18f && hills < 0.50f && lakeNoise > 0.72f && lakeShape > 0.64f;
        int waterLevel = SeaLevel;

        if (lakeCandidate)
        {
            waterLevel = SeaLevel + 1;
            height = MathHelper.Min(height, waterLevel - 2.0f + (roughness - 0.5f) * 0.8f);
        }

        if (edge < 0.18f)
        {
            height = MathHelper.Lerp(SeaLevel - 8f, height, shoreline);
        }

        int terrainHeight = Math.Clamp((int)MathF.Round(height), 3, worldHeight - 6);
        bool isLake = lakeCandidate && terrainHeight <= waterLevel;
        return new TerrainColumn(terrainHeight, waterLevel, isLake, edge);
    }

    private static BlockType GetTerrainBlock(int y, TerrainColumn column)
    {
        bool beachOrSeafloor = column.TerrainHeight <= column.WaterLevel + 2;
        bool mountain = column.TerrainHeight >= SeaLevel + 22;

        if (y == column.TerrainHeight - 1)
        {
            if (beachOrSeafloor || column.IsLake)
            {
                return BlockType.Sand;
            }

            return mountain ? BlockType.Stone : BlockType.Grass;
        }

        if (y >= column.TerrainHeight - 3)
        {
            return beachOrSeafloor ? BlockType.Sand : BlockType.Dirt;
        }

        if (beachOrSeafloor && y >= column.TerrainHeight - 6)
        {
            return BlockType.Sand;
        }

        return BlockType.Stone;
    }

    private static bool ShouldPlaceTree(int x, int z, int terrainHeight, int seed)
    {
        if (terrainHeight <= SeaLevel + 2 || terrainHeight >= SeaLevel + 22)
        {
            return false;
        }

        float chance = FractalNoise(x * 0.21f, z * 0.21f, seed + 211, 2, 0.55f);
        float spacing = FractalNoise(x * 0.08f, z * 0.08f, seed + 503, 1, 0.5f);
        return chance > 0.7f && spacing > 0.5f;
    }

    private static IslandSample SampleIsland(int x, int z, int seed)
    {
        int cellX = FloorDiv(x, IslandCellSize);
        int cellZ = FloorDiv(z, IslandCellSize);
        float best = 0f;
        int bestCellX = cellX;
        int bestCellZ = cellZ;

        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = cellX + dx;
                int cz = cellZ + dz;
                GetIslandCenter(cx, cz, seed, out float centerX, out float centerZ, out float radius);
                float distance = MathF.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                float angle = MathF.Atan2(z - centerZ, x - centerX);
                float wobble =
                    MathF.Sin(angle * 3.0f + Hash01(cx, cz, seed + 41) * MathHelper.TwoPi) * 0.08f +
                    MathF.Sin(angle * 7.0f + Hash01(cx, cz, seed + 97) * MathHelper.TwoPi) * 0.05f;
                float shapedRadius = radius * (1f + wobble);
                float influence = 1f - MathHelper.Clamp(distance / shapedRadius, 0f, 1f);

                if (influence > best)
                {
                    best = influence;
                    bestCellX = cx;
                    bestCellZ = cz;
                }
            }
        }

        return new IslandSample(best, bestCellX, bestCellZ);
    }

    private static void GetIslandCenter(int cellX, int cellZ, int seed, out float x, out float z, out float radius)
    {
        if (cellX == 0 && cellZ == 0)
        {
            x = 0f;
            z = 0f;
            radius = 176f;
            return;
        }

        float jitterX = Hash01(cellX, cellZ, seed + 17) - 0.5f;
        float jitterZ = Hash01(cellX, cellZ, seed + 23) - 0.5f;
        x = cellX * IslandCellSize + IslandCellSize * 0.5f + jitterX * 86f;
        z = cellZ * IslandCellSize + IslandCellSize * 0.5f + jitterZ * 86f;
        radius = 150f + Hash01(cellX, cellZ, seed + 31) * 64f;
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

    private static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;
        if (remainder != 0 && ((remainder < 0) ^ (divisor < 0)))
        {
            quotient--;
        }

        return quotient;
    }

    private readonly record struct TerrainColumn(int TerrainHeight, int WaterLevel, bool IsLake, float IslandInfluence);

    private readonly record struct IslandSample(float Influence, int CellX, int CellZ);
}
