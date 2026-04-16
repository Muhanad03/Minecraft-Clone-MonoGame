namespace NewProject.World;

public sealed class VoxelWorld
{
    private readonly BlockType[] _blocks;

    public VoxelWorld(int width, int height, int depth)
    {
        Width = width;
        Height = height;
        Depth = depth;
        _blocks = new BlockType[width * height * depth];
    }

    public int Width { get; }

    public int Height { get; }

    public int Depth { get; }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z))
        {
            return BlockType.Air;
        }

        return _blocks[GetIndex(x, y, z)];
    }

    public void SetBlock(int x, int y, int z, BlockType block)
    {
        if (!IsInBounds(x, y, z))
        {
            return;
        }

        _blocks[GetIndex(x, y, z)] = block;
    }

    public bool IsSolid(int x, int y, int z)
    {
        return BlockPalette.IsSolid(GetBlock(x, y, z));
    }

    public int GetSurfaceHeight(int x, int z)
    {
        for (int y = Height - 1; y >= 0; y--)
        {
            if (IsSolid(x, y, z))
            {
                return y + 1;
            }
        }

        return 0;
    }

    private bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth;
    }

    private int GetIndex(int x, int y, int z)
    {
        return x + Width * (z + Depth * y);
    }
}
