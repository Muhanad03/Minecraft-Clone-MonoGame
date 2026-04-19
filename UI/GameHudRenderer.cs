using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NewProject.Gameplay;
using NewProject.World;

namespace NewProject.UI;

public sealed class GameHudRenderer : IDisposable
{
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _debugFont;
    private readonly Texture2D _pixelTexture;
    private readonly Dictionary<BlockType, Texture2D> _blockIcons = new();
    private readonly Dictionary<ToolType, Texture2D> _toolIcons = new();

    public GameHudRenderer(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _debugFont = content.Load<SpriteFont>("Fonts/DebugFont");
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        _blockIcons[BlockType.Grass] = content.Load<Texture2D>("Textures/blocks/grass_top");
        _blockIcons[BlockType.Dirt] = content.Load<Texture2D>("Textures/blocks/dirt");
        _blockIcons[BlockType.Stone] = content.Load<Texture2D>("Textures/blocks/stone");
        _blockIcons[BlockType.Sand] = content.Load<Texture2D>("Textures/blocks/sand");
        _blockIcons[BlockType.Torch] = content.Load<Texture2D>("Textures/blocks/torch_on");
        _blockIcons[BlockType.Trunk] = content.Load<Texture2D>("Textures/blocks/log_oak_top");
        _blockIcons[BlockType.Leaves] = content.Load<Texture2D>("Textures/blocks/leaves_oak_opaque");

        _toolIcons[ToolType.Pickaxe] = content.Load<Texture2D>("Textures/items/iron_pickaxe");
        _toolIcons[ToolType.Axe] = content.Load<Texture2D>("Textures/items/iron_axe");
        _toolIcons[ToolType.Sword] = content.Load<Texture2D>("Textures/items/iron_sword");
    }

    public Texture2D GetHeldTexture(HotbarEntry entry) =>
        entry.Kind == HotbarItemKind.Tool ? _toolIcons[entry.Tool] : _blockIcons[entry.Block];

    public void Draw(GraphicsDevice graphicsDevice, HotbarState hotbar, int fps)
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawCrosshair(graphicsDevice.Viewport);
        DrawHotbar(graphicsDevice.Viewport, hotbar);
        _spriteBatch.DrawString(_debugFont, $"FPS: {fps}", new Vector2(12f, 10f), Color.White);
        _spriteBatch.End();
    }

    private void DrawCrosshair(Viewport viewport)
    {
        int centerX = viewport.Width / 2;
        int centerY = viewport.Height / 2;
        Color color = new(255, 255, 255, 220);

        _spriteBatch.Draw(_pixelTexture, new Rectangle(centerX - 8, centerY - 1, 17, 2), color);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(centerX - 1, centerY - 8, 2, 17), color);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(centerX - 6, centerY, 13, 1), new Color(40, 40, 40, 180));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(centerX, centerY - 6, 1, 13), new Color(40, 40, 40, 180));
    }

    private void DrawHotbar(Viewport viewport, HotbarState hotbar)
    {
        int slotSize = 64;
        int gap = 8;
        IReadOnlyList<HotbarEntry> entries = hotbar.Entries;
        int totalWidth = entries.Count * slotSize + (entries.Count - 1) * gap;
        int startX = (viewport.Width - totalWidth) / 2;
        int y = viewport.Height - slotSize - 24;

        for (int i = 0; i < entries.Count; i++)
        {
            Rectangle slotRect = new(startX + i * (slotSize + gap), y, slotSize, slotSize);
            bool selected = i == hotbar.SelectedSlot;
            Color bg = selected ? new Color(42, 42, 42, 220) : new Color(20, 20, 20, 180);
            Color border = selected ? new Color(255, 226, 128) : new Color(180, 180, 180);

            _spriteBatch.Draw(_pixelTexture, slotRect, bg);
            DrawBorder(slotRect, border);

            HotbarEntry entry = entries[i];
            Texture2D icon = GetHeldTexture(entry);
            Rectangle iconRect = new(slotRect.X + 10, slotRect.Y + 8, slotSize - 20, slotSize - 20);
            _spriteBatch.Draw(icon, iconRect, Color.White);

            if (entry.Kind == HotbarItemKind.Block)
            {
                string count = hotbar.GetBlockCount(entry.Block).ToString();
                _spriteBatch.DrawString(_debugFont, count, new Vector2(slotRect.X + 8, slotRect.Bottom - 24), Color.White);
            }
            else
            {
                _spriteBatch.DrawString(_debugFont, (i + 1).ToString(), new Vector2(slotRect.X + 8, slotRect.Bottom - 24), Color.White);
            }
        }
    }

    private void DrawBorder(Rectangle rect, Color color)
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
    }

    public void Dispose()
    {
        _pixelTexture.Dispose();
        _spriteBatch.Dispose();
    }
}
