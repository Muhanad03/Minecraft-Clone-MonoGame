using Microsoft.Xna.Framework;

namespace NewProject.World;

public static class BlockPalette
{
    public static bool IsSolid(BlockType blockType) => blockType != BlockType.Air;

    public static Color GetFaceColor(BlockType blockType, FaceDirection face)
    {
        return blockType switch
        {
            BlockType.Grass when face == FaceDirection.Top => new Color(84, 184, 82),
            BlockType.Grass when face == FaceDirection.Bottom => new Color(76, 56, 42),
            BlockType.Grass => new Color(66, 144, 64),
            BlockType.Dirt when face == FaceDirection.Top => new Color(118, 88, 60),
            BlockType.Dirt => new Color(102, 74, 50),
            BlockType.Stone when face == FaceDirection.Top => new Color(126, 134, 138),
            BlockType.Stone => new Color(100, 108, 114),
            BlockType.Trunk when face == FaceDirection.Top || face == FaceDirection.Bottom => new Color(102, 76, 44),
            BlockType.Trunk => new Color(126, 92, 56),
            BlockType.Leaves when face == FaceDirection.Top => new Color(70, 146, 70),
            BlockType.Leaves => new Color(54, 122, 54),
            _ => Color.Transparent
        };
    }
}
