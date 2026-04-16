using Microsoft.Xna.Framework;
using NewProject.Rendering;

namespace NewProject.World;

public static class BlockPalette
{
    public static bool IsSolid(BlockType blockType) => blockType != BlockType.Air && blockType != BlockType.Water;

    public static Color GetFaceTint(BlockType blockType, FaceDirection face)
    {
        return blockType switch
        {
            BlockType.Grass when face == FaceDirection.Top => new Color(110, 185, 92),
            BlockType.Grass when face == FaceDirection.Bottom => Color.White,
            BlockType.Grass => Color.White,
            BlockType.Leaves => new Color(92, 156, 84),
            BlockType.Water => new Color(150, 190, 255, 150),
            BlockType.Sand => new Color(236, 223, 164),
            _ => Color.White
        };
    }

    public static BlockTextureId GetTexture(BlockType blockType, FaceDirection face)
    {
        return blockType switch
        {
            BlockType.Grass when face == FaceDirection.Top => BlockTextureId.GrassTop,
            BlockType.Grass when face == FaceDirection.Bottom => BlockTextureId.Dirt,
            BlockType.Grass => BlockTextureId.GrassSide,
            BlockType.Dirt => BlockTextureId.Dirt,
            BlockType.Stone => BlockTextureId.Stone,
            BlockType.Sand => BlockTextureId.Sand,
            BlockType.Trunk when face == FaceDirection.Top || face == FaceDirection.Bottom => BlockTextureId.LogTop,
            BlockType.Trunk => BlockTextureId.LogSide,
            BlockType.Leaves => BlockTextureId.Leaves,
            BlockType.Water => BlockTextureId.Water,
            _ => BlockTextureId.Dirt
        };
    }
}
