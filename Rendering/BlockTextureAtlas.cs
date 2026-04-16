using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace NewProject.Rendering;

public enum BlockTextureId
{
    GrassTop,
    GrassSide,
    Dirt,
    LogSide,
    LogTop,
    Leaves
}

public sealed class BlockTextureAtlas
{
    private readonly Dictionary<BlockTextureId, Vector4> _uvRegions = new();

    public BlockTextureAtlas(GraphicsDevice graphicsDevice, ContentManager content)
    {
        (BlockTextureId Id, string Asset)[] entries =
        [
            (BlockTextureId.GrassTop, "Textures/blocks/grass_top"),
            (BlockTextureId.GrassSide, "Textures/blocks/grass_side"),
            (BlockTextureId.Dirt, "Textures/blocks/dirt"),
            (BlockTextureId.LogSide, "Textures/blocks/log_oak"),
            (BlockTextureId.LogTop, "Textures/blocks/log_oak_top"),
            (BlockTextureId.Leaves, "Textures/blocks/leaves_oak_opaque")
        ];

        Texture2D[] textures = new Texture2D[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            textures[i] = content.Load<Texture2D>(entries[i].Asset);
        }

        int tileSize = textures[0].Width;
        int columns = 3;
        int rows = 2;
        Texture = new Texture2D(graphicsDevice, columns * tileSize, rows * tileSize);

        Color[] atlasData = new Color[Texture.Width * Texture.Height];

        for (int i = 0; i < entries.Length; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Texture2D source = textures[i];
            Color[] sourceData = new Color[source.Width * source.Height];
            source.GetData(sourceData);

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    int atlasX = column * tileSize + x;
                    int atlasY = row * tileSize + y;
                    atlasData[atlasY * Texture.Width + atlasX] = sourceData[y * source.Width + x];
                }
            }

            float u0 = (column * tileSize + 0.01f) / Texture.Width;
            float v0 = (row * tileSize + 0.01f) / Texture.Height;
            float u1 = ((column + 1) * tileSize - 0.01f) / Texture.Width;
            float v1 = ((row + 1) * tileSize - 0.01f) / Texture.Height;
            _uvRegions[entries[i].Id] = new Vector4(u0, v0, u1, v1);
        }

        Texture.SetData(atlasData);
    }

    public Texture2D Texture { get; }

    public Vector2[] GetFaceUvs(BlockTextureId textureId)
    {
        Vector4 region = _uvRegions[textureId];
        return
        [
            new Vector2(region.X, region.W),
            new Vector2(region.Z, region.W),
            new Vector2(region.Z, region.Y),
            new Vector2(region.X, region.Y)
        ];
    }
}
