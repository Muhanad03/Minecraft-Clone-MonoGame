using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace NewProject.Rendering;

public sealed class ChunkRenderData : IDisposable
{
    public required VertexBuffer VertexBuffer { get; init; }

    public required IndexBuffer IndexBuffer { get; init; }

    public required int PrimitiveCount { get; init; }

    public void Dispose()
    {
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}

public sealed class ChunkRenderSet : IDisposable
{
    public required ChunkRenderData Solid { get; init; }

    public ChunkRenderData? Water { get; init; }

    public required Vector3[] TorchLights { get; init; }

    public void Dispose()
    {
        Solid.Dispose();
        Water?.Dispose();
    }
}
