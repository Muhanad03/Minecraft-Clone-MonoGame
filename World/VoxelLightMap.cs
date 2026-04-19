using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace NewProject.World;

public sealed class VoxelLightMap
{
    private const int Margin = 10;
    private const byte MaxSkyLight = 15;
    private const byte MaxTorchLight = 13;

    private readonly byte[] _skyLight;
    private readonly byte[] _torchLight;
    private readonly int _width;
    private readonly int _height;
    private readonly int _depth;
    private readonly int _originX;
    private readonly int _originZ;

    private VoxelLightMap(int width, int height, int depth, int originX, int originZ)
    {
        _width = width;
        _height = height;
        _depth = depth;
        _originX = originX;
        _originZ = originZ;
        _skyLight = new byte[width * height * depth];
        _torchLight = new byte[width * height * depth];
    }

    public static VoxelLightMap Build(InfiniteWorld world, WorldChunk chunk)
    {
        int width = chunk.Voxels.Width + Margin * 2;
        int depth = chunk.Voxels.Depth + Margin * 2;
        int originX = chunk.ChunkX * InfiniteWorld.ChunkSize - Margin;
        int originZ = chunk.ChunkZ * InfiniteWorld.ChunkSize - Margin;
        VoxelLightMap lightMap = new(width, world.Height, depth, originX, originZ);

        lightMap.BuildSkyLight(world);
        lightMap.BuildTorchLight(world);
        return lightMap;
    }

    public float SampleLightFactor(Vector3 sample)
    {
        int x = (int)MathF.Floor(sample.X) - _originX;
        int y = (int)MathF.Floor(sample.Y);
        int z = (int)MathF.Floor(sample.Z) - _originZ;

        if (!IsInBounds(x, y, z))
        {
            return 1f;
        }

        int index = GetIndex(x, y, z);
        float sky = _skyLight[index] / 15f;
        float torch = _torchLight[index] / 15f;

        // Keep caves readable but let actual propagated light decide how bright they are.
        float skyFactor = 0.18f + sky * 0.82f;
        float torchFactor = 0.16f + torch * 0.92f;
        return MathHelper.Clamp(MathF.Max(skyFactor, torchFactor), 0.16f, 1.08f);
    }

    private void BuildSkyLight(InfiniteWorld world)
    {
        Queue<int> queue = new();

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _depth; z++)
            {
                byte light = MaxSkyLight;
                int worldX = _originX + x;
                int worldZ = _originZ + z;

                for (int y = _height - 1; y >= 0; y--)
                {
                    BlockType block = world.GetLoadedBlockOrAir(worldX, y, worldZ);
                    if (!AllowsLightThrough(block))
                    {
                        light = 0;
                        continue;
                    }

                    if (block is BlockType.Water or BlockType.Leaves)
                    {
                        light = ReduceLight(light, GetLightLoss(block));
                    }

                    int index = GetIndex(x, y, z);
                    _skyLight[index] = light;

                    if (light > 1 && IsSkySpreadCandidate(world, worldX, y, worldZ))
                    {
                        queue.Enqueue(index);
                    }
                }
            }
        }

        Propagate(world, _skyLight, queue);
    }

    private void BuildTorchLight(InfiniteWorld world)
    {
        Queue<int> queue = new();

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _depth; z++)
            {
                int worldX = _originX + x;
                int worldZ = _originZ + z;

                for (int y = 0; y < _height; y++)
                {
                    if (world.GetLoadedBlockOrAir(worldX, y, worldZ) != BlockType.Torch)
                    {
                        continue;
                    }

                    int index = GetIndex(x, y, z);
                    _torchLight[index] = MaxTorchLight;
                    queue.Enqueue(index);
                }
            }
        }

        Propagate(world, _torchLight, queue);
    }

    private void Propagate(InfiniteWorld world, byte[] lights, Queue<int> queue)
    {
        ReadOnlySpan<Int3> offsets =
        [
            new Int3(1, 0, 0),
            new Int3(-1, 0, 0),
            new Int3(0, 1, 0),
            new Int3(0, -1, 0),
            new Int3(0, 0, 1),
            new Int3(0, 0, -1)
        ];

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            FromIndex(index, out int x, out int y, out int z);
            byte sourceLight = lights[index];
            if (sourceLight <= 1)
            {
                continue;
            }

            foreach (Int3 offset in offsets)
            {
                int nx = x + offset.X;
                int ny = y + offset.Y;
                int nz = z + offset.Z;
                if (!IsInBounds(nx, ny, nz))
                {
                    continue;
                }

                BlockType block = world.GetLoadedBlockOrAir(_originX + nx, ny, _originZ + nz);
                if (!AllowsLightThrough(block))
                {
                    continue;
                }

                byte newLight = ReduceLight(sourceLight, GetLightLoss(block));
                int neighborIndex = GetIndex(nx, ny, nz);
                if (newLight <= lights[neighborIndex])
                {
                    continue;
                }

                lights[neighborIndex] = newLight;
                queue.Enqueue(neighborIndex);
            }
        }
    }

    private static bool AllowsLightThrough(BlockType block)
    {
        return block is BlockType.Air or BlockType.Water or BlockType.Torch or BlockType.Leaves;
    }

    private static bool IsSkySpreadCandidate(InfiniteWorld world, int x, int y, int z)
    {
        BlockType east = world.GetLoadedBlockOrAir(x + 1, y, z);
        BlockType west = world.GetLoadedBlockOrAir(x - 1, y, z);
        BlockType north = world.GetLoadedBlockOrAir(x, y, z - 1);
        BlockType south = world.GetLoadedBlockOrAir(x, y, z + 1);

        return !AllowsLightThrough(east) ||
            !AllowsLightThrough(west) ||
            !AllowsLightThrough(north) ||
            !AllowsLightThrough(south) ||
            east == BlockType.Water ||
            west == BlockType.Water ||
            north == BlockType.Water ||
            south == BlockType.Water;
    }

    private static byte GetLightLoss(BlockType block)
    {
        return block switch
        {
            BlockType.Water => 2,
            BlockType.Leaves => 2,
            _ => 1
        };
    }

    private static byte ReduceLight(byte light, byte loss)
    {
        return light > loss ? (byte)(light - loss) : (byte)0;
    }

    private bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height && z >= 0 && z < _depth;
    }

    private int GetIndex(int x, int y, int z)
    {
        return x + _width * (z + _depth * y);
    }

    private void FromIndex(int index, out int x, out int y, out int z)
    {
        x = index % _width;
        int yz = index / _width;
        z = yz % _depth;
        y = yz / _depth;
    }

    private readonly record struct Int3(int X, int Y, int Z);
}
