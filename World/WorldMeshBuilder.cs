using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NewProject.Rendering;

namespace NewProject.World;

public static class WorldMeshBuilder
{
    private static readonly Vector3 SunTraceDirection = Vector3.Normalize(-VoxelWorldRenderer.LightDirection);

    public static BlockTextureAtlas TextureAtlas { get; set; }

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
                        AddFace(world, vertices, indices, block, FaceDirection.Top, Vector3.Up,
                            new Vector3(globalX, y + 1, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ + 1),
                            new Vector3(globalX, y + 1, globalZ + 1));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX, y - 1, globalZ))
                    {
                        AddFace(world, vertices, indices, block, FaceDirection.Bottom, Vector3.Down,
                            new Vector3(globalX, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ),
                            new Vector3(globalX, y, globalZ));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX - 1, y, globalZ))
                    {
                        AddFace(world, vertices, indices, block, FaceDirection.Left, Vector3.Left,
                            new Vector3(globalX, y, globalZ),
                            new Vector3(globalX, y, globalZ + 1),
                            new Vector3(globalX, y + 1, globalZ + 1),
                            new Vector3(globalX, y + 1, globalZ));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX + 1, y, globalZ))
                    {
                        AddFace(world, vertices, indices, block, FaceDirection.Right, Vector3.Right,
                            new Vector3(globalX + 1, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ + 1));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX, y, globalZ - 1))
                    {
                        AddFace(world, vertices, indices, block, FaceDirection.Back, Vector3.Backward,
                            new Vector3(globalX + 1, y, globalZ),
                            new Vector3(globalX, y, globalZ),
                            new Vector3(globalX, y + 1, globalZ),
                            new Vector3(globalX + 1, y + 1, globalZ));
                    }

                    if (!world.IsSolidLoadedOrAir(globalX, y, globalZ + 1))
                    {
                        AddFace(world, vertices, indices, block, FaceDirection.Front, Vector3.Forward,
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
        InfiniteWorld world,
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
        Color baseColor = BlockPalette.GetFaceTint(blockType, faceDirection);
        Vector2[] uvs = TextureAtlas.GetFaceUvs(BlockPalette.GetTexture(blockType, faceDirection));
        Color[] colors = BuildFaceColors(world, faceDirection, normal, a, b, c, d, baseColor);
        int start = vertices.Count;

        vertices.Add(new VoxelVertex(a, normal, colors[0], uvs[0]));
        vertices.Add(new VoxelVertex(b, normal, colors[1], uvs[1]));
        vertices.Add(new VoxelVertex(c, normal, colors[2], uvs[2]));
        vertices.Add(new VoxelVertex(d, normal, colors[3], uvs[3]));

        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }

    private static Color[] BuildFaceColors(
        InfiniteWorld world,
        FaceDirection faceDirection,
        Vector3 normal,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Color baseColor)
    {
        Vector3 center = (a + b + c + d) * 0.25f;
        Vector3 basis1 = Vector3.Normalize(b - a);
        Vector3 basis2 = Vector3.Normalize(d - a);
        float faceShadow = 1f;

        return
        [
            ShadeVertex(world, a, center, normal, basis1, basis2, baseColor, faceShadow),
            ShadeVertex(world, b, center, normal, basis1, basis2, baseColor, faceShadow),
            ShadeVertex(world, c, center, normal, basis1, basis2, baseColor, faceShadow),
            ShadeVertex(world, d, center, normal, basis1, basis2, baseColor, faceShadow)
        ];
    }

    private static Color ShadeVertex(
        InfiniteWorld world,
        Vector3 vertex,
        Vector3 center,
        Vector3 normal,
        Vector3 basis1,
        Vector3 basis2,
        Color baseColor,
        float faceShadow)
    {
        float sign1 = MathF.Sign(Vector3.Dot(vertex - center, basis1));
        float sign2 = MathF.Sign(Vector3.Dot(vertex - center, basis2));
        sign1 = sign1 == 0f ? 1f : sign1;
        sign2 = sign2 == 0f ? 1f : sign2;

        bool side1 = IsSolidAtSample(world, vertex + normal * 0.5f + basis1 * sign1 * 0.5f);
        bool side2 = IsSolidAtSample(world, vertex + normal * 0.5f + basis2 * sign2 * 0.5f);
        bool corner = IsSolidAtSample(world, vertex + normal * 0.5f + basis1 * sign1 * 0.5f + basis2 * sign2 * 0.5f);

        int occlusion = (side1 && side2) ? 0 : 3 - (BoolToInt(side1) + BoolToInt(side2) + BoolToInt(corner));
        float aoFactor = 0.94f + (occlusion / 3f) * 0.06f;
        return ScaleColor(baseColor, aoFactor * faceShadow);
    }

    private static bool IsSolidAtSample(InfiniteWorld world, Vector3 sample)
    {
        int x = (int)MathF.Floor(sample.X);
        int y = (int)MathF.Floor(sample.Y);
        int z = (int)MathF.Floor(sample.Z);
        return world.IsSolid(x, y, z);
    }

    private static int BoolToInt(bool value) => value ? 1 : 0;

    private static Color ScaleColor(Color color, float scale)
    {
        return new Color(
            (byte)Math.Clamp((int)(color.R * scale), 0, 255),
            (byte)Math.Clamp((int)(color.G * scale), 0, 255),
            (byte)Math.Clamp((int)(color.B * scale), 0, 255),
            color.A);
    }
}
