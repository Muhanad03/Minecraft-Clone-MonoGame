using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NewProject.Rendering;

namespace NewProject.World;

public static class WorldMeshBuilder
{
    public static WorldMeshData BuildChunk(InfiniteWorld world, WorldChunk chunk)
    {
        List<VoxelVertex> vertices = new();
        List<int> indices = new();

        int worldOffsetX = chunk.ChunkX * InfiniteWorld.ChunkSize;
        int worldOffsetZ = chunk.ChunkZ * InfiniteWorld.ChunkSize;

        for (int x = 0; x < chunk.Voxels.Width; x++)
        {
            for (int y = 0; y < chunk.Voxels.Height; y++)
            {
                for (int z = 0; z < chunk.Voxels.Depth; z++)
                {
                    BlockType block = chunk.Voxels.GetBlock(x, y, z);
                    if (block == BlockType.Air)
                    {
                        continue;
                    }

                    int globalX = worldOffsetX + x;
                    int globalZ = worldOffsetZ + z;

                    if (!world.IsSolidLoadedOrAir(globalX, y + 1, globalZ))
                    {
                        AddFace(vertices, indices, block, FaceDirection.Top, Vector3.Up,
                            new Vector3(globalX, y + 1, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ + 1),
                            new Vector3(globalX, y + 1, globalZ + 1));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX, y - 1, globalZ))
                    {
                        AddFace(vertices, indices, block, FaceDirection.Bottom, Vector3.Down,
                            new Vector3(globalX, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ),
                            new Vector3(globalX, y, globalZ));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX - 1, y, globalZ))
                    {
                        AddFace(vertices, indices, block, FaceDirection.Left, Vector3.Left,
                            new Vector3(globalX, y, globalZ),
                            new Vector3(globalX, y, globalZ + 1),
                            new Vector3(globalX, y + 1, globalZ + 1),
                            new Vector3(globalX, y + 1, globalZ));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX + 1, y, globalZ))
                    {
                        AddFace(vertices, indices, block, FaceDirection.Right, Vector3.Right,
                            new Vector3(globalX + 1, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ + 1));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX, y, globalZ - 1))
                    {
                        AddFace(vertices, indices, block, FaceDirection.Back, Vector3.Backward,
                            new Vector3(globalX + 1, y, globalZ),
                            new Vector3(globalX, y, globalZ),
                            new Vector3(globalX, y + 1, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX, y, globalZ + 1))
                    {
                        AddFace(vertices, indices, block, FaceDirection.Front, Vector3.Forward,
                            new Vector3(globalX, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ + 1),
                            new Vector3(globalX + 1, y + 1, globalZ + 1),
                            new Vector3(globalX, y + 1, globalZ + 1));
                    }
                }
            }
        }

        return new WorldMeshData
        {
            Vertices = vertices.ToArray(),
            Indices = indices.ToArray()
        };
    }

    private static void AddFace(
        List<VoxelVertex> vertices,
        List<int> indices,
        BlockType blockType,
        FaceDirection faceDirection,
        Vector3 normal,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        Color color = BlockPalette.GetFaceColor(blockType, faceDirection);
        int start = vertices.Count;

        vertices.Add(new VoxelVertex(a, normal, color));
        vertices.Add(new VoxelVertex(b, normal, color));
        vertices.Add(new VoxelVertex(c, normal, color));
        vertices.Add(new VoxelVertex(d, normal, color));

        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }
}
