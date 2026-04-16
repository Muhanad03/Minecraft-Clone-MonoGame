using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NewProject.Rendering;

public readonly struct VoxelVertex : IVertexType
{
    public readonly Vector3 Position;
    public readonly Vector3 Normal;
    public readonly Color Color;

    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0));

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public VoxelVertex(Vector3 position, Vector3 normal, Color color)
    {
        Position = position;
        Normal = normal;
        Color = color;
    }
}
