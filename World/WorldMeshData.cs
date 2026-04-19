using Microsoft.Xna.Framework;
using NewProject.Rendering;

namespace NewProject.World;

public sealed class WorldMeshData
{
    public required VoxelVertex[] SolidVertices { get; init; }

    public required int[] SolidIndices { get; init; }

    public required VoxelVertex[] WaterVertices { get; init; }

    public required int[] WaterIndices { get; init; }

    public required Vector3[] TorchLights { get; init; }
}
