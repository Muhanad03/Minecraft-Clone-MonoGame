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
