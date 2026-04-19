using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NewProject.Rendering;

namespace NewProject.World;

public static class WorldMeshBuilder
{
    public static BlockTextureAtlas TextureAtlas { get; set; }

    public static WorldMeshData BuildChunk(InfiniteWorld world, WorldChunk chunk)
    {
        List<VoxelVertex> solidVertices = new();
        List<int> solidIndices = new();
        List<VoxelVertex> waterVertices = new();
        List<int> waterIndices = new();
        List<Vector3> torchLights = new();
        VoxelLightMap lightMap = VoxelLightMap.Build(world, chunk);

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
                    if (block == BlockType.Torch)
                    {
                        torchLights.Add(new Vector3(globalX + 0.5f, y + 0.55f, globalZ + 0.5f));
                        AddTorch(world, solidVertices, solidIndices, globalX, y, globalZ);
                        continue;
                    }

                    float waterTop = block == BlockType.Water ? y + 0.88f : y + 1f;

                    if (ShouldRenderFace(world, block, FaceDirection.Top, globalX, y + 1, globalZ))
                    {
                        AddFace(world, lightMap, solidVertices, solidIndices, waterVertices, waterIndices, block, FaceDirection.Top, Vector3.Up,
                            new Vector3(globalX, waterTop, globalZ),
                            new Vector3(globalX + 1, waterTop, globalZ),
                            new Vector3(globalX + 1, waterTop, globalZ + 1),
                            new Vector3(globalX, waterTop, globalZ + 1));
                    }

                    if (ShouldRenderFace(world, block, FaceDirection.Bottom, globalX, y - 1, globalZ))
                    {
                        AddFace(world, lightMap, solidVertices, solidIndices, waterVertices, waterIndices, block, FaceDirection.Bottom, Vector3.Down,
                            new Vector3(globalX, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ),
                            new Vector3(globalX, y, globalZ));
                    }

                    if (ShouldRenderFace(world, block, FaceDirection.Left, globalX - 1, y, globalZ))
                    {
                        AddFace(world, lightMap, solidVertices, solidIndices, waterVertices, waterIndices, block, FaceDirection.Left, Vector3.Left,
                            new Vector3(globalX, y, globalZ),
                            new Vector3(globalX, y, globalZ + 1),
                            new Vector3(globalX, waterTop, globalZ + 1),
                            new Vector3(globalX, waterTop, globalZ));
                    }

                    if (ShouldRenderFace(world, block, FaceDirection.Right, globalX + 1, y, globalZ))
                    {
                        AddFace(world, lightMap, solidVertices, solidIndices, waterVertices, waterIndices, block, FaceDirection.Right, Vector3.Right,
                            new Vector3(globalX + 1, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ),
                            new Vector3(globalX + 1, waterTop, globalZ),
                            new Vector3(globalX + 1, waterTop, globalZ + 1));
                    }

                    if (ShouldRenderFace(world, block, FaceDirection.Back, globalX, y, globalZ - 1))
                    {
                        AddFace(world, lightMap, solidVertices, solidIndices, waterVertices, waterIndices, block, FaceDirection.Back, Vector3.Backward,
                            new Vector3(globalX + 1, y, globalZ),
                            new Vector3(globalX, y, globalZ),
                            new Vector3(globalX, waterTop, globalZ),
                            new Vector3(globalX + 1, waterTop, globalZ));
                    }

                    if (ShouldRenderFace(world, block, FaceDirection.Front, globalX, y, globalZ + 1))
                    {
                        AddFace(world, lightMap, solidVertices, solidIndices, waterVertices, waterIndices, block, FaceDirection.Front, Vector3.Forward,
                            new Vector3(globalX, y, globalZ + 1),
                            new Vector3(globalX + 1, y, globalZ + 1),
                            new Vector3(globalX + 1, waterTop, globalZ + 1),
                            new Vector3(globalX, waterTop, globalZ + 1));
                    }
                }
            }
        }

        return new WorldMeshData
        {
            SolidVertices = solidVertices.ToArray(),
            SolidIndices = solidIndices.ToArray(),
            WaterVertices = waterVertices.ToArray(),
            WaterIndices = waterIndices.ToArray(),
            TorchLights = torchLights.ToArray()
        };
    }

    private static void AddTorch(
        InfiniteWorld world,
        List<VoxelVertex> solidVertices,
        List<int> solidIndices,
        int x,
        int y,
        int z)
    {
        AddTexturedBox(
            solidVertices,
            solidIndices,
            new Vector3(x + 0.5f, y + 0.34f, z + 0.5f),
            new Vector3(0.10f, 0.68f, 0.10f),
            new Color(172, 124, 72),
            new Color(122, 82, 48),
            TextureAtlas.GetFaceUvs(BlockTextureId.LogSide));

        AddTexturedBox(
            solidVertices,
            solidIndices,
            new Vector3(x + 0.5f, y + 0.74f, z + 0.5f),
            new Vector3(0.12f, 0.12f, 0.12f),
            new Color(255, 252, 228),
            new Color(255, 196, 72),
            TextureAtlas.GetFaceUvs(BlockTextureId.Torch),
            false);

        AddTexturedBox(
            solidVertices,
            solidIndices,
            new Vector3(x + 0.5f, y + 0.80f, z + 0.5f),
            new Vector3(0.08f, 0.06f, 0.08f),
            new Color(255, 255, 245),
            new Color(255, 228, 132),
            TextureAtlas.GetFaceUvs(BlockTextureId.Torch),
            false);
    }

    private static void AddFace(
        InfiniteWorld world,
        VoxelLightMap lightMap,
        List<VoxelVertex> solidVertices,
        List<int> solidIndices,
        List<VoxelVertex> waterVertices,
        List<int> waterIndices,
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
        Color[] colors = BuildFaceColors(world, lightMap, normal, a, b, c, d, baseColor);
        List<VoxelVertex> targetVertices = blockType == BlockType.Water ? waterVertices : solidVertices;
        List<int> targetIndices = blockType == BlockType.Water ? waterIndices : solidIndices;
        int start = targetVertices.Count;

        targetVertices.Add(new VoxelVertex(a, normal, colors[0], uvs[0]));
        targetVertices.Add(new VoxelVertex(b, normal, colors[1], uvs[1]));
        targetVertices.Add(new VoxelVertex(c, normal, colors[2], uvs[2]));
        targetVertices.Add(new VoxelVertex(d, normal, colors[3], uvs[3]));

        targetIndices.Add(start);
        targetIndices.Add(start + 1);
        targetIndices.Add(start + 2);
        targetIndices.Add(start);
        targetIndices.Add(start + 2);
        targetIndices.Add(start + 3);
    }

    private static Color[] BuildFaceColors(
        InfiniteWorld world,
        VoxelLightMap lightMap,
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
        Vector3 lightSample = center + normal * 0.55f;
        float faceLight = lightMap.SampleLightFactor(lightSample);

        return
        [
            ShadeVertex(world, lightMap, a, center, normal, basis1, basis2, baseColor, faceLight),
            ShadeVertex(world, lightMap, b, center, normal, basis1, basis2, baseColor, faceLight),
            ShadeVertex(world, lightMap, c, center, normal, basis1, basis2, baseColor, faceLight),
            ShadeVertex(world, lightMap, d, center, normal, basis1, basis2, baseColor, faceLight)
        ];
    }

    private static Color ShadeVertex(
        InfiniteWorld world,
        VoxelLightMap lightMap,
        Vector3 vertex,
        Vector3 center,
        Vector3 normal,
        Vector3 basis1,
        Vector3 basis2,
        Color baseColor,
        float faceLight)
    {
        float sign1 = MathF.Sign(Vector3.Dot(vertex - center, basis1));
        float sign2 = MathF.Sign(Vector3.Dot(vertex - center, basis2));
        sign1 = sign1 == 0f ? 1f : sign1;
        sign2 = sign2 == 0f ? 1f : sign2;

        bool side1 = IsSolidAtSample(world, vertex + normal * 0.5f + basis1 * sign1 * 0.5f);
        bool side2 = IsSolidAtSample(world, vertex + normal * 0.5f + basis2 * sign2 * 0.5f);
        bool corner = IsSolidAtSample(world, vertex + normal * 0.5f + basis1 * sign1 * 0.5f + basis2 * sign2 * 0.5f);

        int occlusion = (side1 && side2) ? 0 : 3 - (BoolToInt(side1) + BoolToInt(side2) + BoolToInt(corner));
        float aoFactor = 0.97f + (occlusion / 3f) * 0.04f;
        float vertexLight = lightMap.SampleLightFactor(vertex + normal * 0.42f);
        float lightFactor = MathHelper.Clamp(aoFactor * MathHelper.Lerp(faceLight, vertexLight, 0.35f), 0.14f, 1.08f);
        return ScaleColor(baseColor, lightFactor);
    }

    private static bool IsSolidAtSample(InfiniteWorld world, Vector3 sample)
    {
        int x = (int)MathF.Floor(sample.X);
        int y = (int)MathF.Floor(sample.Y);
        int z = (int)MathF.Floor(sample.Z);
        return world.IsSolid(x, y, z);
    }

    private static int BoolToInt(bool value) => value ? 1 : 0;

    private static void AddTexturedBox(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 center,
        Vector3 size,
        Color topColor,
        Color sideColor,
        Vector2[] uvs,
        bool brightenTop = true)
    {
        Vector3 half = size * 0.5f;
        Vector3 min = center - half;
        Vector3 max = center + half;

        AddTexturedFace(vertices, indices, Vector3.Up, brightenTop ? topColor : sideColor, uvs,
            new Vector3(min.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z));

        AddTexturedFace(vertices, indices, Vector3.Down, ScaleColor(sideColor, 0.80f), uvs,
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(min.X, min.Y, min.Z));

        AddTexturedFace(vertices, indices, Vector3.Left, sideColor, uvs,
            new Vector3(min.X, min.Y, min.Z),
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z),
            new Vector3(min.X, max.Y, min.Z));

        AddTexturedFace(vertices, indices, Vector3.Right, sideColor, uvs,
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, max.Z));

        AddTexturedFace(vertices, indices, Vector3.Backward, ScaleColor(sideColor, 0.90f), uvs,
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(min.X, min.Y, min.Z),
            new Vector3(min.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z));

        AddTexturedFace(vertices, indices, Vector3.Forward, ScaleColor(sideColor, 0.94f), uvs,
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(max.X, max.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z));
    }

    private static Color ScaleColor(Color color, float scale)
    {
        return new Color(
            (byte)Math.Clamp((int)(color.R * scale), 0, 255),
            (byte)Math.Clamp((int)(color.G * scale), 0, 255),
            (byte)Math.Clamp((int)(color.B * scale), 0, 255),
            color.A);
    }

    private static bool ShouldRenderFace(InfiniteWorld world, BlockType block, FaceDirection faceDirection, int neighborX, int neighborY, int neighborZ)
    {
        BlockType neighbor = world.GetBlock(neighborX, neighborY, neighborZ);
        if (neighbor == block)
        {
            return false;
        }

        if (block == BlockType.Water)
        {
            if (BlockPalette.IsSolid(neighbor))
            {
                return false;
            }

            return neighbor != BlockType.Water;
        }

        if (neighbor == BlockType.Water)
        {
            return true;
        }

        return neighbor == BlockType.Air || !BlockPalette.IsSolid(neighbor);
    }

    private static void AddTexturedFace(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 normal,
        Color color,
        Vector2[] uvs,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int start = vertices.Count;
        vertices.Add(new VoxelVertex(a, normal, color, uvs[0]));
        vertices.Add(new VoxelVertex(b, normal, color, uvs[1]));
        vertices.Add(new VoxelVertex(c, normal, color, uvs[2]));
        vertices.Add(new VoxelVertex(d, normal, color, uvs[3]));

        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }
}
