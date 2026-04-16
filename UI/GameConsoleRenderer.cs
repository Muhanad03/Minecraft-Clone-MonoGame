using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NewProject.Gameplay;

namespace NewProject.UI;

public sealed class GameConsoleRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;

    public GameConsoleRenderer(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = content.Load<SpriteFont>("Fonts/DebugFont");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public void Draw(GraphicsDevice graphicsDevice, GameConsole console)
    {
        if (!console.IsOpen)
        {
            return;
        }

        Viewport viewport = graphicsDevice.Viewport;
        int panelHeight = 190;
        Rectangle panel = new(20, viewport.Height - panelHeight - 18, viewport.Width - 40, panelHeight);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_pixel, panel, new Color(8, 10, 16, 220));
        _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Y, panel.Width, 2), new Color(110, 180, 255));
        _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Bottom - 2, panel.Width, 2), new Color(110, 180, 255));
        _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Y, 2, panel.Height), new Color(110, 180, 255));
        _spriteBatch.Draw(_pixel, new Rectangle(panel.Right - 2, panel.Y, 2, panel.Height), new Color(110, 180, 255));

        int visibleLineCount = 8;
        int startIndex = console.Lines.Count - visibleLineCount - console.ScrollOffset;
        if (startIndex < 0)
        {
            startIndex = 0;
        }

        int endIndex = startIndex + visibleLineCount;
        if (endIndex > console.Lines.Count)
        {
            endIndex = console.Lines.Count;
        }
        Vector2 cursor = new(panel.X + 12, panel.Y + 12);
        for (int i = startIndex; i < endIndex; i++)
        {
            _spriteBatch.DrawString(_font, console.Lines[i], cursor, Color.White);
            cursor.Y += 16f;
        }

        if (console.Lines.Count > visibleLineCount)
        {
            _spriteBatch.DrawString(_font, $"Scroll: {console.ScrollOffset}", new Vector2(panel.Right - 110, panel.Y + 10), new Color(160, 190, 220));
        }

        Rectangle inputBar = new(panel.X + 10, panel.Bottom - 34, panel.Width - 20, 22);
        _spriteBatch.Draw(_pixel, inputBar, new Color(24, 28, 38, 235));
        _spriteBatch.DrawString(_font, $"{console.Input}_", new Vector2(inputBar.X + 8, inputBar.Y + 2), new Color(255, 226, 128));
        _spriteBatch.End();
    }
}
