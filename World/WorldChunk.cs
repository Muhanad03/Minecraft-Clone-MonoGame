using Microsoft.Xna.Framework;

namespace NewProject.World;

public sealed class WorldChunk
{
    public required int ChunkX { get; init; }

    public required int ChunkZ { get; init; }

    public required VoxelWorld Voxels { get; init; }

    public Point Key => new(ChunkX, ChunkZ);
}
