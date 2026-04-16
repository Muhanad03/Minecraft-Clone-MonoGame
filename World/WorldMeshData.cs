using NewProject.Rendering;

namespace NewProject.World;

public sealed class WorldMeshData
{
    public required VoxelVertex[] Vertices { get; init; }

    public required int[] Indices { get; init; }
}
