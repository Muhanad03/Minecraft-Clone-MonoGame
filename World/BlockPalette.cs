using Microsoft.Xna.Framework;
using NewProject.Rendering;

namespace NewProject.World;

public static class BlockPalette
{
    public static bool IsSolid(BlockType blockType) => blockType != BlockType.Air;

    public static Color GetFaceTint(BlockType blockType, FaceDirection face)
    {
        return blockType switch
        {
            BlockType.Grass when face == FaceDirection.Top => new Color(110, 185, 92),
            BlockType.Grass when face == FaceDirection.Bottom => Color.White,
            BlockType.Grass => Color.White,
            BlockType.Leaves => new Color(92, 156, 84),
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
            BlockType.Stone => BlockTextureId.Dirt,
            BlockType.Trunk when face == FaceDirection.Top || face == FaceDirection.Bottom => BlockTextureId.LogTop,
            BlockType.Trunk => BlockTextureId.LogSide,
            BlockType.Leaves => BlockTextureId.Leaves,
            _ => BlockTextureId.Dirt
        };
    }
}
