using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NewProject.Gameplay;
using NewProject.World;

namespace NewProject.Rendering;

public sealed class VoxelWorldRenderer : IDisposable
{
    public static readonly Color SkyColor = new(130, 190, 255);
    public static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.45f, -0.9f, 0.22f));
    private static readonly int[] SunPattern =
    [
        0,0,1,1,1,1,1,1,0,0,
        0,1,1,1,1,1,1,1,1,0,
        1,1,1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1,1,1,
        0,1,1,1,1,1,1,1,1,0,
        0,0,1,1,1,1,1,1,0,0
    ];
    private const int SunColumns = 10;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly RasterizerState _rasterizerState;
    private readonly BlockTextureAtlas _blockTextureAtlas;
    private readonly Effect _effect;
    private readonly BasicEffect _unlitEffect;
    private readonly BasicEffect _cloudEffect;
    private readonly BasicEffect _viewModelColorEffect;
    private readonly BasicEffect _viewModelTextureEffect;
    private readonly Dictionary<Point, ChunkRenderData> _chunkMeshes = new();
    private readonly Queue<Point> _pendingBuilds = new();
    private ChunkRenderData? _cloudRenderData;
    private Point _cloudMeshCenterCell;
    private bool _cloudMeshValid;

    public ViewModelSettings ViewModelSettings { get; } = new();

    public VoxelWorldRenderer(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _blockTextureAtlas = new BlockTextureAtlas(graphicsDevice, content);
        _effect = content.Load<Effect>("Effects/VoxelEffect");
        WorldMeshBuilder.TextureAtlas = _blockTextureAtlas;
        _unlitEffect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = false
        };
        _cloudEffect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = false
        };
        _viewModelColorEffect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = false
        };
        _viewModelTextureEffect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = true
        };
        _rasterizerState = new RasterizerState
        {
            CullMode = CullMode.None
        };
    }

    public void SyncWorld(InfiniteWorld world)
    {
        WorldChunk[] snapshot = world.GetLoadedChunksSnapshot();
        HashSet<Point> loadedKeys = new();

        foreach (WorldChunk chunk in snapshot)
        {
            loadedKeys.Add(chunk.Key);

            if (!_chunkMeshes.ContainsKey(chunk.Key) && !_pendingBuilds.Contains(chunk.Key))
            {
                _pendingBuilds.Enqueue(chunk.Key);
            }
        }

        List<Point> staleKeys = new();
        foreach (Point key in _chunkMeshes.Keys)
        {
            if (!loadedKeys.Contains(key))
            {
                staleKeys.Add(key);
            }
        }

        foreach (Point key in staleKeys)
        {
            _chunkMeshes[key].Dispose();
            _chunkMeshes.Remove(key);
        }

        if (_pendingBuilds.Count == 0)
        {
            return;
        }

        Queue<Point> filteredQueue = new();
        while (_pendingBuilds.Count > 0)
        {
            Point key = _pendingBuilds.Dequeue();
            if (loadedKeys.Contains(key) && !_chunkMeshes.ContainsKey(key))
            {
                filteredQueue.Enqueue(key);
            }
        }

        while (filteredQueue.Count > 0)
        {
            _pendingBuilds.Enqueue(filteredQueue.Dequeue());
        }
    }

    public void BuildPendingChunks(InfiniteWorld world, int maxChunksPerFrame)
    {
        for (int i = 0; i < maxChunksPerFrame && _pendingBuilds.Count > 0; i++)
        {
            Point key = _pendingBuilds.Dequeue();
            if (!world.TryGetLoadedChunk(key, out WorldChunk chunk))
            {
                continue;
            }

            WorldMeshData mesh = WorldMeshBuilder.BuildChunk(world, chunk);
            ChunkRenderData renderData = CreateChunkRenderData(mesh);

            if (_chunkMeshes.TryGetValue(key, out ChunkRenderData existing))
            {
                existing.Dispose();
            }

            _chunkMeshes[key] = renderData;
        }
    }

    public void MarkChunkDirty(Point chunkKey)
    {
        if (!_pendingBuilds.Contains(chunkKey))
        {
            _pendingBuilds.Enqueue(chunkKey);
        }
    }

    public void Draw(Vector3 cameraPosition, Matrix view, Matrix projection, float time)
    {
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.BlendState = BlendState.Opaque;
        _effect.Parameters["World"]?.SetValue(Matrix.Identity);
        _effect.Parameters["View"]?.SetValue(view);
        _effect.Parameters["Projection"]?.SetValue(projection);
        _effect.Parameters["CameraPosition"]?.SetValue(cameraPosition);
        _effect.Parameters["BlockAtlas"]?.SetValue(_blockTextureAtlas.Texture);
        _effect.Parameters["Time"]?.SetValue(time);
        _effect.Parameters["SunDirection"]?.SetValue(LightDirection);
        _effect.Parameters["AmbientColor"]?.SetValue(new Vector3(0.92f, 0.96f, 1.0f));
        _effect.Parameters["SunColor"]?.SetValue(new Vector3(1.05f, 1.0f, 0.94f));
        _effect.Parameters["HorizonColor"]?.SetValue(new Vector3(0.62f, 0.79f, 0.98f));
        _effect.Parameters["ZenithColor"]?.SetValue(new Vector3(0.19f, 0.43f, 0.85f));
        _effect.Parameters["FogColor"]?.SetValue(new Vector3(0.71f, 0.84f, 0.98f));
        _effect.Parameters["ShadowColor"]?.SetValue(new Vector3(1.0f, 1.0f, 1.0f));
        _effect.Parameters["FogStart"]?.SetValue(24f);
        _effect.Parameters["FogEnd"]?.SetValue(120f);

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();

            foreach (ChunkRenderData chunkMesh in _chunkMeshes.Values)
            {
                _graphicsDevice.SetVertexBuffer(chunkMesh.VertexBuffer);
                _graphicsDevice.Indices = chunkMesh.IndexBuffer;
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, chunkMesh.PrimitiveCount);
            }

        }

        DrawCloudLayer(cameraPosition, time, view, projection);
        DrawSun(cameraPosition, view, projection);
    }

    public void DrawViewModel(
        Vector3 cameraPosition,
        Matrix view,
        Matrix projection,
        Texture2D texture,
        bool isTool,
        ToolType? selectedTool,
        float useAnimationTimer,
        float useAnimationDuration)
    {
        if (texture is null)
        {
            return;
        }

        float progress = useAnimationDuration <= 0f
            ? 1f
            : 1f - Math.Clamp(useAnimationTimer / useAnimationDuration, 0f, 1f);
        float swing = MathF.Sin(progress * progress * MathHelper.Pi);
        float settle = MathF.Sin(MathF.Sqrt(Math.Max(progress, 0f)) * MathHelper.Pi);
        bool swordSlash = isTool && selectedTool == ToolType.Sword;

        Matrix cameraWorld = Matrix.Invert(view);
        Vector3 right = Vector3.Normalize(cameraWorld.Right);
        Vector3 up = Vector3.Normalize(cameraWorld.Up);
        Vector3 forward = Vector3.Normalize(cameraWorld.Forward);
        ViewModelSettings settings = ViewModelSettings;

        Vector3 basePosition =
            cameraPosition +
            right * (settings.BaseRightOffset - swing * settings.BaseRightSwing) +
            up * (settings.BaseUpOffset - settle * settings.BaseUpSettle) +
            forward * (settings.BaseForwardOffset + swing * settings.BaseForwardSwing);

        if (swordSlash)
        {
            basePosition +=
                right * (-swing * settings.SwordSlashSideSwing) +
                up * (-settle * settings.SwordSlashUpSwing) +
                forward * (swing * settings.SwordSlashForwardSwing);
        }

        Vector3 armDirection = 
            forward * settings.ArmForwardFactor +
            right * settings.ArmRightFactor +
            up * settings.ArmUpFactor;

        if (swordSlash)
        {
            armDirection +=
                forward * (swing * settings.SwordSlashForwardFactorBoost) +
                right * (swing * settings.SwordSlashRightFactorBoost) +
                up * (swing * settings.SwordSlashUpFactorBoost);
        }

        Vector3 armForward = Vector3.Normalize(armDirection);
        Vector3 armRight = Vector3.Normalize(Vector3.Cross(up, armForward));
        if (armRight.LengthSquared() < 0.001f)
        {
            armRight = right;
        }

        Vector3 armUp = Vector3.Normalize(Vector3.Cross(armForward, armRight));

        List<VoxelVertex> armVertices = new();
        List<int> armIndices = new();
        AddOrientedBox(
            armVertices,
            armIndices,
            basePosition,
            armRight,
            armUp,
            armForward,
            new Vector3(settings.ArmWidth, settings.ArmHeight, settings.ArmLength),
            new Color(72, 136, 162),
            new Color(63, 118, 144));

        Vector3 handCenter =
            basePosition +
            armForward * settings.HandForwardOffset +
            armUp * settings.HandUpOffset +
            armRight * settings.HandSideOffset;
        AddOrientedBox(
            armVertices,
            armIndices,
            handCenter,
            armRight,
            armUp,
            armForward,
            new Vector3(settings.HandWidth, settings.HandHeight, settings.HandLength),
            new Color(214, 176, 140),
            new Color(188, 147, 114));

        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.BlendState = BlendState.AlphaBlend;

        _viewModelColorEffect.World = Matrix.Identity;
        _viewModelColorEffect.View = view;
        _viewModelColorEffect.Projection = projection;

        foreach (EffectPass pass in _viewModelColorEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                armVertices.ToArray(),
                0,
                armVertices.Count,
                armIndices.ToArray(),
                0,
                armIndices.Count / 3,
                VoxelVertex.VertexDeclaration);
        }

        List<VoxelVertex> itemVertices = new();
        List<int> itemIndices = new();
        Vector3 itemRight = armRight;
        Vector3 itemUp = armUp;
        Vector3 itemForward = armForward;
        Vector3 itemCenter =
            handCenter +
            itemRight * settings.ItemSideOffset +
            itemUp * settings.ItemUpOffset +
            itemForward * settings.ItemForwardOffset;

        if (isTool)
        {
            Vector3 toolDirection =
                itemForward * settings.ToolForwardFactor +
                itemRight * settings.ToolRightFactor +
                itemUp * settings.ToolUpFactor;

            Vector3 toolForward = Vector3.Normalize(toolDirection);
            Vector3 toolRight = Vector3.Normalize(Vector3.Cross(itemUp, toolForward));
            if (toolRight.LengthSquared() < 0.001f)
            {
                toolRight = itemRight;
            }

            Vector3 toolUp = Vector3.Normalize(Vector3.Cross(toolForward, toolRight));
            Vector3 toolCenter =
                itemCenter +
                toolRight * settings.ToolSideOffset +
                toolUp * settings.ToolUpOffset +
                toolForward * settings.ToolForwardOffset;
            toolCenter +=
                toolRight * settings.ToolGripRightOffset +
                toolUp * settings.ToolGripUpOffset;

            if (swordSlash)
            {
                toolCenter +=
                    toolRight * settings.SwordSlashSideOffset +
                    toolUp * settings.SwordSlashUpOffset +
                    toolForward * settings.SwordSlashForwardOffset;
            }

            AddTexturedQuad(
                itemVertices,
                itemIndices,
                toolCenter,
                toolRight,
                -toolUp,
                settings.ToolWidth,
                settings.ToolHeight,
                Color.White);
        }
        else
        {
            Vector3 blockForward = Vector3.Normalize(
                itemForward * settings.BlockForwardFactor +
                itemRight * settings.BlockRightFactor +
                itemUp * settings.BlockUpFactor);
            Vector3 blockRight = Vector3.Normalize(Vector3.Cross(itemUp, blockForward));
            if (blockRight.LengthSquared() < 0.001f)
            {
                blockRight = itemRight;
            }

            Vector3 blockUp = Vector3.Normalize(Vector3.Cross(blockForward, blockRight));
            Vector3 blockCenter =
                itemCenter +
                blockRight * settings.BlockSideOffset +
                blockUp * settings.BlockUpOffset +
                blockForward * settings.BlockForwardOffset;
            AddTexturedCube(
                itemVertices,
                itemIndices,
                blockCenter,
                blockRight,
                blockUp,
                blockForward,
                settings.BlockSize,
                Color.White);
        }

        _viewModelTextureEffect.World = Matrix.Identity;
        _viewModelTextureEffect.View = view;
        _viewModelTextureEffect.Projection = projection;
        _viewModelTextureEffect.Texture = texture;

        foreach (EffectPass pass in _viewModelTextureEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                itemVertices.ToArray(),
                0,
                itemVertices.Count,
                itemIndices.ToArray(),
                0,
                itemIndices.Count / 3,
                VoxelVertex.VertexDeclaration);
        }
    }

    public void Dispose()
    {
        foreach (ChunkRenderData chunkMesh in _chunkMeshes.Values)
        {
            chunkMesh.Dispose();
        }

        _chunkMeshes.Clear();
        _pendingBuilds.Clear();
        _cloudRenderData?.Dispose();
        _blockTextureAtlas.Texture.Dispose();
        _cloudEffect.Dispose();
        _unlitEffect.Dispose();
        _viewModelColorEffect.Dispose();
        _viewModelTextureEffect.Dispose();
        _effect.Dispose();
        _rasterizerState.Dispose();
    }

    private ChunkRenderData CreateChunkRenderData(WorldMeshData mesh)
    {
        return CreateChunkRenderData(mesh.Vertices, mesh.Indices);
    }

    private ChunkRenderData CreateChunkRenderData(VoxelVertex[] vertices, int[] indices)
    {
        VertexBuffer vertexBuffer = new(_graphicsDevice, VoxelVertex.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
        vertexBuffer.SetData(vertices);

        IndexBuffer indexBuffer = new(_graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
        indexBuffer.SetData(indices);

        return new ChunkRenderData
        {
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            PrimitiveCount = indices.Length / 3
        };
    }

    private void DrawCloudLayer(Vector3 cameraPosition, float time, Matrix view, Matrix projection)
    {
        const float cloudHeight = 90f;
        const float cloudThickness = 2.25f;
        const float cloudBlockSize = 6f;
        const float cloudSpacing = 64f;
        const int cloudRadius = 4;

        int centerCellX = (int)MathF.Floor(cameraPosition.X / cloudSpacing);
        int centerCellZ = (int)MathF.Floor(cameraPosition.Z / cloudSpacing);
        float drift = time * 2.4f;

        EnsureCloudMesh(centerCellX, centerCellZ, cloudHeight, cloudThickness, cloudBlockSize, cloudSpacing, cloudRadius);
        if (_cloudRenderData is null)
        {
            return;
        }

        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _cloudEffect.World = Matrix.CreateTranslation(drift, 0f, 0f);
        _cloudEffect.View = view;
        _cloudEffect.Projection = projection;

        foreach (EffectPass cloudPass in _cloudEffect.CurrentTechnique.Passes)
        {
            cloudPass.Apply();
            _graphicsDevice.SetVertexBuffer(_cloudRenderData.VertexBuffer);
            _graphicsDevice.Indices = _cloudRenderData.IndexBuffer;
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _cloudRenderData.PrimitiveCount);
        }
    }

    private void EnsureCloudMesh(
        int centerCellX,
        int centerCellZ,
        float cloudHeight,
        float cloudThickness,
        float cloudBlockSize,
        float cloudSpacing,
        int cloudRadius)
    {
        Point cell = new(centerCellX, centerCellZ);
        if (_cloudMeshValid && cell == _cloudMeshCenterCell)
        {
            return;
        }

        _cloudRenderData?.Dispose();
        _cloudRenderData = BuildCloudMesh(centerCellX, centerCellZ, cloudHeight, cloudThickness, cloudBlockSize, cloudSpacing, cloudRadius);
        _cloudMeshCenterCell = cell;
        _cloudMeshValid = _cloudRenderData is not null;
    }

    private ChunkRenderData? BuildCloudMesh(
        int centerCellX,
        int centerCellZ,
        float cloudHeight,
        float cloudThickness,
        float cloudBlockSize,
        float cloudSpacing,
        int cloudRadius)
    {
        List<VoxelVertex> vertices = new();
        List<int> indices = new();

        for (int cellZ = centerCellZ - cloudRadius; cellZ <= centerCellZ + cloudRadius; cellZ++)
        {
            for (int cellX = centerCellX - cloudRadius; cellX <= centerCellX + cloudRadius; cellX++)
            {
                int hash = Hash(cellX, cellZ, 4049);
                if ((hash % 100) > 62)
                {
                    continue;
                }

                float anchorX = cellX * cloudSpacing + ((hash >> 4) % 24) - 12f;
                float anchorZ = cellZ * cloudSpacing + ((hash >> 8) % 24) - 12f;
                AddCloudShape(
                    vertices,
                    indices,
                    new Vector3(anchorX, cloudHeight, anchorZ),
                    cellX,
                    cellZ,
                    hash,
                    cloudBlockSize,
                    cloudThickness);
            }
        }

        if (vertices.Count == 0 || indices.Count == 0)
        {
            return null;
        }

        return CreateChunkRenderData(vertices.ToArray(), indices.ToArray());
    }

    private void DrawSun(Vector3 cameraPosition, Matrix view, Matrix projection)
    {
        const float sunDistance = 220f;
        const float sunBlockSize = 4.5f;
        const float sunThickness = 0.8f;

        Vector3 sunForward = Vector3.Normalize(-LightDirection);
        Vector3 sunRight = Vector3.Normalize(Vector3.Cross(Vector3.Up, sunForward));
        if (sunRight.LengthSquared() < 0.001f)
        {
            sunRight = Vector3.Right;
        }

        Vector3 sunUp = Vector3.Normalize(Vector3.Cross(sunForward, sunRight));
        Vector3 center = cameraPosition + sunForward * sunDistance;
        Vector2 halfSize = new(SunColumns * sunBlockSize * 0.5f, (SunPattern.Length / SunColumns) * sunBlockSize * 0.5f);

        List<VoxelVertex> vertices = new();
        List<int> indices = new();

        for (int i = 0; i < SunPattern.Length; i++)
        {
            if (SunPattern[i] == 0)
            {
                continue;
            }

            int x = i % SunColumns;
            int y = i / SunColumns;
            float localX = x * sunBlockSize - halfSize.X;
            float localY = halfSize.Y - y * sunBlockSize;

            Vector3 blockCenter = center + sunRight * localX + sunUp * localY;
            AddOrientedBox(vertices, indices, blockCenter, sunRight, sunUp, sunForward, sunBlockSize, sunThickness);
        }

        if (vertices.Count == 0)
        {
            return;
        }

        _unlitEffect.World = Matrix.Identity;
        _unlitEffect.View = view;
        _unlitEffect.Projection = projection;

        foreach (EffectPass pass in _unlitEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                vertices.ToArray(),
                0,
                vertices.Count,
                indices.ToArray(),
                0,
                indices.Count / 3,
                VoxelVertex.VertexDeclaration);
        }
    }

    private static void AddCloudShape(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 anchor,
        int cellX,
        int cellZ,
        int seed,
        float blockSize,
        float thickness)
    {
        const int columns = 12;
        const int rows = 6;

        int centerX = 3 + (seed % 6);
        int centerZ = 2 + ((seed >> 3) % 2);
        int radiusX = 3 + ((seed >> 5) % 3);
        int radiusZ = 2 + ((seed >> 8) % 2);
        float edgeNoise = 0.9f + ((seed >> 11) % 40) / 100f;

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < columns; x++)
            {
                float dx = (x - centerX) / (float)radiusX;
                float dz = (z - centerZ) / (float)radiusZ;
                float ellipse = dx * dx + dz * dz;

                int localHash = Hash(cellX * 31 + x, cellZ * 31 + z, seed);
                float wobble = ((localHash % 100) / 100f - 0.5f) * 0.55f;
                bool carvePocket = ((localHash >> 7) % 100) < 16 && ellipse > 0.35f;
                bool keep = ellipse + wobble < edgeNoise && !carvePocket;

                if (!keep)
                {
                    continue;
                }

                bool trimCorner =
                    ((x == 0 || x == columns - 1) && (z == 0 || z == rows - 1)) ||
                    (((localHash >> 11) % 100) < 10 && ellipse > 0.75f);

                if (trimCorner)
                {
                    continue;
                }

                Vector3 min = anchor + new Vector3(x * blockSize, 0f, z * blockSize);
                Vector3 max = min + new Vector3(blockSize, thickness, blockSize);
                AddBox(vertices, indices, min, max);
            }
        }
    }

    private static void AddBox(List<VoxelVertex> vertices, List<int> indices, Vector3 min, Vector3 max)
    {
        Color top = new(251, 253, 255);
        Color side = new(235, 242, 252);
        Color bottom = new(219, 228, 242);
        Vector2 uv = Vector2.Zero;

        AddFace(vertices, indices, Vector3.Up, top, uv,
            new Vector3(min.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z));

        AddFace(vertices, indices, Vector3.Down, bottom, uv,
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(min.X, min.Y, min.Z));

        AddFace(vertices, indices, Vector3.Left, side, uv,
            new Vector3(min.X, min.Y, min.Z),
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z),
            new Vector3(min.X, max.Y, min.Z));

        AddFace(vertices, indices, Vector3.Right, side, uv,
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, max.Z));

        AddFace(vertices, indices, Vector3.Backward, side, uv,
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(min.X, min.Y, min.Z),
            new Vector3(min.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z));

        AddFace(vertices, indices, Vector3.Forward, side, uv,
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(max.X, max.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z));
    }

    private static void AddOrientedBox(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 center,
        Vector3 right,
        Vector3 up,
        Vector3 forward,
        float size,
        float thickness)
    {
        Vector3 halfRight = right * (size * 0.5f);
        Vector3 halfUp = up * (size * 0.5f);
        Vector3 halfForward = forward * (thickness * 0.5f);

        Vector3 lbf = center - halfRight - halfUp - halfForward;
        Vector3 rbf = center + halfRight - halfUp - halfForward;
        Vector3 rtf = center + halfRight + halfUp - halfForward;
        Vector3 ltf = center - halfRight + halfUp - halfForward;
        Vector3 lbb = center - halfRight - halfUp + halfForward;
        Vector3 rbb = center + halfRight - halfUp + halfForward;
        Vector3 rtb = center + halfRight + halfUp + halfForward;
        Vector3 ltb = center - halfRight + halfUp + halfForward;

        Color front = new(255, 245, 170);
        Color edge = new(255, 214, 120);
        Color back = new(245, 188, 92);
        Vector2 uv = Vector2.Zero;

        AddFace(vertices, indices, -forward, front, uv, ltf, rtf, rbf, lbf);
        AddFace(vertices, indices, forward, back, uv, lbb, rbb, rtb, ltb);
        AddFace(vertices, indices, -right, edge, uv, lbf, lbb, ltb, ltf);
        AddFace(vertices, indices, right, edge, uv, rbb, rbf, rtf, rtb);
        AddFace(vertices, indices, up, front, uv, ltb, rtb, rtf, ltf);
        AddFace(vertices, indices, -up, edge, uv, lbf, rbf, rbb, lbb);
    }

    private static void AddOrientedBox(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 center,
        Vector3 right,
        Vector3 up,
        Vector3 forward,
        Vector3 size,
        Color frontColor,
        Color sideColor)
    {
        Vector3 halfRight = right * (size.X * 0.5f);
        Vector3 halfUp = up * (size.Y * 0.5f);
        Vector3 halfForward = forward * (size.Z * 0.5f);

        Vector3 lbf = center - halfRight - halfUp - halfForward;
        Vector3 rbf = center + halfRight - halfUp - halfForward;
        Vector3 rtf = center + halfRight + halfUp - halfForward;
        Vector3 ltf = center - halfRight + halfUp - halfForward;
        Vector3 lbb = center - halfRight - halfUp + halfForward;
        Vector3 rbb = center + halfRight - halfUp + halfForward;
        Vector3 rtb = center + halfRight + halfUp + halfForward;
        Vector3 ltb = center - halfRight + halfUp + halfForward;

        Color highlight = ScaleColor(frontColor, 1.06f);
        Color shadow = ScaleColor(sideColor, 0.92f);
        Vector2 uv = Vector2.Zero;

        AddFace(vertices, indices, -forward, highlight, uv, ltf, rtf, rbf, lbf);
        AddFace(vertices, indices, forward, shadow, uv, lbb, rbb, rtb, ltb);
        AddFace(vertices, indices, -right, sideColor, uv, lbf, lbb, ltb, ltf);
        AddFace(vertices, indices, right, sideColor, uv, rbb, rbf, rtf, rtb);
        AddFace(vertices, indices, up, highlight, uv, ltb, rtb, rtf, ltf);
        AddFace(vertices, indices, -up, shadow, uv, lbf, rbf, rbb, lbb);
    }

    private static void AddTexturedCube(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 center,
        Vector3 right,
        Vector3 up,
        Vector3 forward,
        float size,
        Color color)
    {
        Vector3 halfRight = right * (size * 0.5f);
        Vector3 halfUp = up * (size * 0.5f);
        Vector3 halfForward = forward * (size * 0.5f);
        Vector2[] quadUvs =
        [
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f)
        ];

        Vector3 lbf = center - halfRight - halfUp - halfForward;
        Vector3 rbf = center + halfRight - halfUp - halfForward;
        Vector3 rtf = center + halfRight + halfUp - halfForward;
        Vector3 ltf = center - halfRight + halfUp - halfForward;
        Vector3 lbb = center - halfRight - halfUp + halfForward;
        Vector3 rbb = center + halfRight - halfUp + halfForward;
        Vector3 rtb = center + halfRight + halfUp + halfForward;
        Vector3 ltb = center - halfRight + halfUp + halfForward;

        AddTexturedFace(vertices, indices, -forward, color, quadUvs, ltf, rtf, rbf, lbf);
        AddTexturedFace(vertices, indices, forward, ScaleColor(color, 0.86f), quadUvs, lbb, rbb, rtb, ltb);
        AddTexturedFace(vertices, indices, -right, ScaleColor(color, 0.92f), quadUvs, lbf, lbb, ltb, ltf);
        AddTexturedFace(vertices, indices, right, ScaleColor(color, 0.92f), quadUvs, rbb, rbf, rtf, rtb);
        AddTexturedFace(vertices, indices, up, ScaleColor(color, 1.08f), quadUvs, ltb, rtb, rtf, ltf);
        AddTexturedFace(vertices, indices, -up, ScaleColor(color, 0.78f), quadUvs, lbf, rbf, rbb, lbb);
    }

    private static void AddTexturedQuad(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 center,
        Vector3 right,
        Vector3 up,
        float width,
        float height,
        Color color)
    {
        Vector3 halfRight = right * (width * 0.5f);
        Vector3 halfUp = up * (height * 0.5f);
        Vector2[] quadUvs =
        [
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f)
        ];

        Vector3 a = center - halfRight + halfUp;
        Vector3 b = center + halfRight + halfUp;
        Vector3 c = center + halfRight - halfUp;
        Vector3 d = center - halfRight - halfUp;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));

        AddTexturedFace(vertices, indices, normal, color, quadUvs, a, b, c, d);
    }

    private static void AddFace(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 normal,
        Color color,
        Vector2 uv,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int start = vertices.Count;
        vertices.Add(new VoxelVertex(a, normal, color, uv));
        vertices.Add(new VoxelVertex(b, normal, color, uv));
        vertices.Add(new VoxelVertex(c, normal, color, uv));
        vertices.Add(new VoxelVertex(d, normal, color, uv));

        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
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

    private static Color ScaleColor(Color color, float scale)
    {
        return new Color(
            (byte)Math.Clamp((int)(color.R * scale), 0, 255),
            (byte)Math.Clamp((int)(color.G * scale), 0, 255),
            (byte)Math.Clamp((int)(color.B * scale), 0, 255),
            color.A);
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
}
