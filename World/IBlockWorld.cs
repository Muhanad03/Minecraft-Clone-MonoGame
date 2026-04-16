namespace NewProject.World;

public interface IBlockWorld
{
    int Height { get; }

    bool IsSolid(int x, int y, int z);

    int GetSurfaceHeight(int x, int z);
}
