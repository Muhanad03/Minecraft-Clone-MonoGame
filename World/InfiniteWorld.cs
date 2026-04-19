using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace NewProject.World;

public sealed class InfiniteWorld : IBlockWorld
{
    public const int ChunkSize = 16;

    private readonly Dictionary<Point, WorldChunk> _chunks = new();
    private readonly int _seed;

    public InfiniteWorld(int height, int seed)
    {
        Height = height;
        _seed = seed;
    }

    public int Height { get; }

    public WorldChunk[] GetLoadedChunksSnapshot()
    {
        return _chunks.Values.ToArray();
    }

    public bool TryGetLoadedChunk(Point chunkKey, out WorldChunk chunk)
    {
        return _chunks.TryGetValue(chunkKey, out chunk);
    }

    public bool TryGetLoadedChunk(int chunkX, int chunkZ, out WorldChunk chunk)
    {
        return _chunks.TryGetValue(new Point(chunkX, chunkZ), out chunk);
    }

    public void EnsureChunksAround(Vector3 worldPosition, int chunkRadius)
    {
        Point center = GetChunkCoordinate(worldPosition);
        EnsureChunksAround(center.X, center.Y, chunkRadius);
    }

    public void EnsureChunksAround(int centerChunkX, int centerChunkZ, int chunkRadius)
    {
        for (int chunkZ = centerChunkZ - chunkRadius; chunkZ <= centerChunkZ + chunkRadius; chunkZ++)
        {
            for (int chunkX = centerChunkX - chunkRadius; chunkX <= centerChunkX + chunkRadius; chunkX++)
            {
                GetOrCreateChunk(chunkX, chunkZ);
            }
        }
    }

    public void TrimChunksOutside(int centerChunkX, int centerChunkZ, int keepRadius)
    {
        List<Point> toRemove = new();

        foreach (Point key in _chunks.Keys)
        {
            if (Math.Abs(key.X - centerChunkX) > keepRadius || Math.Abs(key.Y - centerChunkZ) > keepRadius)
            {
                toRemove.Add(key);
            }
        }

        foreach (Point key in toRemove)
        {
            _chunks.Remove(key);
        }
    }

    public bool IsSolid(int x, int y, int z)
    {
        if (y < 0 || y >= Height)
        {
            return false;
        }

        Point chunkCoord = GetChunkCoordinate(x, z);
        WorldChunk chunk = GetOrCreateChunk(chunkCoord.X, chunkCoord.Y);
        int localX = PositiveModulo(x, ChunkSize);
        int localZ = PositiveModulo(z, ChunkSize);
        return chunk.Voxels.IsSolid(localX, y, localZ);
    }

    public bool IsSolidLoadedOrAir(int x, int y, int z)
    {
        if (y < 0 || y >= Height)
        {
            return false;
        }

        Point chunkCoord = GetChunkCoordinate(x, z);
        if (!TryGetLoadedChunk(chunkCoord.X, chunkCoord.Y, out WorldChunk chunk))
        {
            return false;
        }

        int localX = PositiveModulo(x, ChunkSize);
        int localZ = PositiveModulo(z, ChunkSize);
        return chunk.Voxels.IsSolid(localX, y, localZ);
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (y < 0 || y >= Height)
        {
            return BlockType.Air;
        }

        Point chunkCoord = GetChunkCoordinate(x, z);
        WorldChunk chunk = GetOrCreateChunk(chunkCoord.X, chunkCoord.Y);
        int localX = PositiveModulo(x, ChunkSize);
        int localZ = PositiveModulo(z, ChunkSize);
        return chunk.Voxels.GetBlock(localX, y, localZ);
    }

    public BlockType GetLoadedBlockOrAir(int x, int y, int z)
    {
        if (y < 0 || y >= Height)
        {
            return BlockType.Air;
        }

        Point chunkCoord = GetChunkCoordinate(x, z);
        if (!TryGetLoadedChunk(chunkCoord.X, chunkCoord.Y, out WorldChunk chunk))
        {
            return BlockType.Air;
        }

        int localX = PositiveModulo(x, ChunkSize);
        int localZ = PositiveModulo(z, ChunkSize);
        return chunk.Voxels.GetBlock(localX, y, localZ);
    }

    public bool SetBlock(int x, int y, int z, BlockType block)
    {
        if (y < 0 || y >= Height)
        {
            return false;
        }

        Point chunkCoord = GetChunkCoordinate(x, z);
        WorldChunk chunk = GetOrCreateChunk(chunkCoord.X, chunkCoord.Y);
        int localX = PositiveModulo(x, ChunkSize);
        int localZ = PositiveModulo(z, ChunkSize);
        BlockType existing = chunk.Voxels.GetBlock(localX, y, localZ);
        if (existing == block)
        {
            return false;
        }

        chunk.Voxels.SetBlock(localX, y, localZ, block);
        return true;
    }

    public int GetSurfaceHeight(int x, int z)
    {
        Point chunkCoord = GetChunkCoordinate(x, z);
        WorldChunk chunk = GetOrCreateChunk(chunkCoord.X, chunkCoord.Y);
        int localX = PositiveModulo(x, ChunkSize);
        int localZ = PositiveModulo(z, ChunkSize);
        return chunk.Voxels.GetSurfaceHeight(localX, localZ);
    }

    public Point GetChunkCoordinate(Vector3 worldPosition)
    {
        return GetChunkCoordinate((int)MathF.Floor(worldPosition.X), (int)MathF.Floor(worldPosition.Z));
    }

    public Point GetChunkCoordinate(int worldX, int worldZ)
    {
        return new Point(FloorDiv(worldX, ChunkSize), FloorDiv(worldZ, ChunkSize));
    }

    private WorldChunk GetOrCreateChunk(int chunkX, int chunkZ)
    {
        Point key = new(chunkX, chunkZ);
        if (_chunks.TryGetValue(key, out WorldChunk chunk))
        {
            return chunk;
        }

        chunk = new WorldChunk
        {
            ChunkX = chunkX,
            ChunkZ = chunkZ,
            Voxels = WorldGenerator.GenerateChunk(chunkX, chunkZ, ChunkSize, Height, _seed)
        };

        _chunks.Add(key, chunk);
        return chunk;
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

    private static int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
