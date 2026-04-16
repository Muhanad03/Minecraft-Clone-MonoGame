using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NewProject.World;

namespace NewProject.Rendering;

public sealed class VoxelWorldRenderer : IDisposable
{
    public static readonly Color SkyColor = new(130, 190, 255);

    private readonly GraphicsDevice _graphicsDevice;
    private readonly RasterizerState _rasterizerState;
    private readonly Effect _effect;
    private readonly Dictionary<Point, ChunkRenderData> _chunkMeshes = new();
    private readonly Queue<Point> _pendingBuilds = new();

    public VoxelWorldRenderer(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _effect = content.Load<Effect>("Effects/VoxelEffect");
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

    public void Draw(Vector3 cameraPosition, Matrix view, Matrix projection, float time)
    {
        if (_chunkMeshes.Count == 0)
        {
            return;
        }

        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _effect.Parameters["World"]?.SetValue(Matrix.Identity);
        _effect.Parameters["View"]?.SetValue(view);
        _effect.Parameters["Projection"]?.SetValue(projection);
        _effect.Parameters["CameraPosition"]?.SetValue(cameraPosition);
        _effect.Parameters["Time"]?.SetValue(time);
        _effect.Parameters["SunDirection"]?.SetValue(Vector3.Normalize(new Vector3(-0.45f, -0.9f, 0.22f)));
        _effect.Parameters["AmbientColor"]?.SetValue(new Vector3(0.33f, 0.41f, 0.5f));
        _effect.Parameters["SunColor"]?.SetValue(new Vector3(1.0f, 0.92f, 0.78f));
        _effect.Parameters["HorizonColor"]?.SetValue(new Vector3(0.62f, 0.79f, 0.98f));
        _effect.Parameters["ZenithColor"]?.SetValue(new Vector3(0.19f, 0.43f, 0.85f));
        _effect.Parameters["FogColor"]?.SetValue(new Vector3(0.71f, 0.84f, 0.98f));
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
    }

    public void Dispose()
    {
        foreach (ChunkRenderData chunkMesh in _chunkMeshes.Values)
        {
            chunkMesh.Dispose();
        }

        _chunkMeshes.Clear();
        _pendingBuilds.Clear();
        _effect.Dispose();
        _rasterizerState.Dispose();
    }

    private ChunkRenderData CreateChunkRenderData(WorldMeshData mesh)
    {
        VertexBuffer vertexBuffer = new(_graphicsDevice, VoxelVertex.VertexDeclaration, mesh.Vertices.Length, BufferUsage.WriteOnly);
        vertexBuffer.SetData(mesh.Vertices);

        IndexBuffer indexBuffer = new(_graphicsDevice, IndexElementSize.ThirtyTwoBits, mesh.Indices.Length, BufferUsage.WriteOnly);
        indexBuffer.SetData(mesh.Indices);

        return new ChunkRenderData
        {
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            PrimitiveCount = mesh.Indices.Length / 3
        };
    }
}
